#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cria uma cena pronta pra testar o gerador de andar: camera, o objeto <see cref="Andar"/>
/// e um Bootstrap DESLIGADO (e a presenca dele que impede o Bootstrap de plataforma de se
/// instalar sozinho e montar a fase lateral por cima do andar).
///
/// Menu: Tools ▸ Jogo ▸ Andar ▸ Criar cena de teste do andar.
/// </summary>
public static class CenaDoAndar
{
    private const string CAMINHO = "Assets/Scenes/Andar.unity";

    [MenuItem("Tools/Jogo/Andar/Criar cena de teste do andar", false, 60)]
    private static void Criar()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CAMINHO) != null &&
            !EditorUtility.DisplayDialog("Cena do andar", $"{CAMINHO} ja existe. Substituir?", "Substituir", "Cancelar"))
            return;

        Scene cena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Camera cam = Camera.main;

        if (cam != null)
        {
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        new GameObject("Andar").AddComponent<Andar>();

        Bootstrap bootstrap = new GameObject("Bootstrap (desligado)").AddComponent<Bootstrap>();
        bootstrap.Ativo = false;

        EditorSceneManager.SaveScene(cena, CAMINHO);
        Debug.Log($"[Andar] cena criada em {CAMINHO}. Aperte Play: WASD anda, setas atiram.");
    }
}
#endif
