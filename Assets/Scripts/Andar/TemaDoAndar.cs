using UnityEngine;

/// <summary>
/// A cara de cada mundo: nome, chao, paredes, enfeites e quem mora nele.
///   Mundo 1 -> Porao      pedra e tijolo em tom marrom (EPIC RPG World Pack - Old Prison); goblins, orcs, gosmas e bichos
///   Mundo 2 -> Catacumbas pedra e tijolo azuis do Old Prison; feras, orcs e cavaleiros, ossos no chao
///   Mundo 3 -> Cripta     pedra e tijolo frios, verde e roxo (Old Prison); mortos-vivos, velas acesas
///   Mundo 4 (ultimo) -> Abismo chao vermelho do Old Prison, parede vermelha e runas; demonios
/// Cada tema e um mundo inteiro, com 3 fases (<see cref="DoMundo"/>); a primeira fase do
/// mundo so tem os inimigos do comeco da lista e as outras vao abrindo o resto.
/// O <see cref="Andar"/> escolhe o tema antes de montar as salas (<see cref="Usar"/>);
/// <see cref="ArteGerada.Chao"/> e <see cref="ArteGerada.Tijolo"/> leem o tema atual.
/// Chefes, salas especiais e desbloqueios nao dependem do tema.
/// </summary>
public sealed class TemaDoAndar
{
    /// <summary>Aparece no aviso da fase ("Mundo 2 - Fase 1: Catacumbas").</summary>
    public string Nome { get; private set; }

    /// <summary>Imagem do chao em Resources/Masmorra/Temas, ou null pro chao padrao da masmorra.</summary>
    public string Chao { get; private set; }

    /// <summary>Imagem da parede em Resources/Masmorra/Temas, ou null pra parede padrao.</summary>
    public string Parede { get; private set; }

    /// <summary>
    /// Nome da sala pronta do Old Prison (Masmorra/Temas/PrisaoSala{Sala}{0-3}): chao, paredes em
    /// perspectiva, sombra e enfeites ja montados numa imagem so. Null usa chao e parede ladrilhados.
    /// </summary>
    public string Sala { get; private set; }

    /// <summary>Tom do chao e da parede (com arte do pacote, so tinge; ver <see cref="Sala.Pintar"/>).</summary>
    public Color CorDoChao { get; private set; }
    public Color CorDaParede { get; private set; }

    /// <summary>
    /// Cor media do piso da sala pronta (as 4 versoes). O veu que baixa o contraste do chao
    /// (<see cref="global::Sala"/>) puxa cada pixel pra ela, sem clarear nem escurecer a sala.
    /// </summary>
    public Color CorMediaDoPiso { get; private set; } = new Color(0.22f, 0.21f, 0.23f);

    /// <summary>Tom das pedras de obstaculo (a pedra cinza do Tiny Swords tingida pro tema).</summary>
    public Color CorDaPedra { get; private set; } = Color.white;

    /// <summary>Tom dos espinhos do chao (avermelhados; claros no Abismo, que ja e vermelho).</summary>
    public Color CorDosEspinhos { get; private set; } = new Color(1f, 0.6f, 0.55f);

    /// <summary>Quanto o tom pesa sobre a arte do pacote (0 = arte pura, 1 = so a cor).</summary>
    public float ForcaDaCor { get; private set; } = 0.4f;

    /// <summary>Chance de cada enfeite de chao ser caveira ou ossos (o resto e do Tiny Swords).</summary>
    public float ChanceDeOsso { get; private set; } = 0.17f;

    /// <summary>Chance de um enfeite ser uma runa vermelha no chao (so no Abismo).</summary>
    public float ChanceDeRuna { get; private set; }

    /// <summary>A musica que toca no mundo (<see cref="global::Musica"/>), fora da sala do chefe e da loja.</summary>
    public TemaMusical Musica { get; private set; } = TemaMusical.Porao;

    /// <summary>Chance da sala ganhar candelabro num canto.</summary>
    public float ChanceDeCandelabro { get; private set; } = 0.5f;

    private (TipoDeInimigo tipo, float peso)[] inimigos;

