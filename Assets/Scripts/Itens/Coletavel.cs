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
/// Nasce com um pulinho (escala) pra chamar atencao. Monte por <see cref="Criar"/>.
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

    /// <summary>Poe um coletavel no mundo, na posicao dada. <paramref name="pai"/> costuma ser a sala.</summary>
    public static Coletavel Criar(TipoDeColetavel tipo, Vector2 posicao, Transform pai = null)
    {
        GameObject obj = new GameObject(tipo.ToString());
        obj.transform.SetParent(pai, true);
        obj.transform.position = posicao;

        // Formas simples ate ter arte: coracao e moeda redondos, chave comprida, bomba
        // redonda escura com um pavio.
        bool comprido = tipo == TipoDeColetavel.Chave;
        Vector2 tamanho = comprido ? new Vector2(0.22f, 0.45f) : Vector2.one * (tipo == TipoDeColetavel.Moeda ? 0.3f : 0.4f);
        Sprite forma = comprido ? FormasTopDown.Quadrado() : FormasTopDown.Circulo();

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = forma;
        sr.color = CorDe(tipo);
        sr.sortingOrder = 5;
        obj.transform.localScale = new Vector3(tamanho.x, tamanho.y, 1f);

        if (tipo == TipoDeColetavel.Bomba)
        {
            GameObject pavio = new GameObject("Pavio");
            pavio.transform.SetParent(obj.transform, false);
            pavio.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            pavio.transform.localScale = new Vector3(0.2f, 0.3f, 1f);
            SpriteRenderer p = pavio.AddComponent<SpriteRenderer>();
            p.sprite = FormasTopDown.Quadrado();
            p.color = new Color(1f, 0.6f, 0.2f);
            p.sortingOrder = 6;
        }

        // Circulo/quadrado gerado tem 1 unidade: o colisor na escala local cobre o desenho.
        CircleCollider2D colisor = obj.AddComponent<CircleCollider2D>();
        colisor.isTrigger = true;
        colisor.radius = comprido ? 0.7f : 0.6f;

        Coletavel c = obj.AddComponent<Coletavel>();
        c.tipo = tipo;
        return c;
    }

    private void Awake()
    {
        nasceu = Time.time;
        escalaFinal = transform.localScale;
    }

    private void Update()
    {
        // Pulinho de nascer: cresce um pouco alem do tamanho e volta.
        float t = (Time.time - nasceu) / 0.25f;

        if (t <= 1f)
            transform.localScale = escalaFinal * (1f + Mathf.Sin(t * Mathf.PI) * 0.4f);
        else if (transform.localScale != escalaFinal)
            transform.localScale = escalaFinal;
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
