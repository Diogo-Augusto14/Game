using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A tela de progresso, aberta pelo menu inicial: tres abas com o que fica salvo entre
/// partidas (<see cref="Registro"/>, <see cref="Conquistas"/>, <see cref="Bestiario"/>).
///
///   A/D ou Esquerda/Direita  troca a aba       W/S ou Cima/Baixo  troca a pagina
///   Esc                      volta
///
/// No controle: cruz, B volta. Enquanto esta aberta, o menu de baixo ignora as teclas
/// (<see cref="Ocupada"/>).
/// </summary>
[DisallowMultipleComponent]
public class TelaDeProgresso : MonoBehaviour
{
    private enum Aba { Estatisticas, Conquistas, Bestiario }

    private static readonly string[] NomesDasAbas = { "Estatísticas", "Conquistas", "Bestiário" };

    private const int LinhasPorPagina = 9;
    private const float AlturaDaLinha = 50f;
    private const float YDaPrimeira = 175f;

    private static readonly Color CorNormal = new Color(0.75f, 0.72f, 0.75f);
    private static readonly Color CorEscolhida = new Color(1f, 0.85f, 0.35f);
    private static readonly Color CorDoTexto = new Color(1f, 0.97f, 0.92f);
    private static readonly Color CorApagada = new Color(0.5f, 0.47f, 0.5f);

    private static TelaDeProgresso atual;
    private static int quadroQueFechou = -1;

    private Action aoFechar;
    private Aba aba;
    private int pagina;
    private int quadroQueAbriu;

    private readonly Text[] abas = new Text[3];
    private readonly Text[] esquerda = new Text[LinhasPorPagina];
    private readonly Text[] direita = new Text[LinhasPorPagina];
    private Text rodapeDaPagina;

    public static bool Aberta => atual != null;

    public static bool Ocupada => atual != null || Time.frameCount == quadroQueFechou;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar()
    {
        atual = null;
        quadroQueFechou = -1;
    }

    public static void Abrir(Action aoFechar = null)
    {
        if (atual != null)
            return;

        TelaDeProgresso tela = new GameObject("Progresso").AddComponent<TelaDeProgresso>();
        tela.aoFechar = aoFechar;
        tela.Montar();
    }

    private void Montar()
    {
        atual = this;
        quadroQueAbriu = Time.frameCount;

        TelaSimples.Montar(gameObject, 120, new Color(0.03f, 0.02f, 0.03f, 1f));
        TelaSimples.Titulo(transform, "Titulo", 100, new Color(1f, 0.95f, 0.85f), 400f, "Progresso");

        for (int i = 0; i < abas.Length; i++)
        {
            abas[i] = TelaSimples.Texto(transform, "Aba " + NomesDasAbas[i], 40, CorNormal, 280f, NomesDasAbas[i]);
            RectTransform rt = abas[i].rectTransform;
            rt.sizeDelta = new Vector2(420f, 60f);
            rt.anchoredPosition = new Vector2((i - 1) * 440f, 280f);
        }

        for (int i = 0; i < LinhasPorPagina; i++)
        {
            float y = YDaPrimeira - i * AlturaDaLinha;
            esquerda[i] = Linha("Esquerda " + i, -620f, y, 560f, TextAnchor.MiddleLeft, 30);
            direita[i] = Linha("Direita " + i, -40f, y, 680f, TextAnchor.MiddleLeft, 24);
        }

        rodapeDaPagina = TelaSimples.Texto(transform, "Pagina", 24, CorApagada, -290f, "");

        TelaSimples.LinhaDeTeclas(transform, "Teclas", -370f,
            "[A][D] aba | [W][S] página | [Esc] voltar || " +
            "[Pad CruzEsquerda][Pad CruzDireita] aba | [Pad CruzCima][Pad CruzBaixo] página | [Pad B] voltar", 26, new Color(0.92f, 0.92f, 0.95f));

        TelaSimples.Painel(transform, "Painel", ArteDaInterface.MolduraGrande, -40f, new Vector2(1440f, 560f));
        TelaSimples.Faixa(transform, "Faixa", ArteDaInterface.FaixaRosa, 400f, 760f);

        Atualizar();
        Sons.Tocar(Som.MenuAbrir, 1f, 0f);
    }

