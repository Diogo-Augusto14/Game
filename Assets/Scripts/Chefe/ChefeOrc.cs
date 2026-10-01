using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Senhor da Guerra: o orc de elite do Tiny RPG bem maior. O chefe pesado: lento, mas cada
/// golpe enche a sala. Alterna entre quatro ataques, cada um com o seu aviso:
///
///   Machadada -> ergue o machado (se estica, fica laranja) e bate no chao: uma onda de
///                choque em anel com um buraco pra passar
///   Arremesso -> recua o braco e joga machados que giram, freiam la longe e VOLTAM pra
///                ele: desviar na ida nao basta
///   Investida -> abaixa a cabeca, mostra a linha e corre reto ate bater, soltando pedras
///                pros lados enquanto corre
///   Grito     -> grito de guerra: chama bichos do mundo (na segunda fase, que ja chegam com pressa)
///
/// Com metade da vida entra na SEGUNDA FASE (<see cref="ViradaDeFase"/>): a machadada solta
/// duas ondas, joga um machado a mais, corre mais rapido e os avisos ficam mais curtos.
/// </summary>
public class ChefeOrc : InimigoDeSala, IChefe
{
    private enum Ataque { Machadada, Arremesso, Investida, Grito }

    [Header("Chefe")]
    [SerializeField] private string nomeDoChefe = "Senhor da Guerra";
    [SerializeField, Min(0.1f)] private float intervaloEntreAtaques = 1.2f;
    [SerializeField, Min(0f)] private float esperaInicial = 1.3f;
    [SerializeField, Range(0f, 1f)] private float fracaoDaSegundaFase = 0.5f;
    [SerializeField, Min(1f)] private float pressaNaSegundaFase = 1.3f;

    [Header("Tiros")]
    [SerializeField, Min(0f)] private float danoDoTiro = 12f;
    [SerializeField, Min(0.1f)] private float velocidadeDoTiro = 4.2f;
    [SerializeField, Min(0.05f)] private float diametroDoTiro = 0.4f;
    [SerializeField] private Color corDoTiro = new Color(0.75f, 0.6f, 0.4f);

    [Header("Machadada")]
    [SerializeField, Min(0f)] private float avisoDaMachadada = 0.85f;
    [SerializeField, Min(4)] private int tirosPorOnda = 18;
    [SerializeField, Min(1)] private int tamanhoDoBuraco = 3;

    [Header("Arremesso")]
    [SerializeField, Min(0f)] private float avisoDoArremesso = 0.6f;
    [SerializeField, Min(1)] private int machados = 2;
    [SerializeField, Min(0.5f)] private float velocidadeDoMachado = 8f;
    [SerializeField, Min(0f)] private float danoDoMachado = 15f;

    [Header("Investida")]
    [SerializeField, Min(0f)] private float avisoDaInvestida = 0.75f;
    [SerializeField, Min(1f)] private float velocidadeDaInvestida = 8.5f;
    [SerializeField, Min(0f)] private float danoDaInvestida = 25f;
    [SerializeField, Min(0.05f)] private float intervaloDasPedras = 0.18f;

    [Header("Grito")]
    [SerializeField, Min(0f)] private float avisoDoGrito = 0.9f;
    [SerializeField, Min(0)] private int orcsPorGrito = 2;
    [SerializeField, Min(0)] private int maximoDeOrcs = 3;

    [SerializeField, Min(0f)] private float tempoDeRecuperacao = 0.8f;

    private readonly List<InimigoDeSala> lacaios = new List<InimigoDeSala>();
    private Ataque ataqueAtual;
    private Ataque? ultimoAtaque;
    private bool executando;
    private bool segundaFase;
    private bool barraCriada;
    private int ondasSoltas;
    private Vector2 rumo;
    private float danoDeContatoNormal;

