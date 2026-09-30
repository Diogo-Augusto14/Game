using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Os itens pegos na partida, em coluna na lateral direita da tela (embaixo do minimapa).
/// Passar o mouse num item abre a caixa com o nome e o efeito dele
/// (<see cref="ItemPassivo.Efeito"/>). Sem mouse: [I] no teclado ou Select no controle
/// escolhe os itens um por um, e depois do ultimo fecha.
///
/// Com muitos itens, a coluna vira mais colunas pra esquerda e os icones encolhem: o
/// painel nunca passa da altura dele.
///
/// Canvas proprio. Criado pela <see cref="HudDoInventario"/>.
/// </summary>
[DisallowMultipleComponent]
public class PainelDeItens : MonoBehaviour
{
    [SerializeField, Min(8)] private int tamanhoDaFonte = 22;

    [Tooltip("Segundos que a caixa fica aberta quando o item foi escolhido pela tecla ou pelo controle")]
    [SerializeField, Min(0.5f)] private float tempoDaCaixa = 4f;

    // Na tela de referencia (1920x1080): margem da direita, onde a coluna comeca (o
    // minimapa ocupa o canto de cima) e a altura maxima dela. A coluna para ANTES da porta
    // da direita (e dos enfeites em volta dela, no meio da altura da tela): com mais itens,
    // abre colunas pra esquerda em vez de descer por cima da porta.
    private const float Margem = 24f;
    private const float Topo = 252f;
    private const float TopoDaDica = 230f;
    private const float AlturaMaxima = 150f;
    private const float Espaco = 6f;
    private const float Recheio = 6f;             // do fundo escuro ate os itens
    private const float LarguraDaCaixa = 420f;
    private const float Folga = 26f;              // do texto ate a borda da caixa
    private const float DuracaoDoPulo = 0.6f;

    private static readonly Color CorDaCasa = new Color(0f, 0f, 0f, 0.55f);
    private static readonly Color CorDoFundo = new Color(0.04f, 0.03f, 0.06f, 0.72f);
    private static readonly Color CorDaCasaAcesa = new Color(1f, 0.8f, 0.3f, 0.75f);

    private class Casa
    {
        public ItemPassivo item;
        public RectTransform rt;
        public Image fundo;
        public float fimDoPulo;
    }

    private EstatisticasDoJogador estatisticas;
    private readonly List<Casa> casas = new List<Casa>();
    private RectTransform raiz;
    private RectTransform caixa;
    private Image iconeDaCaixa;
    private Text textoDaCaixa;
    private RectTransform dica;
    private RectTransform fundo;
    private float lado = 48f;
    private int linhas = 1;

    private int escolhido = -1;          // pela tecla ou pelo controle
    private float fimDaEscolha;
    private int mostrado = -2;           // o que a caixa mostra agora (-1 = fechada)

    public static PainelDeItens Criar(EstatisticasDoJogador estatisticas)
    {
        PainelDeItens painel = new GameObject("Painel de itens").AddComponent<PainelDeItens>();
        painel.estatisticas = estatisticas;
        return painel;
    }

    private void Start()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 7;   // por cima da HUD e da barra do chefe, embaixo das telas

        CanvasScaler escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 1f;

        raiz = NovoRetangulo("Itens", transform);
        raiz.anchorMin = raiz.anchorMax = raiz.pivot = Vector2.one;
        raiz.anchoredPosition = new Vector2(-Margem, -Topo);
        raiz.sizeDelta = Vector2.zero;

        MontarCaixa();

        if (estatisticas != null)
        {
            // Itens que ja estavam com o jogador (andar novo com a HUD nova).
            foreach (ItemPassivo item in estatisticas.Itens)
                Acrescentar(item, false);

            estatisticas.AoPegarItem += AoPegar;
        }

