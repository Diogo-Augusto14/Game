using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// As imagens que vieram de pacote (nao desenhadas por codigo), lidas de
/// <c>Assets/Arte/Resources</c>:
///
///   Personagens/...  (Tiny RPG Character Asset Pack 02, os 20 bichos, e do Pack 01 v2.0 os
///       orcs, esqueletos, lobisomem, urso, geleia, morceguinho e necromante)
///       uma tira por animacao, quadros de 100x100 com o bicho (uns 20 px) no meio;
///   Personagens/Projeteis  flecha, bala de canhao, magia e raios desses bichos
///   InterfacePixel/00.png ... 07.png  (Pixel UI pack 3)
///       coracoes, paineis e barras recortados pelos retangulos la embaixo
///   TinySwords/...  (Tiny Swords e Tiny Swords Free Pack, da Pixel Frog)
///       folhas em grade (uma linha por animacao): o arqueiro azul do jogador, goblins,
///       barril, arqueiro, dinamite,
///       explosao, caveira de morte e enfeites de chao
///   Masmorra/Esqueleto, EsqueletoFoice, Vampiro  (Enemy Animations Set)
///       uma tira por animacao, quadros de 32x32
///   Masmorra/Tocha, Candelabro, Objetos  (2D Dungeon Asset Pack v5.2 e 2D Pixel Dungeon v2.0)
///       tocha de parede, candelabro e caveira/ossos do chao, em ladrilhos de 16 px
///   Masmorra/Chao, Parede, Portao, Espinhos, Ladrilhos  (2D Dungeon Asset Pack v5.2)
///       chao, tijolos, portao de grade, espinhos, o tileset inteiro (buraco do alcapao)
///       e, na folha Objetos, moeda, frasco, chave, mesas e os icones dos itens
///   Masmorra/Bau, ChaveDourada  (2D Dungeon Asset Pack v5.2, items_animation)
///       bau de madeira abrindo (4 quadros) e a chave dourada girando (8 quadros)
///   Masmorra/BauDeFerro  (2D Pixel Dungeon Asset Pack v2.0, chest e chest_open juntos)
///       bau trancado: 4 quadros parado e 4 abrindo, com o brilho do tesouro
///
/// Tudo e recortado aqui com Sprite.Create, sem fatiar no Sprite Editor: quem clonar o
/// projeto nao precisa preparar nada. Se uma imagem sumir, quem pediu recebe null e volta
/// pro desenho antigo — o jogo continua rodando.
/// </summary>
public static class ArteImportada
{
    /// <summary>Lado de um quadro nas tiras dos personagens.</summary>
    private const int QuadroDoPersonagem = 100;

    /// <summary>
    /// Centro do corpo de cada personagem dentro do quadro de 100x100 (imagem, y pra baixo).
    /// Medido no quadro parado; quem nao esta aqui usa <see cref="CentroPadrao"/>.
    /// </summary>
    private static readonly Dictionary<string, Vector2> CentrosDoCorpo = new Dictionary<string, Vector2>
    {
        { "Bolha", new Vector2(53f, 47f) },
        { "Bruxo", new Vector2(50f, 47f) },
        { "CaoInfernal", new Vector2(50f, 51f) },
        { "CavaleiroCanhao", new Vector2(55f, 45f) },
        { "CavaleiroEscudo", new Vector2(50f, 48f) },
        { "CavaleiroLanca", new Vector2(51f, 44f) },
        { "Demonia", new Vector2(49f, 47f) },
        { "DemoniaFoice", new Vector2(55f, 46f) },
        { "DemonioArqueiro", new Vector2(52f, 50f) },
        { "DemonioLaminas", new Vector2(55f, 48f) },
        { "DemonioMartelo", new Vector2(55f, 47f) },
        { "DemonioTridente", new Vector2(54f, 46f) },
        { "FogoFatuo", new Vector2(52f, 43f) },
        { "Golem", new Vector2(52f, 45f) },
        { "Gosma", new Vector2(48f, 51f) },
        { "Minotauro", new Vector2(56f, 49f) },
        { "Morcego", new Vector2(49f, 46f) },
        { "Olho", new Vector2(50f, 52f) },
        // Tiny RPG Character Asset Pack 01 v2.0
        { "Orc", new Vector2(54f, 50f) },
        { "OrcBlindado", new Vector2(54f, 48f) },
        { "OrcElite", new Vector2(56f, 46f) },
        { "OrcMontado", new Vector2(53f, 44f) },
        { "EsqueletoGuerreiro", new Vector2(56f, 50f) },
        { "EsqueletoBlindado", new Vector2(53f, 47f) },
        { "EsqueletoEspadao", new Vector2(47f, 48f) },
        { "EsqueletoArqueiro", new Vector2(52f, 48f) },
        { "Geleia", new Vector2(48f, 50f) },
        { "Morceguinho", new Vector2(49f, 48f) },
        { "Lobisomem", new Vector2(55f, 50f) },
        { "Urso", new Vector2(51f, 49f) },
        { "Necromante", new Vector2(46f, 45f) },
        // Herois jogaveis do Pack 01 (o corpo, sem contar a arma)
        { "Herois/Soldado", new Vector2(47f, 50f) },
        { "Herois/Cavaleiro", new Vector2(47f, 48f) },
        { "Herois/Templario", new Vector2(49f, 49f) },
        { "Herois/Lanceiro", new Vector2(50f, 48f) },
        { "Herois/Espadachim", new Vector2(47f, 50f) },
        { "Herois/Machadeiro", new Vector2(49f, 48f) },
        { "Herois/Arqueiro", new Vector2(50f, 49f) },
        { "Herois/Mago", new Vector2(49f, 49f) },
        { "Herois/Padre", new Vector2(51f, 48f) },
    };

