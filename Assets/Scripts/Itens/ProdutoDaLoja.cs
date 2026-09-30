using UnityEngine;

/// <summary>
/// Uma coisa a venda: um coletavel ou um item passivo, com preco em moedas. Com o jogador
/// perto, mostra uma etiqueta com o nome e o que faz; o preco fica amarelo quando da pra
/// pagar e vermelho quando nao da.
/// </summary>
[DisallowMultipleComponent]
public class ProdutoDaLoja : MonoBehaviour
{
    [SerializeField] private string tagDoJogador = "Player";

    [SerializeField, Min(0f)] private float curaDoCoracao = 20f;

    private int preco;
    private bool ehItem;
    private TipoDeColetavel coletavel;
    private ItemPassivo item;
    private TextMesh textoDoPreco;
    private Transform desenho;
    private float piscarAte;
    private bool vendido;
    private int precoMostrado = -1;
    private Transform jogador;
    private Inventario inventarioDoJogador;
    private GameObject etiqueta;

    [Tooltip("Distancia em que a etiqueta com nome e efeito aparece")]
    [SerializeField, Min(0.5f)] private float distanciaDaEtiqueta = 1.6f;

    /// <summary>Alguem comprou um produto (o comerciante agradece).</summary>
    public static event System.Action<ProdutoDaLoja> AoVender;

    /// <summary>Tentou comprar e nao deu; true = faltou moeda, false = nao serve agora.</summary>
    public static event System.Action<ProdutoDaLoja, bool> AoRecusar;

    /// <summary>O preco de tabela, sem desconto.</summary>
    public int Preco => preco;

    /// <summary>O que o jogador paga agora (com o desconto dos itens).</summary>
    public int PrecoAtual => Loja.ComDesconto(preco);

    public bool Vendido => vendido;

    public static ProdutoDaLoja Criar(ItemPassivo item, int preco, Vector2 posicao, Transform pai)
    {
        ProdutoDaLoja p = Base($"Loja: {item?.nome}", preco, posicao, pai);
        p.ehItem = true;
        p.item = item;
        Sprite icone = item != null ? ArteImportada.IconeDoItem(item.nome) : null;
        p.desenho = icone != null
            ? Desenhar(p.transform, icone, Color.white, Vector2.one * 0.8f)
            : Desenhar(p.transform, ArteGerada.Bola(), item != null ? item.cor : Color.white, Vector2.one * 0.45f);
        return p;
    }

    public static ProdutoDaLoja Criar(TipoDeColetavel tipo, int preco, Vector2 posicao, Transform pai)
    {
        ProdutoDaLoja p = Base($"Loja: {tipo}", preco, posicao, pai);
        p.coletavel = tipo;

        p.desenho = Desenhar(p.transform, ArteGerada.Coletavel(tipo), Color.white, Vector2.one * Coletavel.TamanhoDe(tipo));
        return p;
    }

    private static ProdutoDaLoja Base(string nome, int preco, Vector2 posicao, Transform pai)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(pai, true);
        obj.transform.position = posicao;

        // Tapete escuro embaixo, pra ficar claro que e vitrine e nao coisa caida.
        // Com o pacote, a mesinha de madeira da masmorra faz de vitrine.
        Sprite mesa = ArteImportada.Objeto(1, 2);

        if (mesa != null)
            FormasDaSala.Desenho(obj.transform, "Mesa", mesa, Color.white, new Vector2(0f, -0.1f), Vector2.one * 1.1f, -8);
        else
            FormasDaSala.Desenho(obj.transform, "Tapete", FormasDaSala.Quadrado(), new Color(0.12f, 0.1f, 0.1f), Vector2.zero, new Vector2(1f, 0.9f), -8);

        CircleCollider2D sensor = obj.AddComponent<CircleCollider2D>();
        sensor.isTrigger = true;
        sensor.radius = 0.45f;

        ProdutoDaLoja p = obj.AddComponent<ProdutoDaLoja>();
        p.preco = preco;
        p.textoDoPreco = Texto(obj.transform, preco.ToString(), new Vector2(0f, -0.65f));

        // Moedinha do lado do numero.
        FormasDaSala.Desenho(obj.transform, "Moeda", ArteGerada.Coletavel(TipoDeColetavel.Moeda), Color.white,
            new Vector2(0.34f, -0.65f), Vector2.one * 0.26f, 21);

