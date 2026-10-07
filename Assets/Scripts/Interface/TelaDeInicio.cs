using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// O menu inicial: nome do jogo, a escolha do heroi (esquerda e direita trocam, ver
/// <see cref="Herois"/>) e os botoes Jogar, Configuracoes e Sair do jogo. Aparece so na
/// primeira vez que a cena abre; recomecar depois de morrer vai direto pro andar 1, como no
/// Isaac. Com o menu na tela o jogo fica congelado e o jogador nao anda.
///
///   W/S ou Cima/Baixo   escolhe o botao       A/D ou Esquerda/Direita   troca o heroi
///   Enter / Espaco      aperta                O                         configuracoes
///   Esc                 vai pro "Sair do jogo" (de novo: sai)
///
/// No controle: cruz, A aperta, Start joga, X configuracoes, Select vai pro sair. O mouse
/// escolhe e clica nos botoes e nas setas do heroi.
///
/// Veio do jogo antigo. La tinha tambem "Continuar" (a partida salva) e "Progresso" (conquistas e
/// bestiario), que dependem de sistemas que o jogo novo ainda nao tem.
///
/// Abre com animacao (o titulo cai no lugar, o heroi e os botoes sobem) e, no Jogar, some
/// aos poucos antes de soltar o jogador.
///
/// O <see cref="GeradorDoAndar"/> monta por <see cref="Mostrar"/>. Sair pro menu pela pausa chama
/// <see cref="VoltarAoMenu"/>, que recarrega a cena com o menu de novo.
/// </summary>
[DisallowMultipleComponent]
public class TelaDeInicio : MonoBehaviour
{
    private const float TempoDeSair = 0.4f;
    private const float YDoTitulo = 390f;

    private string titulo = "The Prettie";
    private Transform jogador;
    private GeradorDoAndar andar;
    private CanvasGroup grupo;
    private CanvasGroup topo;
    private CanvasGroup painelDoHeroi;
    private CanvasGroup rodape;
    private Text textoDoTitulo;
    private MenuDeBotoes menu;
    private int botaoJogar;
    private int botaoSair;
    private Image setaEsquerda;
    private Image setaDireita;
    private int ladoAceso;
    private float acesoAte;
    private RectTransform dicaDoHeroi;
    private Text textoDoHeroi;
    private Text descricaoDoHeroi;
    private Text numerosDoHeroi;
    private Image retratoDoHeroi;
    private Sprite[] quadrosDoRetrato;
    private float abriu;
    private float saindoDesde = -1f;

    /// <summary>O menu ja passou nesta sessao: recomecar nao mostra de novo.</summary>
    public static bool JaPassou { get; private set; }

    public static bool Aberta => atual != null;

    private static TelaDeInicio atual;
    private static bool jaAbriuOJogo;

    // Com "Enter Play Mode" sem recarregar o dominio, o estatico sobreviveria entre Plays.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar()
    {
        JaPassou = false;
        atual = null;
        jaAbriuOJogo = false;
    }

    public static void Mostrar(GeradorDoAndar andar, string titulo = null)
    {
        if (JaPassou || atual != null)
            return;

        TelaDeInicio tela = new GameObject("Menu inicial").AddComponent<TelaDeInicio>();
        tela.andar = andar;
        tela.titulo = string.IsNullOrEmpty(titulo) ? tela.titulo : NomeParaOTitulo(titulo);
        tela.jogador = andar != null ? andar.Jogador : null;
        tela.Montar();
    }

    /// <summary>Pela pausa ou pelo fim de jogo: escurece, recarrega a cena e mostra o menu de novo.</summary>
    public static void VoltarAoMenu()
    {
        TransicaoDeTela.Trocar(() =>
        {
            JaPassou = false;
            TelaDeFimDeJogo.RecarregarCena();
        });
    }

