using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chefe do andar 1 (sorteado com o Monstrao): o Minotauro Furioso, o Minotauro do Tiny
/// RPG bem maior. Briga no corpo a corpo e alterna entre quatro ataques, cada um com o
/// seu aviso:
///
///   Chifrada -> raspa o casco (poeira), mostra a linha de mira (amarela ficando vermelha) e dispara reto,
///               deixando um rastro. Bate na parede e RICOCHETEIA (a mira nova aparece
///               rapidinho), soltando pedras a cada batida
///   Pisao    -> ergue o machado e bate no chao: ondas de tiros saem dele, uma atras da
///               outra, com um buraco que muda de lugar a cada onda
///   Giro     -> gira o machado andando atras do jogador, soltando tiros em quatro
///               direcoes que vao girando. Encostar nele doi mais
///   Invocar  -> (so na segunda fase) chama dois orcs
///
/// Com metade da vida entra na SEGUNDA FASE: ricocheteia mais vezes, o pisao solta uma
/// onda a mais, o giro fica mais rapido e os avisos mais curtos.
/// </summary>
public class ChefeMinotauro : InimigoDeSala, IChefe
{
    private enum Ataque
    {
        Chifrada,
        Pisao,
        Giro,
        Invocar
    }

    [Header("Chefe")]
    [SerializeField] private string nomeDoChefe = "Minotauro Furioso";

    [SerializeField, Min(0.1f)] private float intervaloEntreAtaques = 1f;

    [SerializeField, Min(0f)] private float esperaInicial = 1.2f;

    [SerializeField, Range(0f, 1f)] private float fracaoDaSegundaFase = 0.5f;

    [SerializeField, Min(1f)] private float pressaNaSegundaFase = 1.3f;

    [Header("Tiros")]
    [SerializeField, Min(0f)] private float danoDoTiro = 10f;

    [SerializeField, Min(0.1f)] private float velocidadeDoTiro = 3.8f;

    [SerializeField, Min(0.05f)] private float diametroDoTiro = 0.34f;

    [SerializeField] private Color corDoTiro = new Color(0.85f, 0.6f, 0.35f);

    [Header("Chifrada")]
    [SerializeField, Min(0f)] private float avisoDaChifrada = 0.8f;

    [SerializeField, Min(0.1f)] private float velocidadeDaChifrada = 8f;

    [SerializeField, Min(0f)] private float danoDaChifrada = 20f;

    [Tooltip("Batidas na parede antes de parar (a primeira nao conta como ricochete)")]
    [SerializeField, Min(0)] private int ricochetes = 1;

    [SerializeField, Min(0)] private int ricochetesNaSegundaFase = 3;

    [SerializeField, Min(0)] private int pedrasPorBatida = 5;

    [Tooltip("Segundos parado mirando entre um ricochete e outro")]
    [SerializeField, Min(0f)] private float miraDoRicochete = 0.3f;

    [Header("Pisao")]
    [SerializeField, Min(0f)] private float avisoDoPisao = 0.7f;

    [SerializeField, Min(1)] private int ondas = 2;

    [SerializeField, Min(0.05f)] private float intervaloEntreOndas = 0.4f;

    [SerializeField, Min(4)] private int tirosPorOnda = 20;

    [Tooltip("Quantos tiros faltam no buraco de cada onda")]
    [SerializeField, Min(1)] private int tamanhoDoBuraco = 3;

    [Header("Giro")]
    [SerializeField, Min(0f)] private float avisoDoGiro = 0.6f;

    [SerializeField, Min(0.1f)] private float duracaoDoGiro = 2.4f;

    [SerializeField, Min(0.03f)] private float intervaloDoGiro = 0.16f;

    [SerializeField, Min(0f)] private float velocidadeNoGiro = 2.2f;

    [SerializeField, Min(0f)] private float danoNoGiro = 20f;

    [Header("Invocar")]
    [SerializeField, Min(0f)] private float avisoDeInvocar = 0.9f;

    [SerializeField, Min(0)] private int lacaiosPorVez = 2;

    [SerializeField, Min(0)] private int maximoDeLacaios = 3;

    [Header("Depois de cada ataque")]
    [SerializeField, Min(0f)] private float tempoDeRecuperacao = 0.7f;

    // ---------------- estado ----------------
    private readonly List<InimigoDeSala> lacaios = new List<InimigoDeSala>();

