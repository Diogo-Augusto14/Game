using UnityEngine;

/// <summary>
/// A cara de cada mundo: nome, chao, paredes, enfeites e quem mora nele.
///   Mundo 1 -> Porao      tijolos marrons do 2D Pixel Dungeon v2.0; goblins, orcs, gosmas e bichos
///   Mundo 2 -> Catacumbas pedra cinza do 2D Dungeon v5.2; feras, orcs e cavaleiros, ossos no chao
///   Mundo 3 -> Cripta     laje rachada e parede de friso azul (v5.2); mortos-vivos, velas acesas
///   Mundo 4 (ultimo) -> Abismo chao de pedra tingido de vermelho, parede de friso vermelho e runas; demonios
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

    /// <summary>Tom do chao e da parede (com arte do pacote, so tinge; ver <see cref="Sala.Pintar"/>).</summary>
    public Color CorDoChao { get; private set; }
    public Color CorDaParede { get; private set; }

    /// <summary>Quanto o tom pesa sobre a arte do pacote (0 = arte pura, 1 = so a cor).</summary>
    public float ForcaDaCor { get; private set; } = 0.4f;

    /// <summary>Chance de cada enfeite de chao ser caveira ou ossos (o resto e do Tiny Swords).</summary>
    public float ChanceDeOsso { get; private set; } = 0.17f;

    /// <summary>Chance de um enfeite ser uma runa vermelha no chao (so no Abismo).</summary>
    public float ChanceDeRuna { get; private set; }

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
        Chao = "ChaoPorao",
        Parede = "ParedePorao",
        CorDoChao = new Color(0.26f, 0.19f, 0.14f),
        CorDaParede = new Color(0.36f, 0.28f, 0.22f),
        ChanceDeOsso = 0.1f,
        ChanceDeCandelabro = 0.3f,
        inimigos = new[]
        {
            (TipoDeInimigo.GoblinTocha, 3f), (TipoDeInimigo.Barril, 1.5f),
            (TipoDeInimigo.Geleia, 2f), (TipoDeInimigo.Divisor, 1.5f),
            (TipoDeInimigo.Morceguinho, 2f), (TipoDeInimigo.Morcego, 1.5f),
            (TipoDeInimigo.Perseguidor, 2.5f), (TipoDeInimigo.Orc, 2f),
            (TipoDeInimigo.Atirador, 1.5f), (TipoDeInimigo.Investidor, 1f),
            (TipoDeInimigo.MonstroDeSangue, 1f),
        },
    };

    public static readonly TemaDoAndar Catacumbas = new TemaDoAndar
    {
        Nome = "Catacumbas",
        CorDoChao = new Color(0.14f, 0.17f, 0.22f),
        CorDaParede = new Color(0.28f, 0.32f, 0.4f),
        ChanceDeOsso = 0.35f,
        inimigos = new[]
        {
            (TipoDeInimigo.Saltador, 2.5f), (TipoDeInimigo.Orc, 2f),
            (TipoDeInimigo.OrcBlindado, 1.5f), (TipoDeInimigo.OrcMontado, 1f),
            (TipoDeInimigo.GoblinDinamite, 2f), (TipoDeInimigo.GoblinTocha, 1.5f),
            (TipoDeInimigo.Barril, 1f), (TipoDeInimigo.Arqueiro, 1.5f),
            (TipoDeInimigo.Lobisomem, 1.5f), (TipoDeInimigo.Investidor, 1.5f),
            (TipoDeInimigo.Esqueleto, 1.5f), (TipoDeInimigo.EsqueletoGuerreiro, 1.5f),
            (TipoDeInimigo.CavaleiroEscudo, 1f), (TipoDeInimigo.Sentinela, 1f),
            (TipoDeInimigo.Morcego, 1f), (TipoDeInimigo.Geleia, 1f),
        },
    };

    public static readonly TemaDoAndar Cripta = new TemaDoAndar
    {
        Nome = "Cripta",
        Chao = "ChaoCripta",
        Parede = "ParedeCripta",
        CorDoChao = new Color(0.17f, 0.21f, 0.2f),
        CorDaParede = new Color(0.3f, 0.36f, 0.36f),
        ChanceDeOsso = 0.5f,
        ChanceDeCandelabro = 1f,
        inimigos = new[]
        {
            (TipoDeInimigo.Esqueleto, 2f), (TipoDeInimigo.EsqueletoFoice, 2f),
            (TipoDeInimigo.Vampiro, 2f), (TipoDeInimigo.EsqueletoGuerreiro, 1.5f),
            (TipoDeInimigo.EsqueletoBlindado, 1.5f), (TipoDeInimigo.EsqueletoEspadao, 1.5f),
            (TipoDeInimigo.EsqueletoArqueiro, 2f), (TipoDeInimigo.Necromante, 1.5f),
            (TipoDeInimigo.FogoFatuo, 1.5f), (TipoDeInimigo.Morceguinho, 1f),
            (TipoDeInimigo.CavaleiroEscudo, 1f), (TipoDeInimigo.CavaleiroLanca, 1.5f),
            (TipoDeInimigo.Sentinela, 1.5f), (TipoDeInimigo.Atirador, 1f),
            (TipoDeInimigo.Urso, 1f),
        },
    };

    public static readonly TemaDoAndar Abismo = new TemaDoAndar
    {
        Nome = "Abismo",
        Chao = "ChaoAbismo",
        Parede = "ParedeAbismo",
        CorDoChao = new Color(0.45f, 0.08f, 0.08f),
        CorDaParede = new Color(0.4f, 0.14f, 0.14f),
        ForcaDaCor = 0.55f,
        ChanceDeOsso = 0.3f,
        ChanceDeRuna = 0.35f,
        ChanceDeCandelabro = 0.5f,
        inimigos = new[]
        {
            (TipoDeInimigo.Demonio, 2.5f), (TipoDeInimigo.DemonioTridente, 2f),
            (TipoDeInimigo.DemonioLaminas, 2f), (TipoDeInimigo.DemoniaFoice, 2f),
            (TipoDeInimigo.DemonioArqueiro, 2f), (TipoDeInimigo.Demonia, 2f),
            (TipoDeInimigo.MonstroDeSangue, 2f), (TipoDeInimigo.Perseguidor, 2f),
            (TipoDeInimigo.Divisor, 1.5f), (TipoDeInimigo.FogoFatuo, 1.5f),
            (TipoDeInimigo.Sentinela, 1.5f), (TipoDeInimigo.Investidor, 2f),
            (TipoDeInimigo.OrcElite, 1.5f), (TipoDeInimigo.Atirador, 1.5f),
            (TipoDeInimigo.Vampiro, 1f),
        },
    };

    private static readonly TemaDoAndar[] Comuns = { Porao, Catacumbas, Cripta };
}
