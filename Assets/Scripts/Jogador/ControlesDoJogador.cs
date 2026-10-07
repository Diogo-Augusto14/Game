using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tudo o que o jogador aperta, num lugar so. O resto do jogo pergunta pra ca e nunca le
/// teclado, mouse ou controle direto: trocar uma tecla mexe so neste arquivo.
///
///   andar     WASD ou setas          | analogico esquerdo ou cruz
///   mirar     mouse                  | analogico direito (solto: mira pra onde anda)
///   atirar    botao esquerdo         | RT ou RB
///   esquiva   espaco, shift ou botao direito | LT, LB ou A
///   trocar    Q ou a roda do mouse   | Y
///   recarregar R                     | X
///   interagir E (pegar arma, abrir bau) | B
///
/// Usa o Input System (o pacote novo da Unity), com as acoes montadas aqui no codigo.
/// </summary>
[DisallowMultipleComponent]
public class ControlesDoJogador : MonoBehaviour
{
    [Tooltip("Segundos que um aperto de esquiva fica guardado: apertar um pouco antes da anterior acabar ainda vale")]
    [SerializeField, Min(0f)] private float memoriaDaEsquiva = 0.15f;

    [Tooltip("Analogico direito: abaixo disto conta como solto")]
    [SerializeField, Range(0f, 1f)] private float zonaMortaDaMira = 0.35f;

    private InputAction mover;
    private InputAction mirar;
    private InputAction atirar;
    private InputAction esquivar;
    private InputAction ponteiro;
    private InputAction trocar;
    private InputAction recarregar;
    private InputAction interagir;
    private InputAction habilidade;

    private float esquivaPedidaEm = -10f;
    private Vector2 ultimoMouse;
    private Camera cam;

    /// <summary>Pra onde andar, tamanho ate 1 (analogico pela metade anda devagar).</summary>
    public Vector2 Movimento { get; private set; }

    /// <summary>O botao de atirar esta segurado.</summary>
    public bool Atirando { get; private set; }

    /// <summary>Pra onde o jogador mira, tamanho 1.</summary>
    public Vector2 Mira { get; private set; } = Vector2.right;

    /// <summary>O mouse foi o ultimo a mirar (a camera olha pra frente na direcao do cursor).</summary>
    public bool MiraPeloMouse { get; private set; } = true;

    /// <summary>O ponto do mundo debaixo do cursor.</summary>
    public Vector2 PontoDoMouse { get; private set; }

    /// <summary>Apertou pra trocar de arma neste quadro (Q, roda do mouse ou Y).</summary>
    public bool Trocou { get; private set; }

    /// <summary>Apertou pra recarregar neste quadro.</summary>
    public bool Recarregou { get; private set; }

    /// <summary>Apertou pra interagir neste quadro (pegar arma, abrir bau).</summary>
    public bool Interagiu { get; private set; }

    /// <summary>Apertou a habilidade do heroi neste quadro (F ou o X do controle).</summary>
    public bool UsouHabilidade { get; private set; }

    private bool esperandoSoltar;

    private void Awake()
    {
        mover = new InputAction("Mover", InputActionType.Value);
        mover.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        mover.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        mover.AddBinding("<Gamepad>/leftStick");
        mover.AddBinding("<Gamepad>/dpad");

        mirar = new InputAction("Mirar", InputActionType.Value, "<Gamepad>/rightStick");

        atirar = new InputAction("Atirar", InputActionType.Button, "<Mouse>/leftButton");
        atirar.AddBinding("<Gamepad>/rightTrigger");
        atirar.AddBinding("<Gamepad>/rightShoulder");

        esquivar = new InputAction("Esquivar", InputActionType.Button, "<Keyboard>/space");
        esquivar.AddBinding("<Keyboard>/leftShift");
        esquivar.AddBinding("<Mouse>/rightButton");
        esquivar.AddBinding("<Gamepad>/leftTrigger");
        esquivar.AddBinding("<Gamepad>/leftShoulder");
        esquivar.AddBinding("<Gamepad>/buttonSouth");

        ponteiro = new InputAction("Ponteiro", InputActionType.Value, "<Mouse>/position");

        trocar = new InputAction("Trocar", InputActionType.Button, "<Keyboard>/q");
        trocar.AddBinding("<Gamepad>/buttonNorth");

        recarregar = new InputAction("Recarregar", InputActionType.Button, "<Keyboard>/r");

        interagir = new InputAction("Interagir", InputActionType.Button, "<Keyboard>/e");
        interagir.AddBinding("<Gamepad>/buttonEast");

        habilidade = new InputAction("Habilidade", InputActionType.Button, "<Keyboard>/f");
        habilidade.AddBinding("<Gamepad>/buttonWest");
    }