    private Ataque ataqueAtual;
    private Ataque? ultimoAtaque;
    private bool executando;
    private bool segundaFase;
    private bool barraCriada;
    private bool correndo;
    private int batidasRestantes;
    private int ondasSoltas;
    private float anguloDoGiro;
    private Vector2 rumoDaChifrada;
    private float danoDeContatoNormal;

    private Cronometro recarga;
    private Cronometro aviso;
    private Cronometro execucao;
    private Cronometro proximoDisparo;
    private Cronometro recuperacao;

    private Transform corpo;
    private Vector3 escalaDoCorpo;
    private RastroDoChefe rastro;
    private AnimacaoDePersonagem animacao;
    private ClipesDePersonagem clipes;

    public string Nome => nomeDoChefe;

    private RastroDoChefe Rastro => rastro != null ? rastro : (rastro = RastroDoChefe.Em(this, desenho, Raio));

    public bool NaSegundaFase => segundaFase;

    protected override bool Imparavel => true;

    // ---------------- montagem ----------------
    /// <summary>Sombra no chao. A fabrica chama.</summary>
    public void Enfeitar(ClipesDePersonagem arte)
    {
        clipes = arte;

        FormasDaSala.Desenho(transform, "Sombra", FormasDaSala.Circulo(), new Color(0f, 0f, 0f, 0.35f),
            new Vector2(0f, -Raio * 0.55f), new Vector2(Raio * 2.1f, Raio * 0.7f), 9);
    }

    protected override void Awake()
    {
        base.Awake();

        rb.mass = 20f;
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
        Tingir(Color.white);

        // Vai pra cima do jogador, sem pressa.
        Vector2 alvo = ParaOJogador();
        Andar(alvo.magnitude > 1.5f ? alvo : Vector2.zero, velocidade);

        if (!recarga.Ativo && jogador != null)
            ComecarAtaque(EscolherAtaque());
    }

    private Ataque EscolherAtaque()
    {
        List<Ataque> opcoes = new List<Ataque> { Ataque.Pisao, Ataque.Giro };

        if (VeOJogador())
            opcoes.Add(Ataque.Chifrada);

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
        correndo = false;
        ondasSoltas = 0;
        batidasRestantes = 1 + (segundaFase ? ricochetesNaSegundaFase : ricochetes);

        aviso.Forcar(TempoDeAviso() / Pressa);
        EstadoAtual = Estado.Preparando;
        Frear();

        if (animacao != null)
            animacao.OlharPara(ParaOJogador());
    }

    private float TempoDeAviso()
    {
        switch (ataqueAtual)
        {
            case Ataque.Chifrada: return avisoDaChifrada;
            case Ataque.Pisao: return avisoDoPisao;
            case Ataque.Giro: return avisoDoGiro;
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
            case Ataque.Chifrada:
                Correr(RumoParaOJogador());
                break;

            case Ataque.Pisao:
                Tocar(clipes?.AtaqueEspecial, 14f);
                execucao.Zerar();   // a primeira onda sai ja
                break;

            case Ataque.Giro:
                Tocar(clipes?.AtaqueForte, 12f);
                execucao.Forcar(duracaoDoGiro);
                proximoDisparo.Zerar();
                anguloDoGiro = Random.value * 90f;
                danoDeContato = danoNoGiro;
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
            case Ataque.Chifrada:
                Chifrar(dt);
                break;

            case Ataque.Pisao:
                Frear();

                if (execucao.Ativo)
                    return;

                SoltarOnda();
                ondasSoltas++;

                if (ondasSoltas >= ondas + (segundaFase ? 1 : 0))
                    Recuperar(tempoDeRecuperacao);
                else
                    execucao.Forcar(intervaloEntreOndas / Pressa);

                break;

            case Ataque.Giro:
                Girar(dt);
                break;

            default:
                Recuperar(tempoDeRecuperacao);
                break;
        }
    }

    // ---------------- chifrada ----------------
    private void Correr(Vector2 rumo)
    {
        rumoDaChifrada = rumo;
        correndo = true;
        danoDeContato = danoDaChifrada;
        Rastro.Poeira(rumo, true);
        Rastro.Ligado = true;
        Tocar(clipes?.Ataque, 12f);

        if (animacao != null)
            animacao.OlharPara(rumo);
    }

