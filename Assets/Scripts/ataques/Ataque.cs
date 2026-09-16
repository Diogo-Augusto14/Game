using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Ataque corpo a corpo do jogador, com combo.
///
/// Como funciona o combo: cada golpe tem uma janela de cancelamento perto do fim. Apertar
/// de novo dentro dela emenda no golpe seguinte da sequencia; deixar passar volta pro
/// primeiro. Como o aperto fica guardado no <see cref="Entrada"/>, apertar um tiquinho
/// cedo tambem emenda — e por isso que o combo "nunca falha" nos jogos bons.
///
/// Os tempos de cada golpe NAO sao digitados na mao: sao fracoes de 0 a 1 da duracao do
/// clipe, que o proprio animador informa. Mudar o fps de uma animacao ajusta a janela do
/// golpe junto, sozinho — que e o erro classico de sincronizar hitbox com animacao.
///
/// Sequencias: combo de 3 no chao, combo de 2 no ar, golpe de dash e golpe de mergulho
/// (no ar, segurando pra baixo).
/// </summary>
[DisallowMultipleComponent]
public class Ataque : MonoBehaviour
{
    /// <summary>Um golpe: qual clipe, quanto dói, onde e quando a hitbox abre.</summary>
    [Serializable]
    public class Golpe
    {
        [Tooltip("So pra se achar no Inspector")]
        public string nome = "Golpe";

        [Tooltip("Nome do clipe na biblioteca (veja NomesDeAnimacao)")]
        public string clipe = NomesDeAnimacao.Ataque1;

        [Min(0f)] public float dano = 12f;

        [Min(0f)] public float empurrao = 3.5f;

        public PesoDoGolpe peso = PesoDoGolpe.Leve;

        [Header("Janela da hitbox (fracao do clipe)")]
        [Tooltip("Quando a hitbox LIGA. 0 = primeiro quadro, 1 = ultimo")]
        [Range(0f, 1f)] public float inicioDaJanela = 0.3f;

        [Tooltip("Quando a hitbox DESLIGA")]
        [Range(0f, 1f)] public float fimDaJanela = 0.6f;

        [Tooltip("A partir daqui, apertar de novo emenda no golpe seguinte")]
        [Range(0f, 1f)] public float inicioDoCancelamento = 0.55f;

        [Header("Geometria da hitbox")]
        [Tooltip("Centro da hitbox em relacao ao boneco (X positivo = na frente)")]
        public Vector2 centroDaHitbox = new Vector2(0.28f, 0.28f);

        public Vector2 tamanhoDaHitbox = new Vector2(0.45f, 0.35f);

        [Header("Deslocamento")]
        [Tooltip("Empurrao pra frente ao soltar o golpe (0 = golpeia parado)")]
        public float avanco = 1.4f;

        [Tooltip("Segundos sem controle horizontal durante o avanco")]
        [Min(0f)] public float travaDoAvanco = 0.1f;

        [Tooltip("Velocidade do clipe. 1 = normal, 1.2 = golpe mais seco")]
        [Min(0.1f)] public float velocidadeDoClipe = 1f;
    }

    [Header("Combo no chao (1 -> 2 -> 3)")]
    [SerializeField]
    private Golpe[] comboNoChao =
    {
        new Golpe { nome = "Corte 1", clipe = NomesDeAnimacao.Ataque1, dano = 12f, empurrao = 3f },
        new Golpe { nome = "Corte 2", clipe = NomesDeAnimacao.Ataque2, dano = 14f, empurrao = 3.5f },
        new Golpe
        {
            nome = "Corte 3", clipe = NomesDeAnimacao.Ataque3, dano = 22f, empurrao = 6f,
            peso = PesoDoGolpe.Forte, avanco = 2.2f,
            centroDaHitbox = new Vector2(0.34f, 0.28f), tamanhoDaHitbox = new Vector2(0.6f, 0.45f)
        }
    };

