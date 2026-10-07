using System.Collections.Generic;
using UnityEngine;

/// <summary>O que um item ativo faz ao ser usado (ver <see cref="ItemAtivoDoJogador"/>).</summary>
public enum TipoDeAtivo
{
    Nenhum,
    EspiralDoTempo,
    CristaisDoTrovao,
    MoedaDoDestino,
    CometaDeFogo,
    PessegoEncantado,
    BombaEterna,
}

/// <summary>
/// Um item (veio do jogo antigo): nome, uma linha curta, a descricao, o icone do pacote e o que ele
/// muda no jogador (<see cref="EstatisticasDoJogador"/>). Os ativos dizem o <see cref="Ativo"/> e
/// quantos inimigos precisa derrotar pra recarregar. A lista toda fica no <see cref="CatalogoDeItens"/>.
/// </summary>
public sealed class ItemPassivo
{
    public readonly string Nome;
    public readonly string Resumo;
    public string Descricao;
    public int Icone;
    public System.Action<EstatisticasDoJogador> Aplicar;
    public TipoDeAtivo Ativo;

    /// <summary>Inimigos derrotados pra recarregar (so dos ativos).</summary>
    public int Recarga = 15;

    /// <summary>Preco na loja, em moedas.</summary>
    public int Preco = 15;

    /// <summary>Sinergia: nao sai em bau nem loja, so aparece juntando os dois itens dela.</summary>
    public bool EhSinergia;

    public ItemPassivo(string nome, string resumo)
    {
        Nome = nome;
        Resumo = resumo;
    }

    public bool EhAtivo => Ativo != TipoDeAtivo.Nenhum;

    public Sprite Desenho => ArteDoAntigo.Icone(Icone);
}

/// <summary>Todos os itens, pra sortear (baus, pedestais, loja, altar) e pra achar pelo nome (o salvo).</summary>
public static class CatalogoDeItens
{
    private static List<ItemPassivo> todos;

    public static IReadOnlyList<ItemPassivo> Todos => todos ?? (todos = Montar());

    public static ItemPassivo PeloNome(string nome)
    {
        foreach (ItemPassivo item in Todos)
        {
            if (item.Nome == nome)
                return item;
        }

        foreach (Sinergias.Sinergia s in Sinergias.Todas)
        {
            if (s.Bonus.Nome == nome)
                return s.Bonus;
        }

        return null;
    }

    /// <summary>Um item que o jogador ainda nao tem (ativos so se <paramref name="comAtivos"/>).</summary>
    public static ItemPassivo Sortear(EstatisticasDoJogador jogador, bool comAtivos = true)
    {
        List<ItemPassivo> possiveis = new List<ItemPassivo>();

        foreach (ItemPassivo item in Todos)
        {
            if ((comAtivos || !item.EhAtivo) && (jogador == null || !jogador.Tem(item)))
                possiveis.Add(item);
        }

        if (possiveis.Count == 0)
            possiveis.AddRange(Todos);

        return possiveis[Random.Range(0, possiveis.Count)];
    }

    private static ItemPassivo P(string nome, string resumo, int icone, string descricao, System.Action<EstatisticasDoJogador> efeito, int preco = 15) =>
        new ItemPassivo(nome, resumo) { Icone = icone, Descricao = descricao, Aplicar = efeito, Preco = preco };

    private static ItemPassivo A(string nome, string resumo, int icone, string descricao, TipoDeAtivo ativo, int recarga) =>
        new ItemPassivo(nome, resumo) { Icone = icone, Descricao = descricao, Ativo = ativo, Recarga = recarga, Preco = 20 };

