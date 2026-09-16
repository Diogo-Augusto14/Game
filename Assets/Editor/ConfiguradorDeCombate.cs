#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Ferramenta de editor que faz a parte chata da montagem do sistema de combate:
/// cria as layers, configura a matriz de colisão 2D, adiciona os parâmetros que
/// faltam no Animator Controller, monta as hitboxes como filhas do boneco e marca
/// os Animation Events nos clipes de ataque.
///
/// Menu: Tools > Combate.
/// Este arquivo PRECISA ficar numa pasta chamada Editor — ele não vai pro jogo.
/// </summary>
public static class ConfiguradorDeCombate
{
    private const string LAYER_GOLPE = "Golpe";
    private const string LAYER_INIMIGO = "Inimigo";

    // Parâmetros novos. Os que você já tem (IsGround, IsSliding…) não são tocados.
    private static readonly (string nome, AnimatorControllerParameterType tipo)[] PARAMETROS =
    {
        ("Attack",      AnimatorControllerParameterType.Trigger),
        ("AttackAir",   AnimatorControllerParameterType.Trigger),
        ("Hurt",        AnimatorControllerParameterType.Trigger),
        ("Die",         AnimatorControllerParameterType.Trigger),
        ("IsHealing",   AnimatorControllerParameterType.Bool),
        ("IsDashing",   AnimatorControllerParameterType.Bool),
        ("IsCrouching", AnimatorControllerParameterType.Bool),
        ("IsHanging",   AnimatorControllerParameterType.Bool),
    };

    // =============================================================== TUDO
    [MenuItem("Tools/Combate/Fazer tudo (com o Boneco selecionado)", false, 0)]
    private static void FazerTudo()
    {
        GameObject boneco = Selection.activeGameObject;
        if (boneco == null)
        {
            Aviso("Selecione o Boneco na Hierarchy antes de rodar.");
            return;
        }

        var relatorio = new List<string>();
        relatorio.Add(CriarLayers());
        relatorio.Add(ConfigurarMatriz());
        relatorio.Add(CriarParametros(boneco));
        relatorio.Add(MontarHitboxes(boneco));

        EditorUtility.DisplayDialog("Combate configurado",
            string.Join("\n\n", relatorio) +
            "\n\nFalta só marcar os Animation Events: selecione o clipe de ataque na " +
            "janela Project e rode Tools > Combate > Marcar eventos no clipe.", "Beleza");
    }

    // =============================================================== 1 · LAYERS
    [MenuItem("Tools/Combate/1 · Criar layers Golpe e Inimigo", false, 20)]
    private static void MenuCriarLayers()
    {
        Aviso(CriarLayers());
    }

    private static string CriarLayers()
    {
        int golpe = GarantirLayer(LAYER_GOLPE);
        int inimigo = GarantirLayer(LAYER_INIMIGO);

        if (golpe < 0 || inimigo < 0)
            return "LAYERS: não deu — não há espaço livre em Project Settings > Tags and Layers. " +
                   "Libere uma linha e rode de novo.";

        return $"LAYERS: {LAYER_GOLPE} = camada {golpe}, {LAYER_INIMIGO} = camada {inimigo}.";
    }

