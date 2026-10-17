using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A primeira partida guiada (1.19.0): dicas curtas que aparecem embaixo da tela na primeira vez que
/// cada coisa acontece (os controles no comeco, a primeira sala que tranca, a primeira parede com poeira,
/// o primeiro cristal, o primeiro campeao...). Cada dica aparece uma vez na vida e fica salva
/// (PlayerPrefs); varias juntas fazem fila. So corre com o jogo andando e sem menu aberto.
///
/// Quem quer mostrar chama <see cref="Mostrar"/> com o nome da dica; o texto troca sozinho entre teclado e
/// controle. Fica no objeto do <see cref="GeradorDoAndar"/>.
/// </summary>
[DisallowMultipleComponent]
public class DicasDaPrimeiraVez : MonoBehaviour
{
    private const string Prefixo = "ThePrettie.dica.";
    private const float NaTela = 4.5f;
    private const float Entrada = 0.3f;

    private static DicasDaPrimeiraVez atual;
    private static readonly Queue<string> fila = new Queue<string>();
    private static readonly HashSet<string> naFila = new HashSet<string>();
    private static readonly HashSet<string> vistas = new HashSet<string>();

    private string mostrando;
    private float desde;
    private bool comecou;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar()
    {
        atual = null;
        fila.Clear();
        naFila.Clear();
        vistas.Clear();
    }

    /// <summary>Poe a dica na fila, se ela nunca apareceu.</summary>
    public static void Mostrar(string dica)
    {
        if (string.IsNullOrEmpty(dica) || naFila.Contains(dica) || vistas.Contains(dica))
            return;

        // Ja vista numa partida antes (fica guardado o que ja leu do PlayerPrefs: pedem toda hora).
        if (PlayerPrefs.GetInt(Prefixo + dica, 0) == 1)
        {
            vistas.Add(dica);
            return;
        }

        naFila.Add(dica);
        fila.Enqueue(dica);
    }

    /// <summary>Esquece as dicas vistas (aparecem de novo).</summary>
    public static void Apagar()
    {
        foreach (string d in new[] { "mover", "especial", "esquiva", "sala", "parede", "cristal", "campeao", "arma", "portal", "descanso", "estatua", "mapa",
                                     "premio", "planta", "acampamento" })
            PlayerPrefs.DeleteKey(Prefixo + d);

        vistas.Clear();
    }

    // O texto de cada dica (teclado ou controle).
    private static string Texto(string dica)
    {
        bool pad = Controle.EmUso;

        switch (dica)
        {
            case "mover": return pad ? "Analógico esquerdo anda, o direito mira. RT atira." : "WASD anda, o mouse mira. Botão esquerdo atira.";
            case "especial": return pad ? "LT: tiro especial da arma. Ele recarrega sozinho." : "Botão direito: tiro especial da arma. Ele recarrega sozinho.";
            case "esquiva": return pad ? "A: esquiva. No meio dela nada te acerta." : "Espaço: esquiva. No meio dela nada te acerta.";
            case "sala": return "A sala trancou: derrote todos para as portas abrirem.";
            case "parede": return "Poeira caindo da parede? Ande contra ela: pode ser uma passagem secreta.";
            case "cristal": return "Cristais ficam guardados mesmo se você morrer. Gaste na estátua dos cristais.";
            case "campeao": return "Um campeão! Mais forte, com um poder a mais, mas o prêmio é melhor.";
            case "arma": return pad ? "B pega a arma. Y troca entre as duas que você carrega." : "E pega a arma. Q troca entre as duas que você carrega.";
            case "portal": return "O portal abriu! As outras salas são opcionais, pelos prêmios.";
            case "descanso": return "Área segura: sem inimigos. Loja, fontes e a estátua dos cristais.";
            case "estatua": return "A estátua troca cristais por melhorias que valem para sempre.";
            case "mapa": return pad ? "Select abre o mapa do andar." : "Tab abre o mapa do andar.";
            case "premio": return "O ícone em cima da porta é o prêmio daquela sala. Escolha por onde ir.";
            case "planta": return "Plantas liberam armas e itens para sempre. Veja em Progresso, na aba Desbloqueios.";
            case "acampamento": return pad ? "O acampamento: B conversa com o pessoal. Os cristais vão na estátua." : "O acampamento: E conversa com o pessoal. Os cristais vão na estátua.";
            default: return "";
        }
    }

    private void Awake() => atual = this;

    private void OnDestroy()
    {
        if (atual == this)
            atual = null;
    }

    private static bool Jogando => Time.timeScale > 0f && !TelaDoJogo.MenuAberto;

    private void Update()
    {
        if (!Jogando)
            return;

        // A primeira vez que o jogo anda (fora do menu): os controles.
        if (!comecou)
        {
            comecou = true;
            Mostrar("mover");
            Mostrar("especial");
            Mostrar("esquiva");
            Mostrar("mapa");
        }

        if (mostrando == null)
        {
            if (fila.Count == 0)
                return;

            mostrando = fila.Dequeue();
            naFila.Remove(mostrando);
            vistas.Add(mostrando);
            desde = 0f;
            PlayerPrefs.SetInt(Prefixo + mostrando, 1);
            PlayerPrefs.Save();
        }

        desde += Time.unscaledDeltaTime;

        if (desde >= NaTela)
            mostrando = null;
    }

    private void OnGUI()
    {
        if (mostrando == null || !Jogando || Event.current.type != EventType.Repaint)
            return;

        // Por cima da HUD.
        GUI.depth = -20;
        int u = Desenho.U;
        float a = Mathf.Min(Mathf.Clamp01(desde / Entrada), Mathf.Clamp01((NaTela - desde) / Entrada));
        string texto = Texto(mostrando);
        GUIStyle estilo = Desenho.Estilo(FonteDoJogo.Texto, 18 * u, TextAnchor.MiddleCenter);
        float largura = Mathf.Min(Screen.width - 40 * u, Desenho.Largura(texto, estilo) + 40 * u);
        float altura = 34 * u;

        // Em cima do bloco da arma da HUD (canto de baixo a direita): mais embaixo, a caixa larga cobria a
        // linha do tiro especial quando o nome dele era comprido.
        float baseDaCaixa = Screen.height - 86 * u;

        if (HudDoJogo.TopoDaArma > 0f)
            baseDaCaixa = Mathf.Min(baseDaCaixa, HudDoJogo.TopoDaArma - 8 * u);

        Rect caixa = new Rect((Screen.width - largura) * 0.5f, baseDaCaixa - altura - (1f - a) * 10 * u, largura, altura);

        Desenho.Cor(caixa, new Color(0.04f, 0.03f, 0.04f, 0.8f * a));
        Desenho.Cor(new Rect(caixa.x, caixa.y, caixa.width, 2 * u), new Color(0.55f, 0.2f, 0.12f, 0.9f * a));
        Desenho.Texto(caixa, texto, estilo, new Color(0.95f, 0.9f, 0.82f, a));
    }
}