    private Cronometro recarga;
    private Cronometro relogio;
    private Cronometro proximaPedra;
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
            new Vector2(0f, -Raio * 0.55f), new Vector2(Raio * 2.2f, Raio * 0.75f), 9);
    }

    protected override void Awake()
    {
        base.Awake();
        rb.mass = 25f;
        velocidade = 1.7f;
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

        Vector2 alvo = ParaOJogador();
        Andar(alvo.magnitude > 1.8f ? PeloCaminho(alvo) : Vector2.zero, velocidade);
        animacao?.OlharPara(alvo);

        if (!recarga.Ativo && jogador != null)
            Comecar(Escolher());
    }

    private Ataque Escolher()
    {
        List<Ataque> opcoes = new List<Ataque> { Ataque.Machadada, Ataque.Arremesso };

        if (VeOJogador())
            opcoes.Add(Ataque.Investida);

        lacaios.RemoveAll(l => l == null || l.EstaMorto);

        if (lacaios.Count < maximoDeOrcs && ultimoAtaque != Ataque.Grito)
            opcoes.Add(Ataque.Grito);

        if (ultimoAtaque.HasValue && opcoes.Count > 1)
            opcoes.Remove(ultimoAtaque.Value);

        return opcoes[Random.Range(0, opcoes.Count)];
    }

    private void Comecar(Ataque ataque)
    {
        ataqueAtual = ataque;
        ultimoAtaque = ataque;
        executando = false;
        ondasSoltas = 0;
        relogio.Forcar(Aviso() / Pressa);
        EstadoAtual = Estado.Preparando;
        Frear();
    }

    private float Aviso()
    {
        switch (ataqueAtual)
        {
            case Ataque.Machadada: return avisoDaMachadada;
            case Ataque.Arremesso: return avisoDoArremesso;
            case Ataque.Investida: return avisoDaInvestida;
            default: return avisoDoGrito;
        }
    }

    // ---------------- atacar ----------------
    protected override void AtualizarPreparando(float dt)
    {
        relogio.Contar(dt);

        if (executando)
        {
            Executar(dt);
            return;
        }

        Frear();
        float total = Aviso() / Pressa;
        float t = total <= 0f ? 1f : Mathf.Clamp01(1f - relogio.Restante / total);
        Vector2 paraEle = RumoAoJogador();
        animacao?.OlharPara(paraEle);

        switch (ataqueAtual)
        {
            case Ataque.Machadada:
                Pintar(Color.Lerp(Color.white, new Color(1f, 0.7f, 0.3f), t));
                Esticar(1f - 0.12f * t, 1f + 0.2f * t);
                break;
            case Ataque.Arremesso:
                Pintar(Color.Lerp(Color.white, new Color(0.9f, 0.85f, 0.5f), t));
                if (corpo != null)
                    corpo.localRotation = Quaternion.Euler(0f, 0f, -18f * t * Mathf.Sign(paraEle.x == 0f ? 1f : paraEle.x));
                break;
            case Ataque.Investida:
                Pintar(Color.Lerp(Color.white, new Color(1f, 0.7f, 0.65f), t));
                Rastro.MostrarMira(paraEle, t);
                Rastro.Poeira(paraEle);
                break;
            default:
                Pintar(Color.Lerp(Color.white, new Color(0.6f, 0.9f, 0.4f), t));
                if (corpo != null)
                    corpo.localScale = escalaDoCorpo * (1f + 0.1f * Mathf.Sin(t * Mathf.PI * 5f));
                break;
        }

        if (relogio.Ativo)
            return;

        Arrumar();
        executando = true;

        switch (ataqueAtual)
        {
            case Ataque.Machadada:
                relogio.Zerar();
                break;

            case Ataque.Arremesso:
                Arremessar();
                Recuperar(tempoDeRecuperacao);
                break;

            case Ataque.Investida:
                rumo = paraEle;
                danoDeContato = danoDaInvestida;
                Rastro.Ligado = true;
                Rastro.Poeira(rumo, true);
                Tocar(clipes?.Ataque, 12f);
                Sons.Tocar(Som.Dash, 0.9f);
                proximaPedra.Zerar();
                relogio.Forcar(2.2f);
                break;

            default:
                Gritar();
                Recuperar(tempoDeRecuperacao);
                break;
        }
    }

    private void Executar(float dt)
    {
        switch (ataqueAtual)
        {
            case Ataque.Machadada:
                Frear();

                if (relogio.Ativo)
                    return;

                Bater();
                ondasSoltas++;

                if (ondasSoltas >= 1 + (segundaFase ? 1 : 0))
                    Recuperar(tempoDeRecuperacao);
                else
                    relogio.Forcar(0.4f / Pressa);

                break;

            case Ataque.Investida:
                Investir(dt);
                break;

            default:
                Recuperar(tempoDeRecuperacao);
                break;
        }
    }

    private void Bater()
    {
        Tocar(clipes?.AtaqueEspecial ?? clipes?.Ataque, 14f);
        Sons.Tocar(Som.Pancada, 1f);
        Impacto.Tremer(0.25f, 0.35f);
        EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Poeira, rb.position, new Color(0.8f, 0.7f, 0.5f), 2f);

        // Anel com um buraco que muda de lugar a cada onda.
        int n = tirosPorOnda;
        int buraco = Random.Range(0, n);
        float giro = ondasSoltas % 2 == 0 ? 0f : 180f / n;

        for (int i = 0; i < n; i++)
        {
            if ((i - buraco + n) % n < tamanhoDoBuraco)
                continue;

            Atirar(Rumo(giro + i * 360f / n), 0.9f);
        }
    }

    private void Arremessar()
    {
        Tocar(clipes?.AtaqueForte ?? clipes?.Ataque, 14f);
        Sons.Tocar(Som.Corte, 1f);

        int n = machados + (segundaFase ? 1 : 0);
        float centro = Mathf.Atan2(RumoAoJogador().y, RumoAoJogador().x) * Mathf.Rad2Deg;
        float abertura = n > 1 ? 50f : 0f;

        for (int i = 0; i < n; i++)
        {
            float a = n > 1 ? centro - abertura / 2f + abertura * i / (n - 1) : centro;
            Vector2 r = Rumo(a);
            TiroDaSala machado = TiroDaSala.Disparar(rb.position + r * (Raio + 0.3f), r * velocidadeDoMachado * (segundaFase ? 1.1f : 1f),
                                                     danoDoMachado, gameObject, true, new Color(0.85f, 0.85f, 0.9f), 0.5f);
            MachadoBumerangue.Em(machado, transform, 0.9f);
        }
    }

    private void Investir(float dt)
    {
        float v = velocidadeDaInvestida * (segundaFase ? 1.15f : 1f);
        RaycastHit2D parede = Physics2D.CircleCast(rb.position, Raio * 0.9f, rumo, v * dt + 0.05f, Camadas.MascaraDeParede);

        // Pedras pros dois lados enquanto corre.
        proximaPedra.Contar(dt);

        if (!proximaPedra.Ativo)
        {
            Vector2 lado = new Vector2(-rumo.y, rumo.x);
            Atirar(lado, 0.6f);
            Atirar(-lado, 0.6f);
            proximaPedra.Forcar(intervaloDasPedras);
        }

        if (parede.collider == null && relogio.Ativo)
        {
            rb.linearVelocity = rumo * v;
            return;
        }

        rb.linearVelocity = Vector2.zero;
        Sons.Tocar(Som.Pancada);
        Impacto.Tremer(0.2f, 0.3f);
        Recuperar(tempoDeRecuperacao * 1.6f);   // tonto depois de bater
    }

    private void Gritar()
    {
        Sons.Tocar(Som.Rugido, 1f);
        Impacto.Tremer(0.2f, 0.4f);
        TextoFlutuante.Mostrar(transform.position + Vector3.up * (Raio + 0.6f), "Waaagh!", new Color(0.6f, 1f, 0.4f));

        Sala sala = GetComponentInParent<Sala>();

        if (sala == null)
            return;

        lacaios.RemoveAll(l => l == null || l.EstaMorto);
        int quantos = Mathf.Min(orcsPorGrito, maximoDeOrcs - lacaios.Count);

        for (int i = 0; i < quantos; i++)
        {
            InimigoDeSala orc = sala.CriarInimigo(TemaDoAndar.Lacaio(), sala.PontoLivreAleatorio(1.5f));

            if (orc == null)
                continue;

            lacaios.Add(orc);
            EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Poeira, orc.transform.position, new Color(0.6f, 0.8f, 0.4f), 1f);

            if (segundaFase)
                orc.MultiplicadorDeVelocidade = 1.3f;
        }
    }

    // ---------------- tiros ----------------
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

    private static Vector2 Rumo(float graus)
    {
        float r = graus * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
    }

    // ---------------- recuperar ----------------
    private void Recuperar(float tempo)
    {
        executando = false;
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
        Pintar(Color.Lerp(Color.white, Color.gray, 0.5f));

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
        velocidade *= 1.25f;
        ViradaDeFase.Anunciar(this, "Sangue e aço!", new Color(1f, 0.5f, 0.3f));
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
