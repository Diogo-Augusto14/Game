using UnityEngine;

/// <summary>
/// Anima um personagem importado (<see cref="ClipesDePersonagem"/>) visto de cima, sem
/// Animator: parado ou andando conforme a velocidade do corpo, dor quando apanha, morte
/// quando o Vida zera, e os ataques que o proprio inimigo pede com <see cref="TocarUmaVez"/>.
///
/// A arte olha pra direita. O desenho vira (flipX) pro lado em que o bicho anda; parado ou
/// atacando, vira pro lado que o inimigo mandar em <see cref="OlharPara"/>.
/// </summary>
[DisallowMultipleComponent]
public class AnimacaoDePersonagem : MonoBehaviour
{
    [SerializeField, Min(1f)] private float quadrosPorSegundo = 8f;

    [Tooltip("Abaixo desta velocidade conta como parado")]
    [SerializeField, Min(0f)] private float velocidadeParaAndar = 0.15f;

    [Tooltip("Segundos mostrando a dor depois de apanhar")]
    [SerializeField, Min(0f)] private float tempoDeDor = 0.25f;

    private ClipesDePersonagem clipes;
    private SpriteRenderer desenho;
    private Rigidbody2D corpo;
    private Vida vida;

    private Sprite[] tocando;
    private float inicio;
    private float quadrosPorSegundoAgora;
    private bool umaVez;
    private float fimDaDor;
    private bool morto;

    /// <summary>Liga a animacao. Chame logo depois do AddComponent.</summary>
    public void Configurar(ClipesDePersonagem novosClipes, SpriteRenderer renderizador)
    {
        clipes = novosClipes;
        desenho = renderizador;
        corpo = GetComponent<Rigidbody2D>();
        vida = GetComponent<Vida>();

        if (vida != null)
        {
            vida.AoTomarDano.AddListener(AoApanhar);
            vida.AoMorrer.AddListener(AoMorrer);
        }

        Trocar(clipes.Parado, quadrosPorSegundo, false);
    }

    private void OnDestroy()
    {
        if (vida != null)
        {
            vida.AoTomarDano.RemoveListener(AoApanhar);
            vida.AoMorrer.RemoveListener(AoMorrer);
        }
    }

    /// <summary>True enquanto um ataque (ou a morte) esta tocando ate o fim.</summary>
    public bool OcupadoComAtaque => umaVez && !Terminou;

    /// <summary>
    /// Toca os quadros uma vez, na velocidade pedida, e volta pro normal. Use pra ataques:
    /// escolha o fps pra o quadro do golpe cair no momento do dano.
    /// </summary>
    public void TocarUmaVez(Sprite[] quadros, float fps)
    {
        if (morto || quadros == null || quadros.Length == 0)
            return;

        Trocar(quadros, fps, true);
    }

    /// <summary>Vira o desenho pro lado dado (so o sinal de x importa).</summary>
    public void OlharPara(Vector2 direcao)
    {
        if (desenho != null && Mathf.Abs(direcao.x) > 0.01f)
            desenho.flipX = direcao.x < 0f;
    }

    private void AoApanhar(DanoInfo info)
    {
        if (morto || OcupadoComAtaque || clipes.Dor == null)
            return;

        fimDaDor = Time.time + tempoDeDor;
        Trocar(clipes.Dor, clipes.Dor.Length / Mathf.Max(0.05f, tempoDeDor), false);
    }

    private void AoMorrer()
    {
        morto = true;

        if (clipes.Morte != null)
            Trocar(clipes.Morte, quadrosPorSegundo, true);
    }

    private void Update()
    {
        if (desenho == null || clipes == null)
            return;

        if (!morto && (!umaVez || Terminou))
            EscolherPeloMovimento();

        int quadro = Mathf.FloorToInt((Time.time - inicio) * quadrosPorSegundoAgora);
        quadro = umaVez ? Mathf.Min(quadro, tocando.Length - 1) : quadro % tocando.Length;
        desenho.sprite = tocando[quadro];
    }

    private void EscolherPeloMovimento()
    {
        if (Time.time < fimDaDor)
            return;

        Vector2 velocidade = corpo != null ? corpo.linearVelocity : Vector2.zero;
        bool andando = velocidade.magnitude > velocidadeParaAndar && clipes.Andando != null;

        if (andando)
            OlharPara(velocidade);

        Sprite[] alvo = andando ? clipes.Andando : clipes.Parado;

        if (tocando != alvo || umaVez)
            Trocar(alvo, quadrosPorSegundo, false);
    }

    private bool Terminou => tocando == null || (Time.time - inicio) * quadrosPorSegundoAgora >= tocando.Length;

    private void Trocar(Sprite[] quadros, float fps, bool soUmaVez)
    {
        if (quadros == null || quadros.Length == 0)
            return;

        tocando = quadros;
        quadrosPorSegundoAgora = fps;
        umaVez = soUmaVez;
        inicio = Time.time;
    }
}
