using UnityEngine;

/// <summary>
/// Jogador PROVISORIO, so pra testar a sala enquanto o movimento de cima e o tiro de
/// verdade nao chegam. WASD anda, setas atiram (estilo Isaac: andar e mirar separados).
///
/// A <see cref="DemoDaSala"/> so cria este boneco se a cena nao tiver nenhum objeto com a
/// tag Player. Quando o jogador de verdade existir, este arquivo pode ser apagado.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Vida))]
public class JogadorDeTeste : MonoBehaviour, IControladorDeMovimento
{
    [SerializeField, Min(0f)] private float velocidade = 4f;

    [SerializeField, Min(0.05f)] private float intervaloDeTiro = 0.35f;

    [SerializeField, Min(0.1f)] private float velocidadeDoTiro = 7f;

    [SerializeField, Min(0f)] private float danoDoTiro = 10f;

    [SerializeField] private Color corDoTiro = new Color(0.6f, 0.85f, 1f);

    private Rigidbody2D rb;
    private Vida vida;
    private Cronometro recarga;
    private Cronometro semControle;

    public bool IgnorandoDano => false;

    /// <summary>Monta o boneco completo numa posicao.</summary>
    public static JogadorDeTeste Criar(Vector2 posicao)
    {
        GameObject obj = new GameObject("JogadorDeTeste");
        obj.SetActive(false);
        obj.transform.position = posicao;
        obj.tag = "Player";
        Camadas.Definir(obj, Camadas.Jogador);

        Rigidbody2D corpo = obj.AddComponent<Rigidbody2D>();
        corpo.gravityScale = 0f;
        corpo.freezeRotation = true;
        corpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        corpo.interpolation = RigidbodyInterpolation2D.Interpolate;

        CircleCollider2D circulo = obj.AddComponent<CircleCollider2D>();
        circulo.radius = 0.3f;

        FormasDaSala.Desenho(obj.transform, "Desenho", FormasDaSala.Circulo(), new Color(0.95f, 0.85f, 0.7f),
                             Vector2.zero, Vector2.one * 0.6f, 15);

        Vida v = obj.AddComponent<Vida>();
        v.Configurar(60f, 0f, false, 0f, true);

        JogadorDeTeste jogador = obj.AddComponent<JogadorDeTeste>();
        obj.SetActive(true);
        return jogador;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        vida = GetComponent<Vida>();
        rb.gravityScale = 0f;
        rb.linearDamping = 4f;
    }

    private void OnEnable() => vida.AoMorrer.AddListener(Renascer);

    private void OnDisable() => vida.AoMorrer.RemoveListener(Renascer);

    private void Update()
    {
        recarga.Contar(Time.deltaTime);

        if (vida.EstaMorto || recarga.Ativo)
            return;

        Vector2 mira = Vector2.zero;

        if (Input.GetKey(KeyCode.UpArrow)) mira = Vector2.up;
        else if (Input.GetKey(KeyCode.DownArrow)) mira = Vector2.down;
        else if (Input.GetKey(KeyCode.LeftArrow)) mira = Vector2.left;
        else if (Input.GetKey(KeyCode.RightArrow)) mira = Vector2.right;

        if (mira == Vector2.zero)
            return;

        // O tiro herda um pouco do andar, como no Isaac.
        Vector2 vel = mira * velocidadeDoTiro + rb.linearVelocity * 0.3f;
        TiroDaSala.Disparar(rb.position + mira * 0.35f, vel, danoDoTiro, gameObject, false, corDoTiro, 0.25f);
        recarga.Forcar(intervaloDeTiro);
    }

    private void FixedUpdate()
    {
        semControle.Contar(Time.fixedDeltaTime);

        if (vida.EstaMorto || semControle.Ativo)
            return;

        Vector2 andar = new Vector2(
            (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f),
            (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f));

        rb.linearVelocity = Vector2.ClampMagnitude(andar, 1f) * velocidade;
    }

    public void AplicarImpulsoExterno(Vector2 impulso, float travaSegundos)
    {
        rb.linearVelocity = vida.UltimoGolpe.Direcao * impulso.magnitude;
        semControle.Forcar(travaSegundos);
    }

    private void Renascer()
    {
        rb.linearVelocity = Vector2.zero;
        Invoke(nameof(Reviver), 1f);
    }

    private void Reviver() => vida.Reviver(1.5f);
}
