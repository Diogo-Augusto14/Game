using UnityEngine;

/// <summary>
/// A fogueira do acampamento (1.21.0): lenha, pedras e a chama tremendo (Resources/Cenario/Fogueira, seis
/// quadros feitos pelo Ferramentas/Cenario/desenhos.py), com uma luz laranja grande que treme. Segura gente
/// (da a volta nela). Monte com <see cref="Criar"/>.
/// </summary>
public class Fogueira : MonoBehaviour
{
    private const float QuadrosPorSegundo = 9f;

    private Sprite[] quadros;
    private SpriteRenderer desenho;
    private float fase;

    /// <param name="pe">Onde a fogueira fica (o pe da lenha).</param>
    public static Fogueira Criar(Vector2 pe, Transform pai)
    {
        Sprite[] quadros = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Cenario/Fogueira"), new Vector2Int(32, 40), 32f);

        if (quadros.Length == 0)
            return null;

        GameObject obj = new GameObject("Fogueira");
        obj.transform.SetParent(pai, false);
        // O quadro tem o pe da lenha 18 px abaixo do meio (40 px de altura, a lenha ate a linha 38).
        obj.transform.position = pe + Vector2.up * (18f / 32f);

        Fogueira f = obj.AddComponent<Fogueira>();
        f.quadros = quadros;
        f.desenho = obj.AddComponent<SpriteRenderer>();
        f.desenho.sprite = quadros[0];
        f.desenho.sortingOrder = 10;
        f.fase = Random.value * 10f;
        Iluminacao.Brilhar(f.desenho);

        obj.layer = Pedreiro.CamadaDaParede;
        BoxCollider2D c = obj.AddComponent<BoxCollider2D>();
        c.size = new Vector2(0.9f, 0.35f);
        c.offset = new Vector2(0f, -18f / 32f + 0.15f);

        Iluminacao.Luz(obj.transform, new Vector2(0f, -0.1f), new Color(1f, 0.6f, 0.25f), 6.5f, 1f, 0.15f);
        return f;
    }

    private void Update()
    {
        desenho.sprite = quadros[Mathf.FloorToInt((Time.time + fase) * QuadrosPorSegundo) % quadros.Length];
    }
}
