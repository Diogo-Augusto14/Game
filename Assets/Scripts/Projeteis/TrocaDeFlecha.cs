using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// As flechas que o jogador ja tem e qual esta em uso. Cada item de flecha
/// (<see cref="ItemPassivo.flecha"/>) fica pra sempre na aljava: pegar um novo ja troca pra
/// ele, e Q (ou LT no controle) passa pra proxima, voltando na normal no fim da volta.
///
/// Mostra no canto de baixo, a direita, o desenho e o nome da flecha em uso. So aparece
/// depois da primeira flecha especial. Fica no jogador; o <see cref="EstatisticasDoJogador"/>
/// poe na hora de dar o item.
/// </summary>
[DisallowMultipleComponent]
public class TrocaDeFlecha : MonoBehaviour
{
    [SerializeField] private KeyCode teclaTrocar = KeyCode.Q;

    private readonly List<TipoDeFlecha> aljava = new List<TipoDeFlecha> { TipoDeFlecha.Normal };
    private int emUso;
    private AtiradorTopDown atirador;
    private ArsenalDoJogador arsenal;
    private bool painelEscondido;

    // ---------------- HUD ----------------
    private GameObject hud;
    private RectTransform painel;
    private Image icone;
    private Text nome;
    private Text contagem;
    private float momentoDaTroca = -10f;

    private const float DuracaoDoDestaque = 0.35f;

    /// <summary>A flecha em uso agora.</summary>
    public TipoDeFlecha Atual => aljava[emUso];

    /// <summary>O lugar da flecha em uso na aljava (0 = a normal).</summary>
    public int Indice => emUso;

    public IReadOnlyList<TipoDeFlecha> Aljava => aljava;

    /// <summary>Trocou de flecha (pegou uma nova ou apertou Q/LT).</summary>
    public event System.Action<TipoDeFlecha> AoTrocar;

    public static TrocaDeFlecha Em(GameObject jogador)
    {
        TrocaDeFlecha troca = jogador.GetComponent<TrocaDeFlecha>();
        return troca != null ? troca : jogador.AddComponent<TrocaDeFlecha>();
    }

    private void Awake()
    {
        atirador = GetComponent<AtiradorTopDown>();
        arsenal = GetComponent<ArsenalDoJogador>();
    }

    /// <summary>Volta pra flecha normal (o arsenal chama ao voltar pra arma do heroi).</summary>
    public void VoltarParaNormal()
    {
        if (emUso != 0)
            Usar(0);
    }

    /// <summary>
    /// Esconde o painel das flechas enquanto uma arma de fogo esta na mao (as flechas so valem pra
    /// arma do heroi). Volta a aparecer com <paramref name="esconder"/> falso.
    /// </summary>
    public void EsconderPainel(bool esconder)
    {
        painelEscondido = esconder;
        AtualizarHud();
    }

    private void OnDestroy()
    {
        if (hud != null)
            Destroy(hud);
    }

    /// <summary>Guarda a flecha na aljava (se ainda nao tinha) e ja passa a usar ela.</summary>
    public void Ganhar(TipoDeFlecha tipo)
    {
        int onde = aljava.IndexOf(tipo);

        if (onde < 0)
        {
            aljava.Add(tipo);
            onde = aljava.Count - 1;
        }

        Usar(onde);
    }

    /// <summary>Passa pra proxima flecha da aljava.</summary>
    public void Proxima()
    {
        if (aljava.Count < 2)
            return;

        Usar((emUso + 1) % aljava.Count);
        Sons.Tocar(Som.Menu, 0.6f);
    }

    private void Usar(int indice)
    {
        emUso = indice;

        if (atirador == null)
            atirador = GetComponent<AtiradorTopDown>();

        if (atirador != null)
            atirador.DefinirTipoDeFlecha(Atual);

        momentoDaTroca = Time.unscaledTime;
        AtualizarHud();
        AoTrocar?.Invoke(Atual);
    }

    private void Update()
    {
        // Com o jogo parado (pausa, menu), Q e LT sao dos menus. Com arsenal no jogador, o Q e dele
        // (passa pelas flechas e depois pelas armas); sem ele, troca so de flecha.
        if (arsenal == null && Time.timeScale > 0f && aljava.Count > 1
            && (Input.GetKeyDown(teclaTrocar) || Controle.Apertou(BotaoDoControle.LT)))
            Proxima();

        if (painel != null)
        {
            // Destaque rapido ao trocar: o quadro cresce e volta.
            float t = (Time.unscaledTime - momentoDaTroca) / DuracaoDoDestaque;
            float escala = t < 1f ? 1f + Mathf.Sin(t * Mathf.PI) * 0.15f : 1f;
            painel.localScale = Vector3.one * escala;

            // Com o painel das armas embaixo, este sobe pra nao ficar por cima dele.
            bool acima = arsenal != null && arsenal.ComArmasDeFogo;
            painel.anchoredPosition = new Vector2(-24f, acima ? 204f : 24f);
        }
    }