    private static List<ItemPassivo> Montar() => new List<ItemPassivo>
    {
        P("Elixir da Pressa", "Ataca mais rápido", 266, "Atira 25% mais rápido.", e => e.CadenciaVezes *= 1.25f),
        P("Pedra de Amolar", "Dano para cima", 2129, "Cada tiro causa +1.5 de dano.", e => e.DanoMais += 1.5f),
        P("Ferradura Encantada", "Velocidade para cima", 696, "Anda mais rápido.", e => e.VelocidadeMais += 1f),
        P("Runa Tríplice", "Três tiros por vez", 385, "Solta mais dois tiros em leque, mas atira 30% mais devagar.",
          e => { e.TirosExtras += 2; e.CadenciaVezes *= 0.7f; }, 20),
        P("Runa Gêmea", "Dois tiros por vez", 236, "Solta mais um tiro de uma vez e ganha +0.3 de dano.",
          e => { e.TirosExtras += 1; e.DanoMais += 0.3f; }, 20),
        P("Olho de Falcão", "Alcance e tiro mais rápido", 719, "Os tiros vão mais longe e voam mais rápido.",
          e => { e.AlcanceVezes *= 1.35f; e.VelocidadeDoTiroVezes *= 1.25f; }),
        P("Coração de Leão", "Vida máxima para cima", 659, "Ganha um coração a mais de vida máxima, já cheio.", e => e.MudarVidaMaxima(2f), 20),
        P("Ponta de Chumbo", "Tiros grandes e pesados", 654, "Dano x1.5 e tiros maiores, mas o tiro e o herói ficam mais lentos.",
          e => { e.DanoVezes *= 1.5f; e.TamanhoVezes *= 1.4f; e.VelocidadeDoTiroVezes *= 0.8f; e.VelocidadeMais -= 0.4f; }),
        P("Hidromel", "Tudo mais rápido", 529, "Anda mais rápido e atira mais rápido.", e => { e.VelocidadeMais += 0.6f; e.CadenciaVezes *= 1.15f; }),
        P("Saco de Moedas", "Moedas, chave e bombas", 158, "Ganha na hora 10 moedas, 1 chave e 3 bombas.", e => e.Bolsa?.Ganhar(10, 1, 3), 10),
        P("Essência Fantasma", "Tiros atravessam inimigos", 653, "Os tiros atravessam os inimigos e acertam quem estiver atrás.", e => e.Atravessa = true, 20),
        P("Bússola Maldita", "Tiros perseguem inimigos", 2184, "Os tiros fazem curva sozinhos atrás do inimigo mais perto.", e => e.Persegue = true, 20),
        P("Elmo de Duas Faces", "Ataca pra trás também", 691, "Cada disparo solta também um tiro para trás.", e => e.TiroPraTras = true),
        P("Óleo Ardente", "Tiros de fogo, dano para cima", 438, "Tiros em chamas: dano x1.3 e voam um pouco mais rápido.",
          e => { e.DanoVezes *= 1.3f; e.VelocidadeDoTiroVezes *= 1.1f; }),
        P("Pena da Fênix", "Uma segunda chance", 7, "Uma vez por partida: quando a vida acabaria, você renasce com metade da vida.", e => e.Fenix = true, 25),
        P("Escudo Sagrado", "Bloqueia o primeiro golpe", 665, "O primeiro golpe que você levaria em cada andar é bloqueado.", e => e.Escudo = true, 20),
        P("Sangue de Vampiro", "Matar cura", 742, "A cada 12 inimigos derrotados, recupera meio coração.", e => e.Vampiro = true, 20),
        P("Prego Enferrujado", "Quem bate, apanha", 1444, "Quando você leva dano, espinhos saem de você e ferem os inimigos em volta.", e => e.Prego = true),
        P("Pedra-Ímã", "Coletáveis vêm até você", 117, "Moedas, chaves, bombas e corações perto de você são puxados sozinhos.", e => e.Ima = true, 10),
        P("Amuleto da Sorte", "Mais prêmios", 668, "Os inimigos soltam prêmios com bem mais frequência.", e => e.Sorte *= 1.8f),
        P("Bolsa do Mercador", "Loja mais barata", 160, "Tudo na loja custa 35% menos. Vem com 5 moedas.", e => { e.Desconto *= 0.65f; e.Bolsa?.Ganhar(5, 0, 0); }, 10),
        P("Brasa da Fúria", "Mais forte ferido", 993, "Com um coração de vida ou menos, seus tiros causam 60% mais dano.", e => e.Brasa = true),
        P("Elixir de Névoa", "Mais tempo invencível", 123, "Depois de levar dano, fica invencível por bem mais tempo.", e => e.NevoaExtra += 0.8f),
        P("Orbe Guardião", "Um orbe te protege", 335, "Um orbe gira em volta de você: desmancha tiros inimigos e fere quem encostar.", e => e.Orbes += 1, 20),
        P("Barril de Pólvora", "Bombas mais fortes", 340, "Suas bombas explodem numa área 50% maior e com 50% mais dano. Vem com 2 bombas.",
          e => { e.PolvoraVezes *= 1.5f; e.Bolsa?.Ganhar(0, 0, 2); }, 10),
        P("Carne Assada", "Cura a cada andar", 486, "Ao limpar um andar de inimigos, recupera meio coração.", e => e.CuraAoLimpar += 1f),
        P("Pacto de Sangue", "Poder por um preço", 289, "Seus tiros causam 60% mais dano, mas você perde um coração de vida máxima.",
          e => { e.DanoVezes *= 1.6f; e.MudarVidaMaxima(-2f); }),
        A("Espiral do Tempo", "Ativo: o tempo se arrasta", 720, "Ativo (R): por 6 segundos os inimigos andam bem devagar.", TipoDeAtivo.EspiralDoTempo, 18),
        A("Cristais do Trovão", "Ativo: raio em todos", 159, "Ativo (R): um raio cai em cada inimigo perto (30 de dano).", TipoDeAtivo.CristaisDoTrovao, 20),
        A("Moeda do Destino", "Ativo: troca os itens", 131, "Ativo (R): troca por outros os itens dos pedestais perto.", TipoDeAtivo.MoedaDoDestino, 25),
        A("Cometa de Fogo", "Ativo: anel de chamas", 1002, "Ativo (R): solta 12 bolas de fogo em volta de você.", TipoDeAtivo.CometaDeFogo, 10),
        A("Pêssego Encantado", "Ativo: cura um coração", 440, "Ativo (R): recupera um coração.", TipoDeAtivo.PessegoEncantado, 25),
        A("Bomba Eterna", "Ativo: bomba de graça", 778, "Ativo (R): solta uma bomba sem gastar as suas.", TipoDeAtivo.BombaEterna, 8),
    };
}

