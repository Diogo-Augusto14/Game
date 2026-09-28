using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimapa no canto de cima, a direita, com as regras do Isaac:
///   - sala onde voce esta: branca;
///   - sala ja visitada: cinza;
///   - sala vizinha de uma visitada, onde voce ainda nao entrou: cinza escuro;
///   - o resto do andar fica escondido;
///   - sala do item e do chefe ganham um icone assim que aparecem no mapa.
///
/// Montado por codigo, igual a <see cref="Hud"/>: um quadrado de UI por casa da grade,
/// criado quando o andar e gerado. Trocar de sala so muda cores e liga/desliga
/// quadrados, sem recriar nada.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Andar))]
public class Minimapa : MonoBehaviour
{
    [SerializeField] private Vector2 tamanhoDaSala = new Vector2(26f, 16f);

    [SerializeField, Min(0f)] private float espaco = 3f;

    [SerializeField] private Vector2 margem = new Vector2(24f, 20f);

    [SerializeField] private Color corDoFundo = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color corAtual = Color.white;
    [SerializeField] private Color corVisitada = new Color(0.62f, 0.62f, 0.62f);
    [SerializeField] private Color corDescoberta = new Color(0.28f, 0.28f, 0.28f);
    [SerializeField] private Color corDoItem = new Color(0.95f, 0.78f, 0.25f);
    [SerializeField] private Color corDoChefe = new Color(0.85f, 0.15f, 0.15f);

    private Andar andar;
    private RectTransform painel;
    private Image[,] salas;

    private void Awake()
    {
        andar = GetComponent<Andar>();
    }

    private void OnEnable()
    {
        andar.AoGerar += Montar;
        andar.AoEntrarNaSala += AoEntrar;

        if (andar.Mapa != null)
            Montar(andar.Mapa);
    }

    private void OnDisable()
    {
        andar.AoGerar -= Montar;
        andar.AoEntrarNaSala -= AoEntrar;
    }

    private void AoEntrar(SalaNoMundo sala) => Redesenhar();

    private void Montar(MapaDoAndar mapa)
    {
        if (painel == null)
            painel = CriarPainel();

        foreach (Transform filho in painel)
            Destroy(filho.gameObject);

        Vector2 passo = tamanhoDaSala + Vector2.one * espaco;
        painel.sizeDelta = new Vector2(mapa.Largura * passo.x + espaco, mapa.Altura * passo.y + espaco);
        salas = new Image[mapa.Largura, mapa.Altura];

        foreach (SalaDoAndar sala in mapa.Salas)
        {
            Vector2 canto = new Vector2(espaco + sala.X * passo.x, espaco + sala.Y * passo.y);
            Image quadrado = Quadrado(painel, $"Sala ({sala.X},{sala.Y})", canto, tamanhoDaSala, corDescoberta);
            salas[sala.X, sala.Y] = quadrado;

            if (sala.Tipo == TipoDeSala.Item || sala.Tipo == TipoDeSala.Chefe)
            {
                float lado = tamanhoDaSala.y * 0.5f;
                Vector2 meio = (tamanhoDaSala - Vector2.one * lado) * 0.5f;
                Quadrado(quadrado.rectTransform, "Icone", meio, Vector2.one * lado,
                         sala.Tipo == TipoDeSala.Item ? corDoItem : corDoChefe);
            }
        }

        Redesenhar();
    }

    private void Redesenhar()
    {
        MapaDoAndar mapa = andar.Mapa;

        if (mapa == null || salas == null)
            return;

        foreach (SalaDoAndar sala in mapa.Salas)
        {
            Image quadrado = salas[sala.X, sala.Y];
            quadrado.gameObject.SetActive(sala.Descoberta);

            if (sala == andar.SalaAtual)
                quadrado.color = corAtual;
            else
                quadrado.color = sala.Visitada ? corVisitada : corDescoberta;
        }
    }

    private RectTransform CriarPainel()
    {
        GameObject tela = new GameObject("Minimapa (tela)");
        tela.transform.SetParent(transform, false);

        Canvas canvas = tela.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler escala = tela.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 0.5f;

        GameObject obj = new GameObject("Minimapa", typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(tela.transform, false);
        obj.GetComponent<Image>().color = corDoFundo;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.one; // canto de cima, a direita
        rt.anchoredPosition = -margem;
        return rt;
    }

    /// <summary>Quadrado de UI ancorado no canto de baixo, a esquerda, do pai.</summary>
    private static Image Quadrado(RectTransform pai, string nome, Vector2 canto, Vector2 tamanho, Color cor)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(pai, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
        rt.anchoredPosition = canto;
        rt.sizeDelta = tamanho;

        Image imagem = obj.GetComponent<Image>();
        imagem.color = cor;
        imagem.raycastTarget = false;
        return imagem;
    }
}
