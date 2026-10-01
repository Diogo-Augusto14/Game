using UnityEngine;

/// <summary>
/// O momento em que um chefe muda de fase, igual pra todos: ruge, a tela treme, o jogo
/// congela um instante, os tiros inimigos que estavam no ar somem (um respiro pro jogador
/// perceber a mudanca) e uma frase aparece em cima dele. Os chefes chamam no comeco da
/// fase nova; o que muda em cada um continua no proprio chefe.
/// </summary>
public static class ViradaDeFase
{
    public static void Anunciar(InimigoDeSala chefe, string frase, Color cor)
    {
        if (chefe == null)
            return;

        Sons.Tocar(Som.Rugido, 1f, 0f);
        Impacto.Congelar(0.15f);
        Impacto.Tremer(0.35f, 0.7f);

        Vector2 centro = chefe.transform.position;
        EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Explosao, centro, cor, 2.2f);

        foreach (TiroDaSala tiro in Object.FindObjectsByType<TiroDaSala>())
        {
            if (tiro == null || !tiro.AtingeJogador)
                continue;

            EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Poeira, tiro.transform.position, cor, 0.5f);
            Object.Destroy(tiro.gameObject);
        }

        TextoFlutuante texto = TextoFlutuante.Mostrar((Vector3)centro + Vector3.up * 1.6f, frase, cor);
        texto.Pular(1.6f);
    }
}
