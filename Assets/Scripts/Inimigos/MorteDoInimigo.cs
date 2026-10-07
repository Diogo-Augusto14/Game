using System.Collections;
using UnityEngine;

/// <summary>
/// Quando a vida do inimigo acaba: ele para, deixa de bater nas coisas (tiro e gente passam por ele),
/// toca a animacao de morte (a <see cref="AnimacaoDoInimigo"/> cuida disso) e no fim some numa
/// nuvenzinha de poeira.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Vida))]
public class MorteDoInimigo : MonoBehaviour
{
    [Tooltip("Segundos caido antes de sumir (deixa a animacao de morte tocar)")]
    [SerializeField, Min(0f)] private float espera = 0.9f;

    [Tooltip("Segundos desbotando ate sumir de vez")]
    [SerializeField, Min(0.01f)] private float desbotar = 0.2f;

    [Header("Efeito ao sumir")]
    [Tooltip("Folha do efeito (a poeira). Vazio = some sem efeito")]
    [SerializeField] private Texture2D efeito;

    [SerializeField] private Vector2Int tamanhoDoQuadroDoEfeito = new Vector2Int(64, 64);

    [SerializeField, Min(1f)] private float pixelsPorUnidadeDoEfeito = 20f;

    [SerializeField, Min(1f)] private float quadrosPorSegundoDoEfeito = 20f;

    [Tooltip("Onde o efeito aparece, em relacao ao centro do corpo")]
    [SerializeField] private Vector2 alturaDoEfeito = new Vector2(0f, -0.2f);

    private Vida vida;

    private void Awake()
    {
        vida = GetComponent<Vida>();
    }

    private void OnEnable()
    {
        vida.AoMorrer += Morreu;
    }

    private void OnDisable()
    {
        vida.AoMorrer -= Morreu;
    }

    private void Morreu()
    {
        if (TryGetComponent(out Rigidbody2D corpo))
        {
            corpo.linearVelocity = Vector2.zero;
            corpo.simulated = false;
        }

        foreach (Collider2D colisor in GetComponentsInChildren<Collider2D>())
            colisor.enabled = false;

        StartCoroutine(Sumir());
    }

    private IEnumerator Sumir()
    {
        yield return new WaitForSeconds(espera);

        if (efeito != null)
        {
            EfeitoDeFolha.Tocar(efeito, tamanhoDoQuadroDoEfeito, pixelsPorUnidadeDoEfeito,
                                (Vector2)transform.position + alturaDoEfeito, quadrosPorSegundoDoEfeito);
        }

        SpriteRenderer[] desenhos = GetComponentsInChildren<SpriteRenderer>();
        Color[] cores = new Color[desenhos.Length];

        for (int i = 0; i < desenhos.Length; i++)
            cores[i] = desenhos[i].color;

        for (float t = 0f; t < desbotar; t += Time.deltaTime)
        {
            for (int i = 0; i < desenhos.Length; i++)
            {
                Color c = cores[i];
                c.a *= 1f - t / desbotar;
                desenhos[i].color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
