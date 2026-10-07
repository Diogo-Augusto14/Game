using UnityEngine;

/// <summary>A marca no chao do aviso do chefe (onde vai cair, onde vai explodir): pisca e pulsa.</summary>
public class MarcaPiscando : MonoBehaviour
{
    private SpriteRenderer desenho;
    private float alfa;
    private float nasceu;

    private void Awake()
    {
        desenho = GetComponent<SpriteRenderer>();
        alfa = desenho != null ? desenho.color.a : 1f;
        nasceu = Time.time;
    }

    private void Update()
    {
        if (desenho == null)
            return;

        float t = Time.time - nasceu;
        Color c = desenho.color;
        c.a = alfa * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * 9f)));
        desenho.color = c;
        transform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(t * 9f));
    }
}
