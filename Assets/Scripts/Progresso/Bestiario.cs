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
        new Ficha(TipoDeInimigo.Perseguidor, "Cão Infernal", "Vem reto e rápido, farejando, e dá um bote quando chega perto."),
        new Ficha(TipoDeInimigo.Atirador, "Bruxo", "Aparece, atira três orbes e some."),
        new Ficha(TipoDeInimigo.Investidor, "Minotauro", "Entrou na linha dele, ele investe até bater."),
        new Ficha(TipoDeInimigo.Saltador, "Gosma", "Pula atrás de você."),
        new Ficha(TipoDeInimigo.Geleia, "Geleia", "Pula sem rumo pela sala."),
        new Ficha(TipoDeInimigo.Sentinela, "Cavaleiro Canhão", "Parado, atira nas quatro direções."),
        new Ficha(TipoDeInimigo.Divisor, "Bolha", "Quica pela sala como bola de bilhar; ao morrer, se divide."),
        new Ficha(TipoDeInimigo.DivisorPequeno, "Bolhinha", "Quica ainda mais rápido."),
        new Ficha(TipoDeInimigo.Demonio, "Demônio", "Faz a curva e chega pelo seu lado; corta à frente."),
        new Ficha(TipoDeInimigo.MonstroDeSangue, "Monstro de Sangue", "Anda em pulsos, como coração; espirra gotas."),
        new Ficha(TipoDeInimigo.Morcego, "Morcego", "Circula e mergulha em linha reta."),
        new Ficha(TipoDeInimigo.Morceguinho, "Morceguinho", "Voa em enxame, aos trancos."),
        new Ficha(TipoDeInimigo.CavaleiroLanca, "Cavaleiro da Lança", "Se alinha com você e investe."),
        new Ficha(TipoDeInimigo.CavaleiroEscudo, "Cavaleiro do Escudo", "Bloqueia tiros de frente: dê a volta."),
        new Ficha(TipoDeInimigo.DemonioTridente, "Demônio do Tridente", "Chega perto e se afasta sem parar; arremessa o tridente."),
        new Ficha(TipoDeInimigo.DemonioLaminas, "Demônio das Lâminas", "Avança em três arrancadas."),
        new Ficha(TipoDeInimigo.DemonioArqueiro, "Demônio Arqueiro", "Foge pro fundo da sala e atira três flechas em leque."),
        new Ficha(TipoDeInimigo.Demonia, "Demônia", "Vai e volta num arco em volta de você, atirando em leque."),
        new Ficha(TipoDeInimigo.DemoniaFoice, "Demônia da Foice", "Chega girando em espiral; gira a foice."),
        new Ficha(TipoDeInimigo.FogoFatuo, "Fogo-Fátuo", "Orbita, apaga e solta chamas em cruz."),
        new Ficha(TipoDeInimigo.Orc, "Orc", "Avança aos trancos: passada, para, passada."),
        new Ficha(TipoDeInimigo.OrcBlindado, "Orc Blindado", "Marcha reto e só corrige o rumo de vez em quando."),
        new Ficha(TipoDeInimigo.OrcElite, "Orc de Elite", "Corre pra onde você VAI estar; ferido, entra em fúria."),
        new Ficha(TipoDeInimigo.OrcMontado, "Orc Montado", "Galopa de um lado pro outro e investe na sua linha."),
        new Ficha(TipoDeInimigo.EsqueletoGuerreiro, "Esqueleto Guerreiro", "Em bando, cada um vem por um lado: te cercam."),
        new Ficha(TipoDeInimigo.EsqueletoBlindado, "Esqueleto Blindado", "Avança, recua um passo e avança de novo."),
        new Ficha(TipoDeInimigo.EsqueletoEspadao, "Esqueleto do Espadão", "Fica em guarda; perto, avança. O golpe solta onda."),
        new Ficha(TipoDeInimigo.EsqueletoArqueiro, "Esqueleto Arqueiro", "Corre pra sua linha e atira reto."),
        new Ficha(TipoDeInimigo.Lobisomem, "Lobisomem", "Espreita e arranca em disparada."),
        new Ficha(TipoDeInimigo.Urso, "Urso", "De longe vem correndo; perto, pisoteia."),
        new Ficha(TipoDeInimigo.Necromante, "Necromante", "Se esconde no canto mais longe e levanta esqueletos."),

        new Ficha(TipoDeInimigo.Chefe, "Golem de Magma", "Anel, rajada e investida; chama ajuda.", true),
        new Ficha(TipoDeInimigo.ChefeMinotauro, "Minotauro Furioso", "Chifrada que ricocheteia, pisão e giro.", true),
        new Ficha(TipoDeInimigo.ChefeLobisomem, "Lobisomem Alfa", "Botes em sequência, cerco e uivo.", true),
        new Ficha(TipoDeInimigo.ChefeSaltador, "Demônio do Martelo", "Pula, espirala e cospe bolhas.", true),
        new Ficha(TipoDeInimigo.ChefeOrc, "Senhor da Guerra", "Machadada, machados que voltam e investida.", true),
        new Ficha(TipoDeInimigo.ChefeNecromante, "Rei Necromante", "Some, ergue ossos e muralhas de tiro.", true),
        new Ficha(TipoDeInimigo.ChefeFinal, "Olho do Abismo", "O fim de tudo. Três fases.", true),
    };
}
