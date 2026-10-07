using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O caminho de qualquer lugar da caverna ate o jogador, contornando paredes e buracos. Os inimigos
/// que nao enxergam o jogador seguem por aqui em vez de empacar na parede.
///
/// E um "mapa de distancias": partindo da celula do jogador, cada celula de chao recebe quantos
/// passos ela esta dele (uma busca em largura, que espalha como agua). Pra chegar no jogador, basta
/// ir pra vizinha com menos passos. So recalcula quando o jogador muda de celula, e so ate uma
/// distancia maxima (quem esta mais longe que isso fica parado).
///
/// Quem cria e o <see cref="GeradorDoAndar"/>, a cada andar; os inimigos perguntam em <see cref="Rumo"/>.
/// </summary>
public class MapaDeCaminhos
{
    // Andando na diagonal, so se as duas vizinhas retas forem chao (senao raspa na quina da parede).
    private static readonly Vector2Int[] Vizinhas =
    {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left,
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 1),
    };

    /// <summary>O mapa do andar atual (nulo entre andares).</summary>
    public static MapaDeCaminhos Atual { get; set; }

    private readonly HashSet<Vector2Int> chao;
    private readonly int distanciaMaxima;
    private readonly Dictionary<Vector2Int, int> passos = new Dictionary<Vector2Int, int>();
    private readonly Queue<Vector2Int> fila = new Queue<Vector2Int>();
    private Vector2Int origem = new Vector2Int(int.MinValue, int.MinValue);

    /// <param name="chao">As celulas onde da pra pisar (sem os buracos).</param>
    /// <param name="distanciaMaxima">Ate quantos passos do jogador o mapa vai.</param>
    public MapaDeCaminhos(HashSet<Vector2Int> chao, int distanciaMaxima)
    {
        this.chao = chao;
        this.distanciaMaxima = distanciaMaxima;
    }

    public static Vector2Int Celula(Vector2 ponto) => new Vector2Int(Mathf.RoundToInt(ponto.x), Mathf.RoundToInt(ponto.y));

    /// <summary>Tem chao de pisar neste ponto (dentro do andar e fora dos buracos)?</summary>
    public bool TemChao(Vector2 ponto) => chao.Contains(Celula(ponto));

    /// <summary>Refaz o mapa se o jogador mudou de celula.</summary>
    public void Atualizar(Vector2 jogador)
    {
        Vector2Int celula = Celula(jogador);

        if (celula == origem || !chao.Contains(celula))
            return;

        origem = celula;
        passos.Clear();
        fila.Clear();
        passos[celula] = 0;
        fila.Enqueue(celula);

        while (fila.Count > 0)
        {
            Vector2Int atual = fila.Dequeue();
            int distancia = passos[atual];

            if (distancia >= distanciaMaxima)
                continue;

            foreach (Vector2Int passo in Vizinhas)
            {
                Vector2Int proxima = atual + passo;

                if (passos.ContainsKey(proxima) || !Pisavel(atual, passo))
                    continue;

                passos[proxima] = distancia + 1;
                fila.Enqueue(proxima);
            }
        }
    }

    /// <summary>
    /// Pra onde andar, saindo de <paramref name="de"/>, pra chegar no jogador: a direcao do centro da
    /// vizinha mais perto dele (tamanho 1). Zero se nao tem caminho (longe demais, ou fora do chao).
    /// </summary>
    public Vector2 Rumo(Vector2 de)
    {
        Vector2Int celula = Celula(de);

        if (!passos.TryGetValue(celula, out int aqui))
        {
            // Raspando na beirada (o centro do corpo caiu numa celula de parede): volta pra vizinha mais perto.
            celula = VizinhaConhecida(de);

            if (!passos.TryGetValue(celula, out aqui))
                return Vector2.zero;

            return ((Vector2)celula - de).normalized;
        }

        Vector2Int melhor = celula;
        int menor = aqui;

        foreach (Vector2Int passo in Vizinhas)
        {
            Vector2Int vizinha = celula + passo;

            if (passos.TryGetValue(vizinha, out int d) && d < menor && Pisavel(celula, passo))
            {
                menor = d;
                melhor = vizinha;
            }
        }

        if (melhor == celula)
            return Vector2.zero;

        Vector2 ate = (Vector2)melhor - de;
        return ate.sqrMagnitude > 0.0001f ? ate.normalized : Vector2.zero;
    }

    private bool Pisavel(Vector2Int de, Vector2Int passo)
    {
        if (!chao.Contains(de + passo))
            return false;

        // Diagonal: as duas retas do lado tambem precisam ser chao.
        return passo.x == 0 || passo.y == 0
            || (chao.Contains(de + new Vector2Int(passo.x, 0)) && chao.Contains(de + new Vector2Int(0, passo.y)));
    }

    private Vector2Int VizinhaConhecida(Vector2 de)
    {
        Vector2Int centro = Celula(de);
        Vector2Int melhor = centro;
        float menor = float.MaxValue;

        foreach (Vector2Int passo in Vizinhas)
        {
            Vector2Int v = centro + passo;

            if (passos.ContainsKey(v) && ((Vector2)v - de).sqrMagnitude < menor)
            {
                menor = ((Vector2)v - de).sqrMagnitude;
                melhor = v;
            }
        }

        return melhor;
    }
}
