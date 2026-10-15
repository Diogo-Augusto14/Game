using System.Collections;
using UnityEngine;

/// <summary>
/// O ferreiro: um machadeiro velho do lado de uma bigorna em brasa. Pagando moedas, ele forja a arma da
/// mao (<see cref="ArmaDoJogador.Forjar"/>): a arma ganha "+1" no nome, 30% a mais de dano e sobe uma
/// raridade (ate a lendaria). Cada arma vai ate +3 (<see cref="DadosDaArma.ForjaMaxima"/>); da pra
/// forjar quantas vezes quiser, enquanto tiver moedas.
///
/// O preco sobe com a raridade e com a forja que a arma ja tem (<see cref="Preco"/>); o desconto da loja
/// (itens e a estatua) vale aqui tambem. Combina com a Fonte dos Desejos: a arma que voltar melhor da
/// fonte vale mais forjada. Fica na area segura e, as vezes, numa sala so dele (<see cref="TipoDeSala.Ferreiro"/>).
/// Monte com <see cref="Criar"/>.
/// </summary>
public class Ferreiro : Interativo
{
    private static readonly Color Tom = new Color(0.82f, 0.68f, 0.56f);
    private static readonly Color Brasa = new Color(1f, 0.55f, 0.2f);

    /// <summary>O preco base de cada raridade (comum, incomum, rara, epica, lendaria).</summary>
    private static readonly int[] PrecoDaRaridade = { 15, 25, 35, 50, 70 };

    private Sprite[] parado, martelando;
    private SpriteRenderer desenho;
    private Transform jogador;
    private Vector2 bigorna;
    private bool trabalhando;
    private bool soConversa;

    public override float Alcance => 2.4f;

    public override bool Disponivel => !soConversa && !trabalhando && ArmaNaMao() != null;

    /// <summary>O ferreiro do acampamento: nao forja, so conversa (ver <see cref="Falante"/>).</summary>
    public Ferreiro SoConversa()
    {
        soConversa = true;
        return this;
    }

    public override string Dica
    {
        get
        {
            ArmaCarregada arma = ArmaNaMao();

            if (arma == null)
                return "";

            if (arma.Dados.forja >= DadosDaArma.ForjaMaxima)
                return Idioma.F("{0} já está no máximo", arma.Dados.NomeNaTela);

            return Idioma.F("forjar {0} ({1} moedas): +30% de dano e uma raridade acima", arma.Dados.NomeNaTela, Preco(arma.Dados));
        }
    }

    /// <param name="pe">Onde o ferreiro pisa (a bigorna fica do lado direito).</param>
    public static Ferreiro Criar(Vector2 pe, Transform pai)
    {
        Sprite[] parado = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Personagens/Herois/Machadeiro/Idle"), new Vector2Int(100, 100), 20f);

        if (parado.Length == 0)
            return null;

        GameObject obj = new GameObject("Ferreiro");
        obj.transform.SetParent(pai, false);
        // O desenho tem o pe 0,35 abaixo do meio do quadro.
        obj.transform.position = pe + Vector2.up * 0.35f;

        Ferreiro f = obj.AddComponent<Ferreiro>();
        f.parado = parado;
        f.martelando = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Personagens/Herois/Machadeiro/Attack01"), new Vector2Int(100, 100), 20f);
        f.desenho = obj.AddComponent<SpriteRenderer>();
        f.desenho.sprite = parado[0];
        f.desenho.color = Tom;
        f.desenho.sortingOrder = 10;

        obj.layer = Pedreiro.CamadaDaParede;
        BoxCollider2D c = obj.AddComponent<BoxCollider2D>();
        c.size = new Vector2(0.8f, 0.4f);
        c.offset = new Vector2(0f, -0.2f);

        // A bigorna do lado, com a lamina em brasa (a luz laranja treme como fogo).
        f.bigorna = pe + Vector2.right * 1.3f;
        GameObject b = new GameObject("Bigorna");
        b.transform.SetParent(obj.transform, false);
        b.transform.position = f.bigorna;
        SpriteRenderer sb = b.AddComponent<SpriteRenderer>();
        sb.sprite = ArteDoAntigo.Inteira("Cenario/Bigorna", 32f, true);
        sb.sortingOrder = 10;
        b.layer = Pedreiro.CamadaDaParede;
        BoxCollider2D cb = b.AddComponent<BoxCollider2D>();
        cb.size = new Vector2(1.1f, 0.35f);
        cb.offset = new Vector2(0f, 0.2f);
        Iluminacao.Luz(b.transform, new Vector2(0f, 0.4f), Brasa, 3.2f, 0.9f, 0.12f);
        return f;
    }

