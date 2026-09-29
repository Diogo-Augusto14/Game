using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pecas de tela montada por codigo (menu, pausa): canvas em tela cheia com fundo escuro e
/// textos centralizados. Mesma receita da <see cref="TelaDeFimDeJogo"/>, num lugar so.
/// </summary>
public static class TelaSimples
{
    /// <summary>Poe Canvas, CanvasScaler e um fundo que cobre a tela no objeto dado.</summary>
    public static CanvasGroup Montar(GameObject obj, int ordem, Color fundo)
    {
        Canvas canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ordem;

        CanvasScaler escala = obj.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 1f;

        CanvasGroup grupo = obj.AddComponent<CanvasGroup>();
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        GameObject objFundo = new GameObject("Fundo", typeof(RectTransform));
        objFundo.transform.SetParent(obj.transform, false);
        RectTransform rt = (RectTransform)objFundo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        Image imagem = objFundo.AddComponent<Image>();
        imagem.color = fundo;
        imagem.raycastTarget = false;

        return grupo;
    }

    /// <summary>Texto centralizado na horizontal, a <paramref name="y"/> pixels do meio da tela.</summary>
    public static Text Texto(Transform pai, string nome, int tamanho, Color cor, float y, string conteudo)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);

        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(1600f, tamanho * 4f);

        Text texto = obj.AddComponent<Text>();
        texto.font = TelaDeFimDeJogo.Fonte();
        texto.fontSize = tamanho;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = cor;
        texto.supportRichText = true;
        texto.horizontalOverflow = HorizontalWrapMode.Wrap;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        texto.raycastTarget = false;
        texto.text = conteudo;
        obj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        return texto;
    }

    /// <summary>
    /// Painel do Pixel UI pack (9-slice: as bordas nao esticam) centralizado, a
    /// <paramref name="y"/> do meio da tela, logo acima do fundo escuro e atras dos textos.
    /// Sem a imagem do pacote, nao desenha nada (os textos continuam legiveis no fundo).
    /// </summary>
    public static Image Painel(Transform pai, string nome, Sprite sprite, float y, Vector2 tamanho)
    {
        if (sprite == null)
            return null;

        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        obj.transform.SetSiblingIndex(Mathf.Min(1, pai.childCount - 1));

        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = tamanho;

        Image imagem = obj.AddComponent<Image>();
        imagem.sprite = sprite;
        imagem.type = Image.Type.Sliced;
        imagem.raycastTarget = false;
        return imagem;
    }

    /// <summary>Tira o controle do jogador (menu, pausa) ou devolve.</summary>
    public static void TravarJogador(Transform jogador, bool travar)
    {
        if (jogador == null || !jogador.TryGetComponent(out Entrada entrada))
            return;

        Vida vida = jogador.GetComponent<Vida>();

        entrada.Esquecer();
        entrada.enabled = !travar && (vida == null || !vida.EstaMorto);
    }
}
