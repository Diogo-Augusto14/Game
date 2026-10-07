using UnityEngine;

/// <summary>
/// A vida de quem apanha: o jogador, os inimigos e o boneco de treino usam esta mesma peca.
///
/// Recebe um <see cref="Dano"/>, tira a vida, empurra o corpo um pouquinho e avisa quem escuta
/// (<see cref="AoTomarDano"/>, <see cref="AoMorrer"/>). Piscar, animar e morrer ficam em pecas
/// separadas, que escutam estes avisos.
///
/// Depois de um golpe pode ficar um tempinho sem tomar outro (<see cref="NoTempoSemDano"/>). No
/// jogador a esquiva tambem protege: se o objeto tem um <see cref="IInvulneravel"/>, ele e
/// perguntado antes de cada golpe. Golpe que nao pega devolve false, e o tiro atravessa.
/// </summary>
[DisallowMultipleComponent]
public class Vida : MonoBehaviour
{
    [Tooltip("De que lado esta: tiro do mesmo lado atravessa sem machucar")]
    [SerializeField] private Lado lado = Lado.Inimigos;

    [SerializeField, Min(1f)] private float vidaMaxima = 10f;

    [Tooltip("Nao morre: quando a vida acaba, enche de novo (o boneco de treino)")]
    [SerializeField] private bool imortal;

    [Tooltip("Segundos depois de um golpe em que nada machuca (0 = toma todos)")]
    [SerializeField, Min(0f)] private float tempoSemDano;

    [Tooltip("Quanto o empurrao dos golpes mexe neste corpo (0 = nada, 1 = inteiro). So mexe em Rigidbody2D Dynamic")]
    [SerializeField, Min(0f)] private float pesoDoEmpurrao = 1f;

    [Header("Sensacao")]
    [SerializeField] private AudioClip somDoDano;

    [Tooltip("Toca no lugar do som do dano no golpe que mata")]
    [SerializeField] private AudioClip somDaMorte;

    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;

    [Tooltip("Tremor da camera a cada golpe (0 = nada)")]
    [SerializeField, Min(0f)] private float tremorNoDano;

    [Tooltip("Tremor da camera no golpe que mata (0 = nada)")]
    [SerializeField, Min(0f)] private float tremorNaMorte;

    private Rigidbody2D corpo;
    private AudioSource audioSource;
    private IInvulneravel protecao;
    private float semDanoAte = -10f;

    public Lado Lado => lado;

    public float Atual { get; private set; }

    public float Maxima => vidaMaxima;

    /// <summary>De 0 a 1 (pra uma barra de vida, no futuro).</summary>
    public float Fracao => Atual / vidaMaxima;

    public bool Morto { get; private set; }

    /// <summary>No tempinho sem dano depois de um golpe (o jogador pisca).</summary>
    public bool NoTempoSemDano => Time.time < semDanoAte;

    /// <summary>Um golpe agora nao pega: morto, no tempinho sem dano ou na esquiva.</summary>
    public bool Protegido => Morto || NoTempoSemDano || (protecao != null && protecao.Invulneravel);

    /// <summary>Tomou um golpe (tambem no que mata, antes do <see cref="AoMorrer"/>).</summary>
    public event System.Action<Dano> AoTomarDano;

    /// <summary>A vida acabou. Avisa uma vez so.</summary>
    public event System.Action AoMorrer;

    private void Awake()
    {
        Atual = vidaMaxima;
        corpo = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        TryGetComponent(out protecao);
    }

    /// <summary>Aplica o golpe. False se nao pegou (protegido): quem bateu segue em frente.</summary>
    public bool ReceberDano(Dano dano)
    {
        if (Protegido || dano.quantidade <= 0f)
            return false;

        Atual = Mathf.Max(0f, Atual - dano.quantidade);
        semDanoAte = Time.time + tempoSemDano;

        if (imortal && Atual <= 0f)
            Atual = vidaMaxima;

        // O empurraozinho: soma na velocidade, e quem anda (jogador ou inimigo) freia de volta sozinho.
        if (corpo != null && corpo.bodyType == RigidbodyType2D.Dynamic && dano.empurrao > 0f && dano.direcao.sqrMagnitude > 0.0001f)
            corpo.linearVelocity += dano.direcao.normalized * dano.empurrao * pesoDoEmpurrao;

        bool morreu = Atual <= 0f;

        if (morreu)
            Morto = true;

        AudioClip som = morreu && somDaMorte != null ? somDaMorte : somDoDano;

        if (audioSource != null && som != null)
            audioSource.PlayOneShot(som, volume);

        CameraDoJogo.Tremer(morreu ? Mathf.Max(tremorNaMorte, tremorNoDano) : tremorNoDano, morreu ? 0.25f : 0.15f);

        AoTomarDano?.Invoke(dano);

        if (morreu)
            AoMorrer?.Invoke();

        return true;
    }
}
