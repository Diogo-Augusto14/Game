using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O chefe final, o Olho do Porao: um olho enorme preso no meio da sala do ultimo andar.
/// Nao anda nem e empurrado; a luta e desviar de tiro. A pupila segue o jogador, e a iris
/// muda de cor conforme o ataque que vem:
///
///   Anel     -> (amarela) anel de tiros com uma brecha: ache o buraco
///   Rajada   -> (laranja) fila de tiros mirados no jogador
///   Espiral  -> (azul) dois bracos de espiral girando
///   Laser    -> (vermelha, fase 2+) uma linha de mira e depois um jato de tiros nela
///   Invocar  -> (roxa) chama dois inimigos do porao
///
/// Tres fases: abaixo de 60% da vida os ataques dobram (dois aneis, rajada em leque) e
/// aparece o laser; abaixo de 25% ele solta tiros em cruz girando o tempo todo, mesmo
/// entre um ataque e outro. Vencer ele termina a partida.
/// </summary>
public class ChefeFinal : InimigoDeSala, IChefe
{
    private enum Ataque
    {
        Anel,
        Rajada,
        Espiral,
        Laser,
        Invocar
    }

    [Header("Chefe")]
    [SerializeField] private string nomeDoChefe = "Olho do Porao";

    [SerializeField, Min(0.1f)] private float intervaloEntreAtaques = 1f;

    [SerializeField, Min(0f)] private float esperaInicial = 1.5f;

    [SerializeField, Range(0f, 1f)] private float fracaoDaFase2 = 0.6f;

    [SerializeField, Range(0f, 1f)] private float fracaoDaFase3 = 0.25f;

    [Header("Tiros")]
    [SerializeField, Min(0f)] private float danoDoTiro = 10f;

    [SerializeField, Min(0.1f)] private float velocidadeDoTiro = 3.8f;

    [SerializeField, Min(0.05f)] private float diametroDoTiro = 0.36f;

    [SerializeField] private Color corDoTiro = new Color(0.95f, 0.3f, 0.6f);

    [Header("Anel")]
    [SerializeField, Min(8)] private int tirosNoAnel = 22;

    [Tooltip("Quantos tiros faltam na brecha")]
    [SerializeField, Min(1)] private int tamanhoDaBrecha = 4;

    [Header("Rajada")]
    [SerializeField, Min(1)] private int tirosNaRajada = 6;

    [SerializeField, Min(0.03f)] private float intervaloDaRajada = 0.12f;

    [Header("Espiral")]
    [SerializeField, Min(0.1f)] private float duracaoDaEspiral = 3f;

    [SerializeField, Min(0.03f)] private float intervaloDaEspiral = 0.11f;

    [Header("Laser")]
    [SerializeField, Min(0f)] private float avisoDoLaser = 1f;

    [SerializeField, Min(0.1f)] private float duracaoDoLaser = 0.7f;

    [Header("Invocar")]
    [SerializeField, Min(0)] private int maximoDeLacaios = 4;

    [Header("Fase 3")]
    [SerializeField, Min(0.1f)] private float intervaloDaCruz = 0.55f;

    // ---------------- estado ----------------
    private readonly List<InimigoDeSala> lacaios = new List<InimigoDeSala>();

    private Ataque ataqueAtual;
    private Ataque? ultimoAtaque;
    private bool executando;
    private bool barraCriada;
    private int fase = 1;
    private int disparos;
    private float angulo;
    private Vector2 rumoDoLaser;
    private Vector3 escalaOriginal;

    private Cronometro recarga;
    private Cronometro aviso;
    private Cronometro execucao;
    private Cronometro proximoDisparo;
    private Cronometro recuperacao;
    private Cronometro cruz;
    private float anguloDaCruz;

    private SpriteRenderer iris;
    private Transform pupila;
    private SpriteRenderer mira;
    private float raio;

    public string Nome => nomeDoChefe;

    public bool NaSegundaFase => fase >= 2;

    public int Fase => fase;

    protected override bool Imparavel => true;

