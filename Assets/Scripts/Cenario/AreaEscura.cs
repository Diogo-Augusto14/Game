using UnityEngine;

/// <summary>
/// Um pedaco escuro da caverna (a sala escura do jogo antigo): entrando nele, tudo escurece em volta
/// e o jogador so enxerga um circulo perto dele. Saindo, clareia. Um escuro so, pro andar todo.
/// </summary>
public class AreaEscura : MonoBehaviour
{
    private static Sprite escuro;
    private static AreaEscura dona;

    private Vector2 centro;
    private float raio;
    private Transform jogador;
    private SpriteRenderer sombra;
    private float alfa;

    public static AreaEscura Criar(Vector2 centro, float raio, Transform pai)
    {
        GameObject obj = new GameObject("Area escura");
        obj.transform.SetParent(pai, false);
        AreaEscura a = obj.AddComponent<AreaEscura>();
        a.centro = centro;
        a.raio = raio;
        a.sombra = obj.AddComponent<SpriteRenderer>();
        a.sombra.sprite = Escuro();
        a.sombra.sortingOrder = 35;
        a.sombra.color = new Color(1f, 1f, 1f, 0f);
        obj.transform.localScale = Vector3.one * 60f;
        return a;
    }

    // Preto com um buraco redondo de bordas suaves no meio.
    private static Sprite Escuro()
    {
        if (escuro != null)
            return escuro;

        const int lado = 256;
        Texture2D t = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        Color32[] px = new Color32[lado * lado];

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(lado / 2f, lado / 2f)) / lado;
                float a = Mathf.Clamp01((d - 0.055f) / 0.05f);
                px[y * lado + x] = new Color32(0, 0, 0, (byte)(a * 255));
            }
        }

        t.SetPixels32(px);
        t.Apply();
        escuro = Sprite.Create(t, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), lado);
        return escuro;
    }

    private void Update()
    {
        if (jogador == null)
        {
            GameObject j = GameObject.FindWithTag("Player");
            jogador = j != null ? j.transform : null;

            if (jogador == null)
                return;
        }

        bool dentro = Vector2.Distance(jogador.position, centro) < raio;

        if (dentro && dona != this)
            dona = this;

        if (dona != this && !dentro)
        {
            sombra.color = new Color(1f, 1f, 1f, 0f);
            return;
        }

        alfa = Mathf.MoveTowards(alfa, dentro ? 0.94f : 0f, Time.deltaTime * 1.5f);
        transform.position = jogador.position;
        sombra.color = new Color(1f, 1f, 1f, alfa);

        if (!dentro && alfa <= 0f && dona == this)
            dona = null;
    }

    private void OnDestroy()
    {
        if (dona == this)
            dona = null;
    }
}
