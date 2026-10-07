using System.Collections;
using UnityEngine;

/// <summary>
/// Um chefe: o dono de um andar so dele (a arena). Depois de uma apresentacao curta, alterna os
/// ataques da lista <see cref="ataques"/> sem repetir o mesmo duas vezes seguidas, andando um pouco
/// entre um e outro. Cada ataque tem um preparo (a animacao e o aviso) e depois:
/// - atira o padrao da arma (um anel, uma espiral, leques em rajada...);
/// - ou da investidas seguidas (com arma tambem, ela dispara no fim de cada investida).
///
/// Com pouca vida entra em furia: fica mais rapido e ganha os ataques marcados "so na furia".
/// No alto da tela, o nome e a barra de vida (provisoria, ate a interface da etapa 7).
///
/// Morrer e com a <see cref="Vida"/> e a <see cref="MorteDoInimigo"/>, como qualquer inimigo; o
/// <see cref="GeradorDoAndar"/> abre o portal quando ele cai.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Vida))]
public class Chefe : MonoBehaviour, IAnimavel
{
    [SerializeField] private string nome = "Chefe";

    [Tooltip("Segundos parado no comeco, antes de atacar (a apresentacao)")]
    [SerializeField, Min(0f)] private float apresentacao = 2f;

    [Header("Andar")]
    [SerializeField, Min(0f)] private float velocidade = 1.8f;

    [SerializeField, Min(0f)] private float aceleracao = 15f;

    [Tooltip("Para de chegar perto a esta distancia do jogador")]
    [SerializeField, Min(0f)] private float distanciaParaParar = 4f;

    [Header("Ataques")]
    [SerializeField] private AtaqueDoChefe[] ataques;

    [Tooltip("Segundos andando entre um ataque e o proximo")]
    [SerializeField, Min(0f)] private float pausa = 1.2f;

    [Tooltip("De onde os tiros saem: distancia do centro do corpo, na direcao do jogador")]
    [SerializeField, Min(0f)] private float distanciaDaSaida = 0.6f;

    [Tooltip("Altura de onde os tiros saem, em relacao ao centro do corpo")]
    [SerializeField] private float alturaDaSaida;

    [Header("Furia")]
    [Tooltip("Entra em furia com esta fracao da vida ou menos")]
    [SerializeField, Range(0f, 1f)] private float vidaDaFuria = 0.5f;

    [Tooltip("Na furia tudo fica mais rapido: andar, preparo e pausa (1 = igual)")]
    [SerializeField, Min(1f)] private float ritmoNaFuria = 1.35f;

    [Tooltip("Cor do corpo na furia")]
    [SerializeField] private Color corDaFuria = new Color(1f, 0.75f, 0.75f);

    [SerializeField] private SpriteRenderer corpoDesenhado;

    [Header("Sons")]
    [SerializeField] private AudioClip somAoChegar;
    [SerializeField] private AudioClip somDaFuria;
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;

    private Rigidbody2D corpo;
    private Vida vida;
    private AudioSource audioSource;
    private Transform alvo;
    private Vida vidaDoAlvo;
    private readonly Rajada rajada = new Rajada();
    private Vector2 querAndar;
    private Vector2 rumoDaInvestida;
    private float investindoAte = -1f;
    private float velocidadeDaInvestida;
    private int ultimo = -1;
    private bool apresentou;
    private GUIStyle estiloDoNome;

    public string Nome => nome;

    public bool NaFuria { get; private set; }

    public Vector2 OlhandoPara { get; private set; } = Vector2.down;

    public Vector2 Velocidade => corpo.linearVelocity;

    public event System.Action<int, float> AoAtacar;

    private bool Investindo => Time.time < investindoAte;

    private float Ritmo => NaFuria ? ritmoNaFuria : 1f;

    private bool Acabou => vida.Morto || alvo == null || (vidaDoAlvo != null && vidaDoAlvo.Morto);

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

