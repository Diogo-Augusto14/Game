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
    private bool esperandoSair;

    public ItemPassivo Item => item;

    public bool Vazio => item == null;

    /// <summary>O jogador pegou o item deste pedestal (a sala de desafio comeca a luta aqui).</summary>
    public event System.Action<Pedestal> AoPegar;

    /// <summary>
    /// "Escolha um": pegar o item de um destes pedestais faz o dos outros sumir, como a
    /// sala do tesouro com duas opcoes do Isaac.
    /// </summary>
    public static void EscolhaUm(params Pedestal[] pedestais)
    {
        foreach (Pedestal escolhido in pedestais)
        {
            escolhido.AoPegar += _ =>
            {
                foreach (Pedestal outro in pedestais)
                    if (outro != escolhido)
                        outro.Sumir();
            };
        }
    }

    /// <summary>O item some sem ninguem pegar (fica so o altar).</summary>
    public void Sumir()
    {
        if (item == null)
            return;

        item = null;

        if (desenhoDoItem != null)
            Destroy(desenhoDoItem.gameObject);
    }

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
        Sprite altar = ArteImportada.PecaDaPrisao("Altar", 56f, false);
        srPedra.sortingOrder = 4;

        if (altar != null)
        {
            // O tumulo de pedra do Old Prison, sem esticar; o colisor fica do tamanho da pedra antiga.
            pedra.transform.localScale = Vector3.one;
            srPedra.sprite = altar;
            srPedra.color = Color.white;
            pedra.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 0.5f);
        }
        else
        {
            srPedra.sprite = FormasTopDown.Quadrado();
            srPedra.color = new Color(0.55f, 0.52f, 0.5f);
            pedra.AddComponent<BoxCollider2D>();
        }

        CircleCollider2D sensor = obj.AddComponent<CircleCollider2D>();
        sensor.isTrigger = true;
        sensor.radius = 0.75f;

        Pedestal p = obj.AddComponent<Pedestal>();
        p.item = item;
        p.Desenhar();
        return p;
    }

    /// <summary>Poe <paramref name="novo"/> no lugar do item (Moeda do Destino, troca de ativo).</summary>
    public void Trocar(ItemPassivo novo)
    {
        if (desenhoDoItem != null)
            Destroy(desenhoDoItem.gameObject);

        desenhoDoItem = null;
        item = novo;
        name = $"Pedestal ({item?.nome})";
        Desenhar();
    }

    /// <summary>
    /// Devolve um item pro pedestal (o ativo que o jogador largou ao pegar outro). So da pra
    /// pegar de novo depois de sair de cima, senao os dois trocariam sem parar.
    /// </summary>
    public void Devolver(ItemPassivo antigo)
    {
        Trocar(antigo);
        esperandoSair = true;
    }

    /// <summary>Larga um item num pedestal novo no chao (ativo trocado fora de um pedestal: loja, bau).</summary>
    public static Pedestal Largar(ItemPassivo item, Vector2 posicao, Transform pai)
    {
        Pedestal p = Criar(null, posicao, pai);
        p.Devolver(item);
        return p;
    }

    private void Desenhar()
    {
        if (item != null)
        {
            GameObject desenho = new GameObject("Item");
            desenho.transform.SetParent(transform, false);
            desenho.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            Sprite icone = ArteImportada.IconeDoItem(item.nome);
            desenho.transform.localScale = Vector3.one * (icone != null ? 0.8f : 0.45f);

            SpriteRenderer sr = desenho.AddComponent<SpriteRenderer>();
            sr.sprite = icone != null ? icone : ArteGerada.Bola();
            sr.color = icone != null ? Color.white : item.cor;
            sr.sortingOrder = 6;

            desenhoDoItem = desenho.transform;
        }
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

    private void OnTriggerExit2D(Collider2D outro)
    {
        GameObject quem = outro.attachedRigidbody != null ? outro.attachedRigidbody.gameObject : outro.gameObject;

        if (quem.CompareTag(tagDoJogador))
            esperandoSair = false;
    }

    private void TentarDar(Collider2D outro)
    {
        if (item == null || esperandoSair)
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

        // Trocou de item ativo: o antigo fica aqui no pedestal.
        ItemPassivo largado = estatisticas.Pegar(dado, false);

        if (largado != null)
            Devolver(largado);

        AoPegar?.Invoke(this);
    }
}
