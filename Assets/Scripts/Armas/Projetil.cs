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

        SpriteRenderer desenho = obj.AddComponent<SpriteRenderer>();
        desenho.sprite = arma.desenhoDoTiro;
        desenho.color = arma.cor;
        desenho.sortingOrder = 20;

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
        projetil.explode = arma.efeito == EfeitoDoTiro.Explode;
        projetil.quiques = arma.efeito == EfeitoDoTiro.Quica ? 3 : 0;

        // A furia do Machadeiro: os tiros dele batem em dobro enquanto dura.
        if (lado == Lado.Jogador && dono != null && dono.TryGetComponent(out HabilidadeDoHeroi habilidade))
            projetil.multiplicadorDeDano = habilidade.MultiplicadorDeDano;

        // Os itens do jogador: dano, alcance, velocidade, tamanho, atravessar, perseguir, explodir.
        if (lado == Lado.Jogador && dono != null && dono.TryGetComponent(out EstatisticasDoJogador itens))
        {
            projetil.multiplicadorDeDano *= itens.MultiplicadorDoDano(arma.dano);
            projetil.alcance *= itens.AlcanceVezes;
            projetil.velocidade *= itens.VelocidadeDoTiroVezes;
            projetil.raio *= itens.TamanhoVezes;
            colisor.radius = projetil.raio;
            obj.transform.localScale = Vector3.one * itens.TamanhoVezes;
            projetil.atravessa |= itens.Atravessa;
            projetil.persegue = itens.Persegue;
            projetil.explode |= itens.ExplodeAoAcertar;
            rb.linearVelocity = rumo * projetil.velocidade;
        }

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
                Sumir();
                return;
            }

            rumo = ate.normalized;
        }

        // A Bussola Maldita: vira aos poucos pro inimigo vivo mais perto.
        if (persegue && !voltando)
            Perseguir();

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
            if (!vida.ReceberDano(new Dano(arma.dano * multiplicadorDeDano, rumo, arma.empurrao, dono)) && !vida.Bloqueou)
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

    // Gelo e veneno das flechas especiais (so em inimigo).
    private void Efeito(Vida vida)
    {
        if (vida.Lado != Lado.Inimigos)
            return;

        if (arma.efeito == EfeitoDoTiro.Gela)
            CondicaoDoInimigo.Gelar(vida.gameObject, 2.5f);
        else if (arma.efeito == EfeitoDoTiro.Envenena)
            CondicaoDoInimigo.Envenenar(vida.gameObject, 3f, Mathf.Max(2f, arma.dano * 0.6f));
    }

    // A flecha explosiva (e a Polvora em Chamas): uma explosao pequena onde parou, sem ferir o jogador.
    private void Explodir()
    {
        if (!explode || acabou)
            return;

        explode = false;
        Explosao.Criar(corpo.position, 1.3f, arma.dano * multiplicadorDeDano * 0.8f, lado, dono);
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
