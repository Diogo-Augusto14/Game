using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD montada por codigo: vida (coracoes ou barra) e a lista de controles.
///
/// Por que por codigo e nao por prefab: o Andar precisa conseguir montar o jogo
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

    [Header("Aparencia")]
    [SerializeField] private Vector2 tamanhoDaBarra = new Vector2(320f, 22f);

    [SerializeField] private Vector2 margem = new Vector2(24f, 20f);

    [SerializeField] private Color corDaVida = new Color(0.85f, 0.2f, 0.25f);

    [SerializeField] private Color corDoFundo = new Color(0f, 0f, 0f, 0.55f);

    [Header("Controles na tela")]
    [Tooltip("Mostra a lista de teclas no canto. Desligue quando o jogo tiver menu")]
    [SerializeField] private bool mostrarControles = true;

    [SerializeField, Min(8)] private int tamanhoDaFonte = 20;

    [Header("Coracoes (top-down)")]
    [Tooltip("Vida em coracoes do Pixel UI pack, como no Isaac, em vez da barra")]
    [SerializeField] private bool usarCoracoes;

    [Tooltip("Quanto de vida vale um coracao inteiro (meio coracao = metade)")]
    [SerializeField, Min(1f)] private float vidaPorCoracao = 20f;

    [SerializeField, Min(1)] private int coracoesPorLinha = 6;

    // ---------------- estado ----------------
    private Image preenchimentoDaVida;
    private readonly System.Collections.Generic.List<Image> coracoes = new System.Collections.Generic.List<Image>();
    private RectTransform raizDosCoracoes;
    private int metadesDesenhadas = -1;
    private int coracoesDesenhados = -1;

    /// <summary>Altura que os coracoes ocupam na tela (cada um tem 12 px desenhados 4x).</summary>
    public const float LadoDoCoracao = 48f;

    /// <summary>Espaco entre dois coracoes.</summary>
    public const float EspacoDosCoracoes = 4f;

    /// <summary>
    /// Linhas de coracoes que cabem na area da vida. Passou disso, os coracoes encolhem em
    /// vez de abrir uma linha nova: a area nunca cresce e nao empurra o que vem embaixo.
    /// </summary>
    public const int LinhasDeCoracoes = 2;

    /// <summary>Canto de cima a esquerda da area da vida (a mesma margem padrao da HUD).</summary>
    public static readonly Vector2 CantoDaVida = new Vector2(24f, 20f);

    /// <summary>
    /// Altura FIXA reservada pra vida, cheia ou nao, com 1 ou 20 coracoes. Quem desenha
    /// embaixo dela (moedas, chaves, bombas) se posiciona por aqui, nunca pelos coracoes.
    /// </summary>
    public const float AlturaDaVida = LinhasDeCoracoes * (LadoDoCoracao + EspacoDosCoracoes);

    // Quem monta a HUD por codigo pode trocar a lista (o top-down tem outras teclas).
    private string textoDosControles = TEXTO_DOS_CONTROLES;

    // Teclas entre colchetes viram o desenho da tecla (ver TelaSimples.LinhaDeTeclas).
    private const string TEXTO_DOS_CONTROLES =
        "[W][A][S][D] andar | [Cima][Esquerda][Baixo][Direita] atirar";

    /// <summary>
    /// Aponta a HUD pra uma vida. Chame logo depois do AddComponent, antes do Start.
    /// </summary>
    public void Configurar(Vida alvo, string controles = null)
    {
        vida = alvo;

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
        if (vida != null)
            return;

        GameObject jogador = GameObject.FindWithTag("Player");

        if (jogador != null)
            vida = jogador.GetComponent<Vida>();
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
    }

    private void MontarRaizDosCoracoes()
    {
        // Area de tamanho fixo: o que muda dentro dela (quantos coracoes, de que tamanho)
        // nunca mexe no resto da HUD.
        raizDosCoracoes = CriarPainel("Coracoes", Color.clear);
        Ancorar(raizDosCoracoes, new Vector2(0f, 1f), new Vector2(margem.x, -margem.y),
            new Vector2(LarguraDosCoracoes, AlturaDaVida));
    }

    private float LarguraDosCoracoes => coracoesPorLinha * (LadoDoCoracao + EspacoDosCoracoes);

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
            RectTransform rt = CriarPainel($"Coracao {coracoes.Count}", Color.white, raizDosCoracoes);
            coracoes.Add(rt.GetComponent<Image>());
        }

        if (total != coracoesDesenhados)
            ArrumarCoracoes(total);

        for (int i = 0; i < coracoes.Count; i++)
        {
            coracoes[i].gameObject.SetActive(i < total);

            int cheio = metades - i * 2;
            coracoes[i].sprite = cheio >= 2 ? ArteImportada.CoracaoCheio
                               : cheio == 1 ? ArteImportada.CoracaoMeio
                               : ArteImportada.CoracaoVazio;

            // O meio coracao do pacote tem o vermelho na DIREITA: a fileira parecia
            // esvaziar ao contrario (buraco cinza antes do vermelho). Espelhado, o vermelho
            // fica na esquerda e a vida some da direita pra esquerda, como deve.
            coracoes[i].rectTransform.localScale = new Vector3(cheio == 1 ? -1f : 1f, 1f, 1f);
        }

        coracoesDesenhados = total;
        metadesDesenhadas = metades;
    }

    /// <summary>
    /// Poe cada coracao no lugar. Ate <see cref="LinhasDeCoracoes"/> linhas de
    /// <see cref="coracoesPorLinha"/> ficam no tamanho normal; com mais vida que isso, a
    /// linha fica mais comprida e os coracoes menores, sempre dentro da mesma area.
    /// </summary>
    private void ArrumarCoracoes(int total)
    {
        int porLinha = Mathf.Max(coracoesPorLinha, Mathf.CeilToInt(total / (float)LinhasDeCoracoes));
        float passo = Mathf.Min(LadoDoCoracao + EspacoDosCoracoes, LarguraDosCoracoes / porLinha);
        float lado = passo - EspacoDosCoracoes;

        for (int i = 0; i < coracoes.Count; i++)
        {
            // Pivo no meio: o meio coracao espelha no proprio lugar, sem pular pro lado.
            RectTransform rt = coracoes[i].rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.one * lado;
            rt.anchoredPosition = new Vector2((i % porLinha) * passo + lado * 0.5f, -(i / porLinha) * passo - lado * 0.5f);
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

        // Uma linha no canto de baixo, com o desenho das teclas do pacote Controllers and Keyboard.
        RectTransform linha = TelaSimples.LinhaDeTeclas(transform, "Controles", 0f, textoDosControles,
            tamanhoDaFonte, new Color(1f, 1f, 1f, 0.85f));
        linha.anchorMin = linha.anchorMax = Vector2.zero;
        linha.pivot = Vector2.zero;
        linha.anchoredPosition = margem;
    }

    private static Font FonteEmbutida()
    {
        return FonteDoJogo.Texto;
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
