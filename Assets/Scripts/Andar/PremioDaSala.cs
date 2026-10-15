using UnityEngine;

/// <summary>O premio de uma sala de luta, que aparece em cima das portas dela antes de entrar (1.21.0).</summary>
public enum PremioDaSala
{
    /// <summary>Sala sem premio: a porta nao mostra nada.</summary>
    Nenhum,
    Moedas,
    Coracao,
    Chave,
    Cristal,

    /// <summary>Um item num pedestal.</summary>
    Item,

    /// <summary>Uma planta do mundo (<see cref="Desbloqueios"/>).</summary>
    Planta,
}

/// <summary>
/// Os premios das salas, como no Hades: cada sala de luta ja nasce com o premio sorteado e mostra o icone
/// em cima das portas (e no mapa), entao o jogador escolhe por onde ir. Uma parte das salas nao da nada;
/// a sala do fim sempre da, e mais. A primeira planta da vida sai na sala do fim do andar 1.
/// </summary>
public static class PremiosDasSalas
{
    /// <summary>O premio de uma sala nova (sorteado ao montar o andar).</summary>
    public static PremioDaSala Sortear(bool fim, int andar, int mundo)
    {
        bool temPlanta = Desbloqueios.FaltamNoMundo(mundo);

        // A primeira partida ja ensina alguma coisa: a sala do fim do andar 1 traz uma planta.
        if (fim && andar == 1 && temPlanta && Desbloqueios.Aprendidas == 0)
            return PremioDaSala.Planta;

        (PremioDaSala premio, float peso)[] pesos = fim
            ? new[]
            {
                (PremioDaSala.Moedas, 30f), (PremioDaSala.Coracao, 15f), (PremioDaSala.Cristal, 25f),
                (PremioDaSala.Item, 20f), (PremioDaSala.Planta, temPlanta ? 10f : 0f),
            }
            : new[]
            {
                // No andar 1 quase toda sala da alguma coisa (forca cedo).
                (PremioDaSala.Nenhum, andar <= 1 ? 25f : 38f), (PremioDaSala.Moedas, 22f), (PremioDaSala.Coracao, 12f),
                (PremioDaSala.Chave, 8f), (PremioDaSala.Cristal, 10f), (PremioDaSala.Item, 7f),
                (PremioDaSala.Planta, temPlanta ? 3f : 0f),
            };

        float total = 0f;

        foreach (var p in pesos)
            total += p.peso;

        float ponto = Random.value * total;

        foreach (var p in pesos)
        {
            ponto -= p.peso;

            if (ponto <= 0f && p.peso > 0f)
                return p.premio;
        }

        return fim ? PremioDaSala.Moedas : PremioDaSala.Nenhum;
    }

    /// <summary>O icone do premio (nas portas e no mapa).</summary>
    public static Sprite Icone(PremioDaSala premio)
    {
        switch (premio)
        {
            case PremioDaSala.Moedas: return Coletavel.Desenho(TipoDeColetavel.Moeda, 5);
            case PremioDaSala.Coracao: return Coletavel.Desenho(TipoDeColetavel.Coracao);
            case PremioDaSala.Chave: return Coletavel.Desenho(TipoDeColetavel.Chave);
            case PremioDaSala.Cristal: return Coletavel.Desenho(TipoDeColetavel.Cristal);
            case PremioDaSala.Item: return ArteDoAntigo.Inteira("Cenario/Reliquia", 26f);
            case PremioDaSala.Planta: return ArteDoAntigo.Inteira("Cenario/Planta", 26f);
            default: return null;
        }
    }

    /// <summary>A cor da luzinha em volta do icone.</summary>
    public static Color Cor(PremioDaSala premio)
    {
        switch (premio)
        {
            case PremioDaSala.Coracao: return new Color(1f, 0.45f, 0.45f);
            case PremioDaSala.Cristal: return new Color(0.75f, 0.5f, 1f);
            case PremioDaSala.Planta: return new Color(0.5f, 0.7f, 1f);
            default: return new Color(1f, 0.85f, 0.45f);
        }
    }

    /// <summary>
    /// Solta o premio no lugar (a sala acabou de ser limpa). Na sala do fim vem mais. A planta que ja nao
    /// tem mais o que ensinar (aprendeu outra no caminho) vira cristal.
    /// </summary>
    public static void Soltar(PremioDaSala premio, bool fim, Vector2 onde, Transform pai, int andar, int mundo)
    {
        switch (premio)
        {
            case PremioDaSala.Moedas:
            {
                int moedas = (fim ? Random.Range(7, 11) : Random.Range(4, 7)) + andar / 3;

                for (int i = 0; i < moedas; i++)
                    Coletavel.Criar(TipoDeColetavel.Moeda, onde + Random.insideUnitCircle * 0.7f);

                break;
            }

            case PremioDaSala.Coracao:
                for (int i = 0; i < (fim ? 3 : 2); i++)
                    Coletavel.Criar(TipoDeColetavel.Coracao, onde + Random.insideUnitCircle * 0.5f);

                break;

            case PremioDaSala.Chave:
                Coletavel.Criar(TipoDeColetavel.Chave, onde);

                for (int i = 0; i < 2; i++)
                    Coletavel.Criar(TipoDeColetavel.Moeda, onde + Random.insideUnitCircle * 0.6f);

                break;

            case PremioDaSala.Cristal:
                Coletavel.Criar(TipoDeColetavel.Cristal, onde, (fim ? 2 : 1) * Partida.CristaisVezes);
                break;

            case PremioDaSala.Item:
            {
                GameObject jogador = GameObject.FindWithTag("Player");
                EstatisticasDoJogador itens = jogador != null ? jogador.GetComponent<EstatisticasDoJogador>() : null;
                Pedestal.Criar(onde, pai, CatalogoDeItens.Sortear(itens));
                break;
            }

            case PremioDaSala.Planta:
                if (!Desbloqueios.Soltar(onde, mundo))
                    Coletavel.Criar(TipoDeColetavel.Cristal, onde, Partida.CristaisVezes);

                break;
        }
    }
}
