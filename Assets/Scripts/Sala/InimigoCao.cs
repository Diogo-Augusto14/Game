using UnityEngine;

/// <summary>
/// Cao infernal: corre atras do jogador como o perseguidor (zigue-zague, contornando pedra) e,
/// chegando a uns 3 de distancia e vendo o jogador, se agacha um instante e da um BOTE: um
/// salto rapido e reto pra onde o jogador estava. Depois do bote fica ofegante parado, que e a
/// hora de acertar. Desviar pro lado na hora do agacho faz ele passar direto.
///
///   Agindo      -> persegue
///   Preparando  -> agacha (aviso) e salta
///   Recuperando -> ofegante, parado
/// </summary>
public class InimigoCao : InimigoPerseguidor
{
    [SerializeField, Min(0.5f)] private float distanciaDoBote = 3.2f;

    [SerializeField, Min(0f)] private float tempoAgachado = 0.3f;

    [SerializeField, Min(0.5f)] private float velocidadeDoBote = 9f;

    [SerializeField, Min(0.05f)] private float duracaoDoBote = 0.32f;

    [SerializeField, Min(0f)] private float tempoOfegante = 0.6f;

    [SerializeField, Min(0.3f)] private float intervaloEntreBotes = 1.8f;

    private Cronometro recarga;
    private Cronometro agacho;
    private Cronometro bote;
    private Cronometro ofegante;
    private Vector2 rumoDoBote;
    private float danoNormal;
    private Vector3 escalaDoDesenho;

    protected override void Awake()
    {
        base.Awake();

        // Cao de faro: vem em linha reta e rapido, sem o balanco de lado (o zigue-zague e do goblin).
        zigueZague = 0f;
        velocidade *= 1.2f;
        danoNormal = danoDeContato;
        recarga.Forcar(intervaloEntreBotes * 0.5f);

        if (desenho != null)
            escalaDoDesenho = desenho.transform.localScale;
    }

    protected override void AtualizarAgindo(float dt)
    {
        recarga.Contar(dt);
        danoDeContato = danoNormal;
        Agachar(0f);

        Vector2 alvo = ParaOJogador();

        if (!recarga.Ativo && alvo.magnitude < distanciaDoBote && VeOJogador())
        {
            EstadoAtual = Estado.Preparando;
            agacho.Forcar(tempoAgachado);
            Frear();
            return;
        }

        base.AtualizarAgindo(dt);
    }

    protected override void AtualizarPreparando(float dt)
    {
        if (agacho.Ativo)
        {
            Frear();
            agacho.Contar(dt);
            Agachar(1f - agacho.Restante / Mathf.Max(0.01f, tempoAgachado));

            if (!agacho.Ativo)
            {
                Agachar(0f);
                Vector2 alvo = ParaOJogador();
                rumoDoBote = alvo.sqrMagnitude > 0.0001f ? alvo.normalized : Vector2.down;
                bote.Forcar(duracaoDoBote);
                danoDeContato = danoNormal * 1.5f;
                Sons.Tocar(Som.Rugido, 0.35f);
            }

            return;
        }

        bote.Contar(dt);
        rb.linearVelocity = rumoDoBote * velocidadeDoBote;

        bool bateu = Physics2D.CircleCast(rb.position, Raio * 0.9f, rumoDoBote, velocidadeDoBote * dt + 0.05f,
                                          Camadas.MascaraDeParede).collider != null;

        if (bote.Ativo && !bateu)
            return;

        rb.linearVelocity = Vector2.zero;
        danoDeContato = danoNormal;
        ofegante.Forcar(tempoOfegante);
        EstadoAtual = Estado.Recuperando;
    }

    protected override void AtualizarRecuperando(float dt)
    {
        Frear();
        ofegante.Contar(dt);

        if (ofegante.Ativo)
            return;

        recarga.Forcar(intervaloEntreBotes);
        EstadoAtual = Estado.Agindo;
    }

    /// <summary>Achata o desenho (0 = normal, 1 = bem agachado).</summary>
    private void Agachar(float quanto)
    {
        if (desenho != null)
            desenho.transform.localScale = new Vector3(escalaDoDesenho.x * (1f + 0.15f * quanto), escalaDoDesenho.y * (1f - 0.25f * quanto), escalaDoDesenho.z);
    }
}
