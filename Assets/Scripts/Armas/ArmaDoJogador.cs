using UnityEngine;

/// <summary>
/// Atira com a arma do personagem pra onde ele mira, no ritmo dela. O arqueiro usa o proprio arco
/// (que ja esta no desenho dele): nada e desenhado na mao, so as flechas saem.
///
/// Nao atira no meio da esquiva. Cada disparo avisa <see cref="AoAtirar"/> (a animacao do arco escuta).
/// </summary>
[DisallowMultipleComponent]
public class ArmaDoJogador : MonoBehaviour
{
    [SerializeField] private DadosDaArma arma;

    [Tooltip("De onde o tiro sai: distancia do centro do corpo, na direcao da mira")]
    [SerializeField, Min(0f)] private float distanciaDaSaida = 0.45f;

    [Tooltip("Altura de onde o tiro sai, em relacao ao centro do corpo (o arco fica na altura do peito)")]
    [SerializeField] private float alturaDaSaida;

    private ControlesDoJogador controles;
    private MovimentoDoJogador movimento;
    private AudioSource audioSource;
    private float proximoTiro;
    private bool soltouOGatilho = true;

    public DadosDaArma Arma => arma;

    /// <summary>Saiu um disparo: a direcao e o intervalo ate o proximo (a animacao do tiro cabe nele).</summary>
    public event System.Action<Vector2, float> AoAtirar;

    /// <summary>Troca a arma (pegar outra, no futuro).</summary>
    public void Trocar(DadosDaArma nova) => arma = nova;

    private void Awake()
    {
        controles = GetComponent<ControlesDoJogador>();
        movimento = GetComponent<MovimentoDoJogador>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (arma == null || controles == null)
            return;

        if (!controles.Atirando)
        {
            soltouOGatilho = true;
            return;
        }

        if ((movimento != null && movimento.Esquivando) || Time.time < proximoTiro)
            return;

        if (!arma.automatica && !soltouOGatilho)
            return;

        Atirar(controles.Mira);
        soltouOGatilho = false;
    }

    private void Atirar(Vector2 rumo)
    {
        float intervalo = 1f / arma.tirosPorSegundo;
        proximoTiro = Time.time + intervalo;

        Vector2 origem = (Vector2)transform.position + Vector2.up * alturaDaSaida + rumo * distanciaDaSaida;
        float angulo = Mathf.Atan2(rumo.y, rumo.x) * Mathf.Rad2Deg;
        float primeiro = -arma.abertura * (arma.tirosPorDisparo - 1) * 0.5f;

        for (int i = 0; i < arma.tirosPorDisparo; i++)
        {
            float torto = Random.Range(-arma.dispersao, arma.dispersao);
            float a = (angulo + primeiro + arma.abertura * i + torto) * Mathf.Deg2Rad;
            Projetil.Disparar(origem, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), arma, gameObject);
        }

        CameraDoJogo.Tremer(arma.tremor, 0.06f);

        if (audioSource != null && arma.som != null)
        {
            audioSource.pitch = Random.Range(0.94f, 1.06f);
            audioSource.PlayOneShot(arma.som, arma.volume);
        }

        AoAtirar?.Invoke(rumo, intervalo);
    }
}
