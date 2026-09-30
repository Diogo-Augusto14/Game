using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Esc pausa o jogo: congela tudo e abre o menu de pausa com o andar, os itens pegos e os
/// botoes Continuar, Reiniciar partida, Configuracoes, Menu principal e Sair do jogo.
///
///   W/S ou Cima/Baixo  escolhe        Enter  aperta        mouse  escolhe e clica
///   atalhos: Esc continuar   R reiniciar   O configuracoes   Q menu
///            M musica        N efeitos
///
/// No controle: cruz escolhe, A aperta; Start (ou B) continua, Select reinicia, X
/// configuracoes, Y menu, LB e RB o som.
///
/// Reiniciar comeca uma partida nova do andar 1 com o mesmo heroi (a partida e a fase do
/// roguelike: morrer ou reiniciar volta pro comeco).
///
/// Fica no mesmo objeto do <see cref="Andar"/> (ele poe sozinho). Nao abre por cima do
/// menu inicial nem da tela de fim de jogo. M e N tambem funcionam jogando, sem pausar.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Andar))]
public class TelaDePausa : MonoBehaviour
{
    private const float TempoDeAbrir = 0.18f;

    private Andar andar;
    private GameObject tela;
    private CanvasGroup grupo;
    private CanvasGroup painel;
    private MenuDeBotoes menu;
    private RectTransform opcoes;
    private Text resumo;
    private Text itens;
    private float abriu;

    public bool Pausado => tela != null;

    private void Awake()
    {
        andar = GetComponent<Andar>();
    }

    private void Update()
    {
        if (TelaDeInicio.Aberta || TelaDeFimDeJogo.Atual != null)
            return;

        if (!Pausado)
        {
            if (TelaDeOpcoes.Ocupada)
                return;

            Opcoes.LerTeclas();

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) || Controle.Apertou(BotaoDoControle.Start))
                Pausar();

