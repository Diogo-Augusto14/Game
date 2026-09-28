using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pedaco de HUD dos itens: os contadores de moeda, chave e bomba embaixo da barra de
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
    private Text contadores;
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

        // Embaixo da barra de vida da Hud (margem 24,20 e barra de 22 de altura).
        contadores = CriarTexto("Contadores", fonte, tamanhoDaFonte, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(24f, -52f), new Vector2(500f, 30f));

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
        if (contadores == null || inventario == null)
            return;

        contadores.text = $"Moedas {inventario.Moedas:00}    Chaves {inventario.Chaves:00}    Bombas {inventario.Bombas:00}";
    }

    private void Avisar(ItemPassivo item)
    {
        if (aviso == null)
            return;

        aviso.text = $"{item.nome}\n<size={tamanhoDaFonte}>{item.descricao}</size>";
        aviso.enabled = true;
        fimDoAviso = Time.time + tempoDoAviso;
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
