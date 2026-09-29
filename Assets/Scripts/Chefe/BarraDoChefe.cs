using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida do chefe, larga, embaixo da tela, com o nome em cima (como no Isaac).
/// Tem um "rastro" claro atras da parte vermelha: o pedaco que acabou de sair demora um
/// pouco pra sumir, entao da pra ver o tamanho de cada golpe.
///
/// Canvas proprio. Monte com <see cref="Mostrar"/>; some sozinha quando o chefe morre.
/// </summary>
[DisallowMultipleComponent]
public class BarraDoChefe : MonoBehaviour
{
    private const float Largura = 900f;
    private const float Altura = 26f;

    [SerializeField, Min(8)] private int tamanhoDaFonte = 24;

    [Tooltip("Velocidade com que o rastro alcanca a vida de verdade (fracao por segundo)")]
    [SerializeField, Min(0.01f)] private float velocidadeDoRastro = 0.6f;

    [Tooltip("Segundos que o rastro espera antes de comecar a descer")]
    [SerializeField, Min(0f)] private float atrasoDoRastro = 0.4f;

    [SerializeField] private Color corDaVida = new Color(0.85f, 0.12f, 0.12f);

    [SerializeField] private Color corDaSegundaFase = new Color(1f, 0.35f, 0.1f);

    private IChefe chefe;
    private CanvasGroup grupo;
    private RectTransform preenchimento;
    private RectTransform rastro;
    private Image imagemDaVida;
    private float fracaoDoRastro = 1f;
    private float momentoDoUltimoGolpe;
    private bool sumindo;

    public static BarraDoChefe Mostrar(IChefe chefe)
    {
        BarraDoChefe barra = new GameObject("Barra do chefe").AddComponent<BarraDoChefe>();
        barra.chefe = chefe;
        barra.Montar();
        return barra;
    }

    private void Montar()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6;

        CanvasScaler escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 1f;

        grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        // Com os pacotes: moldura dourada do Dragon Regalia (ou a inclinada do Pixel UI) e
        // as barras de pixel art do Pixel UI (9-slice, as pontas nao esticam). Sem eles: os
        // retangulos lisos de antes.
        bool comArte = ArteImportada.MolduraDaBarra != null;
        Sprite desenhoDaMoldura = ArteDaInterface.MolduraDaBarra;
        Vector4 borda;

        if (desenhoDaMoldura != null && comArte)
        {
            // Bordas da moldura dourada em pixels da imagem, desenhadas 4x.
            borda = ArteDaInterface.BordaDaBarra * 4f;
        }
        else
        {
            desenhoDaMoldura = comArte ? ArteImportada.MolduraDaBarra : null;
            float margem = comArte ? ArteImportada.MargemDaMoldura * (100f / ArteImportada.PixelsPorUnidadeDaInterface) : 4f;
            borda = Vector4.one * margem;
        }

        float altura = comArte ? 16f : Altura;

        // Moldura centralizada embaixo.
        RectTransform moldura = Retangulo("Moldura", transform, new Color(0f, 0f, 0f, 0.75f), desenhoDaMoldura);
        moldura.anchorMin = moldura.anchorMax = new Vector2(0.5f, 0f);
        moldura.pivot = new Vector2(0.5f, 0f);
        moldura.anchoredPosition = new Vector2(0f, 40f);
        moldura.sizeDelta = new Vector2(Largura + borda.x + borda.z, altura + borda.y + borda.w);

        RectTransform fundo = Retangulo("Fundo", moldura, new Color(0.2f, 0.05f, 0.05f), comArte ? ArteImportada.BarraVazia : null);
        Esticar(fundo, 0f);
        fundo.offsetMin = new Vector2(borda.x, borda.y);
        fundo.offsetMax = new Vector2(-borda.z, -borda.w);

        rastro = Retangulo("Rastro", fundo, new Color(1f, 0.9f, 0.75f), comArte ? ArteImportada.BarraAmarela : null);
        Esticar(rastro, 0f);

        preenchimento = Retangulo("Vida", fundo, corDaVida, comArte ? ArteImportada.BarraVermelha : null);
        Esticar(preenchimento, 0f);
        imagemDaVida = preenchimento.GetComponent<Image>();