    private static readonly Vector2 CentroPadrao = new Vector2(52f, 50f);

    /// <summary>Paineis e barras sao desenhados 4x maiores na tela (ref. 100 px por unidade do Canvas).</summary>
    public const float PixelsPorUnidadeDaInterface = 25f;

    private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, ClipesDePersonagem> personagens = new Dictionary<string, ClipesDePersonagem>();
    private static readonly Dictionary<string, Texture2D> texturas = new Dictionary<string, Texture2D>();

    // ================================================================ personagens
    /// <summary>
    /// As animacoes de um personagem do pacote Tiny RPG, ou null se a pasta nao existir.
    /// <paramref name="pixelsPorUnidade"/> decide o tamanho: o corpo tem uns 20 px.
    /// </summary>
    public static ClipesDePersonagem Personagem(string pasta, float pixelsPorUnidade)
    {
        string chave = pasta + "@" + pixelsPorUnidade;

        if (personagens.TryGetValue(chave, out ClipesDePersonagem guardado))
            return guardado;

        ClipesDePersonagem clipes = new ClipesDePersonagem
        {
            Parado = Tira(pasta, "Idle", pixelsPorUnidade),
            Andando = Tira(pasta, "Walk", pixelsPorUnidade),
            Ataque = Tira(pasta, "Attack01", pixelsPorUnidade),
            AtaqueEspecial = Tira(pasta, "Attack02", pixelsPorUnidade),
            AtaqueForte = Tira(pasta, "Attack03", pixelsPorUnidade),
            Dor = Tira(pasta, "Hurt", pixelsPorUnidade),
            Morte = Tira(pasta, "Death", pixelsPorUnidade),
        };

        if (clipes.Parado == null)
            clipes = null;

        personagens[chave] = clipes;
        return clipes;
    }

    private static Sprite[] Tira(string pasta, string animacao, float pixelsPorUnidade)
    {
        // Nem todo bicho tem segundo ou terceiro ataque (padre, esqueleto arqueiro): sem aviso quando falta.
        Texture2D textura = Textura($"Personagens/{pasta}/{animacao}", animacao != "Attack02" && animacao != "Attack03");

        if (textura == null)
            return null;

        int quantos = textura.width / QuadroDoPersonagem;
        Sprite[] quadros = new Sprite[quantos];

        // Pivo no centro do corpo, pra o colisor redondo do inimigo cair em cima dele. O
        // quadro vai inteiro: golpe de lanca e raio chegam quase na borda.
        Vector2 centro = CentrosDoCorpo.TryGetValue(pasta, out Vector2 medido) ? medido : CentroPadrao;
        Vector2 pivo = new Vector2(centro.x / QuadroDoPersonagem, 1f - centro.y / QuadroDoPersonagem);

        for (int i = 0; i < quantos; i++)
        {
            Rect recorte = Recorte(textura, i * QuadroDoPersonagem, 0, QuadroDoPersonagem, QuadroDoPersonagem);

            quadros[i] = Sprite.Create(textura, recorte, pivo, pixelsPorUnidade, 0, SpriteMeshType.FullRect);
            quadros[i].name = $"{pasta} {animacao} {i}";
        }

        return quadros;
    }

    // ================================================================ Tiny Swords
    // Folhas em grade: cada linha e uma animacao, cada celula um quadro quadrado. Os
    // retangulos abaixo (recorte e centro do corpo) estao em coordenadas de UMA celula,
    // y pra baixo, do jeito que aparecem num editor de imagem.

    /// <summary>
    /// Goblin da tocha: parado, correndo e o golpe de tocha em tres direcoes
    /// (lado, baixo, cima). O golpe acerta no quarto quadro (indice 3).
    /// </summary>
    public static ClipesDePersonagem GoblinDaTocha(float pixelsPorUnidade)
    {
        const string folha = "TinySwords/GoblinTocha";
        RectInt recorte = new RectInt(8, 16, 176, 160);
        Vector2 centro = new Vector2(92f, 100f);

        return Clipes(folha, pixelsPorUnidade, c =>
        {
            c.Parado = Linha(folha, 192, 0, 7, recorte, centro, pixelsPorUnidade);
            c.Andando = Linha(folha, 192, 1, 6, recorte, centro, pixelsPorUnidade);
            c.Ataque = Linha(folha, 192, 2, 6, recorte, centro, pixelsPorUnidade);
            c.AtaqueBaixo = Linha(folha, 192, 3, 6, recorte, centro, pixelsPorUnidade);
            c.AtaqueCima = Linha(folha, 192, 4, 6, recorte, centro, pixelsPorUnidade);
        });
    }

    /// <summary>Goblin da dinamite: parado, correndo e o arremesso (a dinamite sai no indice 5).</summary>
    public static ClipesDePersonagem GoblinDaDinamite(float pixelsPorUnidade)
    {
        const string folha = "TinySwords/GoblinDinamite";
        RectInt recorte = new RectInt(40, 40, 120, 112);
        Vector2 centro = new Vector2(94f, 104f);

        return Clipes(folha, pixelsPorUnidade, c =>
        {
            c.Parado = Linha(folha, 192, 0, 6, recorte, centro, pixelsPorUnidade);
            c.Andando = Linha(folha, 192, 1, 6, recorte, centro, pixelsPorUnidade);
            c.Ataque = Linha(folha, 192, 2, 7, recorte, centro, pixelsPorUnidade);
        });
    }

