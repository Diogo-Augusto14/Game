using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Vida de qualquer coisa que toma dano: jogador, inimigo, chefe, vaso.
///
/// O que faz: desconta a defesa, respeita a invencibilidade, aplica o empurrao, pisca o
/// sprite e avisa quem quiser ouvir. O que NAO faz: animar. Animacao e decidida por quem
/// desenha, que le <see cref="UltimoGolpe"/> e
/// <see cref="EstaMorto"/>. Misturar as duas coisas e o que faz um sistema de dano ficar
/// impossivel de reusar em outro bicho.
///
/// Se o objeto tiver um controlador de movimento (via IControladorDeMovimento), o Vida
/// conversa com ele: respeita a invencibilidade do dash e manda o empurrao por lá, pra o
/// controlador travar o proprio andar. Se nao tiver, empurra o Rigidbody2D direto.
/// </summary>
[DisallowMultipleComponent]
public class Vida : MonoBehaviour, IDanificavel
{
    [Header("Vida")]
    [SerializeField, Min(1f)] private float vidaMaxima = 100f;

    [Tooltip("Descontado de cada golpe. Todo golpe tira pelo menos 1, mesmo com defesa alta")]
    [SerializeField, Min(0f)] private float defesa = 0f;

    [Tooltip("Depois de tomar dano, ignora novos golpes por este tempo")]
    [SerializeField, Min(0f)] private float tempoInvencivel = 0.35f;

    [Header("Empurrao")]
    [Tooltip("Se ligado, o golpe empurra este objeto (precisa de Rigidbody2D Dynamic)")]
    [SerializeField] private bool recebeEmpurrao = true;

    [Tooltip("Top-down: empurra na direcao exata do golpe, sem o 'pra cima' do plataforma")]
    [SerializeField] private bool empurraoNaDirecaoDoGolpe = false;

    [Tooltip("Quanto do empurrao vai pra cima (0 = so pro lado, 1 = 45 graus)")]
    [SerializeField, Range(0f, 1f)] private float componenteVertical = 0.35f;

    [Tooltip("Segundos sem controle depois de um golpe leve")]
    [SerializeField, Min(0f)] private float travaGolpeLeve = 0.18f;

    [Tooltip("Segundos sem controle depois de um golpe forte")]
    [SerializeField, Min(0f)] private float travaGolpeForte = 0.45f;

    [Tooltip("Golpe que tira mais que isto (em fracao da vida maxima) conta como FORTE")]
    [SerializeField, Range(0f, 1f)] private float fracaoParaGolpeForte = 0.2f;

    [Header("Feedback")]
    [Tooltip("Sprite que pisca ao tomar dano. Vazio = procura nos filhos")]
    [SerializeField] private SpriteRenderer sprite;

    [SerializeField] private Color corDoFlash = new Color(1f, 0.35f, 0.35f);

    [SerializeField, Min(0f)] private float duracaoDoFlash = 0.09f;

    [Tooltip("Pisca durante toda a invencibilidade (bom no jogador)")]
    [SerializeField] private bool piscarNaInvencibilidade = false;

    [Header("Morte")]
    [Tooltip("Destroi o objeto ao morrer. DESLIGUE no jogador e em tudo que renasce")]
    [SerializeField] private bool destruirAoMorrer = true;

    [Tooltip("Segundos ate destruir — de tempo pra animacao de morte terminar")]
    [SerializeField, Min(0f)] private float atrasoParaDestruir = 1.2f;

    [Header("Eventos")]
    [Tooltip("Disparado a cada golpe recebido. Use pra som, particula, tremer camera...")]
    public UnityEvent<DanoInfo> AoTomarDano = new UnityEvent<DanoInfo>();

    [Tooltip("Disparado uma vez, quando a vida chega a zero")]
    public UnityEvent AoMorrer = new UnityEvent();

    [Tooltip("Disparado sempre que a vida muda — bom pra barra de vida")]
    public UnityEvent AoMudarVida = new UnityEvent();

    // ---------------- estado ----------------
    private Rigidbody2D rb;
    private IControladorDeMovimento controlador;
    private Color corOriginal;
    private Coroutine rotinaFlash;
    private float fimDaInvencibilidade;