    // ---------------- montagem ----------------
    /// <summary>Branco do olho, iris, pupila e a linha do laser. A fabrica chama.</summary>
    public void Enfeitar(float raioDoCorpo)
    {
        raio = raioDoCorpo;

        FormasDaSala.Desenho(transform, "Branco", FormasDaSala.Circulo(), new Color(0.95f, 0.92f, 0.88f),
            Vector2.zero, Vector2.one * raio * 1.6f, 11);
        iris = FormasDaSala.Desenho(transform, "Iris", FormasDaSala.Circulo(), new Color(0.3f, 0.75f, 0.4f),
            Vector2.zero, Vector2.one * raio * 0.9f, 12);
        pupila = FormasDaSala.Desenho(iris.transform, "Pupila", FormasDaSala.Circulo(), Color.black,
            Vector2.zero, Vector2.one * 0.45f, 13).transform;

        // Veias: riscos vermelhos no branco.
        for (int i = 0; i < 5; i++)
        {
            float a = (i * 72f + 20f) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            SpriteRenderer veia = FormasDaSala.Desenho(transform, "Veia", FormasDaSala.Circulo(), new Color(0.8f, 0.15f, 0.2f, 0.8f),
                dir * raio * 0.6f, new Vector2(raio * 0.35f, raio * 0.05f), 11);
            veia.transform.localRotation = Quaternion.Euler(0f, 0f, i * 72f + 20f);
            veia.sortingOrder = 12;
        }

        mira = FormasDaSala.Desenho(transform, "Mira do laser", FormasDaSala.Quadrado(),
            new Color(1f, 0.15f, 0.15f, 0.3f), Vector2.zero, new Vector2(1f, 0.1f), 5);
        mira.enabled = false;
    }

    protected override void Awake()
    {
        base.Awake();

        rb.bodyType = RigidbodyType2D.Kinematic;
        escalaOriginal = transform.localScale;

        if (raio <= 0f && TryGetComponent(out CircleCollider2D c))
            raio = c.radius;

        recarga.Forcar(esperaInicial);
        AoAcordar.AddListener(MostrarBarra);
    }

    private void MostrarBarra()
    {
        if (barraCriada)
            return;

        barraCriada = true;
        BarraDoChefe.Mostrar(this);
        Sons.Tocar(Som.Rugido);
    }

    private void LateUpdate()
    {
        // A pupila olha pro jogador, dentro da iris.
        if (pupila != null && jogador != null && !EstaMorto)
        {
            Vector2 para = ((Vector2)jogador.position - (Vector2)transform.position).normalized;
            pupila.localPosition = para * 0.22f;
        }
    }

    // ---------------- esperar ----------------
    protected override void AtualizarAgindo(float dt)
    {
        ChecarFase();
        rb.linearVelocity = Vector2.zero;
        recarga.Contar(dt);
        Cruz(dt);
        PintarIris(new Color(0.3f, 0.75f, 0.4f));

        if (!recarga.Ativo && jogador != null)
            ComecarAtaque(EscolherAtaque());
    }

    private Ataque EscolherAtaque()
    {
        List<Ataque> opcoes = new List<Ataque> { Ataque.Anel, Ataque.Rajada, Ataque.Espiral };

        if (fase >= 2)
            opcoes.Add(Ataque.Laser);

        lacaios.RemoveAll(l => l == null || l.EstaMorto);

        if (lacaios.Count < maximoDeLacaios && ultimoAtaque != Ataque.Invocar)
            opcoes.Add(Ataque.Invocar);

        if (ultimoAtaque.HasValue && opcoes.Count > 1)
            opcoes.Remove(ultimoAtaque.Value);

        return opcoes[Random.Range(0, opcoes.Count)];
    }

    private void ComecarAtaque(Ataque ataque)
    {
        ataqueAtual = ataque;
        ultimoAtaque = ataque;
        executando = false;
        disparos = 0;
        aviso.Forcar(TempoDeAviso());
        EstadoAtual = Estado.Preparando;
    }

    private float TempoDeAviso()
    {
        float pressa = fase == 1 ? 1f : fase == 2 ? 1.25f : 1.5f;

        switch (ataqueAtual)
        {
            case Ataque.Laser: return avisoDoLaser / pressa;
            case Ataque.Invocar: return 0.9f / pressa;
            default: return 0.6f / pressa;
        }
    }

    // ---------------- atacar ----------------
    protected override void AtualizarPreparando(float dt)
    {
        rb.linearVelocity = Vector2.zero;
        Cruz(dt);

        if (executando)
        {
            Executar(dt);
            return;
        }

        aviso.Contar(dt);
        float total = TempoDeAviso();
        float t = total <= 0f ? 1f : Mathf.Clamp01(1f - aviso.Restante / total);
        Avisar(t);

        if (aviso.Ativo)
            return;

        transform.localScale = escalaOriginal;
        executando = true;
        proximoDisparo.Zerar();

        switch (ataqueAtual)
        {
            case Ataque.Anel:
                AtirarAnel(Random.value * 360f);

                if (fase >= 2)
                {
                    execucao.Forcar(0.45f); // o segundo anel sai daqui a pouco
                    disparos = 1;
                }
                else
                {
                    Recuperar(0.6f);
                }

                break;

            case Ataque.Espiral:
                execucao.Forcar(duracaoDaEspiral);
                angulo = Random.value * 360f;
                break;

            case Ataque.Laser:
                mira.enabled = false;
                execucao.Forcar(duracaoDoLaser);
                Sons.Tocar(Som.Rugido, 0.6f);
                break;

            case Ataque.Invocar:
                Invocar();
                Recuperar(0.6f);
                break;
        }
    }