    [Header("Combo no ar (1 -> 2)")]
    [SerializeField]
    private Golpe[] comboNoAr =
    {
        new Golpe { nome = "Ar 1", clipe = NomesDeAnimacao.AtaqueAr1, dano = 12f, empurrao = 3f, avanco = 0.8f },
        new Golpe { nome = "Ar 2", clipe = NomesDeAnimacao.AtaqueAr2, dano = 16f, empurrao = 4.5f, avanco = 0.8f }
    };

    [Header("Golpe de dash (aperta golpe durante a arrancada)")]
    [SerializeField]
    private Golpe[] golpesDeDash =
    {
        new Golpe
        {
            nome = "Dash 1", clipe = NomesDeAnimacao.AtaqueDash1, dano = 16f, empurrao = 4f,
            avanco = 3.2f, travaDoAvanco = 0.18f, inicioDaJanela = 0.15f, fimDaJanela = 0.5f
        },
        new Golpe
        {
            nome = "Dash 2", clipe = NomesDeAnimacao.AtaqueDash2, dano = 20f, empurrao = 5.5f,
            peso = PesoDoGolpe.Forte, avanco = 3.4f, travaDoAvanco = 0.2f,
            inicioDaJanela = 0.15f, fimDaJanela = 0.55f
        }
    };

    [Header("Mergulho (no ar, segurando pra baixo)")]
    [SerializeField] private bool mergulhoAtivado = true;

    [SerializeField]
    private Golpe golpeDeMergulho = new Golpe
    {
        nome = "Mergulho", clipe = NomesDeAnimacao.AtaqueMergulho, dano = 24f, empurrao = 5f,
        peso = PesoDoGolpe.Forte, avanco = 0f, inicioDaJanela = 0.25f, fimDaJanela = 1f,
        centroDaHitbox = new Vector2(0.1f, 0.12f), tamanhoDaHitbox = new Vector2(0.5f, 0.4f)
    };

    [Tooltip("Velocidade da descida do mergulho")]
    [SerializeField, Min(0f)] private float velocidadeDoMergulho = 12f;

    [Header("Ritmo")]
    [Tooltip("Espera depois do combo terminar antes de poder atacar de novo")]
    [SerializeField, Min(0f)] private float esperaDepoisDoCombo = 0.12f;

    [Tooltip("Segundos sem apertar que zeram o combo de volta pro primeiro golpe")]
    [SerializeField, Min(0.1f)] private float tempoParaZerarCombo = 0.8f;

    [Header("Referencias (vazio = procura sozinho)")]
    [SerializeField] private Espada hitbox;
    [SerializeField] private AnimadorDeSprites animador;
    [SerializeField] private Movimento movimento;
    [SerializeField] private Cura cura;
    [SerializeField] private Entrada entrada;

    [Header("Eventos")]
    public UnityEvent<int> AoGolpear = new UnityEvent<int>();
    public UnityEvent AoTerminarCombo = new UnityEvent();

    // ---------------- estado ----------------
    private Golpe[] sequenciaAtual;
    private Golpe[] ultimaSequencia;
    private Golpe[] sequenciaDeMergulho;
    private int indiceNaSequencia = -1;
    private float tempoDoGolpe;
    private float duracaoDoGolpe;
    private bool janelaAberta;
    private bool mergulhando;
    private float proximoAtaquePermitido;
    private float ultimoAtaqueEm = -99f;
    private BoxCollider2D caixaDaHitbox;

    /// <summary>True enquanto um golpe esta rolando.</summary>
    public bool EstaAtacando => sequenciaAtual != null;

    /// <summary>Clipe que o golpe atual pede (string vazia se nao esta atacando).</summary>
    public string ClipeAtual => GolpeAtual != null ? GolpeAtual.clipe : "";

    /// <summary>Velocidade do clipe pedida pelo golpe atual.</summary>
    public float VelocidadeDoClipe => GolpeAtual != null ? GolpeAtual.velocidadeDoClipe : 1f;

    /// <summary>O golpe rolando agora, ou null.</summary>
    public Golpe GolpeAtual =>
        sequenciaAtual != null && indiceNaSequencia >= 0 && indiceNaSequencia < sequenciaAtual.Length
            ? sequenciaAtual[indiceNaSequencia]
            : null;

