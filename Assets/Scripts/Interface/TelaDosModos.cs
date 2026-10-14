using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A escolha do modo de jogo, aberta pelo "Modos de jogo" do menu inicial (<see cref="TelaDeInicio"/>):
///
///   Normal          a partida de sempre, com as melhorias da estatua dos cristais;
///   Modo maldito    liberado ao zerar: inimigos mais rapidos e cheios, premios melhores, cristais em dobro;
///   Desafio do dia  o mesmo mapa pra todo mundo hoje, sem as melhorias; guarda o recorde do dia.
///
/// Escolher um ja comeca a partida (quem abriu recebe o modo). O Normal e o Maldito ficam lembrados: o
/// "Jogar" do menu passa a usar o ultimo dos dois (<see cref="Preferido"/>). Esc ou B volta.
/// </summary>
[DisallowMultipleComponent]
public class TelaDosModos : MonoBehaviour
{
    private const string ChaveDoPreferido = "ThePrettie.modo-preferido";

    private static readonly Color CorDoTexto = new Color(1f, 0.97f, 0.92f);

    private static TelaDosModos atual;
    private static int quadroQueFechou = -1;

    private Action<ModoDeJogo> aoEscolher;
    private MenuDeBotoes menu;
    private Text descricao;
    private int quadroQueAbriu;
    private int botaoNormal, botaoMaldito, botaoDiario;

    public static bool Aberta => atual != null;

    public static bool Ocupada => atual != null || Time.frameCount == quadroQueFechou;

    /// <summary>O modo do "Jogar": o ultimo escolhido entre o normal e o maldito (o maldito so se liberado).</summary>
    public static ModoDeJogo Preferido =>
        PlayerPrefs.GetInt(ChaveDoPreferido, 0) == (int)ModoDeJogo.Maldito && Partida.MalditoLiberado ? ModoDeJogo.Maldito : ModoDeJogo.Normal;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar()
    {
        atual = null;
        quadroQueFechou = -1;
    }

    public static void Abrir(Action<ModoDeJogo> aoEscolher)
    {
        if (atual != null)
            return;

        TelaDosModos tela = new GameObject("Modos de jogo").AddComponent<TelaDosModos>();
        tela.aoEscolher = aoEscolher;
        tela.Montar();
    }

    private void Montar()
    {
        atual = this;
        quadroQueAbriu = Time.frameCount;

        TelaSimples.Montar(gameObject, 120, new Color(0.03f, 0.02f, 0.03f, 1f));
        TelaSimples.Painel(transform, "Painel", ArteDaInterface.MolduraGrande, -40f, new Vector2(1300f, 600f));
        TelaSimples.Faixa(transform, "Faixa", ArteDaInterface.FaixaRosa, 400f, 760f);
        TelaSimples.Titulo(transform, "Titulo", 100, new Color(1f, 0.95f, 0.85f), 400f, "Modos de jogo");

        menu = new MenuDeBotoes(transform, 0f, new Vector2(620f, 66f), 32) { IntervaloDaEntrada = 0.04f };
        botaoNormal = menu.Adicionar("Normal", 180f, () => Escolher(ModoDeJogo.Normal));
        botaoMaldito = menu.Adicionar(Partida.MalditoLiberado ? "Modo maldito" : "Modo maldito (bloqueado)", 100f, () => Escolher(ModoDeJogo.Maldito));
        botaoDiario = menu.Adicionar($"Desafio do dia ({Partida.Hoje % 100:00}/{Partida.Hoje / 100 % 100:00})", 20f, () => Escolher(ModoDeJogo.Diario));
        menu.Adicionar("Voltar", -60f, Fechar, "[Esc] || [Pad B]");
        menu.Apagar(botaoMaldito, !Partida.MalditoLiberado);
        menu.Escolher(Preferido == ModoDeJogo.Maldito ? botaoMaldito : botaoNormal, false);

        descricao = TelaSimples.Texto(transform, "Descricao", 28, CorDoTexto, -200f, "");
        RectTransform rt = descricao.rectTransform;
        rt.sizeDelta = new Vector2(1150f, 150f);
        descricao.horizontalOverflow = HorizontalWrapMode.Wrap;
        descricao.verticalOverflow = VerticalWrapMode.Overflow;

        TelaSimples.LinhaDeTeclas(transform, "Teclas", -420f,
            "[W][S] escolher | [Enter] jogar | [Esc] voltar || [Pad CruzCima][Pad CruzBaixo] escolher | [Pad A] jogar | [Pad B] voltar",
            26, new Color(0.92f, 0.92f, 0.95f));

        Sons.Tocar(Som.MenuAbrir, 1f, 0f);
    }

    private void Update()
    {
        if (Time.frameCount == quadroQueAbriu)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) || Controle.Apertou(BotaoDoControle.B))
        {
            Fechar();
            return;
        }

        menu.Atualizar();

        if (descricao != null && menu != null)
            descricao.text = Descricao(menu.Escolhido);
    }

    private string Descricao(int botao)
    {
        if (botao == botaoNormal)
            return "A partida de sempre. As melhorias da estátua dos cristais valem.";

        if (botao == botaoMaldito)
        {
            return Partida.MalditoLiberado
                ? "Inimigos 25% mais rápidos, salas mais cheias e com uma onda a mais, o dobro de campeões.\n" +
                  "Em troca: mais sorte nos prêmios, armas raras mais cedo e cristais em dobro."
                : "Zere o jogo uma vez para liberar.";
        }

        if (botao == botaoDiario)
        {
            int recorde = Partida.RecordeDoDia(Partida.Hoje);
            string seu = recorde > 0 ? $"Seu recorde de hoje: {recorde} pontos ({Partida.ResumoDoRecorde(Partida.Hoje)})." : "Você ainda não jogou o desafio de hoje.";
            return "O mesmo mapa para todo mundo hoje, sem as melhorias da estátua (o mapa troca todo dia às 21h de Brasília).\n" + seu;
        }

        return "";
    }

    private void Escolher(ModoDeJogo modo)
    {
        if (modo == ModoDeJogo.Maldito && !Partida.MalditoLiberado)
        {
            Sons.Tocar(Som.MenuNegado, 0.8f, 0f);
            return;
        }

        if (modo != ModoDeJogo.Diario)
        {
            PlayerPrefs.SetInt(ChaveDoPreferido, (int)modo);
            PlayerPrefs.Save();
        }

        Action<ModoDeJogo> escolha = aoEscolher;
        quadroQueFechou = Time.frameCount;
        Destroy(gameObject);
        escolha?.Invoke(modo);
    }

    private void Fechar()
    {
        Sons.Tocar(Som.MenuFechar, 1f, 0f);
        quadroQueFechou = Time.frameCount;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (atual == this)
            atual = null;
    }
}
