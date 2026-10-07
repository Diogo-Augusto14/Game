using UnityEngine;

/// <summary>
/// O altar de sangue (do jogo antigo): um item em cima, que se paga com vida (um coracao). Velas em volta.
/// </summary>
public static class AltarDeSangue
{
    public static void Criar(Vector2 onde, Transform pai, EstatisticasDoJogador jogador)
    {
        GameObject altar = new GameObject("Altar de sangue");
        altar.transform.SetParent(pai, false);
        altar.transform.position = onde + Vector2.up * 0.3f;
        SpriteRenderer sr = altar.AddComponent<SpriteRenderer>();
        sr.sprite = ArteDoAntigo.Peca("Altar");
        sr.sortingOrder = 8;
        sr.color = new Color(1f, 0.75f, 0.75f);

        Pedestal.Criar(onde + Vector2.up * 0.6f, pai, CatalogoDeItens.Sortear(jogador), 0, 2f, Produto.Item, false);

        for (int i = 0; i < 4; i++)
            Vela.Criar(onde + (Vector2)(Quaternion.Euler(0f, 0f, 45f + 90f * i) * Vector2.right) * 1.7f, pai);
    }
}
