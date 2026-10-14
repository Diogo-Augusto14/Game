using UnityEngine;

/// <summary>As melhorias que ficam entre partidas, compradas com cristais na estatua.</summary>
public enum Melhoria
{
    /// <summary>Um coracao a mais no comeco de cada partida.</summary>
    Vida,

    /// <summary>A loja fica mais barata.</summary>
    Desconto,

    /// <summary>Comeca a partida com chave e moedas.</summary>
    Chaveiro,

    /// <summary>Armas novas passam a sair dos baus (<see cref="DadosDaArma.arsenal"/>).</summary>
    Arsenal,

    /// <summary>O tiro especial recarrega mais rapido.</summary>
    Polvora,
}

/// <summary>
/// A progressao entre partidas: os cristais (que caem dos campeoes e dos chefes e ficam guardados na hora
/// de pegar, mesmo morrendo) e as melhorias compradas com eles na estatua dos cristais
/// (<see cref="EstatuaDosCristais"/>), que fica no comeco do primeiro andar e nas areas seguras.
///
/// Cada melhoria tem niveis, cada nivel mais caro. No desafio do dia elas nao valem (todo mundo joga
/// igual). Fica no PlayerPrefs.
/// </summary>
public static class Melhorias
{
    private const string Prefixo = "ThePrettie.melhorias.";

    public class Dados
    {
        public Melhoria Qual;
        public string Nome;

        /// <summary>O que cada nivel da ("+1 coração").</summary>
        public string PorNivel;

        /// <summary>O preco de cada nivel, em cristais (o tamanho e o nivel maximo).</summary>
        public int[] Precos;
    }

    public static readonly Dados[] Todas =
    {
        new Dados { Qual = Melhoria.Vida, Nome = "Coração de pedra", PorNivel = "+1 coração no começo da partida", Precos = new[] { 4, 9, 16 } },
        new Dados { Qual = Melhoria.Desconto, Nome = "Amigo do mercador", PorNivel = "loja 10% mais barata", Precos = new[] { 3, 7, 12 } },
        new Dados { Qual = Melhoria.Chaveiro, Nome = "Chaveiro", PorNivel = "começa com 1 chave e 5 moedas", Precos = new[] { 3, 8 } },
        new Dados { Qual = Melhoria.Arsenal, Nome = "Arsenal", PorNivel = "armas novas nos baús", Precos = new[] { 5, 10, 18 } },
        new Dados { Qual = Melhoria.Polvora, Nome = "Pólvora antiga", PorNivel = "tiro especial recarrega 10% mais rápido", Precos = new[] { 4, 8, 14 } },
    };

    /// <summary>O que cada nivel do Arsenal libera (pro texto da estatua).</summary>
    public static readonly string[] NoArsenal = { "Cajado do Trovão", "Lâmina Real e Bumerangue de Prata", "Cetro do Abismo" };

    private static int cristais = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar() => cristais = -1;

    /// <summary>Os cristais guardados (lidos uma vez; a HUD pergunta todo quadro).</summary>
    public static int Cristais
    {
        get
        {
            if (cristais < 0)
                cristais = PlayerPrefs.GetInt(Prefixo + "cristais", 0);

            return cristais;
        }
    }

    private static void GuardarCristais(int quantos)
    {
        cristais = Mathf.Max(0, quantos);
        PlayerPrefs.SetInt(Prefixo + "cristais", cristais);
    }

    /// <summary>Todos os cristais ja pegos (estatistica).</summary>
    public static int CristaisNoTotal => PlayerPrefs.GetInt(Prefixo + "cristaisNoTotal", 0);

    /// <summary>As melhorias valem nesta partida (no desafio do dia, nao).</summary>
    public static bool Valendo => !Partida.Diario;

    public static Dados De(Melhoria qual) => System.Array.Find(Todas, d => d.Qual == qual);

    /// <summary>O nivel comprado (0 = nada).</summary>
    public static int Nivel(Melhoria qual) => PlayerPrefs.GetInt(Prefixo + qual, 0);

    /// <summary>O nivel que vale na partida (0 no desafio do dia).</summary>
    public static int NivelNaPartida(Melhoria qual) => Valendo ? Nivel(qual) : 0;

    public static bool NoMaximo(Melhoria qual) => Nivel(qual) >= De(qual).Precos.Length;

    /// <summary>O preco do proximo nivel (0 no maximo).</summary>
    public static int Preco(Melhoria qual) => NoMaximo(qual) ? 0 : De(qual).Precos[Nivel(qual)];

    /// <summary>Guarda cristais (pegou do chao). Ja fica salvo.</summary>
    public static void Ganhar(int quantos)
    {
        if (quantos <= 0)
            return;

        GuardarCristais(Cristais + quantos);
        PlayerPrefs.SetInt(Prefixo + "cristaisNoTotal", CristaisNoTotal + quantos);
        PlayerPrefs.Save();
        Partida.CristaisNestaPartida += quantos;
    }

    /// <summary>Compra o proximo nivel. False sem cristais suficientes (ou ja no maximo).</summary>
    public static bool Comprar(Melhoria qual)
    {
        int preco = Preco(qual);

        if (NoMaximo(qual) || Cristais < preco)
            return false;

        GuardarCristais(Cristais - preco);
        PlayerPrefs.SetInt(Prefixo + qual, Nivel(qual) + 1);
        PlayerPrefs.Save();
        return true;
    }

    // ---------------- o que cada uma muda ----------------
    /// <summary>Vida a mais (2 = um coracao) no comeco da partida.</summary>
    public static int VidaAMais => NivelNaPartida(Melhoria.Vida) * 2;

    /// <summary>O quanto a loja cobra (1 = o preco cheio).</summary>
    public static float PrecoNaLoja => 1f - 0.1f * NivelNaPartida(Melhoria.Desconto);

    /// <summary>O quanto a recarga do tiro especial dura (1 = igual).</summary>
    public static float RecargaDoEspecial => 1f - 0.1f * NivelNaPartida(Melhoria.Polvora);

    /// <summary>
    /// A arma ja pode sair dos baus, da fonte e do chao: as do Arsenal precisam do nivel dela (no desafio
    /// do dia, todas saem).
    /// </summary>
    public static bool Liberada(DadosDaArma arma) =>
        arma != null && (arma.arsenal <= 0 || Partida.Diario || Nivel(Melhoria.Arsenal) >= arma.arsenal);

    /// <summary>
    /// Uma partida nova comecou (nao o "Continuar"): a chave e as moedas do Chaveiro. A vida a mais entra
    /// pelo <see cref="Herois.Aplicar"/>.
    /// </summary>
    public static void AplicarNoComeco(GameObject jogador)
    {
        int chaveiro = NivelNaPartida(Melhoria.Chaveiro);

        if (jogador != null && chaveiro > 0 && jogador.TryGetComponent(out Bolsa bolsa))
            bolsa.Ganhar(5 * chaveiro, chaveiro);
    }

    /// <summary>Zera os cristais e as melhorias (o "apagar progresso").</summary>
    public static void Apagar()
    {
        PlayerPrefs.DeleteKey(Prefixo + "cristais");
        cristais = -1;
        PlayerPrefs.DeleteKey(Prefixo + "cristaisNoTotal");

        foreach (Dados d in Todas)
            PlayerPrefs.DeleteKey(Prefixo + d.Qual);

        PlayerPrefs.Save();
    }
}
