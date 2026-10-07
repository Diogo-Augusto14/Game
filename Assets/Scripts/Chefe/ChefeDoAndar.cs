using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O primeiro chefe (tipo o Monstro do Isaac): uma bola grande que anda devagar atras do
/// jogador e alterna entre quatro ataques, cada um com o seu aviso antes:
///
///   Anel      -> incha e solta tiros em volta, em todas as direcoes
///   Rajada    -> treme e solta tres leques de tiros mirados no jogador
///   Investida -> raspa poeira, mostra a linha de mira (amarela ficando vermelha) e dispara reto, deixando um rastro; se bater na parede fica tonto
///   Invocar   -> (so na segunda fase) chama dois bichos simples do mundo
///   Extra     -> floresce: uma rosa de tiros de magma (na segunda fase, duas, encaixadas) - ver ExtraDoChefe
///
/// Com metade da vida entra na SEGUNDA FASE: avisos mais curtos, tiros mais rapidos,
/// anel mais cheio e a investida solta um anel ao bater na parede.
///
/// Os olhos mudam de cor conforme o ataque que vem: o jogador aprende a ler o chefe.
/// A barra de vida (<see cref="BarraDoChefe"/>) aparece quando ele acorda.
///
/// Golpe nao atordoa nem empurra (<see cref="InimigoDeSala.Imparavel"/>). Ao morrer, os
/// lacaios que ainda estiverem vivos morrem junto, e a sala abre.
/// </summary>
public class ChefeDoAndar : InimigoDeSala, IChefe
{
    private enum Ataque
    {
        Anel,
        Rajada,
        Investida,
        Invocar,
        Extra
    }

    [Header("Chefe")]
    [SerializeField] private string nomeDoChefe = "Golem de Magma";

    [Tooltip("Segundos andando entre um ataque e outro")]
    [SerializeField, Min(0.1f)] private float intervaloEntreAtaques = 1.3f;

    [Tooltip("Segundos parado logo depois de acordar, antes do primeiro ataque")]
    [SerializeField, Min(0f)] private float esperaInicial = 1.2f;

    [Tooltip("Quando a vida cai abaixo desta fracao, entra na segunda fase")]
    [SerializeField, Range(0f, 1f)] private float fracaoDaSegundaFase = 0.5f;

    [Tooltip("Na segunda fase os avisos e esperas ficam divididos por isto")]
    [SerializeField, Min(1f)] private float pressaNaSegundaFase = 1.35f;

    [Tooltip("Distancia que ele tenta manter do jogador enquanto anda")]
    [SerializeField, Min(0f)] private float distanciaPreferida = 3f;

    [Header("Tiros")]
    [SerializeField, Min(0f)] private float danoDoTiro = 10f;

    [SerializeField, Min(0.1f)] private float velocidadeDoTiro = 4.2f;

    [SerializeField, Min(0.05f)] private float diametroDoTiro = 0.38f;

    [SerializeField] private Color corDoTiro = new Color(1f, 0.3f, 0.25f);

    [Header("Anel")]
    [SerializeField, Min(0f)] private float avisoDoAnel = 0.8f;

    [SerializeField, Min(3)] private int tirosNoAnel = 12;

    [Header("Rajada")]
    [SerializeField, Min(0f)] private float avisoDaRajada = 0.6f;

    [SerializeField, Min(1)] private int leques = 3;

    [SerializeField, Min(1)] private int tirosPorLeque = 5;

    [SerializeField, Range(0f, 90f)] private float aberturaDoLeque = 28f;

    [SerializeField, Min(0.05f)] private float intervaloEntreLeques = 0.35f;

    [Header("Investida")]
    [SerializeField, Min(0f)] private float avisoDaInvestida = 0.9f;

    [SerializeField, Min(0.1f)] private float velocidadeDaInvestida = 10f;

    [SerializeField, Min(0.1f)] private float duracaoMaximaDaInvestida = 1.2f;

    [SerializeField, Min(0f)] private float danoDaInvestida = 20f;

