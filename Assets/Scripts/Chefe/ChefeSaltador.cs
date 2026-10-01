using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O Demonio do Martelo: fecha o mundo 1 e aparece nas fases do mundo 2 e 3 (ver Andar.ChefesPorMundo).
/// Onde o primeiro corre e atira, este PULA. Alterna entre quatro ataques, cada um com
/// o seu aviso:
///
///   Pulo     -> agacha, salta pra onde o jogador esta e cai soltando um anel de tiros.
///               No ar ninguem acerta ele (nem ele acerta ninguem); uma marca vermelha
///               no chao mostra onde vai cair
///   Espiral  -> gira no lugar soltando bracos de tiro em espiral
///   Cuspe    -> cospe bolhas grandes e lentas que estouram num anel de tiros
///   Invocar  -> (so na segunda fase) chama dois bichos simples do mundo
///
/// Com metade da vida entra na SEGUNDA FASE: pula tres vezes seguidas, a espiral ganha
/// um braco, cospe tres bolhas em leque e os avisos ficam mais curtos.
/// </summary>
public class ChefeSaltador : InimigoDeSala, IChefe
{
    private enum Ataque
    {
        Pulo,
        Espiral,
        Cuspe,
        Invocar
    }

    [Header("Chefe")]
    [SerializeField] private string nomeDoChefe = "Demônio do Martelo";

    [Tooltip("Segundos andando entre um ataque e outro")]
    [SerializeField, Min(0.1f)] private float intervaloEntreAtaques = 1.1f;

    [SerializeField, Min(0f)] private float esperaInicial = 1.2f;

    [SerializeField, Range(0f, 1f)] private float fracaoDaSegundaFase = 0.5f;

    [Tooltip("Na segunda fase os avisos e esperas ficam divididos por isto")]
    [SerializeField, Min(1f)] private float pressaNaSegundaFase = 1.3f;

    [Header("Tiros")]
    [SerializeField, Min(0f)] private float danoDoTiro = 10f;

    [SerializeField, Min(0.1f)] private float velocidadeDoTiro = 4f;

    [SerializeField, Min(0.05f)] private float diametroDoTiro = 0.34f;

    [SerializeField] private Color corDoTiro = new Color(0.55f, 0.95f, 0.35f);

    [Header("Pulo")]
    [SerializeField, Min(0f)] private float avisoDoPulo = 0.55f;

    [SerializeField, Min(0.1f)] private float duracaoDoPulo = 0.85f;

    [SerializeField, Min(0f)] private float alturaDoPulo = 2.2f;

    [SerializeField, Min(0)] private int tirosAoPousar = 8;

    [SerializeField, Min(0f)] private float danoAoPousar = 20f;

    [SerializeField, Min(1)] private int pulosNaSegundaFase = 3;

    [Header("Espiral")]
    [SerializeField, Min(0f)] private float avisoDaEspiral = 0.6f;

    [SerializeField, Min(0.1f)] private float duracaoDaEspiral = 2.2f;

    [SerializeField, Min(0.03f)] private float intervaloDaEspiral = 0.1f;

    [SerializeField, Min(1)] private int bracos = 2;

    [Tooltip("Graus que a espiral gira a cada disparo")]
    [SerializeField] private float giroPorDisparo = 14f;

    [Header("Cuspe")]
    [SerializeField, Min(0f)] private float avisoDoCuspe = 0.5f;

    [SerializeField, Min(0.1f)] private float velocidadeDaBolha = 2.6f;

    [SerializeField, Min(0.1f)] private float diametroDaBolha = 0.75f;

    [SerializeField, Min(0.1f)] private float tempoAteEstourar = 0.9f;

    [SerializeField, Min(3)] private int estilhacos = 8;

    [Header("Invocar")]
    [SerializeField, Min(0f)] private float avisoDeInvocar = 0.9f;

    [SerializeField, Min(0)] private int lacaiosPorVez = 2;

    [SerializeField, Min(0)] private int maximoDeLacaios = 3;

    [Header("Depois de cada ataque")]
    [SerializeField, Min(0f)] private float tempoDeRecuperacao = 0.6f;

    // ---------------- estado ----------------
    private readonly List<InimigoDeSala> lacaios = new List<InimigoDeSala>();
    private readonly List<SpriteRenderer> olhos = new List<SpriteRenderer>();

    private Ataque ataqueAtual;
    private Ataque? ultimoAtaque;
    private bool executando;
    private bool segundaFase;
    private bool barraCriada;
    private int pulosRestantes;
    private float anguloDaEspiral;
    private Vector2 origemDoPulo;
    private Vector2 destinoDoPulo;
    private Vector3 escalaOriginal;

    private Cronometro recarga;
    private Cronometro aviso;
    private Cronometro execucao;
    private Cronometro proximoDisparo;
    private Cronometro recuperacao;

