using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O que vai sendo liberado com o tempo (1.21.0), como no Dead Cells e no Isaac: parte das armas e dos
/// itens comeca trancada e nao sai de bau, loja, pedestal, fonte nem mimico ate ser liberada, e ai fica
/// liberada pra sempre. Dois jeitos de liberar:
///
///   as plantas (<see cref="Plantas"/>): tres ou quatro por mundo. Caem do chefe do mundo, as vezes de um campeao, e
///   podem ser o premio de uma sala (o pergaminho azul em cima da porta). Pegou, aprendeu
///   (<see cref="PlantaNoChao"/>). A primeira planta da vida sai na sala do fim do andar 1;
///   as conquistas (<see cref="PorConquista"/>): cada uma dessas libera um item.
///
/// As armas do Arsenal (a estatua dos cristais) continuam do jeito delas (<see cref="Melhorias.Liberada"/>).
/// No desafio do dia tudo vale (o mesmo jogo pra todo mundo) e nao cai planta. Fica no PlayerPrefs.
/// </summary>
public static class Desbloqueios
{
    private const string Prefixo = "ThePrettie.planta.";

    /// <summary>Uma planta: a de uma arma (pelo nome do asset) ou a de um item (pelo nome dele).</summary>
    public sealed class Planta
    {
        public readonly string Id;
        public readonly string Nome;
        public readonly bool EhArma;
        public readonly int Mundo;

        public Planta(string id, string nome, bool ehArma, int mundo)
        {
            Id = id;
            Nome = nome;
            EhArma = ehArma;
            Mundo = mundo;
        }
    }

    private static Planta Arma(string asset, string nome, int mundo) => new Planta(asset, nome, true, mundo);

    private static Planta Item(string nome, int mundo) => new Planta(nome, nome, false, mundo);

    /// <summary>As plantas de cada mundo (o mundo e a ordem na partida: 1 o Porao, 4 o ultimo).</summary>
    public static readonly Planta[] Plantas =
    {
        Arma("ArcoDeGelo", "Arco de Gelo", 1), Arma("ArcoVenenoso", "Arco Venenoso", 1), Item("Corda Encantada", 1),
        Arma("ArcoExplosivo", "Arco Explosivo", 2), Arma("LaminaDoAbismo", "Lâmina do Abismo", 2), Item("Bobina de Cobre", 2), Item("Elmo de Duas Faces", 2),
        Arma("ArcoRicochete", "Arco Ricochete", 3), Arma("TomoProibido", "Tomo Proibido", 3), Item("Brasa da Fúria", 3),
        Arma("BestaReal", "Besta Real", 4), Item("Runa Tríplice", 4), Item("Cometa de Fogo", 4),
    };

    private static Dictionary<string, Conquistas.Conquista> porConquista;

    /// <summary>O item que cada conquista libera (pelo nome do item).</summary>
    public static IReadOnlyDictionary<string, Conquistas.Conquista> PorConquista =>
        porConquista ??= new Dictionary<string, Conquistas.Conquista>
        {
            { "Pena da Fênix", Conquistas.Persistente },
            { "Olho de Falcão", Conquistas.Cacador },
            { "Sangue de Vampiro", Conquistas.PrimeiroSangue },
            { "Hidromel", Conquistas.SaidaDoPorao },
            { "Saco de Moedas", Conquistas.Mimico },
            { "Amuleto da Sorte", Conquistas.Apostador },
            { "Bolsa do Mercador", Conquistas.Libertador },
            { "Lâmina Larga", Conquistas.Ferreiro },
            { "Bússola Maldita", Conquistas.Naturalista },
            { "Runa Gêmea", Conquistas.Arsenal },
            { "Cristais do Trovão", Conquistas.Campeoes },
            { "Orbe Guardião", Conquistas.LendaDoAbismo },
        };

    /// <summary>As plantas que estao no chao agora (pra nao cair a mesma duas vezes antes de pegar).</summary>
    private static readonly HashSet<string> noChao = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar() => noChao.Clear();

    public static bool Aprendida(Planta p) => p != null && PlayerPrefs.GetInt(Prefixo + p.Id, 0) == 1;

    public static int Aprendidas
    {
        get
        {
            int n = 0;

            foreach (Planta p in Plantas)
            {
                if (Aprendida(p))
                    n++;
            }

            return n;
        }
    }

    public static Planta DaArma(DadosDaArma arma)
    {
        if (arma == null)
            return null;

        string id = arma.Origem.name;

        foreach (Planta p in Plantas)
        {
            if (p.EhArma && p.Id == id)
                return p;
        }

        return null;
    }

    public static Planta DoItem(ItemPassivo item)
    {
        if (item == null)
            return null;

        foreach (Planta p in Plantas)
        {
            if (!p.EhArma && p.Id == item.Nome)
                return p;
        }

        return null;
    }