    /// <summary>Fecha o jogo (no editor, sai do Play), com a tela escurecendo antes.</summary>
    public static void SairDoJogo()
    {
        Sons.Tocar(Som.MenuFechar, 1f, 0f);
        TransicaoDeTela.Trocar(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }

    /// <summary>
    /// A fonte gotica do titulo fica ilegivel em caixa alta: "THE PRETTIE" vira "The Prettie".
    /// Nome ja em maiusculas e minusculas passa como esta.
    /// </summary>
    private static string NomeParaOTitulo(string nome)
    {
        if (nome != nome.ToUpperInvariant())
            return nome;

        System.Globalization.TextInfo texto = System.Globalization.CultureInfo.InvariantCulture.TextInfo;
        return texto.ToTitleCase(nome.ToLowerInvariant());
    }

    private void Montar()
    {
        atual = this;
        abriu = Time.unscaledTime;

        // Fundo opaco: a HUD e a sala do jogo, montadas por baixo, nao aparecem atras do menu.
        grupo = TelaSimples.Montar(gameObject, 100, new Color(0.03f, 0.02f, 0.03f, 1f));

        // Topo: faixa rosa do Dragon Regalia e o nome na fonte gotica.
        topo = TelaSimples.Camada(transform, "Topo");
        TelaSimples.Faixa(topo.transform, "Faixa do titulo", ArteDaInterface.FaixaRosa, YDoTitulo, 1150f);
        textoDoTitulo = TelaSimples.Titulo(topo.transform, "Titulo", 150, new Color(1f, 0.93f, 0.8f), YDoTitulo, titulo);
        TelaSimples.Texto(topo.transform, "Subtitulo", 30, new Color(0.8f, 0.7f, 0.7f), 282f,
            $"Desça os {(andar != null ? andar.Andares : 6)} andares da prisão e derrote os chefes");

        painelDoHeroi = TelaSimples.Camada(transform, "Heroi");
        MontarEscolhaDoHeroi(painelDoHeroi.transform, 105f);

        const float y = -130f;
        const float passo = 80f;
        menu = new MenuDeBotoes(transform, 0f, new Vector2(540f, 66f), 34)
            { AtrasoDaEntrada = 0.3f, IntervaloDaEntrada = 0.06f };

        botaoJogar = menu.Adicionar("Jogar", y, Jogar, "[Enter] || [Pad A]");
        menu.Adicionar("Configurações", y - passo, AbrirOpcoes, "[O] || [Pad X]", IconeDoBotao.Configuracoes);
        botaoSair = menu.Adicionar("Sair do jogo", y - passo * 2f, SairDoJogo, "[Esc] || [Pad Select]", IconeDoBotao.Sair, perigo: true);

        MontarRodape();
        MostrarHeroi();

        // Congela o jogo e segura o jogador ate apertar Jogar.
        Time.timeScale = 0f;
        TelaSimples.TravarJogador(jogador, true);
        Musica.Tocar(TemaMusical.Menu);

        // Primeira vez que o jogo abre: sai do preto. (Voltando ao menu, a transicao ja clareia.)
        if (!jaAbriuOJogo)
        {
            jaAbriuOJogo = true;
            TransicaoDeTela.Revelar(0.8f);
        }

        Animar(Time.unscaledTime);

        // Versao nova instalada: as novidades por cima do menu, uma vez.
        TelaDeNovidades.MostrarSeForNova();
    }

    private void MontarRodape()
    {
        rodape = TelaSimples.Camada(transform, "Rodape");
        Transform pai = rodape.transform;

        TelaSimples.Painel(pai, "Painel dos controles", ArteDaInterface.MolduraPequena, -428f, new Vector2(1500f, 118f));

        Color corDasDicas = new Color(0.92f, 0.92f, 0.95f);
        TelaSimples.LinhaDeTeclas(pai, "Controles", -408f,
            "[W][A][S][D] andar | mouse mira e atira | [Espaco] esquiva | [Q] troca a arma | [R] recarrega | [E] pega e abre | [Esc] pausar || " +
            "[Pad AnalogicoEsquerdo] andar | [Pad AnalogicoDireito] mirar | [Pad RT] atirar | [Pad A] esquiva | [Pad Y] troca | [Pad X] recarrega | [Pad B] pega | [Pad Start] pausar",
            24, corDasDicas);
        TelaSimples.LinhaDeTeclas(pai, "Som", -448f,
            "[M] música | [N] efeitos | Baú dá arma. Mate todos do andar pra abrir o portal. || " +
            "[Pad LB] música | [Pad RB] efeitos | Baú dá arma. Mate todos do andar pra abrir o portal.", 22, new Color(0.8f, 0.85f, 0.95f));

        TelaSimples.Texto(pai, "Creditos", 16, new Color(0.5f, 0.47f, 0.5f), -514f,
            "Sons de interface: Nathan Gibson (CC BY 4.0)    Efeitos: Freedoom (BSD)    Interface: Tiny RPG Dragon Regalia GUI    " +
            "Cenário: Old Prison (EPIC RPG World)    Teclas: Vryell    Fontes: Jersey 15 e Jacquard 12 (OFL)");

        // A versao instalada, no cantinho de baixo a direita: da pra ver se o lancador atualizou.
        Text versao = TelaSimples.Texto(pai, "Versao", 20, new Color(0.6f, 0.57f, 0.62f), 0f, VersaoDoJogo.Texto);
        RectTransform rt = versao.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(300f, 30f);
        rt.anchoredPosition = new Vector2(-16f, 10f);
        versao.alignment = TextAnchor.LowerRight;
    }

    private void Update()
    {
        // O jogador pode ter sido achado depois do menu (mesmo quadro do Start do gerador).
        if (jogador == null && andar != null && andar.Jogador != null)
        {
            jogador = andar.Jogador;
            TelaSimples.TravarJogador(jogador, true);
        }

        float t = Time.unscaledTime;
        Animar(t);

        if (saindoDesde >= 0f)
        {
            Sair(t);
            return;
        }

        // Com as opcoes por cima, as teclas sao delas (o Esc de la nao pode sair do jogo).
        menu.Ligado = !TelaDeOpcoes.Ocupada && !TelaDeNovidades.Ocupada && t - abriu >= 0.4f;

        if (menu.Ligado && !TransicaoDeTela.Ocupada)
            LerAtalhos();

        menu.Atualizar();
    }

    /// <summary>As teclas do menu que nao sao dos botoes (o W/S/Enter/mouse o menu le sozinho).</summary>
    private void LerAtalhos()
    {
        Opcoes.LerTeclas();

        // Esc nao fecha direto: primeiro vai pro botao de sair, e o segundo Esc sai.
        if (Input.GetKeyDown(KeyCode.Escape) || Controle.Apertou(BotaoDoControle.Select))
        {
            if (menu.Escolhido == botaoSair)
                menu.Apertar(botaoSair);
            else
                menu.Escolher(botaoSair);
        }
        else if (Input.GetKeyDown(KeyCode.O) || Controle.Apertou(BotaoDoControle.X))
        {
            AbrirOpcoes();
        }
        else if (Controle.Apertou(BotaoDoControle.Start))
        {
            menu.Apertar(botaoJogar);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Controle.Apertou(BotaoDoControle.CruzEsquerda)
                 || Clicou(setaEsquerda))
        {
            TrocarHeroi(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Controle.Apertou(BotaoDoControle.CruzDireita)
                 || Clicou(setaDireita))
        {
            TrocarHeroi(1);
        }
    }

    private static bool Clicou(Image imagem)
    {
        return imagem != null && imagem.enabled && Input.GetMouseButtonDown(0)
               && RectTransformUtility.RectangleContainsScreenPoint(imagem.rectTransform, Input.mousePosition, null);
    }

    private void Jogar()
    {
        if (!Herois.Liberado(Herois.Atual))
        {
            Sons.Tocar(Som.MenuNegado, 0.8f, 0f);
            return;
        }

        Sons.Tocar(Som.MenuConfirmar, 1f, 0f);
        menu.Ligado = false;
        saindoDesde = Time.unscaledTime;
    }

    private static void AbrirOpcoes() => TelaDeOpcoes.Abrir();

    /// <summary>O menu some crescendo de leve; no fim, o jogo comeca.</summary>
    private void Sair(float agora)
    {
        float t = (agora - saindoDesde) / TempoDeSair;
        grupo.alpha = 1f - TelaSimples.Freando(t);
        topo.transform.localScale = painelDoHeroi.transform.localScale = Vector3.one * (1f + TelaSimples.Freando(t) * 0.06f);
        menu.Atualizar();

        if (t >= 1f)
            Comecar();
    }

    private void Comecar()
    {
        JaPassou = true;
        Time.timeScale = 1f;

        // Por enquanto todo heroi e o Arqueiro (os outros chegam na etapa 8).
        TelaSimples.TravarJogador(jogador, false);

        if (andar != null)
        {
            Musica.Tocar(andar.MusicaDoAndar);
            andar.MostrarNome();
        }

        Destroy(gameObject);
    }

    // ---------------- animacao ----------------
    private void Animar(float t)
    {
        float desde = t - abriu;

        // O titulo cai de cima e passa um pouco do ponto; depois respira devagar.
        float queda = TelaSimples.PassaEVolta(desde / 0.7f);
        topo.alpha = Mathf.Clamp01(desde / 0.35f);
        textoDoTitulo.rectTransform.anchoredPosition = new Vector2(0f, YDoTitulo + (1f - queda) * 160f);
        textoDoTitulo.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 1.5f) * 0.025f);

        // O heroi sobe um pouco depois, e o rodape aparece por ultimo.
        float heroi = TelaSimples.Freando((desde - 0.25f) / 0.4f);
        painelDoHeroi.alpha = heroi;
        ((RectTransform)painelDoHeroi.transform).anchoredPosition = new Vector2(0f, (1f - heroi) * -40f);
        rodape.alpha = TelaSimples.Freando((desde - 0.7f) / 0.5f);

        AnimarRetrato(t);
        AnimarSetas(t);
    }

