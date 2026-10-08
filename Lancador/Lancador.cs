// Lançador do The Prettie: mostra as versões publicadas, deixa escolher qual jogar, baixa a que
// faltar e guarda dois saves: um do jogo antigo (antes da 1.2.0) e um do jogo novo, que todas as
// versões do jogo novo usam juntas (atualizar não perde nada). Veja LEIAME.md.
// Compila com o csc.exe que já vem no Windows (veja compilar.bat). Sem dependências extras.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

static class Lancador
{
    // Repositório PÚBLICO que guarda só os zips das versões.
    const string Repositorio = "Diogo-Augusto14/ThePrettie-Releases";
    const string ExeDoJogo = "ThePrettie.exe";
    const string NomeDoProcesso = "ThePrettie";
    const string ExeDoLancador = "ThePrettie-Lancador.exe";
    // O lançador antigo não troca a si mesmo; o novo vem no zip com este nome e o jogo faz a troca.
    const string LancadorNovo = "ThePrettie-Lancador-novo.exe";
    const string ArquivoDeVersao = "versao.txt";
    const string PastaDasVersoes = "versoes";
    const string PastaDosSaves = "saves";
    const string SaveEmUso = "em-uso.txt";
    const string SaveAntigo = "antigo.txt";
    const string SaveNovo = "novo.txt";

    // Onde a Unity guarda o save (PlayerPrefs) do jogo no Windows: Software\<Company Name>\<Product Name>.
    const string ChaveDoSave = @"Software\DefaultCompany\The Prettie";

    // O jogo recomeçou do zero nesta versão; as de antes são o jogo antigo (estilo Isaac).
    static readonly Version PrimeiraDoJogoNovo = new Version(1, 2, 0);

    class Versao
    {
        public string Numero;
        public string Notas = "";
        public string Data = "";
        public string Zip;
        public bool Instalada;

        public string Pasta { get { return Path.Combine(Path.Combine(Raiz, PastaDasVersoes), Numero); } }
    }

    static Form janela;
    static ListBox lista;
    static TextBox notas;
    static Button jogar;
    static Label texto;
    static ProgressBar barra;
    static List<Versao> versoes = new List<Versao>();
    static string maisNova;

    static string Raiz { get { return AppDomain.CurrentDomain.BaseDirectory; } }
    static string Saves { get { return Path.Combine(Raiz, PastaDosSaves); } }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        janela = new Form
        {
            Text = "The Prettie",
            Width = 560, Height = 420,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false,
        };
        var titulo = new Label { Left = 15, Top = 12, Width = 520, Height = 20, Text = "Escolha a versão:" };
        lista = new ListBox { Left = 15, Top = 35, Width = 220, Height = 260, IntegralHeight = false };
        notas = new TextBox
        {
            Left = 245, Top = 35, Width = 290, Height = 260,
            Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = SystemColors.Window,
        };
        jogar = new Button { Left = 435, Top = 305, Width = 100, Height = 30, Text = "Jogar", Enabled = false };
        texto = new Label { Left = 15, Top = 305, Width = 410, Height = 30, Text = "Procurando versões..." };
        barra = new ProgressBar { Left = 15, Top = 342, Width = 520, Height = 18, Style = ProgressBarStyle.Marquee };

