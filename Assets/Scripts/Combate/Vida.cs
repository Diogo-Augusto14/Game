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

    [Tooltip("Fracao do dano que a armadura segura (0 = nada; os blindados, metade)")]
    [SerializeField, Range(0f, 0.9f)] private float armadura;

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
    private IBloqueioDeDano bloqueio;
    private float semDanoAte = -10f;

    public Lado Lado => lado;

    public float Atual { get; private set; }

    /// <summary>
    /// Perguntado quando o golpe mataria: true = nao morre (a Pena da Fenix poe a vida de volta antes
    /// de responder). Quem nao tem item nenhum, morre.
    /// </summary>
    public System.Func<bool> SegundaChance;

    /// <summary>Segundos a mais sem tomar dano depois de um golpe (o Elixir de Nevoa).</summary>
    public float TempoSemDanoExtra { get; set; }

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
        TryGetComponent(out bloqueio);
    }

    /// <summary>Monta uma vida feita no codigo (mesa, barril): o lado e quanto aguenta.</summary>
    public void Configurar(Lado qual, float maxima, float peso = 0f)
    {
        lado = qual;
        vidaMaxima = Mathf.Max(1f, maxima);
        Atual = vidaMaxima;
        pesoDoEmpurrao = peso;
    }

    /// <summary>Muda a vida maxima sem encher (o Coracao de Leao soma, o Pacto de Sangue tira).</summary>
    public void MudarMaxima(float quanto)
    {
        vidaMaxima = Mathf.Max(1f, vidaMaxima + quanto);
        Atual = Mathf.Clamp(Atual + Mathf.Max(0f, quanto), 0.5f, vidaMaxima);
    }

    /// <summary>Tira vida sem golpe (o preco do altar), sem matar.</summary>
    public void Pagar(float quanto)
    {
        if (!Morto)
            Atual = Mathf.Max(0.5f, Atual - quanto);
    }

    /// <summary>Recupera vida (o Padre), ate o maximo. Morto nao cura.</summary>
    public void Curar(float quanto)
    {
        if (!Morto && quanto > 0f)
            Atual = Mathf.Min(vidaMaxima, Atual + quanto);
    }

    /// <summary>Troca a vida maxima (cada heroi tem a sua) e enche.</summary>
    public void DefinirMaxima(float maxima)
    {
        vidaMaxima = Mathf.Max(1f, maxima);
        Atual = vidaMaxima;
    }

    /// <summary>Poe a vida num valor (o "Continuar" devolve a vida que o jogador tinha).</summary>
    public void DefinirAtual(float quanto)
    {
        if (!Morto)
            Atual = Mathf.Clamp(quanto, 0.5f, vidaMaxima);
    }

    /// <summary>O ultimo golpe foi segurado por um escudo (o tiro bate e some, em vez de atravessar).</summary>
    public bool Bloqueou { get; private set; }

    /// <summary>Aplica o golpe. False se nao pegou (protegido ou bloqueado): quem bateu segue em frente.</summary>
    public bool ReceberDano(Dano dano)
    {
        Bloqueou = false;

        if (Protegido || dano.quantidade <= 0f)
            return false;

        // Escudo (o cavaleiro do escudo, de frente; o Escudo Sagrado do jogador): segura o golpe inteiro.
        // A peca pode chegar depois do Awake (os itens entram no jogador no meio da partida).
        if (bloqueio == null && lado == Lado.Jogador)
            TryGetComponent(out bloqueio);

        if (bloqueio != null && bloqueio.Bloqueia(dano))
        {
            Bloqueou = true;
            return false;
        }

        float quanto = dano.quantidade * (1f - armadura);
        Atual = Mathf.Max(0f, Atual - quanto);
        semDanoAte = Time.time + tempoSemDano + (tempoSemDano > 0f ? TempoSemDanoExtra : 0f);

        if (imortal && Atual <= 0f)
            Atual = vidaMaxima;

        // O empurraozinho: soma na velocidade, e quem anda (jogador ou inimigo) freia de volta sozinho.
        if (corpo != null && corpo.bodyType == RigidbodyType2D.Dynamic && dano.empurrao > 0f && dano.direcao.sqrMagnitude > 0.0001f)
            corpo.linearVelocity += dano.direcao.normalized * dano.empurrao * pesoDoEmpurrao;

        bool morreu = Atual <= 0f;

        if (morreu && SegundaChance != null && SegundaChance())
            morreu = false;

        if (morreu)
            Morto = true;

        AudioClip som = morreu && somDaMorte != null ? somDaMorte : somDoDano;

        if (audioSource != null && som != null)
            audioSource.PlayOneShot(som, volume);

        CameraDoJogo.Tremer(morreu ? Mathf.Max(tremorNaMorte, tremorNoDano) : tremorNoDano, morreu ? 0.25f : 0.15f);

        // O peso do golpe (do jogo antigo): o numero do dano pula do inimigo, o jogo congela um
        // instante nos golpes fortes, nas mortes e quando o jogador apanha.
        if (lado == Lado.Jogador)
        {
            Impacto.Congelar(0.08f);
        }
        else
        {
            bool forte = quanto >= 8f;
            Impacto.Numero((Vector2)transform.position + Vector2.up * 0.6f, quanto, forte);

            if (morreu)
                Impacto.Congelar(TryGetComponent(out Chefe _) ? 0.12f : 0.045f);
            else if (forte)
                Impacto.Congelar(0.03f);
        }

        AoTomarDano?.Invoke(dano);

        if (morreu)
            AoMorrer?.Invoke();

        return true;
    }
}