    // ---------------- escolha do heroi ----------------
    private void MontarEscolhaDoHeroi(Transform pai, float y)
    {
        TelaSimples.Painel(pai, "Painel do heroi", ArteDaInterface.MolduraGrande, y, new Vector2(1300f, 300f));

        GameObject obj = new GameObject("Retrato do heroi", typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-400f, y);

        retratoDoHeroi = obj.AddComponent<Image>();
        retratoDoHeroi.preserveAspect = true;
        retratoDoHeroi.raycastTarget = false;

        textoDoHeroi = TelaSimples.Texto(pai, "Nome do heroi", 46, new Color(1f, 0.9f, 0.6f), y + 85f, "");
        descricaoDoHeroi = TelaSimples.Texto(pai, "Descricao do heroi", 28, new Color(1f, 0.97f, 0.92f), y + 32f, "");
        numerosDoHeroi = TelaSimples.Texto(pai, "Numeros do heroi", 24, new Color(0.85f, 0.93f, 1f), y - 18f, "");
        dicaDoHeroi = TelaSimples.LinhaDeTeclas(pai, "Dica do heroi", y - 72f, "", 22, new Color(0.85f, 0.93f, 1f));
        dicaDoHeroi.anchoredPosition = new Vector2(120f, dicaDoHeroi.anchoredPosition.y);

        setaEsquerda = Imagem(pai, "Seta esquerda", new Vector2(-590f, y), 64f);
        setaDireita = Imagem(pai, "Seta direita", new Vector2(590f, y), 64f);

        foreach (Text texto in new[] { textoDoHeroi, descricaoDoHeroi, numerosDoHeroi })
        {
            RectTransform rtTexto = texto.rectTransform;
            rtTexto.anchoredPosition = new Vector2(120f, rtTexto.anchoredPosition.y);
            rtTexto.sizeDelta = new Vector2(860f, rtTexto.sizeDelta.y);
        }
    }

