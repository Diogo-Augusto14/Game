using UnityEngine;

/// <summary>
/// Bau do pacote da masmorra: encostou, abre e espalha o que tem dentro em volta dele.
/// Fica no lugar, escurecido, depois de aberto. Monte por <see cref="Criar"/>.
/// </summary>
[DisallowMultipleComponent]
public class Bau : MonoBehaviour
{
    [SerializeField] private string tagDoJogador = "Player";

    private TipoDeColetavel[] conteudo;
    private SpriteRenderer desenho;

    public bool Aberto { get; private set; }

    public static Bau Criar(Vector2 posicao, Transform pai, params TipoDeColetavel[] conteudo)
    {
        GameObject obj = new GameObject("Bau");
        obj.transform.SetParent(pai, true);
        obj.transform.position = posicao;

        Sprite sprite = ArteImportada.Bau;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : FormasTopDown.Quadrado();
        sr.color = sprite != null ? Color.white : new Color(0.5f, 0.33f, 0.18f);
        sr.sortingOrder = 4;
        obj.transform.localScale = Vector3.one * (sprite != null ? 1.2f : 0.8f);

        // Solido (nao da pra atravessar) e um sensor um pouco maior pra abrir encostando.
        GameObject corpo = new GameObject("Corpo");
        corpo.transform.SetParent(obj.transform, false);
        corpo.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 0.6f);

        CircleCollider2D sensor = obj.AddComponent<CircleCollider2D>();
        sensor.isTrigger = true;
        sensor.radius = 0.7f;

        Bau bau = obj.AddComponent<Bau>();
        bau.conteudo = conteudo;
        bau.desenho = sr;
        return bau;
    }

    private void OnTriggerEnter2D(Collider2D outro) => TentarAbrir(outro);

    private void OnTriggerStay2D(Collider2D outro) => TentarAbrir(outro);

    private void TentarAbrir(Collider2D outro)
    {
        if (Aberto)
            return;

        GameObject quem = outro.attachedRigidbody != null ? outro.attachedRigidbody.gameObject : outro.gameObject;

        if (!quem.CompareTag(tagDoJogador))
            return;

        Aberto = true;
        desenho.color = new Color(0.45f, 0.45f, 0.45f);
        Sons.Tocar(Som.Segredo);

        // Em roda, abaixo do bau, pra nada cair em cima dele.
        int n = conteudo != null ? conteudo.Length : 0;

        for (int i = 0; i < n; i++)
        {
            float angulo = Mathf.Lerp(200f, 340f, n == 1 ? 0.5f : i / (float)(n - 1)) * Mathf.Deg2Rad;
            Vector2 ponto = (Vector2)transform.position + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * 1.2f;
            Coletavel.Criar(conteudo[i], ponto, transform.parent);
        }
    }
}
