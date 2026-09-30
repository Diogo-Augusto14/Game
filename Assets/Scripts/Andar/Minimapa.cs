using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimapa no canto de cima, a direita, com as regras do Isaac:
///   - sala onde voce esta: branca, com uma moldura piscando em volta;
///   - sala ja visitada: cinza;
///   - sala do outro lado de uma porta de sala visitada, onde voce ainda nao entrou: cinza
///     escuro;
///   - o resto do andar fica escondido;
///   - sala especial ganha o desenho do pacote assim que aparece no mapa: caveira no
///     chefe, taca no item, moeda na loja, trofeu no desafio, idolo na amaldicoada, bau na
///     secreta;
///   - entre duas salas ligadas aparece um tracinho (a porta). Salas coladas sem tracinho
///     tem parede entre elas.
///
/// So o pedaco descoberto do andar aparece, entao o quadro cresce conforme voce explora.
/// Segurando Tab (ou afundando o analogico esquerdo, L3, no controle) o mapa abre grande no meio da tela, com o nome
/// do andar e a legenda dos desenhos.
///
/// Montado por codigo, igual a <see cref="Hud"/>. Refaz o desenho so quando algo muda
/// (andar novo, troca de sala, abrir/fechar o mapa grande).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Andar))]
public class Minimapa : MonoBehaviour
{
    [Header("Canto da tela")]
    [SerializeField] private Vector2 tamanhoDaSala = new Vector2(26f, 16f);

    [SerializeField, Min(0f)] private float espaco = 4f;

    [SerializeField] private Vector2 margem = new Vector2(24f, 20f);

    [Tooltip("Espaco entre a moldura e as salas")]
    [SerializeField, Min(0f)] private float recheio = 14f;

    [Header("Mapa grande (segurar Tab)")]
    [SerializeField] private KeyCode teclaDoMapa = KeyCode.Tab;

    [SerializeField, Min(1f)] private float escalaDoMapaGrande = 2.4f;

    [Header("Cores")]
    [SerializeField] private Color corDoFundo = new Color(0.05f, 0.04f, 0.07f, 0.78f);
    [SerializeField] private Color corAtual = Color.white;
    [SerializeField] private Color corDaMarca = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color corVisitada = new Color(0.62f, 0.62f, 0.66f);
    [SerializeField] private Color corDescoberta = new Color(0.27f, 0.27f, 0.3f);
    [SerializeField] private Color corDaPorta = new Color(0.62f, 0.62f, 0.66f);
    [SerializeField] private Color corDoItem = new Color(0.95f, 0.78f, 0.25f);
    [SerializeField] private Color corDoChefe = new Color(0.85f, 0.15f, 0.15f);
    [SerializeField] private Color corDaLoja = new Color(0.4f, 0.85f, 0.45f);
    [SerializeField] private Color corDaSecreta = new Color(0.55f, 0.5f, 0.65f);
    [SerializeField] private Color corDoDesafio = new Color(0.95f, 0.5f, 0.15f);
    [SerializeField] private Color corDaAmaldicoada = new Color(0.6f, 0.1f, 0.35f);

    private Andar andar;
    private RectTransform tela;
    private RectTransform pequeno;
    private RectTransform grande;
    private Image marcaPequena;
    private Image marcaGrande;
    private bool abertoGrande;

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

    private void AoEntrar(Sala sala) => Redesenhar();

    private void Montar(MapaDoAndar mapa)
    {
        if (tela == null)
            CriarTela();

        Redesenhar();
    }

    private void Update()
    {
        // So com o jogo rodando: menu, pausa e fim de jogo param o tempo.
        bool segurando = Time.timeScale > 0f
                         && (Input.GetKey(teclaDoMapa) || AnalogicoEsquerdoAfundado);

        if (segurando != abertoGrande)
        {
            abertoGrande = segurando;
            Redesenhar();
        }

        // A moldura da sala atual pisca devagar.
        float brilho = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
        Piscar(marcaPequena, brilho);
        Piscar(marcaGrande, brilho);
    }

