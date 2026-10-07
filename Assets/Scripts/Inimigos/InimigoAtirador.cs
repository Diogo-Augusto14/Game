using UnityEngine;

/// <summary>
/// O jeito de todos os inimigos (os numeros mudam de um pra outro): anda ate o jogador e, perto o
/// bastante, para, prepara o ataque (a animacao do ataque e o aviso) e ataca. Depois de um tempo,
/// prepara de novo. O ataque e um destes:
/// - com <see cref="arma"/>: atira o padrao dela (um tiro, leque, anel, rajada, espiral);
/// - com <see cref="investida"/>: corre reto pra onde o jogador estava e descansa um pouco depois;
/// - sem nenhum dos dois: so vai encostando (o dano vem do <see cref="DanoAoEncostar"/>).
///
/// Com arma e investida juntas, corre e atira no fim da corrida (a Geleia cai espirrando gotas).
/// A investida pode vir em sequencia (<see cref="investidasSeguidas"/>: o Demonio das laminas da tres).
///
/// Quem atira de longe pode recuar quando o jogador chega perto demais (<see cref="distanciaParaFugir"/>).
/// O jeito de chegar muda de bicho pra bicho (<see cref="JeitoDeAndar"/>): reto, rodeando o jogador
/// (lobisomem, morcego, demonia) ou aos trancos (morceguinho). Quem voa passa por cima dos buracos.
/// Alguns entram em furia com pouca vida (<see cref="vidaDaFuria"/>): ficam vermelhos e mais rapidos.
///
/// Comeca parado, sem saber do jogador. So acorda quando ve o jogador (perto e sem parede no meio)
/// ou quando leva um tiro; acordado, nao esquece mais. So atira com o caminho livre ate o jogador.
/// Pra chegar nele, vai reto quando da (sem parede nem buraco no caminho); quando nao da, segue o
/// <see cref="MapaDeCaminhos"/>, que contorna paredes e buracos.
///
/// O corpo e um Rigidbody2D Dynamic, como o do jogador: nao atravessa o jogador nem os outros
/// inimigos, e o empurrao dos golpes funciona sozinho (o andar freia de volta).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Vida))]
public class InimigoAtirador : MonoBehaviour, IAnimavel
{
    [Header("Ver o jogador")]
    [Tooltip("Acorda quando o jogador chega a esta distancia, sem parede no meio")]
    [SerializeField, Min(0f)] private float distanciaDeVisao = 10f;

    [Header("Andar")]
    [Tooltip("Velocidade maxima, em unidades por segundo")]
    [SerializeField, Min(0f)] private float velocidade = 2.2f;

    [Tooltip("Quao rapido chega na velocidade e freia (tambem e o que segura o empurrao)")]
    [SerializeField, Min(0f)] private float aceleracao = 20f;

    [Tooltip("Para de chegar perto a esta distancia do jogador")]
    [SerializeField, Min(0f)] private float distanciaParaParar = 4f;

    [Tooltip("Com o jogador mais perto que isto, recua (0 = nunca recua)")]
    [SerializeField, Min(0f)] private float distanciaParaFugir;

    [Tooltip("Reto ate o jogador, rodeando ele na distancia de parar, ou aos trancos")]
    [SerializeField] private JeitoDeAndar jeito = JeitoDeAndar.Direto;

    [Tooltip("Voa: passa por cima dos buracos do chao (morcegos, fogo-fatuo)")]
    [SerializeField] private bool voa;

    [Header("Ataque")]
    [Tooltip("A arma: o padrao dos tiros. Vazio = nao atira")]
    [SerializeField] private DadosDaArma arma;

    [Tooltip("So ataca com o jogador a esta distancia ou menos")]
    [SerializeField, Min(0f)] private float alcanceDoTiro = 8f;

    [Tooltip("Segundos parado preparando antes do ataque: o aviso pro jogador")]
    [SerializeField, Min(0f)] private float preparo = 0.5f;

    [Tooltip("Segundos entre um ataque e o proximo (varia um pouco pra cada inimigo nao atacar junto)")]
    [SerializeField, Min(0.1f)] private float intervalo = 2f;

    [Header("Investida (sem arma)")]
    [Tooltip("Em vez de atirar, corre reto pra onde o jogador estava")]
    [SerializeField] private bool investida;

