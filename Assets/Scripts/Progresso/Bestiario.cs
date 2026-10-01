using System.Collections.Generic;

/// <summary>
/// Nome e uma linha de "como luta" de cada inimigo, pra tela do bestiario
/// (<see cref="TelaDeProgresso"/>). Os nomes do objeto na cena continuam os de antes
/// (o estilo do tiro procura por eles); estes sao os de mostrar.
/// </summary>
public static class Bestiario
{
    public readonly struct Ficha
    {
        public readonly TipoDeInimigo Tipo;
        public readonly string Nome;
        public readonly string ComoLuta;
        public readonly bool Chefe;

        public Ficha(TipoDeInimigo tipo, string nome, string comoLuta, bool chefe = false)
        {
            Tipo = tipo;
            Nome = nome;
            ComoLuta = comoLuta;
            Chefe = chefe;
        }
    }

    private static List<Ficha> fichas;

    /// <summary>Todos, na ordem do bestiario: os comuns primeiro, os chefes no fim.</summary>
    public static IReadOnlyList<Ficha> Todas => fichas ?? (fichas = Montar());

    public static string Nome(TipoDeInimigo tipo)
    {
        foreach (Ficha f in Todas)
            if (f.Tipo == tipo)
                return f.Nome;

        return tipo.ToString();
    }

    private static List<Ficha> Montar() => new List<Ficha>
    {
        new Ficha(TipoDeInimigo.Perseguidor, "Cão Infernal", "Persegue e dá um bote quando chega perto."),
        new Ficha(TipoDeInimigo.Atirador, "Bruxo", "Aparece, atira três orbes e some."),
        new Ficha(TipoDeInimigo.Investidor, "Minotauro", "Entrou na linha dele, ele investe até bater."),
        new Ficha(TipoDeInimigo.Saltador, "Gosma", "Pula atrás de você."),
        new Ficha(TipoDeInimigo.Geleia, "Geleia", "Pula sem rumo pela sala."),
        new Ficha(TipoDeInimigo.Sentinela, "Cavaleiro Canhão", "Parado, atira nas quatro direções."),
        new Ficha(TipoDeInimigo.Divisor, "Bolha", "Ao morrer, se divide em duas."),
        new Ficha(TipoDeInimigo.DivisorPequeno, "Bolhinha", "O que sobra da bolha."),
        new Ficha(TipoDeInimigo.Demonio, "Demônio", "Ergue a espada e corta à frente."),
        new Ficha(TipoDeInimigo.MonstroDeSangue, "Monstro de Sangue", "Lento; espirra um anel de gotas."),
        new Ficha(TipoDeInimigo.GoblinTocha, "Goblin da Tocha", "Corre e golpeia com a tocha."),
        new Ficha(TipoDeInimigo.GoblinDinamite, "Goblin da Dinamite", "De longe, joga dinamite onde você está."),
        new Ficha(TipoDeInimigo.Barril, "Barril de Pólvora", "Parece um barril... até correr e explodir."),
        new Ficha(TipoDeInimigo.Arqueiro, "Arqueiro Sombrio", "Mantém distância e atira flechas retas."),
        new Ficha(TipoDeInimigo.Esqueleto, "Esqueleto", "Anda até você e golpeia com a espada."),
        new Ficha(TipoDeInimigo.EsqueletoFoice, "Esqueleto da Foice", "Perto, gira a foice duas vezes."),
        new Ficha(TipoDeInimigo.Vampiro, "Vampiro", "Dá um bote; se acerta, se cura."),
        new Ficha(TipoDeInimigo.Morcego, "Morcego", "Circula e mergulha em linha reta."),
        new Ficha(TipoDeInimigo.Morceguinho, "Morceguinho", "Voa em enxame, aos trancos."),
        new Ficha(TipoDeInimigo.CavaleiroLanca, "Cavaleiro da Lança", "Se alinha com você e investe."),
        new Ficha(TipoDeInimigo.CavaleiroEscudo, "Cavaleiro do Escudo", "Bloqueia tiros de frente: dê a volta."),
        new Ficha(TipoDeInimigo.DemonioTridente, "Demônio do Tridente", "Treme e arremessa o tridente."),
        new Ficha(TipoDeInimigo.DemonioLaminas, "Demônio das Lâminas", "Avança em três arrancadas."),
        new Ficha(TipoDeInimigo.DemonioArqueiro, "Demônio Arqueiro", "Atira três flechas em leque."),
        new Ficha(TipoDeInimigo.Demonia, "Demônia", "Rodeia você atirando em leque."),
        new Ficha(TipoDeInimigo.DemoniaFoice, "Demônia da Foice", "Gira a foice em volta de si."),
        new Ficha(TipoDeInimigo.FogoFatuo, "Fogo-Fátuo", "Orbita, apaga e solta chamas em cruz."),
        new Ficha(TipoDeInimigo.Orc, "Orc", "Brutamontes de machado."),
        new Ficha(TipoDeInimigo.OrcBlindado, "Orc Blindado", "A armadura segura parte do dano."),
        new Ficha(TipoDeInimigo.OrcElite, "Orc de Elite", "Ferido, entra em fúria."),
        new Ficha(TipoDeInimigo.OrcMontado, "Orc Montado", "Investe quando você entra na linha dele."),
        new Ficha(TipoDeInimigo.EsqueletoGuerreiro, "Esqueleto Guerreiro", "O soldado do necromante."),
        new Ficha(TipoDeInimigo.EsqueletoBlindado, "Esqueleto Blindado", "Nada interrompe o golpe dele."),
        new Ficha(TipoDeInimigo.EsqueletoEspadao, "Esqueleto do Espadão", "O golpe solta uma onda de corte."),
        new Ficha(TipoDeInimigo.EsqueletoArqueiro, "Esqueleto Arqueiro", "Corre pra sua linha e atira reto."),
        new Ficha(TipoDeInimigo.Lobisomem, "Lobisomem", "Espreita e arranca em disparada."),
        new Ficha(TipoDeInimigo.Urso, "Urso", "Pisoteia e solta um anel de pedras."),
        new Ficha(TipoDeInimigo.Necromante, "Necromante", "Foge e levanta esqueletos."),

        new Ficha(TipoDeInimigo.Chefe, "Golem de Magma", "Anel, rajada e investida; chama ajuda.", true),
        new Ficha(TipoDeInimigo.ChefeMinotauro, "Minotauro Furioso", "Chifrada que ricocheteia, pisão e giro.", true),
        new Ficha(TipoDeInimigo.ChefeLobisomem, "Lobisomem Alfa", "Botes em sequência, cerco e uivo.", true),
        new Ficha(TipoDeInimigo.ChefeSaltador, "Demônio do Martelo", "Pula, espirala e cospe bolhas.", true),
        new Ficha(TipoDeInimigo.ChefeOrc, "Senhor da Guerra", "Machadada, machados que voltam e investida.", true),
        new Ficha(TipoDeInimigo.ChefeNecromante, "Rei Necromante", "Some, ergue ossos e muralhas de tiro.", true),
        new Ficha(TipoDeInimigo.ChefeFinal, "Olho do Abismo", "O fim de tudo. Três fases.", true),
    };
}
