using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Movimento.cs — controlador de plataforma 2D completo (versao 4).
///
/// Mecanicas: andar · correr · agachar · escorregar · pular · pulo duplo · corte do pulo ·
///            coyote time · wall slide · wall jump · dash no chao e no ar · esquiva pra tras ·
///            pendurar e subir beirada · subir escada · escorregar na escada ·
///            pouso rolado · atordoamento ao levar golpe · morte.
///
/// Arquitetura — tres regras que valem pro arquivo inteiro:
///
///   1. UM estado manda por quadro. O enum <see cref="Estado"/> diz quem, e cada estado
///      tem o seu Atualizar…(dt). Nenhuma mecanica precisa saber da outra, e e por isso
///      que dash, agachar, beirada e escada nao brigam entre si.
///   2. Entrada e lida pelo componente <see cref="Entrada"/> (no Update) e consumida aqui
///      (no FixedUpdate). Nada de aperto perdido entre um quadro de fisica e outro.
///   3. Outros scripts so LEEM (NoChao, EstadoAtual, Invencivel…) ou escutam os eventos.
///      Quem mexe na velocidade deste Rigidbody e este arquivo, e mais ninguem — com uma
///      unica porta de entrada pra empurroes de fora: <see cref="AplicarImpulsoExterno"/>.
///
/// A animacao NAO e decidida aqui. Quem traduz estado em clipe e o AnimacaoDoJogador,
/// que so le esta vitrine. Movimento cuida de fisica; animacao cuida de desenho.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class Movimento : MonoBehaviour, IControladorDeMovimento
{
    /// <summary>Quem esta no comando neste quadro.</summary>
    public enum Estado
    {
        Normal,
        Agachado,
        Escorregando,
        Dash,
        EsquivaTras,
        ParedeDeslizando,
        Pendurado,
        SubindoBeirada,
        Escada,
        Atordoado,
        Morto
    }

    // ================================================================ andar
    [Header("Andar e correr")]
    [Tooltip("Velocidade de corrida (a padrao). Segurar a tecla de andar devagar usa a de baixo")]
    public float velocidadeMaxima = 3.2f;

    [Tooltip("Velocidade segurando a tecla de andar devagar")]
    public float velocidadeDeCaminhada = 1.4f;

    [Tooltip("Quao rapido ele chega na velocidade alvo")]
    public float aceleracao = 26f;

    [Tooltip("Quao rapido ele PARA ao soltar a tecla ou inverter o lado (0 = usa a aceleracao)")]
    public float desaceleracao = 42f;

    [Range(0f, 1f), Tooltip("Fracao da aceleracao que vale no ar (1 = igual ao chao)")]
    public float controleNoAr = 0.8f;

    // ================================================================ pulo
    [Header("Pulo")]
    [Tooltip("Velocidade vertical do pulo")]
    public float Jump = 6.5f;

    [Tooltip("Pulos por ciclo contando o do chao: 1 = sem pulo duplo, 2 = pulo duplo")]
    public int maximoDePulo = 2;

    [Range(0f, 1f), Tooltip("Soltou o botao ainda subindo -> a subida e multiplicada por isso (pulinho x pulao)")]
    public float corteDoPulo = 0.45f;

    [Tooltip("Segundos de tolerancia pra pular DEPOIS de sair da beirada")]
    public float coyoteTime = 0.1f;

    [Tooltip("Multiplica a gravidade enquanto cai (1 = igual a subida; 1.6 = queda mais seca)")]
    public float gravidadeNaQueda = 1.5f;

    [Tooltip("Velocidade maxima de queda (0 = sem limite)")]
    public float velocidadeMaximaDeQueda = 14f;

    [Tooltip("Caiu mais que esta altura (em unidades) -> pousa rolando")]
    public float alturaDoPousoRolado = 2.2f;

    // ================================================================ chao e parede
    [Header("Chao")]
    [Tooltip("Filho GroundCheck, nos pes. O Bootstrap cria se faltar")]
    public Transform groundCheck;
    public float raioChecagem = 0.09f;
    [Tooltip("Vazio = usa as camadas de chao conhecidas (Chao / Ground / ground)")]
    public LayerMask chaoLayer;

    [Header("Parede")]
    [Tooltip("Filho WallCheck, na frente, na altura do peito")]
    public Transform WallCheck;
    [Tooltip("Tamanho do circulo do WallCheck e do LedgeCheck")]
    public float raioCheck = 0.08f;
    [Tooltip("Vazio = usa as camadas de parede conhecidas (Parede / Wall)")]
    public LayerMask paredeLayer;

    [Tooltip("Velocidade de descida grudado na parede")]
    public float velocidadeDeslizada = 1.6f;

    [Tooltip("Pulo na parede: forca pro lado")]
    public float wallJumpForcaX = 4.5f;

    [Tooltip("Pulo na parede: forca pra cima")]
    public float wallJumpForcaY = 6.2f;

    [Tooltip("Segundos sem controle horizontal depois do wall jump (senao segurar pra parede cancela o pulo)")]
    public float travaAposWallJump = 0.16f;

    [Tooltip("Wall jump devolve os pulos no ar")]
    public bool wallJumpRecarregaPulos = true;

    // ================================================================ dash
    [Header("Dash (arrancada)")]
    [Tooltip("Velocidade FIXA durante o dash (linha reta, sem gravidade)")]
    public float dashVelocidade = 7.5f;

    [Tooltip("Quanto tempo o dash dura, em segundos")]
    public float dashDuracao = 0.16f;

    [Tooltip("Segundos de espera ate poder dar outro dash")]
    public float dashRecarga = 0.35f;

    [Tooltip("Dashes permitidos no ar antes de tocar o chao de novo (0 = dash so no chao)")]
    public int dashesNoAr = 1;

    [Tooltip("Bateu numa parede no meio do dash -> o dash termina na hora")]
    public bool dashParaNaParede = true;

    [Tooltip("Durante o dash, outros scripts leem Invencivel = true (dano ignorado)")]
    public bool dashInvencivel = true;

    [Tooltip("Camadas que o boneco ATRAVESSA durante o dash (ex.: Inimigo). Vazio = nao atravessa nada")]
    public LayerMask dashAtravessa;

    [Header("Esquiva pra tras")]
    [Tooltip("Dash segurando a direcao CONTRARIA a que ele olha -> esquiva pra tras")]
    public bool esquivaAtivada = true;

    public float esquivaVelocidade = 5.5f;
    public float esquivaDuracao = 0.22f;

    // ================================================================ agachar e escorregar
    [Header("Agachar")]
    [Range(0.3f, 0.9f), Tooltip("Altura agachado como fracao da altura em pe")]
    public float alturaAgachado = 0.55f;

    [Range(0f, 1f), Tooltip("Fracao da velocidade enquanto anda agachado")]
    public float velocidadeAgachado = 0.45f;

    [Header("Escorregar")]
    [Tooltip("Dash correndo e segurando pra baixo -> escorrega por baixo das coisas")]
    public bool escorregarAtivado = true;

    public float escorregarVelocidade = 6.5f;
    public float escorregarDuracao = 0.38f;

    // ================================================================ beirada
    [Header("Beirada (ledge grab)")]
    [Tooltip("Filho LedgeCheck: na frente, na altura da cabeca. Vazio = beirada desligada")]
    public Transform ledgeCheck;

    [Tooltip("So agarra se estiver segurando a direcao da parede")]
    public bool precisaSegurarParaAgarrar = false;

    [Tooltip("Quanto o boneco avanca pra frente ao subir (maior que metade da largura do colisor)")]
    public float avancoSubida = 0.22f;

    [Tooltip("Segundos sem poder agarrar de novo depois de soltar/subir")]
    public float semAgarrarAposSoltar = 0.3f;

    [Tooltip("Segundos que a subida da beirada leva. Deixe igual a duracao do clipe de subir")]
    public float duracaoDaSubida = 0.45f;

    // ================================================================ escada
    [Header("Escada")]
    [Tooltip("Velocidade subindo/descendo a escada")]
    public float escadaVelocidade = 1.8f;

    [Tooltip("Velocidade escorregando pela escada (segurando baixo + dash)")]
    public float escadaVelocidadeEscorregando = 5f;

    // ================================================================ referencias
    [Header("Referencias (preenchidas sozinhas)")]
    public Rigidbody2D rb;

    [Tooltip("BoxCollider2D do boneco. Sem ele, agachar e escorregar ficam desligados")]
    public BoxCollider2D colisor;

    [SerializeField] private Entrada entrada;

    // ================================================================ eventos
    [Header("Eventos (arraste sons, particulas, camera...)")]
    public UnityEvent aoPular = new UnityEvent();
    public UnityEvent aoPousar = new UnityEvent();
    public UnityEvent aoPousarRolando = new UnityEvent();
    public UnityEvent aoIniciarDash = new UnityEvent();
    public UnityEvent aoTerminarDash = new UnityEvent();
    public UnityEvent aoEscorregar = new UnityEvent();
    public UnityEvent aoEsquivar = new UnityEvent();
    public UnityEvent aoPularDaParede = new UnityEvent();
    public UnityEvent aoAgarrarBeirada = new UnityEvent();
    public UnityEvent aoSubirBeirada = new UnityEvent();

    /// <summary>Ultimo lado olhado: +1 direita, -1 esquerda.</summary>
    [HideInInspector] public float direcao = 1f;

    // ================================================================ vitrine (so leitura)
    public Estado EstadoAtual { get; private set; } = Estado.Normal;

    public bool NoChao { get; private set; }

    public bool NaParede { get; private set; }

    public bool Dashando => EstadoAtual == Estado.Dash;

    public bool Agachado => EstadoAtual == Estado.Agachado;

    public bool Escorregando => EstadoAtual == Estado.Escorregando;

    public bool Esquivando => EstadoAtual == Estado.EsquivaTras;

    public bool Pendurado => EstadoAtual == Estado.Pendurado;

    public bool SubindoBeirada => EstadoAtual == Estado.SubindoBeirada;

    public bool NaEscada => EstadoAtual == Estado.Escada;

    public bool Atordoado => EstadoAtual == Estado.Atordoado;

    public bool Morto => EstadoAtual == Estado.Morto;

    public bool Deslizando => EstadoAtual == Estado.ParedeDeslizando;

    /// <summary>True enquanto um dash/esquiva invencivel esta rolando.</summary>
    public bool Invencivel => dashInvencivel && (Dashando || Esquivando || Escorregando);

    /// <summary>E isto que o Vida le antes de descontar vida (IControladorDeMovimento).</summary>
    public bool IgnorandoDano => Invencivel;

    /// <summary>Velocidade horizontal, sempre positiva. Pra escolher parado/andar/correr.</summary>
    public float VelocidadeHorizontal => rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;

    public float VelocidadeVertical => rb != null ? rb.linearVelocity.y : 0f;

    /// <summary>True quando o jogador esta pedindo pra correr solto (nao devagar).</summary>
    public bool CorrendoSolto => entrada != null && !entrada.AndarDevagar && Mathf.Abs(LadoPedido) > 0.1f;

    /// <summary>Lado que o teclado pede agora (-1, 0, +1).</summary>
    public float LadoPedido => entrada != null ? entrada.Lado : 0f;

    /// <summary>Pulos ja gastados neste ciclo. 0 = nenhum, 2 = usou o pulo duplo.</summary>
    public int PulosUsados => pulos;

    /// <summary>True se o ultimo pulo foi o segundo (pra escolher o clipe de pulo duplo).</summary>
    public bool UltimoPuloFoiDuplo { get; private set; }

    /// <summary>True no quadro em que o boneco pousa de uma queda alta.</summary>
    public bool PousouRolando { get; private set; }

    /// <summary>Progresso de 0 a 1 da subida da beirada.</summary>
    public float ProgressoDaSubida => duracaoDaSubida <= 0f ? 1f : 1f - (subidaRestante.Restante / duracaoDaSubida);

    /// <summary>Escada em que ele esta encostado agora (null = nenhuma).</summary>
    public Escada EscadaEncostada { get; private set; }

    /// <summary>True enquanto ele escorrega pra baixo na escada.</summary>
    public bool EscorregandoNaEscada { get; private set; }

    // ================================================================ gavetas
    private float escalaBase;
    private float gravidadeBase;
    private bool estavaNoChao;
    private float alturaMaximaNoAr;

    private int pulos;

    private Cronometro coyote;
    private Cronometro trava;              // sem controle horizontal (wall jump, empurrao)
    private Cronometro gracaAposPulo;      // ignora o circulo do chao logo depois de pular
    private Cronometro dashRestante;
    private Cronometro dashRecargaRestante;
    private Cronometro semAgarrar;
    private Cronometro subidaRestante;
    private Cronometro atordoamento;

    private float dashDirecao;
    private int dashesUsadosNoAr;
    private LayerMask excludeBase;

    private Vector2 tamanhoEmPe;
    private Vector2 offsetEmPe;
    private bool colisorEncolhido;

    private bool beiradaAtiva;
    private float topoDaBeirada;

    private const float GRACA_APOS_PULO = 0.08f;
    private const float FOLGA_SUBIDA = 0.02f;

    // ================================================================ editor
    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        colisor = GetComponent<BoxCollider2D>();
        entrada = GetComponent<Entrada>();
        groundCheck = transform.Find("GroundCheck");
        WallCheck = transform.Find("WallCheck");
        ledgeCheck = transform.Find("LedgeCheck");
    }

    private void OnValidate()
    {
        velocidadeMaxima = Mathf.Max(0f, velocidadeMaxima);
        velocidadeDeCaminhada = Mathf.Clamp(velocidadeDeCaminhada, 0f, velocidadeMaxima);
        aceleracao = Mathf.Max(0.01f, aceleracao);
        desaceleracao = Mathf.Max(0f, desaceleracao);
        maximoDePulo = Mathf.Max(1, maximoDePulo);
        coyoteTime = Mathf.Max(0f, coyoteTime);
        gravidadeNaQueda = Mathf.Max(0f, gravidadeNaQueda);
        velocidadeMaximaDeQueda = Mathf.Max(0f, velocidadeMaximaDeQueda);
        raioChecagem = Mathf.Max(0.01f, raioChecagem);
        raioCheck = Mathf.Max(0.01f, raioCheck);
        dashVelocidade = Mathf.Max(0f, dashVelocidade);
        dashDuracao = Mathf.Max(0.01f, dashDuracao);
        dashRecarga = Mathf.Max(0f, dashRecarga);
        dashesNoAr = Mathf.Max(0, dashesNoAr);
        avancoSubida = Mathf.Max(0f, avancoSubida);
        semAgarrarAposSoltar = Mathf.Max(0f, semAgarrarAposSoltar);
        duracaoDaSubida = Mathf.Max(0.05f, duracaoDaSubida);
        escadaVelocidade = Mathf.Max(0f, escadaVelocidade);
    }

    // ================================================================ ciclo de vida
    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (colisor == null) colisor = GetComponent<BoxCollider2D>();
        if (entrada == null) entrada = GetComponent<Entrada>();

        if (entrada == null)
            entrada = gameObject.AddComponent<Entrada>();

        escalaBase = Mathf.Abs(transform.localScale.x);
        if (escalaBase < 0.0001f) escalaBase = 1f;

        gravidadeBase = rb.gravityScale;
        if (gravidadeBase <= 0f) gravidadeBase = 1f;

        // Camada vazia no Inspector = usa as conhecidas do projeto. Assim o componente
        // funciona mesmo recem-adicionado, sem ninguem marcar as caixinhas.
        //
        // Chao inclui as paredes de proposito: da pra FICAR EM PE em cima de um bloco de
        // parede (o topo do bloco da beirada, por exemplo). O contrario nao vale — a
        // checagem de parede usa so a camada de parede, senao ele grudaria no chao.
        if (chaoLayer.value == 0) chaoLayer = Camadas.MascaraDeSolido;
        if (paredeLayer.value == 0) paredeLayer = Camadas.MascaraDeParede;

        GarantirSensores();

        if (colisor != null)
        {
            tamanhoEmPe = colisor.size;
            offsetEmPe = colisor.offset;
            excludeBase = colisor.excludeLayers;
        }

        beiradaAtiva = ledgeCheck != null;
    }

    /// <summary>
    /// Cria os filhos de sensor que faltarem em vez de desligar o script. O projeto tinha
    /// um "GoundCheck " (com erro de digitacao e espaco no fim) que fazia o Movimento se
    /// desligar inteiro no Awake — um jogo que nao anda por causa de uma letra.
    /// </summary>
    private void GarantirSensores()
    {
        if (groundCheck == null) groundCheck = AcharFilhoParecido("GroundCheck", "GoundCheck");
        if (WallCheck == null) WallCheck = AcharFilhoParecido("WallCheck", "WalCheck");
        if (ledgeCheck == null) ledgeCheck = AcharFilhoParecido("LedgeCheck");

        float meiaLargura = colisor != null ? colisor.size.x * 0.5f : 0.12f;
        float altura = colisor != null ? colisor.size.y : 0.55f;

        if (groundCheck == null)
            groundCheck = CriarSensor("GroundCheck", new Vector3(0f, 0f, 0f));

        if (WallCheck == null)
            WallCheck = CriarSensor("WallCheck", new Vector3(meiaLargura + 0.02f, altura * 0.5f, 0f));

        if (ledgeCheck == null)
            ledgeCheck = CriarSensor("LedgeCheck", new Vector3(meiaLargura + 0.02f, altura, 0f));
    }

    private Transform AcharFilhoParecido(params string[] nomes)
    {
        foreach (Transform filho in transform)
        {
            string nome = filho.name.Trim();

            for (int i = 0; i < nomes.Length; i++)
            {
                if (string.Equals(nome, nomes[i], System.StringComparison.OrdinalIgnoreCase))
                    return filho;
            }
        }

        return null;
    }

    private Transform CriarSensor(string nome, Vector3 posicaoLocal)
    {
        GameObject novo = new GameObject(nome);
        novo.transform.SetParent(transform, false);
        novo.transform.localPosition = posicaoLocal;
        return novo.transform;
    }

    private void OnDisable()
    {
        // Desligar no meio de um dash/pendurado nao pode deixar a fisica torta.
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = gravidadeBase;
        }

        if (colisor != null)
        {
            colisor.excludeLayers = excludeBase;
            EncolherColisor(false);
        }

        if (EstadoAtual != Estado.Morto)
            EstadoAtual = Estado.Normal;
    }

    private void Update()
    {
        // Virar e visual: acontece no Update pra responder na hora do aperto.
        bool podeVirar = !trava.Ativo
                      && EstadoAtual != Estado.Dash
                      && EstadoAtual != Estado.EsquivaTras
                      && EstadoAtual != Estado.Escorregando
                      && EstadoAtual != Estado.Pendurado
                      && EstadoAtual != Estado.SubindoBeirada
                      && EstadoAtual != Estado.Atordoado
                      && EstadoAtual != Estado.Morto;

        if (podeVirar)
            Virar(LadoPedido);
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        coyote.Contar(dt);
        trava.Contar(dt);
        gracaAposPulo.Contar(dt);
        dashRestante.Contar(dt);
        dashRecargaRestante.Contar(dt);
        semAgarrar.Contar(dt);
        subidaRestante.Contar(dt);
        atordoamento.Contar(dt);

        PousouRolando = false;

        LerSensores();

        switch (EstadoAtual)
        {
            case Estado.Morto: AtualizarMorto(); break;
            case Estado.Atordoado: AtualizarAtordoado(); break;
            case Estado.SubindoBeirada: AtualizarSubindoBeirada(); break;
            case Estado.Pendurado: AtualizarPendurado(); break;
            case Estado.Escada: AtualizarEscada(dt); break;
            case Estado.Dash: AtualizarDash(); break;
            case Estado.EsquivaTras: AtualizarEsquiva(); break;
            case Estado.Escorregando: AtualizarEscorregar(); break;
            case Estado.ParedeDeslizando: AtualizarParede(dt); break;
            default: AtualizarNormal(dt); break;    // Normal e Agachado
        }
    }

    // ================================================================ sensores
    private void LerSensores()
    {
        bool tocaChao = groundCheck != null
                     && Physics2D.OverlapCircle(groundCheck.position, raioChecagem, chaoLayer);

        NoChao = tocaChao && !gracaAposPulo.Ativo;

        NaParede = WallCheck != null
                && Physics2D.OverlapCircle(WallCheck.position, raioCheck, paredeLayer);

        if (NoChao)
        {
            if (!estavaNoChao)
                Pousar();

            pulos = 0;
            dashesUsadosNoAr = 0;
            coyote.Forcar(coyoteTime);
            alturaMaximaNoAr = transform.position.y;
        }
        else
        {
            // Caiu da beirada sem pular: sobra exatamente UM pulo no ar.
            if (!coyote.Ativo && pulos == 0)
                pulos = Mathf.Max(0, maximoDePulo - 1);

            alturaMaximaNoAr = Mathf.Max(alturaMaximaNoAr, transform.position.y);
        }

        estavaNoChao = NoChao;
    }

    private void Pousar()
    {
        float queda = alturaMaximaNoAr - transform.position.y;

        aoPousar.Invoke();

        if (queda >= alturaDoPousoRolado)
        {
            PousouRolando = true;
            aoPousarRolando.Invoke();
        }
    }

    /// <summary>Beirada = parede no peito e NADA na cabeca: o boneco esta na ponta do bloco.</summary>
    private bool CabecaLivre()
    {
        return beiradaAtiva
            && !Physics2D.OverlapCircle(ledgeCheck.position, raioCheck, paredeLayer);
    }

    // ================================================================ estado NORMAL / AGACHADO
    private void AtualizarNormal(float dt)
    {
        if (TentarEntrarNaEscada()) return;
        if (TentarPendurar()) return;
        if (TentarDash()) return;

        AtualizarAgachar();

        Andar(LadoPedido, dt);

        // Grudou na parede caindo? Vira estado de parede (que tem o seu proprio pulo).
        if (!NoChao && NaParede && EstadoAtual == Estado.Normal && rb.linearVelocity.y <= 0.1f
            && Mathf.Abs(LadoPedido) > 0.1f && Mathf.Approximately(Mathf.Sign(LadoPedido), direcao))
        {
            EntrarNaParede();
            return;
        }

        TentarPular();
        AplicarCorteDoPulo();
        AplicarGravidadeDeQueda();
    }

    private void Andar(float lado, float dt)
    {
        if (trava.Ativo)
            return;

        Vector2 vel = rb.linearVelocity;

        float alvo = VelocidadeAlvo() * lado;

        bool freando = Mathf.Approximately(lado, 0f)
                    || (!Mathf.Approximately(vel.x, 0f) && !Mathf.Approximately(Mathf.Sign(lado), Mathf.Sign(vel.x)));

        float taxa = (freando && desaceleracao > 0f) ? desaceleracao : aceleracao;

        if (!NoChao)
            taxa *= controleNoAr;

        vel.x = Mathf.MoveTowards(vel.x, alvo, taxa * dt);
        rb.linearVelocity = vel;
    }

    private float VelocidadeAlvo()
    {
        if (EstadoAtual == Estado.Agachado)
            return velocidadeMaxima * velocidadeAgachado;

        if (entrada != null && entrada.AndarDevagar)
            return velocidadeDeCaminhada;

        return velocidadeMaxima;
    }

    private void Virar(float lado)
    {
        if (!Mathf.Approximately(lado, 0f))
            direcao = Mathf.Sign(lado);

        AplicarEscala();
    }

    private void AplicarEscala()
    {
        Vector3 escala = transform.localScale;
        escala.x = direcao * escalaBase;
        transform.localScale = escala;
    }

    // ---------------- pulo ----------------
    private void TentarPular()
    {
        if (EstadoAtual == Estado.Agachado)
        {
            // Agachado num lugar apertado: pular levanta primeiro; se nao cabe, nao faz nada.
            if (entrada != null && entrada.PuloPedido && !TetoBloqueado())
            {
                EncolherColisor(false);
                EstadoAtual = Estado.Normal;
            }
            else
            {
                return;
            }
        }

        if (entrada == null || !entrada.PuloPedido)
            return;

        bool podeDoChao = NoChao || coyote.Ativo;

        if (podeDoChao)
        {
            entrada.ConsumirPulo();
            Pular(false);
            pulos = 1;
        }
        else if (pulos < maximoDePulo)
        {
            entrada.ConsumirPulo();
            Pular(true);
            pulos++;
        }
        // Sem pulo sobrando: o aperto continua guardado e sai sozinho ao pousar.
    }

    private void Pular(bool noAr)
    {
        Vector2 vel = rb.linearVelocity;
        vel.y = Jump;
        rb.linearVelocity = vel;

        UltimoPuloFoiDuplo = noAr;
        coyote.Zerar();
        gracaAposPulo.Forcar(GRACA_APOS_PULO);

        if (entrada != null)
            entrada.ConsumirPuloSoltou();

        aoPular.Invoke();
    }

    private void AplicarCorteDoPulo()
    {
        if (entrada == null || !entrada.ConsumirPuloSoltou())
            return;

        Vector2 vel = rb.linearVelocity;

        if (vel.y > 0f)
        {
            vel.y *= corteDoPulo;
            rb.linearVelocity = vel;
        }
    }

    private void AplicarGravidadeDeQueda()
    {
        Vector2 vel = rb.linearVelocity;

        bool caindo = vel.y < 0f && !NoChao;
        rb.gravityScale = caindo ? gravidadeBase * gravidadeNaQueda : gravidadeBase;

        if (velocidadeMaximaDeQueda > 0f && vel.y < -velocidadeMaximaDeQueda)
        {
            vel.y = -velocidadeMaximaDeQueda;
            rb.linearVelocity = vel;
        }
    }

    // ---------------- agachar ----------------
    private void AtualizarAgachar()
    {
        if (colisor == null || entrada == null)
            return;

        bool quer = entrada.PedindoBaixo && NoChao && !entrada.PuloPedido;

        if (quer && EstadoAtual == Estado.Normal)
        {
            EstadoAtual = Estado.Agachado;
            EncolherColisor(true);
        }
        else if (!quer && EstadoAtual == Estado.Agachado && !TetoBloqueado())
        {
            EstadoAtual = Estado.Normal;
            EncolherColisor(false);
        }
    }

    private void EncolherColisor(bool encolher)
    {
        if (colisor == null || encolher == colisorEncolhido)
            return;

        colisorEncolhido = encolher;

        if (encolher)
        {
            float alturaNova = tamanhoEmPe.y * alturaAgachado;
            colisor.size = new Vector2(tamanhoEmPe.x, alturaNova);
            // Desce o centro metade da diferenca: o PE fica no mesmo lugar, encolhe por cima.
            colisor.offset = new Vector2(offsetEmPe.x, offsetEmPe.y - (tamanhoEmPe.y - alturaNova) * 0.5f);
        }
        else
        {
            colisor.size = tamanhoEmPe;
            colisor.offset = offsetEmPe;
        }
    }

    /// <summary>Tem chao/parede na altura da cabeca? Entao nao levanta.</summary>
    private bool TetoBloqueado()
    {
        if (colisor == null)
            return false;

        CaixaDaCabeca(out Vector2 centro, out Vector2 tamanho);
        return Physics2D.OverlapBox(centro, tamanho, 0f, chaoLayer.value | paredeLayer.value);
    }

    private void CaixaDaCabeca(out Vector2 centro, out Vector2 tamanho)
    {
        Vector2 pes = transform.TransformPoint(offsetEmPe + Vector2.down * (tamanhoEmPe.y * 0.5f));
        float ex = Mathf.Abs(transform.lossyScale.x);
        float ey = Mathf.Abs(transform.lossyScale.y);

        float alturaEmPe = tamanhoEmPe.y * ey;
        float alturaBaixo = alturaEmPe * alturaAgachado;
        float alturaCabeca = alturaEmPe - alturaBaixo;

        centro = pes + Vector2.up * (alturaBaixo + alturaCabeca * 0.5f);
        tamanho = new Vector2(tamanhoEmPe.x * ex * 0.9f, alturaCabeca * 0.9f);
    }

    // ================================================================ estado PAREDE
    private void EntrarNaParede()
    {
        EstadoAtual = Estado.ParedeDeslizando;
        pulos = 0;
        dashesUsadosNoAr = 0;
    }

    private void AtualizarParede(float dt)
    {
        // Saiu da parede, pousou, ou soltou a direcao: volta pro normal.
        bool segurandoParaParede = Mathf.Abs(LadoPedido) > 0.1f
                                && Mathf.Approximately(Mathf.Sign(LadoPedido), direcao);

        if (!NaParede || NoChao || !segurandoParaParede)
        {
            EstadoAtual = Estado.Normal;
            rb.gravityScale = gravidadeBase;
            return;
        }

        if (TentarPendurar())
            return;

        // Wall jump: pula pra longe e trava o controle por um instante.
        if (entrada != null && entrada.ConsumirPulo())
        {
            PularDaParede();
            return;
        }

        if (TentarDash())
            return;

        Vector2 vel = rb.linearVelocity;
        vel.y = Mathf.Max(vel.y, -velocidadeDeslizada);
        rb.linearVelocity = vel;
        rb.gravityScale = gravidadeBase;
    }

    private void PularDaParede()
    {
        EstadoAtual = Estado.Normal;

        direcao = -direcao;
        AplicarEscala();

        rb.linearVelocity = new Vector2(wallJumpForcaX * direcao, wallJumpForcaY);

        trava.Forcar(travaAposWallJump);
        gracaAposPulo.Forcar(GRACA_APOS_PULO);
        UltimoPuloFoiDuplo = false;

        pulos = wallJumpRecarregaPulos ? Mathf.Max(0, maximoDePulo - 1) : maximoDePulo;

        if (entrada != null)
            entrada.ConsumirPuloSoltou();

        aoPular.Invoke();
        aoPularDaParede.Invoke();
    }

    // ================================================================ estado DASH / ESQUIVA / ESCORREGAR
    private bool PodeDash()
    {
        if (dashRecargaRestante.Ativo)
            return false;

        bool noChao = NoChao || coyote.Ativo;
        return noChao || dashesUsadosNoAr < dashesNoAr;
    }

    /// <summary>
    /// Um aperto de dash pode virar tres coisas diferentes. A escolha e aqui, num lugar so:
    /// segurando pra baixo correndo = escorregar; segurando pra tras = esquiva; resto = dash.
    /// </summary>
    private bool TentarDash()
    {
        if (entrada == null || !entrada.DashPedido || !PodeDash())
            return false;

        float lado = LadoPedido;

        if (escorregarAtivado && NoChao && entrada.PedindoBaixo && colisor != null)
        {
            entrada.ConsumirDash();
            IniciarEscorregar();
            return true;
        }

        if (esquivaAtivada && NoChao && Mathf.Abs(lado) > 0.1f && !Mathf.Approximately(Mathf.Sign(lado), direcao))
        {
            entrada.ConsumirDash();
            IniciarEsquiva();
            return true;
        }

        entrada.ConsumirDash();
        IniciarDash();
        return true;
    }

    private void IniciarDash()
    {
        dashDirecao = Mathf.Abs(LadoPedido) > 0.1f ? Mathf.Sign(LadoPedido) : direcao;
        direcao = dashDirecao;
        AplicarEscala();

        EstadoAtual = Estado.Dash;
        dashRestante.Forcar(dashDuracao);

        if (!(NoChao || coyote.Ativo))
            dashesUsadosNoAr++;

        EncolherColisor(false);
        AtravessarNoDash(true);
        AplicarVelocidadeReta(dashDirecao * dashVelocidade);

        aoIniciarDash.Invoke();
    }

    private void AtualizarDash()
    {
        AplicarVelocidadeReta(dashDirecao * dashVelocidade);

        bool bateu = dashParaNaParede && NaParede;

        if (!dashRestante.Ativo || bateu)
            TerminarArrancada(bateu, dashDirecao * velocidadeMaxima);
    }

    private void IniciarEsquiva()
    {
        // Esquiva anda pra TRAS: ele continua olhando pra frente.
        dashDirecao = -direcao;

        EstadoAtual = Estado.EsquivaTras;
        dashRestante.Forcar(esquivaDuracao);

        EncolherColisor(false);
        AtravessarNoDash(true);
        AplicarVelocidadeReta(dashDirecao * esquivaVelocidade);

        aoEsquivar.Invoke();
    }

    private void AtualizarEsquiva()
    {
        AplicarVelocidadeReta(dashDirecao * esquivaVelocidade);

        if (!dashRestante.Ativo || (dashParaNaParede && NaParede))
            TerminarArrancada(true, 0f);
    }

    private void IniciarEscorregar()
    {
        dashDirecao = Mathf.Abs(LadoPedido) > 0.1f ? Mathf.Sign(LadoPedido) : direcao;
        direcao = dashDirecao;
        AplicarEscala();

        EstadoAtual = Estado.Escorregando;
        dashRestante.Forcar(escorregarDuracao);

        // Escorregar passa por baixo: o colisor precisa ficar baixo o tempo todo.
        EncolherColisor(true);
        AtravessarNoDash(true);

        aoEscorregar.Invoke();
    }

    private void AtualizarEscorregar()
    {
        // Diferente do dash: a gravidade continua valendo, senao ele "voa" numa rampa.
        Vector2 vel = rb.linearVelocity;
        vel.x = dashDirecao * escorregarVelocidade;
        rb.linearVelocity = vel;

        bool bateu = NaParede;
        bool acabou = !dashRestante.Ativo;

        if (!acabou && !bateu)
            return;

        // So levanta se cabe. Preso num tunel? continua escorregando/agachado.
        AtravessarNoDash(false);
        dashRecargaRestante.Forcar(dashRecarga);

        if (TetoBloqueado())
        {
            EstadoAtual = Estado.Agachado;
        }
        else
        {
            EncolherColisor(false);
            EstadoAtual = Estado.Normal;
        }

        vel = rb.linearVelocity;
        vel.x = bateu ? 0f : dashDirecao * velocidadeMaxima;
        rb.linearVelocity = vel;

        aoTerminarDash.Invoke();
    }

    private void AplicarVelocidadeReta(float velocidadeX)
    {
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(velocidadeX, 0f);
    }

    private void TerminarArrancada(bool bateuNaParede, float velocidadeDeSaida)
    {
        EstadoAtual = Estado.Normal;
        dashRecargaRestante.Forcar(dashRecarga);
        rb.gravityScale = gravidadeBase;

        AtravessarNoDash(false);

        Vector2 vel = rb.linearVelocity;
        vel.x = bateuNaParede ? 0f : velocidadeDeSaida;
        rb.linearVelocity = vel;

        aoTerminarDash.Invoke();
    }

    private void AtravessarNoDash(bool atravessar)
    {
        if (colisor == null)
            return;

        colisor.excludeLayers = atravessar
            ? (LayerMask)(excludeBase.value | dashAtravessa.value)
            : excludeBase;
    }

    // ================================================================ estado PENDURADO / SUBINDO
    private bool TentarPendurar()
    {
        if (!beiradaAtiva || semAgarrar.Ativo) return false;
        if (NoChao) return false;
        if (!NaParede || !CabecaLivre()) return false;
        if (rb.linearVelocity.y > 0.5f) return false;                 // subindo passa direto
        if (precisaSegurarParaAgarrar && LadoPedido * direcao <= 0f) return false;
        if (!EncontrarTopoDaBeirada(out topoDaBeirada)) return false;

        EstadoAtual = Estado.Pendurado;

        // Kinematic: a fisica para de empurrar e ele fica cravado, sem zerar velocidade todo quadro.
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        pulos = 0;
        dashesUsadosNoAr = 0;

        if (entrada != null)
            entrada.Esquecer();

        // Encosta as maos (LedgeCheck) no topo do bloco pra animacao bater com o cenario.
        float maosParaPivo = transform.position.y - ledgeCheck.position.y;
        Vector2 pos = rb.position;
        pos.y = topoDaBeirada + maosParaPivo;
        rb.position = pos;
        transform.position = pos;

        aoAgarrarBeirada.Invoke();
        return true;
    }

    private void AtualizarPendurado()
    {
        if (entrada == null)
            return;

        if (entrada.PedindoBaixo)
        {
            SoltarBeirada();
            return;
        }

        if (entrada.ConsumirPulo() || entrada.PedindoCima)
            ComecarSubida();
    }

    /// <summary>Solta a beirada e cai. Publico pra outros scripts / UnityEvent.</summary>
    public void SoltarBeirada()
    {
        if (EstadoAtual != Estado.Pendurado)
            return;

        EstadoAtual = Estado.Normal;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravidadeBase;
        semAgarrar.Forcar(semAgarrarAposSoltar);
        coyote.Forcar(coyoteTime);
    }

    private void ComecarSubida()
    {
        EstadoAtual = Estado.SubindoBeirada;
        subidaRestante.Forcar(duracaoDaSubida);
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void AtualizarSubindoBeirada()
    {
        // A subida e um teletransporte curto no FIM da animacao: durante o clipe ele fica
        // parado onde estava, e o desenho e que mostra o movimento. Teletransportar no
        // comeco faria o boneco aparecer em cima antes de ter subido.
        if (subidaRestante.Ativo)
            return;

        TerminarSubida();
    }

    /// <summary>Coloca o boneco em cima do bloco. Publico: da pra chamar do fim do clipe.</summary>
    public void TerminarSubida()
    {
        if (EstadoAtual != Estado.SubindoBeirada && EstadoAtual != Estado.Pendurado)
            return;

        EstadoAtual = Estado.Normal;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravidadeBase;

        semAgarrar.Forcar(semAgarrarAposSoltar);
        subidaRestante.Zerar();

        float baseY = colisor != null ? colisor.bounds.min.y : (groundCheck != null ? groundCheck.position.y : transform.position.y);
        float pesParaPivo = transform.position.y - baseY;

        float destinoX = (WallCheck != null ? WallCheck.position.x : transform.position.x) + direcao * avancoSubida;
        Vector2 destino = new Vector2(destinoX, topoDaBeirada + pesParaPivo + FOLGA_SUBIDA);

        rb.position = destino;
        transform.position = destino;
        rb.linearVelocity = Vector2.zero;

        alturaMaximaNoAr = destino.y;

        aoSubirBeirada.Invoke();
    }

    /// <summary>Raio pra baixo um pouco a frente do LedgeCheck: onde bate e o topo do bloco.</summary>
    private bool EncontrarTopoDaBeirada(out float topoY)
    {
        topoY = 0f;

        if (ledgeCheck == null || WallCheck == null)
            return false;

        Vector2 origem = new Vector2(ledgeCheck.position.x + direcao * avancoSubida, ledgeCheck.position.y + 0.05f);
        float distancia = (ledgeCheck.position.y - WallCheck.position.y) + 0.15f;

        RaycastHit2D hit = Physics2D.Raycast(origem, Vector2.down, distancia, paredeLayer.value | chaoLayer.value);

        if (hit.collider == null)
            return false;

        topoY = hit.point.y;
        return true;
    }

    // ================================================================ estado ESCADA
    private bool TentarEntrarNaEscada()
    {
        if (EscadaEncostada == null || entrada == null)
            return false;

        bool pedindo = entrada.PedindoCima || (entrada.PedindoBaixo && !NoChao);

        if (!pedindo)
            return false;

        EstadoAtual = Estado.Escada;
        EscorregandoNaEscada = false;

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        // Centraliza no degrau: subir escada torto fica estranho e engancha no colisor.
        Vector2 pos = rb.position;
        pos.x = EscadaEncostada.CentroX;
        rb.position = pos;
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);

        return true;
    }

    private void AtualizarEscada(float dt)
    {
        if (EscadaEncostada == null)
        {
            SairDaEscada(false);
            return;
        }

        // Pular na escada = desgrudar e pular.
        if (entrada != null && entrada.ConsumirPulo())
        {
            SairDaEscada(false);
            Pular(false);
            pulos = 1;
            return;
        }

        float vertical = entrada != null ? entrada.Vertical : 0f;

        // Escorregar: segurar baixo + dash desce rapido, como bombeiro no cano.
        EscorregandoNaEscada = entrada != null && entrada.PedindoBaixo && entrada.DashPedido;

        if (EscorregandoNaEscada)
        {
            entrada.ConsumirDash();
            rb.linearVelocity = new Vector2(0f, -escadaVelocidadeEscorregando);
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, vertical * escadaVelocidade);
        }

        // Chegou no topo andando pra cima -> sobe pro chao de cima.
        if (vertical > 0.5f && transform.position.y >= EscadaEncostada.TopoY)
        {
            SairDaEscada(true);
            return;
        }

        // Chegou no pe da escada descendo -> volta a andar.
        if (vertical < -0.5f && NoChao)
            SairDaEscada(false);
    }

    private void SairDaEscada(bool porCima)
    {
        EstadoAtual = Estado.Normal;
        EscorregandoNaEscada = false;
        rb.gravityScale = gravidadeBase;

        if (!porCima || EscadaEncostada == null)
            return;

        float topo = EscadaEncostada.TopoY;
        float pesParaPivo = colisor != null ? transform.position.y - colisor.bounds.min.y : 0f;

        Vector2 destino = new Vector2(AcharPisoNoTopoDaEscada(topo), topo + pesParaPivo + FOLGA_SUBIDA);

        rb.position = destino;
        transform.position = new Vector3(destino.x, destino.y, transform.position.z);
        rb.linearVelocity = Vector2.zero;
        alturaMaximaNoAr = destino.y;
    }

    /// <summary>
    /// Em que X o boneco pisa ao sair pelo topo da escada.
    ///
    /// Uma escada raramente tem chao exatamente em cima dela — o normal e o piso ficar do
    /// lado. Aqui a gente tenta sair no lugar e, se nao houver piso ali, da um passo pra
    /// cada lado ate achar um lugar onde caiba de pe. Sem isso o boneco chega no topo,
    /// nao acha chao e cai de volta.
    /// </summary>
    private float AcharPisoNoTopoDaEscada(float topo)
    {
        float xAtual = transform.position.x;
        float passo = (colisor != null ? colisor.size.x : 0.26f) + 0.05f;

        float[] tentativas = { 0f, passo, -passo, passo * 2f, -passo * 2f };

        for (int i = 0; i < tentativas.Length; i++)
        {
            float x = xAtual + tentativas[i];

            bool temPiso = Physics2D.OverlapCircle(new Vector2(x, topo - 0.05f), 0.06f, chaoLayer);

            if (!temPiso)
                continue;

            if (colisor == null)
                return x;

            // O corpo tem que caber em pe nesse ponto (nada de aparecer dentro da parede).
            Vector2 centro = new Vector2(x, topo + colisor.size.y * 0.5f + FOLGA_SUBIDA);
            bool bloqueado = Physics2D.OverlapBox(centro, colisor.size * 0.85f, 0f, chaoLayer.value | paredeLayer.value);

            if (!bloqueado)
                return x;
        }

        return xAtual;
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (outro.TryGetComponent(out Escada escada))
            EscadaEncostada = escada;
    }

    private void OnTriggerExit2D(Collider2D outro)
    {
        if (outro.TryGetComponent(out Escada escada) && EscadaEncostada == escada)
        {
            EscadaEncostada = null;

            if (EstadoAtual == Estado.Escada)
                SairDaEscada(false);
        }
    }

    // ================================================================ estado ATORDOADO / MORTO
    private void AtualizarAtordoado()
    {
        if (atordoamento.Ativo)
        {
            AplicarGravidadeDeQueda();
            return;
        }

        EstadoAtual = Estado.Normal;
    }

    private void AtualizarMorto()
    {
        // Morto continua caindo (ele nao flutua), mas nao anda nem pula.
        Vector2 vel = rb.linearVelocity;
        vel.x = Mathf.MoveTowards(vel.x, 0f, desaceleracao * Time.fixedDeltaTime);
        rb.linearVelocity = vel;
    }

    // ================================================================ ponte com o dano
    /// <summary>
    /// Empurrao de fora: dano, explosao, vento. Quem chama e o Vida.
    ///
    /// Por que precisa da trava: o Andar() puxa a velocidade horizontal de volta pro que o
    /// teclado pede, com a desaceleracao. Sem travar, o impulso seria comido em poucos
    /// quadros e o golpe nao teria peso nenhum.
    /// </summary>
    public void AplicarImpulsoExterno(Vector2 impulso, float travaSegundos)
    {
        if (rb == null || EstadoAtual == Estado.Morto)
            return;

        // Levou golpe pendurado/subindo/na escada: solta. No meio do dash: o dash e cortado.
        switch (EstadoAtual)
        {
            case Estado.Pendurado:
                SoltarBeirada();
                break;

            case Estado.SubindoBeirada:
            case Estado.Escada:
                EstadoAtual = Estado.Normal;
                rb.bodyType = RigidbodyType2D.Dynamic;
                break;

            case Estado.Dash:
            case Estado.EsquivaTras:
            case Estado.Escorregando:
                AtravessarNoDash(false);
                break;
        }

        EncolherColisor(false);

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravidadeBase;
        rb.linearVelocity = Vector2.zero;       // zera pra o empurrao ser sentido por inteiro
        rb.AddForce(impulso, ForceMode2D.Impulse);

        EstadoAtual = Estado.Atordoado;
        atordoamento.Forcar(Mathf.Max(0.01f, travaSegundos));
        trava.Forcar(travaSegundos);

        if (entrada != null)
            entrada.Esquecer();
    }

    /// <summary>
    /// Avanco curto pra frente de um golpe. Diferente do <see cref="AplicarImpulsoExterno"/>:
    /// nao atordoa e nao troca de estado — so empurra e segura o controle horizontal pelo
    /// tempo do avanco, pra o Andar() nao comer o deslocamento no quadro seguinte.
    /// </summary>
    public void AplicarAvancoDeGolpe(float velocidadeX, float travaSegundos)
    {
        if (rb == null || Morto || Atordoado)
            return;

        // No meio de um dash/escorregada quem manda na velocidade e o proprio dash.
        if (Dashando || Esquivando || Escorregando || Pendurado || SubindoBeirada || NaEscada)
            return;

        Vector2 vel = rb.linearVelocity;
        vel.x = velocidadeX;
        rb.linearVelocity = vel;

        trava.Armar(travaSegundos);
    }

    /// <summary>Empurrao vertical seco (mergulho de ataque, mola, trampolim).</summary>
    public void ImpulsoVertical(float velocidadeY)
    {
        if (rb == null || Morto)
            return;

        if (NaEscada || Pendurado || SubindoBeirada)
        {
            EstadoAtual = Estado.Normal;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = gravidadeBase;
        }

        Vector2 vel = rb.linearVelocity;
        vel.y = velocidadeY;
        rb.linearVelocity = vel;
    }

    /// <summary>Liga/desliga o estado de morto. Quem chama e o Player.</summary>
    public void DefinirMorto(bool morto)
    {
        if (morto)
        {
            EstadoAtual = Estado.Morto;
            EncolherColisor(false);
            AtravessarNoDash(false);

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = gravidadeBase;
                rb.linearVelocity = Vector2.zero;
            }

            if (entrada != null)
                entrada.Esquecer();

            return;
        }

        EstadoAtual = Estado.Normal;
        pulos = 0;
        dashesUsadosNoAr = 0;
        atordoamento.Zerar();
        trava.Zerar();
        dashRestante.Zerar();
        dashRecargaRestante.Zerar();
        alturaMaximaNoAr = transform.position.y;
    }

    /// <summary>Zera a velocidade e os cronometros. Usado no teletransporte do renascimento.</summary>
    public void Parar()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        alturaMaximaNoAr = transform.position.y;
    }

    // ================================================================ gizmos
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, raioChecagem);
        }

        if (WallCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(WallCheck.position, raioCheck);
        }

        if (ledgeCheck != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(ledgeCheck.position, raioCheck);
        }

        if (Application.isPlaying && colisor != null)
        {
            CaixaDaCabeca(out Vector2 centro, out Vector2 tamanho);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(centro, tamanho);
        }
    }
}
