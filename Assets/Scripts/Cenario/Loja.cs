using UnityEngine;

/// <summary>
/// A loja (do jogo antigo, aqui num canto da caverna): o balcao do mercador e tres pedestais a venda,
/// um item e duas coisas miudas (coracao, bomba, chave ou municao). Paga com moedas.
/// </summary>
public static class Loja
{
    /// <param name="balcao">Com o balcao do jogo antigo (a loja da caverna); a do mercador da area segura nao tem.</param>
    public static void Criar(Vector2 onde, Transform pai, EstatisticasDoJogador jogador, bool balcao = true)
    {
        GameObject loja = new GameObject("Loja");
        loja.transform.SetParent(pai, false);
        loja.transform.position = onde;

        if (balcao)
        {
            Enfeite(loja.transform, ArteDoAntigo.Peca("Balcao"), new Vector2(0f, 1.6f), 9);
            Enfeite(loja.transform, ArteDoAntigo.Barril, new Vector2(-2.6f, 1.4f), 9);
            Enfeite(loja.transform, ArteDoAntigo.Caixote, new Vector2(2.6f, 1.3f), 9);
        }

        TextMesh placa = new GameObject("Placa").AddComponent<TextMesh>();
        placa.transform.SetParent(loja.transform, false);
        placa.transform.localPosition = new Vector3(0f, balcao ? 3.3f : 4.3f, 0f);
        placa.text = "Loja";
        placa.font = FonteDoJogo.Texto;
        placa.fontSize = 64;
        placa.characterSize = 0.09f;
        placa.anchor = TextAnchor.MiddleCenter;
        placa.color = new Color(1f, 0.85f, 0.4f);

        if (placa.font != null)
            placa.GetComponent<MeshRenderer>().sharedMaterial = placa.font.material;

        placa.GetComponent<MeshRenderer>().sortingOrder = 30;
        Iluminacao.Brilhar(placa.GetComponent<MeshRenderer>());

        ItemPassivo item = CatalogoDeItens.Sortear(jogador);
        Pedestal.Criar(onde + new Vector2(-1.6f, -0.3f), pai, item, item.Preco);

        Produto[] miudos = { Produto.Coracao, Produto.Bomba, Produto.Chave, Produto.Municao };
        Produto a = miudos[Random.Range(0, miudos.Length)];
        Produto b;

        do
            b = miudos[Random.Range(0, miudos.Length)];
        while (b == a);

        Pedestal.Criar(onde + new Vector2(0f, -0.3f), pai, null, Preco(a), 0f, a);
        Pedestal.Criar(onde + new Vector2(1.6f, -0.3f), pai, null, Preco(b), 0f, b);
    }

    private static int Preco(Produto p) => p == Produto.Coracao ? 4 : p == Produto.Municao ? 6 : 5;

    private static void Enfeite(Transform pai, Sprite desenho, Vector2 onde, int ordem)
    {
        if (desenho == null)
            return;

        GameObject obj = new GameObject(desenho.name);
        obj.transform.SetParent(pai, false);
        obj.transform.localPosition = onde;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = desenho;
        sr.sortingOrder = ordem;

        // O balcao e os barris seguram tiro e ninguem atravessa.
        BoxCollider2D c = obj.AddComponent<BoxCollider2D>();
        c.size = new Vector2(desenho.bounds.size.x * 0.9f, 0.6f);
        c.offset = new Vector2(0f, 0.3f);
        obj.layer = Pedreiro.CamadaDaParede;
    }
}
