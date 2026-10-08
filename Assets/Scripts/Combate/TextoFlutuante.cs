using UnityEngine;

/// <summary>
/// Um texto curto que sobe e some no mundo: o numero do dano (<see cref="Impacto.Numero"/>) e a
/// frase do chefe virando de fase (<see cref="ViradaDeFase"/>). Veio do jogo antigo. Use
/// <see cref="Mostrar"/>.
/// </summary>
public class TextoFlutuante : MonoBehaviour
{
    private const float Duracao = 0.9f;

    private TextMesh texto;
    private Color cor;
    private float nasceu;
    private float duracao = Duracao;
    private Vector2 velocidade = new Vector2(0f, 1.2f);
    private float gravidade;

    public static TextoFlutuante Mostrar(Vector3 posicao, string conteudo, Color cor, float duracao = Duracao)
    {
        GameObject obj = new GameObject("Texto flutuante");
        obj.transform.position = posicao;

        // Fonte grande e o caractere pequeno: o texto sai nitido, no tamanho certo do mundo.
        TextMesh texto = obj.AddComponent<TextMesh>();
        texto.font = FonteDoJogo.Texto;
        texto.fontSize = 64;
        texto.characterSize = 0.07f;
        texto.anchor = TextAnchor.MiddleCenter;
        texto.alignment = TextAlignment.Center;
        texto.text = conteudo;
        texto.color = cor;

        MeshRenderer desenho = obj.GetComponent<MeshRenderer>();

        if (texto.font != null)
            desenho.sharedMaterial = texto.font.material;

        desenho.sortingOrder = 40;
        Iluminacao.Brilhar(desenho);

        TextoFlutuante t = obj.AddComponent<TextoFlutuante>();
        t.texto = texto;
        t.cor = cor;
        t.nasceu = Time.time;
        t.duracao = duracao;
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
        float t = (Time.time - nasceu) / duracao;

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
