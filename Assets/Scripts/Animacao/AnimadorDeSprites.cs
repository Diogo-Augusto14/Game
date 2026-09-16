using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Toca animacoes quadro a quadro direto no SpriteRenderer, sem Animator Controller.
///
/// Por que nao usar o Animator: num jogo de pixel art com 40 estados, a maquina de
/// estados visual vira um novelo de transicoes — e quem manda de verdade e o codigo do
/// Movimento/Ataque, que ja sabe em que estado esta. Aqui o codigo pede o clipe pelo
/// nome e pronto: nada de parametro faltando, transicao com tempo errado ou clipe que
/// nao dispara. De graca ainda vem o que o Animator nao da facil: saber o quadro exato
/// (pra abrir a hitbox), o progresso de 0 a 1, e esperar o clipe terminar.
///
/// Coloque no mesmo objeto do SpriteRenderer (ou no pai dele).
/// </summary>
[DisallowMultipleComponent]
public class AnimadorDeSprites : MonoBehaviour
{
    [Header("Referencias (vazio = procura sozinho)")]
    [Tooltip("Vazio = usa a biblioteca de Assets/Resources")]
    [SerializeField] private BibliotecaDeAnimacoes biblioteca;

    [SerializeField] private SpriteRenderer renderizador;

    [Header("Inicio")]
    [Tooltip("Clipe tocado no Start. Vazio = nenhum")]
    [SerializeField] private string clipeInicial = "";

    [Header("Eventos")]
    [Tooltip("Disparado no fim de um clipe sem loop (uma vez so). Manda o nome do clipe")]
    public UnityEvent<string> AoTerminar = new UnityEvent<string>();

    // ---------------- estado ----------------
    private ClipeDeSprites clipe;
    private float tempo;          // segundos tocados do clipe atual
    private int quadro = -1;      // quadro mostrado agora
    private bool terminou;        // clipe sem loop que ja chegou no fim
    private bool avisouFim;       // pra AoTerminar disparar uma vez so

    /// <summary>Multiplica a velocidade do clipe. 1 = normal. Use pra ligar a corrida a velocidade real.</summary>
    public float Velocidade { get; set; } = 1f;

    /// <summary>Nome do clipe tocando agora (string vazia se nenhum).</summary>
    public string ClipeAtual => clipe != null ? clipe.nome : "";

    /// <summary>Quadro mostrado agora, comecando do zero.</summary>
    public int Quadro => quadro;

    public int TotalDeQuadros => clipe != null ? clipe.Quantidade : 0;

    /// <summary>Progresso de 0 a 1 do clipe. Em loop, volta pra 0 a cada passada.</summary>
    public float Normalizado
    {
        get
        {
            if (clipe == null || clipe.Duracao <= 0f)
                return 0f;

            return Mathf.Clamp01(tempo / clipe.Duracao);
        }
    }

    /// <summary>True quando um clipe sem loop chegou no fim. Sempre false em clipes com loop.</summary>
    public bool Terminou => terminou;

    /// <summary>Duracao do clipe atual, ja contando a <see cref="Velocidade"/>.</summary>
    public float DuracaoAtual
    {
        get
        {
            if (clipe == null)
                return 0f;

            return clipe.Duracao / Mathf.Max(0.01f, Velocidade);
        }
    }

    public BibliotecaDeAnimacoes Biblioteca => biblioteca;

    // ---------------- ciclo de vida ----------------
    private void Reset()
    {
        renderizador = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (!GarantirRenderizador())
        {
            Debug.LogError($"[AnimadorDeSprites] {name}: nao achei SpriteRenderer neste objeto nem nos filhos.", this);
            enabled = false;
            return;
        }

        if (biblioteca == null)
            biblioteca = BibliotecaDeAnimacoes.Padrao;

        DesligarAnimatorConcorrente();
    }

