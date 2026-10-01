using UnityEngine;

/// <summary>
/// O Monstro de Sangue (arte do Tiny RPG pack): lento e resistente. Vai se arrastando
/// na direcao do jogador e, de tempos em tempos, para, se contorce e espirra sangue em
/// volta, num anel de tiros.
///
///   Agindo      -> se arrasta ate o jogador
///   Preparando  -> a animacao do ataque especial: o telegrafo do anel
///   Recuperando -> parado um instante depois de espirrar
///
/// No andar 3 em diante o anel vem com mais gotas (veja <see cref="Endurecer"/>).
/// </summary>
public class InimigoDeSangue : InimigoDeSala
{
    [Header("Anel de sangue")]
    [SerializeField, Min(0.1f)] private float intervaloEntreAneis = 2.8f;

    [Tooltip("Telegrafo: segundos se contorcendo antes do anel sair")]
    [SerializeField, Min(0f)] private float tempoDePreparo = 0.6f;

    [SerializeField, Min(0f)] private float tempoDeRecuperacao = 0.5f;

    [SerializeField, Min(1)] private int gotasPorAnel = 6;

    [SerializeField, Min(0f)] private float danoDaGota = 10f;

    [SerializeField, Min(0.1f)] private float velocidadeDaGota = 3.5f;

    [SerializeField] private Color corDaGota = new Color(0.75f, 0.05f, 0.12f);

    private AnimacaoDePersonagem animacao;
    private ClipesDePersonagem clipes;
    private Cronometro recarga;
    private Cronometro preparo;
    private Cronometro recuperacao;
    private float giro;

    public void UsarArte(AnimacaoDePersonagem novaAnimacao, ClipesDePersonagem novosClipes)
    {
        animacao = novaAnimacao;
        clipes = novosClipes;
    }

    /// <summary>Mais gotas por anel (andares mais fundos).</summary>
    public void Endurecer() => gotasPorAnel = 8;

    protected override void Awake()
    {
        base.Awake();
        velocidade = 0.9f;
        recarga.Forcar(intervaloEntreAneis * 0.6f);
        giro = Random.value * 360f;
    }

    protected override void AtualizarAgindo(float dt)
    {
        recarga.Contar(dt);

        if (!recarga.Ativo)
        {
            EstadoAtual = Estado.Preparando;
            preparo.Forcar(tempoDePreparo);
            Frear();

            animacao?.OlharPara(ParaOJogador());

            Sprite[] quadros = clipes != null ? (clipes.AtaqueEspecial ?? clipes.Ataque) : null;

            if (quadros != null)
                animacao?.TocarUmaVez(quadros, quadros.Length / Mathf.Max(0.05f, tempoDePreparo + tempoDeRecuperacao));

            return;
        }

        Vector2 alvo = ParaOJogador();

        if (alvo.sqrMagnitude > 0.25f)
            Aproximar(alvo, velocidade, dt);
        else
            Frear();
    }

    protected override void AtualizarPreparando(float dt)
    {
        Frear();
        preparo.Contar(dt);

        if (preparo.Ativo)
            return;

        Espirrar();
        EstadoAtual = Estado.Recuperando;
        recuperacao.Forcar(tempoDeRecuperacao);
    }

    protected override void AtualizarRecuperando(float dt)
    {
        Frear();
        recuperacao.Contar(dt);

        if (recuperacao.Ativo)
            return;

        recarga.Forcar(intervaloEntreAneis);
        EstadoAtual = Estado.Agindo;
    }

    private void Espirrar()
    {
        Sons.Tocar(Som.TiroInimigo, 0.8f);

        // Cada anel sai girado meio passo em relacao ao anterior: o buraco seguro muda de lugar.
        float passo = 360f / gotasPorAnel;
        giro += passo * 0.5f;

        for (int i = 0; i < gotasPorAnel; i++)
        {
            float angulo = (giro + passo * i) * Mathf.Deg2Rad;
            Vector2 direcao = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));

            TiroDaSala.Disparar(rb.position + direcao * (Raio + 0.1f), direcao * velocidadeDaGota,
                              danoDaGota, gameObject, true, corDaGota, 0.26f);
        }
    }

    protected override void Morrer()
    {
        Vector3 escala = transform.localScale;
        base.Morrer();
        transform.localScale = escala;
    }
}
