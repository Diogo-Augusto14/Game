using UnityEngine;

/// <summary>
/// Boneco minimo de visao de cima, so pra andar pelo andar enquanto o movimento
/// top-down de verdade nao chega: WASD ou setas, 8 direcoes, sem gravidade.
///
/// O <see cref="Andar"/> so cria este boneco quando a cena nao tem ninguem com a tag
/// Player. Quando o jogador de verdade existir, este arquivo pode ser apagado.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class JogadorDeTeste : MonoBehaviour
{
    [SerializeField, Min(0f)] private float velocidade = 5f;

    private Rigidbody2D rb;
    private Vector2 direcao;

    public static GameObject Criar()
    {
        GameObject obj = new GameObject("Jogador de teste") { tag = "Player" };

        Rigidbody2D corpo = obj.AddComponent<Rigidbody2D>();
        corpo.gravityScale = 0f;
        corpo.freezeRotation = true;
        corpo.interpolation = RigidbodyInterpolation2D.Interpolate;
        corpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        obj.AddComponent<CircleCollider2D>().radius = 0.3f;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = Construtor.SpriteDeBloco();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = Vector2.one * 0.6f;
        sr.color = new Color(1f, 0.85f, 0.75f);
        sr.sortingOrder = 10;

        obj.AddComponent<JogadorDeTeste>();
        return obj;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        direcao = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (direcao.sqrMagnitude > 1f)
            direcao.Normalize();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = direcao * velocidade;
    }
}
