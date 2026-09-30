using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O que fica salvo entre partidas (no PlayerPrefs do computador): quantas vezes o jogo foi
/// zerado e quais herois ja foram liberados. Como no Isaac, voce comeca so com um e ganha
/// os outros jogando (as condicoes de cada um estao em <see cref="Herois"/>).
///
/// O <see cref="Andar"/> avisa aqui quando um mundo e fechado e quando o jogo e zerado; cada
/// heroi novo aparece num aviso na tela e fica liberado no menu dali em diante.
/// </summary>
public static class Progresso
{
    private const string Prefixo = "ThePrettie.";
    private const string ChaveDeVitorias = Prefixo + "vitorias";

    /// <summary>Quantas vezes o chefe final ja caiu.</summary>
    public static int Vitorias => PlayerPrefs.GetInt(ChaveDeVitorias, 0);

    public static bool HeroiLiberado(string nome) => PlayerPrefs.GetInt(ChaveDoHeroi(nome), 0) == 1;

    /// <summary>
    /// O chefe da ultima fase do mundo <paramref name="mundo"/> caiu. Devolve os herois
    /// liberados agora (lista vazia se nenhum).
    /// </summary>
    public static List<Herois.Heroi> VenceuMundo(int mundo)
    {
        return Liberar(h => h.LiberaNoMundo > 0 && mundo >= h.LiberaNoMundo);
    }

    /// <summary>O chefe final caiu com <paramref name="heroi"/>. Devolve os herois liberados agora.</summary>
    public static List<Herois.Heroi> Zerou(Herois.Heroi heroi)
    {
        PlayerPrefs.SetInt(ChaveDeVitorias, Vitorias + 1);

        int vitorias = Vitorias;
        string nome = heroi != null ? heroi.Nome : null;

        // Zerar tambem conta como ter fechado todos os mundos.
        return Liberar(h => (h.LiberaComVitorias > 0 && vitorias >= h.LiberaComVitorias)
                         || (h.LiberaZerandoCom != null && h.LiberaZerandoCom == nome)
                         || h.LiberaNoMundo > 0);
    }

    /// <summary>Zera tudo: so o heroi livre volta a ficar disponivel.</summary>
    public static void Apagar()
    {
        foreach (Herois.Heroi heroi in Herois.Todos)
            PlayerPrefs.DeleteKey(ChaveDoHeroi(heroi.Nome));

        PlayerPrefs.DeleteKey(ChaveDeVitorias);
        PlayerPrefs.Save();
    }

    private static List<Herois.Heroi> Liberar(System.Predicate<Herois.Heroi> condicao)
    {
        List<Herois.Heroi> novos = new List<Herois.Heroi>();

        foreach (Herois.Heroi heroi in Herois.Todos)
        {
            if (heroi.Livre || HeroiLiberado(heroi.Nome) || !condicao(heroi))
                continue;

            PlayerPrefs.SetInt(ChaveDoHeroi(heroi.Nome), 1);
            novos.Add(heroi);
        }

        // Salva na hora: fechar o jogo na tela de vitoria nao pode perder o heroi.
        PlayerPrefs.Save();

        foreach (Herois.Heroi heroi in novos)
            Debug.Log($"[Progresso] heroi liberado: {heroi.Nome}");

        return novos;
    }

    private static string ChaveDoHeroi(string nome) => Prefixo + "heroi." + nome.Replace(' ', '_');
}
