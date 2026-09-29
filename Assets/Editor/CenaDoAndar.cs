#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// O Play do editor sempre comeca pela cena do jogo (Assets/Scenes/Jogo.unity), qualquer
/// que seja a cena aberta. Assim nunca roda uma cena velha ou de teste sem querer.
///
/// Menu: Tools ▸ Jogo ▸ Play sempre pela cena do jogo (marcado = ligado). Desmarque pra
/// testar outra cena, como a sala de treino (TopDown).
/// </summary>
[InitializeOnLoad]
public static class CenaDoAndar
{
    private const string CAMINHO = "Assets/Scenes/Jogo.unity";
    private const string MENU = "Tools/Jogo/Play sempre pela cena do jogo";
    private const string CHAVE = "Jogo.PlayPelaCenaDoJogo";

    static CenaDoAndar()
    {
        // delayCall: o AssetDatabase pode nao estar pronto no meio da recarga de dominio.
        EditorApplication.delayCall += Aplicar;
    }

    private static bool Ligado
    {
        get => EditorPrefs.GetBool(CHAVE, true);
        set => EditorPrefs.SetBool(CHAVE, value);
    }

    [MenuItem(MENU, false, 60)]
    private static void Alternar()
    {
        Ligado = !Ligado;
        Aplicar();
    }

    [MenuItem(MENU, true)]
    private static bool MarcarNoMenu()
    {
        Menu.SetChecked(MENU, Ligado);
        return true;
    }

    private static void Aplicar()
    {
        EditorSceneManager.playModeStartScene = Ligado ? AssetDatabase.LoadAssetAtPath<SceneAsset>(CAMINHO) : null;
    }
}
#endif
