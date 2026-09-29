using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD montada por codigo: barra de vida, frascos de cura e a lista de controles.
///
/// Por que por codigo e nao por prefab: o Bootstrap precisa conseguir montar o jogo
/// inteiro sem nenhum asset preparado a mao. Uma HUD de prefab quebraria o "so dar play"
/// na primeira vez que alguem clonasse o projeto sem a pasta de prefabs.
///
/// A barra usa Image do tipo Filled: mudar fillAmount e uma atribuicao, sem recalcular
/// layout — da pra atualizar todo quadro sem custo nenhum.
/// </summary>
[DisallowMultipleComponent]
public class Hud : MonoBehaviour
{
    [Header("Alvo")]
    [Tooltip("Vazio = usa o jogador da cena")]
    [SerializeField] private Vida vida;

    [SerializeField] private Cura cura;

    [Header("Aparencia")]
    [SerializeField] private Vector2 tamanhoDaBarra = new Vector2(320f, 22f);

    [SerializeField] private Vector2 margem = new Vector2(24f, 20f);

    [SerializeField] private Color corDaVida = new Color(0.85f, 0.2f, 0.25f);

    [SerializeField] private Color corDoFundo = new Color(0f, 0f, 0f, 0.55f);

    [SerializeField] private Color corDoFrascoCheio = new Color(0.35f, 0.85f, 1f);

    [SerializeField] private Color corDoFrascoVazio = new Color(1f, 1f, 1f, 0.18f);

    [Header("Controles na tela")]
    [Tooltip("Mostra a lista de teclas no canto. Desligue quando o jogo tiver menu")]
    [SerializeField] private bool mostrarControles = true;

    [SerializeField, Min(8)] private int tamanhoDaFonte = 16;

    [Header("Coracoes (top-down)")]
    [Tooltip("Vida em coracoes do Pixel UI pack, como no Isaac, em vez da barra")]
    [SerializeField] private bool usarCoracoes;

    [Tooltip("Quanto de vida vale um coracao inteiro (meio coracao = metade)")]
    [SerializeField, Min(1f)] private float vidaPorCoracao = 20f;

    [SerializeField, Min(1)] private int coracoesPorLinha = 6;

    // ---------------- estado ----------------
    private Image preenchimentoDaVida;
    private Image preenchimentoDaCura;
    private Image[] frascos;
    private RectTransform raizDosFrascos;
    private int frascosDesenhados = -1;
    private readonly System.Collections.Generic.List<Image> coracoes = new System.Collections.Generic.List<Image>();
    private RectTransform raizDosCoracoes;
    private int metadesDesenhadas = -1;
    private int coracoesDesenhados = -1;

    /// <summary>Altura que os coracoes ocupam na tela (cada um tem 12 px desenhados 4x).</summary>
    public const float LadoDoCoracao = 48f;

    // Quem monta a HUD por codigo pode trocar a lista (o top-down tem outras teclas).
    private string textoDosControles = TEXTO_DOS_CONTROLES;

    private const string TEXTO_DOS_CONTROLES =
        "A / D  andar      Ctrl  devagar\n" +
        "Espaco  pular (2x)   no ar + parede = wall jump\n" +
        "Shift  dash    Shift + Baixo  escorregar    Shift + tras  esquiva\n" +
        "J / Mouse  golpe (combo 1-2-3)    no ar + Baixo  mergulho\n" +
        "Baixo  agachar    Cima  escada / subir beirada    E (segurar)  curar";

    /// <summary>
    /// Aponta a HUD pra uma vida (e frascos, se houver) sem depender do Player do
    /// plataforma. Chame logo depois do AddComponent, antes do Start.
    /// </summary>
    public void Configurar(Vida alvo, Cura frascos = null, string controles = null)
    {
        vida = alvo;
        cura = frascos;

        if (controles != null)
            textoDosControles = controles;
    }

    /// <summary>Mostra a vida em coracoes em vez da barra. Chame antes do Start.</summary>
    public void UsarCoracoes(float vidaDeUmCoracao = 20f)
    {
        usarCoracoes = true;
        vidaPorCoracao = Mathf.Max(1f, vidaDeUmCoracao);
    }

    // ---------------- ciclo de vida ----------------
    private void Start()
    {
        GarantirAlvo();
        Montar();
    }

    private void GarantirAlvo()
    {
        if (vida == null && Player.Atual != null)
            vida = Player.Atual.Saude;

        if (cura == null && Player.Atual != null)
            cura = Player.Atual.Frascos;
    }

