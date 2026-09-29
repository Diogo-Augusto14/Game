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
    /// Painel de pacote (9-slice: as bordas nao esticam) centralizado, a
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

    /// <summary>
    /// Faixa de titulo (Dragon Regalia) atras de um texto que esta em <paramref name="yDoTexto"/>:
    /// o pano da faixa fica centrado no texto. Sem a imagem, nao desenha nada.
    /// </summary>
    public static Image Faixa(Transform pai, string nome, Sprite sprite, float yDoTexto, float largura)
    {
        return Painel(pai, nome, sprite, yDoTexto - ArteDaInterface.MeioDoPanoDaFaixa,
                      new Vector2(largura, ArteDaInterface.AlturaDaFaixa));
    }

    // ---------------- dicas de controle com o desenho das teclas ----------------
    private static readonly System.Collections.Generic.Dictionary<string, KeyCode> ApelidosDeTecla =
        new System.Collections.Generic.Dictionary<string, KeyCode>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Enter", KeyCode.Return }, { "Esc", KeyCode.Escape }, { "Espaco", KeyCode.Space },
            { "Cima", KeyCode.UpArrow }, { "Baixo", KeyCode.DownArrow },
            { "Esquerda", KeyCode.LeftArrow }, { "Direita", KeyCode.RightArrow },
        };

    /// <summary>
    /// Uma linha centralizada de dicas com o desenho das teclas, a <paramref name="y"/> do meio
    /// da tela. <paramref name="conteudo"/> mistura teclas entre colchetes e texto, e "|" separa
    /// os grupos: <c>"[W][A][S][D] andar | [Esc] pausar"</c>. Tecla sem desenho vira texto.
    /// Botao do controle e "[Pad X]" (nomes do <see cref="BotaoDoControle"/>). Com " || ", o
    /// que vem depois e a mesma linha pro controle: ela troca sozinha quando o jogador passa
    /// do teclado pro controle (ver <see cref="DicaDupla"/>).
    /// </summary>
    public static RectTransform LinhaDeTeclas(Transform pai, string nome, float y, string conteudo, int tamanho, Color cor)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);

        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);

        HorizontalLayoutGroup linha = obj.AddComponent<HorizontalLayoutGroup>();
        linha.childAlignment = TextAnchor.MiddleCenter;
        linha.spacing = Mathf.Round(tamanho * 0.2f);
        linha.childControlWidth = true;
        linha.childControlHeight = true;
        linha.childForceExpandWidth = false;
        linha.childForceExpandHeight = false;

        ContentSizeFitter ajuste = obj.AddComponent<ContentSizeFitter>();
        ajuste.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TrocarLinhaDeTeclas(rt, conteudo, tamanho, cor);
        return rt;
    }

    /// <summary>Refaz o conteudo de uma linha da <see cref="LinhaDeTeclas"/> (ex.: musica ligada/desligada).</summary>
    public static void TrocarLinhaDeTeclas(RectTransform linha, string conteudo, int tamanho, Color cor)
    {
        int corte = conteudo.IndexOf("||", System.StringComparison.Ordinal);
        DicaDupla dupla = linha.GetComponent<DicaDupla>();

        if (corte >= 0 || dupla != null)
        {
            string teclado = corte >= 0 ? conteudo.Substring(0, corte).Trim() : conteudo;
            string controle = corte >= 0 ? conteudo.Substring(corte + 2).Trim() : conteudo;

            if (dupla == null)
                dupla = linha.gameObject.AddComponent<DicaDupla>();

            conteudo = dupla.Guardar(teclado, controle, tamanho, cor);
        }

        Desenhar(linha, conteudo, tamanho, cor);
    }

    /// <summary>Desenha uma versao so (teclado ou controle) da linha.</summary>
    public static void Desenhar(RectTransform linha, string conteudo, int tamanho, Color cor)
    {
        for (int i = linha.childCount - 1; i >= 0; i--)
        {
            // Destroy so acontece no fim do quadro: tira do pai ja pra o layout nao contar.
            Transform filho = linha.GetChild(i);
            filho.SetParent(null, false);
            Object.Destroy(filho.gameObject);
        }

        float lado = Mathf.Round(tamanho * 1.6f);
        int i0 = 0;

        while (i0 < conteudo.Length)
        {
            char c = conteudo[i0];

            if (c == '[')
            {
                int fim = conteudo.IndexOf(']', i0);

                if (fim > i0)
                {
                    string nomeDaTecla = conteudo.Substring(i0 + 1, fim - i0 - 1);
                    Tecla(linha, nomeDaTecla, lado, tamanho, cor);
                    i0 = fim + 1;
                    continue;
                }
            }

            if (c == '|')
            {
                Espaco(linha, tamanho * 1.4f);
                i0++;
                continue;
            }

            int proximo = conteudo.IndexOfAny(new[] { '[', '|' }, i0 + 1);

            if (proximo < 0)
                proximo = conteudo.Length;

            string pedaco = conteudo.Substring(i0, proximo - i0).Trim();

            if (pedaco.Length > 0)
                TextoDaLinha(linha, pedaco, tamanho, cor);

            i0 = proximo;
        }
    }

    private static void Tecla(Transform linha, string nome, float lado, int tamanho, Color cor)
    {
        if (nome.StartsWith("Pad ", System.StringComparison.OrdinalIgnoreCase))
        {
            BotaoDoPad(linha, nome.Substring(4).Trim(), lado, tamanho, cor);
            return;
        }

        if (!ApelidosDeTecla.TryGetValue(nome, out KeyCode tecla) && !System.Enum.TryParse(nome, true, out tecla))
            tecla = KeyCode.None;

        if (ArteDaInterface.Tecla(tecla) == null)
        {
            TextoDaLinha(linha, nome, tamanho, cor);
            return;
        }

        GameObject obj = new GameObject("Tecla " + nome, typeof(RectTransform));
        obj.transform.SetParent(linha, false);

        Image imagem = obj.AddComponent<Image>();
        imagem.raycastTarget = false;
        obj.AddComponent<IconeDeTecla>().Configurar(tecla);

        LayoutElement tamanhoFixo = obj.AddComponent<LayoutElement>();
        tamanhoFixo.preferredWidth = tamanhoFixo.minWidth = lado;
        tamanhoFixo.preferredHeight = tamanhoFixo.minHeight = lado;
    }

    private static void BotaoDoPad(Transform linha, string nome, float lado, int tamanho, Color cor)
    {
        if (!System.Enum.TryParse(nome, true, out BotaoDoControle botao)
            || ArteDaInterface.DesenhoDoBotao(botao, Controle.PlayStation) == null)
        {
            TextoDaLinha(linha, nome, tamanho, cor);
            return;
        }

        GameObject obj = new GameObject("Botao " + nome, typeof(RectTransform));
        obj.transform.SetParent(linha, false);

        Image imagem = obj.AddComponent<Image>();
        imagem.raycastTarget = false;
        obj.AddComponent<IconeDeTecla>().Configurar(botao);

        LayoutElement tamanhoFixo = obj.AddComponent<LayoutElement>();
        tamanhoFixo.preferredWidth = tamanhoFixo.minWidth = lado;
        tamanhoFixo.preferredHeight = tamanhoFixo.minHeight = lado;
    }

    private static void Espaco(Transform linha, float largura)
    {
        GameObject obj = new GameObject("Espaco", typeof(RectTransform));
        obj.transform.SetParent(linha, false);
        LayoutElement espaco = obj.AddComponent<LayoutElement>();
        espaco.preferredWidth = espaco.minWidth = largura;
    }

    private static void TextoDaLinha(Transform linha, string conteudo, int tamanho, Color cor)
    {
        GameObject obj = new GameObject("Texto", typeof(RectTransform));
        obj.transform.SetParent(linha, false);

        Text texto = obj.AddComponent<Text>();
        texto.font = TelaDeFimDeJogo.Fonte();
        texto.fontSize = tamanho;
        texto.alignment = TextAnchor.MiddleLeft;
        texto.color = cor;
        texto.supportRichText = true;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        texto.raycastTarget = false;
        texto.text = conteudo;
        obj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
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