    private void OnEnable()
    {
        mover.Enable();
        mirar.Enable();
        atirar.Enable();
        esquivar.Enable();
        ponteiro.Enable();
        trocar.Enable();
        recarregar.Enable();
        interagir.Enable();
        habilidade.Enable();
    }

    private void OnDisable()
    {
        mover.Disable();
        mirar.Disable();
        atirar.Disable();
        esquivar.Disable();
        ponteiro.Disable();
        trocar.Disable();
        recarregar.Disable();
        interagir.Disable();
        habilidade.Disable();
        Trocou = Recarregou = Interagiu = UsouHabilidade = false;

        // Desligado (um menu travou o jogador): solta tudo, e ao voltar os botoes so valem depois de soltos.
        Movimento = Vector2.zero;
        Atirando = false;
        esperandoSoltar = true;
    }

    private void OnDestroy()
    {
        mover.Dispose();
        mirar.Dispose();
        atirar.Dispose();
        esquivar.Dispose();
        ponteiro.Dispose();
        trocar.Dispose();
        recarregar.Dispose();
        interagir.Dispose();
        habilidade.Dispose();
    }

    private void Update()
    {
        // Jogo parado (pausa, menu): nada daqui vale. Saindo do menu, os botoes so voltam a valer depois
        // de soltos (o clique ou o espaco que escolheu "Continuar" nao vira tiro nem esquiva).
        bool jogando = Time.timeScale > 0f;

        if (!jogando)
            esperandoSoltar = true;
        else if (esperandoSoltar && !atirar.IsPressed() && !esquivar.IsPressed())
            esperandoSoltar = false;

        bool botoes = jogando && !esperandoSoltar;

        Movimento = jogando ? Vector2.ClampMagnitude(mover.ReadValue<Vector2>(), 1f) : Vector2.zero;
        Atirando = botoes && atirar.IsPressed();

        if (botoes && esquivar.WasPressedThisFrame())
            esquivaPedidaEm = Time.time;

        float roda = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
        Trocou = botoes && (trocar.WasPressedThisFrame() || Mathf.Abs(roda) > 0.01f);
        Recarregou = botoes && recarregar.WasPressedThisFrame();
        Interagiu = botoes && interagir.WasPressedThisFrame();
        UsouHabilidade = botoes && habilidade.WasPressedThisFrame();

        LerMira();
    }

    private void LerMira()
    {
        // Analogico direito empurrado manda na mira.
        Vector2 analogico = mirar.ReadValue<Vector2>();

        if (analogico.magnitude >= zonaMortaDaMira)
        {
            Mira = analogico.normalized;
            MiraPeloMouse = false;
            return;
        }

        // O mouse retoma a mira quando mexe ou clica.
        Vector2 naTela = ponteiro.ReadValue<Vector2>();

        if ((naTela - ultimoMouse).sqrMagnitude > 1f || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
            MiraPeloMouse = true;

        ultimoMouse = naTela;

        if (cam == null)
            cam = Camera.main;

        if (cam != null)
            PontoDoMouse = cam.ScreenToWorldPoint(new Vector3(naTela.x, naTela.y, -cam.transform.position.z));

        if (MiraPeloMouse)
        {
            Vector2 ate = PontoDoMouse - (Vector2)transform.position;

            if (ate.sqrMagnitude > 0.01f)
                Mira = ate.normalized;
        }
        else if (Movimento.sqrMagnitude > 0.04f)
        {
            // No controle, com o analogico direito solto, mira pra onde anda.
            Mira = Movimento.normalized;
        }
    }

    /// <summary>True (e gasta o aperto) se o jogador pediu esquiva agora ou ha pouco.</summary>
    public bool ConsumirEsquiva()
    {
        if (Time.time - esquivaPedidaEm > memoriaDaEsquiva)
            return false;

        esquivaPedidaEm = -10f;
        return true;
    }
}
