using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O que vai espalhado na caverna alem dos inimigos e das armas (as salas especiais e o cenario vivo do
/// jogo antigo, adaptados pra caverna): loja, altar de sangue, desafio, emboscada, baus trancado e
/// amaldicoado, pedestal de tesouro, pedra rachada com premio, area escura e as armadilhas (serra no
/// trilho, tronco rolante, espinhos), alem de mesas, barris, baratas e velas.
///
/// Cada coisa sai sorteada por andar; as que ocupam lugar saem do chao do mapa de caminhos (os
/// inimigos contornam e ninguem nasce em cima).
/// </summary>
public static class RecheioDaCaverna
{
    public static void Espalhar(GeradorDoAndar gerador, HashSet<Vector2Int> chao, Transform pai, int andar, int mundo,
                                Sprite[] quadrosDoBau)
    {
        List<Vector2> usados = new List<Vector2> { Vector2.zero };
        GameObject jogador = GameObject.FindWithTag("Player");
        EstatisticasDoJogador itens = jogador != null ? jogador.GetComponent<EstatisticasDoJogador>() : null;

        // ---------------- os lugares especiais ----------------
        if (Random.value < 0.6f && Achar(chao, usados, 3, 2, 12f, out Vector2 loja))
        {
            Loja.Criar(loja, pai, itens);
            Ocupar(chao, loja + Vector2.up * 1.5f, 3, 1);
            Ocupar(chao, loja + Vector2.down * 0.3f, 2, 0);
        }

        if (Random.value < 0.3f && Achar(chao, usados, 2, 2, 12f, out Vector2 altar))
        {
            AltarDeSangue.Criar(altar, pai, itens);
            Ocupar(chao, altar, 0, 0);
        }

        if (Random.value < 0.3f && Achar(chao, usados, 2, 2, 14f, out Vector2 desafio))
        {
            Emboscada.Criar(desafio, pai, gerador, true, quadrosDoBau);
            Ocupar(chao, desafio, 0, 0);
        }

        if (Random.value < 0.45f && Achar(chao, usados, 2, 2, 14f, out Vector2 emboscada))
            Emboscada.Criar(emboscada, pai, gerador, false, quadrosDoBau);

        if (quadrosDoBau != null && quadrosDoBau.Length > 0)
        {
            if (Random.value < 0.5f && Achar(chao, usados, 1, 1, 10f, out Vector2 trancado))
            {
                Bau.Criar(quadrosDoBau, trancado, pai, gerador.ArmasDoBau, null, null, null).ComoTipo(TipoDeBau.Trancado);
                Ocupar(chao, trancado, 0, 0);
            }

            if (Random.value < 0.25f && Achar(chao, usados, 1, 1, 10f, out Vector2 maldito))
            {
                Bau.Criar(quadrosDoBau, maldito, pai, gerador.ArmasDoBau, null, null, null).ComoTipo(TipoDeBau.Amaldicoado);
                Ocupar(chao, maldito, 0, 0);
            }
        }

        if (Random.value < 0.3f && Achar(chao, usados, 1, 1, 12f, out Vector2 tesouro))
        {
            Pedestal.Criar(tesouro, pai, CatalogoDeItens.Sortear(itens));
            Ocupar(chao, tesouro, 0, 0);
        }

        // Pedra rachada (so bomba quebra) guardando um premio.
        int pedras = Random.Range(1, 3);

        for (int i = 0; i < pedras; i++)
        {
            if (!Achar(chao, usados, 1, 1, 9f, out Vector2 pedra))
                break;

            Quebravel.PedraRachada(pedra, pai, onde => Premio(onde, pai, gerador, quadrosDoBau, itens));
            Ocupar(chao, pedra, 1, 0);
        }

        if (Random.value < 0.35f && Achar(chao, usados, 1, 1, 16f, out Vector2 escuro))
            AreaEscura.Criar(escuro, 7f, pai);

        // ---------------- armadilhas ----------------
        int serras = Random.Range(0, 3);

        for (int i = 0; i < serras; i++)
        {
            if (Reta(chao, usados, out Vector2 de, out Vector2 ate))
                SerraNoTrilho.Criar(de, ate, pai);
        }

        if (Random.value < 0.5f && Reta(chao, usados, out Vector2 troncoDe, out Vector2 troncoAte))
            TroncoRolante.Criar(troncoDe, troncoAte, pai);

        int espinhos = Random.Range(1, 4);

        for (int i = 0; i < espinhos; i++)
        {
            if (!Achar(chao, usados, 1, 1, 9f, out Vector2 canto))
                break;

            float fase = Random.value * 3f;

            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                    EspinhosQueSaem.Criar(canto + new Vector2(x, y), pai, fase);
        }

        // ---------------- cenario ----------------
        int moveis = Random.Range(5, 10);

        for (int i = 0; i < moveis; i++)
        {
            if (!Achar(chao, usados, 1, 1, 4f, out Vector2 onde))
                break;

            float qual = Random.value;

            if (qual < 0.4f)
                Quebravel.Mesa(onde, pai);
            else if (qual < 0.7f)
                Quebravel.Barril(onde, pai);
            else
                Quebravel.Caixote(onde, pai);

            Ocupar(chao, onde, 0, 0);
        }

        for (int i = 0; i < Random.Range(1, 4); i++)
        {
            if (Achar(chao, usados, 1, 1, 4f, out Vector2 onde, false))
                Baratas.Criar(onde, pai, chao);
        }

        // Velas: mais na Cripta (mundo 3).
        int velas = mundo == 3 ? Random.Range(8, 14) : Random.Range(2, 6);

        for (int i = 0; i < velas; i++)
        {
            if (Achar(chao, usados, 0, 0, 2.5f, out Vector2 onde, false))
                Vela.Criar(onde + new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f)), pai);
        }
    }

    // O premio da pedra rachada: moedas, ou chave e bomba, ou um bau.
    private static void Premio(Vector2 onde, Transform pai, GeradorDoAndar gerador, Sprite[] quadrosDoBau, EstatisticasDoJogador itens)
    {
        float sorte = Random.value;

        if (sorte < 0.4f)
        {
            for (int i = 0; i < 5; i++)
                Coletavel.Criar(TipoDeColetavel.Moeda, onde);
        }
        else if (sorte < 0.7f)
        {
            Coletavel.Criar(TipoDeColetavel.Chave, onde);
            Coletavel.Criar(TipoDeColetavel.Bomba, onde);
        }
        else if (quadrosDoBau != null && quadrosDoBau.Length > 0)
        {
            Bau.Criar(quadrosDoBau, onde, pai, gerador.ArmasDoBau, null, null, null);
        }
        else
        {
            Pedestal.Criar(onde, pai, CatalogoDeItens.Sortear(itens));
        }
    }

    /// <summary>
    /// Um lugar livre: um retangulo de chao de (2*mx+1) x (2*my+1) celulas em volta, longe do comeco e
    /// a <paramref name="espaco"/> dos outros.
    /// </summary>
    private static bool Achar(HashSet<Vector2Int> chao, List<Vector2> usados, int mx, int my, float espaco, out Vector2 onde, bool marcar = true)
    {
        // Junta todos os lugares que servem e sorteia um (sortear as cegas quase nunca achava os saloes grandes).
        List<Vector2Int> servem = new List<Vector2Int>();

        foreach (Vector2Int c in chao)
        {
            if (((Vector2)c).magnitude >= 9f && Livre(chao, c, mx + 1, my + 1) && usados.TrueForAll(u => Vector2.Distance(u, c) >= espaco))
                servem.Add(c);
        }

        if (servem.Count == 0)
        {
            onde = default;
            return false;
        }

        onde = servem[Random.Range(0, servem.Count)];

        if (marcar)
            usados.Add(onde);

        return true;
    }

    private static bool Livre(HashSet<Vector2Int> chao, Vector2Int c, int mx, int my)
    {
        for (int x = -mx; x <= mx; x++)
            for (int y = -my; y <= my; y++)
                if (!chao.Contains(c + new Vector2Int(x, y)))
                    return false;

        return true;
    }

    private static void Ocupar(HashSet<Vector2Int> chao, Vector2 centro, int mx, int my)
    {
        Vector2Int c = MapaDeCaminhos.Celula(centro);

        for (int x = -mx; x <= mx; x++)
            for (int y = -my; y <= my; y++)
                chao.Remove(c + new Vector2Int(x, y));
    }

    // Um corredor reto (de 6 a 9 celulas, com chao dos dois lados) pra serra ou o tronco.
    private static bool Reta(HashSet<Vector2Int> chao, List<Vector2> usados, out Vector2 de, out Vector2 ate)
    {
        List<Vector2Int> celulas = new List<Vector2Int>(chao);

        for (int tentativa = 0; tentativa < 120 && celulas.Count > 0; tentativa++)
        {
            Vector2Int c = celulas[Random.Range(0, celulas.Count)];

            if (((Vector2)c).magnitude < 10f || !usados.TrueForAll(u => Vector2.Distance(u, c) >= 7f))
                continue;

            bool deitado = Random.value < 0.5f;
            Vector2Int passo = deitado ? Vector2Int.right : Vector2Int.up;
            Vector2Int lado = deitado ? Vector2Int.up : Vector2Int.right;
            int n = 0;

            while (n < 9 && chao.Contains(c + passo * n) && chao.Contains(c + passo * n + lado) && chao.Contains(c + passo * n - lado))
                n++;

            if (n < 6)
                continue;

            de = c;
            ate = c + passo * (n - 1);
            usados.Add((de + ate) * 0.5f);
            return true;
        }

        de = ate = default;
        return false;
    }
}
