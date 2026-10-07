using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// As armas que o jogador carrega, a que esta na mao, o pente e a municao de cada uma. Fica no
/// jogador.
///
/// O primeiro lugar e sempre a arma do heroi (a de sempre: municao infinita, sem pente, sem
/// recarga). As armas de fogo (<see cref="ArmaDeFogo"/>) entram depois, uma por item pego: pegar
/// uma nova ja passa pra ela. Cada uma tem o proprio pente e a propria municao: o pente acaba
/// e pede recarga (R, ou sozinha), a municao acaba de vez e so volta com caixa de municao.
///
///   R / L3          recarrega
///   Q / LT          proxima arma (antes, anda pelas flechas do heroi, <see cref="TrocaDeFlecha"/>)
///   roda do mouse   proxima / anterior
///   1 a 9           escolhe pelo lugar
///
/// Quem atira e o <see cref="AtiradorTopDown"/>, que pergunta <see cref="PodeDisparar"/> e avisa
/// <see cref="RegistrarDisparo"/>. A arma desenhada na mao e o <see cref="ArmaNaMao"/>.
/// </summary>
[DisallowMultipleComponent]
public class ArsenalDoJogador : MonoBehaviour
{
    public class Slot
    {
        /// <summary>null = a arma do heroi.</summary>
        public ArmaDeFogo arma;

        /// <summary>Municao total, contando o que esta no pente.</summary>
        public int municao;

        public int pente;
    }

    [SerializeField] private KeyCode teclaRecarregar = KeyCode.R;
    [SerializeField] private KeyCode teclaTrocar = KeyCode.Q;

    private readonly List<Slot> slots = new List<Slot> { new Slot() };
    private int emUso;
    private bool recarregando;
    private float inicioDaRecarga;
    private float duracaoDaRecarga;
    private float proximoAviso;

    private AtiradorTopDown atirador;
    private TrocaDeFlecha troca;
    private Vida vida;
    private ArmaNaMao naMao;

    /// <summary>O arsenal do jogador desta cena (a tabela de drops pergunta se falta municao).</summary>
    public static ArsenalDoJogador Atual { get; private set; }

    public IReadOnlyList<Slot> Slots => slots;

    public int Indice => emUso;

    public Slot SlotAtual => slots[emUso];

    public ArmaDeFogo ArmaAtual => slots[emUso].arma;

    /// <summary>A arma na mao e a do heroi.</summary>
    public bool HeroiAtivo => slots[emUso].arma == null;

    /// <summary>Tem alguma arma de fogo (depois de <see cref="ComecarPartida"/>, a inicial ja conta).</summary>
    public bool ComArmasDeFogo => slots.Count > 1;

    public bool Recarregando => recarregando;

    /// <summary>0 a 1: quanto da recarga ja passou.</summary>
    public float ProgressoDaRecarga =>
        recarregando ? Mathf.Clamp01((Time.time - inicioDaRecarga) / Mathf.Max(0.01f, duracaoDaRecarga)) : 0f;

    /// <summary>Pente, municao ou arma mudou (a HUD escuta).</summary>
    public event System.Action AoMudar;

    /// <summary>Um tiro de arma de fogo saiu (a arma na mao coiceia e solta o clarao).</summary>
    public event System.Action<ArmaDeFogo> AoDisparar;

    public static ArsenalDoJogador Em(GameObject jogador)
    {
        ArsenalDoJogador arsenal = jogador.GetComponent<ArsenalDoJogador>();
        return arsenal != null ? arsenal : jogador.AddComponent<ArsenalDoJogador>();
    }

    private void Awake()
    {
        Atual = this;
        atirador = GetComponent<AtiradorTopDown>();
        troca = GetComponent<TrocaDeFlecha>();
        vida = GetComponent<Vida>();
        naMao = ArmaNaMao.Criar(this);

        if (atirador != null)
            atirador.DefinirArma(null, this);
    }

    private void OnDestroy()
    {
        if (Atual == this)
            Atual = null;
    }

    // ---------------- ganhar e escolher ----------------
    /// <summary>
    /// Guarda a arma (se ja tinha, so enche metade da municao dela) e passa a usar ela.
    /// </summary>
    public void Ganhar(ArmaDeFogo arma)
    {
        if (arma == null)
            return;

        int onde = slots.FindIndex(s => s.arma == arma);

        if (onde >= 0)
        {
            Slot repetida = slots[onde];

            if (!arma.Infinita)
                repetida.municao = Mathf.Min(arma.municaoMaxima, repetida.municao + arma.municaoMaxima / 2);

            Selecionar(onde);
            return;
        }

        slots.Add(new Slot
        {
            arma = arma,
            municao = arma.Infinita ? 0 : arma.municaoMaxima,
            pente = arma.tamanhoDoPente,
        });

        Selecionar(slots.Count - 1);
    }

