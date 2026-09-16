#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Monta na CENA (fora do Play) o boneco, o inimigo e as pecas de cenario, usando a mesma
/// receita que o Bootstrap usa no Play (<see cref="Construtor"/>). Os objetos ficam de
/// verdade na cena: da pra mover, esticar, duplicar e salvar.
///
/// Menu: Tools ▸ Jogo ▸ Montar na cena.
///
/// Serve pra sair do automatico: voce roda uma vez, ganha Jogador e Inimigo prontos (e
/// salvos como prefab em Assets/Prefabs), mais uma paleta de blocos pra clonar e esticar.
/// O Bootstrap e desligado na hora, e a partir dai a cena e sua.
///
/// O detalhe que faz isso funcionar: fora do Play, <c>AddComponent</c> NAO chama Awake.
/// Entao tudo que um componente normalmente cria pra si mesmo no Awake (filhos de sensor,
/// hitbox) precisa ser criado aqui, explicitamente, senao o prefab sai pela metade. E a
/// razao de a receita criar esses filhos ela mesma em vez de deixar pro Awake.
/// </summary>
public static class MontadorDeCena
{
    private const string PASTA_PREFABS = "Assets/Prefabs";
    private const string PASTA_SPRITES = "Assets/Sprites";
    private const string CAMINHO_DO_BLOCO = PASTA_SPRITES + "/bloco.png";

    private const string FOLHA_DO_JOGADOR = "Assets/player/idle/sprite sheets/idle.png";
    private const string FOLHA_DO_INIMIGO = "Assets/Bandits - Pixel Art/Sprites/LightBandit.png";

    private const int LADO_DO_BLOCO = 16;

    /// <summary>Uma peca da paleta: nome, tamanho, cor e papel.</summary>
    private struct Peca
    {
        public string nome;
        public float largura;
        public float altura;
        public Color cor;
        public Construtor.TipoDeBloco tipo;
        public string paraQueServe;

        public Peca(string nome, float largura, float altura, Color cor,
                    Construtor.TipoDeBloco tipo, string paraQueServe)
        {
            this.nome = nome;
            this.largura = largura;
            this.altura = altura;
            this.cor = cor;
            this.tipo = tipo;
            this.paraQueServe = paraQueServe;
        }
    }

    /// <summary>
    /// A paleta, na ordem em que aparece na cena: um de cada tipo, lado a lado, apoiados
    /// na mesma linha de base. A ideia e duplicar (Ctrl+D) e esticar, nao montar de novo.
    /// </summary>
    private static readonly Peca[] PALETA =
    {
        new Peca("Chao",       4.0f, 0.60f, Construtor.COR_CHAO,       Construtor.TipoDeBloco.Chao,
                 "chao normal — pisa em cima"),
        new Peca("Plataforma", 1.6f, 0.25f, Construtor.COR_PLATAFORMA, Construtor.TipoDeBloco.Chao,
                 "plataforma fina — mesma coisa, so mais magra"),
        new Peca("Tunel",      1.6f, 0.40f, Construtor.COR_TUNEL,      Construtor.TipoDeBloco.Chao,
                 "teto baixo — deixe ~0.35 de vao pra so passar escorregando"),
        new Peca("Parede",     0.3f, 3.0f,  Construtor.COR_PAREDE,     Construtor.TipoDeBloco.Parede,
                 "parede — deslizar e wall jump (camada de parede)"),
        new Peca("Beirada",    1.2f, 2.0f,  Construtor.COR_BEIRADA,    Construtor.TipoDeBloco.Parede,
                 "bloco alto — pendurar na quina de cima e subir"),
        new Peca("Escada",     0.4f, 2.4f,  Construtor.COR_ESCADA,     Construtor.TipoDeBloco.Escada,
                 "escada — trigger, sobe com Cima"),
    };

    private const float ESPACO_ENTRE_PECAS = 0.6f;