    /// <summary>
    /// Clique do analogico esquerdo (L3), segurado. Os outros botoes ja tem dono: A B X Y
    /// atiram, Start pausa, Select mostra os itens, LB/LT/RB/RT sao bomba, flecha e dash.
    /// (Controle.Segurando(AnalogicoEsquerdo) diz se o analogico esta inclinado, nao clicado.)
    /// </summary>
    private static bool AnalogicoEsquerdoAfundado
        => Controle.Atual != null && Controle.Atual.leftStickButton.isPressed;

    private void Piscar(Image marca, float brilho)
    {
        if (marca == null)
            return;

        Color cor = corDaMarca;
        cor.a = brilho;
        marca.color = cor;
    }

    // ---------------- desenho ----------------
    private void Redesenhar()
    {
        MapaDoAndar mapa = andar.Mapa;

        if (mapa == null || tela == null)
            return;

        marcaPequena = Desenhar(pequeno, mapa, tamanhoDaSala, espaco, false);
        grande.gameObject.SetActive(abertoGrande);

        if (abertoGrande)
            marcaGrande = Desenhar(grande, mapa, tamanhoDaSala * escalaDoMapaGrande, espaco * escalaDoMapaGrande, true);
        else
            marcaGrande = null;
    }

    /// <summary>
    /// Refaz o conteudo de um quadro (pequeno ou grande) com as salas descobertas. Devolve a
    /// moldura da sala atual, que pisca no <see cref="Update"/>.
    /// </summary>
    private Image Desenhar(RectTransform quadro, MapaDoAndar mapa, Vector2 celula, float vao, bool comLegenda)
    {
        for (int i = quadro.childCount - 1; i >= 0; i--)
        {
            Transform filho = quadro.GetChild(i);
            filho.SetParent(null, false);
            Destroy(filho.gameObject);
        }

        // Caixa em volta do que ja foi descoberto.
        int xMin = int.MaxValue, xMax = int.MinValue, yMin = int.MaxValue, yMax = int.MinValue;

        foreach (SalaDoAndar sala in mapa.Salas)
        {
            if (!sala.Descoberta)
                continue;

            xMin = Mathf.Min(xMin, sala.X);
            xMax = Mathf.Max(xMax, sala.X);
            yMin = Mathf.Min(yMin, sala.Y);
            yMax = Mathf.Max(yMax, sala.Y);
        }

        if (xMin > xMax)
            return null;

        Vector2 passo = celula + Vector2.one * vao;
        Vector2 miolo = new Vector2((xMax - xMin + 1) * passo.x - vao, (yMax - yMin + 1) * passo.y - vao);
        float borda = comLegenda ? recheio * 2f : recheio;
        float topo = comLegenda ? 70f : 0f;
        float rodape = comLegenda ? 80f : 0f;
        quadro.sizeDelta = new Vector2(Mathf.Max(miolo.x + borda * 2f, comLegenda ? 760f : 0f),
                                       miolo.y + borda * 2f + topo + rodape);

        Fundo(quadro);

        // Canto de baixo, a esquerda, das salas (as salas ficam centradas na largura).
        Vector2 origem = new Vector2((quadro.sizeDelta.x - miolo.x) * 0.5f, borda + rodape);
        Vector2 Canto(SalaDoAndar s) => origem + new Vector2((s.X - xMin) * passo.x, (s.Y - yMin) * passo.y);

        // Portas primeiro, pra ficarem por baixo das salas.
        foreach (SalaDoAndar sala in mapa.Salas)
        {
            if (!sala.Descoberta)
                continue;

            foreach (Direcao d in new[] { Direcao.Direita, Direcao.Cima })
            {
                SalaDoAndar outra = mapa.PelaPorta(sala, d);

                if (outra == null || !outra.Descoberta || !(sala.Visitada || outra.Visitada))
                    continue;

                // A porta da secreta so aparece depois que alguem entrou nela.
                if ((sala.Tipo == TipoDeSala.Secreta || outra.Tipo == TipoDeSala.Secreta) && !(sala.Visitada && outra.Visitada))
                    continue;

                bool deitada = d == Direcao.Direita;
                Vector2 tamanho = deitada ? new Vector2(vao + 4f, celula.y * 0.3f) : new Vector2(celula.x * 0.22f, vao + 4f);
                Vector2 meio = Canto(sala) + (deitada ? new Vector2(celula.x + vao * 0.5f, celula.y * 0.5f)
                                                      : new Vector2(celula.x * 0.5f, celula.y + vao * 0.5f));
                Quadrado(quadro, "Porta", meio - tamanho * 0.5f, tamanho, corDaPorta);
            }
        }

        Image marca = null;

        foreach (SalaDoAndar sala in mapa.Salas)
        {
            if (!sala.Descoberta)
                continue;

            Vector2 canto = Canto(sala);
            bool aqui = sala == andar.SalaAtual;

            if (aqui)
            {
                float folga = Mathf.Max(2f, vao * 0.6f);
                marca = Quadrado(quadro, "Voce esta aqui", canto - Vector2.one * folga, celula + Vector2.one * folga * 2f, corDaMarca);
            }

            Color cor = aqui ? corAtual : sala.Visitada ? corVisitada : corDescoberta;
            Image quadrado = Quadrado(quadro, $"Sala ({sala.X},{sala.Y})", canto, celula, cor);
            Icone(quadrado.rectTransform, sala.Tipo, celula);
        }

        if (comLegenda)
            Legenda(quadro, mapa);
        else
            TelaSimples.LinhaDeTeclas(quadro, "Dica", -quadro.sizeDelta.y * 0.5f - 18f, "[Tab] mapa || [Pad AnalogicoEsquerdo] afundar: mapa", 18,
                                      new Color(0.8f, 0.8f, 0.8f));

        return marca;
    }

