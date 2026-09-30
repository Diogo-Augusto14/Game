using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Letreiro que aparece no meio da tela ao comecar uma fase ("Mundo 1 - Fase 2"), fica um pouco e
/// some sozinho. Um de cada vez: um aviso novo troca o texto do que ja esta na tela.
/// </summary>
[DisallowMultipleComponent]
public class AvisoDoAndar : MonoBehaviour
{
    [SerializeField, Min(0f)] private float tempoNaTela = 1.6f;

    [SerializeField, Min(0.01f)] private float tempoDoFade = 0.4f;

    private static AvisoDoAndar atual;

    private CanvasGroup grupo;
    private Text texto;
    private Image faixa;
    private float desde;

    public static void Mostrar(string mensagem)
    {
        if (atual == null)
        {
            atual = new GameObject("Aviso do andar").AddComponent<AvisoDoAndar>();
            atual.Montar();
        }

        atual.texto.text = mensagem;

        // A placa do Dragon Regalia acompanha o tamanho do texto.
        if (atual.faixa != null)
            atual.faixa.rectTransform.sizeDelta = new Vector2(atual.texto.preferredWidth + 240f, ArteDaInterface.AlturaDaFaixa);
        atual.desde = Time.unscaledTime;
        atual.grupo.alpha = 1f;
    }

    private void OnDestroy()
    {
        if (atual == this)
            atual = null;
    }

    private void Update()
    {
        float t = Time.unscaledTime - desde - tempoNaTela;
        grupo.alpha = t <= 0f ? 1f : Mathf.Clamp01(1f - t / tempoDoFade);

        if (grupo.alpha <= 0f)
            Destroy(gameObject);
    }

    private void Montar()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 8;

        CanvasScaler escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 1f;

        grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        Sprite placa = ArteDaInterface.FaixaAzul;

        if (placa != null)
        {
            GameObject objFaixa = new GameObject("Faixa", typeof(RectTransform));
            objFaixa.transform.SetParent(transform, false);
            RectTransform rtFaixa = (RectTransform)objFaixa.transform;
            rtFaixa.anchorMin = rtFaixa.anchorMax = new Vector2(0.5f, 0.5f);
            rtFaixa.anchoredPosition = new Vector2(0f, 260f - ArteDaInterface.MeioDoPanoDaFaixa);

            faixa = objFaixa.AddComponent<Image>();
            faixa.sprite = placa;
            faixa.type = Image.Type.Sliced;
            faixa.raycastTarget = false;
        }

        GameObject obj = new GameObject("Texto", typeof(RectTransform));
        obj.transform.SetParent(transform, false);

        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 260f);
        rt.sizeDelta = new Vector2(1200f, 140f);

        texto = obj.AddComponent<Text>();
        texto.font = TelaDeFimDeJogo.Fonte();
        texto.fontSize = 64;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.white;
        texto.raycastTarget = false;
        obj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
    }
}
