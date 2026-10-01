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

    // A sala onde nasceu (o mapa de caminho pelos ladrilhos dela) e o passeio dos andarilhos.
    private Sala sala;
    private Vector2 pontoDoPasseio;
    private float trocarPasseioEm;
    private float paradoAte;

    /// <summary>Algum inimigo (chefe incluido) acabou de morrer. Itens que curam ao matar escutam aqui.</summary>
    public static event System.Action<InimigoDeSala> AlgumMorreu;

    /// <summary>Todo inimigo ligado na cena (lagrima teleguiada procura aqui, sem Find).</summary>
    public static readonly System.Collections.Generic.List<InimigoDeSala> Ativos = new System.Collections.Generic.List<InimigoDeSala>();

    public Estado EstadoAtual { get; protected set; } = Estado.Dormindo;

    public Vida Vida => vida;

    public bool EstaMorto => EstadoAtual == Estado.Morto;

    /// <summary>Ainda se passa por coisa do cenario (o barril fechado): fica sem o <see cref="ContornoClaro"/>, pra nao entregar.</summary>
    public virtual bool Disfarcado => false;

    /// <summary>Troca a velocidade andando (a fabrica usa nas variacoes de um mesmo comportamento).</summary>
    public void DefinirVelocidade(float nova) => velocidade = Mathf.Max(0f, nova);

    /// <summary>Fracao da velocidade ao andar: 1 = normal, menos = gelado (flecha de gelo).</summary>
    public float MultiplicadorDeVelocidade { get; set; } = 1f;

    /// <summary>
    /// Como o bicho chega perto do jogador quando so anda atras dele. Cada jeito e de UMA
    /// especie so (a fabrica escolhe), pra nenhuma andar igual a outra:
    ///   Direta       -> reto, contornando obstaculo (so quem nao tem jeito proprio)
    ///   Ziguezague   -> vai e volta de lado enquanto avanca, rapido            (goblin da tocha)
    ///   PassoPesado  -> passada forte, para, passada                          (orc)
    ///   Marcha       -> escolhe um rumo e vai reto nele; so corrige de tempo em tempo (orc blindado)
    ///   Finta        -> avanca, recua um passo, avanca de novo                (esqueleto blindado)
    ///   Pulsante     -> anda em pulsos, como batida de coracao: tum-tum... pausa (monstro de sangue)
    ///   Flanco       -> faz uma curva e chega pelo lado                        (demonio)
    ///   Revoada      -> arrancadas curtas em V (esquerda, direita) e paradas no ar (vampiro)
    ///   Interceptar  -> corre pra onde o jogador VAI estar, nao pra onde esta  (orc de elite)
    ///   Cerco        -> cada um mira um ponto em volta do jogador: o bando cerca (esqueleto guerreiro)
    ///   Espiral      -> chega girando em volta do jogador, em espiral          (demonia da foice)
    ///   Cambaleante  -> lento e torto, acelerando e quase parando              (esqueleto)
    ///   Arrasto      -> arrasta os pes devagar e de vez em quando desliza rapido (esqueleto da foice)
    ///   Vaivem       -> chega perto e se afasta, sem parar, de 2,5 a 6          (demonio do tridente)
    ///   Guarda       -> parado em guarda; so avanca com o jogador perto        (esqueleto do espadao)
    ///   Embalo       -> de longe vem correndo, de perto anda pesado            (urso)
    /// </summary>
    public enum Aproximacao
    {
        Direta, Ziguezague, PassoPesado, Marcha, Finta, Pulsante, Flanco, Revoada,
        Interceptar, Cerco, Espiral, Cambaleante, Arrasto, Vaivem, Guarda, Embalo
    }

    public Aproximacao JeitoDeChegar { get; set; } = Aproximacao.Direta;

    private float faseDaAproximacao = -1f;
    private float tempoDoPasso;
    private bool passoParado;
    private float ladoDoFlanco;
    private float anguloDoCerco;
    private Vector2 rumoDaMarcha;
    private Rigidbody2D corpoDoJogador;

    /// <summary>A especie (a fabrica preenche). O bestiario e as conquistas contam por aqui.</summary>
    public TipoDeInimigo Tipo { get; set; }

    /// <summary>Velocidade maxima andando agora (a dificuldade da fase multiplica em cima dela).</summary>
    public float Velocidade => velocidade;

    /// <summary>O som da morte: a <see cref="FabricaDeInimigos"/> escolhe pelo tipo do bicho (ossos, gosma, demonio...).</summary>
    public Som SomDeMorte { get; set; } = Som.MorteInimigo;

    /// <summary>
    /// Troca a cor base do desenho (inimigo campeao). O flash de dano, a sala acordando e o
    /// <see cref="Vida"/> voltam pra esta cor, nao pra original.
    /// </summary>
    public void Tingir(Color cor)
    {
        if (desenho == null)
            return;

        corOriginal = cor;
        desenho.color = cor;
        vida?.TrocarCorBase(cor);
    }

    /// <summary>Do inimigo ate o jogador (zero sem jogador). A animacao usa pra virar pro alvo.</summary>
    public Vector2 DirecaoDoJogador => rb != null ? ParaOJogador() : Vector2.zero;

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
        sala = GetComponentInParent<Sala>();
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
        vida.AoMorrer.AddListener(AvisarMorte);
    }

    protected virtual void OnDisable()
    {
        Ativos.Remove(this);
        vida.AoTomarDano.RemoveListener(AoLevarGolpe);
        vida.AoMorrer.RemoveListener(Morrer);
        vida.AoMorrer.RemoveListener(AvisarMorte);
    }

    /// <summary>A sala chama quando o jogador entra. Chamar de novo nao faz nada.</summary>
    public void Acordar()
    {
        if (EstadoAtual != Estado.Dormindo)
            return;

        Registro.Viu(Tipo);

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
        velocidadeAlvo *= MultiplicadorDeVelocidade;
        Vector2 alvo = direcao.sqrMagnitude > 0.0001f ? Desviar(direcao.normalized) * velocidadeAlvo : Vector2.zero;

        rb.linearVelocity = aceleracao > 0f
            ? Vector2.MoveTowards(rb.linearVelocity, alvo, aceleracao * Time.fixedDeltaTime)
            : alvo;
    }

    protected void Frear() => Andar(Vector2.zero, 0f);

    /// <summary>
    /// A direcao <paramref name="desejada"/> pro jogador, ou, se tem pedra, bloco ou buraco no
    /// caminho reto, a direcao do caminho pelos ladrilhos da sala (mesmo tamanho). Quem persegue
    /// usa isto pra contornar o obstaculo em vez de ficar empurrando ele.
    /// </summary>
    protected Vector2 PeloCaminho(Vector2 desejada)
    {
        return jogador == null ? desejada : PeloCaminhoAte(jogador.position, desejada);
    }

    /// <summary>Como <see cref="PeloCaminho"/>, ate um ponto qualquer do mundo.</summary>
    protected Vector2 PeloCaminhoAte(Vector2 destino, Vector2 desejada)
    {
        if (sala == null || desejada.sqrMagnitude < 0.0001f || sala.LinhaLivre(rb.position, destino, Raio * 0.8f))
            return desejada;

        Vector2? passo = sala.ProximoPasso(rb.position, destino);
        return passo.HasValue ? passo.Value * desejada.magnitude : desejada;
    }

    /// <summary>
    /// Anda atras do jogador no jeito da especie (<see cref="JeitoDeChegar"/>), contornando
    /// obstaculo. <paramref name="alvo"/> e o vetor ate o jogador.
    /// </summary>
    protected void Aproximar(Vector2 alvo, float velocidadeBase, float dt)
    {
        float distancia = alvo.magnitude;

        if (distancia < 0.0001f || jogador == null)
        {
            Frear();
            return;
        }

        Vector2 frente = alvo / distancia;
        Vector2 lado = new Vector2(-frente.y, frente.x);

        // Sorteios por bicho, uma vez: fase, lado e lugar no cerco.
        if (faseDaAproximacao < 0f)
        {
            faseDaAproximacao = Random.value * 10f;
            ladoDoFlanco = Random.value < 0.5f ? -1f : 1f;
            anguloDoCerco = Random.value * 360f;
            rumoDaMarcha = frente;
        }

        faseDaAproximacao += dt;
        tempoDoPasso -= dt;

        switch (JeitoDeChegar)
        {
            case Aproximacao.Ziguezague:
            {
                Vector2 rumo = distancia > 1.4f ? frente + lado * Mathf.Sin(faseDaAproximacao * 6f) * 1.1f : frente;
                Andar(LivreOuCaminho(rumo, frente), velocidadeBase * 1.15f);
                return;
            }

            case Aproximacao.PassoPesado:
                if (tempoDoPasso <= 0f)
                {
                    passoParado = !passoParado;
                    tempoDoPasso = passoParado ? Random.Range(0.35f, 0.55f) : Random.Range(0.6f, 0.85f);
                }

                if (passoParado)
                    Frear();
                else
                    Andar(PeloCaminho(frente), velocidadeBase * 1.45f);

                return;

            case Aproximacao.Marcha:
            {
                // Rumo fixo; so mira de novo a cada 1,6 s ou quando trava em algo.
                bool travado = Physics2D.CircleCast(rb.position, Raio * 0.9f, rumoDaMarcha, 0.35f, Camadas.MascaraDeParede).collider != null;

                if (tempoDoPasso <= 0f || travado)
                {
                    rumoDaMarcha = PeloCaminho(frente).normalized;
                    tempoDoPasso = 1.6f;
                }

                Andar(rumoDaMarcha, velocidadeBase);
                return;
            }

            case Aproximacao.Finta:
                // 0,9 s avancando, 0,35 s recuando de lado.
                if (tempoDoPasso <= 0f)
                {
                    passoParado = !passoParado;
                    tempoDoPasso = passoParado ? 0.35f : 0.9f;
                }

                if (passoParado && distancia < 4f)
                    Andar(-frente + lado * ladoDoFlanco * 0.6f, velocidadeBase * 0.9f);
                else
                    Andar(PeloCaminho(frente), velocidadeBase * 1.2f);

                return;

            case Aproximacao.Pulsante:
            {
                // Tum-tum, pausa: dois empurroes curtos e um descanso, num ciclo de 1,4 s.
                float t = Mathf.Repeat(faseDaAproximacao, 1.4f);
                bool pulso = t < 0.18f || (t > 0.32f && t < 0.5f);
                Andar(PeloCaminho(frente), pulso ? velocidadeBase * 2.6f : 0f);
                return;
            }

            case Aproximacao.Flanco:
                if (distancia > 1.8f)
                {
                    Vector2 doLado = Quaternion.Euler(0f, 0f, 75f * ladoDoFlanco) * -frente;
                    Vector2 ponto = (Vector2)jogador.position + doLado * Mathf.Min(1.8f, distancia * 0.6f);
                    Andar(PeloCaminhoAte(ponto, ponto - rb.position), velocidadeBase * 1.05f);
                }
                else
                {
                    Andar(PeloCaminho(frente), velocidadeBase);
                }

                return;

            case Aproximacao.Revoada:
                // Arrancada de 0,25 s na diagonal (alternando o lado), parada de 0,45 s no ar.
                if (tempoDoPasso <= 0f)
                {
                    passoParado = !passoParado;
                    tempoDoPasso = passoParado ? 0.45f : 0.25f;

                    if (!passoParado)
                        ladoDoFlanco = -ladoDoFlanco;
                }

                if (passoParado)
                    Frear();
                else
                    Andar(LivreOuCaminho(frente + lado * ladoDoFlanco * 0.9f, frente), velocidadeBase * 3f);

                return;

            case Aproximacao.Interceptar:
            {
                if (corpoDoJogador == null)
                    corpoDoJogador = jogador.GetComponent<Rigidbody2D>();

                // Mira onde o jogador estara daqui a pouco (mais adiante quanto mais longe).
                Vector2 adiante = corpoDoJogador != null ? corpoDoJogador.linearVelocity * Mathf.Clamp(distancia * 0.25f, 0.2f, 1.2f) : Vector2.zero;
                Vector2 ponto = (Vector2)jogador.position + adiante;
                Andar(PeloCaminhoAte(ponto, ponto - rb.position), velocidadeBase * 1.1f);
                return;
            }

            case Aproximacao.Cerco:
                if (distancia > 2.2f)
                {
                    float a = anguloDoCerco * Mathf.Deg2Rad;
                    Vector2 ponto = (Vector2)jogador.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 1.6f;
                    Andar(PeloCaminhoAte(ponto, ponto - rb.position), velocidadeBase);
                }
                else
                {
                    Andar(PeloCaminho(frente), velocidadeBase * 0.9f);
                }

                return;

            case Aproximacao.Espiral:
            {
                // Mais de lado que de frente: entra girando.
                Vector2 rumo = lado * ladoDoFlanco * 1.2f + frente * (distancia > 1.2f ? 0.7f : 1.5f);
                Andar(LivreOuCaminho(rumo, frente), velocidadeBase * 1.1f);
                return;
            }

            case Aproximacao.Cambaleante:
            {
                float passo = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(faseDaAproximacao * 2.3f));
                Vector2 rumo = frente + lado * Mathf.Sin(faseDaAproximacao * 1.3f) * 0.45f;
                Andar(LivreOuCaminho(rumo, frente), velocidadeBase * passo);
                return;
            }

            case Aproximacao.Arrasto:
                // Arrasta (60%) e a cada 2,4 s desliza 0,4 s bem rapido.
                if (tempoDoPasso <= 0f)
                {
                    passoParado = !passoParado;
                    tempoDoPasso = passoParado ? 0.4f : 2.4f;
                }

                Andar(PeloCaminho(frente), passoParado ? velocidadeBase * 3f : velocidadeBase * 0.55f);
                return;

            case Aproximacao.Vaivem:
                // Vai ate 2,5 do jogador e volta ate 6, e de novo: o tridente vem de varias distancias.
                if (distancia < 2.5f)
                    passoParado = true;       // passoParado = voltando
                else if (distancia > 6f)
                    passoParado = false;

                Andar(passoParado ? LivreOuCaminho(-frente, -frente) : PeloCaminho(frente), velocidadeBase * 1.1f);
                return;

            case Aproximacao.Guarda:
                // Em guarda ate o jogador chegar a 4,5; ai avanca firme.
                if (distancia > 4.5f && VeOJogador())
                    Frear();
                else
                    Andar(PeloCaminho(frente), velocidadeBase * 1.1f);

                return;

            case Aproximacao.Embalo:
                // Velocidade sobe com a distancia: de longe e uma corrida, de perto um passo.
                Andar(PeloCaminho(frente), velocidadeBase * Mathf.Lerp(0.6f, 2.2f, Mathf.InverseLerp(1.5f, 6f, distancia)));
                return;

            default:
                Andar(PeloCaminho(frente), velocidadeBase);
                return;
        }
    }

    /// <summary>O rumo torto se o caminho ate o jogador esta livre; senao, o caminho pela sala.</summary>
    protected Vector2 LivreOuCaminho(Vector2 rumo, Vector2 frente)
    {
        Vector2 pelaSala = PeloCaminho(frente);
        return pelaSala == frente ? rumo : pelaSala;
    }

    /// <summary>
    /// Anda pela sala de um ponto livre a outro, contornando obstaculo, com umas paradinhas.
    /// E o jeito dos que atiram: em vez de ficar plantado a uma distancia fixa do jogador, vai
    /// pra la e pra ca soltando tiro. Sem sala, fica de lado pro jogador.
    /// </summary>
    protected void Passear(float velocidadeDoPasseio)
    {
        if (sala == null)
        {
            Frear();
            return;
        }

        if (Time.time < paradoAte)
        {
            Frear();
            return;
        }

        Vector2 falta = pontoDoPasseio - rb.position;

        if (Time.time >= trocarPasseioEm || falta.sqrMagnitude < 0.3f)
        {
            // Chegou (ou demorou demais, preso): as vezes da uma paradinha, depois escolhe outro ponto.
            if (falta.sqrMagnitude < 0.3f && Random.value < 0.4f)
                paradoAte = Time.time + Random.Range(0.3f, 0.9f);

            pontoDoPasseio = (Vector2)sala.transform.position + sala.PontoLivreAleatorio(1.3f);
            trocarPasseioEm = Time.time + Random.Range(2f, 4f);
            falta = pontoDoPasseio - rb.position;
        }

        Andar(PeloCaminhoAte(pontoDoPasseio, falta.normalized), velocidadeDoPasseio);
    }

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

    /// <summary>A sala onde o inimigo nasceu (null fora de uma sala). Pra sortear ponto, chamar ajuda...</summary>
    protected Sala SalaDoInimigo => sala;

    /// <summary>Todo inimigo vivo da mesma sala que esta (pra limitar quem invoca mais bicho).</summary>
    protected int VivosNaSala => sala != null ? sala.InimigosVivos : 0;

    /// <summary>
    /// Some ou volta: sem colisor (tiro passa, nao machuca encostando) e com o desenho
    /// transparente na fracao <paramref name="alfa"/>. Pro bruxo que some e o fogo-fatuo que apaga.
    /// </summary>
    protected void Intangivel(bool sumido, float alfa)
    {
        foreach (Collider2D c in GetComponents<Collider2D>())
            c.enabled = !sumido;

        if (desenho != null)
        {
            Color cor = desenho.color;
            cor.a = alfa;
            desenho.color = cor;
        }
    }

    /// <summary>Um tiro do inimigo saindo da borda do corpo, no angulo dado (graus), com o estilo dele.</summary>
    protected TiroDaSala Disparar(float anguloEmGraus, float velocidadeDoTiro, float dano, Color cor, float diametro = 0.3f)
    {
        Vector2 rumo = new Vector2(Mathf.Cos(anguloEmGraus * Mathf.Deg2Rad), Mathf.Sin(anguloEmGraus * Mathf.Deg2Rad));
        return TiroDaSala.Disparar(rb.position + rumo * (Raio + 0.15f), rumo * velocidadeDoTiro, dano, gameObject, true, cor, diametro);
    }

    /// <summary>O angulo (graus) do inimigo ate o jogador.</summary>
    protected float AnguloDoJogador()
    {
        Vector2 alvo = ParaOJogador();
        return Mathf.Atan2(alvo.y, alvo.x) * Mathf.Rad2Deg;
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

        bool forte = info.Peso == PesoDoGolpe.Forte;
        Impacto.Numero(rb.position + Vector2.up * Raio, info.Quantidade, forte);

        if (forte)
        {
            Impacto.Congelar(0.03f);
            Impacto.Tremer(0.05f, 0.1f);
        }

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

    private void AvisarMorte() => AlgumMorreu?.Invoke(this);

    protected virtual void Morrer()
    {
        EstadoAtual = Estado.Morto;
        Sons.Tocar(SomDeMorte, SomDeMorte == Som.MorteChefe ? 1f : 0.8f);

        // A morte "pesa": um congelamento curtinho e a tela treme (muito mais no chefe).
        bool chefe = SomDeMorte == Som.MorteChefe;
        Impacto.Congelar(chefe ? 0.12f : 0.045f);
        Impacto.Tremer(chefe ? 0.4f : 0.08f, chefe ? 0.7f : 0.15f);

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
            c.enabled = false;

        // Encolhe: o Vida pisca e restaura a cor por conta propria, entao a morte se
        // mostra pelo tamanho, que ninguem mais mexe. Com arte, a animacao de morte mostra.
        if (!TryGetComponent(out AnimacaoDePersonagem _))
            transform.localScale *= 0.6f;
    }
}
