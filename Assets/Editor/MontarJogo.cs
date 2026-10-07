using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Monta a cena do jogo do zero: o jogador (prefab), a camera, a luz, o gerador do andar (a caverna)
/// e o boneco de treino, mais o que eles usam (as armas, o prefab do Bruxo e as imagens geradas:
/// mira, sombra, bala inimiga e boneco). A arte da caverna vem do pacote Old Prison, ja importada
/// em Assets/Arte/OldPrison (ver Ferramentas/OldPrison). Menu Jogo ▸ Montar a cena do zero.
///
/// Rodar de novo refaz tudo: o que voce mudou a mao na cena e no prefab se perde. Depois de montada,
/// a cena e editada normalmente pelo Inspector; isto aqui so serve pro comeco (ou pra voltar ao
/// comeco).
/// </summary>
public static class MontarJogo
{
    public const string Cena = "Assets/Cenas/Jogo.unity";
    private const string PrefabDoJogador = "Assets/Prefabs/Jogador.prefab";
    private const string PrefabDoBruxo = "Assets/Prefabs/Bruxo.prefab";
    private const string PrefabDoBoneco = "Assets/Prefabs/BonecoDeTreino.prefab";
    private const string ArmaDoArqueiro = "Assets/Dados/Armas/ArcoDoArqueiro.asset";
    private const string ArmaDoBruxo = "Assets/Dados/Armas/MagiaDoBruxo.asset";
    private const string PastaDasArmas = "Assets/Dados/Armas/";
    private const string FolhaDoBau = "Assets/Arte/Resources/Masmorra/Prisao/BauDeMadeira.png";
    private const string DesenhoDaCaixa = "Assets/Arte/Armas/CaixaDeMunicao.png";
    private const string PastaGerada = "Assets/Arte/Gerada";
    private const string Mira = PastaGerada + "/Mira.png";
    private const string Sombra = PastaGerada + "/Sombra.png";
    private const string Bala = PastaGerada + "/BalaInimiga.png";
    private const string Boneco = PastaGerada + "/BonecoDeTreino.png";
    private const string Silhueta = "Assets/Shaders/Silhueta.shader";

    private const string Heroi = "Assets/Arte/Resources/Personagens/Herois/Arqueiro/";
    private const string Bruxo = "Assets/Arte/Resources/Personagens/Bruxo/";
    private const string Flecha = "Assets/Arte/Resources/Personagens/Projeteis/FlechaDoArqueiro.png";
    private const string OldPrison = "Assets/Arte/OldPrison/";
    private const string Vortice = "Assets/Arte/Resources/Masmorra/Prisao/Vortice.png";
    private const string Poeira = "Assets/Arte/Resources/Efeitos/Poeira.png";
    private const string Sons = "Assets/Arte/Resources/Sons/";
    private const string Fontes = "Assets/Arte/Resources/Fontes/";
    private const string Icones = "Assets/Arte/Resources/Icones/";
    private const string Interface = "Assets/Arte/Resources/InterfaceDragao/";

    /// <summary>Todo o desenho do jogo usa 20 pixels por unidade: o boneco (20 px de altura) fica com 1 unidade.</summary>
    public const float PixelsPorUnidade = 20f;

    [MenuItem("Jogo/Montar a cena do zero", false, 1)]
    public static void Montar()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        CriarPasta("Assets/Cenas");
        CriarPasta("Assets/Prefabs");
        CriarPasta("Assets/Dados/Armas");
        CriarPasta(PastaGerada);

        GerarImagens();
        AjustarFlecha();

        Shader silhueta = AssetDatabase.LoadAssetAtPath<Shader>(Silhueta);
        DadosDaArma arco = CriarArco();
        DadosDaArma magia = CriarMagiaDoBruxo();
        GameObject jogador = CriarJogador(arco, silhueta);
        GameObject bruxo = CriarBruxo(magia, silhueta);
        GameObject boneco = CriarBoneco(silhueta);
        CriarCena(jogador, bruxo, boneco);

