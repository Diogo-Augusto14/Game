using System.Collections.Generic;
using UnityEngine;

/// <summary>Quem fala no acampamento.</summary>
public enum QuemFala
{
    Mercador,
    Ferreiro,
    Apostador,
}

/// <summary>
/// O que o pessoal do acampamento fala (1.21.0), como a casa do Hades: morrer vira conversa e a historia sai
/// aos poucos. Cada vez que o jogador chega pra conversar (<see cref="DaVez"/>), a pessoa:
///
///   1. comenta a ultima partida, uma vez por partida nova (<see cref="GuardarFim"/>: onde caiu, quem
///      matou, se foi um chefe, se trouxe planta, se zerou); na primeira vez de todas, se apresenta;
///   2. conta o proximo pedaco da historia dela, um por partida, quando o jogador ja chegou longe o
///      bastante pra ouvir (andar mais fundo, plantas, vitorias);
///   3. sem nada novo, solta uma frase qualquer.
///
/// Tudo fica no PlayerPrefs. Os textos estao todos aqui (em portugues; o ingles sai do Idioma): da pra
/// mudar a historia sem mexer em mais nada.
/// </summary>
public static class Falas
{
    private const string Prefixo = "ThePrettie.falas.";

    private readonly struct Linha
    {
        public readonly string Texto;
        public readonly System.Func<bool> Pode;

        public Linha(string texto, System.Func<bool> pode = null)
        {
            Texto = texto;
            Pode = pode;
        }
    }

    // ---------------- quem e quem ----------------
    /// <summary>O nome inteiro (em cima da caixa da conversa).</summary>
    public static string Nome(QuemFala quem)
    {
        switch (quem)
        {
            case QuemFala.Ferreiro: return Idioma.T("Mestre Ferraz, o ferreiro");
            case QuemFala.Apostador: return Idioma.T("O Apostador");
            default: return Idioma.T("Bento, o mercador");
        }
    }

    /// <summary>O nome curto (na dica "conversar com ...").</summary>
    public static string Curto(QuemFala quem)
    {
        switch (quem)
        {
            case QuemFala.Ferreiro: return Idioma.T("Mestre Ferraz");
            case QuemFala.Apostador: return Idioma.T("o Apostador");
            default: return Idioma.T("Bento");
        }
    }

    // ---------------- a historia (um pedaco por partida) ----------------
    private static Linha[] Historia(QuemFala quem)
    {
        switch (quem)
        {
            case QuemFala.Ferreiro:
                return new[]
                {
                    new Linha("Não sou de falar. Traz planta que eu aprendo. Traz moeda que eu forjo."),
                    new Linha("Essa planta que você trouxe é desenho antigo. Quem fez sabia o que estava fazendo.", () => Desbloqueios.Aprendidas >= 1),
                    new Linha("As armas lá de baixo são do mesmo ferro destas grades. Pensa nisso quando bater.", () => Registro.MaisFundo >= 3),
                    new Linha("Já são várias plantas. Cada mundo esconde as dele, e os guardiões ficam com as melhores.", () => Desbloqueios.Aprendidas >= 4),
                    new Linha("Fui eu que forjei as correntes desta prisão. Antes de virar prisioneiro dela.", () => Registro.MaisFundo >= 7),
                    new Linha("O Olho caiu e as correntes afrouxaram. Eu sinto isso no ferro.", () => Progresso.Vitorias >= 1),
                };

            case QuemFala.Apostador:
                return new[]
                {
                    new Linha("Aposto três moedas que você não passa do segundo andar. Brincadeira... ou não."),
                    new Linha("Aqui embaixo tudo é sorte: o baú que vem, o que não vem, a porta que você escolhe.", () => Registro.Partidas >= 3),
                    new Linha("Tem alguém lá no fundo que também aposta. Só que ele aposta com a gente.", () => Registro.MaisFundo >= 3),
                    new Linha("Sabe por que os campeões brilham? Alguém lá embaixo dá poder pra eles, só pra ver quem aguenta.", () => Registro.MaisFundo >= 5),
                    new Linha("Eu vi o Olho do Abismo uma vez, de longe. Ele não te olha. Ele te conta.", () => Registro.MaisFundo >= 9),
                    new Linha("Você ganhou a maior aposta desta prisão. Mas a casa sempre pede revanche.", () => Progresso.Vitorias >= 1),
                };

            default:
                return new[]
                {
                    new Linha("Eu também caí aqui, sabia? Três anos naquela cela até alguém aparecer com uma chave. Agora eu vendo, e não pergunto de onde vem."),
                    new Linha("Quem desce não volta, quem volta não fala. Mas as moedas sempre sobem. Engraçado, né?", () => Registro.Partidas >= 2),
                    new Linha("Cada mundo lá embaixo tem um guardião. Dizem que todos obedecem à mesma coisa.", () => Registro.MaisFundo >= 3),
                    new Linha("Você saiu do Porão! Depois vêm as Catacumbas e a Cripta. Eu nunca passei da primeira escada.", () => Registro.MaisFundo >= 4),
                    new Linha("Os ossos das Catacumbas, os mortos da Cripta... alguém junta tudo isso lá no fundo. Alguma coisa que não pisca.", () => Registro.MaisFundo >= 7),
                    new Linha("Você viu o Olho do Abismo e voltou. Então a prisão inteira mentiu pra gente esse tempo todo.", () => Progresso.Vitorias >= 1),
                };
        }
    }

