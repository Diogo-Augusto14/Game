using UnityEngine;

/// <summary>
/// Contrato entre o sistema de dano e um controlador de movimento (Movimento.cs).
/// O Vida procura por isto no Awake: se achar, conversa; se não achar (inimigos
/// simples, vasos), aplica o empurrão direto no Rigidbody2D.
/// Assim o Vida continua servindo pra qualquer coisa, sem conhecer o Movimento.
/// </summary>
public interface IControladorDeMovimento
{
    /// <summary>True quando o dano deve ser ignorado (ex: durante o dash).</summary>
    bool IgnorandoDano { get; }

    /// <summary>
    /// Empurrão vindo de fora (dano, explosão, vento). Quem implementa é responsável
    /// por travar o próprio controle horizontal por <paramref name="travaSegundos"/>,
    /// senão o movimento come o impulso no quadro seguinte.
    /// </summary>
    void AplicarImpulsoExterno(Vector2 impulso, float travaSegundos);
}
