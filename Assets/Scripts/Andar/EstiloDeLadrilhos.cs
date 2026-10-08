using UnityEngine;

/// <summary>
/// Como o <see cref="Pedreiro"/> desenha os andares de um mundo: as folhas do chao, das paredes e do
/// fundo, e as tabelas e variacoes de cada uma. Todos usam as regras que poem as faces do Old Prison
/// (os pacotes da serie EPIC RPG World tem o mesmo molde).
///
///   Old Prison  (Porao e Catacumbas): o chao e uma plataforma de pedra; buracos caem num abismo roxo.
///   Cripta      (pacote Crypt): paredes do Crypt e chao de pedra inteiro, sem buracos.
///   Profundezas (pacote The Depths of the Mountain): sem paredes; as salas flutuam sobre o vazio e a
///               beirada de cada uma cai num penhasco.
///
/// Os dados de cada um saem das ferramentas (<see cref="DadosDoOldPrison"/>, <see cref="DadosDosTemas"/>).
/// </summary>
public class EstiloDeLadrilhos
{
    public Texture2D chao;
    public Texture2D paredes;

    /// <summary>O fundo dos buracos (e do vazio). Nulo = o abismo roxo do Old Prison.</summary>
    public Texture2D abismo;

    public int[] cantosDoChao = DadosDoOldPrison.CantosDoChao;
    public int[] chaoInteiro = DadosDoOldPrison.ChaoInteiro;
    public float[] pesoDoChaoInteiro = DadosDoOldPrison.PesoDoChaoInteiro;
    public Regra[] variacaoDoChao = DadosDoOldPrison.RegrasDeVariacao;

    /// <summary>Falso = o chao e so ladrilhos inteiros, sem beirada nem regras.</summary>
    public bool regrasNoChao = true;

    public int[] cantosDasParedes = DadosDoOldPrison.CantosDasParedes;
    public Regra[] variacaoDasParedes = DadosDoOldPrison.RegrasDeVariacao;

    /// <summary>Sem paredes: o que nao e chao e vazio (segura quem anda, o tiro passa por cima).</summary>
    public bool flutuante;

    /// <summary>O mundo tem buracos de abismo no meio das salas.</summary>
    public bool temBuracos = true;

    /// <summary>O mundo tem pocas de sangue no chao.</summary>
    public bool temSangue = true;

    /// <summary>A cor do fundo da camera neste mundo (nulo = a da cena).</summary>
    public Color? corDoFundo;

    /// <summary>O Old Prison, com as folhas dadas (as do pacote ou recoloridas).</summary>
    public static EstiloDeLadrilhos OldPrison(Texture2D chao, Texture2D paredes) =>
        new EstiloDeLadrilhos { chao = chao, paredes = paredes };

    /// <summary>A Cripta: as folhas em Resources/Temas/Cripta (nulo se faltarem).</summary>
    public static EstiloDeLadrilhos Cripta()
    {
        Texture2D chao = Resources.Load<Texture2D>("Temas/Cripta/Chao");
        Texture2D paredes = Resources.Load<Texture2D>("Temas/Cripta/Paredes");

        if (chao == null || paredes == null)
            return null;

        return new EstiloDeLadrilhos
        {
            chao = chao,
            paredes = paredes,
            chaoInteiro = DadosDosTemas.ChaoDaCripta,
            pesoDoChaoInteiro = DadosDosTemas.PesoDoChaoDaCripta,
            regrasNoChao = false,
            temBuracos = false,
        };
    }

    /// <summary>As Profundezas: as folhas em Resources/Temas/Profundezas (nulo se faltarem).</summary>
    public static EstiloDeLadrilhos Profundezas()
    {
        Texture2D chao = Resources.Load<Texture2D>("Temas/Profundezas/Chao");
        Texture2D abismo = Resources.Load<Texture2D>("Temas/Profundezas/Abismo");

        if (chao == null || abismo == null)
            return null;

        return new EstiloDeLadrilhos
        {
            chao = chao,
            abismo = abismo,
            cantosDoChao = DadosDosTemas.CantosDoChaoDasProfundezas,
            chaoInteiro = DadosDosTemas.ChaoDasProfundezas,
            pesoDoChaoInteiro = DadosDosTemas.PesoDoChaoDasProfundezas,
            variacaoDoChao = DadosDosTemas.VariacaoDoChaoDasProfundezas,
            flutuante = true,
            temSangue = false,
            corDoFundo = DadosDosTemas.VazioDasProfundezas,
        };
    }
}