    // ---------------- a primeira vez ----------------
    private static string Primeira(QuemFala quem)
    {
        switch (quem)
        {
            case QuemFala.Ferreiro: return Idioma.T("Hm. Novo aqui. A fogueira esquenta, o ferro ensina.");
            case QuemFala.Apostador: return Idioma.T("Uma cara nova! A casa adora caras novas.");
            default: return Idioma.T("Mais um que caiu aqui! Bem-vindo ao acampamento, o único lugar quente desta prisão.");
        }
    }

    // ---------------- o comentario da ultima partida ----------------
    private static string Reacao(QuemFala quem)
    {
        bool venceu = PlayerPrefs.GetInt(Prefixo + "ultima.venceu", 0) == 1;
        int andar = PlayerPrefs.GetInt(Prefixo + "ultima.andar", 1);
        int plantas = PlayerPrefs.GetInt(Prefixo + "ultima.plantas", 0);
        string id = PlayerPrefs.GetString(Prefixo + "ultima.quem", "");
        string nome = id.Length > 0 ? Idioma.T(Bestiario.Nome(id)) : null;
        bool chefe = id.Length > 0 && Bestiario.EhChefe(id);

        switch (quem)
        {
            case QuemFala.Ferreiro:
                if (venceu) return Idioma.T("... Bom trabalho.");
                if (plantas > 0) return Idioma.T("Trouxe planta. É assim que se faz.");
                if (chefe) return Idioma.F("{0} te quebrou. Volta mais forte. É pra isso que serve a bigorna.", nome);
                if (andar >= 4) return Idioma.F("Andar {0}. O ferro de lá é mais escuro. Você vai ver.", andar);
                if (andar <= 1) return Idioma.T("Caiu cedo. A arma não era o problema. Era a pressa.");
                if (nome != null && Random.value < 0.5f) return Idioma.F("{0}. Lembra do jeito que ele bate. Da próxima, ele cai.", nome);
                return Idioma.F("Andar {0}. Nada mal pra quem chegou ontem.", andar);

            case QuemFala.Apostador:
                if (venceu) return Idioma.T("Impossível! Quem apostou contra você perdeu tudo. Inclusive eu.");
                if (chefe) return Idioma.F("{0} de novo? A casa sempre ganha. Por enquanto.", nome);
                if (andar >= 4) return Idioma.F("Andar {0}. As apostas em você estão subindo.", andar);
                if (andar <= 1) return Idioma.T("Andar 1? Eu apostei no andar 2. Você me deve uma.");
                if (nome != null && Random.value < 0.5f) return Idioma.F("{0}? Que jeito sem graça de perder.", nome);
                return Idioma.F("Andar {0}! Ganhei a aposta. Obrigado pela confiança.", andar);

            default:
                if (venceu) return Idioma.T("Você voltou do fundo! Desconto pro resto da vida. Brincadeira. Talvez.");
                if (chefe) return Idioma.F("{0}? O último que foi atrás dele comprou uma espada e não voltou pra buscar o troco.", nome);
                if (andar >= 4) return Idioma.F("Andar {0}! Mais fundo que quase todo freguês que eu já tive.", andar);
                if (andar <= 1) return Idioma.T("Andar 1 de novo? Calma. Todo mundo tropeça no primeiro degrau.");
                if (nome != null && Random.value < 0.5f) return Idioma.F("Foi {0} que te pegou? Esse aí já me levou muito freguês.", nome);
                return Idioma.F("Andar {0}! Já é mais longe que muita gente que passou por aqui.", andar);
        }
    }

