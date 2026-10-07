using System.Collections;
using UnityEngine;

/// <summary>
/// Um tiro em voo (flecha, bala...). Vai reto ate o alcance e cai; bate em qualquer coisa solida e
/// some. Ainda nao machuca ninguem: quando houver inimigo, e aqui que o dano entra (<see cref="Dano"/>).
/// Monte com <see cref="Disparar"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Projetil : MonoBehaviour
{
    private Rigidbody2D corpo;
    private GameObject dono;
    private float velocidade;
    private float alcance;
    private float percorrido;
    private bool acabou;

    public float Dano { get; private set; }

    public GameObject Dono => dono;

    public static Projetil Disparar(Vector2 origem, Vector2 rumo, DadosDaArma arma, GameObject dono)
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
        projetil.Dano = arma.dano;
        projetil.velocidade = arma.velocidade;
        projetil.alcance = arma.alcance;
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

        percorrido += velocidade * Time.fixedDeltaTime;

        if (percorrido >= alcance)
            Sumir();
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (acabou || outro.isTrigger)
            return;

        // Nao acerta quem atirou.
        if (dono != null && outro.transform.IsChildOf(dono.transform))
            return;

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
