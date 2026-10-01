using UnityEngine;

/// <summary>
/// Buraco no chao: quem anda nao atravessa, mas tiro e flecha passam por cima (a colisao
/// fica na camada padrao, nao na de parede). Cada ladrilho e um quadrado escuro; a borda
/// clara so aparece nos lados que dao pro chao, entao ladrilhos colados viram um buraco so.
/// </summary>
[DisallowMultipleComponent]
public class Fosso : MonoBehaviour
{
    private static Sprite pixel;

    /// <summary>Um pixel branco de 1x1 unidade, sem borda (o Quadrado do jogo tem borda por ladrilho).</summary>
    public static Sprite Pixel()
    {
        if (pixel != null)
            return pixel;

        Texture2D textura = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "PixelGerado", filterMode = FilterMode.Point };
        textura.SetPixel(0, 0, Color.white);
        textura.Apply();
        pixel = Sprite.Create(textura, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        pixel.name = "PixelGerado";
        return pixel;
    }

    public static Fosso Criar(Transform pai, Vector2 posicaoLocal)
    {
        SpriteRenderer sr = FormasDaSala.Desenho(pai, "Fosso", Pixel(), new Color(0.03f, 0.02f, 0.04f),
            posicaoLocal, Vector2.one * 1.02f, -6);

        // Colisao fora da camada de parede: o tiro voa por cima.
        BoxCollider2D caixa = sr.gameObject.AddComponent<BoxCollider2D>();
        caixa.size = Vector2.one / 1.02f * 0.96f;

        return sr.gameObject.AddComponent<Fosso>();
    }

    /// <summary>Borda do buraco: uma faixa fina por lado que da pra o chao, mais grossa e clara em cima.</summary>
    public static void DesenharBorda(Transform fosso, bool cima, bool baixo, bool esquerda, bool direita, Color cor)
    {
        if (cima)
            Faixa(fosso, new Vector2(0f, 0.43f), new Vector2(1f, 0.14f), cor);

        if (baixo)
            Faixa(fosso, new Vector2(0f, -0.47f), new Vector2(1f, 0.06f), cor * 0.6f);

        if (esquerda)
            Faixa(fosso, new Vector2(-0.47f, 0f), new Vector2(0.06f, 1f), cor * 0.7f);

        if (direita)
            Faixa(fosso, new Vector2(0.47f, 0f), new Vector2(0.06f, 1f), cor * 0.7f);
    }

    private static void Faixa(Transform pai, Vector2 posicao, Vector2 tamanho, Color cor)
    {
        cor.a = 1f;
        FormasDaSala.Desenho(pai, "Borda", Pixel(), cor, posicao, tamanho, -5);
    }
}
