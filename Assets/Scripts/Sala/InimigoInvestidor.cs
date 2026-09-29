using UnityEngine;

/// <summary>
/// Anda devagar em linha reta, virando de vez em quando, e quando o jogador fica na mesma
/// linha ou coluna (tipo o Charger do Isaac) treme um instante e dispara reto ate bater
/// em parede ou pedra. Batendo, fica tonto: a hora de acertar ele.
///
///   Agindo      -> vagueia nas quatro direcoes e espia se o jogador esta alinhado
///   Preparando  -> treme (aviso) e depois corre reto, com dano de contato maior
///   Recuperando -> tonto depois da corrida
/// </summary>
public class InimigoInvestidor : InimigoDeSala
{
    [Header("Vagar")]
    [Tooltip("Segundos andando numa direcao antes de sortear outra")]
    [SerializeField] private Vector2 tempoPorDirecao = new Vector2(0.8f, 1.8f);

    [Header("Investida")]
    [Tooltip("Quao perto da mesma linha/coluna o jogador precisa estar (unidades)")]
    [SerializeField, Min(0.05f)] private float toleranciaDeAlinhamento = 0.45f;

    [SerializeField, Min(0.5f)] private float alcance = 9f;

    [Tooltip("Aviso: segundos tremendo antes de sair correndo")]
    [SerializeField, Min(0f)] private float tempoDeAviso = 0.35f;

    [SerializeField, Min(0.1f)] private float velocidadeDaInvestida = 7.5f;

    [SerializeField, Min(0.1f)] private float duracaoMaxima = 1.6f;

    [SerializeField, Min(0f)] private float danoDaInvestida = 15f;

    [Tooltip("Segundos tonto depois de bater")]
    [SerializeField, Min(0f)] private float tontoAposBater = 0.8f;

    [Tooltip("Segundos entre uma investida e outra")]
    [SerializeField, Min(0f)] private float recarga = 1f;

    private static readonly Vector2[] Rumos = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

    private Vector2 rumoDoPasseio;
    private Vector2 rumoDaInvestida;
    private bool correndo;
    private float danoNormal;
    private Vector3 posicaoDoDesenho;

    private Cronometro trocaDeRumo;
    private Cronometro aviso;
    private Cronometro corrida;
    private Cronometro tonto;
    private Cronometro espera;

    protected override void Awake()
    {
        base.Awake();
        danoNormal = danoDeContato;

        if (desenho != null)
            posicaoDoDesenho = desenho.transform.localPosition;

        SortearRumo();
        espera.Forcar(recarga * 0.5f);
    }

    protected override void AtualizarAgindo(float dt)
    {
        trocaDeRumo.Contar(dt);
        espera.Contar(dt);
        danoDeContato = danoNormal;
        correndo = false;

        if (!espera.Ativo && Alinhado(out Vector2 rumo))
        {
            rumoDaInvestida = rumo;
            aviso.Forcar(tempoDeAviso);
            EstadoAtual = Estado.Preparando;
            Frear();
            return;
        }

        // Parede logo a frente: vira antes de encostar.
        bool bloqueado = Physics2D.CircleCast(rb.position, Raio * 0.9f, rumoDoPasseio, 0.4f, Camadas.MascaraDeParede).collider != null;

        if (!trocaDeRumo.Ativo || bloqueado)
            SortearRumo();

        Andar(rumoDoPasseio, velocidade);
    }

    protected override void AtualizarPreparando(float dt)
    {
        if (!correndo)
        {
            Frear();
            aviso.Contar(dt);
            Tremer(0.07f);

            if (aviso.Ativo)
                return;

            Tremer(0f);
            correndo = true;
            danoDeContato = danoDaInvestida;
            corrida.Forcar(duracaoMaxima);
            return;
        }

        corrida.Contar(dt);

        float passo = velocidadeDaInvestida * dt;
        RaycastHit2D parede = Physics2D.CircleCast(rb.position, Raio * 0.9f, rumoDaInvestida,
                                                   passo + 0.05f, Camadas.MascaraDeParede);

        if (parede.collider != null)
        {
            Parar(tontoAposBater);
            return;
        }

        rb.linearVelocity = rumoDaInvestida * velocidadeDaInvestida;

        if (!corrida.Ativo)
            Parar(0.3f);
    }

    private void Parar(float tempoTonto)
    {
        rb.linearVelocity = Vector2.zero;
        correndo = false;
        danoDeContato = danoNormal;
        tonto.Forcar(tempoTonto);
        EstadoAtual = Estado.Recuperando;
    }

    protected override void AtualizarRecuperando(float dt)
    {
        Frear();
        tonto.Contar(dt);
        Tremer(tonto.Restante > 0.3f ? 0.03f : 0f);

        if (tonto.Ativo)
            return;

        Tremer(0f);
        espera.Forcar(recarga);
        SortearRumo();
        EstadoAtual = Estado.Agindo;
    }

    /// <summary>Jogador na mesma linha ou coluna, perto e sem parede no meio.</summary>
    private bool Alinhado(out Vector2 rumo)
    {
        rumo = Vector2.zero;
        Vector2 alvo = ParaOJogador();

        if (alvo.sqrMagnitude < 0.0001f || alvo.magnitude > alcance || !VeOJogador())
            return false;

        if (Mathf.Abs(alvo.y) <= toleranciaDeAlinhamento)
            rumo = new Vector2(Mathf.Sign(alvo.x), 0f);
        else if (Mathf.Abs(alvo.x) <= toleranciaDeAlinhamento)
            rumo = new Vector2(0f, Mathf.Sign(alvo.y));

        return rumo != Vector2.zero;
    }

    private void SortearRumo()
    {
        Vector2 anterior = rumoDoPasseio;

        for (int i = 0; i < 4 && rumoDoPasseio == anterior; i++)
            rumoDoPasseio = Rumos[Random.Range(0, Rumos.Length)];

        trocaDeRumo.Forcar(Random.Range(tempoPorDirecao.x, tempoPorDirecao.y));
    }

    private void Tremer(float forca)
    {
        if (desenho != null)
            desenho.transform.localPosition = posicaoDoDesenho + (Vector3)(Random.insideUnitCircle * forca);
    }

    protected override void Morrer()
    {
        Tremer(0f);
        danoDeContato = danoNormal;
        base.Morrer();
    }
}
