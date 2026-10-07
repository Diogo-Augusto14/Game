using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// O painel da arma na mao, no canto de baixo a direita: o desenho dela, o nome, o pente (uma
/// bala desenhada por tiro), a municao que sobra fora do pente e "recarregando". So aparece depois
/// da primeira arma de fogo; com a arma do heroi na mao mostra a municao infinita.
///
/// O painel das flechas (<see cref="TrocaDeFlecha"/>) fica logo acima, quando aparece.
/// Monte com <see cref="Criar"/>.
/// </summary>
[DisallowMultipleComponent]
public class HudDasArmas : MonoBehaviour
{
    private const int MaximoDeBalas = 30;
    private const float LarguraDaBala = 9f;
    private const float PassoDaBala = 11f;

    private ArsenalDoJogador arsenal;
    private RectTransform painel;
    private Image icone;
    private Text nome;
    private Text municao;
    private Image[] balas;
    private float momentoDaTroca = -10f;
    private ArmaDeFogo ultimaArma;

    private static readonly Color BalaCheia = new Color(1f, 0.86f, 0.35f);
    private static readonly Color BalaVazia = new Color(1f, 1f, 1f, 0.16f);

    public static HudDasArmas Criar(ArsenalDoJogador arsenal)
    {
        HudDasArmas hud = new GameObject("HUD das armas", typeof(RectTransform)).AddComponent<HudDasArmas>();
        hud.arsenal = arsenal;
        return hud;
    }

    private void Start()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;

        CanvasScaler escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 1f;

        MontarPainel();

        if (arsenal != null)
            arsenal.AoMudar += Atualizar;

