using UnityEngine;

/// <summary>
/// Sprites simples gerados por codigo (quadrado e circulo brancos, de 1 unidade), pra a
/// cena top-down rodar sem nenhuma arte preparada. A cor vem do SpriteRenderer.color.
/// Troque por sprites de verdade quando tiver a arte.
/// </summary>
public static class FormasTopDown
{
    private static Sprite quadrado;
    private static Sprite circulo;
    private static Sprite quadradoEsquerda;

    /// <summary>Quadrado branco de 1x1 unidade.</summary>
    public static Sprite Quadrado()
    {
        if (quadrado != null)
            return quadrado;

        const int lado = 4;
        Texture2D textura = NovaTextura("TopDownQuadrado", lado);
        Color32[] pixels = new Color32[lado * lado];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 255);

        textura.SetPixels32(pixels);
        textura.Apply();

        quadrado = Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f), lado);
        quadrado.name = "TopDownQuadrado";
        return quadrado;
    }

    /// <summary>Quadrado branco de 1x1 com o pivo na ponta esquerda — pra barras que encolhem.</summary>
    public static Sprite QuadradoAncoradoNaEsquerda()
    {
        if (quadradoEsquerda != null)
            return quadradoEsquerda;

        Texture2D textura = Quadrado().texture;
        quadradoEsquerda = Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0f, 0.5f), textura.width);
        quadradoEsquerda.name = "TopDownQuadradoEsquerda";
        return quadradoEsquerda;
    }

    /// <summary>Circulo branco de 1 unidade de diametro, com a borda suavizada.</summary>
    public static Sprite Circulo()
    {
        if (circulo != null)
            return circulo;

        const int lado = 32;
        Texture2D textura = NovaTextura("TopDownCirculo", lado);
        Color32[] pixels = new Color32[lado * lado];
        float raio = lado * 0.5f;

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float dx = x + 0.5f - raio;
                float dy = y + 0.5f - raio;
                float distancia = Mathf.Sqrt(dx * dx + dy * dy);
                byte alfa = (byte)(Mathf.Clamp01(raio - distancia) * 255f);
                pixels[y * lado + x] = new Color32(255, 255, 255, alfa);
            }
        }

        textura.SetPixels32(pixels);
        textura.Apply();

        circulo = Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f), lado);
        circulo.name = "TopDownCirculo";
        return circulo;
    }

    private static Texture2D NovaTextura(string nome, int lado)
    {
        return new Texture2D(lado, lado, TextureFormat.RGBA32, false)
        {
            name = nome,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
    }
}