    // ================================================================ menus
    [MenuItem("Tools/Jogo/Montar na cena/Kit completo (jogador, inimigo e cenario)", false, 0)]
    private static void KitCompleto()
    {
        int grupo = ComecarGrupo("Montar kit na cena");

        Vector3 origem = OndeACenaEstaOlhando();
        Sprite spriteDoBloco = GarantirSpriteDoBloco();

        List<GameObject> raizes = MontarPaleta(origem, spriteDoBloco, out float topoDoChao, out float centroDoChao);

        GameObject jogador = MontarJogador(new Vector3(centroDoChao - 1.1f, topoDoChao, 0f));
        GameObject inimigo = MontarInimigo(new Vector3(centroDoChao + 0.9f, topoDoChao, 0f));

        raizes.Add(jogador);
        raizes.Add(inimigo);

        bool salvouPrefabs = SalvarPrefabs(jogador, inimigo, raizes);

        AjustarCamera(jogador);
        GarantirHud();
        DesligarBootstrap();

        Finalizar(grupo, raizes, jogador);

        string legenda = "";

        foreach (Peca p in PALETA)
            legenda += $"\n  {p.nome} — {p.paraQueServe}";

        Debug.Log("[MontadorDeCena] paleta de cenario:" + legenda);

        EditorUtility.DisplayDialog(
            "Kit montado na cena",
            "Na cena agora: Jogador, Inimigo e a paleta de blocos (um de cada tipo, lado a lado).\n\n" +
            "Pra construir a fase: selecione um bloco, Ctrl+D pra duplicar, e estique com a " +
            "ferramenta de retangulo do SpriteRenderer. A colisao acompanha o desenho sozinha.\n\n" +
            "A paleta:" + legenda + "\n\n" +
            (salvouPrefabs
                ? $"Prefabs salvos em {PASTA_PREFABS}."
                : "Prefabs NAO foram salvos (voce escolheu nao sobrescrever).") +
            "\n\nO Bootstrap foi DESLIGADO nesta cena. A partir daqui o Play usa o que esta na cena.",
            "Beleza");
    }

    [MenuItem("Tools/Jogo/Montar na cena/So o jogador", false, 20)]
    private static void SoOJogador()
    {
        int grupo = ComecarGrupo("Montar jogador na cena");

        GameObject jogador = MontarJogador(OndeACenaEstaOlhando());

        Finalizar(grupo, new List<GameObject> { jogador }, jogador);
        Debug.Log("[MontadorDeCena] jogador montado. O Bootstrap continua ligado — desligue o " +
                  "\"Montar Jogador\" dele (ou a chave Ativo) pra ele nao criar outro no Play.");
    }

    [MenuItem("Tools/Jogo/Montar na cena/So o inimigo", false, 21)]
    private static void SoOInimigo()
    {
        int grupo = ComecarGrupo("Montar inimigo na cena");

        GameObject inimigo = MontarInimigo(OndeACenaEstaOlhando());

        Finalizar(grupo, new List<GameObject> { inimigo }, inimigo);
        Debug.Log("[MontadorDeCena] inimigo montado.");
    }

    [MenuItem("Tools/Jogo/Montar na cena/So as pecas de cenario", false, 22)]
    private static void SoAsPecas()
    {
        int grupo = ComecarGrupo("Montar pecas de cenario");

        List<GameObject> raizes = MontarPaleta(
            OndeACenaEstaOlhando(), GarantirSpriteDoBloco(), out _, out _);

        Finalizar(grupo, raizes, raizes.Count > 0 ? raizes[0] : null);
        Debug.Log("[MontadorDeCena] paleta de cenario montada. Duplique e estique.");
    }

    [MenuItem("Tools/Jogo/Montar na cena/Desligar o Bootstrap desta cena", false, 40)]
    private static void MenuDesligarBootstrap()
    {
        int grupo = ComecarGrupo("Desligar o Bootstrap");

        Bootstrap bootstrap = DesligarBootstrap();

        Undo.CollapseUndoOperations(grupo);
        MarcarCenaSuja();

        Selection.activeGameObject = bootstrap != null ? bootstrap.gameObject : null;

        EditorUtility.DisplayDialog(
            "Bootstrap desligado",
            "Existe um objeto \"Bootstrap\" na cena com a chave Ativo DESMARCADA.\n\n" +
            "Ele nao monta mais nada, e a presenca dele impede o automatico de instalar " +
            "outro. Nao apague esse objeto: se apagar, o automatico volta no proximo Play.\n\n" +
            "Pra tirar o automatico do projeto inteiro, adicione o simbolo " +
            "JOGO_SEM_BOOTSTRAP em Project Settings > Player > Scripting Define Symbols.",
            "Beleza");
    }

