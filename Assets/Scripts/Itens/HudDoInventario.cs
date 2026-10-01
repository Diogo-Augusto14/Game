using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pedaco de HUD dos itens: os contadores de moeda, chave e bomba (com icone) embaixo da
/// vida, um em cada linha, e o nome do item no meio da tela quando o jogador pega um (some
/// sozinho). Os itens pegos ficam na lateral direita (<see cref="PainelDeItens"/>).
///
/// Os contadores ficam num lugar fixo, calculado pela area reservada da vida
/// (<see cref="Hud.AlturaDaVida"/>) e nao pelos coracoes: ganhar ou perder vida nunca
/// mexe neles.
///
/// Canvas proprio, separado da <see cref="Hud"/>, pra nao mexer na HUD do plataforma.
/// Monte com <see cref="Criar"/>.
/// </summary>
[DisallowMultipleComponent]
public class HudDoInventario : MonoBehaviour
{
    [SerializeField, Min(8)] private int tamanhoDaFonte = 18;

    [Tooltip("Tamanho do numero dos contadores")]
    [SerializeField, Min(8)] private int tamanhoDosContadores = 30;

    [Tooltip("Segundos que o nome do item fica na tela")]
    [SerializeField, Min(0.5f)] private float tempoDoAviso = 2.5f;

    private Inventario inventario;
    private EstatisticasDoJogador estatisticas;
    private Text moedas;
    private Text chaves;
    private Text bombas;
    private Text aviso;
    private Image iconeDoAviso;
    private float fimDoAviso;
    private bool avisoAtivo;

    // Lado do icone de cada contador e a distancia entre uma linha e a outra.
    private const float LadoDoIcone = 40f;
    private const float PassoDaLinha = 48f;

    // Pulinho do contador que acabou de mudar: da pra ver o que entrou ou saiu.
    private readonly int[] ultimos = { -1, -1, -1 };
    private readonly float[] fimDoPulo = new float[3];
    private readonly RectTransform[] linhas = new RectTransform[3];
    private const float DuracaoDoPulo = 0.25f;

    // Espaco do item ativo, embaixo dos contadores: moldura, icone, barra de carga e a tecla.
    private const float LadoDoAtivo = 72f;
    private const float AlturaDaBarra = 72f;
    private RectTransform espacoDoAtivo;
    private Image iconeDoAtivo;
    private Image fundoDaBarra;
    private Image cargaDoAtivo;
    private Text teclaDoAtivo;
    private ItemAtivoDoJogador ativo;

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

        Font fonte = FonteDoJogo.Texto;

        // Uma coluna embaixo da area da vida: moedas, chaves e bombas, uma por linha.
        float y = -Hud.CantoDaVida.y - Hud.AlturaDaVida - 12f;
        float x = Hud.CantoDaVida.x;
        moedas = Contador(0, "Moedas", TipoDeColetavel.Moeda, fonte, new Vector2(x, y));
        chaves = Contador(1, "Chaves", TipoDeColetavel.Chave, fonte, new Vector2(x, y - PassoDaLinha));
        bombas = Contador(2, "Bombas", TipoDeColetavel.Bomba, fonte, new Vector2(x, y - PassoDaLinha * 2f));

        MontarEspacoDoAtivo(fonte, new Vector2(x, y - PassoDaLinha * 3f - 10f));

        aviso = CriarTexto("Aviso do item", transform, fonte, tamanhoDaFonte + 14, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(-400f, -180f), new Vector2(800f, 100f));
        aviso.enabled = false;

        // O desenho do item em cima do nome, pra ligar o aviso ao icone que vai pra lateral.
        GameObject objIcone = new GameObject("Icone do aviso", typeof(RectTransform));
        objIcone.transform.SetParent(transform, false);
        RectTransform rtIcone = (RectTransform)objIcone.transform;
        rtIcone.anchorMin = rtIcone.anchorMax = new Vector2(0.5f, 1f);
        rtIcone.pivot = new Vector2(0.5f, 0f);
        rtIcone.anchoredPosition = new Vector2(0f, -170f);
        rtIcone.sizeDelta = new Vector2(64f, 64f);
        iconeDoAviso = objIcone.AddComponent<Image>();
        iconeDoAviso.preserveAspect = true;
        iconeDoAviso.raycastTarget = false;
        iconeDoAviso.enabled = false;

        if (estatisticas != null)
            PainelDeItens.Criar(estatisticas);

        if (inventario != null)
            inventario.AoMudar += AtualizarContadores;

        if (estatisticas != null)
        {
            estatisticas.AoPegarItem += Avisar;
            estatisticas.AoFormarSinergia += AvisarSinergia;
        }