    private void Chifrar(float dt)
    {
        // Entre um ricochete e outro: parado, mirando o proximo rumo.
        if (!correndo)
        {
            Frear();
            float total = miraDoRicochete / Pressa;
            float t = total <= 0f ? 1f : Mathf.Clamp01(1f - execucao.Restante / total);
            Rastro.MostrarMira(rumoDaChifrada, 0.5f + 0.5f * t);

            if (execucao.Ativo)
                return;

            LimparAviso();
            Correr(rumoDaChifrada);
            return;
        }

        float v = velocidadeDaChifrada * (segundaFase ? 1.15f : 1f);
        RaycastHit2D parede = Physics2D.CircleCast(rb.position, Raio * 0.9f, rumoDaChifrada, v * dt + 0.05f,
                                                   Camadas.MascaraDeParede);

        if (parede.collider == null)
        {
            rb.linearVelocity = rumoDaChifrada * v;
            return;
        }

        // Bateu: pedras voando pra longe da parede.
        rb.linearVelocity = Vector2.zero;
        correndo = false;
        Rastro.Ligado = false;
        Sons.Tocar(Som.Pancada);

        Vector2 normal = parede.normal;
        float anguloDaNormal = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;

        for (int i = 0; i < pedrasPorBatida; i++)
        {
            float desvio = pedrasPorBatida == 1 ? 0f : Mathf.Lerp(-70f, 70f, i / (float)(pedrasPorBatida - 1));
            Atirar(Rumo(anguloDaNormal + desvio), 0.9f);
        }

        batidasRestantes--;

        if (batidasRestantes <= 0)
        {
            danoDeContato = danoDeContatoNormal;
            Recuperar(tempoDeRecuperacao * 1.5f);   // tonto depois da ultima batida
            return;
        }

        // Ricochete: o rumo refletido puxa um pouco pro jogador, pra nao ficar previsivel demais.
        Vector2 refletido = Vector2.Reflect(rumoDaChifrada, normal);
        rumoDaChifrada = (refletido + RumoParaOJogador() * 0.8f).normalized;

        if (Vector2.Dot(rumoDaChifrada, normal) < 0.2f)
            rumoDaChifrada = refletido;

        execucao.Forcar(miraDoRicochete / Pressa);
    }

    // ---------------- pisao ----------------
    private void SoltarOnda()
    {
        Sons.Tocar(Som.Pancada, 0.8f);

        int n = tirosPorOnda;
        int comecoDoBuraco = Random.Range(0, n);
        float giro = ondasSoltas % 2 == 0 ? 0f : 180f / n;

        for (int i = 0; i < n; i++)
        {
            // Os tiros do buraco nao saem: um vao pra passar, num lugar diferente a cada onda.
            if ((i - comecoDoBuraco + n) % n < tamanhoDoBuraco)
                continue;

            Atirar(Rumo(giro + i * 360f / n), 0.85f);
        }
    }

    // ---------------- giro ----------------
    private void Girar(float dt)
    {
        Andar(ParaOJogador(), velocidadeNoGiro * (segundaFase ? 1.2f : 1f));
        proximoDisparo.Contar(dt);

        if (!proximoDisparo.Ativo)
        {
            for (int i = 0; i < 4; i++)
                Atirar(Rumo(anguloDoGiro + i * 90f), 1f);

            anguloDoGiro += segundaFase ? 17f : 12f;
            proximoDisparo.Forcar(intervaloDoGiro);

            // A animacao do giro recomeca enquanto durar.
            if (animacao != null && !animacao.OcupadoComAtaque)
                Tocar(clipes?.AtaqueForte, 12f);
        }

        if (execucao.Ativo)
            return;

        danoDeContato = danoDeContatoNormal;
        Recuperar(tempoDeRecuperacao);
    }

    // ---------------- lacaios ----------------
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
            Vector2 ponto = DentroDaSala(rb.position + lado * (Raio + 0.9f)) - (Vector2)sala.transform.position;
            InimigoDeSala lacaio = sala.CriarInimigo(TipoDeInimigo.Orc, ponto);