    /// <summary>
    /// Barril de TNT com um goblin dentro:
    ///   Parado = fechado, AtaqueEspecial = saindo do barril, Andando = correndo com o
    ///   barril, Ataque = pavio aceso (em loop enquanto queima).
    /// </summary>
    public static ClipesDePersonagem Barril(float pixelsPorUnidade)
    {
        const string folha = "TinySwords/Barril";
        RectInt recorte = new RectInt(24, 0, 80, 112);
        Vector2 centro = new Vector2(64f, 72f);

        return Clipes(folha, pixelsPorUnidade, c =>
        {
            c.Parado = Linha(folha, 128, 0, 1, recorte, centro, pixelsPorUnidade);
            c.AtaqueEspecial = Linha(folha, 128, 1, 6, recorte, centro, pixelsPorUnidade);
            c.Andando = Linha(folha, 128, 4, 3, recorte, centro, pixelsPorUnidade);
            c.Ataque = Linha(folha, 128, 5, 3, recorte, centro, pixelsPorUnidade);
        });
    }

    /// <summary>Arqueiro sombrio (Free Pack): parado, correndo e o tiro (a flecha sai no indice 5).</summary>
    public static ClipesDePersonagem Arqueiro(float pixelsPorUnidade)
    {
        RectInt recorte = new RectInt(40, 40, 112, 104);
        Vector2 centro = new Vector2(94f, 100f);

        return Clipes("TinySwords/Arqueiro", pixelsPorUnidade, c =>
        {
            c.Parado = Linha("TinySwords/ArqueiroParado", 192, 0, 6, recorte, centro, pixelsPorUnidade);
            c.Andando = Linha("TinySwords/ArqueiroAndando", 192, 0, 4, recorte, centro, pixelsPorUnidade);
            c.Ataque = Linha("TinySwords/ArqueiroAtirando", 192, 0, 8, recorte, centro, pixelsPorUnidade);
        });
    }

    /// <summary>A caveira que sobe quando um goblin ou o arqueiro morre (14 quadros).</summary>
    public static Sprite[] Caveira(float pixelsPorUnidade)
    {
        RectInt recorte = new RectInt(24, 24, 80, 88);
        Vector2 centro = new Vector2(64f, 72f);
        Sprite[] subindo = Linha("TinySwords/Caveira", 128, 0, 7, recorte, centro, pixelsPorUnidade);
        Sprite[] sumindo = Linha("TinySwords/Caveira", 128, 1, 7, recorte, centro, pixelsPorUnidade);

        if (subindo == null || sumindo == null)
            return null;

        Sprite[] todos = new Sprite[subindo.Length + sumindo.Length];
        subindo.CopyTo(todos, 0);
        sumindo.CopyTo(todos, subindo.Length);
        return todos;
    }

    /// <summary>A banana de dinamite girando (6 quadros).</summary>
    public static Sprite[] Dinamite(float pixelsPorUnidade)
        => Linha("TinySwords/Dinamite", 64, 0, 6, new RectInt(0, 0, 64, 64), new Vector2(34f, 28f), pixelsPorUnidade);

    /// <summary>
    /// A explosao (9 quadros). A bola de fogo tem uns 105 px de largura: pra ela cobrir um
    /// circulo de raio r, use <c>ExplosaoPixelsPorUnidade(r)</c>.
    /// </summary>
    public static Sprite[] Explosao(float pixelsPorUnidade)
        => Linha("TinySwords/Explosao", 192, 0, 9, new RectInt(0, 0, 192, 192), new Vector2(96f, 94f), pixelsPorUnidade);

    public static float ExplosaoPixelsPorUnidade(float raio) => 105f / Mathf.Max(0.1f, raio * 2f);

    /// <summary>A flecha do arqueiro, apontando pra direita (48 px de comprimento).</summary>
    public static Sprite Flecha(float pixelsPorUnidade)
    {
        Sprite[] um = Linha("TinySwords/Flecha", 64, 0, 1, new RectInt(8, 24, 48, 16), new Vector2(32f, 32f), pixelsPorUnidade);
        return um != null ? um[0] : null;
    }

    /// <summary>
    /// O jogador: o arqueiro azul do Tiny Swords (Update 010). Parado, correndo e o tiro pra
    /// cima, pro lado e pra baixo (as diagonais da folha ficam de fora: o tiro e em 4 direcoes).
    /// A morte e a caveira do pacote.
    /// </summary>
    public static ClipesDePersonagem ArqueiroAzul(float pixelsPorUnidade)
    {
        RectInt recorte = new RectInt(0, 0, 192, 192);
        Vector2 centro = new Vector2(98f, 100f);
        const string folha = "TinySwords/ArqueiroAzul";

        return Clipes(folha, pixelsPorUnidade, c =>
        {
            c.Parado = Linha(folha, 192, 0, 6, recorte, centro, pixelsPorUnidade);
            c.Andando = Linha(folha, 192, 1, 6, recorte, centro, pixelsPorUnidade);
            c.AtaqueCima = Linha(folha, 192, 2, 8, recorte, centro, pixelsPorUnidade);
            c.Ataque = Linha(folha, 192, 4, 8, recorte, centro, pixelsPorUnidade);
            c.AtaqueBaixo = Linha(folha, 192, 6, 8, recorte, centro, pixelsPorUnidade);
            // So a caveira subindo (a primeira linha): ela fica na tela ate o fim de jogo.
            Sprite[] caveira = Caveira(pixelsPorUnidade * 128f / 192f);

            if (caveira != null)
            {
                c.Morte = new Sprite[7];
                System.Array.Copy(caveira, c.Morte, 7);
            }
        });
    }

