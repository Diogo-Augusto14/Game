using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Inimigo de chao completo, com maquina de estados de verdade:
///
///   Parado / Patrulhando  -> anda de um lado pro outro, para nas bordas e paredes
///   Alerta                -> viu o jogador e "acorda" (um instante parado: o jogador VE que foi notado)
///   Perseguindo           -> corre atras, sem cair da plataforma
///   Preparando            -> telegrafa o golpe (a janelinha pra o jogador reagir)
///   Atacando              -> a hitbox abre por uma fracao do clipe
///   Recuperando           -> respira; daqui pode emendar o segundo golpe do combo
///   Atordoado             -> levou porrada, o empurrao acontece
///   Morto                 -> para tudo, o Vida limpa o objeto
///
/// Vida, empurrao, flash e morte ficam no componente Vida. Animacao fica no
/// AnimacaoDoInimigo. Aqui e SO comportamento — e por isso da pra trocar o sprite,
/// a vida ou a animacao sem mexer numa linha de IA.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Vida))]
[DisallowMultipleComponent]
public class Inimigo : MonoBehaviour
{
    public enum Estado
    {
        Parado,
        Patrulhando,
        Alerta,
        Perseguindo,
        Preparando,
        Atacando,
        Recuperando,
        Atordoado,
        Morto
    }

    [Header("Alvo")]
    [Tooltip("O boneco. Vazio = acha o jogador sozinho")]
    [SerializeField] private Transform jogador;

    [SerializeField] private string tagDoJogador = "Player";

    [Header("Movimento")]
    [Tooltip("Velocidade da patrulha (passeando)")]
    [SerializeField, Min(0f)] private float velocidadeDePatrulha = 0.7f;

    [Tooltip("Velocidade perseguindo o jogador")]
    [SerializeField, Min(0f)] private float velocidadeDePerseguicao = 1.8f;

    [Tooltip("Quao rapido ele muda de velocidade (0 = troca na hora)")]
    [SerializeField, Min(0f)] private float aceleracao = 12f;

    [Header("Patrulha")]
    [Tooltip("Ligado: fica andando de um lado pro outro quando nao ve ninguem")]
    [SerializeField] private bool patrulhar = true;

    [Tooltip("Quanto ele anda pra cada lado a partir de onde nasceu (0 = so vira nas bordas)")]
    [SerializeField, Min(0f)] private float alcanceDaPatrulha = 2.5f;

    [Tooltip("Segundos parado a cada vez que chega na ponta da patrulha")]
    [SerializeField, Min(0f)] private float pausaNaPatrulha = 0.8f;

    [Header("Visao")]
    [Tooltip("Distancia em que ele percebe o jogador")]
    [SerializeField, Min(0f)] private float raioDeVisao = 4.5f;

    [Tooltip("Distancia em que ele percebe MESMO de costas (ouvido)")]
    [SerializeField, Min(0f)] private float raioDeAlertaPorTras = 1.5f;

    [Tooltip("Altura maxima de diferenca pra ver o jogador (evita agro de outra plataforma)")]
    [SerializeField, Min(0f)] private float diferencaDeAlturaMaxima = 1.6f;

    [Tooltip("Parede entre os dois esconde o jogador")]
    [SerializeField] private bool paredeEsconde = true;

    [Tooltip("Segundos que ele continua perseguindo depois de perder o jogador de vista")]
    [SerializeField, Min(0f)] private float memoriaDeAgro = 3f;

    [Tooltip("Segundos parado ao notar o jogador, antes de sair correndo")]
    [SerializeField, Min(0f)] private float tempoDeAlerta = 0.35f;

    [Header("Ataque")]
    [Tooltip("Distancia horizontal em que ele para de andar e ataca. Tem que ser MENOR que o " +
             "alcance da hitbox (centro + metade do tamanho), senao ele golpeia o ar")]
    [SerializeField, Min(0f)] private float distanciaDeAtaque = 0.6f;

