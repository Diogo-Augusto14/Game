/// <summary>
/// O que a partida conta (pra tela do fim) e o recado de "jogar de novo" que sobrevive a recarga da
/// cena: com <see cref="PularMenu"/> ligado, a cena volta direto pro jogo, sem o menu inicial.
/// </summary>
public static class Partida
{
    /// <summary>Recarregou a cena pra jogar de novo: comeca direto, sem o menu.</summary>
    public static bool PularMenu;

    public static int InimigosMortos;

    /// <summary>Segundos jogando (sem contar menu e pausa).</summary>
    public static float Tempo;

    /// <summary>O andar mais fundo que o jogador chegou.</summary>
    public static int Andar;

    /// <summary>O nome do andar mais fundo ("Andar 3 de 6: Covil do Minotauro").</summary>
    public static string NomeDoAndar;

    public static void Zerar()
    {
        InimigosMortos = 0;
        Tempo = 0f;
        Andar = 0;
        NomeDoAndar = "";
    }
}
