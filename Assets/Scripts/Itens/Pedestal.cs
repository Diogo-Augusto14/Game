using UnityEngine;

/// <summary>
/// O pedestal da sala do item: uma pedra com um item passivo flutuando em cima. Encostou,
/// pegou. O item vai pro <see cref="EstatisticasDoJogador"/> e o pedestal fica vazio.
/// Monte por <see cref="Criar"/>.
/// </summary>
[DisallowMultipleComponent]
public class Pedestal : MonoBehaviour
{
    [SerializeField] private string tagDoJogador = "Player";

    private ItemPassivo item;
    private Transform desenhoDoItem;
    private float fase;

    public ItemPassivo Item => item;

    public bool Vazio => item == null;

    public static Pedestal Criar(ItemPassivo item, Vector2 posicao, Transform pai = null)
    {
        GameObject obj = new GameObject($"Pedestal ({item?.nome})");
        obj.transform.SetParent(pai, true);
        obj.transform.position = posicao;

        // A pedra: solida, pra nao dar pra atravessar, e um sensor um pouco maior em volta.
        GameObject pedra = new GameObject("Pedra");
        pedra.transform.SetParent(obj.transform, false);
        pedra.transform.localScale = new Vector3(0.8f, 0.5f, 1f);
        pedra.transform.localPosition = new Vector3(0f, -0.2f, 0f);

        SpriteRenderer srPedra = pedra.AddComponent<SpriteRenderer>();
        srPedra.sprite = FormasTopDown.Quadrado();
        srPedra.color = new Color(0.55f, 0.52f, 0.5f);
        srPedra.sortingOrder = 4;
        pedra.AddComponent<BoxCollider2D>();

        CircleCollider2D sensor = obj.AddComponent<CircleCollider2D>();
        sensor.isTrigger = true;
        sensor.radius = 0.75f;

        Pedestal p = obj.AddComponent<Pedestal>();
        p.item = item;

        if (item != null)
        {
            GameObject desenho = new GameObject("Item");
            desenho.transform.SetParent(obj.transform, false);
            desenho.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            desenho.transform.localScale = Vector3.one * 0.45f;

            SpriteRenderer sr = desenho.AddComponent<SpriteRenderer>();
            sr.sprite = FormasTopDown.Circulo();
            sr.color = item.cor;
            sr.sortingOrder = 6;

            p.desenhoDoItem = desenho.transform;
        }

        return p;
    }

    private void Update()
    {
        if (desenhoDoItem == null)
            return;

        // Flutua pra cima e pra baixo, como no Isaac.
        fase += Time.deltaTime * 3f;
        desenhoDoItem.localPosition = new Vector3(0f, 0.35f + Mathf.Sin(fase) * 0.06f, 0f);
    }

    private void OnTriggerEnter2D(Collider2D outro) => TentarDar(outro);

    private void OnTriggerStay2D(Collider2D outro) => TentarDar(outro);

    private void TentarDar(Collider2D outro)
    {
        if (item == null)
            return;

        GameObject quem = outro.attachedRigidbody != null ? outro.attachedRigidbody.gameObject : outro.gameObject;

        if (!quem.CompareTag(tagDoJogador))
            return;

        EstatisticasDoJogador estatisticas = quem.GetComponent<EstatisticasDoJogador>();

        if (estatisticas == null)
            estatisticas = quem.AddComponent<EstatisticasDoJogador>();

        ItemPassivo dado = item;
        item = null;

        if (desenhoDoItem != null)
            Destroy(desenhoDoItem.gameObject);

        estatisticas.Pegar(dado);
    }
}
