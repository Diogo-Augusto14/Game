using UnityEngine;

/// <summary>O que um pedestal da: um item, ou (na loja) coracao, bomba, chave ou municao.</summary>
public enum Produto
{
    Item,
    Coracao,
    Bomba,
    Chave,
    Municao,
}

/// <summary>
/// Um pedestal de pedra com uma coisa em cima, girando devagar (veio do jogo antigo). De graca (o premio
/// do bau trancado, da emboscada), a venda (a loja: moedas, com o desconto da Bolsa do Mercador) ou
/// pelo preco de vida (o altar de sangue). A dica mostra o nome e o que faz. Pegar um ativo com outro
/// na mao deixa o velho no pedestal.
/// </summary>
public class Pedestal : Interativo
{
    private ItemPassivo item;
    private Produto produto;
    private int preco;
    private float custoEmVida;
    private SpriteRenderer icone;
    private Vector3 baseDoIcone;
    private float fase;

    public override float Alcance => 1.3f;

    public static Pedestal Criar(Vector2 onde, Transform pai, ItemPassivo item, int preco = 0, float custoEmVida = 0f,
                                 Produto produto = Produto.Item, bool comBase = true)
    {
        GameObject obj = new GameObject("Pedestal");
        obj.transform.SetParent(pai, false);
        obj.transform.position = onde;

        if (comBase)
        {
            SpriteRenderer pedra = obj.AddComponent<SpriteRenderer>();
            pedra.sprite = Resources.Load<Sprite>("Itens/Pedestal");
            pedra.sortingOrder = 9;

            // O pedestal segura tiro e ninguem atravessa.
            BoxCollider2D colisor = obj.AddComponent<BoxCollider2D>();
            colisor.size = new Vector2(0.8f, 0.5f);
            obj.layer = Pedreiro.CamadaDaParede;
        }

        GameObject desenho = new GameObject("Icone");
        desenho.transform.SetParent(obj.transform, false);
        desenho.transform.localPosition = new Vector3(0f, comBase ? 0.75f : 0.2f, 0f);

        Pedestal pedestal = obj.AddComponent<Pedestal>();
        pedestal.icone = desenho.AddComponent<SpriteRenderer>();
        pedestal.icone.sortingOrder = 11;
        Iluminacao.Brilhar(pedestal.icone);
        Iluminacao.Luz(obj.transform, new Vector2(0f, 0.6f), new Color(1f, 0.9f, 0.6f), 2.6f, 0.7f, 0.05f);
        pedestal.baseDoIcone = desenho.transform.localPosition;
        pedestal.item = item;
        pedestal.produto = produto;
        pedestal.preco = preco;
        pedestal.custoEmVida = custoEmVida;
        pedestal.fase = Random.value * 10f;
        pedestal.Desenhar();
        return pedestal;
    }

    private string Nome
    {
        get
        {
            switch (produto)
            {
                case Produto.Coracao: return "Coração";
                case Produto.Bomba: return "Bomba";
                case Produto.Chave: return "Chave";
                case Produto.Municao: return "Munição";
                default: return item != null ? item.Nome : "?";
            }
        }
    }

    private int PrecoAgora
    {
        get
        {
            GameObject jogador = GameObject.FindWithTag("Player");
            float desconto = jogador != null && jogador.TryGetComponent(out EstatisticasDoJogador e) ? e.Desconto : 1f;
            return Mathf.Max(1, Mathf.CeilToInt(preco * desconto));
        }
    }

    public override string Dica
    {
        get
        {
            string resumo = produto == Produto.Item && item != null ? " - " + item.Resumo : "";

            if (preco > 0)
                return $"comprar {Nome} ({PrecoAgora} moedas){resumo}";

            if (custoEmVida > 0f)
                return $"dar {custoEmVida / 2f:0.#} coração por {Nome}{resumo}";

            return $"pegar {Nome}{resumo}";
        }
    }

    private void Desenhar()
    {
        switch (produto)
        {
            case Produto.Coracao: icone.sprite = Coletavel.Desenho(TipoDeColetavel.Coracao); break;
            case Produto.Bomba: icone.sprite = Coletavel.Desenho(TipoDeColetavel.Bomba); break;
            case Produto.Chave: icone.sprite = Coletavel.Desenho(TipoDeColetavel.Chave); break;
            case Produto.Municao: icone.sprite = ArteDoAntigo.Icone(2155, 48f); break;
            default: icone.sprite = item != null ? item.Desenho : null; break;
        }
    }

    private void Update()
    {
        icone.transform.localPosition = baseDoIcone + Vector3.up * (0.08f * Mathf.Sin((Time.time + fase) * 2.5f));
    }

