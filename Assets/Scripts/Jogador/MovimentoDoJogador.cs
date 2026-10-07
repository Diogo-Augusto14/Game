using UnityEngine;

/// <summary>
/// Andar e esquivar. Visto de cima, sem gravidade: o corpo acelera ate a velocidade, freia
/// quando solta, e a esquiva (o dash) e uma arrancada curta na direcao em que anda (parado,
/// na ultima direcao), sem tomar dano no comeco dela. Depois da esquiva tem uma recarga curta.
///
/// Quem diz pra onde andar e o <see cref="ControlesDoJogador"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class MovimentoDoJogador : MonoBehaviour
{
    [Header("Andar")]
    [Tooltip("Velocidade maxima, em unidades por segundo")]
    [SerializeField, Min(0f)] private float velocidade = 5.5f;

    [Tooltip("Quao rapido chega na velocidade (unidades por segundo, a cada segundo)")]
    [SerializeField, Min(0f)] private float aceleracao = 60f;

    [Tooltip("Quao rapido para ao soltar")]
    [SerializeField, Min(0f)] private float freio = 70f;

    [Header("Esquiva")]
    [SerializeField, Min(0f)] private float velocidadeDaEsquiva = 15f;

    [SerializeField, Min(0.01f)] private float duracaoDaEsquiva = 0.24f;

    [Tooltip("Fracao do comeco da esquiva em que nada machuca")]
    [SerializeField, Range(0f, 1f)] private float fracaoInvulneravel = 0.85f;

    [Tooltip("Segundos depois da esquiva antes de poder esquivar de novo")]
    [SerializeField, Min(0f)] private float recargaDaEsquiva = 0.3f;

    private Rigidbody2D corpo;
    private ControlesDoJogador controles;
    private float esquivaComecou = -10f;
    private float esquivaAcaba = -10f;
    private float invulneravelAte = -10f;
    private float proximaEsquiva;
    private Vector2 rumoDaEsquiva = Vector2.right;
    private Vector2 ultimaDirecao = Vector2.right;

    /// <summary>No meio da esquiva.</summary>
    public bool Esquivando => Time.time < esquivaAcaba;

    /// <summary>Nada machuca agora (o comeco da esquiva). Pros inimigos, quando existirem.</summary>
    public bool Invulneravel => Time.time < invulneravelAte;

    /// <summary>0 no comeco da esquiva, 1 no fim.</summary>
    public float ProgressoDaEsquiva => Esquivando ? Mathf.Clamp01((Time.time - esquivaComecou) / duracaoDaEsquiva) : 1f;

    public Vector2 RumoDaEsquiva => rumoDaEsquiva;

    public Vector2 Velocidade => corpo.linearVelocity;

    /// <summary>Comecou uma esquiva, com a direcao (o rastro e o som escutam).</summary>
    public event System.Action<Vector2> AoEsquivar;

    private void Awake()
    {
        corpo = GetComponent<Rigidbody2D>();
        controles = GetComponent<ControlesDoJogador>();

        corpo.bodyType = RigidbodyType2D.Dynamic;
        corpo.gravityScale = 0f;
        corpo.freezeRotation = true;
        corpo.linearDamping = 0f;
        corpo.interpolation = RigidbodyInterpolation2D.Interpolate;
        corpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Update()
    {
        if (controles == null || Esquivando || Time.time < proximaEsquiva)
            return;

        if (controles.ConsumirEsquiva())
            Esquivar();
    }

    private void FixedUpdate()
    {
        if (Esquivando)
        {
            // Arranca forte e perde um pouco de forca no fim: le como um rolamento, nao um teleporte.
            float freiando = Mathf.Lerp(1f, 0.55f, ProgressoDaEsquiva);
            corpo.linearVelocity = rumoDaEsquiva * velocidadeDaEsquiva * freiando;
            return;
        }

        Vector2 pedido = controles != null ? controles.Movimento : Vector2.zero;

        if (pedido.sqrMagnitude > 0.01f)
            ultimaDirecao = pedido.normalized;

        Vector2 alvo = pedido * velocidade;
        float taxa = pedido.sqrMagnitude > 0.01f ? aceleracao : freio;
        corpo.linearVelocity = Vector2.MoveTowards(corpo.linearVelocity, alvo, taxa * Time.fixedDeltaTime);
    }

    private void Esquivar()
    {
        Vector2 pedido = controles.Movimento;
        rumoDaEsquiva = pedido.sqrMagnitude > 0.01f ? pedido.normalized : ultimaDirecao;

        esquivaComecou = Time.time;
        esquivaAcaba = Time.time + duracaoDaEsquiva;
        invulneravelAte = Time.time + duracaoDaEsquiva * fracaoInvulneravel;
        proximaEsquiva = esquivaAcaba + recargaDaEsquiva;

        corpo.linearVelocity = rumoDaEsquiva * velocidadeDaEsquiva;
        AoEsquivar?.Invoke(rumoDaEsquiva);
    }
}