/// <summary>
/// As sinergias (vieram do jogo antigo): ter os dois itens de uma da um bonus a mais, anunciado na
/// tela ("Sinergia: Polvora em Chamas!").
/// </summary>
public static class Sinergias
{
    public sealed class Sinergia
    {
        public readonly string A;
        public readonly string B;
        public readonly ItemPassivo Bonus;

        public Sinergia(string a, string b, ItemPassivo bonus)
        {
            A = a;
            B = b;
            Bonus = bonus;
            bonus.EhSinergia = true;
        }
    }

    private static List<Sinergia> todas;

    public static IReadOnlyList<Sinergia> Todas => todas ?? (todas = Montar());

    private static Sinergia S(string a, string b, string nome, string resumo, System.Action<EstatisticasDoJogador> efeito) =>
        new Sinergia(a, b, new ItemPassivo(nome, resumo) { Descricao = resumo, Aplicar = efeito, Icone = 2154 });

    private static List<Sinergia> Montar() => new List<Sinergia>
    {
        S("Runa Tríplice", "Runa Gêmea", "Coro de Runas", "Mais um tiro, sem perder ritmo", e => { e.TirosExtras += 1; e.CadenciaVezes *= 1.3f; }),
        S("Óleo Ardente", "Barril de Pólvora", "Pólvora em Chamas", "Os tiros explodem ao acertar", e => e.ExplodeAoAcertar = true),
        S("Essência Fantasma", "Bússola Maldita", "Espírito Caçador", "Tiros fantasmas mais longe e mais fortes", e => { e.AlcanceVezes *= 1.3f; e.DanoVezes *= 1.2f; }),
        S("Sangue de Vampiro", "Pacto de Sangue", "Sede Eterna", "Cura matando mais vezes, dano para cima", e => { e.VampiroCada = 7; e.DanoVezes *= 1.15f; }),
        S("Orbe Guardião", "Elmo de Duas Faces", "Vigília Dupla", "Mais um orbe te protege", e => e.Orbes += 1),
        S("Pedra-Ímã", "Amuleto da Sorte", "Mão de Midas", "Mais prêmios e moedas", e => { e.Sorte *= 1.3f; e.Bolsa?.Ganhar(10, 0, 0); }),
        S("Ponta de Chumbo", "Pedra de Amolar", "Golpe de Bigorna", "Cada tiro é um golpe pesado", e => e.DanoMais += 2f),
        S("Hidromel", "Elixir da Pressa", "Frenesi", "Ainda mais rápido em tudo", e => { e.CadenciaVezes *= 1.2f; e.VelocidadeMais += 0.5f; }),
        S("Coração de Leão", "Carne Assada", "Banquete do Rei", "Mais um coração e mais cura por andar", e => { e.MudarVidaMaxima(2f); e.CuraAoLimpar += 1f; }),
        S("Elixir de Névoa", "Escudo Sagrado", "Névoa Sagrada", "Ainda mais tempo invencível", e => e.NevoaExtra += 0.6f),
        S("Brasa da Fúria", "Pacto de Sangue", "Ira Sangrenta", "A fúria fica muito mais forte", e => e.BrasaVezes = 2.2f),
        S("Bolsa do Mercador", "Saco de Moedas", "Tesouro do Mercador", "Loja ainda mais barata e moedas", e => { e.Desconto *= 0.8f; e.Bolsa?.Ganhar(10, 0, 0); }),
        S("Olho de Falcão", "Ponta de Chumbo", "Balista", "Tiros rápidos que atravessam", e => { e.VelocidadeDoTiroVezes *= 1.3f; e.Atravessa = true; }),
    };
}
