using UnityEngine;

/// <summary>
/// O que a <see cref="AnimacaoDoInimigo"/> precisa saber de quem ela desenha: pra onde olha, se
/// esta andando e quando ataca. O <see cref="InimigoAtirador"/> e o <see cref="Chefe"/> contam.
/// </summary>
public interface IAnimavel
{
    /// <summary>Pra onde olha (tamanho 1): o desenho espelha pro lado.</summary>
    Vector2 OlhandoPara { get; }

    Vector2 Velocidade { get; }

    /// <summary>
    /// Comecou um ataque: qual animacao (0 = a primeira) e quantos segundos ate o golpe (o quadro do
    /// golpe tem que chegar nessa hora).
    /// </summary>
    event System.Action<int, float> AoAtacar;
}
