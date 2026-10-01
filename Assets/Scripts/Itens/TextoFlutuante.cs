using UnityEngine;

/// <summary>
/// Um texto curto que sobe e some no mundo ("Bloqueou!", "+ vida"). Mostra o que um item
/// acabou de fazer sem precisar de HUD. Use <see cref="Mostrar"/>.
/// </summary>
public class TextoFlutuante : MonoBehaviour
{
    private const float Duracao = 0.9f;

    private TextMesh texto;
    private Color cor;
    private float nasceu;
    private Vector2 velocidade = new Vector2(0f, 1.2f);
    private float gravidade;

    public static TextoFlutuante Mostrar(Vector3 posicao, string conteudo, Color cor)
    {
        GameObject obj = new GameObject("Texto flutuante");
        obj.transform.position = posicao;

        TextMesh texto = ProdutoDaLoja.Texto(obj.transform, conteudo, Vector2.zero);
        texto.characterSize = 0.07f;
        texto.color = cor;
        texto.GetComponent<MeshRenderer>().sortingOrder = 40;

        TextoFlutuante t = obj.AddComponent<TextoFlutuante>();
        t.texto = texto;
        t.cor = cor;
        t.nasceu = Time.time;
        return t;
    }

    /// <summary>
    /// Vira numero de dano: maior (<paramref name="escala"/>), sai pulando pra um lado e cai
    /// um pouco, em vez de so subir.
    /// </summary>
    public void Pular(float escala)
    {
        texto.characterSize *= escala;
        velocidade = new Vector2(Random.Range(-0.8f, 0.8f), 2.6f);
        gravidade = 5f;
        transform.localScale = Vector3.one * 1.35f;
    }

    private void Update()
    {
        float t = (Time.time - nasceu) / Duracao;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        velocidade.y -= gravidade * Time.deltaTime;
        transform.position += (Vector3)(velocidade * Time.deltaTime);

        // Numero de dano: nasce grande e encolhe pro tamanho normal (o "pop").
        if (transform.localScale.x > 1f)
            transform.localScale = Vector3.one * Mathf.MoveTowards(transform.localScale.x, 1f, 3f * Time.deltaTime);

        texto.color = new Color(cor.r, cor.g, cor.b, 1f - t * t);
    }
}