    [Tooltip("Segundos tonto depois de bater na parede: a janela pra bater nele")]
    [SerializeField, Min(0f)] private float tontoAposBater = 1.1f;

    [Header("Invocar")]
    [SerializeField, Min(0f)] private float avisoDeInvocar = 1f;

    [SerializeField, Min(0)] private int lacaiosPorVez = 2;

    [SerializeField, Min(0)] private int maximoDeLacaios = 3;

    [Header("Depois de cada ataque")]
    [SerializeField, Min(0f)] private float tempoDeRecuperacao = 0.6f;

    // ---------------- estado ----------------
    private readonly List<InimigoDeSala> lacaios = new List<InimigoDeSala>();

    private Ataque ataqueAtual;
    private Ataque? ultimoAtaque;
    private bool executando;       // dentro do Preparando: false = aviso, true = atacando
    private bool segundaFase;
    private bool barraCriada;
    private int lequesSoltos;
    private Vector2 rumoDaInvestida;
    private float danoDeContatoNormal;
    private float raio;
    private Vector3 escalaOriginal;

    private Cronometro recarga;
    private Cronometro aviso;
    private Cronometro execucao;
    private Cronometro recuperacao;

    private Transform corpo;
    private Vector3 posicaoDoCorpo;
    private readonly List<SpriteRenderer> olhos = new List<SpriteRenderer>();
    private RastroDoChefe rastro;
    private ExtraDoChefe extra;

    public string Nome => nomeDoChefe;

    private RastroDoChefe Rastro => rastro != null ? rastro : (rastro = RastroDoChefe.Em(this, desenho, raio));

    public bool NaSegundaFase => segundaFase;

    protected override bool Imparavel => true;

    // ---------------- montagem ----------------
    /// <summary>
    /// Poe os olhos. A <see cref="FabricaDeInimigos"/> chama logo depois de
    /// montar o corpo; o raio e o do colisor. Com arte importada o bicho ja tem cara: sem
    /// olhos desenhados, e o aviso de ataque tinge o corpo inteiro.
    /// </summary>
    public void Enfeitar(float raioDoCorpo, bool comArte = false)
    {
        raio = raioDoCorpo;

        for (int lado = -1; lado <= 1 && !comArte; lado += 2)
        {
            SpriteRenderer olho = FormasDaSala.Desenho(transform, "Olho", FormasDaSala.Circulo(), Color.white,
                new Vector2(lado * raio * 0.38f, raio * 0.22f), Vector2.one * raio * 0.42f, 11);
            olhos.Add(olho);

            FormasDaSala.Desenho(olho.transform, "Pupila", FormasDaSala.Circulo(), Color.black,
                Vector2.zero, Vector2.one * 0.45f, 12);
        }
    }

    protected override void Awake()
    {
        base.Awake();

        escalaOriginal = transform.localScale;
        danoDeContatoNormal = danoDeContato;

        // Pesado: o jogador nao consegue empurrar o chefe encostando nele.
        rb.mass = 20f;

        if (desenho != null)
        {
            corpo = desenho.transform;
            posicaoDoCorpo = corpo.localPosition;
        }

        if (raio <= 0f && TryGetComponent(out CircleCollider2D circulo))
            raio = circulo.radius;

        recarga.Forcar(esperaInicial);
        AoAcordar.AddListener(MostrarBarra);
        extra = ExtraDoChefe.Para(this, TipoDeInimigo.Chefe);
    }

    private void MostrarBarra()
    {
        if (barraCriada)
            return;

        barraCriada = true;
        BarraDoChefe.Mostrar(this);
        Sons.Tocar(Som.Rugido);
    }

    // ---------------- andar ----------------
    protected override void AtualizarAgindo(float dt)
    {
        ChecarSegundaFase();
        recarga.Contar(dt);

        Vector2 alvo = ParaOJogador();
        float distancia = alvo.magnitude;

        // Anda devagar: perto demais recua, longe demais chega perto.
        if (distancia > 0.0001f)
        {
            Vector2 frente = alvo / distancia;
            float sobra = distancia - distanciaPreferida;
            Andar(Mathf.Abs(sobra) < 0.5f ? Vector2.zero : frente * Mathf.Sign(sobra), velocidade);
        }
        else
        {
            Frear();
        }

        PintarOlhos(Color.white);

        if (!recarga.Ativo && jogador != null)
            ComecarAtaque(EscolherAtaque());
    }

