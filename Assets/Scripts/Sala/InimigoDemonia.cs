using UnityEngine;

/// <summary>
/// Demonia: corre em circulo em volta do jogador a meia distancia, sem parar, e de tempos em
/// tempos freia, abre os bracos (o aviso) e solta tres bolas de fogo em leque; depois volta a
/// circular, as vezes pro outro lado. Nao foge nem chega perto: o perigo e o leque chegando de
/// um angulo diferente a cada vez.
///
///   Agindo      -> circula
///   Preparando  -> freia e mira (a animacao de ataque)
///   Recuperando -> pausa curta
/// </summary>
public class InimigoDemonia : InimigoDeSala
{
    [SerializeField, Min(0.5f)] private float raio = 4f;

    [SerializeField, Min(0.3f)] private float intervaloEntreLeques = 2.2f;

    [SerializeField, Min(0f)] private float tempoDePreparo = 0.45f;

    [SerializeField, Min(0f)] private float danoDoTiro = 10f;

    [SerializeField, Range(0f, 60f)] private float aberturaDoLeque = 22f;

    [SerializeField] private Color corDoTiro = new Color(1f, 0.5f, 0.3f);

    private Cronometro recarga;
    private Cronometro preparo;
    private Cronometro pausa;
    private float sentido = 1f;

    protected override void Awake()
    {
        base.Awake();
        sentido = Random.value < 0.5f ? -1f : 1f;
        recarga.Forcar(intervaloEntreLeques * Random.Range(0.5f, 1f));
    }

    private float trocaDoPendulo;

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

        if (!recarga.Ativo && VeOJogador())
        {
            EstadoAtual = Estado.Preparando;
            preparo.Forcar(tempoDePreparo);
            Frear();
            return;
        }

        // Pendulo: anda num arco em volta do jogador e volta pelo mesmo arco (troca o sentido a
        // cada 1,2 a 1,8 s), em vez de dar a volta inteira como o fogo-fatuo.
        trocaDoPendulo -= dt;

        if (trocaDoPendulo <= 0f)
        {
            sentido = -sentido;
            trocaDoPendulo = Random.Range(1.2f, 1.8f);
        }

        Vector2 frente = alvo / distancia;
        Vector2 lado = new Vector2(-frente.y, frente.x) * sentido;
        Vector2 rumo = lado + frente * Mathf.Clamp(distancia - raio, -1f, 1f);

        // Parede ou pedra no caminho do circulo: troca o sentido.
        if (Physics2D.CircleCast(rb.position, Raio * 0.9f, rumo.normalized, 0.45f, Camadas.MascaraDeParede).collider != null)
            sentido = -sentido;

        Andar(rumo, velocidade * 1.3f);
    }

    protected override void AtualizarPreparando(float dt)
    {
        Frear();
        preparo.Contar(dt);

        if (preparo.Ativo)
            return;

        float angulo = AnguloDoJogador();

        for (int i = -1; i <= 1; i++)
            Disparar(angulo + i * aberturaDoLeque, 4.5f, danoDoTiro, corDoTiro);

        recarga.Forcar(intervaloEntreLeques);
        pausa.Forcar(0.25f);

        if (Random.value < 0.4f)
            sentido = -sentido;

        EstadoAtual = Estado.Recuperando;
    }

    protected override void AtualizarRecuperando(float dt)
    {
        Frear();
        pausa.Contar(dt);

        if (!pausa.Ativo)
            EstadoAtual = Estado.Agindo;
    }
}