    /// <summary>Tema do andar em jogo, ou null fora do jogo (sala de treino, cena de teste).</summary>
    public static TemaDoAndar Atual { get; private set; }

    public static void Usar(TemaDoAndar tema) => Atual = tema;

    /// <summary>
    /// O tema do andar <paramref name="andar"/>. O ultimo andar e sempre o Abismo; antes dele
    /// os tres primeiros temas se repetem, se o jogo tiver mais andares.
    /// </summary>
    public static TemaDoAndar DoNumero(int andar, bool ultimo)
    {
        if (ultimo)
            return Abismo;

        return Comuns[(Mathf.Max(1, andar) - 1) % Comuns.Length];
    }

    /// <summary>
    /// O tema do mundo <paramref name="mundo"/> (cada mundo tem 3 fases com a mesma cara).
    /// O ultimo mundo e sempre o Abismo; antes dele Porao, Catacumbas e Cripta, nessa ordem.
    /// </summary>
    public static TemaDoAndar DoMundo(int mundo, int totalDeMundos)
    {
        if (mundo >= totalDeMundos)
            return Abismo;

        return Comuns[(Mathf.Max(1, mundo) - 1) % Comuns.Length];
    }

    /// <summary>Um inimigo comum do tema, sorteado pelo peso.</summary>
    public TipoDeInimigo SortearInimigo() => SortearInimigo(1f);

    /// <summary>
    /// Um dos tres bichos mais simples do mundo: o que os chefes chamam pra ajudar. Assim nem
    /// o lacaio de chefe traz bicho de outro mundo.
    /// </summary>
    public static TipoDeInimigo Lacaio() => Atual != null ? Atual.SortearInimigo(0f) : TipoDeInimigo.GoblinTocha;

    /// <summary>
    /// Um inimigo comum do tema, sorteado pelo peso, so entre a primeira parte da lista
    /// (<paramref name="variedade"/> de 0 a 1). As listas comecam pelos bichos mais simples,
    /// entao a primeira fase do mundo tem os basicos e as outras vao abrindo o resto.
    /// </summary>
    public TipoDeInimigo SortearInimigo(float variedade)
    {
        int quantos = Mathf.Clamp(Mathf.CeilToInt(inimigos.Length * Mathf.Clamp01(variedade)), Mathf.Min(3, inimigos.Length), inimigos.Length);
        float total = 0f;

        for (int i = 0; i < quantos; i++)
            total += inimigos[i].peso;

        float sorteio = Random.value * total;

        for (int i = 0; i < quantos; i++)
        {
            sorteio -= inimigos[i].peso;

            if (sorteio <= 0f)
                return inimigos[i].tipo;
        }

        return inimigos[quantos - 1].tipo;
    }

    // ================================================================ os temas
    public static readonly TemaDoAndar Porao = new TemaDoAndar
    {
        Nome = "Porão",
        Musica = TemaMusical.Porao,
        Sala = "Porao",
        CorMediaDoPiso = new Color(0.24f, 0.216f, 0.222f),
        CorDaPedra = new Color(0.85f, 0.65f, 0.5f),
        Chao = "PrisaoChaoPorao",
        Parede = "PrisaoParedePorao",
        CorDoChao = Color.white,
        CorDaParede = Color.white,
        ForcaDaCor = 0f,
        ChanceDeOsso = 0.1f,
        ChanceDeCandelabro = 0.3f,
        inimigos = new[]
        {
            // Mundo 1: goblins, gosmas e morcegos. Os primeiros tres sao os mais simples (e os
            // que os chefes chamam).
            (TipoDeInimigo.GoblinTocha, 3f), (TipoDeInimigo.Saltador, 2.5f),
            (TipoDeInimigo.Morceguinho, 2f), (TipoDeInimigo.Geleia, 2f),
            (TipoDeInimigo.Divisor, 1.5f), (TipoDeInimigo.Barril, 1.5f),
            (TipoDeInimigo.Morcego, 1.5f), (TipoDeInimigo.Arqueiro, 1.5f),
            (TipoDeInimigo.GoblinDinamite, 1.5f),
        },
    };

