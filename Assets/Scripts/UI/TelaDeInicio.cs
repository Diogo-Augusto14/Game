using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// O menu inicial: nome do jogo, a escolha do heroi (esquerda e direita trocam, ver
/// <see cref="Herois"/>), os controles e "Enter pra jogar". Aparece so na primeira
/// vez que a cena abre; recomecar depois de morrer (R) vai direto pro andar 1, como no
/// Isaac. Com o menu na tela o jogo fica congelado e o jogador nao anda.
///
/// O <see cref="Andar"/> monta por <see cref="Mostrar"/>. Sair pro menu pela pausa chama
/// <see cref="VoltarAoMenu"/>, que recarrega a cena com o menu de novo.
/// </summary>
[DisallowMultipleComponent]
public class TelaDeInicio : MonoBehaviour
{
    private static readonly KeyCode[] TeclasDeJogar = { KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space };

    private string titulo = "THE PRETTIE";
    private Transform jogador;
    private Andar andar;
    private Text textoDoTitulo;
    private RectTransform linhaDeJogar;
    private Image botaoDeJogar;
    private Image ponteiro;
    private Image setaEsquerda;
    private Image setaDireita;
    private int ladoAceso;
    private float acesoAte;
    private RectTransform dicaDoHeroi;
    private Text textoDoHeroi;
    private Text descricaoDoHeroi;
    private Text numerosDoHeroi;
    private Image retratoDoHeroi;
    private ClipesDePersonagem clipesDoRetrato;
    private float abriu;

    /// <summary>O menu ja passou nesta sessao: recomecar nao mostra de novo.</summary>
    public static bool JaPassou { get; private set; }

    public static bool Aberta => atual != null;

    private static TelaDeInicio atual;

    // Com "Enter Play Mode" sem recarregar o dominio, o estatico sobreviveria entre Plays.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar()
    {
        JaPassou = false;
        atual = null;
    }

    public static void Mostrar(Andar andar, string titulo)
    {
        if (JaPassou || atual != null)
            return;

        TelaDeInicio tela = new GameObject("Menu inicial").AddComponent<TelaDeInicio>();
        tela.andar = andar;
        tela.titulo = string.IsNullOrEmpty(titulo) ? tela.titulo : titulo;
        tela.jogador = andar != null ? andar.Jogador : null;
        tela.Montar();
    }

    /// <summary>Pela pausa: recarrega a cena e mostra o menu de novo.</summary>
    public static void VoltarAoMenu()
    {
        JaPassou = false;
        TelaDeFimDeJogo.RecarregarCena();
    }

    private void Montar()
    {
        atual = this;
        abriu = Time.unscaledTime;

        TelaSimples.Montar(gameObject, 100, new Color(0.03f, 0.02f, 0.03f, 1f));

        textoDoTitulo = TelaSimples.Texto(transform, "Titulo", 100, new Color(1f, 0.93f, 0.8f), 400f, titulo);
        TelaSimples.Texto(transform, "Subtitulo", 30, new Color(0.75f, 0.65f, 0.65f), 268f,
            $"Desca {(andar != null ? andar.AndarFinal : 4)} andares e derrote o Olho do Porao");

        MontarEscolhaDoHeroi(88f);

        Color corDasDicas = new Color(0.92f, 0.92f, 0.95f);
        TelaSimples.LinhaDeTeclas(transform, "Controles", -158f,
            "[W][A][S][D] andar | [Cima][Esquerda][Baixo][Direita] atirar | [E] bomba", 28, corDasDicas);
        TelaSimples.LinhaDeTeclas(transform, "Opcoes", -212f, "[Esc] pausar | [M] musica | [N] efeitos", 28, corDasDicas);
        TelaSimples.Texto(transform, "Dica", 24, new Color(0.8f, 0.85f, 0.95f), -260f,
            "Bomba abre parede rachada. Moeda compra na loja.");

        linhaDeJogar = TelaSimples.LinhaDeTeclas(transform, "Jogar", -376f, "", 40, new Color(1f, 0.85f, 0.35f));
        MontarPonteiro(-376f);
        TelaSimples.LinhaDeTeclas(transform, "Sair", -462f, "[Esc] sair do jogo", 24, new Color(0.65f, 0.6f, 0.6f));
        TelaSimples.Texto(transform, "Creditos", 17, new Color(0.45f, 0.42f, 0.45f), -514f,
            "Sons de interface: Nathan Gibson (CC BY 4.0)    Interface: Tiny RPG Dragon Regalia GUI    Teclas: Vryell");

        // Molduras do Dragon Regalia atras de tudo (Painel poe logo acima do fundo escuro).
        botaoDeJogar = TelaSimples.Painel(transform, "Botao de jogar", ArteDaInterface.Botao(0), -376f, new Vector2(470f, 105f));
        TelaSimples.Painel(transform, "Painel dos controles", ArteDaInterface.MolduraPequena, -208f, new Vector2(1300f, 200f));
        TelaSimples.Faixa(transform, "Faixa do titulo", ArteDaInterface.FaixaRosa, 400f, 1150f);
        MostrarHeroi();

        // Congela o jogo e segura o jogador ate apertar Enter.
        Time.timeScale = 0f;
        TelaSimples.TravarJogador(jogador, true);
        Musica.Tocar(TemaMusical.Menu);
    }