    private void TrocarHeroi(int passo)
    {
        if (Herois.Todos.Length <= 1)
            return;

        ladoAceso = passo;
        acesoAte = Time.unscaledTime + 0.15f;
        Herois.Escolher(Herois.Escolhido + passo);
        Sons.Tocar(Som.Menu, 0.6f);
        MostrarHeroi();
    }

    private void MostrarHeroi()
    {
        Herois.Heroi heroi = Herois.Atual;
        bool liberado = Herois.Liberado(heroi);

        textoDoHeroi.text = liberado ? heroi.Nome : $"{heroi.Nome}  (bloqueado)";
        descricaoDoHeroi.text = heroi.Descricao;
        numerosDoHeroi.text = liberado ? heroi.Numeros : "? ? ?";

        if (Herois.Todos.Length > 1)
        {
            TelaSimples.TrocarLinhaDeTeclas(dicaDoHeroi,
                $"[A][D] ou [Esquerda][Direita] troca o herói ({LiberadosNoTotal()} de {Herois.Todos.Length} liberados) || " +
                $"[Pad CruzEsquerda][Pad CruzDireita] troca o herói ({LiberadosNoTotal()} de {Herois.Todos.Length} liberados)",
                22, new Color(0.85f, 0.93f, 1f));
        }
        else
        {
            TelaSimples.TrocarLinhaDeTeclas(dicaDoHeroi, "Mais heróis chegam em breve", 22, new Color(0.85f, 0.93f, 1f));
        }

        // Herói bloqueado: o botao de jogar apaga e avisa.
        if (menu != null)
        {
            menu.Apagar(botaoJogar, !liberado);
            menu.TrocarRotulo(botaoJogar, liberado ? "Jogar" : "Herói bloqueado");
        }

        quadrosDoRetrato = Herois.Parado(heroi);
        retratoDoHeroi.enabled = quadrosDoRetrato.Length > 0;

        // Os do Tiny RPG tem o corpo pequeno no meio de um quadro de 100 px: o quadro vai grande.
        retratoDoHeroi.rectTransform.sizeDelta = Vector2.one * 380f;

        // Bloqueado aparece so a silhueta, como no Isaac.
        retratoDoHeroi.color = liberado ? Color.white : new Color(0f, 0f, 0f, 0.85f);
    }

