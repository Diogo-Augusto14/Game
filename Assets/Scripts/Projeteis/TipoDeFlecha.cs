using System.Collections.Generic;
using UnityEngine;

/// <summary>Os tipos de flecha do jogador. Cada um (menos a normal) sai de um item.</summary>
public enum TipoDeFlecha
{
    Normal,
    Rapida,
    Pesada,
    Explosiva,
    Perfurante,
    Gelo,
    Venenosa,
    Ricochete,
}

/// <summary>
/// Tudo que um tipo de flecha muda: os numeros do tiro (multiplicadores sobre os do heroi e
/// dos itens), o comportamento especial (explodir, atravessar, gelar, envenenar, quicar) e
/// a aparencia. O item que da a flecha tem o mesmo nome e descricao.
/// </summary>
public class DefinicaoDeFlecha
{
    public TipoDeFlecha tipo;
    public string nome;

    [Tooltip("Frase curta com o efeito: aparece ao pegar o item")]
    public string descricao;

    /// <summary>Cor do nome na HUD e do item.</summary>
    public Color cor = Color.white;

    // ---------------- numeros ----------------
    public float multiplicaDano = 1f;
    public float multiplicaCadencia = 1f;
    public float multiplicaVelocidade = 1f;
    public float multiplicaAlcance = 1f;
    public float multiplicaEmpurrao = 1f;
    public float multiplicaTamanho = 1f;

    // ---------------- comportamento ----------------
    public bool atravessa;

    /// <summary>Raio da explosao ao bater ou cair (0 = nao explode) e o dano dela, em fracao do dano da flecha.</summary>
    public float raioDaExplosao;
    public float danoDaExplosao;

    /// <summary>Fracao da velocidade que sobra no inimigo gelado, e por quanto tempo.</summary>
    public float lentidao = 1f;
    public float tempoDeLentidao;

    /// <summary>Veneno: quantas mordidas, o dano de cada uma (fracao do dano da flecha) e o intervalo.</summary>
    public int mordidasDeVeneno;
    public float danoDoVeneno;
    public float intervaloDoVeneno = 0.8f;

    /// <summary>Quantas vezes quica na parede antes de quebrar.</summary>
    public int ricochetes;

    /// <summary>Como a flecha aparece. Null = o tiro do heroi (flecha normal).</summary>
    public AparenciaDoProjetil aparencia;

    public bool Especial => tipo != TipoDeFlecha.Normal;
}

/// <summary>
/// Os tipos de flecha, com a arte dos pacotes: flechas do Tiny RPG (soldado, arqueiro,
/// esqueleto, demonio) e do Tiny Swords, os cristais do mago e o raio verde do necromante;
/// chama, poeira, explosao e respingo do Tiny Swords Free Pack.
/// </summary>
public static class CatalogoDeFlechas
{
    /// <summary>A arte das flechas sai em 100 pixels por unidade; o tamanho vem pela escala.</summary>
    private const float Pixels = 100f;

    private static Dictionary<TipoDeFlecha, DefinicaoDeFlecha> todas;
    private static readonly Dictionary<TipoDeFlecha, Sprite> icones = new Dictionary<TipoDeFlecha, Sprite>();

    public static DefinicaoDeFlecha De(TipoDeFlecha tipo)
    {
        if (todas == null)
            todas = Montar();

        return todas.TryGetValue(tipo, out DefinicaoDeFlecha d) ? d : todas[TipoDeFlecha.Normal];
    }

    /// <summary>As flechas especiais, na ordem do enum (cada uma vira um item).</summary>
    public static IEnumerable<DefinicaoDeFlecha> Especiais()
    {
        foreach (TipoDeFlecha tipo in System.Enum.GetValues(typeof(TipoDeFlecha)))
            if (tipo != TipoDeFlecha.Normal)
                yield return De(tipo);
    }

    /// <summary>O desenho da flecha como icone de item (~1 unidade no lado maior). Null = nao e flecha.</summary>
    public static Sprite IconeDoItem(string nomeDoItem)
    {
        foreach (DefinicaoDeFlecha d in Especiais())
            if (d.nome == nomeDoItem)
                return Icone(d.tipo);

        return null;
    }

