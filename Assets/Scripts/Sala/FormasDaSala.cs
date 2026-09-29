using UnityEngine;

/// <summary>
/// Sprites simples gerados em memoria (quadrado e circulo) pra a sala e os inimigos
/// aparecerem sem nenhuma arte pronta. Troque pelo seu desenho quando tiver: todo mundo
/// aqui so pede um Sprite, entao basta arrastar outro no SpriteRenderer.
/// </summary>
public static class FormasDaSala
{
    private const int LadoDoCirculo = 32;

    private static Sprite circulo;

    /// <summary>Quadrado 1x1 unidade (o mesmo bloco do resto do jogo).</summary>
    public static Sprite Quadrado() => Construtor.SpriteDeBloco();

    /// <summary>Circulo cheio de 1 unidade de diametro, com borda escurecida.</summary>
    public static Sprite Circulo()
    {
        if (circulo != null)
            return circulo;

        Texture2D textura = new Texture2D(LadoDoCirculo, LadoDoCirculo, TextureFormat.RGBA32, false)
        {
            name = "CirculoGerado",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[LadoDoCirculo * LadoDoCirculo];
        float raio = LadoDoCirculo * 0.5f;

        for (int y = 0; y < LadoDoCirculo; y++)
        {
            for (int x = 0; x < LadoDoCirculo; x++)
            {
                float dx = x + 0.5f - raio;
                float dy = y + 0.5f - raio;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                byte tom = d > raio - 2f ? (byte)170 : (byte)255;
                pixels[y * LadoDoCirculo + x] = d <= raio ? new Color32(tom, tom, tom, 255) : new Color32(0, 0, 0, 0);
            }
        }

        textura.SetPixels32(pixels);
        textura.Apply();

        circulo = Sprite.Create(
            textura, new Rect(0f, 0f, LadoDoCirculo, LadoDoCirculo), new Vector2(0.5f, 0.5f),
            LadoDoCirculo, 0, SpriteMeshType.FullRect);

        circulo.name = "CirculoGerado";
        return circulo;
    }

    /// <summary>Como <see cref="Desenho"/>, mas o sprite repete (modo Tiled) pra cobrir o tamanho.</summary>
    public static SpriteRenderer DesenhoLadrilhado(Transform pai, string nome, Sprite sprite, Color cor,
                                                   Vector2 posicaoLocal, Vector2 tamanho, int ordem)
    {
        SpriteRenderer sr = Desenho(pai, nome, sprite, cor, posicaoLocal, Vector2.one, ordem);
        sr.transform.localScale = Vector3.one;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = tamanho;
        return sr;
    }

    /// <summary>Cria um filho so com desenho (sem colisao).</summary>
    public static SpriteRenderer Desenho(Transform pai, string nome, Sprite sprite, Color cor,
                                         Vector2 posicaoLocal, Vector2 tamanho, int ordem)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(pai, false);
        obj.transform.localPosition = posicaoLocal;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = cor;
        sr.sortingOrder = ordem;

        // Modo Tiled: o quadradinho repete em vez de esticar (fica com cara de ladrilho).
        if (sprite == Quadrado())
        {
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = tamanho;
        }
        else
        {
            obj.transform.localScale = new Vector3(tamanho.x, tamanho.y, 1f);
        }

        return sr;
    }
}
