using System.Collections.Generic;
using UnityEngine;

/// <summary>O desenho de cada arma (<see cref="ArteDasArmas"/>). Cada arma do catalogo tem um.</summary>
public enum EstiloDeArma
{
    Pistola,
    Revolver,
    Escopeta,
    Submetralhadora,
    Fuzil,
    Rifle,
    LancaGranadas,
    PistolaDeGelo,
    PistolaToxica,
    Quicadora,
}

/// <summary>
/// Uma arma de fogo, como no Gungeon: dano por bala, cadencia, pente que acaba e pede recarga,
/// municao que acaba de vez. As armas do catalogo (<see cref="CatalogoDeArmas"/>) viram itens
/// (<see cref="ItemPassivo.arma"/>) e saem de bau, loja e pedestal como qualquer outro.
///
/// Os itens passivos continuam valendo: cada multiplicador de dano, cadencia e alcance que o
/// jogador tem por item vale tambem pra arma (<see cref="AtiradorTopDown.DefinirFatores"/>), e os
/// efeitos de bala (atravessar, perseguir, explodir) tambem. O efeito da propria arma (gelo,
/// veneno, explosao, ricochete) usa os mesmos efeitos das flechas especiais.
///
/// O heroi tem a arma dele (a de sempre, municao infinita e sem pente): ela e a primeira do
/// <see cref="ArsenalDoJogador"/> e nao esta aqui.
/// </summary>
public class ArmaDeFogo
{
    public string nome;

    /// <summary>Frase curta que aparece na tela ao pegar.</summary>
    public string descricao;

    public EstiloDeArma estilo;

    [Tooltip("A arma com que toda partida comeca: municao infinita, nao sai de bau nem de loja")]
    public bool inicial;

    /// <summary>Cor da bala, do nome e do item.</summary>
    public Color cor = new Color(1f, 0.9f, 0.5f);

    // ---------------- tiro ----------------
    [Tooltip("Dano de cada bala")]
    public float dano = 4f;

    public float tirosPorSegundo = 3f;
    public float velocidadeDaBala = 12f;

    [Tooltip("Distancia que a bala voa antes de cair, em unidades")]
    public float alcance = 8f;

    [Tooltip("Diametro da bala, em unidades")]
    public float tamanhoDaBala = 0.22f;

    public float empurrao = 2.5f;

    [Header("Varias balas")]
    public int balasPorTiro = 1;

    [Tooltip("Graus entre uma bala e a vizinha, em leque")]
    public float abertura;

    [Tooltip("A cada bala, sorteia ate esses graus pra cada lado (mira solta)")]
    public float imprecisao;

    [Tooltip("Cada bala sai um pouco mais rapida ou mais lenta, ate essa fracao (0.1 = 10%)")]
    public float variacaoDeVelocidade;

    // ---------------- pente e municao ----------------
    public int tamanhoDoPente = 8;
    public float tempoDeRecarga = 1.2f;

    [Tooltip("Municao total, contando o pente. 0 ou menos = infinita")]
    public int municaoMaxima = 100;

    // ---------------- efeito ----------------
    [Tooltip("Efeito da bala, o mesmo das flechas especiais (Normal = nenhum)")]
    public TipoDeFlecha efeito = TipoDeFlecha.Normal;

    public bool atravessa;

    // ---------------- sensacao ----------------
    public Som som = Som.Tiro;
    public float volumeDoSom = 0.55f;

    [Tooltip("Quanto a arma coice na mao (so visual, em unidades)")]
    public float recuo = 0.12f;

    [Tooltip("Tremor de camera a cada tiro")]
    public float tremor = 0.03f;

    public bool Infinita => municaoMaxima <= 0;

    /// <summary>O efeito completo, pra tela de itens e loja.</summary>
    public string DescricaoCompleta
    {
        get
        {
            string balas = balasPorTiro > 1 ? $"{balasPorTiro} balas de {dano:0.#}" : $"{dano:0.#} de dano";
            string municao = Infinita ? "munição infinita" : $"{municaoMaxima} de munição";
            return $"{descricao}\n{balas} · {tirosPorSegundo:0.#} tiros/s · pente de {tamanhoDoPente} · recarga {tempoDeRecarga:0.0}s · {municao}.";
        }
    }

