using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// "O que mudou": na primeira vez que o jogo abre numa versao nova, aparece por cima do menu
/// inicial uma tela com as novidades (o <see cref="VersaoDoJogo.Novidades"/>, as mesmas notas
/// da pagina de versoes). Enter, Esc, A ou B fecha; nao aparece de novo nesta versao.
///
/// O texto vem em Markdown simples: "## titulo" vira titulo e "- **Nome:** texto" vira um
/// ponto com o nome em destaque.
/// </summary>
[DisallowMultipleComponent]
public class TelaDeNovidades : MonoBehaviour
{
    private const string ChaveDaVersaoVista = "ThePrettie.versao-vista";

    private static TelaDeNovidades atual;
    private static int quadroQueFechou = -1;
    private int quadroQueAbriu;

    public static bool Ocupada => atual != null || Time.frameCount == quadroQueFechou;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar()
    {
        atual = null;
        quadroQueFechou = -1;
    }

    /// <summary>Abre se a versao instalada e nova pra este computador e tem novidades. Senao, nada.</summary>
    public static void MostrarSeForNova()
    {
        string versao = VersaoDoJogo.Texto;

        if (atual != null || versao == "dev" || PlayerPrefs.GetString(ChaveDaVersaoVista, "") == versao)
            return;

        // Marca ja: se o arquivo nao existir, nao fica procurando toda vez.
        PlayerPrefs.SetString(ChaveDaVersaoVista, versao);
        PlayerPrefs.Save();

        string texto = VersaoDoJogo.Novidades;

        if (string.IsNullOrWhiteSpace(texto))
            return;

        TelaDeNovidades tela = new GameObject("Novidades").AddComponent<TelaDeNovidades>();
        tela.Montar(versao, Converter(texto));
    }

    private void Montar(string versao, string corpo)
    {
        atual = this;
        quadroQueAbriu = Time.frameCount;

        // Fundo opaco: com a cor em espaco linear, 3% de transparencia ja deixava o menu aparecendo.
        TelaSimples.Montar(gameObject, 130, new Color(0.03f, 0.02f, 0.03f, 1f));
        TelaSimples.Titulo(transform, "Titulo", 90, new Color(1f, 0.95f, 0.85f), 400f, "Novidades");
        TelaSimples.Faixa(transform, "Faixa", ArteDaInterface.FaixaRosa, 400f, 760f);
        TelaSimples.Texto(transform, "Versao", 30, new Color(0.8f, 0.7f, 0.75f), 315f, "Versão " + versao.TrimStart('v'));

        // Area do texto bem dentro da moldura (a borda dela tem uns 50 px): sem encostar nem vazar.
        UnityEngine.UI.Text texto = TelaSimples.Texto(transform, "Corpo", 28, new Color(1f, 0.97f, 0.92f), -22f, corpo);
        RectTransform rt = texto.rectTransform;
        rt.sizeDelta = new Vector2(1280f, 450f);
        texto.alignment = TextAnchor.UpperLeft;
        texto.verticalOverflow = VerticalWrapMode.Truncate;
        texto.resizeTextForBestFit = true;
        texto.resizeTextMinSize = 14;
        texto.resizeTextMaxSize = 28;
        texto.lineSpacing = 1.1f;

        TelaSimples.Painel(transform, "Painel", ArteDaInterface.MolduraGrande, -20f, new Vector2(1440f, 620f));
        TelaSimples.LinhaDeTeclas(transform, "Teclas", -375f, "[Enter] continuar || [Pad A] continuar", 28, new Color(0.92f, 0.92f, 0.95f));
        Sons.Tocar(Som.MenuAbrir, 1f, 0f);
    }

    private void Update()
    {
        if (Time.frameCount == quadroQueAbriu)
            return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Escape)
            || Input.GetKeyDown(KeyCode.Space) || Controle.Apertou(BotaoDoControle.A) || Controle.Apertou(BotaoDoControle.B)
            || Controle.Apertou(BotaoDoControle.Start) || Input.GetMouseButtonDown(0))
        {
            Sons.Tocar(Som.MenuFechar, 1f, 0f);
            quadroQueFechou = Time.frameCount;
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (atual == this)
            atual = null;
    }

    /// <summary>Markdown simples das notas pro rich text do Unity.</summary>
    private static string Converter(string markdown)
    {
        StringBuilder saida = new StringBuilder();

        foreach (string bruta in markdown.Replace("\r", "").Split('\n'))
        {
            string linha = bruta.Trim();

            if (linha.Length == 0)
                continue;

            // Titulo da pagina de versoes ("## Novidades"): a tela ja tem o dela.
            if (linha.StartsWith("#"))
            {
                string titulo = linha.TrimStart('#').Trim();

                if (!titulo.Equals("Novidades", System.StringComparison.OrdinalIgnoreCase))
                    saida.Append("<color=#FFD966><b>").Append(titulo).Append("</b></color>\n");

                continue;
            }

            if (linha.StartsWith("- ") || linha.StartsWith("* "))
                linha = "• " + linha.Substring(2);

            linha = Regex.Replace(linha, @"\*\*(.+?)\*\*", "<color=#FFD966>$1</color>");
            saida.Append(linha).Append('\n');
        }

        return saida.ToString().TrimEnd();
    }
}