    private Ataque EscolherAtaque()
    {
        List<Ataque> opcoes = new List<Ataque> { Ataque.Anel, Ataque.Rajada, Ataque.Extra };

        // Na segunda fase o florescer sai mais.
        if (segundaFase)
            opcoes.Add(Ataque.Extra);

        if (VeOJogador())
            opcoes.Add(Ataque.Investida);

        LimparLacaiosMortos();

        if (segundaFase && lacaiosPorVez > 0 && lacaios.Count < maximoDeLacaios && ultimoAtaque != Ataque.Invocar)
            opcoes.Add(Ataque.Invocar);

        // Nunca o mesmo ataque duas vezes seguidas (sobrando opcao).
        if (ultimoAtaque.HasValue && opcoes.Count > 1)
            opcoes.Remove(ultimoAtaque.Value);

        return opcoes[Random.Range(0, opcoes.Count)];
    }

    private void ComecarAtaque(Ataque ataque)
    {
        ataqueAtual = ataque;
        ultimoAtaque = ataque;
        executando = false;
        lequesSoltos = 0;

        float tempo;

        switch (ataque)
        {
            case Ataque.Anel: tempo = avisoDoAnel; break;
            case Ataque.Rajada: tempo = avisoDaRajada; break;
            case Ataque.Investida: tempo = avisoDaInvestida; break;
            case Ataque.Extra: tempo = extra.Aviso; break;
            default: tempo = avisoDeInvocar; break;
        }

        aviso.Forcar(tempo / Pressa);
        EstadoAtual = Estado.Preparando;
        Frear();
    }

    // ---------------- atacar ----------------
    protected override void AtualizarPreparando(float dt)
    {
        if (executando)
        {
            Executar(dt);
            return;
        }

        aviso.Contar(dt);
        float total = TempoDeAvisoAtual();
        float t = total <= 0f ? 1f : Mathf.Clamp01(1f - aviso.Restante / total);

        Avisar(t);

        if (aviso.Ativo)
            return;

        LimparAviso();
        executando = true;

        switch (ataqueAtual)
        {
            case Ataque.Anel:
                AtirarAnel(segundaFase ? tirosNoAnel + 4 : tirosNoAnel, Random.value * 360f);
                Recuperar(tempoDeRecuperacao);
                break;

            case Ataque.Rajada:
                execucao.Zerar();   // o primeiro leque sai ja
                break;

            case Ataque.Investida:
                rumoDaInvestida = RumoParaOJogador();
                danoDeContato = danoDaInvestida;
                execucao.Forcar(duracaoMaximaDaInvestida);
                Rastro.Poeira(rumoDaInvestida, true);
                Rastro.Ligado = true;
                break;

            case Ataque.Invocar:
                Invocar();
                Recuperar(tempoDeRecuperacao);
                break;

            case Ataque.Extra:
                extra.Soltar(segundaFase ? 2 : 1);
                break;
        }
    }