    private Transform corpo;
    private Vector3 posicaoDoCorpo;
    private Vector3 escalaDoCorpo;
    private SpriteRenderer marca;
    private Collider2D colisor;

    public string Nome => nomeDoChefe;

    public bool NaSegundaFase => segundaFase;

    protected override bool Imparavel => true;

    // ---------------- montagem ----------------
    /// <summary>
    /// Olhos no alto da cabeca, sombra e a marca de onde vai cair. A fabrica chama. Com arte
    /// importada o bicho ja tem cara: so sombra e marca, e o aviso tinge o corpo.
    /// </summary>
    public void Enfeitar(bool comArte = false)
    {
        if (corpo == null)
            return;

        // Olhos filhos do corpo (que tem escala 2*raio): posicoes em fracao do diametro.
        for (int lado = -1; lado <= 1 && !comArte; lado += 2)
        {
            SpriteRenderer olho = FormasDaSala.Desenho(corpo, "Olho", FormasDaSala.Circulo(), Color.white,
                new Vector2(lado * 0.24f, 0.36f), Vector2.one * 0.28f, 11);
            olhos.Add(olho);

            FormasDaSala.Desenho(olho.transform, "Pupila", FormasDaSala.Circulo(), Color.black,
                new Vector2(0f, -0.1f), new Vector2(0.6f, 0.35f), 12);
        }

        // Boca: uma faixa escura.
        if (!comArte)
            FormasDaSala.Desenho(corpo, "Boca", FormasDaSala.Circulo(), new Color(0.1f, 0.2f, 0.08f),
            new Vector2(0f, -0.08f), new Vector2(0.55f, 0.1f), 11);

        FormasDaSala.Desenho(transform, "Sombra", FormasDaSala.Circulo(), new Color(0f, 0f, 0f, 0.35f),
            new Vector2(0f, -Raio * 0.45f), new Vector2(Raio * 2.1f, Raio * 0.8f), 9);

        // A marca fica solta no mundo (nao e filha do chefe), senao andaria junto com ele.
        marca = FormasDaSala.Desenho(transform.parent, "Marca do pulo", FormasDaSala.Circulo(),
            new Color(1f, 0.15f, 0.1f, 0.3f), Vector2.zero, Vector2.one * Raio * 2f, -5);
        marca.enabled = false;
    }

    protected override void Awake()
    {
        base.Awake();

        escalaOriginal = transform.localScale;
        rb.mass = 20f;
        colisor = GetComponent<Collider2D>();

        if (desenho != null)
        {
            corpo = desenho.transform;
            posicaoDoCorpo = corpo.localPosition;
            escalaDoCorpo = corpo.localScale;
        }

        recarga.Forcar(esperaInicial);
        AoAcordar.AddListener(MostrarBarra);
    }

    private void OnDestroy()
    {
        if (marca != null)
            Destroy(marca.gameObject);
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
        PintarOlhos(Color.white);

        // Chega perto devagar; perto o bastante, espera.
        Vector2 alvo = ParaOJogador();
        Andar(alvo.magnitude > 3.5f ? alvo : Vector2.zero, velocidade * 0.7f);

        if (!recarga.Ativo && jogador != null)
            ComecarAtaque(EscolherAtaque());
    }

    private Ataque EscolherAtaque()
    {
        List<Ataque> opcoes = new List<Ataque> { Ataque.Pulo, Ataque.Espiral, Ataque.Cuspe };

        lacaios.RemoveAll(l => l == null || l.EstaMorto);

        if (segundaFase && lacaiosPorVez > 0 && lacaios.Count < maximoDeLacaios && ultimoAtaque != Ataque.Invocar)
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
        pulosRestantes = ataque == Ataque.Pulo ? (segundaFase ? pulosNaSegundaFase : 1) : 0;

        aviso.Forcar(TempoDeAviso() / Pressa);
        EstadoAtual = Estado.Preparando;
        Frear();
    }

    private float TempoDeAviso()
    {
        switch (ataqueAtual)
        {
            case Ataque.Pulo: return avisoDoPulo;
            case Ataque.Espiral: return avisoDaEspiral;
            case Ataque.Cuspe: return avisoDoCuspe;
            default: return avisoDeInvocar;
        }
    }

    // ---------------- atacar ----------------
    protected override void AtualizarPreparando(float dt)
    {
        if (executando)
        {
            Executar(dt);
            return;
        }

        Frear();
        aviso.Contar(dt);

        float total = TempoDeAviso() / Pressa;
        float t = total <= 0f ? 1f : Mathf.Clamp01(1f - aviso.Restante / total);
        Avisar(t);

        if (aviso.Ativo)
            return;

        LimparAviso();
        executando = true;

        switch (ataqueAtual)
        {
            case Ataque.Pulo:
                Saltar();
                break;

            case Ataque.Espiral:
                execucao.Forcar(duracaoDaEspiral);
                proximoDisparo.Zerar();
                anguloDaEspiral = Random.value * 360f;
                break;

            case Ataque.Cuspe:
                Cuspir();
                Recuperar(tempoDeRecuperacao);
                break;

            case Ataque.Invocar:
                Invocar();
                Recuperar(tempoDeRecuperacao);
                break;
        }
    }

