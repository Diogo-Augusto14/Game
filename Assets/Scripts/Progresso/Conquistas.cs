using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// As conquistas: cada uma tem um jeito de ganhar e, algumas, um item que so entra no sorteio
/// depois dela (como no Isaac). Ganhar mostra um aviso no canto da tela
/// (<see cref="AvisoDeConquista"/>) e fica salvo pra sempre. A lista aparece na
/// <see cref="TelaDeProgresso"/>.
/// </summary>
public static class Conquistas
{
    public sealed class Conquista
    {
        public readonly string Id;
        public readonly string Nome;
        public readonly string ComoGanhar;
        public readonly string Libera;

        public Conquista(string id, string nome, string comoGanhar, string libera = null)
        {
            Id = id;
            Nome = nome;
            ComoGanhar = comoGanhar;
            Libera = libera;
        }
    }

    private const string Prefixo = "ThePrettie.conquista.";

    public static readonly Conquista PrimeiroSangue = new Conquista("primeiro-sangue", "Primeiro Sangue", "Derrote um chefe.", "Cometa de Fogo");
    public static readonly Conquista Cacador = new Conquista("cacador", "Caçador", "Derrote 150 inimigos (somando as partidas).", "Cristais do Trovão");
    public static readonly Conquista Alquimista = new Conquista("alquimista", "Alquimista", "Forme uma sinergia de itens.", "Moeda do Destino");
    public static readonly Conquista SaidaDoPorao = new Conquista("saida-do-porao", "Saída do Porão", "Feche o primeiro mundo.", "Espiral do Tempo");
    public static readonly Conquista Persistente = new Conquista("persistente", "Persistente", "Morra 5 vezes. Faz parte.");
    public static readonly Conquista DoadorDeSangue = new Conquista("doador", "Doador de Sangue", "Faça 3 sacrifícios no altar numa só partida.");
    public static readonly Conquista NinguemMePega = new Conquista("emboscada", "Ninguém Me Pega", "Sobreviva a uma emboscada.");
    public static readonly Conquista OlhosNoEscuro = new Conquista("escuridao", "Olhos no Escuro", "Limpe uma sala escura.");
    public static readonly Conquista LuaMinguante = new Conquista("lobo", "Lua Minguante", "Derrote o Lobisomem Alfa.");
    public static readonly Conquista FimDaGuerra = new Conquista("guerra", "Fim da Guerra", "Derrote o Senhor da Guerra.");
    public static readonly Conquista Colecionador = new Conquista("colecionador", "Colecionador", "Tenha 10 itens numa só partida.");
    public static readonly Conquista MaoNaMassa = new Conquista("ativo", "Mão na Massa", "Use itens ativos 10 vezes.");
    public static readonly Conquista Naturalista = new Conquista("naturalista", "Naturalista", "Encontre 30 tipos de inimigo.");
    public static readonly Conquista LendaDoAbismo = new Conquista("zerou", "Lenda do Abismo", "Derrote o Olho do Abismo.");

    public static readonly IReadOnlyList<Conquista> Todas = new[]
    {
        PrimeiroSangue, Cacador, Alquimista, SaidaDoPorao, Persistente, DoadorDeSangue, NinguemMePega,
        OlhosNoEscuro, LuaMinguante, FimDaGuerra, Colecionador, MaoNaMassa, Naturalista, LendaDoAbismo,
    };

    public static bool Tem(Conquista c) => c != null && PlayerPrefs.GetInt(Prefixo + c.Id, 0) == 1;

    public static int Quantas
    {
        get
        {
            int n = 0;

            foreach (Conquista c in Todas)
                if (Tem(c))
                    n++;

            return n;
        }
    }

    /// <summary>Ganha a conquista (uma vez so) e avisa na tela.</summary>
    public static void Conquistar(Conquista c)
    {
        if (c == null || Tem(c))
            return;

        PlayerPrefs.SetInt(Prefixo + c.Id, 1);
        PlayerPrefs.Save();
        AvisoDeConquista.Mostrar(c);
        Debug.Log($"[Conquistas] {c.Nome}");
    }

    /// <summary>
    /// O item pode sair no sorteio? Item que nenhuma conquista libera, sempre; os outros, so
    /// depois da conquista.
    /// </summary>
    public static bool ItemLiberado(string nomeDoItem)
    {
        foreach (Conquista c in Todas)
            if (c.Libera == nomeDoItem)
                return Tem(c);

        return true;
    }

    public static void Apagar()
    {
        foreach (Conquista c in Todas)
            PlayerPrefs.DeleteKey(Prefixo + c.Id);
    }
}
