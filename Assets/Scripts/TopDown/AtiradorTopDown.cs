using UnityEngine;

/// <summary>
/// O tiro do Isaac: segurou uma seta, sai lagrima naquela direcao, no ritmo da cadencia.
/// A direcao do tiro nao depende de pra onde o boneco anda — da pra fugir pra um lado
/// atirando pro outro. So as quatro direcoes retas, como no jogo original.
///
/// Os tres numeros que definem a "arma" ficam aqui: dano, alcance e cadencia. Os itens
/// mexem neles por <see cref="Configurar"/> e <see cref="ConfigurarLagrima"/>.
///
/// Precisa de <see cref="Entrada"/> no mesmo objeto. Se tiver <see cref="MovimentoTopDown"/>,
/// a lagrima herda um pouco da velocidade do boneco.
/// </summary>
[DisallowMultipleComponent]
public class AtiradorTopDown : MonoBehaviour
{
    [Header("Arma")]
    [Tooltip("Dano de cada lagrima (o Isaac comeca com 3.5)")]
    [SerializeField, Min(0f)] private float dano = 3.5f;

    [Tooltip("Distancia que a lagrima voa antes de cair, em unidades")]
    [SerializeField, Min(0.1f)] private float alcance = 6.5f;

    [Tooltip("Lagrimas por segundo segurando a seta")]
    [SerializeField, Min(0.1f)] private float tirosPorSegundo = 2.7f;

    [Tooltip("Velocidade da lagrima, em unidades por segundo")]
    [SerializeField, Min(0.1f)] private float velocidadeDoTiro = 9f;

    [Tooltip("Quanto da velocidade do boneco a lagrima herda (0 = nada, 1 = tudo)")]
    [SerializeField, Range(0f, 1f)] private float herancaDaVelocidade = 0.35f;

    [Tooltip("Lagrimas por disparo. Mais de uma sai em leque (itens tipo olho triplo)")]
    [SerializeField, Min(1)] private int lagrimasPorDisparo = 1;

    [Tooltip("Graus entre uma lagrima e a vizinha quando sai mais de uma")]
    [SerializeField, Range(0f, 45f)] private float aberturaDoLeque = 10f;

    [Tooltip("Empurrao que a lagrima da em quem acerta")]
    [SerializeField, Min(0f)] private float forcaEmpurrao = 2.5f;

    [Header("Aparencia")]
    [Tooltip("Diametro da lagrima, em unidades")]
    [SerializeField, Min(0.05f)] private float tamanho = 0.28f;

    [SerializeField] private Color cor = new Color(0.55f, 0.8f, 1f);

    [Tooltip("Sprite da lagrima. Vazio = um circulo gerado por codigo")]
    [SerializeField] private Sprite sprite;

    [Tooltip("Distancia do centro do boneco de onde a lagrima nasce")]
    [SerializeField, Min(0f)] private float distanciaDoCorpo = 0.3f;

    [Tooltip("Alterna olho esquerdo/direito a cada tiro, como o Isaac")]
    [SerializeField, Min(0f)] private float afastamentoDosOlhos = 0.1f;

    [Tooltip("Filho que mostra pra onde o boneco olha (opcional)")]
    [SerializeField] private Transform olho;

    [SerializeField, Min(0f)] private float distanciaDoOlho = 0.18f;

    // ---------------- efeitos de item ----------------
    private bool atravessa;
    private bool teleguiada;
    private bool paraTras;
    private Color corOriginal;
    private bool guardouCor;

    // ---------------- estado ----------------
    private Entrada entrada;
    private MovimentoTopDown movimento;
    private Cronometro recarga;
    private bool olhoDireito;
    private Vector2 olhando = Vector2.down;

    public float Dano => dano;

    public float Alcance => alcance;

    public float TirosPorSegundo => tirosPorSegundo;

    public float VelocidadeDoTiro => velocidadeDoTiro;

    public float Tamanho => tamanho;

    public int LagrimasPorDisparo => lagrimasPorDisparo;

    /// <summary>Pra onde o boneco olha: o tiro manda, senao o andar.</summary>
    public Vector2 Olhando => olhando;

    // ---------------- ciclo de vida ----------------
    private void Awake()
    {
        entrada = GetComponent<Entrada>();
        movimento = GetComponent<MovimentoTopDown>();

        if (entrada == null)
            Debug.LogWarning("[AtiradorTopDown] sem Entrada no objeto — o boneco nao vai atirar.", this);

        if (sprite == null)
            sprite = ArteGerada.Bola();
    }

    private void Update()
    {
        recarga.Contar(Time.deltaTime);

        if (entrada == null)
            return;

        if (entrada.Atirando)
            olhando = entrada.Tiro;
        else if (entrada.Andar != Vector2.zero)
            olhando = entrada.Andar;

        if (olho != null)
            olho.localPosition = olhando * distanciaDoOlho;

        if (entrada.Atirando && !recarga.Ativo)
        {
            Atirar(entrada.Tiro);
            recarga.Forcar(1f / tirosPorSegundo);
        }
    }