            return;
        }

        Animar();
        menu.Ligado = !TelaDeOpcoes.Ocupada;

        if (menu.Ligado && !TransicaoDeTela.Ocupada)
            LerAtalhos();

        // O atalho pode ter fechado a pausa (Continuar) neste quadro.
        if (Pausado)
            menu.Atualizar();
    }

    private void LerAtalhos()
    {
        bool mudou = Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.N)
                     || Controle.Apertou(BotaoDoControle.LB) || Controle.Apertou(BotaoDoControle.RB);
        Opcoes.LerTeclas();

        if (mudou)
            Atualizar();

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)
            || Controle.Apertou(BotaoDoControle.Start) || Controle.Apertou(BotaoDoControle.B))
        {
            Continuar();
        }
        else if (Input.GetKeyDown(KeyCode.R) || Controle.Apertou(BotaoDoControle.Select))
        {
            Reiniciar();
        }
        else if (Input.GetKeyDown(KeyCode.O) || Controle.Apertou(BotaoDoControle.X))
        {
            AbrirOpcoes();
        }
        else if (Input.GetKeyDown(KeyCode.Q) || Controle.Apertou(BotaoDoControle.Y))
        {
            IrAoMenu();
        }
    }

    private void OnDisable()
    {
        if (Pausado)
            Fechar();
    }

    public void Pausar()
    {
        tela = new GameObject("Pausa");
        grupo = TelaSimples.Montar(tela, 30, new Color(0f, 0f, 0f, 0.75f));
        abriu = Time.unscaledTime;

        // Tudo que cresce ao abrir fica numa camada so; o fundo escuro so aparece.
        painel = TelaSimples.Camada(tela.transform, "Painel");
        Transform pai = painel.transform;

        // Moldura do Dragon Regalia no meio e a faixa rosa atras do titulo (antes dos textos: ficam atras).
        TelaSimples.Painel(pai, "Moldura", ArteDaInterface.MolduraGrande, -20f, new Vector2(1000f, 720f));
        TelaSimples.Faixa(pai, "Faixa", ArteDaInterface.FaixaRosa, 380f, 760f);
        TelaSimples.Titulo(pai, "Titulo", 120, new Color(1f, 0.95f, 0.85f), 380f, "Pausado");

        resumo = TelaSimples.Texto(pai, "Resumo", 32, Color.white, 262f, "");
        itens = TelaSimples.Texto(pai, "Itens", 24, Color.white, 218f, "");
        itens.rectTransform.sizeDelta = new Vector2(880f, 64f);

        menu = new MenuDeBotoes(pai, 0f, new Vector2(520f, 76f), 34) { IntervaloDaEntrada = 0.03f };
        menu.Adicionar("Continuar", 140f, Continuar, "[Esc] || [Pad Start]");
        menu.Adicionar("Reiniciar partida", 52f, Reiniciar, "[R] || [Pad Select]");
        menu.Adicionar("Configurações", -36f, AbrirOpcoes, "[O] || [Pad X]", IconeDoBotao.Configuracoes);
        menu.Adicionar("Menu principal", -124f, IrAoMenu, "[Q] || [Pad Y]");
        menu.Adicionar("Sair do jogo", -212f, TelaDeInicio.SairDoJogo, null, IconeDoBotao.Sair, perigo: true);

        opcoes = TelaSimples.LinhaDeTeclas(pai, "Som", -300f, "", 26, new Color(1f, 0.85f, 0.4f));
        TelaSimples.LinhaDeTeclas(tela.transform, "Navegar", -470f,
            "[W][S] escolher | [Enter] confirmar | mouse também funciona || [Pad CruzCima][Pad CruzBaixo] escolher | [Pad A] confirmar",
            24, new Color(0.85f, 0.85f, 0.9f));

        Atualizar();
        Animar();

        Time.timeScale = 0f;
        TelaSimples.TravarJogador(andar.Jogador, true);
        Sons.Tocar(Som.MenuAbrir, 1f, 0f);
    }

    public void Continuar()
    {
        Fechar();
        Sons.Tocar(Som.MenuFechar, 1f, 0f);
    }

    private void Fechar()
    {
        if (tela != null)
            Destroy(tela);

        tela = null;
        menu = null;
        Time.timeScale = 1f;
        TelaSimples.TravarJogador(andar.Jogador, false);
    }

    private static void Reiniciar()
    {
        Sons.Tocar(Som.MenuConfirmar, 1f, 0f);
        TransicaoDeTela.Trocar(TelaDeFimDeJogo.RecarregarCena);
    }

    private void AbrirOpcoes() => TelaDeOpcoes.Abrir(Atualizar);

    private static void IrAoMenu()
    {
        Sons.Tocar(Som.MenuFechar, 1f, 0f);
        TelaDeInicio.VoltarAoMenu();
    }

    /// <summary>Abrindo: o fundo escurece e o painel cresce de 90% pro tamanho certo.</summary>
    private void Animar()
    {
        float t = (Time.unscaledTime - abriu) / TempoDeAbrir;
        grupo.alpha = Mathf.Clamp01(t);
        painel.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, TelaSimples.PassaEVolta(t));
    }

    private void Atualizar()
    {
        if (tela == null)
            return;

        resumo.text = $"{andar.NomeDaFase} de {andar.QuantidadeDeMundos} mundos: {andar.Tema.Nome}{(andar.UltimoAndar ? " (última fase)" : "")}";
        EstatisticasDoJogador estatisticas = andar.Jogador != null ? andar.Jogador.GetComponent<EstatisticasDoJogador>() : null;
        TelaDeFimDeJogo.EncaixarItens(itens, estatisticas, "", "<color=#aaaaaa>Nenhum item ainda</color>", 2);
        TelaSimples.TrocarLinhaDeTeclas(opcoes,
            $"[M] música: {Musica()} | [N] efeitos: {Efeitos()} || [Pad LB] música: {Musica()} | [Pad RB] efeitos: {Efeitos()}",
            26, new Color(1f, 0.85f, 0.4f));
    }

    private static string Musica() => Opcoes.MusicaLigada ? "ligada" : "desligada";

    private static string Efeitos() => Opcoes.EfeitosLigados ? "ligados" : "desligados";
}