    private void Executar(float dt)
    {
        execucao.Contar(dt);

        switch (ataqueAtual)
        {
            case Ataque.Pulo:
                Voar();
                break;

            case Ataque.Espiral:
                Frear();
                proximoDisparo.Contar(dt);

                if (!proximoDisparo.Ativo)
                {
                    int n = bracos + (segundaFase ? 1 : 0);

                    for (int i = 0; i < n; i++)
                        Atirar(Rumo(anguloDaEspiral + i * 360f / n));

                    anguloDaEspiral += giroPorDisparo;
                    proximoDisparo.Forcar(intervaloDaEspiral);
                }

                if (!execucao.Ativo)
                    Recuperar(tempoDeRecuperacao);

                break;

            default:
                Recuperar(tempoDeRecuperacao);
                break;
        }
    }

    // ---------------- pulo ----------------
    private void Saltar()
    {
        origemDoPulo = rb.position;
        destinoDoPulo = DentroDaSala(jogador != null ? (Vector2)jogador.position : rb.position);

        Sons.Tocar(Som.Pulo);

        // No ar: nada acerta e nada e acertado.
        if (colisor != null)
            colisor.enabled = false;

        rb.linearVelocity = (destinoDoPulo - origemDoPulo) / duracaoDoPulo;
        execucao.Forcar(duracaoDoPulo);

        if (marca != null)
        {
            marca.enabled = true;
            marca.transform.position = destinoDoPulo;
        }
    }

    private void Voar()
    {
        float t = Mathf.Clamp01(1f - execucao.Restante / duracaoDoPulo);
        rb.linearVelocity = (destinoDoPulo - origemDoPulo) / duracaoDoPulo;

        if (corpo != null)
            corpo.localPosition = posicaoDoCorpo + Vector3.up * Mathf.Sin(t * Mathf.PI) * alturaDoPulo;

        // A marca cresce ate o tamanho do chefe: quando fica cheia, ele cai.
        if (marca != null)
        {
            marca.transform.localScale = Vector3.one * Raio * 2f * Mathf.Lerp(0.3f, 1.2f, t);
            Color cor = marca.color;
            cor.a = Mathf.Lerp(0.15f, 0.5f, t);
            marca.color = cor;
        }

        if (execucao.Ativo)
            return;

        Pousar();
    }

    private void Pousar()
    {
        rb.position = destinoDoPulo;
        rb.linearVelocity = Vector2.zero;

        if (corpo != null)
            corpo.localPosition = posicaoDoCorpo;

        if (colisor != null)
            colisor.enabled = true;

        if (marca != null)
            marca.enabled = false;

        Sons.Tocar(Som.Pancada);

        // Pancada no chao: machuca quem estiver embaixo e espalha tiros.
        if (jogador != null && Vector2.Distance(jogador.position, destinoDoPulo) < Raio + 0.6f)
        {
            IDanificavel alvo = jogador.GetComponentInParent<IDanificavel>();
            alvo?.TomarDano(new DanoInfo(danoAoPousar, (Vector2)jogador.position - destinoDoPulo, 6f, destinoDoPulo, gameObject));
        }

        int quantos = tirosAoPousar + (segundaFase ? 4 : 0);

        for (int i = 0; i < quantos; i++)
            Atirar(Rumo(i * 360f / quantos + (pulosRestantes % 2) * 180f / quantos));

        pulosRestantes--;

        if (pulosRestantes > 0)
        {
            // Proximo pulo seguido: aviso curto, mesmo ataque.
            executando = false;
            aviso.Forcar(0.35f);
            return;
        }

        Recuperar(tempoDeRecuperacao);
    }

    private Vector2 DentroDaSala(Vector2 ponto)
    {
        Sala sala = GetComponentInParent<Sala>();

        if (sala == null)
            return ponto;

        Vector2 centro = sala.transform.position;
        Vector2 limite = sala.TamanhoInterno * 0.5f - Vector2.one * (Raio + 0.2f);
        Vector2 local = ponto - centro;
        return centro + new Vector2(Mathf.Clamp(local.x, -limite.x, limite.x), Mathf.Clamp(local.y, -limite.y, limite.y));
    }