    /// <summary>Nomes dos enfeites de chao (cogumelos, pedrinhas, moitas, ossos).</summary>
    private static readonly string[] Enfeites = { "01", "02", "03", "04", "05", "06", "07", "10", "14", "15" };

    /// <summary>
    /// Um enfeite de chao sorteado, ou null sem a arte: os do Tiny Swords (64 px = 1
    /// ladrilho) e, de vez em quando, uma caveira ou ossos da masmorra.
    /// </summary>
    public static Sprite EnfeiteAleatorio() => EnfeiteAleatorio(2f / (Enfeites.Length + 2));

    /// <summary>
    /// Como <see cref="EnfeiteAleatorio()"/>, com a chance de sair caveira ou ossos
    /// escolhida pelo tema do andar.
    /// </summary>
    public static Sprite EnfeiteAleatorio(float chanceDeOsso)
    {
        if (Random.value < chanceDeOsso)
        {
            // Objetos.png: caveira na coluna 2 da linha 3, ossos na coluna 2 da linha 4.
            Sprite[] osso = Linha("Masmorra/Objetos", 16, Random.value < 0.5f ? 3 : 4, 1,
                                  new RectInt(32, 0, 16, 16), new Vector2(40f, 8f), 20f);

            if (osso != null)
                return osso[0];
        }

        string nome = Enfeites[Random.Range(0, Enfeites.Length)];
        Sprite[] um = Linha("TinySwords/Enfeites/" + nome, 64, 0, 1, new RectInt(0, 0, 64, 64), new Vector2(32f, 32f), 64f);
        return um != null ? um[0] : null;
    }

    // ================================================================ masmorra
    /// <summary>
    /// Um bicho do Enemy Animations Set (Esqueleto, EsqueletoFoice, Vampiro): tiras de
    /// quadros 32x32, o corpo com uns 16 px. <paramref name="centro"/> e o meio do corpo no quadro.
    /// </summary>
    public static ClipesDePersonagem Masmorra(string pasta, Vector2 centro, float pixelsPorUnidade)
    {
        string caminho = "Masmorra/" + pasta + "/";
        RectInt recorte = new RectInt(0, 0, 32, 32);

        return Clipes(caminho, pixelsPorUnidade, c =>
        {
            c.Parado = Linha(caminho + "Parado", 32, 0, 99, recorte, centro, pixelsPorUnidade);
            c.Andando = Linha(caminho + "Andando", 32, 0, 99, recorte, centro, pixelsPorUnidade);
            c.Ataque = Linha(caminho + "Ataque", 32, 0, 99, recorte, centro, pixelsPorUnidade);
            c.Dor = Linha(caminho + "Dor", 32, 0, 99, recorte, centro, pixelsPorUnidade);
            c.Morte = Linha(caminho + "Morte", 32, 0, 99, recorte, centro, pixelsPorUnidade);
        });
    }

    /// <summary>Tocha de parede acesa (6 quadros de 16x28), com o pivo no suporte.</summary>
    public static Sprite[] TochaDeParede(float pixelsPorUnidade)
        => Linha("Masmorra/Tocha", 16, 0, 6, new RectInt(0, 0, 16, 28), new Vector2(8f, 20f), pixelsPorUnidade, 28);

    /// <summary>Candelabro de chao com a chama tremendo (4 quadros de 16x16), pivo no pe.</summary>
    public static Sprite[] Candelabro(float pixelsPorUnidade)
        => Linha("Masmorra/Candelabro", 16, 0, 4, new RectInt(0, 0, 16, 16), new Vector2(8f, 15f), pixelsPorUnidade);

    // ================================================================ cenario da masmorra
    // 2D Dungeon Asset Pack v5.2: ladrilhos de 16 px, 16 pixels por unidade (1 ladrilho = 1
    // unidade, o mesmo tamanho da arte gerada que eles substituem).

    private const float PixelsDoLadrilho = 16f;

    /// <summary>Chao (4x4 ladrilhos misturados, pra repeticao nao aparecer). Para modo Tiled.</summary>
    public static Sprite ChaoDaMasmorra => Inteira("Masmorra/Chao", PixelsDoLadrilho);

    /// <summary>Tijolos da parede (4 ladrilhos lado a lado). Para modo Tiled.</summary>
    public static Sprite ParedeDaMasmorra => Inteira("Masmorra/Parede", PixelsDoLadrilho);

    /// <summary>
    /// Chao do tema do andar (Masmorra/Temas, montado com ladrilhos dos dois pacotes de
    /// masmorra), ou null quando o tema usa o chao padrao ou nao ha tema.
    /// </summary>
    public static Sprite ChaoDoTema => DoTema(TemaDoAndar.Atual?.Chao);

    /// <summary>Parede do tema do andar, ou null pra parede padrao.</summary>
    public static Sprite ParedeDoTema => DoTema(TemaDoAndar.Atual?.Parede);

