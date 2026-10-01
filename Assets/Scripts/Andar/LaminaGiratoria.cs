using UnityEngine;

/// <summary>
/// Armadilha que anda: uma serra girando num trilho, de um lado ao outro da sala, sem
/// parar. Corta o heroi (meio coracao) e passa por cima de pedra e buraco; inimigo nao liga
/// pra ela. Quando a sala e limpa, a serra encolhe e some.
///
/// O desenho da serra e gerado aqui (disco com dentes); o trilho e uma faixa escura no chao,
/// pra dar pra ver por onde ela vai passar.
/// </summary>
[DisallowMultipleComponent]
public class LaminaGiratoria : MonoBehaviour
{
    private const float Raio = 0.38f;

    [SerializeField, Min(0.1f)] private float velocidade = 2.6f;
    [SerializeField, Min(0f)] private float dano = 10f;

    private static Sprite serra;

    private Rigidbody2D rb;
    private Vector2 de;
    private Vector2 ate;
    private float t;
    private float sentido = 1f;
    private float sumindo = -1f;
    private Transform desenho;

    /// <summary>Uma serra entre <paramref name="a"/> e <paramref name="b"/> (relativos ao centro da sala).</summary>
    public static LaminaGiratoria Criar(Sala sala, Vector2 a, Vector2 b, float comecoDoCaminho)
    {
        Vector2 centro = sala.transform.position;

        // Trilho: faixa escura de ponta a ponta.
        Vector2 meio = (a + b) * 0.5f;
        Vector2 eixo = b - a;
        bool deitado = Mathf.Abs(eixo.x) >= Mathf.Abs(eixo.y);
        Vector2 tamanho = deitado ? new Vector2(Mathf.Abs(eixo.x) + 0.6f, 0.16f) : new Vector2(0.16f, Mathf.Abs(eixo.y) + 0.6f);
        FormasDaSala.Desenho(sala.transform, "Trilho", Fosso.Pixel(), new Color(0.08f, 0.06f, 0.06f, 0.75f), meio, tamanho, 2);

        GameObject obj = new GameObject("Lamina giratoria");
        obj.transform.SetParent(sala.transform, false);
        obj.transform.position = centro + Vector2.Lerp(a, b, comecoDoCaminho);

        LaminaGiratoria lamina = obj.AddComponent<LaminaGiratoria>();
        lamina.de = centro + a;
        lamina.ate = centro + b;
        lamina.t = comecoDoCaminho;
        sala.AoLimpar.AddListener(lamina.Recolher);
        return lamina;
    }

    private void Awake()
    {
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        CircleCollider2D corpo = gameObject.AddComponent<CircleCollider2D>();
        corpo.isTrigger = true;
        corpo.radius = Raio * 0.85f;

        GameObject d = new GameObject("Serra");
        d.transform.SetParent(transform, false);
        d.transform.localScale = Vector3.one * Raio * 2f;
        SpriteRenderer sr = d.AddComponent<SpriteRenderer>();
        sr.sprite = Serra();
        sr.sortingOrder = 8;
        desenho = d.transform;
    }

    private void FixedUpdate()
    {
        desenho.Rotate(0f, 0f, -720f * Time.fixedDeltaTime);

        if (sumindo >= 0f)
        {
            sumindo += Time.fixedDeltaTime;
            desenho.localScale = Vector3.one * Raio * 2f * Mathf.Clamp01(1f - sumindo / 0.5f);

            if (sumindo >= 0.5f)
                Destroy(gameObject);

            return;
        }

        float comprimento = Mathf.Max(0.1f, Vector2.Distance(de, ate));
        t += sentido * velocidade / comprimento * Time.fixedDeltaTime;

        if (t >= 1f || t <= 0f)
        {
            t = Mathf.Clamp01(t);
            sentido = -sentido;
        }

        rb.MovePosition(Vector2.Lerp(de, ate, t));
    }

    private void Recolher() => sumindo = 0f;

    private void OnTriggerStay2D(Collider2D outro)
    {
        if (sumindo >= 0f)
            return;

        GameObject quem = outro.attachedRigidbody != null ? outro.attachedRigidbody.gameObject : outro.gameObject;

        if (!quem.CompareTag("Player") || !quem.TryGetComponent(out Vida vida))
            return;

        if (vida.EstaInvencivel)
            return;

        Vector2 direcao = (Vector2)quem.transform.position - rb.position;
        vida.TomarDano(new DanoInfo(dano, direcao, 5f, rb.position, gameObject));
        Sons.Tocar(Som.Corte, 0.8f);
    }

    /// <summary>Disco de metal com doze dentes e um furo no meio, gerado uma vez.</summary>
    private static Sprite Serra()
    {
        if (serra != null)
            return serra;

        const int lado = 32;
        Texture2D textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false)
        {
            name = "SerraGerada",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[lado * lado];
        float c = lado * 0.5f;

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float dx = x + 0.5f - c, dy = y + 0.5f - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Atan2(dy, dx);

                // Dente: o raio de fora sobe e desce em serra, doze vezes na volta.
                float dente = Mathf.Repeat(a / (Mathf.PI * 2f) * 12f, 1f);
                float raioDeFora = 11f + 4.5f * dente;
                Color32 cor = new Color32(0, 0, 0, 0);

                if (d <= 3f)
                    cor = new Color32(40, 35, 35, 255);                      // furo
                else if (d <= 10f)
                    cor = d > 8.5f ? new Color32(120, 120, 130, 255)         // aro
                                   : new Color32(185, 185, 195, 255);        // disco
                else if (d <= raioDeFora)
                    cor = new Color32(225, 225, 235, 255);                   // dentes

                pixels[y * lado + x] = cor;
            }
        }

        textura.SetPixels32(pixels);
        textura.Apply();
        serra = Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f), lado, 0, SpriteMeshType.FullRect);
        serra.name = "SerraGerada";
        return serra;
    }
}
