using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// As conquistas (vieram do jogo antigo): cada uma tem um jeito de ganhar. Ganhar mostra um aviso
/// no canto da tela (<see cref="AvisoDeConquista"/>) e fica salvo pra sempre. A lista aparece na
/// <see cref="TelaDeProgresso"/>.
/// </summary>
public static class Conquistas
{
    public sealed class Conquista
    {
        public readonly string Id;
        public readonly string Nome;
        public readonly string ComoGanhar;

        public Conquista(string id, string nome, string comoGanhar)
        {
            Id = id;
            Nome = nome;
            ComoGanhar = comoGanhar;
        }
    }

    private const string Prefixo = "ThePrettie.conquista.";

    public static readonly Conquista PrimeiroSangue = new Conquista("primeiro-sangue", "Primeiro Sangue", "Derrote um chefe.");
    public static readonly Conquista Cacador = new Conquista("cacador", "Caçador", "Derrote 150 inimigos (somando as partidas).");
    public static readonly Conquista Exterminador = new Conquista("exterminador", "Exterminador", "Derrote 1000 inimigos (somando as partidas).");
    public static readonly Conquista SaidaDoPorao = new Conquista("saida-do-porao", "Saída do Porão", "Feche o primeiro mundo.");
    public static readonly Conquista Persistente = new Conquista("persistente", "Persistente", "Morra 5 vezes. Faz parte.");
    public static readonly Conquista Touro = new Conquista("touro", "Pelo Chifre", "Derrote o Minotauro.");
    public static readonly Conquista LuaMinguante = new Conquista("lobo", "Lua Minguante", "Derrote o Lobisomem Alfa.");
    public static readonly Conquista FimDaGuerra = new Conquista("guerra", "Fim da Guerra", "Derrote o Senhor da Guerra.");
    public static readonly Conquista Martelada = new Conquista("martelo", "Martelada", "Derrote o Demônio do Martelo.");
    public static readonly Conquista Descanso = new Conquista("rei", "Descanse em Paz", "Derrote o Rei Necromante.");
    public static readonly Conquista Brasa = new Conquista("golem", "Apagando a Brasa", "Derrote o Golem de Brasa.");
    public static readonly Conquista LendaDoAbismo = new Conquista("zerou", "Lenda do Abismo", "Derrote o Olho do Abismo.");
    public static readonly Conquista Veterano = new Conquista("veterano", "Veterano", "Zere o jogo 3 vezes.");
    public static readonly Conquista Naturalista = new Conquista("naturalista", "Naturalista", "Encontre 25 tipos de inimigo.");
    public static readonly Conquista Arsenal = new Conquista("arsenal", "Arsenal", "Pegue 25 armas (somando as partidas).");
    public static readonly Conquista Especialista = new Conquista("especialista", "Especialista", "Use habilidades 50 vezes.");
    public static readonly Conquista TodosOsHerois = new Conquista("herois", "Companhia Completa", "Libere todos os heróis.");

    public static readonly IReadOnlyList<Conquista> Todas = new[]
    {
        PrimeiroSangue, Cacador, Exterminador, SaidaDoPorao, Persistente, Touro, LuaMinguante, FimDaGuerra,
        Martelada, Descanso, Brasa, LendaDoAbismo, Veterano, Naturalista, Arsenal, Especialista, TodosOsHerois,
    };

    /// <summary>A conquista de vencer este chefe (pelo nome do prefab), ou nula.</summary>
    public static Conquista DoChefe(string id)
    {
        switch (id)
        {
            case "Minotauro": return Touro;
            case "LobisomemAlfa": return LuaMinguante;
            case "SenhorDaGuerra": return FimDaGuerra;
            case "DemonioDoMartelo": return Martelada;
            case "ReiNecromante": return Descanso;
            case "Golem": return Brasa;
            case "OlhoDoAbismo": return LendaDoAbismo;
            default: return null;
        }
    }

    public static bool Tem(Conquista c) => c != null && PlayerPrefs.GetInt(Prefixo + c.Id, 0) == 1;

    public static int Quantas
    {
        get
        {
            int n = 0;

            foreach (Conquista c in Todas)
            {
                if (Tem(c))
                    n++;
            }

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
        AvisoDeConquista.Mostrar("Conquista: " + c.Nome, c.ComoGanhar);
        Debug.Log($"[Conquistas] {c.Nome}");
    }

    public static void Apagar()
    {
        foreach (Conquista c in Todas)
            PlayerPrefs.DeleteKey(Prefixo + c.Id);
    }
}
