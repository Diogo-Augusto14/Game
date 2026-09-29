using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base dos inimigos de sala (visao de cima, sem gravidade). E a maquina de estados do
/// <see cref="Inimigo"/> de plataforma, enxugada pro estilo Isaac:
///
///   Dormindo    -> parado ate a sala acordar (o jogador entrou)
///   Acordando   -> um instante parado, piscando: o jogador VE que vai comecar
///   Agindo      -> o comportamento proprio de cada inimigo (perseguir, se posicionar...)
///   Preparando  -> telegrafo do ataque (quem nao ataca a distancia nunca entra aqui)
///   Recuperando -> respira depois do ataque
///   Atordoado   -> levou golpe; o empurrao acontece
///   Morto       -> para tudo; a Sala conta a morte, o Vida limpa o objeto
///
/// Tudo que e comum fica aqui: acordar, dano por encostar, empurrao em qualquer direcao,
/// morrer. Cada inimigo so escreve o que faz no estado Agindo (e, se atirar, no Preparando).
///
/// Vida, flash e morte continuam no componente Vida. Esta classe implementa
/// IControladorDeMovimento so pra receber o empurrao na DIRECAO do golpe: o Vida foi
/// feito pra plataforma e empurra pro lado + pra cima, o que numa sala vista de cima
/// jogaria o inimigo sempre pro norte.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Vida))]
public abstract class InimigoDeSala : MonoBehaviour, IControladorDeMovimento
{
    public enum Estado
    {
        Dormindo,
        Acordando,
        Agindo,
        Preparando,
        Recuperando,
        Atordoado,
        Morto
    }

    [Header("Alvo")]
    [SerializeField] private string tagDoJogador = "Player";

    [Header("Movimento")]
    [Tooltip("Velocidade maxima andando")]
    [SerializeField, Min(0f)] protected float velocidade = 1.6f;

    [Tooltip("Quao rapido chega na velocidade (0 = na hora)")]
    [SerializeField, Min(0f)] protected float aceleracao = 10f;

    [Header("Acordar")]
    [Tooltip("Ligado: fica parado ate a sala mandar acordar. Desligado: ja nasce acordado")]
    [SerializeField] private bool comecaDormindo = true;

    [Tooltip("Segundos parado ao acordar, antes de agir")]
    [SerializeField, Min(0f)] private float tempoAcordando = 0.5f;

    [Header("Dano por encostar")]
    [SerializeField, Min(0f)] protected float danoDeContato = 10f;

    [SerializeField, Min(0f)] private float empurraoDeContato = 4f;

    [Tooltip("Segundos entre um dano de contato e outro")]
    [SerializeField, Min(0.05f)] private float intervaloDeContato = 0.6f;

    [Header("Ao tomar dano")]
    [SerializeField, Min(0f)] private float tempoAtordoadoLeve = 0.15f;

    [SerializeField, Min(0f)] private float tempoAtordoadoForte = 0.4f;

    [Header("Eventos")]
    public UnityEvent AoAcordar = new UnityEvent();

    // ---------------- estado ----------------
    protected Rigidbody2D rb;
    protected Vida vida;
    protected Transform jogador;
    protected SpriteRenderer desenho;

    private Cronometro acordando;
    private Cronometro atordoamento;
    private Cronometro recargaDoContato;
    private Color corOriginal;
    private float raioDoCorpo;
    private float ladoDoDesvio = 1f;

    /// <summary>Todo inimigo ligado na cena (lagrima teleguiada procura aqui, sem Find).</summary>
    public static readonly System.Collections.Generic.List<InimigoDeSala> Ativos = new System.Collections.Generic.List<InimigoDeSala>();

    public Estado EstadoAtual { get; protected set; } = Estado.Dormindo;

    public Vida Vida => vida;

    public bool EstaMorto => EstadoAtual == Estado.Morto;

    public bool IgnorandoDano => false;

    /// <summary>
    /// Ligado: golpe nao atordoa nem empurra (chefe). Senao cada lagrima interromperia o
    /// ataque no meio e o chefe viveria sendo jogado contra a parede.
    /// </summary>
    protected virtual bool Imparavel => false;

