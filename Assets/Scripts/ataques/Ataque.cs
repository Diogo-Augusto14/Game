using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ataque corpo a corpo do jogador. Escolhe entre o golpe do chão e o golpe do ar,
/// liga a hitbox certa nos momentos certos — de preferência por Animation Events do
/// clip, ou por tempo enquanto os eventos não estiverem marcados.
///
/// Conversa com o Movimento (não ataca em dash nem pendurado) e com a Cura
/// (não ataca enquanto está se curando). Andar e pular durante o golpe continua
/// liberado, estilo Hollow Knight.
///
/// Coloque no mesmo objeto que o Animator, o Movimento e o Vida.
/// </summary>
[DisallowMultipleComponent]
public class Ataque : MonoBehaviour
{
    /// <summary>Um tipo de golpe: qual hitbox, qual animação e os tempos dele.</summary>
    [System.Serializable]
    public class Golpe
    {
        [Tooltip("Só pra você se achar no Inspector")]
        public string nome = "Golpe";

        [Tooltip("A hitbox deste golpe (objeto filho do boneco com o componente Espada)")]
        public Espada hitbox;

        [Tooltip("Nome do Trigger no Animator Controller. Vazio = não anima")]
        public string triggerDoAnimator = "Attack";

        [Tooltip("(Só sem Animation Events) Tempo até a hitbox ligar — a preparação")]
        [Min(0f)] public float atrasoAntesDoGolpe = 0.05f;

        [Tooltip("(Só sem Animation Events) Quanto tempo a hitbox fica ligada")]
        [Min(0.01f)] public float duracaoDoGolpe = 0.15f;

        [Tooltip("(Só sem Animation Events) Tempo até o ataque terminar — a recuperação")]
        [Min(0f)] public float recuperacao = 0.15f;

        [Tooltip("Espera depois que este golpe termina antes de poder atacar de novo")]
        [Min(0f)] public float intervaloDepois = 0.1f;

        [HideInInspector] public int hashDoTrigger;
    }

    [Header("Golpes")]
    [SerializeField] private Golpe golpeNoChao = new Golpe
    {
        nome = "Chão", triggerDoAnimator = "Attack",
        atrasoAntesDoGolpe = 0.05f, duracaoDoGolpe = 0.15f, recuperacao = 0.15f
    };

    [Tooltip("Golpe usado quando o boneco não está no chão. Sem hitbox aqui, ele usa o do chão")]
    [SerializeField] private Golpe golpeNoAr = new Golpe
    {
        nome = "Ar", triggerDoAnimator = "AttackAir",
        atrasoAntesDoGolpe = 0.04f, duracaoDoGolpe = 0.16f, recuperacao = 0.1f
    };

    [Header("Referências (vazio = procura sozinho)")]
    [SerializeField] private Animator animator;
    [SerializeField] private Movimento movimento;
    [SerializeField] private Cura cura;

    [Header("Como ligar/desligar a hitbox")]
    [Tooltip("LIGADO: o clip de ataque chama AtivarGolpe / DesativarGolpe / TerminarAtaque.\n" +
             "DESLIGADO: usa os tempos de cada golpe (modo provisório)")]
    [SerializeField] private bool usarEventosDeAnimacao = false;

    [Header("Input")]
    [Tooltip("Nome da Action no Input Actions asset (Edit > Project Settings > Input System Package)")]
    [SerializeField] private string acaoDeAtaque = "Attack";

    // ---------- estado interno ----------
    // Mesma proteção do Movimento: só escreve no Animator o que existe de verdade.
    private readonly HashSet<int> parametrosDoAnimator = new HashSet<int>();

    private InputAction ataqueAction;
    private Golpe golpeAtual;
    private float proximoAtaquePermitido;
    private Coroutine rotinaPorTempo;

    /// <summary>Outros scripts podem ler isso (Cura, som, UI, IA de inimigo).</summary>
    public bool EstaAtacando { get; private set; }

    /// <summary>Qual golpe está rolando agora (null se nenhum).</summary>
    public Golpe GolpeAtual => golpeAtual;

    // ---------- ciclo de vida ----------
    private void Reset()
    {
        animator  = GetComponent<Animator>();
        movimento = GetComponent<Movimento>();
        cura      = GetComponent<Cura>();
    }

