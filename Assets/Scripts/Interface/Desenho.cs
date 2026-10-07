using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ajudas pra desenhar a interface em pixel art (no OnGUI): tudo em multiplos inteiros do tamanho
/// original (<see cref="U"/>), pra os pixels ficarem quadrados e nitidos em qualquer resolucao.
/// </summary>
public static class Desenho
{
    private static readonly Dictionary<(Font, int, TextAnchor), GUIStyle> estilos = new Dictionary<(Font, int, TextAnchor), GUIStyle>();

    /// <summary>Quantos pixels da tela vale 1 pixel da arte: 1 ate 810 de altura, 2 em 1080, 3 em 1440...</summary>
    public static int U => Mathf.Max(1, Mathf.RoundToInt(Screen.height / 540f));

    public static readonly Color Claro = new Color(0.96f, 0.93f, 0.86f);
    public static readonly Color Dourado = new Color(1f, 0.82f, 0.36f);
    public static readonly Color Vermelho = new Color(0.92f, 0.28f, 0.26f);
    public static readonly Color Apagado = new Color(0.6f, 0.58f, 0.66f);
    public static readonly Color Sombra = new Color(0.05f, 0.03f, 0.08f, 0.9f);

    /// <summary>Um retangulo de uma cor so.</summary>
    public static void Cor(Rect onde, Color cor)
    {
        GUI.color = cor;
        GUI.DrawTexture(onde, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    /// <summary>Um sprite inteiro (so a parte dele na folha), esticado no retangulo.</summary>
    public static void Sprite(Rect onde, Sprite sprite, Color cor)
    {
        if (sprite == null)
            return;

        Texture2D t = sprite.texture;
        Rect r = sprite.textureRect;
        GUI.color = cor;
        GUI.DrawTextureWithTexCoords(onde, t, new Rect(r.x / t.width, r.y / t.height, r.width / t.width, r.height / t.height));
        GUI.color = Color.white;
    }

    /// <summary>
    /// Um pedaco de uma imagem: <paramref name="fonte"/> em pixels da imagem, contando de cima pra baixo
    /// (como num editor de imagem).
    /// </summary>
    public static void Parte(Rect onde, Texture2D t, Rect fonte, Color cor)
    {
        if (t == null)
            return;

        GUI.color = cor;
        GUI.DrawTextureWithTexCoords(onde, t, new Rect(fonte.x / t.width, 1f - (fonte.y + fonte.height) / t.height,
                                                        fonte.width / t.width, fonte.height / t.height));
        GUI.color = Color.white;
    }

    public static void Imagem(Rect onde, Texture2D t, Color cor) => Parte(onde, t, new Rect(0f, 0f, t != null ? t.width : 0, t != null ? t.height : 0), cor);

    /// <summary>
    /// Moldura "em 9 partes": os cantos ficam do tamanho certo (vezes <see cref="U"/>) e o resto estica.
    /// <paramref name="fonte"/> e o pedaco da imagem que e a moldura; <paramref name="borda"/>, em pixels
    /// da imagem. Sem o meio, so a borda (o meio a gente pinta de outra cor).
    /// </summary>
    public static void Moldura(Rect onde, Texture2D t, Rect fonte, int borda, Color cor, bool comMeio = true)
    {
        if (t == null)
            return;

        float b = borda * U;
        float[] xs = { onde.x, onde.x + b, onde.x + onde.width - b, onde.x + onde.width };
        float[] ys = { onde.y, onde.y + b, onde.y + onde.height - b, onde.y + onde.height };
        float[] fx = { fonte.x, fonte.x + borda, fonte.x + fonte.width - borda, fonte.x + fonte.width };
        float[] fy = { fonte.y, fonte.y + borda, fonte.y + fonte.height - borda, fonte.y + fonte.height };

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (!comMeio && i == 1 && j == 1)
                    continue;

                Parte(new Rect(xs[i], ys[j], xs[i + 1] - xs[i], ys[j + 1] - ys[j]), t,
                      new Rect(fx[i], fy[j], fx[i + 1] - fx[i], fy[j + 1] - fy[j]), cor);
            }
        }
    }

    public static GUIStyle Estilo(Font fonte, int tamanho, TextAnchor alinhamento)
    {
        var chave = (fonte, tamanho, alinhamento);

        if (!estilos.TryGetValue(chave, out GUIStyle estilo) || estilo == null)
        {
            estilo = new GUIStyle(GUI.skin.label) { font = fonte, fontSize = tamanho, alignment = alinhamento, wordWrap = false };
            estilo.normal.textColor = Color.white;
            estilos[chave] = estilo;
        }

        return estilo;
    }

    /// <summary>Texto com uma sombra escura embaixo (le em cima de qualquer chao).</summary>
    public static void Texto(Rect onde, string texto, GUIStyle estilo, Color cor)
    {
        float s = U;
        GUI.color = new Color(Sombra.r, Sombra.g, Sombra.b, Sombra.a * cor.a);
        GUI.Label(new Rect(onde.x + s, onde.y + s, onde.width, onde.height), texto, estilo);
        GUI.color = cor;
        GUI.Label(onde, texto, estilo);
        GUI.color = Color.white;
    }

    /// <summary>A largura do texto, pra encaixar coisas do lado.</summary>
    public static float Largura(string texto, GUIStyle estilo) => estilo.CalcSize(new GUIContent(texto)).x;
}
