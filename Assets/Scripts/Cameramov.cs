using UnityEngine;

/// <summary>
/// Câmera 2D de plataforma. Segue o alvo com zona morta, suavização separada
/// por eixo, antecipação na direção da corrida e limites da fase.
/// Coloque este componente na Main Camera (ortográfica).
/// </summary>
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class Cameramov : MonoBehaviour
{
    [Header("Alvo")]
    [Tooltip("O personagem que a câmera vai seguir")]
    [SerializeField] private Transform alvo;

    [Tooltip("Rigidbody2D do alvo. Só é usado pra antecipação — pode ficar vazio")]
    [SerializeField] private Rigidbody2D alvoRb;

    [Tooltip("Deslocamento em relação ao alvo (ex: um pouco acima da cabeça)")]
    [SerializeField] private Vector2 deslocamento = new Vector2(0f, 1f);

    [Header("Suavização (segundos até alcançar o alvo)")]
    [Tooltip("Horizontal. Menor = mais rápida/seca")]
    [SerializeField, Min(0f)] private float tempoSuavizacaoX = 0.12f;

    [Tooltip("Vertical. Em plataforma costuma ser maior que o X pra câmera não acompanhar cada pulo")]
    [SerializeField, Min(0f)] private float tempoSuavizacaoY = 0.30f;

    [Header("Zona morta")]
    [Tooltip("Largura/altura (em unidades do mundo) que o alvo pode andar sem a câmera se mover. 0 = desliga")]
    [SerializeField] private Vector2 zonaMorta = new Vector2(1.5f, 1.0f);

    [Header("Antecipação (look-ahead)")]
    [Tooltip("Quanto a câmera desliza na direção da corrida. 0 = desliga")]
    [SerializeField, Min(0f)] private float antecipacaoX = 2f;

    [Tooltip("Velocidade horizontal mínima do alvo pra antecipação ligar (evita ativar em micro-movimentos)")]
    [SerializeField, Min(0f)] private float velocidadeMinimaAntecipacao = 0.5f;

    [Tooltip("Quão rápido a antecipação entra e sai. Maior = mais rápida")]
    [SerializeField, Min(0.01f)] private float velocidadeAntecipacao = 4f;

    [Header("Limites da fase")]
    [Tooltip("Se ligado, a borda da câmera nunca passa do retângulo abaixo")]
    [SerializeField] private bool usarLimites = false;

    [Tooltip("Canto inferior-esquerdo da fase (mundo)")]
    [SerializeField] private Vector2 limiteMinimo = new Vector2(-20f, -10f);

    [Tooltip("Canto superior-direito da fase (mundo)")]
    [SerializeField] private Vector2 limiteMaximo = new Vector2(20f, 10f);

    // ---------- estado interno ----------
    private Camera cam;

    // Ponto que a câmera "quer" enquadrar. Só muda quando o alvo sai da zona morta.
    private Vector2 pontoDeInteresse;

    // Velocidades que o SmoothDamp usa pra lembrar o movimento entre quadros.
    private float velocidadeX;
    private float velocidadeY;

    // Antecipação suavizada (vai de -antecipacaoX a +antecipacaoX).
    private float antecipacaoAtual;

    // ---------- ciclo de vida ----------
    private void Awake()
    {
        cam = GetComponent<Camera>();

        if (alvo != null && alvoRb == null)
            alvoRb = alvo.GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Começa exatamente em cima do alvo — sem "voo" no primeiro quadro.
        if (alvo != null)
            PosicionarImediatamente();
    }

    private void LateUpdate()
    {
        if (alvo == null)
            return;

        AtualizarPontoDeInteresse();
        AtualizarAntecipacao();

        Vector2 destino = pontoDeInteresse + new Vector2(antecipacaoAtual, 0f);

        if (usarLimites)
            destino = AplicarLimites(destino);

        Vector3 pos = transform.position;
        pos.x = Mathf.SmoothDamp(pos.x, destino.x, ref velocidadeX, tempoSuavizacaoX);
        pos.y = Mathf.SmoothDamp(pos.y, destino.y, ref velocidadeY, tempoSuavizacaoY);
        transform.position = pos;
    }

    // ---------- lógica ----------

    /// Move o ponto de interesse só quando o alvo sai da zona morta.
    private void AtualizarPontoDeInteresse()
    {
        Vector2 alvoPos = (Vector2)alvo.position + deslocamento;
        Vector2 diferenca = alvoPos - pontoDeInteresse;
        Vector2 metade = zonaMorta * 0.5f;

        if (Mathf.Abs(diferenca.x) > metade.x)
            pontoDeInteresse.x += diferenca.x - Mathf.Sign(diferenca.x) * metade.x;

        if (Mathf.Abs(diferenca.y) > metade.y)
            pontoDeInteresse.y += diferenca.y - Mathf.Sign(diferenca.y) * metade.y;
    }

    /// Desliza a câmera na direção em que o alvo está correndo.
    private void AtualizarAntecipacao()
    {
        float alvoAntecipacao = 0f;

        if (antecipacaoX > 0f && alvoRb != null)
        {
            float vx = alvoRb.linearVelocity.x;
            if (Mathf.Abs(vx) > velocidadeMinimaAntecipacao)
                alvoAntecipacao = Mathf.Sign(vx) * antecipacaoX;
        }

        // Interpolação exponencial: independente de framerate e nunca ultrapassa.
        float t = 1f - Mathf.Exp(-velocidadeAntecipacao * Time.deltaTime);
        antecipacaoAtual = Mathf.Lerp(antecipacaoAtual, alvoAntecipacao, t);
    }

    /// Garante que a borda da câmera não passe dos limites da fase.
    private Vector2 AplicarLimites(Vector2 destino)
    {
        float metadeAltura = cam.orthographicSize;
        float metadeLargura = metadeAltura * cam.aspect;

        float minX = limiteMinimo.x + metadeLargura;
        float maxX = limiteMaximo.x - metadeLargura;
        float minY = limiteMinimo.y + metadeAltura;
        float maxY = limiteMaximo.y - metadeAltura;

        // Se a fase é menor que a câmera num eixo, centraliza nesse eixo.
        destino.x = minX > maxX ? (minX + maxX) * 0.5f : Mathf.Clamp(destino.x, minX, maxX);
        destino.y = minY > maxY ? (minY + maxY) * 0.5f : Mathf.Clamp(destino.y, minY, maxY);

        return destino;
    }

    // ---------- API pública (pra outros scripts usarem) ----------

    /// Troca o alvo. Com imediato = true a câmera pula direto pra ele (útil em respawn/troca de sala).
    public void DefinirAlvo(Transform novoAlvo, bool imediato = false)
    {
        alvo = novoAlvo;
        alvoRb = alvo != null ? alvo.GetComponent<Rigidbody2D>() : null;

        if (imediato && alvo != null)
            PosicionarImediatamente();
    }

    /// Define os limites da fase a partir de um Bounds (ex: BoxCollider2D da sala).
    public void DefinirLimites(Bounds limites)
    {
        usarLimites = true;
        limiteMinimo = limites.min;
        limiteMaximo = limites.max;
    }

    /// Coloca a câmera exatamente no alvo, zerando toda a suavização.
    public void PosicionarImediatamente()
    {
        pontoDeInteresse = (Vector2)alvo.position + deslocamento;
        antecipacaoAtual = 0f;
        velocidadeX = 0f;
        velocidadeY = 0f;

        Vector2 destino = usarLimites ? AplicarLimites(pontoDeInteresse) : pontoDeInteresse;
        transform.position = new Vector3(destino.x, destino.y, transform.position.z);
    }

    // ---------- visualização no editor ----------
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Zona morta (amarelo) em volta do ponto de interesse.
        Vector2 centro = Application.isPlaying
            ? pontoDeInteresse
            : (alvo != null ? (Vector2)alvo.position + deslocamento : (Vector2)transform.position);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(centro, new Vector3(zonaMorta.x, zonaMorta.y, 0f));

        // Limites da fase (ciano).
        if (usarLimites)
        {
            Gizmos.color = Color.cyan;
            Vector2 tamanho = limiteMaximo - limiteMinimo;
            Gizmos.DrawWireCube((limiteMinimo + limiteMaximo) * 0.5f, new Vector3(tamanho.x, tamanho.y, 0f));
        }
    }
#endif
}
