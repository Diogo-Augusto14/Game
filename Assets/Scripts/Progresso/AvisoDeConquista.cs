using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// O aviso de conquista (e de heroi liberado): uma plaquinha que desce no canto de cima a direita
/// (o titulo e, embaixo, como ganhou), fica uns segundos e sobe de volta. Varias juntas
/// fazem fila. Canvas proprio, por cima da HUD e das telas; anda em tempo real (aparece
/// mesmo com o jogo congelado na tela de vitoria).
/// </summary>
[DisallowMultipleComponent]
public class AvisoDeConquista : MonoBehaviour
{
    private const float Entrada = 0.35f;
    private const float NaTela = 3.2f;
    private const float Largura = 520f;
    private const float Altura = 110f;

    private static AvisoDeConquista atual;

    private readonly Queue<(string titulo, string detalhe)> fila = new Queue<(string titulo, string detalhe)>();
    private RectTransform placa;
    private Text titulo;
    private Text detalhe;
    private float desde = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar() => atual = null;

    public static void Mostrar(string titulo, string detalhe)
    {
        if (atual == null)
        {
            atual = new GameObject("Aviso de conquista").AddComponent<AvisoDeConquista>();
            DontDestroyOnLoad(atual.gameObject);
            atual.Montar();
        }

        atual.fila.Enqueue((titulo, detalhe));
    }

    private void Montar()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        CanvasScaler escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 1f;

        GameObject obj = new GameObject("Placa", typeof(RectTransform));
        obj.transform.SetParent(transform, false);
        placa = (RectTransform)obj.transform;
        placa.anchorMin = placa.anchorMax = new Vector2(1f, 1f);
        placa.pivot = new Vector2(1f, 1f);
        placa.sizeDelta = new Vector2(Largura, Altura);

        Image fundo = obj.AddComponent<Image>();
        Sprite moldura = ArteDaInterface.MolduraPequena;
        fundo.sprite = moldura;
        fundo.type = moldura != null ? Image.Type.Sliced : Image.Type.Simple;
        fundo.color = moldura != null ? Color.white : new Color(0.08f, 0.06f, 0.08f, 0.92f);
        fundo.raycastTarget = false;

        titulo = Texto("Titulo", 30, new Color(1f, 0.85f, 0.4f), new Vector2(0f, -18f));
        detalhe = Texto("Detalhe", 22, new Color(0.92f, 0.9f, 0.95f), new Vector2(0f, -60f));
        placa.gameObject.SetActive(false);
    }

    private Text Texto(string nome, int tamanho, Color cor, Vector2 posicao)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(placa, false);
        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = new Vector2(-40f, tamanho * 1.6f);

        Text t = obj.AddComponent<Text>();
        t.font = FonteDoJogo.Texto;
        t.fontSize = tamanho;
        t.color = cor;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        obj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
        return t;
    }

    private void Update()
    {
        float agora = Time.unscaledTime;

        if (desde < 0f)
        {
            if (fila.Count == 0)
                return;

            (string t, string d) aviso = fila.Dequeue();
            titulo.text = aviso.t;
            detalhe.text = aviso.d;
            placa.gameObject.SetActive(true);
            desde = agora;
            Sons.Tocar(Som.Vitoria, 0.6f, 0f);
        }

        float t = agora - desde;
        float total = Entrada * 2f + NaTela;

        // Desce, fica, sobe.
        float dentro = t < Entrada ? TelaSimples.Freando(t / Entrada)
                     : t > total - Entrada ? 1f - TelaSimples.Freando((t - (total - Entrada)) / Entrada)
                     : 1f;
        placa.anchoredPosition = new Vector2(-24f, Mathf.Lerp(Altura + 20f, -24f, dentro));

        if (t < total)
            return;

        placa.gameObject.SetActive(false);
        desde = -1f;
    }
}
