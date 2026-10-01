using UnityEngine;

/// <summary>
/// A armadilha do tronco com estacas do Old Prison: um tronco rolando de uma parede a outra da
/// sala, para um instante encostado e volta. Deitado ele atravessa a sala de cima a baixo; em pe,
/// de um lado ao outro. Machuca o heroi (meio coracao) e passa por cima de pedra e buraco; bicho
/// nao liga pra ele, como a <see cref="LaminaGiratoria"/>. Uma alavanca de engrenagem na parede
/// mostra quem move a maquina. Quando a sala e limpa, o tronco para e some.
/// </summary>
[DisallowMultipleComponent]
public class TroncoRolante : MonoBehaviour
{
    /// <summary>Pixels por unidade do tronco: ~3.2 de comprimento.</summary>
    private const float PixelsDoTronco = 40f;

    [SerializeField, Min(0.1f)] private float velocidade = 2.4f;
    [SerializeField, Min(0f)] private float parada = 0.7f;
    [SerializeField, Min(0f)] private float dano = 10f;

    private Rigidbody2D rb;
    private SpriteRenderer desenho;
    private Sprite[] quadros;
    private Vector2 de;
    private Vector2 ate;
    private float t;
    private float sentido = 1f;
    private float paradoAte;
    private float giro;
    private float sumindo = -1f;

    /// <summary>
    /// Um tronco na faixa <paramref name="faixa"/> (x do tronco deitado ou y do em pe, relativo
    /// ao centro da sala), rolando entre as paredes.
    /// </summary>
    public static TroncoRolante Criar(Sala sala, bool deitado, float faixa)
    {
        Sprite[] quadros = ArteImportada.TroncoRolando(deitado, PixelsDoTronco);

        if (quadros == null)
            return null;

        Vector2 centro = sala.transform.position;
        Vector2 meio = sala.TamanhoInterno * 0.5f;

        // Deitado: rola no eixo y, encostando meio tronco (0.6) em cada parede. Em pe: no eixo x.
        Vector2 a = deitado ? new Vector2(faixa, -meio.y + 0.6f) : new Vector2(-meio.x + 0.6f, faixa);
        Vector2 b = deitado ? new Vector2(faixa, meio.y - 0.6f) : new Vector2(meio.x - 0.6f, faixa);

        GameObject obj = new GameObject("Tronco rolante");
        obj.transform.SetParent(sala.transform, false);
        obj.transform.position = centro + a;

        TroncoRolante tronco = obj.AddComponent<TroncoRolante>();
        tronco.quadros = quadros;
        tronco.de = centro + a;
        tronco.ate = centro + b;
        tronco.paradoAte = Time.time + 1.2f;   // da tempo de entrar na sala

        tronco.rb = obj.AddComponent<Rigidbody2D>();
        tronco.rb.bodyType = RigidbodyType2D.Kinematic;
        tronco.rb.gravityScale = 0f;

        BoxCollider2D corpo = obj.AddComponent<BoxCollider2D>();
        corpo.isTrigger = true;
        corpo.size = deitado ? new Vector2(3f, 0.6f) : new Vector2(0.6f, 3f);

        tronco.desenho = obj.AddComponent<SpriteRenderer>();
        tronco.desenho.sprite = quadros[0];
        tronco.desenho.sortingOrder = 7;

        PorAlavanca(sala, deitado, faixa);
        sala.AoLimpar.AddListener(tronco.Recolher);
        return tronco;
    }

    /// <summary>A alavanca de engrenagem girando na parede de cima, perto de onde o tronco passa.</summary>
    private static void PorAlavanca(Sala sala, bool deitado, float faixa)
    {
        Sprite[] alavanca = ArteImportada.Alavanca;
        Sprite[] engrenagem = ArteImportada.Engrenagem;
        Vector2 centro = sala.transform.position;
        Vector2 meio = sala.TamanhoInterno * 0.5f;

        // Fora do vao da porta de cima (x de -1 a 1).
        float x = deitado ? faixa + (faixa >= 0f ? 1.3f : -1.3f) : (Random.value < 0.5f ? -2.2f : 2.2f);
        x = Mathf.Clamp(x, -meio.x + 0.8f, meio.x - 0.8f);

        if (Mathf.Abs(x) < 1.3f)
            x = Mathf.Sign(x == 0f ? 1f : x) * 1.3f;

        Vector2 ponto = centro + new Vector2(x, meio.y - 0.1f);
        EfeitoDeQuadros.Criar(alavanca, 8f, ponto, 1, sala.transform)?.EmLoop();
        EfeitoDeQuadros.Criar(engrenagem, 8f, ponto + new Vector2(0.75f, 0.35f), 1, sala.transform)?.EmLoop();
    }

    private void FixedUpdate()
    {
        if (sumindo >= 0f)
        {
            sumindo += Time.fixedDeltaTime;
            desenho.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - sumindo / 0.5f));

            if (sumindo >= 0.5f)
                Destroy(gameObject);

            return;
        }

        if (Time.time < paradoAte)
            return;

        float comprimento = Mathf.Max(0.1f, Vector2.Distance(de, ate));
        t += sentido * velocidade / comprimento * Time.fixedDeltaTime;

        if (t >= 1f || t <= 0f)
        {
            t = Mathf.Clamp01(t);
            sentido = -sentido;
            paradoAte = Time.time + parada;
            Sons.Tocar(Som.Pancada, 0.5f);
        }

        // Os quadros giram no sentido em que ele rola (voltando, de tras pra frente).
        giro += sentido * velocidade * Time.fixedDeltaTime * 6f;
        int quadro = Mathf.FloorToInt(Mathf.Repeat(giro, quadros.Length));
        desenho.sprite = quadros[quadro];

        rb.MovePosition(Vector2.Lerp(de, ate, t));
    }

    private void Recolher() => sumindo = 0f;

    private void OnTriggerStay2D(Collider2D outro)
    {
        if (sumindo >= 0f)
            return;

        GameObject quem = outro.attachedRigidbody != null ? outro.attachedRigidbody.gameObject : outro.gameObject;

        if (!quem.CompareTag("Player") || !quem.TryGetComponent(out Vida vida) || vida.EstaInvencivel)
            return;

        Vector2 direcao = (Vector2)quem.transform.position - rb.position;
        vida.TomarDano(new DanoInfo(dano, direcao, 6f, rb.position, gameObject));
        Sons.Tocar(Som.Pancada, 0.9f);
    }
}
