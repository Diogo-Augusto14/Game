using UnityEngine;

/// <summary>
/// O rastro da esquiva: copias do corpo, azuladas, que ficam pra tras e somem. So aparece durante a
/// esquiva; mostra o caminho e deixa o "rolamento" mais legivel.
/// </summary>
[DisallowMultipleComponent]
public class RastroDaEsquiva : MonoBehaviour
{
    [SerializeField] private Color cor = new Color(0.55f, 0.8f, 1f, 0.55f);

    [Tooltip("Segundos entre uma copia e outra")]
    [SerializeField, Min(0.01f)] private float intervalo = 0.035f;

    [Tooltip("Segundos que cada copia leva pra sumir")]
    [SerializeField, Min(0.01f)] private float duracao = 0.2f;

    private MovimentoDoJogador movimento;
    private AnimacaoDoJogador animacao;
    private float proxima;

    private void Awake()
    {
        movimento = GetComponent<MovimentoDoJogador>();
        animacao = GetComponent<AnimacaoDoJogador>();
    }

    private void LateUpdate()
    {
        if (movimento == null || animacao == null || animacao.Corpo == null || !movimento.Esquivando || Time.time < proxima)
            return;

        proxima = Time.time + intervalo;
        SpriteRenderer corpo = animacao.Corpo;

        GameObject copia = new GameObject("Rastro da esquiva");
        copia.transform.SetPositionAndRotation(corpo.transform.position, corpo.transform.rotation);
        copia.transform.localScale = corpo.transform.lossyScale;

        SpriteRenderer desenho = copia.AddComponent<SpriteRenderer>();
        desenho.sprite = corpo.sprite;
        desenho.flipX = corpo.flipX;
        desenho.color = cor;
        desenho.sortingOrder = corpo.sortingOrder - 1;

        copia.AddComponent<CopiaQueSome>().Comecar(desenho, duracao);
    }
}

/// <summary>Uma copia do desenho que desbota e some (o rastro da esquiva).</summary>
public class CopiaQueSome : MonoBehaviour
{
    private SpriteRenderer desenho;
    private Color corInicial;
    private float duracao;
    private float comecou;

    public void Comecar(SpriteRenderer qual, float segundos)
    {
        desenho = qual;
        corInicial = qual.color;
        duracao = segundos;
        comecou = Time.time;
    }

    private void Update()
    {
        float t = (Time.time - comecou) / duracao;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        Color cor = corInicial;
        cor.a *= 1f - t;
        desenho.color = cor;
    }
}