    // ---------------- sem nada novo ----------------
    private static string Solta(QuemFala quem)
    {
        bool uma = Random.value < 0.5f;

        switch (quem)
        {
            case QuemFala.Ferreiro: return Idioma.T(uma ? "A bigorna não esquenta sozinha." : "Plantas. Traz plantas.");
            case QuemFala.Apostador:
                return Idioma.T(uma ? "Sorte é só azar que ainda não chegou." : "Escolhe bem a porta. Ou não. Eu me divirto do mesmo jeito.");
            default:
                return Idioma.T(uma ? "Volta com moedas que a gente conversa." : "Cuidado com os baús que tremem. Não pergunta como eu sei.");
        }
    }

    // ---------------- a conversa da vez ----------------
    /// <summary>O que a pessoa fala agora (ja no idioma do jogo): uma ou mais falas, na ordem.</summary>
    public static List<string> DaVez(QuemFala quem)
    {
        List<string> linhas = new List<string>();
        string p = Prefixo + quem + ".";
        int fim = PlayerPrefs.GetInt(Prefixo + "ultima.numero", 0);

        // 1. A ultima partida (uma vez por partida que terminou), ou a apresentacao.
        if (fim > 0 && PlayerPrefs.GetInt(p + "reagiu", 0) != fim)
        {
            linhas.Add(Reacao(quem));
            PlayerPrefs.SetInt(p + "reagiu", fim);
        }
        else if (fim == 0 && PlayerPrefs.GetInt(p + "conheceu", 0) == 0)
        {
            linhas.Add(Primeira(quem));
            PlayerPrefs.SetInt(p + "conheceu", 1);
        }

        // 2. O proximo pedaco da historia (um por partida, quando ja da pra ouvir).
        int partida = Registro.Partidas;

        if (PlayerPrefs.GetInt(p + "contou-na-partida", -1) != partida)
        {
            Linha[] historia = Historia(quem);
            int i = PlayerPrefs.GetInt(p + "historia", 0);

            if (i < historia.Length && (historia[i].Pode == null || historia[i].Pode()))
            {
                linhas.Add(Idioma.T(historia[i].Texto));
                PlayerPrefs.SetInt(p + "historia", i + 1);
                PlayerPrefs.SetInt(p + "contou-na-partida", partida);
            }
        }

        // 3. Nada novo.
        if (linhas.Count == 0)
            linhas.Add(Solta(quem));

        PlayerPrefs.Save();
        return linhas;
    }

    /// <summary>A partida acabou (morreu ou zerou): guarda o que o pessoal vai comentar.</summary>
    public static void GuardarFim(bool venceu, int andar, string quemMatou)
    {
        PlayerPrefs.SetInt(Prefixo + "ultima.numero", PlayerPrefs.GetInt(Prefixo + "ultima.numero", 0) + 1);
        PlayerPrefs.SetInt(Prefixo + "ultima.venceu", venceu ? 1 : 0);
        PlayerPrefs.SetInt(Prefixo + "ultima.andar", Mathf.Max(1, andar));
        PlayerPrefs.SetInt(Prefixo + "ultima.plantas", Desbloqueios.NestaPartida);
        PlayerPrefs.SetString(Prefixo + "ultima.quem", venceu || quemMatou == null ? "" : quemMatou);
    }

    /// <summary>Esquece tudo (o "apagar progresso"): a historia comeca de novo.</summary>
    public static void Apagar()
    {
        foreach (string chave in new[] { "ultima.numero", "ultima.venceu", "ultima.andar", "ultima.plantas", "ultima.quem" })
            PlayerPrefs.DeleteKey(Prefixo + chave);

        foreach (QuemFala quem in System.Enum.GetValues(typeof(QuemFala)))
        {
            foreach (string chave in new[] { "reagiu", "conheceu", "contou-na-partida", "historia" })
                PlayerPrefs.DeleteKey(Prefixo + quem + "." + chave);
        }
    }
}
