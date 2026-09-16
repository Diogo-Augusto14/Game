#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Le as folhas de sprites das pastas de arte e gera o asset
/// <c>Assets/Resources/BibliotecaDeAnimacoes.asset</c> com TODOS os clipes do jogo.
///
/// Menu: Tools ▸ Jogo ▸ Reconstruir animacoes.
///
/// Tres decisoes que resolvem os problemas classicos de animacao de pixel art:
///
/// 1. GRADE EXPLICITA. Cada folha tem colunas e linhas declaradas na tabela de receitas.
///    O fatiamento automatico da Unity corta justo no desenho, entao cada quadro sai com
///    um tamanho diferente — e o boneco tremelica de quadro em quadro. Recortando por
///    celula, todos os quadros de um clipe tem o mesmo tamanho.
///
/// 2. PIVO NOS PES, CALCULADO. Pra cada clipe o script varre os pixels, acha a linha mais
///    baixa com desenho (os pes) e o centro horizontal da base (o corpo). O pivo vai ali.
///    E por isso que o boneco nao "escorrega" ao trocar de animacao, mesmo com celulas de
///    tamanhos bem diferentes — a folha de ataque tem 160px de largura, a de parado 46.
///
/// 3. CELULA VAZIA E DESCARTADA. As folhas tem a ultima linha incompleta; as celulas sem
///    pixel nenhum viram um piscar de sprite invisivel no meio da animacao.
///
/// Nada disso mexe nos arquivos de arte: o asset guarda textura + retangulo + pivo, e os
/// Sprites sao criados em tempo de execucao. Os cortes que voce ja tem nas folhas
/// continuam intactos.
/// </summary>
public static class ConstrutorDeAnimacoes
{
    private const string PASTA_DA_BIBLIOTECA = "Assets/Resources";
    private const string CAMINHO_DA_BIBLIOTECA = PASTA_DA_BIBLIOTECA + "/BibliotecaDeAnimacoes.asset";

    private const string PLAYER = "Assets/player/";
    private const string BANDIT = "Assets/Bandits - Pixel Art/Sprites/";

    private const float PIXELS_POR_UNIDADE = 100f;
    private const byte LIMITE_DE_ALFA = 8;

    /// <summary>Uma receita: de onde vem o clipe e como ele e lido.</summary>
    private struct Receita
    {
        public string nome;
        public string caminho;
        public int colunas;
        public int linhas;
        public float fps;
        public bool loop;

        /// <summary>Primeiro quadro (contando so as celulas com desenho). 0 = do comeco.</summary>
        public int primeiro;

        /// <summary>Quantos quadros usar. 0 = todos a partir do primeiro.</summary>
        public int quantidade;

        /// <summary>Regiao da textura onde a grade vive. Tudo 0 = a textura inteira.</summary>
        public RectInt regiao;

        public Receita(string nome, string caminho, int colunas, int linhas, float fps, bool loop,
                       int primeiro = 0, int quantidade = 0, RectInt regiao = default)
        {
            this.nome = nome;
            this.caminho = caminho;
            this.colunas = colunas;
            this.linhas = linhas;
            this.fps = fps;
            this.loop = loop;
            this.primeiro = primeiro;
            this.quantidade = quantidade;
            this.regiao = regiao;
        }
    }

    // Regiao da folha dos bandidos: os quadros de 48x48 ocupam 384x240 comecando 16px
    // acima da base da textura (512x256). O resto da imagem e vazio.
    private static readonly RectInt REGIAO_DO_BANDIDO = new RectInt(0, 16, 384, 240);

