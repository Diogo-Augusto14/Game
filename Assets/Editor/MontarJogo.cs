using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Monta a cena do jogo do zero: o jogador (prefab), a camera, a luz e o chao infinito, mais o que
/// eles usam (a arma do arqueiro, a imagem da mira e a da sombra). Menu Jogo ▸ Montar a cena do zero.
///
/// Rodar de novo refaz tudo: o que voce mudou a mao na cena e no prefab se perde. Depois de montada,
/// a cena e editada normalmente pelo Inspector; isto aqui so serve pro comeco (ou pra voltar ao
/// comeco).
/// </summary>
public static class MontarJogo
{
    public const string Cena = "Assets/Cenas/Jogo.unity";
    private const string PrefabDoJogador = "Assets/Prefabs/Jogador.prefab";
    private const string ArmaDoArqueiro = "Assets/Dados/Armas/ArcoDoArqueiro.asset";
    private const string PastaGerada = "Assets/Arte/Gerada";
    private const string Mira = PastaGerada + "/Mira.png";
    private const string Sombra = PastaGerada + "/Sombra.png";

    private const string Heroi = "Assets/Arte/Resources/Personagens/Herois/Arqueiro/";
    private const string Flecha = "Assets/Arte/Resources/Personagens/Projeteis/FlechaDoArqueiro.png";
    private const string Chao = "Assets/Arte/Resources/Masmorra/Temas/PrisaoChaoPorao.png";
    private const string SomDoTiro = "Assets/Arte/Resources/Sons/Tiro.ogg";

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

        DadosDaArma arco = CriarArco();
        GameObject jogador = CriarJogador(arco);
        CriarCena(jogador);

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
        AssetDatabase.Refresh();

        TextureImporter mira = (TextureImporter)AssetImporter.GetAtPath(Mira);
        mira.textureType = TextureImporterType.Cursor;
        mira.filterMode = FilterMode.Point;
        mira.textureCompression = TextureImporterCompression.Uncompressed;
        mira.mipmapEnabled = false;
        mira.SaveAndReimport();

        TextureImporter sombra = (TextureImporter)AssetImporter.GetAtPath(Sombra);
        sombra.textureType = TextureImporterType.Sprite;
        sombra.spriteImportMode = SpriteImportMode.Single;
        sombra.spritePixelsPerUnit = PixelsPorUnidade;
        sombra.filterMode = FilterMode.Point;
        sombra.textureCompression = TextureImporterCompression.Uncompressed;
        sombra.mipmapEnabled = false;
        sombra.SaveAndReimport();
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
        arco.apontarODesenho = true;
        arco.cor = Color.white;
        arco.raio = 0.12f;
        arco.automatica = true;
        arco.tirosPorSegundo = 6f;
        arco.velocidade = 15f;
        arco.alcance = 11f;
        arco.dano = 3f;
        arco.tirosPorDisparo = 1;
        arco.abertura = 10f;
        arco.dispersao = 2.5f;
        arco.tremor = 0.02f;
        arco.som = AssetDatabase.LoadAssetAtPath<AudioClip>(SomDoTiro);
        arco.volume = 0.4f;
        EditorUtility.SetDirty(arco);
        return arco;
    }

    // ---------------- o jogador ----------------
    private static GameObject CriarJogador(DadosDaArma arco)
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
        GameObject sombra = new GameObject("Sombra");
        sombra.transform.SetParent(raiz.transform, false);
        sombra.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        SpriteRenderer desenhoDaSombra = sombra.AddComponent<SpriteRenderer>();
        desenhoDaSombra.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Sombra);
        desenhoDaSombra.sortingOrder = 1;

        GameObject corpoDesenhado = new GameObject("Corpo");
        corpoDesenhado.transform.SetParent(raiz.transform, false);
        SpriteRenderer desenhoDoCorpo = corpoDesenhado.AddComponent<SpriteRenderer>();
        desenhoDoCorpo.sortingOrder = 10;

        ArmaDoJogador arma = raiz.AddComponent<ArmaDoJogador>();
        Preencher(arma, "arma", arco);

        AnimacaoDoJogador animacao = raiz.AddComponent<AnimacaoDoJogador>();
        Preencher(animacao, "corpo", desenhoDoCorpo);
        Preencher(animacao, "parado", AssetDatabase.LoadAssetAtPath<Texture2D>(Heroi + "Idle.png"));
        Preencher(animacao, "andando", AssetDatabase.LoadAssetAtPath<Texture2D>(Heroi + "Walk.png"));
        Preencher(animacao, "ataque", AssetDatabase.LoadAssetAtPath<Texture2D>(Heroi + "Attack01.png"));

        raiz.AddComponent<RastroDaEsquiva>();

        CursorDaMira cursor = raiz.AddComponent<CursorDaMira>();
        Preencher(cursor, "mira", AssetDatabase.LoadAssetAtPath<Texture2D>(Mira));

        // O desenho do corpo e cortado da folha ao dar Play: na cena parada so aparece a sombra.
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, PrefabDoJogador);
        Object.DestroyImmediate(raiz);
        return prefab;
    }

    // ---------------- a cena ----------------
    private static void CriarCena(GameObject prefabDoJogador)
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
        cam.backgroundColor = new Color(0.05f, 0.04f, 0.06f);
        objCamera.AddComponent<AudioListener>();
        CameraDoJogo cameraDoJogo = objCamera.AddComponent<CameraDoJogo>();
        Preencher(cameraDoJogo, "alvo", jogador.transform);

        // Luz global 2D: sem ela o URP 2D deixa os sprites escuros.
        Light2D luz = new GameObject("Luz global").AddComponent<Light2D>();
        luz.lightType = Light2D.LightType.Global;
        luz.intensity = 1f;

        GameObject chao = new GameObject("Chao infinito");
        chao.AddComponent<SpriteRenderer>();
        ChaoInfinito chaoInfinito = chao.AddComponent<ChaoInfinito>();
        Preencher(chaoInfinito, "ladrilho", AssetDatabase.LoadAssetAtPath<Texture2D>(Chao));

        EditorSceneManager.SaveScene(cena, Cena);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Cena, true) };
        PlayPelaCenaDoJogo.Aplicar();
    }

    // ---------------- ajudas ----------------
    private static void Preencher(Object componente, string campo, Object valor)
    {
        SerializedObject so = new SerializedObject(componente);
        SerializedProperty propriedade = so.FindProperty(campo);

        if (propriedade == null)
        {
            Debug.LogError($"[Jogo] {componente.GetType().Name} nao tem o campo '{campo}'");
            return;
        }

        propriedade.objectReferenceValue = valor;
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
