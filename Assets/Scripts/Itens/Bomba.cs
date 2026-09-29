using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bomba acesa no chao. Pisca cada vez mais rapido e explode: machuca tudo que tem
/// <see cref="Vida"/> dentro do raio, jogador incluido (com dano menor), e empurra pra fora.
/// Solta pelo <see cref="Inventario"/> (tecla E).
/// </summary>
[DisallowMultipleComponent]
public class Bomba : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float pavio = 1.5f;
    [SerializeField, Min(0.1f)] private float raio = 1.6f;
    [SerializeField, Min(0f)] private float dano = 60f;

    [Tooltip("Dano em quem tem a tag Player. O Isaac perde um coracao inteiro")]
    [SerializeField, Min(0f)] private float danoNoJogador = 20f;

    [SerializeField, Min(0f)] private float empurrao = 8f;

    [SerializeField] private string tagDoJogador = "Player";

    private GameObject dono;
    private SpriteRenderer desenho;
    private bool explodiu;

    public static Bomba Criar(Vector2 posicao, GameObject quemSoltou)
    {
        GameObject obj = new GameObject("Bomba acesa");
        obj.transform.position = posicao;
        obj.transform.localScale = Vector3.one * 0.6f;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = ArteGerada.Coletavel(TipoDeColetavel.Bomba);
        sr.color = Color.white;
        sr.sortingOrder = 4;

        Bomba bomba = obj.AddComponent<Bomba>();
        bomba.dono = quemSoltou;
        return bomba;
    }

    private void Awake()
    {
        desenho = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        StartCoroutine(Queimar());
    }

    private IEnumerator Queimar()
    {
        Color escura = desenho.color;
        Color acesa = new Color(1f, 0.45f, 0.3f);

        for (float t = 0f; t < pavio; t += Time.deltaTime)
        {
            // Pisca mais rapido perto de explodir.
            float ritmo = Mathf.Lerp(3f, 14f, t / pavio);
            desenho.color = Mathf.Repeat(t * ritmo, 1f) < 0.5f ? escura : acesa;
            yield return null;
        }

        Explodir();
    }

    public void Explodir()
    {
        if (explodiu)
            return;

        explodiu = true;
        Vector2 centro = transform.position;
        Sons.Tocar(Som.Explosao);

        // Cada Vida uma vez so, mesmo que tenha varios colisores.
        HashSet<Vida> atingidos = new HashSet<Vida>();

        foreach (Collider2D c in Physics2D.OverlapCircleAll(centro, raio))
        {
            // Porta secreta escondida na parede: a explosao abre.
            Porta porta = c.GetComponentInParent<Porta>();

            if (porta != null && porta.Escondida)
                porta.Revelar();

            Vida vida = c.GetComponentInParent<Vida>();

            if (vida == null || !atingidos.Add(vida))
                continue;

            Vector2 direcao = (Vector2)vida.transform.position - centro;

            if (direcao.sqrMagnitude < 0.0001f)
                direcao = Vector2.up;

            float quanto = vida.CompareTag(tagDoJogador) ? danoNoJogador : dano;
            vida.TomarDano(new DanoInfo(quanto, direcao.normalized, empurrao, centro, dono));
        }

        StopAllCoroutines();
        StartCoroutine(Clarao());
    }

    private IEnumerator Clarao()
    {
        // O proprio desenho vira o clarao: cresce ate o raio e some.
        const float DURACAO = 0.25f;
        Color cor = new Color(1f, 0.75f, 0.3f, 0.8f);
        Vector3 inicio = transform.localScale;
        Vector3 fim = Vector3.one * raio * 2f;
        desenho.sortingOrder = 30;
        desenho.sprite = FormasTopDown.Circulo();

        for (float t = 0f; t < DURACAO; t += Time.deltaTime)
        {
            float k = t / DURACAO;
            transform.localScale = Vector3.Lerp(inicio, fim, Mathf.Sqrt(k));
            desenho.color = new Color(cor.r, cor.g, cor.b, cor.a * (1f - k));
            yield return null;
        }

        Destroy(gameObject);
    }
}
