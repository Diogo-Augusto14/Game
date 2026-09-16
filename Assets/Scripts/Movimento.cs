using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Movimento.cs — controlador de plataforma 2D (versão 3, 08/09/2026).
///
/// Compatível com a cena atual: mesmos nomes de campos públicos (os valores do Inspector continuam),
/// mesmos filhos (GroundCheck / WallCheck), mesmas layers, mesmos apelidos de input ("Horizontal",
/// "Vertical", "Jump") e mesmos parâmetros do Animator (dJump, IsSliding, xVelocity, yVelocity,
/// IsGround, IsTop). Parâmetros NOVOS do Animator são opcionais: IsDashing, IsCrouching, IsHanging.
///
/// Mecânicas: andar · pulo (buffer, coyote, corte) · pulo duplo · wall slide / wall jump ·
///            DASH · AGACHAR · PENDURAR NA BEIRADA.
///
/// NOVO NA VERSÃO 3 — ponte com o sistema de dano (Vida / Espada / Inimigo). São 3 adições,
/// todas marcadas com "NOVO (v3)" no arquivo, e nada do que já existia mudou:
///   1. implementa IControladorDeMovimento;
///   2. propriedade IgnorandoDano (o Vida usa pra respeitar a invencibilidade do dash);
///   3. método AplicarImpulsoExterno (empurrão do dano usando a mesma trava do wall jump).
///
/// Arquitetura:
///   • Input é lido no Update e consumido no FixedUpdate (nunca perde um aperto).
///   • Um enum Estado diz quem manda: Normal, Agachado, Dash, Pendurado. Cada estado tem o seu
///     Atualizar…(); os outros nem rodam. Sem briga entre mecânicas.
///   • Outros scripts só LEEM (Invencivel, EstadoAtual…) ou se inscrevem nos eventos (aoPular, aoPousar…).
///   • Reset() preenche as referências sozinho ao adicionar o componente; OnValidate() impede valor inválido.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class Movimento : MonoBehaviour, IControladorDeMovimento   // NOVO (v3): interface
{
    /// <summary>Quem está no comando neste quadro. Outros scripts podem ler <see cref="EstadoAtual"/>.</summary>
    public enum Estado { Normal, Agachado, Dash, Pendurado }

    // ------------------------------------------------------------------ caixinhas (Inspector)
    [Header("Andar")]
    [Tooltip("Velocidade de caminhada do personagem")]
    public float velocidadeMaxima = 6f;
    public float aceleracao = 20f;
    [Tooltip("Quão rápido ele PARA ao soltar a tecla ou inverter o lado (0 = usa a aceleração)")]
    public float desaceleracao = 40f;
    [Range(0f, 1f), Tooltip("Fração da aceleração que vale no ar (1 = igual ao chão)")]
    public float controleNoAr = 0.75f;

    [Header("pular")]
    [Tooltip("Velocidade do pulo do personagem")]
    public float Jump = 5f;
    [Tooltip("Pulos por 'ciclo' contando o do chão: 1 = sem pulo duplo, 2 = pulo duplo")]
    public int maximoDePulo = 1;
    [Range(0f, 1f), Tooltip("Soltou o botão ainda subindo → a subida é multiplicada por isso (pulinho × pulão)")]
    public float corteDoPulo = 0.5f;
    [Tooltip("Segundos de tolerância pra pular DEPOIS de sair da beirada")]
    public float coyoteTime = 0.1f;
    [Tooltip("Segundos que um aperto de pular fica guardado esperando o chão chegar")]
    public float jumpBuffer = 0.12f;
    [Tooltip("Multiplica a gravidade enquanto cai (1 = igual à subida; 1.6 = queda mais seca)")]
    public float gravidadeNaQueda = 1.6f;
    [Tooltip("Velocidade máxima de queda (0 = sem limite)")]
    public float velocidadeMaximaDeQueda = 18f;

    [Header("Chão")]
    public Transform groundCheck;      // filho GroundCheck (Reset acha pelo nome)
    public float raioChecagem = 0.2f;  // tamanho do círculo de detecção
    public LayerMask chaoLayer;        // qual layer conta como "chão"
    private bool estaNoChao;

    [Header("Parede")]
    public Transform WallCheck;        // filho WallCheck, na frente, altura do peito (Reset acha pelo nome)
    public float raioCheck = 0.2f;     // tamanho do círculo de detecção (também usado pelo LedgeCheck)
    public LayerMask paredeLayer;      // qual layer conta como "parede"
    private bool estaNaParede;
    private bool estaDeslizando;
    public float velocidadeDeslizada = 2f;
    [Tooltip("Pulo na parede")]
    public float wallJumpForcaX = 8f;
    public float wallJumpForcaY = 6f;
    [Tooltip("Segundos sem controle horizontal depois do wall jump (senão segurar pra parede cancela o pulo)")]
    public float travaAposWallJump = 0.15f;
    [Tooltip("Wall jump devolve os pulos no ar (só faz diferença com pulo duplo)")]
    public bool wallJumpRecarregaPulos = true;

    [Header("Dash (arrancada)")]
    [Tooltip("Tecla do dash. LeftShift = Shift esquerdo. Não precisa mexer no Input Manager")]
    public KeyCode teclaDash = KeyCode.LeftShift;
    [Tooltip("Velocidade FIXA durante o dash (linha reta, sem gravidade)")]
    public float dashVelocidade = 18f;
    [Tooltip("Quanto tempo o dash dura, em segundos")]
    public float dashDuracao = 0.15f;
    [Tooltip("Segundos de espera até poder dar outro dash")]
    public float dashRecarga = 0.4f;
    [Tooltip("Segundos que um aperto de dash fica guardado (apertou um tiquinho antes de pousar / da recarga acabar → sai mesmo assim)")]
    public float dashBuffer = 0.1f;
    [Tooltip("Dashes permitidos no ar antes de tocar o chão de novo (0 = dash só no chão)")]
    public int dashesNoAr = 1;
    [Tooltip("Bateu numa parede no meio do dash → o dash termina na hora (em vez de ficar 'empurrando')")]
    public bool dashParaNaParede = true;
    [Tooltip("Durante o dash, outros scripts leem 'Invencivel' = true (dano ignorado)")]
    public bool dashInvencivel = true;
    [Tooltip("Layers que o boneco ATRAVESSA fisicamente durante o dash (ex.: Inimigo, Projetil). Vazio = não atravessa nada")]
    public LayerMask dashAtravessa;

    [Header("Agachar")]
    [Range(0.3f, 0.9f), Tooltip("Altura agachado como fração da altura em pé (0.5 = metade)")]
    public float alturaAgachado = 0.5f;
    [Range(0f, 1f), Tooltip("Fração da velocidadeMaxima enquanto anda agachado")]
    public float velocidadeAgachado = 0.5f;

    [Header("Beirada (ledge grab)")]
    [Tooltip("Filho NOVO: na frente do boneco (mesmo X do WallCheck), na altura da cabeça. Vazio = beirada desligada")]
    public Transform ledgeCheck;
    [Tooltip("Só agarra se estiver segurando a direção da parede (desligado = agarra automaticamente ao cair perto)")]
    public bool precisaSegurarParaAgarrar = false;
    [Tooltip("Segundos pendurado antes de subir sozinho. 0 = só sobe com Pular ou com um Animation Event chamando SubirAgora()")]
    public float tempoPendurado = 0.25f;
    [Tooltip("Quanto o boneco avança pra frente ao subir (deixe maior que metade da largura do colisor)")]
    public float avancoSubida = 0.4f;
    [Tooltip("Segundos sem poder agarrar de novo depois de soltar/subir (evita grudar no mesmo lugar)")]
    public float semAgarrarAposSoltar = 0.3f;

    [Header("Referências")]
    [Tooltip("Preenchido sozinho ao adicionar o componente (Reset) ou no Awake")]
    public Rigidbody2D rb;
    [Tooltip("BoxCollider2D do boneco (acha sozinho). Sem ele, só o agachar fica desligado")]
    public BoxCollider2D colisor;
    private Animator animator;

    [Header("Eventos (arraste partículas, sons, câmera…)")]
    public UnityEvent aoPular          = new UnityEvent();
    public UnityEvent aoPousar         = new UnityEvent();
    public UnityEvent aoIniciarDash    = new UnityEvent();
    public UnityEvent aoTerminarDash   = new UnityEvent();
    public UnityEvent aoAgarrarBeirada = new UnityEvent();
    public UnityEvent aoSubirBeirada   = new UnityEvent();

    // Guarda o último lado olhado (+1 direita, -1 esquerda) pra não "esquecer"
    // pra onde olha quando a tecla é solta (viraria 0 e o boneco sumiria)
    [HideInInspector] public float direcao = 1f;

    // ------------------------------------------------------------------ vitrine (outros scripts só LEEM)
    /// <summary>Estado que está no comando neste quadro.</summary>
    public Estado EstadoAtual { get; private set; } = Estado.Normal;
    /// <summary>True durante o dash quando <see cref="dashInvencivel"/> está ligado. Inimigo/Dano: <c>if (mov.Invencivel) return;</c></summary>
    public bool Invencivel => EstadoAtual == Estado.Dash && dashInvencivel;
    public bool Dashando   => EstadoAtual == Estado.Dash;
    public bool Agachado   => EstadoAtual == Estado.Agachado;
    public bool Pendurado  => EstadoAtual == Estado.Pendurado;
    public bool NoChao     => estaNoChao;
    public bool Deslizando => estaDeslizando;

    // NOVO (v3): é isto que o Vida lê antes de descontar vida. Mesmo valor do Invencivel,
    // só que com o nome que a interface pede — o Vida não precisa conhecer esta classe.
    public bool IgnorandoDano => Invencivel;

    // ------------------------------------------------------------------ gavetas (só este script)
    private float escalaBase;          // tamanho original em X, pra virar sem crescer/encolher
    private float gravidadeBase;       // Gravity Scale do Inspector, lido uma vez no Awake
    private float ladoInput;           // input lido no Update, consumido no FixedUpdate
    private float verticalInput;       // idem, eixo "Vertical" (S / seta pra baixo = agachar / soltar beirada)
    private bool soltouPulo;           // GetButtonUp guardado pro FixedUpdate
    private int pulos = 0;             // pulos já dados neste ciclo (0 = nenhum)
    private bool estavaNoChao;         // quadro anterior, pra disparar aoPousar uma vez só

    // dash
    private float dashDirecao;         // +1 / -1, fixa durante o dash
    private float dashRestante;        // segundos que faltam do dash atual
    private float dashRecargaRestante; // segundos até liberar outro
    private float dashBufferRestante;  // aperto de dash guardado
    private int dashesUsadosNoAr;      // zera ao tocar o chão
    private LayerMask excludeBase;     // Exclude Layers original do colisor (devolvido ao fim do dash)

    // agachar
    private Vector2 tamanhoEmPe;       // size / offset do BoxCollider2D como estavam no Inspector
    private Vector2 offsetEmPe;

    // beirada
    private bool beiradaAtiva;         // false se não existe LedgeCheck
    private float penduradoRestante;   // segundos até subir sozinho
    private float semAgarrarRestante;  // não agarra de novo enquanto > 0

    // timers (segundos restantes) — descem todo FixedUpdate
    private float bufferRestante;      // aperto de pular guardado
    private float coyoteRestante;      // tolerância depois da beirada
    private float travaRestante;       // sem controle horizontal (wall jump E empurrão de dano)
    private float gracaRestante;       // logo após pular, ignora o círculo do chão

    private const float GRACA_APOS_PULO = 0.1f;
    private const float FOLGA_SUBIDA    = 0.02f;   // pés ficam este tanto acima do topo ao subir

    // parâmetros do Animator por hash (mesmos nomes; só evita comparar string todo quadro)
    private static readonly int hDJump       = Animator.StringToHash("dJump");
    private static readonly int hIsSliding   = Animator.StringToHash("IsSliding");
    private static readonly int hXVel        = Animator.StringToHash("xVelocity");
    private static readonly int hYVel        = Animator.StringToHash("yVelocity");
    private static readonly int hIsGround    = Animator.StringToHash("IsGround");
    private static readonly int hIsTop       = Animator.StringToHash("IsTop");
    private static readonly int hIsDashing   = Animator.StringToHash("IsDashing");   // opcional
    private static readonly int hIsCrouching = Animator.StringToHash("IsCrouching"); // opcional
    private static readonly int hIsHanging   = Animator.StringToHash("IsHanging");   // opcional

    // Lista do que EXISTE no Animator Controller — os parâmetros novos só são escritos se você criou
    private readonly HashSet<int> parametrosDoAnimator = new HashSet<int>();

    // ------------------------------------------------------------------ editor (só roda na Unity, fora do Play)
    /// <summary>Chamado ao adicionar o componente ou clicar em ⋮ → Reset: preenche as referências pelo nome dos filhos.</summary>
    void Reset()
    {
        rb          = GetComponent<Rigidbody2D>();
        colisor     = GetComponent<BoxCollider2D>();
        groundCheck = transform.Find("GroundCheck");
        WallCheck   = transform.Find("WallCheck");
        ledgeCheck  = transform.Find("LedgeCheck");
    }

    /// <summary>Roda toda vez que um valor muda no Inspector: impede valores que quebrariam a física.</summary>
    void OnValidate()
    {
        velocidadeMaxima       = Mathf.Max(0f, velocidadeMaxima);
        aceleracao             = Mathf.Max(0.01f, aceleracao);
        desaceleracao          = Mathf.Max(0f, desaceleracao);
        maximoDePulo           = Mathf.Max(1, maximoDePulo);
        coyoteTime             = Mathf.Max(0f, coyoteTime);
        jumpBuffer             = Mathf.Max(0f, jumpBuffer);
        gravidadeNaQueda       = Mathf.Max(0f, gravidadeNaQueda);
        velocidadeMaximaDeQueda = Mathf.Max(0f, velocidadeMaximaDeQueda);
        raioChecagem           = Mathf.Max(0.01f, raioChecagem);
        raioCheck              = Mathf.Max(0.01f, raioCheck);
        dashVelocidade         = Mathf.Max(0f, dashVelocidade);
        dashDuracao            = Mathf.Max(0.01f, dashDuracao);
        dashRecarga            = Mathf.Max(0f, dashRecarga);
        dashBuffer             = Mathf.Max(0f, dashBuffer);
        dashesNoAr             = Mathf.Max(0, dashesNoAr);
        tempoPendurado         = Mathf.Max(0f, tempoPendurado);
        avancoSubida           = Mathf.Max(0f, avancoSubida);
        semAgarrarAposSoltar   = Mathf.Max(0f, semAgarrarAposSoltar);
    }

    // ------------------------------------------------------------------ ciclo de vida
    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();          // busca UMA vez só, igual ao rb
        escalaBase = Mathf.Abs(transform.localScale.x);
        gravidadeBase = rb.gravityScale;

        // Segunda chance pelo nome dos filhos (caso o Reset não tenha rodado)
        if (groundCheck == null) groundCheck = transform.Find("GroundCheck");
        if (WallCheck   == null) WallCheck   = transform.Find("WallCheck");
        if (ledgeCheck  == null) ledgeCheck  = transform.Find("LedgeCheck");

        // Erro claro no Console em vez de NullReferenceException a cada quadro
        if (groundCheck == null || WallCheck == null)
        {
            Debug.LogError($"[Movimento] {name}: crie os filhos GroundCheck e WallCheck (ou arraste nas caixinhas do Inspector).", this);
            enabled = false;
            return;
        }

        // Agachar: guarda o tamanho "em pé" do colisor pra devolver depois
        if (colisor == null) colisor = GetComponent<BoxCollider2D>();
        if (colisor != null)
        {
            tamanhoEmPe = colisor.size;
            offsetEmPe  = colisor.offset;
            excludeBase = colisor.excludeLayers;
        }
        else
        {
            Debug.LogWarning($"[Movimento] {name}: sem BoxCollider2D — o agachar fica desligado (o resto funciona).", this);
        }

        // Beirada: só liga se o filho existe
        beiradaAtiva = ledgeCheck != null;
        if (!beiradaAtiva)
            Debug.LogWarning($"[Movimento] {name}: sem LedgeCheck — pendurar na beirada fica desligado (o resto funciona).", this);

        // Descobre quais parâmetros o Animator Controller tem (evita aviso de parâmetro inexistente)
        foreach (AnimatorControllerParameter p in animator.parameters)
            parametrosDoAnimator.Add(p.nameHash);
    }

    void OnDisable()
    {
        // Se o script for desligado no meio de um dash / pendurado, devolve a física ao normal
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = gravidadeBase;
        }
        if (colisor != null)
        {
            colisor.excludeLayers = excludeBase;
            if (EstadoAtual == Estado.Agachado) { colisor.size = tamanhoEmPe; colisor.offset = offsetEmPe; }
        }
        EstadoAtual = Estado.Normal;
    }

    void Update()
    {
        // Só LEITURA de input aqui (GetButtonDown/Up/KeyDown só valem por um quadro — no FixedUpdate perderia apertos)
        ladoInput     = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        if (Input.GetButtonDown("Jump")) bufferRestante     = jumpBuffer;   // guarda o aperto por um instante
        if (Input.GetButtonUp("Jump"))   soltouPulo         = true;
        if (Input.GetKeyDown(teclaDash)) dashBufferRestante = dashBuffer;

        // após wall jump não vira pra parede de novo; no dash e pendurado o lado fica travado
        bool podeVirar = travaRestante <= 0f && EstadoAtual != Estado.Dash && EstadoAtual != Estado.Pendurado;
        if (podeVirar) Virar(ladoInput);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // --- timers
        bufferRestante      -= dt;
        coyoteRestante      -= dt;
        travaRestante       -= dt;
        gracaRestante       -= dt;
        dashRecargaRestante -= dt;
        dashBufferRestante  -= dt;
        semAgarrarRestante  -= dt;

        LerSensores();

        // --- quem manda neste quadro
        switch (EstadoAtual)
        {
            case Estado.Pendurado: AtualizarPendurado(dt); break;
            case Estado.Dash:      AtualizarDash(dt);      break;
            default:               AtualizarNormal(dt);    break;   // Normal e Agachado
        }

        AtualizarAnimator();
    }

    // ------------------------------------------------------------------ NOVO (v3): ponte com o dano
    /// <summary>
    /// Empurrão vindo de fora: dano, explosão, vento. Chamado pelo Vida quando este boneco leva golpe.
    ///
    /// Por que precisa da trava: o Andar() puxa a velocidade horizontal de volta pro que o teclado
    /// pede, com a desaceleracao (40 por padrão). Sem travar, o impulso do empurrão seria comido em
    /// poucos quadros e o golpe não teria peso nenhum. É a mesma trava que o wall jump já usa.
    /// </summary>
    public void AplicarImpulsoExterno(Vector2 impulso, float travaSegundos)
    {
        if (rb == null) return;

        // Levou golpe pendurado na beirada → solta. Levou no meio do dash (com dashInvencivel
        // desligado) → o dash é cortado. Os dois já devolvem bodyType e gravidade ao normal.
        if (EstadoAtual == Estado.Pendurado) SoltarAgora();
        else if (EstadoAtual == Estado.Dash) TerminarDash(false);

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravidadeBase;
        rb.linearVelocity = Vector2.zero;          // zera pra o empurrão ser sentido por inteiro
        rb.AddForce(impulso, ForceMode2D.Impulse);

        travaRestante = Mathf.Max(travaRestante, travaSegundos);
        soltouPulo = false;
        estaDeslizando = false;
    }

    // ------------------------------------------------------------------ estado NORMAL / AGACHADO
    void AtualizarNormal(float dt)
    {
        if (TentarPendurar()) return;                                     // agarrou: PARA aqui

        AtualizarAgachar();                                               // Normal ↔ Agachado

        if (dashBufferRestante > 0f && PodeDash()) { IniciarDash(); return; }

        Andar(ladoInput, dt);

        // --- deslizar na parede (calculado ANTES do pulo, que depende disso)
        estaDeslizando = estaNaParede && !estaNoChao && ladoInput != 0f && Mathf.Sign(ladoInput) == direcao;
        animator.SetBool(hIsSliding, estaDeslizando);
        if (estaDeslizando)
        {
            Vector2 vel = rb.linearVelocity;
            vel.y = Mathf.Max(vel.y, -velocidadeDeslizada);
            rb.linearVelocity = vel;
        }

        // --- pulo (o aperto guardado no buffer vale por alguns instantes; agachado não pula)
        if (bufferRestante > 0f && EstadoAtual == Estado.Normal)
        {
            bool puloDoChao = estaNoChao || coyoteRestante > 0f;

            if (estaDeslizando)
            {
                PularDaParede();
                ConsumirPulo();
            }
            else if (puloDoChao)
            {
                Pular();
                pulos = 1;
                animator.SetInteger(hDJump, pulos);
                ConsumirPulo();
            }
            else if (pulos < maximoDePulo)                 // pulo no ar (só se sobrou)
            {
                Pular();
                pulos += 1;
                animator.SetInteger(hDJump, pulos);
                ConsumirPulo();
            }
            // sem pulo disponível: o buffer continua vivo até expirar (pousou → pula sozinho)
        }

        // --- corte do pulo (soltou cedo enquanto sobe → pulinho)
        if (soltouPulo)
        {
            Vector2 vel = rb.linearVelocity;
            if (vel.y > 0f) { vel.y *= corteDoPulo; rb.linearVelocity = vel; }
            soltouPulo = false;
        }

        AplicarGravidadeDeQueda();
    }

    void Andar(float lado, float dt)
    {
        if (travaRestante > 0f) return;   // wall jump / empurrão em andamento: teclado não manda no X

        Vector2 vel = rb.linearVelocity;
        float velocidadeAlvo = velocidadeMaxima * lado;
        if (EstadoAtual == Estado.Agachado) velocidadeAlvo *= velocidadeAgachado;

        // parar / inverter usa a desaceleração (mais forte); acelerar usa a aceleração
        bool freando = lado == 0f || (vel.x != 0f && Mathf.Sign(lado) != Mathf.Sign(vel.x));
        float taxa = (freando && desaceleracao > 0f) ? desaceleracao : aceleracao;
        if (!estaNoChao) taxa *= controleNoAr;

        vel.x = Mathf.MoveTowards(vel.x, velocidadeAlvo, taxa * dt);
        rb.linearVelocity = vel;
    }

    void Virar(float lado)
    {
        if (lado != 0f) direcao = Mathf.Sign(lado);
        AplicarEscala();
    }

    void AplicarEscala()
    {
        transform.localScale = new Vector3(direcao * escalaBase, transform.localScale.y, 1f);
    }

    void Pular()
    {
        Vector2 vel = rb.linearVelocity;
        vel.y = Jump;
        rb.linearVelocity = vel;
        aoPular.Invoke();
    }

    void PularDaParede()
    {
        direcao = -direcao;                 // passa a olhar pra longe da parede
        AplicarEscala();

        Vector2 vel = rb.linearVelocity;
        vel.x = wallJumpForcaX * direcao;   // empurra pra longe da parede
        vel.y = wallJumpForcaY;
        rb.linearVelocity = vel;

        travaRestante = travaAposWallJump;  // por um instante, segurar pra parede não cancela o pulo
        if (wallJumpRecarregaPulos) pulos = Mathf.Max(0, maximoDePulo - 1);
        aoPular.Invoke();
    }

    // Um pulo aconteceu: zera o que não pode valer duas vezes
    void ConsumirPulo()
    {
        bufferRestante = 0f;
        coyoteRestante = 0f;
        gracaRestante  = GRACA_APOS_PULO;
        soltouPulo     = false;
    }

    // Queda mais seca que a subida + teto de velocidade de queda
    void AplicarGravidadeDeQueda()
    {
        Vector2 vel = rb.linearVelocity;
        bool caindo = vel.y < 0f && !estaNoChao && !estaDeslizando;
        rb.gravityScale = caindo ? gravidadeBase * gravidadeNaQueda : gravidadeBase;

        if (velocidadeMaximaDeQueda > 0f && vel.y < -velocidadeMaximaDeQueda)
        {
            vel.y = -velocidadeMaximaDeQueda;
            rb.linearVelocity = vel;
        }
    }

    // ------------------------------------------------------------------ sensores
    void LerSensores()
    {
        bool tocaChao = Physics2D.OverlapCircle(groundCheck.position, raioChecagem, chaoLayer);
        estaNoChao   = tocaChao && gracaRestante <= 0f;   // no 1º instante do pulo o círculo ainda toca o chão: ignorar
        estaNaParede = Physics2D.OverlapCircle(WallCheck.position, raioCheck, paredeLayer);

        if (estaNoChao)
        {
            pulos = 0;
            dashesUsadosNoAr = 0;
            coyoteRestante = coyoteTime;
            animator.SetInteger(hDJump, pulos);
            if (!estavaNoChao) aoPousar.Invoke();          // uma vez por pouso
        }
        else if (coyoteRestante <= 0f && pulos == 0)
        {
            // Caiu da beirada sem pular: sobra exatamente UM pulo no ar
            // (com maximoDePulo = 1 sobra 1; com pulo duplo ligado, sobra 1 também — não 2)
            pulos = Mathf.Max(0, maximoDePulo - 1);
        }
        estavaNoChao = estaNoChao;
    }

    // beirada = tem parede no peito (WallCheck) e NÃO tem parede na cabeça (LedgeCheck)
    bool LivreAcimaDaParede()
    {
        return beiradaAtiva && !Physics2D.OverlapCircle(ledgeCheck.position, raioCheck, paredeLayer);
    }

    // ------------------------------------------------------------------ estado DASH
    bool PodeDash()
    {
        if (dashRecargaRestante > 0f) return false;        // ainda recarregando
        if (EstadoAtual == Estado.Agachado) return false;  // agachado num túnel não pode arrancar em pé
        bool noChao = estaNoChao || coyoteRestante > 0f;
        return noChao || dashesUsadosNoAr < dashesNoAr;    // no ar só se ainda sobrou
    }

    void IniciarDash()
    {
        // direção: pra onde a tecla aponta; sem tecla, pra onde o boneco olha
        dashDirecao = ladoInput != 0f ? Mathf.Sign(ladoInput) : direcao;
        direcao = dashDirecao;
        AplicarEscala();

        EstadoAtual        = Estado.Dash;
        dashRestante       = dashDuracao;
        dashBufferRestante = 0f;
        if (!(estaNoChao || coyoteRestante > 0f)) dashesUsadosNoAr += 1;

        estaDeslizando = false;
        animator.SetBool(hIsSliding, false);
        soltouPulo = false;

        if (colisor != null) colisor.excludeLayers = excludeBase.value | dashAtravessa.value;   // atravessa inimigos etc.

        AplicarVelocidadeDoDash();
        aoIniciarDash.Invoke();
    }

    void AtualizarDash(float dt)
    {
        dashRestante -= dt;
        AplicarVelocidadeDoDash();                         // todo quadro: linha reta, velocidade fixa

        bool bateuNaParede = dashParaNaParede && estaNaParede;
        if (dashRestante <= 0f || bateuNaParede) TerminarDash(bateuNaParede);
    }

    void AplicarVelocidadeDoDash()
    {
        rb.gravityScale   = 0f;                            // sem gravidade = linha reta de verdade
        rb.linearVelocity = new Vector2(dashDirecao * dashVelocidade, 0f);
    }

    void TerminarDash(bool bateuNaParede)
    {
        EstadoAtual = Estado.Normal;
        dashRecargaRestante = dashRecarga;
        rb.gravityScale = gravidadeBase;
        if (colisor != null) colisor.excludeLayers = excludeBase;

        // sai do dash já na velocidade de corrida (sem "freada"); bateu na parede → para
        Vector2 vel = rb.linearVelocity;
        vel.x = bateuNaParede ? 0f : dashDirecao * velocidadeMaxima;
        rb.linearVelocity = vel;
        aoTerminarDash.Invoke();
    }

    // ------------------------------------------------------------------ estado AGACHADO
    void AtualizarAgachar()
    {
        if (colisor == null) return;

        // quer agachar: segurando pra baixo, no chão, e sem pulo pedido (pular levanta antes)
        bool quer = verticalInput < -0.5f && estaNoChao && bufferRestante <= 0f;

        if (quer && EstadoAtual == Estado.Normal)
            SetAgachado(true);
        else if (!quer && EstadoAtual == Estado.Agachado && !TetoBloqueado())
            SetAgachado(false);                            // só levanta se cabe
    }

    void SetAgachado(bool valor)
    {
        EstadoAtual = valor ? Estado.Agachado : Estado.Normal;
        if (valor)
        {
            float alturaNova = tamanhoEmPe.y * alturaAgachado;
            colisor.size   = new Vector2(tamanhoEmPe.x, alturaNova);
            // desce o centro pra metade da diferença: o PÉ fica no mesmo lugar, encolhe por cima
            colisor.offset = new Vector2(offsetEmPe.x, offsetEmPe.y - (tamanhoEmPe.y - alturaNova) * 0.5f);
        }
        else
        {
            colisor.size   = tamanhoEmPe;
            colisor.offset = offsetEmPe;
        }
    }

    // Caixa exatamente na região da "cabeça" (entre o topo agachado e o topo em pé). Tem chão/parede ali? Não levanta.
    bool TetoBloqueado()
    {
        CaixaDaCabeca(out Vector2 centro, out Vector2 tamanho);
        return Physics2D.OverlapBox(centro, tamanho, 0f, chaoLayer.value | paredeLayer.value);
    }

    void CaixaDaCabeca(out Vector2 centro, out Vector2 tamanho)
    {
        Vector2 pes = transform.TransformPoint(offsetEmPe + Vector2.down * (tamanhoEmPe.y * 0.5f));
        float ex = Mathf.Abs(transform.lossyScale.x);
        float ey = Mathf.Abs(transform.lossyScale.y);
        float alturaEmPe   = tamanhoEmPe.y * ey;
        float alturaBaixo  = alturaEmPe * alturaAgachado;
        float alturaCabeca = alturaEmPe - alturaBaixo;

        centro  = pes + Vector2.up * (alturaBaixo + alturaCabeca * 0.5f);
        tamanho = new Vector2(tamanhoEmPe.x * ex * 0.9f, alturaCabeca * 0.95f);   // um pouco menor pra não pegar as bordas
    }

    // ------------------------------------------------------------------ estado PENDURADO (ledge grab)
    bool TentarPendurar()
    {
        if (!beiradaAtiva || semAgarrarRestante > 0f) return false;
        if (estaNoChao || EstadoAtual != Estado.Normal) return false;
        if (!estaNaParede || !LivreAcimaDaParede()) return false;         // parede no peito + nada na cabeça = ponta do bloco
        if (rb.linearVelocity.y > 0.5f) return false;                     // subindo passa direto; só agarra parando/caindo
        if (precisaSegurarParaAgarrar && ladoInput * direcao <= 0f) return false;

        EstadoAtual = Estado.Pendurado;
        penduradoRestante = tempoPendurado;

        // Kinematic = a física não empurra nem puxa; fica cravado no lugar sem zerar velocidade todo quadro
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        bufferRestante = 0f;                  // um pulo apertado antes não vira subida instantânea
        soltouPulo     = false;
        estaDeslizando = false;
        animator.SetBool(hIsSliding, false);

        // pendurado recupera tudo, como se fosse chão
        pulos = 0;
        dashesUsadosNoAr = 0;
        animator.SetInteger(hDJump, pulos);

        // encosta as "mãos" (LedgeCheck) exatamente no topo do bloco, pra animação bater
        if (EncontrarTopoDaBeirada(out float topoY))
        {
            float maosParaPivo = transform.position.y - ledgeCheck.position.y;
            Vector2 pos = rb.position;
            pos.y = topoY + maosParaPivo;
            rb.position = pos;
            transform.position = pos;
        }

        aoAgarrarBeirada.Invoke();
        return true;
    }

    void AtualizarPendurado(float dt)
    {
        penduradoRestante -= dt;

        if (verticalInput < -0.5f)                          { SoltarAgora(); return; }   // baixo = solta
        if (bufferRestante > 0f)                            { SubirAgora();  return; }   // pular = sobe já
        if (tempoPendurado > 0f && penduradoRestante <= 0f) { SubirAgora();  return; }   // tempo acabou = sobe sozinho
        // tempoPendurado = 0 → espera Pular ou um Animation Event chamar SubirAgora()
    }

    /// <summary>Solta a beirada e cai. Público pra Animation Event / outros scripts.</summary>
    public void SoltarAgora()
    {
        if (EstadoAtual != Estado.Pendurado) return;
        EstadoAtual = Estado.Normal;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravidadeBase;
        semAgarrarRestante = semAgarrarAposSoltar;
        coyoteRestante = coyoteTime;          // soltou e apertou pular logo em seguida? ainda vale um pulo
    }

    /// <summary>Sobe pra cima do bloco. Público pra Animation Event (último quadro da animação de subir) / outros scripts.</summary>
    public void SubirAgora()
    {
        if (EstadoAtual != Estado.Pendurado) return;
        EstadoAtual = Estado.Normal;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravidadeBase;
        bufferRestante = 0f;
        semAgarrarRestante = semAgarrarAposSoltar;
        gracaRestante = 0f;

        // pés → pivô: quanto o transform fica acima da base do colisor (ou do GroundCheck)
        float baseY = colisor != null ? colisor.bounds.min.y : groundCheck.position.y;
        float pesParaPivo = transform.position.y - baseY;

        float destinoX = WallCheck.position.x + direcao * avancoSubida;
        float topo     = EncontrarTopoDaBeirada(out float topoY) ? topoY : ledgeCheck.position.y;
        Vector2 destino = new Vector2(destinoX, topo + pesParaPivo + FOLGA_SUBIDA);

        rb.position = destino;                // teletransporte curto: pés em cima do bloco
        transform.position = destino;
        rb.linearVelocity = Vector2.zero;
        aoSubirBeirada.Invoke();
    }

    // Raio pra baixo, um pouco à frente do LedgeCheck: onde ele bate é o topo do bloco
    bool EncontrarTopoDaBeirada(out float topoY)
    {
        Vector2 origem = new Vector2(ledgeCheck.position.x + direcao * avancoSubida, ledgeCheck.position.y + 0.05f);
        float distancia = (ledgeCheck.position.y - WallCheck.position.y) + 0.1f;
        RaycastHit2D hit = Physics2D.Raycast(origem, Vector2.down, distancia, paredeLayer.value | chaoLayer.value);
        topoY = hit.collider != null ? hit.point.y : 0f;
        return hit.collider != null;
    }

    // ------------------------------------------------------------------ animator
    // Mesmos parâmetros e mesmos valores de sempre — só escritos num lugar só, por último
    void AtualizarAnimator()
    {
        Vector2 vel = rb.linearVelocity;
        animator.SetFloat(hXVel, vel.x);
        animator.SetFloat(hYVel, vel.y);
        animator.SetBool(hIsGround, estaNoChao);
        animator.SetBool(hIsTop, ladoInput == 0f);

        // novos: só escreve se o parâmetro existir no Animator Controller
        SetBoolSeguro(hIsDashing,   EstadoAtual == Estado.Dash);
        SetBoolSeguro(hIsCrouching, EstadoAtual == Estado.Agachado);
        SetBoolSeguro(hIsHanging,   EstadoAtual == Estado.Pendurado);
    }

    void SetBoolSeguro(int hash, bool valor)
    {
        if (parametrosDoAnimator.Contains(hash)) animator.SetBool(hash, valor);
    }

    // ------------------------------------------------------------------ gizmos
    // Círculos na Scene com o Boneco selecionado — pra ajustar raio e posição sem chutar
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;                   // amarelo = pés
            Gizmos.DrawWireSphere(groundCheck.position, raioChecagem);
        }
        if (WallCheck != null)
        {
            Gizmos.color = Color.cyan;                     // azul = peito (parede)
            Gizmos.DrawWireSphere(WallCheck.position, raioCheck);
        }
        if (ledgeCheck != null)
        {
            Gizmos.color = Color.magenta;                  // rosa = cabeça (beirada)
            Gizmos.DrawWireSphere(ledgeCheck.position, raioCheck);
        }

        // caixa vermelha = região da cabeça checada antes de levantar (só quando a cena roda)
        if (Application.isPlaying && colisor != null)
        {
            CaixaDaCabeca(out Vector2 centro, out Vector2 tamanho);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(centro, tamanho);
        }
    }
}