        AtualizarContadores();
    }

    private void OnDestroy()
    {
        if (inventario != null)
            inventario.AoMudar -= AtualizarContadores;

        if (estatisticas != null)
        {
            estatisticas.AoPegarItem -= Avisar;
            estatisticas.AoFormarSinergia -= AvisarSinergia;
        }

        if (ativo != null)
            ativo.AoMudar -= AtualizarAtivo;
    }

    private void Update()
    {
        // O componente do ativo so aparece quando o jogador pega o primeiro.
        if (ativo == null && estatisticas != null && estatisticas.TryGetComponent(out ItemAtivoDoJogador achado))
        {
            ativo = achado;
            ativo.AoMudar += AtualizarAtivo;
            AtualizarAtivo();
        }

        // Pronto pra usar: a barra pulsa.
        if (cargaDoAtivo != null && ativo != null && ativo.Pronto)
        {
            float brilho = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f);
            cargaDoAtivo.color = new Color(0.45f * brilho, 1f * brilho, 0.55f * brilho);
        }

        // Pausado, o aviso some (senao ficava por cima do titulo "Pausado"); volta ao despausar.
        if (aviso != null)
        {
            bool parado = Time.timeScale <= 0f;

            if (avisoAtivo && Time.time >= fimDoAviso)
                avisoAtivo = false;

            aviso.enabled = avisoAtivo && !parado;
            iconeDoAviso.enabled = avisoAtivo && !parado && iconeDoAviso.sprite != null;
        }

        for (int i = 0; i < linhas.Length; i++)
        {
            if (linhas[i] == null)
                continue;

            float falta = Mathf.Clamp01((fimDoPulo[i] - Time.unscaledTime) / DuracaoDoPulo);
            linhas[i].localScale = Vector3.one * (1f + 0.3f * Mathf.Sin(falta * Mathf.PI));
        }
    }

    private void MontarEspacoDoAtivo(Font fonte, Vector2 posicao)
    {
        GameObject obj = new GameObject("Item ativo", typeof(RectTransform));
        obj.transform.SetParent(transform, false);
        espacoDoAtivo = (RectTransform)obj.transform;
        espacoDoAtivo.anchorMin = espacoDoAtivo.anchorMax = new Vector2(0f, 1f);
        espacoDoAtivo.pivot = new Vector2(0f, 1f);
        espacoDoAtivo.anchoredPosition = posicao;
        espacoDoAtivo.sizeDelta = new Vector2(LadoDoAtivo + 30f, LadoDoAtivo + 30f);

        Image moldura = Quadro("Moldura", espacoDoAtivo, Vector2.zero, new Vector2(LadoDoAtivo, LadoDoAtivo), new Color(0f, 0f, 0f, 0.55f));
        Outline borda = moldura.gameObject.AddComponent<Outline>();
        borda.effectColor = new Color(1f, 0.95f, 0.8f, 0.7f);
        borda.effectDistance = new Vector2(3f, 3f);

        iconeDoAtivo = Quadro("Icone", espacoDoAtivo, new Vector2(8f, -8f), new Vector2(LadoDoAtivo - 16f, LadoDoAtivo - 16f), Color.white);
        iconeDoAtivo.preserveAspect = true;

        fundoDaBarra = Quadro("Barra", espacoDoAtivo, new Vector2(LadoDoAtivo + 8f, 0f), new Vector2(12f, AlturaDaBarra), new Color(0f, 0f, 0f, 0.6f));
        cargaDoAtivo = Quadro("Carga", fundoDaBarra.rectTransform, Vector2.zero, new Vector2(12f, 0f), new Color(0.45f, 1f, 0.55f));
        RectTransform rtCarga = cargaDoAtivo.rectTransform;
        rtCarga.anchorMin = rtCarga.anchorMax = new Vector2(0f, 0f);
        rtCarga.pivot = new Vector2(0f, 0f);
        rtCarga.anchoredPosition = Vector2.zero;

        teclaDoAtivo = CriarTexto("Tecla", espacoDoAtivo, fonte, tamanhoDaFonte, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, -LadoDoAtivo - 4f), new Vector2(200f, 30f));
        teclaDoAtivo.text = Controle.EmUso ? "RT" : "Espaço";

        espacoDoAtivo.gameObject.SetActive(false);
    }

    private static Image Quadro(string nome, RectTransform pai, Vector2 posicao, Vector2 tamanho, Color cor)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;

        Image imagem = obj.AddComponent<Image>();
        imagem.color = cor;
        imagem.raycastTarget = false;
        return imagem;
    }

    private void AtualizarAtivo()
    {
        if (espacoDoAtivo == null || ativo == null)
            return;

        bool tem = ativo.Item != null;
        espacoDoAtivo.gameObject.SetActive(tem);

        if (!tem)
            return;

        Sprite icone = ArteImportada.IconeDoItem(ativo.Item.nome);
        iconeDoAtivo.sprite = icone != null ? icone : ArteGerada.Bola();
        iconeDoAtivo.color = icone != null ? Color.white : ativo.Item.cor;

        // Descarregado o icone fica apagado; a barra enche uma fatia por sala limpa.
        float fracao = ativo.CargasMaximas > 0 ? (float)ativo.Cargas / ativo.CargasMaximas : 0f;
        cargaDoAtivo.rectTransform.sizeDelta = new Vector2(12f, AlturaDaBarra * fracao);
        cargaDoAtivo.color = ativo.Pronto ? new Color(0.45f, 1f, 0.55f) : new Color(0.95f, 0.8f, 0.3f);

        if (!ativo.Pronto)
            iconeDoAtivo.color = new Color(iconeDoAtivo.color.r * 0.5f, iconeDoAtivo.color.g * 0.5f, iconeDoAtivo.color.b * 0.5f, 0.8f);

        teclaDoAtivo.text = Controle.EmUso ? "RT" : "Espaço";
    }

    private void AtualizarContadores()
    {
        if (moedas == null || inventario == null)
            return;

        Mostrar(0, moedas, inventario.Moedas);
        Mostrar(1, chaves, inventario.Chaves);
        Mostrar(2, bombas, inventario.Bombas);
    }

    private void Mostrar(int indice, Text texto, int valor)
    {
        // Na primeira vez so escreve; depois, qualquer mudanca da o pulinho.
        if (ultimos[indice] >= 0 && ultimos[indice] != valor)
            fimDoPulo[indice] = Time.unscaledTime + DuracaoDoPulo;

        ultimos[indice] = valor;
        texto.text = valor.ToString("00");
    }

    /// <summary>Sinergia nova: o mesmo aviso do item, em dourado e um pouco mais demorado.</summary>
    private void AvisarSinergia(ItemPassivo sinergia)
    {
        if (aviso == null)
            return;

        aviso.text = $"<color=#FFD966>Sinergia: {sinergia.nome}!</color>\n<size={tamanhoDaFonte}>{sinergia.descricao}</size>";
        avisoAtivo = true;
        aviso.enabled = true;
        iconeDoAviso.enabled = false;
        iconeDoAviso.sprite = null;
        fimDoAviso = Time.time + tempoDoAviso + 1f;
    }

    private void Avisar(ItemPassivo item)
    {
        if (aviso == null)
            return;

        aviso.text = $"{item.nome}\n<size={tamanhoDaFonte}>{item.descricao}</size>";
        avisoAtivo = true;
        aviso.enabled = true;

        Sprite icone = ArteImportada.IconeDoItem(item.nome);
        iconeDoAviso.sprite = icone;
        iconeDoAviso.enabled = icone != null;

        fimDoAviso = Time.time + tempoDoAviso;
    }

    /// <summary>
    /// Uma linha da coluna: a pixel art do coletavel e o numero do lado. A linha inteira e
    /// um objeto so, pra o pulinho crescer icone e numero juntos.
    /// </summary>
    private Text Contador(int indice, string nome, TipoDeColetavel tipo, Font fonte, Vector2 posicao)
    {
        GameObject objLinha = new GameObject(nome, typeof(RectTransform));
        objLinha.transform.SetParent(transform, false);

        RectTransform linha = (RectTransform)objLinha.transform;
        linha.anchorMin = linha.anchorMax = new Vector2(0f, 1f);
        linha.pivot = new Vector2(0f, 0.5f);   // cresce a partir da esquerda, sem sair da coluna
        linha.anchoredPosition = posicao - new Vector2(0f, LadoDoIcone * 0.5f);
        linha.sizeDelta = new Vector2(LadoDoIcone + 80f, LadoDoIcone);
        linhas[indice] = linha;

        GameObject obj = new GameObject("Icone", typeof(RectTransform));
        obj.transform.SetParent(linha, false);

        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(LadoDoIcone, LadoDoIcone);

        Image icone = obj.AddComponent<Image>();
        icone.sprite = ArteGerada.Coletavel(tipo);
        icone.preserveAspect = true;   // a dinamite e comprida: sem isto ficava achatada
        icone.raycastTarget = false;

        // Contorno claro: a bomba e escura e sumia no chao escuro.
        Outline contorno = obj.AddComponent<Outline>();
        contorno.effectColor = new Color(1f, 1f, 1f, 0.55f);
        contorno.effectDistance = new Vector2(2f, 2f);

        Text numero = CriarTexto("Numero", linha, fonte, tamanhoDosContadores, TextAnchor.MiddleLeft,
            new Vector2(0f, 0.5f), new Vector2(LadoDoIcone + 10f, LadoDoIcone * 0.5f), new Vector2(80f, LadoDoIcone));

        // Numero branco e grosso com sombra: le em cima de qualquer chao, no meio da briga.
        numero.fontStyle = FontStyle.Bold;
        Shadow sombra = numero.gameObject.AddComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.9f);
        sombra.effectDistance = new Vector2(3f, -3f);
        return numero;
    }

    private Text CriarTexto(string nome, Transform pai, Font fonte, int tamanho, TextAnchor alinhamento,
                           Vector2 ancora, Vector2 posicao, Vector2 area)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);

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