    // ---------------- tiro ----------------
    /// <summary>
    /// Solta as lagrimas de um disparo na direcao pedida (uma so, ou um leque se algum item
    /// deu lagrimas extras). Devolve a do meio. Publico pra dar pra testar/roteirizar.
    /// </summary>
    public Lagrima Atirar(Vector2 direcao)
    {
        direcao = direcao.sqrMagnitude > 0.0001f ? direcao.normalized : Vector2.down;

        // Olho esquerdo, olho direito: desloca um pouquinho pro lado da direcao do tiro.
        Vector2 lado = new Vector2(-direcao.y, direcao.x) * (olhoDireito ? afastamentoDosOlhos : -afastamentoDosOlhos);
        olhoDireito = !olhoDireito;

        Vector2 origem = (Vector2)transform.position + direcao * distanciaDoCorpo + lado;
        Vector2 heranca = movimento != null ? movimento.Velocidade * herancaDaVelocidade : Vector2.zero;

        Sons.Tocar(Som.Tiro, 0.55f);

        Lagrima doMeio = null;
        float primeiroAngulo = -aberturaDoLeque * (lagrimasPorDisparo - 1) * 0.5f;

        for (int i = 0; i < lagrimasPorDisparo; i++)
        {
            Vector2 rumo = Quaternion.Euler(0f, 0f, primeiroAngulo + aberturaDoLeque * i) * direcao;
            Lagrima lagrima = Soltar(origem, rumo * velocidadeDoTiro + heranca);

            if (i == lagrimasPorDisparo / 2)
                doMeio = lagrima;
        }

        // Olho na Nuca: uma lagrima pra tras, do outro lado do corpo.
        if (paraTras)
            Soltar((Vector2)transform.position - direcao * distanciaDoCorpo, -direcao * velocidadeDoTiro + heranca);

        return doMeio;
    }

    private Lagrima Soltar(Vector2 origem, Vector2 velocidade)
    {
        Lagrima lagrima = CriarLagrima(origem);
        lagrima.Disparar(gameObject, velocidade, dano, alcance, forcaEmpurrao);
        lagrima.DefinirEfeitos(atravessa, teleguiada);
        return lagrima;
    }

    /// <summary>
    /// Liga os efeitos especiais que os itens dao a lagrima. <paramref name="novaCor"/>
    /// null = volta a cor original.
    /// </summary>
    public void DefinirEfeitos(bool lagrimaAtravessa, bool lagrimaTeleguiada, bool tambemPraTras, Color? novaCor)
    {
        if (!guardouCor)
        {
            corOriginal = cor;
            guardouCor = true;
        }

        atravessa = lagrimaAtravessa;
        teleguiada = lagrimaTeleguiada;
        paraTras = tambemPraTras;
        cor = novaCor ?? corOriginal;
    }

    private Lagrima CriarLagrima(Vector2 posicao)
    {
        GameObject obj = new GameObject("Lagrima");
        obj.transform.position = posicao;
        obj.transform.localScale = Vector3.one * tamanho;

        SpriteRenderer desenho = obj.AddComponent<SpriteRenderer>();
        desenho.sprite = sprite;
        desenho.color = cor;
        desenho.sortingOrder = 20;

        // O circulo gerado tem 1 unidade de diametro: raio 0.5 bate com o desenho.
        CircleCollider2D colisor = obj.AddComponent<CircleCollider2D>();
        colisor.radius = 0.5f;

        obj.AddComponent<Rigidbody2D>();

        return obj.AddComponent<Lagrima>();
    }

    /// <summary>Troca os numeros da arma (itens, power-ups). Valores fora do limite sao ajustados.</summary>
    public void Configurar(float novoDano, float novoAlcance, float novaCadencia)
    {
        dano = Mathf.Max(0f, novoDano);
        alcance = Mathf.Max(0.1f, novoAlcance);
        tirosPorSegundo = Mathf.Max(0.1f, novaCadencia);
    }

    /// <summary>Troca como a lagrima sai (itens). Valores fora do limite sao ajustados.</summary>
    public void ConfigurarLagrima(float novaVelocidade, float novoTamanho, int quantasPorDisparo)
    {
        velocidadeDoTiro = Mathf.Max(0.1f, novaVelocidade);
        tamanho = Mathf.Max(0.05f, novoTamanho);
        lagrimasPorDisparo = Mathf.Max(1, quantasPorDisparo);
    }

    /// <summary>Aponta o filho que mostra a direcao do olhar.</summary>
    public void DefinirOlho(Transform alvo)
    {
        olho = alvo;
    }
}
