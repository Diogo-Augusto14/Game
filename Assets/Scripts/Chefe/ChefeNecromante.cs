using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O chefe do andar 3, o Rei Necromante (o Necromante do Tiny RPG, bem maior). Nao corre
/// atras de ninguem: fica longe, some e aparece, e enche a sala de ossos. Alterna entre
/// quatro ataques, cada um com o seu aviso:
///
///   Sumico   -> encolhe e some; uma marca roxa no chao mostra onde vai voltar. Volta
///               longe do jogador soltando um anel de tiros
///   Ossos    -> levanta o cajado: marcas vermelhas aparecem em volta do jogador e, um
///               tempo depois, cada uma explode em ossos (cruz de tiros)
///   Muralha  -> uma parede de tiros atravessa a sala de um lado ao outro, com um buraco
///               pra passar. O buraco pisca no aviso
///   Invocar  -> levanta dois esqueletos do chao
///
/// Com metade da vida entra na SEGUNDA FASE: mais marcas de ossos (em X e em cruz), a
/// muralha vem em dobro (uma de lado e uma de cima), levanta tres esqueletos e depois de
/// cada ataque tem chance de sumir e aparecer em outro canto.
/// </summary>
public class ChefeNecromante : InimigoDeSala, IChefe
{
    private enum Ataque
    {
        Sumico,
        Ossos,
        Muralha,
        Invocar
    }

    [Header("Chefe")]
    [SerializeField] private string nomeDoChefe = "Rei Necromante";

    [SerializeField, Min(0.1f)] private float intervaloEntreAtaques = 1.2f;

    [SerializeField, Min(0f)] private float esperaInicial = 1.2f;

    [SerializeField, Range(0f, 1f)] private float fracaoDaSegundaFase = 0.5f;

    [SerializeField, Min(1f)] private float pressaNaSegundaFase = 1.3f;

    [Tooltip("Fica mais ou menos a esta distancia do jogador")]
    [SerializeField, Min(0f)] private float distanciaPreferida = 4.5f;

    [Header("Tiros")]
    [SerializeField, Min(0f)] private float danoDoTiro = 10f;

    [SerializeField, Min(0.1f)] private float velocidadeDoTiro = 3.8f;

    [SerializeField, Min(0.05f)] private float diametroDoTiro = 0.34f;

    [SerializeField] private Color corDoTiro = new Color(0.7f, 0.45f, 1f);

    [Header("Sumico")]
    [SerializeField, Min(0f)] private float avisoDoSumico = 0.5f;

    [Tooltip("Segundos sumido (a marca mostra onde volta)")]
    [SerializeField, Min(0.1f)] private float tempoSumido = 0.8f;

    [SerializeField, Min(0)] private int tirosAoVoltar = 10;

    [Tooltip("Na segunda fase, chance de sumir de novo depois de outro ataque")]
    [SerializeField, Range(0f, 1f)] private float chanceDeSumirDepois = 0.5f;

    [Header("Ossos")]
    [SerializeField, Min(0f)] private float avisoDosOssos = 0.4f;

    [Tooltip("Segundos entre a marca aparecer e explodir")]
    [SerializeField, Min(0.2f)] private float tempoAteExplodir = 1f;

    [SerializeField, Min(1)] private int marcasDeOssos = 4;

    [SerializeField, Min(0.1f)] private float raioDaExplosao = 0.65f;

    [SerializeField, Min(0f)] private float danoDaExplosao = 15f;

    [Header("Muralha")]
    [SerializeField, Min(0f)] private float avisoDaMuralha = 0.8f;

    [SerializeField, Min(0.1f)] private float velocidadeDaMuralha = 4.5f;

    [Tooltip("Distancia entre os tiros da muralha")]
    [SerializeField, Min(0.2f)] private float espacoNaMuralha = 0.5f;

    [Tooltip("Largura do buraco por onde o jogador passa")]
    [SerializeField, Min(0.5f)] private float larguraDoBuraco = 1.6f;

