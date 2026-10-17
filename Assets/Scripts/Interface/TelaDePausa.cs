using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Esc pausa o jogo: congela tudo e abre o menu de pausa com o andar, as armas e os
/// botoes Continuar, Salvar e sair, Reiniciar partida, Configuracoes e Sair do jogo.
///
///   W/S ou Cima/Baixo  escolhe        Enter  aperta        mouse  escolhe e clica
///   atalhos: Esc continuar   O configuracoes   M musica   N efeitos
///
/// No controle: cruz escolhe, A aperta; Start (ou B) continua, X configuracoes, LB e RB o som.
///
/// Salvar e sair volta pro menu com a partida guardada do jeito que estava ao chegar neste andar (o
/// comeco de cada andar fica salvo sozinho): o "Continuar" recomeca o andar com o que o jogador tinha ao
/// chegar, como no Gungeon. Reiniciar comeca uma partida nova do andar 1 com o mesmo heroi.
///
/// O que abandona a partida (Salvar e sair, Reiniciar, Sair do jogo) pede dois apertos: o primeiro troca o
/// botao por "Aperte de novo pra...", o segundo faz; escolher outro botao (ou esperar) desarma. E nao tem
/// atalho: ate a 1.22.3, R reiniciava e Q ia pro menu na hora, e no jogo o R e o item, o Q troca a arma
/// (no controle, Select abria o mapa e reiniciava aqui; Y trocava a arma e saia).
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

    // Segundos que o botao fica pedindo o segundo aperto.
    private const float TempoParaConfirmar = 3f;

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

    // Os botoes que abandonam a partida e o que esta pedindo o segundo aperto agora (-1 = nenhum).
    private int botaoSalvar, botaoReiniciar, botaoSair;
    private int armado = -1;
    private string rotuloArmado;
    private float armadoEm;

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

            bool podePausar = !andar.Trocando && (vidaDoJogador == null || !vidaDoJogador.Morto) && Time.timeScale > 0f && !Minimapa.Ocupado && !TelaDaEstatua.Ocupada && !TelaDasApostas.Ocupada;

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
        {
            menu.Atualizar();

            // Escolheu outro botao (ou demorou): o que pedia o segundo aperto volta ao normal.
            if (armado >= 0 && (menu.Escolhido != armado || Time.unscaledTime - armadoEm > TempoParaConfirmar))
                Desarmar();
        }
    }

    private void LerAtalhos()
    {
        bool mudou = Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.N)
                     || Controle.Apertou(BotaoDoControle.LB) || Controle.Apertou(BotaoDoControle.RB);
        Opcoes.LerTeclas();

        if (mudou)
            Atualizar();

        // Sem atalho pro que abandona a partida (reiniciar, sair): so pelos botoes, com dois apertos.
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)
            || Controle.Apertou(BotaoDoControle.Start) || Controle.Apertou(BotaoDoControle.B))
        {
            Continuar();
        }
        else if (Input.GetKeyDown(KeyCode.O) || Controle.Apertou(BotaoDoControle.X))
        {
            AbrirOpcoes();
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

        // Sem o "Menu principal" (tinha ate a 1.22.3): o Salvar e sair faz o mesmo (o comeco do andar ja fica salvo).
        menu = new MenuDeBotoes(pai, 0f, new Vector2(520f, 66f), 32) { IntervaloDaEntrada = 0.03f };
        armado = -1;
        menu.Adicionar("Continuar", 113f, Continuar, "[Esc] || [Pad Start]");
        botaoSalvar = menu.Adicionar("Salvar e sair", 39f, PedirSalvarESair);
        botaoReiniciar = menu.Adicionar("Reiniciar partida", -35f, PedirReiniciar);
        menu.Adicionar("Configurações", -109f, AbrirOpcoes, "[O] || [Pad X]", IconeDoBotao.Configuracoes);
        botaoSair = menu.Adicionar("Sair do jogo", -183f, PedirSairDoJogo, null, IconeDoBotao.Sair, perigo: true);

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

    // Os tres botoes que abandonam a partida: o primeiro aperto so pede confirmacao.
    private void PedirSalvarESair() => Confirmar(botaoSalvar, "Salvar e sair", "Aperte de novo pra sair", SalvarESair);

    private void PedirReiniciar() => Confirmar(botaoReiniciar, "Reiniciar partida", "Aperte de novo pra reiniciar", Reiniciar);

    private void PedirSairDoJogo() => Confirmar(botaoSair, "Sair do jogo", "Aperte de novo pra fechar o jogo", TelaDeInicio.SairDoJogo);

    // Primeiro aperto: o botao vira o pedido (e muda de cor) e da um aviso; segundo aperto no mesmo botao: faz.
    private void Confirmar(int botao, string rotulo, string pedido, System.Action acao)
    {
        if (armado == botao)
        {
            Desarmar();
            acao();
            return;
        }

        Desarmar();
        armado = botao;
        rotuloArmado = rotulo;
        armadoEm = Time.unscaledTime;
        menu.TrocarRotulo(botao, pedido);
        menu.Apagar(botao, true);
        Sons.Tocar(Som.MenuNegado, 0.7f, 0f);
    }

    private void Desarmar()
    {
        if (armado >= 0 && menu != null)
        {
            menu.TrocarRotulo(armado, rotuloArmado);
            menu.Apagar(armado, false);
        }

        armado = -1;
    }

    private static void Reiniciar()
    {
        Sons.Tocar(Som.MenuConfirmar, 1f, 0f);
        TransicaoDeTela.Trocar(TelaDeFimDeJogo.RecarregarCena);
    }

    // Trocou o idioma nas configuracoes: a pausa e montada de novo, no idioma novo.
    private void AbrirOpcoes() => TelaDeOpcoes.Abrir(() =>
    {
        if (TelaDeOpcoes.IdiomaMudou)
        {
            Fechar();
            Pausar();
        }
        else
        {
            Atualizar();
        }
    });

    /// <summary>
    /// Volta pro menu inicial com a partida guardada como estava ao chegar neste andar: o comeco de cada
    /// andar ja fica salvo sozinho, e aqui nao grava de novo. Ate a 1.22.3 gravava o meio do andar, e o
    /// "Continuar" montava o andar de novo com premios novos: saindo e voltando, itens, moedas e cristais sem
    /// fim (a fonte e o calice da area segura tambem).
    /// </summary>
    private static void SalvarESair()
    {
        AvisoDeConquista.Mostrar("Partida salva", "\"Continuar\" volta pro começo deste andar, com o que você tinha ao chegar nele.");
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

        resumo.text = andar.NomeNaTela + (andar.Andar >= andar.Andares ? " " + Idioma.T("(último andar)") : "");
        ArmaDoJogador armas = andar.Jogador != null ? andar.Jogador.GetComponent<ArmaDoJogador>() : null;
        itens.text = Idioma.T("Armas:") + " " + TelaDeFimDeJogo.ListaDeArmas(armas);
        TelaSimples.TrocarLinhaDeTeclas(opcoes,
            Idioma.F("[M] música: {0} | [N] efeitos: {1} || [Pad LB] música: {0} | [Pad RB] efeitos: {1}", Musica(), Efeitos()),
            26, new Color(1f, 0.85f, 0.4f));
    }

    private static string Musica() => Idioma.T(Opcoes.MusicaLigada ? "ligada" : "desligada");

    private static string Efeitos() => Idioma.T(Opcoes.EfeitosLigados ? "ligados" : "desligados");
}