        AssetDatabase.SaveAssets();
        Debug.Log("[Jogo] cena montada: " + Cena);
    }

    // ---------------- imagens geradas ----------------
    private static void GerarImagens()
    {
        // Mira: quatro tracos brancos com contorno escuro e um ponto no meio (cursor de 32 x 32).
        File.WriteAllBytes(Mira, DesenharMira().EncodeToPNG());
        // Sombra: uma elipse escura e macia debaixo do boneco.
        File.WriteAllBytes(Sombra, DesenharSombra().EncodeToPNG());
        // A bala do inimigo e o boneco de treino: desenhados letra por letra (ver os desenhos la embaixo).
        File.WriteAllBytes(Bala, DesenharPorLetras(DesenhoDaBala, CoresDaBala).EncodeToPNG());
        File.WriteAllBytes(Boneco, DesenharPorLetras(DesenhoDoBoneco, CoresDoBoneco).EncodeToPNG());
        AssetDatabase.Refresh();

        TextureImporter mira = (TextureImporter)AssetImporter.GetAtPath(Mira);
        mira.textureType = TextureImporterType.Cursor;
        mira.filterMode = FilterMode.Point;
        mira.textureCompression = TextureImporterCompression.Uncompressed;
        mira.mipmapEnabled = false;
        mira.SaveAndReimport();

        ImportarSprite(Sombra, SpriteAlignment.Center);
        ImportarSprite(Bala, SpriteAlignment.Center);
        // O boneco balanca em volta do pe: o pivo fica embaixo.
        ImportarSprite(Boneco, SpriteAlignment.BottomCenter);
    }

    private static void ImportarSprite(string caminho, SpriteAlignment alinhamento)
    {
        TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(caminho);
        TextureImporterSettings ajustes = new TextureImporterSettings();
        importador.ReadTextureSettings(ajustes);
        ajustes.spriteAlignment = (int)alinhamento;
        importador.SetTextureSettings(ajustes);

        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.spritePixelsPerUnit = PixelsPorUnidade;
        importador.filterMode = FilterMode.Point;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.mipmapEnabled = false;
        importador.SaveAndReimport();
    }

    private static Texture2D DesenharMira()
    {
        const int lado = 32;
        Color32 branco = new Color32(255, 255, 255, 255);
        Color32 contorno = new Color32(24, 22, 30, 255);
        bool[] cheio = new bool[lado * lado];

        void Marcar(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < lado && y < lado)
                cheio[y * lado + x] = true;
        }

        for (int d = 5; d <= 11; d++)
        {
            for (int e = 0; e < 2; e++)
            {
                Marcar(15 + e, 16 + d);
                Marcar(15 + e, 15 - d);
                Marcar(16 + d, 15 + e);
                Marcar(15 - d, 15 + e);
            }
        }

        Marcar(15, 15);
        Marcar(16, 16);
        Marcar(15, 16);
        Marcar(16, 15);

        Texture2D textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[lado * lado];

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                int i = y * lado + x;

                if (cheio[i])
                {
                    pixels[i] = branco;
                    continue;
                }

                bool vizinho = (x > 0 && cheio[i - 1]) || (x < lado - 1 && cheio[i + 1])
                            || (y > 0 && cheio[i - lado]) || (y < lado - 1 && cheio[i + lado]);
                pixels[i] = vizinho ? contorno : new Color32(0, 0, 0, 0);
            }
        }

        textura.SetPixels32(pixels);
        textura.Apply();
        return textura;
    }

    private static Texture2D DesenharSombra()
    {
        const int largura = 24, altura = 10;
        Texture2D textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[largura * altura];

        for (int y = 0; y < altura; y++)
        {
            for (int x = 0; x < largura; x++)
            {
                float dx = (x + 0.5f - largura * 0.5f) / (largura * 0.5f);
                float dy = (y + 0.5f - altura * 0.5f) / (altura * 0.5f);
                float d = dx * dx + dy * dy;
                byte alfa = d <= 1f ? (byte)(d < 0.55f ? 110 : 70) : (byte)0;
                pixels[y * largura + x] = new Color32(0, 0, 0, alfa);
            }
        }

        textura.SetPixels32(pixels);
        textura.Apply();
        return textura;
    }

    // Cada letra e um pixel (ponto = vazio); a primeira linha e a de cima.
    private static readonly string[] DesenhoDaBala =
    {
        "...oooo...",
        ".ooPPPPoo.",
        ".oPWWPPPo.",
        "oPWWPPPPPo",
        "oPWPPPPPPo",
        "oPPPPPPPdo",
        "oPPPPPPddo",
        ".oPPPPddo.",
        ".ooddddoo.",
        "...oooo...",
    };

    private static readonly Dictionary<char, Color32> CoresDaBala = new Dictionary<char, Color32>
    {
        { 'o', new Color32(70, 12, 34, 255) },
        { 'P', new Color32(255, 76, 116, 255) },
        { 'W', new Color32(255, 220, 228, 255) },
        { 'd', new Color32(200, 38, 84, 255) },
    };

    private static readonly string[] DesenhoDoBoneco =
    {
        "......ooooooo......",
        ".....ossssssSo.....",
        "....osssssssSSo....",
        "....ossessseSSo....",
        "....osssssssSSo....",
        "....ossesesesSo....",
        ".....osssssSSo.....",
        "......oyyyyyo......",
        "oo..osssssssSSo..oo",
        "oyoooosssssssSoooyo",
        "oYwwwwwwwwwwwwwwwYo",
        "oyoWWWWWWWWWWWWWoyo",
        "oo..ossRRRRRsSo..oo",
        "....osRrrrrrRSo....",
        "....osRrRRRrRSo....",
        "....osRrRRRrRSo....",
        "....osRrRRRrRSo....",
        "....osRrrrrrRSo....",
        "....ossRRRRRsSo....",
        "....oyYyyYyyYyo....",
        "....oooowwWoooo....",
        ".......owwWo.......",
        ".......owwWo.......",
        ".......owwWo.......",
        ".....ooowwWooo.....",
        ".....oWWWWWWWo.....",
        ".....ooooooooo.....",
    };

    private static readonly Dictionary<char, Color32> CoresDoBoneco = new Dictionary<char, Color32>
    {
        { 'o', new Color32(40, 24, 28, 255) },    // contorno
        { 's', new Color32(204, 166, 106, 255) }, // saco
        { 'S', new Color32(160, 124, 74, 255) },  // saco na sombra
        { 'e', new Color32(70, 40, 34, 255) },    // costura
        { 'y', new Color32(236, 208, 110, 255) }, // palha
        { 'Y', new Color32(192, 158, 70, 255) },  // palha na sombra
        { 'w', new Color32(132, 82, 46, 255) },   // madeira
        { 'W', new Color32(88, 52, 32, 255) },    // madeira na sombra
        { 'R', new Color32(196, 52, 52, 255) },   // alvo
        { 'r', new Color32(240, 228, 206, 255) }, // alvo, o claro
    };

    private static Texture2D DesenharPorLetras(string[] linhas, Dictionary<char, Color32> cores)
    {
        int largura = linhas[0].Length, altura = linhas.Length;
        Texture2D textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[largura * altura];

        for (int linha = 0; linha < altura; linha++)
        {
            for (int x = 0; x < largura; x++)
            {
                // Na textura a linha 0 e embaixo.
                int y = altura - 1 - linha;
                pixels[y * largura + x] = cores.TryGetValue(linhas[linha][x], out Color32 cor) ? cor : new Color32(0, 0, 0, 0);
            }
        }

        textura.SetPixels32(pixels);
        textura.Apply();
        return textura;
    }

    // A flecha do pacote vem a 100 pixels por unidade; no jogo tudo e 20, pra ela ficar no tamanho do boneco.
    private static void AjustarFlecha()
    {
        TextureImporter flecha = (TextureImporter)AssetImporter.GetAtPath(Flecha);

        if (flecha == null || Mathf.Approximately(flecha.spritePixelsPerUnit, PixelsPorUnidade))
            return;

        flecha.spritePixelsPerUnit = PixelsPorUnidade;
        flecha.SaveAndReimport();
    }

    // ---------------- a arma ----------------
    private static DadosDaArma CriarArco()
    {
        DadosDaArma arco = AssetDatabase.LoadAssetAtPath<DadosDaArma>(ArmaDoArqueiro);

        if (arco == null)
        {
            arco = ScriptableObject.CreateInstance<DadosDaArma>();
            AssetDatabase.CreateAsset(arco, ArmaDoArqueiro);
        }

        arco.nome = "Arco do Arqueiro";
        arco.desenhoDoTiro = AssetDatabase.LoadAssetAtPath<Sprite>(Flecha);
        // Na mao o arco ja esta no desenho do Arqueiro; largado no chao, aparece este.
        arco.desenhoNoChao = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Armas/Arco.png");
        arco.apontarODesenho = true;
        arco.cor = Color.white;
        arco.raio = 0.12f;
        arco.automatica = true;
        arco.tirosPorSegundo = 6f;
        arco.velocidade = 15f;
        arco.alcance = 11f;
        arco.dano = 3f;
        arco.empurrao = 3f;
        arco.tirosPorDisparo = 1;
        arco.abertura = 10f;
        arco.dispersao = 2.5f;
        arco.tremor = 0.02f;
        arco.som = Som("Tiro.ogg");
        arco.volume = 0.4f;
        // A arma do personagem: nunca acaba e nunca sai do bau.
        arco.infinita = true;
        arco.pesoNoBau = 0f;
        EditorUtility.SetDirty(arco);
        return arco;
    }

    // A bala do Bruxo: lenta, redonda e bem visivel, pra dar tempo de desviar andando.
    private static DadosDaArma CriarMagiaDoBruxo()
    {
        DadosDaArma magia = AssetDatabase.LoadAssetAtPath<DadosDaArma>(ArmaDoBruxo);

        if (magia == null)
        {
            magia = ScriptableObject.CreateInstance<DadosDaArma>();
            AssetDatabase.CreateAsset(magia, ArmaDoBruxo);
        }

        magia.nome = "Magia do Bruxo";
        magia.desenhoDoTiro = AssetDatabase.LoadAssetAtPath<Sprite>(Bala);
        magia.apontarODesenho = false;
        magia.cor = Color.white;
        magia.raio = 0.15f;
        magia.automatica = true;
        magia.tirosPorSegundo = 1f;
        magia.velocidade = 4.5f;
        magia.alcance = 14f;
        magia.dano = 1f;
        magia.empurrao = 5f;
        magia.tirosPorDisparo = 1;
        magia.abertura = 10f;
        magia.dispersao = 0f;
        magia.tremor = 0f;
        magia.som = Som("TiroInimigo.ogg");
        magia.volume = 0.35f;
        magia.infinita = true;
        magia.pesoNoBau = 0f;
        EditorUtility.SetDirty(magia);
        return magia;
    }

    // ---------------- o jogador ----------------
    private static GameObject CriarJogador(DadosDaArma arco, Shader silhueta)
    {
        GameObject raiz = new GameObject("Jogador");
        raiz.tag = "Player";

        Rigidbody2D corpo = raiz.AddComponent<Rigidbody2D>();
        corpo.gravityScale = 0f;
        corpo.freezeRotation = true;

        CircleCollider2D colisor = raiz.AddComponent<CircleCollider2D>();
        colisor.radius = 0.3f;
        colisor.offset = new Vector2(0f, -0.2f);

        AudioSource audioSource = raiz.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        raiz.AddComponent<ControlesDoJogador>();
        raiz.AddComponent<MovimentoDoJogador>();

        // A sombra no chao e o corpo (filhos: o corpo gira na esquiva sem girar o colisor).
        SpriteRenderer desenhoDoCorpo = CriarSombraECorpo(raiz, -0.5f, 0f);

        ArmaDoJogador arma = raiz.AddComponent<ArmaDoJogador>();
        Preencher(arma, "arma", arco);
        Preencher(arma, "somSemMunicao", Som("Negado.ogg"));
        Preencher(arma, "somDaTroca", Som("Chave.ogg"));

        AnimacaoDoJogador animacao = raiz.AddComponent<AnimacaoDoJogador>();
        Preencher(animacao, "corpo", desenhoDoCorpo);
        Preencher(animacao, "parado", AssetDatabase.LoadAssetAtPath<Texture2D>(Heroi + "Idle.png"));
        Preencher(animacao, "andando", AssetDatabase.LoadAssetAtPath<Texture2D>(Heroi + "Walk.png"));
        Preencher(animacao, "ataque", AssetDatabase.LoadAssetAtPath<Texture2D>(Heroi + "Attack01.png"));
        Preencher(animacao, "morte", AssetDatabase.LoadAssetAtPath<Texture2D>(Heroi + "Death.png"));

        raiz.AddComponent<RastroDaEsquiva>();

        CursorDaMira cursor = raiz.AddComponent<CursorDaMira>();
        Preencher(cursor, "mira", AssetDatabase.LoadAssetAtPath<Texture2D>(Mira));

        // Vida: 6 pontos, e depois de um golpe 1 segundo sem tomar outro (piscando).
        Vida vida = raiz.AddComponent<Vida>();
        AjustarEnum(vida, "lado", (int)Lado.Jogador);
        Ajustar(vida, "vidaMaxima", 6f);
        Ajustar(vida, "tempoSemDano", 1f);
        Preencher(vida, "somDoDano", Som("DanoJogador.ogg"));
        Preencher(vida, "somDaMorte", Som("MorteJogador.ogg"));
        Ajustar(vida, "volume", 0.7f);
        Ajustar(vida, "tremorNoDano", 0.15f);
        Ajustar(vida, "tremorNaMorte", 0.3f);

        PiscarAoTomarDano piscar = raiz.AddComponent<PiscarAoTomarDano>();
        PreencherLista(piscar, "desenhos", desenhoDoCorpo);
        Preencher(piscar, "silhueta", silhueta);
        Ajustar(piscar, "piscarNoTempoSemDano", true);

        raiz.AddComponent<MorteDoJogador>();

        // A arma achada na mao e o "E" pra pegar/abrir.
        raiz.AddComponent<ArmaNaMao>();
        raiz.AddComponent<InteracaoDoJogador>();

        // O desenho do corpo e cortado da folha ao dar Play: na cena parada so aparece a sombra.
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, PrefabDoJogador);
        Object.DestroyImmediate(raiz);
        return prefab;
    }

    // ---------------- o primeiro inimigo ----------------
    private static GameObject CriarBruxo(DadosDaArma magia, Shader silhueta)
    {
        GameObject raiz = new GameObject("Bruxo");

        Rigidbody2D corpo = raiz.AddComponent<Rigidbody2D>();
        corpo.gravityScale = 0f;
        corpo.freezeRotation = true;

        // Um pouco maior que o do jogador: o chapeu e o manto tambem levam flechada.
        CircleCollider2D colisor = raiz.AddComponent<CircleCollider2D>();
        colisor.radius = 0.4f;
        colisor.offset = new Vector2(0f, -0.1f);

        AudioSource audioSource = raiz.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        SpriteRenderer desenhoDoCorpo = CriarSombraECorpo(raiz, -0.5f, 0f);

        // 15 de vida: 5 flechas do arco.
        Vida vida = raiz.AddComponent<Vida>();
        AjustarEnum(vida, "lado", (int)Lado.Inimigos);
        Ajustar(vida, "vidaMaxima", 15f);
        Preencher(vida, "somDoDano", Som("Acerto.ogg"));
        Preencher(vida, "somDaMorte", Som("MorteInimigo.ogg"));
        Ajustar(vida, "volume", 0.45f);
        Ajustar(vida, "tremorNaMorte", 0.06f);

        InimigoAtirador atirador = raiz.AddComponent<InimigoAtirador>();
        Preencher(atirador, "arma", magia);

        AnimacaoDoInimigo animacao = raiz.AddComponent<AnimacaoDoInimigo>();
        Preencher(animacao, "corpo", desenhoDoCorpo);
        Preencher(animacao, "parado", AssetDatabase.LoadAssetAtPath<Texture2D>(Bruxo + "Idle.png"));
        Preencher(animacao, "andando", AssetDatabase.LoadAssetAtPath<Texture2D>(Bruxo + "Walk.png"));
        Preencher(animacao, "ataque", AssetDatabase.LoadAssetAtPath<Texture2D>(Bruxo + "Attack01.png"));
        Preencher(animacao, "morte", AssetDatabase.LoadAssetAtPath<Texture2D>(Bruxo + "Death.png"));

        PiscarAoTomarDano piscar = raiz.AddComponent<PiscarAoTomarDano>();
        PreencherLista(piscar, "desenhos", desenhoDoCorpo);
        Preencher(piscar, "silhueta", silhueta);

        MorteDoInimigo morte = raiz.AddComponent<MorteDoInimigo>();
        Preencher(morte, "efeito", AssetDatabase.LoadAssetAtPath<Texture2D>(Poeira));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, PrefabDoBruxo);
        Object.DestroyImmediate(raiz);
        return prefab;
    }

    // ---------------- o boneco de treino ----------------
    private static GameObject CriarBoneco(Shader silhueta)
    {
        GameObject raiz = new GameObject("Boneco de treino");

        // Dynamic com tudo travado: nao sai do lugar, ninguem passa por dentro, e o tiro (cinematico) sente ele.
        Rigidbody2D corpo = raiz.AddComponent<Rigidbody2D>();
        corpo.gravityScale = 0f;
        corpo.constraints = RigidbodyConstraints2D.FreezeAll;

        CircleCollider2D colisor = raiz.AddComponent<CircleCollider2D>();
        colisor.radius = 0.35f;

        AudioSource audioSource = raiz.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // O desenho tem o pivo no pe: o corpo fica embaixo, pra balancar em volta da base.
        SpriteRenderer desenhoDoCorpo = CriarSombraECorpo(raiz, -0.6f, -0.6f);
        desenhoDoCorpo.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Boneco);

        Vida vida = raiz.AddComponent<Vida>();
        AjustarEnum(vida, "lado", (int)Lado.Inimigos);
        Ajustar(vida, "vidaMaxima", 100f);
        Ajustar(vida, "imortal", true);
        Ajustar(vida, "pesoDoEmpurrao", 0f);
        Preencher(vida, "somDoDano", Som("Acerto.ogg"));
        Ajustar(vida, "volume", 0.3f);

        PiscarAoTomarDano piscar = raiz.AddComponent<PiscarAoTomarDano>();
        PreencherLista(piscar, "desenhos", desenhoDoCorpo);
        Preencher(piscar, "silhueta", silhueta);

        BonecoDeTreino boneco = raiz.AddComponent<BonecoDeTreino>();
        Preencher(boneco, "corpo", desenhoDoCorpo.transform);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, PrefabDoBoneco);
        Object.DestroyImmediate(raiz);
        return prefab;
    }

    // A sombra no chao e o corpo desenhado, filhos da raiz. Devolve o desenho do corpo.
    private static SpriteRenderer CriarSombraECorpo(GameObject raiz, float alturaDaSombra, float alturaDoCorpo)
    {
        GameObject sombra = new GameObject("Sombra");
        sombra.transform.SetParent(raiz.transform, false);
        sombra.transform.localPosition = new Vector3(0f, alturaDaSombra, 0f);
        SpriteRenderer desenhoDaSombra = sombra.AddComponent<SpriteRenderer>();
        desenhoDaSombra.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Sombra);
        desenhoDaSombra.sortingOrder = 1;

        GameObject corpo = new GameObject("Corpo");
        corpo.transform.SetParent(raiz.transform, false);
        corpo.transform.localPosition = new Vector3(0f, alturaDoCorpo, 0f);
        SpriteRenderer desenhoDoCorpo = corpo.AddComponent<SpriteRenderer>();
        desenhoDoCorpo.sortingOrder = 10;
        return desenhoDoCorpo;
    }

    // ---------------- a cena ----------------
    private static void CriarCena(GameObject prefabDoJogador, GameObject prefabDoBruxo, GameObject prefabDoBoneco)
    {
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject jogador = (GameObject)PrefabUtility.InstantiatePrefab(prefabDoJogador);
        jogador.transform.position = Vector3.zero;

        GameObject objCamera = new GameObject("Main Camera");
        objCamera.tag = "MainCamera";
        objCamera.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = objCamera.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        // Da cor do topo das paredes do Old Prison: alem da beirada da caverna, a parede continua no escuro.
        cam.backgroundColor = new Color32(17, 25, 42, 255);
        objCamera.AddComponent<AudioListener>();
        CameraDoJogo cameraDoJogo = objCamera.AddComponent<CameraDoJogo>();
        Preencher(cameraDoJogo, "alvo", jogador.transform);

        // Luz global 2D: sem ela o URP 2D deixa os sprites escuros.
        Light2D luz = new GameObject("Luz global").AddComponent<Light2D>();
        luz.lightType = Light2D.LightType.Global;
        luz.intensity = 1f;

        // O andar: cava a caverna ao dar Play, com o comeco (a clareira) no centro do mundo.
        GameObject andar = new GameObject("Andar");
        AudioSource somDoAndar = andar.AddComponent<AudioSource>();
        somDoAndar.playOnAwake = false;
        somDoAndar.spatialBlend = 0f;
        GeradorDoAndar gerador = andar.AddComponent<GeradorDoAndar>();
        // O Bruxo e feito aqui; os outros inimigos (e as armas deles) ja vem prontos no projeto.
        var inimigos = new (GameObject prefab, int andar, float peso)[]
        {
            (prefabDoBruxo, 1, 3f),
            (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Esqueleto.prefab"), 1, 3f),
            (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gosma.prefab"), 1, 2f),
            (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EsqueletoArqueiro.prefab"), 1, 2f),
            (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Necromante.prefab"), 2, 1.5f),
            (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Olho.prefab"), 4, 1.5f),
        };
        Mexer(gerador, "inimigos", p =>
        {
            p.arraySize = inimigos.Length;

            for (int i = 0; i < inimigos.Length; i++)
            {
                SerializedProperty item = p.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("prefab").objectReferenceValue = inimigos[i].prefab;
                item.FindPropertyRelative("primeiroAndar").intValue = inimigos[i].andar;
                item.FindPropertyRelative("peso").floatValue = inimigos[i].peso;
            }
        });

        // A ordem dos andares: duas cavernas e o Minotauro, duas cavernas e o Golem.
        GameObject minotauro = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minotauro.prefab");
        GameObject golem = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Golem.prefab");
        var andares = new (string nome, GameObject chefe)[]
        {
            ("", null), ("", null), ("Covil do Minotauro", minotauro),
            ("", null), ("", null), ("Coracao da Prisao", golem),
        };
        Mexer(gerador, "andares", p =>
        {
            p.arraySize = andares.Length;

            for (int i = 0; i < andares.Length; i++)
            {
                SerializedProperty item = p.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("nome").stringValue = andares[i].nome;
                item.FindPropertyRelative("chefe").objectReferenceValue = andares[i].chefe;
            }
        });
        foreach (string folha in new[] { "Chao", "Paredes", "Abismo", "Sangue", "Enfeites" })
            Preencher(gerador, folha.ToLowerInvariant(), AssetDatabase.LoadAssetAtPath<Texture2D>(OldPrison + folha + ".png"));
        Preencher(gerador, "vortice", AssetDatabase.LoadAssetAtPath<Texture2D>(Vortice));
        Preencher(gerador, "portalAbrindo", Som("Segredo.ogg"));

        // As armas que caem dos baus e do chao (feitas por Ferramentas/Armas; os .asset ja vem no projeto).
        PreencherLista(gerador, "armas", System.Array.ConvertAll(
            new[] { "Varinha", "Tomo", "BestaDeRepeticao", "Cajado", "Machado" },
            nome => (Object)AssetDatabase.LoadAssetAtPath<DadosDaArma>(PastaDasArmas + nome + ".asset")));
        Preencher(gerador, "bau", AssetDatabase.LoadAssetAtPath<Texture2D>(FolhaDoBau));
        Preencher(gerador, "caixaDeMunicao", AssetDatabase.LoadAssetAtPath<Sprite>(DesenhoDaCaixa));
        Preencher(gerador, "somDoBau", Som("BauAbre.ogg"));
        Preencher(gerador, "somDaMunicao", Som("Item.ogg"));

        // A interface do jogo (o HUD). Os menus do jogo antigo se montam sozinhos ao dar Play.
        GameObject objInterface = new GameObject("Interface");
        TelaDoJogo tela = objInterface.AddComponent<TelaDoJogo>();
        tela.fonteTexto = AssetDatabase.LoadAssetAtPath<Font>(Fontes + "Jersey15.ttf");
        tela.fonteTitulo = AssetDatabase.LoadAssetAtPath<Font>(Fontes + "Jacquard12.ttf");
        tela.coracao = AssetDatabase.LoadAssetAtPath<Texture2D>(Icones + "fb659.png");
        tela.bolsa = AssetDatabase.LoadAssetAtPath<Texture2D>(Icones + "fb158.png");
        tela.molduraPequena = AssetDatabase.LoadAssetAtPath<Texture2D>(Interface + "MolduraPequena.png");
        tela.barraDourada = AssetDatabase.LoadAssetAtPath<Texture2D>(Interface + "BarraDourada.png");

        // O boneco de treino na clareira do comeco.
        GameObject boneco = (GameObject)PrefabUtility.InstantiatePrefab(prefabDoBoneco);
        boneco.transform.position = new Vector3(3f, 1.5f, 0f);

        EditorSceneManager.SaveScene(cena, Cena);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Cena, true) };
        PlayPelaCenaDoJogo.Aplicar();
    }

    // ---------------- ajudas ----------------
    private static AudioClip Som(string arquivo) => AssetDatabase.LoadAssetAtPath<AudioClip>(Sons + arquivo);

    private static void Preencher(Object componente, string campo, Object valor) =>
        Mexer(componente, campo, p => p.objectReferenceValue = valor);

    private static void PreencherLista(Object componente, string campo, params Object[] valores) =>
        Mexer(componente, campo, p =>
        {
            p.arraySize = valores.Length;

            for (int i = 0; i < valores.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
        });

    private static void Ajustar(Object componente, string campo, float valor) =>
        Mexer(componente, campo, p => p.floatValue = valor);

    private static void Ajustar(Object componente, string campo, bool valor) =>
        Mexer(componente, campo, p => p.boolValue = valor);

    private static void AjustarEnum(Object componente, string campo, int indice) =>
        Mexer(componente, campo, p => p.enumValueIndex = indice);

    // Mexe num campo do componente como o Inspector mexe (vale pros campos privados com SerializeField).
    private static void Mexer(Object componente, string campo, System.Action<SerializedProperty> mexer)
    {
        SerializedObject so = new SerializedObject(componente);
        SerializedProperty propriedade = so.FindProperty(campo);

        if (propriedade == null)
        {
            Debug.LogError($"[Jogo] {componente.GetType().Name} nao tem o campo '{campo}'");
            return;
        }

        mexer(propriedade);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CriarPasta(string caminho)
    {
        if (AssetDatabase.IsValidFolder(caminho))
            return;

        string pai = Path.GetDirectoryName(caminho).Replace('\\', '/');
        CriarPasta(pai);
        AssetDatabase.CreateFolder(pai, Path.GetFileName(caminho));
    }
}