    [SerializeField, Min(0f)] private float velocidadeDaInvestida = 10f;

    [Tooltip("Segundos correndo")]
    [SerializeField, Min(0.05f)] private float duracaoDaInvestida = 0.3f;

    [Tooltip("Segundos parado depois de correr: a hora de bater nele")]
    [SerializeField, Min(0f)] private float descanso = 0.6f;

    [Tooltip("Quantas investidas seguidas (com um preparo curtinho entre elas)")]
    [SerializeField, Min(1)] private int investidasSeguidas = 1;

    [SerializeField, Min(0f)] private float preparoEntreInvestidas = 0.3f;

    [Header("Furia")]
    [Tooltip("Entra em furia com esta fracao da vida ou menos (0 = nunca)")]
    [SerializeField, Range(0f, 1f)] private float vidaDaFuria;

    [Tooltip("Na furia anda, prepara e ataca mais rapido (1 = igual)")]
    [SerializeField, Min(1f)] private float ritmoNaFuria = 1.5f;

    [SerializeField] private Color corDaFuria = new Color(1f, 0.55f, 0.5f);

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
    private float proximaOlhada;
    private bool caminhoLivre;
    private bool andaReto;
    private readonly Rajada rajada = new Rajada();
    private Vector2 rumoDaInvestida;
    private float investindoAte = -1f;
    private float descansaAte = -1f;
    private bool investiu;
    private int investidasFaltando;
    private float sentido = 1f;
    private float trocaSentidoEm;
    private Vector2 tranco;
    private float trocaTrancoEm;
    private SpriteRenderer desenho;

    /// <summary>Com pouca vida, mais rapido (so quem tem <see cref="vidaDaFuria"/>).</summary>
    public bool NaFuria { get; private set; }

    private float Ritmo => NaFuria ? ritmoNaFuria : 1f;

    /// <summary>Ja viu o jogador (ou levou tiro) e esta atras dele.</summary>
    public bool Acordado { get; private set; }

    /// <summary>Parado, preparando o ataque (sai quando acabar).</summary>
    public bool Preparando => atiraEm >= 0f;

    /// <summary>Correndo na investida.</summary>
    public bool Investindo => Time.time < investindoAte;

    /// <summary>No meio de um ataque (preparando, correndo ou soltando a rajada): nao da pra sumir agora.</summary>
    public bool Ocupado => Preparando || Investindo || investiu || rajada.Atirando;

    /// <summary>A arma dele (o escudo do cavaleiro abre enquanto ataca).</summary>
    public DadosDaArma Arma => arma;

    /// <summary>Segundos de preparo (a animacao do ataque cabe neles).</summary>
    public float Preparo => preparo;

    /// <summary>Pra onde olha: o jogador. Tamanho 1.</summary>
    public Vector2 OlhandoPara { get; private set; } = Vector2.left;

    public Vector2 Velocidade => corpo.linearVelocity;

    /// <summary>Comecou a preparar o ataque (a animacao do ataque escuta): sempre a animacao 0.</summary>
    public event System.Action<int, float> AoAtacar;

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

        // O desenho do corpo: o de ordem mais alta (a sombra fica embaixo).
        foreach (SpriteRenderer d in GetComponentsInChildren<SpriteRenderer>())
        {
            if (desenho == null || d.sortingOrder > desenho.sortingOrder)
                desenho = d;
        }

