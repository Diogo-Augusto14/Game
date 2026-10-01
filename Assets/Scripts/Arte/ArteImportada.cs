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
///       dinamite (a bomba do jogador), flecha, explosao, caveira de morte e enfeites de chao.
///       Personagem nenhum vem daqui: inimigos e herois sao todos do Tiny RPG.
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
    /// A explosao (9 quadros). A bola de fogo tem uns 105 px de largura: pra ela cobrir um
    /// circulo de raio r, use <c>ExplosaoPixelsPorUnidade(r)</c>.
    /// </summary>
    public static Sprite[] Explosao(float pixelsPorUnidade)
        => Linha("TinySwords/Explosao", 192, 0, 9, new RectInt(0, 0, 192, 192), new Vector2(96f, 94f), pixelsPorUnidade);

    public static float ExplosaoPixelsPorUnidade(float raio) => 105f / Mathf.Max(0.1f, raio * 2f);

    /// <summary>
    /// A flecha do Tiny RPG (a do Arqueiro), apontando pra direita. Quem chama pensa em 48 px de
    /// comprimento (a flecha antiga do Tiny Swords): a conta abaixo mantem o mesmo tamanho na tela.
    /// </summary>
    public static Sprite Flecha(float pixelsPorUnidade)
        => FlechaDoHeroi("Flecha", pixelsPorUnidade * 22f / 48f);

    /// <summary>Os 25 ossos e caveiras do Old Prison (null antes do primeiro sorteio ou sem a arte).</summary>
    private static Sprite[] ossosDaPrisao;

    /// <summary>
    /// Na folha de ossos do Old Prison, os esqueletos inteiros (sentados e deitados de bracos
    /// abertos): do tamanho de um bicho, de longe pareciam inimigo parado.
    /// </summary>
    private static readonly int[] EsqueletosInteiros = { 8, 17, 18, 19, 20, 21 };

    /// <summary>O enfeite e um osso ou caveira do Old Prison.</summary>
    public static bool OssoDaPrisao(Sprite enfeite)
        => enfeite != null && ossosDaPrisao != null && System.Array.IndexOf(ossosDaPrisao, enfeite) >= 0;

    /// <summary>O enfeite e um dos esqueletos inteiros do Old Prison.</summary>
    public static bool EsqueletoInteiro(Sprite enfeite)
        => OssoDaPrisao(enfeite) && System.Array.IndexOf(EsqueletosInteiros, System.Array.IndexOf(ossosDaPrisao, enfeite)) >= 0;

    /// <summary>Nomes dos enfeites de chao (cogumelos, pedrinhas, moitas, ossos).</summary>

    /// <summary>
    /// Um enfeite de chao sorteado, ou null sem a arte: os do Tiny Swords (64 px = 1
    /// ladrilho) e, de vez em quando, uma caveira ou ossos da masmorra.
    /// </summary>
    public static Sprite EnfeiteAleatorio() => EnfeiteAleatorio(0.5f);

    /// <summary>
    /// Como <see cref="EnfeiteAleatorio()"/>, com a chance de sair caveira ou ossos
    /// escolhida pelo tema do andar.
    /// </summary>
    public static Sprite EnfeiteAleatorio(float chanceDeOsso)
    {
        // Old Prison: ossos e caveiras clareados pra aparecer no chao escuro.
        Sprite[] prisao = Linha("Masmorra/Temas/PrisaoEnfeites", 32, 0, 25, new RectInt(0, 0, 32, 32), new Vector2(16f, 16f), PixelsDaPrisao);
        ossosDaPrisao = prisao;

        if (prisao != null && prisao.Length >= 25 && Random.value < Mathf.Max(chanceDeOsso, 0.5f))
            return prisao[Random.Range(0, 25)];

        // O resto: pedrinhas, ossinhos e papeis soltos do Old Prison.
        Sprite[] miudezas = Linha("Masmorra/Prisao/Miudezas", 32, 0, 10, new RectInt(0, 0, 32, 32), new Vector2(16f, 16f), PixelsDaPrisao);
        return miudezas != null && miudezas.Length > 0 ? miudezas[Random.Range(0, miudezas.Length)] : null;
    }

    /// <summary>Tocha de parede acesa (6 quadros de 16x28), com o pivo no suporte.</summary>
    public static Sprite[] TochaDeParede(float pixelsPorUnidade)
        => Linha("Masmorra/Tocha", 16, 0, 6, new RectInt(0, 0, 16, 28), new Vector2(8f, 20f), pixelsPorUnidade, 28);

    /// <summary>
    /// A sala inteira do Old Prison para o tema atual (15x9 unidades: chao, paredes, sombra e
    /// enfeites), uma das 4 versoes sorteada; null sem tema com sala pronta ou sem a arte.
    /// </summary>
    public static Sprite SalaDaPrisao()
    {
        string nome = TemaDoAndar.Atual?.Sala;
        return string.IsNullOrEmpty(nome) ? null : Inteira($"Masmorra/Temas/PrisaoSala{nome}{Random.Range(0, 4)}", PixelsDaPrisao);
    }

    /// <summary>
    /// O vao da porta desenhado por cima da sala pronta: passagem escura com batentes na parede de
    /// cima, chao saindo da sala nas outras. O pivo fica no meio da faixa da parede.
    /// </summary>
    public static Sprite VaoDaPrisao(LadoDaPorta lado)
    {
        switch (lado)
        {
            case LadoDaPorta.Cima:
                return Unico("Masmorra/Temas/PrisaoVaoCima", 48, new RectInt(0, 0, 48, 48), new Vector2(24f, 16f), PixelsDaPrisao, 48);
            case LadoDaPorta.Baixo:
                return Unico("Masmorra/Temas/PrisaoVaoBaixo", 48, new RectInt(0, 0, 48, 32), new Vector2(24f, 16f), PixelsDaPrisao, 32);
            default:
                return Unico("Masmorra/Temas/PrisaoVaoLado", 32, new RectInt(0, 0, 32, 48), new Vector2(16f, 24f), PixelsDaPrisao, 48);
        }
    }

    /// <summary>
    /// Dica da porta secreta do Old Prison. Em cima e a parede rachada (mesmo pivo do vao). Embaixo e
    /// dos lados a parede fica igual as outras e a dica sao pedrinhas do pack caidas no chao, na cor
    /// dos tijolos do tema (pivo no meio). Null sem a arte.
    /// </summary>
    public static Sprite RachaduraDaPrisao(LadoDaPorta lado)
    {
        string tema = TemaDoAndar.Atual?.Sala;

        if (string.IsNullOrEmpty(tema))
            return null;

        if (lado == LadoDaPorta.Cima)
            return Unico("Masmorra/Temas/PrisaoRachaduraCima" + tema, 48, new RectInt(0, 0, 48, 48), new Vector2(24f, 16f), PixelsDaPrisao, 48);

        return Unico("Masmorra/Temas/PrisaoPedrinhas" + tema, 64, new RectInt(0, 0, 64, 32), new Vector2(32f, 16f), PixelsDaPrisao, 32);
    }

    /// <summary>A grade de ferro da porta (5 quadros: 0 fechada, 4 aberta), mesmo pivo do vao.</summary>
    public static Sprite[] GradeDaPrisao(LadoDaPorta lado)
    {
        switch (lado)
        {
            case LadoDaPorta.Cima:
                return Linha("Masmorra/Temas/PrisaoGradeCima", 48, 0, 5, new RectInt(0, 0, 48, 48), new Vector2(24f, 16f), PixelsDaPrisao, 48);
            case LadoDaPorta.Baixo:
                return Linha("Masmorra/Temas/PrisaoGradeBaixo", 48, 0, 5, new RectInt(0, 0, 48, 32), new Vector2(24f, 16f), PixelsDaPrisao, 32);
            default:
                return Linha("Masmorra/Temas/PrisaoGradeLado", 32, 0, 5, new RectInt(0, 0, 32, 48), new Vector2(16f, 24f), PixelsDaPrisao, 48);
        }
    }

    /// <summary>Bandeira de parede do Old Prison (7 modelos), ou null sem a arte.</summary>
    public static Sprite BandeiraDaPrisao(int modelo)
    {
        Sprite[] todas = Linha("Masmorra/Temas/PrisaoBandeiras", 32, 0, 7, new RectInt(0, 0, 32, 40), new Vector2(16f, 20f), PixelsDaPrisao, 40);
        return todas != null && todas.Length > 0 ? todas[Mathf.Abs(modelo) % todas.Length] : null;
    }

    /// <summary>Candelabro alto do Old Prison com as velas acesas (2 quadros), pivo no pe.</summary>
    public static Sprite[] CandelabroDaPrisao()
        => Linha("Masmorra/Temas/PrisaoCandelabros", 32, 0, 2, new RectInt(0, 0, 32, 64), new Vector2(16f, 62f), PixelsDaPrisao, 64);

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
        => string.IsNullOrEmpty(nome) ? null : Inteira("Masmorra/Temas/" + nome, nome.StartsWith("Prisao") ? PixelsDaPrisao : PixelsDoLadrilho);

    /// <summary>EPIC RPG World Pack - Old Prison: ladrilhos de 32 px, 32 pixels por unidade (1 ladrilho = 1 unidade).</summary>
    private const float PixelsDaPrisao = 32f;

    /// <summary>Os 5 quadros do portao subindo, do fechado (0) ao aberto (4): a animacao da porta.</summary>
    public static Sprite[] QuadrosDoPortao
        => Linha("Masmorra/Portao", 16, 0, 5, new RectInt(0, 0, 16, 16), new Vector2(8f, 8f), PixelsDoLadrilho, 32);

    /// <summary>Bau de madeira abrindo (4 quadros de 16x16): fechado, tampa tremendo, aberto, aberto com o ouro.</summary>
    public static Sprite[] BauAbrindo
        => Linha("Masmorra/Prisao/BauDeMadeira", 128, 0, 11, new RectInt(30, 40, 68, 68), new Vector2(63f, 86f), 48f, 160);

    /// <summary>Bau de pedra do Old Prison, fechado (o trancado: pede chave).</summary>
    public static Sprite[] BauDeFerroParado
    {
        get
        {
            Sprite[] todos = BauDeFerroAbrindo;
            return todos != null && todos.Length > 0 ? new[] { todos[0] } : null;
        }
    }

    /// <summary>Bau de pedra do Old Prison abrindo (16 quadros: a tampa desliza pro lado).</summary>
    public static Sprite[] BauDeFerroAbrindo
        => Linha("Masmorra/Prisao/BauDePedra", 169, 0, 16, new RectInt(24, 48, 120, 60), new Vector2(103f, 82f), 56f, 120);

    /// <summary>Chave dourada girando (8 quadros de 16x16): a chave que o chefe deixa.</summary>
    public static Sprite[] ChaveDourada
        => Linha("Masmorra/ChaveDourada", 16, 0, 8, new RectInt(0, 0, 16, 16), new Vector2(8f, 8f), PixelsDoLadrilho);

    /// <summary>
    /// Espinhos do Old Prison: o chao de sangue com lancas do tileset, um ladrilho (um quadro so,
    /// todo pra fora; quem usa pega o ultimo).
    /// </summary>
    public static Sprite[] EspinhosDoChao
        => Linha("Masmorra/Prisao/Espinhos", 32, 0, 1, new RectInt(0, 0, 32, 32), new Vector2(16f, 16f), PixelsDaPrisao);

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

    /// <summary>A donzela de ferro do Old Prison (pivo no meio, ~0.6x1.3): enfeite e marca da sala amaldicoada.</summary>
    public static Sprite IdoloMaldito => PecaDaPrisao("Donzela", 64f, false);

    /// <summary>
    /// O desenho de cada item passivo, pelo nome, com os icones do Raven Fantasy Icons.
    /// Nome desconhecido usa uma gema.
    /// </summary>
    public static Sprite IconeDoItem(string nome)
    {
        // Item de flecha: o desenho da propria flecha.
        Sprite flecha = CatalogoDeFlechas.IconeDoItem(nome);

        if (flecha != null)
            return flecha;

        Sprite ativo = CatalogoDeAtivos.Icone(nome);

        if (ativo != null)
            return ativo;

        // Os nomes ganharam acento ("Café"); os casos abaixo continuam sem.
        switch (FonteDoJogo.SemAcentos(nome))
        {
            case "Elixir da Pressa": return IconeDoPacote(266);
            case "Pedra de Amolar": return IconeDoPacote(2129);
            case "Ferradura Encantada": return IconeDoPacote(696);
            case "Runa Triplice": return IconeDoPacote(385);
            case "Runa Gemea": return IconeDoPacote(236);
            case "Olho de Falcao": return IconeDoPacote(719);
            case "Coracao de Leao": return IconeDoPacote(659);
            case "Ponta de Chumbo": return IconeDoPacote(654);
            case "Hidromel": return IconeDoPacote(529);
            case "Saco de Moedas": return IconeDoPacote(158);
            case "Essencia Fantasma": return IconeDoPacote(653);
            case "Bussola Maldita": return IconeDoPacote(2184);
            case "Elmo de Duas Faces": return IconeDoPacote(691);
            case "Oleo Ardente": return IconeDoPacote(438);
            case "Pena da Fenix": return IconeDoPacote(7);
            case "Escudo Sagrado": return IconeDoPacote(665);
            case "Sangue de Vampiro": return IconeDoPacote(742);
            case "Prego Enferrujado": return IconeDoPacote(1444);
            case "Pedra-Ima": return IconeDoPacote(117);
            case "Amuleto da Sorte": return IconeDoPacote(668);
            case "Bolsa do Mercador": return IconeDoPacote(160);
            case "Brasa da Furia": return IconeDoPacote(993);
            case "Elixir de Nevoa": return IconeDoPacote(123);
            case "Orbe Guardiao": return IconeDoPacote(335);
            case "Barril de Polvora": return IconeDoPacote(340);
            case "Carne Assada": return IconeDoPacote(486);
            case "Pacto de Sangue": return IconeDoPacote(289);
        }

        // Item sem desenho proprio: uma gema, sempre a mesma.
        return IconeDoPacote(2129);
    }

    /// <summary>
    /// Um icone 32x32 do pack Raven Fantasy Icons (Resources/Icones/fbN, N = numero na folha
    /// completa), ~1 unidade de lado.
    /// </summary>
    public static Sprite IconeDoPacote(int numero)
        => Inteira($"Icones/fb{numero}", 32f);

    /// <summary>
    /// Uma celula de 16x16 da folha <c>Masmorra/ObjetosV2</c> (as tres ultimas linhas do
    /// tileset do 2D Pixel Dungeon Asset Pack v2.0): linha 0 tem o escudo (4) e a caveira (7);
    /// linha 1 moeda (6), frasco azul (7), chave (8) e frasco vermelho (9); linha 2 frasco
    /// azul grande (7), frasquinho vermelho (8) e chave dourada (9).
    /// </summary>
    public static Sprite ObjetoV2(int coluna, int linha)
        => Celula("Masmorra/ObjetosV2", coluna, linha);

    /// <summary>Um desenho da folha inteira do Pixel UI pack 3 (InterfacePixel/All), pra usar no mundo.</summary>
    public static Sprite IconeDaInterface(int x, int y, int largura, int altura, float pixelsPorUnidade)
        => Interface("All", x, y, largura, altura, Vector4.zero, pixelsPorUnidade);

    /// <summary>A gema azul do Pixel UI pack 3: o orbe que gira em volta do jogador.</summary>
    public static Sprite OrbeAzul => IconeDaInterface(182, 216, 36, 32, 38f);

    /// <summary>Uma das quatro pedras do Tiny Swords, sorteada; ~1 unidade de lado.</summary>
    public static Sprite PedraAleatoria()
    {
        // Old Prison: doze pedras de um ladrilho cada (32 px), da mesma paleta da sala.
        Sprite[] pedras = Linha("Masmorra/Prisao/Pedras", 32, 0, 12, new RectInt(0, 0, 32, 32), new Vector2(16f, 16f), PixelsDaPrisao);
        return pedras != null && pedras.Length > 0 ? pedras[Random.Range(0, pedras.Length)] : null;
    }

    /// <summary>Barril do Old Prison (enfeite da loja), com o pivo no pe.</summary>
    public static Sprite BarrilDaPrisao
        => Unico("Masmorra/Prisao/Barril", 32, new RectInt(0, 16, 32, 48), new Vector2(16f, 60f), PixelsDaPrisao, 64);

    /// <summary>Caixote do Old Prison (enfeite da loja).</summary>
    public static Sprite CaixoteDaPrisao
        => Unico("Masmorra/Prisao/Caixote", 32, new RectInt(0, 0, 32, 32), new Vector2(16f, 24f), PixelsDaPrisao);

    /// <summary>
    /// A serra giratoria da armadilha do Old Prison (sem sombra: ela gira inteira), com o pivo no
    /// eixo. Uns 60 px de lamina = 1 unidade.
    /// </summary>
    public static Sprite SerraDaPrisao
        => Unico("Masmorra/Prisao/Serra", 128, new RectInt(34, 22, 76, 76), new Vector2(72f, 60f), 60f);

    /// <summary>
    /// Pecas do trilho da serra (32 px = 1 ladrilho): 0 deitado, 1 em pe, 2 ponta da esquerda,
    /// 3 ponta da direita, 4 ponta de cima, 5 ponta de baixo.
    /// </summary>
    public static Sprite[] TrilhoDaPrisao
        => Linha("Masmorra/Prisao/Trilhos", 32, 0, 6, new RectInt(0, 0, 32, 32), new Vector2(16f, 16f), PixelsDaPrisao);

    /// <summary>
    /// Uma peca solta do atlas do Old Prison (Masmorra/Prisao/Pecas/<paramref name="nome"/>), ja
    /// recortada, com o pivo no pe (ou no meio, com <paramref name="pivoNoPe"/> falso). A 32 px por
    /// unidade fica do tamanho do ladrilho da sala.
    /// </summary>
    public static Sprite PecaDaPrisao(string nome, float pixelsPorUnidade = PixelsDaPrisao, bool pivoNoPe = true)
    {
        string caminho = "Masmorra/Prisao/Pecas/" + nome;
        string chave = $"{caminho}@{pixelsPorUnidade}:{pivoNoPe}";

        if (sprites.TryGetValue(chave, out Sprite guardado))
            return guardado;

        Texture2D textura = Textura(caminho);
        Sprite sprite = null;

        if (textura != null)
        {
            sprite = Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height),
                                   pivoNoPe ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f), pixelsPorUnidade, 0, SpriteMeshType.FullRect);
            sprite.name = caminho;
        }

        sprites[chave] = sprite;
        return sprite;
    }

    /// <summary>Uma das <paramref name="quantas"/> variacoes numeradas de uma peca (Retrato1..12), sorteada.</summary>
    public static Sprite PecaSorteada(string nome, int quantas, float pixelsPorUnidade = PixelsDaPrisao, bool pivoNoPe = true)
        => PecaDaPrisao(nome + Random.Range(1, quantas + 1), pixelsPorUnidade, pivoNoPe);

    /// <summary>
    /// O vortice do portao do Old Prison (12 quadros girando): o buraco que leva ao proximo
    /// andar. Uns 39 px de largura; a 30 px por unidade fica com ~1.3.
    /// </summary>
    public static Sprite[] Vortice(float pixelsPorUnidade)
        => Linha("Masmorra/Prisao/Vortice", 96, 0, 12, new RectInt(24, 22, 48, 44), new Vector2(47.5f, 43.5f), pixelsPorUnidade);

    /// <summary>
    /// A grade da cela do Old Prison subindo (23 quadros). So a parte de baixo da grade fechada
    /// (72x48, o vao inteiro de 1.5x1 a 48 px por unidade): abrindo, ela some pra cima do recorte.
    /// </summary>
    public static Sprite[] PortaoDaCela
        => Linha("Masmorra/Prisao/PortaoDaCela", 128, 0, 23, new RectInt(29, 101, 72, 48), new Vector2(65f, 125f), 48f, 160);

    /// <summary>
    /// A mesa que vira de lado (quadros 0 a 6) e quebra (7 a 9), vista de cima, pivo no meio do
    /// tampo em pe. A 48 px por unidade o tampo tem ~0.8x1.5.
    /// </summary>
    public static Sprite[] MesaQueVira
        => Linha("Masmorra/Prisao/Mesa", 96, 0, 10, new RectInt(0, 16, 96, 96), new Vector2(30.5f, 72.5f), 48f, 128);

    /// <summary>
    /// O tronco com estacas rolando (11 quadros). Deitado (rola pra cima e pra baixo) ou em pe
    /// (rola pros lados). Pivo no meio; a 40 px por unidade o deitado tem ~3.2 de comprimento.
    /// </summary>
    public static Sprite[] TroncoRolando(bool deitado, float pixelsPorUnidade)
        => deitado
            ? Linha("Masmorra/Prisao/Tronco", 192, 0, 11, new RectInt(24, 26, 144, 46), new Vector2(96f, 49f), pixelsPorUnidade, 96)
            : Linha("Masmorra/Prisao/TroncoEmPe", 96, 0, 11, new RectInt(24, 8, 46, 132), new Vector2(47.5f, 74f), pixelsPorUnidade, 160);

    /// <summary>
    /// O portao de caveira do Old Prison com o pedaco de parede em volta (18 quadros: as duas
    /// folhas abrem pra sala). Pivo no meio da parede de cima, como o vao e a grade da porta; a
    /// 48 px por unidade fica com 2 de largura, o pe na linha do chao e o alto cortado rente a
    /// parede (o arco passava pra fora da sala).
    /// </summary>
    public static Sprite[] PortaoDeCaveira
        => Linha("Masmorra/Prisao/PortaoDeCaveira", 96, 0, 18, new RectInt(0, 27, 96, 91), new Vector2(48f, 51f), 48f, 118);

    /// <summary>
    /// Porta de madeira do Old Prison abrindo (9 quadros: fechada ate de lado), modelo 1 ou 2.
    /// Pivo no meio da parede de cima; a 51 px por unidade cabe no vao (1.1 x 1.5).
    /// </summary>
    public static Sprite[] PortaDeMadeira(int modelo)
        => Linha("Masmorra/Prisao/PortaDeMadeira" + (modelo == 2 ? 2 : 1), 96, 0, 9, new RectInt(14, 32, 68, 80), new Vector2(47f, 59f), 51f, 128);

    /// <summary>A alavanca de engrenagem do Old Prison (12 quadros: a manivela vai e volta).</summary>
    public static Sprite[] Alavanca
        => Linha("Masmorra/Prisao/Alavanca", 64, 0, 12, new RectInt(4, 20, 56, 40), new Vector2(32f, 57f), PixelsDaPrisao);

    /// <summary>A engrenagem inteira girando (4 quadros de 43x45).</summary>
    public static Sprite[] Engrenagem
        => Linha("Masmorra/Prisao/Engrenagem", 43, 0, 4, new RectInt(0, 0, 43, 45), new Vector2(21.5f, 22.5f), PixelsDaPrisao, 45);

    /// <summary>Gota de sangue pingando da parede (9 quadros de 32x64), pivo no alto da queda.</summary>
    public static Sprite[] GotaDeSangue
        => Linha("Masmorra/Prisao/Sangue", 32, 0, 9, new RectInt(0, 0, 32, 64), new Vector2(16f, 0f), PixelsDaPrisao, 64);

    /// <summary>A chama magica azul (8 quadros), pivo na base da chama.</summary>
    public static Sprite[] ChamaMagica
        => Linha("Masmorra/Prisao/ChamaMagica", 32, 0, 8, new RectInt(8, 28, 16, 22), new Vector2(16.5f, 47f), PixelsDaPrisao, 64);

    /// <summary>Um bando de baratas andando (8 quadros de 67x60).</summary>
    public static Sprite[] Baratas
        => Linha("Masmorra/Prisao/Baratas", 67, 0, 8, new RectInt(10, 14, 46, 36), new Vector2(33f, 32f), PixelsDaPrisao, 60);

    /// <summary>A poeira que sobe dos buracos (16 quadros de 160x64).</summary>
    public static Sprite[] PoeiraDoFosso
        => Linha("Masmorra/Prisao/PoeiraDoFosso", 160, 0, 16, new RectInt(16, 20, 144, 32), new Vector2(88f, 36f), PixelsDaPrisao, 64);

    /// <summary>Dinamite acesa do Tiny Swords (6 quadros): o desenho da bomba do jogador.</summary>
    public static Sprite[] Dinamite(float pixelsPorUnidade)
        => Linha("TinySwords/Dinamite", 64, 0, 6, new RectInt(0, 0, 64, 64), new Vector2(34f, 28f), pixelsPorUnidade);

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

    // ---------------- flechas especiais e tiros dos inimigos ----------------
    /// <summary>A bola de fogo do mago voando (quadros 0 a 3, com o rastro pra tras): aponta pra direita.</summary>
    public static Sprite[] BolaDeFogoVoando(float pixelsPorUnidade)
        => Linha("Personagens/Projeteis/BolaDeFogo", 100, 0, 4, new RectInt(36, 40, 30, 24), new Vector2(55f, 51.5f), pixelsPorUnidade);

    /// <summary>A bola de fogo estourando (quadros 4 a 6 da mesma tira).</summary>
    public static Sprite[] BolaDeFogoEstourando(float pixelsPorUnidade)
        => Fatia(Linha("Personagens/Projeteis/BolaDeFogo", 100, 0, 7, new RectInt(36, 36, 32, 32), new Vector2(54f, 52f), pixelsPorUnidade), 4, 3);

    /// <summary>A estrela azul de cristal girando (quadros 2 a 5 dos cristais do mago).</summary>
    public static Sprite[] CristalGirando(float pixelsPorUnidade)
        => Fatia(Linha("Personagens/Projeteis/Cristais", 100, 0, 6, new RectInt(34, 30, 36, 40), new Vector2(53f, 50f), pixelsPorUnidade), 2, 4);

    /// <summary>Os cristais se juntando (quadros 0 e 1): o gelo prendendo o inimigo.</summary>
    public static Sprite[] CristaisSeJuntando(float pixelsPorUnidade)
        => Linha("Personagens/Projeteis/Cristais", 100, 0, 2, new RectInt(22, 12, 66, 78), new Vector2(55f, 51f), pixelsPorUnidade);

    /// <summary>O raio verde do necromante caindo (quadros 0 e 1): aponta pra BAIXO.</summary>
    public static Sprite[] MagiaVerde(float pixelsPorUnidade)
        => Linha("Personagens/Projeteis/MagiaVerde", 100, 0, 2, new RectInt(42, 14, 14, 30), new Vector2(49f, 36f), pixelsPorUnidade);

    /// <summary>A nuvem verde em que o raio do necromante estoura (quadros 3 a 5).</summary>
    public static Sprite[] MagiaVerdeEstourando(float pixelsPorUnidade)
        => Fatia(Linha("Personagens/Projeteis/MagiaVerde", 100, 0, 6, new RectInt(28, 20, 50, 42), new Vector2(53f, 44f), pixelsPorUnidade), 3, 3);

    /// <summary>A bala do cavaleiro do canhao.</summary>
    public static Sprite BalaDeCanhao(float pixelsPorUnidade)
        => Unico("Personagens/Projeteis/BolaDeCanhao", 32, new RectInt(11, 12, 11, 9), new Vector2(16.5f, 16.5f), pixelsPorUnidade);

    /// <summary>
    /// Uma pedra do Old Prison pros tiros de pedra (1 a 12). A pedra tem 32 px, metade das do
    /// Tiny Swords de antes: os pixels por unidade caem pela metade pro tiro ficar do mesmo tamanho.
    /// </summary>
    public static Sprite Pedra(int qual, float pixelsPorUnidade)
    {
        Sprite[] pedras = Linha("Masmorra/Prisao/Pedras", 32, 0, 12, new RectInt(0, 0, 32, 32), new Vector2(16f, 16f), pixelsPorUnidade * 0.5f);
        return pedras != null && pedras.Length > 0 ? pedras[Mathf.Clamp(qual, 1, pedras.Length) - 1] : null;
    }

    /// <summary>Explosao pequena (Tiny Swords Free Pack, 8 quadros): a da flecha explosiva.</summary>
    public static Sprite[] ExplosaoPequena(float pixelsPorUnidade)
        => Linha("Efeitos/ExplosaoPequena", 192, 0, 8, new RectInt(58, 56, 74, 72), new Vector2(95f, 92f), pixelsPorUnidade);

    /// <summary>Respingo d'agua (Tiny Swords Free Pack, 9 quadros); tingido vira gelo, sangue, bolha.</summary>
    public static Sprite[] Respingo(float pixelsPorUnidade)
        => Linha("Efeitos/Respingo", 192, 0, 9, new RectInt(48, 54, 100, 92), new Vector2(97f, 98f), pixelsPorUnidade);

    /// <summary>A bolhinha redonda do primeiro quadro do respingo.</summary>
    public static Sprite Bolhinha(float pixelsPorUnidade)
        => Unico("Efeitos/Respingo", 192, new RectInt(82, 83, 28, 27), new Vector2(95.5f, 96f), pixelsPorUnidade);

    /// <summary>Chama subindo (Tiny Swords Free Pack, 10 quadros): o rastro da flecha explosiva.</summary>
    public static Sprite[] Chama(float pixelsPorUnidade)
        => Linha("Efeitos/Chama", 64, 0, 10, new RectInt(14, 20, 38, 44), new Vector2(33f, 44f), pixelsPorUnidade);

    private static Sprite[] Fatia(Sprite[] quadros, int inicio, int quantos)
    {
        if (quadros == null || quadros.Length < inicio + quantos)
            return null;

        Sprite[] parte = new Sprite[quantos];
        System.Array.Copy(quadros, inicio, parte, 0, quantos);
        return parte;
    }

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