        if (comArte)
        {
            // A barra ja e vermelha: a cor so tinge. Fase dois puxa pro laranja.
            corDaVida = Color.white;
            corDaSegundaFase = new Color(1f, 0.7f, 0.35f);
            fundo.GetComponent<Image>().color = Color.white;
            moldura.GetComponent<Image>().color = Color.white;
            rastro.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.85f);
        }

        Font fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (fonte == null)
            fonte = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject objNome = new GameObject("Nome", typeof(RectTransform));
        objNome.transform.SetParent(moldura, false);
        RectTransform rtNome = (RectTransform)objNome.transform;
        rtNome.anchorMin = rtNome.anchorMax = new Vector2(0.5f, 1f);
        rtNome.pivot = new Vector2(0.5f, 0f);
        rtNome.anchoredPosition = new Vector2(0f, 4f);
        rtNome.sizeDelta = new Vector2(Largura, 36f);

        Text nome = objNome.AddComponent<Text>();
        nome.font = fonte;
        nome.fontSize = tamanhoDaFonte;
        nome.alignment = TextAnchor.LowerCenter;
        nome.color = Color.white;
        nome.text = chefe != null ? chefe.Nome : "Chefe";
        nome.raycastTarget = false;
        objNome.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);

        if (chefe != null)
        {
            chefe.Vida.AoMudarVida.AddListener(AoMudarVida);
            chefe.Vida.AoMorrer.AddListener(Sumir);
        }

        Aplicar(preenchimento, Fracao);
        Aplicar(rastro, fracaoDoRastro);
    }

    private void OnDestroy()
    {
        if (Existe)
        {
            chefe.Vida.AoMudarVida.RemoveListener(AoMudarVida);
            chefe.Vida.AoMorrer.RemoveListener(Sumir);
        }
    }

    private float Fracao => Existe ? chefe.Vida.Fracao : 0f;

    /// <summary>
    /// O chefe chega como interface, e o "== null" da Unity (que ve objeto destruido) so
    /// funciona com o tipo Object: sem isto a barra acharia que um chefe destruido existe.
    /// </summary>
    private bool Existe => chefe is Object objeto ? objeto != null : chefe != null;

    private void AoMudarVida()
    {
        momentoDoUltimoGolpe = Time.time;
        Aplicar(preenchimento, Fracao);
    }

    private void Sumir()
    {
        sumindo = true;
        Aplicar(preenchimento, 0f);
    }

    private void Update()
    {
        // Aparece e some com fade.
        float alvo = sumindo ? 0f : 1f;
        grupo.alpha = Mathf.MoveTowards(grupo.alpha, alvo, Time.deltaTime * (sumindo ? 1.2f : 3f));

        if (sumindo && grupo.alpha <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // O chefe foi destruido sem passar pelo AoMorrer (troca de andar, por exemplo).
        if (!Existe && !sumindo)
            Sumir();

        if (Existe && imagemDaVida != null)
            imagemDaVida.color = chefe.NaSegundaFase ? corDaSegundaFase : corDaVida;

        float fracao = sumindo ? 0f : Fracao;

        if (fracaoDoRastro > fracao && Time.time - momentoDoUltimoGolpe >= atrasoDoRastro)
            fracaoDoRastro = Mathf.MoveTowards(fracaoDoRastro, fracao, velocidadeDoRastro * Time.deltaTime);
        else if (fracaoDoRastro < fracao)
            fracaoDoRastro = fracao;

        Aplicar(rastro, fracaoDoRastro);
    }

    // ---------------- ajudas de UI ----------------
    private static void Aplicar(RectTransform barra, float fracao)
    {
        if (barra != null)
            barra.anchorMax = new Vector2(Mathf.Clamp01(fracao), 1f);
    }

    private static RectTransform Retangulo(string nome, Transform pai, Color cor, Sprite sprite = null)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);

        Image imagem = obj.AddComponent<Image>();
        imagem.color = cor;

        if (sprite != null)
        {
            imagem.sprite = sprite;
            imagem.type = Image.Type.Sliced;
        }

        imagem.raycastTarget = false;
        return (RectTransform)obj.transform;
    }

    /// <summary>Ocupa o pai inteiro, com uma margem. Ancorado na esquerda pra crescer pra direita.</summary>
    private static void Esticar(RectTransform rt, float margem)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.offsetMin = new Vector2(margem, margem);
        rt.offsetMax = new Vector2(-margem, -margem);
    }
}