    private Text Linha(string nome, float x, float y, float largura, TextAnchor alinhamento, int tamanho)
    {
        Text t = TelaSimples.Texto(transform, nome, tamanho, CorDoTexto, y, "");
        RectTransform rt = t.rectTransform;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(largura, AlturaDaLinha);
        t.alignment = alinhamento;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize = 16;
        t.resizeTextMaxSize = tamanho;
        return t;
    }

    private void Update()
    {
        if (Time.frameCount == quadroQueAbriu)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) || Controle.Apertou(BotaoDoControle.B)
            || Controle.Apertou(BotaoDoControle.Start) || Controle.Apertou(BotaoDoControle.Y))
        {
            Fechar();
            return;
        }

        if (Apertou(KeyCode.LeftArrow, KeyCode.A) || Controle.Apertou(BotaoDoControle.CruzEsquerda))
            TrocarAba(-1);
        else if (Apertou(KeyCode.RightArrow, KeyCode.D) || Controle.Apertou(BotaoDoControle.CruzDireita))
            TrocarAba(1);
        else if (Apertou(KeyCode.UpArrow, KeyCode.W) || Controle.Apertou(BotaoDoControle.CruzCima))
            TrocarPagina(-1);
        else if (Apertou(KeyCode.DownArrow, KeyCode.S) || Controle.Apertou(BotaoDoControle.CruzBaixo))
            TrocarPagina(1);
    }

    private static bool Apertou(KeyCode a, KeyCode b) => Input.GetKeyDown(a) || Input.GetKeyDown(b);

    private void TrocarAba(int passo)
    {
        aba = (Aba)(((int)aba + passo + 3) % 3);
        pagina = 0;
        Sons.Tocar(Som.Menu, 0.6f);
        Atualizar();
    }

    private void TrocarPagina(int passo)
    {
        int paginas = Paginas(Linhas().Count);
        int nova = Mathf.Clamp(pagina + passo, 0, paginas - 1);

        if (nova == pagina)
            return;

        pagina = nova;
        Sons.Tocar(Som.Menu, 0.6f);
        Atualizar();
    }

    private static int Paginas(int linhas) => Mathf.Max(1, (linhas + LinhasPorPagina - 1) / LinhasPorPagina);

    private void Fechar()
    {
        Sons.Tocar(Som.MenuFechar, 1f, 0f);
        quadroQueFechou = Time.frameCount;
        Destroy(gameObject);
        aoFechar?.Invoke();
    }

    private void OnDestroy()
    {
        if (atual == this)
            atual = null;
    }

    // ---------------- conteudo ----------------
    private readonly struct LinhaDaTela
    {
        public readonly string Esquerda;
        public readonly string Direita;
        public readonly Color Cor;

        public LinhaDaTela(string esquerda, string direita, Color cor)
        {
            Esquerda = esquerda;
            Direita = direita;
            Cor = cor;
        }
    }

    private void Atualizar()
    {
        for (int i = 0; i < abas.Length; i++)
            abas[i].color = i == (int)aba ? CorEscolhida : CorNormal;

        List<LinhaDaTela> linhas = Linhas();
        int paginas = Paginas(linhas.Count);
        pagina = Mathf.Clamp(pagina, 0, paginas - 1);

        for (int i = 0; i < LinhasPorPagina; i++)
        {
            int indice = pagina * LinhasPorPagina + i;
            bool tem = indice < linhas.Count;
            esquerda[i].text = tem ? linhas[indice].Esquerda : "";
            direita[i].text = tem ? linhas[indice].Direita : "";
            esquerda[i].color = tem ? linhas[indice].Cor : CorDoTexto;
        }

        rodapeDaPagina.text = paginas > 1 ? $"Página {pagina + 1} de {paginas}" : "";
    }

    private List<LinhaDaTela> Linhas()
    {
        switch (aba)
        {
            case Aba.Conquistas: return LinhasDasConquistas();
            case Aba.Bestiario: return LinhasDoBestiario();
            default: return LinhasDasEstatisticas();
        }
    }

    private static List<LinhaDaTela> LinhasDasEstatisticas()
    {
        int maisLonge = Registro.MaisLonge;
        TipoDeInimigo? carrasco = Registro.Carrasco;

        return new List<LinhaDaTela>
        {
            Numero("Partidas", Registro.Partidas.ToString()),
            Numero("Vitórias", Progresso.Vitorias.ToString()),
            Numero("Mortes", Registro.Mortes.ToString()),
            Numero("Mais longe", maisLonge > 0 ? $"Mundo {maisLonge / 10} - Fase {maisLonge % 10}" : "-"),
            Numero("Tempo jogado", Tempo(Registro.TempoJogado)),
            Numero("Inimigos derrotados", Registro.InimigosDerrotados.ToString()),
            Numero("Chefes derrotados", Registro.ChefesDerrotados.ToString()),
            Numero("Salas exploradas", Registro.SalasExploradas.ToString()),
            Numero("Itens pegos", Registro.ItensPegos.ToString()),
            Numero("Sinergias formadas", Registro.SinergiasFormadas.ToString()),
            Numero("Itens ativos usados", Registro.AtivosUsados.ToString()),
            Numero("Inimigos conhecidos", $"{Registro.TiposVistos} de {Bestiario.Todas.Count}"),
            Numero("Conquistas", $"{Conquistas.Quantas} de {Conquistas.Todas.Count}"),
            Numero("Quem mais te matou", carrasco.HasValue
                ? $"{Bestiario.Nome(carrasco.Value)} ({Registro.MortesPor(carrasco.Value)}x)"
                : "ninguém, ainda"),
        };
    }

    private static LinhaDaTela Numero(string nome, string valor) => new LinhaDaTela(nome, valor, CorDoTexto);

    private static string Tempo(float segundos)
    {
        int total = Mathf.FloorToInt(segundos);
        return total >= 3600 ? $"{total / 3600}h {total / 60 % 60:00}min" : $"{total / 60}min {total % 60:00}s";
    }

    private static List<LinhaDaTela> LinhasDasConquistas()
    {
        List<LinhaDaTela> linhas = new List<LinhaDaTela>();

        foreach (Conquistas.Conquista c in Conquistas.Todas)
        {
            bool tem = Conquistas.Tem(c);
            string libera = string.IsNullOrEmpty(c.Libera) ? "" : $"  (libera: {c.Libera})";
            linhas.Add(new LinhaDaTela((tem ? "• " : "  ") + c.Nome, c.ComoGanhar + libera, tem ? CorEscolhida : CorApagada));
        }

        return linhas;
    }

    private static List<LinhaDaTela> LinhasDoBestiario()
    {
        List<LinhaDaTela> linhas = new List<LinhaDaTela>();

        foreach (Bestiario.Ficha f in Bestiario.Todas)
        {
            if (!Registro.JaViu(f.Tipo))
            {
                linhas.Add(new LinhaDaTela("???", f.Chefe ? "Um chefe ainda não enfrentado." : "Ainda não encontrado.", CorApagada));
                continue;
            }

            Color cor = f.Chefe ? new Color(1f, 0.55f, 0.5f) : CorDoTexto;
            linhas.Add(new LinhaDaTela(f.Nome, $"{f.ComoLuta}  Derrotados: {Registro.Derrotados(f.Tipo)}", cor));
        }

        return linhas;
    }
}
