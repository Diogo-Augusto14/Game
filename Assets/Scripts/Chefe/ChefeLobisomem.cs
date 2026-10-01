using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lobisomem Alfa: o lobisomem do Tiny RPG bem maior. O chefe rapido: nao para quieto e
/// ataca de perto. Alterna entre quatro ataques, cada um com o seu aviso:
///
///   Botes  -> a linha de mira aparece e ele salta reto, tres vezes seguidas (mirando de
///             novo a cada salto). Na segunda fase cada bote termina num leque de garras
///   Garras -> fica laranja, se encolhe e rasga o ar duas vezes: dois leques de tiros, o
///             segundo encaixado nos vaos do primeiro
///   Cerco  -> corre em circulo em volta do jogador soltando tiros pra dentro e fecha com
///             um bote
///   Uivo   -> se estica e uiva: chama bichos do mundo (na segunda fase, com um anel de tiros)
///
/// Com metade da vida entra na SEGUNDA FASE (<see cref="ViradaDeFase"/>): um bote a mais,
/// garras nos botes, cerco mais rapido e avisos mais curtos.
/// </summary>
public class ChefeLobisomem : InimigoDeSala, IChefe
{
    private enum Ataque { Botes, Garras, Cerco, Uivo }

    private enum Passo { Aviso, Mirando, Saltando, Rasgando, Circulando }

    [Header("Chefe")]
    [SerializeField] private string nomeDoChefe = "Lobisomem Alfa";
    [SerializeField, Min(0.1f)] private float intervaloEntreAtaques = 0.9f;
    [SerializeField, Min(0f)] private float esperaInicial = 1f;
    [SerializeField, Range(0f, 1f)] private float fracaoDaSegundaFase = 0.5f;
    [SerializeField, Min(1f)] private float pressaNaSegundaFase = 1.3f;

    [Header("Tiros")]
    [SerializeField, Min(0f)] private float danoDoTiro = 10f;
    [SerializeField, Min(0.1f)] private float velocidadeDoTiro = 5f;
    [SerializeField, Min(0.05f)] private float diametroDoTiro = 0.34f;
    [SerializeField] private Color corDoTiro = new Color(0.9f, 0.25f, 0.3f);

    [Header("Botes")]
    [SerializeField, Min(1)] private int botes = 3;
    [SerializeField, Min(0f)] private float avisoDoBote = 0.6f;
    [SerializeField, Min(0f)] private float miraEntreBotes = 0.35f;
    [SerializeField, Min(1f)] private float velocidadeDoBote = 11f;
    [SerializeField, Min(0.05f)] private float duracaoDoBote = 0.35f;
    [SerializeField, Min(0f)] private float danoDoBote = 20f;

    [Header("Garras")]
    [SerializeField, Min(0f)] private float avisoDasGarras = 0.55f;
    [SerializeField, Min(1)] private int tirosPorGarra = 7;
    [SerializeField, Range(10f, 180f)] private float aberturaDaGarra = 70f;

    [Header("Cerco")]
    [SerializeField, Min(0f)] private float avisoDoCerco = 0.4f;
    [SerializeField, Min(0.5f)] private float duracaoDoCerco = 2.4f;
    [SerializeField, Min(1f)] private float raioDoCerco = 3.5f;
    [SerializeField, Min(1f)] private float velocidadeDoCerco = 6.5f;
    [SerializeField, Min(0.05f)] private float intervaloDoCerco = 0.28f;

    [Header("Uivo")]
    [SerializeField, Min(0f)] private float avisoDoUivo = 0.8f;
    [SerializeField, Min(0)] private int caesPorUivo = 2;
    [SerializeField, Min(0)] private int maximoDeCaes = 4;

    [SerializeField, Min(0f)] private float tempoDeRecuperacao = 0.6f;

    private readonly List<InimigoDeSala> lacaios = new List<InimigoDeSala>();
    private Ataque ataqueAtual;
    private Ataque? ultimoAtaque;
    private Passo passo;
    private bool segundaFase;
    private bool barraCriada;
    private int botesQueFaltam;
    private int rasgos;
    private float anguloNoCerco;
    private float sentidoDoCerco = 1f;
    private Vector2 rumo;
    private float danoDeContatoNormal;

    private Cronometro recarga;
    private Cronometro relogio;
    private Cronometro proximoTiro;
    private Cronometro recuperacao;

    private Transform corpo;
    private Vector3 escalaDoCorpo;
    private RastroDoChefe rastro;
    private AnimacaoDePersonagem animacao;
    private ClipesDePersonagem clipes;

    public string Nome => nomeDoChefe;
    public bool NaSegundaFase => segundaFase;
    protected override bool Imparavel => true;

