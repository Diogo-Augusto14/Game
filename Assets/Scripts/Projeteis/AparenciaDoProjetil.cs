using System.Collections.Generic;
using UnityEngine;

/// <summary>O efeito que aparece onde um projetil bate ou some.</summary>
public enum EfeitoDeImpacto
{
    Nenhum,
    Poeira,
    Explosao,
    Respingo,
    Fogo,
    NuvemVerde,
    Cristais,
}

/// <summary>
/// Como um projetil aparece e se mexe na tela: os quadros, a cor, o tamanho e as
/// animacoes (girar, pulsar, balancar, piscar, deixar rastro). Serve igual pras flechas do
/// jogador (<see cref="CatalogoDeFlechas"/>) e pros tiros dos inimigos (<see cref="EstilosDeTiro"/>).
///
/// Quem desenha e o <see cref="VisualDoProjetil"/>. O tamanho e relativo ao diametro do
/// colisor (a escala do objeto): 2 = o desenho tem o dobro do diametro no lado maior.
/// </summary>
public class AparenciaDoProjetil
{
    /// <summary>Quadros da animacao (um so = parado). Todos na mesma escala de pixels.</summary>
    public Sprite[] quadros;

    public float quadrosPorSegundo = 12f;

    public Color cor = Color.white;

    /// <summary>Lado maior do desenho, em diametros do colisor.</summary>
    public float tamanho = 2f;

    /// <summary>Gira o desenho pro rumo do voo (flecha, bola de fogo com rastro).</summary>
    public bool apontar = true;

    /// <summary>Pra onde o desenho aponta na imagem, em graus (0 = direita, -90 = baixo).</summary>
    public float anguloDoDesenho;

    /// <summary>Graus por segundo girando em volta de si (bala, pedra, estrela).</summary>
    public float giro;

    /// <summary>Quanto o tamanho pulsa (0.15 = 15% pra mais e pra menos) e quantas vezes por segundo.</summary>
    public float pulso;
    public float ritmoDoPulso = 6f;

    /// <summary>Graus de balanco pros lados, como flecha pesada cortando o ar.</summary>
    public float balanco;
    public float ritmoDoBalanco = 7f;

    /// <summary>Pisca entre a cor e esta (alfa 0 = nao pisca), como pavio aceso.</summary>
    public Color corDoPisca = new Color(0f, 0f, 0f, 0f);
    public float ritmoDoPisca = 10f;

    /// <summary>Copias do desenho que ficam pra tras e somem (0 = sem rastro).</summary>
    public float intervaloDoRastro;
    public Color corDoRastro = new Color(1f, 1f, 1f, 0.5f);
    public float duracaoDoRastro = 0.18f;

    /// <summary>Faiscas animadas soltas pelo caminho (chama da flecha explosiva, gota do veneno).</summary>
    public Sprite[] faiscas;
    public float intervaloDasFaiscas = 0.06f;
    public float tamanhoDasFaiscas = 0.5f;
    public Color corDasFaiscas = Color.white;

    /// <summary>O que aparece quando o projetil bate ou cai.</summary>
    public EfeitoDeImpacto impacto = EfeitoDeImpacto.Poeira;
    public Color corDoImpacto = Color.white;

    /// <summary>Tamanho do efeito de impacto, em unidades do mundo.</summary>
    public float tamanhoDoImpacto = 0.6f;

    public AparenciaDoProjetil(Sprite[] quadros, Color cor, float tamanho)
    {
        this.quadros = quadros;
        this.cor = cor;
        this.tamanho = tamanho;
    }

    public AparenciaDoProjetil(Sprite quadro, Color cor, float tamanho)
        : this(quadro != null ? new[] { quadro } : null, cor, tamanho)
    {
    }

    public bool TemDesenho => quadros != null && quadros.Length > 0 && quadros[0] != null;
}

/// <summary>
/// Os efeitos de impacto, todos de pacote: poeira, explosao e respingo do Tiny Swords Free
/// Pack, a bola de fogo estourando e a nuvem verde do Tiny RPG, os cristais do mago.
/// </summary>
public static class EfeitosDeImpacto
{
    /// <summary>Os quadros saem em 100 pixels por unidade; o tamanho vem pela escala.</summary>
    private const float Pixels = 100f;

    private static readonly Dictionary<EfeitoDeImpacto, Sprite[]> guardados = new Dictionary<EfeitoDeImpacto, Sprite[]>();

    public static void Mostrar(EfeitoDeImpacto tipo, Vector2 onde, Color cor, float tamanho)
    {
        if (tipo == EfeitoDeImpacto.Nenhum)
            return;

        Sprite[] quadros = Quadros(tipo);

        if (quadros == null)
            return;

        EfeitoDeQuadros efeito = EfeitoDeQuadros.Criar(quadros, Ritmo(tipo), onde, 25);

        if (efeito == null)
            return;

        efeito.transform.localScale = Vector3.one * EscalaPara(quadros[0], tamanho);
        efeito.GetComponent<SpriteRenderer>().color = cor;
    }

    /// <summary>Escala que deixa o lado maior do sprite com <paramref name="tamanho"/> unidades.</summary>
    public static float EscalaPara(Sprite sprite, float tamanho)
    {
        if (sprite == null)
            return 1f;

        Vector2 lados = sprite.bounds.size;
        float maior = Mathf.Max(lados.x, lados.y);
        return maior > 0.0001f ? tamanho / maior : 1f;
    }

    public static Sprite[] Quadros(EfeitoDeImpacto tipo)
    {
        if (guardados.TryGetValue(tipo, out Sprite[] ja))
            return ja;

        Sprite[] quadros = null;

        switch (tipo)
        {
            case EfeitoDeImpacto.Poeira: quadros = ArteImportada.Poeira(Pixels); break;
            case EfeitoDeImpacto.Explosao: quadros = ArteImportada.ExplosaoPequena(Pixels); break;
            case EfeitoDeImpacto.Respingo: quadros = ArteImportada.Respingo(Pixels); break;
            case EfeitoDeImpacto.Fogo: quadros = ArteImportada.BolaDeFogoEstourando(Pixels); break;
            case EfeitoDeImpacto.NuvemVerde: quadros = ArteImportada.MagiaVerdeEstourando(Pixels); break;
            case EfeitoDeImpacto.Cristais: quadros = ArteImportada.CristaisSeJuntando(Pixels); break;
        }

        guardados[tipo] = quadros;
        return quadros;
    }

    private static float Ritmo(EfeitoDeImpacto tipo)
    {
        switch (tipo)
        {
            case EfeitoDeImpacto.Fogo: return 14f;
            case EfeitoDeImpacto.NuvemVerde: return 10f;
            case EfeitoDeImpacto.Cristais: return 10f;
            default: return 20f;
        }
    }
}