    private void Update()
    {
        // O jogador pode ter sido montado depois do menu (mesmo quadro do Start do Andar).
        if (jogador == null && andar != null && andar.Jogador != null)
        {
            jogador = andar.Jogador;
            TelaSimples.TravarJogador(jogador, true);
        }

        float t = Time.unscaledTime;
        textoDoTitulo.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 1.5f) * 0.03f);

        Opcoes.LerTeclas();
        AnimarRetrato(t);
        AnimarPonteiro(t);
        AnimarSetas(t);

        // Meio segundo de respiro: o Enter que abriu o Play nao pula o menu.
        if (t - abriu < 0.4f)
            return;

        // No .exe e o unico jeito de fechar sem Alt+F4. No editor, sai do Play.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            TrocarHeroi(-1);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            TrocarHeroi(1);

        foreach (KeyCode tecla in TeclasDeJogar)
        {
            if (Input.GetKeyDown(tecla))
            {
                if (Herois.Liberado(Herois.Atual))
                    Comecar();
                else
                    Sons.Tocar(Som.MenuNegado, 0.8f, 0f);

                return;
            }
        }
    }

    private void Comecar()
    {
        JaPassou = true;
        Time.timeScale = 1f;

        // O jogador ja esta na sala desde antes do menu: veste o heroi escolhido agora.
        if (jogador != null)
            Herois.Aplicar(jogador.gameObject, Herois.Atual);

        TelaSimples.TravarJogador(jogador, false);
        Sons.Tocar(Som.MenuConfirmar, 1f, 0f);

        if (andar != null)
            Musica.Tocar(Musica.DoAndar(andar.NumeroDoAndar));

        Destroy(gameObject);
    }

    // ---------------- escolha do heroi ----------------
    private void MontarEscolhaDoHeroi(float y)
    {
        TelaSimples.Painel(transform, "Painel do heroi", ArteDaInterface.MolduraGrande, y, new Vector2(1300f, 320f));

        GameObject obj = new GameObject("Retrato do heroi", typeof(RectTransform));
        obj.transform.SetParent(transform, false);
        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-400f, y);

        retratoDoHeroi = obj.AddComponent<Image>();
        retratoDoHeroi.preserveAspect = true;
        retratoDoHeroi.raycastTarget = false;

        textoDoHeroi = TelaSimples.Texto(transform, "Nome do heroi", 46, new Color(1f, 0.9f, 0.6f), y + 88f, "");
        descricaoDoHeroi = TelaSimples.Texto(transform, "Descricao do heroi", 28, new Color(1f, 0.97f, 0.92f), y + 35f, "");
        numerosDoHeroi = TelaSimples.Texto(transform, "Numeros do heroi", 24, new Color(0.85f, 0.93f, 1f), y - 15f, "");
        dicaDoHeroi = TelaSimples.LinhaDeTeclas(transform, "Dica do heroi", y - 68f, "", 22, new Color(0.85f, 0.93f, 1f));
        dicaDoHeroi.anchoredPosition = new Vector2(120f, dicaDoHeroi.anchoredPosition.y);

        setaEsquerda = Imagem("Seta esquerda", new Vector2(-590f, y), 64f);
        setaDireita = Imagem("Seta direita", new Vector2(590f, y), 64f);

        foreach (Text texto in new[] { textoDoHeroi, descricaoDoHeroi, numerosDoHeroi })
        {
            RectTransform rtTexto = texto.rectTransform;
            rtTexto.anchoredPosition = new Vector2(120f, rtTexto.anchoredPosition.y);
            rtTexto.sizeDelta = new Vector2(860f, rtTexto.sizeDelta.y);
        }

        MostrarHeroi();
    }

    private void TrocarHeroi(int passo)
    {
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
        descricaoDoHeroi.text = liberado ? heroi.Descricao : $"<color=#ff9966>Pra liberar: {heroi.Requisito}</color>";
        numerosDoHeroi.text = liberado
            ? $"Vida {heroi.Vida / 20f:0.#}     Velocidade {heroi.Velocidade:0.#}     Dano {heroi.Dano:0.#}     " +
              $"Tiros/s {heroi.Cadencia:0.#}     Alcance {heroi.Alcance:0.#}"
            : "? ? ?";

        TelaSimples.TrocarLinhaDeTeclas(dicaDoHeroi,
            $"[A][D] ou [Esquerda][Direita] troca o heroi ({LiberadosNoTotal()} de {Herois.Todos.Length} liberados)",
            22, new Color(0.85f, 0.93f, 1f));

        // Na montagem, o nome e a descricao vem antes do botao de jogar existir.
        if (linhaDeJogar != null)
        {
            TelaSimples.TrocarLinhaDeTeclas(linhaDeJogar, liberado ? "[Enter] jogar" : "Heroi bloqueado", 40,
                liberado ? new Color(1f, 0.85f, 0.35f) : new Color(1f, 0.6f, 0.4f));
        }

        if (botaoDeJogar != null)
            botaoDeJogar.sprite = ArteDaInterface.Botao(liberado ? 0 : 3);

        if (ponteiro != null)
            ponteiro.enabled = liberado && ponteiro.sprite != null;

        clipesDoRetrato = Herois.Clipes(heroi);
        retratoDoHeroi.enabled = clipesDoRetrato != null && clipesDoRetrato.Parado != null;

        // O arqueiro azul tem celula de 192 px com o corpo grande; os do Tiny RPG, 100 px
        // com o corpo pequeno no meio: o quadro deles vai maior pra ficarem do mesmo tamanho.
        retratoDoHeroi.rectTransform.sizeDelta = Vector2.one * (heroi.Pasta == null ? 230f : 380f);

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

    private Image Imagem(string nome, Vector2 posicao, float lado)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(transform, false);
        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = Vector2.one * lado;

        Image imagem = obj.AddComponent<Image>();
        imagem.raycastTarget = false;
        return imagem;
    }

    /// <summary>O ponteiro dourado que aponta pro botao de jogar.</summary>
    private void MontarPonteiro(float y)
    {
        ponteiro = Imagem("Ponteiro", new Vector2(-300f, y), 78f);
        ponteiro.sprite = ArteDaInterface.Ponteiro(0);
        ponteiro.enabled = ponteiro.sprite != null;
    }

    private void AnimarPonteiro(float tempo)
    {
        if (ponteiro == null || !ponteiro.enabled)
            return;

        ponteiro.sprite = ArteDaInterface.Ponteiro(Mathf.FloorToInt(tempo * 8f));
        RectTransform rt = ponteiro.rectTransform;
        rt.anchoredPosition = new Vector2(-300f + Mathf.Abs(Mathf.Sin(tempo * 4f)) * 12f, rt.anchoredPosition.y);
    }

    /// <summary>A seta do lado que trocou acende por um instante.</summary>
    private void AnimarSetas(float tempo)
    {
        bool aceso = tempo < acesoAte;
        setaEsquerda.sprite = ArteDaInterface.SetaEsquerda(aceso && ladoAceso < 0 ? 1 : 0);
        setaDireita.sprite = ArteDaInterface.SetaDireita(aceso && ladoAceso > 0 ? 1 : 0);
        setaEsquerda.enabled = setaEsquerda.sprite != null;
        setaDireita.enabled = setaDireita.sprite != null;
    }

    private void AnimarRetrato(float tempo)
    {
        if (retratoDoHeroi == null || !retratoDoHeroi.enabled)
            return;

        Sprite[] quadros = clipesDoRetrato.Parado;
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
