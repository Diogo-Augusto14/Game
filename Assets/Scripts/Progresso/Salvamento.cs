using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A partida salva, como no Isaac: sair no meio guarda a partida e o menu ganha o botao
/// "Continuar". O que fica salvo e o COMECO da fase em que o jogador estava: o heroi, a fase,
/// a semente (a mesma planta, os mesmos inimigos, os mesmos itens), a vida, os itens, o item
/// ativo, moedas, chaves, bombas e vazios. Voltar recomeca essa fase com tudo isso. As armas de
/// fogo voltam junto com os itens, com o pente e a municao cheios.
///
/// Quando grava: ao descer pra cada fase nova (<see cref="Andar.ProximoAndar"/>) e, na
/// primeira fase, ao sair pela pausa. Quando apaga: ao morrer, ao vencer e ao comecar uma
/// partida nova. Fica no PlayerPrefs, em JSON.
/// </summary>
public static class Salvamento
{
    private const string Chave = "ThePrettie.partida-salva";

    [Serializable]
    public class Dados
    {
        public int versao = 1;
        public string heroi;
        public int andar;
        public int semente;
        public float vida;
        public float vidaMaxima;
        public List<string> itens = new List<string>();
        public string ativo;
        public int cargas;
        public int moedas;
        public int chaves;
        public int bombas;

        /// <summary>-1 = partida salva antes dos vazios existirem: fica com os que o jogador ja nasce.</summary>
        public int vazios = -1;

        public bool renasceu;
        public List<string> jaSairam = new List<string>();
    }

    public static bool Existe => PlayerPrefs.HasKey(Chave);

