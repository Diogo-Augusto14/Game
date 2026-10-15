using UnityEngine;

/// <summary>
/// Desenha a arma que esta na mao, girando pra onde o jogador mira, e da um coice a cada tiro. Vale
/// pra todas, a do heroi tambem (o boneco do heroi nao tem mais a arma desenhada; ver
/// Ferramentas/Personagens/desarmar_herois.py). Some na esquiva e quando o jogador morre. As laminas
/// (<see cref="DadosDaArma.golpeNaMao"/>) balancam a cada golpe, um pra cada lado: a lamina corre em arco
/// em volta do heroi, deixando um rastro claro atras (1.22.2).
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
    private float duracaoDoGolpe = 0.16f;
    private SpriteRenderer[] rastro;

    // Copias da lamina que vem atras dela no golpe, cada uma um pouco mais atrasada e mais fraca.
    private const int PedacosDoRastro = 3;
    private const float AtrasoDoRastro = 0.022f;

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

        rastro = new SpriteRenderer[PedacosDoRastro];

        for (int i = 0; i < PedacosDoRastro; i++)
        {
            GameObject pedaco = new GameObject("Rastro do golpe");
            pedaco.transform.SetParent(transform, false);
            rastro[i] = pedaco.AddComponent<SpriteRenderer>();
            rastro[i].enabled = false;
        }
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
            // Golpe rapido nas armas rapidas, mas sempre dando pra ver.
            duracaoDoGolpe = Mathf.Clamp(intervalo * 0.55f, 0.12f, 0.24f);
        }
    }

    // O balanco da lamina: corta de um lado pro outro rapido e volta pro meio. Com atraso, onde ela estava
    // um pouco antes (o rastro).
    private float Golpe(DadosDaArma dados, float atraso = 0f)
    {
        float p = (Time.time - atraso - golpeComecou) / duracaoDoGolpe;

        if (dados.golpeNaMao <= 0f || p < 0f || p >= 1f)
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

        foreach (SpriteRenderer pedaco in rastro)
            pedaco.enabled = false;

        if (!mostrar)
            return;

        Vector2 mira = controles.Mira;
        coice = Mathf.MoveTowards(coice, 0f, Time.deltaTime * Mathf.Max(0.01f, dados.coice) / voltaDoCoice);

        desenho.sprite = desenhoDaArma;
        desenho.flipY = mira.x < 0f;

        // O desenho de chao das armas dos herois e na diagonal: o giro acerta (espelhado, ao contrario).
        float giro = desenho.flipY ? -dados.giroNaMao : dados.giroNaMao;
        // Mirando pra cima a arma fica atras do corpo; pra baixo e pros lados, na frente.
        desenho.sortingOrder = mira.y > 0.5f ? 9 : 11;
        Posicionar(desenho.transform, mira, Golpe(dados), giro, dados.distanciaDaMao - coice);

        // O rastro so aparece na ida do golpe, que e a parte rapida.
        float ida = (Time.time - golpeComecou) / duracaoDoGolpe;

        if (dados.golpeNaMao <= 0f || ida >= 0.7f)
            return;

        for (int i = 0; i < rastro.Length; i++)
        {
            float atraso = (i + 1) * AtrasoDoRastro;

            if (Time.time - atraso < golpeComecou)
                break;

            SpriteRenderer pedaco = rastro[i];
            pedaco.enabled = true;
            pedaco.sprite = desenhoDaArma;
            pedaco.flipY = desenho.flipY;
            pedaco.sortingOrder = desenho.sortingOrder - 1;
            pedaco.color = new Color(1f, 0.95f, 0.85f, 0.45f / (i + 1));
            Posicionar(pedaco.transform, mira, Golpe(dados, atraso), giro, dados.distanciaDaMao);
        }
    }

    // A arma em volta da mao: o balanco gira o rumo, entao a lamina corre em arco em volta do heroi.
    private void Posicionar(Transform arma, Vector2 mira, float balanco, float giro, float distancia)
    {
        Vector2 rumo = Quaternion.Euler(0f, 0f, balanco) * mira;
        arma.localPosition = (Vector3)(rumo * distancia) + new Vector3(0f, alturaDaMao, 0f);
        arma.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(rumo.y, rumo.x) * Mathf.Rad2Deg + giro);
    }
}
