using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// O Play do editor sempre comeca pela cena do jogo (Assets/Cenas/Jogo.unity), qualquer que seja a
/// cena aberta. Menu Jogo ▸ Play sempre pela cena do jogo (marcado = ligado).
/// </summary>
[InitializeOnLoad]
public static class PlayPelaCenaDoJogo
{
    private const string Menu = "Jogo/Play sempre pela cena do jogo";
    private const string Chave = "Jogo.PlayPelaCenaDoJogo";

    static PlayPelaCenaDoJogo()
    {
        // delayCall: no meio da recarga o AssetDatabase pode nao estar pronto.
        EditorApplication.delayCall += Aplicar;
    }

    private static bool Ligado
    {
        get => EditorPrefs.GetBool(Chave, true);
        set => EditorPrefs.SetBool(Chave, value);
    }

    [MenuItem(Menu, false, 20)]
    private static void Alternar()
    {
        Ligado = !Ligado;
        Aplicar();
    }

    [MenuItem(Menu, true)]
    private static bool MarcarNoMenu()
    {
        UnityEditor.Menu.SetChecked(Menu, Ligado);
        return true;
    }

    public static void Aplicar()
    {
        EditorSceneManager.playModeStartScene = Ligado ? AssetDatabase.LoadAssetAtPath<SceneAsset>(MontarJogo.Cena) : null;
    }
}
