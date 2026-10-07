using UnityEngine;

/// <summary>
/// As armas do jogador e o tiro: carrega duas, como no Soul Knight. A primeira e a do personagem
/// (o arco do Arqueiro: infinita, nunca sai da mao); a segunda e a que ele achar. Troca entre as
/// duas com Q (ou a roda do mouse, ou Y no controle).
///
/// Pegar outra arma (<see cref="Pegar"/>) troca a segunda: a velha cai no chao, com a municao que
/// tinha. A arma atira pra onde o jogador mira, no ritmo dela, gastando o pente; com o pente vazio,
/// recarrega sozinha (ou com R antes). Sem municao nenhuma, so faz o clique.
///
/// Nao atira no meio da esquiva. Cada disparo avisa <see cref="AoAtirar"/> (a animacao do arco e a
/// arma na mao escutam).
/// </summary>
[DisallowMultipleComponent]
public class ArmaDoJogador : MonoBehaviour
{
    [Tooltip("A arma do personagem: a primeira, que nunca sai da mao")]
    [SerializeField] private DadosDaArma arma;

    [Tooltip("A segunda arma no comeco da partida (vazio = so a do personagem)")]
    [SerializeField] private DadosDaArma segundaNoComeco;

    [Tooltip("De onde o tiro sai: distancia do centro do corpo, na direcao da mira (a arma na mao usa a dela)")]
    [SerializeField, Min(0f)] private float distanciaDaSaida = 0.45f;

    [Tooltip("Altura de onde o tiro sai, em relacao ao centro do corpo (o arco fica na altura do peito)")]
    [SerializeField] private float alturaDaSaida;

    [Header("Sons")]
    [Tooltip("O clique de quem aperta o gatilho sem municao")]
    [SerializeField] private AudioClip somSemMunicao;

    [Tooltip("Ao trocar de arma")]
    [SerializeField] private AudioClip somDaTroca;

    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;

    private ControlesDoJogador controles;
    private MovimentoDoJogador movimento;
    private AudioSource audioSource;
    private readonly ArmaCarregada[] maos = new ArmaCarregada[2];
    private int naMao;
    private float proximoTiro;
    private bool soltouOGatilho = true;
    private float recarregaAte = -1f;
    private float proximoClique;
    private readonly Rajada rajada = new Rajada();

    /// <summary>A arma na mao agora.</summary>
    public ArmaCarregada Atual => maos[naMao];

    /// <summary>A outra (nula se so tem a do personagem).</summary>
    public ArmaCarregada Outra => maos[1 - naMao];

    /// <summary>Os dados da arma na mao (atalho).</summary>
    public DadosDaArma Arma => Atual != null ? Atual.Dados : arma;

    public bool Recarregando => recarregaAte >= 0f;

    /// <summary>0 no comeco da recarga, 1 no fim.</summary>
    public float ProgressoDaRecarga =>
        Recarregando ? Mathf.Clamp01(1f - (recarregaAte - Time.time) / Mathf.Max(0.01f, Atual.Dados.recarga)) : 0f;

    /// <summary>Saiu um disparo: a direcao e o intervalo ate o proximo (a animacao do tiro cabe nele).</summary>
    public event System.Action<Vector2, float> AoAtirar;

    /// <summary>Trocou a arma na mao (por Q ou porque pegou outra).</summary>
    public event System.Action AoTrocar;

    private void Awake()
    {
        controles = GetComponent<ControlesDoJogador>();
        movimento = GetComponent<MovimentoDoJogador>();
        audioSource = GetComponent<AudioSource>();

        if (arma != null)
            maos[0] = new ArmaCarregada(arma);

        if (segundaNoComeco != null)
            maos[1] = new ArmaCarregada(segundaNoComeco);
    }

    /// <summary>
    /// Pega uma arma: entra no lugar da segunda e vai pra mao. Devolve a que saiu (pra cair no chao),
    /// ou nulo se a segunda estava vazia.
    /// </summary>
    public ArmaCarregada Pegar(ArmaCarregada nova)
    {
        ArmaCarregada velha = maos[1];
        maos[1] = nova;
        MudarPara(1);
        return velha;
    }

    private void Update()
    {
        if (controles == null || Atual == null)
            return;

        if (controles.Trocou && Outra != null)
            MudarPara(1 - naMao);

        // O resto da rajada sai sozinho, pra onde o jogador mira agora.
        if (rajada.Atirando)
            DispararDaRajada();

        if (Recarregando && Time.time >= recarregaAte)
        {
            Atual.Recarregar();
            recarregaAte = -1f;
        }

        if (controles.Recarregou && !Recarregando && Atual.PodeRecarregar)
            ComecarRecarga();

        if (!controles.Atirando)
        {
            soltouOGatilho = true;
            return;
        }

        if ((movimento != null && movimento.Esquivando) || Time.time < proximoTiro || Recarregando)
            return;

        if (!Atual.Dados.automatica && !soltouOGatilho)
            return;

        soltouOGatilho = false;

        if (Atual.TemNoPente)
        {
            Atirar(controles.Mira);
        }
        else if (Atual.PodeRecarregar)
        {
            ComecarRecarga();
        }
        else if (Time.time >= proximoClique)
        {
            proximoClique = Time.time + 0.3f;
            Tocar(somSemMunicao, 1f);
        }
    }

    private void MudarPara(int qual)
    {
        naMao = qual;
        recarregaAte = -1f;
        rajada.Parar();
        proximoTiro = Mathf.Max(proximoTiro, Time.time + 0.15f);
        Tocar(somDaTroca, 1f);
        AoTrocar?.Invoke();
    }

    private void ComecarRecarga()
    {
        recarregaAte = Time.time + Atual.Dados.recarga;
        Tocar(Atual.Dados.somDaRecarga, 1f);
    }

    private void Atirar(Vector2 rumo)
    {
        DadosDaArma dados = Atual.Dados;
        float intervalo = 1f / dados.tirosPorSegundo;
        proximoTiro = Time.time + intervalo;
        Atual.Gastar();
        rajada.Comecar(dados);
        DispararDaRajada();
        AoAtirar?.Invoke(rumo, intervalo);
    }

    // Um disparo (o unico, ou o da vez na rajada).
    private void DispararDaRajada()
    {
        DadosDaArma dados = Atual.Dados;
        Vector2 rumo = controles.Mira;

        // A arma na mao atira da ponta dela; a do personagem, de perto do corpo.
        float saida = dados.desenhoNaMao != null ? dados.distanciaDaMao + 0.3f : distanciaDaSaida;
        Vector2 origem = (Vector2)transform.position + Vector2.up * alturaDaSaida + rumo * saida;

        if (!rajada.Atualizar(origem, rumo, gameObject, Lado.Jogador))
            return;

        CameraDoJogo.Tremer(dados.tremor, 0.06f);
        Tocar(dados.som, dados.volume, true);
    }

    private void Tocar(AudioClip som, float quanto, bool variar = false)
    {
        if (audioSource == null || som == null)
            return;

        audioSource.pitch = variar ? Random.Range(0.94f, 1.06f) : 1f;
        audioSource.PlayOneShot(som, quanto * (variar ? 1f : volume));
    }
}