        Tocar(somAoChegar);
        StartCoroutine(Lutar());
    }

    private void Update()
    {
        if (vida.Morto)
            return;

        if (!NaFuria && vida.Fracao <= vidaDaFuria)
        {
            NaFuria = true;
            Tocar(somDaFuria);
            CameraDoJogo.Tremer(0.2f, 0.4f);

            if (corpoDesenhado != null)
                corpoDesenhado.color = corDaFuria;
        }

        // Olha pro jogador, menos no meio da investida (ai olha pra onde corre).
        if (alvo != null && !Investindo)
        {
            Vector2 ate = (Vector2)alvo.position - (Vector2)transform.position;

            if (ate.sqrMagnitude > 0.0001f)
                OlhandoPara = ate.normalized;
        }
    }

    private void FixedUpdate()
    {
        if (vida.Morto)
            return;

        if (Investindo)
        {
            corpo.linearVelocity = rumoDaInvestida * velocidadeDaInvestida;
            return;
        }

        corpo.linearVelocity = Vector2.MoveTowards(corpo.linearVelocity, querAndar * velocidade * Ritmo, aceleracao * Time.fixedDeltaTime);
    }

    private IEnumerator Lutar()
    {
        yield return new WaitForSeconds(apresentacao);
        apresentou = true;

        while (!vida.Morto)
        {
            // Anda um pouco (chegando perto, mas nao demais) antes do proximo ataque.
            for (float ate = Time.time + pausa / Ritmo; Time.time < ate && !vida.Morto;)
            {
                querAndar = Acabou ? Vector2.zero : Caminho();
                yield return null;
            }

            querAndar = Vector2.zero;

            if (Acabou)
            {
                yield return null;
                continue;
            }

            AtaqueDoChefe ataque = Escolher();

            if (ataque == null)
            {
                yield return null;
                continue;
            }

            yield return Atacar(ataque);
        }

        querAndar = Vector2.zero;
    }

    private IEnumerator Atacar(AtaqueDoChefe ataque)
    {
        float preparo = ataque.preparo / Ritmo;
        AoAtacar?.Invoke(ataque.animacao, preparo);
        yield return new WaitForSeconds(preparo);

        if (Acabou)
            yield break;

        if (ataque.investidas <= 0)
        {
            yield return Disparar(ataque.arma);
            yield break;
        }

        for (int i = 0; i < ataque.investidas && !Acabou; i++)
        {
            // Da segunda em diante, um preparo curtinho (o aviso de que vai de novo).
            if (i > 0)
            {
                float curto = ataque.preparoEntreInvestidas / Ritmo;
                AoAtacar?.Invoke(ataque.animacao, curto);
                yield return new WaitForSeconds(curto);
            }

            rumoDaInvestida = OlhandoPara;
            velocidadeDaInvestida = ataque.velocidadeDaInvestida * Ritmo;
            investindoAte = Time.time + ataque.duracaoDaInvestida;
            yield return new WaitForSeconds(ataque.duracaoDaInvestida);

            corpo.linearVelocity = Vector2.zero;
            CameraDoJogo.Tremer(0.08f, 0.15f);

            if (ataque.arma != null && !Acabou)
                yield return Disparar(ataque.arma);
        }
    }

    // A rajada inteira da arma, sempre mirando no jogador; parado enquanto ela sai.
    private IEnumerator Disparar(DadosDaArma arma)
    {
        if (arma == null)
            yield break;

        rajada.Comecar(arma);

        while (rajada.Atirando && !Acabou)
        {
            Vector2 origem = (Vector2)transform.position + Vector2.up * alturaDaSaida + OlhandoPara * distanciaDaSaida;

            if (rajada.Atualizar(origem, OlhandoPara, gameObject, vida.Lado))
            {
                Tocar(arma.som, arma.volume);
                CameraDoJogo.Tremer(arma.tremor, 0.06f);
            }

            yield return null;
        }

        rajada.Parar();
    }

    // Sorteia um ataque que vale agora (fora os "so na furia" antes dela), sem repetir o ultimo.
    private AtaqueDoChefe Escolher()
    {
        if (ataques == null || ataques.Length == 0)
            return null;

        int validos = 0;

        for (int i = 0; i < ataques.Length; i++)
        {
            if (Vale(i, true))
                validos++;
        }

        bool semRepetir = validos > 0;

        if (!semRepetir)
        {
            for (int i = 0; i < ataques.Length; i++)
            {
                if (Vale(i, false))
                    validos++;
            }
        }

        if (validos == 0)
            return null;

        int sorteado = Random.Range(0, validos);

        for (int i = 0; i < ataques.Length; i++)
        {
            if (!Vale(i, semRepetir))
                continue;

            if (sorteado-- == 0)
            {
                ultimo = i;
                return ataques[i];
            }
        }

        return null;
    }

    private bool Vale(int i, bool semRepetir) =>
        ataques[i] != null && (!ataques[i].soNaFuria || NaFuria) && (!semRepetir || i != ultimo)
        && (ataques[i].arma != null || ataques[i].investidas > 0);

    // Vai reto quando da; com pilar no meio, pelo mapa de caminhos da arena.
    private Vector2 Caminho()
    {
        Vector2 ate = (Vector2)alvo.position - (Vector2)transform.position;

        if (ate.magnitude <= distanciaParaParar)
            return Vector2.zero;

        bool parede = Physics2D.Linecast(transform.position, alvo.position, 1 << Pedreiro.CamadaDaParede).collider != null;

        if (parede && MapaDeCaminhos.Atual != null)
            return MapaDeCaminhos.Atual.Rumo(transform.position);

        return ate.normalized;
    }

    private void Tocar(AudioClip som, float quanto = -1f)
    {
        if (audioSource != null && som != null)
            audioSource.PlayOneShot(som, quanto < 0f ? volume : quanto);
    }

    // O nome e a barra de vida no alto da tela (provisorio: a interface de verdade vem na etapa 7).
    private void OnGUI()
    {
        if (!apresentou || vida.Morto)
            return;

        if (estiloDoNome == null)
            estiloDoNome = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter, fontSize = Mathf.Max(16, Screen.height / 32) };

        float largura = Screen.width * 0.5f;
        float altura = Mathf.Max(10f, Screen.height / 50f);
        Rect barra = new Rect((Screen.width - largura) * 0.5f, Screen.height * 0.07f, largura, altura);

        GUI.color = Color.white;
        GUI.Label(new Rect(barra.x, barra.y - 40f, largura, 38f), nome, estiloDoNome);
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(barra.x - 2f, barra.y - 2f, barra.width + 4f, barra.height + 4f), Texture2D.whiteTexture);
        GUI.color = NaFuria ? new Color(1f, 0.35f, 0.2f) : new Color(0.85f, 0.15f, 0.2f);
        GUI.DrawTexture(new Rect(barra.x, barra.y, barra.width * vida.Fracao, barra.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}

/// <summary>Um ataque do chefe: o padrao (arma), as investidas, o preparo e a animacao.</summary>
[System.Serializable]
public class AtaqueDoChefe
{
    [Tooltip("So pra se achar no Inspector")]
    public string nome;

    [Tooltip("O padrao dos tiros. Com investidas, dispara no fim de cada uma")]
    public DadosDaArma arma;

    [Tooltip("Quantas investidas seguidas (0 = nenhuma, so atira)")]
    [Min(0)] public int investidas;

    [Min(0f)] public float velocidadeDaInvestida = 12f;

    [Min(0.05f)] public float duracaoDaInvestida = 0.4f;

    [Tooltip("Segundos de aviso antes de cada investida, da segunda em diante")]
    [Min(0f)] public float preparoEntreInvestidas = 0.35f;

    [Tooltip("Segundos parado preparando: o aviso pro jogador")]
    [Min(0f)] public float preparo = 0.8f;

    [Tooltip("Qual animacao de ataque toca (0 = a primeira; 1, 2... = os outros ataques da AnimacaoDoInimigo)")]
    [Min(0)] public int animacao;

    [Tooltip("So aparece depois que o chefe entra em furia")]
    public bool soNaFuria;
}
