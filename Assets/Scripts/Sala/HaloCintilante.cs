using UnityEngine;

/// <summary>
/// Brilho macio de vela ou tocha: um disco de luz que treme de leve e muda de tamanho.
/// Fica atras dos bichos (so desenho, sem colisao).
/// </summary>
public class HaloCintilante : MonoBehaviour
{
    private static Sprite suave;

    private SpriteRenderer desenho;
    private Color corBase;
    private float tamanhoBase;
    private float fase;

    /// <summary>Disco com a luz caindo de dentro pra fora (64 px, 1 unidade de diametro).</summary>
    public static Sprite Suave()
    {
        if (suave != null)
            return suave;

        const int lado = 64;
        Texture2D textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false)
        {
            name = "HaloGerado",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[lado * lado];
        float raio = lado * 0.5f;

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(raio, raio)) / raio;
                float a = Mathf.Clamp01(1f - d);
                pixels[y * lado + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
            }
        }

        textura.SetPixels32(pixels);
        textura.Apply();
        suave = Sprite.Create(textura, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), lado, 0, SpriteMeshType.FullRect);
        suave.name = "HaloGerado";
        return suave;
    }

    public static HaloCintilante Criar(Transform pai, Vector2 posicaoLocal, float diametro, Color cor)
    {
        SpriteRenderer sr = FormasDaSala.Desenho(pai, "Halo", Suave(), cor, posicaoLocal, Vector2.one * diametro, -7);
        HaloCintilante halo = sr.gameObject.AddComponent<HaloCintilante>();
        halo.desenho = sr;
        halo.corBase = cor;
        halo.tamanhoBase = diametro;
        halo.fase = Random.value * 10f;
        return halo;
    }

    private void Update()
    {
        float t = Time.time * 7f + fase;
        float tremor = 0.5f + 0.25f * Mathf.Sin(t) + 0.25f * Mathf.Sin(t * 2.3f + 1f);
        float tamanho = tamanhoBase * (0.93f + 0.1f * tremor);
        transform.localScale = new Vector3(tamanho, tamanho, 1f);

        Color cor = corBase;
        cor.a *= 0.8f + 0.4f * tremor;
        desenho.color = cor;
    }
}
