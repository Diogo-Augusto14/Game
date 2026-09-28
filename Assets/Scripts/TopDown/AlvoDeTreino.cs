using System.Collections;
using UnityEngine;

/// <summary>
/// Boneco de treino pra testar o tiro: tem <see cref="Vida"/>, mostra uma barrinha em
/// cima da cabeca e, quando morre, some por um instante e volta com a vida cheia.
/// Nao anda nem ataca — e so um saco de pancada ate os inimigos de verdade chegarem.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Vida))]
public class AlvoDeTreino : MonoBehaviour
{
    [Tooltip("Segundos sumido antes de voltar")]
    [SerializeField, Min(0f)] private float tempoParaVoltar = 1.5f;

    [Tooltip("Largura da barrinha de vida, em unidades")]
    [SerializeField, Min(0.1f)] private float larguraDaBarra = 0.8f;

    [SerializeField] private Color corDaBarra = new Color(0.85f, 0.2f, 0.25f);

    // ---------------- estado ----------------
    private Vida vida;
    private Transform barra;
    private Vector3 posicaoInicial;
    private Renderer[] desenhos;
    private Collider2D[] colisores;

    private void Awake()
    {
        vida = GetComponent<Vida>();
        posicaoInicial = transform.position;

        MontarBarra();

        desenhos = GetComponentsInChildren<Renderer>(true);
        colisores = GetComponentsInChildren<Collider2D>(true);

        vida.AoMorrer.AddListener(AoMorrer);
    }

    private void OnDestroy()
    {
        if (vida != null)
            vida.AoMorrer.RemoveListener(AoMorrer);
    }

    private void LateUpdate()
    {
        if (barra == null)
            return;

        // Encolhe pela esquerda: o sprite da barra tem o pivo na ponta esquerda.
        Vector3 escala = barra.localScale;
        escala.x = larguraDaBarra * vida.Fracao;
        barra.localScale = escala;
    }

    private void MontarBarra()
    {
        const float y = 0.65f;

        CriarRetangulo("Barra (fundo)", FormasTopDown.Quadrado(), new Vector3(0f, y, 0f),
            new Vector3(larguraDaBarra + 0.06f, 0.14f, 1f), new Color(0f, 0f, 0f, 0.6f), 30);

        // Sprite com o pivo na ponta esquerda: escalar em X encolhe pra esquerda.
        barra = CriarRetangulo("Barra", FormasTopDown.QuadradoAncoradoNaEsquerda(),
            new Vector3(-larguraDaBarra * 0.5f, y, 0f),
            new Vector3(larguraDaBarra, 0.08f, 1f), corDaBarra, 31);
    }

    private Transform CriarRetangulo(string nome, Sprite sprite, Vector3 posicao, Vector3 escala, Color cor, int ordem)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = posicao;
        obj.transform.localScale = escala;

        SpriteRenderer desenho = obj.AddComponent<SpriteRenderer>();
        desenho.sprite = sprite;
        desenho.color = cor;
        desenho.sortingOrder = ordem;

        return obj.transform;
    }

    private void AoMorrer()
    {
        StartCoroutine(RotinaVoltar());
    }

    private IEnumerator RotinaVoltar()
    {
        Mostrar(false);

        yield return new WaitForSeconds(tempoParaVoltar);

        transform.position = posicaoInicial;

        if (TryGetComponent(out Rigidbody2D rb))
            rb.linearVelocity = Vector2.zero;

        vida.Reviver(0.3f);
        Mostrar(true);
    }

    private void Mostrar(bool visivel)
    {
        foreach (Renderer desenho in desenhos)
        {
            if (desenho != null)
                desenho.enabled = visivel;
        }

        foreach (Collider2D colisor in colisores)
        {
            if (colisor != null)
                colisor.enabled = visivel;
        }
    }
}
