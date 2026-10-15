using UnityEngine;

/// <summary>
/// A partida salva, pro "Continuar" do menu inicial (veio do jogo antigo). Salva sozinho no comeco de
/// cada andar, e tambem pelo "Salvar e sair" da pausa: o heroi, o andar, a vida, as duas armas (com
/// a municao), os numeros da partida e o caminho (o tema de cada mundo). Continuar recomeca o andar
/// salvo do comeco; salvo na area segura, volta pra ela (a escolha do caminho ainda nao foi feita).
///
/// Morrer, vencer ou comecar uma partida nova apaga o salvo. Fica no PlayerPrefs.
/// </summary>
public static class Salvamento
{
    private const string Prefixo = "ThePrettie.salvo.";

    public static bool Existe => PlayerPrefs.GetInt(Prefixo + "existe", 0) == 1;

    /// <summary>"Andar 4 com o Soldado" (ou "Área segura com o Soldado"), pro botao do menu.</summary>
    public static string Resumo => Existe
        ? (PlayerPrefs.GetInt(Prefixo + "descanso", 0) == 1 ? Idioma.T("Área segura") : Idioma.F("Andar {0}", PlayerPrefs.GetInt(Prefixo + "andar", 1)))
          + " " + Idioma.F("com o {0}", Idioma.T(PlayerPrefs.GetString(Prefixo + "heroi", "?")))
          + (PlayerPrefs.GetInt(Prefixo + "modo", 0) == (int)ModoDeJogo.Maldito ? " (maldito)"
             : PlayerPrefs.GetInt(Prefixo + "modo", 0) == (int)ModoDeJogo.Diario ? " (desafio do dia)" : "")
        : "";

