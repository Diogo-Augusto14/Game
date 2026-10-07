using UnityEngine;

/// <summary>
/// Desenha a arma que esta na mao (a pistola, a escopeta...), girando pra onde o jogador mira, e da
/// um coice a cada tiro. A arma do personagem (o arco do Arqueiro) ja esta no desenho dele: com ela,
/// nada aparece aqui. Some na esquiva e quando o jogador morre.
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
    }

    private void LateUpdate()
    {
        DadosDaArma dados = armas.Arma;
        bool mostrar = dados != null && dados.desenhoNaMao != null && controles != null
                    && (movimento == null || !movimento.Esquivando) && (vida == null || !vida.Morto);
        desenho.enabled = mostrar;

        if (!mostrar)
            return;

        Vector2 mira = controles.Mira;
        coice = Mathf.MoveTowards(coice, 0f, Time.deltaTime * Mathf.Max(0.01f, dados.coice) / voltaDoCoice);

        desenho.sprite = dados.desenhoNaMao;
        desenho.flipY = mira.x < 0f;
        // Mirando pra cima a arma fica atras do corpo; pra baixo e pros lados, na frente.
        desenho.sortingOrder = mira.y > 0.5f ? 9 : 11;
        desenho.transform.localPosition = (Vector3)(mira * (dados.distanciaDaMao - coice)) + new Vector3(0f, alturaDaMao, 0f);
        desenho.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(mira.y, mira.x) * Mathf.Rad2Deg);
    }
}
