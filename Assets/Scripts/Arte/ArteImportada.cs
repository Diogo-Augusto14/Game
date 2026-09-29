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
    public Sprite[] Dor;
    public Sprite[] Morte;
}
