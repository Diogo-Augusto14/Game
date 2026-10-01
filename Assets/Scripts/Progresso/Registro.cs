using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tudo o que fica salvo entre partidas alem dos herois (<see cref="Progresso"/>): os numeros
/// de todas as partidas, o bestiario (quem ja foi visto, quantos de cada um caiu, quem mais
/// matou o jogador) e as conquistas (<see cref="Conquistas"/>). Fica no PlayerPrefs.
///
/// Quem avisa: o <see cref="InimigoDeSala"/> (acordou, morreu), o <see cref="Andar"/> (sala
/// nova, morte, vitoria, mundo fechado), os itens (pegou, sinergia, ativo) e as salas especiais.
/// Os contadores "desta partida" zeram quando a cena carrega (cada partida recarrega a cena).
/// </summary>
public static class Registro
{
    private const string Prefixo = "ThePrettie.registro.";

    // ---------------- desta partida ----------------
    public static int ItensNaPartida { get; private set; }
    public static int SacrificiosNaPartida { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Preparar()
    {
        ZerarPartida();
        SceneManager.sceneLoaded -= AoCarregar;
        SceneManager.sceneLoaded += AoCarregar;
        InimigoDeSala.AlgumMorreu -= AoMorrerInimigo;
        InimigoDeSala.AlgumMorreu += AoMorrerInimigo;
        ItemAtivoDoJogador.AoUsar -= AoUsarAtivo;
        ItemAtivoDoJogador.AoUsar += AoUsarAtivo;
    }

    private static void AoCarregar(Scene cena, LoadSceneMode modo)
    {
        if (modo != LoadSceneMode.Single)
            return;

        ZerarPartida();

        // Recomecar depois de morrer recarrega a cena direto no jogo: partida nova. (Do menu,
        // conta no Jogar: TelaDeInicio chama ComecouPartida.)
        if (TelaDeInicio.JaPassou)
            ComecouPartida();
    }

    private static void ZerarPartida()
    {
        ItensNaPartida = 0;
        SacrificiosNaPartida = 0;
    }

    // ---------------- numeros de sempre ----------------
    public static int Partidas => Ler("partidas");
    public static int Mortes => Ler("mortes");
    public static int InimigosDerrotados => Ler("inimigos");
    public static int ChefesDerrotados => Ler("chefes");
    public static int SalasExploradas => Ler("salas");
    public static int ItensPegos => Ler("itens");
    public static int SinergiasFormadas => Ler("sinergias");
    public static int AtivosUsados => Ler("ativos");
    public static float TempoJogado => PlayerPrefs.GetFloat(Prefixo + "tempo", 0f);

    /// <summary>Mais longe que ja chegou: mundo * 10 + fase (0 = nunca jogou).</summary>
    public static int MaisLonge => Ler("mais-longe");

    public static int Vistos(TipoDeInimigo tipo) => Ler("visto." + tipo);
    public static bool JaViu(TipoDeInimigo tipo) => Vistos(tipo) > 0;
    public static int Derrotados(TipoDeInimigo tipo) => Ler("derrotou." + tipo);
    public static int MortesPor(TipoDeInimigo tipo) => Ler("matou." + tipo);

    public static int TiposVistos
    {
        get
        {
            int n = 0;

            foreach (Bestiario.Ficha f in Bestiario.Todas)
                if (JaViu(f.Tipo))
                    n++;

            return n;
        }
    }

    /// <summary>O inimigo que mais matou o jogador (null se ninguem ainda).</summary>
    public static TipoDeInimigo? Carrasco
    {
        get
        {
            TipoDeInimigo? pior = null;
            int maior = 0;

            foreach (Bestiario.Ficha f in Bestiario.Todas)
            {
                int n = MortesPor(f.Tipo);

                if (n > maior)
                {
                    maior = n;
                    pior = f.Tipo;
                }
            }

            return pior;
        }
    }

    // ---------------- avisos ----------------
    public static void ComecouPartida()
    {
        // Partida nova: a salva (se tinha) deixa de valer.
        Salvamento.Apagar();
        Somar("partidas");
        Salvar();
    }

    public static void Viu(TipoDeInimigo tipo)
    {
        if (JaViu(tipo))
            return;

        Somar("visto." + tipo);
        Salvar();

        if (TiposVistos >= 30)
            Conquistas.Conquistar(Conquistas.Naturalista);
    }

    private static void AoMorrerInimigo(InimigoDeSala inimigo)
    {
        if (inimigo == null)
            return;

        Somar("inimigos");
        Somar("derrotou." + inimigo.Tipo);

        if (InimigosDerrotados >= 150)
            Conquistas.Conquistar(Conquistas.Cacador);

        if (inimigo is IChefe)
        {
            Somar("chefes");
            Conquistas.Conquistar(Conquistas.PrimeiroSangue);

            if (inimigo.Tipo == TipoDeInimigo.ChefeLobisomem)
                Conquistas.Conquistar(Conquistas.LuaMinguante);
            else if (inimigo.Tipo == TipoDeInimigo.ChefeOrc)
                Conquistas.Conquistar(Conquistas.FimDaGuerra);
            else if (inimigo.Tipo == TipoDeInimigo.ChefeFinal)
                Conquistas.Conquistar(Conquistas.LendaDoAbismo);

            Salvar();
        }
    }

    public static void ExplorouSala() => Somar("salas");

    public static void ChegouEm(int mundo, int fase)
    {
        int aqui = mundo * 10 + fase;

        if (aqui > MaisLonge)
        {
            PlayerPrefs.SetInt(Prefixo + "mais-longe", aqui);
            Salvar();
        }
    }

    public static void FechouMundo(int mundo)
    {
        if (mundo >= 1)
            Conquistas.Conquistar(Conquistas.SaidaDoPorao);
    }

    public static void PegouItem()
    {
        Somar("itens");
        ItensNaPartida++;

        if (ItensNaPartida >= 10)
            Conquistas.Conquistar(Conquistas.Colecionador);
    }

    public static void FormouSinergia()
    {
        Somar("sinergias");
        Conquistas.Conquistar(Conquistas.Alquimista);
        Salvar();
    }

    private static void AoUsarAtivo(ItemPassivo item)
    {
        Somar("ativos");

        if (AtivosUsados >= 10)
            Conquistas.Conquistar(Conquistas.MaoNaMassa);
    }

    public static void Sacrificou()
    {
        SacrificiosNaPartida++;

        if (SacrificiosNaPartida >= 3)
            Conquistas.Conquistar(Conquistas.DoadorDeSangue);
    }

    public static void Morreu(GameObject atacante)
    {
        Somar("mortes");
        GuardarTempo();

        InimigoDeSala quem = atacante != null ? atacante.GetComponentInParent<InimigoDeSala>() : null;

        if (quem != null)
            Somar("matou." + quem.Tipo);

        if (Mortes >= 5)
            Conquistas.Conquistar(Conquistas.Persistente);

        Salvar();
    }

    public static void Venceu()
    {
        GuardarTempo();
        Salvar();
    }

    private static void GuardarTempo()
    {
        PlayerPrefs.SetFloat(Prefixo + "tempo", TempoJogado + ResumoDaPartida.Tempo);
    }

    /// <summary>Zera estatisticas, bestiario e conquistas (os herois ficam no Progresso.Apagar).</summary>
    public static void Apagar()
    {
        foreach (string chave in new[] { "partidas", "mortes", "inimigos", "chefes", "salas", "itens", "sinergias", "ativos", "mais-longe", "tempo" })
            PlayerPrefs.DeleteKey(Prefixo + chave);

        foreach (Bestiario.Ficha f in Bestiario.Todas)
        {
            PlayerPrefs.DeleteKey(Prefixo + "visto." + f.Tipo);
            PlayerPrefs.DeleteKey(Prefixo + "derrotou." + f.Tipo);
            PlayerPrefs.DeleteKey(Prefixo + "matou." + f.Tipo);
        }

        Conquistas.Apagar();
        Salvar();
    }

    // ---------------- PlayerPrefs ----------------
    private static int Ler(string chave) => PlayerPrefs.GetInt(Prefixo + chave, 0);

    private static void Somar(string chave, int quanto = 1) => PlayerPrefs.SetInt(Prefixo + chave, Ler(chave) + quanto);

    private static void Salvar() => PlayerPrefs.Save();
}
