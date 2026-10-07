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
    private float percorrido;
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

        // Freia ou acelera (sem parar de vez: tiro parado no ar so confunde) e faz a curva.
        if (aceleracao != 0f || curva != 0f)
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

        if (Physics2D.CircleCast(corpo.position, raio, rumo, passo, 1 << Pedreiro.CamadaDaParede))
        {
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

            // Protegido (esquiva, tempinho depois do golpe): tambem passa reto.
            if (!vida.ReceberDano(new Dano(arma.dano, rumo, arma.empurrao, dono)))
                return;
        }

        Sumir();
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