        lista.SelectedIndexChanged += (s, e) => MostrarNotas();
        lista.DoubleClick += (s, e) => { if (jogar.Enabled) Jogar(); };
        jogar.Click += (s, e) => Jogar();
        janela.AcceptButton = jogar;
        janela.Controls.AddRange(new Control[] { titulo, lista, notas, jogar, texto, barra });
        janela.Shown += (s, e) => new Thread(Preparar) { IsBackground = true }.Start();
        Application.Run(janela);
    }

    // ------------------------------------------------------------ começo
    static void Preparar()
    {
        try
        {
            LimparLancadorVelho();
            Migrar();
            GuardarSavePendente();
        }
        catch (Exception ex)
        {
            Status("Aviso: " + ex.Message, null);
            Thread.Sleep(1500);
        }

        bool online = true;

        try { BaixarLista(); }
        catch (Exception) { online = false; }

        JuntarInstaladas();
        versoes.Sort((a, b) => Comparar(b.Numero, a.Numero));

        Na(() =>
        {
            lista.Items.Clear();
            foreach (var v in versoes)
                lista.Items.Add(Rotulo(v));

            if (versoes.Count > 0)
            {
                int escolhida = Math.Max(0, versoes.FindIndex(v => v.Numero == maisNova));
                lista.SelectedIndex = escolhida;
            }
        });

        if (versoes.Count == 0)
            Pronto("Nenhuma versão encontrada (sem internet?)", false);
        else
            Pronto(online ? "Pronto. A versão de cima é a mais nova." : "Sem internet: só as versões já instaladas.", true);
    }

    static string Rotulo(Versao v)
    {
        string rotulo = v.Numero;

        if (v.Numero == maisNova)
            rotulo += "  (mais nova)";

        rotulo += v.Instalada ? "  - instalada" : "  - baixar";

        if (File.Exists(ArquivoDoSave(v.Numero)))
            rotulo += ", com save";

        return rotulo;
    }

    static void MostrarNotas()
    {
        int i = lista.SelectedIndex;

        if (i < 0 || i >= versoes.Count)
            return;

        var v = versoes[i];
        string texto = "Versão " + v.Numero + (v.Data.Length > 0 ? " (" + v.Data + ")" : "") + "\r\n\r\n";
        texto += v.Notas.Length > 0 ? v.Notas.Replace("\r\n", "\n").Replace("\n", "\r\n") : "Sem notas.";
        notas.Text = texto;
    }

    // ------------------------------------------------------------ jogar
    static void Jogar()
    {
        int i = lista.SelectedIndex;

        if (i < 0 || i >= versoes.Count)
            return;

        var v = versoes[i];
        Habilitar(false);
        new Thread(() => JogarVersao(v)) { IsBackground = true }.Start();
    }

    static void JogarVersao(Versao v)
    {
        try
        {
            if (Process.GetProcessesByName(NomeDoProcesso).Length > 0)
            {
                Pronto("O jogo já está aberto: feche ele antes de abrir outra versão.", true);
                return;
            }

            if (!v.Instalada)
            {
                Instalar(v);
                v.Instalada = true;
            }

            if (v.Numero == maisNova)
                AtualizarLancador(v.Pasta);

            TirarLancadores(v.Pasta);
            TrocarSave(v.Numero);
            File.WriteAllText(Path.Combine(Saves, SaveEmUso), v.Numero);

            Status("Abrindo a versão " + v.Numero + "...", null);
            var jogo = Process.Start(new ProcessStartInfo(Path.Combine(v.Pasta, ExeDoJogo)) { WorkingDirectory = v.Pasta });
            Na(() => janela.Hide());
            jogo.WaitForExit();

            GuardarSave(v.Numero);
            File.Delete(Path.Combine(Saves, SaveEmUso));
            Na(() =>
            {
                lista.Items[versoes.IndexOf(v)] = Rotulo(v);
                janela.Show();
                janela.Activate();
            });
            Pronto("Save guardado (vale pra todas as versões do " + (Comparar(v.Numero, PrimeiraDoJogoNovo.ToString()) < 0 ? "jogo antigo" : "jogo novo") + ").", true);
        }
        catch (Exception ex)
        {
            Na(() => janela.Show());
            Pronto("Não deu: " + ex.Message, true);
        }
    }

    // ------------------------------------------------------------ versões
    static void BaixarLista()
    {
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
        string json;

        using (var web = NovoCliente())
            json = web.DownloadString(UrlDasVersoes());

        var leitor = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        object lido = leitor.DeserializeObject(json);
        var lancamentos = lido as object[] ?? new object[] { lido };

        foreach (var item in lancamentos)
        {
            var r = item as Dictionary<string, object>;

            if (r == null || Verdade(r, "draft") || Verdade(r, "prerelease"))
                continue;

            string tag = Texto(r, "tag_name");
            string zip = null;
            var anexos = r.ContainsKey("assets") ? r["assets"] as object[] : null;

            if (anexos != null)
            {
                foreach (var a in anexos.OfType<Dictionary<string, object>>())
                {
                    string url = Texto(a, "browser_download_url");

                    if (url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        zip = url;
                        break;
                    }
                }
            }

            if (tag.Length == 0 || zip == null)
                continue;

            var v = new Versao { Numero = tag.TrimStart('v', 'V'), Notas = Texto(r, "body"), Zip = zip };
            string data = Texto(r, "published_at");
            DateTime quando;

            if (DateTime.TryParse(data, out quando))
                v.Data = quando.ToString("dd/MM/yyyy");

            if (!versoes.Any(x => x.Numero == v.Numero))
                versoes.Add(v);

            if (maisNova == null || Comparar(v.Numero, maisNova) > 0)
                maisNova = v.Numero;
        }
    }

    // As que estão na pasta de versões contam mesmo sem internet.
    static void JuntarInstaladas()
    {
        string pasta = Path.Combine(Raiz, PastaDasVersoes);

        if (!Directory.Exists(pasta))
            return;

        foreach (string dir in Directory.GetDirectories(pasta))
        {
            if (!File.Exists(Path.Combine(dir, ExeDoJogo)))
                continue;

            string numero = Path.GetFileName(dir);
            var v = versoes.FirstOrDefault(x => x.Numero == numero);

            if (v == null)
            {
                v = new Versao { Numero = numero };
                versoes.Add(v);
            }

            v.Instalada = true;
        }

        if (maisNova == null)
        {
            foreach (var v in versoes)
            {
                if (maisNova == null || Comparar(v.Numero, maisNova) > 0)
                    maisNova = v.Numero;
            }
        }
    }

    static void Instalar(Versao v)
    {
        if (v.Zip == null)
            throw new InvalidOperationException("versão sem zip");

        string zip = Path.Combine(Path.GetTempPath(), "ThePrettie-" + v.Numero + ".zip");
        string extraido = Path.Combine(Path.GetTempPath(), "ThePrettie-" + v.Numero);

        try
        {
            Status("Baixando a versão " + v.Numero + "...", 0);

            using (var web = NovoCliente())
            {
                web.DownloadProgressChanged += (s, e) => Status("Baixando a versão " + v.Numero + "... " + e.ProgressPercentage + "%", e.ProgressPercentage);
                var pronto = new ManualResetEvent(false);
                Exception erro = null;
                web.DownloadFileCompleted += (s, e) => { erro = e.Error; pronto.Set(); };
                web.DownloadFileAsync(new Uri(v.Zip), zip);
                pronto.WaitOne();

                if (erro != null)
                    throw erro;
            }

            Status("Instalando a versão " + v.Numero + "...", null);

            if (Directory.Exists(extraido))
                Directory.Delete(extraido, true);

            ZipFile.ExtractToDirectory(zip, extraido);

            // Se o zip veio com uma pasta única por fora, entra nela.
            string origem = extraido;
            var subs = Directory.GetDirectories(extraido);

            if (Directory.GetFiles(extraido).Length == 0 && subs.Length == 1)
                origem = subs[0];

            if (Directory.Exists(v.Pasta))
                Directory.Delete(v.Pasta, true);

            Copiar(origem, v.Pasta);
            File.WriteAllText(Path.Combine(v.Pasta, ArquivoDeVersao), v.Numero);
        }
        finally
        {
            try { File.Delete(zip); } catch { }
            try { if (Directory.Exists(extraido)) Directory.Delete(extraido, true); } catch { }
        }
    }

    // ------------------------------------------------------------ o próprio lançador
    // A versão mais nova traz o lançador mais novo: se for diferente deste, troca (vale na próxima vez).
    // Um .exe aberto não pode ser apagado, mas pode mudar de nome.
    static void AtualizarLancador(string pastaDaVersao)
    {
        string novo = Path.Combine(pastaDaVersao, ExeDoLancador);
        string atual = Application.ExecutablePath;

        if (!File.Exists(novo) || File.ReadAllBytes(novo).SequenceEqual(File.ReadAllBytes(atual)))
            return;

        string velho = atual + ".old";

        try
        {
            if (File.Exists(velho))
                File.Delete(velho);

            File.Move(atual, velho);
            File.Copy(novo, atual);
        }
        catch (Exception)
        {
            // Não deu pra trocar agora: fica pra próxima.
        }
    }

    static void LimparLancadorVelho()
    {
        try { File.Delete(Application.ExecutablePath + ".old"); } catch { }
        try { File.Delete(Path.Combine(Raiz, LancadorNovo)); } catch { }
    }

    // As pastas das versões não precisam de lançador (o daqui de fora cuida de tudo).
    static void TirarLancadores(string pasta)
    {
        try { File.Delete(Path.Combine(pasta, ExeDoLancador)); } catch { }
        try { File.Delete(Path.Combine(pasta, LancadorNovo)); } catch { }
    }

    // ------------------------------------------------------------ a primeira vez do lançador novo
    // O lançador antigo instalava o jogo aqui mesmo, ao lado dele. Agora cada versão tem a sua pasta:
    // a instalação antiga vai pra versoes\<número>, e o save que já existe fica guardado pra ela (e
    // como "antigo": as versões do jogo antigo, que nunca rodaram por este lançador, começam com ele).
    static void Migrar()
    {
        Directory.CreateDirectory(Saves);
        string exe = Path.Combine(Raiz, ExeDoJogo);

        if (!File.Exists(exe))
            return;

        string numero = "0.0.0";

        try { numero = File.ReadAllText(Path.Combine(Raiz, ArquivoDeVersao)).Trim(); }
        catch { }

        Status("Organizando as versões (só desta vez)...", null);
        var save = LerRegistro();

        if (!File.Exists(Path.Combine(Saves, SaveAntigo)))
            EscreverSave(Path.Combine(Saves, SaveAntigo), save);

        if (!File.Exists(ArquivoDoSave(numero)))
            EscreverSave(ArquivoDoSave(numero), save);

        string destino = Path.Combine(Path.Combine(Raiz, PastaDasVersoes), numero);

        if (Directory.Exists(destino))
            Directory.Delete(destino, true);

        Directory.CreateDirectory(destino);
        string eu = Path.GetFileName(Application.ExecutablePath);

        foreach (string arquivo in Directory.GetFiles(Raiz))
        {
            string nome = Path.GetFileName(arquivo);

            if (nome.Equals(eu, StringComparison.OrdinalIgnoreCase) || nome.StartsWith("ThePrettie-Lancador", StringComparison.OrdinalIgnoreCase))
                continue;

            File.Move(arquivo, Path.Combine(destino, nome));
        }

        foreach (string pasta in Directory.GetDirectories(Raiz))
        {
            string nome = Path.GetFileName(pasta);

            if (nome.Equals(PastaDasVersoes, StringComparison.OrdinalIgnoreCase) || nome.Equals(PastaDosSaves, StringComparison.OrdinalIgnoreCase))
                continue;

            Directory.Move(pasta, Path.Combine(destino, nome));
        }
    }

    // ------------------------------------------------------------ saves
    // O lançador fechou com o jogo aberto (ou caiu): o que está no registro é daquela versão.
    static void GuardarSavePendente()
    {
        string marca = Path.Combine(Saves, SaveEmUso);

        if (!File.Exists(marca) || Process.GetProcessesByName(NomeDoProcesso).Length > 0)
            return;

        GuardarSave(File.ReadAllText(marca).Trim());
        File.Delete(marca);
    }

    // Um save pro jogo antigo e um pro jogo novo: as versões de cada um dividem o mesmo.
    static string ArquivoDoSave(string numero)
    {
        bool antigo = Comparar(numero, PrimeiraDoJogoNovo.ToString()) < 0;
        string arquivo = Path.Combine(Saves, antigo ? SaveAntigo : SaveNovo);

        if (!antigo && !File.Exists(arquivo))
            JuntarSavesDoJogoNovo(arquivo);

        return arquivo;
    }

    // Antes cada versão tinha o seu save (<versão>.txt): o save do jogo novo começa com o maior deles
    // (o que guardou mais coisa: herois liberados, conquistas, configurações), pra ninguém perder o que
    // já tinha. Empate: o mais recente.
    static void JuntarSavesDoJogoNovo(string destino)
    {
        if (!Directory.Exists(Saves))
            return;

        string melhor = null;
        long maior = -1;
        DateTime quando = DateTime.MinValue;

        foreach (string arquivo in Directory.GetFiles(Saves, "*.txt"))
        {
            Version v;

            if (!Version.TryParse(Path.GetFileNameWithoutExtension(arquivo), out v) || v < PrimeiraDoJogoNovo)
                continue;

            long tamanho = new FileInfo(arquivo).Length;
            DateTime mexido = File.GetLastWriteTimeUtc(arquivo);

            if (tamanho > maior || (tamanho == maior && mexido > quando))
            {
                maior = tamanho;
                quando = mexido;
                melhor = arquivo;
            }
        }

        if (melhor != null)
            File.Copy(melhor, destino, true);
    }

    static void GuardarSave(string numero)
    {
        EscreverSave(ArquivoDoSave(numero), LerRegistro());
    }

    // Põe no registro o save do jogo da versão (o antigo ou o novo). Nunca jogado: começa vazio.
    static void TrocarSave(string numero)
    {
        string arquivo = ArquivoDoSave(numero);
        var valores = File.Exists(arquivo) ? LerSave(arquivo) : new List<Valor>();

        using (var chave = Registry.CurrentUser.CreateSubKey(ChaveDoSave))
        {
            foreach (string nome in chave.GetValueNames())
                chave.DeleteValue(nome, false);

            foreach (var v in valores)
                chave.SetValue(v.Nome, v.Dado(), v.Tipo);
        }
    }

    class Valor
    {
        public string Nome;
        public RegistryValueKind Tipo;
        public byte[] Bytes;

        public object Dado()
        {
            switch (Tipo)
            {
                case RegistryValueKind.DWord: return BitConverter.ToInt32(Bytes, 0);
                case RegistryValueKind.QWord: return BitConverter.ToInt64(Bytes, 0);
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString: return Encoding.UTF8.GetString(Bytes);
                case RegistryValueKind.MultiString: return Encoding.UTF8.GetString(Bytes).Split('\0');
                default: return Bytes;
            }
        }
    }

    static List<Valor> LerRegistro()
    {
        var valores = new List<Valor>();

        using (var chave = Registry.CurrentUser.OpenSubKey(ChaveDoSave))
        {
            if (chave == null)
                return valores;

            foreach (string nome in chave.GetValueNames())
            {
                var tipo = chave.GetValueKind(nome);
                object dado = chave.GetValue(nome, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                byte[] bytes;

                switch (tipo)
                {
                    case RegistryValueKind.DWord: bytes = BitConverter.GetBytes((int)dado); break;
                    case RegistryValueKind.QWord: bytes = BitConverter.GetBytes((long)dado); break;
                    case RegistryValueKind.String:
                    case RegistryValueKind.ExpandString: bytes = Encoding.UTF8.GetBytes((string)dado); break;
                    case RegistryValueKind.MultiString: bytes = Encoding.UTF8.GetBytes(string.Join("\0", (string[])dado)); break;
                    default: bytes = dado as byte[] ?? new byte[0]; tipo = RegistryValueKind.Binary; break;
                }

                valores.Add(new Valor { Nome = nome, Tipo = tipo, Bytes = bytes });
            }
        }

        return valores;
    }

    // Uma linha por valor: nome, tipo e conteúdo (os dois em base64, pra nada quebrar a linha).
    static void EscreverSave(string arquivo, List<Valor> valores)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(arquivo));
        var linhas = valores.Select(v => Convert.ToBase64String(Encoding.UTF8.GetBytes(v.Nome)) + "\t" + (int)v.Tipo + "\t" + Convert.ToBase64String(v.Bytes));
        File.WriteAllLines(arquivo, linhas.ToArray());
    }

    static List<Valor> LerSave(string arquivo)
    {
        var valores = new List<Valor>();

        foreach (string linha in File.ReadAllLines(arquivo))
        {
            var partes = linha.Split('\t');

            if (partes.Length != 3)
                continue;

            valores.Add(new Valor
            {
                Nome = Encoding.UTF8.GetString(Convert.FromBase64String(partes[0])),
                Tipo = (RegistryValueKind)int.Parse(partes[1]),
                Bytes = Convert.FromBase64String(partes[2]),
            });
        }

        return valores;
    }

    // ------------------------------------------------------------ ajudas
    // Só para testes: a variável de ambiente PRETTIE_URL_TESTE troca a origem (ex.: servidor local),
    // com um JSON no formato da API do GitHub (uma lista de releases, ou uma só).
    static string UrlDasVersoes()
    {
        string teste = Environment.GetEnvironmentVariable("PRETTIE_URL_TESTE");

        if (!string.IsNullOrEmpty(teste))
            return teste;

        return "https://api.github.com/repos/" + Repositorio + "/releases?per_page=100";
    }

    static WebClient NovoCliente()
    {
        var web = new WebClient { Encoding = Encoding.UTF8 };
        web.Headers[HttpRequestHeader.UserAgent] = "ThePrettie-Lancador";
        return web;
    }

    static string Texto(Dictionary<string, object> d, string chave)
    {
        object v;
        return d.TryGetValue(chave, out v) && v != null ? v.ToString() : "";
    }

    static bool Verdade(Dictionary<string, object> d, string chave)
    {
        object v;
        return d.TryGetValue(chave, out v) && v is bool && (bool)v;
    }

    static int Comparar(string a, string b)
    {
        try { return new Version(Normalizar(a)).CompareTo(new Version(Normalizar(b))); }
        catch { return string.CompareOrdinal(a, b); }
    }

    static string Normalizar(string v)
    {
        return v.Split('.').Length == 1 ? v + ".0" : v;
    }

    static void Copiar(string origem, string destino)
    {
        foreach (string arq in Directory.GetFiles(origem, "*", SearchOption.AllDirectories))
        {
            string rel = arq.Substring(origem.Length).TrimStart('\\', '/');
            string alvo = Path.Combine(destino, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(alvo));
            File.Copy(arq, alvo, true);
        }
    }

    static void Na(Action acao)
    {
        try { janela.Invoke(acao); }
        catch { }
    }

    static void Habilitar(bool sim)
    {
        Na(() =>
        {
            jogar.Enabled = sim && versoes.Count > 0;
            lista.Enabled = sim;
        });
    }

    static void Pronto(string msg, bool habilitar)
    {
        Na(() =>
        {
            texto.Text = msg;
            barra.Style = ProgressBarStyle.Continuous;
            barra.Value = 0;
        });
        Habilitar(habilitar);
    }

    static void Status(string msg, int? porcento)
    {
        Na(() =>
        {
            texto.Text = msg;

            if (porcento.HasValue)
            {
                barra.Style = ProgressBarStyle.Continuous;
                barra.Value = Math.Max(0, Math.Min(100, porcento.Value));
            }
            else
            {
                barra.Style = ProgressBarStyle.Marquee;
            }
        });
    }
}