    public Sprite Desenho => ArteDasArmas.Arma(estilo);

    public Sprite Icone => ArteDasArmas.Icone(estilo);
}

/// <summary>
/// As armas do jogo. Pra criar uma nova, e so acrescentar uma entrada em <see cref="Montar"/> (e um
/// desenho em <see cref="ArteDasArmas"/>, ou reaproveitar um estilo). Os numeros sao de partida:
/// o mais util pra equilibrar e a municao e o dano por bala.
/// </summary>
public static class CatalogoDeArmas
{
    private static List<ArmaDeFogo> todas;

    public static IReadOnlyList<ArmaDeFogo> Todas => todas ?? (todas = Montar());

    /// <summary>A arma de todo comeco de partida (a pistola de municao infinita).</summary>
    public static ArmaDeFogo Inicial
    {
        get
        {
            foreach (ArmaDeFogo arma in Todas)
                if (arma.inicial)
                    return arma;

            return null;
        }
    }

    public static ArmaDeFogo PorNome(string nome)
    {
        foreach (ArmaDeFogo arma in Todas)
            if (arma.nome == nome)
                return arma;

        return null;
    }

    /// <summary>O icone da arma com esse nome de item, ou null (nao e arma).</summary>
    public static Sprite IconeDoItem(string nomeDoItem) => PorNome(nomeDoItem)?.Icone;

