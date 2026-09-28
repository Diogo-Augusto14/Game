using UnityEngine;

/// <summary>
/// O tiro do Isaac: segurou uma seta, sai lagrima naquela direcao, no ritmo da cadencia.
/// A direcao do tiro nao depende de pra onde o boneco anda — da pra fugir pra um lado
/// atirando pro outro. So as quatro direcoes retas, como no jogo original.
///
/// Os tres numeros que definem a "arma" ficam aqui: dano, alcance e cadencia. Itens
/// no futuro so precisam mexer neles (via <see cref="Configurar"/>).
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

    // ---------------- estado ----------------
    private Entrada entrada;
    private MovimentoTopDown movimento;
    private Cronometro recarga;
    private bool olhoDireito;
    private Vector2 olhando = Vector2.down;

    public float Dano => dano;

    public float Alcance => alcance;

    public float TirosPorSegundo => tirosPorSegundo;

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
            sprite = FormasTopDown.Circulo();
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
    /// <summary>Solta uma lagrima na direcao pedida. Publico pra dar pra testar/roteirizar.</summary>
    public Lagrima Atirar(Vector2 direcao)
    {
        direcao = direcao.sqrMagnitude > 0.0001f ? direcao.normalized : Vector2.down;

        // Olho esquerdo, olho direito: desloca um pouquinho pro lado da direcao do tiro.
        Vector2 lado = new Vector2(-direcao.y, direcao.x) * (olhoDireito ? afastamentoDosOlhos : -afastamentoDosOlhos);
        olhoDireito = !olhoDireito;

        Vector2 origem = (Vector2)transform.position + direcao * distanciaDoCorpo + lado;

        Vector2 velocidade = direcao * velocidadeDoTiro;

        if (movimento != null)
            velocidade += movimento.Velocidade * herancaDaVelocidade;

        Lagrima lagrima = CriarLagrima(origem);
        lagrima.Disparar(gameObject, velocidade, dano, alcance, forcaEmpurrao);

        return lagrima;
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

    /// <summary>Aponta o filho que mostra a direcao do olhar.</summary>
    public void DefinirOlho(Transform alvo)
    {
        olho = alvo;
    }
}
