#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Prepara o projeto pra rodar: cria as camadas e tags que o jogo espera, arruma a matriz
/// de colisao 2D e gera a biblioteca de animacoes se ela nao existir.
///
/// Menu: Tools ▸ Jogo ▸ Preparar projeto.
///
/// Tambem roda SOZINHO em dois momentos, e e isso que sustenta o "so dar play":
///   • ao abrir o projeto (ou depois de recompilar), se a biblioteca nao existir;
///   • ao entrar no Play, pela mesma razao.
/// Quem clona o repositorio aperta Play e o jogo funciona, sem passo manual nenhum.
/// </summary>
[InitializeOnLoad]
public static class ConfiguradorDoProjeto
{
    private static readonly string[] CAMADAS = { "Chao", "Parede", "Inimigo", "Golpe" };
    private static readonly string[] TAGS = { "Player" };

    // ================================================================ automatico
    static ConfiguradorDoProjeto()
    {
        EditorApplication.playModeStateChanged += AoTrocarDeModo;

        // delayCall: mexer em assets no meio da recarga de dominio da pau.
        EditorApplication.delayCall += GarantirBiblioteca;
    }

    private static void AoTrocarDeModo(PlayModeStateChange estado)
    {
        if (estado == PlayModeStateChange.ExitingEditMode)
            GarantirBiblioteca();
    }

    private static void GarantirBiblioteca()
    {
        if (ConstrutorDeAnimacoes.BibliotecaExiste())
            return;

        Debug.Log("[ConfiguradorDoProjeto] biblioteca de animacoes faltando — construindo agora.");

        GarantirCamadasETags();
        ConstrutorDeAnimacoes.Construir(out int clipes, out int quadros, out List<string> problemas);

        Debug.Log($"[ConfiguradorDoProjeto] biblioteca pronta: {clipes} clipes, {quadros} quadros.");

        foreach (string problema in problemas)
            Debug.LogWarning("[ConfiguradorDoProjeto] " + problema);
    }

    // ================================================================ menu
    [MenuItem("Tools/Jogo/Preparar projeto", false, 0)]
    private static void PrepararTudo()
    {
        List<string> relatorio = new List<string>
        {
            GarantirCamadasETags(),
            ArrumarMatrizDeColisao()
        };

        ConstrutorDeAnimacoes.Construir(out int clipes, out int quadros, out List<string> problemas);
        relatorio.Add($"ANIMACOES: {clipes} clipes, {quadros} quadros.");

        string mensagem = string.Join("\n\n", relatorio);

        if (problemas.Count > 0)
            mensagem += "\n\nProblemas:\n" + string.Join("\n", problemas);

        mensagem += "\n\nPode apertar Play.";

        EditorUtility.DisplayDialog("Projeto preparado", mensagem, "Beleza");
    }

    [MenuItem("Tools/Jogo/Conferir a cena", false, 40)]
    private static void ConferirCena()
    {
        List<string> avisos = new List<string>();

        if (!ConstrutorDeAnimacoes.BibliotecaExiste())
            avisos.Add("- A biblioteca de animacoes nao existe. Rode Preparar projeto.");

        bool temChao = false;

        foreach (Collider2D c in Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (!c.isTrigger && c.attachedRigidbody == null)
            {
                temChao = true;
                break;
            }
        }

        if (!temChao)
            avisos.Add("- A cena nao tem chao. O Bootstrap monta um de emergencia, mas o melhor e desenhar o seu.");

        if (Camera.main == null)
            avisos.Add("- Nao existe camera com a tag MainCamera. O Bootstrap cria uma.");

        foreach (string camada in CAMADAS)
        {
            if (LayerMask.NameToLayer(camada) < 0 && ApelidoExiste(camada) < 0)
                avisos.Add($"- Falta a camada \"{camada}\". Rode Preparar projeto.");
        }

        string mensagem = avisos.Count == 0
            ? "Tudo certo. Aperte Play."
            : string.Join("\n", avisos);

        EditorUtility.DisplayDialog("Conferencia da cena", mensagem, "Beleza");
    }

    private static int ApelidoExiste(string camada)
    {
        // O projeto veio com "ground" e "Wall"; os nomes em portugues sao os preferidos,
        // mas os antigos continuam valendo.
        switch (camada)
        {
            case "Chao": return Mathf.Max(LayerMask.NameToLayer("Ground"), LayerMask.NameToLayer("ground"));
            case "Parede": return Mathf.Max(LayerMask.NameToLayer("Wall"), LayerMask.NameToLayer("Walll"));
            default: return -1;
        }
    }

