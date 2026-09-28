#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu <c>Tools ▸ Jogo ▸ Sala ▸ Criar cena de teste</c>: cria (ou recria) a cena
/// Assets/Scenes/SalaDeTeste.unity com camera, a <see cref="DemoDaSala"/> e um Bootstrap
/// DESLIGADO — que e o que impede o Bootstrap de plataforma de se instalar sozinho e
/// montar o boneco de plataforma no meio da sala.
/// </summary>
public static class CriadorDaSalaDeTeste
{
    private const string Caminho = "Assets/Scenes/SalaDeTeste.unity";

    [MenuItem("Tools/Jogo/Sala/Criar cena de teste", false, 60)]
    private static void Criar()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject objCamera = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera cam = objCamera.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.04f, 0.04f);
        objCamera.transform.position = new Vector3(0f, 0f, -10f);

        GameObject objBootstrap = new GameObject("Bootstrap (desligado)");
        objBootstrap.AddComponent<Bootstrap>().Ativo = false;

        new GameObject("Sala de teste").AddComponent<DemoDaSala>();

        EditorSceneManager.SaveScene(cena, Caminho);
        AdicionarNaBuild();

        Debug.Log($"[Sala] cena criada em {Caminho}. Aperte Play: WASD anda, setas atiram.");
    }

    private static void AdicionarNaBuild()
    {
        var cenas = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        foreach (EditorBuildSettingsScene c in cenas)
        {
            if (c.path == Caminho)
                return;
        }

        cenas.Add(new EditorBuildSettingsScene(Caminho, true));
        EditorBuildSettings.scenes = cenas.ToArray();
    }
}
#endif
