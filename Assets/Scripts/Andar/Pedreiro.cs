using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Transforma a planta da <see cref="Caverna"/> em mundo, com a arte do pacote Old Prison, do jeito
/// que o Tiled Map Editor do pacote monta: cada ladrilho e escolhido pelos seus 4 cantos (a tabela de
/// cantos) e depois as regras de encaixe poem as faces de tijolo e as variacoes (<see cref="Automapa"/>).
///
/// As camadas, de baixo pra cima (cada uma um Tilemap):
///
///   abismo     o fundo roxo, so nos buracos
///   chao       a plataforma de pedra; nos buracos ela acaba numa beirada com face caindo no abismo
///   sangue     as pocas (so enfeite)
///   enfeites   ossos, pedrinhas e papeis soltos
///   paredes    as paredes altas: o topo e a face de tijolo de 2 de altura (atras de quem anda)
///
/// e os colisores: as paredes na camada "Wall" (seguram gente e tiro) e os buracos na camada "Buraco"
/// (seguram gente; o tiro passa por cima).
///
/// O ladrilho fica entre as celulas: os 4 cantos do ladrilho com canto de baixo-esquerdo em (x, y)
/// sao os centros das celulas (x, y), (x + 1, y), (x, y + 1) e (x + 1, y + 1). 1 ladrilho = 1 unidade.
/// </summary>
public class Pedreiro
{
    public const int OrdemDoAbismo = -104;
    public const int OrdemDoChao = -103;
    public const int OrdemDoSangue = -102;
    public const int OrdemDosEnfeites = -101;

    /// <summary>As paredes ficam atras de quem anda (a face de tijolo se ve de frente).</summary>
    public const int OrdemDasParedes = 5;

    /// <summary>Celulas de parede desenhadas em volta da caverna (alem disso, so o fundo da camera).</summary>
    private const int Margem = 5;

    private static int camadaDaParede = -1;
    private static int camadaDoBuraco = -2;

    private readonly Folha chao;
    private readonly Folha paredes;
    private readonly Folha abismo;
    private readonly Folha sangue;
    private readonly Folha enfeites;
    private readonly float chanceDeEnfeite;

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

    /// <summary>A camada "Buraco": segura quem anda, o tiro ignora. -1 se o projeto nao tiver essa camada.</summary>
    public static int CamadaDoBuraco
    {
        get
        {
            if (camadaDoBuraco == -2)
                camadaDoBuraco = LayerMask.NameToLayer("Buraco");

            return camadaDoBuraco;
        }
    }

    public Pedreiro(Texture2D chao, Texture2D paredes, Texture2D abismo, Texture2D sangue, Texture2D enfeites, float chanceDeEnfeite)
    {
        this.chao = new Folha(chao);
        this.paredes = new Folha(paredes);
        this.abismo = new Folha(abismo);
        this.sangue = new Folha(sangue);
        this.enfeites = new Folha(enfeites);
        this.chanceDeEnfeite = chanceDeEnfeite;
    }

