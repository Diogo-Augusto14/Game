using UnityEngine;

/// <summary>
/// Bolha lenta que persegue o jogador e, ao morrer, se parte em duas menores e mais
/// rapidas (que nao se partem de novo). A sala so abre quando os pedacos morrem tambem.
///
/// Nao persegue: QUICA pela sala na diagonal, batendo nas paredes e pedras e mudando de
/// rumo como bola de bilhar (com uma puxadinha pro lado do jogador a cada batida). Os
/// pedacos quicam mais rapido. O corpo "respira" o tempo todo.
/// </summary>
public class InimigoDivisor : InimigoPerseguidor
{
    [Header("Divisor")]
    [Tooltip("Desligado nos pedacos: eles morrem de vez")]
    [SerializeField] private bool seDivide = true;

    [SerializeField, Min(0)] private int pedacos = 2;

    private Vector3 escalaDoCorpo;
    private float fasePulso;

    /// <summary>A fabrica chama nos pedacos: menores, mais rapidos e sem se dividir.</summary>
    public void VirarPedaco(float novaVelocidade)
    {
        seDivide = false;
        velocidade = novaVelocidade;
    }

    protected override void Awake()
    {
        base.Awake();
        fasePulso = Random.value * Mathf.PI * 2f;

        if (desenho != null)
            escalaDoCorpo = desenho.transform.localScale;
    }

    private Vector2 rumoDoQuique;

    protected override void AtualizarAgindo(float dt)
    {
        if (rumoDoQuique == Vector2.zero)
            rumoDoQuique = new Vector2(Random.value < 0.5f ? -1f : 1f, Random.value < 0.5f ? -1f : 1f).normalized;

        RaycastHit2D batida = Physics2D.CircleCast(rb.position, Raio * 0.9f, rumoDoQuique, 0.3f, Camadas.MascaraDeParede);

        if (batida.collider != null)
        {
            // Rebate e puxa um pouco pro jogador, pra nao ficar quicando longe dele pra sempre.
            Vector2 rebatido = Vector2.Reflect(rumoDoQuique, batida.normal);
            Vector2 paraEle = ParaOJogador().sqrMagnitude > 0.01f ? ParaOJogador().normalized : rebatido;
            rumoDoQuique = (rebatido + paraEle * 0.35f).normalized;

            if (Vector2.Dot(rumoDoQuique, batida.normal) < 0.2f)
                rumoDoQuique = rebatido;
        }

        rb.linearVelocity = rumoDoQuique * velocidade * MultiplicadorDeVelocidade;

        // Respira: estica num eixo e encolhe no outro.
        if (desenho != null)
        {
            float s = Mathf.Sin(Time.time * 5f + fasePulso) * 0.08f;
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
