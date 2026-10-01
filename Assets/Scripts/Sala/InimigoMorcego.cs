using UnityEngine;

/// <summary>
/// Os bichos que voam baixo e machucam encostando, cada um com o seu voo:
///
///   Enxame   (morceguinho) -> voo nervoso, como as moscas do Isaac: vai pra um ponto sorteado
///                             perto do jogador, troca de ponto toda hora e treme no caminho.
///                             Nunca vem reto, entao em grupo vira uma nuvem dificil de mirar.
///   Mergulho (morcego)     -> circula o jogador a uns 3 de distancia e, de tempos em tempos,
///                             para no ar, pisca (o aviso) e mergulha reto onde o jogador estava;
///                             depois sobe e volta a circular. A hora de acertar e logo depois.
///
/// Voam: buraco nao segura (Fosso.Sobrevoar, pela fabrica).
/// </summary>
public class InimigoMorcego : InimigoDeSala
{
    public enum Voo { Enxame, Mergulho }

    [SerializeField] private Voo voo = Voo.Enxame;

    [Header("Mergulho")]
    [SerializeField, Min(0.5f)] private float raioDoCirculo = 3f;

    [SerializeField, Min(0.3f)] private float intervaloEntreMergulhos = 2.6f;

    [SerializeField, Min(0f)] private float avisoDoMergulho = 0.4f;

    [SerializeField, Min(0.5f)] private float velocidadeDoMergulho = 8f;

    [SerializeField, Min(0.05f)] private float duracaoDoMergulho = 0.45f;

    private Cronometro recarga;
    private Cronometro aviso;
    private Cronometro mergulho;
    private Cronometro trocaDePonto;
    private Vector2 pontoDoEnxame;
    private Vector2 rumoDoMergulho;
    private float sentido = 1f;
    private float danoNormal;
    private Vector3 posicaoDoDesenho;

    /// <summary>A fabrica escolhe o voo de cada morcego.</summary>
    public void UsarVoo(Voo novo) => voo = novo;

    protected override void Awake()
    {
        base.Awake();
        danoNormal = danoDeContato;
        sentido = Random.value < 0.5f ? -1f : 1f;
        recarga.Forcar(intervaloEntreMergulhos * Random.Range(0.6f, 1.2f));

        if (desenho != null)
            posicaoDoDesenho = desenho.transform.localPosition;
    }

    protected override void AtualizarAgindo(float dt)
    {
        if (voo == Voo.Enxame)
            VoarEmEnxame(dt);
        else
            Circular(dt);
    }

    private void VoarEmEnxame(float dt)
    {
        trocaDePonto.Contar(dt);

        if (!trocaDePonto.Ativo || (pontoDoEnxame - rb.position).sqrMagnitude < 0.2f)
        {
            Vector2 centro = jogador != null ? (Vector2)jogador.position : rb.position;
            pontoDoEnxame = centro + Random.insideUnitCircle * 1.6f;
            trocaDePonto.Forcar(Random.Range(0.25f, 0.55f));
        }

        Vector2 rumo = (pontoDoEnxame - rb.position).normalized + Random.insideUnitCircle * 0.6f;
        Andar(rumo, velocidade);
    }

    private void Circular(float dt)
    {
        // Volta ao normal mesmo se um golpe interrompeu o mergulho no meio.
        danoDeContato = danoNormal;
        Tremer(0f);
        recarga.Contar(dt);

        Vector2 alvo = ParaOJogador();
        float distancia = alvo.magnitude;

        if (distancia < 0.0001f)
        {
            Frear();
            return;
        }

        if (!recarga.Ativo && distancia < raioDoCirculo + 2.5f)
        {
            EstadoAtual = Estado.Preparando;
            aviso.Forcar(avisoDoMergulho);
            Frear();
            return;
        }

        Vector2 frente = alvo / distancia;
        Vector2 lado = new Vector2(-frente.y, frente.x) * sentido;
        Vector2 rumo = lado + frente * Mathf.Clamp(distancia - raioDoCirculo, -1f, 1f);

        if (Physics2D.CircleCast(rb.position, Raio * 0.9f, rumo.normalized, 0.4f, Camadas.MascaraDeParede).collider != null)
            sentido = -sentido;

        Andar(rumo, velocidade);
    }

    protected override void AtualizarPreparando(float dt)
    {
        if (voo == Voo.Enxame)
        {
            EstadoAtual = Estado.Agindo;
            return;
        }

        // Parado no ar, tremendo: o aviso. O rumo trava no fim, onde o jogador estiver.
        if (aviso.Ativo)
        {
            Frear();
            aviso.Contar(dt);
            Tremer(0.06f);

            if (!aviso.Ativo)
            {
                Tremer(0f);
                Vector2 alvo = ParaOJogador();
                rumoDoMergulho = alvo.sqrMagnitude > 0.0001f ? alvo.normalized : Vector2.down;
                mergulho.Forcar(duracaoDoMergulho);
                danoDeContato = danoNormal * 1.5f;
            }

            return;
        }

        mergulho.Contar(dt);
        rb.linearVelocity = rumoDoMergulho * velocidadeDoMergulho;

        bool bateu = Physics2D.CircleCast(rb.position, Raio * 0.9f, rumoDoMergulho, velocidadeDoMergulho * dt + 0.05f,
                                          Camadas.MascaraDeParede).collider != null;

        if (mergulho.Ativo && !bateu)
            return;

        rb.linearVelocity *= 0.3f;
        danoDeContato = danoNormal;
        recarga.Forcar(intervaloEntreMergulhos);
        sentido = Random.value < 0.5f ? -1f : 1f;
        EstadoAtual = Estado.Agindo;
    }

    private void Tremer(float forca)
    {
        if (desenho != null)
            desenho.transform.localPosition = posicaoDoDesenho + (Vector3)(Random.insideUnitCircle * forca);
    }
}
