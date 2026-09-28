using UnityEngine;

/// <summary>
/// Mantem distancia e atira no jogador. O ciclo:
///
///   Agindo      -> se afasta se o jogador chegou perto, se aproxima se esta longe,
///                  e anda de lado no meio-termo (fica dificil de acertar)
///   Preparando  -> para e incha um pouco: o telegrafo do tiro
///   Recuperando -> atirou; fica parado um instante antes de voltar a andar
///
/// So atira vendo o jogador (sem parede no meio).
/// </summary>
public class InimigoAtirador : InimigoDeSala
{
    [Header("Distancia")]
    [Tooltip("Mais perto que isto, ele recua")]
    [SerializeField, Min(0f)] private float distanciaMinima = 2.5f;

    [Tooltip("Mais longe que isto, ele chega perto")]
    [SerializeField, Min(0f)] private float distanciaMaxima = 5f;

    [Header("Tiro")]
    [Tooltip("Segundos entre um tiro e outro (contando do fim do anterior)")]
    [SerializeField, Min(0.1f)] private float intervaloEntreTiros = 1.6f;

    [Tooltip("Telegrafo: segundos parado antes do tiro sair")]
    [SerializeField, Min(0f)] private float tempoDePreparo = 0.45f;

    [SerializeField, Min(0f)] private float tempoDeRecuperacao = 0.35f;

    [SerializeField, Min(0f)] private float danoDoTiro = 10f;

    [SerializeField, Min(0.1f)] private float velocidadeDoTiro = 5f;

    [Tooltip("Quantos tiros por vez, em leque")]
    [SerializeField, Min(1)] private int tirosPorVez = 1;

    [Tooltip("Abertura do leque em graus (so conta com mais de um tiro)")]
    [SerializeField, Range(0f, 90f)] private float aberturaDoLeque = 20f;

    [SerializeField] private Color corDoTiro = new Color(1f, 0.45f, 0.3f);

    private Cronometro recarga;
    private Cronometro preparo;
    private Cronometro recuperacao;
    private float ladoDoPasso = 1f;
    private Vector3 escalaOriginal;

    protected override void Awake()
    {
        base.Awake();
        escalaOriginal = transform.localScale;
        ladoDoPasso = Random.value < 0.5f ? -1f : 1f;

        // Nao atira no mesmo instante em que acorda.
        recarga.Forcar(intervaloEntreTiros * 0.5f);
    }

    protected override void AtualizarAgindo(float dt)
    {
        recarga.Contar(dt);

        Vector2 alvo = ParaOJogador();
        float distancia = alvo.magnitude;

        if (distancia < 0.0001f)
        {
            Frear();
            return;
        }

        Vector2 frente = alvo / distancia;
        Vector2 lado = new Vector2(-frente.y, frente.x) * ladoDoPasso;

        if (!recarga.Ativo && VeOJogador())
        {
            EstadoAtual = Estado.Preparando;
            preparo.Forcar(tempoDePreparo);
            Frear();
            return;
        }

        if (distancia < distanciaMinima)
            Andar(-frente + lado * 0.3f, velocidade);
        else if (distancia > distanciaMaxima)
            Andar(frente, velocidade);
        else
            Andar(lado, velocidade * 0.6f);
    }

    protected override void AtualizarPreparando(float dt)
    {
        Frear();
        preparo.Contar(dt);

        // Incha conforme o tiro carrega.
        float t = tempoDePreparo <= 0f ? 1f : 1f - preparo.Restante / tempoDePreparo;
        transform.localScale = escalaOriginal * (1f + 0.25f * t);

        if (preparo.Ativo)
            return;

        transform.localScale = escalaOriginal;
        Atirar();

        EstadoAtual = Estado.Recuperando;
        recuperacao.Forcar(tempoDeRecuperacao);
    }

    protected override void AtualizarRecuperando(float dt)
    {
        Frear();
        recuperacao.Contar(dt);

        if (recuperacao.Ativo)
            return;

        recarga.Forcar(intervaloEntreTiros);
        ladoDoPasso = -ladoDoPasso;   // troca o lado do passo a cada tiro
        EstadoAtual = Estado.Agindo;
    }

    private void Atirar()
    {
        Vector2 alvo = ParaOJogador();

        if (alvo.sqrMagnitude < 0.0001f)
            return;

        float anguloBase = Mathf.Atan2(alvo.y, alvo.x) * Mathf.Rad2Deg;

        for (int i = 0; i < tirosPorVez; i++)
        {
            float desvio = tirosPorVez == 1 ? 0f : Mathf.Lerp(-aberturaDoLeque, aberturaDoLeque, i / (float)(tirosPorVez - 1));
            float angulo = (anguloBase + desvio) * Mathf.Deg2Rad;
            Vector2 direcao = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));

            TiroDaSala.Disparar(rb.position + direcao * 0.35f, direcao * velocidadeDoTiro,
                              danoDoTiro, gameObject, true, corDoTiro);
        }
    }

    protected override void Morrer()
    {
        transform.localScale = escalaOriginal;
        base.Morrer();
    }
}
