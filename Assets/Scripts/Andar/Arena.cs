using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A planta do andar do chefe: um salao oval, sem caverna em volta, com quatro pilares pra se
/// esconder dos tiros. O jogador entra pela parte de baixo (no ponto 0, 0, como nas cavernas) e o
/// chefe fica no meio de cima.
///
/// Sai no mesmo formato da <see cref="Caverna"/> (as celulas de chao), entao o
/// <see cref="Pedreiro"/> constroi igual e o <see cref="MapaDeCaminhos"/> funciona igual.
/// </summary>
public static class Arena
{
    /// <summary>Onde o chefe aparece.</summary>
    public static Vector2Int OndeOChefeFica(Vector2Int raio) => new Vector2Int(0, Centro(raio).y + raio.y / 3);

    /// <summary>
    /// Monta a planta: um oval com estes raios (em celulas) e os pilares, ja ajeitada pras paredes do
    /// Old Prison.
    /// </summary>
    public static HashSet<Vector2Int> Montar(Vector2Int raio)
    {
        HashSet<Vector2Int> chao = new HashSet<Vector2Int>();
        Vector2Int centro = Centro(raio);

        for (int x = -raio.x; x <= raio.x; x++)
        {
            for (int y = -raio.y; y <= raio.y; y++)
            {
                float dx = x / (raio.x + 0.5f), dy = y / (raio.y + 0.5f);

                if (dx * dx + dy * dy <= 1f)
                    chao.Add(centro + new Vector2Int(x, y));
            }
        }

        // Quatro pilares de 3 x 4 (a altura minima das paredes do pacote e 3), nos cantos do salao,
        // longe da entrada e do chefe.
        int px = Mathf.Max(3, raio.x / 2);
        int py = Mathf.Max(3, raio.y / 2);

        foreach (Vector2Int canto in new[] { new Vector2Int(-px, -py), new Vector2Int(px, -py), new Vector2Int(-px, py), new Vector2Int(px, py) })
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -2; y <= 1; y++)
                    chao.Remove(centro + canto + new Vector2Int(x, y));
            }
        }

        Caverna.Ajeitar(chao);
        return chao;
    }

    // A entrada (0, 0) fica 2 celulas acima da beirada de baixo.
    private static Vector2Int Centro(Vector2Int raio) => new Vector2Int(0, raio.y - 2);
}