    public static Dados Ler()
    {
        if (!Existe)
            return null;

        try
        {
            Dados d = JsonUtility.FromJson<Dados>(PlayerPrefs.GetString(Chave));
            return d != null && d.andar >= 1 ? d : null;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Salvamento] partida salva estragada, ignorando: " + e.Message);
            return null;
        }
    }

    public static void Apagar()
    {
        if (!Existe)
            return;

        PlayerPrefs.DeleteKey(Chave);
        PlayerPrefs.Save();
    }

    /// <summary>"Mundo 2 - Fase 1", pro botao do menu.</summary>
    public static string Onde(Dados d, int fasesPorMundo)
    {
        int mundo = (d.andar - 1) / fasesPorMundo + 1;
        int fase = (d.andar - 1) % fasesPorMundo + 1;
        return $"Mundo {mundo} - Fase {fase}";
    }

    /// <summary>Grava o estado de agora como o comeco da fase atual (chame logo depois de gerar a fase).</summary>
    public static void SalvarComecoDaFase(Andar andar)
    {
        if (andar == null || andar.Jogador == null)
            return;

        GameObject jogador = andar.Jogador.gameObject;
        Dados d = new Dados
        {
            heroi = Herois.Atual.Nome,
            andar = andar.NumeroDoAndar,
            semente = andar.SementeUsada,
        };

        if (jogador.TryGetComponent(out Vida vida))
        {
            d.vida = vida.VidaAtual;
            d.vidaMaxima = vida.VidaMaxima;
        }

        if (jogador.TryGetComponent(out EstatisticasDoJogador estatisticas))
            foreach (ItemPassivo item in estatisticas.Itens)
                d.itens.Add(item.nome);

        if (jogador.TryGetComponent(out ItemAtivoDoJogador ativo) && ativo.Item != null)
        {
            d.ativo = ativo.Item.nome;
            d.cargas = ativo.Cargas;
        }

        if (jogador.TryGetComponent(out Inventario inventario))
        {
            d.moedas = inventario.Moedas;
            d.chaves = inventario.Chaves;
            d.bombas = inventario.Bombas;
            d.vazios = inventario.Vazios;
        }

        if (jogador.TryGetComponent(out EfeitosDosItens efeitos))
            d.renasceu = efeitos.RenasceuJa;

        foreach (ItemPassivo item in andar.SairamAntesDaFase)
            d.jaSairam.Add(item.nome);

        Gravar(d);
    }

    /// <summary>
    /// Primeira fase: o comeco dela e o heroi do jeito que nasce, sem item. Grava isso (com a
    /// semente da fase) se ainda nao tem partida salva.
    /// </summary>
    public static void SalvarComecoDoJogo(Andar andar)
    {
        if (andar == null || Existe || andar.NumeroDoAndar != 1)
            return;

        Herois.Heroi heroi = Herois.Atual;
        Gravar(new Dados
        {
            heroi = heroi.Nome,
            andar = 1,
            semente = andar.SementeUsada,
            vida = heroi.Vida,
            vidaMaxima = heroi.Vida,
            bombas = 1,
        });
    }

    private static void Gravar(Dados d)
    {
        PlayerPrefs.SetString(Chave, JsonUtility.ToJson(d));
        PlayerPrefs.Save();
    }

    /// <summary>Escolhe o heroi salvo (antes de vestir o jogador). False se ele nao existe mais.</summary>
    public static bool EscolherHeroi(Dados d)
    {
        for (int i = 0; i < Herois.Todos.Length; i++)
        {
            if (Herois.Todos[i].Nome != d.heroi)
                continue;

            Herois.Escolher(i);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Refaz a fase salva e devolve ao jogador o que ele tinha. Chame depois de vestir o heroi
    /// (<see cref="Herois.Aplicar"/>): os numeros de base dos itens sao os do heroi.
    /// </summary>
    public static void Restaurar(Dados d, Andar andar)
    {
        if (d == null || andar == null)
            return;

        List<ItemPassivo> jaSairam = new List<ItemPassivo>();

        foreach (string nome in d.jaSairam)
        {
            ItemPassivo item = Item(nome);

            if (item != null)
                jaSairam.Add(item);
        }

        andar.Continuar(d.andar, d.semente, jaSairam);

        if (andar.Jogador == null)
            return;

        GameObject jogador = andar.Jogador.gameObject;
        List<ItemPassivo> itens = new List<ItemPassivo>();

        foreach (string nome in d.itens)
        {
            ItemPassivo item = Item(nome);

            if (item != null && !item.EAtivo)
                itens.Add(item);
        }

        EstatisticasDoJogador estatisticas = jogador.GetComponent<EstatisticasDoJogador>();

        if (estatisticas == null)
            estatisticas = jogador.AddComponent<EstatisticasDoJogador>();

        estatisticas.Restaurar(itens);

        ItemPassivo ativo = Item(d.ativo);

        if (ativo != null && ativo.EAtivo)
            ItemAtivoDoJogador.Em(jogador).Equipar(ativo, d.cargas);

        if (jogador.TryGetComponent(out Inventario inventario))
        {
            inventario.Adicionar(TipoDeColetavel.Moeda, d.moedas - inventario.Moedas);
            inventario.Adicionar(TipoDeColetavel.Chave, d.chaves - inventario.Chaves);
            inventario.Adicionar(TipoDeColetavel.Bomba, d.bombas - inventario.Bombas);

            if (d.vazios >= 0)
                inventario.Adicionar(TipoDeColetavel.Vazio, d.vazios - inventario.Vazios);
        }

        if (d.renasceu && jogador.TryGetComponent(out EfeitosDosItens efeitos))
            efeitos.MarcarRenascido();

        if (jogador.TryGetComponent(out Vida vida) && d.vidaMaxima > 0f)
            vida.Restaurar(d.vida, d.vidaMaxima);
    }

    private static ItemPassivo Item(string nome)
    {
        if (string.IsNullOrEmpty(nome))
            return null;

        foreach (ItemPassivo item in CatalogoDeItens.Todos)
            if (item.nome == nome)
                return item;

        return null;
    }
}
