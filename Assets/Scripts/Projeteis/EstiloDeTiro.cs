using System.Collections.Generic;
using UnityEngine;

/// <summary>Os estilos de tiro dos inimigos. Gema = o tiro antigo (quem nao tem estilo proprio).</summary>
public enum EstiloDeTiro
{
    Gema,
    OrbeSombrio,
    BolaDeFogo,
    ChamaFantasma,
    Maldicao,
    MaldicaoTeleguiada,
    BalaDeCanhao,
    GotaDeSangue,
    FlechaGoblin,
    FlechaDemoniaca,
    FlechaDeOsso,
    Pedra,
    Brasa,
    Bolha,
    OndaDeChoque,
    EstrelaDoOlho,
}

/// <summary>
/// Cada inimigo atira do seu jeito: formato, cor, animacao e, quando faz sentido,
/// movimento proprio (onda, aceleracao, freio, perseguir o jogador).
///
/// O <see cref="TiroDaSala"/> descobre o estilo sozinho por quem atirou
/// (<see cref="DoAtirador"/>): primeiro um <see cref="EstiloDoAtirador"/> no bicho, se
/// tiver; senao pelo nome que a <see cref="FabricaDeInimigos"/> da. Inimigo novo sem estilo
/// continua com a gema de sempre.
///
/// Arte: bola de fogo do mago e raio verde do necromante (Tiny RPG Pack 01), bala do
/// cavaleiro do canhao, flecha do demonio e do esqueleto (Tiny RPG), flecha e pedras do
/// Tiny Swords, respingo, poeira e cristais dos mesmos pacotes.
/// </summary>
public static class EstilosDeTiro
{
    private const float Pixels = 100f;

    private static readonly Dictionary<EstiloDeTiro, AparenciaDoProjetil> aparencias = new Dictionary<EstiloDeTiro, AparenciaDoProjetil>();

    /// <summary>O nome que a fabrica da ao inimigo -> o estilo do tiro dele.</summary>
    private static readonly Dictionary<string, EstiloDeTiro> porNome = new Dictionary<string, EstiloDeTiro>
    {
        { "Atirador", EstiloDeTiro.OrbeSombrio },          // bruxo
        { "Demonia", EstiloDeTiro.BolaDeFogo },
        { "Fogo fatuo", EstiloDeTiro.ChamaFantasma },
        { "Necromante", EstiloDeTiro.MaldicaoTeleguiada },
        { "Sentinela", EstiloDeTiro.BalaDeCanhao },        // cavaleiro do canhao
        { "Monstro de sangue", EstiloDeTiro.GotaDeSangue },
        { "Arqueiro", EstiloDeTiro.FlechaGoblin },
        { "Demonio arqueiro", EstiloDeTiro.FlechaDemoniaca },
        { "Esqueleto arqueiro", EstiloDeTiro.FlechaDeOsso },
        { "Chefe", EstiloDeTiro.Pedra },                   // golem
        { "Chefe Saltador", EstiloDeTiro.Brasa },          // demonio do martelo
        { "Chefe Necromante", EstiloDeTiro.Maldicao },
        { "Chefe Minotauro", EstiloDeTiro.OndaDeChoque },
        { "Chefe Final", EstiloDeTiro.EstrelaDoOlho },
    };

    /// <summary>O estilo do tiro de quem atirou.</summary>
    public static EstiloDeTiro DoAtirador(GameObject dono)
    {
        if (dono == null)
            return EstiloDeTiro.Gema;

        if (dono.TryGetComponent(out EstiloDoAtirador marcado))
            return marcado.Estilo;

        return porNome.TryGetValue(dono.name, out EstiloDeTiro estilo) ? estilo : EstiloDeTiro.Gema;
    }

    /// <summary>O som do disparo: flecha, fogo, canhao, gosma ou magia.</summary>
    public static Som SomDoDisparo(EstiloDeTiro estilo)
    {
        switch (estilo)
        {
            case EstiloDeTiro.FlechaGoblin:
            case EstiloDeTiro.FlechaDemoniaca:
            case EstiloDeTiro.FlechaDeOsso:
                return Som.FlechaInimigo;

            case EstiloDeTiro.BolaDeFogo:
            case EstiloDeTiro.Brasa:
            case EstiloDeTiro.ChamaFantasma:
                return Som.TiroDeFogo;

            case EstiloDeTiro.BalaDeCanhao:
            case EstiloDeTiro.Pedra:
            case EstiloDeTiro.OndaDeChoque:
                return Som.Canhao;

            case EstiloDeTiro.GotaDeSangue:
            case EstiloDeTiro.Bolha:
                return Som.Gosma;

            default:
                return Som.TiroInimigo;
        }
    }

