using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// As imagens que vieram de pacote (nao desenhadas por codigo), lidas de
/// <c>Assets/Arte/Resources</c>:
///
///   Personagens/Demonio, Personagens/MonstroDeSangue  (Tiny RPG Character Asset Pack 02)
///       uma tira por animacao, quadros de 100x100 com o bicho (uns 20 px) no meio
///   InterfacePixel/00.png ... 07.png  (Pixel UI pack 3)
///       coracoes, paineis e barras recortados pelos retangulos la embaixo
///   TinySwords/...  (Tiny Swords e Tiny Swords Free Pack, da Pixel Frog)
///       folhas em grade (uma linha por animacao): goblins, barril, arqueiro, dinamite,
///       explosao, caveira de morte e enfeites de chao
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
    /// Pedaco do quadro de 100x100 que vale a pena: o bicho mais o golpe (que sai pro lado).
    /// Em coordenadas de imagem (y pra baixo): x 20..84, y 20..68.
    /// </summary>
    private static readonly RectInt RecorteDoPersonagem = new RectInt(20, 20, 64, 48);

    /// <summary>Centro do corpo dentro do quadro de 100x100 (imagem, y pra baixo).</summary>
    private static readonly Vector2 CentroDoCorpo = new Vector2(52f, 50f);

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
        Texture2D textura = Textura($"Personagens/{pasta}/{animacao}");

        if (textura == null)
            return null;

        int quantos = textura.width / QuadroDoPersonagem;
        Sprite[] quadros = new Sprite[quantos];

        // Pivo no centro do corpo, pra o colisor redondo do inimigo cair em cima dele.
        Vector2 pivo = new Vector2(
            (CentroDoCorpo.x - RecorteDoPersonagem.x) / RecorteDoPersonagem.width,
            (RecorteDoPersonagem.yMax - CentroDoCorpo.y) / RecorteDoPersonagem.height);

        for (int i = 0; i < quantos; i++)
        {
            Rect recorte = Recorte(textura, i * QuadroDoPersonagem + RecorteDoPersonagem.x, RecorteDoPersonagem.y,
                                   RecorteDoPersonagem.width, RecorteDoPersonagem.height);

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

    /// <summary>Nomes dos enfeites de chao (cogumelos, pedrinhas, moitas, ossos).</summary>
    private static readonly string[] Enfeites = { "01", "02", "03", "04", "05", "06", "07", "10", "14", "15" };

    /// <summary>Um enfeite de chao sorteado (64 px = 1 ladrilho), ou null sem a arte.</summary>
    public static Sprite EnfeiteAleatorio()
    {
        string nome = Enfeites[Random.Range(0, Enfeites.Length)];
        Sprite[] um = Linha("TinySwords/Enfeites/" + nome, 64, 0, 1, new RectInt(0, 0, 64, 64), new Vector2(32f, 32f), 64f);
        return um != null ? um[0] : null;
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
                                  float pixelsPorUnidade)
    {
        string chave = $"{caminho}#{linha}@{pixelsPorUnidade}";

        if (linhas.TryGetValue(chave, out Sprite[] guardados))
            return guardados;

        Texture2D textura = Textura(caminho);
        Sprite[] quadros = null;

        if (textura != null && (linha + 1) * celula <= textura.height)
        {
            quantos = Mathf.Min(quantos, textura.width / celula);
            quadros = new Sprite[quantos];

            Vector2 pivo = new Vector2(
                (centro.x - recorte.x) / recorte.width,
                (recorte.yMax - centro.y) / recorte.height);

            for (int i = 0; i < quantos; i++)
            {
                Rect r = Recorte(textura, i * celula + recorte.x, linha * celula + recorte.y, recorte.width, recorte.height);
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

    private static Texture2D Textura(string caminho)
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
        else
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

    /// <summary>Golpe pra baixo e pra cima, pra quem tem arte nas tres direcoes (senao usa <see cref="Ataque"/> virado).</summary>
    public Sprite[] AtaqueBaixo;
    public Sprite[] AtaqueCima;
    public Sprite[] Dor;
    public Sprite[] Morte;
}