    /// <summary>Progresso de 0 a 1 do golpe atual.</summary>
    public float Progresso => duracaoDoGolpe <= 0f ? 0f : Mathf.Clamp01(tempoDoGolpe / duracaoDoGolpe);

    /// <summary>True durante o mergulho (queda com a lamina pra baixo).</summary>
    public bool Mergulhando => mergulhando;

    // ---------------- ciclo de vida ----------------
    private void Reset()
    {
        animador = GetComponentInChildren<AnimadorDeSprites>();
        movimento = GetComponent<Movimento>();
        cura = GetComponent<Cura>();
        entrada = GetComponent<Entrada>();
        hitbox = GetComponentInChildren<Espada>(true);
    }

    private void Awake()
    {
        if (animador == null) animador = GetComponentInChildren<AnimadorDeSprites>();
        if (movimento == null) movimento = GetComponent<Movimento>();
        if (cura == null) cura = GetComponent<Cura>();
        if (entrada == null) entrada = GetComponent<Entrada>();
        if (hitbox == null) hitbox = GetComponentInChildren<Espada>(true);

        if (hitbox != null)
            caixaDaHitbox = hitbox.GetComponent<BoxCollider2D>();

        if (hitbox == null)
            Debug.LogError($"[Ataque] {name}: sem hitbox (componente Espada num filho). O golpe nao vai machucar ninguem.", this);
    }

    private void OnDisable()
    {
        Cancelar();
    }

    private void Update()
    {
        if (EstaAtacando)
        {
            AtualizarGolpe();
            return;
        }

        if (entrada != null && entrada.AtaquePedido && PodeAtacar())
        {
            entrada.ConsumirAtaque();
            Comecar(EscolherSequencia());
        }
    }