    private RastroDoChefe Rastro => rastro != null ? rastro : (rastro = RastroDoChefe.Em(this, desenho, Raio));
    private float Pressa => segundaFase ? pressaNaSegundaFase : 1f;

    public void Enfeitar(ClipesDePersonagem arte)
    {
        clipes = arte;
        FormasDaSala.Desenho(transform, "Sombra", FormasDaSala.Circulo(), new Color(0f, 0f, 0f, 0.35f),
            new Vector2(0f, -Raio * 0.55f), new Vector2(Raio * 2f, Raio * 0.7f), 9);
    }

    protected override void Awake()
    {
        base.Awake();
        rb.mass = 15f;
        velocidade = 3.4f;
        animacao = GetComponent<AnimacaoDePersonagem>();
        danoDeContatoNormal = danoDeContato;

        if (desenho != null)
        {
            corpo = desenho.transform;
            escalaDoCorpo = corpo.localScale;
        }

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

    // ---------------- andar ----------------
    protected override void AtualizarAgindo(float dt)
    {
        ChecarSegundaFase();
        recarga.Contar(dt);
        Pintar(Color.white);

        // Rodeia de perto: chega ate uns 2.5 do jogador e anda de lado.
        Vector2 alvo = ParaOJogador();
        float d = alvo.magnitude;
        Vector2 lado = d > 0.01f ? new Vector2(-alvo.y, alvo.x) / d * sentidoDoCerco : Vector2.zero;
        Andar(d > 2.5f ? PeloCaminho(alvo) : lado, velocidade);
        animacao?.OlharPara(alvo);

        if (!recarga.Ativo && jogador != null)
            Comecar(Escolher());
    }

    private Ataque Escolher()
    {
        List<Ataque> opcoes = new List<Ataque> { Ataque.Garras, Ataque.Cerco };

        if (VeOJogador())
            opcoes.Add(Ataque.Botes);

        lacaios.RemoveAll(l => l == null || l.EstaMorto);

        if (lacaios.Count < maximoDeCaes && ultimoAtaque != Ataque.Uivo)
            opcoes.Add(Ataque.Uivo);

        if (ultimoAtaque.HasValue && opcoes.Count > 1)
            opcoes.Remove(ultimoAtaque.Value);

        return opcoes[Random.Range(0, opcoes.Count)];
    }

    private void Comecar(Ataque ataque)
    {
        ataqueAtual = ataque;
        ultimoAtaque = ataque;
        passo = Passo.Aviso;
        botesQueFaltam = botes + (segundaFase ? 1 : 0);
        rasgos = 0;
        relogio.Forcar(AvisoDe(ataque) / Pressa);
        EstadoAtual = Estado.Preparando;
        Frear();
    }

    private float AvisoDe(Ataque ataque)
    {
        switch (ataque)
        {
            case Ataque.Botes: return avisoDoBote;
            case Ataque.Garras: return avisoDasGarras;
            case Ataque.Cerco: return avisoDoCerco;
            default: return avisoDoUivo;
        }
    }

    // ---------------- atacar ----------------
    protected override void AtualizarPreparando(float dt)
    {
        relogio.Contar(dt);

        switch (passo)
        {
            case Passo.Aviso: Avisar(); break;
            case Passo.Mirando: Mirar(); break;
            case Passo.Saltando: Saltar(dt); break;
            case Passo.Rasgando: Rasgar(); break;
            case Passo.Circulando: Circular(dt); break;
        }
    }

    private void Avisar()
    {
        Frear();
        float total = AvisoDe(ataqueAtual) / Pressa;
        float t = total <= 0f ? 1f : Mathf.Clamp01(1f - relogio.Restante / total);
        Vector2 paraEle = RumoAoJogador();
        animacao?.OlharPara(paraEle);

        switch (ataqueAtual)
        {
            case Ataque.Botes:
                Pintar(Color.Lerp(Color.white, new Color(1f, 0.7f, 0.65f), t));
                Rastro.MostrarMira(paraEle, t);
                Rastro.Poeira(paraEle);
                Tremer(0.05f * t);
                break;
            case Ataque.Garras:
                Pintar(Color.Lerp(Color.white, new Color(1f, 0.7f, 0.3f), t));
                Esticar(1f + 0.12f * t, 1f - 0.12f * t);
                break;
            case Ataque.Cerco:
                Pintar(Color.Lerp(Color.white, new Color(0.7f, 0.85f, 1f), t));
                Rastro.Poeira(-paraEle);
                break;
            default:
                Pintar(Color.Lerp(Color.white, new Color(0.75f, 0.6f, 1f), t));
                Esticar(1f - 0.1f * t, 1f + 0.2f * t);
                break;
        }

        if (relogio.Ativo)
            return;

        Arrumar();

        switch (ataqueAtual)
        {
            case Ataque.Botes:
                ComecarBote(paraEle);
                break;
            case Ataque.Garras:
                passo = Passo.Rasgando;
                relogio.Zerar();
                break;
            case Ataque.Cerco:
                passo = Passo.Circulando;
                relogio.Forcar(duracaoDoCerco);
                proximoTiro.Zerar();
                Vector2 doJogador = jogador != null ? rb.position - (Vector2)jogador.position : Vector2.right;
                anguloNoCerco = Mathf.Atan2(doJogador.y, doJogador.x);
                sentidoDoCerco = Random.value < 0.5f ? 1f : -1f;
                break;
            default:
                Uivar();
                Recuperar(tempoDeRecuperacao);
                break;
        }
    }

    private void ComecarBote(Vector2 direcao)
    {
        rumo = direcao;
        passo = Passo.Saltando;
        relogio.Forcar(duracaoDoBote);
        danoDeContato = danoDoBote;
        Rastro.EsconderMira();
        Rastro.Ligado = true;
        Rastro.Poeira(rumo, true);
        Sons.Tocar(Som.Dash, 0.8f);
        Tocar(clipes?.Ataque, 14f);
        animacao?.OlharPara(rumo);
    }

    private void Saltar(float dt)
    {
        float v = velocidadeDoBote * (segundaFase ? 1.1f : 1f);
        RaycastHit2D parede = Physics2D.CircleCast(rb.position, Raio * 0.9f, rumo, v * dt + 0.05f, Camadas.MascaraDeParede);

        if (parede.collider == null && relogio.Ativo)
        {
            rb.linearVelocity = rumo * v;
            return;
        }

        rb.linearVelocity = Vector2.zero;
        Rastro.Ligado = false;
        danoDeContato = danoDeContatoNormal;

        // Segunda fase: o bote termina num leque de garras.
        if (segundaFase)
            Leque(rumo, 3, 40f, 0.9f, 0f);

        botesQueFaltam--;

        if (botesQueFaltam <= 0)
        {
            Recuperar(tempoDeRecuperacao * 1.3f);
            return;
        }

        passo = Passo.Mirando;
        relogio.Forcar(miraEntreBotes / Pressa);
    }

    private void Mirar()
    {
        Frear();
        float total = miraEntreBotes / Pressa;
        float t = total <= 0f ? 1f : Mathf.Clamp01(1f - relogio.Restante / total);
        Rastro.MostrarMira(RumoAoJogador(), 0.5f + 0.5f * t);

        if (!relogio.Ativo)
            ComecarBote(RumoAoJogador());
    }

    private void Rasgar()
    {
        Frear();

        if (relogio.Ativo)
            return;

        // Dois rasgos: o segundo encaixa nos vaos do primeiro.
        Tocar(rasgos == 0 ? clipes?.AtaqueEspecial : clipes?.Ataque, 16f);
        Sons.Tocar(Som.Corte, 0.8f);
        Leque(RumoAoJogador(), tirosPorGarra, aberturaDaGarra, 1f, rasgos == 0 ? 0f : 0.5f);
        rasgos++;

        if (rasgos >= 2 + (segundaFase ? 1 : 0))
            Recuperar(tempoDeRecuperacao);
        else
            relogio.Forcar(0.3f / Pressa);
    }

    private void Circular(float dt)
    {
        if (jogador == null)
        {
            Recuperar(tempoDeRecuperacao);
            return;
        }

        // Corre na roda em volta do jogador; o centro acompanha o jogador.
        float v = velocidadeDoCerco * (segundaFase ? 1.2f : 1f);
        anguloNoCerco += sentidoDoCerco * v / raioDoCerco * dt;
        Vector2 ponto = (Vector2)jogador.position + new Vector2(Mathf.Cos(anguloNoCerco), Mathf.Sin(anguloNoCerco)) * raioDoCerco;
        Vector2 ate = ponto - rb.position;
        Andar(ate.sqrMagnitude > 0.01f ? ate : Vector2.zero, v * 1.3f);
        animacao?.OlharPara(rb.linearVelocity);
        Rastro.Ligado = true;

        proximoTiro.Contar(dt);

        if (!proximoTiro.Ativo)
        {
            Atirar(RumoAoJogador(), 0.85f);
            proximoTiro.Forcar(intervaloDoCerco / Pressa);
        }

        if (relogio.Ativo)
            return;

        // Fecha a roda com um bote.
        Rastro.Ligado = false;
        botesQueFaltam = 1;
        ComecarBote(RumoAoJogador());
    }

    private void Uivar()
    {
        Sons.Tocar(Som.Rugido, 1f);
        Impacto.Tremer(0.2f, 0.45f);
        Tocar(clipes?.AtaqueForte ?? clipes?.AtaqueEspecial, 10f);
        TextoFlutuante.Mostrar(transform.position + Vector3.up * (Raio + 0.6f), "Auuuu!", new Color(0.8f, 0.7f, 1f));

        if (segundaFase)
            Leque(Vector2.right, 12, 330f, 0.75f, 0f);

        Sala sala = GetComponentInParent<Sala>();

        if (sala == null)
            return;

        lacaios.RemoveAll(l => l == null || l.EstaMorto);
        int quantos = Mathf.Min(caesPorUivo + (segundaFase ? 1 : 0), maximoDeCaes - lacaios.Count);

        for (int i = 0; i < quantos; i++)
        {
            Vector2 local = sala.PontoLivreAleatorio(1.5f);
            InimigoDeSala cao = sala.CriarInimigo(TemaDoAndar.Lacaio(), local);

            if (cao != null)
            {
                lacaios.Add(cao);
                EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Poeira, cao.transform.position, new Color(0.6f, 0.5f, 0.8f), 1f);
            }
        }
    }

