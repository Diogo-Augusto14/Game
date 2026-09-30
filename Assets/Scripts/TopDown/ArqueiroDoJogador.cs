using UnityEngine;

/// <summary>
/// Anima o jogador com o heroi escolhido (<see cref="Herois"/>): parado ou correndo conforme
/// a velocidade, o golpe a cada disparo do <see cref="AtiradorTopDown"/> (o arqueiro azul
/// tem tiro pra cima, pro lado e pra baixo; os do Tiny RPG, so de lado) e a morte.
/// Os herois de espada alternam os ataques da folha a cada golpe (combo de 2 ou 3), e no
/// dash o heroi corre acelerado (o rastro e a poeira sao do <see cref="RastroDoDash"/>).
///
/// O desenho e o SpriteRenderer da raiz (o Vida pisca ele); aqui so troca o sprite e o flipX.
/// </summary>
[DisallowMultipleComponent]
public class ArqueiroDoJogador : MonoBehaviour
{
    [SerializeField, Min(1f)] private float quadrosPorSegundo = 10f;

    [Tooltip("Abaixo desta velocidade conta como parado")]
    [SerializeField, Min(0f)] private float velocidadeParaAndar = 0.2f;

    [Tooltip("Segundos da animacao de tiro (encurta se a cadencia for mais rapida)")]
    [SerializeField, Min(0.05f)] private float duracaoDoTiro = 0.3f;

    [Tooltip("Quanto a corrida acelera durante o dash")]
    [SerializeField, Min(1f)] private float aceleracaoNoDash = 2.5f;

    private int primeiroQuadroDoTiro = 3;
    private bool emCombo;
    private int golpeDoCombo;

    private ClipesDePersonagem clipes;
    private SpriteRenderer desenho;
    private Rigidbody2D corpo;
    private AtiradorTopDown atirador;
    private Vida vida;
    private MovimentoTopDown movimento;

    private Sprite[] tocando;
    private float inicio;
    private float fps;
    private bool umaVez;
    private bool morto;

    /// <summary>
    /// Liga a animacao (ou troca de heroi, chamando de novo). O tiro sai na hora, entao o
    /// golpe comeca em <paramref name="quadroDoTiro"/>, com o arco ja puxado; -1 = no meio.
    /// </summary>
    public void Configurar(ClipesDePersonagem novosClipes, SpriteRenderer renderizador, int quadroDoTiro = 3,
                           bool alternarAtaques = false)
    {
        Desligar();

        clipes = novosClipes;
        primeiroQuadroDoTiro = quadroDoTiro;
        emCombo = alternarAtaques;
        golpeDoCombo = 0;
        morto = false;
        desenho = renderizador;
        corpo = GetComponent<Rigidbody2D>();
        atirador = GetComponent<AtiradorTopDown>();
        vida = GetComponent<Vida>();
        movimento = GetComponent<MovimentoTopDown>();

        if (atirador != null)
            atirador.AoAtirar += Atirou;

        if (vida != null)
            vida.AoMorrer.AddListener(Morreu);

        if (desenho != null)
            desenho.flipX = false;

        Tocar(clipes.Parado, quadrosPorSegundo, false);
    }

    private void OnDestroy() => Desligar();

    private void Desligar()
    {
        if (atirador != null)
            atirador.AoAtirar -= Atirou;

        if (vida != null)
            vida.AoMorrer.RemoveListener(Morreu);
    }

    private void Atirou(Vector2 direcao)
    {
        if (morto)
            return;

        Sprite[] quadros = emCombo ? GolpeDoCombo() : clipes.Ataque;

        if (Mathf.Abs(direcao.y) > Mathf.Abs(direcao.x))
            quadros = (direcao.y > 0f ? clipes.AtaqueCima : clipes.AtaqueBaixo) ?? quadros;
        else if (Mathf.Abs(direcao.x) > 0.01f)
            desenho.flipX = direcao.x < 0f;

        if (quadros == null || quadros.Length == 0)
            return;

        int primeiro = primeiroQuadroDoTiro < 0 ? quadros.Length / 3 : primeiroQuadroDoTiro;
        primeiro = Mathf.Clamp(primeiro, 0, quadros.Length - 1);

        float duracao = atirador != null ? Mathf.Min(duracaoDoTiro, 1f / atirador.TirosPorSegundo) : duracaoDoTiro;
        Sprite[] soltando = new Sprite[quadros.Length - primeiro];
        System.Array.Copy(quadros, primeiro, soltando, 0, soltando.Length);
        Tocar(soltando, soltando.Length / duracao, true);
    }

    /// <summary>Ataque 1, ataque 2 e (se a folha tiver) ataque 3, em roda.</summary>
    private Sprite[] GolpeDoCombo()
    {
        Sprite[][] golpes = { clipes.Ataque, clipes.AtaqueEspecial, clipes.AtaqueForte };

        for (int tentativa = 0; tentativa < golpes.Length; tentativa++)
        {
            Sprite[] golpe = golpes[golpeDoCombo % golpes.Length];
            golpeDoCombo = (golpeDoCombo + 1) % golpes.Length;

            if (golpe != null && golpe.Length > 0)
                return golpe;
        }

        return clipes.Ataque;
    }

    private void Morreu()
    {
        morto = true;
        desenho.flipX = false;

        if (clipes.Morte != null)
            Tocar(clipes.Morte, 12f, true);
    }

    private void Update()
    {
        if (desenho == null || clipes == null)
            return;

        bool terminou = (Time.time - inicio) * fps >= tocando.Length;

        // O dash corta o golpe no meio: a arrancada tem que aparecer na hora.
        bool dashando = movimento != null && movimento.Dashando;

        if (!morto && (!umaVez || terminou || dashando))
            EscolherPeloMovimento();

        int quadro = Mathf.FloorToInt((Time.time - inicio) * fps);
        quadro = umaVez ? Mathf.Min(quadro, tocando.Length - 1) : quadro % tocando.Length;
        desenho.sprite = tocando[quadro];
    }

    private void EscolherPeloMovimento()
    {
        Vector2 velocidade = corpo != null ? corpo.linearVelocity : Vector2.zero;
        bool andando = velocidade.magnitude > velocidadeParaAndar && clipes.Andando != null;

        if (andando && Mathf.Abs(velocidade.x) > 0.05f)
            desenho.flipX = velocidade.x < 0f;

        Sprite[] alvo = andando ? clipes.Andando : clipes.Parado;
        float alvoFps = andando && movimento != null && movimento.Dashando ? quadrosPorSegundo * aceleracaoNoDash : quadrosPorSegundo;

        if (tocando != alvo || umaVez || !Mathf.Approximately(fps, alvoFps))
            Tocar(alvo, alvoFps, false);
    }

    private void Tocar(Sprite[] quadros, float novoFps, bool soUmaVez)
    {
        if (quadros == null || quadros.Length == 0)
            return;

        tocando = quadros;
        fps = novoFps;
        umaVez = soUmaVez;
        inicio = Time.time;
    }
}
