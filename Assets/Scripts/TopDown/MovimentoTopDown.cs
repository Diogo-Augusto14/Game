using UnityEngine;

/// <summary>
/// Andar de cima, estilo Isaac: oito direcoes, sem gravidade, sem pulo. Le a
/// <see cref="Entrada"/> (WASD no modo top-down) e acelera/freia ate a velocidade alvo,
/// em vez de parar seco — e o que da aquele deslizar curtinho do Isaac.
///
/// Implementa <see cref="IControladorDeMovimento"/> pro <see cref="Vida"/> conseguir
/// empurrar o boneco quando ele apanha: durante a trava o controle nao come o impulso.
///
/// Dash: Shift (ou RB/RT no controle) da uma arrancada curta na direcao do andar (parado,
/// pra ultima direcao). Durante a arrancada o boneco nao toma dano, entao serve pra esquivar
/// de tiro e de golpe; depois tem recarga. Quem desenha o rastro e a poeira e o
/// <see cref="RastroDoDash"/>, que escuta <see cref="AoDash"/> e <see cref="AoDashPronto"/>.
///
/// Precisa de Rigidbody2D (Dynamic) e Entrada no mesmo objeto.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class MovimentoTopDown : MonoBehaviour, IControladorDeMovimento
{
    [Header("Andar")]
    [Tooltip("Velocidade maxima, em unidades por segundo")]
    [SerializeField, Min(0f)] private float velocidade = 4.5f;

    [Tooltip("Quao rapido chega na velocidade maxima (unidades/s²). Alto = responde na hora")]
    [SerializeField, Min(0f)] private float aceleracao = 45f;

    [Tooltip("Quao rapido para ao soltar as teclas (unidades/s²). Baixo = escorrega mais")]
    [SerializeField, Min(0f)] private float desaceleracao = 30f;

    [Tooltip("Freio durante o empurrao de um golpe, em fracao da desaceleracao normal")]
    [SerializeField, Range(0f, 1f)] private float freioNoEmpurrao = 0.3f;

    [Header("Dash")]
    [Tooltip("Velocidade durante a arrancada, em unidades por segundo")]
    [SerializeField, Min(0f)] private float velocidadeDoDash = 15f;

    [Tooltip("Segundos da arrancada (distancia = velocidade x duracao)")]
    [SerializeField, Min(0.01f)] private float duracaoDoDash = 0.15f;

    [Tooltip("Segundos, depois da arrancada, ate poder dar outra")]
    [SerializeField, Min(0f)] private float recargaDoDash = 0.7f;

    [Tooltip("Invencivel mais este tanto depois da arrancada (a esquiva nao falha por um quadro)")]
    [SerializeField, Min(0f)] private float protecaoDepoisDoDash = 0.08f;

    // ---------------- estado ----------------
    private Rigidbody2D rb;
    private Entrada entrada;
    private Cronometro semControle;
    private Cronometro dashando;
    private Cronometro recargaDash;
    private Cronometro protecaoDash;
    private Vector2 rumoDoDash;
    private bool esperandoRecarga;

    /// <summary>Ultima direcao em que andou (nunca zero). Comeca olhando pra baixo, como no Isaac.</summary>
    public Vector2 UltimaDirecao { get; private set; } = Vector2.down;

    /// <summary>Velocidade atual do corpo. O tiro herda um pouco dela.</summary>
    public Vector2 Velocidade => rb != null ? rb.linearVelocity : Vector2.zero;

    /// <summary>Velocidade maxima de andar. Itens mexem nela por <see cref="DefinirVelocidadeMaxima"/>.</summary>
    public float VelocidadeMaxima => velocidade;

    public bool Andando => entrada != null && entrada.Andar != Vector2.zero;

    /// <summary>No meio da arrancada.</summary>
    public bool Dashando => dashando.Ativo;

    /// <summary>Direcao da arrancada atual (ou da ultima).</summary>
    public Vector2 RumoDoDash => rumoDoDash;

    /// <summary>0 = acabou de dar o dash, 1 = pronto pra outro.</summary>
    public float DashCarregado => recargaDash.Ativo ? 1f - recargaDash.Restante / (duracaoDoDash + recargaDoDash) : 1f;

    /// <summary>Comecou uma arrancada, com a direcao.</summary>
    public event System.Action<Vector2> AoDash;

    /// <summary>A recarga acabou: da pra dar outro dash.</summary>
    public event System.Action AoDashPronto;

    // ---------------- IControladorDeMovimento ----------------
    /// <summary>Durante o dash (e um instante depois) o golpe passa reto: e a esquiva.</summary>
    public bool IgnorandoDano => dashando.Ativo || protecaoDash.Ativo;

    /// <summary>Golpe nenhum passa por esse tempo (o vazio). E a mesma protecao do fim do dash.</summary>
    public void DarProtecao(float segundos) => protecaoDash.Forcar(segundos);

    public void AplicarImpulsoExterno(Vector2 impulso, float travaSegundos)
    {
        if (rb == null)
            return;

        dashando.Zerar();
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(impulso, ForceMode2D.Impulse);
        semControle.Forcar(travaSegundos);
    }

    // ---------------- ciclo de vida ----------------
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        entrada = GetComponent<Entrada>();

        if (entrada == null)
            Debug.LogWarning("[MovimentoTopDown] sem Entrada no objeto — o boneco nao vai andar.", this);

        // Visto de cima nao existe "cair": gravidade zero neste corpo, seja qual for a do mundo.
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearDamping = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void FixedUpdate()
    {
        float delta = Time.fixedDeltaTime;
        semControle.Contar(delta);
        protecaoDash.Contar(delta);
        recargaDash.Contar(delta);

        if (esperandoRecarga && !recargaDash.Ativo)
        {
            esperandoRecarga = false;
            AoDashPronto?.Invoke();
        }

        if (dashando.Ativo)
        {
            dashando.Contar(delta);
            rb.linearVelocity = rumoDoDash * velocidadeDoDash;

            // Fim da arrancada: sai andando na velocidade normal, sem freada seca.
            if (!dashando.Ativo)
            {
                rb.linearVelocity = rumoDoDash * velocidade;
                protecaoDash.Forcar(protecaoDepoisDoDash);
            }

            return;
        }

        if (semControle.Ativo)
        {
            // Apanhando: deixa o empurrao agir, so com um freio leve.
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, desaceleracao * freioNoEmpurrao * delta);
            return;
        }

        Vector2 pedido = entrada != null ? entrada.Andar : Vector2.zero;

        if (entrada != null && entrada.enabled && !recargaDash.Ativo && entrada.ConsumirDash())
        {
            ComecarDash(pedido != Vector2.zero ? pedido : UltimaDirecao);
            return;
        }

        Vector2 alvo = pedido * velocidade;
        float taxa = pedido == Vector2.zero ? desaceleracao : aceleracao;

        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, alvo, taxa * delta);

        if (pedido != Vector2.zero)
            UltimaDirecao = pedido;
    }

    private void ComecarDash(Vector2 direcao)
    {
        rumoDoDash = direcao.sqrMagnitude > 0.0001f ? direcao.normalized : Vector2.down;
        UltimaDirecao = rumoDoDash;
        dashando.Forcar(duracaoDoDash);
        recargaDash.Forcar(duracaoDoDash + recargaDoDash);
        esperandoRecarga = true;
        rb.linearVelocity = rumoDoDash * velocidadeDoDash;
        AoDash?.Invoke(rumoDoDash);
    }

    /// <summary>Troca a velocidade maxima (itens). Nunca fica abaixo de 1.</summary>
    public void DefinirVelocidadeMaxima(float nova)
    {
        velocidade = Mathf.Max(1f, nova);
    }

    /// <summary>Para na hora e esquece o empurrao. Usado ao renascer / trocar de sala.</summary>
    public void Parar()
    {
        semControle.Zerar();
        dashando.Zerar();
        protecaoDash.Zerar();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }
}
