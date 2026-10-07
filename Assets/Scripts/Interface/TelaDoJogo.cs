using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// A interface do jogo, num objeto so (o "Interface" da cena): o HUD durante o jogo
/// (<see cref="HudDoJogo"/>) e as telas que param o jogo:
/// - o menu inicial (Jogar, Controles, Sair), por cima da primeira caverna;
/// - a pausa (Esc ou Start): Continuar, Controles, Recomecar, Menu inicial, Sair;
/// - os controles (teclado e controle);
/// - o fim da partida (morreu ou venceu), com o andar, os inimigos derrotados e o tempo.
///
/// Parar o jogo e o tempo parado (Time.timeScale = 0): o jogador e os inimigos ficam congelados e os
/// controles do jogador nao valem. Nos menus da pra usar o mouse, o teclado (setas ou W/S, Enter) ou o
/// controle (direcional, A pra escolher, B pra voltar).
///
/// Tudo e desenhado no OnGUI com a arte de interface (molduras, botoes, a barra dourada, o ponteiro)
/// e as fontes em pixel, em tamanhos inteiros (<see cref="Desenho"/>).
/// </summary>
[DisallowMultipleComponent]
public class TelaDoJogo : MonoBehaviour
{
    private enum Estado { Menu, Jogando, Pausado, Controles, Fim }

    [Header("Fontes")]
    [Tooltip("Texto comum (Jersey 15)")]
    public Font fonteTexto;

    [Tooltip("Titulos (Jacquard 12)")]
    public Font fonteTitulo;

    [Header("Imagens")]
    public Texture2D coracao;
    public Texture2D bolsa;
    public Texture2D molduraPequena;
    public Texture2D molduraGrande;
    public Texture2D barraDourada;

    [Tooltip("Os 4 jeitos do botao lado a lado (normal, escolhido, ...)")]
    public Texture2D botao;

    [Tooltip("O ponteiro animado que aponta o botao escolhido (quadros lado a lado, quadrados)")]
    public Texture2D ponteiro;

    [Header("Sons")]
    [SerializeField] private AudioClip somDeMover;
    [SerializeField] private AudioClip somDeEscolher;
    [SerializeField] private AudioClip somDeAbrir;
    [SerializeField] private AudioClip somDeFechar;
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;

    /// <summary>A tela da cena (pra morte do jogador e a vitoria chamarem o fim).</summary>
    public static TelaDoJogo Atual { get; private set; }

    /// <summary>O jogo esta parado num menu.</summary>
    public bool Parado => estado != Estado.Jogando;

    private static readonly string[] OpcoesDoMenu = { "Jogar", "Controles", "Sair" };
    private static readonly string[] OpcoesDaPausa = { "Continuar", "Controles", "Recomeçar", "Menu inicial", "Sair" };
    private static readonly string[] OpcoesDoFim = { "Jogar de novo", "Menu inicial" };
    private static readonly string[] OpcoesDosControles = { "Voltar" };

    private static readonly string[,] Controles =
    {
        { "Andar", "W A S D ou setas", "analógico esquerdo" },
        { "Mirar", "mouse", "analógico direito" },
        { "Atirar", "botão esquerdo", "RT ou RB" },
        { "Esquivar", "espaço, shift ou botão direito", "LT, LB ou A" },
        { "Trocar de arma", "Q ou rodinha", "Y" },
        { "Recarregar", "R", "X" },
        { "Pegar, abrir", "E", "B" },
        { "Pausar", "Esc", "Start" },
    };

    private HudDoJogo hud;
    private GeradorDoAndar gerador;
    private Vida vidaDoJogador;
    private ArmaDoJogador armasDoJogador;
    private InteracaoDoJogador interacao;
    private AudioSource audioSource;

    private Estado estado = Estado.Menu;
    private Estado voltarDosControles;
    private int escolhido;
    private bool venceu;
    private readonly List<Rect> botoes = new List<Rect>();
    private int clicado = -1;

    private InputAction navegar;
    private InputAction confirmar;
    private InputAction voltar;
    private InputAction pausar;
    private float ultimoY;

    private void Awake()
    {
        Atual = this;
        hud = new HudDoJogo(this);
        audioSource = GetComponent<AudioSource>();

        navegar = new InputAction("Navegar", InputActionType.Value);
        navegar.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        navegar.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        navegar.AddBinding("<Gamepad>/dpad");
        navegar.AddBinding("<Gamepad>/leftStick");

        confirmar = new InputAction("Confirmar", InputActionType.Button, "<Keyboard>/enter");
        confirmar.AddBinding("<Keyboard>/numpadEnter");
        confirmar.AddBinding("<Keyboard>/space");
        confirmar.AddBinding("<Gamepad>/buttonSouth");

        voltar = new InputAction("Voltar", InputActionType.Button, "<Gamepad>/buttonEast");

        pausar = new InputAction("Pausar", InputActionType.Button, "<Keyboard>/escape");
        pausar.AddBinding("<Gamepad>/start");
    }

