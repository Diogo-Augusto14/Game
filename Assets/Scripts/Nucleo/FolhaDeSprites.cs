using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Corta uma folha de animacao em quadros. A folha e uma imagem com os quadros lado a lado, todos
/// do mesmo tamanho (uma linha so ou varias, lidas da esquerda pra direita e de cima pra baixo).
/// O pivo de cada quadro fica no meio dele.
///
/// Trocar o boneco e so trocar a folha e dizer o tamanho do quadro: nao precisa fatiar nada no
/// Sprite Editor. A mesma folha cortada do mesmo jeito sai do guardado na segunda vez.
/// </summary>
public static class FolhaDeSprites
{
    private static readonly Dictionary<(Texture2D, Vector2Int, float), Sprite[]> guardadas =
        new Dictionary<(Texture2D, Vector2Int, float), Sprite[]>();

    public static Sprite[] Cortar(Texture2D folha, Vector2Int tamanhoDoQuadro, float pixelsPorUnidade)
    {
        if (folha == null || tamanhoDoQuadro.x <= 0 || tamanhoDoQuadro.y <= 0)
            return new Sprite[0];

        var chave = (folha, tamanhoDoQuadro, pixelsPorUnidade);

        if (guardadas.TryGetValue(chave, out Sprite[] prontos) && prontos.Length > 0 && prontos[0] != null)
            return prontos;

        int colunas = folha.width / tamanhoDoQuadro.x;
        int linhas = folha.height / tamanhoDoQuadro.y;
        List<Sprite> quadros = new List<Sprite>();

        for (int linha = 0; linha < linhas; linha++)
        {
            for (int coluna = 0; coluna < colunas; coluna++)
            {
                // Na textura a linha 0 e embaixo; a primeira linha de quadros e a de cima.
                Rect recorte = new Rect(coluna * tamanhoDoQuadro.x, folha.height - (linha + 1) * tamanhoDoQuadro.y,
                                        tamanhoDoQuadro.x, tamanhoDoQuadro.y);
                Sprite quadro = Sprite.Create(folha, recorte, new Vector2(0.5f, 0.5f), pixelsPorUnidade, 0, SpriteMeshType.FullRect);
                quadro.name = $"{folha.name} {quadros.Count}";
                quadros.Add(quadro);
            }
        }

        Sprite[] resultado = quadros.ToArray();
        guardadas[chave] = resultado;
        return resultado;
    }
}