    // ---------------- ciclo de vida ----------------
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        vida = GetComponent<Vida>();
        desenho = GetComponentInChildren<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearDamping = 3f;   // o empurrao morre sozinho enquanto atordoado

        if (desenho != null)
            corOriginal = desenho.color;

        EstadoAtual = comecaDormindo ? Estado.Dormindo : Estado.Agindo;
    }

    protected virtual void OnEnable()
    {
        Ativos.Add(this);
        vida.AoTomarDano.AddListener(AoLevarGolpe);
        vida.AoMorrer.AddListener(Morrer);
    }

    protected virtual void OnDisable()
    {
        Ativos.Remove(this);
        vida.AoTomarDano.RemoveListener(AoLevarGolpe);
        vida.AoMorrer.RemoveListener(Morrer);
    }

    /// <summary>A sala chama quando o jogador entra. Chamar de novo nao faz nada.</summary>
    public void Acordar()
    {
        if (EstadoAtual != Estado.Dormindo)
            return;

        EstadoAtual = Estado.Acordando;
        acordando.Forcar(tempoAcordando);
        AoAcordar?.Invoke();
    }

    // ---------------- loop ----------------
    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        acordando.Contar(dt);
        atordoamento.Contar(dt);
        recargaDoContato.Contar(dt);

        if (EstadoAtual == Estado.Morto)
            return;

        if (jogador == null)
            AcharJogador();

        switch (EstadoAtual)
        {
            case Estado.Dormindo:
                Frear();
                break;

            case Estado.Acordando:
                Frear();

                if (!acordando.Ativo)
                {
                    EstadoAtual = Estado.Agindo;

                    if (desenho != null)
                        desenho.color = corOriginal;
                }

                break;

            case Estado.Atordoado:
                // Nao mexe na velocidade: e o que deixa o empurrao ser sentido.
                if (!atordoamento.Ativo)
                    EstadoAtual = Estado.Agindo;

                break;

            case Estado.Agindo:
                AtualizarAgindo(dt);
                break;

            case Estado.Preparando:
                AtualizarPreparando(dt);
                break;

            case Estado.Recuperando:
                AtualizarRecuperando(dt);
                break;
        }
    }

    private void Update()
    {
        if (desenho == null || EstadoAtual != Estado.Acordando)
            return;

        // Pisca enquanto acorda: aviso de que o inimigo vai comecar a se mexer.
        float t = Mathf.Repeat(Time.time * 10f, 1f);
        Color apagado = Color.Lerp(corOriginal, Color.white, 0.6f);
        apagado.a = 0.45f;
        desenho.color = t < 0.5f ? corOriginal : apagado;
    }

    /// <summary>O comportamento do inimigo. Roda a cada FixedUpdate enquanto Agindo.</summary>
    protected abstract void AtualizarAgindo(float dt);

    /// <summary>Telegrafo do ataque. Quem nao usa pode deixar como esta.</summary>
    protected virtual void AtualizarPreparando(float dt)
    {
        Frear();
        EstadoAtual = Estado.Agindo;
    }

    protected virtual void AtualizarRecuperando(float dt)
    {
        Frear();
        EstadoAtual = Estado.Agindo;
    }

    // ---------------- ajudas pros filhos ----------------
    protected Vector2 ParaOJogador()
    {
        if (jogador == null)
            return Vector2.zero;

        return (Vector2)jogador.position - rb.position;
    }

    /// <summary>Anda na direcao dada (normalizada ou nao), acelerando. Contorna pedra no caminho.</summary>
    protected void Andar(Vector2 direcao, float velocidadeAlvo)
    {
        Vector2 alvo = direcao.sqrMagnitude > 0.0001f ? Desviar(direcao.normalized) * velocidadeAlvo : Vector2.zero;

        rb.linearVelocity = aceleracao > 0f
            ? Vector2.MoveTowards(rb.linearVelocity, alvo, aceleracao * Time.fixedDeltaTime)
            : alvo;
    }

    protected void Frear() => Andar(Vector2.zero, 0f);

    /// <summary>
    /// Com parede ou pedra logo a frente, escorrega ao longo dela em vez de ficar
    /// empurrando. Sem isto um perseguidor atras de uma pedra fica parado pra sempre.
    /// Topando de frente, sempre escolhe o mesmo lado, pra nao ficar indeciso.
    /// </summary>
    protected Vector2 Desviar(Vector2 direcao)
    {
        RaycastHit2D batida = Physics2D.CircleCast(rb.position, Raio * 0.9f, direcao, 0.45f, Camadas.MascaraDeParede);

        // Distancia zero = ja comecou encostado (o normal vem errado): deixa a fisica resolver.
        if (batida.collider == null || batida.distance <= 0f)
            return direcao;

        Vector2 tangente = new Vector2(-batida.normal.y, batida.normal.x);
        float lado = Vector2.Dot(tangente, direcao);

        if (Mathf.Abs(lado) < 0.2f)
            lado = ladoDoDesvio;
        else
            ladoDoDesvio = Mathf.Sign(lado);

        return tangente * Mathf.Sign(lado);
    }

    /// <summary>Raio do corpo (o CircleCollider2D). Bom pra checar colisao a frente.</summary>
    protected float Raio
    {
        get
        {
            if (raioDoCorpo <= 0f)
                raioDoCorpo = TryGetComponent(out CircleCollider2D c) ? c.radius * transform.lossyScale.x : 0.3f;

            return raioDoCorpo;
        }
    }

    /// <summary>True se nao tem parede entre o inimigo e o jogador.</summary>
    protected bool VeOJogador()
    {
        if (jogador == null)
            return false;

        RaycastHit2D hit = Physics2D.Linecast(rb.position, jogador.position, Camadas.MascaraDeParede);
        return hit.collider == null;
    }

    private void AcharJogador()
    {
        GameObject obj = GameObject.FindWithTag(tagDoJogador);

        if (obj != null)
            jogador = obj.transform;
    }

    // ---------------- dano ----------------
    private void OnCollisionStay2D(Collision2D contato)
    {
        if (EstadoAtual == Estado.Morto || EstadoAtual == Estado.Dormindo || danoDeContato <= 0f)
            return;

        if (recargaDoContato.Ativo)
            return;

        GameObject outro = contato.rigidbody != null ? contato.rigidbody.gameObject : contato.gameObject;

        if (!outro.CompareTag(tagDoJogador))
            return;

        IDanificavel alvo = outro.GetComponentInParent<IDanificavel>();

        if (alvo == null)
            return;

        Vector2 direcao = (Vector2)outro.transform.position - rb.position;
        Vector2 ponto = contato.contactCount > 0 ? contato.GetContact(0).point : (Vector2)outro.transform.position;

        alvo.TomarDano(new DanoInfo(danoDeContato, direcao, empurraoDeContato, ponto, gameObject));
        recargaDoContato.Forcar(intervaloDeContato);
    }

    private void AoLevarGolpe(DanoInfo info)
    {
        if (EstadoAtual == Estado.Morto)
            return;

        Sons.Tocar(Som.Acerto, 0.7f);

        // Levar tiro dormindo acorda (nao tem graca matar inimigo parado de longe).
        if (EstadoAtual == Estado.Dormindo)
            AoAcordar?.Invoke();

        if (Imparavel)
            return;

        EstadoAtual = Estado.Atordoado;
        atordoamento.Forcar(info.Peso == PesoDoGolpe.Forte ? tempoAtordoadoForte : tempoAtordoadoLeve);
    }

    /// <summary>
    /// O Vida chama isto com o empurrao "de plataforma" (lado + cima). Aqui so a forca
    /// importa: a direcao vem do golpe de verdade, que o Vida ja guardou em UltimoGolpe.
    /// </summary>
    public void AplicarImpulsoExterno(Vector2 impulso, float travaSegundos)
    {
        if (EstadoAtual == Estado.Morto || Imparavel)
            return;

        Vector2 direcao = vida.UltimoGolpe.Direcao;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direcao * impulso.magnitude, ForceMode2D.Impulse);

        atordoamento.Armar(travaSegundos);
    }

    protected virtual void Morrer()
    {
        EstadoAtual = Estado.Morto;
        Sons.Tocar(Som.MorteInimigo, 0.8f);

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
            c.enabled = false;

        // Encolhe: o Vida pisca e restaura a cor por conta propria, entao a morte se
        // mostra pelo tamanho, que ninguem mais mexe.
        transform.localScale *= 0.6f;
    }
}
