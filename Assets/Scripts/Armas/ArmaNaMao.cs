using UnityEngine;

/// <summary>
/// Desenha a arma que esta na mao, girando pra onde o jogador mira, e da um coice a cada tiro. Vale
/// pra todas, a do heroi tambem (o boneco do heroi nao tem mais a arma desenhada; ver
/// Ferramentas/Personagens/desarmar_herois.py). Some na esquiva e quando o jogador morre. As laminas
/// (<see cref="DadosDaArma.golpeNaMao"/>) balancam a cada golpe, um pra cada lado.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ArmaDoJogador))]
public class ArmaNaMao : MonoBehaviour
{
    [Tooltip("Altura da mao em relacao ao centro do corpo")]
    [SerializeField] private float alturaDaMao = -0.1f;

    [Tooltip("Segundos que o coice leva pra voltar")]
    [SerializeField, Min(0.01f)] private float voltaDoCoice = 0.08f;

    private ArmaDoJogador armas;
    private ControlesDoJogador controles;
    private MovimentoDoJogador movimento;
    private Vida vida;
    private SpriteRenderer desenho;
    private float coice;
    private float golpeComecou = -10f;
    private float ladoDoGolpe = 1f;

    private const float DuracaoDoGolpe = 0.16f;

    private void Awake()
    {
        armas = GetComponent<ArmaDoJogador>();
        controles = GetComponent<ControlesDoJogador>();
        movimento = GetComponent<MovimentoDoJogador>();
        vida = GetComponent<Vida>();

        GameObject obj = new GameObject("Arma na mao");
        obj.transform.SetParent(transform, false);
        desenho = obj.AddComponent<SpriteRenderer>();
        desenho.enabled = false;
    }

    private void OnEnable()
    {
        armas.AoAtirar += Atirou;
    }

    private void OnDisable()
    {
        armas.AoAtirar -= Atirou;
    }

    private void Atirou(Vector2 rumo, float intervalo)
    {
        coice = armas.Arma != null ? armas.Arma.coice : 0f;

        if (armas.Arma != null && armas.Arma.golpeNaMao > 0f)
        {
            golpeComecou = Time.time;
            ladoDoGolpe = -ladoDoGolpe;
        }
    }

    // O balanco da lamina: corta de um lado pro outro rapido e volta pro meio.
    private float Golpe(DadosDaArma dados)
    {
        float p = (Time.time - golpeComecou) / DuracaoDoGolpe;

        if (dados.golpeNaMao <= 0f || p >= 1f)
            return 0f;

        float angulo = p < 0.7f ? Mathf.Lerp(1f, -1f, p / 0.7f) : Mathf.Lerp(-1f, 0f, (p - 0.7f) / 0.3f);
        return angulo * dados.golpeNaMao * ladoDoGolpe;
    }

    private void LateUpdate()
    {
        DadosDaArma dados = armas.Arma;
        Sprite desenhoDaArma = dados != null ? dados.Desenho : null;
        bool mostrar = desenhoDaArma != null && controles != null
                    && (movimento == null || !movimento.Esquivando) && (vida == null || !vida.Morto);
        desenho.enabled = mostrar;

        if (!mostrar)
            return;

        Vector2 mira = controles.Mira;
        coice = Mathf.MoveTowards(coice, 0f, Time.deltaTime * Mathf.Max(0.01f, dados.coice) / voltaDoCoice);

        desenho.sprite = desenhoDaArma;
        desenho.flipY = mira.x < 0f;

        // O desenho de chao das armas dos herois e na diagonal: o giro acerta (espelhado, ao contrario).
        float giro = (desenho.flipY ? -dados.giroNaMao : dados.giroNaMao) + Golpe(dados);
        // Mirando pra cima a arma fica atras do corpo; pra baixo e pros lados, na frente.
        desenho.sortingOrder = mira.y > 0.5f ? 9 : 11;
        desenho.transform.localPosition = (Vector3)(mira * (dados.distanciaDaMao - coice)) + new Vector3(0f, alturaDaMao, 0f);
        desenho.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(mira.y, mira.x) * Mathf.Rad2Deg + giro);
    }
}