    /// <summary>
    /// Resolve o SpriteRenderer sob demanda, e nao so no Awake.
    ///
    /// Motivo: ferramentas de editor montam o objeto FORA do Play, onde AddComponent nao
    /// chama Awake. Se a resolucao morasse so no Awake, qualquer chamada de Tocar() feita
    /// por um menu do editor daria NullReference num campo que "deveria" estar preenchido.
    /// </summary>
    private bool GarantirRenderizador()
    {
        if (renderizador != null)
            return true;

        renderizador = GetComponent<SpriteRenderer>();

        if (renderizador == null)
            renderizador = GetComponentInChildren<SpriteRenderer>();

        return renderizador != null;
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(clipeInicial))
            Tocar(clipeInicial);
    }

    /// <summary>
    /// Um Animator no mesmo objeto tambem escreve em SpriteRenderer.sprite — os dois
    /// brigariam a cada quadro e o sprite ficaria piscando. Este script e quem manda.
    /// </summary>
    private void DesligarAnimatorConcorrente()
    {
        Animator animator = GetComponent<Animator>();

        if (animator == null && renderizador != null)
            animator = renderizador.GetComponent<Animator>();

        if (animator == null || !animator.enabled)
            return;

        animator.enabled = false;
        Debug.Log($"[AnimadorDeSprites] {name}: desliguei o Animator — quem anima o sprite agora e este script.", this);
    }

    private void Update()
    {
        if (clipe == null || !clipe.Valido)
            return;

        if (terminou)
        {
            AvisarFim();
            return;
        }

        tempo += Time.deltaTime * Mathf.Max(0f, Velocidade);

        float duracao = clipe.Duracao;
        int quadroDesejado;

        if (clipe.emLoop)
        {
            if (duracao > 0f)
                tempo = Mathf.Repeat(tempo, duracao);

            quadroDesejado = Mathf.Min((int)(tempo * clipe.quadrosPorSegundo), clipe.Quantidade - 1);
        }
        else
        {
            quadroDesejado = (int)(tempo * clipe.quadrosPorSegundo);

            if (quadroDesejado >= clipe.Quantidade)
            {
                quadroDesejado = clipe.Quantidade - 1;
                terminou = true;
            }
        }

        Mostrar(quadroDesejado);

        if (terminou)
        {
            if (!clipe.manterUltimoQuadro)
                renderizador.sprite = null;

            AvisarFim();
        }
    }

    // ---------------- uso ----------------
    /// <summary>
    /// Toca o clipe. Pedir o MESMO clipe que ja esta tocando nao reinicia nada — entao
    /// da pra chamar isto todo quadro, direto da maquina de estados, sem medo.
    /// </summary>
    public void Tocar(string nome, bool reiniciar = false)
    {
        if (string.IsNullOrEmpty(nome))
            return;

        if (!reiniciar && clipe != null && clipe.nome == nome)
            return;

        if (biblioteca == null)
            biblioteca = BibliotecaDeAnimacoes.Padrao;

        if (biblioteca == null)
            return;

        ClipeDeSprites novo = biblioteca.Buscar(nome);

        if (novo == null)
        {
            Debug.LogWarning($"[AnimadorDeSprites] {name}: nao existe o clipe \"{nome}\" na biblioteca.", this);
            return;
        }

        if (!GarantirRenderizador())
            return;

        Definir(novo);
    }

    /// <summary>Toca o primeiro nome que existir na biblioteca. Devolve o que foi tocado.</summary>
    public string TocarPrimeiroQueExistir(params string[] nomes)
    {
        if (biblioteca == null)
            biblioteca = BibliotecaDeAnimacoes.Padrao;

        if (biblioteca == null)
            return null;

        string escolhido = biblioteca.Primeiro(nomes);

        if (escolhido != null)
            Tocar(escolhido);

        return escolhido;
    }

    /// <summary>Reinicia o clipe atual do primeiro quadro.</summary>
    public void Reiniciar()
    {
        if (clipe != null)
            Definir(clipe);
    }

    public bool TemClipe(string nome)
    {
        if (biblioteca == null)
            biblioteca = BibliotecaDeAnimacoes.Padrao;

        return biblioteca != null && biblioteca.Tem(nome);
    }

    /// <summary>Segundos que o clipe dura, pra quem precisa cronometrar um golpe.</summary>
    public float DuracaoDe(string nome)
    {
        if (biblioteca == null)
            biblioteca = BibliotecaDeAnimacoes.Padrao;

        ClipeDeSprites c = biblioteca != null ? biblioteca.Buscar(nome) : null;
        return c != null ? c.Duracao : 0f;
    }

    // ---------------- interno ----------------
    private void Definir(ClipeDeSprites novo)
    {
        clipe = novo;
        tempo = 0f;
        quadro = -1;
        terminou = false;
        avisouFim = false;

        Mostrar(0);
    }

    private void Mostrar(int indice)
    {
        if (indice == quadro)
            return;

        quadro = indice;
        renderizador.sprite = clipe.Quadro(indice);
    }

    private void AvisarFim()
    {
        if (avisouFim)
            return;

        avisouFim = true;
        AoTerminar?.Invoke(clipe.nome);
    }
}