        // Plaquinha do Pixel UI pack atras do preco (fatiada: as bordas nao esticam).
        Sprite placa = ArteImportada.PlacaNoMundo;

        if (placa != null)
        {
            SpriteRenderer sr = FormasDaSala.Desenho(obj.transform, "Placa", placa, Color.white, new Vector2(0.1f, -0.65f), Vector2.one, 20);
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(0.95f, 0.38f);
        }
        return p;
    }

    private static Transform Desenhar(Transform pai, Sprite forma, Color cor, Vector2 tamanho)
    {
        SpriteRenderer sr = FormasDaSala.Desenho(pai, "Produto", forma, cor, new Vector2(0f, 0.1f), tamanho, 6);

        if (forma == FormasDaSala.Quadrado())
        {
            // Quadrado em modo Tiled nao escala; aqui a peca e pequena, entao usa escala.
            sr.drawMode = SpriteDrawMode.Simple;
            sr.transform.localScale = new Vector3(tamanho.x, tamanho.y, 1f);
        }

        return sr.transform;
    }

    /// <summary>Texto no mundo (TextMesh), por cima do chao. Usado pro preco.</summary>
    public static TextMesh Texto(Transform pai, string conteudo, Vector2 posicaoLocal)
    {
        GameObject obj = new GameObject("Preco");
        obj.transform.SetParent(pai, false);
        obj.transform.localPosition = posicaoLocal;

        Font fonte = TelaDeFimDeJogo.Fonte();
        TextMesh texto = obj.AddComponent<TextMesh>();
        texto.font = fonte;
        texto.fontSize = 48;
        texto.characterSize = 0.06f;
        texto.anchor = TextAnchor.MiddleCenter;
        texto.alignment = TextAlignment.Center;
        texto.color = Color.white;
        texto.text = conteudo;

        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        mr.sharedMaterial = fonte.material;
        mr.sortingOrder = 21;
        return texto;
    }

    private void Update()
    {
        if (desenho != null)
            desenho.localPosition = new Vector3(0f, 0.1f + Mathf.Sin(Time.time * 3f + transform.position.x) * 0.05f, 0f);

        if (jogador == null)
        {
            GameObject achado = GameObject.FindWithTag(tagDoJogador);

            if (achado != null)
            {
                jogador = achado.transform;
                inventarioDoJogador = achado.GetComponent<Inventario>();
            }
        }

        int agora = PrecoAtual;

        if (textoDoPreco != null && agora != precoMostrado)
        {
            precoMostrado = agora;
            textoDoPreco.text = agora.ToString();
        }

        bool daPraPagar = inventarioDoJogador == null || inventarioDoJogador.Moedas >= agora;

        if (textoDoPreco != null)
            textoDoPreco.color = Time.time < piscarAte && Mathf.Repeat(Time.time * 10f, 1f) < 0.5f
                ? new Color(1f, 0.3f, 0.3f)
                : daPraPagar ? new Color(1f, 0.92f, 0.55f) : new Color(1f, 0.55f, 0.5f);

        bool perto = jogador != null
                     && ((Vector2)(jogador.position - transform.position)).sqrMagnitude <= distanciaDaEtiqueta * distanciaDaEtiqueta;

        if (perto && etiqueta == null)
            etiqueta = CriarEtiqueta();

        if (etiqueta != null && etiqueta.activeSelf != perto)
            etiqueta.SetActive(perto);
    }

    /// <summary>Nome em amarelo e o efeito embaixo, numa plaquinha acima do produto.</summary>
    private GameObject CriarEtiqueta()
    {
        string nome, efeito;

        if (ehItem)
        {
            nome = item != null ? item.nome : "?";
            efeito = item != null ? item.Efeito : "";
        }
        else
        {
            nome = NomeDe(coletavel);
            efeito = EfeitoDe(coletavel);
        }

        string corpo = Quebrar(efeito, 26);
        int linhas = 1 + (string.IsNullOrEmpty(corpo) ? 0 : corpo.Split('\n').Length);
        int largura = Mathf.Max(nome.Length, MaiorLinha(corpo));

        GameObject obj = new GameObject("Etiqueta");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(0f, 0.75f + linhas * 0.1f, 0f);

        Sprite placa = ArteImportada.PlacaNoMundo;

        if (placa != null)
        {
            SpriteRenderer sr = FormasDaSala.Desenho(obj.transform, "Fundo", placa, Color.white, Vector2.zero, Vector2.one, 32);
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(0.35f + largura * 0.085f, 0.2f + linhas * 0.19f);
        }

        TextMesh texto = Texto(obj.transform, "", Vector2.zero);
        texto.characterSize = 0.036f;
        texto.richText = true;
        texto.text = string.IsNullOrEmpty(corpo)
            ? $"<color=#ffd24a>{nome}</color>"
            : $"<color=#ffd24a>{nome}</color>\n{corpo}";
        texto.GetComponent<MeshRenderer>().sortingOrder = 33;
        return obj;
    }

    private static string NomeDe(TipoDeColetavel tipo)
    {
        switch (tipo)
        {
            case TipoDeColetavel.Coracao: return "Coracao";
            case TipoDeColetavel.Bomba: return "Bomba";
            case TipoDeColetavel.Chave: return "Chave";
            default: return "Moeda";
        }
    }

    private static string EfeitoDe(TipoDeColetavel tipo)
    {
        switch (tipo)
        {
            case TipoDeColetavel.Coracao: return "Recupera um coracao de vida.";
            case TipoDeColetavel.Bomba: return "Mais uma bomba (tecla E).";
            case TipoDeColetavel.Chave: return "Abre portas e baus trancados.";
            default: return "";
        }
    }

    /// <summary>Quebra o texto em linhas de ate <paramref name="largura"/> letras, sem cortar palavra.</summary>
    private static string Quebrar(string texto, int largura)
    {
        if (string.IsNullOrEmpty(texto))
            return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int naLinha = 0;

        foreach (string palavra in texto.Split(' '))
        {
            if (naLinha > 0 && naLinha + 1 + palavra.Length > largura)
            {
                sb.Append('\n');
                naLinha = 0;
            }
            else if (naLinha > 0)
            {
                sb.Append(' ');
                naLinha++;
            }

            sb.Append(palavra);
            naLinha += palavra.Length;
        }

        return sb.ToString();
    }

    private static int MaiorLinha(string texto)
    {
        int maior = 0;

        foreach (string l in texto.Split('\n'))
            maior = Mathf.Max(maior, l.Length);

        return maior;
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (vendido)
            return;

        GameObject quem = outro.attachedRigidbody != null ? outro.attachedRigidbody.gameObject : outro.gameObject;

        if (quem.CompareTag(tagDoJogador))
            TentarComprar(quem);
    }

    /// <summary>Compra se o jogador tiver moeda e se o produto servir pra ele agora.</summary>
    public bool TentarComprar(GameObject jogador)
    {
        Inventario inventario = jogador.GetComponent<Inventario>();

        if (vendido || inventario == null)
            return false;

        int custo = PrecoAtual;
        bool faltouMoeda = inventario.Moedas < custo;

        if (faltouMoeda || !Serve(jogador, inventario))
        {
            piscarAte = Time.time + 0.6f;
            Sons.Tocar(Som.Negado);
            AoRecusar?.Invoke(this, faltouMoeda);
            return false;
        }

        inventario.Adicionar(TipoDeColetavel.Moeda, -custo);
        vendido = true;

        if (ehItem)
        {
            EstatisticasDoJogador estatisticas = jogador.GetComponent<EstatisticasDoJogador>();

            if (estatisticas == null)
                estatisticas = jogador.AddComponent<EstatisticasDoJogador>();

            estatisticas.Pegar(item);
        }
        else if (coletavel == TipoDeColetavel.Coracao)
        {
            jogador.GetComponent<Vida>()?.Curar(curaDoCoracao);
        }
        else
        {
            inventario.Adicionar(coletavel, 1);
        }

        Sons.Tocar(Som.Compra);
        AoVender?.Invoke(this);
        Destroy(gameObject);
        return true;
    }

    /// <summary>Coracao com vida cheia (ou contador lotado) nao vende: o Isaac tambem nao deixa.</summary>
    private bool Serve(GameObject jogador, Inventario inventario)
    {
        if (ehItem)
            return item != null;

        if (coletavel == TipoDeColetavel.Coracao)
        {
            Vida vida = jogador.GetComponent<Vida>();
            return vida != null && !vida.EstaMorto && vida.VidaAtual < vida.VidaMaxima;
        }

        return inventario.CabeMais(coletavel);
    }
}