    private void Update()
    {
        if (vida == null)
        {
            GarantirAlvo();

            if (vida == null)
                return;
        }

        if (preenchimentoDaVida != null)
            preenchimentoDaVida.fillAmount = vida.Fracao;

        AtualizarCoracoes();

        AtualizarFrascos();
    }

    private void AtualizarFrascos()
    {
        if (cura == null || raizDosFrascos == null)
            return;

        if (frascosDesenhados != cura.FrascosMaximos)
            MontarFrascos(cura.FrascosMaximos);

        for (int i = 0; i < frascos.Length; i++)
            frascos[i].color = i < cura.Frascos ? corDoFrascoCheio : corDoFrascoVazio;

        if (preenchimentoDaCura != null)
            preenchimentoDaCura.fillAmount = cura.Curando ? cura.Progresso : 0f;
    }

    // ---------------- montagem ----------------
    private void Montar()
    {
        Canvas canvas = GetComponent<Canvas>();

        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        CanvasScaler escala = GetComponent<CanvasScaler>();

        if (escala == null)
        {
            escala = gameObject.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920f, 1080f);
            escala.matchWidthOrHeight = 1f;
        }

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        // Sem as imagens do pacote, os coracoes voltam a ser a barra.
        if (usarCoracoes && ArteImportada.CoracaoCheio != null)
            MontarRaizDosCoracoes();
        else
            MontarBarraDeVida();

        MontarRaizDosFrascos();

