using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Combinacoes de itens: com os dois itens de uma dupla, o jogador ganha um bonus a mais (a
/// sinergia), anunciado na tela ("Sinergia: Polvora em Chamas!"). O bonus e um
/// <see cref="ItemPassivo"/> invisivel que o <see cref="EstatisticasDoJogador"/> soma junto
/// com os itens pegos, entao qualquer campo de item serve, inclusive os que so as
/// sinergias usam (explodir ao acertar, golpe pesado, fogo ao renascer, espinhos no escudo).
///
/// A descricao de cada item lista com quem ele combina (<see cref="Parceiros"/>), pra dar
/// pra caçar a dupla.
/// </summary>
public static class Sinergias
{
    private sealed class Dupla
    {
        public string a;
        public string b;
        public ItemPassivo bonus;
    }

    private static List<Dupla> duplas;

    private static List<Dupla> Duplas => duplas ?? (duplas = Montar());

    private static List<Dupla> Montar()
    {
        Color cor = new Color(1f, 0.85f, 0.4f);

        return new List<Dupla>
        {
            D("Runa Tríplice", "Runa Gêmea", new ItemPassivo("Coro de Runas", "Mais um tiro, sem perder ritmo", cor)
                { lagrimasExtras = 1, multiplicaCadencia = 1.43f }),

            D("Óleo Ardente", "Barril de Pólvora", new ItemPassivo("Pólvora em Chamas", "Os tiros explodem ao acertar", cor)
                { explodeAoAcertar = 0.9f }),

            D("Essência Fantasma", "Bússola Maldita", new ItemPassivo("Espírito Caçador", "Tiros fantasmas mais longe e mais fortes", cor)
                { somaAlcance = 2f, multiplicaDano = 1.2f }),

            D("Pena da Fênix", "Óleo Ardente", new ItemPassivo("Renascer em Chamas", "Ao renascer, explode em fogo", cor)
                { fogoAoRenascer = true }),

            D("Escudo Sagrado", "Prego Enferrujado", new ItemPassivo("Bastião de Espinhos", "O escudo solta espinhos ao bloquear", cor)
                { espinhosNoEscudo = true, danoDeEspinhos = 6f }),

            D("Sangue de Vampiro", "Pacto de Sangue", new ItemPassivo("Sede Eterna", "Cura matando mais vezes, dano para cima", cor)
                { inimigosParaCurar = 3, multiplicaDano = 1.1f }),

            D("Orbe Guardião", "Elmo de Duas Faces", new ItemPassivo("Vigília Dupla", "Mais um orbe te protege", cor)
                { orbes = 1 }),

            D("Pedra-Ímã", "Amuleto da Sorte", new ItemPassivo("Mão de Midas", "Mais prêmios, ímã maior e moedas", cor)
                { multiplicaSorte = 1.3f, raioDoIma = 1.5f, moedas = 10 }),

            D("Ponta de Chumbo", "Pedra de Amolar", new ItemPassivo("Golpe de Bigorna", "Cada tiro é um golpe pesado", cor)
                { golpePesado = true, somaDano = 0.5f }),

            D("Hidromel", "Elixir da Pressa", new ItemPassivo("Frenesi", "Ainda mais rápido em tudo", cor)
                { somaCadencia = 0.5f, somaVelocidade = 0.3f }),

            D("Coração de Leão", "Carne Assada", new ItemPassivo("Banquete do Rei", "Mais um coração e mais cura por sala", cor)
                { somaVidaMaxima = 20f, curaAoLimparSala = 10f }),

            D("Elixir de Névoa", "Escudo Sagrado", new ItemPassivo("Névoa Sagrada", "Ainda mais tempo invencível", cor)
                { somaInvencibilidade = 0.4f }),

            D("Brasa da Fúria", "Pacto de Sangue", new ItemPassivo("Ira Sangrenta", "A fúria fica muito mais forte", cor)
                { furia = 0.4f }),

            D("Bolsa do Mercador", "Saco de Moedas", new ItemPassivo("Tesouro do Mercador", "Loja ainda mais barata e moedas", cor)
                { descontoNaLoja = 0.15f, moedas = 15, chaves = 1 }),

            D("Olho de Falcão", "Ponta de Chumbo", new ItemPassivo("Balista", "Tiros rápidos que atravessam", cor)
                { somaVelocidadeDoTiro = 3f, atravessa = true }),
        };
    }

    private static Dupla D(string a, string b, ItemPassivo bonus) => new Dupla { a = a, b = b, bonus = bonus };

    /// <summary>Os bonus das duplas completas em <paramref name="itens"/>.</summary>
    public static List<ItemPassivo> Ativas(IReadOnlyList<ItemPassivo> itens)
    {
        List<ItemPassivo> bonus = new List<ItemPassivo>();
        HashSet<string> nomes = new HashSet<string>();

        foreach (ItemPassivo i in itens)
            nomes.Add(i.nome);

        foreach (Dupla d in Duplas)
            if (nomes.Contains(d.a) && nomes.Contains(d.b))
                bonus.Add(d.bonus);

        return bonus;
    }

    /// <summary>"Combina com: X, Y" pra descricao do item, ou vazio.</summary>
    public static string Parceiros(string nome)
    {
        List<string> outros = new List<string>();

        foreach (Dupla d in Duplas)
        {
            if (d.a == nome)
                outros.Add(d.b);
            else if (d.b == nome)
                outros.Add(d.a);
        }

        return outros.Count == 0 ? "" : "Combina com: " + string.Join(", ", outros) + ".";
    }

    /// <summary>Todas as sinergias (pra lista de conquistas).</summary>
    public static int Quantas => Duplas.Count;
}