    private void Executar(float dt)
    {
        execucao.Contar(dt);
        proximoDisparo.Contar(dt);

        switch (ataqueAtual)
        {
            case Ataque.Anel:
                if (!execucao.Ativo)
                {
                    AtirarAnel(Random.value * 360f);
                    Recuperar(0.6f);
                }

                break;

            case Ataque.Rajada:
                if (proximoDisparo.Ativo)
                    return;

                Vector2 rumo = RumoParaOJogador();
                Atirar(rumo, 1.3f);

                if (fase >= 2)
                {
                    Atirar(Girar(rumo, 25f), 1.3f);
                    Atirar(Girar(rumo, -25f), 1.3f);
                }

                disparos++;
                proximoDisparo.Forcar(intervaloDaRajada);

                if (disparos >= tirosNaRajada)
                    Recuperar(0.5f);

                break;

            case Ataque.Espiral:
                if (!proximoDisparo.Ativo)
                {
                    int bracos = fase >= 3 ? 4 : fase == 2 ? 3 : 2;

                    for (int i = 0; i < bracos; i++)
                    {
                        Atirar(Girar(Vector2.right, angulo + i * 360f / bracos), 1f);
                        Atirar(Girar(Vector2.right, -angulo + i * 360f / bracos + 180f / bracos), 0.8f);
                    }

                    angulo += 13f;
                    proximoDisparo.Forcar(intervaloDaEspiral);
                }

                if (!execucao.Ativo)
                    Recuperar(0.7f);

                break;

            case Ataque.Laser:
                // Jato rapido e denso ao longo da linha que foi mostrada.
                if (!proximoDisparo.Ativo)
                {
                    Atirar(Girar(rumoDoLaser, Random.Range(-3f, 3f)), 2.3f);
                    proximoDisparo.Forcar(0.04f);
                }

                if (!execucao.Ativo)
                    Recuperar(0.8f);

                break;

            default:
                Recuperar(0.5f);
                break;
        }
    }

    private void Recuperar(float tempo)
    {
        executando = false;
        recuperacao.Forcar(tempo);
        EstadoAtual = Estado.Recuperando;
    }

    protected override void AtualizarRecuperando(float dt)
    {
        rb.linearVelocity = Vector2.zero;
        Cruz(dt);
        recuperacao.Contar(dt);
        PintarIris(new Color(0.25f, 0.45f, 0.3f));

        if (recuperacao.Ativo)
            return;

        float pressa = fase == 1 ? 1f : fase == 2 ? 1.3f : 1.6f;
        recarga.Forcar(intervaloEntreAtaques / pressa);
        EstadoAtual = Estado.Agindo;
    }

    // ---------------- avisos ----------------
    private void Avisar(float t)
    {
        switch (ataqueAtual)
        {
            case Ataque.Anel:
                PintarIris(Color.Lerp(Color.white, new Color(1f, 0.85f, 0.2f), t));
                transform.localScale = escalaOriginal * (1f + 0.12f * t);
                break;

            case Ataque.Rajada:
                PintarIris(Color.Lerp(Color.white, new Color(1f, 0.5f, 0.1f), t));
                break;

            case Ataque.Espiral:
                PintarIris(Color.Lerp(Color.white, new Color(0.3f, 0.7f, 1f), t));
                break;

            case Ataque.Laser:
                PintarIris(Color.Lerp(Color.white, Color.red, t));
                rumoDoLaser = RumoParaOJogador();
                MostrarMira(rumoDoLaser, t);
                break;

            case Ataque.Invocar:
                PintarIris(Color.Lerp(Color.white, new Color(0.6f, 0.3f, 1f), t));
                transform.localScale = escalaOriginal * (1f - 0.08f * Mathf.Sin(t * Mathf.PI * 4f));
                break;
        }
    }