    public float VidaAtual { get; private set; }

    public float VidaMaxima => vidaMaxima;

    public float Defesa => defesa;

    public bool EstaMorto => VidaAtual <= 0f;

    /// <summary>O ultimo golpe recebido. Quem anima le o Peso daqui.</summary>
    public DanoInfo UltimoGolpe { get; private set; }

    /// <summary>Time.time do ultimo golpe. -1 se nunca levou.</summary>
    public float MomentoDoUltimoGolpe { get; private set; } = -1f;

    /// <summary>Fracao de 0 a 1 — pronta pra uma Image em Filled.</summary>
    public float Fracao => vidaMaxima <= 0f ? 0f : VidaAtual / vidaMaxima;

    /// <summary>Invencivel por tempo (pos-golpe) ou porque o controlador mandou (dash).</summary>
    public bool EstaInvencivel => Time.time < fimDaInvencibilidade
                               || (controlador != null && controlador.IgnorandoDano);

    // ---------------- ciclo de vida ----------------
    private void Awake()
    {
        VidaAtual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();

        // Busca UMA vez. Se este objeto tiver Movimento.cs, ele aparece aqui.
        TryGetComponent(out controlador);

        if (sprite == null)
            sprite = GetComponentInChildren<SpriteRenderer>();

        if (sprite != null)
            corOriginal = sprite.color;
    }

    private void Update()
    {
        if (!piscarNaInvencibilidade || sprite == null || EstaMorto)
            return;

        // Pisca-pisca enquanto invencivel: o jogador VE que esta protegido.
        bool invencivelPorTempo = Time.time < fimDaInvencibilidade;
        float alfa = invencivelPorTempo && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f ? 0.35f : 1f;

        Color cor = sprite.color;

        if (!Mathf.Approximately(cor.a, alfa))
        {
            cor.a = alfa;
            sprite.color = cor;
        }
    }

    // ---------------- IDanificavel ----------------
    public void TomarDano(DanoInfo info)
    {
        if (EstaMorto)
            return;

        if (EstaInvencivel && !info.IgnoraInvencibilidade)
            return;

        float danoReal = Mathf.Max(1f, info.Quantidade - defesa);

        VidaAtual = Mathf.Max(0f, VidaAtual - danoReal);
        fimDaInvencibilidade = Time.time + tempoInvencivel;

        // Reclassifica o peso: um golpe fraquinho de um inimigo forte nao deve jogar o
        // boneco do outro lado da tela, e um golpe que tira 1/3 da vida tem que doer.
        PesoDoGolpe peso = danoReal >= vidaMaxima * fracaoParaGolpeForte
            ? PesoDoGolpe.Forte
            : info.Peso;

        UltimoGolpe = new DanoInfo(
            danoReal, info.Direcao, info.ForcaEmpurrao, info.PontoDeImpacto,
            info.Atacante, peso, info.IgnoraInvencibilidade);

        MomentoDoUltimoGolpe = Time.time;

        AplicarEmpurrao(UltimoGolpe);
        Piscar();

        AoTomarDano?.Invoke(UltimoGolpe);
        AoMudarVida?.Invoke();

        if (EstaMorto)
            Morrer();
    }

    /// <summary>Recupera vida (frasco, checkpoint). Nao passa do maximo.</summary>
    public void Curar(float quantidade)
    {
        if (EstaMorto)
            return;

        VidaAtual = Mathf.Min(vidaMaxima, VidaAtual + Mathf.Max(0f, quantidade));
        AoMudarVida?.Invoke();
    }

    /// <summary>Aumenta (ou diminui) a vida maxima e cura o tanto que aumentou (itens de coracao).</summary>
    public void AumentarVidaMaxima(float quanto)
    {
        vidaMaxima = Mathf.Max(1f, vidaMaxima + quanto);
        VidaAtual = Mathf.Clamp(VidaAtual + Mathf.Max(0f, quanto), 0f, vidaMaxima);
        AoMudarVida?.Invoke();
    }

