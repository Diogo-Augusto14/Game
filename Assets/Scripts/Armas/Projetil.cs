using System.Collections;
using UnityEngine;

/// <summary>
/// Um tiro em voo (flecha, bala...). Vai ate o alcance e cai; bate em qualquer coisa solida e some.
/// Se o que bateu tem <see cref="Vida"/>, machuca e empurra. Pode frear ou acelerar e fazer curva em
/// voo (<see cref="DadosDaArma.aceleracao"/>, <see cref="DadosDaArma.curva"/>): e o que da graca aos
/// padroes de bala dos inimigos.
///
/// Parede (a camada "Wall") e procurada a cada passo, olhando o caminho
/// da frente: assim o tiro nao atravessa parede fina, por mais rapido que seja.
///
/// Atravessa sem machucar quem e do mesmo lado de quem atirou (bala de inimigo passa pelos outros
/// inimigos) e quem esta protegido (o jogador na esquiva ou no tempinho depois de um golpe).
/// Monte com <see cref="Disparar"/> (ou <see cref="DadosDaArma.Disparar"/>, que solta o leque inteiro).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Projetil : MonoBehaviour
{
    private Rigidbody2D corpo;
    private GameObject dono;
    private DadosDaArma arma;
    private Lado lado;
    private Vector2 rumo;
    private float velocidade;
    private float alcance;
    private float raio;
    private float aceleracao;
    private float curva;
    private float giroDoDesenho;
    private bool apontar;
    private bool voltando;
    private float percorrido;
    private float multiplicadorDeDano = 1f;
    private System.Collections.Generic.List<Vida> acertados;
    private bool atravessa;
    private bool persegue;
    private bool explode;
    private int quiques;

    // Os itens das armas novas (1.20.0): o bumerangue que sai de novo, o raio mais longe, as laminas.
    private int voltasExtras;
    private Vector2 rumoDoLancamento;
    private float velocidadeDoLancamento, alcanceDoLancamento;
    private int saltosExtras;
    private float alcanceDoRaio = Raio.Alcance;
    private float chanceDeRaio;
    private bool cortaTiros, devolveTiros;

    /// <summary>Multiplica o dano deste tiro (a furia do Machadeiro dobra).</summary>
    public float MultiplicadorDeDano { get => multiplicadorDeDano; set => multiplicadorDeDano = value; }
    private bool acabou;

    public GameObject Dono => dono;

    public DadosDaArma Arma => arma;

    /// <summary>O lado de quem atirou: nao machuca ninguem desse lado.</summary>
    public Lado Lado => lado;

    public static Projetil Disparar(Vector2 origem, Vector2 rumo, DadosDaArma arma, GameObject dono, Lado lado)
    {
        rumo = rumo.sqrMagnitude > 0.0001f ? rumo.normalized : Vector2.right;

        GameObject obj = new GameObject(arma.nome + " (tiro)");
        obj.transform.position = origem;

        if (arma.apontarODesenho)
            obj.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(rumo.y, rumo.x) * Mathf.Rad2Deg);

        obj.transform.localScale = Vector3.one * arma.escalaDoDesenho;
        SpriteRenderer desenho = obj.AddComponent<SpriteRenderer>();
        desenho.sprite = arma.desenhoDoTiro;
        desenho.color = arma.cor;
        desenho.sortingOrder = 20;
        Iluminacao.Brilhar(desenho);

        // Cinematico + gatilho: voa pela velocidade e so avisa quando encosta (nao empurra nada).
        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D colisor = obj.AddComponent<CircleCollider2D>();
        colisor.isTrigger = true;
        colisor.radius = arma.raio;

        Projetil projetil = obj.AddComponent<Projetil>();
        projetil.dono = dono;
        projetil.arma = arma;
        projetil.lado = lado;
        projetil.rumo = rumo;
        projetil.velocidade = arma.velocidade;
        projetil.alcance = arma.alcance;
        projetil.raio = arma.raio;
        projetil.aceleracao = arma.aceleracao;
        projetil.curva = arma.curva;
        projetil.giroDoDesenho = arma.giroDoDesenho;
        projetil.apontar = arma.apontarODesenho;
        rb.linearVelocity = rumo * arma.velocidade;

        projetil.atravessa = arma.atravessa;
        projetil.persegue = arma.persegue;
        projetil.explode = arma.efeito == EfeitoDoTiro.Explode;
        projetil.quiques = arma.efeito == EfeitoDoTiro.Quica ? 3 : 0;
        projetil.cortaTiros = arma.cortaTiros;
        projetil.devolveTiros = arma.devolveTiros;

        // A furia do Machadeiro: os tiros dele batem em dobro enquanto dura.
        if (lado == Lado.Jogador && dono != null && dono.TryGetComponent(out HabilidadeDoHeroi habilidade))
            projetil.multiplicadorDeDano = habilidade.MultiplicadorDeDano;

        // Os itens do jogador: dano, alcance, velocidade, tamanho, atravessar, perseguir, explodir.
        if (lado == Lado.Jogador && dono != null && dono.TryGetComponent(out EstatisticasDoJogador itens))
        {
            projetil.multiplicadorDeDano *= itens.MultiplicadorDoDano(arma.dano);

            // O Barril de Polvora: o tiro especial bate mais forte.
            if (arma.ehEspecial)
                projetil.multiplicadorDeDano *= itens.PolvoraVezes;
            projetil.alcance *= itens.AlcanceVezes;
            projetil.velocidade *= itens.VelocidadeDoTiroVezes;
            projetil.raio *= itens.TamanhoVezes;
            colisor.radius = projetil.raio;
            obj.transform.localScale = Vector3.one * arma.escalaDoDesenho * itens.TamanhoVezes;
            projetil.atravessa |= itens.Atravessa;
            projetil.persegue |= itens.Persegue;
            projetil.explode |= itens.ExplodeAoAcertar;

            // A Lamina Larga: o corte fica maior e bate mais.
            if (arma.cortaTiros)
            {
                projetil.raio *= itens.CorteVezes;
                colisor.radius = projetil.raio;
                obj.transform.localScale *= itens.CorteVezes;
                projetil.multiplicadorDeDano *= itens.CorteDanoVezes;
            }

            // A Corda Encantada (o bumerangue sai de novo), a Bobina de Cobre (o raio) e as sinergias das laminas.
            projetil.voltasExtras = arma.volta ? itens.VoltasExtras : 0;
            projetil.saltosExtras = itens.SaltosExtras;
            projetil.alcanceDoRaio = Raio.Alcance * itens.AlcanceDoRaioVezes;
            projetil.chanceDeRaio = itens.ChanceDeRaio;
            projetil.cortaTiros |= arma.volta && itens.BumerangueCorta;
            projetil.devolveTiros |= itens.LaminasDevolvem;
            rb.linearVelocity = rumo * projetil.velocidade;
        }

        projetil.rumoDoLancamento = rumo;
        projetil.velocidadeDoLancamento = projetil.velocidade;
        projetil.alcanceDoLancamento = projetil.alcance;
        return projetil;
    }

    private void Awake()
    {
        corpo = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (acabou)
            return;

        // Bumerangue: freou ate quase parar, vira e volta acelerando pra quem jogou.
        if (arma.volta && !voltando && aceleracao < 0f && velocidade <= 0.85f)
        {
            voltando = true;
            aceleracao = -aceleracao;
            alcance = percorrido + 40f;
        }

        if (voltando)
        {
            if (dono == null)
            {
                Sumir();
                return;
            }

            Vector2 ate = (Vector2)dono.transform.position - corpo.position;

            if (ate.magnitude < 0.7f)
            {
                if (voltasExtras > 0)
                    Relancar();
                else
                    Sumir();

                return;
            }

            rumo = ate.normalized;
        }

        // A Bussola Maldita (e o Tomo Proibido): vira aos poucos pro inimigo vivo mais perto.
        if (persegue && !voltando)
            Perseguir();

        // As laminas: o corte apaga (ou devolve) os tiros do outro lado que encosta.
        if (cortaTiros)
            Cortar();

        // Freia ou acelera (sem parar de vez: tiro parado no ar so confunde) e faz a curva.
        if (aceleracao != 0f || curva != 0f || voltando || persegue)
        {
            velocidade = Mathf.Max(0.8f, velocidade + aceleracao * Time.fixedDeltaTime);
            float a = Mathf.Atan2(rumo.y, rumo.x) + curva * Mathf.Deg2Rad * Time.fixedDeltaTime;
            rumo = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            corpo.linearVelocity = rumo * velocidade;

            if (apontar && giroDoDesenho == 0f)
                corpo.MoveRotation(a * Mathf.Rad2Deg);
        }

        if (giroDoDesenho != 0f)
            corpo.MoveRotation(corpo.rotation + giroDoDesenho * Time.fixedDeltaTime);

        float passo = velocidade * Time.fixedDeltaTime;

        RaycastHit2D parede = Physics2D.CircleCast(corpo.position, raio, rumo, passo, 1 << Pedreiro.CamadaDaParede);

        if (parede.collider != null)
        {
            // A flecha que quica: reflete na parede e segue.
            if (quiques > 0 && parede.normal.sqrMagnitude > 0.01f)
            {
                quiques--;
                rumo = Vector2.Reflect(rumo, parede.normal).normalized;
                corpo.linearVelocity = rumo * velocidade;

                if (apontar)
                    corpo.MoveRotation(Mathf.Atan2(rumo.y, rumo.x) * Mathf.Rad2Deg);

                return;
            }

            Explodir();
            Sumir();
            return;
        }

        percorrido += passo;

        if (percorrido >= alcance)
            Sumir();
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        // Gatilhos nao seguram tiro, e buraco o tiro passa por cima.
        if (acabou || outro.isTrigger || outro.gameObject.layer == Pedreiro.CamadaDoBuraco)
            return;

        // Nao acerta quem atirou.
        if (dono != null && outro.transform.IsChildOf(dono.transform))
            return;

        Vida vida = outro.GetComponentInParent<Vida>();

        if (vida != null)
        {
            // Mesmo lado de quem atirou: passa reto.
            if (vida.Lado == lado)
                return;

            // A onda que atravessa acerta cada um uma vez so.
            if (atravessa && acertados != null && acertados.Contains(vida))
                return;

            // Protegido (esquiva, tempinho depois do golpe): tambem passa reto. Bateu num escudo: some.
            if (!vida.ReceberDano(new Dano(arma.dano * multiplicadorDeDano, rumo, arma.empurrao, dono) { especial = arma.ehEspecial }) && !vida.Bloqueou)
                return;

            Efeito(vida);

            if (atravessa && !vida.Bloqueou)
            {
                if (acertados == null)
                    acertados = new System.Collections.Generic.List<Vida>();
                acertados.Add(vida);
                return;
            }
        }

        Explodir();
        Sumir();
    }

    // Gelo e veneno das flechas especiais (so em inimigo), o raio que pula e o tiro que se divide.
    private void Efeito(Vida vida)
    {
        if (vida.Lado != Lado.Inimigos)
            return;

        if (arma.encadeia > 0)
            Raio.Encadear(vida, arma.dano * multiplicadorDeDano * 0.7f, arma.encadeia + saltosExtras, dono, lado, arma.cor, alcanceDoRaio);
        else if (chanceDeRaio > 0f && Random.value < chanceDeRaio)
            Raio.Encadear(vida, arma.dano * multiplicadorDeDano * 0.5f, 1 + saltosExtras, dono, lado, new Color(0.6f, 0.85f, 1f), alcanceDoRaio);

        if (arma.divideAoAcertar > 0)
            Dividir(vida);

        if (arma.efeito == EfeitoDoTiro.Gela)
            CondicaoDoInimigo.Gelar(vida.gameObject, 2.5f);
        else if (arma.efeito == EfeitoDoTiro.Envenena)
            CondicaoDoInimigo.Envenenar(vida.gameObject, 3f, Mathf.Max(2f, arma.dano * 0.6f));
    }

    // A flecha explosiva (e a Polvora em Chamas): uma explosao pequena onde parou, sem ferir o jogador. A do
    // tiro especial explosivo e maior.
    private void Explodir()
    {
        if (!explode || acabou)
            return;

        explode = false;
        Explosao.Criar(corpo.position, arma.ehEspecial ? 2.2f : 1.3f, arma.dano * multiplicadorDeDano * 0.8f, lado, dono, arma.ehEspecial);
    }

    // Os pedacos saem em leque pra frente, de onde acertou (o que foi acertado ja levou o golpe: atravessam ele).
    private void Dividir(Vida acertado)
    {
        DadosDaArma pedaco = arma.Fragmento;
        int n = arma.divideAoAcertar;
        float angulo = Mathf.Atan2(rumo.y, rumo.x) * Mathf.Rad2Deg;

        for (int i = 0; i < n; i++)
        {
            float a = (angulo + (i - (n - 1) * 0.5f) * 28f) * Mathf.Deg2Rad;
            Vector2 r = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Projetil p = Disparar(corpo.position + r * 0.3f, r, pedaco, dono, lado);
            p.acertados = new System.Collections.Generic.List<Vida> { acertado };
            p.atravessa = true;
            p.multiplicadorDeDano = multiplicadorDeDano;
        }
    }

    // A Corda Encantada: o bumerangue chegou na mao e sai de novo, pra onde o jogador mira agora.
    private void Relancar()
    {
        voltasExtras--;
        voltando = false;
        rumo = dono != null && dono.TryGetComponent(out ControlesDoJogador controles) && controles.Mira.sqrMagnitude > 0.01f
            ? controles.Mira.normalized : rumoDoLancamento;
        velocidade = velocidadeDoLancamento;
        aceleracao = -Mathf.Abs(aceleracao);
        alcance = alcanceDoLancamento;
        percorrido = 0f;
        acertados?.Clear();
        corpo.linearVelocity = rumo * velocidade;
        Sons.Tocar(Som.Bumerangue, 0.4f);
    }

    // Procura tiros do outro lado encostando no corte.
    private void Cortar()
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(corpo.position, raio + 0.15f))
        {
            if (c == null || !c.TryGetComponent(out Projetil outro) || outro == this || outro.acabou || outro.lado == lado)
                continue;

            Sons.Tocar(Som.CorteDeTiro, 0.55f);

            if (devolveTiros)
                outro.Devolver(dono, lado, rumo);
            else
            {
                outro.Sumir();
                EfeitoDeFolha efeito = EfeitoDeFolha.Tocar(ArteDoAntigo.ExplosaoPequena, outro.transform.position, 30f, 25);

                if (efeito != null)
                    efeito.transform.localScale = Vector3.one * 0.5f;
            }
        }
    }

    /// <summary>
    /// O tiro muda de lado (a Lamina Real devolve): vai pra onde o corte ia, mais rapido e batendo bem mais
    /// forte (tiro de inimigo tira pouco: no inimigo, quatro vezes).
    /// </summary>
    public void Devolver(GameObject novoDono, Lado novoLado, Vector2 pra)
    {
        if (acabou || lado == novoLado)
            return;

        dono = novoDono;
        lado = novoLado;
        rumo = pra.sqrMagnitude > 0.0001f ? pra.normalized : -rumo;
        velocidade = Mathf.Max(velocidade, 10f) * 1.2f;
        aceleracao = Mathf.Max(0f, aceleracao);
        curva = 0f;
        alcance = percorrido + 12f;
        multiplicadorDeDano = Mathf.Max(multiplicadorDeDano, 1f) * 4f;
        corpo.linearVelocity = rumo * velocidade;

        if (TryGetComponent(out SpriteRenderer desenho))
            desenho.color = new Color(1f, 0.9f, 0.45f, desenho.color.a);

        if (apontar)
            corpo.MoveRotation(Mathf.Atan2(rumo.y, rumo.x) * Mathf.Rad2Deg);
    }

    private void Perseguir()
    {
        Vida alvo = null;
        float melhor = 6f * 6f;

        foreach (Vida v in GeradorDoAndar.Vivos)
        {
            if (v == null || v.Morto)
                continue;

            float d = ((Vector2)v.transform.position - corpo.position).sqrMagnitude;

            if (d < melhor)
            {
                melhor = d;
                alvo = v;
            }
        }

        if (alvo == null)
            return;

        Vector2 quer = ((Vector2)alvo.transform.position - corpo.position).normalized;
        float atual = Mathf.Atan2(rumo.y, rumo.x) * Mathf.Rad2Deg;
        float desejado = Mathf.Atan2(quer.y, quer.x) * Mathf.Rad2Deg;
        float novo = Mathf.MoveTowardsAngle(atual, desejado, 220f * Time.fixedDeltaTime) * Mathf.Deg2Rad;
        rumo = new Vector2(Mathf.Cos(novo), Mathf.Sin(novo));
    }

    /// <summary>Para, encolhe um instante e some.</summary>
    public void Sumir()
    {
        if (acabou)
            return;

        acabou = true;
        corpo.linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(Encolher());
    }

    private IEnumerator Encolher()
    {
        Vector3 inicio = transform.localScale;

        for (float t = 0f; t < 0.08f; t += Time.deltaTime)
        {
            transform.localScale = inicio * (1f - t / 0.08f);
            yield return null;
        }

        Destroy(gameObject);
    }
}
