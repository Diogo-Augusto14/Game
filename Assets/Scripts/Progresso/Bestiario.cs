using System.Collections.Generic;

/// <summary>
/// Nome e uma linha de "como luta" de cada inimigo, pra tela do bestiario (<see cref="TelaDeProgresso"/>).
/// O Id e o nome do prefab (o <see cref="Registro"/> conta por ele).
/// </summary>
public static class Bestiario
{
    public readonly struct Ficha
    {
        public readonly string Id;
        public readonly string Nome;
        public readonly string ComoLuta;
        public readonly bool Chefe;

        public Ficha(string id, string nome, string comoLuta, bool chefe = false)
        {
            Id = id;
            Nome = nome;
            ComoLuta = comoLuta;
            Chefe = chefe;
        }
    }

    private static List<Ficha> fichas;

    /// <summary>Todos, na ordem do bestiario: os comuns primeiro (mundo a mundo), os chefes no fim.</summary>
    public static IReadOnlyList<Ficha> Todas => fichas ?? (fichas = Montar());

    public static string Nome(string id)
    {
        foreach (Ficha f in Todas)
        {
            if (f.Id == id)
                return f.Nome;
        }

        return id;
    }

    private static List<Ficha> Montar() => new List<Ficha>
    {
        new Ficha("Esqueleto", "Esqueleto Guerreiro", "Vem chegando e investe quando fica perto."),
        new Ficha("Gosma", "Gosma", "Pula em você e estoura em gotas ao morrer."),
        new Ficha("Bruxo", "Bruxo", "Some, reaparece perto e atira três orbes em leque."),
        new Ficha("EsqueletoArqueiro", "Esqueleto Arqueiro", "Mantém distância e atira flechas em leque."),
        new Ficha("Geleia", "Geleia", "Pula e espirra gotas quando cai."),
        new Ficha("Morceguinho", "Morceguinho", "Voa aos trancos, por cima dos buracos."),
        new Ficha("Orc", "Orc", "Chega perto e dá machadadas em leque."),
        new Ficha("Bolha", "Bolha", "Pula em você; ao morrer, se divide em duas."),
        new Ficha("CaoInfernal", "Cão Infernal", "Rápido, dá um bote curto quando chega perto."),
        new Ficha("Morcego", "Morcego", "Circula em volta e mergulha em linha reta."),
        new Ficha("Lobisomem", "Lobisomem", "Rodeia e arranca em duas disparadas seguidas."),
        new Ficha("OrcBlindado", "Orc Blindado", "Lento, mas a armadura segura metade do dano."),
        new Ficha("EsqueletoBlindado", "Esqueleto Blindado", "A armadura segura metade do dano."),
        new Ficha("Necromante", "Necromante", "Foge, solta anéis e levanta esqueletos do chão."),
        new Ficha("CavaleiroLanca", "Cavaleiro da Lança", "Lento, mas investe longe com a lança."),
        new Ficha("Urso", "Urso", "Não é empurrado; bate no chão e solta pedras."),
        new Ficha("OrcMontado", "Orc Montado", "Galopa em duas investidas seguidas."),
        new Ficha("Demonio", "Demônio", "Rápido; corta à frente."),
        new Ficha("Demonia", "Demônia", "Rodeia você atirando orbes em leque."),
        new Ficha("DemoniaFoice", "Demônia da Foice", "Gira em volta e solta um anel de cortes."),
        new Ficha("FogoFatuo", "Fogo-Fátuo", "Orbita, some e solta chamas em cruz."),
        new Ficha("MonstroDeSangue", "Monstro de Sangue", "Lento e forte; espirra um anel de sangue."),
        new Ficha("DemonioTridente", "Demônio do Tridente", "Mantém distância e arremessa o tridente."),
        new Ficha("DemonioArqueiro", "Demônio Arqueiro", "Foge e atira flechas de fogo em leque."),
        new Ficha("Olho", "Olho", "Gira uma espiral de tiros."),
        new Ficha("DemonioLaminas", "Demônio das Lâminas", "Avança em três arrancadas."),
        new Ficha("OrcElite", "Orc de Elite", "Ferido, entra em fúria e fica bem mais rápido."),
        new Ficha("EsqueletoEspadao", "Esqueleto do Espadão", "O golpe do espadão solta uma onda de corte."),
        new Ficha("CavaleiroEscudo", "Cavaleiro do Escudo", "Bloqueia tiros de frente: dê a volta, ou acerte quando ele ataca."),
        new Ficha("CavaleiroCanhao", "Cavaleiro Canhão", "Parado, dispara balas de canhão em rajada."),
        new Ficha("EsqueletoDaPrisao", "Esqueleto da Prisão", "Chega perto e dá estocadas com a espada."),
        new Ficha("EsqueletoEscudeiro", "Esqueleto Escudeiro", "O escudo segura os tiros de frente."),
        new Ficha("EsqueletoMago", "Esqueleto Mago", "Foge e atira rajadas de orbes verdes."),
        new Ficha("Assassino", "Assassino", "Rodeia e salta em duas arrancadas seguidas."),
        new Ficha("EsqueletoDaCripta", "Esqueleto da Cripta", "Vem em grupo e corta à frente."),
        new Ficha("EsqueletoRubro", "Esqueleto Rubro", "Ferido, entra em fúria e corta mais rápido."),
        new Ficha("Aranha", "Aranha", "Corre aos trancos e dá botes curtos."),
        new Ficha("AranhaVenenosa", "Aranha Venenosa", "Mantém distância e cospe veneno em dupla."),
        new Ficha("Minhocao", "Minhocão", "Some embaixo da terra e sai perto de você cuspindo um anel."),
        new Ficha("MinhocaoSombrio", "Minhocão Sombrio", "Como o minhocão, com anéis em três voltas."),
        new Ficha("GoblinBrutamonte", "Goblin Brutamonte", "Não é empurrado; a pancada no chão solta pedras em volta."),
        new Ficha("GoblinDaClava", "Goblin da Clava", "Clavadas em leque; ferido, entra em fúria."),
        new Ficha("GoblinAssassino", "Goblin Assassino", "Rápido: três arrancadas com as adagas."),
        new Ficha("PoteMimico", "Pote Mímico", "Parece um pote; acorda cuspindo um leque roxo."),
        new Ficha("Minotauro", "Minotauro", "Investidas triplas, pisão e cortes.", true),
        new Ficha("LobisomemAlfa", "Lobisomem Alfa", "Botes em sequência, garras e uivo que chama cães.", true),
        new Ficha("SenhorDaGuerra", "Senhor da Guerra", "Machadada, machados que voltam e grito de guerra.", true),
        new Ficha("DemonioDoMartelo", "Demônio do Martelo", "Pula em você, espirala e cospe fogo no chão.", true),
        new Ficha("ReiNecromante", "Rei Necromante", "Some, ergue ossos do chão e levanta os mortos.", true),
        new Ficha("Golem", "Golem de Brasa", "Espirais de fogo, leques e anéis que aceleram.", true),
        new Ficha("OlhoDoAbismo", "Olho do Abismo", "O fim de tudo. Três fases.", true),
    };
}
