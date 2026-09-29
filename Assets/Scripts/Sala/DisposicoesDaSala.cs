using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Os desenhos de obstaculo que uma sala comum pode ter (pedras e espinhos), no estilo do
/// Isaac: cada sala sorteia um, e a mesma planta de andar fica com salas bem diferentes.
///
/// Cada disposicao e escrita so pra um quarto da sala (x de 1 a 6, y de 1 a 3, em
/// ladrilhos a partir do centro) e espelhada pros outros tres. Como x e y nunca sao 0,
/// a CRUZ do meio fica sempre livre: toda porta liga no centro, e dali em todas as outras
/// portas, entao nenhum desenho tranca o caminho. O jogador tambem nasce no centro.
/// </summary>
public static class DisposicoesDaSala
{
    private struct Peca
    {
        public TipoDeObstaculo Tipo;
        public Vector2Int Celula;

        public Peca(TipoDeObstaculo tipo, int x, int y)
        {
            Tipo = tipo;
            Celula = new Vector2Int(x, y);
        }
    }

    private const TipoDeObstaculo P = TipoDeObstaculo.Pedra;
    private const TipoDeObstaculo E = TipoDeObstaculo.Espinhos;

    private static readonly Peca[][] Desenhos =
    {
        // Pilares: uma pedra em cada quarto.
        new[] { new Peca(P, 3, 2) },

        // Cantos cheios.
        new[] { new Peca(P, 6, 3), new Peca(P, 5, 3), new Peca(P, 6, 2) },

        // Barras deitadas.
        new[] { new Peca(P, 2, 2), new Peca(P, 3, 2), new Peca(P, 4, 2) },

        // Anel em volta do centro (aberto na cruz).
        new[] { new Peca(P, 1, 2), new Peca(P, 2, 2), new Peca(P, 2, 1) },

        // Colunas: a sala vira tres corredores ligados pelo meio.
        new[] { new Peca(P, 3, 1), new Peca(P, 3, 2), new Peca(P, 3, 3) },

        // Canteiros de espinho.
        new[] { new Peca(E, 3, 1), new Peca(E, 3, 2), new Peca(E, 4, 1), new Peca(E, 4, 2) },

        // Pedra no canto cercada de espinho.
        new[] { new Peca(P, 6, 3), new Peca(E, 5, 3), new Peca(E, 6, 2), new Peca(E, 5, 2) },

        // Misto: pedras perto das paredes, espinhos no meio do caminho.
        new[] { new Peca(P, 5, 1), new Peca(P, 5, 2), new Peca(E, 2, 2), new Peca(E, 2, 3) },
    };

    /// <summary>Quantos desenhos existem (sem contar a sala vazia).</summary>
    public static int Quantidade => Desenhos.Length;

    /// <summary>
    /// Sorteia um desenho e poe na sala. <paramref name="chanceDeVazia"/> e a chance de a
    /// sala ficar sem obstaculo nenhum. Espinhos so a partir de <paramref name="comEspinhos"/>.
    /// </summary>
    public static void Sortear(Sala sala, float chanceDeVazia, bool comEspinhos)
    {
        if (Random.value < chanceDeVazia)
            return;

        List<int> opcoes = new List<int>();

        for (int i = 0; i < Desenhos.Length; i++)
            if (comEspinhos || !TemEspinho(Desenhos[i]))
                opcoes.Add(i);

        Aplicar(sala, opcoes[Random.Range(0, opcoes.Count)]);
    }

    /// <summary>Poe o desenho de numero <paramref name="indice"/>, espelhado nos quatro quartos.</summary>
    public static void Aplicar(Sala sala, int indice)
    {
        foreach (Peca peca in Desenhos[Mathf.Clamp(indice, 0, Desenhos.Length - 1)])
        {
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                    sala.PorObstaculo(peca.Tipo, new Vector2Int(peca.Celula.x * sx, peca.Celula.y * sy));
        }
    }

    private static bool TemEspinho(Peca[] desenho)
    {
        foreach (Peca peca in desenho)
            if (peca.Tipo == TipoDeObstaculo.Espinhos)
                return true;

        return false;
    }
}