    /// <summary>Runa vermelha pintada no chao (2D Dungeon v5.2), enfeite do Abismo.</summary>
    public static Sprite Runa => Inteira("Masmorra/Temas/Runa", PixelsDoLadrilho);

    private static Sprite DoTema(string nome)
        => string.IsNullOrEmpty(nome) ? null : Inteira("Masmorra/Temas/" + nome, PixelsDoLadrilho);

    /// <summary>Portao de grade: quadro 0 fechado, 4 aberto (a parte de cima da folha, 16x16).</summary>
    public static Sprite Portao(bool aberto)
    {
        Sprite[] quadros = QuadrosDoPortao;
        return quadros != null ? quadros[aberto ? quadros.Length - 1 : 0] : null;
    }

    /// <summary>Os 5 quadros do portao subindo, do fechado (0) ao aberto (4): a animacao da porta.</summary>
    public static Sprite[] QuadrosDoPortao
        => Linha("Masmorra/Portao", 16, 0, 5, new RectInt(0, 0, 16, 16), new Vector2(8f, 8f), PixelsDoLadrilho, 32);

    /// <summary>Bau de madeira abrindo (4 quadros de 16x16): fechado, tampa tremendo, aberto, aberto com o ouro.</summary>
    public static Sprite[] BauAbrindo
        => Linha("Masmorra/Bau", 16, 0, 4, new RectInt(0, 0, 16, 16), new Vector2(8f, 8f), PixelsDoLadrilho);

    /// <summary>Bau de ferro trancado parado (4 quadros, respirando), com o pivo no meio.</summary>
    public static Sprite[] BauDeFerroParado
        => Linha("Masmorra/BauDeFerro", 16, 0, 4, new RectInt(0, 0, 16, 16), new Vector2(8f, 8f), PixelsDoLadrilho);

    /// <summary>Bau de ferro abrindo (4 quadros, o ultimo com o brilho do tesouro).</summary>
    public static Sprite[] BauDeFerroAbrindo
    {
        get
        {
            Sprite[] todos = Linha("Masmorra/BauDeFerro", 16, 0, 8, new RectInt(0, 0, 16, 16), new Vector2(8f, 8f), PixelsDoLadrilho);
            return todos != null && todos.Length >= 8 ? new[] { todos[4], todos[5], todos[6], todos[7] } : null;
        }
    }

    /// <summary>Chave dourada girando (8 quadros de 16x16): a chave que o chefe deixa.</summary>
    public static Sprite[] ChaveDourada
        => Linha("Masmorra/ChaveDourada", 16, 0, 8, new RectInt(0, 0, 16, 16), new Vector2(8f, 8f), PixelsDoLadrilho);

    /// <summary>Espinhos saindo do chao (5 quadros de 16x16, o ultimo todo pra fora).</summary>
    public static Sprite[] EspinhosDoChao
        => Linha("Masmorra/Espinhos", 16, 0, 5, new RectInt(0, 0, 16, 16), new Vector2(8f, 8f), PixelsDoLadrilho);

    /// <summary>Um ladrilho do tileset inteiro (coluna, linha), ex. o buraco do alcapao em (3, 9).</summary>
    public static Sprite Ladrilho(int coluna, int linha)
        => Celula("Masmorra/Ladrilhos", coluna, linha);

    /// <summary>Um objeto da folha de itens (coluna, linha): moeda, frasco, chave, bau, mesa...</summary>
    public static Sprite Objeto(int coluna, int linha)
        => Celula("Masmorra/Objetos", coluna, linha);

    /// <summary>
    /// Trofeu de escudo (16x32, duas linhas da folha de objetos): azul ou vermelho, pivo no
    /// pe. Marca a porta da sala de desafio.
    /// </summary>
    public static Sprite Trofeu(bool vermelho)
        => Unico("Masmorra/Objetos", 16, new RectInt(vermelho ? 16 : 0, 0, 16, 32), new Vector2(vermelho ? 24f : 8f, 32f),
                 PixelsDoLadrilho, 32);

    /// <summary>Bau fechado grande da folha de objetos (o que fica na sala amaldicoada).</summary>
    public static Sprite Bau => Objeto(5, 4);

    /// <summary>O idolo de olho vermelho da folha de objetos: enfeite e marca da sala amaldicoada.</summary>
    public static Sprite IdoloMaldito => Objeto(2, 1);

