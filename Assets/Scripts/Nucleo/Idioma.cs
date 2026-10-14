using UnityEngine;

/// <summary>Os idiomas do jogo.</summary>
public enum Lingua
{
    Portugues,
    Ingles,
}

/// <summary>
/// O idioma do jogo (1.19.0): portugues ou ingles. O jogo e escrito em portugues; na hora de mostrar, os
/// textos passam por <see cref="T"/>, que troca pela traducao (<see cref="Traducoes"/>) quando o idioma e o
/// ingles. Os textos montados com numeros usam <see cref="F"/> com um modelo ("Andar {0} de {1}"), que
/// tambem fica no dicionario. Os dados (nomes de item, de arma) continuam em portugues por dentro: o
/// salvamento guarda eles assim.
///
/// As pecas que desenham texto ja traduzem sozinhas (<see cref="TelaSimples"/>, <see cref="MenuDeBotoes"/>,
/// <see cref="Desenho.Texto"/>, <see cref="TextoFlutuante"/>, os avisos). O primeiro idioma vem do sistema
/// (portugues no Brasil e em Portugal, ingles no resto); troca nas Configuracoes e fica salvo.
/// </summary>
public static class Idioma
{
    private const string Chave = "ThePrettie.idioma";

    private static Lingua? atual;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar() => atual = null;

    public static Lingua Atual
    {
        get
        {
            if (atual == null)
            {
                int padrao = Application.systemLanguage == SystemLanguage.Portuguese ? 0 : 1;
                atual = (Lingua)Mathf.Clamp(PlayerPrefs.GetInt(Chave, padrao), 0, 1);
            }

            return atual.Value;
        }
        set
        {
            atual = value;
            PlayerPrefs.SetInt(Chave, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static bool Ingles => Atual == Lingua.Ingles;

    /// <summary>"Português" ou "English" (como aparece nas Configuracoes).</summary>
    public static string NomeDoAtual => Ingles ? "English" : "Português";

    /// <summary>O texto no idioma do jogo (sem traducao, fica como esta).</summary>
    public static string T(string texto)
    {
        if (!Ingles || string.IsNullOrEmpty(texto))
            return texto;

        return Traducoes.Ingles.TryGetValue(texto, out string traduzido) ? traduzido : texto;
    }

    /// <summary>Um modelo com lacunas ("Andar {0} de {1}") traduzido e preenchido.</summary>
    public static string F(string modelo, params object[] valores)
    {
        try
        {
            return string.Format(T(modelo), valores);
        }
        catch (System.FormatException)
        {
            return string.Format(modelo, valores);
        }
    }
}