    /// <summary>A Moeda do Destino: troca o item por outro (so os pedestais de item).</summary>
    public bool Trocar()
    {
        if (produto != Produto.Item || item == null)
            return false;

        GameObject jogador = GameObject.FindWithTag("Player");
        item = CatalogoDeItens.Sortear(jogador != null ? jogador.GetComponent<EstatisticasDoJogador>() : null);
        Desenhar();
        EfeitoDeFolha.Tocar(ArteDoAntigo.ExplosaoPequena, icone.transform.position, 24f, 25);
        return true;
    }

    public override void Usar(GameObject jogador)
    {
        Bolsa bolsa = jogador.GetComponent<Bolsa>();
        Vida vida = jogador.GetComponent<Vida>();

        // Coracao com a vida cheia nao vale a compra.
        if (produto == Produto.Coracao && vida != null && vida.Atual >= vida.Maxima)
        {
            Negar(jogador, "Vida cheia");
            return;
        }

        if (produto == Produto.Municao && !TemArmaPraEncher(jogador))
        {
            Negar(jogador, "Nada pra encher");
            return;
        }

        if (preco > 0)
        {
            if (bolsa == null || !bolsa.Gastar(PrecoAgora))
            {
                Negar(jogador, "Faltam moedas");
                return;
            }

            Sons.Tocar(Som.Compra, 0.8f, 0f);
        }

        if (custoEmVida > 0f)
        {
            if (vida == null || vida.Atual <= custoEmVida)
            {
                Negar(jogador, "Vida de menos");
                return;
            }

            vida.Pagar(custoEmVida);
            Sons.Tocar(Som.DanoJogador, 0.8f, 0f);
            CameraDoJogo.Tremer(0.15f, 0.2f);
        }

        Dar(jogador, bolsa, vida);
    }

    private void Dar(GameObject jogador, Bolsa bolsa, Vida vida)
    {
        switch (produto)
        {
            case Produto.Coracao:
                vida?.Curar(2f);
                Sons.Tocar(Som.Coracao, 0.8f);
                break;

            case Produto.Bomba:
                bolsa?.Ganhar(0, 0, 1);
                break;

            case Produto.Chave:
                bolsa?.Ganhar(0, 1, 0);
                break;

            case Produto.Municao:
                EncherMunicao(jogador);
                break;

            default:
                if (item == null)
                    return;

                if (item.EhAtivo)
                {
                    ItemAtivoDoJogador ativo = jogador.GetComponent<ItemAtivoDoJogador>();
                    ItemPassivo velho = ativo != null ? ativo.Equipar(item) : null;
                    Sons.Tocar(Som.Item, 0.8f, 0f);
                    TextoFlutuante.Mostrar(jogador.transform.position + Vector3.up * 1.6f, item.Nome, new Color(1f, 0.9f, 0.55f), 2f);
                    TextoFlutuante.Mostrar(jogador.transform.position + Vector3.up * 1.15f, item.Resumo, new Color(0.9f, 0.9f, 0.95f), 2f);

                    // O ativo velho fica aqui, de graca.
                    if (velho != null)
                    {
                        item = velho;
                        preco = 0;
                        custoEmVida = 0f;
                        Desenhar();
                        return;
                    }
                }
                else if (jogador.TryGetComponent(out EstatisticasDoJogador estatisticas))
                {
                    estatisticas.Pegar(item);
                }

                break;
        }

        Destroy(gameObject);
    }

    private static bool TemArmaPraEncher(GameObject jogador)
    {
        ArmaDoJogador armas = jogador.GetComponent<ArmaDoJogador>();

        for (int i = 0; armas != null && i < 2; i++)
        {
            ArmaCarregada arma = armas.Mao(i);

            if (arma != null && !arma.Infinita && arma.Falta > 0)
                return true;
        }

        return false;
    }

    private static void EncherMunicao(GameObject jogador)
    {
        ArmaDoJogador armas = jogador.GetComponent<ArmaDoJogador>();

        for (int i = 0; armas != null && i < 2; i++)
        {
            ArmaCarregada arma = armas.Mao(i);

            if (arma != null && !arma.Infinita)
                arma.Ganhar(arma.Dados.municaoMaxima);
        }

        Sons.Tocar(Som.Destranca, 0.8f, 0f);
    }

    private static void Negar(GameObject jogador, string porque)
    {
        Sons.Tocar(Som.Negado, 0.7f, 0f);
        TextoFlutuante.Mostrar(jogador.transform.position + Vector3.up * 1.2f, porque, new Color(1f, 0.6f, 0.55f), 0.9f);
    }
}
