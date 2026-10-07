using UnityEngine;

/// <summary>
/// O primeiro inimigo: anda ate o jogador e, perto o bastante, para, prepara o tiro (a animacao do
/// ataque e o aviso) e solta uma bala lenta na direcao dele. Depois de um tempo, prepara de novo.
///
/// O corpo e um Rigidbody2D Dynamic, como o do jogador: nao atravessa o jogador nem os outros
/// inimigos, e o empurrao dos golpes funciona sozinho (o andar freia de volta).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Vida))]
public class InimigoAtirador : MonoBehaviour
{
    [Header("Andar")]
    [Tooltip("Velocidade maxima, em unidades por segundo")]
    [SerializeField, Min(0f)] private float velocidade = 2.2f;

    [Tooltip("Quao rapido chega na velocidade e freia (tambem e o que segura o empurrao)")]
    [SerializeField, Min(0f)] private float aceleracao = 20f;

    [Tooltip("Para de chegar perto a esta distancia do jogador")]
    [SerializeField, Min(0f)] private float distanciaParaParar = 4f;

    [Header("Tiro")]
    [SerializeField] private DadosDaArma arma;

    [Tooltip("So atira com o jogador a esta distancia ou menos")]
    [SerializeField, Min(0f)] private float alcanceDoTiro = 8f;

    [Tooltip("Segundos parado preparando antes da bala sair: o aviso pro jogador")]
    [SerializeField, Min(0f)] private float preparo = 0.5f;

    [Tooltip("Segundos entre um tiro e o proximo (varia um pouco pra cada inimigo nao atirar junto)")]
    [SerializeField, Min(0.1f)] private float intervalo = 2f;

    [Tooltip("De onde a bala sai: distancia do centro do corpo, na direcao do jogador")]
    [SerializeField, Min(0f)] private float distanciaDaSaida = 0.4f;

    [Tooltip("Altura de onde a bala sai, em relacao ao centro do corpo")]
    [SerializeField] private float alturaDaSaida;

    private Rigidbody2D corpo;
    private Vida vida;
    private AudioSource audioSource;
    private Transform alvo;
    private Vida vidaDoAlvo;
    private Vector2 querAndar;
    private float atiraEm = -1f;
    private float proximoAtaque;

    /// <summary>Parado, preparando o tiro (a bala sai quando acabar).</summary>
    public bool Preparando => atiraEm >= 0f;

    /// <summary>Segundos de preparo (a animacao do ataque cabe neles).</summary>
    public float Preparo => preparo;

    /// <summary>Pra onde olha: o jogador. Tamanho 1.</summary>
    public Vector2 OlhandoPara { get; private set; } = Vector2.left;

    public Vector2 Velocidade => corpo.linearVelocity;

    /// <summary>Comecou a preparar um tiro (a animacao do ataque escuta).</summary>
    public event System.Action AoPreparar;

    private void Awake()
    {
        corpo = GetComponent<Rigidbody2D>();
        vida = GetComponent<Vida>();
        audioSource = GetComponent<AudioSource>();

        corpo.bodyType = RigidbodyType2D.Dynamic;
        corpo.gravityScale = 0f;
        corpo.freezeRotation = true;
        corpo.linearDamping = 0f;
        corpo.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Start()
    {
        GameObject jogador = GameObject.FindWithTag("Player");

        if (jogador != null)
        {
            alvo = jogador.transform;
            vidaDoAlvo = jogador.GetComponent<Vida>();
        }

        // O primeiro tiro demora um pouco: quem acabou de chegar nao atira na hora.
        proximoAtaque = Time.time + Random.Range(0.8f, intervalo);
    }

    private void Update()
    {
        querAndar = Vector2.zero;

        if (vida.Morto || alvo == null || (vidaDoAlvo != null && vidaDoAlvo.Morto))
        {
            atiraEm = -1f;
            return;
        }

        Vector2 ateOAlvo = (Vector2)alvo.position - (Vector2)transform.position;
        float distancia = ateOAlvo.magnitude;

        // Continua olhando pro jogador enquanto prepara: a bala vai pra onde ele esta quando sai.
        if (distancia > 0.01f)
            OlhandoPara = ateOAlvo / distancia;

        if (Preparando)
        {
            if (Time.time >= atiraEm)
            {
                Atirar();
                atiraEm = -1f;
                proximoAtaque = Time.time + intervalo * Random.Range(0.85f, 1.15f);
            }

            return;
        }

        if (arma != null && distancia <= alcanceDoTiro && Time.time >= proximoAtaque)
        {
            atiraEm = Time.time + preparo;
            AoPreparar?.Invoke();
            return;
        }

        if (distancia > distanciaParaParar)
            querAndar = OlhandoPara;
    }

    private void FixedUpdate()
    {
        if (vida.Morto)
            return;

        corpo.linearVelocity = Vector2.MoveTowards(corpo.linearVelocity, querAndar * velocidade, aceleracao * Time.fixedDeltaTime);
    }

    private void Atirar()
    {
        Vector2 origem = (Vector2)transform.position + Vector2.up * alturaDaSaida + OlhandoPara * distanciaDaSaida;
        arma.Disparar(origem, OlhandoPara, gameObject, vida.Lado);

        if (audioSource != null && arma.som != null)
        {
            audioSource.pitch = Random.Range(0.94f, 1.06f);
            audioSource.PlayOneShot(arma.som, arma.volume);
        }
    }
}