    /// <summary>A arma pode sair (nao tem planta, ou a planta ja foi aprendida). As do Arsenal: ver Melhorias.</summary>
    public static bool Liberada(DadosDaArma arma)
    {
        if (arma == null)
            return false;

        if (Partida.Diario)
            return true;

        Planta p = DaArma(arma);
        return p == null || Aprendida(p);
    }

    /// <summary>O item pode aparecer (sem trava, a planta aprendida ou a conquista ganha).</summary>
    public static bool Liberado(ItemPassivo item)
    {
        if (item == null)
            return false;

        if (Partida.Diario)
            return true;

        Planta p = DoItem(item);

        if (p != null)
            return Aprendida(p);

        return !PorConquista.TryGetValue(item.Nome, out Conquistas.Conquista c) || Conquistas.Tem(c);
    }

    /// <summary>O mundo ainda tem planta pra achar (nunca no desafio do dia).</summary>
    public static bool FaltamNoMundo(int mundo)
    {
        if (Partida.Diario)
            return false;

        int m = Mathf.Clamp(mundo, 1, 4);

        foreach (Planta p in Plantas)
        {
            if (p.Mundo == m && !Aprendida(p) && !noChao.Contains(p.Id))
                return true;
        }

        return false;
    }

    /// <summary>Uma planta do mundo que ainda nao foi aprendida nem esta no chao (nula se nao tiver).</summary>
    public static Planta Sortear(int mundo)
    {
        if (Partida.Diario)
            return null;

        int m = Mathf.Clamp(mundo, 1, 4);
        List<Planta> faltam = new List<Planta>();

        foreach (Planta p in Plantas)
        {
            if (p.Mundo == m && !Aprendida(p) && !noChao.Contains(p.Id))
                faltam.Add(p);
        }

        return faltam.Count > 0 ? faltam[Random.Range(0, faltam.Count)] : null;
    }

    /// <summary>Solta uma planta do mundo ali (se ainda tiver alguma pra achar). Devolve se soltou.</summary>
    public static bool Soltar(Vector2 onde, int mundo)
    {
        Planta p = Sortear(mundo);

        if (p == null)
            return false;

        PlantaNoChao.Criar(p, onde);
        return true;
    }

    public static void NoChao(Planta p, bool esta)
    {
        if (p == null)
            return;

        if (esta)
            noChao.Add(p.Id);
        else
            noChao.Remove(p.Id);
    }

    /// <summary>Aprende a planta pra sempre e avisa na tela.</summary>
    public static void Aprender(Planta p)
    {
        if (p == null || Aprendida(p))
            return;

        PlayerPrefs.SetInt(Prefixo + p.Id, 1);
        PlayerPrefs.SetInt(Prefixo + "nesta-partida", PlayerPrefs.GetInt(Prefixo + "nesta-partida", 0) + 1);
        PlayerPrefs.Save();
        AvisoDeConquista.Mostrar(Idioma.F("Planta: {0}", Idioma.T(p.Nome)),
                                 p.EhArma ? "Arma nova: agora ela pode sair dos baús." : "Item novo: agora ele pode aparecer nas partidas.");
    }

    /// <summary>Quantas plantas foram aprendidas na partida de agora (as falas do acampamento comentam).</summary>
    public static int NestaPartida => PlayerPrefs.GetInt(Prefixo + "nesta-partida", 0);

    /// <summary>Partida nova: zera a conta das plantas da partida.</summary>
    public static void ComecouPartida() => PlayerPrefs.SetInt(Prefixo + "nesta-partida", 0);

    /// <summary>Uma conquista acabou de sair: avisa o item que ela liberou (se liberar algum).</summary>
    public static void AoConquistar(Conquistas.Conquista c)
    {
        foreach (KeyValuePair<string, Conquistas.Conquista> par in PorConquista)
        {
            if (par.Value == c)
                AvisoDeConquista.Mostrar(Idioma.F("Liberado: {0}", Idioma.T(par.Key)), "Item novo: agora ele pode aparecer nas partidas.");
        }
    }

    /// <summary>O item que esta conquista libera (nulo se nenhum).</summary>
    public static string ItemDa(Conquistas.Conquista c)
    {
        foreach (KeyValuePair<string, Conquistas.Conquista> par in PorConquista)
        {
            if (par.Value == c)
                return par.Key;
        }

        return null;
    }

    /// <summary>Esquece as plantas aprendidas (o "apagar progresso").</summary>
    public static void Apagar()
    {
        foreach (Planta p in Plantas)
            PlayerPrefs.DeleteKey(Prefixo + p.Id);

        PlayerPrefs.DeleteKey(Prefixo + "nesta-partida");
        noChao.Clear();
    }
}
