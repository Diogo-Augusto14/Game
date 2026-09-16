using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Cura no estilo Hollow Knight: segure a tecla, parado no chao, por alguns instantes pra
/// gastar um frasco e recuperar vida. Apanhar, andar, pular, dar dash ou atacar cancela —
/// e o frasco NAO e gasto. E essa troca (vida por ficar exposto) que faz a cura ser uma
/// decisao em vez de um botao.
///
/// Coloque no boneco, junto com Vida, Movimento e Entrada.
/// </summary>
[RequireComponent(typeof(Vida))]
[DisallowMultipleComponent]
public class Cura : MonoBehaviour
{
    [Header("Regras")]
    [Tooltip("Segundos segurando ate a cura acontecer")]
    [SerializeField, Min(0.05f)] private float tempoParaCurar = 0.85f;

    [Tooltip("Quanto de vida cada frasco recupera")]
    [SerializeField, Min(1f)] private float quantidadeCurada = 34f;

    [Tooltip("Frascos disponiveis")]
    [SerializeField, Min(0)] private int frascosMaximos = 3;

    [Tooltip("So cura com os dois pes no chao")]
    [SerializeField] private bool precisaEstarNoChao = true;

    [Tooltip("Andar cancela a cura")]
    [SerializeField] private bool andarCancela = true;

    [Header("Eventos")]
    public UnityEvent AoComecar = new UnityEvent();
    public UnityEvent AoCurar = new UnityEvent();
    public UnityEvent AoCancelar = new UnityEvent();
    [Tooltip("Disparado quando a quantidade de frascos muda — bom pra UI")]
    public UnityEvent AoMudarFrascos = new UnityEvent();

    // ---------------- estado ----------------
    private Vida vida;
    private Movimento movimento;
    private Ataque ataque;
    private Entrada entrada;
    private float progresso;

    /// <summary>True enquanto a tecla esta segurada e a cura continua valendo.</summary>
    public bool Curando { get; private set; }

    /// <summary>Frascos que restam.</summary>
    public int Frascos { get; private set; }

    public int FrascosMaximos => frascosMaximos;

    /// <summary>Progresso de 0 a 1 — pronto pra uma barra na UI.</summary>
    public float Progresso => tempoParaCurar <= 0f ? 0f : Mathf.Clamp01(progresso / tempoParaCurar);

    // ---------------- ciclo de vida ----------------
    private void Awake()
    {
        vida = GetComponent<Vida>();
        movimento = GetComponent<Movimento>();
        ataque = GetComponent<Ataque>();
        entrada = GetComponent<Entrada>();

        Frascos = frascosMaximos;
    }

    private void OnEnable()
    {
        vida.AoTomarDano.AddListener(AoLevarGolpe);
    }

    private void OnDisable()
    {
        vida.AoTomarDano.RemoveListener(AoLevarGolpe);
        Cancelar();
    }

    private void Update()
    {
        bool segurando = entrada != null && entrada.CuraSegurada;

        if (!Curando)
        {
            if (segurando && PodeComecar())
                Comecar();

            return;
        }

        if (!segurando || !ContinuaValido())
        {
            Cancelar();
            return;
        }

        progresso += Time.deltaTime;

        if (progresso >= tempoParaCurar)
            Concluir();
    }

    // ---------------- logica ----------------
    private bool PodeComecar()
    {
        if (Frascos <= 0) return false;
        if (vida.EstaMorto) return false;
        if (vida.VidaAtual >= vida.VidaMaxima) return false;
        if (ataque != null && ataque.EstaAtacando) return false;

        return ContinuaValido();
    }

    private bool ContinuaValido()
    {
        if (movimento == null)
            return true;

        if (movimento.Morto || movimento.Atordoado) return false;
        if (precisaEstarNoChao && !movimento.NoChao) return false;
        if (movimento.Dashando || movimento.Esquivando || movimento.Escorregando) return false;
        if (movimento.Pendurado || movimento.SubindoBeirada || movimento.NaEscada) return false;
        if (andarCancela && Mathf.Abs(movimento.LadoPedido) > 0.1f) return false;

        return true;
    }

    private void Comecar()
    {
        Curando = true;
        progresso = 0f;
        AoComecar?.Invoke();
    }

    private void Concluir()
    {
        Frascos = Mathf.Max(0, Frascos - 1);
        vida.Curar(quantidadeCurada);

        Curando = false;
        progresso = 0f;

        AoCurar?.Invoke();
        AoMudarFrascos?.Invoke();
    }

    /// <summary>Interrompe a cura sem gastar frasco. Publico pra outros scripts e UnityEvents.</summary>
    public void Cancelar()
    {
        if (!Curando)
            return;

        Curando = false;
        progresso = 0f;
        AoCancelar?.Invoke();
    }

    /// <summary>Devolve todos os frascos. Um checkpoint ou banco chama isto.</summary>
    public void Recarregar()
    {
        Frascos = frascosMaximos;
        AoMudarFrascos?.Invoke();
    }

    private void AoLevarGolpe(DanoInfo info)
    {
        Cancelar();
    }
}