    private void Awake()
    {
        if (animator == null)  animator  = GetComponent<Animator>();
        if (movimento == null) movimento = GetComponent<Movimento>();
        if (cura == null)      cura      = GetComponent<Cura>();

        ataqueAction = InputSystem.actions.FindAction(acaoDeAtaque);
        if (ataqueAction == null)
            Debug.LogError($"[Ataque] {name}: action \"{acaoDeAtaque}\" não encontrada nas " +
                           "Project-wide Actions. Confira o nome em Project Settings > Input System Package.", this);

        golpeNoChao.hashDoTrigger = Hash(golpeNoChao.triggerDoAnimator);
        golpeNoAr.hashDoTrigger   = Hash(golpeNoAr.triggerDoAnimator);

        if (golpeNoChao.hitbox == null)
            golpeNoChao.hitbox = GetComponentInChildren<Espada>(true);

        if (golpeNoChao.hitbox == null)
            Debug.LogError($"[Ataque] {name}: nenhuma hitbox no golpe do chão. " +
                           "Rode Tools > Combate > Montar hitboxes no boneco.", this);

        if (animator != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
                parametrosDoAnimator.Add(p.nameHash);
        }
    }

    private static int Hash(string nome)
    {
        return string.IsNullOrEmpty(nome) ? 0 : Animator.StringToHash(nome);
    }

    private void Update()
    {
        if (ataqueAction != null && ataqueAction.WasPressedThisFrame() && PodeAtacar())
            Atacar();
    }

    private void OnDisable()
    {
        CancelarAtaque();
    }

    // ---------- lógica ----------
    private bool PodeAtacar()
    {
        if (EstaAtacando) return false;
        if (Time.time < proximoAtaquePermitido) return false;

        // Dash é linha reta e invencível; pendurado ele está com as duas mãos na beirada.
        if (movimento != null && (movimento.Dashando || movimento.Pendurado)) return false;

        // Curando: o golpe cancela a cura em vez de sair junto.
        if (cura != null && cura.Curando)
        {
            cura.Cancelar();
            return false;
        }

        return EscolherGolpe().hitbox != null;
    }

    private Golpe EscolherGolpe()
    {
        bool noAr = movimento != null && !movimento.NoChao;
        if (noAr && golpeNoAr.hitbox != null)
            return golpeNoAr;
        return golpeNoChao;
    }

    private void Atacar()
    {
        golpeAtual = EscolherGolpe();
        EstaAtacando = true;

        if (animator != null && golpeAtual.hashDoTrigger != 0
            && parametrosDoAnimator.Contains(golpeAtual.hashDoTrigger))
            animator.SetTrigger(golpeAtual.hashDoTrigger);

        if (!usarEventosDeAnimacao)
            rotinaPorTempo = StartCoroutine(GolpePorTempo(golpeAtual));
    }

    private IEnumerator GolpePorTempo(Golpe g)
    {
        yield return new WaitForSeconds(g.atrasoAntesDoGolpe);
        AtivarGolpe();
        yield return new WaitForSeconds(g.duracaoDoGolpe);
        DesativarGolpe();
        yield return new WaitForSeconds(g.recuperacao);
        TerminarAtaque();
    }

    // ---------- chamados por Animation Events (ou pela rotina por tempo) ----------

    /// <summary>Liga a hitbox. Marque no quadro em que a espada está esticada.</summary>
    public void AtivarGolpe()
    {
        if (golpeAtual?.hitbox != null)
            golpeAtual.hitbox.Ligar();
    }

    /// <summary>Desliga a hitbox. Marque no quadro em que a espada começa a recolher.</summary>
    public void DesativarGolpe()
    {
        if (golpeAtual?.hitbox != null)
            golpeAtual.hitbox.Desligar();
    }

    /// <summary>Encerra o ataque e libera o próximo. Marque no último quadro do clip.</summary>
    public void TerminarAtaque()
    {
        float intervalo = golpeAtual != null ? golpeAtual.intervaloDepois : 0.1f;

        rotinaPorTempo = null;
        EstaAtacando = false;
        golpeAtual = null;
        proximoAtaquePermitido = Time.time + intervalo;
    }

    /// <summary>Interrompe o ataque na hora (ex: o boneco apanhou). Ligável em UnityEvent.</summary>
    public void CancelarAtaque()
    {
        if (!EstaAtacando)
            return;

        if (rotinaPorTempo != null)
        {
            StopCoroutine(rotinaPorTempo);
            rotinaPorTempo = null;
        }

        DesativarGolpe();
        TerminarAtaque();
    }
}