    private void Fundo(RectTransform quadro)
    {
        Image fundo = Esticado(quadro, "Fundo", corDoFundo);

        // Moldura dourada do Dragon Regalia em volta (a mesma do menu), fina.
        Sprite moldura = ArteDaInterface.MolduraPequena;

        if (moldura == null)
            return;

        Image borda = Esticado(quadro, "Moldura", Color.white);
        borda.sprite = moldura;
        borda.type = Image.Type.Sliced;
        borda.fillCenter = false;
        borda.pixelsPerUnitMultiplier = 3f;
        fundo.rectTransform.offsetMin = Vector2.one * 4f;
        fundo.rectTransform.offsetMax = -Vector2.one * 4f;
    }

    /// <summary>Titulo com o nome do andar e a legenda dos desenhos, no mapa grande.</summary>
    private void Legenda(RectTransform quadro, MapaDoAndar mapa)
    {
        float meiaAltura = quadro.sizeDelta.y * 0.5f;
        string titulo = andar.UltimoAndar ? $"Ultimo andar: {andar.Tema.Nome}" : $"Andar {andar.NumeroDoAndar}: {andar.Tema.Nome}";
        TelaSimples.Texto(quadro, "Titulo", 40, new Color(1f, 0.9f, 0.55f), meiaAltura - 50f, titulo);

        (TipoDeSala tipo, string nome)[] itens =
        {
            (TipoDeSala.Chefe, "Chefe"), (TipoDeSala.Item, "Tesouro"), (TipoDeSala.Loja, "Loja"),
            (TipoDeSala.Desafio, "Desafio"), (TipoDeSala.Amaldicoada, "Amaldicoada"), (TipoDeSala.Secreta, "Secreta"),
        };

        // So o que ja apareceu no mapa, pra legenda nao entregar a secreta.
        List<(TipoDeSala tipo, string nome)> vistos = new List<(TipoDeSala, string)>();

        foreach ((TipoDeSala tipo, string nome) item in itens)
            foreach (SalaDoAndar sala in mapa.Salas)
                if (sala.Tipo == item.tipo && sala.Descoberta)
                {
                    vistos.Add(item);
                    break;
                }

        GameObject obj = new GameObject("Legenda", typeof(RectTransform));
        obj.transform.SetParent(quadro, false);
        RectTransform linha = (RectTransform)obj.transform;
        linha.anchorMin = linha.anchorMax = linha.pivot = new Vector2(0.5f, 0f);
        linha.anchoredPosition = new Vector2(0f, 26f);

        HorizontalLayoutGroup layout = obj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 22f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        ContentSizeFitter ajuste = obj.AddComponent<ContentSizeFitter>();
        ajuste.horizontalFit = ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        foreach ((TipoDeSala tipo, string nome) in vistos)
        {
            GameObject icone = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            icone.transform.SetParent(linha, false);
            LayoutElement tamanho = icone.GetComponent<LayoutElement>();
            tamanho.preferredWidth = tamanho.preferredHeight = 30f;
            Image imagem = icone.GetComponent<Image>();
            imagem.raycastTarget = false;
            imagem.preserveAspect = true;
            Sprite desenho = DesenhoDe(tipo);
            imagem.sprite = desenho;
            imagem.color = desenho != null ? Color.white : CorDe(tipo);

            Text texto = TelaSimples.Texto(linha, nome, 24, new Color(0.9f, 0.9f, 0.9f), 0f, nome);
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        TelaSimples.LinhaDeTeclas(quadro, "Dica", -meiaAltura - 26f, "[Tab] segure para ver o mapa || [Pad AnalogicoEsquerdo] afunde e segure para ver o mapa", 22,
                                  new Color(0.75f, 0.75f, 0.75f));
    }

    private void Icone(RectTransform sala, TipoDeSala tipo, Vector2 celula)
    {
        if (tipo == TipoDeSala.Normal || tipo == TipoDeSala.Inicio)
            return;

        Sprite desenho = DesenhoDe(tipo);
        float lado = celula.y * (desenho != null ? 0.95f : 0.5f);
        Vector2 canto = (celula - Vector2.one * lado) * 0.5f;
        Image icone = Quadrado(sala, "Icone", canto, Vector2.one * lado, desenho != null ? Color.white : CorDe(tipo));

        if (desenho != null)
        {
            icone.sprite = desenho;
            icone.preserveAspect = true;
        }
    }

    /// <summary>O desenho do pacote que marca cada sala especial (null = usa o quadradinho colorido).</summary>
    private static Sprite DesenhoDe(TipoDeSala tipo)
    {
        switch (tipo)
        {
            case TipoDeSala.Chefe: return ArteImportada.Objeto(2, 3);        // caveira
            case TipoDeSala.Item: return ArteImportada.Objeto(10, 4);        // taca dourada
            case TipoDeSala.Loja: return ArteImportada.Objeto(3, 3);         // moeda
            case TipoDeSala.Desafio: return ArteImportada.Trofeu(true);      // trofeu da porta
            case TipoDeSala.Amaldicoada: return ArteImportada.IdoloMaldito;  // idolo de olho vermelho
            case TipoDeSala.Secreta: return ArteImportada.Bau;               // bau
            default: return null;
        }
    }

    private Color CorDe(TipoDeSala tipo)
    {
        switch (tipo)
        {
            case TipoDeSala.Item: return corDoItem;
            case TipoDeSala.Chefe: return corDoChefe;
            case TipoDeSala.Loja: return corDaLoja;
            case TipoDeSala.Secreta: return corDaSecreta;
            case TipoDeSala.Desafio: return corDoDesafio;
            case TipoDeSala.Amaldicoada: return corDaAmaldicoada;
            default: return corDescoberta;
        }
    }

    // ---------------- montagem ----------------
    private void CriarTela()
    {
        GameObject obj = new GameObject("Minimapa (tela)", typeof(RectTransform));
        obj.transform.SetParent(transform, false);
        tela = (RectTransform)obj.transform;

        Canvas canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler escala = obj.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 0.5f;

        pequeno = Area("Minimapa", Vector2.one); // canto de cima, a direita
        pequeno.anchoredPosition = -margem;

        grande = Area("Mapa grande", new Vector2(0.5f, 0.5f));
        grande.anchoredPosition = Vector2.zero;
        grande.gameObject.SetActive(false);
    }

    private RectTransform Area(string nome, Vector2 ancora)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(tela, false);

        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = ancora;
        return rt;
    }

    /// <summary>Imagem que cobre o pai inteiro (fundo e moldura).</summary>
    private static Image Esticado(RectTransform pai, string nome, Color cor)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(pai, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        Image imagem = obj.GetComponent<Image>();
        imagem.color = cor;
        imagem.raycastTarget = false;
        return imagem;
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
