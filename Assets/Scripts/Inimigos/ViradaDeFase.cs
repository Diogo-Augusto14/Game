using UnityEngine;

/// <summary>
/// O momento em que um chefe muda de fase (entra em furia), igual pra todos (veio do jogo antigo):
/// ruge, a tela treme, o jogo congela um instante, os tiros inimigos que estavam no ar somem (um
/// respiro pro jogador perceber a mudanca) e uma frase aparece em cima dele. O <see cref="Chefe"/>
/// chama; o que muda em cada um continua no proprio chefe.
/// </summary>
public static class ViradaDeFase
{
    public static void Anunciar(Chefe chefe, string frase, Color cor)
    {
        if (chefe == null)
            return;

        Impacto.Congelar(0.15f);
        Impacto.Tremer(0.35f, 0.7f);

        foreach (Projetil tiro in Object.FindObjectsByType<Projetil>(FindObjectsSortMode.None))
        {
            if (tiro != null && tiro.Lado != Lado.Jogador)
                tiro.Sumir();
        }

        if (!string.IsNullOrEmpty(frase))
        {
            Vector3 centro = chefe.transform.position;
            TextoFlutuante texto = TextoFlutuante.Mostrar(centro + Vector3.up * 2.2f, frase, cor, 1.8f);
            texto.Pular(1.6f);
        }
    }
}