    // ---------------- cuspe e lacaios ----------------
    private void Cuspir()
    {
        Vector2 alvo = ParaOJogador();
        float anguloBase = alvo.sqrMagnitude > 0.0001f ? Mathf.Atan2(alvo.y, alvo.x) * Mathf.Rad2Deg : -90f;
        int quantas = segundaFase ? 3 : 1;

        for (int i = 0; i < quantas; i++)
        {
            float desvio = quantas == 1 ? 0f : Mathf.Lerp(-30f, 30f, i / (float)(quantas - 1));
            Vector2 rumo = Rumo(anguloBase + desvio);

            TiroDaSala bolha = TiroDaSala.Disparar(rb.position + rumo * (Raio + 0.35f), rumo * velocidadeDaBolha,
                                                   danoDoTiro, gameObject, true, corDoTiro, diametroDaBolha);
            BolhaQueEstoura.Colocar(bolha, tempoAteEstourar, estilhacos, velocidadeDoTiro * 0.9f, danoDoTiro, corDoTiro, gameObject);
        }
    }

    private void Invocar()
    {
        Sala sala = GetComponentInParent<Sala>();

        if (sala == null)
            return;

        lacaios.RemoveAll(l => l == null || l.EstaMorto);
        int quantos = Mathf.Min(lacaiosPorVez, maximoDeLacaios - lacaios.Count);

        for (int i = 0; i < quantos; i++)
        {
            Vector2 lado = i % 2 == 0 ? Vector2.left : Vector2.right;
            Vector2 ponto = DentroDaSala(rb.position + lado * (Raio + 0.8f)) - (Vector2)sala.transform.position;
            InimigoDeSala lacaio = sala.CriarInimigo(TemaDoAndar.Lacaio(), ponto);

            if (lacaio != null)
                lacaios.Add(lacaio);
        }
    }

    private void Atirar(Vector2 rumo)
    {
        float v = velocidadeDoTiro * (segundaFase ? 1.15f : 1f);
        TiroDaSala.Disparar(rb.position + rumo * (Raio + 0.2f), rumo * v, danoDoTiro, gameObject, true, corDoTiro, diametroDoTiro);
    }

    private static Vector2 Rumo(float graus)
    {
        float r = graus * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
    }

    // ---------------- recuperar ----------------
    private void Recuperar(float tempo)
    {
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
            case Ataque.Pulo:
                // Agacha: achata e alarga.
                PintarOlhos(Color.Lerp(Color.white, Color.red, t));
                if (corpo != null)
                    corpo.localScale = new Vector3(escalaDoCorpo.x * (1f + 0.25f * t), escalaDoCorpo.y * (1f - 0.3f * t), 1f);
                break;

            case Ataque.Espiral:
                PintarOlhos(Color.Lerp(Color.white, new Color(0.3f, 0.9f, 1f), t));
                if (corpo != null)
                    corpo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 6f) * 12f * t);
                break;

            case Ataque.Cuspe:
                // Incha a garganta.
                PintarOlhos(Color.Lerp(Color.white, new Color(0.7f, 1f, 0.3f), t));
                transform.localScale = escalaOriginal * (1f + 0.2f * t);
                break;

            case Ataque.Invocar:
                PintarOlhos(Color.Lerp(Color.white, new Color(0.6f, 0.3f, 1f), t));
                transform.localScale = escalaOriginal * (1f - 0.1f * Mathf.Sin(t * Mathf.PI * 4f));
                break;
        }
    }

    private void LimparAviso()
    {
        transform.localScale = escalaOriginal;

        if (corpo != null)
        {
            corpo.localScale = escalaDoCorpo;
            corpo.localRotation = Quaternion.identity;
        }
    }

    private void PintarOlhos(Color cor)
    {
        if (segundaFase)
            cor = Color.Lerp(cor, Color.red, 0.5f);

        foreach (SpriteRenderer olho in olhos)
            if (olho != null)
                olho.color = cor;

        if (olhos.Count == 0 && desenho != null)
            desenho.color = Color.Lerp(Color.white, cor, 0.6f);
    }

    // ---------------- fases e morte ----------------
    private void ChecarSegundaFase()
    {
        if (segundaFase || vida.Fracao > fracaoDaSegundaFase)
            return;

        segundaFase = true;
        velocidade *= 1.2f;
        ViradaDeFase.Anunciar(this, "O martelo arde!", new Color(1f, 0.55f, 0.2f));
    }

    private float Pressa => segundaFase ? pressaNaSegundaFase : 1f;

    protected override void Morrer()
    {
        LimparAviso();

        if (corpo != null)
            corpo.localPosition = posicaoDoCorpo;

        if (marca != null)
            marca.enabled = false;

        lacaios.RemoveAll(l => l == null || l.EstaMorto);
        foreach (InimigoDeSala lacaio in lacaios.ToArray())
            lacaio.Vida.MatarAgora();

        lacaios.Clear();
        base.Morrer();
    }
}
