using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tudo o que fica salvo entre partidas alem dos herois (<see cref="Progresso"/>), como no jogo
/// antigo: os numeros de todas as partidas, o bestiario (quem ja foi visto, quantos de cada um
/// caiu, quem mais matou o jogador) e as conquistas (<see cref="Conquistas"/>). Fica no PlayerPrefs.
///
/// Quem avisa: o <see cref="InimigoAtirador"/> e o <see cref="Chefe"/> (viu), o
/// <see cref="GeradorDoAndar"/> (morreu um inimigo, andar novo, vitoria), a
/// <see cref="MorteDoJogador"/> e a <see cref="ArmaDoJogador"/> (pegou arma).
/// </summary>
public static class Registro
{
    private const string Prefixo = "ThePrettie.registro.";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Preparar()
    {
        SceneManager.sceneLoaded -= AoCarregar;
        SceneManager.sceneLoaded += AoCarregar;
    }

    private static void AoCarregar(Scene cena, LoadSceneMode modo)
    {
        // "Tentar de novo" recarrega a cena direto no jogo: partida nova. (Do menu, conta no Jogar.)
        if (modo == LoadSceneMode.Single && TelaDeInicio.JaPassou)
            ComecouPartida();
    }

    // ---------------- numeros de sempre ----------------
    public static int Partidas => Ler("partidas");
    public static int Mortes => Ler("mortes");
    public static int InimigosDerrotados => Ler("inimigos");
    public static int ChefesDerrotados => Ler("chefes");
    public static int ArmasPegas => Ler("armas");
    public static int HabilidadesUsadas => Ler("habilidades");
    public static float TempoJogado => PlayerPrefs.GetFloat(Prefixo + "tempo", 0f);

    /// <summary>O andar mais fundo que ja alcancou (0 = nunca jogou).</summary>
    public static int MaisFundo => Ler("mais-fundo");

    public static bool JaViu(string id) => Ler("visto." + id) > 0;
    public static int Derrotados(string id) => Ler("derrotou." + id);
    public static int MortesPor(string id) => Ler("matou." + id);

    public static int TiposVistos
    {
        get
        {
            int n = 0;

            foreach (Bestiario.Ficha f in Bestiario.Todas)
            {
                if (JaViu(f.Id))
                    n++;
            }

            return n;
        }
    }

    /// <summary>O inimigo que mais matou o jogador (null se ninguem ainda).</summary>
    public static string Carrasco
    {
        get
        {
            string pior = null;
            int maior = 0;

            foreach (Bestiario.Ficha f in Bestiario.Todas)
            {
                int n = MortesPor(f.Id);

                if (n > maior)
                {
                    maior = n;
                    pior = f.Id;
                }
            }

            return pior;
        }
    }

    /// <summary>A ficha de quem e este objeto (o nome do prefab, sem o "(Clone)").</summary>
    public static string Id(GameObject obj)
    {
        if (obj == null)
            return null;

        string nome = obj.name;
        int clone = nome.IndexOf("(Clone)", System.StringComparison.Ordinal);
        nome = (clone >= 0 ? nome.Substring(0, clone) : nome).Trim();

        // As bolinhas sao da Bolha.
        return nome == "Bolinha" ? "Bolha" : nome;
    }

    // ---------------- avisos ----------------
    public static void ComecouPartida()
    {
        Somar("partidas");
        Salvar();
    }

    public static void Viu(GameObject inimigo)
    {
        string id = Id(inimigo);

        if (id == null || JaViu(id))
            return;

        Somar("visto." + id);
        Salvar();

        if (TiposVistos >= 25)
            Conquistas.Conquistar(Conquistas.Naturalista);
    }

    public static void Derrotou(GameObject inimigo, bool chefe)
    {
        string id = Id(inimigo);

        if (id == null)
            return;

        Somar("inimigos");
        Somar("derrotou." + id);

        if (InimigosDerrotados >= 150)
            Conquistas.Conquistar(Conquistas.Cacador);

        if (InimigosDerrotados >= 1000)
            Conquistas.Conquistar(Conquistas.Exterminador);

        if (chefe)
        {
            Somar("chefes");
            Conquistas.Conquistar(Conquistas.PrimeiroSangue);
            Conquistas.Conquistar(Conquistas.DoChefe(id));
            Salvar();
        }
    }

    public static void ChegouNoAndar(int andar)
    {
        if (andar > MaisFundo)
        {
            PlayerPrefs.SetInt(Prefixo + "mais-fundo", andar);
            Salvar();
        }
    }

    public static void PegouArma()
    {
        Somar("armas");

        if (ArmasPegas >= 25)
            Conquistas.Conquistar(Conquistas.Arsenal);
    }

    public static void UsouHabilidade()
    {
        Somar("habilidades");

        if (HabilidadesUsadas >= 50)
            Conquistas.Conquistar(Conquistas.Especialista);
    }

    public static void Morreu(GameObject atacante)
    {
        Somar("mortes");
        GuardarTempo();
        string id = atacante != null ? Id(atacante.GetComponentInParent<Vida>() != null ? atacante.GetComponentInParent<Vida>().gameObject : atacante) : null;

        if (id != null)
            Somar("matou." + id);

        if (Mortes >= 5)
            Conquistas.Conquistar(Conquistas.Persistente);

        Salvar();
    }

    public static void Venceu()
    {
        GuardarTempo();
        Salvar();
    }

    private static void GuardarTempo() => PlayerPrefs.SetFloat(Prefixo + "tempo", TempoJogado + ResumoDaPartida.Tempo);

    /// <summary>Zera estatisticas, bestiario e conquistas (os herois ficam no Progresso.Apagar).</summary>
    public static void Apagar()
    {
        foreach (string chave in new[] { "partidas", "mortes", "inimigos", "chefes", "armas", "habilidades", "mais-fundo", "tempo" })
            PlayerPrefs.DeleteKey(Prefixo + chave);

        foreach (Bestiario.Ficha f in Bestiario.Todas)
        {
            PlayerPrefs.DeleteKey(Prefixo + "visto." + f.Id);
            PlayerPrefs.DeleteKey(Prefixo + "derrotou." + f.Id);
            PlayerPrefs.DeleteKey(Prefixo + "matou." + f.Id);
        }

        Conquistas.Apagar();
        Salvar();
    }

    // ---------------- PlayerPrefs ----------------
    private static int Ler(string chave) => PlayerPrefs.GetInt(Prefixo + chave, 0);

    private static void Somar(string chave, int quanto = 1) => PlayerPrefs.SetInt(Prefixo + chave, Ler(chave) + quanto);

    private static void Salvar() => PlayerPrefs.Save();
}
