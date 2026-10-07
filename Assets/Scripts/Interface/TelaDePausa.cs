using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Esc pausa o jogo: congela tudo e abre o menu de pausa com o andar, as armas e os
/// botoes Continuar, Reiniciar partida, Configuracoes, Menu principal e Sair do jogo.
///
///   W/S ou Cima/Baixo  escolhe        Enter  aperta        mouse  escolhe e clica
///   atalhos: Esc continuar   R reiniciar   O configuracoes   Q menu principal
///            M musica        N efeitos
///
/// No controle: cruz escolhe, A aperta; Start (ou B) continua, Select reinicia, X
/// configuracoes, Y menu, LB e RB o som.
///
/// Salvar e sair guarda a partida (o "Continuar" do menu volta pro comeco deste andar) e vai pro menu.
/// Reiniciar comeca uma partida nova do andar 1 com o mesmo heroi. Menu principal volta pro menu
/// inicial sem salvar (o comeco de cada andar ja fica salvo sozinho).
///
/// Fica no mesmo objeto do <see cref="GeradorDoAndar"/> (ele poe sozinho). Nao abre por cima do
/// menu inicial, da tela de fim de jogo, da troca de andar nem com o jogador morto. M e N tambem
/// funcionam jogando, sem pausar. Veio do jogo antigo.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(GeradorDoAndar))]
public class TelaDePausa : MonoBehaviour
{
    private const float TempoDeAbrir = 0.18f;

    private GeradorDoAndar andar;
    private Vida vidaDoJogador;
    private GameObject tela;
    private CanvasGroup grupo;
    private CanvasGroup painel;
    private MenuDeBotoes menu;
    private RectTransform opcoes;
    private Text resumo;
    private Text itens;
    private float abriu;

    public bool Pausado => tela != null;

    /// <summary>A pausa aberta agora (a interface do jogo some enquanto isso).</summary>
    public static bool Aberta { get; private set; }

    private void Awake()
    {
        andar = GetComponent<GeradorDoAndar>();
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

            if (vidaDoJogador == null && andar.Jogador != null)
                vidaDoJogador = andar.Jogador.GetComponent<Vida>();

            bool podePausar = !andar.Trocando && (vidaDoJogador == null || !vidaDoJogador.Morto) && Time.timeScale > 0f;

            if (podePausar && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) || Controle.Apertou(BotaoDoControle.Start)))
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

    private void OnDestroy()
    {
        Aberta = false;
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

        menu = new MenuDeBotoes(pai, 0f, new Vector2(520f, 66f), 32) { IntervaloDaEntrada = 0.03f };
        menu.Adicionar("Continuar", 150f, Continuar, "[Esc] || [Pad Start]");
        menu.Adicionar("Salvar e sair", 76f, SalvarESair);
        menu.Adicionar("Reiniciar partida", 2f, Reiniciar, "[R] || [Pad Select]");
        menu.Adicionar("Configurações", -72f, AbrirOpcoes, "[O] || [Pad X]", IconeDoBotao.Configuracoes);
        menu.Adicionar("Menu principal", -146f, IrAoMenu, "[Q] || [Pad Y]");
        menu.Adicionar("Sair do jogo", -220f, TelaDeInicio.SairDoJogo, null, IconeDoBotao.Sair, perigo: true);

        opcoes = TelaSimples.LinhaDeTeclas(pai, "Som", -310f, "", 26, new Color(1f, 0.85f, 0.4f));
        TelaSimples.LinhaDeTeclas(tela.transform, "Navegar", -470f,
            "[W][S] escolher | [Enter] confirmar | mouse também funciona || [Pad CruzCima][Pad CruzBaixo] escolher | [Pad A] confirmar",
            24, new Color(0.85f, 0.85f, 0.9f));

        Atualizar();
        Animar();

        Time.timeScale = 0f;
        Aberta = true;
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
        Aberta = false;
        Time.timeScale = 1f;
        TelaSimples.TravarJogador(andar.Jogador, false);
    }

    private static void Reiniciar()
    {
        Sons.Tocar(Som.MenuConfirmar, 1f, 0f);
        TransicaoDeTela.Trocar(TelaDeFimDeJogo.RecarregarCena);
    }

    private void AbrirOpcoes() => TelaDeOpcoes.Abrir(Atualizar);

    /// <summary>Salva a partida (o andar recomeca do comeco no "Continuar") e volta pro menu inicial.</summary>
    private void SalvarESair()
    {
        Salvamento.Salvar(andar, andar.Jogador != null ? andar.Jogador.gameObject : null);
        AvisoDeConquista.Mostrar("Partida salva", "No menu, \"Continuar\" volta pro começo deste andar.");
        IrAoMenu();
    }

    /// <summary>Volta pro menu inicial (sem salvar: a partida volta do ultimo andar salvo).</summary>
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

        resumo.text = andar.NomeNaTela + (andar.Andar >= andar.Andares ? " (último andar)" : "");
        ArmaDoJogador armas = andar.Jogador != null ? andar.Jogador.GetComponent<ArmaDoJogador>() : null;
        itens.text = "Armas: " + TelaDeFimDeJogo.ListaDeArmas(armas);
        TelaSimples.TrocarLinhaDeTeclas(opcoes,
            $"[M] música: {Musica()} | [N] efeitos: {Efeitos()} || [Pad LB] música: {Musica()} | [Pad RB] efeitos: {Efeitos()}",
            26, new Color(1f, 0.85f, 0.4f));
    }

    private static string Musica() => Opcoes.MusicaLigada ? "ligada" : "desligada";

    private static string Efeitos() => Opcoes.EfeitosLigados ? "ligados" : "desligados";
}
