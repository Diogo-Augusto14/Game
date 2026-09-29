using UnityEngine;

/// <summary>
/// Teclas de som que valem em qualquer tela: M liga/desliga a musica e N os efeitos
/// (LB e RB no controle, no menu e na pausa).
/// Desligar guarda o volume antigo pra ligar de volta no mesmo nivel.
/// </summary>
public static class Opcoes
{
    private static float musicaAntes = 0.45f;
    private static float efeitosAntes = 0.7f;

    public static bool MusicaLigada => Musica.Volume > 0f;

    public static bool EfeitosLigados => Sons.Volume > 0f;

    /// <summary>Chame uma vez por quadro de quem estiver com a tela (menu, pausa, jogo).</summary>
    public static void LerTeclas()
    {
        // No controle, LB e RB so com o jogo parado (menu, pausa): jogando, LB e bomba.
        bool parado = Time.timeScale == 0f;

        if (Input.GetKeyDown(KeyCode.M) || (parado && Controle.Apertou(BotaoDoControle.LB)))
            AlternarMusica();

        if (Input.GetKeyDown(KeyCode.N) || (parado && Controle.Apertou(BotaoDoControle.RB)))
            AlternarEfeitos();
    }

    public static void AlternarMusica()
    {
        if (MusicaLigada)
        {
            musicaAntes = Musica.Volume;
            Musica.Volume = 0f;
        }
        else
        {
            Musica.Volume = Mathf.Max(0.1f, musicaAntes);
        }
    }

    public static void AlternarEfeitos()
    {
        if (EfeitosLigados)
        {
            efeitosAntes = Sons.Volume;
            Sons.Volume = 0f;
        }
        else
        {
            Sons.Volume = Mathf.Max(0.1f, efeitosAntes);
            Sons.Tocar(Som.Menu);
        }
    }
}
