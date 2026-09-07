using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class Movimento : MonoBehaviour
{
    [Header("Andar")]
    [Tooltip("Velocidade de caminhada do personagem")]
    public float velocidadeMaxima = 6f;
    public float aceleracao = 20f;
    [Header("pular")]
    [Tooltip("Velocidade do pulo do personagem")]
    public float Jump = 5f;
    private bool pularInput;
    private int maximoDePulo = 1;
    private int pulos = 0;


    [Header("Chão")]
    public Transform groundCheck;      // arraste o objeto GroundCheck aqui no Inspector
    public float raioChecagem = 0.2f;  // tamanho do círculo de detecção
    public LayerMask chaoLayer;        // qual layer conta como "chão"
    private bool estaNoChao;
    [Header("Parede")]
    public Transform WallCheck;      // arraste o objeto GroundCheck aqui no Inspector
    public float raioCheck = 0.2f;  // tamanho do círculo de detecção
    public LayerMask paredeLayer;        // qual layer conta como "chão"
    private bool estaNaParede;
    private bool estaDeslizando;
    public float velocidadeDeslizada = 2f;
    [Tooltip("Pulo na parede")]
    public float wallJumpForcaX = 8f;
    public float wallJumpForcaY = 6f;


    [Header("Referências")]
    [Tooltip("Se deixar vazio, o script acha sozinho no Awake")]
    public Rigidbody2D rb;
    private Animator animator;

    // Guarda o último lado olhado (+1 direita, -1 esquerda) pra não "esquecer"
    // pra onde olha quando a tecla é solta (viraria 0 e o boneco sumiria)
    [HideInInspector] public float direcao = 1f;

    // Tamanho original do boneco no eixo X, pra virar sem crescer/encolher
    private float escalaBase;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>(); // busca UMA vez só, igual ao rb
        escalaBase = Mathf.Abs(transform.localScale.x);
    }

    // Guarda o input lido no Update() pra usar depois no FixedUpdate(),
    // porque o FixedUpdate não roda todo quadro e não pode ler Input.GetAxisRaw
    // diretamente sem risco de "perder" um aperto de tecla rápido
    private float ladoInput;

    void Update()
    {
        ladoInput = Input.GetAxisRaw("Horizontal");
        Virar(ladoInput);

        if (Input.GetButtonDown("Jump"))
        {
            pularInput = true;
        }

    }

   void FixedUpdate()
{
    estaNoChao = Physics2D.OverlapCircle(groundCheck.position, raioChecagem, chaoLayer);
    estaNaParede = Physics2D.OverlapCircle(WallCheck.position, raioCheck, paredeLayer);
    
    if (estaNoChao)
    {
        pulos = 0;
        animator.SetInteger("dJump", pulos);
    }

    Andar(ladoInput);

    // Calcula estaDeslizando AQUI, antes do pulo usar esse valor
    estaDeslizando = estaNaParede && !estaNoChao && ladoInput != 0 && Mathf.Sign(ladoInput) == direcao;
    animator.SetBool("IsSliding", estaDeslizando);
    if (estaDeslizando)
    {
        Vector2 vel = rb.linearVelocity;
        vel.y = Mathf.Max(vel.y, -velocidadeDeslizada);
        rb.linearVelocity = vel;
    }

    // Agora o pulo já enxerga o valor certo de estaDeslizando
    if (pularInput)
    {
        if (estaDeslizando)
        {
            PularDaParede();
        }
        else if (pulos < maximoDePulo)
        {
            Pular();
            pulos += 1;
            animator.SetInteger("dJump", pulos);
        }
        pularInput = false;
    }
}

    void Andar(float lado)
    {
        Vector2 vel = rb.linearVelocity;

        float velocidadeAlvo = velocidadeMaxima * lado;
        vel.x = Mathf.MoveTowards(vel.x, velocidadeAlvo, aceleracao * Time.deltaTime);



        rb.linearVelocity = vel;

        animator.SetFloat("xVelocity", rb.linearVelocity.x);
        animator.SetFloat("yVelocity", rb.linearVelocity.y);
        animator.SetBool("IsGround", estaNoChao);


        if (lado == 0f) animator.SetBool("IsTop", true);
        else animator.SetBool("IsTop", false);
    }

    void Virar(float lado)
    {
        if (lado != 0f) direcao = Mathf.Sign(lado);
        transform.localScale = new Vector3(direcao * escalaBase, transform.localScale.y, 1f);
    }

    void Pular()
    {
        Vector2 vel = rb.linearVelocity;
        vel.y = Jump;
        rb.linearVelocity = vel;


    }
    void PularDaParede()
{
    Vector2 vel = rb.linearVelocity;
    vel.x = wallJumpForcaX * (-direcao);
    vel.y = wallJumpForcaY;
    rb.linearVelocity = vel;
}

}