    [Tooltip("Telegrafo: segundos parado antes do golpe sair")]
    [SerializeField, Min(0f)] private float tempoDePreparo = 0.3f;

    [Tooltip("Quanto tempo o golpe inteiro leva")]
    [SerializeField, Min(0.05f)] private float duracaoDoGolpe = 0.45f;

    [Tooltip("Dentro do golpe, quando a hitbox LIGA (fracao de 0 a 1)")]
    [SerializeField, Range(0f, 1f)] private float inicioDaJanela = 0.25f;

    [Tooltip("Dentro do golpe, quando a hitbox DESLIGA")]
    [SerializeField, Range(0f, 1f)] private float fimDaJanela = 0.55f;

    [Tooltip("Segundos de descanso depois do golpe")]
    [SerializeField, Min(0f)] private float tempoDeRecuperacao = 0.6f;

    [Tooltip("Chance (0 a 1) de emendar um segundo golpe sem descansar")]
    [SerializeField, Range(0f, 1f)] private float chanceDeCombo = 0.45f;

    [Tooltip("Dano de cada golpe")]
    [SerializeField, Min(0f)] private float dano = 14f;

    [Tooltip("Forca do empurrao no jogador")]
    [SerializeField, Min(0f)] private float forcaEmpurrao = 4.5f;

    [Tooltip("Avanco pra frente ao golpear (0 = golpeia parado)")]
    [SerializeField, Min(0f)] private float avancoDoGolpe = 1.2f;

    [Header("Hitbox")]
    [Tooltip("Vazio = criada sozinha no Awake")]
    [SerializeField] private Espada hitbox;

    [SerializeField] private Vector2 centroDaHitbox = new Vector2(0.34f, 0.2f);

    [SerializeField] private Vector2 tamanhoDaHitbox = new Vector2(0.6f, 0.36f);

    [Header("Sensores")]
    [Tooltip("Ligado: para antes de cair da plataforma")]
    [SerializeField] private bool naoCairDaBorda = true;

    [Tooltip("Vazio = criado sozinho: um ponto na frente e abaixo dos pes")]
    [SerializeField] private Transform checadorDeBorda;

    [Tooltip("Vazio = usa as camadas de chao do projeto")]
    [SerializeField] private LayerMask camadaChao;

    [SerializeField, Min(0.05f)] private float alcanceDoChecador = 0.35f;

    [SerializeField, Min(0.02f)] private float raioDoChao = 0.08f;

    [Header("Ao tomar dano")]
    [Tooltip("Segundos parado depois de levar golpe — e o que deixa o empurrao acontecer")]
    [SerializeField, Min(0f)] private float tempoAtordoadoLeve = 0.25f;

    [SerializeField, Min(0f)] private float tempoAtordoadoForte = 0.55f;

    [Tooltip("Levar golpe vira o inimigo pra quem bateu (nao da pra bater nas costas de gracas)")]
    [SerializeField] private bool viraParaQuemBateu = true;

    [Header("Eventos")]
    public UnityEvent AoNotarJogador = new UnityEvent();
    public UnityEvent AoGolpear = new UnityEvent();

    // ---------------- estado interno ----------------
    private Rigidbody2D rb;
    private Vida vida;
    private Collider2D corpo;

    private float direcao = 1f;
    private float centroDaPatrulha;
    private float tempoNoGolpe;
    private bool janelaAberta;
    private int golpeDoCombo;
    private bool viuOJogador;

    private Cronometro alerta;
    private Cronometro preparo;
    private Cronometro recuperacao;
    private Cronometro atordoamento;
    private Cronometro pausa;
    private Cronometro agro;

    public Estado EstadoAtual { get; private set; } = Estado.Parado;

    public bool NoChao { get; private set; }