    // ---------------- tiros ----------------
    /// <summary>Leque de <paramref name="n"/> tiros em volta de <paramref name="centro"/>. Meio passo = encaixa nos vaos.</summary>
    private void Leque(Vector2 centro, int n, float abertura, float pressa, float meioPasso)
    {
        float baseAngulo = Mathf.Atan2(centro.y, centro.x) * Mathf.Rad2Deg;
        float passoAngulo = n > 1 ? abertura / (n - 1) : 0f;
        float inicio = baseAngulo - abertura / 2f + passoAngulo * meioPasso;

        for (int i = 0; i < n; i++)
        {
            float a = (inicio + passoAngulo * i) * Mathf.Deg2Rad;
            Atirar(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), pressa);
        }
    }

    private void Atirar(Vector2 direcao, float pressa)
    {
        float v = velocidadeDoTiro * pressa * (segundaFase ? 1.15f : 1f);
        TiroDaSala.Disparar(rb.position + direcao * (Raio + 0.2f), direcao * v, danoDoTiro, gameObject, true, corDoTiro, diametroDoTiro);
    }

    private Vector2 RumoAoJogador()
    {
        Vector2 alvo = ParaOJogador();
        return alvo.sqrMagnitude > 0.0001f ? alvo.normalized : Vector2.down;
    }

    // ---------------- recuperar ----------------
    private void Recuperar(float tempo)
    {
        Arrumar();
        Rastro.Ligado = false;
        danoDeContato = danoDeContatoNormal;
        recuperacao.Forcar(tempo / Pressa);
        EstadoAtual = Estado.Recuperando;
    }

    protected override void AtualizarRecuperando(float dt)
    {
        Frear();
        recuperacao.Contar(dt);
        Pintar(Color.Lerp(Color.white, Color.gray, 0.4f));

        if (recuperacao.Ativo)
            return;

        recarga.Forcar(intervaloEntreAtaques / Pressa);
        EstadoAtual = Estado.Agindo;
    }

    // ---------------- desenho ----------------
    private void Tocar(Sprite[] quadros, float fps)
    {
        if (animacao != null && quadros != null)
            animacao.TocarUmaVez(quadros, fps * Pressa);
    }

    private void Pintar(Color cor)
    {
        if (segundaFase)
            cor = Color.Lerp(cor, Color.red, 0.3f);

        if (desenho != null)
            desenho.color = cor;
    }

    private void Esticar(float x, float y)
    {
        if (corpo != null)
            corpo.localScale = new Vector3(escalaDoCorpo.x * x, escalaDoCorpo.y * y, 1f);
    }

    private void Tremer(float forca)
    {
        if (corpo != null)
            corpo.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-1f, 1f) * forca * 120f);
    }

    private void Arrumar()
    {
        if (corpo != null)
        {
            corpo.localScale = escalaDoCorpo;
            corpo.localRotation = Quaternion.identity;
        }

        Rastro.EsconderMira();
    }

    // ---------------- fases e morte ----------------
    private void ChecarSegundaFase()
    {
        if (segundaFase || vida.Fracao > fracaoDaSegundaFase)
            return;

        segundaFase = true;
        velocidade *= 1.2f;
        ViradaDeFase.Anunciar(this, "A lua cheia!", new Color(0.85f, 0.8f, 1f));
    }

    protected override void Morrer()
    {
        Arrumar();
        Rastro.Ligado = false;

        lacaios.RemoveAll(l => l == null || l.EstaMorto);
        foreach (InimigoDeSala lacaio in lacaios.ToArray())
            lacaio.Vida.MatarAgora();

        lacaios.Clear();
        base.Morrer();
    }
}