    /// <summary>Quanto custa forjar esta arma agora (com o desconto da loja).</summary>
    public int Preco(DadosDaArma arma)
    {
        int raridade = Mathf.Clamp((int)arma.raridade, 0, PrecoDaRaridade.Length - 1);
        int preco = PrecoDaRaridade[raridade] + 10 * arma.forja;
        Transform j = Jogador();
        float desconto = j != null && j.TryGetComponent(out EstatisticasDoJogador e) ? e.Desconto : 1f;
        return Mathf.Max(1, Mathf.CeilToInt(preco * desconto * Melhorias.PrecoNaLoja));
    }

    private void Update()
    {
        if (trabalhando)
            return;

        desenho.sprite = parado[Mathf.FloorToInt(Time.time * 8f) % parado.Length];
        Transform j = Jogador();

        if (j != null)
            desenho.flipX = j.position.x < transform.position.x;
    }

    private Transform Jogador()
    {
        if (jogador == null)
        {
            GameObject j = GameObject.FindWithTag("Player");
            jogador = j != null ? j.transform : null;
        }

        return jogador;
    }

    private ArmaCarregada ArmaNaMao()
    {
        Transform j = Jogador();
        return j != null && j.TryGetComponent(out ArmaDoJogador armas) ? armas.Atual : null;
    }

    public override void Usar(GameObject quem)
    {
        if (trabalhando || !quem.TryGetComponent(out ArmaDoJogador armas) || armas.Atual == null)
            return;

        Vector3 cima = transform.position + Vector3.up * 1.6f;

        if (armas.Atual.Dados.forja >= DadosDaArma.ForjaMaxima)
        {
            Sons.Tocar(Som.Negado, 0.7f, 0f);
            TextoFlutuante.Mostrar(cima, "Essa já está no máximo", new Color(1f, 0.75f, 0.5f), 1.2f);
            return;
        }

        if (!quem.TryGetComponent(out Bolsa bolsa) || !bolsa.Gastar(Preco(armas.Atual.Dados)))
        {
            Sons.Tocar(Som.Negado, 0.7f, 0f);
            TextoFlutuante.Mostrar(cima, "Faltam moedas", new Color(1f, 0.6f, 0.55f), 1.2f);
            return;
        }

        StartCoroutine(Forjar(armas));
    }

    // Ele vira pra bigorna, martela duas vezes (faiscas) e a arma volta pra mao melhor.
    private IEnumerator Forjar(ArmaDoJogador armas)
    {
        trabalhando = true;
        desenho.flipX = false;
        Sons.Tocar(Som.Bigorna, 0.9f, 0.03f);

        Sprite[] golpe = martelando.Length > 0 ? martelando : parado;

        for (int vez = 0; vez < 2; vez++)
        {
            for (int i = 0; i < golpe.Length; i++)
            {
                desenho.sprite = golpe[i];
                yield return new WaitForSeconds(0.045f);
            }

            EfeitoDeFolha.Tocar(ArteDoAntigo.ExplosaoPequena, bigorna + new Vector2(0f, 0.45f), 30f, 25);
            CameraDoJogo.Tremer(0.05f + 0.05f * vez, 0.15f);
        }

        if (armas != null && armas.Forjar())
        {
            DadosDaArma nova = armas.Arma;
            TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.6f, nova.NomeNaTela + " [" + Raridades.Nome(nova.raridade) + "]",
                                   Raridades.Cor(nova.raridade), 2.2f);
            Sons.Tocar(Som.Segredo, 0.7f);
            Conquistas.Contar("forjas", 10, Conquistas.Ferreiro);
        }

        trabalhando = false;
    }
}