    /// <summary>0 = primeiro golpe, 1 = segundo golpe do combo. Quem anima usa isto.</summary>
    public int GolpeAtual => golpeDoCombo;

    /// <summary>Progresso de 0 a 1 do golpe.</summary>
    public float ProgressoDoGolpe => duracaoDoGolpe <= 0f ? 0f : Mathf.Clamp01(tempoNoGolpe / duracaoDoGolpe);

    /// <summary>Quanto tempo o golpe leva. Quem anima usa pra encaixar o clipe no golpe.</summary>
    public float DuracaoDoGolpe => duracaoDoGolpe;

    public float Direcao => direcao;

    /// <summary>Velocidade horizontal em modulo — pra escolher parado x correr.</summary>
    public float VelocidadeHorizontal => rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;

    public bool Perseguindo => EstadoAtual == Estado.Perseguindo || EstadoAtual == Estado.Alerta;

    // ---------------- ciclo de vida ----------------
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        vida = GetComponent<Vida>();
        corpo = GetComponent<Collider2D>();

        rb.freezeRotation = true;
        centroDaPatrulha = transform.position.x;

        if (camadaChao.value == 0)
            camadaChao = Camadas.MascaraDeChao;

        GarantirJogador();
        GarantirSensores();
        GarantirHitbox();

