using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O que libera herois, salvo entre partidas (PlayerPrefs): os mundos vencidos (o chefe de cada
/// mundo), as vitorias (zerar o jogo) e com quem zerou. Quando um heroi novo fica liberado, avisa
/// na tela (<see cref="AvisoDeConquista"/>). Os numeros de jogo ficam no <see cref="Registro"/>.
/// </summary>
public static class Progresso
{
    private const string Prefixo = "ThePrettie.progresso.";

    public static int Vitorias => PlayerPrefs.GetInt(Prefixo + "vitorias", 0);

    public static bool VenceuOMundo(int mundo) => PlayerPrefs.GetInt(Prefixo + "mundo." + mundo, 0) == 1;

    public static bool ZerouCom(string heroi) => PlayerPrefs.GetInt(Prefixo + "zerou." + heroi, 0) == 1;

    /// <summary>Venceu o chefe do mundo (o andar dele).</summary>
    public static void VencerMundo(int mundo)
    {
        if (VenceuOMundo(mundo))
            return;

        Mudar(() => PlayerPrefs.SetInt(Prefixo + "mundo." + mundo, 1));

        if (mundo == 1)
            Conquistas.Conquistar(Conquistas.SaidaDoPorao);
    }

    /// <summary>Zerou o jogo (venceu o ultimo chefe) com este heroi.</summary>
    public static void Zerar(string heroi)
    {
        Mudar(() =>
        {
            PlayerPrefs.SetInt(Prefixo + "vitorias", Vitorias + 1);

            if (!string.IsNullOrEmpty(heroi))
                PlayerPrefs.SetInt(Prefixo + "zerou." + heroi, 1);
        });

        if (Vitorias >= 3)
            Conquistas.Conquistar(Conquistas.Veterano);
    }

    // Muda o progresso e avisa dos herois que acabaram de ficar liberados.
    private static void Mudar(System.Action mudanca)
    {
        List<Herois.Heroi> antes = new List<Herois.Heroi>();

        foreach (Herois.Heroi h in Herois.Todos)
        {
            if (!Herois.Liberado(h))
                antes.Add(h);
        }

        mudanca();
        PlayerPrefs.Save();

        bool todos = true;

        foreach (Herois.Heroi h in Herois.Todos)
        {
            if (!Herois.Liberado(h))
                todos = false;
            else if (antes.Contains(h))
                AvisoDeConquista.Mostrar("Herói liberado: " + h.Nome, h.Descricao);
        }

        if (todos)
            Conquistas.Conquistar(Conquistas.TodosOsHerois);
    }

    /// <summary>Zera os herois liberados e as vitorias (as estatisticas ficam no Registro.Apagar).</summary>
    public static void Apagar()
    {
        PlayerPrefs.DeleteKey(Prefixo + "vitorias");

        for (int mundo = 1; mundo <= 4; mundo++)
            PlayerPrefs.DeleteKey(Prefixo + "mundo." + mundo);

        foreach (Herois.Heroi h in Herois.Todos)
            PlayerPrefs.DeleteKey(Prefixo + "zerou." + h.Nome);

        PlayerPrefs.Save();
    }
}
