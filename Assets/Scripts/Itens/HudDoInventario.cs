using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pedaco de HUD dos itens: os contadores de moeda, chave e bomba (com icone) embaixo da
/// vida, e o nome do item no meio da tela quando o jogador pega um (some sozinho).
///
/// Canvas proprio, separado da <see cref="Hud"/>, pra nao mexer na HUD do plataforma.
/// Monte com <see cref="Criar"/>.
/// </summary>
[DisallowMultipleComponent]
public class HudDoInventario : MonoBehaviour
{
    [SerializeField, Min(8)] private int tamanhoDaFonte = 18;

    [Tooltip("Segundos que o nome do item fica na tela")]
    [SerializeField, Min(0.5f)] private float tempoDoAviso = 2.5f;

    private Inventario inventario;
    private EstatisticasDoJogador estatisticas;
    private Text moedas;
    private Text chaves;
    private Text bombas;
    private Text aviso;
    private float fimDoAviso;

    public static HudDoInventario Criar(GameObject jogador)
    {
        HudDoInventario hud = new GameObject("Hud do inventario").AddComponent<HudDoInventario>();
        hud.inventario = jogador.GetComponent<Inventario>();
        hud.estatisticas = jogador.GetComponent<EstatisticasDoJogador>();

        if (hud.estatisticas == null)
            hud.estatisticas = jogador.AddComponent<EstatisticasDoJogador>();

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

        Font fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (fonte == null)
            fonte = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Embaixo dos coracoes da Hud (margem 24,20 e coracoes de 48): icone + numero.
        const float y = -20f - Hud.LadoDoCoracao - 14f;
        moedas = Contador("Moedas", TipoDeColetavel.Moeda, fonte, new Vector2(24f, y));
        chaves = Contador("Chaves", TipoDeColetavel.Chave, fonte, new Vector2(124f, y));
        bombas = Contador("Bombas", TipoDeColetavel.Bomba, fonte, new Vector2(224f, y));

        aviso = CriarTexto("Aviso do item", fonte, tamanhoDaFonte + 14, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(-400f, -180f), new Vector2(800f, 100f));
        aviso.enabled = false;

        if (inventario != null)
            inventario.AoMudar += AtualizarContadores;

        if (estatisticas != null)
            estatisticas.AoPegarItem += Avisar;

        AtualizarContadores();
    }

    private void OnDestroy()
    {
        if (inventario != null)
            inventario.AoMudar -= AtualizarContadores;

        if (estatisticas != null)
            estatisticas.AoPegarItem -= Avisar;
    }

    private void Update()
    {
        if (aviso != null && aviso.enabled && Time.time >= fimDoAviso)
            aviso.enabled = false;
    }

    private void AtualizarContadores()
    {
        if (moedas == null || inventario == null)
            return;

        moedas.text = inventario.Moedas.ToString("00");
        chaves.text = inventario.Chaves.ToString("00");
        bombas.text = inventario.Bombas.ToString("00");
    }

    private void Avisar(ItemPassivo item)
    {
        if (aviso == null)
            return;

        aviso.text = $"{item.nome}\n<size={tamanhoDaFonte}>{item.descricao}</size>";
        aviso.enabled = true;
        fimDoAviso = Time.time + tempoDoAviso;
    }

    /// <summary>A pixel art do coletavel (32x32 na tela) e o numero do lado.</summary>
    private Text Contador(string nome, TipoDeColetavel tipo, Font fonte, Vector2 posicao)
    {
        GameObject obj = new GameObject("Icone " + nome, typeof(RectTransform));
        obj.transform.SetParent(transform, false);

        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = new Vector2(32f, 32f);

        Image icone = obj.AddComponent<Image>();
        icone.sprite = ArteGerada.Coletavel(tipo);
        icone.raycastTarget = false;

        return CriarTexto(nome, fonte, tamanhoDaFonte + 6, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), posicao + new Vector2(38f, 0f), new Vector2(60f, 32f));
    }

    private Text CriarTexto(string nome, Font fonte, int tamanho, TextAnchor alinhamento,
                           Vector2 ancora, Vector2 posicao, Vector2 area)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(transform, false);

        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = ancora;
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = area;

        Text texto = obj.AddComponent<Text>();
        texto.font = fonte;
        texto.fontSize = tamanho;
        texto.color = Color.white;
        texto.alignment = alinhamento;
        texto.supportRichText = true;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        texto.raycastTarget = false;

        // Contorno escuro pra ler em cima de qualquer chao.
        obj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        return texto;
    }
}