    /// <summary>
    /// O desenho de cada item passivo, pelo nome: frascos, gemas, livro, pergaminho, taca.
    /// Nome desconhecido sorteia um fixo (o mesmo nome sempre da o mesmo desenho).
    /// </summary>
    public static Sprite IconeDoItem(string nome)
    {
        // Os nomes ganharam acento ("Café"); os casos abaixo continuam sem.
        switch (FonteDoJogo.SemAcentos(nome))
        {
            case "Cebola Triste": return Objeto(9, 2);
            case "Seringa Vermelha": return Objeto(9, 3);
            case "Tenis Velho": return Objeto(6, 1);
            case "Olho Triplo": return Objeto(5, 1);
            case "Olho Gemeo": return Objeto(3, 1);
            case "Luneta": return Objeto(11, 3);
            case "Coracao Extra": return Objeto(10, 2);
            case "Lagrima de Chumbo": return Objeto(4, 1);
            case "Cafe": return Objeto(10, 4);
            case "Saco de Moedas": return Objeto(4, 3);
            case "Lagrima Fantasma": return Objeto(11, 4);
            case "Bussola Maldita": return Objeto(11, 2);
            case "Olho na Nuca": return Objeto(10, 3);
            case "Pimenta": return Objeto(10, 1);
            case "Pena da Fenix": return IconeDaInterface(80, 260, 80, 40, 64f);
            case "Escudo Sagrado": return ObjetoV2(4, 0);
            case "Sangue de Vampiro": return ObjetoV2(9, 1);
            case "Prego Enferrujado": return Objeto(0, 3);
            case "Pedra-Ima": return Objeto(8, 1);
            case "Amuleto da Sorte": return IconeDaInterface(134, 216, 36, 32, 38f);
            case "Bolsa do Mercador": return SacoDeOuro;
            case "Brasa da Furia": return IconeDaInterface(86, 216, 36, 32, 38f);
            case "Elixir de Nevoa": return ObjetoV2(7, 2);
            case "Orbe Guardiao": return IconeDaInterface(182, 216, 36, 32, 38f);
            case "Barril de Polvora": return Objeto(4, 4);
            case "Carne Assada": return CarneAssada;
            case "Pacto de Sangue": return ObjetoV2(7, 0);
        }

        (int, int)[] reserva = { (7, 1), (8, 1), (8, 2), (1, 3), (1, 4) };
        int h = 0;

        foreach (char c in nome ?? "")
            h = h * 31 + c;

        (int coluna, int linha) = reserva[Mathf.Abs(h) % reserva.Length];
        return Objeto(coluna, linha);
    }

    /// <summary>
    /// Uma celula de 16x16 da folha <c>Masmorra/ObjetosV2</c> (as tres ultimas linhas do
    /// tileset do 2D Pixel Dungeon Asset Pack v2.0): linha 0 tem o escudo (4) e a caveira (7);
    /// linha 1 moeda (6), frasco azul (7), chave (8) e frasco vermelho (9); linha 2 frasco
    /// azul grande (7), frasquinho vermelho (8) e chave dourada (9).
    /// </summary>
    public static Sprite ObjetoV2(int coluna, int linha)
        => Celula("Masmorra/ObjetosV2", coluna, linha);

    /// <summary>Saco de ouro do Tiny Swords (Resources), ~1 unidade.</summary>
    public static Sprite SacoDeOuro
        => Unico("TinySwords/Ouro", 128, new RectInt(46, 50, 48, 50), new Vector2(70f, 75f), 48f);

    /// <summary>Pedaco de carne do Tiny Swords (Resources), ~1 unidade.</summary>
    public static Sprite CarneAssada
        => Unico("TinySwords/Carne", 128, new RectInt(40, 57, 56, 45), new Vector2(68f, 79.5f), 52f);

    /// <summary>Um desenho da folha inteira do Pixel UI pack 3 (InterfacePixel/All), pra usar no mundo.</summary>
    public static Sprite IconeDaInterface(int x, int y, int largura, int altura, float pixelsPorUnidade)
        => Interface("All", x, y, largura, altura, Vector4.zero, pixelsPorUnidade);

    /// <summary>A gema azul do Pixel UI pack 3: o orbe que gira em volta do jogador.</summary>
    public static Sprite OrbeAzul => IconeDaInterface(182, 216, 36, 32, 38f);

    /// <summary>
    /// O comerciante da loja: o homem de chapeu de aba larga e casaco do 2D Pixel Dungeon
    /// Asset Pack v2.0 (Dungeon_Character_2), 16x16, pivo no pe. Gente, nao monstro.
    /// </summary>
    public static Sprite Comerciante
        => Unico("Masmorra/Moradores", 16, new RectInt(32, 0, 16, 16), new Vector2(40f, 16f), PixelsDoLadrilho);

    /// <summary>Uma das quatro pedras do Tiny Swords, sorteada; ~1 unidade de lado.</summary>
    public static Sprite PedraAleatoria()
        => Inteira($"TinySwords/Pedra{Random.Range(1, 5)}", 60f);

    /// <summary>A dinamite parada do Tiny Swords, como icone de bomba (~1 unidade).</summary>
    public static Sprite BombaDeDinamite
    {
        get
        {
            Sprite[] quadros = Dinamite(50f);
            return quadros != null ? quadros[0] : null;
        }
    }

    /// <summary>Tiro dos inimigos: a gema laranja da folha de objetos, a ~1 unidade (o tiro escala).</summary>
    public static Sprite TiroMagico
    {
        get
        {
            Sprite[] um = Linha("Masmorra/Objetos", 16, 1, 1, new RectInt(7 * 16, 0, 16, 16), new Vector2(7 * 16 + 8f, 8f), 11f);
            return um != null ? um[0] : null;
        }
    }

    // ---------------- tiros dos herois (Tiny RPG Pack 01) ----------------
    /// <summary>Flecha de 32x32 do pacote (Soldado, Arqueiro, Dardo), apontando pra direita.</summary>
    public static Sprite FlechaDoHeroi(string nome, float pixelsPorUnidade)
        => Unico($"Personagens/Projeteis/{nome}", 32, new RectInt(6, 12, 22, 10), new Vector2(16.5f, 16.5f), pixelsPorUnidade);

    /// <summary>A bola de fogo do mago (primeiro quadro), com o rastro pra tras: aponta pra direita.</summary>
    public static Sprite BolaDeFogo(float pixelsPorUnidade)
        => Unico("Personagens/Projeteis/BolaDeFogo", 100, new RectInt(40, 44, 24, 16), new Vector2(55f, 51.5f), pixelsPorUnidade);

