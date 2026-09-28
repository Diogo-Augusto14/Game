using System.Collections;
using UnityEngine;

/// <summary>
/// O projetil do Isaac. Voa reto, e quando percorre o alcance "cai" (encolhe e some).
/// Bate em qualquer <see cref="IDanificavel"/> e estoura; bate em parede/pedra e estoura.
///
/// Quem cria e o <see cref="AtiradorTopDown"/>, que chama <see cref="Disparar"/> logo
/// depois de montar o objeto. Sozinha ela nao sabe dano, alcance nem quem atirou.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Lagrima : MonoBehaviour
{
    [Tooltip("Segundos da animacao de estourar (encolher e sumir)")]
    [SerializeField, Min(0f)] private float duracaoDoEstouro = 0.08f;

    // ---------------- estado ----------------
    private Rigidbody2D rb;
    private Collider2D corpo;
    private GameObject dono;
    private float dano;
    private float alcance;
    private float forcaEmpurrao;
    private float percorrido;
    private bool acabou;

    public GameObject Dono => dono;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        corpo = GetComponent<CircleCollider2D>();

        // Dynamic + trigger: detecta tudo (parede estatica, inimigo, pedra) sem empurrar
        // nada por colisao — quem empurra e o DanoInfo, pelo Vida.
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        corpo.isTrigger = true;
    }

    /// <summary>Solta a lagrima. <paramref name="velocidade"/> ja vem com direcao.</summary>
    public void Disparar(GameObject quemAtirou, Vector2 velocidade, float quantoDano, float ateOnde, float empurrao)
    {
        dono = quemAtirou;
        dano = quantoDano;
        alcance = Mathf.Max(0.1f, ateOnde);
        forcaEmpurrao = empurrao;
        percorrido = 0f;
        acabou = false;

        rb.linearVelocity = velocidade;
    }

    private void FixedUpdate()
    {
        if (acabou)
            return;

        // Conta a distancia de verdade percorrida: se herdou a velocidade do jogador,
        // o alcance continua o mesmo em qualquer direcao.
        percorrido += rb.linearVelocity.magnitude * Time.fixedDeltaTime;

        if (percorrido >= alcance)
            Estourar();
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (acabou)
            return;

        // Nao acerta quem atirou nem as outras lagrimas.
        if (dono != null && outro.transform.IsChildOf(dono.transform))
            return;

        if (outro.GetComponentInParent<Lagrima>() != null)
            return;

        IDanificavel alvo = outro.GetComponentInParent<IDanificavel>();

        if (alvo != null)
        {
            Vector2 direcao = rb.linearVelocity.sqrMagnitude > 0.0001f ? rb.linearVelocity : Vector2.right;
            alvo.TomarDano(new DanoInfo(dano, direcao, forcaEmpurrao, transform.position, dono));
            Estourar();
            return;
        }

        // Trigger de cenario (zona, porta) nao para a lagrima; so coisa solida.
        if (!outro.isTrigger)
            Estourar();
    }

    /// <summary>Para, desliga a colisao, encolhe e some.</summary>
    public void Estourar()
    {
        if (acabou)
            return;

        acabou = true;
        rb.linearVelocity = Vector2.zero;
        corpo.enabled = false;

        StartCoroutine(RotinaEstouro());
    }

    private IEnumerator RotinaEstouro()
    {
        Vector3 escalaInicial = transform.localScale;
        float tempo = 0f;

        while (tempo < duracaoDoEstouro)
        {
            tempo += Time.deltaTime;
            float t = tempo / duracaoDoEstouro;

            // Espalha um pouco e encolhe: le como "splash" mesmo sem arte.
            transform.localScale = escalaInicial * Mathf.Lerp(1.4f, 0f, t);
            yield return null;
        }

        Destroy(gameObject);
    }
}
