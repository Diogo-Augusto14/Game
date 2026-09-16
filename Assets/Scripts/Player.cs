using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// O "cerebro" do boneco. Nao cuida de vida (Vida), de andar (Movimento), de bater
/// (Ataque) nem de curar (Cura): coordena as pecas e reage ao que acontece com ele —
/// levou golpe, caiu no buraco, morreu, renasceu.
///
/// Coloque no objeto raiz do boneco.
/// </summary>
[RequireComponent(typeof(Vida))]
[DisallowMultipleComponent]
public class Player : MonoBehaviour
{
    [Header("Referencias (vazio = procura no proprio objeto)")]
    [SerializeField] private Movimento movimento;
    [SerializeField] private Ataque ataque;
    [SerializeField] private Cura cura;
    [SerializeField] private Entrada entrada;
    [SerializeField] private Rigidbody2D rb;

    [Header("Morte e renascimento")]
    [Tooltip("Tempo parado antes de renascer. Deixe igual a duracao da animacao de morte")]
    [SerializeField, Min(0f)] private float tempoAteRenascer = 1.6f;

    [Tooltip("Onde renasce. Vazio = onde o boneco estava quando a fase comecou")]
    [SerializeField] private Transform pontoDeRenascimento;

    [Tooltip("Invencibilidade logo depois de renascer, pra nao morrer de novo na hora")]
    [SerializeField, Min(0f)] private float invencibilidadeAoRenascer = 1.2f;

    [Tooltip("Renascer devolve todos os frascos de cura")]
    [SerializeField] private bool recarregarFrascosAoRenascer = true;

    [Header("Buraco (cair pra fora da fase)")]
    [Tooltip("Caiu abaixo deste Y? morre. Sem isso o boneco cai pra sempre e o jogo trava sem travar")]
    [SerializeField] private float alturaDaMorte = -20f;

    [Tooltip("Dano da queda no buraco em fracao da vida maxima (1 = morte na hora)")]
    [SerializeField, Range(0f, 1f)] private float danoDoBuraco = 1f;

    [Header("Eventos")]
    [Tooltip("Disparado quando o boneco morre. Use pra UI, som, fade...")]
    public UnityEvent AoMorrer = new UnityEvent();

    [Tooltip("Disparado quando o boneco volta. Use pra tirar o fade, resetar UI...")]
    public UnityEvent AoRenascer = new UnityEvent();

    // ---------------- estado ----------------
    private Vida vida;
    private Cameramov camera2D;
    private Vector3 posicaoInicial;
    private Coroutine rotinaDeRenascimento;

    /// <summary>O jogador da cena. Preenchido no Awake — inimigos e UI acham por aqui.</summary>
    public static Player Atual { get; private set; }

    public bool EstaMorto => vida != null && vida.EstaMorto;

    /// <summary>A vida do boneco. Nome "Saude" pra nao colidir com o tipo Vida.</summary>
    public Vida Saude => vida;

    public Cura Frascos => cura;

    // ---------------- ciclo de vida ----------------
    private void Reset()
    {
        movimento = GetComponent<Movimento>();
        ataque = GetComponent<Ataque>();
        cura = GetComponent<Cura>();
        entrada = GetComponent<Entrada>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        vida = GetComponent<Vida>();

        if (movimento == null) movimento = GetComponent<Movimento>();
        if (ataque == null) ataque = GetComponent<Ataque>();
        if (cura == null) cura = GetComponent<Cura>();
        if (entrada == null) entrada = GetComponent<Entrada>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        posicaoInicial = transform.position;
        Atual = this;
    }

    private void OnDestroy()
    {
        if (Atual == this)
            Atual = null;
    }

    private void Start()
    {
        camera2D = Camera.main != null ? Camera.main.GetComponent<Cameramov>() : null;

        if (camera2D == null)
            camera2D = FindAnyObjectByType<Cameramov>();
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

    private void Update()
    {
        if (!EstaMorto && transform.position.y < alturaDaMorte)
            CairNoBuraco();
    }

    // ---------------- reacoes ----------------
    private void AoLevarGolpe(DanoInfo info)
    {
        // Levou porrada no meio do golpe? O golpe e cortado na hora.
        // (a Cura ja se cancela sozinha, ela tambem escuta o Vida)
        if (ataque != null)
            ataque.Cancelar();
    }

    private void CairNoBuraco()
    {
        if (danoDoBuraco >= 1f)
        {
            vida.MatarAgora();
            return;
        }

        // Nao matou: tira um pedaco da vida e devolve pro checkpoint.
        vida.TomarDano(DanoInfo.DoCenario(vida.VidaMaxima * danoDoBuraco, transform.position, PesoDoGolpe.Forte));

        if (!EstaMorto)
            Teleportar(DestinoDoRenascimento());
    }

    private void Morrer()
    {
        if (movimento != null)
            movimento.DefinirMorto(true);

        TravarAcoes(true);

        AoMorrer?.Invoke();

        if (rotinaDeRenascimento != null)
            StopCoroutine(rotinaDeRenascimento);

        rotinaDeRenascimento = StartCoroutine(RotinaRenascer());
    }

    private IEnumerator RotinaRenascer()
    {
        yield return new WaitForSeconds(tempoAteRenascer);

        Teleportar(DestinoDoRenascimento());

        vida.Reviver(invencibilidadeAoRenascer);

        if (recarregarFrascosAoRenascer && cura != null)
            cura.Recarregar();

        if (movimento != null)
            movimento.DefinirMorto(false);

        TravarAcoes(false);

        // Camera pula direto pro ponto novo em vez de voar pela fase.
        if (camera2D != null)
            camera2D.PosicionarImediatamente();

        rotinaDeRenascimento = null;
        AoRenascer?.Invoke();
    }

    private Vector3 DestinoDoRenascimento()
    {
        return pontoDeRenascimento != null ? pontoDeRenascimento.position : posicaoInicial;
    }

    /// <summary>
    /// Move o boneco de verdade: com Rigidbody2D em Interpolate, mexer so no transform
    /// deixa um rastro entre a posicao velha e a nova. rb.position vai junto.
    /// </summary>
    private void Teleportar(Vector3 destino)
    {
        transform.position = destino;

        if (rb != null)
        {
            rb.position = destino;
            rb.linearVelocity = Vector2.zero;
        }

        if (movimento != null)
            movimento.Parar();
    }

    /// <summary>
    /// Trava as acoes sem desligar o Movimento: desligar o componente tiraria a gravidade
    /// e o boneco morto ficaria parado no ar. O estado Morto do Movimento ja impede andar.
    /// </summary>
    private void TravarAcoes(bool travar)
    {
        if (ataque != null) ataque.enabled = !travar;
        if (cura != null) cura.enabled = !travar;

        if (entrada != null && travar)
            entrada.Esquecer();
    }

    // ---------------- API publica ----------------
    /// <summary>Um checkpoint chama isto ao ser tocado.</summary>
    public void DefinirPontoDeRenascimento(Transform novoPonto)
    {
        pontoDeRenascimento = novoPonto;

        if (cura != null)
            cura.Recarregar();
    }

    /// <summary>Define o Y abaixo do qual o boneco morre (o Bootstrap ajusta pela fase).</summary>
    public void DefinirAlturaDaMorte(float y)
    {
        alturaDaMorte = y;
    }
}
