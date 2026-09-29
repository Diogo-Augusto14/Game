using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A loja do andar, como no Isaac: um item passivo e tres coletaveis a venda, cada um com
/// o preco embaixo. Encostou com moeda suficiente, comprou. Sem moeda, o preco pisca
/// vermelho. Monte por <see cref="Montar"/> numa sala vazia.
/// </summary>
public static class Loja
{
    public const int PrecoDoItem = 15;
    public const int PrecoDoCoracao = 3;
    public const int PrecoDaBomba = 5;
    public const int PrecoDaChave = 5;

    public static void Montar(Transform sala, ICollection<ItemPassivo> itensQueJaSairam)
    {
        Vector2 centro = sala.position;

        ProdutoDaLoja.Criar(CatalogoDeItens.Sortear(itensQueJaSairam), PrecoDoItem, centro + new Vector2(-3f, 0.5f), sala);
        ProdutoDaLoja.Criar(TipoDeColetavel.Coracao, PrecoDoCoracao, centro + new Vector2(-1f, 0.5f), sala);
        ProdutoDaLoja.Criar(TipoDeColetavel.Bomba, PrecoDaBomba, centro + new Vector2(1f, 0.5f), sala);
        ProdutoDaLoja.Criar(TipoDeColetavel.Chave, PrecoDaChave, centro + new Vector2(3f, 0.5f), sala);

        // O vendedor, no alto da sala: so enfeite.
        Transform vendedor = new GameObject("Vendedor").transform;
        vendedor.SetParent(sala, false);
        vendedor.position = centro + new Vector2(0f, 2.3f);
        // O bruxo do Tiny RPG faz de vendedor, parado atras do balcao.
        ClipesDePersonagem bruxo = ArteImportada.Personagem("Bruxo", 26f);

        if (bruxo != null)
        {
            EfeitoDeQuadros.Criar(bruxo.Parado, 8f, vendedor.position, 8, vendedor)?.EmLoop();
            FormasDaSala.Desenho(vendedor, "Balcao", ArteImportada.Objeto(0, 2), Color.white, new Vector2(0f, -0.55f), Vector2.one * 1.3f, 9);
            return;
        }

        FormasDaSala.Desenho(vendedor, "Corpo", FormasDaSala.Circulo(), new Color(0.35f, 0.3f, 0.45f), Vector2.zero, Vector2.one * 0.8f, 8);
        FormasDaSala.Desenho(vendedor, "Chapeu", FormasDaSala.Quadrado(), new Color(0.2f, 0.15f, 0.25f), new Vector2(0f, 0.4f), new Vector2(0.9f, 0.2f), 9);

        for (int lado = -1; lado <= 1; lado += 2)
            FormasDaSala.Desenho(vendedor, "Olho", FormasDaSala.Circulo(), new Color(1f, 0.9f, 0.4f), new Vector2(lado * 0.15f, 0.08f), Vector2.one * 0.14f, 9);
    }
}
