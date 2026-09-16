using System.Collections;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// "Cérebro" do boneco. Não cuida de vida (Vida), andar (Movimento), bater (Ataque)
/// nem curar (Cura): só reage ao que acontece com ele — levou golpe, morreu, renasceu —
/// e coordena as peças.
/// Coloque no objeto raiz do boneco.
/// </summary>
[RequireComponent(typeof(Vida))]
[DisallowMultipleComponent]
public class Player : MonoBehaviour
{
    [Header("Referências (vazio = procura no próprio objeto)")]
    [SerializeField] private Movimento movimento;
    [SerializeField] private Ataque ataque;
    [SerializeField] private Cura cura;
    [SerializeField] private Rigidbody2D rb;

    [Header("Morte e renascimento")]
    [Tooltip("Tempo parado antes de renascer. Deixe igual à duração da animação de morte — sem animação, 0.5 basta")]
    [SerializeField, Min(0f)] private float tempoAteRenascer = 1.5f;

    [Tooltip("Onde renasce. Vazio = onde o boneco estava quando a fase começou")]
    [SerializeField] private Transform pontoDeRenascimento;

    [Tooltip("Invencibilidade logo depois de renascer, pra não morrer de novo na hora")]
    [SerializeField, Min(0f)] private float invencibilidadeAoRenascer = 1f;

    [Tooltip("Renascer devolve todos os frascos de cura")]
    [SerializeField] private bool recarregarFrascosAoRenascer = true;

    [Header("Eventos")]
    [Tooltip("Disparado quando o boneco morre. Use pra UI, som, fade...")]
    public UnityEvent AoMorrer;

    [Tooltip("Disparado quando o boneco volta. Use pra tirar o fade, resetar UI...")]
    public UnityEvent AoRenascer;

    // ---------- estado ----------
    private Vida vida;
    private Cameramov camera2D;
    private Vector3 posicaoInicial;

    public bool EstaMorto => vida.EstaMorto;

    // ---------- ciclo de vida ----------
    private void Reset()
    {
        movimento = GetComponent<Movimento>();
        ataque    = GetComponent<Ataque>();
        cura      = GetComponent<Cura>();
        rb        = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        vida = GetComponent<Vida>();

        if (movimento == null) movimento = GetComponent<Movimento>();
        if (ataque == null)    ataque    = GetComponent<Ataque>();
        if (cura == null)      cura      = GetComponent<Cura>();
        if (rb == null)        rb        = GetComponent<Rigidbody2D>();

        posicaoInicial = transform.position;

        if (Camera.main != null)
            camera2D = Camera.main.GetComponent<Cameramov>();
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

    // ---------- reações ----------
    private void AoLevarGolpe(DanoInfo info)
    {
        // Levou porrada no meio do golpe? O golpe é cortado na hora.
        // (a Cura já se cancela sozinha, ela também escuta o Vida)
        if (ataque != null)
            ataque.CancelarAtaque();
    }

    private void Morrer()
    {
        TravarControles(true);

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        AoMorrer?.Invoke();
        StartCoroutine(RotinaRenascer());
    }

    private IEnumerator RotinaRenascer()
    {
        yield return new WaitForSeconds(tempoAteRenascer);

        Vector3 destino = pontoDeRenascimento != null ? pontoDeRenascimento.position : posicaoInicial;
        Teleportar(destino);

        vida.Reviver(invencibilidadeAoRenascer);

        if (recarregarFrascosAoRenascer && cura != null)
            cura.Recarregar();

        // Câmera pula direto pro ponto novo em vez de "voar" pela fase.
        if (camera2D != null)
            camera2D.PosicionarImediatamente();

        TravarControles(false);
        AoRenascer?.Invoke();
    }

    /// <summary>
    /// Move o boneco de verdade: com Rigidbody2D em Interpolate, mexer só no transform
    /// deixa um rastro entre a posição velha e a nova. rb.position vai junto.
    /// </summary>
    private void Teleportar(Vector3 destino)
    {
        transform.position = destino;

        if (rb != null)
        {
            rb.position = destino;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void TravarControles(bool travar)
    {
        // O OnDisable do Movimento já devolve bodyType, gravidade e o colisor ao normal.
        if (movimento != null) movimento.enabled = !travar;
        if (ataque != null) ataque.enabled = !travar;
        if (cura != null) cura.enabled = !travar;
    }

    // ---------- API pública ----------

    /// <summary>Um checkpoint chama isto ao ser tocado.</summary>
    public void DefinirPontoDeRenascimento(Transform novoPonto)
    {
        pontoDeRenascimento = novoPonto;

        if (cura != null)
            cura.Recarregar();
    }
}
