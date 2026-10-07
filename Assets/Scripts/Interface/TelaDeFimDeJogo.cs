using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// A tela de quando a partida acaba (morte ou vitoria), em tres tempos:
///
///   1. o golpe: a tela pisca (vermelho na morte, dourado na vitoria), o jogo entra em
///      camera lenta e escurece enquanto o heroi cai;
///   2. o titulo desce com um tranco e o resumo da partida aparece linha a linha: heroi,
///      andar, tempo, inimigos derrotados, chefes derrotados e as armas;
///   3. os botoes sobem: Tentar de novo (R), Menu principal (Q) e Sair do jogo.
///
/// W/S ou Cima/Baixo escolhem, Enter / Espaco / A apertam, o mouse clica. Esc vai pro
/// "Sair do jogo" (de novo: sai). Com a tela inteira, o jogo congela.
///
/// Canvas proprio, montado por codigo. A <see cref="MorteDoJogador"/> chama <see cref="Mostrar"/>
/// quando a vida do jogador chega a zero; o <see cref="GeradorDoAndar"/> chama
/// <see cref="MostrarVitoria"/> depois do ultimo andar. Veio do jogo antigo.
/// </summary>
[DisallowMultipleComponent]
public class TelaDeFimDeJogo : MonoBehaviour
{
    [Tooltip("Segundos depois da morte ate o resumo aparecer: da pra ver o heroi cair")]
    [SerializeField, Min(0f)] private float atraso = 1.3f;

    [SerializeField, Min(0.01f)] private float tempoDoFade = 0.5f;

    [Tooltip("Velocidade do jogo durante o golpe (1 = normal)")]
    [SerializeField, Range(0.05f, 1f)] private float cameraLenta = 0.4f;

    [Tooltip("Com a tela inteira, o jogo congela (Time.timeScale = 0)")]
    [SerializeField] private bool congelarOJogo = true;

    private const float IntervaloDasLinhas = 0.1f;

    private CanvasGroup conteudo;
    private Image clarao;
    private Image fundo;
    private Color corDoClarao;
    private Color corDoFundo;
    private Text titulo;
    private float yDoTitulo;
    private readonly List<CanvasGroup> linhas = new List<CanvasGroup>();
    private MenuDeBotoes menu;
    private int botaoSair;
    private Image retrato;
    private Sprite[] quadrosDoRetrato;
    private bool retratoEmLoop;
    private float desde;
    private bool congelou;
    private bool vitoria;

    public static TelaDeFimDeJogo Atual { get; private set; }

    public static TelaDeFimDeJogo Mostrar(GeradorDoAndar andar) => Criar(andar, false);

    /// <summary>A mesma tela, mas de vitoria: o jogador venceu o chefe final.</summary>
    public static TelaDeFimDeJogo MostrarVitoria(GeradorDoAndar andar) => Criar(andar, true);

    private static TelaDeFimDeJogo Criar(GeradorDoAndar andar, bool vitoria)
    {
        if (Atual != null)
            return Atual;

        TelaDeFimDeJogo tela = new GameObject(vitoria ? "Tela de vitoria" : "Tela de fim de jogo").AddComponent<TelaDeFimDeJogo>();
        tela.vitoria = vitoria;

        if (vitoria)
        {
            tela.atraso = 1.5f;
            tela.cameraLenta = 0.6f;
            Sons.Tocar(Som.Vitoria);
        }

        Musica.Tocar(vitoria ? TemaMusical.Vitoria : TemaMusical.FimDeJogo);
        tela.Montar(andar);
        return tela;
    }

    /// <summary>
    /// Recarrega a cena aberta: tudo volta como no Play. Funciona mesmo com a cena fora do
    /// Build Settings quando roda no editor (a cena de teste do andar costuma estar fora).
    /// Corte seco: pra escurecer antes, passe pela <see cref="TransicaoDeTela"/>.
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

        // Nunca deixa o jogo lento ou congelado pra tras (sair do Play, trocar de cena).
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // A tela pode ficar sem o menu se a cena for recarregada com ela aberta: so espera sumir.
        if (menu == null)
            return;

        float t = Time.unscaledTime - desde;

        Golpe(t);
        Revelar(t - atraso);
        AnimarRetrato(t - atraso);

        if (!menu.Pronto || TransicaoDeTela.Ocupada)
        {
            menu.Atualizar();
            return;
        }

        // Esc nao fecha direto: primeiro vai pro botao de sair, e o segundo Esc sai.
        if (Input.GetKeyDown(KeyCode.Escape) || Controle.Apertou(BotaoDoControle.Select))
        {
            if (menu.Escolhido == botaoSair)
                menu.Apertar(botaoSair);
            else
                menu.Escolher(botaoSair);
        }
        else if (Input.GetKeyDown(KeyCode.R) || Controle.Apertou(BotaoDoControle.Start))
        {
            menu.Apertar(0);
        }
        else if (Input.GetKeyDown(KeyCode.Q) || Controle.Apertou(BotaoDoControle.Y))
        {
            menu.Apertar(1);
        }

