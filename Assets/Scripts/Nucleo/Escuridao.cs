using UnityEngine;

/// <summary>
/// O escuro do andar: um veu preto por cima do mundo inteiro (abaixo so da camada "Brilho" e da interface),
/// com buracos claros onde ha luz (<see cref="FonteDeLuz"/>). Nao depende das luzes 2D do URP.
///
/// O veu e uma textura pequena (4 pontos por celula, filtrada, entao a borda da luz fica macia) que cobre
/// a camera e e refeita a cada quadro: em cada ponto soma a luz ambiente (<see cref="Ambiente"/>) e a de
/// cada fonte perto, que cai do meio pra borda. Onde a soma passa de 1 o veu some; onde e 0, e preto. A
/// cor das fontes tinge um pouco o veu (o fogo deixa a volta quente).
/// </summary>
public class Escuridao : MonoBehaviour
{
    private const int PontosPorUnidade = 4;
    private const int Ordem = 32000;
    // Quanto a cor das luzes tinge o veu.
    private const float Tinta = 0.12f;

    private static float ambiente = 0.12f;
    private static Color tom = Color.white;

    private Camera cam;
    private SpriteRenderer desenho;
    private Texture2D textura;
    private Color32[] pontos;
    private float[] r, g, b;
    private int largura, altura;

    /// <summary>A luz que tem em todo lugar, mesmo longe de tudo (0 = preto, 1 = sem escuro).</summary>
    public static float Ambiente => ambiente;

    /// <summary>Muda a luz de fundo do andar e o tom dela.</summary>
    public static void MudarAmbiente(float intensidade, Color cor)
    {
        ambiente = Mathf.Clamp01(intensidade);
        tom = cor;
    }

    /// <summary>Poe o veu na cena (um so).</summary>
    public static Escuridao Criar()
    {
        Escuridao ja = FindAnyObjectByType<Escuridao>();

        if (ja != null)
            return ja;

        GameObject obj = new GameObject("Escuridao");
        Escuridao e = obj.AddComponent<Escuridao>();
        e.desenho = obj.AddComponent<SpriteRenderer>();
        e.desenho.sortingOrder = Ordem;
        return e;
    }

    private void LateUpdate()
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null || desenho == null)
            return;

        // A area da camera, com uma folga, nos pontos do mundo (presos numa grade: o veu nao treme).
        float meiaAltura = cam.orthographicSize + 1f;
        float meiaLargura = meiaAltura * cam.aspect + 1f;
        int w = Mathf.CeilToInt(meiaLargura * 2f * PontosPorUnidade) + 1;
        int h = Mathf.CeilToInt(meiaAltura * 2f * PontosPorUnidade) + 1;

        if (textura == null || w != largura || h != altura)
            Refazer(w, h);

        Vector3 centro = cam.transform.position;
        float x0 = Mathf.Floor((centro.x - meiaLargura) * PontosPorUnidade) / PontosPorUnidade;
        float y0 = Mathf.Floor((centro.y - meiaAltura) * PontosPorUnidade) / PontosPorUnidade;

        float ar = ambiente * tom.r, ag = ambiente * tom.g, ab = ambiente * tom.b;

        for (int i = 0; i < r.Length; i++)
        {
            r[i] = ar;
            g[i] = ag;
            b[i] = ab;
        }

        foreach (FonteDeLuz luz in FonteDeLuz.Acesas)
            Somar(luz, x0, y0);

        for (int i = 0; i < pontos.Length; i++)
        {
            float lr = Mathf.Min(r[i], 1f), lg = Mathf.Min(g[i], 1f), lb = Mathf.Min(b[i], 1f);
            float menor = Mathf.Min(lr, Mathf.Min(lg, lb));
            float veu = 1f - menor;

            // O veu escurece por igual; a cor que sobra das luzes entra como tinta por cima.
            float k = veu > 0.01f ? Tinta / veu : 0f;
            pontos[i] = new Color32(
                (byte)(Mathf.Clamp01((lr - menor) * k) * 255f),
                (byte)(Mathf.Clamp01((lg - menor) * k) * 255f),
                (byte)(Mathf.Clamp01((lb - menor) * k) * 255f),
                (byte)(veu * 255f));
        }

        textura.SetPixels32(pontos);
        textura.Apply(false);

        // O ponto (0, 0) da textura cobre o canto (x0, y0); o meio do ponto fica no meio da celula dele.
        transform.position = new Vector3(x0 - 0.5f / PontosPorUnidade, y0 - 0.5f / PontosPorUnidade, 0f);
    }

    private void Somar(FonteDeLuz luz, float x0, float y0)
    {
        if (luz == null || luz.intensidade <= 0f || luz.raio <= 0f)
            return;

        Vector2 p = luz.transform.position;
        float raio = luz.raio;
        int xMin = Mathf.Max(0, Mathf.FloorToInt((p.x - raio - x0) * PontosPorUnidade));
        int xMax = Mathf.Min(largura - 1, Mathf.CeilToInt((p.x + raio - x0) * PontosPorUnidade));
        int yMin = Mathf.Max(0, Mathf.FloorToInt((p.y - raio - y0) * PontosPorUnidade));
        int yMax = Mathf.Min(altura - 1, Mathf.CeilToInt((p.y + raio - y0) * PontosPorUnidade));

        if (xMin > xMax || yMin > yMax)
            return;

        float cr = luz.cor.r * luz.intensidade, cg = luz.cor.g * luz.intensidade, cb = luz.cor.b * luz.intensidade;
        float inverso = 1f / (raio * raio);

        for (int y = yMin; y <= yMax; y++)
        {
            float dy = y0 + (float)y / PontosPorUnidade - p.y;

            for (int x = xMin; x <= xMax; x++)
            {
                float dx = x0 + (float)x / PontosPorUnidade - p.x;
                float d2 = (dx * dx + dy * dy) * inverso;

                if (d2 >= 1f)
                    continue;

                // Cai devagar perto do meio e rapido na borda.
                float f = 1f - d2;
                f *= f;
                int i = y * largura + x;
                r[i] += cr * f;
                g[i] += cg * f;
                b[i] += cb * f;
            }
        }
    }

    private void Refazer(int w, int h)
    {
        largura = w;
        altura = h;

        if (textura != null)
            Destroy(textura);

        textura = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = "Escuridao",
        };
        pontos = new Color32[w * h];
        r = new float[w * h];
        g = new float[w * h];
        b = new float[w * h];
        desenho.sprite = Sprite.Create(textura, new Rect(0, 0, w, h), Vector2.zero, PontosPorUnidade, 0, SpriteMeshType.FullRect);
    }
}