    /// <summary>
    /// Monta a caverna como filhos de <paramref name="pai"/>. A planta (<paramref name="celulasDeChao"/>)
    /// inclui os buracos; quem anda pisa no chao sem os buracos.
    /// </summary>
    public void Construir(Transform pai, HashSet<Vector2Int> celulasDeChao, HashSet<Vector2Int> buracos,
                          HashSet<Vector2Int> pocas, System.Random sorte)
    {
        RectInt planta = Caverna.Limites(celulasDeChao);
        RectInt limites = new RectInt(planta.xMin - Margem, planta.yMin - Margem, planta.width + Margem * 2, planta.height + Margem * 2);
        Grade grade = new Grade(limites);

        // Paredes: o topo e escolhido pelos cantos; as regras poem as faces embaixo dele.
        int[,] paredesDaGrade = grade.PorCantos(c => Caverna.Topo(celulasDeChao, c.x, c.y), DadosDoOldPrison.CantosDasParedes, null, sorte);
        Automapa.Aplicar(paredesDaGrade, DadosDoOldPrison.RegrasQuePoem, false, sorte);
        Automapa.Aplicar(paredesDaGrade, DadosDoOldPrison.RegrasDeVariacao, true, sorte);

        // Chao: a plataforma e tudo que nao e buraco (embaixo das paredes ela fica escondida).
        int[,] chaoDaGrade = grade.PorCantos(c => !buracos.Contains(c), DadosDoOldPrison.CantosDoChao, Sorteio.Chao, sorte);
        Automapa.Aplicar(chaoDaGrade, DadosDoOldPrison.RegrasQuePoem, false, sorte);
        Automapa.Aplicar(chaoDaGrade, DadosDoOldPrison.RegrasDeVariacao, true, sorte);

        // Abismo: embaixo de todo ladrilho que encosta num buraco.
        int[,] abismoDaGrade = grade.Vazia();
        int[,] sangueDaGrade = grade.PorCantos(c => pocas.Contains(c), DadosDoOldPrison.CantosDoSangue, Sorteio.Sangue, sorte);
        int[,] enfeitesDaGrade = grade.Vazia();

        for (int coluna = 0; coluna < grade.Largura; coluna++)
        {
            for (int linha = 0; linha < grade.Altura; linha++)
            {
                Vector2Int canto = grade.Canto(coluna, linha);

                if (grade.AlgumCanto(canto, c => buracos.Contains(c)))
                    abismoDaGrade[coluna, linha] = sorte.Next(DadosDoOldPrison.LadrilhosDoAbismo);
                else if (sangueDaGrade[coluna, linha] < 0 && grade.TodosOsCantos(canto, c => Andavel(celulasDeChao, buracos, c))
                         && sorte.NextDouble() < chanceDeEnfeite)
                    enfeitesDaGrade[coluna, linha] = sorte.Next(DadosDoOldPrison.Enfeites);
            }
        }

        Transform ladrilhos = new GameObject("Ladrilhos").transform;
        ladrilhos.SetParent(pai, false);
        ladrilhos.gameObject.AddComponent<Grid>();

        grade.Pintar(ladrilhos, "Abismo", abismoDaGrade, abismo, OrdemDoAbismo);
        grade.Pintar(ladrilhos, "Chao", chaoDaGrade, chao, OrdemDoChao);
        grade.Pintar(ladrilhos, "Sangue", sangueDaGrade, sangue, OrdemDoSangue);
        grade.Pintar(ladrilhos, "Enfeites", enfeitesDaGrade, enfeites, OrdemDosEnfeites);
        grade.Pintar(ladrilhos, "Paredes", paredesDaGrade, paredes, OrdemDasParedes);

        // Colisores so onde encosta em quem anda: o resto ninguem alcanca.
        HashSet<Vector2Int> paredesSolidas = new HashSet<Vector2Int>();
        HashSet<Vector2Int> buracosSolidos = new HashSet<Vector2Int>();

        for (int x = limites.xMin; x < limites.xMax; x++)
        {
            for (int y = limites.yMin; y < limites.yMax; y++)
            {
                Vector2Int c = new Vector2Int(x, y);

                if (!EncostaEmQuemAnda(celulasDeChao, buracos, c))
                    continue;

                if (!celulasDeChao.Contains(c))
                    paredesSolidas.Add(c);
                else if (buracos.Contains(c))
                    buracosSolidos.Add(c);
            }
        }

        Colisores(pai, "Colisores das paredes", paredesSolidas, CamadaDaParede);
        Colisores(pai, "Colisores dos buracos", buracosSolidos, Mathf.Max(0, CamadaDoBuraco));
    }

    /// <summary>Onde da pra pisar: chao que nao e buraco.</summary>
    public static bool Andavel(HashSet<Vector2Int> celulasDeChao, HashSet<Vector2Int> buracos, Vector2Int c) =>
        celulasDeChao.Contains(c) && !buracos.Contains(c);

    private static bool EncostaEmQuemAnda(HashSet<Vector2Int> celulasDeChao, HashSet<Vector2Int> buracos, Vector2Int c)
    {
        if (Andavel(celulasDeChao, buracos, c))
            return false;

        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (Andavel(celulasDeChao, buracos, c + new Vector2Int(dx, dy)))
                    return true;

        return false;
    }

    private static void Colisores(Transform pai, string nome, HashSet<Vector2Int> celulas, int camada)
    {
        Transform grupo = new GameObject(nome).transform;
        grupo.SetParent(pai, false);

        foreach (RectInt r in Caverna.Juntar(celulas))
        {
            GameObject obj = new GameObject("Colisor");
            obj.transform.SetParent(grupo, false);
            obj.transform.position = new Vector3(r.xMin - 0.5f + r.width * 0.5f, r.yMin - 0.5f + r.height * 0.5f, 0f);
            obj.layer = camada;
            obj.AddComponent<BoxCollider2D>().size = new Vector2(r.width, r.height);
        }
    }

    // ---------------------------------------------------------------- sorteios dos ladrilhos cheios
    private static class Sorteio
    {
        public static int Chao(System.Random sorte)
        {
            float total = 0f;

            foreach (float peso in DadosDoOldPrison.PesoDoChaoInteiro)
                total += peso;

            double ponto = sorte.NextDouble() * total;

            for (int i = 0; i < DadosDoOldPrison.ChaoInteiro.Length; i++)
            {
                ponto -= DadosDoOldPrison.PesoDoChaoInteiro[i];

                if (ponto <= 0)
                    return DadosDoOldPrison.ChaoInteiro[i];
            }

            return DadosDoOldPrison.ChaoInteiro[0];
        }

        public static int Sangue(System.Random sorte) =>
            DadosDoOldPrison.SangueInteiro[sorte.Next(DadosDoOldPrison.SangueInteiro.Length)];
    }