        menu.Atualizar();
    }

    /// <summary>O instante da morte: clarao, camera lenta e a tela escurecendo.</summary>
    private void Golpe(float t)
    {
        clarao.color = new Color(corDoClarao.r, corDoClarao.g, corDoClarao.b, corDoClarao.a * Mathf.Exp(-t * 5f));

        float escuro = Mathf.Clamp01(t / (atraso + tempoDoFade));
        fundo.color = new Color(corDoFundo.r, corDoFundo.g, corDoFundo.b, corDoFundo.a * escuro);

        if (congelou)
            return;

        if (t < atraso)
        {
            // Freia rapido ate a camera lenta, e fica nela enquanto o heroi cai.
            Time.timeScale = Mathf.Lerp(1f, cameraLenta, Mathf.Clamp01(t / 0.15f));
            return;
        }

        if (congelarOJogo)
        {
            congelou = true;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    /// <summary>Depois do golpe: titulo com tranco, linhas do resumo uma a uma.</summary>
    private void Revelar(float t)
    {
        conteudo.alpha = Mathf.Clamp01(t / 0.2f);

        float queda = TelaSimples.PassaEVolta(t / 0.45f);
        titulo.transform.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, queda);
        titulo.rectTransform.anchoredPosition = new Vector2(0f, yDoTitulo + (1f - queda) * 60f);

        for (int i = 0; i < linhas.Count; i++)
        {
            float a = TelaSimples.Freando((t - 0.35f - i * IntervaloDasLinhas) / 0.25f);
            linhas[i].alpha = a;
            linhas[i].transform.localPosition = new Vector3((1f - a) * -40f, 0f, 0f);
        }
    }

    // ---------------- montagem ----------------
    private void Montar(GeradorDoAndar andar)
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20; // por cima da HUD, do minimapa e da barra do chefe

        CanvasScaler escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 1f;

        corDoClarao = vitoria ? new Color(1f, 0.85f, 0.35f, 0.45f) : new Color(0.9f, 0.05f, 0.05f, 0.55f);
        corDoFundo = vitoria ? new Color(0.02f, 0.05f, 0.02f, 0.88f) : new Color(0.07f, 0f, 0f, 0.9f);
        clarao = Cobrir("Clarao");
        fundo = Cobrir("Fundo");

        conteudo = TelaSimples.Camada(transform, "Conteudo");
        conteudo.alpha = 0f;
        Transform pai = conteudo.transform;

        // Dragon Regalia: faixa atras do titulo (rosa na vitoria, azul na morte) e moldura
        // dourada no resumo. Criadas antes dos textos: ficam atras deles.
        yDoTitulo = 335f;
        TelaSimples.Painel(pai, "Painel do resumo", ArteDaInterface.MolduraGrande, 40f, new Vector2(1200f, 390f));
        TelaSimples.Faixa(pai, "Faixa do titulo", vitoria ? ArteDaInterface.FaixaRosa : ArteDaInterface.FaixaAzul, yDoTitulo, 900f);
        titulo = TelaSimples.Titulo(pai, "Titulo", 140, vitoria ? new Color(1f, 0.9f, 0.4f) : new Color(1f, 0.32f, 0.3f),
                                    yDoTitulo, vitoria ? "Você venceu!" : "Você morreu");

        MontarRetrato(pai);
        MontarResumo(pai, andar);

        // Os botoes sobem depois da ultima linha do resumo.
        menu = new MenuDeBotoes(pai, 0f, new Vector2(500f, 76f), 34)
        {
            AtrasoDaEntrada = atraso + 0.35f + linhas.Count * IntervaloDasLinhas,
        };
        menu.Adicionar(vitoria ? "Jogar de novo" : "Tentar de novo", -268f, TentarDeNovo, "[R] || [Pad Start]");
        menu.Adicionar("Menu principal", -352f, IrAoMenu, "[Q] || [Pad Y]");
        botaoSair = menu.Adicionar("Sair do jogo", -436f, TelaDeInicio.SairDoJogo, "[Esc] || [Pad Select]", IconeDoBotao.Sair, perigo: true);
    }

    private void MontarResumo(Transform pai, GeradorDoAndar andar)
    {
        Herois.Heroi heroi = Herois.Atual;
        string andarAlcancado = andar == null ? "?"
            : vitoria ? $"Os {andar.Andares} andares da prisão"
            : andar.NomeNaTela;

        string[,] dados =
        {
            { "Herói", heroi != null ? heroi.Nome : "?" },
            { vitoria ? "Venceu" : "Chegou até", andarAlcancado },
            { "Tempo", ResumoDaPartida.TempoFormatado() },
            { "Inimigos derrotados", ResumoDaPartida.InimigosDerrotados.ToString() },
            { "Chefes derrotados", ResumoDaPartida.ChefesDerrotados.ToString() },
        };

        for (int i = 0; i < dados.GetLength(0); i++)
            LinhaDeDado(pai, 175f - i * 48f, dados[i, 0], dados[i, 1]);

        // As armas que o jogador tinha na mao (no jogo antigo eram os itens pegos).
        Text armas = LinhaDeTexto(pai, "Armas", -76f, 24, new Color(0.85f, 0.85f, 0.85f), "");
        armas.rectTransform.sizeDelta = new Vector2(1080f, 64f);
        armas.text = "Armas: " + ListaDeArmas(Armas(andar));
    }

    /// <summary>"Nome ........ valor": o nome alinhado a direita, o valor a esquerda.</summary>
    private void LinhaDeDado(Transform pai, float y, string nome, string valor)
    {
        CanvasGroup linha = TelaSimples.Camada(pai, "Linha " + nome);
        linhas.Add(linha);

        Text rotulo = TelaSimples.Texto(linha.transform, "Nome", 28, new Color(0.75f, 0.8f, 0.95f), y, nome);
        rotulo.alignment = TextAnchor.MiddleRight;
        rotulo.rectTransform.pivot = new Vector2(1f, 0.5f);
        rotulo.rectTransform.sizeDelta = new Vector2(420f, 60f);
        rotulo.rectTransform.anchoredPosition = new Vector2(70f, y);

        Text texto = TelaSimples.Texto(linha.transform, "Valor", 30, Color.white, y, valor);
        texto.alignment = TextAnchor.MiddleLeft;
        texto.rectTransform.pivot = new Vector2(0f, 0.5f);
        texto.rectTransform.sizeDelta = new Vector2(480f, 60f);
        texto.rectTransform.anchoredPosition = new Vector2(100f, y);
    }

    private Text LinhaDeTexto(Transform pai, string nome, float y, int tamanho, Color cor, string conteudo)
    {
        CanvasGroup linha = TelaSimples.Camada(pai, "Linha " + nome);
        linhas.Add(linha);

        Text texto = TelaSimples.Texto(linha.transform, nome, tamanho, cor, y, conteudo);
        texto.rectTransform.sizeDelta = new Vector2(1080f, tamanho * 3f);
        return texto;
    }

    /// <summary>O heroi na esquerda do resumo: caindo (morte) ou parado respirando (vitoria).</summary>
    private void MontarRetrato(Transform pai)
    {
        Herois.Heroi heroi = Herois.Atual;
        Sprite[] morte = Herois.Morte(heroi);

        retratoEmLoop = vitoria || morte.Length == 0;
        quadrosDoRetrato = retratoEmLoop ? Herois.Parado(heroi) : morte;

        if (quadrosDoRetrato == null || quadrosDoRetrato.Length == 0)
            return;

        GameObject obj = new GameObject("Retrato", typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-400f, 70f);

        // Os do Tiny RPG tem o corpo pequeno no meio de um quadro de 100 px: o quadro vai grande.
        rt.sizeDelta = Vector2.one * 380f;

        retrato = obj.AddComponent<Image>();
        retrato.preserveAspect = true;
        retrato.raycastTarget = false;
        retrato.sprite = quadrosDoRetrato[0];
    }

    private void AnimarRetrato(float t)
    {
        if (retrato == null || t < 0f)
            return;

        int quadro = Mathf.FloorToInt(t * 8f);
        retrato.sprite = quadrosDoRetrato[retratoEmLoop ? quadro % quadrosDoRetrato.Length
                                                        : Mathf.Min(quadro, quadrosDoRetrato.Length - 1)];
    }

    private Image Cobrir(string nome)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(transform, false);
        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        Image imagem = obj.AddComponent<Image>();
        imagem.color = Color.clear;
        imagem.raycastTarget = false;
        return imagem;
    }

    private static void TentarDeNovo()
    {
        Sons.Tocar(Som.MenuConfirmar, 1f, 0f);
        TransicaoDeTela.Trocar(RecarregarCena);
    }

    private static void IrAoMenu()
    {
        Sons.Tocar(Som.MenuFechar, 1f, 0f);
        TelaDeInicio.VoltarAoMenu();
    }

    private static ArmaDoJogador Armas(GeradorDoAndar andar)
    {
        return andar != null && andar.Jogador != null ? andar.Jogador.GetComponent<ArmaDoJogador>() : null;
    }

    /// <summary>As armas que o jogador carrega, a da mao primeiro ("Varinha de Faíscas   Arco do Arqueiro").</summary>
    internal static string ListaDeArmas(ArmaDoJogador armas)
    {
        if (armas == null || armas.Atual == null)
            return "<color=#aaaaaa>nenhuma</color>";

        string lista = $"<color=#FFD966>{armas.Atual.Dados.nome}</color>";

        if (armas.Outra != null)
            lista += $"   <color=#cccccc>{armas.Outra.Dados.nome}</color>";

        return lista;
    }

    /// <summary>A fonte de texto do jogo. Mantido pra quem ja chamava daqui; o certo e <see cref="FonteDoJogo.Texto"/>.</summary>
    internal static Font Fonte() => FonteDoJogo.Texto;
}
