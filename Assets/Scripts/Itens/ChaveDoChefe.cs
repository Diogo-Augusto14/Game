using UnityEngine;

/// <summary>
/// A chave dourada que todo chefe (menos o final) deixa ao morrer: vale uma chave, mas gira
/// e brilha no chao pra ninguem sair da sala sem ela. E ela que abre a sala do tesouro
/// trancada do andar seguinte, ou um bau de ferro, se o jogador preferir.
/// </summary>
public static class ChaveDoChefe
{
    /// <summary>Tamanho da chave no chao, em unidades (maior que a chave comum).</summary>
    private const float Tamanho = 1.1f;

    public static Coletavel Criar(Vector2 posicao, Transform pai)
    {
        Coletavel chave = Coletavel.Criar(TipoDeColetavel.Chave, posicao, pai);
        chave.name = "Chave do chefe";

        Sprite[] quadros = ArteImportada.ChaveDourada;

        if (quadros == null)
            return chave;

        // Os quadros girando num filho; o desenho parado do coletavel se esconde. O filho
        // desfaz a escala do coletavel pra chave ficar do tamanho certo.
        // Filho do desenho que flutua: gira e sobe e desce junto.
        Transform flutuante = chave.Desenho != null ? chave.Desenho.transform : chave.transform;
        EfeitoDeQuadros giro = EfeitoDeQuadros.Criar(quadros, 10f, posicao, 6, flutuante);

        if (giro == null)
            return chave;

        giro.EmLoop();
        giro.name = "Giro";
        giro.transform.localScale = Vector3.one * (Tamanho / Mathf.Max(0.01f, chave.transform.localScale.x));

        if (chave.Desenho != null)
            chave.Desenho.enabled = false;

        return chave;
    }
}