    public static readonly TemaDoAndar Catacumbas = new TemaDoAndar
    {
        Nome = "Catacumbas",
        Musica = TemaMusical.Catacumbas,
        Sala = "Catacumbas",
        CorMediaDoPiso = new Color(0.194f, 0.222f, 0.282f),
        CorDaPedra = new Color(0.8f, 0.88f, 1f),
        Chao = "PrisaoChaoCatacumbas",
        Parede = "PrisaoParedeCatacumbas",
        CorDoChao = Color.white,
        CorDaParede = Color.white,
        ForcaDaCor = 0f,
        ChanceDeOsso = 0.35f,
        inimigos = new[]
        {
            // Mundo 2: orcs, feras e cavaleiros.
            (TipoDeInimigo.Orc, 3f), (TipoDeInimigo.Lobisomem, 2f),
            (TipoDeInimigo.CavaleiroLanca, 2f), (TipoDeInimigo.OrcBlindado, 1.5f),
            (TipoDeInimigo.CavaleiroEscudo, 1.5f), (TipoDeInimigo.Investidor, 1.5f),
            (TipoDeInimigo.OrcMontado, 1.5f), (TipoDeInimigo.Urso, 1f),
            (TipoDeInimigo.Sentinela, 1f), (TipoDeInimigo.OrcElite, 1f),
        },
    };

    public static readonly TemaDoAndar Cripta = new TemaDoAndar
    {
        Nome = "Cripta",
        Musica = TemaMusical.Cripta,
        Sala = "Cripta",
        CorMediaDoPiso = new Color(0.186f, 0.239f, 0.255f),
        CorDaPedra = new Color(0.75f, 0.95f, 0.9f),
        Chao = "PrisaoChaoCripta",
        Parede = "PrisaoParedeCripta",
        CorDoChao = Color.white,
        CorDaParede = Color.white,
        ForcaDaCor = 0f,
        ChanceDeOsso = 0.5f,
        ChanceDeCandelabro = 1f,
        inimigos = new[]
        {
            // Mundo 3: mortos-vivos e magia.
            (TipoDeInimigo.Esqueleto, 3f), (TipoDeInimigo.EsqueletoGuerreiro, 2f),
            (TipoDeInimigo.EsqueletoFoice, 2f), (TipoDeInimigo.EsqueletoArqueiro, 2f),
            (TipoDeInimigo.Vampiro, 1.5f), (TipoDeInimigo.FogoFatuo, 1.5f),
            (TipoDeInimigo.Atirador, 1.5f), (TipoDeInimigo.EsqueletoBlindado, 1.5f),
            (TipoDeInimigo.EsqueletoEspadao, 1.5f), (TipoDeInimigo.Necromante, 1f),
        },
    };

    public static readonly TemaDoAndar Abismo = new TemaDoAndar
    {
        Nome = "Abismo",
        Musica = TemaMusical.Abismo,
        Sala = "Abismo",
        CorMediaDoPiso = new Color(0.262f, 0.182f, 0.206f),
        CorDaPedra = new Color(0.85f, 0.9f, 1f),
        CorDosEspinhos = new Color(1f, 1f, 1f),
        Chao = "PrisaoChaoAbismo",
        Parede = "PrisaoParedeAbismo",
        CorDoChao = Color.white,
        CorDaParede = Color.white,
        ForcaDaCor = 0f,
        ChanceDeOsso = 0.3f,
        ChanceDeRuna = 0.35f,
        ChanceDeCandelabro = 0.5f,
        inimigos = new[]
        {
            // Mundo 4: demonios e o que vive no fundo.
            (TipoDeInimigo.Demonio, 3f), (TipoDeInimigo.Perseguidor, 2.5f),
            (TipoDeInimigo.DemonioLaminas, 2f), (TipoDeInimigo.DemonioTridente, 2f),
            (TipoDeInimigo.DemonioArqueiro, 2f), (TipoDeInimigo.Demonia, 2f),
            (TipoDeInimigo.DemoniaFoice, 1.5f), (TipoDeInimigo.MonstroDeSangue, 1.5f),
        },
    };

    private static readonly TemaDoAndar[] Comuns = { Porao, Catacumbas, Cripta };
}