        if (mostrarControles)
            MontarControles();
    }

    private void MontarBarraDeVida()
    {
        RectTransform fundo = CriarPainel("Vida (fundo)", corDoFundo);
        Ancorar(fundo, new Vector2(0f, 1f), new Vector2(margem.x, -margem.y), tamanhoDaBarra);

        RectTransform barra = CriarPainel("Vida", corDaVida, fundo);
        Esticar(barra, 3f);

        preenchimentoDaVida = barra.GetComponent<Image>();
        preenchimentoDaVida.type = Image.Type.Filled;
        preenchimentoDaVida.fillMethod = Image.FillMethod.Horizontal;
        preenchimentoDaVida.fillOrigin = (int)Image.OriginHorizontal.Left;
        preenchimentoDaVida.fillAmount = 1f;

        // Barrinha fina de progresso da cura, embaixo da vida.
        RectTransform fundoCura = CriarPainel("Cura (fundo)", corDoFundo);
        Ancorar(fundoCura, new Vector2(0f, 1f),
            new Vector2(margem.x, -(margem.y + tamanhoDaBarra.y + 4f)),
            new Vector2(tamanhoDaBarra.x, 6f));

        RectTransform barraCura = CriarPainel("Cura", corDoFrascoCheio, fundoCura);
        Esticar(barraCura, 1f);

        preenchimentoDaCura = barraCura.GetComponent<Image>();
        preenchimentoDaCura.type = Image.Type.Filled;
        preenchimentoDaCura.fillMethod = Image.FillMethod.Horizontal;
        preenchimentoDaCura.fillAmount = 0f;
    }

    private void MontarRaizDosCoracoes()
    {
        raizDosCoracoes = CriarPainel("Coracoes", Color.clear);
        Ancorar(raizDosCoracoes, new Vector2(0f, 1f), new Vector2(margem.x, -margem.y),
            new Vector2(coracoesPorLinha * (LadoDoCoracao + 4f), LadoDoCoracao));
    }

    /// <summary>
    /// Um coracao a cada <see cref="vidaPorCoracao"/> de vida maxima. A vida atual e
    /// arredondada pra cima em meios coracoes: com qualquer resto de vida, sobra meio.
    /// So refaz as imagens quando a conta muda.
    /// </summary>
    private void AtualizarCoracoes()
    {
        if (raizDosCoracoes == null)
            return;

        int total = Mathf.Max(1, Mathf.CeilToInt(vida.VidaMaxima / vidaPorCoracao - 0.001f));
        int metades = Mathf.Clamp(Mathf.CeilToInt(vida.VidaAtual / (vidaPorCoracao * 0.5f) - 0.001f), 0, total * 2);

        if (total == coracoesDesenhados && metades == metadesDesenhadas)
            return;

        while (coracoes.Count < total)
        {
            int i = coracoes.Count;
            RectTransform rt = CriarPainel($"Coracao {i}", Color.white, raizDosCoracoes);
            Ancorar(rt, new Vector2(0f, 1f),
                new Vector2((i % coracoesPorLinha) * (LadoDoCoracao + 4f), -(i / coracoesPorLinha) * (LadoDoCoracao + 4f)),
                Vector2.one * LadoDoCoracao);
            coracoes.Add(rt.GetComponent<Image>());
        }

        for (int i = 0; i < coracoes.Count; i++)
        {
            coracoes[i].gameObject.SetActive(i < total);

            int cheio = metades - i * 2;
            coracoes[i].sprite = cheio >= 2 ? ArteImportada.CoracaoCheio
                               : cheio == 1 ? ArteImportada.CoracaoMeio
                               : ArteImportada.CoracaoVazio;
        }

        coracoesDesenhados = total;
        metadesDesenhadas = metades;
    }

    private void MontarRaizDosFrascos()
    {
        raizDosFrascos = CriarPainel("Frascos", Color.clear);
        Ancorar(raizDosFrascos, new Vector2(0f, 1f),
            new Vector2(margem.x, -(margem.y + tamanhoDaBarra.y + 16f)),
            new Vector2(tamanhoDaBarra.x, 18f));
    }

    private void MontarFrascos(int quantidade)
    {
        // Limpa o que tinha antes: a quantidade de frascos pode mudar no meio do jogo.
        for (int i = raizDosFrascos.childCount - 1; i >= 0; i--)
            Destroy(raizDosFrascos.GetChild(i).gameObject);

        frascos = new Image[Mathf.Max(0, quantidade)];
        frascosDesenhados = quantidade;

        const float lado = 16f;
        const float espaco = 6f;

        for (int i = 0; i < frascos.Length; i++)
        {
            RectTransform pip = CriarPainel($"Frasco {i}", corDoFrascoVazio, raizDosFrascos);
            Ancorar(pip, new Vector2(0f, 0.5f), new Vector2(i * (lado + espaco), 0f), new Vector2(lado, lado));
            frascos[i] = pip.GetComponent<Image>();
        }
    }

    private void MontarControles()
    {
        Font fonte = FonteEmbutida();

        if (fonte == null)
        {
            // Sem fonte nao da pra desenhar texto; o Console ainda informa as teclas.
            Debug.Log("[Hud] controles:\n" + textoDosControles);
            return;
        }

        RectTransform fundo = CriarPainel("Controles (fundo)", new Color(0f, 0f, 0f, 0.4f));
        Ancorar(fundo, new Vector2(0f, 0f), new Vector2(margem.x, margem.y), new Vector2(620f, 120f));

        GameObject obj = new GameObject("Controles", typeof(RectTransform));
        obj.transform.SetParent(fundo, false);

        Text texto = obj.AddComponent<Text>();
        texto.font = fonte;
        texto.fontSize = tamanhoDaFonte;
        texto.color = new Color(1f, 1f, 1f, 0.85f);
        texto.alignment = TextAnchor.LowerLeft;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        texto.text = textoDosControles;

        Esticar((RectTransform)obj.transform, 8f);
    }

    private static Font FonteEmbutida()
    {
        // O nome da fonte embutida mudou entre versoes da Unity: tenta as duas.
        Font fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (fonte == null)
            fonte = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return fonte;
    }

    // ---------------- helpers de UI ----------------
    private RectTransform CriarPainel(string nome, Color cor, Transform pai = null)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai != null ? pai : transform, false);

        Image img = obj.AddComponent<Image>();
        img.sprite = SpriteBranco();
        img.color = cor;
        img.raycastTarget = false;

        return (RectTransform)obj.transform;
    }

    // Um quadradinho branco serve de sprite pra tudo: a cor vem do Image.color.
    // Sem sprite, o Image ignora o modo Filled e a barra de vida nunca diminuiria.
    private static Sprite spriteBranco;

    private static Sprite SpriteBranco()
    {
        if (spriteBranco != null)
            return spriteBranco;

        Texture2D textura = new Texture2D(4, 4, TextureFormat.RGBA32, false)
        {
            name = "HudBranco",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[16];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        textura.SetPixels(pixels);
        textura.Apply();

        spriteBranco = Sprite.Create(textura, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        spriteBranco.name = "HudBranco";

        return spriteBranco;
    }

    private static void Ancorar(RectTransform alvo, Vector2 ancora, Vector2 posicao, Vector2 tamanho)
    {
        alvo.anchorMin = ancora;
        alvo.anchorMax = ancora;
        alvo.pivot = new Vector2(0f, ancora.y);
        alvo.anchoredPosition = posicao;
        alvo.sizeDelta = tamanho;
    }

    private static void Esticar(RectTransform alvo, float margemInterna)
    {
        alvo.anchorMin = Vector2.zero;
        alvo.anchorMax = Vector2.one;
        alvo.pivot = new Vector2(0.5f, 0.5f);
        alvo.offsetMin = new Vector2(margemInterna, margemInterna);
        alvo.offsetMax = new Vector2(-margemInterna, -margemInterna);
    }
}