    public static void Salvar(GeradorDoAndar andar, GameObject jogador)
    {
        if (andar == null || jogador == null)
            return;

        if (jogador.TryGetComponent(out Vida vida) && vida.Morto)
            return;

        PlayerPrefs.SetInt(Prefixo + "existe", 1);
        PlayerPrefs.SetString(Prefixo + "heroi", Herois.Atual.Nome);
        PlayerPrefs.SetInt(Prefixo + "andar", andar.Andar);
        PlayerPrefs.SetInt(Prefixo + "descanso", andar.Descansando ? 1 : 0);
        PlayerPrefs.SetString(Prefixo + "caminho", andar.Caminho);
        PlayerPrefs.SetInt(Prefixo + "modo", (int)Partida.Modo);
        PlayerPrefs.SetInt(Prefixo + "dia", Partida.Dia);
        PlayerPrefs.SetFloat(Prefixo + "vida", vida != null ? vida.Atual : 0f);
        PlayerPrefs.SetInt(Prefixo + "inimigos", ResumoDaPartida.InimigosDerrotados);
        PlayerPrefs.SetInt(Prefixo + "chefes", ResumoDaPartida.ChefesDerrotados);
        PlayerPrefs.SetFloat(Prefixo + "tempo", ResumoDaPartida.Tempo);

        if (jogador.TryGetComponent(out ArmaDoJogador armas))
        {
            for (int i = 0; i < 2; i++)
            {
                ArmaCarregada arma = armas.Mao(i);
                PlayerPrefs.SetString(Prefixo + "arma" + i, arma != null ? arma.Dados.name : "");
                PlayerPrefs.SetInt(Prefixo + "pente" + i, arma != null ? arma.NoPente : 0);
                PlayerPrefs.SetInt(Prefixo + "reserva" + i, arma != null ? arma.Reserva : 0);
                PlayerPrefs.SetInt(Prefixo + "forja" + i, arma != null ? arma.Dados.forja : 0);
            }

            PlayerPrefs.SetInt(Prefixo + "naMao", armas.NaMao);
        }

        // A bolsa, os itens (pelo nome) e o item ativo com a carga.
        if (jogador.TryGetComponent(out Bolsa bolsa))
        {
            PlayerPrefs.SetInt(Prefixo + "moedas", bolsa.Moedas);
            PlayerPrefs.SetInt(Prefixo + "chaves", bolsa.Chaves);
        }

        if (jogador.TryGetComponent(out EstatisticasDoJogador itens))
        {
            System.Collections.Generic.List<string> nomes = new System.Collections.Generic.List<string>();

            foreach (ItemPassivo item in itens.Itens)
            {
                if (!item.EhSinergia)
                    nomes.Add(item.Nome);
            }

            PlayerPrefs.SetString(Prefixo + "itens", string.Join("|", nomes));
        }

        if (jogador.TryGetComponent(out ItemAtivoDoJogador ativo))
        {
            PlayerPrefs.SetString(Prefixo + "ativo", ativo.Item != null ? ativo.Item.Nome : "");
            PlayerPrefs.SetInt(Prefixo + "carga", ativo.Carga);
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Volta a partida salva: o heroi (com as armas e a vida de quando salvou) no comeco do andar.
    /// Falso se nao tinha partida salva (ou o heroi nao existe mais).
    /// </summary>
    public static bool Continuar(GeradorDoAndar andar, GameObject jogador)
    {
        Herois.Heroi heroi = Existe ? Herois.PeloNome(PlayerPrefs.GetString(Prefixo + "heroi", "")) : null;

        if (andar == null || jogador == null || heroi == null)
            return false;

        // O modo primeiro: o desafio do dia monta os andares com a semente do dia dele, e nele as melhorias nao valem.
        Partida.Modo = (ModoDeJogo)Mathf.Clamp(PlayerPrefs.GetInt(Prefixo + "modo", 0), 0, 2);
        Partida.Dia = PlayerPrefs.GetInt(Prefixo + "dia", Partida.Hoje);
        Partida.CristaisNestaPartida = 0;

        Herois.Escolher(System.Array.IndexOf(Herois.Todos, heroi));
        Herois.Aplicar(jogador, heroi);

        if (jogador.TryGetComponent(out ArmaDoJogador armas))
        {
            ArmaCarregada primeira = Arma(andar, 0);
            ArmaCarregada segunda = Arma(andar, 1);

            if (primeira != null || segunda != null)
                armas.Restaurar(primeira ?? segunda, primeira != null ? segunda : null, PlayerPrefs.GetInt(Prefixo + "naMao", 0));
        }

        if (jogador.TryGetComponent(out Bolsa bolsa))
            bolsa.Definir(PlayerPrefs.GetInt(Prefixo + "moedas", 0), PlayerPrefs.GetInt(Prefixo + "chaves", 0));

        // Os itens voltam calados (sem aviso), com as sinergias; depois a vida (que eles podem mudar).
        if (jogador.TryGetComponent(out EstatisticasDoJogador itens))
        {
            foreach (string nome in PlayerPrefs.GetString(Prefixo + "itens", "").Split('|'))
            {
                ItemPassivo item = CatalogoDeItens.PeloNome(nome);

                if (item != null)
                    itens.Pegar(item, false);
            }
        }

        if (jogador.TryGetComponent(out ItemAtivoDoJogador ativo))
            ativo.Equipar(CatalogoDeItens.PeloNome(PlayerPrefs.GetString(Prefixo + "ativo", "")), PlayerPrefs.GetInt(Prefixo + "carga", 0));

        if (jogador.TryGetComponent(out Vida vida))
            vida.DefinirAtual(PlayerPrefs.GetFloat(Prefixo + "vida", vida.Maxima));

        ResumoDaPartida.Restaurar(PlayerPrefs.GetInt(Prefixo + "inimigos", 0), PlayerPrefs.GetInt(Prefixo + "chefes", 0),
                                  PlayerPrefs.GetFloat(Prefixo + "tempo", 0f));
        andar.Caminho = PlayerPrefs.GetString(Prefixo + "caminho", "");
        andar.Continuar(Mathf.Clamp(PlayerPrefs.GetInt(Prefixo + "andar", 1), 1, andar.Andares), PlayerPrefs.GetInt(Prefixo + "descanso", 0) == 1);
        return true;
    }

    private static ArmaCarregada Arma(GeradorDoAndar andar, int i)
    {
        DadosDaArma dados = andar.ArmaPeloNome(PlayerPrefs.GetString(Prefixo + "arma" + i, ""));

        if (dados == null)
            return null;

        // A forja do ferreiro volta junto.
        dados = dados.Forjada(PlayerPrefs.GetInt(Prefixo + "forja" + i, 0));
        ArmaCarregada arma = new ArmaCarregada(dados);
        arma.Definir(PlayerPrefs.GetInt(Prefixo + "pente" + i, 0), PlayerPrefs.GetInt(Prefixo + "reserva" + i, 0));
        return arma;
    }

    public static void Apagar()
    {
        foreach (string chave in new[] { "existe", "heroi", "andar", "descanso", "caminho", "modo", "dia", "vida", "inimigos", "chefes", "tempo", "naMao",
                                         "arma0", "arma1", "pente0", "pente1", "reserva0", "reserva1", "forja0", "forja1",
                                         "moedas", "chaves", "bombas", "itens", "ativo", "carga" })
            PlayerPrefs.DeleteKey(Prefixo + chave);

        PlayerPrefs.Save();
    }
}