    private void OnEnable()
    {
        navegar.Enable();
        confirmar.Enable();
        voltar.Enable();
        pausar.Enable();
    }

    private void OnDisable()
    {
        navegar.Disable();
        confirmar.Disable();
        voltar.Disable();
        pausar.Disable();
    }

    private void OnDestroy()
    {
        navegar.Dispose();
        confirmar.Dispose();
        voltar.Dispose();
        pausar.Dispose();

        if (Atual == this)
            Atual = null;

        // Saiu da cena com o jogo parado: o tempo volta ao normal.
        Time.timeScale = 1f;
    }

    private void Start()
    {
        gerador = FindAnyObjectByType<GeradorDoAndar>();
        GameObject jogador = GameObject.FindWithTag("Player");

        if (jogador != null)
        {
            vidaDoJogador = jogador.GetComponent<Vida>();
            armasDoJogador = jogador.GetComponent<ArmaDoJogador>();
            interacao = jogador.GetComponent<InteracaoDoJogador>();
        }

        // "Jogar de novo" recarrega a cena e cai direto no jogo; senao, o menu inicial.
        if (Partida.PularMenu)
        {
            Partida.PularMenu = false;
            Comecar();
        }
        else
        {
            Mudar(Estado.Menu);
        }
    }

    // ---------------- os estados ----------------
    private void Mudar(Estado novo)
    {
        estado = novo;
        escolhido = 0;
        Time.timeScale = novo == Estado.Jogando ? 1f : 0f;
    }

    private void Comecar()
    {
        Partida.Zerar();
        Mudar(Estado.Jogando);

        if (gerador != null)
        {
            Partida.Andar = gerador.Andar;
            Partida.NomeDoAndar = gerador.NomeNaTela;
            gerador.MostrarNome();
        }
    }

    private void Pausar()
    {
        Tocar(somDeAbrir);
        Mudar(Estado.Pausado);
    }

    /// <summary>A partida acabou: a tela do fim (o jogo para).</summary>
    public void Fim(bool ganhou)
    {
        venceu = ganhou;
        Mudar(Estado.Fim);
    }

    private static void Recarregar(bool direto)
    {
        Partida.PularMenu = direto;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private static void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private string[] Opcoes()
    {
        switch (estado)
        {
            case Estado.Menu: return OpcoesDoMenu;
            case Estado.Pausado: return OpcoesDaPausa;
            case Estado.Fim: return OpcoesDoFim;
            case Estado.Controles: return OpcoesDosControles;
            default: return new string[0];
        }
    }

    private void Escolher(string opcao)
    {
        Tocar(somDeEscolher);

        switch (opcao)
        {
            case "Jogar":
                Comecar();
                break;
            case "Continuar":
                Mudar(Estado.Jogando);
                break;
            case "Controles":
                voltarDosControles = estado;
                Mudar(Estado.Controles);
                break;
            case "Voltar":
                Mudar(voltarDosControles);
                break;
            case "Recomeçar":
            case "Jogar de novo":
                Recarregar(true);
                break;
            case "Menu inicial":
                Recarregar(false);
                break;
            case "Sair":
                Sair();
                break;
        }
    }

    // ---------------- os controles dos menus ----------------
    private void Update()
    {
        if (estado == Estado.Jogando)
        {
            Partida.Tempo += Time.deltaTime;

            bool vivo = vidaDoJogador == null || !vidaDoJogador.Morto;

            if (vivo && pausar.WasPressedThisFrame() && (gerador == null || !gerador.Trocando))
                Pausar();

            return;
        }

        string[] opcoes = Opcoes();

        if (opcoes.Length == 0)
            return;

        // Sobe e desce uma opcao por toque (o analogico tambem: precisa voltar pro meio pra andar de novo).
        float y = navegar.ReadValue<Vector2>().y;

        if (Mathf.Abs(y) > 0.5f && Mathf.Abs(ultimoY) <= 0.5f)
        {
            escolhido = (escolhido + (y < 0f ? 1 : -1) + opcoes.Length) % opcoes.Length;
            Tocar(somDeMover);
        }

        ultimoY = y;

        if (clicado >= 0 && clicado < opcoes.Length)
        {
            escolhido = clicado;
            clicado = -1;
            Escolher(opcoes[escolhido]);
            return;
        }

        if (confirmar.WasPressedThisFrame())
        {
            Escolher(opcoes[Mathf.Clamp(escolhido, 0, opcoes.Length - 1)]);
            return;
        }

        bool voltou = voltar.WasPressedThisFrame() || pausar.WasPressedThisFrame();

        if (voltou && estado == Estado.Pausado)
        {
            Tocar(somDeFechar);
            Mudar(Estado.Jogando);
        }
        else if (voltou && estado == Estado.Controles)
        {
            Tocar(somDeFechar);
            Mudar(voltarDosControles);
        }
    }

    private void Tocar(AudioClip som)
    {
        if (audioSource != null && som != null)
            audioSource.PlayOneShot(som, volume);
    }

    // ---------------- desenho ----------------
    private void OnGUI()
    {
        // Por cima de tudo que os outros desenham (a seta do andar, o escuro da morte).
        GUI.depth = -10;
        int u = Desenho.U;

        if (estado != Estado.Menu && estado != Estado.Fim)
            hud.Desenhar(gerador, vidaDoJogador, armasDoJogador, interacao);

        // O escuro da troca de andar.
        if (gerador != null && gerador.Escuro > 0f)
            Desenho.Cor(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, gerador.Escuro));

        botoes.Clear();

        switch (estado)
        {
            case Estado.Menu:
                Desenho.Cor(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.03f, 0.03f, 0.07f, 0.72f));
                Titulo("The Prettie", Screen.height * 0.16f, 64 * u, Desenho.Dourado);
                Subtitulo("Desça a prisão. Derrote os chefes.", Screen.height * 0.16f + 70 * u, Desenho.Apagado);
                Botoes(OpcoesDoMenu, Screen.height * 0.5f);
                Rodape();
                break;

            case Estado.Pausado:
                Desenho.Cor(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.03f, 0.03f, 0.07f, 0.6f));
                Rect painel = Painel(new Vector2(220 * u, 300 * u));
                Titulo("Pausado", painel.y + 40 * u, 36 * u, Desenho.Dourado);
                Botoes(OpcoesDaPausa, painel.y + 70 * u);
                break;