    /// <summary>
    /// Recomeca o arsenal pro comeco de uma partida: so a arma do heroi e a arma inicial (a pistola
    /// de municao infinita), que ja vai pra mao. O <see cref="Herois.Aplicar"/> chama ao vestir o
    /// heroi; as armas achadas depois entram por <see cref="Ganhar"/>.
    /// </summary>
    public void ComecarPartida()
    {
        slots.RemoveRange(1, slots.Count - 1);
        emUso = 0;
        recarregando = false;

        ArmaDeFogo inicial = CatalogoDeArmas.Inicial;

        if (inicial != null)
        {
            slots.Add(new Slot { arma = inicial, municao = 0, pente = inicial.tamanhoDoPente });
            Selecionar(1, false);
        }
        else
        {
            Selecionar(0, false);
        }
    }

    /// <summary>Poe na mao a arma desse lugar (0 = a do heroi). Trocar de arma cancela a recarga.</summary>
    public void Selecionar(int indice, bool comSom = true)
    {
        indice = Mathf.Clamp(indice, 0, slots.Count - 1);
        bool mudou = indice != emUso;

        emUso = indice;
        recarregando = false;

        if (atirador == null)
            atirador = GetComponent<AtiradorTopDown>();

        if (atirador != null)
            atirador.DefinirArma(ArmaAtual, this);

        if (naMao != null)
            naMao.Mostrar(ArmaAtual);

        if (troca == null)
            troca = GetComponent<TrocaDeFlecha>();

        // Com arma de fogo na mao as flechas do heroi nao valem: o painel delas sai da tela.
        if (troca != null)
            troca.EsconderPainel(!HeroiAtivo);

        if (mudou && comSom)
            Sons.Tocar(Som.Menu, 0.6f);

        // Pente vazio de uma arma que ainda tem municao: ja comeca a recarregar.
        if (!HeroiAtivo && SlotAtual.pente <= 0)
            Recarregar();

        AoMudar?.Invoke();
    }

    /// <summary>
    /// Passa pra proxima arma. Com o heroi na mao e mais de uma flecha na aljava, anda primeiro
    /// pelas flechas (como o Q sempre fez) e so depois da ultima vai pra proxima arma.
    /// </summary>
    public void ProximaArma()
    {
        if (troca == null)
            troca = GetComponent<TrocaDeFlecha>();

        if (HeroiAtivo && troca != null && troca.Aljava.Count > 1 && (slots.Count < 2 || troca.Indice < troca.Aljava.Count - 1))
        {
            troca.Proxima();
            return;
        }

        if (slots.Count < 2)
            return;

        IrPara((emUso + 1) % slots.Count);
    }

    public void ArmaAnterior()
    {
        if (slots.Count < 2)
            return;

        IrPara((emUso - 1 + slots.Count) % slots.Count);
    }

    private void IrPara(int indice)
    {
        Selecionar(indice);

        // Voltou pro heroi: comeca de novo na flecha normal.
        if (HeroiAtivo && troca != null)
            troca.VoltarParaNormal();
    }

    // ---------------- pente e recarga ----------------
    /// <summary>O heroi sempre pode; arma de fogo so com bala no pente e fora da recarga.</summary>
    public bool PodeDisparar => HeroiAtivo || (!recarregando && SlotAtual.pente > 0);

    /// <summary>O <see cref="AtiradorTopDown"/> chama depois de cada tiro de arma de fogo.</summary>
    public void RegistrarDisparo()
    {
        Slot slot = SlotAtual;

        if (slot.arma == null)
            return;

        slot.pente = Mathf.Max(0, slot.pente - 1);

        if (!slot.arma.Infinita)
            slot.municao = Mathf.Max(0, slot.municao - 1);

        AoDisparar?.Invoke(slot.arma);

        if (slot.pente <= 0)
        {
            if (slot.arma.Infinita || slot.municao > 0)
                Recarregar();
            else
                AcabouAMunicao(slot);
        }

        AoMudar?.Invoke();
    }

    /// <summary>Encheu o pente com o que sobra de municao. Nada acontece com a arma do heroi ou com o pente cheio.</summary>
    public void Recarregar()
    {
        Slot slot = SlotAtual;

        if (slot.arma == null || recarregando || slot.pente >= slot.arma.tamanhoDoPente)
            return;

        // Sem municao alem do que ja esta no pente: nao ha de onde encher.
        if (!slot.arma.Infinita && slot.municao <= slot.pente)
            return;

        recarregando = true;
        inicioDaRecarga = Time.time;
        duracaoDaRecarga = slot.arma.tempoDeRecarga;
        Sons.Tocar(Som.Destranca, 0.5f, 0.1f);
        AoMudar?.Invoke();
    }

