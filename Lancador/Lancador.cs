// Lançador do The Prettie: verifica se existe versão nova, baixa, atualiza e abre o jogo.
// Compila com o csc.exe que já vem no Windows (veja compilar.bat). Sem dependências extras.
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

static class Lancador
{
    // Repositório PÚBLICO que guarda só os zips das versões.
    const string Repositorio = "Diogo-Augusto14/ThePrettie-Releases";
    const string ExeDoJogo = "ThePrettie.exe";
    const string ExeDoLancador = "ThePrettie-Lancador.exe";
    const string ArquivoDeVersao = "versao.txt";

    static Form janela;
    static Label texto;
    static ProgressBar barra;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        janela = new Form
        {
            Text = "The Prettie",
            Width = 420, Height = 130,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false, MinimizeBox = false, ControlBox = false,
        };
        texto = new Label { Left = 15, Top = 15, Width = 380, Height = 22, Text = "Verificando atualizações..." };
        barra = new ProgressBar { Left = 15, Top = 45, Width = 380, Height = 22, Style = ProgressBarStyle.Marquee };
        janela.Controls.Add(texto);
        janela.Controls.Add(barra);
        janela.Shown += (s, e) => new Thread(Trabalhar) { IsBackground = true }.Start();
        Application.Run(janela);
    }

    static string Pasta { get { return AppDomain.CurrentDomain.BaseDirectory; } }

    static void Trabalhar()
    {
        try { Atualizar(); }
        catch (Exception) { /* sem internet ou erro: abre o jogo mesmo assim */ }
        Status("Abrindo o jogo...", null);
        try
        {
            Process.Start(new ProcessStartInfo(Path.Combine(Pasta, ExeDoJogo)) { WorkingDirectory = Pasta });
        }
        catch (Exception ex)
        {
            janela.Invoke((Action)(() => MessageBox.Show("Não foi possível abrir o jogo: " + ex.Message, "The Prettie")));
        }
        janela.Invoke((Action)(() => Application.Exit()));
    }

    static void Atualizar()
    {
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
        string instalada = LerVersaoInstalada();

        string json;
        using (var web = NovoCliente())
            json = web.DownloadString(UrlDaUltimaVersao());

        string tag = Pegar(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
        string url = Pegar(json, "\"browser_download_url\"\\s*:\\s*\"([^\"]+\\.zip)\"");
        if (tag == null || url == null) return;

        string remota = tag.TrimStart('v', 'V');
        if (!EhMaisNova(remota, instalada)) return;

        string zip = Path.Combine(Path.GetTempPath(), "ThePrettie-" + remota + ".zip");
        string extraido = Path.Combine(Path.GetTempPath(), "ThePrettie-" + remota);
        try
        {
            Status("Baixando versão " + remota + "...", 0);
            using (var web = NovoCliente())
            {
                web.DownloadProgressChanged += (s, e) => Status("Baixando versão " + remota + "... " + e.ProgressPercentage + "%", e.ProgressPercentage);
                var pronto = new ManualResetEvent(false);
                Exception erro = null;
                web.DownloadFileCompleted += (s, e) => { erro = e.Error; pronto.Set(); };
                web.DownloadFileAsync(new Uri(url), zip);
                pronto.WaitOne();
                if (erro != null) throw erro;
            }

            Status("Instalando versão " + remota + "...", null);
            if (Directory.Exists(extraido)) Directory.Delete(extraido, true);
            ZipFile.ExtractToDirectory(zip, extraido);

            // Se o zip veio com uma pasta única por fora, entra nela.
            string raiz = extraido;
            var subs = Directory.GetDirectories(extraido);
            if (Directory.GetFiles(extraido).Length == 0 && subs.Length == 1) raiz = subs[0];

            Copiar(raiz, Pasta);
            File.WriteAllText(Path.Combine(Pasta, ArquivoDeVersao), remota);
        }
        finally
        {
            try { File.Delete(zip); } catch { }
            try { if (Directory.Exists(extraido)) Directory.Delete(extraido, true); } catch { }
        }
    }

    // Só para testes: a variável de ambiente PRETTIE_URL_TESTE troca a origem (ex.: servidor local).
    // O usuário final não tem essa variável definida, então sempre usa o repositório oficial.
    static string UrlDaUltimaVersao()
    {
        string teste = Environment.GetEnvironmentVariable("PRETTIE_URL_TESTE");
        if (!string.IsNullOrEmpty(teste)) return teste;
        return "https://api.github.com/repos/" + Repositorio + "/releases/latest";
    }

    static WebClient NovoCliente()
    {
        var web = new WebClient();
        web.Headers[HttpRequestHeader.UserAgent] = "ThePrettie-Lancador";
        return web;
    }

    static string LerVersaoInstalada()
    {
        try { return File.ReadAllText(Path.Combine(Pasta, ArquivoDeVersao)).Trim(); }
        catch { return "0.0.0"; }
    }

    static bool EhMaisNova(string remota, string instalada)
    {
        try { return new Version(Normalizar(remota)) > new Version(Normalizar(instalada)); }
        catch { return remota != instalada; }
    }

    static string Normalizar(string v)
    {
        var partes = v.Split('.');
        return partes.Length == 1 ? v + ".0" : v;
    }

    static string Pegar(string texto, string padrao)
    {
        var m = Regex.Match(texto, padrao);
        return m.Success ? m.Groups[1].Value : null;
    }

    static void Copiar(string origem, string destino)
    {
        foreach (string arq in Directory.GetFiles(origem, "*", SearchOption.AllDirectories))
        {
            string rel = arq.Substring(origem.Length).TrimStart('\\', '/');
            if (string.Equals(rel, ExeDoLancador, StringComparison.OrdinalIgnoreCase)) continue; // não sobrescreve a si mesmo
            string alvo = Path.Combine(destino, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(alvo));
            File.Copy(arq, alvo, true);
        }
    }

    static void Status(string msg, int? porcento)
    {
        try
        {
            janela.Invoke((Action)(() =>
            {
                texto.Text = msg;
                if (porcento.HasValue) { barra.Style = ProgressBarStyle.Continuous; barra.Value = Math.Max(0, Math.Min(100, porcento.Value)); }
                else barra.Style = ProgressBarStyle.Marquee;
            }));
        }
        catch { }
    }
}