        EstadoAtual = patrulhar ? Estado.Patrulhando : Estado.Parado;
    }

    private void OnEnable()
    {
        vida.AoTomarDano.AddListener(AoLevarGolpe);
        vida.AoMorrer.AddListener(Morrer);
    }

    private void OnDisable()
    {
        vida.AoTomarDano.RemoveListener(AoLevarGolpe);
        vida.AoMorrer.RemoveListener(Morrer);
    }

    private void GarantirJogador()
    {
        if (jogador != null)
            return;

        if (Player.Atual != null)
        {
            jogador = Player.Atual.transform;
            return;
        }

        GameObject obj = GameObject.FindWithTag(tagDoJogador);

        if (obj != null)
            jogador = obj.transform;
    }

    /// <summary>
    /// Cria os sensores que faltarem. Um inimigo arrastado pra cena sem filhos ainda
    /// funciona — e isso que faz o "so dar play" ser verdade.
    /// </summary>
    private void GarantirSensores()
    {
        float meiaLargura = corpo != null ? corpo.bounds.extents.x : 0.15f;

        if (checadorDeBorda == null)
        {
            Transform achado = transform.Find("ChecadorDeBorda");

            checadorDeBorda = achado != null
                ? achado
                : CriarFilho("ChecadorDeBorda", new Vector3(meiaLargura + 0.03f, 0.02f, 0f));
        }
    }

    private void GarantirHitbox()
    {
        if (hitbox == null)
            hitbox = GetComponentInChildren<Espada>(true);

        if (hitbox == null)
        {
            GameObject obj = new GameObject("Hitbox");
            obj.transform.SetParent(transform, false);

            BoxCollider2D caixa = obj.AddComponent<BoxCollider2D>();
            caixa.isTrigger = true;

            hitbox = obj.AddComponent<Espada>();
            Camadas.Definir(obj, Camadas.Golpe);
        }

        hitbox.transform.localPosition = centroDaHitbox;

        BoxCollider2D box = hitbox.GetComponent<BoxCollider2D>();

        if (box != null)
            box.size = tamanhoDaHitbox;

        hitbox.Desligar();
    }

    private Transform CriarFilho(string nome, Vector3 posicaoLocal)
    {
        GameObject novo = new GameObject(nome);
        novo.transform.SetParent(transform, false);
        novo.transform.localPosition = posicaoLocal;
        return novo.transform;
    }

    // ---------------- loop ----------------
    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        alerta.Contar(dt);
        preparo.Contar(dt);
        recuperacao.Contar(dt);
        atordoamento.Contar(dt);
        pausa.Contar(dt);
        agro.Contar(dt);

        LerSensores();

        if (EstadoAtual == Estado.Morto)
            return;

        if (jogador == null)
            GarantirJogador();

        switch (EstadoAtual)
        {
            case Estado.Atordoado: AtualizarAtordoado(); break;
            case Estado.Atacando: AtualizarAtacando(dt); break;
            case Estado.Preparando: AtualizarPreparando(); break;
            case Estado.Recuperando: AtualizarRecuperando(); break;
            case Estado.Alerta: AtualizarAlerta(); break;
            case Estado.Perseguindo: AtualizarPerseguindo(); break;
            default: AtualizarPasseio(); break;   // Parado e Patrulhando
        }
    }

    private void LerSensores()
    {
        NoChao = Physics2D.OverlapCircle(PesDoInimigo(), raioDoChao, camadaChao);
    }

    private Vector2 PesDoInimigo()
    {
        if (corpo != null)
            return new Vector2(corpo.bounds.center.x, corpo.bounds.min.y + 0.02f);

        return transform.position;
    }

    // ---------------- passeio (parado / patrulhando) ----------------
    private void AtualizarPasseio()
    {
        if (Percebeu())
        {
            EntrarEmAlerta();
            return;
        }

        if (!patrulhar)
        {
            Frear();
            return;
        }

        if (pausa.Ativo)
        {
            Frear();
            return;
        }

        EstadoAtual = Estado.Patrulhando;

        bool chegouNaPonta = alcanceDaPatrulha > 0f
                          && Mathf.Abs(transform.position.x - centroDaPatrulha) >= alcanceDaPatrulha
                          && Mathf.Sign(transform.position.x - centroDaPatrulha) == direcao;

        if (chegouNaPonta || !PodeAndarPraFrente())
        {
            Virar(-direcao);
            pausa.Forcar(pausaNaPatrulha);
            Frear();
            return;
        }

        Andar(velocidadeDePatrulha);
    }

    // ---------------- alerta ----------------
    private void EntrarEmAlerta()
    {
        EstadoAtual = Estado.Alerta;
        alerta.Forcar(tempoDeAlerta);
        agro.Forcar(memoriaDeAgro);

        VirarParaOJogador();
        Frear();

        if (!viuOJogador)
        {
            viuOJogador = true;
            AoNotarJogador?.Invoke();
        }
    }

    private void AtualizarAlerta()
    {
        Frear();
        VirarParaOJogador();

        if (!alerta.Ativo)
            EstadoAtual = Estado.Perseguindo;
    }

    // ---------------- perseguir ----------------
    private void AtualizarPerseguindo()
    {
        if (jogador == null)
        {
            Desistir();
            return;
        }

        if (Percebeu())
            agro.Forcar(memoriaDeAgro);
        else if (!agro.Ativo)
        {
            Desistir();
            return;
        }

        float dx = jogador.position.x - transform.position.x;

        VirarParaOJogador();

        if (Mathf.Abs(dx) <= distanciaDeAtaque && Mathf.Abs(jogador.position.y - transform.position.y) <= diferencaDeAlturaMaxima)
        {
            ComecarPreparo();
            return;
        }

        if (!PodeAndarPraFrente())
        {
            // Beirada no caminho: para e espera o jogador chegar em vez de se jogar.
            Frear();
            return;
        }

        Andar(velocidadeDePerseguicao);
    }

    private void Desistir()
    {
        EstadoAtual = patrulhar ? Estado.Patrulhando : Estado.Parado;
        viuOJogador = false;
        centroDaPatrulha = transform.position.x;
        Frear();
    }

    // ---------------- atacar ----------------
    private void ComecarPreparo()
    {
        EstadoAtual = Estado.Preparando;
        preparo.Forcar(tempoDePreparo);
        Frear();
        VirarParaOJogador();
    }

    private void AtualizarPreparando()
    {
        Frear();

        if (preparo.Ativo)
            return;

        ComecarGolpe();
    }

    private void ComecarGolpe()
    {
        EstadoAtual = Estado.Atacando;
        tempoNoGolpe = 0f;
        janelaAberta = false;

        if (avancoDoGolpe > 0f)
            rb.linearVelocity = new Vector2(direcao * avancoDoGolpe, rb.linearVelocity.y);

        AoGolpear?.Invoke();
    }

    private void AtualizarAtacando(float dt)
    {
        tempoNoGolpe += dt;

        float t = ProgressoDoGolpe;
        bool deveAbrir = t >= inicioDaJanela && t <= fimDaJanela;

        if (deveAbrir && !janelaAberta)
        {
            janelaAberta = true;

            PesoDoGolpe peso = golpeDoCombo > 0 ? PesoDoGolpe.Forte : PesoDoGolpe.Leve;
            hitbox?.Ligar(dano, forcaEmpurrao, peso);
        }
        else if (!deveAbrir && janelaAberta)
        {
            FecharJanela();
        }

        // Depois do avanco inicial ele para: golpe de espada nao desliza pela tela.
        if (t > 0.3f)
            Frear();

        if (t < 1f)
            return;

        FecharJanela();

        // Emenda o segundo golpe? So se o jogador ainda esta do lado e a sorte ajudar.
        bool podeEmendar = golpeDoCombo == 0
                        && JogadorPerto(distanciaDeAtaque * 1.3f)
                        && Random.value < chanceDeCombo;

        if (podeEmendar)
        {
            golpeDoCombo = 1;
            ComecarGolpe();
            return;
        }

        golpeDoCombo = 0;
        EstadoAtual = Estado.Recuperando;
        recuperacao.Forcar(tempoDeRecuperacao);
    }

    private void AtualizarRecuperando()
    {
        Frear();

        if (recuperacao.Ativo)
            return;

        EstadoAtual = Estado.Perseguindo;
    }

    private void FecharJanela()
    {
        janelaAberta = false;
        hitbox?.Desligar();
    }

    // ---------------- dano e morte ----------------
    private void AoLevarGolpe(DanoInfo info)
    {
        if (EstadoAtual == Estado.Morto)
            return;

        FecharJanela();

        EstadoAtual = Estado.Atordoado;
        atordoamento.Forcar(info.Peso == PesoDoGolpe.Forte ? tempoAtordoadoForte : tempoAtordoadoLeve);

        golpeDoCombo = 0;
        agro.Forcar(memoriaDeAgro);
        viuOJogador = true;

        // Levou golpe de quem nao tinha visto: agora sabe onde o jogador esta.
        if (info.Atacante != null && jogador == null)
            jogador = info.Atacante.transform;

        if (viraParaQuemBateu)
            Virar(-info.LadoDoEmpurrao);
    }

    private void AtualizarAtordoado()
    {
        // Nao mexe na velocidade: e o que deixa o empurrao do golpe ser sentido.
        if (atordoamento.Ativo)
            return;

        EstadoAtual = jogador != null ? Estado.Perseguindo : Estado.Patrulhando;
        agro.Forcar(memoriaDeAgro);
    }

    private void Morrer()
    {
        EstadoAtual = Estado.Morto;

        FecharJanela();

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;   // para de cair e de ser empurrado

        // Desliga TODOS os colliders: o corpo nao bloqueia mais o jogador nem toma golpe.
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
            c.enabled = false;
    }

    // ---------------- percepcao ----------------
    private bool Percebeu()
    {
        if (jogador == null)
            return false;

        Vector2 paraOJogador = (Vector2)jogador.position - (Vector2)transform.position;

        if (Mathf.Abs(paraOJogador.y) > diferencaDeAlturaMaxima)
            return false;

        float distancia = paraOJogador.magnitude;

        if (distancia > raioDeVisao)
            return false;

        // Fora do campo de visao (de costas) ele so percebe bem de perto.
        bool naFrente = Mathf.Sign(paraOJogador.x) == direcao || Mathf.Abs(paraOJogador.x) < 0.1f;

        if (!naFrente && distancia > raioDeAlertaPorTras)
            return false;

        if (!paredeEsconde)
            return true;

        // Parede no meio esconde: raio do peito do inimigo ate o peito do jogador.
        Vector2 origem = (Vector2)transform.position + Vector2.up * 0.2f;
        Vector2 destino = (Vector2)jogador.position + Vector2.up * 0.2f;

        RaycastHit2D hit = Physics2D.Linecast(origem, destino, Camadas.MascaraDeParede);
        return hit.collider == null;
    }

    private bool JogadorPerto(float distancia)
    {
        if (jogador == null)
            return false;

        return Mathf.Abs(jogador.position.x - transform.position.x) <= distancia
            && Mathf.Abs(jogador.position.y - transform.position.y) <= diferencaDeAlturaMaxima;
    }

    // ---------------- movimento ----------------
    private void Andar(float velocidade)
    {
        float alvo = direcao * velocidade;
        Vector2 vel = rb.linearVelocity;

        vel.x = aceleracao > 0f
            ? Mathf.MoveTowards(vel.x, alvo, aceleracao * Time.fixedDeltaTime)
            : alvo;

        rb.linearVelocity = vel;
    }

    private void Frear()
    {
        Vector2 vel = rb.linearVelocity;

        vel.x = aceleracao > 0f
            ? Mathf.MoveTowards(vel.x, 0f, aceleracao * Time.fixedDeltaTime)
            : 0f;

        rb.linearVelocity = vel;
    }

    private bool PodeAndarPraFrente()
    {
        if (!naoCairDaBorda || checadorDeBorda == null)
            return true;

        // Chao na frente? Se nao tem, e beirada.
        bool temChao = Physics2D.Raycast(checadorDeBorda.position, Vector2.down, alcanceDoChecador, camadaChao);

        if (!temChao)
            return false;

        // Parede na frente?
        Vector2 origem = corpo != null ? (Vector2)corpo.bounds.center : (Vector2)transform.position;
        float alcance = (corpo != null ? corpo.bounds.extents.x : 0.15f) + 0.06f;

        RaycastHit2D parede = Physics2D.Raycast(origem, Vector2.right * direcao, alcance, Camadas.MascaraDeParede);
        return parede.collider == null;
    }

    private void VirarParaOJogador()
    {
        if (jogador == null)
            return;

        float dx = jogador.position.x - transform.position.x;

        if (Mathf.Abs(dx) > 0.05f)
            Virar(Mathf.Sign(dx));
    }

    private void Virar(float novaDirecao)
    {
        if (Mathf.Approximately(novaDirecao, direcao) || Mathf.Approximately(novaDirecao, 0f))
            return;

        direcao = Mathf.Sign(novaDirecao);

        // Mantem o tamanho original do sprite, so troca o sinal do X.
        Vector3 escala = transform.localScale;
        escala.x = Mathf.Abs(escala.x) * direcao;
        transform.localScale = escala;
    }

    // ---------------- editor ----------------
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, raioDeVisao);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, raioDeAlertaPorTras);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + Vector3.left * distanciaDeAtaque,
                        transform.position + Vector3.right * distanciaDeAtaque);

        if (patrulhar && alcanceDaPatrulha > 0f)
        {
            float centro = Application.isPlaying ? centroDaPatrulha : transform.position.x;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            Gizmos.DrawLine(new Vector3(centro - alcanceDaPatrulha, transform.position.y - 0.1f),
                            new Vector3(centro + alcanceDaPatrulha, transform.position.y - 0.1f));
        }

        if (checadorDeBorda != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(checadorDeBorda.position, checadorDeBorda.position + Vector3.down * alcanceDoChecador);
        }
    }
#endif
}