        // Quem voa nao bate nos buracos (os tiros tambem passam por cima deles).
        if (voa && Pedreiro.CamadaDoBuraco >= 0)
        {
            foreach (Collider2D c in GetComponents<Collider2D>())
                c.excludeLayers |= 1 << Pedreiro.CamadaDoBuraco;
        }
    }

    private void OnEnable()
    {
        vida.AoTomarDano += Apanhou;
    }

    private void OnDisable()
    {
        vida.AoTomarDano -= Apanhou;
    }

    private void Apanhou(Dano dano) => Acordar();

    /// <summary>Passa a ir atras do jogador (ja acordado, nada muda).</summary>
    public void Acordar()
    {
        if (Acordado)
            return;

        Acordado = true;

        // Quem acabou de acordar demora um pouco pra atirar.
        proximoAtaque = Mathf.Max(proximoAtaque, Time.time + Random.Range(0.5f, 1f));
    }

    private void Start()
    {
        GameObject jogador = GameObject.FindWithTag("Player");

        if (jogador != null)
        {
            alvo = jogador.transform;
            vidaDoAlvo = jogador.GetComponent<Vida>();
        }

        proximoAtaque = Time.time + Random.Range(0.8f, intervalo);
        proximaOlhada = Time.time + Random.Range(0f, 0.2f);
        sentido = Random.value < 0.5f ? -1f : 1f;
    }

    private void Update()
    {
        querAndar = Vector2.zero;

        if (vida.Morto || alvo == null || (vidaDoAlvo != null && vidaDoAlvo.Morto))
        {
            atiraEm = -1f;
            rajada.Parar();
            return;
        }

        if (!NaFuria && vidaDaFuria > 0f && vida.Fracao <= vidaDaFuria)
            EntrarEmFuria();

        Vector2 ateOAlvo = (Vector2)alvo.position - (Vector2)transform.position;
        float distancia = ateOAlvo.magnitude;

        // Olha pro jogador umas vezes por segundo (nao a cada quadro: sao muitos inimigos no andar).
        if (Time.time >= proximaOlhada)
        {
            proximaOlhada = Time.time + 0.2f;
            caminhoLivre = distancia <= Mathf.Max(distanciaDeVisao, alcanceDoTiro) && !ParedeNoMeio(alvo.position);
            andaReto = caminhoLivre && !BarradoNoCaminho(ateOAlvo, distancia);

            if (caminhoLivre && distancia <= distanciaDeVisao)
                Acordar();
        }

        if (!Acordado)
            return;

        // Continua olhando pro jogador enquanto prepara: a bala vai pra onde ele esta quando sai.
        if (distancia > 0.01f)
            OlhandoPara = ateOAlvo / distancia;

        // Na investida e no descanso depois dela, nada de pensar.
        if (Investindo)
            return;

        if (investiu)
            AcabouAInvestida();

        // A rajada continua saindo, sempre pra onde o jogador esta; parado enquanto atira.
        if (rajada.Atirando)
        {
            if (rajada.Atualizar(Saida(), OlhandoPara, gameObject, vida.Lado))
                TocarOTiro();

            return;
        }

        if (Preparando)
        {
            if (Time.time >= atiraEm)
            {
                Atacar();
                atiraEm = -1f;
                proximoAtaque = Time.time + intervalo / Ritmo * Random.Range(0.85f, 1.15f);
            }

            return;
        }

        if (Time.time < descansaAte)
            return;

        if ((investida ? andaReto : arma != null) && caminhoLivre && distancia <= alcanceDoTiro && Time.time >= proximoAtaque)
        {
            investidasFaltando = 0;
            atiraEm = Time.time + preparo / Ritmo;
            AoAtacar?.Invoke(0, preparo / Ritmo);
            return;
        }

        querAndar = Andar(distancia);
    }

    // Pra onde ir agora, fora do ataque: fugir, chegar (reto ou pelo mapa), rodear ou aos trancos.
    private Vector2 Andar(float distancia)
    {
        if (distanciaParaFugir > 0f && caminhoLivre && distancia < distanciaParaFugir)
            return -OlhandoPara;

        if (!caminhoLivre || !andaReto)
            return distancia > distanciaParaParar || !caminhoLivre
                ? (andaReto || MapaDeCaminhos.Atual == null ? OlhandoPara : MapaDeCaminhos.Atual.Rumo(transform.position))
                : Vector2.zero;

        switch (jeito)
        {
            case JeitoDeAndar.Rodeando:
            {
                // Gira em volta do jogador, corrigindo a distancia; troca de lado de vez em quando.
                if (Time.time >= trocaSentidoEm)
                {
                    trocaSentidoEm = Time.time + Random.Range(2f, 4f);

                    if (Random.value < 0.4f)
                        sentido = -sentido;
                }

                Vector2 deLado = new Vector2(-OlhandoPara.y, OlhandoPara.x) * sentido;
                float falta = Mathf.Clamp((distancia - distanciaParaParar) * 0.6f, -1f, 1f);
                return (deLado + OlhandoPara * falta).normalized;
            }

            case JeitoDeAndar.AosTrancos:
            {
                // Um rumo torto que muda varias vezes por segundo; perto do jogador fica so tremulando.
                if (Time.time >= trocaTrancoEm)
                {
                    trocaTrancoEm = Time.time + Random.Range(0.2f, 0.45f);
                    tranco = Random.insideUnitCircle.normalized;
                }

                Vector2 ir = distancia > distanciaParaParar ? OlhandoPara : Vector2.zero;
                return (ir + tranco * 0.9f).normalized;
            }

            default:
                return distancia > distanciaParaParar ? OlhandoPara : Vector2.zero;
        }
    }

    private void EntrarEmFuria()
    {
        NaFuria = true;
        CameraDoJogo.Tremer(0.12f, 0.2f);
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.4f, "!", new Color(1f, 0.35f, 0.25f), 0.8f).Pular(1.4f);

        if (desenho != null)
            desenho.color = new Color(corDaFuria.r, corDaFuria.g, corDaFuria.b, desenho.color.a);
    }

    // Fim de uma corrida: atira (quem tem arma), e prepara a proxima da sequencia ou descansa.
    private void AcabouAInvestida()
    {
        investiu = false;
        corpo.linearVelocity = Vector2.zero;

        if (arma != null)
        {
            rajada.Comecar(arma);

            if (rajada.Atualizar(Saida(), OlhandoPara, gameObject, vida.Lado))
                TocarOTiro();
        }

        if (investidasFaltando > 0)
        {
            atiraEm = Time.time + preparoEntreInvestidas / Ritmo;
            AoAtacar?.Invoke(0, preparoEntreInvestidas / Ritmo);
        }
        else
        {
            descansaAte = Time.time + descanso / Ritmo;
        }
    }

    private void FixedUpdate()
    {
        if (vida.Morto)
            return;

        // Na investida a velocidade e cheia na hora (sem acelerar); parede e gente seguram pela fisica.
        if (Investindo)
        {
            corpo.linearVelocity = rumoDaInvestida * velocidadeDaInvestida * Ritmo;
            return;
        }

        corpo.linearVelocity = Vector2.MoveTowards(corpo.linearVelocity, querAndar * velocidade * Ritmo, aceleracao * Time.fixedDeltaTime);
    }

    private bool ParedeNoMeio(Vector2 ate) =>
        Physics2D.Linecast(transform.position, ate, 1 << Pedreiro.CamadaDaParede).collider != null;

    // O corpo inteiro passa reto ate o jogador? (o tiro voa por cima do buraco; o corpo nao)
    private bool BarradoNoCaminho(Vector2 ateOAlvo, float distancia)
    {
        int camadas = 1 << Pedreiro.CamadaDaParede;

        if (!voa && Pedreiro.CamadaDoBuraco >= 0)
            camadas |= 1 << Pedreiro.CamadaDoBuraco;

        return Physics2D.CircleCast(transform.position, 0.35f, ateOAlvo, distancia, camadas).collider != null;
    }

    private void Atacar()
    {
        if (investida)
        {
            // A primeira de uma sequencia conta quantas faltam; as outras so descontam.
            if (investidasFaltando <= 0)
                investidasFaltando = investidasSeguidas;

            investidasFaltando--;
            rumoDaInvestida = OlhandoPara;
            investindoAte = Time.time + duracaoDaInvestida;
            investiu = true;
        }
        else if (arma != null)
        {
            rajada.Comecar(arma);

            if (rajada.Atualizar(Saida(), OlhandoPara, gameObject, vida.Lado))
                TocarOTiro();
        }
    }

    private Vector2 Saida() => (Vector2)transform.position + Vector2.up * alturaDaSaida + OlhandoPara * distanciaDaSaida;

    private void TocarOTiro()
    {
        if (audioSource != null && arma.som != null)
        {
            audioSource.pitch = Random.Range(0.94f, 1.06f);
            audioSource.PlayOneShot(arma.som, arma.volume);
        }
    }
}

/// <summary>Como o inimigo chega no jogador quando nao esta atacando.</summary>
public enum JeitoDeAndar
{
    /// <summary>Reto ate a distancia de parar.</summary>
    Direto,

    /// <summary>Gira em volta do jogador na distancia de parar (lobisomem, morcego, demonia).</summary>
    Rodeando,

    /// <summary>Aos trancos, mudando de rumo o tempo todo (morceguinho).</summary>
    AosTrancos,
}
