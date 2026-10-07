using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Transforma a planta da <see cref="Caverna"/> (as celulas de chao) em coisas do mundo:
///
///   chao    um desenho ladrilhado so, por baixo da caverna inteira
///   frente  a face de tijolos das paredes que tem chao logo embaixo (fica atras de quem anda)
///   topo    o resto da rocha, visto de cima (fica na frente de quem anda) ate uma margem em volta
///   parede  os colisores, so nas celulas de rocha encostadas no chao (camada "Wall")
///
/// Cada celula e 1 unidade; a celula (x, y) tem o centro no ponto (x, y) do mundo. Pra nao criar um
/// objeto por celula, as celulas vizinhas iguais viram poucos retangulos (<see cref="Caverna.Juntar"/>).
/// </summary>
public class Pedreiro
{
    // Chao embaixo de tudo; a frente da parede atras de quem anda; o topo na frente de quem anda.
    public const int OrdemDoChao = -100;
    public const int OrdemDaFrente = 5;
    public const int OrdemDoTopo = 40;

    /// <summary>Celulas de rocha desenhadas em volta da caverna (alem disso, so o fundo escuro da camera).</summary>
    private const int Margem = 4;

    private static int camadaDaParede = -1;

    private readonly Sprite chao;
    private readonly Sprite frente;
    private readonly Sprite topo;
    private readonly Color tomDoChao;

    /// <summary>A camada "Wall" (ou a Default, se o projeto nao tiver essa camada).</summary>
    public static int CamadaDaParede
    {
        get
        {
            if (camadaDaParede < 0)
                camadaDaParede = Mathf.Max(0, LayerMask.NameToLayer("Wall"));

            return camadaDaParede;
        }
    }

    public Pedreiro(Sprite chao, Sprite frente, Sprite topo, Color tomDoChao)
    {
        this.chao = chao;
        this.frente = frente;
        this.topo = topo;
        this.tomDoChao = tomDoChao;
    }

    /// <summary>Monta a caverna inteira como filhos de <paramref name="pai"/>.</summary>
    public void Construir(Transform pai, HashSet<Vector2Int> celulasDeChao)
    {
        RectInt limites = Limites(celulasDeChao);

        Transform pecasDoChao = Grupo(pai, "Chao");
        Transform pecasDaFrente = Grupo(pai, "Frente das paredes");
        Transform pecasDoTopo = Grupo(pai, "Topo das paredes");
        Transform pecasSolidas = Grupo(pai, "Colisores das paredes");

        // O chao: um ladrilhado so, do tamanho da caverna; a rocha cobre o que nao e chao.
        Peca(pecasDoChao, "Chao", Mundo(limites), chao, OrdemDoChao, tomDoChao);

        HashSet<Vector2Int> deFrente = new HashSet<Vector2Int>();
        HashSet<Vector2Int> deTopo = new HashSet<Vector2Int>();
        HashSet<Vector2Int> solidas = new HashSet<Vector2Int>();

        for (int x = limites.xMin - Margem; x < limites.xMax + Margem; x++)
        {
            for (int y = limites.yMin - Margem; y < limites.yMax + Margem; y++)
            {
                Vector2Int c = new Vector2Int(x, y);

                if (celulasDeChao.Contains(c))
                    continue;

                if (celulasDeChao.Contains(c + Vector2Int.down))
                    deFrente.Add(c);
                else
                    deTopo.Add(c);

                if (EncostaNoChao(celulasDeChao, c))
                    solidas.Add(c);
            }
        }

        foreach (RectInt r in Caverna.Juntar(deFrente))
            Peca(pecasDaFrente, "Parede", Mundo(r), frente, OrdemDaFrente, Color.white);

        foreach (RectInt r in Caverna.Juntar(deTopo))
            Peca(pecasDoTopo, "Rocha", Mundo(r), topo, OrdemDoTopo, Color.white);

        foreach (RectInt r in Caverna.Juntar(solidas))
        {
            GameObject obj = new GameObject("Parede");
            obj.transform.SetParent(pecasSolidas, false);
            obj.transform.position = Mundo(r).center;
            obj.layer = CamadaDaParede;
            obj.AddComponent<BoxCollider2D>().size = Mundo(r).size;
        }
    }

    /// <summary>Um retangulo desenhado com o sprite repetido (ladrilhado).</summary>
    private static void Peca(Transform pai, string nome, Rect lugar, Sprite desenho, int ordem, Color cor)
    {

        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(pai, false);
        obj.transform.position = lugar.center;

        SpriteRenderer sprite = obj.AddComponent<SpriteRenderer>();
        sprite.sprite = desenho;
        sprite.drawMode = SpriteDrawMode.Tiled;
        sprite.size = lugar.size;
        sprite.sortingOrder = ordem;
        sprite.color = cor;
    }

    /// <summary>Sprite de uma textura inteira, pronto pra ser ladrilhado (FullRect, pivo no meio).</summary>
    public static Sprite Ladrilho(Texture2D textura, float pixelsPorUnidade)
    {
        if (textura == null)
            return null;

        return Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0.5f, 0.5f),
                             pixelsPorUnidade, 0, SpriteMeshType.FullRect);
    }

    /// <summary>O retangulo do mundo coberto por um bloco de celulas.</summary>
    public static Rect Mundo(RectInt celulas) =>
        new Rect(celulas.xMin - 0.5f, celulas.yMin - 0.5f, celulas.width, celulas.height);

    private static RectInt Limites(HashSet<Vector2Int> celulas)
    {
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;

        foreach (Vector2Int c in celulas)
        {
            x0 = Mathf.Min(x0, c.x);
            y0 = Mathf.Min(y0, c.y);
            x1 = Mathf.Max(x1, c.x);
            y1 = Mathf.Max(y1, c.y);
        }

        return new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
    }

    private static bool EncostaNoChao(HashSet<Vector2Int> chao, Vector2Int c)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (chao.Contains(c + new Vector2Int(dx, dy)))
                    return true;

        return false;
    }

    private static Transform Grupo(Transform pai, string nome)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(pai, false);
        return obj.transform;
    }
}