    private void MostrarMira(Vector2 rumo, float t)
    {
        const float comprimento = 14f;
        mira.enabled = true;
        mira.size = new Vector2(comprimento, Mathf.Lerp(0.05f, 0.6f, t));
        mira.transform.localPosition = rumo * (comprimento * 0.5f) / Mathf.Max(0.01f, transform.localScale.x);
        mira.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(rumo.y, rumo.x) * Mathf.Rad2Deg);

        Color cor = mira.color;
        cor.a = Mathf.Lerp(0.15f, 0.5f, t);
        mira.color = cor;
    }

    private void PintarIris(Color cor)
    {
        if (iris != null)
            iris.color = fase >= 3 ? Color.Lerp(cor, Color.red, 0.35f) : cor;
    }

    // ---------------- tiros ----------------
    private void AtirarAnel(float anguloInicial)
    {
        // Brecha: tamanhoDaBrecha tiros seguidos nao saem, em lugar sorteado.
        int inicioDaBrecha = Random.Range(0, tirosNoAnel);

        for (int i = 0; i < tirosNoAnel; i++)
        {
            int distancia = (i - inicioDaBrecha + tirosNoAnel) % tirosNoAnel;

            if (distancia < tamanhoDaBrecha)
                continue;

            Atirar(Girar(Vector2.right, anguloInicial + i * 360f / tirosNoAnel), 0.9f);
        }
    }

    /// <summary>Fase 3: tiros em cruz girando, o tempo todo.</summary>
    private void Cruz(float dt)
    {
        if (fase < 3)
            return;

        cruz.Contar(dt);

        if (cruz.Ativo)
            return;

        for (int i = 0; i < 4; i++)
            Atirar(Girar(Vector2.right, anguloDaCruz + i * 90f), 0.7f);

        anguloDaCruz += 17f;
        cruz.Forcar(intervaloDaCruz);
    }

    private void Atirar(Vector2 rumo, float multiplicador)
    {
        TiroDaSala.Disparar(rb.position + rumo * (raio + 0.25f), rumo * velocidadeDoTiro * multiplicador,
                          danoDoTiro, gameObject, true, corDoTiro, diametroDoTiro);
    }

    private void Invocar()
    {
        Sala sala = GetComponentInParent<Sala>();

        if (sala == null)
            return;

        lacaios.RemoveAll(l => l == null || l.EstaMorto);
        TipoDeInimigo[] tipos = { TipoDeInimigo.Perseguidor, TipoDeInimigo.Saltador, TipoDeInimigo.Investidor };
        int quantos = Mathf.Min(2, maximoDeLacaios - lacaios.Count);

        for (int i = 0; i < quantos; i++)
        {
            // Nos cantos da sala, longe do olho.
            Vector2 canto = new Vector2(i % 2 == 0 ? -1f : 1f, Random.value < 0.5f ? -1f : 1f);
            Vector2 ponto = Vector2.Scale(canto, sala.TamanhoInterno * 0.5f - Vector2.one * 1.2f);
            InimigoDeSala lacaio = sala.CriarInimigo(tipos[Random.Range(0, tipos.Length)], ponto);

            if (lacaio != null)
                lacaios.Add(lacaio);
        }
    }

    private Vector2 RumoParaOJogador()
    {
        Vector2 alvo = ParaOJogador();
        return alvo.sqrMagnitude > 0.0001f ? alvo.normalized : Vector2.down;
    }

    private static Vector2 Girar(Vector2 v, float graus) => Quaternion.Euler(0f, 0f, graus) * v;

    // ---------------- fases e morte ----------------
    private void ChecarFase()
    {
        int nova = vida.Fracao <= fracaoDaFase3 ? 3 : vida.Fracao <= fracaoDaFase2 ? 2 : 1;

        if (nova <= fase)
            return;

        // A cor do corpo e do Vida (ele pisca e restaura): a fase aparece na iris e no tamanho.
        fase = nova;
        escalaOriginal *= 1.08f;
        transform.localScale = escalaOriginal;
        Sons.Tocar(Som.Rugido);
    }

    protected override void Morrer()
    {
        transform.localScale = escalaOriginal;

        if (mira != null)
            mira.enabled = false;

        lacaios.RemoveAll(l => l == null || l.EstaMorto);
        foreach (InimigoDeSala lacaio in lacaios.ToArray())
            lacaio.Vida.MatarAgora();

        lacaios.Clear();

        // Some com os tiros que ainda voam: a vitoria nao pode matar ninguem.
        foreach (TiroDaSala tiro in FindObjectsByType<TiroDaSala>(FindObjectsSortMode.None))
            Destroy(tiro.gameObject);

        base.Morrer();
    }
}