    /// <summary>O desenho da flecha com 1 unidade no lado maior (HUD, pedestal, loja).</summary>
    public static Sprite Icone(TipoDeFlecha tipo)
    {
        if (icones.TryGetValue(tipo, out Sprite ja))
            return ja;

        // Os pixels por unidade sao o lado maior de cada recorte: o desenho sai com 1 unidade.
        Sprite icone = null;

        switch (tipo)
        {
            case TipoDeFlecha.Normal: icone = ArteImportada.Flecha(48f); break;
            case TipoDeFlecha.Rapida: icone = ArteImportada.FlechaDoHeroi("FlechaDoArqueiro", 22f); break;
            case TipoDeFlecha.Pesada: icone = ArteImportada.IconeDoPacote(2155); break;
            case TipoDeFlecha.Explosiva: icone = ArteImportada.IconeDoPacote(1002); break;
            case TipoDeFlecha.Perfurante: icone = ArteImportada.IconeDoPacote(2154); break;
            case TipoDeFlecha.Gelo: icone = Primeiro(ArteImportada.CristalGirando(40f)); break;
            case TipoDeFlecha.Venenosa: icone = ArteImportada.IconeDoPacote(778); break;
            case TipoDeFlecha.Ricochete: icone = ArteImportada.Flecha(48f); break;
        }

        icones[tipo] = icone;
        return icone;
    }

    private static Sprite Primeiro(Sprite[] quadros) => quadros != null && quadros.Length > 0 ? quadros[0] : null;