    /// <summary>
    /// A tabela. Mudar fps, loop ou a faixa de quadros de qualquer animacao e mexer numa
    /// linha daqui e rodar o menu de novo.
    /// </summary>
    private static readonly Receita[] RECEITAS =
    {
        // ---------------------------------------------------------------- parado e andar
        new Receita(NomesDeAnimacao.Parado,       PLAYER + "idle/sprite sheets/idle.png",            10, 1, 10f, true),
        new Receita(NomesDeAnimacao.AndarInicio,  PLAYER + "walk/sprite sheets/from idle.png",        2, 1, 14f, false),
        new Receita(NomesDeAnimacao.Andar,        PLAYER + "walk/sprite sheets/walk.png",             4, 6, 16f, true),
        new Receita(NomesDeAnimacao.CorrerInicio, PLAYER + "run/sprite sheets/run_start.png",         2, 1, 16f, false),
        new Receita(NomesDeAnimacao.Correr,       PLAYER + "run/sprite sheets/run.png",               4, 5, 20f, true),
        new Receita(NomesDeAnimacao.CorrerParar,  PLAYER + "run/sprite sheets/run_stop.png",          4, 4, 22f, false),
        new Receita(NomesDeAnimacao.CorrerVirar,  PLAYER + "run/sprite sheets/run_turn.png",          4, 2, 20f, false),

        // ---------------------------------------------------------------- agachar e escorregar
        new Receita(NomesDeAnimacao.Agachar,      PLAYER + "crouching/sprite sheets/crouching.png",   3, 5, 16f, false),
        new Receita(NomesDeAnimacao.Escorregar,   PLAYER + "slide/sprite sheets/slide_merged.png",    3, 6, 45f, false),

        // ---------------------------------------------------------------- pulo e queda
        // A folha do pulo tem o arco inteiro (subida e queda) em 24 quadros: a primeira
        // metade vira "pular", a segunda "cair".
        new Receita(NomesDeAnimacao.Pular,        PLAYER + "jump/sprite sheets/jump.png",             4, 6, 18f, false, 0, 12),
        new Receita(NomesDeAnimacao.Cair,         PLAYER + "jump/sprite sheets/jump.png",             4, 6, 14f, false, 12, 12),
        new Receita(NomesDeAnimacao.PuloDuplo,       PLAYER + "jump/sprite sheets/double jump_vertical.png", 7, 1, 18f, false),
        new Receita(NomesDeAnimacao.PuloDuploFrente, PLAYER + "jump/sprite sheets/double jump_forward.png",  7, 1, 18f, false),
        new Receita(NomesDeAnimacao.PousarRolando,   PLAYER + "jump/sprite sheets/roll landing.png",         4, 3, 20f, false),

        // ---------------------------------------------------------------- parede
        new Receita(NomesDeAnimacao.ParedeDeslizar, PLAYER + "wall jump/sprite sheets/wall slide.png", 3, 1, 10f, true),
        new Receita(NomesDeAnimacao.ParedePular,    PLAYER + "wall jump/sprite sheets/wall jump.png",  3, 1, 14f, false),

        // ---------------------------------------------------------------- dash e esquiva
        new Receita(NomesDeAnimacao.DashAereo,   PLAYER + "aerial dash/sprite sheets/aerial dash.png", 6, 2, 45f, false),
        new Receita(NomesDeAnimacao.EsquivaTras, PLAYER + "back dodge/sprite sheets/back dodge.png",   4, 6, 40f, false),

        // ---------------------------------------------------------------- beirada
        new Receita(NomesDeAnimacao.BeiradaAgarrar, PLAYER + "ledge action/sprite sheets/hang.png",      4, 2, 16f, false),
        new Receita(NomesDeAnimacao.BeiradaParado,  PLAYER + "ledge action/sprite sheets/hang idle.png", 4, 2,  8f, true),
        new Receita(NomesDeAnimacao.BeiradaSubir,   PLAYER + "ledge action/sprite sheets/up.png",        4, 2, 18f, false),

        // ---------------------------------------------------------------- escada
        new Receita(NomesDeAnimacao.EscadaEntrar,        PLAYER + "ladder action/sprite sheets/01_ladder climb start.png",      3, 1, 12f, false),
        new Receita(NomesDeAnimacao.EscadaSubir,         PLAYER + "ladder action/sprite sheets/02_ladder climb up.png",         3, 2, 12f, true),
        new Receita(NomesDeAnimacao.EscadaSair,          PLAYER + "ladder action/sprite sheets/03_ladder climb end.png",        3, 2, 12f, false),
        new Receita(NomesDeAnimacao.EscadaEscorregar,    PLAYER + "ladder action/sprite sheets/04_ladder slide loop_merged.png", 4, 1, 16f, true),
        new Receita(NomesDeAnimacao.EscadaEscorregarFim, PLAYER + "ladder action/sprite sheets/05_ladder slide end.png",        3, 1, 14f, false),

        // ---------------------------------------------------------------- ataques
        new Receita(NomesDeAnimacao.Ataque1,   PLAYER + "atk/1x atk/sprite sheets/1x atk_merged.png",           4, 5, 30f, false),
        new Receita(NomesDeAnimacao.Ataque2,   PLAYER + "atk/2x atk/sprite sheets/2x atk_merged(short).png",    4, 5, 30f, false),
        new Receita(NomesDeAnimacao.Ataque2a,  PLAYER + "atk/2x-1 atk/sprite sheets/2x-1 atk_merged.png",       3, 2, 18f, false),
        new Receita(NomesDeAnimacao.Ataque2b,  PLAYER + "atk/2x-2 atk/sprite sheets/2x-2 atk_merged.png",       3, 4, 22f, false),
        new Receita(NomesDeAnimacao.Ataque3,   PLAYER + "atk/3x atk/sprite sheets/3x atk_merged.png",           4, 9, 34f, false),
        new Receita(NomesDeAnimacao.AtaqueAr1, PLAYER + "jump attack/jump atk 1x/sprite sheets/jump attack 1x_merged.png", 4, 3, 22f, false),
        new Receita(NomesDeAnimacao.AtaqueAr2, PLAYER + "jump attack/jump atk 2x/sprite sheets/jump atk 2x_merged.png",    4, 2, 20f, false),
        new Receita(NomesDeAnimacao.AtaqueDash1, PLAYER + "dodge atk/dodge atk 1x/sprite sheets/dodge atk 1x_merged.png",  4, 5, 34f, false),
        new Receita(NomesDeAnimacao.AtaqueDash2, PLAYER + "dodge atk/dodge atk 2x/sprite sheets/dodge atk 2x_merged.png",  4, 4, 30f, false),
        new Receita(NomesDeAnimacao.AtaqueMergulho, PLAYER + "dodge atk/dodge atk 3x(vertical atk)/sprite sheets/dodge atk 3x_char.png", 4, 9, 28f, false),
        new Receita(NomesDeAnimacao.MergulhoQueda,  PLAYER + "dodge atk/dodge atk 3x(vertical atk)/sprite sheets/dodge atk 3x_fall loop1.png", 3, 1, 14f, true),

        // ---------------------------------------------------------------- reacoes
        // "dano" e "dano_forte" usam so o comeco da folha: o resto dela e o boneco no chao,
        // que e justamente o que serve de morte.
        new Receita(NomesDeAnimacao.Dano,      PLAYER + "hurt/sprite sheets/normal hit.png", 4, 6, 24f, false, 0, 8),
        new Receita(NomesDeAnimacao.DanoForte, PLAYER + "hurt/sprite sheets/hard hit.png",   6, 6, 22f, false, 0, 12),
        new Receita(NomesDeAnimacao.Morrer,    PLAYER + "hurt/sprite sheets/hard hit.png",   6, 6, 20f, false),
        new Receita(NomesDeAnimacao.Curar,     PLAYER + "healing/sprite sheets/healing_merged.png", 3, 6, 16f, false),

        // ---------------------------------------------------------------- efeitos
        new Receita(NomesDeAnimacao.FxImpacto1, PLAYER + "hit fx/hit fx_01.png", 5, 1, 24f, false),
        new Receita(NomesDeAnimacao.FxImpacto2, PLAYER + "hit fx/hit fx_02.png", 5, 1, 24f, false),

        // ---------------------------------------------------------------- inimigo (bandido)
        new Receita(NomesDeAnimacao.InimigoParado,  BANDIT + "LightBandit.png", 8, 5,  6f, true,   0,  4, REGIAO_DO_BANDIDO),
        new Receita(NomesDeAnimacao.InimigoAlerta,  BANDIT + "LightBandit.png", 8, 5,  8f, true,   4,  4, REGIAO_DO_BANDIDO),
        new Receita(NomesDeAnimacao.InimigoCorrer,  BANDIT + "LightBandit.png", 8, 5, 12f, true,   8,  8, REGIAO_DO_BANDIDO),
        new Receita(NomesDeAnimacao.InimigoAtaque1, BANDIT + "LightBandit.png", 8, 5, 16f, false, 16,  8, REGIAO_DO_BANDIDO),
        new Receita(NomesDeAnimacao.InimigoAtaque2, BANDIT + "LightBandit.png", 8, 5, 16f, false, 24,  8, REGIAO_DO_BANDIDO),
        new Receita(NomesDeAnimacao.InimigoDano,    BANDIT + "LightBandit.png", 8, 5, 10f, false, 32,  2, REGIAO_DO_BANDIDO),
        new Receita(NomesDeAnimacao.InimigoNoAr,    BANDIT + "LightBandit.png", 8, 5,  8f, false, 34,  1, REGIAO_DO_BANDIDO),
        new Receita(NomesDeAnimacao.InimigoMorrer,  BANDIT + "LightBandit.png", 8, 5,  4f, false, 35,  1, REGIAO_DO_BANDIDO),
    };