    private static int LiberadosNoTotal()
    {
        int total = 0;

        foreach (Herois.Heroi heroi in Herois.Todos)
        {
            if (Herois.Liberado(heroi))
                total++;
        }

        return total;
    }

    private static Image Imagem(Transform pai, string nome, Vector2 posicao, float lado)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = Vector2.one * lado;

        Image imagem = obj.AddComponent<Image>();
        imagem.raycastTarget = false;
        return imagem;
    }

    /// <summary>A seta do lado que trocou acende por um instante.</summary>
    private void AnimarSetas(float tempo)
    {
        bool aceso = tempo < acesoAte;
        setaEsquerda.sprite = ArteDaInterface.SetaEsquerda(aceso && ladoAceso < 0 ? 1 : 0);
        setaDireita.sprite = ArteDaInterface.SetaDireita(aceso && ladoAceso > 0 ? 1 : 0);
        // Com um heroi so, nao tem pra onde trocar: sem setas.
        bool varios = Herois.Todos.Length > 1;
        setaEsquerda.enabled = varios && setaEsquerda.sprite != null;
        setaDireita.enabled = varios && setaDireita.sprite != null;
    }

    private void AnimarRetrato(float tempo)
    {
        if (retratoDoHeroi == null || !retratoDoHeroi.enabled)
            return;

        Sprite[] quadros = quadrosDoRetrato;
        retratoDoHeroi.sprite = quadros[Mathf.FloorToInt(tempo * 8f) % quadros.Length];
    }

    private void OnDestroy()
    {
        if (atual == this)
            atual = null;

        // Sair do Play com o menu aberto nao pode deixar o tempo parado.
        if (Time.timeScale == 0f && TelaDeFimDeJogo.Atual == null)
            Time.timeScale = 1f;
    }
}