            if (lacaio != null)
                lacaios.Add(lacaio);
        }
    }

    private Vector2 DentroDaSala(Vector2 ponto)
    {
        Sala sala = GetComponentInParent<Sala>();

        if (sala == null)
            return ponto;

        Vector2 centro = sala.transform.position;
        Vector2 limite = sala.TamanhoInterno * 0.5f - Vector2.one * 0.7f;
        Vector2 local = ponto - centro;
        return centro + new Vector2(Mathf.Clamp(local.x, -limite.x, limite.x), Mathf.Clamp(local.y, -limite.y, limite.y));
    }

    // ---------------- tiros ----------------
    private void Atirar(Vector2 rumo, float multiplicador)
    {
        float v = velocidadeDoTiro * multiplicador * (segundaFase ? 1.15f : 1f);
        TiroDaSala.Disparar(rb.position + rumo * (Raio + 0.2f), rumo * v, danoDoTiro, gameObject, true, corDoTiro, diametroDoTiro);
    }

    private Vector2 RumoParaOJogador()
    {
        Vector2 alvo = ParaOJogador();
        return alvo.sqrMagnitude > 0.0001f ? alvo.normalized : Vector2.down;
    }

    private static Vector2 Rumo(float graus)
    {
        float r = graus * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
    }

    private void Tocar(Sprite[] quadros, float fps)
    {
        if (animacao != null && quadros != null)
            animacao.TocarUmaVez(quadros, fps * Pressa);
    }

    // ---------------- recuperar ----------------
    private void Recuperar(float tempo)
    {
        executando = false;
        correndo = false;
        Rastro.Ligado = false;
        danoDeContato = danoDeContatoNormal;
        LimparAviso();
        recuperacao.Forcar(tempo / Pressa);
        EstadoAtual = Estado.Recuperando;
    }

    protected override void AtualizarRecuperando(float dt)
    {
        Frear();
        recuperacao.Contar(dt);
        Tingir(Color.Lerp(Color.white, Color.gray, 0.5f));

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
            case Ataque.Chifrada:
                // Raspa o casco (treme, poeira) e mostra a linha de mira, que segue o jogador ate o fim.
                // Avermelha so um pouco: quem avisa o perigo e a linha no chao.
                Tingir(Color.Lerp(Color.white, new Color(1f, 0.72f, 0.65f), t));
                Rastro.Poeira(RumoParaOJogador());
                Rastro.MostrarMira(RumoParaOJogador(), t);
                if (animacao != null)
                    animacao.OlharPara(ParaOJogador());
                if (corpo != null)
                    corpo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 10f) * 6f * t);
                break;

            case Ataque.Pisao:
                // Estica pra cima: vai bater no chao.
                Tingir(Color.Lerp(Color.white, new Color(1f, 0.75f, 0.3f), t));
                if (corpo != null)
                    corpo.localScale = new Vector3(escalaDoCorpo.x * (1f - 0.12f * t), escalaDoCorpo.y * (1f + 0.18f * t), 1f);
                break;

            case Ataque.Giro:
                Tingir(Color.Lerp(Color.white, new Color(0.4f, 0.9f, 1f), t));
                if (corpo != null)
                    corpo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 6f) * 10f * t);
                break;

            case Ataque.Invocar:
                Tingir(Color.Lerp(Color.white, new Color(0.5f, 0.8f, 0.3f), t));
                if (corpo != null)
                    corpo.localScale = escalaDoCorpo * (1f + 0.08f * Mathf.Sin(t * Mathf.PI * 4f));
                break;
        }
    }

    private void LimparAviso()
    {
        if (corpo != null)
        {
            corpo.localScale = escalaDoCorpo;
            corpo.localRotation = Quaternion.identity;
        }

        Rastro.EsconderMira();
    }

    private void Tingir(Color cor)
    {
        if (segundaFase)
            cor = Color.Lerp(cor, Color.red, 0.3f);

        if (desenho != null)
            desenho.color = cor;
    }

    // ---------------- fases e morte ----------------
    private void ChecarSegundaFase()
    {
        if (segundaFase || vida.Fracao > fracaoDaSegundaFase)
            return;

        segundaFase = true;
        velocidade *= 1.2f;
    }

    private float Pressa => segundaFase ? pressaNaSegundaFase : 1f;

    protected override void Morrer()
    {
        LimparAviso();

        lacaios.RemoveAll(l => l == null || l.EstaMorto);
        foreach (InimigoDeSala lacaio in lacaios.ToArray())
            lacaio.Vida.MatarAgora();

        lacaios.Clear();
        base.Morrer();
    }
}