    /// <summary>A estrela branca dos cristais do mago (setimo quadro). O padre atira ela pintada.</summary>
    public static Sprite Estrela(float pixelsPorUnidade)
        => Unico("Personagens/Projeteis/Cristais", 100, new RectInt(640, 34, 26, 32), new Vector2(653f, 50f), pixelsPorUnidade);

    /// <summary>O risco de luz no fim dos cristais: vira o corte dos herois de espada e machado.</summary>
    public static Sprite Corte(float pixelsPorUnidade)
        => Unico("Personagens/Projeteis/Cristais", 100, new RectInt(839, 30, 25, 35), new Vector2(851.5f, 47.5f), pixelsPorUnidade);

    /// <summary>
    /// A onda de corte dos herois de espada e machado: os rastros de golpe do Tiny RPG
    /// (Knight, Knight Templar, Swordsman), recortados em <c>Projeteis/Cortes</c>, uma linha
    /// por heroi, celulas de 40 px, desenhados voando pra direita. Linha 0 = cavaleiro,
    /// 1 = templario, 2 = espadachim (corte em X), 3 = machadeiro (meia-lua de fogo).
    /// </summary>
    public static Sprite[] OndaDeCorte(int linha, float pixelsPorUnidade)
    {
        int quantos = linha == 3 ? 3 : 2;
        return Linha("Personagens/Projeteis/Cortes", 40, linha, quantos, new RectInt(0, 0, 40, 40), new Vector2(20f, 20f), pixelsPorUnidade);
    }

    /// <summary>O brilho de lamina do espadachim (Tiny RPG): pisca no disparo e quando o dash recarrega.</summary>
    public static Sprite[] Brilho(float pixelsPorUnidade)
        => Linha("Personagens/Projeteis/Cortes", 40, 4, 3, new RectInt(0, 0, 40, 40), new Vector2(20f, 20f), pixelsPorUnidade);

    /// <summary>A nuvem de poeira do Tiny Swords (Particle FX, Dust_01): 8 quadros de 64 px.</summary>
    public static Sprite[] Poeira(float pixelsPorUnidade)
        => Linha("TinySwords/Poeira", 64, 0, 8, new RectInt(0, 0, 64, 64), new Vector2(32f, 32f), pixelsPorUnidade);

    private static Sprite Unico(string caminho, int celula, RectInt recorte, Vector2 centro, float pixelsPorUnidade,
                                int alturaDaCelula = 0)
    {
        Sprite[] um = Linha(caminho, celula, 0, 1, recorte, centro, pixelsPorUnidade, alturaDaCelula);
        return um != null && um.Length > 0 ? um[0] : null;
    }

    private static Sprite Celula(string caminho, int coluna, int linha)
    {
        Sprite[] um = Linha(caminho, 16, linha, 1, new RectInt(coluna * 16, 0, 16, 16),
                            new Vector2(coluna * 16 + 8f, 8f), PixelsDoLadrilho);
        return um != null ? um[0] : null;
    }

