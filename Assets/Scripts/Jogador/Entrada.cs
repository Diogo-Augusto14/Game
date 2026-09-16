using UnityEngine;

/// <summary>
/// Camada de entrada do jogador. Todo o resto do codigo pergunta pra ca, nunca pro
/// Input direto. Duas razoes praticas:
///
/// 1. Os apertos ficam GUARDADOS por um instante (buffer). Apertar pular um tiquinho
///    antes de encostar no chao ainda pula — e o que faz o controle parecer justo em
///    vez de "engasgado". Isso nao funciona lendo Input dentro do FixedUpdate, porque
///    GetButtonDown vale por um quadro de Update e o FixedUpdate pode nem rodar nele.
/// 2. Trocar teclado por controle, ou pelo Input System, mexe neste arquivo e mais nada.
///
/// Coloque no mesmo objeto do Movimento.
/// </summary>
[DisallowMultipleComponent]
public class Entrada : MonoBehaviour
{
    [Header("Teclas")]
    [Tooltip("Arrancada / esquiva")]
    [SerializeField] private KeyCode teclaDash = KeyCode.LeftShift;

    [Tooltip("Golpe. O botao esquerdo do mouse tambem serve, sempre")]
    [SerializeField] private KeyCode teclaAtaque = KeyCode.J;

    [Tooltip("Segure pra se curar")]
    [SerializeField] private KeyCode teclaCura = KeyCode.E;

    [Tooltip("Segure pra andar devagar em vez de correr")]
    [SerializeField] private KeyCode teclaAndarDevagar = KeyCode.LeftControl;

    [Header("Tolerancias (segundos)")]
    [Tooltip("Quanto tempo um aperto de pular fica guardado esperando o chao")]
    [SerializeField, Min(0f)] private float bufferDoPulo = 0.12f;

    [Tooltip("Quanto tempo um aperto de dash fica guardado")]
    [SerializeField, Min(0f)] private float bufferDoDash = 0.12f;

    [Tooltip("Quanto tempo um aperto de ataque fica guardado (e o que faz o combo nao falhar)")]
    [SerializeField, Min(0f)] private float bufferDoAtaque = 0.2f;

    // ---------------- estado ----------------
    private Cronometro puloGuardado;
    private Cronometro dashGuardado;
    private Cronometro ataqueGuardado;

    /// <summary>-1, 0 ou +1. Sem zona morta: e teclado.</summary>
    public float Horizontal { get; private set; }

    /// <summary>-1, 0 ou +1. Baixo agacha / solta a beirada, cima sobe escada.</summary>
    public float Vertical { get; private set; }

    /// <summary>Botao de pular segurado agora.</summary>
    public bool PuloSegurado { get; private set; }

    // Soltar o botao tambem vale por um quadro de Update so. Fica travado aqui ate o
    // FixedUpdate vir buscar, senao o corte do pulo falha de vez em quando.
    private bool puloSoltou;

    public bool CuraSegurada { get; private set; }

    public bool AndarDevagar { get; private set; }

    /// <summary>Lado pedido pelo teclado, ou 0. Atalho legivel pro Horizontal.</summary>
    public float Lado => Horizontal;

    public bool PedindoBaixo => Vertical < -0.5f;

    public bool PedindoCima => Vertical > 0.5f;

    // ---------------- ciclo de vida ----------------
    private void Update()
    {
        float delta = Time.deltaTime;

        puloGuardado.Contar(delta);
        dashGuardado.Contar(delta);
        ataqueGuardado.Contar(delta);

        Horizontal = Input.GetAxisRaw("Horizontal");
        Vertical = Input.GetAxisRaw("Vertical");

        PuloSegurado = Input.GetButton("Jump");
        CuraSegurada = Input.GetKey(teclaCura);
        AndarDevagar = Input.GetKey(teclaAndarDevagar);

        if (Input.GetButtonDown("Jump"))
            puloGuardado.Forcar(bufferDoPulo);

        if (Input.GetButtonUp("Jump"))
            puloSoltou = true;

        if (Input.GetKeyDown(teclaDash))
            dashGuardado.Forcar(bufferDoDash);

        if (Input.GetKeyDown(teclaAtaque) || Input.GetMouseButtonDown(0))
            ataqueGuardado.Forcar(bufferDoAtaque);
    }

    // ---------------- consumo ----------------
    /// <summary>True (e gasta o aperto) se o jogador pediu pulo agora ou pouco antes.</summary>
    public bool ConsumirPulo() => puloGuardado.Consumir();

    /// <summary>True (e gasta o aperto) se o jogador pediu dash agora ou pouco antes.</summary>
    public bool ConsumirDash() => dashGuardado.Consumir();

    /// <summary>True (e gasta o aperto) se o jogador pediu golpe agora ou pouco antes.</summary>
    public bool ConsumirAtaque() => ataqueGuardado.Consumir();

    /// <summary>True (uma vez) se o jogador soltou o botao de pular desde a ultima checagem.</summary>
    public bool ConsumirPuloSoltou()
    {
        if (!puloSoltou)
            return false;

        puloSoltou = false;
        return true;
    }

    /// <summary>Le sem gastar. Use quando so precisa saber se o pedido existe.</summary>
    public bool PuloPedido => puloGuardado.Ativo;

    public bool DashPedido => dashGuardado.Ativo;

    public bool AtaquePedido => ataqueGuardado.Ativo;

    /// <summary>Joga fora os apertos guardados. Usado ao morrer, ao renascer, em cutscene.</summary>
    public void Esquecer()
    {
        puloGuardado.Zerar();
        dashGuardado.Zerar();
        ataqueGuardado.Zerar();
        puloSoltou = false;
    }
}
