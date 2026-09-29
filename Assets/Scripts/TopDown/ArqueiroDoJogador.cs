using UnityEngine;

/// <summary>
/// Anima o jogador com o heroi escolhido (<see cref="Herois"/>): parado ou correndo conforme
/// a velocidade, o golpe a cada disparo do <see cref="AtiradorTopDown"/> (o arqueiro azul
/// tem tiro pra cima, pro lado e pra baixo; os do Tiny RPG, so de lado) e a morte.
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

    private int primeiroQuadroDoTiro = 3;

    private ClipesDePersonagem clipes;
    private SpriteRenderer desenho;
    private Rigidbody2D corpo;
    private AtiradorTopDown atirador;
    private Vida vida;

    private Sprite[] tocando;
    private float inicio;
    private float fps;
    private bool umaVez;
    private bool morto;

    /// <summary>
    /// Liga a animacao (ou troca de heroi, chamando de novo). O tiro sai na hora, entao o
    /// golpe comeca em <paramref name="quadroDoTiro"/>, com o arco ja puxado; -1 = no meio.
    /// </summary>
    public void Configurar(ClipesDePersonagem novosClipes, SpriteRenderer renderizador, int quadroDoTiro = 3)
    {
        Desligar();

        clipes = novosClipes;
        primeiroQuadroDoTiro = quadroDoTiro;
        morto = false;
        desenho = renderizador;
        corpo = GetComponent<Rigidbody2D>();
        atirador = GetComponent<AtiradorTopDown>();
        vida = GetComponent<Vida>();

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

        Sprite[] quadros = clipes.Ataque;

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

        if (!morto && (!umaVez || terminou))
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

        if (tocando != alvo || umaVez)
            Tocar(alvo, quadrosPorSegundo, false);
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