    // ---------------------------------------------------------------- a grade de ladrilhos
    /// <summary>
    /// Os ladrilhos que cobrem a caverna, como [coluna, linha] com as linhas crescendo pra baixo (o
    /// jeito do Tiled, que as regras esperam).
    /// </summary>
    private class Grade
    {
        private readonly int esquerda;
        private readonly int topo;

        public readonly int Largura;
        public readonly int Altura;

        public Grade(RectInt celulas)
        {
            // Os ladrilhos ficam entre as celulas: um a mais em cada direcao.
            esquerda = celulas.xMin - 1;
            topo = celulas.yMax - 1;
            Largura = celulas.width + 1;
            Altura = celulas.height + 1;
        }

        /// <summary>O canto de baixo-esquerdo do ladrilho (a celula de onde ele comeca).</summary>
        public Vector2Int Canto(int coluna, int linha) => new Vector2Int(esquerda + coluna, topo - linha);

        public int[,] Vazia()
        {
            int[,] grade = new int[Largura, Altura];

            for (int x = 0; x < Largura; x++)
                for (int y = 0; y < Altura; y++)
                    grade[x, y] = -1;

            return grade;
        }

        /// <summary>
        /// Cada ladrilho pela tabela de cantos (cima-esq * 8 + cima-dir * 4 + baixo-dir * 2 + baixo-esq).
        /// O cheio (os 4 dentro) pode ser sorteado.
        /// </summary>
        public int[,] PorCantos(System.Func<Vector2Int, bool> dentro, int[] tabela, System.Func<System.Random, int> cheio, System.Random sorte)
        {
            int[,] grade = new int[Largura, Altura];

            for (int coluna = 0; coluna < Largura; coluna++)
            {
                for (int linha = 0; linha < Altura; linha++)
                {
                    Vector2Int c = Canto(coluna, linha);
                    int indice = (dentro(c + Vector2Int.up) ? 8 : 0) + (dentro(c + Vector2Int.one) ? 4 : 0)
                               + (dentro(c + Vector2Int.right) ? 2 : 0) + (dentro(c) ? 1 : 0);
                    grade[coluna, linha] = indice == 15 && cheio != null ? cheio(sorte) : tabela[indice];
                }
            }

            return grade;
        }

        public bool AlgumCanto(Vector2Int c, System.Func<Vector2Int, bool> teste) =>
            teste(c) || teste(c + Vector2Int.right) || teste(c + Vector2Int.up) || teste(c + Vector2Int.one);

        public bool TodosOsCantos(Vector2Int c, System.Func<Vector2Int, bool> teste) =>
            teste(c) && teste(c + Vector2Int.right) && teste(c + Vector2Int.up) && teste(c + Vector2Int.one);

        /// <summary>Poe os ladrilhos num Tilemap novo. O ladrilho (x, y) cobre do ponto (x, y) ao (x + 1, y + 1).</summary>
        public void Pintar(Transform grid, string nome, int[,] ladrilhos, Folha folha, int ordem)
        {
            GameObject obj = new GameObject(nome);
            obj.transform.SetParent(grid, false);
            Tilemap mapa = obj.AddComponent<Tilemap>();
            obj.AddComponent<TilemapRenderer>().sortingOrder = ordem;

            List<Vector3Int> lugares = new List<Vector3Int>();
            List<TileBase> pecas = new List<TileBase>();

            for (int coluna = 0; coluna < Largura; coluna++)
            {
                for (int linha = 0; linha < Altura; linha++)
                {
                    Tile peca = folha.Ladrilho(ladrilhos[coluna, linha]);

                    if (peca == null)
                        continue;

                    Vector2Int c = Canto(coluna, linha);
                    lugares.Add(new Vector3Int(c.x, c.y, 0));
                    pecas.Add(peca);
                }
            }

            mapa.SetTiles(lugares.ToArray(), pecas.ToArray());
        }
    }

    // ---------------------------------------------------------------- uma folha de ladrilhos de 32 x 32
    /// <summary>Corta os ladrilhos de uma folha so quando precisa, e guarda.</summary>
    private class Folha
    {
        private readonly Texture2D textura;
        private readonly Dictionary<int, Tile> prontos = new Dictionary<int, Tile>();

        public Folha(Texture2D textura)
        {
            this.textura = textura;
        }

        public Tile Ladrilho(int numero)
        {
            if (numero < 0 || textura == null)
                return null;

            if (prontos.TryGetValue(numero, out Tile pronto))
                return pronto;

            int lado = DadosDoOldPrison.Lado;
            int colunas = textura.width / lado;
            int coluna = numero % colunas, linha = numero / colunas;

            // Na textura a linha 0 e embaixo; a primeira linha de ladrilhos e a de cima.
            Rect recorte = new Rect(coluna * lado, textura.height - (linha + 1) * lado, lado, lado);
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Sprite.Create(textura, recorte, new Vector2(0.5f, 0.5f), lado, 0, SpriteMeshType.FullRect);
            tile.colliderType = Tile.ColliderType.None;
            prontos[numero] = tile;
            return tile;
        }
    }
}
