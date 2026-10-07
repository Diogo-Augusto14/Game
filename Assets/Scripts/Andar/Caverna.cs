using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cava a planta de uma caverna: um conjunto de celulas de chao (1 unidade cada), todas ligadas.
///
/// Como no Nuclear Throne, quem cava sao "andarilhos": comecam no centro, andam uma celula por vez,
/// as vezes viram, as vezes se dividem em dois ou somem, e de vez em quando abrem uma galeria larga.
/// Cavam ate a caverna ter o tamanho pedido. Como todo andarilho nasce num lugar ja cavado, a
/// caverna sai inteira ligada, sem pedaco solto.
///
/// O comeco (em volta do centro do mundo) e sempre uma clareira aberta, onde o jogador nasce.
/// </summary>
public static class Caverna
{
    private static readonly Vector2Int[] Direcoes = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    private class Andarilho
    {
        public Vector2Int onde;
        public Vector2Int rumo;
    }

    /// <param name="celulas">Quantas celulas de chao a caverna tera (mais ou menos).</param>
    /// <param name="raio">A caverna nao passa desta distancia do centro, em celulas.</param>
    /// <param name="clareira">Metade da largura e da altura da clareira do comeco, em celulas.</param>
    public static HashSet<Vector2Int> Cavar(int celulas, int raio, Vector2Int clareira)
    {
        HashSet<Vector2Int> chao = new HashSet<Vector2Int>();

        for (int x = -clareira.x; x <= clareira.x; x++)
            for (int y = -clareira.y; y <= clareira.y; y++)
                chao.Add(new Vector2Int(x, y));

        List<Andarilho> andarilhos = new List<Andarilho> { Novo(Vector2Int.zero) };

        for (int passo = 0; chao.Count < celulas && passo < celulas * 40; passo++)
        {
            for (int i = andarilhos.Count - 1; i >= 0; i--)
            {
                Andarilho a = andarilhos[i];

                // Quase sempre um corredor de 2; as vezes so 1 (passagem estreita) ou uma galeria de 5.
                float sorte = Random.value;
                int largura = sorte < 0.02f ? 5 : sorte < 0.08f ? 1 : 2;
                Cavar(chao, a.onde, largura);

                // Vira de vez em quando (pouco: assim sai corredor comprido, e nao um bolo no comeco).
                sorte = Random.value;

                if (sorte < 0.14f)
                    a.rumo = Girar(a.rumo, Random.value < 0.5f ? 1 : 3);
                else if (sorte < 0.16f)
                    a.rumo = -a.rumo;

                // Se divide ou some (nunca fica sem nenhum, nem com muitos).
                if (andarilhos.Count < 6 && Random.value < 0.04f)
                    andarilhos.Add(Novo(a.onde));
                else if (andarilhos.Count > 1 && Random.value < 0.03f)
                {
                    andarilhos.RemoveAt(i);
                    continue;
                }

                Vector2Int proximo = a.onde + a.rumo;

                // Na beirada, volta pra dentro.
                if (Mathf.Abs(proximo.x) > raio || Mathf.Abs(proximo.y) > raio)
                {
                    a.rumo = -a.rumo;
                    proximo = a.onde + a.rumo;
                }

                a.onde = proximo;
            }
        }

        return chao;
    }

    private static Andarilho Novo(Vector2Int onde) =>
        new Andarilho { onde = onde, rumo = Direcoes[Random.Range(0, Direcoes.Length)] };

    // Quarto de volta (1 = sentido horario, 3 = anti-horario).
    private static Vector2Int Girar(Vector2Int rumo, int quartos)
    {
        for (int i = 0; i < quartos; i++)
            rumo = new Vector2Int(rumo.y, -rumo.x);

        return rumo;
    }

    private static void Cavar(HashSet<Vector2Int> chao, Vector2Int centro, int largura)
    {
        int de = -(largura - 1) / 2;

        for (int x = de; x < de + largura; x++)
            for (int y = de; y < de + largura; y++)
                chao.Add(centro + new Vector2Int(x, y));
    }

    /// <summary>
    /// Junta um conjunto de celulas em poucos retangulos (cada celula num retangulo so): cresce pro
    /// lado o quanto da e depois pra cima, enquanto a fileira inteira couber.
    /// </summary>
    public static List<RectInt> Juntar(HashSet<Vector2Int> celulas)
    {
        List<RectInt> retangulos = new List<RectInt>();
        HashSet<Vector2Int> usadas = new HashSet<Vector2Int>();
        List<Vector2Int> ordem = new List<Vector2Int>(celulas);
        ordem.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

        foreach (Vector2Int inicio in ordem)
        {
            if (usadas.Contains(inicio))
                continue;

            int largura = 1;

            while (Livre(celulas, usadas, inicio + new Vector2Int(largura, 0)))
                largura++;

            int altura = 1;

            while (FileiraLivre(celulas, usadas, inicio.x, inicio.y + altura, largura))
                altura++;

            for (int x = 0; x < largura; x++)
                for (int y = 0; y < altura; y++)
                    usadas.Add(new Vector2Int(inicio.x + x, inicio.y + y));

            retangulos.Add(new RectInt(inicio.x, inicio.y, largura, altura));
        }

        return retangulos;
    }

    private static bool Livre(HashSet<Vector2Int> celulas, HashSet<Vector2Int> usadas, Vector2Int c) =>
        celulas.Contains(c) && !usadas.Contains(c);

    private static bool FileiraLivre(HashSet<Vector2Int> celulas, HashSet<Vector2Int> usadas, int x0, int y, int largura)
    {
        for (int x = x0; x < x0 + largura; x++)
        {
            if (!Livre(celulas, usadas, new Vector2Int(x, y)))
                return false;
        }

        return true;
    }
}