    // ---------------- HUD ----------------
    private void AtualizarHud()
    {
        if (aljava.Count < 2 || painelEscondido)
        {
            if (hud != null)
                hud.SetActive(false);

            return;
        }

        if (hud == null)
            MontarHud();

        hud.SetActive(true);

        DefinicaoDeFlecha d = CatalogoDeFlechas.De(Atual);
        Sprite desenho = CatalogoDeFlechas.Icone(Atual);
        icone.sprite = desenho;
        icone.enabled = desenho != null;
        icone.color = d.aparencia != null ? d.aparencia.cor : Color.white;
        nome.text = d.nome;
        nome.color = d.cor;
        contagem.text = $"{emUso + 1}/{aljava.Count}";
    }

    private void MontarHud()
    {
        hud = new GameObject("HUD da flecha", typeof(RectTransform));

        Canvas canvas = hud.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;

        CanvasScaler escala = hud.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 1f;

        // Painel no canto de baixo a DIREITA: desenho da flecha, nome e a tecla de trocar. O canto
        // de baixo a esquerda e da linha de teclas da HUD, o meio de baixo e da barra do chefe e
        // a lateral direita, de cima pra baixo, e do minimapa e dos itens. Com arma de fogo na mao
        // o painel some; o das armas (HudDasArmas) so aparece depois da primeira e fica embaixo do
        // das flechas, que sobe pra dar lugar.
        GameObject objPainel = new GameObject("Painel", typeof(RectTransform));
        objPainel.transform.SetParent(hud.transform, false);
        painel = (RectTransform)objPainel.transform;
        painel.anchorMin = painel.anchorMax = new Vector2(1f, 0f);
        painel.pivot = new Vector2(1f, 0f);
        painel.anchoredPosition = new Vector2(-24f, 24f);
        painel.sizeDelta = new Vector2(340f, 96f);

        Image fundo = objPainel.AddComponent<Image>();
        fundo.sprite = ArteImportada.PainelMarrom;
        fundo.type = Image.Type.Sliced;
        fundo.color = fundo.sprite != null ? new Color(1f, 1f, 1f, 0.92f) : new Color(0f, 0f, 0f, 0.6f);
        fundo.raycastTarget = false;

        GameObject objIcone = new GameObject("Icone", typeof(RectTransform));
        objIcone.transform.SetParent(painel, false);
        RectTransform rtIcone = (RectTransform)objIcone.transform;
        rtIcone.anchorMin = rtIcone.anchorMax = new Vector2(0f, 0.5f);
        rtIcone.pivot = new Vector2(0.5f, 0.5f);
        rtIcone.anchoredPosition = new Vector2(52f, 0f);
        rtIcone.sizeDelta = new Vector2(64f, 64f);
        icone = objIcone.AddComponent<Image>();
        icone.preserveAspect = true;
        icone.raycastTarget = false;

        Font fonte = TelaDeFimDeJogo.Fonte();
        nome = Texto("Nome", fonte, 26, new Vector2(96f, 16f), new Vector2(230f, 40f), TextAnchor.MiddleLeft);
        contagem = Texto("Contagem", fonte, 18, new Vector2(96f, -24f), new Vector2(60f, 30f), TextAnchor.MiddleLeft);
        contagem.color = new Color(1f, 1f, 1f, 0.75f);

        RectTransform dica = TelaSimples.LinhaDeTeclas(painel, "Dica", -24f, "[Q] trocar || [Pad LT] trocar", 18,
                                                      new Color(1f, 1f, 1f, 0.85f));
        dica.anchorMin = dica.anchorMax = new Vector2(0f, 0.5f);
        dica.pivot = new Vector2(0f, 0.5f);
        dica.anchoredPosition = new Vector2(160f, -24f);
    }

    private Text Texto(string nomeDoObjeto, Font fonte, int tamanho, Vector2 posicao, Vector2 area, TextAnchor alinhamento)
    {
        GameObject obj = new GameObject(nomeDoObjeto, typeof(RectTransform));
        obj.transform.SetParent(painel, false);

        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = area;

        Text texto = obj.AddComponent<Text>();
        texto.font = fonte;
        texto.fontSize = tamanho;
        texto.alignment = alinhamento;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        texto.raycastTarget = false;
        obj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
        return texto;
    }
}