    // ================================================================ montagem
    private static GameObject MontarJogador(Vector3 posicao)
    {
        // tocarAnimacao = false: fora do Play o clipe criaria um Sprite temporario, que
        // nao sobrevive a salvar a cena. A previa vem de um Sprite de verdade da folha.
        GameObject jogador = Construtor.MontarJogador(posicao, tocarAnimacao: false);

        PorSpriteDePrevia(jogador, FOLHA_DO_JOGADOR, "idle_0");
        Undo.RegisterCreatedObjectUndo(jogador, "Montar jogador");

        return jogador;
    }

    private static GameObject MontarInimigo(Vector3 posicao)
    {
        GameObject inimigo = Construtor.MontarInimigo(posicao, "Inimigo", tocarAnimacao: false);

        PorSpriteDePrevia(inimigo, FOLHA_DO_INIMIGO, "LightBandit_0");
        Undo.RegisterCreatedObjectUndo(inimigo, "Montar inimigo");

        return inimigo;
    }

    /// <summary>
    /// Enfileira um bloco de cada tipo, todos apoiados na mesma linha de base, e devolve
    /// as raizes criadas. Tambem informa o topo e o centro da peca de chao, que e onde o
    /// boneco e o inimigo sao colocados de pe.
    /// </summary>
    private static List<GameObject> MontarPaleta(
        Vector3 origem, Sprite sprite, out float topoDoChao, out float centroDoChao)
    {
        GameObject pai = new GameObject("Kit de cenario");
        pai.transform.position = origem;
        Undo.RegisterCreatedObjectUndo(pai, "Montar pecas de cenario");

        List<GameObject> raizes = new List<GameObject> { pai };

        topoDoChao = origem.y;
        centroDoChao = origem.x;

        float cursor = origem.x;

        for (int i = 0; i < PALETA.Length; i++)
        {
            Peca p = PALETA[i];

            cursor += p.largura * 0.5f;

            // Apoia pela BASE: assim as pecas ficam numa prateleira, nao flutuando cada
            // uma na sua altura.
            Vector2 centro = new Vector2(cursor, origem.y + p.altura * 0.5f);

            GameObject bloco = Construtor.MontarBloco(
                p.nome, centro, new Vector2(p.largura, p.altura), p.cor, p.tipo, pai.transform, sprite);

            if (i == 0)
            {
                topoDoChao = origem.y + p.altura;
                centroDoChao = cursor;
            }

            raizes.Add(bloco);

            cursor += p.largura * 0.5f + ESPACO_ENTRE_PECAS;
        }

        return raizes;
    }

    // ================================================================ previa
    /// <summary>
    /// Poe no SpriteRenderer um quadro de verdade da folha de arte, so pra o objeto nao
    /// ficar invisivel no editor. No Play o AnimadorDeSprites assume e troca por um quadro
    /// recortado com o pivo calculado.
    ///
    /// Funciona porque os cortes dessas folhas ja tem pivo embaixo e no centro
    /// (alignment Custom, pivot 0.5/0) — ou seja, os pes do boneco na origem do objeto,
    /// que e exatamente onde a receita os coloca.
    /// </summary>
    private static void PorSpriteDePrevia(GameObject raiz, string folha, string nomeDoQuadro)
    {
        SpriteRenderer sr = raiz.GetComponentInChildren<SpriteRenderer>(true);

        if (sr == null)
            return;

        Sprite quadro = AcharSubSprite(folha, nomeDoQuadro);

        if (quadro == null)
        {
            Debug.LogWarning($"[MontadorDeCena] nao achei o quadro de previa em {folha}. " +
                             "O objeto fica invisivel no editor, mas aparece normalmente no Play.");
            return;
        }

        sr.sprite = quadro;
    }

