using UnityEngine;

/// <summary>
/// Inimigo de chão que persegue o jogador quando ele entra no raio de visão,
/// para na distância de ataque e bate em intervalos. Vida, empurrão e morte
/// ficam no componente Vida — este script só cuida do comportamento.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Vida))]
[DisallowMultipleComponent]
public class Inimigo : MonoBehaviour
{
    [Header("Alvo")]
    [Tooltip("O boneco. Vazio = procura pelo objeto com a tag abaixo")]
    [SerializeField] private Transform jogador;

    [SerializeField] private string tagDoJogador = "Player";

    [Header("Movimento")]
    [SerializeField, Min(0f)] private float velocidade = 2f;

    [Tooltip("Só começa a perseguir quando o jogador está a menos que isto")]
    [SerializeField, Min(0f)] private float raioDeVisao = 6f;

    [Tooltip("Distância horizontal em que ele para de andar e começa a bater")]
    [SerializeField, Min(0f)] private float distanciaDeAtaque = 1.2f;

    [Header("Borda")]
    [Tooltip("Se ligado, para antes de cair de uma plataforma (precisa do Checador De Borda)")]
    [SerializeField] private bool naoCairDaBorda = true;

    [Tooltip("Objeto vazio, filho, na frente e um pouco abaixo dos pés")]
    [SerializeField] private Transform checadorDeBorda;

    [Tooltip("A mesma camada de chão usada no Movimento do jogador")]
    [SerializeField] private LayerMask camadaChao;

    [SerializeField, Min(0.05f)] private float alcanceDoChecador = 0.5f;

    [Header("Ataque")]
    [SerializeField, Min(0f)] private float dano = 2f;
    [SerializeField, Min(0f)] private float forcaEmpurrao = 5f;
    [SerializeField, Min(0.05f)] private float intervaloEntreAtaques = 1f;

    [Header("Ao tomar dano")]
    [Tooltip("Tempo que fica parado depois de levar golpe — é o que deixa o empurrão acontecer")]
    [SerializeField, Min(0f)] private float tempoAtordoado = 0.25f;

    // ---------- estado interno ----------
    private Rigidbody2D rb;
    private Vida vida;
    private IDanificavel vidaDoJogador;

    private float direcao = 1f;          // +1 direita, -1 esquerda
    private float fimDoAtordoamento;
    private float proximoAtaquePermitido;
    private bool morto;

    // ---------- ciclo de vida ----------
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        vida = GetComponent<Vida>();
        rb.freezeRotation = true;

        if (jogador == null)
        {
            GameObject obj = GameObject.FindWithTag(tagDoJogador);
            if (obj != null)
                jogador = obj.transform;
            else
                Debug.LogWarning($"{name}: nenhum objeto com a tag '{tagDoJogador}'. Marque o boneco com essa tag.", this);
        }

        if (jogador != null)
            jogador.TryGetComponent(out vidaDoJogador);
    }

    private void OnEnable()
    {
        vida.AoTomarDano.AddListener(AoLevarGolpe);
        vida.AoMorrer.AddListener(Morrer);
    }

    private void OnDisable()
    {
        vida.AoTomarDano.RemoveListener(AoLevarGolpe);
        vida.AoMorrer.RemoveListener(Morrer);
    }

    private void FixedUpdate()
    {
        if (morto)
            return;

        // Atordoado: não mexe na velocidade, deixa a física aplicar o empurrão.
        if (Time.time < fimDoAtordoamento)
            return;

        if (jogador == null)
        {
            Parar();
            return;
        }

        float dx = jogador.position.x - transform.position.x;
        float distancia = Vector2.Distance(jogador.position, transform.position);

        if (distancia > raioDeVisao)
        {
            Parar();
            return;
        }

        Virar(Mathf.Sign(dx));

        if (Mathf.Abs(dx) <= distanciaDeAtaque)
        {
            Parar();
            TentarAtacar();
            return;
        }

        if (naoCairDaBorda && !TemChaoAFrente())
        {
            Parar();
            return;
        }

        rb.linearVelocity = new Vector2(direcao * velocidade, rb.linearVelocity.y);
    }

    // ---------- comportamento ----------
    private void Parar()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void Virar(float novaDirecao)
    {
        if (Mathf.Approximately(novaDirecao, direcao))
            return;

        direcao = novaDirecao;

        // Mantém o tamanho original do sprite, só troca o sinal do X.
        Vector3 escala = transform.localScale;
        escala.x = Mathf.Abs(escala.x) * direcao;
        transform.localScale = escala;
    }

    private bool TemChaoAFrente()
    {
        if (checadorDeBorda == null)
            return true;

        return Physics2D.Raycast(checadorDeBorda.position, Vector2.down, alcanceDoChecador, camadaChao);
    }

    private void TentarAtacar()
    {
        if (Time.time < proximoAtaquePermitido || vidaDoJogador == null)
            return;

        proximoAtaquePermitido = Time.time + intervaloEntreAtaques;

        Vector2 direcaoDoGolpe = ((Vector2)jogador.position - (Vector2)transform.position).normalized;
        vidaDoJogador.TomarDano(new DanoInfo(dano, direcaoDoGolpe, forcaEmpurrao, jogador.position, gameObject));
    }

    // ---------- reações ao Vida ----------
    private void AoLevarGolpe(DanoInfo info)
    {
        fimDoAtordoamento = Time.time + tempoAtordoado;
    }

    private void Morrer()
    {
        morto = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false; // some da física na hora; o Vida destrói o objeto

        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
            c.enabled = false;
    }

    // ---------- visualização no editor ----------
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, raioDeVisao);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + Vector3.left * distanciaDeAtaque,
                        transform.position + Vector3.right * distanciaDeAtaque);

        if (checadorDeBorda != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(checadorDeBorda.position, checadorDeBorda.position + Vector3.down * alcanceDoChecador);
        }
    }
#endif
}
