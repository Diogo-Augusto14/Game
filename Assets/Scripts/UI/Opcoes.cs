using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// As opcoes do jogo, salvas entre partidas: volume da musica e dos efeitos (ficam na
/// <see cref="Musica"/> e nos <see cref="Sons"/>), tela cheia e resolucao. A tela de
/// mexer nelas e a <see cref="TelaDeOpcoes"/>.
///
/// Teclas que valem em qualquer tela: M liga/desliga a musica e N os efeitos (LB e RB no
/// controle, so no menu e na pausa). Desligar
/// guarda o volume antigo pra ligar de volta no mesmo nivel (tambem salvo).
/// </summary>
public static class Opcoes
{
    private const string ChaveDaMusicaAntes = "volume-da-musica-antes";
    private const string ChaveDosEfeitosAntes = "volume-dos-efeitos-antes";
    private const string ChaveDaTelaCheia = "tela-cheia";
    private const string ChaveDaLargura = "resolucao-largura";
    private const string ChaveDaAltura = "resolucao-altura";

    /// <summary>Quanto cada passo da barra de volume muda.</summary>
    public const float PassoDoVolume = 0.1f;

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

    // ---------------- volumes ----------------
    public static void AlternarMusica()
    {
        if (MusicaLigada)
        {
            PlayerPrefs.SetFloat(ChaveDaMusicaAntes, Musica.Volume);
            Musica.Volume = 0f;
        }
        else
        {
            Musica.Volume = Mathf.Max(0.1f, PlayerPrefs.GetFloat(ChaveDaMusicaAntes, 0.45f));
        }

        PlayerPrefs.Save();
    }

    public static void AlternarEfeitos()
    {
        if (EfeitosLigados)
        {
            PlayerPrefs.SetFloat(ChaveDosEfeitosAntes, Sons.Volume);
            Sons.Volume = 0f;
        }
        else
        {
            Sons.Volume = Mathf.Max(0.1f, PlayerPrefs.GetFloat(ChaveDosEfeitosAntes, 0.7f));
            Sons.Tocar(Som.Menu);
        }

        PlayerPrefs.Save();
    }

    /// <summary>Sobe ou desce a musica um <see cref="PassoDoVolume"/> (passo = +1 ou -1).</summary>
    public static void MudarMusica(int passo)
    {
        Musica.Volume = Passo(Musica.Volume, passo);

        // Ligar de novo com M volta pro ultimo nivel escolhido aqui.
        if (Musica.Volume > 0f)
            PlayerPrefs.SetFloat(ChaveDaMusicaAntes, Musica.Volume);

        PlayerPrefs.Save();
    }

    public static void MudarEfeitos(int passo)
    {
        Sons.Volume = Passo(Sons.Volume, passo);

        if (Sons.Volume > 0f)
            PlayerPrefs.SetFloat(ChaveDosEfeitosAntes, Sons.Volume);

        PlayerPrefs.Save();
    }

    private static float Passo(float atual, int passo)
    {
        // Arredonda pro passo mais perto antes, pra 0.45 virar 0.5 / 0.4 e nao 0.55.
        float degraus = Mathf.Round(atual / PassoDoVolume) + passo;
        return Mathf.Clamp01(degraus * PassoDoVolume);
    }

    // ---------------- tela ----------------
    public static bool TelaCheia => PlayerPrefs.HasKey(ChaveDaTelaCheia)
        ? PlayerPrefs.GetInt(ChaveDaTelaCheia) == 1
        : Screen.fullScreen;

    /// <summary>A resolucao escolhida (ou a da janela, se nunca escolheu).</summary>
    public static Vector2Int Resolucao => PlayerPrefs.HasKey(ChaveDaLargura)
        ? new Vector2Int(PlayerPrefs.GetInt(ChaveDaLargura), PlayerPrefs.GetInt(ChaveDaAltura))
        : new Vector2Int(Screen.width, Screen.height);

    /// <summary>As resolucoes do monitor, sem repetir (cada uma vem uma vez por taxa de quadros).</summary>
    public static List<Vector2Int> Resolucoes()
    {
        List<Vector2Int> lista = new List<Vector2Int>();

        foreach (Resolution r in Screen.resolutions)
        {
            Vector2Int tamanho = new Vector2Int(r.width, r.height);

            if (tamanho.x >= 800 && tamanho.y >= 600 && !lista.Contains(tamanho))
                lista.Add(tamanho);
        }

        // No editor a lista pode vir vazia: umas comuns pra menu nao ficar sem nada.
        if (lista.Count == 0)
            lista.AddRange(new[] { new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080) });

        lista.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        return lista;
    }

    public static void AlternarTelaCheia()
    {
        PlayerPrefs.SetInt(ChaveDaTelaCheia, TelaCheia ? 0 : 1);
        PlayerPrefs.Save();
        AplicarTela();
    }

    /// <summary>Anda na lista de <see cref="Resolucoes"/> a partir da atual (passo = +1 ou -1).</summary>
    public static void MudarResolucao(int passo)
    {
        List<Vector2Int> lista = Resolucoes();
        int indice = Mathf.Clamp(MaisPerto(lista, Resolucao) + passo, 0, lista.Count - 1);

        PlayerPrefs.SetInt(ChaveDaLargura, lista[indice].x);
        PlayerPrefs.SetInt(ChaveDaAltura, lista[indice].y);
        PlayerPrefs.Save();
        AplicarTela();
    }

    private static int MaisPerto(List<Vector2Int> lista, Vector2Int alvo)
    {
        int melhor = 0;

        for (int i = 1; i < lista.Count; i++)
        {
            if (Mathf.Abs(lista[i].x * lista[i].y - alvo.x * alvo.y) < Mathf.Abs(lista[melhor].x * lista[melhor].y - alvo.x * alvo.y))
                melhor = i;
        }

        return melhor;
    }

    private static void AplicarTela()
    {
        Vector2Int tamanho = Resolucao;
        Screen.SetResolution(tamanho.x, tamanho.y, TelaCheia ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    // O .exe abre com o que foi escolhido da ultima vez. No editor a janela do Game manda.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AplicarAoAbrir()
    {
        if (!Application.isEditor && (PlayerPrefs.HasKey(ChaveDaTelaCheia) || PlayerPrefs.HasKey(ChaveDaLargura)))
            AplicarTela();
    }
}
