using System.Collections;
using UnityEngine;

/// <summary>
/// Um chefe: o dono de um andar so dele (a arena). Depois de uma apresentacao curta, alterna os
/// ataques da lista <see cref="ataques"/> sem repetir o mesmo duas vezes seguidas, andando um pouco
/// entre um e outro. Cada ataque tem um preparo (a animacao e o aviso) e depois:
/// - atira o padrao da arma (um anel, uma espiral, leques em rajada...);
/// - ou da investidas seguidas (com arma tambem, ela dispara no fim de cada investida).
///
/// Os ataques tambem podem (veio dos chefes do jogo antigo):
/// - pular ate o jogador (<see cref="AtaqueDoChefe.salto"/>: no ar nao acerta nem e acertado; cai atirando);
/// - sumir e reaparecer longe (<see cref="AtaqueDoChefe.sumir"/>);
/// - levantar bichos do chao (<see cref="AtaqueDoChefe.invocar"/>);
/// - marcar o chao em volta do jogador e, depois do preparo, atirar de cada marca (<see cref="AtaqueDoChefe.marcas"/>).
///
/// Com pouca vida entra em furia (a <see cref="ViradaDeFase"/>): fica mais rapido e ganha os ataques
/// marcados "so na furia". Com menos ainda, se tiver <see cref="armaDoFim"/>, atira ela o tempo todo.
/// No alto da tela, a <see cref="TelaDoJogo"/> mostra o nome e a barra de vida.
///
/// Morrer e com a <see cref="Vida"/> e a <see cref="MorteDoInimigo"/>, como qualquer inimigo; o
/// <see cref="GeradorDoAndar"/> abre o portal quando ele cai.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Vida))]
public class Chefe : MonoBehaviour, IAnimavel, IInvulneravel
{
    [SerializeField] private string nome = "Chefe";

    [Tooltip("O nome do andar dele (\"Covil do Minotauro\"), no aviso ao chegar")]
    [SerializeField] private string lugar;

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

    [Tooltip("A frase que aparece em cima dele quando entra em furia")]
    [SerializeField] private string fraseDaFuria = "Chega de brincadeira!";

    [SerializeField] private SpriteRenderer corpoDesenhado;

    [Header("Fim da luta")]
    [Tooltip("Com pouca vida (abaixo de Vida do fim) atira esta arma o tempo todo, girando. Vazio = nada")]
    [SerializeField] private DadosDaArma armaDoFim;

    [SerializeField, Range(0f, 1f)] private float vidaDoFim = 0.25f;

    [SerializeField, Min(0.1f)] private float intervaloDoFim = 0.6f;

    [SerializeField] private float giroDoFim = 15f;

    [Header("Pulo, sumico, marcas e invocacao")]
    [Tooltip("A marca no chao (onde vai cair, onde vai explodir)")]
    [SerializeField] private Sprite marca;

    [Tooltip("Altura do pulo, em unidades")]
    [SerializeField, Min(0f)] private float alturaDoPulo = 2.5f;

    [Tooltip("Maximo de bichos levantados vivos ao mesmo tempo")]
    [SerializeField, Min(1)] private int maximoDeInvocados = 4;

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
    private bool noAr;
    private bool sumido;
    private float proximoDoFim;
    private float giroAcumulado;
    private Collider2D[] colisores;
    private readonly System.Collections.Generic.List<Vida> invocados = new System.Collections.Generic.List<Vida>();

    /// <summary>No ar (pulando) ou sumido: nada acerta.</summary>
    public bool Invulneravel => noAr || sumido;

    public string Nome => nome;

    public string Lugar => string.IsNullOrEmpty(lugar) ? nome : lugar;

    public Vida Vida => vida;

    /// <summary>Ja passou a apresentacao (a barra de vida aparece).</summary>
    public bool Apresentou => apresentou;

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
        colisores = GetComponents<Collider2D>();

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
            ViradaDeFase.Anunciar(this, fraseDaFuria, new Color(1f, 0.45f, 0.3f));