    /// <summary>Muda o desenho e o movimento do tiro pro estilo dado.</summary>
    public static void Aplicar(TiroDaSala tiro, EstiloDeTiro estilo)
    {
        if (tiro == null || estilo == EstiloDeTiro.Gema)
            return;

        AparenciaDoProjetil aparencia = Aparencia(estilo);

        if (VisualDoProjetil.Vestir(tiro.gameObject, aparencia) == null)
            return;

        ComportamentoDoTiro.Colocar(tiro, estilo);
    }

    public static AparenciaDoProjetil Aparencia(EstiloDeTiro estilo)
    {
        if (aparencias.TryGetValue(estilo, out AparenciaDoProjetil ja))
            return ja;

        AparenciaDoProjetil a = Montar(estilo);
        aparencias[estilo] = a;
        return a;
    }

    // O tamanho e em diametros do colisor (o tiro comum tem 0.3 de diametro).
    private static AparenciaDoProjetil Montar(EstiloDeTiro estilo)
    {
        switch (estilo)
        {
            // Bruxo: bola de fogo roxa que vai ondulando.
            case EstiloDeTiro.OrbeSombrio:
                return new AparenciaDoProjetil(ArteImportada.BolaDeFogoVoando(Pixels), new Color(0.8f, 0.5f, 1f), 2.6f)
                {
                    quadrosPorSegundo = 12f,
                    intervaloDoRastro = 0.05f,
                    corDoRastro = new Color(0.6f, 0.3f, 0.9f, 0.4f),
                    impacto = EfeitoDeImpacto.Fogo,
                    corDoImpacto = new Color(0.8f, 0.5f, 1f),
                    tamanhoDoImpacto = 0.5f,
                };

            // Demonia: bola de fogo de verdade, que comeca devagar e acelera.
            case EstiloDeTiro.BolaDeFogo:
                return new AparenciaDoProjetil(ArteImportada.BolaDeFogoVoando(Pixels), Color.white, 2.6f)
                {
                    quadrosPorSegundo = 14f,
                    faiscas = ArteImportada.Chama(Pixels),
                    intervaloDasFaiscas = 0.08f,
                    tamanhoDasFaiscas = 0.25f,
                    impacto = EfeitoDeImpacto.Fogo,
                    corDoImpacto = Color.white,
                    tamanhoDoImpacto = 0.55f,
                };

            // Fogo fatuo: chama azul transparente, em zigue-zague largo.
            case EstiloDeTiro.ChamaFantasma:
                return new AparenciaDoProjetil(ArteImportada.BolaDeFogoVoando(Pixels), new Color(0.45f, 0.9f, 1f, 0.85f), 2.6f)
                {
                    quadrosPorSegundo = 10f,
                    pulso = 0.12f,
                    ritmoDoPulso = 4f,
                    intervaloDoRastro = 0.04f,
                    corDoRastro = new Color(0.4f, 0.8f, 1f, 0.35f),
                    duracaoDoRastro = 0.25f,
                    impacto = EfeitoDeImpacto.Fogo,
                    corDoImpacto = new Color(0.5f, 0.9f, 1f),
                    tamanhoDoImpacto = 0.5f,
                };

            // Necromante: raio verde; o do bicho comum persegue um pouco, o do chefe nao.
            case EstiloDeTiro.Maldicao:
            case EstiloDeTiro.MaldicaoTeleguiada:
                return new AparenciaDoProjetil(ArteImportada.MagiaVerde(Pixels), Color.white, 3f)
                {
                    quadrosPorSegundo = 8f,
                    anguloDoDesenho = -90f,
                    faiscas = ArteImportada.Poeira(Pixels),
                    intervaloDasFaiscas = 0.09f,
                    tamanhoDasFaiscas = 0.22f,
                    corDasFaiscas = new Color(0.45f, 0.85f, 0.3f, 0.6f),
                    impacto = EfeitoDeImpacto.NuvemVerde,
                    corDoImpacto = Color.white,
                    tamanhoDoImpacto = 0.7f,
                };

            // Cavaleiro do canhao: bala preta girando; bate levantando poeira.
            case EstiloDeTiro.BalaDeCanhao:
                return new AparenciaDoProjetil(ArteImportada.BalaDeCanhao(Pixels), Color.white, 1.5f)
                {
                    apontar = false,
                    giro = 540f,
                    impacto = EfeitoDeImpacto.Poeira,
                    corDoImpacto = new Color(0.8f, 0.8f, 0.8f),
                    tamanhoDoImpacto = 0.6f,
                };

            // Monstro de sangue: gota vermelha que treme e vai freando.
            case EstiloDeTiro.GotaDeSangue:
                return new AparenciaDoProjetil(Primeiro(ArteImportada.BolaDeFogoEstourando(Pixels)), new Color(0.8f, 0.1f, 0.15f), 3f)
                {
                    apontar = false,
                    pulso = 0.15f,
                    ritmoDoPulso = 5f,
                    impacto = EfeitoDeImpacto.Respingo,
                    corDoImpacto = new Color(0.7f, 0.05f, 0.1f),
                    tamanhoDoImpacto = 0.6f,
                };

            case EstiloDeTiro.FlechaGoblin:
                return Flecha(ArteImportada.Flecha(Pixels), Color.white, 2.4f, new Color(0f, 0f, 0f, 0f));

            case EstiloDeTiro.FlechaDemoniaca:
                return Flecha(ArteImportada.FlechaDoHeroi("Flecha", Pixels), new Color(1f, 0.7f, 0.7f), 2.4f,
                              new Color(0.8f, 0.1f, 0.1f, 0.45f));

            case EstiloDeTiro.FlechaDeOsso:
                return Flecha(ArteImportada.FlechaDoHeroi("Dardo", Pixels), new Color(0.95f, 0.92f, 0.8f), 2.4f,
                              new Color(0f, 0f, 0f, 0f));

            // Golem: pedrinha girando.
            case EstiloDeTiro.Pedra:
                return new AparenciaDoProjetil(ArteImportada.Pedra(3, Pixels), new Color(0.85f, 0.8f, 0.75f), 1.8f)
                {
                    apontar = false,
                    giro = -300f,
                    impacto = EfeitoDeImpacto.Poeira,
                    corDoImpacto = new Color(0.75f, 0.7f, 0.65f),
                    tamanhoDoImpacto = 0.7f,
                };

            // Demonio do martelo: brasa vermelha que acelera.
            case EstiloDeTiro.Brasa:
                return new AparenciaDoProjetil(ArteImportada.BolaDeFogoVoando(Pixels), new Color(1f, 0.45f, 0.35f), 2.6f)
                {
                    quadrosPorSegundo = 16f,
                    faiscas = ArteImportada.Chama(Pixels),
                    intervaloDasFaiscas = 0.1f,
                    tamanhoDasFaiscas = 0.22f,
                    corDasFaiscas = new Color(1f, 0.6f, 0.5f),
                    impacto = EfeitoDeImpacto.Fogo,
                    corDoImpacto = new Color(1f, 0.55f, 0.45f),
                    tamanhoDoImpacto = 0.5f,
                };

            // Bolha do chefe saltador: bolha que respira; estoura em respingo.
            case EstiloDeTiro.Bolha:
                return new AparenciaDoProjetil(ArteImportada.Bolhinha(Pixels), new Color(0.55f, 1f, 0.6f), 1.4f)
                {
                    apontar = false,
                    pulso = 0.12f,
                    ritmoDoPulso = 2.5f,
                    impacto = EfeitoDeImpacto.Respingo,
                    corDoImpacto = new Color(0.55f, 1f, 0.6f),
                    tamanhoDoImpacto = 0.9f,
                };

            // Minotauro: onda de choque (o risco de luz) atravessada no rumo, cortando o ar.
            case EstiloDeTiro.OndaDeChoque:
                return new AparenciaDoProjetil(ArteImportada.Corte(Pixels), new Color(1f, 0.7f, 0.35f), 2.6f)
                {
                    pulso = 0.1f,
                    ritmoDoPulso = 8f,
                    intervaloDoRastro = 0.04f,
                    corDoRastro = new Color(1f, 0.55f, 0.2f, 0.4f),
                    impacto = EfeitoDeImpacto.Poeira,
                    corDoImpacto = new Color(1f, 0.8f, 0.6f),
                    tamanhoDoImpacto = 0.6f,
                };

            // Olho do Porao: estrela vermelha girando.
            case EstiloDeTiro.EstrelaDoOlho:
                return new AparenciaDoProjetil(ArteImportada.Estrela(Pixels), new Color(1f, 0.35f, 0.55f), 2.2f)
                {
                    apontar = false,
                    giro = 420f,
                    pulso = 0.1f,
                    ritmoDoPulso = 5f,
                    impacto = EfeitoDeImpacto.Respingo,
                    corDoImpacto = new Color(0.9f, 0.2f, 0.45f),
                    tamanhoDoImpacto = 0.55f,
                };
        }

        return null;
    }

    private static Sprite Primeiro(Sprite[] quadros) => quadros != null && quadros.Length > 0 ? quadros[0] : null;

    private static AparenciaDoProjetil Flecha(Sprite sprite, Color cor, float tamanho, Color rastro)
    {
        return new AparenciaDoProjetil(sprite, cor, tamanho)
        {
            intervaloDoRastro = rastro.a > 0f ? 0.04f : 0f,
            corDoRastro = rastro,
            impacto = EfeitoDeImpacto.Poeira,
            corDoImpacto = new Color(0.85f, 0.8f, 0.7f),
            tamanhoDoImpacto = 0.4f,
        };
    }
}