    /// <summary>Volta com a vida cheia e um instante de invencibilidade. Usado no renascimento.</summary>
    public void Reviver(float invencibilidadeInicial = 1f)
    {
        VidaAtual = vidaMaxima;
        fimDaInvencibilidade = Time.time + invencibilidadeInicial;
        MomentoDoUltimoGolpe = -1f;

        RestaurarCor();
        AoMudarVida?.Invoke();
    }

    /// <summary>Mata na hora, sem empurrao (buraco, lava, roteiro).</summary>
    public void MatarAgora()
    {
        if (EstaMorto)
            return;

        VidaAtual = 0f;
        AoMudarVida?.Invoke();
        Morrer();
    }

    /// <summary>Segundos de trava de controle que este golpe merece.</summary>
    public float TravaDe(PesoDoGolpe peso) => peso == PesoDoGolpe.Forte ? travaGolpeForte : travaGolpeLeve;

    /// <summary>
    /// Ajusta os numeros de fora. Existe porque um componente adicionado por codigo pega
    /// os valores padrao do arquivo, e o padrao nao pode servir pros dois casos ao mesmo
    /// tempo: o inimigo precisa ser destruido ao morrer, o jogador NAO (ele renasce).
    /// </summary>
    public void Configurar(float maxima, float novaDefesa, bool destruir, float atrasoDaDestruicao, bool piscar)
    {
        vidaMaxima = Mathf.Max(1f, maxima);
        defesa = Mathf.Max(0f, novaDefesa);
        destruirAoMorrer = destruir;
        atrasoParaDestruir = Mathf.Max(0f, atrasoDaDestruicao);
        piscarNaInvencibilidade = piscar;

        VidaAtual = vidaMaxima;
        AoMudarVida?.Invoke();
    }

    /// <summary>Segundos de invencibilidade depois de cada golpe. 0 = toma todos (inimigo do Isaac).</summary>
    public void DefinirInvencibilidade(float segundos)
    {
        tempoInvencivel = Mathf.Max(0f, segundos);
    }

    /// <summary>Liga o empurrao na direcao do golpe (jogo top-down).</summary>
    public void UsarEmpurraoTopDown(bool ligado = true)
    {
        empurraoNaDirecaoDoGolpe = ligado;
    }

    // ---------------- interno ----------------
    private void AplicarEmpurrao(DanoInfo info)
    {
        if (!recebeEmpurrao || info.ForcaEmpurrao <= 0f)
            return;

        // Plataforma: so o lado importa (esquerda/direita) + um pouco pra cima.
        // Top-down: nao existe "cima" fisico, entao vai na direcao do golpe.
        Vector2 lado = empurraoNaDirecaoDoGolpe
            ? info.Direcao
            : new Vector2(info.LadoDoEmpurrao, componenteVertical).normalized;
        Vector2 impulso = lado * info.ForcaEmpurrao;
        float trava = TravaDe(info.Peso);

        // Tem controlador de movimento? Ele que aplica — e trava o proprio andar, senao o
        // impulso seria desfeito no FixedUpdate seguinte.
        if (controlador != null)
        {
            controlador.AplicarImpulsoExterno(impulso, trava);
            return;
        }

        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic)
            return;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(impulso, ForceMode2D.Impulse);
    }

    private void Piscar()
    {
        if (sprite == null || duracaoDoFlash <= 0f)
            return;

        if (rotinaFlash != null)
            StopCoroutine(rotinaFlash);

        rotinaFlash = StartCoroutine(RotinaFlash());
    }

    private IEnumerator RotinaFlash()
    {
        sprite.color = new Color(corDoFlash.r, corDoFlash.g, corDoFlash.b, sprite.color.a);
        yield return new WaitForSeconds(duracaoDoFlash);
        RestaurarCor();
        rotinaFlash = null;
    }

    private void RestaurarCor()
    {
        if (sprite != null)
            sprite.color = corOriginal;
    }

    private void Morrer()
    {
        AoMorrer?.Invoke();

        if (destruirAoMorrer)
            Destroy(gameObject, atrasoParaDestruir);
    }

    private void OnDisable()
    {
        // Nao deixa o sprite preso na cor do flash se o objeto for desativado no meio.
        if (rotinaFlash != null)
        {
            StopCoroutine(rotinaFlash);
            rotinaFlash = null;
        }

        RestaurarCor();
    }
}