    // ================================================================ camadas e tags
    private static string GarantirCamadasETags()
    {
        List<string> criadas = new List<string>();
        List<string> faltaram = new List<string>();

        foreach (string camada in CAMADAS)
        {
            if (LayerMask.NameToLayer(camada) >= 0 || ApelidoExiste(camada) >= 0)
                continue;

            if (CriarCamada(camada))
                criadas.Add(camada);
            else
                faltaram.Add(camada);
        }

        foreach (string tag in TAGS)
        {
            if (!TagExiste(tag))
                CriarTag(tag);
        }

        Camadas.Esquecer();

        string texto = "CAMADAS: ";
        texto += criadas.Count > 0 ? $"criei {string.Join(", ", criadas)}. " : "nenhuma faltando. ";

        if (faltaram.Count > 0)
            texto += $"NAO couberam: {string.Join(", ", faltaram)} — libere linhas em Project Settings > Tags and Layers.";

        return texto;
    }

    private static bool CriarCamada(string nome)
    {
        Object[] ativos = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");

        if (ativos == null || ativos.Length == 0)
            return false;

        SerializedObject so = new SerializedObject(ativos[0]);
        SerializedProperty camadas = so.FindProperty("layers");

        if (camadas == null)
            return false;

        // 0 a 7 sao reservadas pela Unity.
        for (int i = 8; i < camadas.arraySize; i++)
        {
            SerializedProperty item = camadas.GetArrayElementAtIndex(i);

            if (!string.IsNullOrEmpty(item.stringValue))
                continue;

            item.stringValue = nome;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            return true;
        }

        return false;
    }

    private static bool TagExiste(string tag)
    {
        foreach (string existente in UnityEditorInternal.InternalEditorUtility.tags)
        {
            if (existente == tag)
                return true;
        }

        return false;
    }

    private static void CriarTag(string tag)
    {
        Object[] ativos = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");

        if (ativos == null || ativos.Length == 0)
            return;

        SerializedObject so = new SerializedObject(ativos[0]);
        SerializedProperty tags = so.FindProperty("tags");

        if (tags == null)
            return;

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    // ================================================================ matriz de colisao
    /// <summary>
    /// A camada Golpe e so hitbox: ela precisa ATRAVESSAR tudo e conversar apenas com
    /// quem pode levar dano. Sem isso a hitbox do golpe empurra o inimigo fisicamente,
    /// ou pior: colide com o chao e nunca encosta em ninguem.
    /// </summary>
    private static string ArrumarMatrizDeColisao()
    {
        int golpe = ResolverCamada("Golpe");
        int inimigo = ResolverCamada("Inimigo");
        int jogador = ResolverCamada("Player");

        if (golpe < 0)
            return "MATRIZ: a camada Golpe nao existe — rode Preparar projeto de novo.";

        for (int outra = 0; outra < 32; outra++)
        {
            bool deveColidir = outra == inimigo || outra == jogador;
            Physics2D.IgnoreLayerCollision(golpe, outra, !deveColidir);
        }

        // Golpe com Golpe nunca: duas hitboxes nao se machucam.
        Physics2D.IgnoreLayerCollision(golpe, golpe, true);

        GravarMatrizNoArquivo();

        return "MATRIZ: a camada Golpe agora so encosta em Player e Inimigo.";
    }

    private static int ResolverCamada(string nome)
    {
        int direto = LayerMask.NameToLayer(nome);
        return direto >= 0 ? direto : ApelidoExiste(nome);
    }

    /// <summary>
    /// Physics2D.IgnoreLayerCollision vale so pra sessao. Pra a matriz continuar depois de
    /// fechar a Unity, o valor tem que ir pro arquivo de Physics2DSettings.
    /// </summary>
    private static void GravarMatrizNoArquivo()
    {
        Object[] ativos = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset");

        if (ativos == null || ativos.Length == 0)
            return;

        SerializedObject so = new SerializedObject(ativos[0]);
        SerializedProperty matriz = so.FindProperty("m_LayerCollisionMatrix");

        if (matriz == null || !matriz.isArray)
            return;

        for (int i = 0; i < matriz.arraySize && i < 32; i++)
        {
            long mascara = 0;

            for (int j = 0; j < 32; j++)
            {
                if (!Physics2D.GetIgnoreLayerCollision(i, j))
                    mascara |= 1L << j;
            }

            SerializedProperty item = matriz.GetArrayElementAtIndex(i);

            if (item.propertyType == SerializedPropertyType.Integer)
                item.longValue = mascara;
        }

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }
}
#endif
