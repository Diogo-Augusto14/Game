using UnityEngine;

/// <summary>
/// Os numeros de uma bala de inimigo: o quanto machuca, a pressa, o tamanho e a cor. Os padroes
/// (<see cref="PadroesDeBala"/>) usam o mesmo conjunto pra todas as balas de um disparo.
/// </summary>
public struct BalaDeInimigo
{
    public float dano;
    public float velocidade;
    public float diametro;
    public Color cor;

    /// <summary>Bala de chefe: passa do teto das balas dos bichos comuns, pra o chefe nunca ficar sem padrao.</summary>
    public bool deChefe;

    public BalaDeInimigo(float velocidade, Color cor, float dano = 10f, float diametro = 0.3f)
    {
        this.dano = dano;
        this.velocidade = velocidade;
        this.diametro = diametro;
        this.cor = cor;
        deChefe = false;
    }

    public BalaDeInimigo ComVelocidade(float nova)
    {
        velocidade = nova;
        return this;
    }
}

/// <summary>
/// Os desenhos que as balas fazem no ar, no estilo do Gungeon: anel, anel com buraco pra passar,
/// leque, cruz, cortina e rosa. Cada um solta as balas de uma vez e devolve quantas saíram.
/// Os que acontecem ao longo do tempo (espiral, rajada) estao no <see cref="AtiradorDePadroes"/>.
///
/// Os numeros de cada padrao (quantas balas, quao rapidas) vem de quem chama; o que sobe com a
/// dificuldade fica em <see cref="PerfilDeBalas"/>. Todas as balas saem a partir de
/// <paramref name="origem"/>, ja afastadas do corpo pelo <paramref name="raioDoCorpo"/>.
/// </summary>
public static class PadroesDeBala
{
    /// <summary>
    /// Teto de balas inimigas no ar: passou disso, os padroes novos nao soltam bala (o jogo nao
    /// vira uma parede que nao tem como desviar, nem pesa). Os chefes nao passam por aqui.
    /// </summary>
    public const int TetoDeBalas = 160;

    /// <summary>Quanto o teto sobe pras balas de chefe (os bichos comuns da sala nao tiram o lugar do chefe).</summary>
    public const int FolgaDoChefe = 80;

    private static int TetoDe(BalaDeInimigo bala) => bala.deChefe ? TetoDeBalas + FolgaDoChefe : TetoDeBalas;

    /// <summary>Uma bala inimiga, com o teto respeitado. Null se o ar ja esta cheio.</summary>
    public static TiroDaSala Bala(Vector2 origem, Vector2 rumo, BalaDeInimigo bala, GameObject dono, float raioDoCorpo)
    {
        if (TiroDaSala.NoAr >= TetoDe(bala))
            return null;

        rumo.Normalize();
        return TiroDaSala.Disparar(origem + rumo * (raioDoCorpo + 0.15f), rumo * bala.velocidade, bala.dano, dono, true,
                                   bala.cor, bala.diametro, true);
    }

    public static Vector2 Rumo(float anguloEmGraus)
    {
        float a = anguloEmGraus * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    }

    /// <summary>Balas igualmente espacadas em volta do corpo.</summary>
    public static int Anel(Vector2 origem, GameObject dono, float raioDoCorpo, BalaDeInimigo bala, int quantas, float anguloInicial)
    {
        int saiu = 0;

        for (int i = 0; i < quantas; i++)
            if (Bala(origem, Rumo(anguloInicial + i * 360f / quantas), bala, dono, raioDoCorpo) != null)
                saiu++;

        return saiu;
    }

    /// <summary>
    /// Um anel com um buraco: as balas dentro de <paramref name="larguraDoBuraco"/> graus do
    /// <paramref name="anguloDoBuraco"/> ficam de fora. Pra passar e so achar o buraco (e o buraco
    /// costuma apontar pro jogador, entao ele tem pra onde ir).
    /// </summary>
    public static int AnelComBuraco(Vector2 origem, GameObject dono, float raioDoCorpo, BalaDeInimigo bala, int quantas,
                                    float anguloDoBuraco, float larguraDoBuraco)
    {
        int saiu = 0;

        for (int i = 0; i < quantas; i++)
        {
            float angulo = anguloDoBuraco + i * 360f / quantas;

            if (Mathf.Abs(Mathf.DeltaAngle(angulo, anguloDoBuraco)) * 2f < larguraDoBuraco)
                continue;

            if (Bala(origem, Rumo(angulo), bala, dono, raioDoCorpo) != null)
                saiu++;
        }

        return saiu;
    }

    /// <summary>Um leque de balas centrado em <paramref name="angulo"/> (graus), com <paramref name="abertura"/> entre uma e outra.</summary>
    public static int Leque(Vector2 origem, GameObject dono, float raioDoCorpo, BalaDeInimigo bala, int quantas,
                            float angulo, float abertura)
    {
        int saiu = 0;
        float primeiro = angulo - abertura * (quantas - 1) * 0.5f;

        for (int i = 0; i < quantas; i++)
            if (Bala(origem, Rumo(primeiro + i * abertura), bala, dono, raioDoCorpo) != null)
                saiu++;

        return saiu;
    }

    /// <summary>
    /// Uma cortina: uma fileira de balas lado a lado, todas indo na mesma direcao, com um buraco
    /// pra passar. <paramref name="buraco"/> e a posicao do buraco na fileira (0 a quantas - 1).
    /// </summary>
    public static int Cortina(Vector2 origem, GameObject dono, float raioDoCorpo, BalaDeInimigo bala, int quantas,
                              float angulo, float espaco, int buraco)
    {
        int saiu = 0;
        Vector2 rumo = Rumo(angulo);
        Vector2 lado = new Vector2(-rumo.y, rumo.x);

        for (int i = 0; i < quantas; i++)
        {
            if (i == buraco || i == buraco + 1)
                continue;

            Vector2 ponto = origem + lado * ((i - (quantas - 1) * 0.5f) * espaco);

            // A fileira nasce a frente do corpo; a bala sai a partir do ponto, sem afastar de novo.
            if (TiroDaSala.NoAr < TetoDe(bala))
            {
                TiroDaSala.Disparar(ponto + rumo * (raioDoCorpo + 0.3f), rumo * bala.velocidade, bala.dano, dono, true, bala.cor, bala.diametro, true);
                saiu++;
            }
        }

        return saiu;
    }

    /// <summary>
    /// Uma rosa: varias pétalas, cada uma e um pequeno leque, em volta do corpo. Mais bonita e
    /// com mais lugar pra passar entre as petalas do que um anel cheio.
    /// </summary>
    public static int Rosa(Vector2 origem, GameObject dono, float raioDoCorpo, BalaDeInimigo bala, int petalas, int balasPorPetala,
                           float anguloInicial, float aberturaDaPetala)
    {
        int saiu = 0;

        for (int p = 0; p < petalas; p++)
            saiu += Leque(origem, dono, raioDoCorpo, bala, balasPorPetala, anguloInicial + p * 360f / petalas, aberturaDaPetala);

        return saiu;
    }
}
