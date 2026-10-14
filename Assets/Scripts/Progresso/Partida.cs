using UnityEngine;

/// <summary>Como a partida e jogada: a normal, o modo maldito (liberado ao zerar) ou o desafio do dia.</summary>
public enum ModoDeJogo
{
    Normal,

    /// <summary>Inimigos mais rapidos, mais cheios e com mais ondas; premios melhores e cristais em dobro.</summary>
    Maldito,

    /// <summary>O mapa do dia: todo mundo joga os mesmos andares hoje, sem as melhorias da estatua. Vale o recorde.</summary>
    Diario,
}

/// <summary>
/// O modo da partida de agora (<see cref="ModoDeJogo"/>) e o que ele muda. Escolhido no menu inicial
/// (<see cref="TelaDosModos"/>); fica guardado enquanto o jogo esta aberto, entao "tentar de novo" repete
/// o modo (e o desafio do dia repete o mesmo mapa). O salvamento guarda e devolve.
///
/// O desafio do dia: cada andar e montado com uma semente tirada da data (em UTC, o mesmo dia pra todo
/// mundo) e do numero do andar (<see cref="Semear"/>), entao o mapa, as salas, os inimigos e os campeoes
/// saem iguais; o que acontece na luta (o que cai dos baus, os tiros) continua sorteado. O recorde e
/// guardado por dia, neste computador (<see cref="RecordeDoDia"/>).
///
/// O modo maldito: os inimigos andam e atacam 25% mais rapido, as salas tem 25% mais inimigos e uma onda a
/// mais, os campeoes saem o dobro; em troca, mais sorte nos premios, armas raras mais cedo e cristais em dobro.
/// </summary>
public static class Partida
{
    private const string Prefixo = "ThePrettie.partida.";

    /// <summary>O modo desta partida.</summary>
    public static ModoDeJogo Modo { get; set; }

    /// <summary>O dia do desafio desta partida (aaaammdd); so vale no <see cref="ModoDeJogo.Diario"/>.</summary>
    public static int Dia { get; set; }

    public static bool Maldito => Modo == ModoDeJogo.Maldito;

    public static bool Diario => Modo == ModoDeJogo.Diario;

    /// <summary>Cristais pegos nesta partida (a tela do fim mostra).</summary>
    public static int CristaisNestaPartida { get; set; }

    /// <summary>O dia de hoje como numero (aaaammdd), em UTC: o mesmo pra todo mundo.</summary>
    public static int Hoje
    {
        get
        {
            System.DateTime d = System.DateTime.UtcNow;
            return d.Year * 10000 + d.Month * 100 + d.Day;
        }
    }

    /// <summary>O modo maldito aparece no menu depois de zerar o jogo uma vez.</summary>
    public static bool MalditoLiberado => Progresso.Vitorias > 0;

    /// <summary>"Normal", "Maldito" ou "Desafio de 14/10".</summary>
    public static string NomeDoModo =>
        Maldito ? "Modo maldito" : Diario ? $"Desafio de {Dia % 100:00}/{Dia / 100 % 100:00}" : "Normal";

    /// <summary>Uma partida nova comecou (os numeros do modo voltam do zero).</summary>
    public static void Comecar(ModoDeJogo modo)
    {
        Modo = modo;
        CristaisNestaPartida = 0;

        if (modo == ModoDeJogo.Diario)
            Dia = Hoje;
    }

    /// <summary>
    /// No desafio do dia, poe o sorteio no ponto do andar (chamado antes de montar cada andar): o mesmo
    /// dia e o mesmo andar dao o mesmo mapa. Nos outros modos nao mexe em nada.
    /// </summary>
    public static void Semear(int andar)
    {
        if (Diario)
            Random.InitState(unchecked(Dia * 7919 + andar * 104729 + 17));
    }

    // ---------------- o que o modo maldito muda ----------------
    /// <summary>Quanto mais rapido os inimigos e os chefes andam e atacam.</summary>
    public static float RitmoDosInimigos => Maldito ? 1.25f : 1f;

    /// <summary>Quantos inimigos a mais em cada sala (1 = igual).</summary>
    public static float InimigosVezes => Maldito ? 1.25f : 1f;

    /// <summary>Ondas a mais em cada sala de luta.</summary>
    public static int OndasAMais => Maldito ? 1 : 0;

    /// <summary>A sorte nos premios que caem dos inimigos (1 = igual).</summary>
    public static float SorteVezes => Maldito ? 1.5f : 1f;

    /// <summary>Andares a mais na conta da raridade dos baus (as raras saem mais cedo).</summary>
    public static int AndaresDeRaridade => Maldito ? 3 : 0;

    public static int CristaisVezes => Maldito ? 2 : 1;

    /// <summary>A chance de um inimigo de sala vir campeao (<see cref="Campeao"/>), que sobe com o andar.</summary>
    public static float ChanceDeCampeao(int andar) => Mathf.Min(0.16f, 0.04f + 0.01f * andar) * (Maldito ? 2f : 1f);

    // ---------------- o recorde do desafio do dia ----------------
    /// <summary>Os pontos de uma partida: andares, chefes, inimigos e um tanto a mais por zerar.</summary>
    public static int Pontos(int andar, bool venceu) =>
        (venceu ? 5000 : 0) + andar * 500 + ResumoDaPartida.ChefesDerrotados * 1000 + ResumoDaPartida.InimigosDerrotados * 5;

    /// <summary>Os pontos da partida que acabou de terminar (a tela do fim mostra no desafio do dia).</summary>
    public static int UltimosPontos { get; private set; }

    /// <summary>A partida que acabou de terminar bateu o recorde do dia.</summary>
    public static bool BateuORecorde { get; private set; }

    /// <summary>O melhor de hoje (0 = ainda nao jogou o desafio de hoje).</summary>
    public static int RecordeDoDia(int dia) => PlayerPrefs.GetInt(Prefixo + "recorde." + dia, 0);

    /// <summary>"Andar 5, 62 inimigos" do recorde do dia ("" sem recorde).</summary>
    public static string ResumoDoRecorde(int dia) => PlayerPrefs.GetString(Prefixo + "resumo." + dia, "");

    /// <summary>
    /// A partida acabou (morreu ou venceu): no desafio do dia, confere o recorde. Devolve true se bateu.
    /// </summary>
    public static bool Terminar(int andar, bool venceu)
    {
        UltimosPontos = Pontos(andar, venceu);
        BateuORecorde = false;

        if (!Diario)
            return false;

        int pontos = UltimosPontos;

        if (pontos <= RecordeDoDia(Dia))
            return false;

        BateuORecorde = true;

        PlayerPrefs.SetInt(Prefixo + "recorde." + Dia, pontos);
        PlayerPrefs.SetString(Prefixo + "resumo." + Dia,
            (venceu ? "Zerou" : $"Andar {andar}") + $", {ResumoDaPartida.InimigosDerrotados} inimigos, {ResumoDaPartida.TempoFormatado()}");
        PlayerPrefs.Save();
        return true;
    }
}