    [Header("Invocar")]
    [SerializeField, Min(0f)] private float avisoDeInvocar = 0.9f;

    [SerializeField, Min(0)] private int lacaiosPorVez = 2;

    [SerializeField, Min(0)] private int maximoDeLacaios = 4;

    [Header("Depois de cada ataque")]
    [SerializeField, Min(0f)] private float tempoDeRecuperacao = 0.7f;

    // ---------------- estado ----------------
    private readonly List<InimigoDeSala> lacaios = new List<InimigoDeSala>();
    private readonly List<(SpriteRenderer marca, float explode)> ossos = new List<(SpriteRenderer, float)>();
    private readonly List<SpriteRenderer> buracos = new List<SpriteRenderer>();

    private Ataque ataqueAtual;
    private Ataque? ultimoAtaque;
    private bool executando;
    private bool segundaFase;
    private bool barraCriada;
    private bool sumido;
    private bool sumicoExtra;
    private Vector2 destinoDoSumico;
    private readonly List<(bool deLado, float buraco)> muralhas = new List<(bool, float)>();

    private Cronometro recarga;
    private Cronometro aviso;
    private Cronometro execucao;
    private Cronometro recuperacao;

    private Transform corpo;
    private Vector3 escalaDoCorpo;
    private SpriteRenderer marcaDoSumico;
    private Collider2D colisor;
    private AnimacaoDePersonagem animacao;
    private ClipesDePersonagem clipes;
    private float danoDeContatoNormal;

    public string Nome => nomeDoChefe;

    public bool NaSegundaFase => segundaFase;

    protected override bool Imparavel => true;

    // ---------------- montagem ----------------
    /// <summary>Sombra e a marca de onde ele volta do sumico. A fabrica chama.</summary>
    public void Enfeitar(ClipesDePersonagem arte)
    {
        clipes = arte;

        FormasDaSala.Desenho(transform, "Sombra", FormasDaSala.Circulo(), new Color(0f, 0f, 0f, 0.35f),
            new Vector2(0f, -Raio * 0.55f), new Vector2(Raio * 2.1f, Raio * 0.7f), 9);

        // Solta no mundo (filha da sala), senao andaria junto com ele.
        marcaDoSumico = FormasDaSala.Desenho(transform.parent, "Marca do sumico", FormasDaSala.Circulo(),
            new Color(0.6f, 0.3f, 1f, 0.3f), Vector2.zero, Vector2.one * Raio * 2f, -5);
        marcaDoSumico.enabled = false;
    }

