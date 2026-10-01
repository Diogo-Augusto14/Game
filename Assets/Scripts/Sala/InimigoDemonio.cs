using UnityEngine;

/// <summary>
/// O Demonio (arte do Tiny RPG pack): corre atras do jogador e, chegando perto, para,
/// levanta a espada e da um golpe curto pra frente com um passinho de avanco.
///
///   Agindo      -> persegue
///   Preparando  -> ergue a espada (a animacao de ataque ate o quadro do corte)
///   golpe       -> dano num circulo a frente dele + avanco curto
///   Recuperando -> termina o corte parado; da tempo de o jogador revidar
///
/// Da pra fugir: o golpe sai onde o jogador ESTAVA quando ele comecou a erguer a espada.
/// </summary>
public class InimigoDemonio : InimigoDeSala
{
    [Header("Golpe de espada")]
    [Tooltip("Mais perto que isto do jogador, ele comeca o golpe")]
    [SerializeField, Min(0f)] private float alcanceParaGolpear = 1.3f;

    [Tooltip("Telegrafo: segundos erguendo a espada")]
    [SerializeField, Min(0f)] private float tempoDePreparo = 0.4f;

    [SerializeField, Min(0f)] private float tempoDeRecuperacao = 0.45f;

    [SerializeField, Min(0.1f)] private float intervaloEntreGolpes = 1.2f;

    [SerializeField, Min(0f)] private float danoDoGolpe = 15f;

    [Tooltip("Raio da area do corte, centrada a frente dele")]
    [SerializeField, Min(0.1f)] private float raioDoGolpe = 0.55f;

    [Tooltip("Velocidade do passinho pra frente no corte")]
    [SerializeField, Min(0f)] private float avanco = 4f;

    /// <summary>A tira de ataque tem 7 quadros; o corte aparece no quinto (indice 4).</summary>
    private const int QuadroDoCorte = 4;

    private AnimacaoDePersonagem animacao;
    private ClipesDePersonagem clipes;
    private Cronometro recarga;
    private Cronometro preparo;
    private Cronometro recuperacao;
    private Vector2 direcaoDoGolpe = Vector2.right;

    public void UsarArte(AnimacaoDePersonagem novaAnimacao, ClipesDePersonagem novosClipes)
    {
        animacao = novaAnimacao;
        clipes = novosClipes;
    }

    protected override void Awake()
    {
        base.Awake();
        velocidade = 2.1f;
        recarga.Forcar(intervaloEntreGolpes * 0.5f);
    }

    protected override void AtualizarAgindo(float dt)
    {
        recarga.Contar(dt);

        Vector2 alvo = ParaOJogador();
        float distancia = alvo.magnitude;

        if (distancia < 0.0001f)
        {
            Frear();
            return;
        }

        if (!recarga.Ativo && distancia <= alcanceParaGolpear && VeOJogador())
        {
            direcaoDoGolpe = alvo / distancia;
            EstadoAtual = Estado.Preparando;
            preparo.Forcar(tempoDePreparo);
            Frear();

            animacao?.OlharPara(direcaoDoGolpe);

            // Chega no quadro do corte exatamente quando o preparo acaba.
            if (clipes != null && clipes.Ataque != null)
                animacao?.TocarUmaVez(clipes.Ataque, QuadroDoCorte / Mathf.Max(0.05f, tempoDePreparo));

            return;
        }

        Aproximar(alvo, VeOJogador() ? velocidade : velocidade * 0.6f, dt);
    }

    protected override void AtualizarPreparando(float dt)
    {
        Frear();
        preparo.Contar(dt);

        if (preparo.Ativo)
            return;

        Golpear();
        EstadoAtual = Estado.Recuperando;
        recuperacao.Forcar(tempoDeRecuperacao);
    }

    protected override void AtualizarRecuperando(float dt)
    {
        recuperacao.Contar(dt);

        // O passinho do corte morre sozinho.
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, 12f * dt);

        if (recuperacao.Ativo)
            return;

        recarga.Forcar(intervaloEntreGolpes);
        EstadoAtual = Estado.Agindo;
    }

    private void Golpear()
    {
        Sons.Tocar(Som.Pancada, 0.6f);
        rb.linearVelocity = direcaoDoGolpe * avanco;

        Vector2 centro = rb.position + direcaoDoGolpe * (Raio + raioDoGolpe * 0.6f);

        foreach (Collider2D c in Physics2D.OverlapCircleAll(centro, raioDoGolpe))
        {
            GameObject quem = c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.gameObject;

            if (quem == gameObject || !quem.CompareTag("Player"))
                continue;

            quem.GetComponentInParent<IDanificavel>()?.TomarDano(
                new DanoInfo(danoDoGolpe, direcaoDoGolpe, 5f, centro, gameObject));
            break;
        }
    }

    protected override void Morrer()
    {
        Vector3 escala = transform.localScale;
        base.Morrer();

        // A animacao de morte mostra a queda; encolher junto so esconderia ela.
        transform.localScale = escala;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 origem = transform.position;
        Gizmos.DrawWireSphere(origem + direcaoDoGolpe * (Raio + raioDoGolpe * 0.6f), raioDoGolpe);
    }
}
