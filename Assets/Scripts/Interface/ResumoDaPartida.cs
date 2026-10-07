using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Os numeros da partida que a <see cref="TelaDeFimDeJogo"/> mostra: tempo jogado,
/// inimigos derrotados e chefes derrotados. Zera sozinho quando a cena carrega (cada
/// partida nova recarrega a cena). Veio do jogo antigo (la eram as salas exploradas).
///
/// Quem conta e o <see cref="GeradorDoAndar"/> (inimigo ou chefe morreu).
/// </summary>
public static class ResumoDaPartida
{
    public static int InimigosDerrotados { get; private set; }

    public static int ChefesDerrotados { get; private set; }

    /// <summary>
    /// Segundos de jogo desde que a cena abriu. E tempo do jogo (escalado): menu, pausa e
    /// telas com o jogo congelado nao contam.
    /// </summary>
    public static float Tempo => tempoAntes + Time.timeSinceLevelLoad - inicio;

    private static float tempoAntes;
    private static float inicio;

    /// <summary>Os numeros de uma partida salva (o "Continuar" do menu).</summary>
    public static void Restaurar(int inimigos, int chefes, float tempo)
    {
        InimigosDerrotados = inimigos;
        ChefesDerrotados = chefes;
        tempoAntes = tempo;
        inicio = Time.timeSinceLevelLoad;
    }

    public static void ContarInimigo() => InimigosDerrotados++;

    public static void ContarChefe() => ChefesDerrotados++;

    /// <summary>"mm:ss", ou "h:mm:ss" passando de uma hora.</summary>
    public static string TempoFormatado()
    {
        int total = Mathf.FloorToInt(Tempo);
        int horas = total / 3600;
        int minutos = total / 60 % 60;
        int segundos = total % 60;
        return horas > 0 ? $"{horas}:{minutos:00}:{segundos:00}" : $"{minutos:00}:{segundos:00}";
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Preparar()
    {
        Zerar();
        SceneManager.sceneLoaded -= AoCarregar;
        SceneManager.sceneLoaded += AoCarregar;
    }

    private static void AoCarregar(Scene cena, LoadSceneMode modo)
    {
        if (modo == LoadSceneMode.Single)
            Zerar();
    }

    private static void Zerar()
    {
        InimigosDerrotados = 0;
        ChefesDerrotados = 0;
        tempoAntes = 0f;
        inicio = 0f;
    }
}
