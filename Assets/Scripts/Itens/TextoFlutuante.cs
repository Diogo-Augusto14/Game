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

    private void Update()
    {
        float t = (Time.time - nasceu) / Duracao;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += Vector3.up * (1.2f * Time.deltaTime);
        texto.color = new Color(cor.r, cor.g, cor.b, 1f - t * t);
    }
}
