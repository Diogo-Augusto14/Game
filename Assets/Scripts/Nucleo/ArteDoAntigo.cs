using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Recorta a arte que veio do jogo antigo (em Resources: os icones dos itens, as pecas do Old Prison,
/// a dinamite, a explosao...). Os retangulos sao em coordenadas de imagem (x, y do canto de CIMA a
/// esquerda), como num editor de imagem. Tudo fica guardado: pedir de novo nao recorta de novo.
/// </summary>
public static class ArteDoAntigo
{
    /// <summary>Pixels por unidade das pecas do Old Prison (o mesmo dos ladrilhos da caverna).</summary>
    public const float PixelsDaPrisao = 32f;

    private static readonly Dictionary<string, Texture2D> texturas = new Dictionary<string, Texture2D>();
    private static readonly Dictionary<string, Sprite[]> linhas = new Dictionary<string, Sprite[]>();
    private static readonly Dictionary<string, Sprite> soltos = new Dictionary<string, Sprite>();

    public static Texture2D Textura(string caminho)
    {
        if (texturas.TryGetValue(caminho, out Texture2D guardada) && guardada != null)
            return guardada;

        Texture2D textura = Resources.Load<Texture2D>(caminho);

        if (textura != null)
        {
            textura.filterMode = FilterMode.Point;
            textura.wrapMode = TextureWrapMode.Clamp;
        }
        else
        {
            Debug.LogWarning("[Arte] faltou " + caminho);
        }

        texturas[caminho] = textura;
        return textura;
    }

    /// <summary>
    /// Uma tira de quadros: cada quadro numa celula de <paramref name="celula"/> de largura (e
    /// <paramref name="altura"/> de altura, 0 = igual), recortado em <paramref name="recorte"/> dentro da
    /// celula, com o pivo em <paramref name="centro"/> (tudo em pixels, do canto de cima a esquerda).
    /// </summary>
    public static Sprite[] Linha(string caminho, int celula, int quantos, RectInt recorte, Vector2 centro,
                                 float pixelsPorUnidade, int altura = 0)
    {
        string chave = $"{caminho}:{celula}:{recorte}:{centro}:{pixelsPorUnidade}";

        if (linhas.TryGetValue(chave, out Sprite[] guardados))
            return guardados;

        Texture2D textura = Textura(caminho);
        Sprite[] quadros = new Sprite[0];

        if (textura != null)
        {
            if (altura <= 0)
                altura = textura.height;

            quantos = Mathf.Min(quantos, textura.width / celula);
            quadros = new Sprite[quantos];
            Vector2 pivo = new Vector2((centro.x - recorte.x) / recorte.width, (recorte.yMax - centro.y) / recorte.height);

            for (int i = 0; i < quantos; i++)
            {
                Rect r = new Rect(i * celula + recorte.x, textura.height - recorte.y - recorte.height, recorte.width, recorte.height);
                quadros[i] = Sprite.Create(textura, r, pivo, pixelsPorUnidade, 0, SpriteMeshType.FullRect);
                quadros[i].name = $"{caminho} {i}";
            }
        }

        linhas[chave] = quadros;
        return quadros;
    }

    /// <summary>A imagem inteira, pivo no meio (ou no pe).</summary>
    public static Sprite Inteira(string caminho, float pixelsPorUnidade, bool pivoNoPe = false)
    {
        string chave = $"{caminho}@{pixelsPorUnidade}:{pivoNoPe}";

        if (soltos.TryGetValue(chave, out Sprite guardado))
            return guardado;

        Texture2D textura = Textura(caminho);
        Sprite sprite = null;

        if (textura != null)
        {
            sprite = Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height),
                                   pivoNoPe ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f), pixelsPorUnidade, 0, SpriteMeshType.FullRect);
            sprite.name = caminho;
        }

        soltos[chave] = sprite;
        return sprite;
    }

    /// <summary>Um icone de item do pacote (Icones/fbN, 32 px).</summary>
    public static Sprite Icone(int numero, float pixelsPorUnidade = 40f) => Inteira("Icones/fb" + numero, pixelsPorUnidade);

    /// <summary>Uma peca solta do Old Prison (Masmorra/Prisao/Pecas/...), pivo no pe.</summary>
    public static Sprite Peca(string nome, float pixelsPorUnidade = PixelsDaPrisao) =>
        Inteira("Masmorra/Prisao/Pecas/" + nome, pixelsPorUnidade, true);

    // ---------------- as que o jogo usa ----------------
    public static Sprite[] Dinamite => Linha("TinySwords/Dinamite", 64, 6, new RectInt(0, 0, 64, 64), new Vector2(34f, 28f), 70f);

    public static Sprite[] Explosao(float raio) =>
        Linha("TinySwords/Explosao", 192, 9, new RectInt(0, 0, 192, 192), new Vector2(96f, 94f), 105f / Mathf.Max(0.1f, raio * 2f));

    public static Sprite[] ExplosaoPequena => Linha("Efeitos/ExplosaoPequena", 192, 8, new RectInt(58, 56, 74, 72), new Vector2(95f, 92f), 60f);

    public static Sprite[] Chave => Linha("Masmorra/ChaveDourada", 16, 8, new RectInt(0, 0, 16, 16), new Vector2(8f, 8f), 22f);

    public static Sprite Serra => Linha("Masmorra/Prisao/Serra", 128, 1, new RectInt(34, 22, 76, 76), new Vector2(72f, 60f), 60f)[0];

    public static Sprite[] Trilhos => Linha("Masmorra/Prisao/Trilhos", 32, 6, new RectInt(0, 0, 32, 32), new Vector2(16f, 16f), PixelsDaPrisao);

    public static Sprite[] Pedras => Linha("Masmorra/Prisao/Pedras", 32, 12, new RectInt(0, 0, 32, 32), new Vector2(16f, 16f), PixelsDaPrisao);

    public static Sprite[] Mesa => Linha("Masmorra/Prisao/Mesa", 96, 10, new RectInt(0, 16, 96, 96), new Vector2(30.5f, 72.5f), 48f, 128);

    public static Sprite[] Tronco => Linha("Masmorra/Prisao/TroncoEmPe", 96, 11, new RectInt(24, 8, 46, 132), new Vector2(47.5f, 74f), 40f, 160);

    public static Sprite[] ChamaMagica => Linha("Masmorra/Prisao/ChamaMagica", 32, 8, new RectInt(8, 28, 16, 22), new Vector2(16.5f, 47f), PixelsDaPrisao, 64);

    public static Sprite[] Baratas => Linha("Masmorra/Prisao/Baratas", 67, 8, new RectInt(10, 14, 46, 36), new Vector2(33f, 32f), PixelsDaPrisao, 60);

    public static Sprite Barril => Linha("Masmorra/Prisao/Barril", 32, 1, new RectInt(0, 16, 32, 48), new Vector2(16f, 60f), PixelsDaPrisao, 64)[0];

    public static Sprite Caixote => Linha("Masmorra/Prisao/Caixote", 32, 1, new RectInt(0, 0, 32, 32), new Vector2(16f, 30f), PixelsDaPrisao)[0];

}
