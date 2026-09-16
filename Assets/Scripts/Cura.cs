using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Cura no estilo Hollow Knight: segure a tecla parado no chão por alguns instantes
/// pra gastar um frasco e recuperar vida. Apanhar, andar, pular, dar dash ou atacar
/// cancela — e o frasco não é gasto.
///
/// Coloque no boneco, junto com Vida e Movimento.
/// </summary>
[RequireComponent(typeof(Vida))]
[DisallowMultipleComponent]
public class Cura : MonoBehaviour
{
    [Header("Regras")]
    [Tooltip("Tecla que precisa ficar segurada. Não mexe no Input Manager")]
    [SerializeField] private KeyCode teclaDeCura = KeyCode.E;

    [Tooltip("Segundos segurando até a cura acontecer")]
    [SerializeField, Min(0.05f)] private float tempoParaCurar = 0.9f;

    [Tooltip("Quanto de vida cada frasco recupera")]
    [SerializeField, Min(1f)] private float quantidadeCurada = 25f;

    [Tooltip("Frascos disponíveis")]
    [SerializeField, Min(0)] private int frascosMaximos = 3;

    [Tooltip("Só cura com os dois pés no chão")]
    [SerializeField] private bool precisaEstarNoChao = true;

    [Tooltip("Andar cancela a cura")]
    [SerializeField] private bool andarCancela = true;

    [Header("Animação (opcional — só escreve se o parâmetro existir)")]
    [SerializeField] private Animator animator;

    [Tooltip("Bool ligado enquanto está se curando. Vazio = não anima")]
    [SerializeField] private string boolDoAnimator = "IsHealing";

    [Header("Eventos")]
    public UnityEvent AoComecar;
    public UnityEvent AoCurar;
    public UnityEvent AoCancelar;
    [Tooltip("Disparado quando a quantidade de frascos muda — bom pra UI")]
    public UnityEvent AoMudarFrascos;

    // ---------- estado ----------
    private Vida vida;
    private Movimento movimento;
    private Ataque ataque;
    private readonly HashSet<int> parametrosDoAnimator = new HashSet<int>();
    private int hashBool;
    private float progresso;

    /// <summary>True enquanto a tecla está sendo segurada e a cura é válida.</summary>
    public bool Curando { get; private set; }

    /// <summary>Frascos que restam.</summary>
    public int Frascos { get; private set; }

    public int FrascosMaximos => frascosMaximos;

    /// <summary>Progresso de 0 a 1 — pronto pra uma barra circular na UI.</summary>
    public float Progresso => tempoParaCurar <= 0f ? 0f : Mathf.Clamp01(progresso / tempoParaCurar);

    // ---------- ciclo de vida ----------
    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        vida = GetComponent<Vida>();
        movimento = GetComponent<Movimento>();
        ataque = GetComponent<Ataque>();
        Frascos = frascosMaximos;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
                parametrosDoAnimator.Add(p.nameHash);
        }

        hashBool = string.IsNullOrEmpty(boolDoAnimator) ? 0 : Animator.StringToHash(boolDoAnimator);
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
        bool segurando = Input.GetKey(teclaDeCura);

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

    // ---------- lógica ----------
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

        if (precisaEstarNoChao && !movimento.NoChao) return false;
        if (movimento.Dashando || movimento.Pendurado) return false;
        if (andarCancela && Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f) return false;

        return true;
    }

    private void Comecar()
    {
        Curando = true;
        progresso = 0f;
        Animar(true);
        AoComecar?.Invoke();
    }

    private void Concluir()
    {
        Frascos = Mathf.Max(0, Frascos - 1);
        vida.Curar(quantidadeCurada);

        Curando = false;
        progresso = 0f;
        Animar(false);

        AoCurar?.Invoke();
        AoMudarFrascos?.Invoke();
    }

    /// <summary>Interrompe a cura sem gastar frasco. Público pra outros scripts e UnityEvents.</summary>
    public void Cancelar()
    {
        if (!Curando)
            return;

        Curando = false;
        progresso = 0f;
        Animar(false);
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

    private void Animar(bool valor)
    {
        if (animator != null && hashBool != 0 && parametrosDoAnimator.Contains(hashBool))
            animator.SetBool(hashBool, valor);
    }
}