            if (corpoDesenhado != null)
                corpoDesenhado.color = new Color(corDaFuria.r, corDaFuria.g, corDaFuria.b, corpoDesenhado.color.a);
        }

        // O fim da luta: a arma do fim sai sozinha, girando um pouco a cada vez.
        if (armaDoFim != null && apresentou && !Acabou && !sumido && vida.Fracao <= vidaDoFim && Time.time >= proximoDoFim)
        {
            proximoDoFim = Time.time + intervaloDoFim;
            giroAcumulado += giroDoFim;
            armaDoFim.Disparar((Vector2)transform.position + Vector2.up * alturaDaSaida, OlhandoPara, gameObject, vida.Lado, giroAcumulado);
            Tocar(armaDoFim.som, armaDoFim.volume * 0.6f);
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

        // As marcas aparecem ja no preparo (o aviso de onde vai explodir).
        GameObject[] marcas = ataque.marcas > 0 && ataque.arma != null ? Marcar(ataque) : null;
        yield return new WaitForSeconds(preparo);

        if (Acabou)
        {
            Apagar(marcas);
            yield break;
        }

        if (marcas != null)
        {
            DispararDasMarcas(ataque.arma, marcas);
            yield break;
        }

        if (ataque.sumir)
            yield return Sumir();

        if (ataque.invocar != null && !Acabou)
            Invocar(ataque.invocar, ataque.quantos);

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

            if (ataque.salto)
            {
                yield return Pular(ataque.duracaoDaInvestida / Mathf.Sqrt(Ritmo));
            }
            else
            {
                rumoDaInvestida = OlhandoPara;
                velocidadeDaInvestida = ataque.velocidadeDaInvestida * Ritmo;
                investindoAte = Time.time + ataque.duracaoDaInvestida;
                yield return new WaitForSeconds(ataque.duracaoDaInvestida);
            }

            corpo.linearVelocity = Vector2.zero;
            CameraDoJogo.Tremer(ataque.salto ? 0.3f : 0.08f, ataque.salto ? 0.25f : 0.15f);

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

    // Pula ate onde o jogador esta: a sombra corre no chao, o corpo sobe e desce em arco. Uma
    // marca mostra onde vai cair. No ar, nada encosta nele (sem colisor) e nada acerta (Invulneravel).
    private IEnumerator Pular(float duracao)
    {
        Vector2 de = corpo.position;
        Vector2 para = alvo != null ? (Vector2)alvo.position : de;
        GameObject aviso = CriarMarca(para, new Color(1f, 0.25f, 0.2f, 0.7f));
        Transform desenho = corpoDesenhado != null ? corpoDesenhado.transform : null;
        Vector3 baseDoDesenho = desenho != null ? desenho.localPosition : Vector3.zero;

        noAr = true;
        Colidir(false);

        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.05f, duracao))
        {
            corpo.linearVelocity = Vector2.zero;
            corpo.position = Vector2.Lerp(de, para, t);

            if (desenho != null)
                desenho.localPosition = baseDoDesenho + Vector3.up * Mathf.Sin(t * Mathf.PI) * alturaDoPulo / Mathf.Max(0.01f, transform.localScale.y);

            yield return null;
        }

        corpo.position = para;

        if (desenho != null)
            desenho.localPosition = baseDoDesenho;

        Colidir(true);
        noAr = false;
        Apagar(new[] { aviso });
    }

    // Some (desbota), aparece longe do jogador dentro da arena e volta a aparecer.
    private IEnumerator Sumir()
    {
        yield return Desbotar(1f, 0f, 0.3f);
        sumido = true;
        Colidir(false);

        Vector2 novo = corpo.position;

        for (int i = 0; i < 20 && alvo != null; i++)
        {
            Vector2 ponto = (Vector2)alvo.position + Random.insideUnitCircle.normalized * Random.Range(5f, 8f);
            int bloqueia = 1 << Pedreiro.CamadaDaParede;

            if (Physics2D.OverlapCircle(ponto, 1f, bloqueia) == null
                && (MapaDeCaminhos.Atual == null || MapaDeCaminhos.Atual.TemChao(ponto)))
            {
                novo = ponto;
                break;
            }
        }

        GameObject aviso = CriarMarca(novo, new Color(0.7f, 0.4f, 1f, 0.7f));
        yield return new WaitForSeconds(0.35f);
        corpo.position = novo;
        transform.position = novo;
        Apagar(new[] { aviso });

        sumido = false;
        Colidir(true);
        yield return Desbotar(0f, 1f, 0.25f);
    }

    private IEnumerator Desbotar(float de, float ate, float segundos)
    {
        SpriteRenderer[] desenhos = GetComponentsInChildren<SpriteRenderer>();

        for (float t = 0f; t <= 1f; t += Time.deltaTime / segundos)
        {
            foreach (SpriteRenderer d in desenhos)
            {
                if (d == null)
                    continue;

                Color c = d.color;
                c.a = Mathf.Lerp(de, ate, t);
                d.color = c;
            }

            yield return null;
        }

        foreach (SpriteRenderer d in desenhos)
        {
            if (d == null)
                continue;

            Color c = d.color;
            c.a = ate;
            d.color = c;
        }
    }

    private void Colidir(bool sim)
    {
        foreach (Collider2D c in colisores)
            c.enabled = sim;
    }

    // Levanta bichos em volta dele (ate o maximo de vivos), saindo do chao.
    private void Invocar(GameObject prefab, int quantos)
    {
        invocados.RemoveAll(v => v == null || v.Morto);

        for (int i = 0; i < quantos && invocados.Count < maximoDeInvocados; i++)
        {
            Vector2 onde = (Vector2)transform.position + Random.insideUnitCircle.normalized * Random.Range(1.8f, 2.6f);

            if (Physics2D.OverlapCircle(onde, 0.45f, 1 << Pedreiro.CamadaDaParede) != null)
                continue;

            GameObject novo = Instantiate(prefab, onde, Quaternion.identity, GeradorDoAndar.Raiz);

            if (novo.TryGetComponent(out Vida dele))
            {
                invocados.Add(dele);
                GeradorDoAndar.Registrar(dele);
            }

            if (novo.TryGetComponent(out InimigoAtirador levantado))
            {
                levantado.Acordar();
                novo.AddComponent<SaindoDoChao>().Comecar(0.6f);
            }
        }
    }

    // Marcas no chao em volta do jogador (e uma em cima dele): de cada uma sai a arma depois do preparo.
    private GameObject[] Marcar(AtaqueDoChefe ataque)
    {
        GameObject[] marcas = new GameObject[ataque.marcas];
        Vector2 centro = alvo != null ? (Vector2)alvo.position : (Vector2)transform.position;
        float giro = Random.Range(0f, 360f);

        for (int i = 0; i < marcas.Length; i++)
        {
            Vector2 onde = i == 0 ? centro : centro + (Vector2)(Quaternion.Euler(0f, 0f, giro + 360f * i / (marcas.Length - 1)) * Vector2.right) * ataque.raioDasMarcas;
            marcas[i] = CriarMarca(onde, new Color(1f, 0.25f, 0.2f, 0.75f));
        }

        return marcas;
    }

    private void DispararDasMarcas(DadosDaArma arma, GameObject[] marcas)
    {
        foreach (GameObject m in marcas)
        {
            if (m == null)
                continue;

            arma.Disparar(m.transform.position, Vector2.right, gameObject, vida.Lado, Random.Range(0f, 45f));
        }

        Tocar(arma.som, arma.volume);
        CameraDoJogo.Tremer(arma.tremor, 0.1f);
        Apagar(marcas);
    }

    private GameObject CriarMarca(Vector2 onde, Color cor)
    {
        if (marca == null)
            return null;

        GameObject obj = new GameObject("Marca do chefe");
        obj.transform.SetParent(GeradorDoAndar.Raiz, false);
        obj.transform.position = onde;
        SpriteRenderer desenho = obj.AddComponent<SpriteRenderer>();
        desenho.sprite = marca;
        desenho.color = cor;
        desenho.sortingOrder = Pedreiro.OrdemDosEnfeites + 1;
        obj.AddComponent<MarcaPiscando>();
        return obj;
    }

    private static void Apagar(GameObject[] marcas)
    {
        if (marcas == null)
            return;

        foreach (GameObject m in marcas)
        {
            if (m != null)
                Destroy(m);
        }
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
        && (ataques[i].arma != null || ataques[i].investidas > 0 || ataques[i].invocar != null || ataques[i].sumir);

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

    [Tooltip("As investidas viram pulos ate onde o jogador esta (cai atirando a arma)")]
    public bool salto;

    [Tooltip("Some e reaparece longe do jogador antes de atirar")]
    public bool sumir;

    [Tooltip("Levanta estes bichos do chao")]
    public GameObject invocar;

    [Min(0)] public int quantos = 2;

    [Tooltip("Marcas no chao em volta do jogador; depois do preparo cada uma atira a arma (0 = a arma sai do chefe)")]
    [Min(0)] public int marcas;

    [Min(0f)] public float raioDasMarcas = 3f;
}