    private static Sprite AcharSubSprite(string caminhoDaFolha, string nomePreferido)
    {
        Object[] pedacos = AssetDatabase.LoadAllAssetRepresentationsAtPath(caminhoDaFolha);

        if (pedacos == null)
            return null;

        Sprite primeiro = null;

        foreach (Object pedaco in pedacos)
        {
            Sprite s = pedaco as Sprite;

            if (s == null)
                continue;

            if (s.name == nomePreferido)
                return s;

            if (primeiro == null)
                primeiro = s;
        }

        return primeiro;
    }

    // ================================================================ sprite do bloco
    /// <summary>
    /// Garante um PNG de verdade em Assets/Sprites/bloco.png com os mesmos pixels que o
    /// Bootstrap gera na memoria.
    ///
    /// Por que um arquivo e nao o sprite gerado: um Sprite criado por codigo nao e asset —
    /// ao salvar a cena, a referencia morre e os blocos ficariam sem desenho. Com o PNG,
    /// os blocos e os prefabs apontam pra um asset de verdade. E, de bonus, da pra trocar
    /// esse arquivo pelo seu tileset depois: todos os blocos mudam de cara juntos.
    /// </summary>
    private static Sprite GarantirSpriteDoBloco()
    {
        Sprite existente = AssetDatabase.LoadAssetAtPath<Sprite>(CAMINHO_DO_BLOCO);

        if (existente != null)
            return existente;

        GarantirPasta(PASTA_SPRITES);

        Texture2D temporaria = new Texture2D(LADO_DO_BLOCO, LADO_DO_BLOCO, TextureFormat.RGBA32, false);

        try
        {
            temporaria.SetPixels32(Construtor.PixelsDoBloco(LADO_DO_BLOCO));
            temporaria.Apply();
            File.WriteAllBytes(Path.GetFullPath(CAMINHO_DO_BLOCO), temporaria.EncodeToPNG());
        }
        finally
        {
            Object.DestroyImmediate(temporaria);
        }

        AssetDatabase.ImportAsset(CAMINHO_DO_BLOCO, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importador = AssetImporter.GetAtPath(CAMINHO_DO_BLOCO) as TextureImporter;

        if (importador != null)
        {
            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.spritePixelsPerUnit = LADO_DO_BLOCO;   // 16 px = 1 unidade
            importador.filterMode = FilterMode.Point;
            importador.wrapMode = TextureWrapMode.Repeat;     // Repeat = o modo Tiled repete
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.mipmapEnabled = false;
            importador.alphaIsTransparency = true;

            // Tiled so funciona com malha Full Rect, e o mesh type mora aqui.
            TextureImporterSettings ajustes = new TextureImporterSettings();
            importador.ReadTextureSettings(ajustes);
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            ajustes.spriteExtrude = 0;
            importador.SetTextureSettings(ajustes);

            importador.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(CAMINHO_DO_BLOCO);
    }

    // ================================================================ prefabs
    /// <summary>
    /// Salva Jogador, Inimigo e cada peca da paleta como prefab, e liga as instancias da
    /// cena a eles. Devolve false se o usuario recusou sobrescrever prefabs existentes.
    /// </summary>
    private static bool SalvarPrefabs(GameObject jogador, GameObject inimigo, List<GameObject> raizes)
    {
        GarantirPasta(PASTA_PREFABS);

        List<GameObject> paraSalvar = new List<GameObject> { jogador, inimigo };

        foreach (GameObject raiz in raizes)
        {
            foreach (Peca p in PALETA)
            {
                if (raiz != null && raiz.name == p.nome)
                    paraSalvar.Add(raiz);
            }
        }

        List<string> jaExistem = new List<string>();

        foreach (GameObject obj in paraSalvar)
        {
            if (obj == null)
                continue;

            string caminho = $"{PASTA_PREFABS}/{obj.name}.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(caminho) != null)
                jaExistem.Add(obj.name);
        }

        if (jaExistem.Count > 0)
        {
            bool sobrescrever = EditorUtility.DisplayDialog(
                "Prefabs ja existem",
                $"Estes prefabs ja existem em {PASTA_PREFABS}:\n\n  {string.Join(", ", jaExistem)}\n\n" +
                "Sobrescrever apaga o que voce ajustou neles.",
                "Sobrescrever", "Manter os meus");

            if (!sobrescrever)
                return false;
        }

        foreach (GameObject obj in paraSalvar)
        {
            if (obj == null)
                continue;

            string caminho = $"{PASTA_PREFABS}/{obj.name}.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(obj, caminho, InteractionMode.AutomatedAction);
        }

        AssetDatabase.SaveAssets();
        return true;
    }

    // ================================================================ camera, HUD, bootstrap
    private static void AjustarCamera(GameObject jogador)
    {
        Camera cam = Camera.main;

        if (cam == null)
            return;

        Undo.RecordObject(cam, "Ajustar camera");
        cam.orthographic = true;
        cam.orthographicSize = 2.8f;

        Cameramov seguidor = cam.GetComponent<Cameramov>();

        if (seguidor == null)
            seguidor = Undo.AddComponent<Cameramov>(cam.gameObject);

        Undo.RecordObject(seguidor, "Ajustar camera");
        seguidor.DefinirAlvo(jogador != null ? jogador.transform : null);

        if (jogador != null)
        {
            Vector3 pos = jogador.transform.position;
            cam.transform.position = new Vector3(pos.x, pos.y + 0.6f, cam.transform.position.z);
        }
    }

    private static void GarantirHud()
    {
        if (Object.FindAnyObjectByType<Hud>() != null)
            return;

        GameObject obj = new GameObject("HUD");
        obj.AddComponent<Hud>();
        Undo.RegisterCreatedObjectUndo(obj, "Montar HUD");
    }

    /// <summary>
    /// Deixa na cena um Bootstrap com a chave mestra desligada. Dois efeitos: ele nao
    /// monta nada, e a existencia dele impede a instalacao automatica de outro no Play.
    /// </summary>
    private static Bootstrap DesligarBootstrap()
    {
        Bootstrap bootstrap = Object.FindAnyObjectByType<Bootstrap>(FindObjectsInactive.Include);

        if (bootstrap == null)
        {
            GameObject obj = new GameObject("Bootstrap (desligado)");
            bootstrap = obj.AddComponent<Bootstrap>();
            Undo.RegisterCreatedObjectUndo(obj, "Desligar o Bootstrap");
        }

        Undo.RecordObject(bootstrap, "Desligar o Bootstrap");
        bootstrap.Ativo = false;
        EditorUtility.SetDirty(bootstrap);

        return bootstrap;
    }

    // ================================================================ utilidades
    private static Vector3 OndeACenaEstaOlhando()
    {
        SceneView janela = SceneView.lastActiveSceneView;
        Vector3 ponto = janela != null ? janela.pivot : Vector3.zero;

        // Arredonda pra deixar as pecas em posicoes redondas, mais facil de alinhar depois.
        return new Vector3(Mathf.Round(ponto.x * 2f) * 0.5f, Mathf.Round(ponto.y * 2f) * 0.5f, 0f);
    }

    private static int ComecarGrupo(string nome)
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(nome);
        return Undo.GetCurrentGroup();
    }

    private static void Finalizar(int grupo, List<GameObject> raizes, GameObject selecionar)
    {
        Undo.CollapseUndoOperations(grupo);
        MarcarCenaSuja();

        if (selecionar != null)
            Selection.activeGameObject = selecionar;

        // Sincroniza colisor com desenho ja no editor, sem esperar o primeiro Update.
        foreach (GameObject raiz in raizes)
        {
            if (raiz == null)
                continue;

            foreach (BlocoDoCenario bloco in raiz.GetComponentsInChildren<BlocoDoCenario>(true))
                bloco.Sincronizar();
        }
    }

    private static void MarcarCenaSuja()
    {
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    private static void GarantirPasta(string caminho)
    {
        if (AssetDatabase.IsValidFolder(caminho))
            return;

        string pai = Path.GetDirectoryName(caminho).Replace("\\", "/");
        string nome = Path.GetFileName(caminho);

        AssetDatabase.CreateFolder(pai, nome);
    }
}
#endif