    private void TerminarRecarga()
    {
        Slot slot = SlotAtual;
        recarregando = false;

        if (slot.arma != null)
            slot.pente = slot.arma.Infinita ? slot.arma.tamanhoDoPente : Mathf.Min(slot.arma.tamanhoDoPente, slot.municao);

        Sons.Tocar(Som.Destranca, 0.8f, 0.05f);
        AoMudar?.Invoke();
    }

    /// <summary>
    /// O jogador apertou pra atirar e nao saiu nada: avisa com um clique e, se o pente so esta
    /// vazio, comeca a recarregar. Limitado a um aviso a cada instante.
    /// </summary>
    public void AvisarSemTiro()
    {
        if (recarregando)
            return;

        Slot slot = SlotAtual;

        if (slot.arma != null && slot.pente <= 0 && (slot.arma.Infinita || slot.municao > 0))
        {
            Recarregar();
            return;
        }

        if (Time.time < proximoAviso)
            return;

        proximoAviso = Time.time + 0.5f;
        Sons.Tocar(Som.Negado, 0.5f);
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.1f, "Sem munição", new Color(1f, 0.65f, 0.45f));
    }

    // Arma sem uma bala: o jogador volta pra arma mais proxima que ainda atira (pelo menos a do heroi).
    private void AcabouAMunicao(Slot slot)
    {
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.1f, $"{slot.arma.nome}: sem munição", new Color(1f, 0.65f, 0.45f));

        for (int i = emUso - 1; i > 0; i--)
        {
            if (slots[i].arma.Infinita || slots[i].municao > 0)
            {
                IrPara(i);
                return;
            }
        }

        IrPara(0);
    }

    // ---------------- municao ----------------
    /// <summary>
    /// Enche essa fracao da municao maxima de cada arma de fogo (caixa de municao). Devolve false
    /// se nenhuma arma precisava (a caixa fica no chao).
    /// </summary>
    public bool AdicionarMunicao(float fracao)
    {
        bool encheu = false;

        foreach (Slot slot in slots)
        {
            if (slot.arma == null || slot.arma.Infinita || slot.municao >= slot.arma.municaoMaxima)
                continue;

            int quanto = Mathf.CeilToInt(slot.arma.municaoMaxima * fracao);
            slot.municao = Mathf.Min(slot.arma.municaoMaxima, slot.municao + quanto);
            encheu = true;
        }

        if (!encheu)
            return false;

        // A arma na mao estava seca e acabou de ganhar bala.
        if (!HeroiAtivo && SlotAtual.pente <= 0)
            Recarregar();

        TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.1f, "Munição!", new Color(1f, 0.88f, 0.4f));
        AoMudar?.Invoke();
        return true;
    }

    /// <summary>Tem alguma arma de fogo com municao que acaba (so assim uma caixa de municao serve de algo).</summary>
    public bool TemMunicaoLimitada
    {
        get
        {
            foreach (Slot slot in slots)
                if (slot.arma != null && !slot.arma.Infinita)
                    return true;

            return false;
        }
    }

    /// <summary>Alguma arma de fogo esta com menos de 60% da municao: a tabela de drops solta mais caixas.</summary>
    public bool PrecisaDeMunicao
    {
        get
        {
            foreach (Slot slot in slots)
                if (slot.arma != null && !slot.arma.Infinita && slot.municao < slot.arma.municaoMaxima * 0.6f)
                    return true;

            return false;
        }
    }

    // ---------------- entrada ----------------
    private void Update()
    {
        // Pausa, menu e fim de jogo: as teclas daqui (R, Q, numeros) sao dos menus.
        if (Time.timeScale <= 0f || (vida != null && vida.EstaMorto))
            return;

        if (Input.GetKeyDown(teclaTrocar) || Controle.Apertou(BotaoDoControle.LT))
            ProximaArma();

        float roda = Input.mouseScrollDelta.y;

        if (roda < -0.1f)
            IrPara(slots.Count > 1 ? (emUso + 1) % slots.Count : 0);
        else if (roda > 0.1f)
            ArmaAnterior();

        for (int i = 0; i < 9 && i < slots.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                IrPara(i);
        }

        if (Input.GetKeyDown(teclaRecarregar) || Controle.Apertou(BotaoDoControle.AnalogicoEsquerdo))
            Recarregar();

        if (recarregando && Time.time >= inicioDaRecarga + duracaoDaRecarga)
            TerminarRecarga();
    }
}
