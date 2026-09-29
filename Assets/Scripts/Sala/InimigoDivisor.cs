using UnityEngine;

/// <summary>
/// Bolha lenta que persegue o jogador e, ao morrer, se parte em duas menores e mais
/// rapidas (que nao se partem de novo). A sala so abre quando os pedacos morrem tambem.
///
/// Anda igual ao <see cref="InimigoPerseguidor"/>; a diferenca e so a morte e o
/// "respirar" do corpo, que deixa claro que ela nao e um perseguidor comum.
/// </summary>
public class InimigoDivisor : InimigoPerseguidor
{
    [Header("Divisor")]
    [Tooltip("Desligado nos pedacos: eles morrem de vez")]
    [SerializeField] private bool seDivide = true;

    [SerializeField, Min(0)] private int pedacos = 2;

    private Vector3 escalaDoCorpo;
    private float fase;

    /// <summary>A fabrica chama nos pedacos: menores, mais rapidos e sem se dividir.</summary>
    public void VirarPedaco(float novaVelocidade)
    {
        seDivide = false;
        velocidade = novaVelocidade;
    }

    protected override void Awake()
    {
        base.Awake();
        fase = Random.value * Mathf.PI * 2f;

        if (desenho != null)
            escalaDoCorpo = desenho.transform.localScale;
    }

    protected override void AtualizarAgindo(float dt)
    {
        base.AtualizarAgindo(dt);

        // Respira: estica num eixo e encolhe no outro.
        if (desenho != null)
        {
            float s = Mathf.Sin(Time.time * 5f + fase) * 0.08f;
            desenho.transform.localScale = new Vector3(escalaDoCorpo.x * (1f + s), escalaDoCorpo.y * (1f - s), 1f);
        }
    }

    protected override void Morrer()
    {
        if (desenho != null)
            desenho.transform.localScale = escalaDoCorpo;

        // Os pedacos nascem ANTES da sala contar esta morte (o evento de morte chama este
        // metodo primeiro), entao a sala nunca abre no meio da divisao.
        Sala sala = seDivide ? GetComponentInParent<Sala>() : null;

        if (sala != null)
        {
            Vector2 ponto = rb.position - (Vector2)sala.transform.position;
            Vector2 lado = Random.insideUnitCircle.normalized;

            for (int i = 0; i < pedacos; i++)
            {
                Vector2 desvio = Quaternion.Euler(0f, 0f, i * 360f / pedacos) * lado * Raio * 0.8f;
                sala.CriarInimigo(TipoDeInimigo.DivisorPequeno, ponto + desvio);
            }
        }

        base.Morrer();
    }
}