    // ================================================================ menu
    [MenuItem("Tools/Jogo/Reconstruir animacoes", false, 20)]
    public static void Reconstruir()
    {
        string relatorio = Construir(out int clipes, out int quadros, out List<string> problemas);

        string mensagem = $"{clipes} clipes, {quadros} quadros.\n\n{relatorio}";

        if (problemas.Count > 0)
            mensagem += "\n\nProblemas:\n" + string.Join("\n", problemas);

        EditorUtility.DisplayDialog("Biblioteca de animacoes", mensagem, "Beleza");
    }

    /// <summary>Roda o construtor sem abrir janela. Usado pelo Configurador do Projeto.</summary>
    public static string Construir(out int totalDeClipes, out int totalDeQuadros, out List<string> problemas)
    {
        problemas = new List<string>();
        totalDeClipes = 0;
        totalDeQuadros = 0;

        List<ClipeDeSprites> clipes = new List<ClipeDeSprites>(RECEITAS.Length);
        Dictionary<string, Texture2D> texturasLidas = new Dictionary<string, Texture2D>();
        Dictionary<string, Color32[]> pixelsLidos = new Dictionary<string, Color32[]>();
        Dictionary<string, Vector2Int> tamanhosLidos = new Dictionary<string, Vector2Int>();

        try
        {
            for (int i = 0; i < RECEITAS.Length; i++)
            {
                Receita receita = RECEITAS[i];

                EditorUtility.DisplayProgressBar(
                    "Construindo animacoes",
                    $"{receita.nome} ({i + 1}/{RECEITAS.Length})",
                    (i + 1f) / RECEITAS.Length);

                ClipeDeSprites clipe = MontarClipe(receita, texturasLidas, pixelsLidos, tamanhosLidos, problemas);

                if (clipe == null)
                    continue;

                clipes.Add(clipe);
                totalDeClipes++;
                totalDeQuadros += clipe.Quantidade;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Salvar(clipes);

        return $"Salvo em {CAMINHO_DA_BIBLIOTECA}";
    }

    // ================================================================ montagem de um clipe
    private static ClipeDeSprites MontarClipe(
        Receita receita,
        Dictionary<string, Texture2D> texturas,
        Dictionary<string, Color32[]> pixels,
        Dictionary<string, Vector2Int> tamanhos,
        List<string> problemas)
    {
        if (!texturas.TryGetValue(receita.caminho, out Texture2D textura))
        {
            // Normalizar ANTES de carregar: o SaveAndReimport troca o objeto Texture2D na
            // memoria, e uma referencia pega antes dele viraria uma referencia morta.
            NormalizarImportacao(receita.caminho);

            textura = AssetDatabase.LoadAssetAtPath<Texture2D>(receita.caminho);

            if (textura == null)
            {
                problemas.Add($"{receita.nome}: nao achei {receita.caminho}");
                return null;
            }

            texturas[receita.caminho] = textura;
        }

        if (!pixels.TryGetValue(receita.caminho, out Color32[] cores))
        {
            if (!LerPixelsDoArquivo(receita.caminho, out cores, out Vector2Int tamanho))
            {
                problemas.Add($"{receita.nome}: nao consegui ler os pixels de {receita.caminho}");
                return null;
            }

            pixels[receita.caminho] = cores;
            tamanhos[receita.caminho] = tamanho;
        }

        Vector2Int dim = tamanhos[receita.caminho];

        RectInt regiao = receita.regiao.width > 0 && receita.regiao.height > 0
            ? receita.regiao
            : new RectInt(0, 0, dim.x, dim.y);

        int colunas = Mathf.Max(1, receita.colunas);
        int linhas = Mathf.Max(1, receita.linhas);

        if (regiao.width % colunas != 0 || regiao.height % linhas != 0)
        {
            problemas.Add(
                $"{receita.nome}: a regiao {regiao.width}x{regiao.height} nao divide " +
                $"em {colunas}x{linhas} — confira a grade na tabela de receitas");
        }

        int larguraDaCelula = regiao.width / colunas;
        int alturaDaCelula = regiao.height / linhas;

        if (larguraDaCelula <= 0 || alturaDaCelula <= 0)
        {
            problemas.Add($"{receita.nome}: celula de tamanho zero");
            return null;
        }

        // --- celulas com desenho, em ordem de leitura (linha de cima primeiro)
        List<RectInt> comDesenho = new List<RectInt>(colunas * linhas);

        for (int linha = 0; linha < linhas; linha++)
        {
            for (int coluna = 0; coluna < colunas; coluna++)
            {
                RectInt celula = new RectInt(
                    regiao.x + coluna * larguraDaCelula,
                    regiao.y + regiao.height - (linha + 1) * alturaDaCelula,
                    larguraDaCelula,
                    alturaDaCelula);

                if (TemDesenho(cores, dim, celula))
                    comDesenho.Add(celula);
            }
        }

        if (comDesenho.Count == 0)
        {
            problemas.Add($"{receita.nome}: nenhuma celula com desenho");
            return null;
        }

        // --- faixa pedida pela receita
        int inicio = Mathf.Clamp(receita.primeiro, 0, comDesenho.Count - 1);
        int quantos = receita.quantidade > 0
            ? Mathf.Min(receita.quantidade, comDesenho.Count - inicio)
            : comDesenho.Count - inicio;

        List<RectInt> escolhidas = comDesenho.GetRange(inicio, quantos);

        // --- pivo nos pes, uma vez pro clipe todo
        Vector2 pivo = CalcularPivo(cores, dim, escolhidas);

        Rect[] quadros = new Rect[escolhidas.Count];

        for (int i = 0; i < escolhidas.Count; i++)
        {
            RectInt r = escolhidas[i];
            quadros[i] = new Rect(r.x, r.y, r.width, r.height);
        }

        return new ClipeDeSprites
        {
            nome = receita.nome,
            textura = textura,
            quadros = quadros,
            pivo = pivo,
            pixelsPorUnidade = PIXELS_POR_UNIDADE,
            quadrosPorSegundo = receita.fps,
            emLoop = receita.loop,
            manterUltimoQuadro = true
        };
    }

    // ================================================================ pixels
    /// <summary>
    /// Le os pixels do PNG do disco em vez de pedir pra textura importada. A textura do
    /// projeto costuma vir com "Read/Write" desligado e comprimida — GetPixels nela daria
    /// erro ou devolveria cor errada. O arquivo original sempre da a verdade.
    /// </summary>
    private static bool LerPixelsDoArquivo(string caminho, out Color32[] cores, out Vector2Int tamanho)
    {
        cores = null;
        tamanho = Vector2Int.zero;

        string caminhoCompleto = Path.GetFullPath(caminho);

        if (!File.Exists(caminhoCompleto))
            return false;

        byte[] bytes = File.ReadAllBytes(caminhoCompleto);
        Texture2D temporaria = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        try
        {
            if (!temporaria.LoadImage(bytes))
                return false;

            cores = temporaria.GetPixels32();
            tamanho = new Vector2Int(temporaria.width, temporaria.height);
            return true;
        }
        finally
        {
            Object.DestroyImmediate(temporaria);
        }
    }

    private static bool TemDesenho(Color32[] cores, Vector2Int dim, RectInt celula)
    {
        int xFim = Mathf.Min(celula.xMax, dim.x);
        int yFim = Mathf.Min(celula.yMax, dim.y);

        for (int y = Mathf.Max(0, celula.y); y < yFim; y++)
        {
            int linha = y * dim.x;

            for (int x = Mathf.Max(0, celula.x); x < xFim; x++)
            {
                if (cores[linha + x].a > LIMITE_DE_ALFA)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// O pivo do clipe: X no centro da base do corpo, Y na linha dos pes.
    ///
    /// O Y e a linha mais baixa com desenho entre TODOS os quadros — assim o chao e o
    /// mesmo do primeiro ao ultimo quadro. O X e a media do centro da faixa de baixo de
    /// cada quadro: a faixa de baixo e o pe, e o pe nao anda pela celula (o efeito de
    /// espada, que ocupa metade da folha de ataque, fica de fora dessa media).
    /// </summary>
    private static Vector2 CalcularPivo(Color32[] cores, Vector2Int dim, List<RectInt> celulas)
    {
        int larguraDaCelula = celulas[0].width;
        int alturaDaCelula = celulas[0].height;

        int pesMaisBaixo = alturaDaCelula;     // em coordenadas da celula, 0 = base
        double somaDeCentros = 0;
        int quantosCentros = 0;

        for (int i = 0; i < celulas.Count; i++)
        {
            RectInt celula = celulas[i];

            if (!Extremos(cores, dim, celula, out int baixo, out int alto))
                continue;

            pesMaisBaixo = Mathf.Min(pesMaisBaixo, baixo);

            // Faixa de baixo do conteudo: 20% da altura do desenho, pelo menos 2 pixels.
            int faixa = Mathf.Max(2, (int)((alto - baixo + 1) * 0.2f));
            int limite = Mathf.Min(alto, baixo + faixa - 1);

            long somaX = 0;
            long contagem = 0;

            for (int y = baixo; y <= limite; y++)
            {
                int linhaGlobal = (celula.y + y) * dim.x;

                for (int x = 0; x < larguraDaCelula; x++)
                {
                    if (cores[linhaGlobal + celula.x + x].a <= LIMITE_DE_ALFA)
                        continue;

                    somaX += x;
                    contagem++;
                }
            }

            if (contagem <= 0)
                continue;

            somaDeCentros += (double)somaX / contagem;
            quantosCentros++;
        }

        float centroX = quantosCentros > 0
            ? (float)(somaDeCentros / quantosCentros)
            : larguraDaCelula * 0.5f;

        if (pesMaisBaixo >= alturaDaCelula)
            pesMaisBaixo = 0;

        return new Vector2(
            Mathf.Clamp01((centroX + 0.5f) / larguraDaCelula),
            Mathf.Clamp01(pesMaisBaixo / (float)alturaDaCelula));
    }

    /// <summary>Linha mais baixa e mais alta com desenho dentro da celula (0 = base da celula).</summary>
    private static bool Extremos(Color32[] cores, Vector2Int dim, RectInt celula, out int baixo, out int alto)
    {
        baixo = -1;
        alto = -1;

        int xFim = Mathf.Min(celula.xMax, dim.x);
        int yFim = Mathf.Min(celula.yMax, dim.y);

        for (int y = Mathf.Max(0, celula.y); y < yFim; y++)
        {
            int linha = y * dim.x;
            bool achouNestaLinha = false;

            for (int x = Mathf.Max(0, celula.x); x < xFim; x++)
            {
                if (cores[linha + x].a <= LIMITE_DE_ALFA)
                    continue;

                achouNestaLinha = true;
                break;
            }

            if (!achouNestaLinha)
                continue;

            int relativo = y - celula.y;

            if (baixo < 0)
                baixo = relativo;

            alto = relativo;
        }

        return baixo >= 0;
    }

    // ================================================================ importacao
    /// <summary>
    /// Deixa a textura com cara de pixel art: sem filtro (senao borra), sem compressao
    /// (senao suja as bordas), sem mipmap e com 100 pixels por unidade em todas as folhas.
    /// NAO mexe no modo de fatiamento nem nos cortes que a folha ja tem.
    /// </summary>
    private static void NormalizarImportacao(string caminho)
    {
        TextureImporter importador = AssetImporter.GetAtPath(caminho) as TextureImporter;

        if (importador == null)
            return;

        bool mudou = false;

        if (importador.filterMode != FilterMode.Point) { importador.filterMode = FilterMode.Point; mudou = true; }
        if (importador.textureCompression != TextureImporterCompression.Uncompressed) { importador.textureCompression = TextureImporterCompression.Uncompressed; mudou = true; }
        if (importador.mipmapEnabled) { importador.mipmapEnabled = false; mudou = true; }
        if (!importador.alphaIsTransparency) { importador.alphaIsTransparency = true; mudou = true; }
        if (importador.wrapMode != TextureWrapMode.Clamp) { importador.wrapMode = TextureWrapMode.Clamp; mudou = true; }
        if (!Mathf.Approximately(importador.spritePixelsPerUnit, PIXELS_POR_UNIDADE)) { importador.spritePixelsPerUnit = PIXELS_POR_UNIDADE; mudou = true; }
        if (importador.maxTextureSize < 2048) { importador.maxTextureSize = 2048; mudou = true; }
        if (importador.npotScale != TextureImporterNPOTScale.None) { importador.npotScale = TextureImporterNPOTScale.None; mudou = true; }

        if (!mudou)
            return;

        importador.SaveAndReimport();
    }

    // ================================================================ salvar
    private static void Salvar(List<ClipeDeSprites> clipes)
    {
        // Nenhum clipe lido (pastas de arte renomeadas, por exemplo): melhor manter a
        // biblioteca que existe do que trocar uma boa por uma vazia.
        if (clipes.Count == 0)
        {
            Debug.LogError("[ConstrutorDeAnimacoes] nenhum clipe foi lido — a biblioteca NAO foi alterada. " +
                           "Confira se a pasta Assets/player continua no lugar.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(PASTA_DA_BIBLIOTECA))
            AssetDatabase.CreateFolder("Assets", "Resources");

        BibliotecaDeAnimacoes biblioteca = AssetDatabase.LoadAssetAtPath<BibliotecaDeAnimacoes>(CAMINHO_DA_BIBLIOTECA);

        if (biblioteca == null)
        {
            biblioteca = ScriptableObject.CreateInstance<BibliotecaDeAnimacoes>();
            biblioteca.Substituir(clipes);
            AssetDatabase.CreateAsset(biblioteca, CAMINHO_DA_BIBLIOTECA);
        }
        else
        {
            // Atualiza o asset que ja existe: quem arrastou a biblioteca num Inspector
            // continua apontando pro mesmo objeto.
            biblioteca.Substituir(clipes);
            EditorUtility.SetDirty(biblioteca);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>True se a biblioteca ja existe no disco.</summary>
    public static bool BibliotecaExiste()
    {
        return AssetDatabase.LoadAssetAtPath<BibliotecaDeAnimacoes>(CAMINHO_DA_BIBLIOTECA) != null;
    }
}
#endif
