using UnityEngine;

/// <summary>
/// As fontes do jogo, lidas de <c>Assets/Arte/Resources/Fontes</c> (as duas com licenca
/// aberta OFL, arquivo da licenca do lado):
///
///   <see cref="Texto"/>   Jersey 15: pixel, facil de ler, numeros bem distintos (5 nao
///                         parece S) e todos os acentos. Todo texto. O arquivo tem o
///                         unitsPerEm diminuido (1350 -> 1080) pra desenhar do tamanho da
///                         fonte antiga com o mesmo fontSize.
///   <see cref="Titulo"/>  Jacquard 12: pixel gotica, de masmorra. So titulos grandes, em
///                         maiusculas e minusculas ("Você morreu"): em caixa alta fica ilegivel.
///
/// Sem o arquivo, volta pra fonte que vem com a Unity (o jogo continua legivel).
/// </summary>
public static class FonteDoJogo
{
    private static Font texto;
    private static Font titulo;

    public static Font Texto => texto != null ? texto : texto = Carregar("Fontes/Jersey15");

    public static Font Titulo => titulo != null ? titulo : titulo = Carregar("Fontes/Jacquard12", Texto);

    /// <summary>
    /// O texto sem acentos ("Templário" vira "Templario"). Pra chaves salvas e nomes usados
    /// como codigo, que eram escritos sem acento antes da fonte nova: o que ja estava salvo
    /// continua valendo.
    /// </summary>
    public static string SemAcentos(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return texto;

        System.Text.StringBuilder limpo = new System.Text.StringBuilder(texto.Length);

        foreach (char c in texto.Normalize(System.Text.NormalizationForm.FormD))
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                limpo.Append(c);
        }

        return limpo.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    private static Font Carregar(string caminho, Font reserva = null)
    {
        Font fonte = Resources.Load<Font>(caminho);

        if (fonte != null)
            return fonte;

        Debug.LogWarning($"[FonteDoJogo] nao achei Resources/{caminho}. Usando a fonte da Unity.");

        if (reserva != null)
            return reserva;

        fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return fonte != null ? fonte : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