    /// <summary>A imagem inteira como um sprite, pivo no meio.</summary>
    private static Sprite Inteira(string caminho, float pixelsPorUnidade)
    {
        string chave = caminho + "@" + pixelsPorUnidade;

        if (sprites.TryGetValue(chave, out Sprite guardado))
            return guardado;

        Texture2D textura = Textura(caminho);
        Sprite sprite = null;

        if (textura != null)
        {
            sprite = Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height), new Vector2(0.5f, 0.5f),
                                   pixelsPorUnidade, 0, SpriteMeshType.FullRect);
            sprite.name = caminho;
        }

        sprites[chave] = sprite;
        return sprite;
    }

    private static ClipesDePersonagem Clipes(string nome, float pixelsPorUnidade, System.Action<ClipesDePersonagem> montar)
    {
        string chave = nome + "@" + pixelsPorUnidade;

        if (personagens.TryGetValue(chave, out ClipesDePersonagem guardado))
            return guardado;

        ClipesDePersonagem clipes = new ClipesDePersonagem();
        montar(clipes);

        if (clipes.Parado == null)
            clipes = null;

        personagens[chave] = clipes;
        return clipes;
    }

    private static readonly Dictionary<string, Sprite[]> linhas = new Dictionary<string, Sprite[]>();

    /// <summary>
    /// Os quadros de uma linha de uma folha em grade de celulas quadradas de lado
    /// <paramref name="celula"/>. O pivo fica no centro do corpo, pra o colisor cair em cima dele.
    /// </summary>
    private static Sprite[] Linha(string caminho, int celula, int linha, int quantos, RectInt recorte, Vector2 centro,
                                  float pixelsPorUnidade, int alturaDaCelula = 0)
    {
        if (alturaDaCelula <= 0)
            alturaDaCelula = celula;

        string chave = $"{caminho}#{linha}:{recorte.x},{recorte.y},{recorte.width}x{recorte.height}@{pixelsPorUnidade}";

        if (linhas.TryGetValue(chave, out Sprite[] guardados))
            return guardados;

        Texture2D textura = Textura(caminho);
        Sprite[] quadros = null;

        if (textura != null && (linha + 1) * alturaDaCelula <= textura.height)
        {
            quantos = Mathf.Min(quantos, textura.width / celula);
            quadros = new Sprite[quantos];

            Vector2 pivo = new Vector2(
                (centro.x - recorte.x) / recorte.width,
                (recorte.yMax - centro.y) / recorte.height);

            for (int i = 0; i < quantos; i++)
            {
                Rect r = Recorte(textura, i * celula + recorte.x, linha * alturaDaCelula + recorte.y, recorte.width, recorte.height);
                quadros[i] = Sprite.Create(textura, r, pivo, pixelsPorUnidade, 0, SpriteMeshType.FullRect);
                quadros[i].name = $"{caminho} {linha}:{i}";
            }
        }

        linhas[chave] = quadros;
        return quadros;
    }

    // ================================================================ interface
    // Retangulos em coordenadas de imagem (x, y do canto de CIMA a esquerda, largura, altura),
    // do jeito que aparecem num editor de imagem. Borda = quanto nao estica (esq, baixo, dir, cima).

    public static Sprite CoracaoVazio => Interface("00", 82, 114, 12, 12, Vector4.zero);
    public static Sprite CoracaoMeio => Interface("00", 98, 114, 12, 12, Vector4.zero);
    public static Sprite CoracaoCheio => Interface("00", 114, 114, 12, 12, Vector4.zero);

    /// <summary>Painel arredondado de borda azulada e miolo cinza.</summary>
    public static Sprite PainelCinza => Interface("00", 0, 85, 48, 22, new Vector4(8, 8, 8, 8));

    /// <summary>Painel arredondado de borda escura e miolo azul.</summary>
    public static Sprite PainelAzul => Interface("00", 64, 85, 48, 22, new Vector4(8, 8, 8, 8));

    /// <summary>Painel arredondado de borda marrom e miolo cinza.</summary>
    public static Sprite PainelMarrom => Interface("00", 128, 85, 48, 22, new Vector4(8, 8, 8, 8));

    /// <summary>O painel marrom pequeno, pra usar no mundo (SpriteRenderer fatiado): placa de preco da loja.</summary>
    public static Sprite PlacaNoMundo => Interface("00", 128, 85, 48, 22, new Vector4(8, 8, 8, 8), 100f);

    /// <summary>Moldura inclinada, oca, da barra (a barra entra 3 px pra dentro).</summary>
    public static Sprite MolduraDaBarra => Interface("04", 0, 3, 48, 11, new Vector4(7, 3, 7, 3));

    public static Sprite BarraVermelha => Interface("04", 51, 6, 42, 5, new Vector4(5, 0, 5, 0));
    public static Sprite BarraAmarela => Interface("04", 51, 22, 42, 5, new Vector4(5, 0, 5, 0));
    public static Sprite BarraVazia => Interface("04", 291, 6, 42, 5, new Vector4(5, 0, 5, 0));

    /// <summary>Quantos pixels da moldura ficam em volta da barra.</summary>
    public const float MargemDaMoldura = 3f;

    private static Sprite Interface(string folha, int x, int y, int largura, int altura, Vector4 borda,
                                    float pixelsPorUnidade = PixelsPorUnidadeDaInterface)
    {
        string chave = $"{folha}:{x},{y},{largura},{altura}@{pixelsPorUnidade}";

        if (sprites.TryGetValue(chave, out Sprite guardado))
            return guardado;

        Texture2D textura = Textura("InterfacePixel/" + folha);
        Sprite sprite = null;

        if (textura != null)
        {
            sprite = Sprite.Create(textura, Recorte(textura, x, y, largura, altura), new Vector2(0.5f, 0.5f),
                                   pixelsPorUnidade, 0, SpriteMeshType.FullRect, borda);
            sprite.name = $"Interface {chave}";
        }

        sprites[chave] = sprite;
        return sprite;
    }

    // ================================================================ ajudas
    /// <summary>Converte um retangulo de editor de imagem (y pra baixo) pro da Unity (y pra cima).</summary>
    private static Rect Recorte(Texture2D textura, int x, int y, int largura, int altura)
        => new Rect(x, textura.height - y - altura, largura, altura);

    private static Texture2D Textura(string caminho, bool avisarSeFaltar = true)
    {
        if (texturas.TryGetValue(caminho, out Texture2D guardada))
            return guardada;

        Texture2D textura = Resources.Load<Texture2D>(caminho);

        if (textura != null)
        {
            // Pixel art: nada de borrar, mesmo se alguem mexer no import.
            textura.filterMode = FilterMode.Point;
            textura.wrapMode = TextureWrapMode.Clamp;
        }
        else if (avisarSeFaltar)
        {
            Debug.LogWarning($"[ArteImportada] nao achei Resources/{caminho}. Usando o desenho antigo.");
        }

        texturas[caminho] = textura;
        return textura;
    }
}

/// <summary>Os quadros de cada animacao de um personagem importado. Qualquer um pode ser null.</summary>
public class ClipesDePersonagem
{
    public Sprite[] Parado;
    public Sprite[] Andando;
    public Sprite[] Ataque;
    public Sprite[] AtaqueEspecial;

    /// <summary>Terceiro ataque do Tiny RPG (so alguns tem: cavaleiros, chefes).</summary>
    public Sprite[] AtaqueForte;

    /// <summary>Golpe pra baixo e pra cima, pra quem tem arte nas tres direcoes (senao usa <see cref="Ataque"/> virado).</summary>
    public Sprite[] AtaqueBaixo;
    public Sprite[] AtaqueCima;
    public Sprite[] Dor;
    public Sprite[] Morte;
}