    // ---------------- decisao ----------------
    private bool PodeAtacar()
    {
        if (Time.time < proximoAtaquePermitido)
            return false;

        if (movimento == null)
            return true;

        // Pendurado ele esta com as duas maos na beirada; na escada, so uma; morto, nenhuma.
        if (movimento.Pendurado || movimento.SubindoBeirada || movimento.NaEscada
            || movimento.Atordoado || movimento.Morto || movimento.Escorregando)
            return false;

        // Curando: o golpe cancela a cura em vez de sair junto com ela.
        if (cura != null && cura.Curando)
        {
            cura.Cancelar();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Qual sequencia o contexto pede. A ordem das perguntas importa: dash ganha do ar,
    /// e mergulho ganha do combo aereo normal.
    /// </summary>
    private Golpe[] EscolherSequencia()
    {
        bool noAr = movimento != null && !movimento.NoChao;

        if (movimento != null && (movimento.Dashando || movimento.Esquivando) && TemGolpes(golpesDeDash))
            return golpesDeDash;

        if (noAr && mergulhoAtivado && entrada != null && entrada.PedindoBaixo)
        {
            // Guardado num campo: montar o array a cada golpe geraria lixo de memoria.
            sequenciaDeMergulho ??= new[] { golpeDeMergulho };
            return sequenciaDeMergulho;
        }

        if (noAr && TemGolpes(comboNoAr))
            return comboNoAr;

        return comboNoChao;
    }

    private static bool TemGolpes(Golpe[] sequencia) => sequencia != null && sequencia.Length > 0;

    // ---------------- execucao ----------------
    private void Comecar(Golpe[] sequencia)
    {
        if (!TemGolpes(sequencia))
            return;

        // Emendou rapido na MESMA sequencia? continua o combo. Senao (outra sequencia,
        // demorou demais, ou o combo ja acabou inteiro) volta pro primeiro golpe.
        bool continuandoCombo = sequencia == ultimaSequencia
                             && indiceNaSequencia >= 0
                             && indiceNaSequencia + 1 < sequencia.Length
                             && Time.time - ultimoAtaqueEm <= tempoParaZerarCombo;

        sequenciaAtual = sequencia;
        ultimaSequencia = sequencia;
        indiceNaSequencia = continuandoCombo ? indiceNaSequencia + 1 : 0;

        IniciarGolpeAtual();
    }

    private void IniciarGolpeAtual()
    {
        Golpe g = GolpeAtual;

        if (g == null)
        {
            Terminar();
            return;
        }

        mergulhando = sequenciaAtual == sequenciaDeMergulho;

        tempoDoGolpe = 0f;
        janelaAberta = false;
        ultimoAtaqueEm = Time.time;

        AjustarHitbox(g);

        if (animador != null)
        {
            animador.Velocidade = g.velocidadeDoClipe;
            animador.Tocar(g.clipe, true);

            duracaoDoGolpe = animador.DuracaoDe(g.clipe) / Mathf.Max(0.1f, g.velocidadeDoClipe);
        }

        // Sem clipe na biblioteca a duracao seria 0 e o golpe nem apareceria.
        if (duracaoDoGolpe <= 0.01f)
            duracaoDoGolpe = 0.35f;

        AplicarDeslocamento(g);

        AoGolpear?.Invoke(indiceNaSequencia);
    }

    private void AplicarDeslocamento(Golpe g)
    {
        if (movimento == null)
            return;

        if (mergulhando)
        {
            movimento.ImpulsoVertical(-velocidadeDoMergulho);
            return;
        }

        if (g.avanco > 0f)
            movimento.AplicarAvancoDeGolpe(movimento.direcao * g.avanco, g.travaDoAvanco);
    }

    private void AtualizarGolpe()
    {
        Golpe g = GolpeAtual;

        if (g == null)
        {
            Terminar();
            return;
        }

        tempoDoGolpe += Time.deltaTime;
        float t = Progresso;

        // --- janela da hitbox
        bool deveEstarAberta = t >= g.inicioDaJanela && t <= g.fimDaJanela;

        if (deveEstarAberta && !janelaAberta)
        {
            janelaAberta = true;
            hitbox?.Ligar(g.dano, g.empurrao, g.peso);
        }
        else if (!deveEstarAberta && janelaAberta)
        {
            janelaAberta = false;
            hitbox?.Desligar();
        }

        // --- mergulho: acaba quando encosta no chao, nao quando o clipe termina
        if (mergulhando)
        {
            if (movimento != null && movimento.NoChao)
                Terminar();

            return;
        }

        // --- emenda no golpe seguinte
        bool podeEmendar = t >= g.inicioDoCancelamento && indiceNaSequencia < sequenciaAtual.Length - 1;

        if (podeEmendar && entrada != null && entrada.AtaquePedido && PodeAtacar())
        {
            entrada.ConsumirAtaque();
            FecharJanela();
            indiceNaSequencia++;
            IniciarGolpeAtual();
            return;
        }

        if (t >= 1f)
            Terminar();
    }

    private void AjustarHitbox(Golpe g)
    {
        if (hitbox == null)
            return;

        hitbox.transform.localPosition = new Vector3(g.centroDaHitbox.x, g.centroDaHitbox.y, 0f);

        if (caixaDaHitbox != null)
            caixaDaHitbox.size = g.tamanhoDaHitbox;
    }

    private void FecharJanela()
    {
        janelaAberta = false;
        hitbox?.Desligar();
    }

    private void Terminar()
    {
        FecharJanela();

        bool eraUltimo = sequenciaAtual == null || indiceNaSequencia >= sequenciaAtual.Length - 1;

        mergulhando = false;
        sequenciaAtual = null;
        tempoDoGolpe = 0f;
        duracaoDoGolpe = 0f;

        if (animador != null)
            animador.Velocidade = 1f;

        proximoAtaquePermitido = Time.time + (eraUltimo ? esperaDepoisDoCombo : 0f);

        AoTerminarCombo?.Invoke();
    }

    /// <summary>Interrompe o golpe na hora (ex.: o boneco apanhou). Ligavel em UnityEvent.</summary>
    public void Cancelar()
    {
        if (!EstaAtacando)
            return;

        Terminar();
        indiceNaSequencia = -1;
    }
}
