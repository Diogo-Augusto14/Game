using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A loja do andar, como no Isaac: dois itens passivos e tres coletaveis a venda, cada um
/// com o preco embaixo e uma etiqueta com o nome e o efeito quando o jogador chega perto.
/// Encostou com moeda suficiente, comprou. Sem moeda, o preco pisca vermelho. Atras do
/// balcao fica o <see cref="Comerciante"/>. Monte por <see cref="Montar"/> numa sala vazia.
/// </summary>
public static class Loja
{
    public const int PrecoDoItem = 15;
    public const int PrecoDoCoracao = 3;
    public const int PrecoDaBomba = 5;
    public const int PrecoDaChave = 5;

    /// <summary>
    /// Fracao descontada de todo preco (item Bolsa do Mercador). Quem ajusta e o
    /// <see cref="EfeitosDosItens"/>, que volta pra 0 quando o jogador some.
    /// </summary>
    public static float Desconto;

    /// <summary>O preco com o desconto dos itens aplicado; nunca menos de 1 moeda.</summary>
    public static int ComDesconto(int preco)
        => Mathf.Max(1, Mathf.RoundToInt(preco * (1f - Mathf.Clamp01(Desconto))));

    public static void Montar(Transform sala, ICollection<ItemPassivo> itensQueJaSairam)
    {
        Vector2 centro = sala.position;
        const float linha = -0.4f;

        ProdutoDaLoja.Criar(CatalogoDeItens.Sortear(itensQueJaSairam), PrecoDoItem, centro + new Vector2(-4f, linha), sala);
        ProdutoDaLoja.Criar(CatalogoDeItens.Sortear(itensQueJaSairam), PrecoDoItem, centro + new Vector2(-2f, linha), sala);
        ProdutoDaLoja.Criar(TipoDeColetavel.Coracao, PrecoDoCoracao, centro + new Vector2(0f, linha), sala);
        ProdutoDaLoja.Criar(TipoDeColetavel.Bomba, PrecoDaBomba, centro + new Vector2(2f, linha), sala);
        ProdutoDaLoja.Criar(TipoDeColetavel.Chave, PrecoDaChave, centro + new Vector2(4f, linha), sala);

        // O comerciante, no alto da sala, atras do balcao.
        Comerciante.Criar(centro + new Vector2(0f, 1.9f), sala);
    }
}