    protected override void Awake()
    {
        base.Awake();

        rb.mass = 20f;
        colisor = GetComponent<Collider2D>();
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

    private void OnDestroy()
    {
        LimparMarcas();

        if (marcaDoSumico != null)
            Destroy(marcaDoSumico.gameObject);
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

        // Mantem distancia: perto demais recua, longe demais chega perto.
        Vector2 alvo = ParaOJogador();
        float distancia = alvo.magnitude;
        float sobra = distancia - distanciaPreferida;
        Andar(distancia < 0.0001f || Mathf.Abs(sobra) < 0.7f ? Vector2.zero : alvo * Mathf.Sign(sobra), velocidade * 0.8f);

        if (!recarga.Ativo && jogador != null)
            ComecarAtaque(EscolherAtaque());
    }

    private Ataque EscolherAtaque()
    {
        List<Ataque> opcoes = new List<Ataque> { Ataque.Sumico, Ataque.Ossos, Ataque.Muralha };

        lacaios.RemoveAll(l => l == null || l.EstaMorto);

        if (lacaiosPorVez > 0 && lacaios.Count < maximoDeLacaios && ultimoAtaque != Ataque.Invocar)
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

        if (ataque == Ataque.Muralha)
            PrepararMuralhas();

        aviso.Forcar(TempoDeAviso() / Pressa);
        EstadoAtual = Estado.Preparando;
        Frear();

        if (animacao != null && clipes != null)
        {
            animacao.OlharPara(ParaOJogador());
            animacao.TocarUmaVez(ataque == Ataque.Invocar || ataque == Ataque.Muralha ? clipes.AtaqueEspecial ?? clipes.Ataque : clipes.Ataque,
                                 10f * Pressa);
        }
    }

    private float TempoDeAviso()
    {
        switch (ataqueAtual)
        {
            case Ataque.Sumico: return avisoDoSumico;
            case Ataque.Ossos: return avisoDosOssos;
            case Ataque.Muralha: return avisoDaMuralha;
            default: return avisoDeInvocar;
        }
    }

    // ---------------- atacar ----------------
    protected override void AtualizarPreparando(float dt)
    {
        AtualizarOssos();

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
            case Ataque.Sumico:
                Sumir();
                break;

            case Ataque.Ossos:
                MarcarOssos();
                Recuperar(tempoDeRecuperacao);
                break;

            case Ataque.Muralha:
                SoltarMuralhas();
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
        Frear();

        if (ataqueAtual != Ataque.Sumico)
        {
            Recuperar(tempoDeRecuperacao);
            return;
        }

        // Sumido: a marca cresce ate o tamanho dele; cheia, ele volta.
        float t = Mathf.Clamp01(1f - execucao.Restante / (tempoSumido / Pressa));

        if (marcaDoSumico != null)
        {
            marcaDoSumico.transform.localScale = Vector3.one * Raio * 2f * Mathf.Lerp(0.3f, 1.2f, t);
            Color cor = marcaDoSumico.color;
            cor.a = Mathf.Lerp(0.15f, 0.55f, t);
            marcaDoSumico.color = cor;
        }

        if (!execucao.Ativo)
            Voltar();
    }

    // ---------------- sumico ----------------
    private void Sumir()
    {
        sumido = true;
        destinoDoSumico = PontoLongeDoJogador();

        if (colisor != null)
            colisor.enabled = false;

        danoDeContato = 0f;

        if (corpo != null)
            corpo.localScale = Vector3.zero;

        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = false;

        if (marcaDoSumico != null)
        {
            marcaDoSumico.enabled = true;
            marcaDoSumico.transform.position = destinoDoSumico;
        }

        Sons.Tocar(Som.Segredo, 0.7f);
        execucao.Forcar(tempoSumido / Pressa);
    }

    private void Voltar()
    {
        sumido = false;
        rb.position = destinoDoSumico;
        transform.position = destinoDoSumico;
        rb.linearVelocity = Vector2.zero;

        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = true;

        if (corpo != null)
            corpo.localScale = escalaDoCorpo;

        if (colisor != null)
            colisor.enabled = true;

        danoDeContato = danoDeContatoNormal;

        if (marcaDoSumico != null)
            marcaDoSumico.enabled = false;

        Sons.Tocar(Som.Pancada, 0.6f);

        int quantos = tirosAoVoltar + (segundaFase ? 4 : 0);
        float inicio = Random.value * 360f;

        for (int i = 0; i < quantos; i++)
            Atirar(Rumo(inicio + i * 360f / quantos), destinoDoSumico);

        Recuperar(tempoDeRecuperacao);
    }

    /// <summary>O ponto livre da sala mais longe do jogador, entre alguns sorteados.</summary>
    private Vector2 PontoLongeDoJogador()
    {
        Sala sala = GetComponentInParent<Sala>();

        if (sala == null)
            return rb.position;

        Vector2 centro = sala.transform.position;
        Vector2 jogadorEm = jogador != null ? (Vector2)jogador.position : rb.position;
        Vector2 melhor = rb.position;
        float maiorDistancia = -1f;

        for (int i = 0; i < 8; i++)
        {
            Vector2 ponto = centro + sala.PontoLivreAleatorio(1.4f);
            float d = Vector2.Distance(ponto, jogadorEm);

            if (d > maiorDistancia)
            {
                maiorDistancia = d;
                melhor = ponto;
            }
        }

        return melhor;
    }

    // ---------------- ossos ----------------
    private void MarcarOssos()
    {
        if (jogador == null)
            return;

        Vector2 alvo = jogador.position;
        int quantas = marcasDeOssos + (segundaFase ? 2 : 0);

        // A primeira cai em cima do jogador; as outras em volta dele.
        for (int i = 0; i < quantas; i++)
        {
            Vector2 ponto = i == 0 ? alvo : alvo + Random.insideUnitCircle.normalized * Random.Range(1.2f, 2.6f);
            ponto = DentroDaSala(ponto, raioDaExplosao);

            SpriteRenderer marca = FormasDaSala.Desenho(transform.parent, "Marca dos ossos", FormasDaSala.Circulo(),
                new Color(1f, 0.15f, 0.1f, 0.2f), Vector2.zero, Vector2.one * raioDaExplosao * 2f, -5);
            marca.transform.position = ponto;

            // Uma depois da outra, pra dar tempo de fugir.
            ossos.Add((marca, Time.time + tempoAteExplodir / Pressa + i * 0.12f));
        }

        Sons.Tocar(Som.Aviso, 0.7f);
    }

    /// <summary>Roda sempre (mesmo em outros ataques): as marcas explodem sozinhas na hora delas.</summary>
    private void AtualizarOssos()
    {
        for (int i = ossos.Count - 1; i >= 0; i--)
        {
            (SpriteRenderer marca, float explode) = ossos[i];

            if (marca == null)
            {
                ossos.RemoveAt(i);
                continue;
            }

            float falta = explode - Time.time;

            if (falta > 0f)
            {
                // Pisca mais forte perto de explodir.
                Color cor = marca.color;
                cor.a = falta < 0.3f ? (Mathf.Repeat(Time.time * 12f, 1f) < 0.5f ? 0.6f : 0.25f) : Mathf.Lerp(0.45f, 0.2f, falta);
                marca.color = cor;
                continue;
            }

            Explodir(marca.transform.position);
            Destroy(marca.gameObject);
            ossos.RemoveAt(i);
        }
    }

    private void Explodir(Vector2 centro)
    {
        Explosao.Efeito(centro, raioDaExplosao);
        Sons.Tocar(Som.Explosao, 0.5f);

        if (jogador != null && Vector2.Distance(jogador.position, centro) < raioDaExplosao + 0.25f)
        {
            IDanificavel alvo = jogador.GetComponentInParent<IDanificavel>();
            alvo?.TomarDano(new DanoInfo(danoDaExplosao, (Vector2)jogador.position - centro, 5f, centro, gameObject));
        }

        // Ossos voando: cruz (e X tambem, na segunda fase).
        int quantos = segundaFase ? 8 : 4;
        float giro = segundaFase ? 0f : (Random.value < 0.5f ? 0f : 45f);

        for (int i = 0; i < quantos; i++)
            Atirar(Rumo(giro + i * 360f / quantos), centro, 0.8f);
    }

    private void LimparMarcas()
    {
        foreach ((SpriteRenderer marca, float _) in ossos)
            if (marca != null)
                Destroy(marca.gameObject);

        ossos.Clear();
        LimparBuracos();
    }

    // ---------------- muralha ----------------
    /// <summary>Sorteia de onde vem cada muralha e onde fica o buraco, pra o aviso mostrar.</summary>
    private void PrepararMuralhas()
    {
        muralhas.Clear();
        LimparBuracos();

        bool deLado = Random.value < 0.5f;
        muralhas.Add((deLado, 0f));

        if (segundaFase)
            muralhas.Add((!deLado, 0f));

        Sala sala = GetComponentInParent<Sala>();
        Vector2 meio = sala != null ? sala.TamanhoInterno * 0.5f : new Vector2(6.5f, 3.5f);
        Vector2 centro = sala != null ? (Vector2)sala.transform.position : rb.position;

        for (int i = 0; i < muralhas.Count; i++)
        {
            bool lado = muralhas[i].deLado;
            float alcance = (lado ? meio.y : meio.x) - larguraDoBuraco * 0.5f;
            float buraco = Random.Range(-alcance, alcance);
            muralhas[i] = (lado, buraco);

            // O buraco aparece no chao como uma faixa verde atravessando a sala.
            Vector2 tamanho = lado ? new Vector2(meio.x * 2f, larguraDoBuraco) : new Vector2(larguraDoBuraco, meio.y * 2f);
            Vector2 posicao = centro + (lado ? new Vector2(0f, buraco) : new Vector2(buraco, 0f));
            SpriteRenderer faixa = FormasDaSala.Desenho(transform.parent, "Buraco da muralha", FormasDaSala.Quadrado(),
                new Color(0.3f, 1f, 0.4f, 0.12f), Vector2.zero, tamanho, -5);
            faixa.transform.position = posicao;
            buracos.Add(faixa);
        }
    }

    private void SoltarMuralhas()
    {
        Sala sala = GetComponentInParent<Sala>();

        if (sala == null)
            return;

        Vector2 centro = sala.transform.position;
        Vector2 meio = sala.TamanhoInterno * 0.5f;
        float v = velocidadeDaMuralha * (segundaFase ? 1.1f : 1f);

        foreach ((bool deLado, float buraco) in muralhas)
        {
            // De lado: coluna de tiros andando no x; senao linha de tiros andando no y.
            // Sai do lado mais longe do jogador, pra dar tempo de achar o buraco.
            Vector2 jogadorLocal = jogador != null ? (Vector2)jogador.position - centro : Vector2.zero;
            float sinal = deLado ? (jogadorLocal.x > 0f ? -1f : 1f) : (jogadorLocal.y > 0f ? -1f : 1f);
            float largura = deLado ? meio.y : meio.x;
            float partida = (deLado ? meio.x : meio.y) - 0.3f;

            for (float p = -largura + espacoNaMuralha * 0.5f; p < largura; p += espacoNaMuralha)
            {
                if (Mathf.Abs(p - buraco) < larguraDoBuraco * 0.5f)
                    continue;

                Vector2 origem = deLado ? new Vector2(sinal * partida, p) : new Vector2(p, sinal * partida);
                Vector2 rumo = deLado ? new Vector2(-sinal, 0f) : new Vector2(0f, -sinal);
                TiroDaSala.Disparar(centro + origem, rumo * v, danoDoTiro, gameObject, true, corDoTiro, diametroDoTiro);
            }
        }

        LimparBuracos();
    }

    private void LimparBuracos()
    {
        foreach (SpriteRenderer faixa in buracos)
            if (faixa != null)
                Destroy(faixa.gameObject);

        buracos.Clear();
    }

    // ---------------- invocar ----------------
    private void Invocar()
    {
        Sala sala = GetComponentInParent<Sala>();

        if (sala == null)
            return;

        lacaios.RemoveAll(l => l == null || l.EstaMorto);
        int quantos = Mathf.Min(lacaiosPorVez + (segundaFase ? 1 : 0), maximoDeLacaios - lacaios.Count);

        for (int i = 0; i < quantos; i++)
        {
            float angulo = i * 360f / Mathf.Max(1, quantos) + 90f;
            Vector2 ponto = DentroDaSala(rb.position + Rumo(angulo) * (Raio + 1f), 0.5f) - (Vector2)sala.transform.position;
            TipoDeInimigo tipo = i % 2 == 0 ? TipoDeInimigo.EsqueletoGuerreiro : TipoDeInimigo.Esqueleto;
            InimigoDeSala lacaio = sala.CriarInimigo(tipo, ponto);

            if (lacaio != null)
                lacaios.Add(lacaio);
        }
    }

    // ---------------- tiros ----------------
    private void Atirar(Vector2 rumo, Vector2 origem, float multiplicador = 1f)
    {
        float v = velocidadeDoTiro * multiplicador * (segundaFase ? 1.15f : 1f);
        TiroDaSala.Disparar(origem + rumo * 0.3f, rumo * v, danoDoTiro, gameObject, true, corDoTiro, diametroDoTiro);
    }

    private static Vector2 Rumo(float graus)
    {
        float r = graus * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
    }

    private Vector2 DentroDaSala(Vector2 ponto, float margem)
    {
        Sala sala = GetComponentInParent<Sala>();

        if (sala == null)
            return ponto;

        Vector2 centro = sala.transform.position;
        Vector2 limite = sala.TamanhoInterno * 0.5f - Vector2.one * (margem + 0.2f);
        Vector2 local = ponto - centro;
        return centro + new Vector2(Mathf.Clamp(local.x, -limite.x, limite.x), Mathf.Clamp(local.y, -limite.y, limite.y));
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
        AtualizarOssos();
        Frear();
        recuperacao.Contar(dt);
        Tingir(Color.Lerp(Color.white, Color.gray, 0.5f));

        if (recuperacao.Ativo)
            return;

        // Na segunda fase as vezes some logo depois de atacar (nunca dois sumicos seguidos).
        if (segundaFase && !sumicoExtra && ultimoAtaque != Ataque.Sumico && Random.value < chanceDeSumirDepois)
        {
            sumicoExtra = true;
            ataqueAtual = Ataque.Sumico;
            aviso.Forcar(avisoDoSumico * 0.5f);
            executando = false;
            EstadoAtual = Estado.Preparando;
            return;
        }

        sumicoExtra = false;
        recarga.Forcar(intervaloEntreAtaques / Pressa);
        EstadoAtual = Estado.Agindo;
    }

    // ---------------- avisos ----------------
    private void Avisar(float t)
    {
        switch (ataqueAtual)
        {
            case Ataque.Sumico:
                // Encolhe e fica roxo.
                Tingir(Color.Lerp(Color.white, new Color(0.6f, 0.3f, 1f), t));
                if (corpo != null)
                    corpo.localScale = escalaDoCorpo * (1f - 0.4f * t);
                break;

            case Ataque.Ossos:
                Tingir(Color.Lerp(Color.white, new Color(1f, 0.35f, 0.3f), t));
                break;

            case Ataque.Muralha:
                Tingir(Color.Lerp(Color.white, new Color(0.3f, 1f, 0.5f), t));

                // O buraco da muralha pisca: e ali que tem de ficar.
                foreach (SpriteRenderer faixa in buracos)
                {
                    if (faixa == null)
                        continue;

                    Color cor = faixa.color;
                    cor.a = Mathf.Repeat(Time.time * 6f, 1f) < 0.5f ? 0.3f : 0.12f;
                    faixa.color = cor;
                }

                break;

            case Ataque.Invocar:
                Tingir(Color.Lerp(Color.white, new Color(0.5f, 0.5f, 0.6f), t));
                if (corpo != null)
                    corpo.localScale = escalaDoCorpo * (1f + 0.08f * Mathf.Sin(t * Mathf.PI * 4f));
                break;
        }
    }

    private void LimparAviso()
    {
        if (corpo != null && !sumido)
            corpo.localScale = escalaDoCorpo;
    }

    private void Tingir(Color cor)
    {
        if (segundaFase)
            cor = Color.Lerp(cor, new Color(0.8f, 0.4f, 1f), 0.35f);

        if (desenho != null && !sumido)
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
        // Morreu sumido (bomba na marca, por exemplo): volta a aparecer pra morrer.
        if (sumido)
        {
            sumido = false;

            foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
                sr.enabled = true;
        }

        if (corpo != null)
            corpo.localScale = escalaDoCorpo;

        if (marcaDoSumico != null)
            marcaDoSumico.enabled = false;

        LimparMarcas();

        lacaios.RemoveAll(l => l == null || l.EstaMorto);
        foreach (InimigoDeSala lacaio in lacaios.ToArray())
            lacaio.Vida.MatarAgora();

        lacaios.Clear();
        base.Morrer();
    }
}
