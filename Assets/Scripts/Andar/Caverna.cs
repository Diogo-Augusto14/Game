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
///
/// Depois de cavar, <see cref="Ajeitar"/> deixa a planta no jeito que as paredes do Old Prison sabem
/// desenhar, e <see cref="AbrirBuracos"/> e <see cref="EspalharPocas"/> escolhem onde ficam os buracos
/// de abismo e as pocas de sangue.
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

                // Corredor de 3 ou 4 (2 ficava apertado demais pra desviar de tiro), as vezes uma galeria de 6.
                int largura = Random.value < 0.03f ? 6 : Random.value < 0.5f ? 3 : 4;
                Cavar(chao, a.onde, largura);

                // Vira de vez em quando (pouco: assim sai corredor comprido, e nao um bolo no comeco).
                float sorte = Random.value;

                if (sorte < 0.12f)
                    a.rumo = Girar(a.rumo, Random.value < 0.5f ? 1 : 3);
                else if (sorte < 0.14f)
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

    /// <summary>
    /// Ajeita a planta pras paredes do Old Prison, abrindo chao (nunca fechando, pra nada ficar isolado):
    ///
    ///   parede com menos de 3 celulas de altura entre dois chaos vira chao (a parede tem um topo e
    ///   uma face de 2 de altura: menos que 3 nao cabe);
    ///   parede ou topo de parede que so se encosta pela diagonal ganha uma abertura (o pacote nao tem
    ///   desenho pra esse encontro).
    ///
    /// Repete ate nao ter mais o que abrir.
    /// </summary>
    public static void Ajeitar(HashSet<Vector2Int> chao)
    {
        List<Vector2Int> abrir = new List<Vector2Int>();

        for (int volta = 0; volta < 200; volta++)
        {
            abrir.Clear();
            RectInt limites = Limites(chao);

            for (int x = limites.xMin; x < limites.xMax; x++)
            {
                int y = limites.yMin;

                while (y < limites.yMax)
                {
                    if (chao.Contains(new Vector2Int(x, y)))
                    {
                        y++;
                        continue;
                    }

                    int comeco = y;

                    while (y < limites.yMax && !chao.Contains(new Vector2Int(x, y)))
                        y++;

                    // So a parede entre dois chaos (a que encosta na beirada continua pra fora).
                    if (comeco > limites.yMin && y < limites.yMax && y - comeco < 3)
                    {
                        for (int k = comeco; k < y; k++)
                            abrir.Add(new Vector2Int(x, k));
                    }
                }
            }

            for (int x = limites.xMin - 3; x < limites.xMax + 3; x++)
            {
                for (int y = limites.yMin - 3; y < limites.yMax + 3; y++)
                {
                    bool a = Parede(chao, x, y), b = Parede(chao, x + 1, y);
                    bool c = Parede(chao, x, y + 1), d = Parede(chao, x + 1, y + 1);

                    if (a && d && !b && !c)
                        abrir.Add(new Vector2Int(x, y));
                    else if (b && c && !a && !d)
                        abrir.Add(new Vector2Int(x + 1, y));

                    // O mesmo no topo das paredes: abre embaixo da coluna de topo mais alta.
                    a = Topo(chao, x, y);
                    b = Topo(chao, x + 1, y);
                    c = Topo(chao, x, y + 1);
                    d = Topo(chao, x + 1, y + 1);

                    if (c && b && !a && !d)
                        abrir.Add(new Vector2Int(x, y - 1));
                    else if (a && d && !b && !c)
                        abrir.Add(new Vector2Int(x + 1, y - 1));
                }
            }

            if (abrir.Count == 0)
                return;

            foreach (Vector2Int celula in abrir)
                chao.Add(celula);
        }
    }

    /// <summary>Nao e chao: parede.</summary>
    public static bool Parede(HashSet<Vector2Int> chao, int x, int y) => !chao.Contains(new Vector2Int(x, y));

    /// <summary>
    /// O topo de uma parede (a parte de cima, vista de cima): parede com mais 2 de parede embaixo. As
    /// duas de baixo ficam pra face de tijolo, que se ve de frente.
    /// </summary>
    public static bool Topo(HashSet<Vector2Int> chao, int x, int y) =>
        Parede(chao, x, y) && Parede(chao, x, y - 1) && Parede(chao, x, y - 2);

    /// <summary>
    /// Escolhe buracos de abismo: retangulos de chao (de 3 a 6 de largura e 3 a 5 de altura) nas
    /// partes abertas da caverna, longe do comeco, com chao em volta (pra nunca cortar um caminho).
    /// As celulas continuam na planta; quem as separa e quem constroi.
    /// </summary>
    public static HashSet<Vector2Int> AbrirBuracos(HashSet<Vector2Int> chao, int quantos, float longeDoComeco)
    {
        HashSet<Vector2Int> buracos = new HashSet<Vector2Int>();
        List<Vector2Int> celulas = new List<Vector2Int>(chao);
        int feitos = 0;

        for (int tentativa = 0; feitos < quantos && tentativa < quantos * 60; tentativa++)
        {
            Vector2Int canto = celulas[Random.Range(0, celulas.Count)];
            Vector2Int tamanho = new Vector2Int(Random.Range(3, 7), Random.Range(3, 6));

            if (((Vector2)canto).magnitude < longeDoComeco || !CabeBuraco(chao, buracos, canto, tamanho))
                continue;

            for (int x = 0; x < tamanho.x; x++)
                for (int y = 0; y < tamanho.y; y++)
                    buracos.Add(canto + new Vector2Int(x, y));

            feitos++;
        }

        return buracos;
    }

    // O retangulo e 2 celulas em volta sao chao, sem outro buraco.
    private static bool CabeBuraco(HashSet<Vector2Int> chao, HashSet<Vector2Int> buracos, Vector2Int canto, Vector2Int tamanho)
    {
        for (int x = -2; x < tamanho.x + 2; x++)
        {
            for (int y = -2; y < tamanho.y + 2; y++)
            {
                Vector2Int c = canto + new Vector2Int(x, y);

                if (!chao.Contains(c) || buracos.Contains(c))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Escolhe pocas de sangue: manchas redondas no chao andavel, longe das paredes e dos buracos. So
    /// enfeite: ninguem escorrega nem se machuca.
    /// </summary>
    public static HashSet<Vector2Int> EspalharPocas(HashSet<Vector2Int> chao, HashSet<Vector2Int> buracos, int quantas)
    {
        HashSet<Vector2Int> pocas = new HashSet<Vector2Int>();
        List<Vector2Int> celulas = new List<Vector2Int>(chao);

        for (int i = 0; i < quantas; i++)
        {
            Vector2Int centro = celulas[Random.Range(0, celulas.Count)];
            Vector2 raio = new Vector2(Random.Range(1.2f, 3f), Random.Range(1f, 2.4f));

            for (int x = -3; x <= 3; x++)
            {
                for (int y = -3; y <= 3; y++)
                {
                    Vector2Int c = centro + new Vector2Int(x, y);

                    if ((x / raio.x) * (x / raio.x) + (y / raio.y) * (y / raio.y) <= 1f && LongeDaBeirada(chao, buracos, c))
                        pocas.Add(c);
                }
            }
        }

        // O desenho da poca tambem nao tem o encontro pela diagonal: completa.
        List<Vector2Int> completar = new List<Vector2Int>();

        foreach (Vector2Int c in pocas)
        {
            Vector2Int direita = c + Vector2Int.right, cima = c + Vector2Int.up, diagonal = c + Vector2Int.one;

            if (pocas.Contains(diagonal) && !pocas.Contains(direita) && !pocas.Contains(cima))
                completar.Add(direita);

            Vector2Int esquerda = c + Vector2Int.left, diagonalEsquerda = c + new Vector2Int(-1, 1);

            if (pocas.Contains(diagonalEsquerda) && !pocas.Contains(esquerda) && !pocas.Contains(cima))
                completar.Add(cima);
        }

        foreach (Vector2Int c in completar)
            pocas.Add(c);

        return pocas;
    }

    private static bool LongeDaBeirada(HashSet<Vector2Int> chao, HashSet<Vector2Int> buracos, Vector2Int c)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int v = c + new Vector2Int(x, y);

                if (!chao.Contains(v) || buracos.Contains(v))
                    return false;
            }
        }

        return true;
    }

    /// <summary>O menor retangulo de celulas que cobre todas.</summary>
    public static RectInt Limites(HashSet<Vector2Int> celulas)
    {
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;

        foreach (Vector2Int c in celulas)
        {
            x0 = Mathf.Min(x0, c.x);
            y0 = Mathf.Min(y0, c.y);
            x1 = Mathf.Max(x1, c.x);
            y1 = Mathf.Max(y1, c.y);
        }

        return new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
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
