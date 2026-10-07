using System.IO;
using UnityEngine;

/// <summary>
/// O lançador antigo não consegue trocar a si mesmo: quando instala uma versão, pula o próprio .exe.
/// Por isso o zip traz o lançador novo também com outro nome (ThePrettie-Lancador-novo.exe), e o jogo,
/// ao abrir (o lançador já fechou), põe ele no lugar do velho.
///
/// O lançador novo já sabe se atualizar sozinho; isto aqui só serve pra quem ainda tem o antigo.
/// </summary>
public static class TrocaDoLancador
{
    private const string Lancador = "ThePrettie-Lancador.exe";
    private const string LancadorNovo = "ThePrettie-Lancador-novo.exe";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Trocar()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // dataPath e a pasta ThePrettie_Data; o jogo e o lançador ficam na pasta de cima.
        string pasta = Path.GetDirectoryName(Application.dataPath);
        string novo = Path.Combine(pasta, LancadorNovo);

        if (!File.Exists(novo))
            return;

        try
        {
            File.Copy(novo, Path.Combine(pasta, Lancador), true);
            File.Delete(novo);
        }
        catch (System.Exception erro)
        {
            // O lançador ainda aberto (ou sem permissão): tenta de novo na próxima vez que o jogo abrir.
            Debug.LogWarning("[Lançador] não deu pra trocar agora: " + erro.Message);
        }
#endif
    }
}