    private void Executar(float dt)
    {
        execucao.Contar(dt);

        switch (ataqueAtual)
        {
            case Ataque.Rajada:
                Frear();

                if (execucao.Ativo)
                    return;

                AtirarLeque();
                lequesSoltos++;

                if (lequesSoltos >= leques)
                    Recuperar(tempoDeRecuperacao);
                else
                    execucao.Forcar(intervaloEntreLeques / Pressa);

                break;

            case Ataque.Investida:
                float passo = velocidadeDaInvestida * dt;
                RaycastHit2D parede = Physics2D.CircleCast(rb.position, raio * 0.9f, rumoDaInvestida,
                                                           passo + 0.05f, Camadas.MascaraDeParede);

                if (parede.collider != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    Sons.Tocar(Som.Pancada);

                    // Segunda fase: a pancada na parede espalha tiros.
                    if (segundaFase)
                        AtirarAnel(8, Random.value * 360f);

                    TerminarInvestida(tontoAposBater);
                    return;
                }

                rb.linearVelocity = rumoDaInvestida * velocidadeDaInvestida;

                if (!execucao.Ativo)
                    TerminarInvestida(tempoDeRecuperacao);

                break;

            case Ataque.Extra:
                // Parado ate a ultima rosa sair.
                Frear();

                if (!extra.Ocupado)
                    Recuperar(tempoDeRecuperacao);

                break;

            default:
                Recuperar(tempoDeRecuperacao);
                break;
        }
    }

    private void TerminarInvestida(float tempo)
    {
        rb.linearVelocity *= 0.3f;
        danoDeContato = danoDeContatoNormal;
        Recuperar(tempo);
    }

    private void Recuperar(float tempo)
    {
        Rastro.Ligado = false;
        executando = false;
        recuperacao.Forcar(tempo / Pressa);
        EstadoAtual = Estado.Recuperando;
    }

    protected override void AtualizarRecuperando(float dt)
    {
        Frear();
        recuperacao.Contar(dt);
        PintarOlhos(Color.Lerp(Color.white, Color.gray, 0.6f));

        if (recuperacao.Ativo)
            return;

        recarga.Forcar(intervaloEntreAtaques / Pressa);
        EstadoAtual = Estado.Agindo;
    }

    // ---------------- avisos ----------------
    private void Avisar(float t)
    {
        switch (ataqueAtual)
        {
            case Ataque.Anel:
                // Incha conforme carrega.
                PintarOlhos(Color.Lerp(Color.white, new Color(1f, 0.85f, 0.2f), t));
                transform.localScale = escalaOriginal * (1f + 0.25f * t);
                break;

            case Ataque.Rajada:
                // Treme cada vez mais.
                PintarOlhos(Color.Lerp(Color.white, new Color(1f, 0.5f, 0.1f), t));
                if (corpo != null)
                    corpo.localPosition = posicaoDoCorpo + (Vector3)(Random.insideUnitCircle * 0.06f * t);
                break;

            case Ataque.Investida:
                // Raspa o chao (poeira nos pes) e a linha de mira segue o jogador ate o ultimo instante.
                PintarOlhos(Color.Lerp(Color.white, Color.red, t));
                Rastro.Poeira(RumoParaOJogador());
                Rastro.MostrarMira(RumoParaOJogador(), t);
                break;

            case Ataque.Invocar:
                PintarOlhos(Color.Lerp(Color.white, new Color(0.6f, 0.3f, 1f), t));
                transform.localScale = escalaOriginal * (1f - 0.12f * Mathf.Sin(t * Mathf.PI * 4f));
                break;

            case Ataque.Extra:
                // Incha e brilha na cor das balas que vem.
                PintarOlhos(Color.Lerp(Color.white, extra.Cor, t));
                transform.localScale = escalaOriginal * (1f + 0.2f * t);
                break;
        }
    }

    private void LimparAviso()
    {
        transform.localScale = escalaOriginal;

        if (corpo != null)
            corpo.localPosition = posicaoDoCorpo;

        Rastro.EsconderMira();
    }

    private void PintarOlhos(Color cor)
    {
        // Segunda fase: olhos sempre puxando pro vermelho.
        if (segundaFase)
            cor = Color.Lerp(cor, Color.red, 0.5f);

        foreach (SpriteRenderer olho in olhos)
            if (olho != null)
                olho.color = cor;

        if (olhos.Count == 0 && desenho != null)
            desenho.color = Color.Lerp(Color.white, cor, 0.6f);
    }