        Atualizar();
    }

    private void OnDestroy()
    {
        if (arsenal != null)
            arsenal.AoMudar -= Atualizar;
    }

    private void Update()
    {
        if (painel == null)
            return;

        // Destaque rapido ao trocar de arma: o quadro cresce e volta.
        float t = (Time.unscaledTime - momentoDaTroca) / 0.35f;
        painel.localScale = Vector3.one * (t < 1f ? 1f + Mathf.Sin(t * Mathf.PI) * 0.12f : 1f);
    }

    private void MontarPainel()
    {
        GameObject obj = new GameObject("Painel", typeof(RectTransform));
        obj.transform.SetParent(transform, false);
        painel = (RectTransform)obj.transform;
        painel.anchorMin = painel.anchorMax = new Vector2(1f, 0f);
        painel.pivot = new Vector2(1f, 0f);
        painel.anchoredPosition = new Vector2(-24f, 24f);
        painel.sizeDelta = new Vector2(360f, 140f);

        Image fundo = obj.AddComponent<Image>();
        fundo.sprite = ArteImportada.PainelMarrom;
        fundo.type = Image.Type.Sliced;
        fundo.color = fundo.sprite != null ? new Color(1f, 1f, 1f, 0.92f) : new Color(0f, 0f, 0f, 0.6f);
        fundo.raycastTarget = false;

        GameObject objIcone = new GameObject("Icone", typeof(RectTransform));
        objIcone.transform.SetParent(painel, false);
        RectTransform rtIcone = (RectTransform)objIcone.transform;
        rtIcone.anchorMin = rtIcone.anchorMax = new Vector2(0f, 1f);
        rtIcone.pivot = new Vector2(0.5f, 0.5f);
        rtIcone.anchoredPosition = new Vector2(58f, -58f);
        rtIcone.sizeDelta = new Vector2(72f, 72f);
        icone = objIcone.AddComponent<Image>();
        icone.preserveAspect = true;
        icone.raycastTarget = false;

        Font fonte = FonteDoJogo.Texto;
        nome = Texto("Nome", fonte, 26, new Vector2(108f, -40f), new Vector2(240f, 34f));
        municao = Texto("Municao", fonte, 34, new Vector2(108f, -78f), new Vector2(240f, 42f));
        municao.fontStyle = FontStyle.Bold;

        // As balas do pente, numa fileira embaixo.
        balas = new Image[MaximoDeBalas];

        for (int i = 0; i < MaximoDeBalas; i++)
        {
            GameObject objBala = new GameObject("Bala " + i, typeof(RectTransform));
            objBala.transform.SetParent(painel, false);
            RectTransform rt = (RectTransform)objBala.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(24f + i * PassoDaBala, 22f);
            rt.sizeDelta = new Vector2(LarguraDaBala, 20f);

            balas[i] = objBala.AddComponent<Image>();
            balas[i].raycastTarget = false;
            objBala.SetActive(false);
        }

        RectTransform dica = TelaSimples.LinhaDeTeclas(painel, "Dica", 0f,
            "[R] recarregar | [Q] trocar || [Pad AnalogicoEsquerdo] recarregar | [Pad LT] trocar", 16, new Color(1f, 1f, 1f, 0.8f));
        // Fora do quadro, logo em cima: dentro ela sobrepunha a moldura.
        dica.anchorMin = dica.anchorMax = new Vector2(1f, 1f);
        dica.pivot = new Vector2(1f, 0f);
        dica.anchoredPosition = new Vector2(-6f, 6f);
    }

    private Text Texto(string nomeDoObjeto, Font fonte, int tamanho, Vector2 posicao, Vector2 area)
    {
        GameObject obj = new GameObject(nomeDoObjeto, typeof(RectTransform));
        obj.transform.SetParent(painel, false);

        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = area;

        Text texto = obj.AddComponent<Text>();
        texto.font = fonte;
        texto.fontSize = tamanho;
        texto.alignment = TextAnchor.MiddleLeft;
        texto.supportRichText = true;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        texto.raycastTarget = false;
        obj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
        return texto;
    }

    private void Atualizar()
    {
        if (painel == null || arsenal == null)
            return;

        painel.gameObject.SetActive(arsenal.ComArmasDeFogo);

        if (!arsenal.ComArmasDeFogo)
            return;

        ArsenalDoJogador.Slot slot = arsenal.SlotAtual;
        ArmaDeFogo arma = slot.arma;

        if (arma != ultimaArma)
        {
            ultimaArma = arma;
            momentoDaTroca = Time.unscaledTime;
        }

        if (arma == null)
        {
            // A arma do heroi: sem pente e sem fim.
            icone.sprite = null;
            icone.enabled = false;
            nome.text = "Arma do " + Herois.Atual.Nome;
            nome.color = Color.white;
            municao.text = "<size=46>∞</size>";
            MostrarBalas(0, 0);
            return;
        }

        icone.sprite = arma.Icone;
        icone.enabled = true;
        nome.text = arma.nome;
        nome.color = arma.cor;

        if (arsenal.Recarregando)
        {
            municao.text = "<size=30><color=#FFD966>Recarregando...</color></size>";
        }
        else
        {
            string reserva = arma.Infinita ? "∞" : Mathf.Max(0, slot.municao - slot.pente).ToString();
            municao.text = $"<size=46>{slot.pente}</size> / {reserva}";
        }

        MostrarBalas(arma.tamanhoDoPente, arsenal.Recarregando ? 0 : slot.pente);
    }

    private void MostrarBalas(int tamanhoDoPente, int noPente)
    {
        // Pente maior que a fileira: cada bala desenhada vale mais de um tiro.
        int quantas = Mathf.Min(tamanhoDoPente, MaximoDeBalas);
        int cheias = tamanhoDoPente <= MaximoDeBalas
            ? noPente
            : Mathf.CeilToInt(noPente * (float)MaximoDeBalas / tamanhoDoPente);

        for (int i = 0; i < MaximoDeBalas; i++)
        {
            balas[i].gameObject.SetActive(i < quantas);

            if (i < quantas)
                balas[i].color = i < cheias ? BalaCheia : BalaVazia;
        }
    }
}
