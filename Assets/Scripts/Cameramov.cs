using UnityEngine;

/// <summary>
/// Camera 2D de plataforma: segue o alvo com zona morta, suavizacao separada por eixo,
/// antecipacao na direcao da corrida, limites da fase e tremida no impacto.
///
/// A zona morta e o detalhe que mais muda a sensacao do jogo: sem ela a camera reage a
/// cada pulinho e da embrulho no estomago. Com ela, andar pouco nao mexe nada.
///
/// Coloque na Main Camera (ortografica).
/// </summary>
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class Cameramov : MonoBehaviour
{
    [Header("Alvo")]
    [Tooltip("Quem a camera segue. Vazio = acha o jogador sozinho")]
    [SerializeField] private Transform alvo;

    [Tooltip("Rigidbody2D do alvo. Usado so pra antecipacao — pode ficar vazio")]
    [SerializeField] private Rigidbody2D alvoRb;

    [Tooltip("Deslocamento em relacao ao alvo (ex.: um pouco acima da cabeca)")]
    [SerializeField] private Vector2 deslocamento = new Vector2(0f, 0.35f);

    [Header("Suavizacao (segundos ate alcancar o alvo)")]
    [Tooltip("Horizontal. Menor = mais seca")]
    [SerializeField, Min(0f)] private float tempoSuavizacaoX = 0.12f;

    [Tooltip("Vertical. Em plataforma costuma ser maior que o X, pra nao acompanhar cada pulo")]
    [SerializeField, Min(0f)] private float tempoSuavizacaoY = 0.24f;

    [Header("Zona morta")]
    [Tooltip("Quanto o alvo pode andar sem a camera se mover (unidades do mundo). 0 = desliga")]
    [SerializeField] private Vector2 zonaMorta = new Vector2(0.45f, 0.5f);

    [Header("Antecipacao (look-ahead)")]
    [Tooltip("Quanto a camera desliza na direcao da corrida. 0 = desliga")]
    [SerializeField, Min(0f)] private float antecipacaoX = 0.7f;

    [Tooltip("Velocidade horizontal minima do alvo pra antecipacao ligar")]
    [SerializeField, Min(0f)] private float velocidadeMinimaAntecipacao = 0.6f;

    [Tooltip("Quao rapido a antecipacao entra e sai")]
    [SerializeField, Min(0.01f)] private float velocidadeAntecipacao = 4f;

    [Header("Limites da fase")]
    [Tooltip("Se ligado, a borda da camera nunca passa do retangulo abaixo")]
    [SerializeField] private bool usarLimites = false;

    [SerializeField] private Vector2 limiteMinimo = new Vector2(-20f, -10f);

    [SerializeField] private Vector2 limiteMaximo = new Vector2(20f, 10f);

    [Header("Tremida")]
    [Tooltip("Quanto tempo uma tremida leva pra sumir")]
    [SerializeField, Min(0.01f)] private float duracaoDaTremida = 0.18f;

    [Tooltip("Tremida maxima em unidades do mundo (segura o exagero)")]
    [SerializeField, Min(0f)] private float tremidaMaxima = 0.25f;

    // ---------------- estado ----------------
    private static Cameramov instancia;

    private Camera cam;

    // Ponto que a camera "quer" enquadrar. So muda quando o alvo sai da zona morta.
    private Vector2 pontoDeInteresse;

    private float velocidadeX;
    private float velocidadeY;
    private float antecipacaoAtual;

    private float tremidaForca;
    private float tremidaRestante;

    /// <summary>
    /// A Camera deste objeto, resolvida sob demanda. Nao fica so no Awake porque
    /// ferramentas de editor chamam DefinirLimites/PosicionarImediatamente fora do Play,
    /// onde o Awake nunca rodou — e AplicarLimites precisa da camera pra saber o
    /// tamanho do enquadramento.
    /// </summary>
    private Camera Cam
    {
        get
        {
            if (cam == null)
                cam = GetComponent<Camera>();

            return cam;
        }
    }

    // ---------------- ciclo de vida ----------------
    private void Awake()
    {
        cam = GetComponent<Camera>();
        instancia = this;
    }

    private void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }

    private void Start()
    {
        GarantirAlvo();

        if (alvo != null)
            PosicionarImediatamente();
    }

    private void LateUpdate()
    {
        if (alvo == null)
        {
            GarantirAlvo();

            if (alvo == null)
                return;
        }

        AtualizarPontoDeInteresse();
        AtualizarAntecipacao();

        Vector2 destino = pontoDeInteresse + new Vector2(antecipacaoAtual, 0f);

        if (usarLimites)
            destino = AplicarLimites(destino);

        Vector3 pos = transform.position;
        pos.x = Mathf.SmoothDamp(pos.x, destino.x, ref velocidadeX, tempoSuavizacaoX);
        pos.y = Mathf.SmoothDamp(pos.y, destino.y, ref velocidadeY, tempoSuavizacaoY);

        pos += (Vector3)CalcularTremida();

        transform.position = pos;
    }

    private void GarantirAlvo()
    {
        if (alvo == null && Player.Atual != null)
            alvo = Player.Atual.transform;

        if (alvo != null && alvoRb == null)
            alvoRb = alvo.GetComponent<Rigidbody2D>();
    }

    // ---------------- logica ----------------
    /// <summary>Move o ponto de interesse so quando o alvo sai da zona morta.</summary>
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

    /// <summary>Desliza a camera na direcao em que o alvo esta correndo.</summary>
    private void AtualizarAntecipacao()
    {
        float desejada = 0f;

        if (antecipacaoX > 0f && alvoRb != null)
        {
            float vx = alvoRb.linearVelocity.x;

            if (Mathf.Abs(vx) > velocidadeMinimaAntecipacao)
                desejada = Mathf.Sign(vx) * antecipacaoX;
        }

        // Interpolacao exponencial: independente de framerate e nunca ultrapassa.
        float t = 1f - Mathf.Exp(-velocidadeAntecipacao * Time.deltaTime);
        antecipacaoAtual = Mathf.Lerp(antecipacaoAtual, desejada, t);
    }

    private Vector2 CalcularTremida()
    {
        if (tremidaRestante <= 0f)
            return Vector2.zero;

        tremidaRestante -= Time.deltaTime;

        if (tremidaRestante <= 0f)
            return Vector2.zero;

        float forca = tremidaForca * (tremidaRestante / duracaoDaTremida);
        return Random.insideUnitCircle * forca;
    }

    /// <summary>Garante que a borda da camera nao passe dos limites da fase.</summary>
    private Vector2 AplicarLimites(Vector2 destino)
    {
        if (Cam == null)
            return destino;

        float metadeAltura = Cam.orthographicSize;
        float metadeLargura = metadeAltura * Cam.aspect;

        float minX = limiteMinimo.x + metadeLargura;
        float maxX = limiteMaximo.x - metadeLargura;
        float minY = limiteMinimo.y + metadeAltura;
        float maxY = limiteMaximo.y - metadeAltura;

        // Fase menor que a camera num eixo: centraliza nesse eixo.
        destino.x = minX > maxX ? (minX + maxX) * 0.5f : Mathf.Clamp(destino.x, minX, maxX);
        destino.y = minY > maxY ? (minY + maxY) * 0.5f : Mathf.Clamp(destino.y, minY, maxY);

        return destino;
    }

    // ---------------- API publica ----------------
    /// <summary>Troca o alvo. Com imediato = true a camera pula direto pra ele.</summary>
    public void DefinirAlvo(Transform novoAlvo, bool imediato = false)
    {
        alvo = novoAlvo;
        alvoRb = alvo != null ? alvo.GetComponent<Rigidbody2D>() : null;

        if (imediato && alvo != null)
            PosicionarImediatamente();
    }

    /// <summary>Define os limites da fase a partir de um Bounds (ex.: o retangulo da sala).</summary>
    public void DefinirLimites(Bounds limites)
    {
        usarLimites = true;
        limiteMinimo = limites.min;
        limiteMaximo = limites.max;
    }

    /// <summary>Coloca a camera exatamente no alvo, zerando toda a suavizacao.</summary>
    public void PosicionarImediatamente()
    {
        if (alvo == null)
            return;

        pontoDeInteresse = (Vector2)alvo.position + deslocamento;
        antecipacaoAtual = 0f;
        velocidadeX = 0f;
        velocidadeY = 0f;

        Vector2 destino = usarLimites ? AplicarLimites(pontoDeInteresse) : pontoDeInteresse;
        transform.position = new Vector3(destino.x, destino.y, transform.position.z);
    }

    /// <summary>Sacode a camera. Chamado pela hitbox ao acertar, por explosao, por queda.</summary>
    public void Sacudir(float forca)
    {
        tremidaForca = Mathf.Min(Mathf.Max(tremidaForca, forca), tremidaMaxima);
        tremidaRestante = duracaoDaTremida;
    }

    /// <summary>
    /// Atalho estatico pra quem nao tem referencia da camera (a hitbox, por exemplo).
    /// Nao reclama se nao houver camera na cena — feedback visual nunca deve quebrar o jogo.
    /// </summary>
    public static void Tremer(float forca)
    {
        if (instancia != null)
            instancia.Sacudir(forca);
    }

    // ---------------- editor ----------------
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector2 centro = Application.isPlaying
            ? pontoDeInteresse
            : (alvo != null ? (Vector2)alvo.position + deslocamento : (Vector2)transform.position);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(centro, new Vector3(zonaMorta.x, zonaMorta.y, 0f));

        if (usarLimites)
        {
            Gizmos.color = Color.cyan;
            Vector2 tamanho = limiteMaximo - limiteMinimo;
            Gizmos.DrawWireCube((limiteMinimo + limiteMaximo) * 0.5f, new Vector3(tamanho.x, tamanho.y, 0f));
        }
    }
#endif
}