    // ---------------- tiros e lacaios ----------------
    private void AtirarAnel(int quantos, float anguloInicial)
    {
        for (int i = 0; i < quantos; i++)
        {
            float angulo = (anguloInicial + i * 360f / quantos) * Mathf.Deg2Rad;
            Atirar(new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)));
        }
    }

    private void AtirarLeque()
    {
        Vector2 rumo = RumoParaOJogador();
        float anguloBase = Mathf.Atan2(rumo.y, rumo.x) * Mathf.Rad2Deg;

        // Leques alternados saem meio passo deslocados: nao da pra ficar parado no vao.
        float passo = tirosPorLeque > 1 ? aberturaDoLeque * 2f / (tirosPorLeque - 1) : 0f;
        float deslocamento = lequesSoltos % 2 == 1 ? passo * 0.5f : 0f;

        for (int i = 0; i < tirosPorLeque; i++)
        {
            float desvio = tirosPorLeque == 1 ? 0f : -aberturaDoLeque + i * passo + deslocamento;
            float angulo = (anguloBase + desvio) * Mathf.Deg2Rad;
            Atirar(new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)));
        }
    }

    private void Atirar(Vector2 rumo)
    {
        float velocidadeReal = velocidadeDoTiro * (segundaFase ? 1.15f : 1f);

        TiroDaSala.Disparar(rb.position + rumo * (raio + 0.2f), rumo * velocidadeReal,
                          danoDoTiro, gameObject, true, corDoTiro, diametroDoTiro);
    }

    private void Invocar()
    {
        Sala sala = GetComponentInParent<Sala>();

        if (sala == null)
            return;

        LimparLacaiosMortos();
        int quantos = Mathf.Min(lacaiosPorVez, maximoDeLacaios - lacaios.Count);

        for (int i = 0; i < quantos; i++)
        {
            // Um de cada lado do chefe, dentro da sala.
            Vector2 lado = i % 2 == 0 ? Vector2.left : Vector2.right;
            Vector2 ponto = rb.position + lado * (raio + 0.8f) - (Vector2)sala.transform.position;
            Vector2 limite = sala.TamanhoInterno * 0.5f - Vector2.one * 0.6f;
            ponto = new Vector2(Mathf.Clamp(ponto.x, -limite.x, limite.x), Mathf.Clamp(ponto.y, -limite.y, limite.y));

            InimigoDeSala lacaio = sala.CriarInimigo(TemaDoAndar.Lacaio(), ponto);

            if (lacaio != null)
                lacaios.Add(lacaio);
        }
    }

    private void LimparLacaiosMortos()
    {
        lacaios.RemoveAll(l => l == null || l.EstaMorto);
    }

    // ---------------- fases e morte ----------------
    private void ChecarSegundaFase()
    {
        if (segundaFase || vida.Fracao > fracaoDaSegundaFase)
            return;

        segundaFase = true;
        velocidade *= 1.25f;
        ViradaDeFase.Anunciar(this, "O magma ferve!", new Color(1f, 0.45f, 0.2f));

        foreach (SpriteRenderer olho in olhos)
            if (olho != null)
                olho.transform.localScale *= 1.25f;
    }

    private float Pressa => segundaFase ? pressaNaSegundaFase : 1f;

    private float TempoDeAvisoAtual()
    {
        switch (ataqueAtual)
        {
            case Ataque.Anel: return avisoDoAnel / Pressa;
            case Ataque.Rajada: return avisoDaRajada / Pressa;
            case Ataque.Investida: return avisoDaInvestida / Pressa;
            case Ataque.Extra: return extra.Aviso / Pressa;
            default: return avisoDeInvocar / Pressa;
        }
    }

    private Vector2 RumoParaOJogador()
    {
        Vector2 alvo = ParaOJogador();
        return alvo.sqrMagnitude > 0.0001f ? alvo.normalized : Vector2.down;
    }

    protected override void Morrer()
    {
        LimparAviso();
        danoDeContato = danoDeContatoNormal;

        // Os lacaios caem junto: a sala so abre com todo mundo morto.
        LimparLacaiosMortos();
        foreach (InimigoDeSala lacaio in lacaios.ToArray())
            lacaio.Vida.MatarAgora();

        lacaios.Clear();
        base.Morrer();
    }
}