            case Estado.Controles:
                Desenho.Cor(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.03f, 0.03f, 0.07f, 0.8f));
                TelaDosControles(u);
                break;

            case Estado.Fim:
                Desenho.Cor(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.02f, 0.01f, 0.04f, 0.8f));
                TelaDoFim(u);
                break;
        }

        // Clique do mouse num botao (o Update escolhe no proximo quadro, junto com o teclado).
        Event e = Event.current;

        for (int i = 0; i < botoes.Count; i++)
        {
            if (!botoes[i].Contains(e.mousePosition))
                continue;

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                if (escolhido != i)
                    Tocar(somDeMover);

                escolhido = i;
            }
            else if (e.type == EventType.MouseDown && e.button == 0)
            {
                clicado = i;
                e.Use();
            }
        }
    }

    private void Titulo(string texto, float y, int tamanho, Color cor)
    {
        GUIStyle estilo = Desenho.Estilo(fonteTitulo, tamanho, TextAnchor.MiddleCenter);
        Desenho.Texto(new Rect(0f, y - tamanho * 0.6f, Screen.width, tamanho * 1.2f), texto, estilo, cor);
    }

    private void Subtitulo(string texto, float y, Color cor)
    {
        int u = Desenho.U;
        GUIStyle estilo = Desenho.Estilo(fonteTexto, 20 * u, TextAnchor.MiddleCenter);
        Desenho.Texto(new Rect(0f, y - 12 * u, Screen.width, 24 * u), texto, estilo, cor);
    }

    private void Rodape()
    {
        int u = Desenho.U;
        GUIStyle estilo = Desenho.Estilo(fonteTexto, 14 * u, TextAnchor.LowerRight);
        Desenho.Texto(new Rect(0f, Screen.height - 24 * u, Screen.width - 8 * u, 20 * u), "versão " + Application.version, estilo, Desenho.Apagado);
    }

    // A moldura grande do pacote, com o meio (azul no desenho) pintado de escuro: o azul comeca a 9
    // pixels da beirada dos lados, 15 em cima e 16 embaixo.
    private Rect Painel(Vector2 tamanho)
    {
        int u = Desenho.U;
        Rect onde = new Rect((Screen.width - tamanho.x) * 0.5f, (Screen.height - tamanho.y) * 0.5f, tamanho.x, tamanho.y);

        if (molduraGrande != null)
            Desenho.Moldura(onde, molduraGrande, new Rect(0f, 0f, molduraGrande.width, molduraGrande.height), 24, Color.white);

        Desenho.Cor(new Rect(onde.x + 9 * u, onde.y + 15 * u, onde.width - 19 * u, onde.height - 31 * u), new Color(0.07f, 0.06f, 0.12f, 1f));
        return onde;
    }

    // Os botoes em coluna, com o escolhido aceso e o ponteiro do lado.
    private void Botoes(string[] opcoes, float comeco)
    {
        int u = Desenho.U;
        float altura = 35 * u, passo = 40 * u;
        GUIStyle estilo = Desenho.Estilo(fonteTexto, 22 * u, TextAnchor.MiddleCenter);
        float largura = 150 * u;

        // Todos do mesmo tamanho, cabendo o texto mais comprido (com folga pras pontas do losango).
        foreach (string opcao in opcoes)
            largura = Mathf.Max(largura, Desenho.Largura(opcao, estilo) + 48 * u);

        escolhido = Mathf.Clamp(escolhido, 0, opcoes.Length - 1);

        for (int i = 0; i < opcoes.Length; i++)
        {
            Rect onde = new Rect((Screen.width - largura) * 0.5f, comeco + i * passo, largura, altura);
            bool aceso = i == escolhido;
            botoes.Add(onde);

            if (botao != null)
            {
                // A folha tem 4 botoes lado a lado: o 0 e o normal, o 1 o escolhido.
                float w = botao.width / 4f;
                Desenho.Moldura(onde, botao, new Rect((aceso ? 1 : 0) * w, 0f, w, botao.height), 12, Color.white);
            }
            else
            {
                Desenho.Cor(onde, aceso ? new Color(0.9f, 0.6f, 0.2f) : new Color(0.3f, 0.32f, 0.4f));
            }

            Desenho.Texto(onde, opcoes[i], estilo, aceso ? Color.white : Desenho.Claro);

            if (aceso && ponteiro != null)
            {
                int quadros = Mathf.Max(1, ponteiro.width / ponteiro.height);
                int quadro = Mathf.FloorToInt(Time.unscaledTime * 8f) % quadros;
                float lado = ponteiro.height * u;
                Desenho.Parte(new Rect(onde.x - lado - 4 * u, onde.center.y - lado * 0.5f, lado, lado), ponteiro,
                              new Rect(quadro * ponteiro.height, 0f, ponteiro.height, ponteiro.height), Color.white);
            }
        }
    }

    private void TelaDosControles(int u)
    {
        Titulo("Controles", Screen.height * 0.12f, 40 * u, Desenho.Dourado);

        GUIStyle cabeca = Desenho.Estilo(fonteTexto, 18 * u, TextAnchor.MiddleLeft);
        GUIStyle linha = Desenho.Estilo(fonteTexto, 20 * u, TextAnchor.MiddleLeft);
        float coluna = Mathf.Min(Screen.width * 0.28f, 200 * u);
        float x0 = (Screen.width - coluna * 3f) * 0.5f;
        float y = Screen.height * 0.22f;

        Desenho.Texto(new Rect(x0 + coluna, y, coluna, 24 * u), "Teclado e mouse", cabeca, Desenho.Apagado);
        Desenho.Texto(new Rect(x0 + coluna * 2f, y, coluna, 24 * u), "Controle", cabeca, Desenho.Apagado);
        y += 30 * u;

        for (int i = 0; i < Controles.GetLength(0); i++)
        {
            Desenho.Texto(new Rect(x0, y, coluna, 24 * u), Controles[i, 0], linha, Desenho.Dourado);
            Desenho.Texto(new Rect(x0 + coluna, y, coluna, 24 * u), Controles[i, 1], linha, Desenho.Claro);
            Desenho.Texto(new Rect(x0 + coluna * 2f, y, coluna, 24 * u), Controles[i, 2], linha, Desenho.Claro);
            y += 26 * u;
        }

        Botoes(OpcoesDosControles, y + 20 * u);
    }

    private void TelaDoFim(int u)
    {
        Titulo(venceu ? "Você venceu!" : "Você morreu", Screen.height * 0.2f, 56 * u, venceu ? Desenho.Dourado : Desenho.Vermelho);

        int minutos = Mathf.FloorToInt(Partida.Tempo / 60f);
        int segundos = Mathf.FloorToInt(Partida.Tempo % 60f);
        string[] linhas =
        {
            venceu ? "A prisão caiu." : "Chegou até: " + Partida.NomeDoAndar,
            $"Inimigos derrotados: {Partida.InimigosMortos}",
            $"Tempo: {minutos}:{segundos:00}",
        };

        GUIStyle estilo = Desenho.Estilo(fonteTexto, 22 * u, TextAnchor.MiddleCenter);
        float y = Screen.height * 0.2f + 60 * u;

        foreach (string linha in linhas)
        {
            Desenho.Texto(new Rect(0f, y, Screen.width, 26 * u), linha, estilo, Desenho.Claro);
            y += 28 * u;
        }

        Botoes(OpcoesDoFim, y + 30 * u);
    }
}