    private static List<ArmaDeFogo> Montar()
    {
        return new List<ArmaDeFogo>
        {
            // A de todo comeco de partida. Infinita: o pente e a recarga e que seguram o ritmo.
            new ArmaDeFogo
            {
                nome = "Pistola", estilo = EstiloDeArma.Pistola, cor = new Color(1f, 0.95f, 0.7f), inicial = true,
                descricao = "Sem muita força, mas nunca acaba a munição.",
                dano = 3.2f, tirosPorSegundo = 4f, velocidadeDaBala = 12f, alcance = 8.5f,
                tamanhoDaBala = 0.21f, imprecisao = 1.5f,
                tamanhoDoPente = 10, tempoDeRecarga = 0.9f, municaoMaxima = 0,
                recuo = 0.1f, tremor = 0.02f,
            },

            new ArmaDeFogo
            {
                nome = "Revólver", estilo = EstiloDeArma.Revolver, cor = new Color(1f, 0.88f, 0.45f),
                descricao = "Forte e preciso, mas o pente é curto.",
                dano = 7f, tirosPorSegundo = 3f, velocidadeDaBala = 15f, alcance = 9.5f,
                tamanhoDoPente = 6, tempoDeRecarga = 1.0f, municaoMaxima = 150,
                empurrao = 3f, tremor = 0.04f, recuo = 0.14f,
            },

            new ArmaDeFogo
            {
                nome = "Escopeta de Cano Serrado", estilo = EstiloDeArma.Escopeta, cor = new Color(1f, 0.62f, 0.3f),
                descricao = "Seis chumbos por tiro: mortal de perto.",
                dano = 2.6f, tirosPorSegundo = 1.5f, velocidadeDaBala = 13f, alcance = 5.5f,
                tamanhoDaBala = 0.2f, balasPorTiro = 6, imprecisao = 9f, variacaoDeVelocidade = 0.12f,
                tamanhoDoPente = 2, tempoDeRecarga = 1.3f, municaoMaxima = 60,
                empurrao = 2f, som = Som.Canhao, volumeDoSom = 0.4f, tremor = 0.09f, recuo = 0.22f,
            },

            new ArmaDeFogo
            {
                nome = "Submetralhadora", estilo = EstiloDeArma.Submetralhadora, cor = new Color(1f, 0.95f, 0.6f),
                descricao = "Uma chuva de balas, cada uma fraca.",
                dano = 1.9f, tirosPorSegundo = 12f, velocidadeDaBala = 14f, alcance = 7.5f,
                tamanhoDaBala = 0.17f, imprecisao = 6f,
                tamanhoDoPente = 28, tempoDeRecarga = 1.5f, municaoMaxima = 420,
                empurrao = 1f, tremor = 0.015f, recuo = 0.07f, volumeDoSom = 0.32f,
            },

            new ArmaDeFogo
            {
                nome = "Fuzil de Assalto", estilo = EstiloDeArma.Fuzil, cor = new Color(1f, 0.85f, 0.4f),
                descricao = "Rajada firme, boa pra tudo.",
                dano = 3.6f, tirosPorSegundo = 6.5f, velocidadeDaBala = 15f, alcance = 9f,
                tamanhoDaBala = 0.19f, imprecisao = 2.5f,
                tamanhoDoPente = 20, tempoDeRecarga = 1.6f, municaoMaxima = 260,
                empurrao = 2f, tremor = 0.03f, recuo = 0.1f, volumeDoSom = 0.4f,
            },

            new ArmaDeFogo
            {
                nome = "Rifle de Precisão", estilo = EstiloDeArma.Rifle, cor = new Color(0.8f, 0.95f, 1f),
                descricao = "Um tiro, uma baixa. Atravessa tudo na linha.",
                dano = 18f, tirosPorSegundo = 1.1f, velocidadeDaBala = 24f, alcance = 15f,
                tamanhoDaBala = 0.2f, atravessa = true,
                tamanhoDoPente = 4, tempoDeRecarga = 1.7f, municaoMaxima = 48,
                empurrao = 5f, tremor = 0.07f, recuo = 0.2f,
            },

            new ArmaDeFogo
            {
                nome = "Lança-Granadas", estilo = EstiloDeArma.LancaGranadas, cor = new Color(1f, 0.55f, 0.25f),
                descricao = "A bala explode e leva todo mundo em volta.",
                dano = 8f, tirosPorSegundo = 1.1f, velocidadeDaBala = 9.5f, alcance = 10f,
                tamanhoDaBala = 0.3f, efeito = TipoDeFlecha.Explosiva,
                tamanhoDoPente = 3, tempoDeRecarga = 1.9f, municaoMaxima = 36,
                empurrao = 3f, som = Som.Canhao, volumeDoSom = 0.55f, tremor = 0.08f, recuo = 0.2f,
            },

            new ArmaDeFogo
            {
                nome = "Pistola de Gelo", estilo = EstiloDeArma.PistolaDeGelo, cor = new Color(0.6f, 0.85f, 1f),
                descricao = "Quem leva fica lento.",
                dano = 3.8f, tirosPorSegundo = 4f, velocidadeDaBala = 12f, alcance = 8f,
                tamanhoDaBala = 0.24f, efeito = TipoDeFlecha.Gelo,
                tamanhoDoPente = 12, tempoDeRecarga = 1.2f, municaoMaxima = 160,
                som = Som.Magia, volumeDoSom = 0.4f,
            },

            new ArmaDeFogo
            {
                nome = "Pistola Tóxica", estilo = EstiloDeArma.PistolaToxica, cor = new Color(0.55f, 0.95f, 0.35f),
                descricao = "O veneno continua trabalhando depois do tiro.",
                dano = 3.4f, tirosPorSegundo = 5f, velocidadeDaBala = 12f, alcance = 8f,
                tamanhoDaBala = 0.22f, imprecisao = 2f, efeito = TipoDeFlecha.Venenosa,
                tamanhoDoPente = 14, tempoDeRecarga = 1.3f, municaoMaxima = 170,
                som = Som.Gosma, volumeDoSom = 0.4f,
            },

            new ArmaDeFogo
            {
                nome = "Quicadora", estilo = EstiloDeArma.Quicadora, cor = new Color(1f, 0.8f, 0.3f),
                descricao = "A bala ricocheteia nas paredes até três vezes.",
                dano = 3.6f, tirosPorSegundo = 3.5f, velocidadeDaBala = 11f, alcance = 9f,
                tamanhoDaBala = 0.22f, imprecisao = 2f, efeito = TipoDeFlecha.Ricochete,
                tamanhoDoPente = 10, tempoDeRecarga = 1.25f, municaoMaxima = 170,
            },
        };
    }
}
