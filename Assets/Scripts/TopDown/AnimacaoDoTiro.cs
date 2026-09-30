using UnityEngine;

/// <summary>
/// Anima o tiro enquanto ele voa: repete os quadros (a onda de corte tremula) e, logo que
/// sai, cresce de 70% ate o tamanho cheio, como um golpe que se abre. Quando a
/// <see cref="Lagrima"/> estoura (desliga o colisor), para e deixa o estouro dela agir.
///
/// O <see cref="AtiradorTopDown"/> poe no tiro quando o heroi tem quadros de tiro.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class AnimacaoDoTiro : MonoBehaviour
{
    /// <summary>Segundos pra onda abrir ate o tamanho cheio.</summary>
    private const float TempoPraAbrir = 0.12f;

    private const float TamanhoAoSair = 0.7f;

    private Sprite[] quadros;
    private float quadrosPorSegundo;
    private float inicio;
    private Vector3 escalaCheia;
    private SpriteRenderer desenho;
    private Collider2D corpo;

    public void Configurar(Sprite[] novosQuadros, float novosQuadrosPorSegundo)
    {
        quadros = novosQuadros;
        quadrosPorSegundo = Mathf.Max(1f, novosQuadrosPorSegundo);
        inicio = Time.time;
        desenho = GetComponent<SpriteRenderer>();
        corpo = GetComponent<Collider2D>();
        escalaCheia = transform.localScale;

        desenho.sprite = quadros[0];
        transform.localScale = escalaCheia * TamanhoAoSair;
    }

    private void Update()
    {
        if (quadros == null)
            return;

        // Estourou: a Lagrima encolhe a partir daqui.
        if (corpo != null && !corpo.enabled)
        {
            enabled = false;
            return;
        }

        float tempo = Time.time - inicio;
        desenho.sprite = quadros[Mathf.FloorToInt(tempo * quadrosPorSegundo) % quadros.Length];

        float t = Mathf.Clamp01(tempo / TempoPraAbrir);
        transform.localScale = escalaCheia * Mathf.Lerp(TamanhoAoSair, 1f, 1f - (1f - t) * (1f - t));
    }
}
