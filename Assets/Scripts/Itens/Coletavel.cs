using UnityEngine;

public enum TipoDeColetavel
{
    Coracao,
    Moeda,
    Chave,
    Bomba
}

/// <summary>
/// Coisinha no chao que o jogador pega encostando: coracao, moeda, chave ou bomba.
/// Coracao so e pego se o jogador estiver machucado, como no Isaac.
///
/// Nasce com um pulinho (escala) pra chamar atencao e depois fica flutuando no lugar, subindo
/// e descendo sobre uma sombra, com um brilho na cor dele atras. Monte por <see cref="Criar"/>.
/// </summary>
[DisallowMultipleComponent]
public class Coletavel : MonoBehaviour
{
    [SerializeField] private TipoDeColetavel tipo;

    [Tooltip("Quanto um coracao cura")]
    [SerializeField, Min(0f)] private float cura = 20f;

    [Tooltip("Segundos sem poder pegar depois de nascer (da tempo de ver o que caiu)")]
    [SerializeField, Min(0f)] private float atrasoParaPegar = 0.3f;

    [SerializeField] private string tagDoJogador = "Player";

    private float nasceu;
    private Vector3 escalaFinal;
    private Transform flutuante;
    private SpriteRenderer brilho;
    private Transform sombra;
    private float fase;

    /// <summary>O desenho (num filho que sobe e desce; o colisor fica parado na raiz).</summary>
    public SpriteRenderer Desenho { get; private set; }

    public TipoDeColetavel Tipo => tipo;

    public static Color CorDe(TipoDeColetavel tipo)
    {
        switch (tipo)
        {
            case TipoDeColetavel.Coracao: return new Color(0.95f, 0.2f, 0.3f);
            case TipoDeColetavel.Moeda: return new Color(1f, 0.82f, 0.2f);
            case TipoDeColetavel.Chave: return new Color(0.8f, 0.8f, 0.85f);
            default: return new Color(0.2f, 0.2f, 0.25f);
        }
    }

    /// <summary>Tamanho do desenho no mundo, em unidades.</summary>
    /// <remarks>Os icones do Raven Fantasy enchem o quadro de 32px: saem menores que os antigos do ladrilho.</remarks>
    public static float TamanhoDe(TipoDeColetavel tipo)
        => tipo == TipoDeColetavel.Moeda ? 0.55f : 0.62f;

    /// <summary>A cor do brilho atras do coletavel. A bomba, que e escura, brilha laranja como o pavio.</summary>
    private static Color BrilhoDe(TipoDeColetavel tipo)
    {
        switch (tipo)
        {
            case TipoDeColetavel.Coracao: return new Color(1f, 0.3f, 0.35f, 0.45f);
            case TipoDeColetavel.Moeda: return new Color(1f, 0.85f, 0.3f, 0.45f);
            case TipoDeColetavel.Chave: return new Color(1f, 0.95f, 0.6f, 0.45f);
            default: return new Color(1f, 0.6f, 0.25f, 0.5f);
        }
    }

    /// <summary>Poe um coletavel no mundo, na posicao dada. <paramref name="pai"/> costuma ser a sala.</summary>
    public static Coletavel Criar(TipoDeColetavel tipo, Vector2 posicao, Transform pai = null)
    {
        GameObject obj = new GameObject(tipo.ToString());
        obj.transform.SetParent(pai, true);
        obj.transform.position = posicao;

        obj.transform.localScale = Vector3.one * TamanhoDe(tipo);

        // Sombra no chao, brilho atras e o desenho num filho que flutua (o colisor nao mexe).
        Transform sombra = FormasDaSala.Desenho(obj.transform, "Sombra", HaloCintilante.Suave(), new Color(0f, 0f, 0f, 0.55f),
                                                new Vector2(0f, -0.5f), new Vector2(0.9f, 0.35f), 4).transform;
        SpriteRenderer brilho = FormasDaSala.Desenho(obj.transform, "Brilho", HaloCintilante.Suave(), BrilhoDe(tipo),
                                                     Vector2.zero, Vector2.one * 1.9f, 4);

        // Pixel art de 1 unidade (ArteGerada), ja colorida.
        SpriteRenderer sr = FormasDaSala.Desenho(obj.transform, "Desenho", ArteGerada.Coletavel(tipo), Color.white,
                                                 Vector2.zero, Vector2.one, 5);

        // O desenho tem 1 unidade: o colisor na escala local cobre o desenho.
        CircleCollider2D colisor = obj.AddComponent<CircleCollider2D>();
        colisor.isTrigger = true;
        colisor.radius = 0.5f;

        Coletavel c = obj.AddComponent<Coletavel>();
        c.tipo = tipo;
        c.Desenho = sr;
        c.flutuante = sr.transform;
        c.brilho = brilho;
        c.sombra = sombra;
        return c;
    }

    private void Awake()
    {
        nasceu = Time.time;
        escalaFinal = transform.localScale;
        fase = Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        // Pulinho de nascer: cresce um pouco alem do tamanho e volta.
        float t = (Time.time - nasceu) / 0.25f;

        if (t <= 1f)
            transform.localScale = escalaFinal * (1f + Mathf.Sin(t * Mathf.PI) * 0.4f);
        else if (transform.localScale != escalaFinal)
            transform.localScale = escalaFinal;

        // Flutua: sobe e desce devagar; a sombra encolhe quando ele sobe e o brilho pulsa junto.
        float onda = Mathf.Sin(Time.time * 3.2f + fase);
        float altura = 0.1f + onda * 0.1f;

        if (flutuante != null)
            flutuante.localPosition = new Vector3(0f, altura, 0f);

        if (sombra != null)
            sombra.localScale = new Vector3(0.9f, 0.35f, 1f) * (1f - altura * 0.8f);

        if (brilho != null)
        {
            Color c = BrilhoDe(tipo);
            c.a *= 0.75f + 0.25f * onda;
            brilho.color = c;
            brilho.transform.localPosition = new Vector3(0f, altura, 0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D outro) => TentarPegar(outro);

    // Stay tambem: coracao com vida cheia, ou coletavel que nasceu embaixo do jogador,
    // tem que dar pra pegar sem sair e voltar.
    private void OnTriggerStay2D(Collider2D outro) => TentarPegar(outro);

    private void TentarPegar(Collider2D outro)
    {
        if (Time.time - nasceu < atrasoParaPegar)
            return;

        GameObject quem = outro.attachedRigidbody != null ? outro.attachedRigidbody.gameObject : outro.gameObject;

        if (!quem.CompareTag(tagDoJogador))
            return;

        if (tipo == TipoDeColetavel.Coracao)
        {
            Vida vida = quem.GetComponent<Vida>();

            if (vida == null || vida.EstaMorto || vida.VidaAtual >= vida.VidaMaxima)
                return;

            vida.Curar(cura);
            Sons.Tocar(Som.Coracao);
        }
        else
        {
            Inventario inventario = quem.GetComponent<Inventario>();

            if (inventario == null || !inventario.CabeMais(tipo))
                return;

            inventario.Adicionar(tipo, 1);
            Sons.Tocar(tipo == TipoDeColetavel.Moeda ? Som.Moeda : Som.Chave);
        }

        Destroy(gameObject);
    }
}
