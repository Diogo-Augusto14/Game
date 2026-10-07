using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Uma regra de encaixe do Tiled ("automapping"): se as celulas em volta de um lugar forem como a
/// entrada pede, escreve a saida. As regras do pacote Old Prison ficam em <see cref="DadosDoOldPrison"/>.
///
/// Entrada: grupos de 4 numeros (coluna, linha, tipo, ladrilho), relativos ao canto de cima da regra.
/// Saida: grupos de 3 (coluna, linha, ladrilho; -1 apaga). As linhas crescem pra baixo, como no Tiled.
/// </summary>
public class Regra
{
    public readonly float chance;
    public readonly int[] entrada;
    public readonly int[] saida;

    // Os ladrilhos que a entrada pede: "Outro" e qualquer coisa fora deles (inclusive vazio).
    private readonly HashSet<int> usados = new HashSet<int>();

    public Regra(float chance, int[] entrada, int[] saida)
    {
        this.chance = chance;
        this.entrada = entrada;
        this.saida = saida;

        for (int i = 0; i < entrada.Length; i += 4)
        {
            if (entrada[i + 2] == Automapa.Ladrilho)
                usados.Add(entrada[i + 3]);
        }
    }

    public bool Casa(int[,] grade, int x, int y)
    {
        for (int i = 0; i < entrada.Length; i += 4)
        {
            int atual = Automapa.Ler(grade, x + entrada[i], y + entrada[i + 1]);

            switch (entrada[i + 2])
            {
                case Automapa.Ladrilho:
                    if (atual != entrada[i + 3])
                        return false;
                    break;
                case Automapa.Outro:
                    if (atual >= 0 && usados.Contains(atual))
                        return false;
                    break;
                case Automapa.Vazio:
                    if (atual >= 0)
                        return false;
                    break;
                case Automapa.NaoVazio:
                    if (atual < 0)
                        return false;
                    break;
            }
        }

        return true;
    }
}

/// <summary>
/// Aplica regras de encaixe numa grade de ladrilhos, do jeito do Tiled: cada regra, na ordem, procura
/// o mapa inteiro e escreve a saida em todo lugar que casou (a de depois pode cobrir a de antes).
///
/// A grade e [coluna, linha], com as linhas crescendo pra baixo (como no Tiled); -1 = vazio, e fora
/// da grade conta como vazio.
/// </summary>
public static class Automapa
{
    // Tipos de celula da entrada de uma regra (os "ladrilhos especiais" do Tiled).
    public const int Ladrilho = 0;
    public const int Outro = 1;
    public const int Vazio = 2;
    public const int NaoVazio = 3;

    /// <summary>Quanto uma regra pode passar da beirada da grade (as do Old Prison tem ate 4 de altura).</summary>
    private const int Folga = 4;

    /// <param name="emOrdem">
    /// True: cada regra ve o que as anteriores escreveram. False: todas veem a grade como estava antes
    /// (e o jeito padrao do Tiled; as regras de variacao do pacote pedem em ordem).
    /// </param>
    public static void Aplicar(int[,] grade, Regra[] regras, bool emOrdem, System.Random sorte)
    {
        int largura = grade.GetLength(0), altura = grade.GetLength(1);
        int[,] antes = emOrdem ? null : (int[,])grade.Clone();
        List<Vector2Int> achados = new List<Vector2Int>();

        foreach (Regra regra in regras)
        {
            int[,] olhando = emOrdem ? grade : antes;
            achados.Clear();

            for (int y = -Folga; y < altura; y++)
            {
                for (int x = -Folga; x < largura; x++)
                {
                    if (regra.Casa(olhando, x, y))
                        achados.Add(new Vector2Int(x, y));
                }
            }

            foreach (Vector2Int lugar in achados)
            {
                if (regra.chance < 1f && sorte.NextDouble() >= regra.chance)
                    continue;

                for (int i = 0; i < regra.saida.Length; i += 3)
                {
                    int x = lugar.x + regra.saida[i], y = lugar.y + regra.saida[i + 1];

                    if (x >= 0 && y >= 0 && x < largura && y < altura)
                        grade[x, y] = regra.saida[i + 2];
                }
            }
        }
    }

    public static int Ler(int[,] grade, int x, int y)
    {
        if (x < 0 || y < 0 || x >= grade.GetLength(0) || y >= grade.GetLength(1))
            return -1;

        return grade[x, y];
    }
}
