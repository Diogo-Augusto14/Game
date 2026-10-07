using UnityEngine;

/// <summary>
/// A partida salva, pro "Continuar" do menu inicial (veio do jogo antigo). Salva sozinho no comeco de
/// cada andar, e tambem pelo "Salvar e sair" da pausa: o heroi, o andar, a vida, as duas armas (com
/// a municao) e os numeros da partida. Continuar recomeca o andar salvo do comeco.
///
/// Morrer, vencer ou comecar uma partida nova apaga o salvo. Fica no PlayerPrefs.
/// </summary>
public static class Salvamento
{
    private const string Prefixo = "ThePrettie.salvo.";

    public static bool Existe => PlayerPrefs.GetInt(Prefixo + "existe", 0) == 1;

    /// <summary>"Andar 4 de 12 com o Soldado", pro botao do menu.</summary>
    public static string Resumo => Existe
        ? $"Andar {PlayerPrefs.GetInt(Prefixo + "andar", 1)} com o {PlayerPrefs.GetString(Prefixo + "heroi", "?")}"
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
            }

            PlayerPrefs.SetInt(Prefixo + "naMao", armas.NaMao);
        }

        // A bolsa, os itens (pelo nome) e o item ativo com a carga.
        if (jogador.TryGetComponent(out Bolsa bolsa))
        {
            PlayerPrefs.SetInt(Prefixo + "moedas", bolsa.Moedas);
            PlayerPrefs.SetInt(Prefixo + "chaves", bolsa.Chaves);
            PlayerPrefs.SetInt(Prefixo + "bombas", bolsa.Bombas);
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
            bolsa.Definir(PlayerPrefs.GetInt(Prefixo + "moedas", 0), PlayerPrefs.GetInt(Prefixo + "chaves", 0), PlayerPrefs.GetInt(Prefixo + "bombas", 1));

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
        andar.Continuar(Mathf.Clamp(PlayerPrefs.GetInt(Prefixo + "andar", 1), 1, andar.Andares));
        return true;
    }

    private static ArmaCarregada Arma(GeradorDoAndar andar, int i)
    {
        DadosDaArma dados = andar.ArmaPeloNome(PlayerPrefs.GetString(Prefixo + "arma" + i, ""));

        if (dados == null)
            return null;

        ArmaCarregada arma = new ArmaCarregada(dados);
        arma.Definir(PlayerPrefs.GetInt(Prefixo + "pente" + i, 0), PlayerPrefs.GetInt(Prefixo + "reserva" + i, 0));
        return arma;
    }

    public static void Apagar()
    {
        foreach (string chave in new[] { "existe", "heroi", "andar", "vida", "inimigos", "chefes", "tempo", "naMao",
                                         "arma0", "arma1", "pente0", "pente1", "reserva0", "reserva1",
                                         "moedas", "chaves", "bombas", "itens", "ativo", "carga" })
            PlayerPrefs.DeleteKey(Prefixo + chave);

        PlayerPrefs.Save();
    }
}