    private static Dictionary<TipoDeFlecha, DefinicaoDeFlecha> Montar()
    {
        var lista = new Dictionary<TipoDeFlecha, DefinicaoDeFlecha>();

        lista[TipoDeFlecha.Normal] = new DefinicaoDeFlecha
        {
            tipo = TipoDeFlecha.Normal,
            nome = "Tiro Normal",
            descricao = "O tiro de sempre do heroi",
            cor = Color.white,
        };

        // Rapida: a flecha de pena dourada do arqueiro, com rastro de fantasmas.
        lista[TipoDeFlecha.Rapida] = new DefinicaoDeFlecha
        {
            tipo = TipoDeFlecha.Rapida,
            nome = "Encanto Ligeiro",
            descricao = "Voa bem mais rapido e sai mais seguido, mas bate um pouco mais fraco",
            cor = new Color(1f, 0.9f, 0.4f),
            multiplicaVelocidade = 1.6f,
            multiplicaCadencia = 1.25f,
            multiplicaDano = 0.8f,
            aparencia = new AparenciaDoProjetil(ArteImportada.FlechaDoHeroi("FlechaDoArqueiro", Pixels), new Color(1f, 0.95f, 0.65f), 2.1f)
            {
                intervaloDoRastro = 0.025f,
                corDoRastro = new Color(1f, 0.85f, 0.35f, 0.45f),
                duracaoDoRastro = 0.12f,
                impacto = EfeitoDeImpacto.Poeira,
                corDoImpacto = new Color(1f, 0.95f, 0.75f, 0.8f),
                tamanhoDoImpacto = 0.45f,
            },
        };

        // Pesada: a flecha grossa do soldado, cor de ferro, balancando no ar.
        lista[TipoDeFlecha.Pesada] = new DefinicaoDeFlecha
        {
            tipo = TipoDeFlecha.Pesada,
            nome = "Encanto Pesado",
            descricao = "Tiro lento e maior, quase o dobro do dano e empurra o inimigo longe",
            cor = new Color(0.7f, 0.78f, 0.9f),
            multiplicaDano = 1.8f,
            multiplicaEmpurrao = 3f,
            multiplicaVelocidade = 0.7f,
            multiplicaCadencia = 0.75f,
            multiplicaTamanho = 1.35f,
            aparencia = new AparenciaDoProjetil(ArteImportada.FlechaDoHeroi("FlechaDoSoldado", Pixels), new Color(0.75f, 0.8f, 0.92f), 2.2f)
            {
                balanco = 7f,
                ritmoDoBalanco = 9f,
                impacto = EfeitoDeImpacto.Poeira,
                corDoImpacto = new Color(0.85f, 0.85f, 0.85f),
                tamanhoDoImpacto = 0.95f,
            },
        };

        // Explosiva: a flecha escura do demonio com o pavio piscando e chama pelo caminho.
        lista[TipoDeFlecha.Explosiva] = new DefinicaoDeFlecha
        {
            tipo = TipoDeFlecha.Explosiva,
            nome = "Encanto Explosivo",
            descricao = "Explode ao bater ou cair e machuca todo inimigo em volta",
            cor = new Color(1f, 0.5f, 0.25f),
            multiplicaCadencia = 0.8f,
            raioDaExplosao = 1.1f,
            danoDaExplosao = 0.7f,
            aparencia = new AparenciaDoProjetil(ArteImportada.FlechaDoHeroi("Flecha", Pixels), new Color(1f, 0.6f, 0.4f), 2.1f)
            {
                corDoPisca = new Color(1f, 0.95f, 0.55f),
                ritmoDoPisca = 9f,
                faiscas = ArteImportada.Chama(Pixels),
                intervaloDasFaiscas = 0.05f,
                tamanhoDasFaiscas = 0.35f,
                impacto = EfeitoDeImpacto.Nenhum,   // a explosao desenha a sua
            },
        };

        // Perfurante: a flecha fina do esqueleto, gelo-azulada, com rastro de luz.
        lista[TipoDeFlecha.Perfurante] = new DefinicaoDeFlecha
        {
            tipo = TipoDeFlecha.Perfurante,
            nome = "Encanto Perfurante",
            descricao = "Atravessa todos os inimigos da fila e voa mais longe",
            cor = new Color(0.6f, 1f, 1f),
            atravessa = true,
            multiplicaAlcance = 1.3f,
            multiplicaVelocidade = 1.3f,
            multiplicaDano = 1.1f,
            aparencia = new AparenciaDoProjetil(ArteImportada.FlechaDoHeroi("Dardo", Pixels), new Color(0.75f, 1f, 1f), 2.3f)
            {
                pulso = 0.08f,
                ritmoDoPulso = 10f,
                intervaloDoRastro = 0.02f,
                corDoRastro = new Color(0.45f, 0.9f, 1f, 0.5f),
                duracaoDoRastro = 0.1f,
                impacto = EfeitoDeImpacto.Respingo,
                corDoImpacto = new Color(0.7f, 1f, 1f, 0.8f),
                tamanhoDoImpacto = 0.5f,
            },
        };

        // Gelo: a estrela de cristal do mago girando; deixa o inimigo lento.
        lista[TipoDeFlecha.Gelo] = new DefinicaoDeFlecha
        {
            tipo = TipoDeFlecha.Gelo,
            nome = "Encanto de Gelo",
            descricao = "Congela: o inimigo atingido fica lento por um tempo",
            cor = new Color(0.55f, 0.8f, 1f),
            multiplicaDano = 0.9f,
            lentidao = 0.45f,
            tempoDeLentidao = 2.5f,
            aparencia = new AparenciaDoProjetil(ArteImportada.CristalGirando(Pixels), Color.white, 1.8f)
            {
                quadrosPorSegundo = 10f,
                apontar = false,
                giro = 360f,
                intervaloDoRastro = 0.05f,
                corDoRastro = new Color(0.6f, 0.85f, 1f, 0.35f),
                duracaoDoRastro = 0.15f,
                impacto = EfeitoDeImpacto.Cristais,
                corDoImpacto = new Color(0.85f, 0.95f, 1f),
                tamanhoDoImpacto = 0.9f,
            },
        };

        // Venenosa: o raio verde do necromante, pingando fumaca verde.
        lista[TipoDeFlecha.Venenosa] = new DefinicaoDeFlecha
        {
            tipo = TipoDeFlecha.Venenosa,
            nome = "Encanto Venenoso",
            descricao = "Envenena: o inimigo continua perdendo vida por alguns segundos",
            cor = new Color(0.55f, 0.95f, 0.35f),
            multiplicaDano = 0.7f,
            mordidasDeVeneno = 3,
            danoDoVeneno = 0.45f,
            intervaloDoVeneno = 0.8f,
            aparencia = new AparenciaDoProjetil(ArteImportada.MagiaVerde(Pixels), Color.white, 2.4f)
            {
                quadrosPorSegundo = 8f,
                anguloDoDesenho = -90f,
                faiscas = ArteImportada.Poeira(Pixels),
                intervaloDasFaiscas = 0.07f,
                tamanhoDasFaiscas = 0.25f,
                corDasFaiscas = new Color(0.45f, 0.85f, 0.3f, 0.7f),
                impacto = EfeitoDeImpacto.NuvemVerde,
                corDoImpacto = Color.white,
                tamanhoDoImpacto = 0.9f,
            },
        };

        // Ricochete: a flecha do Tiny Swords, dourada, quicando nas paredes.
        lista[TipoDeFlecha.Ricochete] = new DefinicaoDeFlecha
        {
            tipo = TipoDeFlecha.Ricochete,
            nome = "Encanto Ricochete",
            descricao = "Quica nas paredes ate 3 vezes antes de quebrar",
            cor = new Color(1f, 0.8f, 0.3f),
            ricochetes = 3,
            multiplicaAlcance = 1.6f,
            aparencia = new AparenciaDoProjetil(ArteImportada.Flecha(Pixels), new Color(1f, 0.82f, 0.35f), 2f)
            {
                intervaloDoRastro = 0.04f,
                corDoRastro = new Color(1f, 0.8f, 0.3f, 0.35f),
                duracaoDoRastro = 0.15f,
                impacto = EfeitoDeImpacto.Poeira,
                corDoImpacto = new Color(1f, 0.85f, 0.45f, 0.85f),
                tamanhoDoImpacto = 0.5f,
            },
        };

        return lista;
    }
}
