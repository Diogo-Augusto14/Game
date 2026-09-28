using UnityEngine;

/// <summary>
/// Andar de cima, estilo Isaac: oito direcoes, sem gravidade, sem pulo. Le a
/// <see cref="Entrada"/> (WASD no modo top-down) e acelera/freia ate a velocidade alvo,
/// em vez de parar seco — e o que da aquele deslizar curtinho do Isaac.
///
/// Implementa <see cref="IControladorDeMovimento"/> pro <see cref="Vida"/> conseguir
/// empurrar o boneco quando ele apanha: durante a trava o controle nao come o impulso.
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

    // ---------------- estado ----------------
    private Rigidbody2D rb;
    private Entrada entrada;
    private Cronometro semControle;

    /// <summary>Ultima direcao em que andou (nunca zero). Comeca olhando pra baixo, como no Isaac.</summary>
    public Vector2 UltimaDirecao { get; private set; } = Vector2.down;

    /// <summary>Velocidade atual do corpo. O tiro herda um pouco dela.</summary>
    public Vector2 Velocidade => rb != null ? rb.linearVelocity : Vector2.zero;

    /// <summary>Velocidade maxima de andar. Itens mexem nela por <see cref="DefinirVelocidadeMaxima"/>.</summary>
    public float VelocidadeMaxima => velocidade;

    public bool Andando => entrada != null && entrada.Andar != Vector2.zero;

    // ---------------- IControladorDeMovimento ----------------
    /// <summary>Sem dash ainda, entao nunca ignora dano.</summary>
    public bool IgnorandoDano => false;

    public void AplicarImpulsoExterno(Vector2 impulso, float travaSegundos)
    {
        if (rb == null)
            return;

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

        if (semControle.Ativo)
        {
            // Apanhando: deixa o empurrao agir, so com um freio leve.
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, desaceleracao * freioNoEmpurrao * delta);
            return;
        }

        Vector2 pedido = entrada != null ? entrada.Andar : Vector2.zero;
        Vector2 alvo = pedido * velocidade;
        float taxa = pedido == Vector2.zero ? desaceleracao : aceleracao;

        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, alvo, taxa * delta);

        if (pedido != Vector2.zero)
            UltimaDirecao = pedido;
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

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }
}