    /// <summary>Devolve o índice da layer, criando se não existir. -1 se não couber.</summary>
    private static int GarantirLayer(string nome)
    {
        int existente = LayerMask.NameToLayer(nome);
        if (existente >= 0)
            return existente;

        Object[] ativos = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (ativos == null || ativos.Length == 0)
            return -1;

        SerializedObject so = new SerializedObject(ativos[0]);
        SerializedProperty layers = so.FindProperty("layers");
        if (layers == null)
            return -1;

        // 0 a 7 são reservadas pela Unity.
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty item = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(item.stringValue))
            {
                item.stringValue = nome;
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                return i;
            }
        }
        return -1;
    }

    // =============================================================== 2 · MATRIZ
    [MenuItem("Tools/Combate/2 · Configurar matriz de colisão 2D", false, 21)]
    private static void MenuConfigurarMatriz()
    {
        Aviso(ConfigurarMatriz());
    }

    private static string ConfigurarMatriz()
    {
        int golpe = LayerMask.NameToLayer(LAYER_GOLPE);
        int inimigo = LayerMask.NameToLayer(LAYER_INIMIGO);

        if (golpe < 0 || inimigo < 0)
            return "MATRIZ: crie as layers primeiro (passo 1).";

        // A layer Golpe só conversa com a layer Inimigo. Todo o resto é ignorado
        // pela própria física, antes de chegar no seu código.
        for (int outra = 0; outra < 32; outra++)
            Physics2D.IgnoreLayerCollision(golpe, outra, outra != inimigo);

        bool salvou = GravarMatrizNoArquivo(golpe, inimigo);

        return salvou
            ? $"MATRIZ: {LAYER_GOLPE} agora só colide com {LAYER_INIMIGO}."
            : $"MATRIZ: aplicada nesta sessão, mas NÃO consegui gravar no arquivo de " +
              $"Project Settings. Confira à mão em Edit > Project Settings > Physics 2D > " +
              $"Layer Collision Matrix: na linha {LAYER_GOLPE}, só {LAYER_INIMIGO} marcado.";
    }

    /// <summary>Escreve a matriz no Physics2DSettings.asset pra a mudança sobreviver ao fechar a Unity.</summary>
    private static bool GravarMatrizNoArquivo(int golpe, int inimigo)
    {
        try
        {
            Object[] ativos = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset");
            if (ativos == null || ativos.Length == 0)
                return false;

            SerializedObject so = new SerializedObject(ativos[0]);
            SerializedProperty matriz = so.FindProperty("m_LayerCollisionMatrix");
            if (matriz == null || !matriz.isArray || matriz.arraySize < 32)
                return false;

            // Cada posição i guarda a máscara das layers com que i colide.
            long mascaraDoGolpe = 1L << inimigo;
            matriz.GetArrayElementAtIndex(golpe).longValue = mascaraDoGolpe;

            // A matriz é simétrica: as outras linhas precisam tirar o bit do Golpe.
            for (int i = 0; i < 32; i++)
            {
                if (i == golpe)
                    continue;

                SerializedProperty item = matriz.GetArrayElementAtIndex(i);
                long valor = item.longValue;
                valor = (i == inimigo) ? (valor | (1L << golpe)) : (valor & ~(1L << golpe));
                item.longValue = valor;
            }

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Combate] não gravei a matriz no arquivo: {e.Message}");
            return false;
        }
    }

    // =============================================================== 3 · ANIMATOR
    [MenuItem("Tools/Combate/3 · Criar parâmetros do Animator", false, 22)]
    private static void MenuCriarParametros()
    {
        GameObject boneco = Selection.activeGameObject;
        if (boneco == null)
        {
            Aviso("Selecione o Boneco (ou o inimigo) na Hierarchy.");
            return;
        }
        Aviso(CriarParametros(boneco));
    }

    private static string CriarParametros(GameObject alvo)
    {
        Animator animator = alvo.GetComponentInChildren<Animator>();
        if (animator == null)
            return "ANIMATOR: este objeto não tem Animator.";

        AnimatorController controlador = animator.runtimeAnimatorController as AnimatorController;
        if (controlador == null)
            return "ANIMATOR: o Animator Controller não é um asset editável " +
                   "(pode ser um Override Controller). Crie os parâmetros à mão.";

        var existentes = new HashSet<string>();
        foreach (AnimatorControllerParameter p in controlador.parameters)
            existentes.Add(p.name);

        var criados = new List<string>();
        foreach (var (nome, tipo) in PARAMETROS)
        {
            if (existentes.Contains(nome))
                continue;

            controlador.AddParameter(nome, tipo);
            criados.Add(nome);
        }

        EditorUtility.SetDirty(controlador);
        AssetDatabase.SaveAssets();

        return criados.Count == 0
            ? "ANIMATOR: todos os parâmetros já existiam."
            : $"ANIMATOR: criados em {controlador.name} → {string.Join(", ", criados)}.";
    }

    // =============================================================== 4 · HITBOXES
    [MenuItem("Tools/Combate/4 · Montar hitboxes no Boneco", false, 23)]
    private static void MenuMontarHitboxes()
    {
        GameObject boneco = Selection.activeGameObject;
        if (boneco == null)
        {
            Aviso("Selecione o Boneco na Hierarchy.");
            return;
        }
        Aviso(MontarHitboxes(boneco));
    }

    private static string MontarHitboxes(GameObject boneco)
    {
        int layerGolpe = LayerMask.NameToLayer(LAYER_GOLPE);
        if (layerGolpe < 0)
            return "HITBOXES: crie as layers primeiro (passo 1).";

        Espada chao = GarantirHitbox(boneco, "Espada", new Vector3(0.9f, 0f, 0f),
                                     new Vector2(1.2f, 0.8f), 5f, 6f, layerGolpe);
        Espada ar = GarantirHitbox(boneco, "EspadaAr", new Vector3(0.9f, 0f, 0f),
                                   new Vector2(1.3f, 1.0f), 5f, 7f, layerGolpe);

        Ataque ataque = boneco.GetComponent<Ataque>();
        if (ataque == null)
            ataque = Undo.AddComponent<Ataque>(boneco);

        SerializedObject so = new SerializedObject(ataque);
        so.FindProperty("golpeNoChao.hitbox").objectReferenceValue = chao;
        so.FindProperty("golpeNoAr.hitbox").objectReferenceValue = ar;
        so.ApplyModifiedProperties();

        return "HITBOXES: Espada e EspadaAr criadas como filhas do Boneco e ligadas no " +
               "componente Ataque. Ajuste posição e tamanho olhando o gizmo na Scene.";
    }

    private static Espada GarantirHitbox(GameObject pai, string nome, Vector3 posicao,
                                         Vector2 tamanho, float dano, float empurrao, int layer)
    {
        Transform t = pai.transform.Find(nome);
        GameObject alvo;

        if (t != null)
        {
            alvo = t.gameObject;   // já existe: não mexe na posição que você ajustou
        }
        else
        {
            alvo = new GameObject(nome);
            Undo.RegisterCreatedObjectUndo(alvo, $"Criar {nome}");
            alvo.transform.SetParent(pai.transform, false);
            alvo.transform.localPosition = posicao;

            BoxCollider2D box = alvo.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = tamanho;
        }

        alvo.layer = layer;

        BoxCollider2D colisor = alvo.GetComponent<BoxCollider2D>();
        if (colisor == null)
        {
            colisor = Undo.AddComponent<BoxCollider2D>(alvo);
            colisor.size = tamanho;
        }
        colisor.isTrigger = true;

        Espada espada = alvo.GetComponent<Espada>();
        if (espada == null)
        {
            espada = Undo.AddComponent<Espada>(alvo);
            SerializedObject so = new SerializedObject(espada);
            so.FindProperty("dano").floatValue = dano;
            so.FindProperty("forcaEmpurrao").floatValue = empurrao;
            so.ApplyModifiedProperties();
        }

        return espada;
    }

    // =============================================================== 5 · EVENTOS
    [MenuItem("Tools/Combate/5 · Marcar eventos no clipe de ataque", false, 24)]
    private static void MarcarEventos()
    {
        var clipes = new List<AnimationClip>();
        foreach (Object obj in Selection.objects)
        {
            if (obj is AnimationClip clip)
                clipes.Add(clip);
        }

        if (clipes.Count == 0)
        {
            Aviso("Selecione um ou mais clipes de ataque (.anim) na janela Project.");
            return;
        }

        var feitos = new List<string>();
        foreach (AnimationClip clip in clipes)
        {
            if ((clip.hideFlags & HideFlags.NotEditable) != 0)
            {
                feitos.Add($"{clip.name}: não editável (clipe importado), pulei.");
                continue;
            }

            // Posições de partida: 25% / 55% / fim. Você arrasta na janela Animation
            // até bater com o quadro em que a espada está esticada.
            AnimationEvent[] eventos =
            {
                new AnimationEvent { time = clip.length * 0.25f, functionName = "AtivarGolpe" },
                new AnimationEvent { time = clip.length * 0.55f, functionName = "DesativarGolpe" },
                new AnimationEvent { time = Mathf.Max(0f, clip.length - 0.01f), functionName = "TerminarAtaque" },
            };

            AnimationUtility.SetAnimationEvents(clip, eventos);
            EditorUtility.SetDirty(clip);
            feitos.Add($"{clip.name}: 3 eventos marcados.");
        }

        AssetDatabase.SaveAssets();
        Aviso(string.Join("\n", feitos) +
              "\n\nOs tempos são um chute inicial. Abra Window > Animation, arraste os " +
              "marcadores até os quadros certos e marque 'Usar Eventos De Animação' no componente Ataque.");
    }

    // =============================================================== conferência
    [MenuItem("Tools/Combate/Conferir montagem (com o Boneco selecionado)", false, 40)]
    private static void Conferir()
    {
        GameObject boneco = Selection.activeGameObject;
        if (boneco == null)
        {
            Aviso("Selecione o Boneco na Hierarchy.");
            return;
        }

        var problemas = new List<string>();

        if (boneco.tag != "Player")
            problemas.Add("• A tag do Boneco não é Player — o Inimigo não vai achar ele.");

        Rigidbody2D rb = boneco.GetComponent<Rigidbody2D>();
        if (rb == null)
            problemas.Add("• Falta Rigidbody2D no Boneco.");
        else
        {
            if (rb.interpolation != RigidbodyInterpolation2D.Interpolate)
                problemas.Add("• Rigidbody2D sem Interpolate — a imagem vai tremer.");
            if (rb.bodyType != RigidbodyType2D.Dynamic)
                problemas.Add("• Rigidbody2D não está em Dynamic — sem empurrão.");
        }

        Vida vida = boneco.GetComponent<Vida>();
        if (vida == null)
            problemas.Add("• Falta o componente Vida no Boneco.");
        else
        {
            SerializedObject so = new SerializedObject(vida);
            if (so.FindProperty("destruirAoMorrer").boolValue)
                problemas.Add("• Vida do Boneco com 'Destruir Ao Morrer' LIGADO — ele vai sumir em vez de renascer.");
        }

        if (boneco.GetComponent<Player>() == null)
            problemas.Add("• Falta o componente Player no Boneco.");
        if (boneco.GetComponent<Ataque>() == null)
            problemas.Add("• Falta o componente Ataque no Boneco.");
        if (boneco.GetComponentInChildren<Espada>(true) == null)
            problemas.Add("• Nenhuma hitbox (Espada) nos filhos do Boneco.");

        if (Camera.main == null || Camera.main.GetComponent<Cameramov>() == null)
            problemas.Add("• A Main Camera não tem o componente Cameramov.");

        if (LayerMask.NameToLayer(LAYER_GOLPE) < 0)
            problemas.Add("• A layer Golpe não existe.");
        if (LayerMask.NameToLayer(LAYER_INIMIGO) < 0)
            problemas.Add("• A layer Inimigo não existe.");

        Aviso(problemas.Count == 0
            ? "Está tudo no lugar. Pode dar Play."
            : "Faltando:\n\n" + string.Join("\n", problemas));
    }

    private static void Aviso(string texto)
    {
        EditorUtility.DisplayDialog("Combate", texto, "OK");
        Debug.Log($"[Combate] {texto}");
    }
}
#endif