        Arrumar();
        MostrarCaixa(-1);
    }

    private void OnDestroy()
    {
        if (estatisticas != null)
            estatisticas.AoPegarItem -= AoPegar;
    }

    private void AoPegar(ItemPassivo item)
    {
        Acrescentar(item, true);
        Arrumar();
    }

    // ---------------- quadro a quadro ----------------
    private void Update()
    {
        float agora = Time.unscaledTime;

        // Pulinho do item que acabou de chegar.
        foreach (Casa casa in casas)
        {
            float falta = Mathf.Clamp01((casa.fimDoPulo - agora) / DuracaoDoPulo);
            float pulo = 1f + 0.35f * Mathf.Sin(falta * Mathf.PI);
            casa.rt.localScale = Vector3.one * pulo;
        }

        // Pausa, menu e fim de jogo congelam o tempo e cobrem a tela: nada de caixa.
        bool jogando = Time.timeScale > 0f && !TelaDeInicio.Aberta
                       && TelaDeFimDeJogo.Atual == null && !TelaDeOpcoes.Ocupada;

        if (!jogando)
        {
            escolhido = -1;
            MostrarCaixa(-1);
            return;
        }

        if (casas.Count > 0 && (Input.GetKeyDown(KeyCode.I) || Controle.Apertou(BotaoDoControle.Select)))
        {
            escolhido = escolhido + 1 < casas.Count ? escolhido + 1 : -1;
            fimDaEscolha = agora + tempoDaCaixa;
        }

        if (escolhido >= 0 && agora >= fimDaEscolha)
            escolhido = -1;

        // O mouse em cima de um item manda mais que a escolha pela tecla.
        int embaixoDoMouse = -1;

        if (Input.mousePresent)
        {
            for (int i = 0; i < casas.Count; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(casas[i].rt, Input.mousePosition, null))
                {
                    embaixoDoMouse = i;
                    break;
                }
            }
        }

        MostrarCaixa(embaixoDoMouse >= 0 ? embaixoDoMouse : escolhido);
    }

    // ---------------- casas ----------------
    private void Acrescentar(ItemPassivo item, bool pular)
    {
        if (item == null)
            return;

        RectTransform rt = NovoRetangulo(item.nome, raiz);
        rt.anchorMin = rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);

        Image fundo = rt.gameObject.AddComponent<Image>();
        fundo.color = CorDaCasa;
        fundo.raycastTarget = false;

        RectTransform rtIcone = NovoRetangulo("Icone", rt);
        rtIcone.anchorMin = Vector2.zero;
        rtIcone.anchorMax = Vector2.one;
        rtIcone.offsetMin = Vector2.one * 4f;
        rtIcone.offsetMax = -Vector2.one * 4f;

        // O desenho do pacote (o mesmo do pedestal). Sem ele, um quadrado da cor do item.
        Image icone = rtIcone.gameObject.AddComponent<Image>();
        icone.sprite = ArteImportada.IconeDoItem(item.nome);
        icone.color = icone.sprite != null ? Color.white : item.cor;
        icone.preserveAspect = true;
        icone.raycastTarget = false;

        casas.Add(new Casa
        {
            item = item,
            rt = rt,
            fundo = fundo,
            fimDoPulo = pular ? Time.unscaledTime + DuracaoDoPulo : 0f
        });
    }

    /// <summary>
    /// Poe as casas no lugar: de cima pra baixo e, quando a coluna enche, outra coluna a
    /// esquerda. Icones menores com mais itens, pra caber sem cobrir o meio da tela.
    /// </summary>
    private void Arrumar()
    {
        int total = casas.Count;
        lado = total <= 12 ? 44f : total <= 24 ? 36f : 28f;
        float passo = lado + Espaco;
        linhas = Mathf.Max(1, Mathf.FloorToInt((AlturaMaxima + Espaco) / passo));

        for (int i = 0; i < total; i++)
        {
            RectTransform rt = casas[i].rt;
            rt.sizeDelta = Vector2.one * lado;
            rt.anchoredPosition = new Vector2(-(i / linhas) * passo - lado * 0.5f, -(i % linhas) * passo - lado * 0.5f);
        }

        ArrumarFundo(total, passo);
        ArrumarDica(total, passo);
        mostrado = -2;   // a caixa pode ter mudado de lugar
    }

    /// <summary>
    /// Fundo escuro atras dos itens: a parede de algumas salas tem desenho (estatuas,
    /// enfeites) e os icones sumiam em cima dele. So aparece com pelo menos um item.
    /// </summary>
    private void ArrumarFundo(int total, float passo)
    {
        if (fundo == null)
        {
            fundo = NovoRetangulo("Fundo", raiz);
            fundo.SetAsFirstSibling();
            fundo.anchorMin = fundo.anchorMax = fundo.pivot = Vector2.one;
            fundo.anchoredPosition = new Vector2(Recheio, Recheio);

            Image imagem = fundo.gameObject.AddComponent<Image>();
            imagem.color = CorDoFundo;
            imagem.raycastTarget = false;
        }

        fundo.gameObject.SetActive(total > 0);

        if (total == 0)
            return;

        int colunas = (total + linhas - 1) / linhas;
        int usadas = Mathf.Min(total, linhas);
        fundo.sizeDelta = new Vector2(colunas * passo - Espaco + Recheio * 2f, usadas * passo - Espaco + Recheio * 2f);
    }

    /// <summary>"[I] ver itens" FIXO em cima da coluna (embaixo ela passava por cima dos enfeites da sala com muitos itens), so quando tem item pra ver.</summary>
    private void ArrumarDica(int total, float passo)
    {
        if (total == 0)
        {
            if (dica != null)
                dica.gameObject.SetActive(false);

            return;
        }

        if (dica == null)
        {
            dica = TelaSimples.LinhaDeTeclas(transform, "Dica dos itens", 0f,
                "[I] ver itens || [Pad Select] ver itens", 16, new Color(1f, 1f, 1f, 0.75f));
            dica.anchorMin = dica.anchorMax = dica.pivot = Vector2.one;
        }

        dica.anchoredPosition = new Vector2(-Margem, -TopoDaDica);

        dica.gameObject.SetActive(true);
    }

    // ---------------- caixa da descricao ----------------
    private void MontarCaixa()
    {
        caixa = NovoRetangulo("Caixa do item", transform);
        caixa.anchorMin = caixa.anchorMax = caixa.pivot = Vector2.one;
        caixa.sizeDelta = new Vector2(LarguraDaCaixa, 160f);

        // A moldura dourada pequena do Dragon Regalia, desenhada na metade do tamanho
        // (a borda inteira seria grossa demais pra uma caixinha). Sem ela, um fundo escuro.
        Image fundo = caixa.gameObject.AddComponent<Image>();
        fundo.raycastTarget = false;
        Sprite moldura = ArteDaInterface.MolduraPequena;

        if (moldura != null)
        {
            fundo.sprite = moldura;
            fundo.type = Image.Type.Sliced;
            fundo.pixelsPerUnitMultiplier = 2f;
        }
        else
        {
            fundo.color = new Color(0.08f, 0.06f, 0.1f, 0.92f);
        }

        RectTransform rtIcone = NovoRetangulo("Icone", caixa);
        rtIcone.anchorMin = rtIcone.anchorMax = rtIcone.pivot = new Vector2(0f, 1f);
        rtIcone.anchoredPosition = new Vector2(Folga, -Folga);
        rtIcone.sizeDelta = Vector2.one * 48f;
        iconeDaCaixa = rtIcone.gameObject.AddComponent<Image>();
        iconeDaCaixa.preserveAspect = true;
        iconeDaCaixa.raycastTarget = false;

        RectTransform rtTexto = NovoRetangulo("Texto", caixa);
        rtTexto.anchorMin = rtTexto.anchorMax = rtTexto.pivot = new Vector2(0f, 1f);
        rtTexto.anchoredPosition = new Vector2(Folga + 60f, -Folga + 4f);
        rtTexto.sizeDelta = new Vector2(LarguraDaCaixa - Folga * 2f - 60f, 100f);

        textoDaCaixa = rtTexto.gameObject.AddComponent<Text>();
        textoDaCaixa.font = TelaDeFimDeJogo.Fonte();
        textoDaCaixa.fontSize = tamanhoDaFonte;
        textoDaCaixa.color = Color.white;
        textoDaCaixa.alignment = TextAnchor.UpperLeft;
        textoDaCaixa.supportRichText = true;
        textoDaCaixa.horizontalOverflow = HorizontalWrapMode.Wrap;
        textoDaCaixa.verticalOverflow = VerticalWrapMode.Overflow;
        textoDaCaixa.lineSpacing = 1.05f;
        textoDaCaixa.raycastTarget = false;
        rtTexto.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
    }

    /// <summary>Abre a caixa ao lado do item <paramref name="indice"/> (-1 fecha).</summary>
    private void MostrarCaixa(int indice)
    {
        if (indice >= casas.Count)
            indice = -1;

        if (indice == mostrado)
            return;

        if (mostrado >= 0 && mostrado < casas.Count)
            casas[mostrado].fundo.color = CorDaCasa;

        mostrado = indice;
        caixa.gameObject.SetActive(indice >= 0);

        if (indice < 0)
            return;

        Casa casa = casas[indice];
        casa.fundo.color = CorDaCasaAcesa;

        Sprite sprite = ArteImportada.IconeDoItem(casa.item.nome);
        iconeDaCaixa.sprite = sprite;
        iconeDaCaixa.color = sprite != null ? Color.white : casa.item.cor;

        textoDaCaixa.text =
            $"<size={tamanhoDaFonte + 6}><b>{casa.item.nome}</b></size>\n" +
            $"<color=#FFD86B>Efeito:</color> {casa.item.Efeito}";

        float altura = Mathf.Max(48f, textoDaCaixa.preferredHeight) + Folga * 2f;
        caixa.sizeDelta = new Vector2(LarguraDaCaixa, altura);

        // A esquerda do item, com o topo na altura dele; nunca sai por baixo da tela.
        Vector2 centro = raiz.anchoredPosition + casa.rt.anchoredPosition;
        float x = centro.x - lado * 0.5f - 12f;
        float y = centro.y + lado * 0.5f;
        y = Mathf.Max(y, -1080f + altura + 110f);
        caixa.anchoredPosition = new Vector2(x, y);
    }

    private static RectTransform NovoRetangulo(string nome, Transform pai)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        return (RectTransform)obj.transform;
    }
}
