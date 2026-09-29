using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// A tela de quando o jogador morre no andar: escurece tudo, mostra ate que andar ele
/// chegou, os itens que pegou e a semente do andar, e espera R (ou Enter / Espaco) pra
/// comecar uma partida nova do zero.
///
/// Canvas proprio, montado por codigo como a <see cref="Hud"/>. O <see cref="Andar"/>
/// chama <see cref="Mostrar"/> quando a vida do jogador chega a zero.
/// </summary>
[DisallowMultipleComponent]
public class TelaDeFimDeJogo : MonoBehaviour
{
    [Tooltip("Segundos depois da morte ate a tela aparecer: da pra ver o que aconteceu")]
    [SerializeField, Min(0f)] private float atraso = 0.8f;

    [SerializeField, Min(0.01f)] private float tempoDoFade = 0.5f;

    [Tooltip("Com a tela inteira, o jogo congela (Time.timeScale = 0)")]
    [SerializeField] private bool congelarOJogo = true;

    private static readonly KeyCode[] TeclasDeRecomecar = { KeyCode.R, KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space };

    private CanvasGroup grupo;
    private float desde;
    private bool congelou;

    public static TelaDeFimDeJogo Atual { get; private set; }

    public static TelaDeFimDeJogo Mostrar(Andar andar) => Criar(andar, false);

    /// <summary>A mesma tela, mas de vitoria: o jogador venceu o chefe final.</summary>
    public static TelaDeFimDeJogo MostrarVitoria(Andar andar) => Criar(andar, true);

    private static TelaDeFimDeJogo Criar(Andar andar, bool vitoria)
    {
        if (Atual != null)
            return Atual;

        TelaDeFimDeJogo tela = new GameObject(vitoria ? "Tela de vitoria" : "Tela de fim de jogo").AddComponent<TelaDeFimDeJogo>();
        tela.Montar(andar, vitoria);

        if (vitoria)
        {
            tela.atraso = 1.5f;
            Sons.Tocar(Som.Vitoria);
        }

        return tela;
    }

    /// <summary>
    /// Recarrega a cena aberta: tudo volta como no Play. Funciona mesmo com a cena fora do
    /// Build Settings quando roda no editor (a cena de teste do andar costuma estar fora).
    /// </summary>
    public static void RecarregarCena()
    {
        Time.timeScale = 1f;
        Scene cena = SceneManager.GetActiveScene();

        if (cena.buildIndex >= 0)
        {
            SceneManager.LoadScene(cena.buildIndex);
            return;
        }

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            cena.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(cena.name);
#endif
    }

    private void Awake()
    {
        Atual = this;
        desde = Time.unscaledTime;
    }

    private void OnDestroy()
    {
        if (Atual == this)
            Atual = null;

        // Nunca deixa o jogo congelado pra tras (sair do Play, trocar de cena).
        if (congelou)
            Time.timeScale = 1f;
    }

    private void Update()
    {
        float t = Time.unscaledTime - desde - atraso;

        if (t < 0f)
            return;

        grupo.alpha = Mathf.Clamp01(t / tempoDoFade);

        if (grupo.alpha >= 1f && congelarOJogo && !congelou)
        {
            congelou = true;
            Time.timeScale = 0f;
        }

        // So aceita a tecla com a tela ja visivel: quem estava atirando nao pula a tela sem querer.
        if (grupo.alpha < 1f)
            return;

        foreach (KeyCode tecla in TeclasDeRecomecar)
        {
            if (Input.GetKeyDown(tecla))
            {
                RecarregarCena();
                return;
            }
        }
    }

    // ---------------- montagem ----------------
    private void Montar(Andar andar, bool vitoria)
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20; // por cima da HUD, do minimapa e da barra do chefe

        CanvasScaler escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 1f;

        grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        GameObject fundo = new GameObject("Fundo", typeof(RectTransform));
        fundo.transform.SetParent(transform, false);
        RectTransform rtFundo = (RectTransform)fundo.transform;
        rtFundo.anchorMin = Vector2.zero;
        rtFundo.anchorMax = Vector2.one;
        rtFundo.offsetMin = rtFundo.offsetMax = Vector2.zero;
        Image imagem = fundo.AddComponent<Image>();
        imagem.color = vitoria ? new Color(0.02f, 0.05f, 0.02f, 0.85f) : new Color(0.05f, 0f, 0f, 0.85f);
        imagem.raycastTarget = false;

        Font fonte = Fonte();

        if (vitoria)
            Texto("Titulo", fonte, 96, new Color(1f, 0.85f, 0.3f), 170f, "VOCE VENCEU!");
        else
            Texto("Titulo", fonte, 96, new Color(0.9f, 0.15f, 0.15f), 170f, "VOCE MORREU");

        Texto("Resumo", fonte, 36, Color.white, 40f, vitoria ? ResumoDaVitoria(andar) : Resumo(andar));
        Texto("Itens", fonte, 28, new Color(0.85f, 0.85f, 0.85f), -70f, Itens(andar));
        Texto("Recomecar", fonte, 34, new Color(1f, 0.85f, 0.4f), -220f, vitoria ? "R  jogar de novo" : "R  recomecar do andar 1");
    }

    private static string Resumo(Andar andar)
    {
        if (andar == null)
            return "";

        return $"Chegou ate o andar {andar.NumeroDoAndar}\n" +
               $"<size=24><color=#999999>semente {andar.SementeUsada}</color></size>";
    }

    private static string ResumoDaVitoria(Andar andar)
    {
        if (andar == null)
            return "";

        return $"O Olho do Porao caiu. Voce desceu {andar.NumeroDoAndar} andares\n" +
               $"<size=24><color=#999999>semente {andar.SementeUsada}</color></size>";
    }

    private static string Itens(Andar andar)
    {
        EstatisticasDoJogador estatisticas = andar != null && andar.Jogador != null
            ? andar.Jogador.GetComponent<EstatisticasDoJogador>()
            : null;

        if (estatisticas == null || estatisticas.Itens.Count == 0)
            return "Nenhum item pego";

        List<string> nomes = new List<string>();

        foreach (ItemPassivo item in estatisticas.Itens)
            nomes.Add($"<color=#{ColorUtility.ToHtmlStringRGB(item.cor)}>{item.nome}</color>");

        return "Itens: " + string.Join("   ", nomes);
    }

    private void Texto(string nome, Font fonte, int tamanho, Color cor, float y, string conteudo)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(transform, false);

        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(1600f, tamanho * 3f);

        Text texto = obj.AddComponent<Text>();
        texto.font = fonte;
        texto.fontSize = tamanho;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = cor;
        texto.supportRichText = true;
        texto.horizontalOverflow = HorizontalWrapMode.Wrap;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        texto.raycastTarget = false;
        texto.text = conteudo;
        obj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
    }

    internal static Font Fonte()
    {
        Font fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (fonte == null)
            fonte = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return fonte;
    }
}
