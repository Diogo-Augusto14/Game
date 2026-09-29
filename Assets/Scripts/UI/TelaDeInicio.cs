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
    private Text textoDeJogar;
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

        TelaSimples.Montar(gameObject, 100, new Color(0.03f, 0.02f, 0.03f, 0.97f));

        textoDoTitulo = TelaSimples.Texto(transform, "Titulo", 110, new Color(0.95f, 0.85f, 0.75f), 390f, titulo);
        TelaSimples.Texto(transform, "Subtitulo", 30, new Color(0.7f, 0.6f, 0.6f), 300f,
            $"Desca {(andar != null ? andar.AndarFinal : 4)} andares e derrote o Olho do Porao");

        MontarEscolhaDoHeroi(110f);

        TelaSimples.Texto(transform, "Controles", 28, new Color(0.85f, 0.85f, 0.85f), -150f,
            "W A S D  andar        Setas  atirar        E  bomba\n" +
            "Esc  pausar        M  musica        N  efeitos\n" +
            "Bomba abre parede rachada. Moeda compra na loja.");

        textoDeJogar = TelaSimples.Texto(transform, "Jogar", 44, new Color(1f, 0.85f, 0.35f), -330f, "Enter  jogar");
        MostrarHeroi();
        TelaSimples.Texto(transform, "Sair", 26, new Color(0.6f, 0.55f, 0.55f), -405f, "Esc  sair do jogo");

        // Paineis do Pixel UI pack atras dos controles e do "botao" de jogar.
        TelaSimples.Painel(transform, "Painel do botao", ArteImportada.PainelAzul, -330f, new Vector2(520f, 110f));
        TelaSimples.Painel(transform, "Painel dos controles", ArteImportada.PainelMarrom, -150f, new Vector2(1180f, 170f));

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
        textoDeJogar.color = new Color(1f, 0.85f, 0.35f, 0.55f + Mathf.Sin(t * 4f) * 0.45f);

        Opcoes.LerTeclas();
        AnimarRetrato(t);

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
                    Sons.Tocar(Som.DanoJogador, 0.5f);

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
        Sons.Tocar(Som.Menu);

        if (andar != null)
            Musica.Tocar(Musica.DoAndar(andar.NumeroDoAndar));

        Destroy(gameObject);
    }

    // ---------------- escolha do heroi ----------------
    private void MontarEscolhaDoHeroi(float y)
    {
        TelaSimples.Painel(transform, "Painel do heroi", ArteImportada.PainelMarrom, y, new Vector2(1180f, 250f));

        GameObject obj = new GameObject("Retrato do heroi", typeof(RectTransform));
        obj.transform.SetParent(transform, false);
        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-400f, y);

        retratoDoHeroi = obj.AddComponent<Image>();
        retratoDoHeroi.preserveAspect = true;
        retratoDoHeroi.raycastTarget = false;

        textoDoHeroi = TelaSimples.Texto(transform, "Nome do heroi", 46, new Color(1f, 0.9f, 0.6f), y + 65f, "");
        descricaoDoHeroi = TelaSimples.Texto(transform, "Descricao do heroi", 28, new Color(0.9f, 0.85f, 0.8f), y + 5f, "");
        numerosDoHeroi = TelaSimples.Texto(transform, "Numeros do heroi", 24, new Color(0.7f, 0.8f, 0.9f), y - 60f, "");

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
        Herois.Escolher(Herois.Escolhido + passo);
        Sons.Tocar(Som.Menu, 0.6f);
        MostrarHeroi();
    }

    private void MostrarHeroi()
    {
        Herois.Heroi heroi = Herois.Atual;
        bool liberado = Herois.Liberado(heroi);

        textoDoHeroi.text = liberado ? $"<   {heroi.Nome}   >" : $"<   {heroi.Nome}  (bloqueado)   >";
        descricaoDoHeroi.text = liberado ? heroi.Descricao : $"<color=#ff9966>Pra liberar: {heroi.Requisito}</color>";
        numerosDoHeroi.text =
            (liberado
                ? $"Vida {heroi.Vida / 20f:0.#}     Velocidade {heroi.Velocidade:0.#}     Dano {heroi.Dano:0.#}     " +
                  $"Tiros/s {heroi.Cadencia:0.#}     Alcance {heroi.Alcance:0.#}\n"
                : "? ? ?\n") +
            $"A / D  ou  esquerda / direita  troca o heroi  ({LiberadosNoTotal()} de {Herois.Todos.Length} liberados)";

        // Na montagem, o nome e a descricao vem antes do texto de jogar existir.
        if (textoDeJogar != null)
            textoDeJogar.text = liberado ? "Enter  jogar" : "Heroi bloqueado";

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
