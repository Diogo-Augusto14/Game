using UnityEngine;

/// <summary>
/// Uma copia parada de um desenho que vai sumindo: o rastro da flecha rapida e dos tiros
/// que deixam "fantasma" pra tras. Criada pelo <see cref="VisualDoProjetil"/>.
/// </summary>
[DisallowMultipleComponent]
public class RastroQueSome : MonoBehaviour
{
    private SpriteRenderer desenho;
    private Color cor;
    private float duracao;
    private float inicio;
    private Vector3 escala;

    public static void Criar(SpriteRenderer original, Color cor, float duracao)
    {
        if (original == null || original.sprite == null)
            return;

        GameObject obj = new GameObject("Rastro");
        obj.transform.SetPositionAndRotation(original.transform.position, original.transform.rotation);
        obj.transform.localScale = original.transform.lossyScale;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = original.sprite;
        sr.flipX = original.flipX;
        sr.color = cor;
        sr.sortingOrder = original.sortingOrder - 1;

        RastroQueSome rastro = obj.AddComponent<RastroQueSome>();
        rastro.desenho = sr;
        rastro.cor = cor;
        rastro.duracao = Mathf.Max(0.02f, duracao);
        rastro.inicio = Time.time;
        rastro.escala = obj.transform.localScale;
    }

    private void Update()
    {
        float t = (Time.time - inicio) / duracao;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        Color c = cor;
        c.a = cor.a * (1f - t);
        desenho.color = c;
        transform.localScale = escala * Mathf.Lerp(1f, 0.7f, t);
    }
}
