using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Monta um andar inteiro no estilo do Isaac: sorteia a grade de salas
/// (<see cref="GeradorDeAndar"/>), cria uma <see cref="Sala"/> por casa com porta onde ha
/// vizinha, povoa as salas com inimigos, poe o jogador na sala inicial e cuida da troca
/// de sala e da camera.
///
/// COMO AS SALAS FICAM NO MUNDO: cada casa da grade vira uma sala de verdade, lado a lado,
/// com as paredes coladas e os vaos das portas alinhados. A sala e as portas sao da pasta
/// Sala: a sala tranca as portas sozinha quando o jogador entra com inimigo vivo, e cada
/// porta avisa em <see cref="Porta.AoAtravessar"/> quando o jogador passa. O Andar escuta
/// esse aviso, poe o jogador na porta oposta da sala vizinha e desliza a camera ate la.
///
/// Pra usar: um objeto vazio com este componente numa cena (a Assets/Scenes/Jogo.unity ja vem assim).
/// Se a cena nao tiver ninguem com a tag Player, ele monta o jogador top-down.
/// </summary>
[DisallowMultipleComponent]
public class Andar : MonoBehaviour
{
    [Header("Geracao")]
    [Tooltip("Fase em que a partida comeca, contando todas: 1 = Mundo 1 Fase 1, 4 = Mundo 2 Fase 1. " +
             "A dificuldade (DificuldadeDaFase) sobe a cada fase")]
    [SerializeField, Min(1)] private int numeroDoAndar = 1;

    [Tooltip("0 = sorteia um andar novo a cada Play. Qualquer outro numero repete sempre o mesmo andar")]
    [SerializeField] private int semente;

    [SerializeField, Min(3)] private int larguraDaGrade = 9;

    [SerializeField, Min(3)] private int alturaDaGrade = 8;

    [Tooltip("Enfeites de chao (cogumelo, pedrinha, osso) por sala: sorteado entre o minimo e o maximo")]
    [SerializeField] private Vector2Int enfeitesPorSala = new Vector2Int(2, 5);

    [Header("Inimigos")]
    [Tooltip("Inimigos numa sala comum: sorteado entre o minimo e o maximo")]
    [SerializeField] private Vector2Int inimigosPorSala = new Vector2Int(2, 4);

    [Tooltip("Inimigo nao nasce mais perto que isto de uma porta")]
    [SerializeField, Min(0f)] private float distanciaDasPortas = 3f;

    [Tooltip("Sentinelas no maximo por sala: sao paradas, em excesso a sala vira tiroteio")]
    [SerializeField, Min(0)] private int maximoDeSentinelas = 2;

    [Header("Variedade das salas")]
    [Tooltip("Chance de uma sala comum nao ter pedra nem espinho na primeira fase (cai 2% por fase; nunca duas vizinhas vazias)")]
    [SerializeField, Range(0f, 1f)] private float chanceDeSalaVazia = 0.15f;

    [Tooltip("Salas logo depois da inicial vem com um inimigo a menos; as perto do chefe, com um a mais")]
    [SerializeField] private bool dosarPelaDistancia = true;

    [Tooltip("Espinhos so aparecem a partir desta fase (contando todas: 2 = Mundo 1 Fase 2)")]
    [SerializeField, Min(1)] private int espinhosAPartirDoAndar = 2;

    [Header("Itens e coletaveis")]
    [Tooltip("Chance de cada inimigo soltar coracao, moeda, bomba ou chave ao morrer, na primeira fase (+1% por fase)")]
    [SerializeField, Range(0f, 1f)] private float chanceDeDropDoInimigo = 0.15f;

    [Tooltip("Chance de cair um premio no meio da sala quando ela e limpa, na primeira fase (+2% por fase)")]
    [SerializeField, Range(0f, 1f)] private float chanceDePremioDaSala = 0.5f;

    [Tooltip("A partir desta fase (contando todas) a porta da sala do item fica trancada (precisa de chave)")]
    [SerializeField, Min(1)] private int trancarItemAPartirDoAndar = 2;

    [Tooltip("Chance da sala do tesouro ter dois pedestais: pega um, o outro some")]
    [SerializeField, Range(0f, 1f)] private float chanceDeDuasOpcoes = 0.3f;

    [Header("Chaves e baus")]
    [Tooltip("Baus de ferro trancados por andar (em salas comuns longe do inicio)")]
    [SerializeField, Min(0)] private int bausTrancadosPorAndar = 1;

    [Tooltip("A partir deste andar aparece um bau trancado a mais")]
    [SerializeField, Min(1)] private int bauExtraAPartirDoAndar = 3;

    [Tooltip("Chance do bau de ferro ter um item passivo (senao, coletaveis variados)")]
    [SerializeField, Range(0f, 1f)] private float chanceDeItemNoBau = 0.35f;

    [Tooltip("Chance da sala comum, ao ser limpa, soltar um bau de madeira no lugar do premio")]
    [SerializeField, Range(0f, 1f)] private float chanceDeBauNaSala = 0.1f;

    [Header("Sala de desafio")]
    [Tooltip("Ondas de inimigos depois de pegar o item: na primeira fase e depois dela (+1 no ultimo mundo)")]
    [SerializeField] private Vector2Int ondasDoDesafio = new Vector2Int(2, 3);

    [Tooltip("Inimigos por onda na primeira fase; a cada 3 fases vem mais um")]
    [SerializeField, Min(1)] private int inimigosPorOnda = 3;

    [Header("Fim do andar")]
    [Tooltip("Distancia do centro da sala do chefe ate o alcapao, pro lado oposto da porta (o pedestal fica no centro)")]
    [SerializeField, Min(0f)] private float distanciaDoAlcapao = 2f;

    [Header("Mundos e fases")]
    [Tooltip("Quantos mundos o jogo tem. Cada um e um tema (Porao, Catacumbas, Cripta); o ultimo e sempre o Abismo, " +
             "com o chefe final na ultima fase")]
    [SerializeField, Min(1)] private int quantidadeDeMundos = 4;

    [Tooltip("Fases em cada mundo. Toda fase termina num chefe; o da ultima e o mais forte do mundo")]
    [SerializeField, Min(1)] private int fasesPorMundo = 3;

    [Tooltip("Mostra 'Mundo 1 - Fase 2' no meio da tela a cada fase nova")]
    [SerializeField] private bool avisarAndarNovo = true;

    [Header("Menu")]
    [Tooltip("Mostra o menu inicial ao abrir a cena (recomecar depois de morrer pula o menu)")]
    [SerializeField] private bool mostrarMenu = true;

    [SerializeField] private string nomeDoJogo = "THE PRETTIE";

    [Header("Jogador")]
    [Tooltip("Sem ninguem com a tag Player na cena, monta o jogador top-down (WASD anda, setas atiram)")]
    [SerializeField] private bool criarJogador = true;

    [SerializeField] private Color corDoJogador = new Color(1f, 0.85f, 0.75f);

    [Tooltip("Monta a barra de vida se a cena nao tiver HUD")]
    [SerializeField] private bool montarHud = true;

    [Header("Camera")]
    [Tooltip("Forca ortografica, com zoom pra caber uma sala inteira")]
    [SerializeField] private bool ajustarCamera = true;

    [SerializeField, Min(0f)] private float tempoDaTransicao = 0.3f;

    [Header("Minimapa")]
    [SerializeField] private bool mostrarMinimapa = true;

    [Header("Cores")]
    [SerializeField] private Color corDoChaoDoItem = new Color(0.32f, 0.28f, 0.14f);
    [SerializeField] private Color corDoChaoDoChefe = new Color(0.32f, 0.14f, 0.14f);
    [SerializeField] private Color corDoChaoDaLoja = new Color(0.16f, 0.24f, 0.18f);
    [SerializeField] private Color corDoChaoDaSecreta = new Color(0.1f, 0.09f, 0.12f);
    [SerializeField] private Color corDoChaoDoDesafio = new Color(0.34f, 0.2f, 0.12f);
    [SerializeField] private Color corDoChaoDaAmaldicoada = new Color(0.2f, 0.06f, 0.1f);
    [SerializeField] private Color corDoBatenteDaLoja = new Color(0.4f, 0.85f, 0.45f);
    [SerializeField] private Color corDoBatenteDoItem = new Color(0.95f, 0.78f, 0.25f);
    [SerializeField] private Color corDoBatenteDoChefe = new Color(0.8f, 0.15f, 0.15f);
    [SerializeField] private Color corDoBatenteDoDesafio = new Color(0.95f, 0.5f, 0.15f);

    // Teclas entre colchetes aparecem desenhadas na HUD (TelaSimples.LinhaDeTeclas); depois
    // do "||" vem a mesma linha com os botoes do controle.
    private const string CONTROLES =
        "[W][A][S][D] andar | [Cima][Esquerda][Baixo][Direita] atirar\n[E] bomba | [Shift] dash | [Esc] pausa || " +
        "[Pad AnalogicoEsquerdo] andar | [Pad Y][Pad X][Pad A][Pad B] atirar\n[Pad LB] bomba | [Pad RB] dash | [Pad Start] pausa";

    // ---------------- estado ----------------
    private Sala[,] noMundo;
    private readonly Dictionary<Sala, SalaDoAndar> salaDoMapa = new Dictionary<Sala, SalaDoAndar>();
    private Transform raizDasSalas;
    private Transform jogador;
    private Rigidbody2D corpoDoJogador;
    private Vida vidaDoJogador;
    private Camera cam;
    private Coroutine transicao;
    private Vector2Int telaEnquadrada;
    private Vector2 gravidadeAnterior;
    private bool mexeuNaGravidade;

    // Itens que ja apareceram nesta partida: o proximo pedestal sorteia outro.
    private readonly HashSet<ItemPassivo> itensQueJaSairam = new HashSet<ItemPassivo>();

    // Salas comuns deste andar sorteadas pra ter bau de ferro trancado.
    private readonly HashSet<SalaDoAndar> comBauTrancado = new HashSet<SalaDoAndar>();

    public static Andar Atual { get; private set; }

    public MapaDoAndar Mapa { get; private set; }

    public SalaDoAndar SalaAtual { get; private set; }

    /// <summary>A fase contando todas, desde a primeira do jogo (1 = Mundo 1 Fase 1).</summary>
    public int NumeroDoAndar => numeroDoAndar;

    /// <summary>O mundo em jogo, de 1 ate <see cref="QuantidadeDeMundos"/>.</summary>
    public int Mundo => (numeroDoAndar - 1) / fasesPorMundo + 1;

    /// <summary>A fase dentro do mundo, de 1 ate <see cref="FasesPorMundo"/>.</summary>
    public int Fase => (numeroDoAndar - 1) % fasesPorMundo + 1;

    public int QuantidadeDeMundos => quantidadeDeMundos;

    public int FasesPorMundo => fasesPorMundo;

    /// <summary>"Mundo 2 - Fase 3", pro aviso na tela, a pausa e o fim de jogo.</summary>
    public string NomeDaFase => $"Mundo {Mundo} - Fase {Fase}";

    /// <summary>Os numeros da dificuldade desta fase (vida e velocidade dos inimigos, premios...).</summary>
    public DificuldadeDaFase Dificuldade { get; private set; } = new DificuldadeDaFase(1, 1, 3);

    /// <summary>Porao, Catacumbas, Cripta ou Abismo: a cara e os inimigos deste andar.</summary>
    public TemaDoAndar Tema { get; private set; } = TemaDoAndar.Porao;

    /// <summary>A semente que gerou este andar. Anote quando achar um andar com problema.</summary>
    public int SementeUsada { get; private set; }

    /// <summary>Os parametros com que este andar foi gerado (depois do <see cref="AoPrepararGeracao"/>).</summary>
    public ParametrosDoAndar ParametrosUsados { get; private set; }

    /// <summary>
    /// Chamado logo antes de sortear cada andar, com os parametros padrao daquele andar
    /// (quantas salas, tamanho da grade, circuitos, salas especiais...). Quem cuida da
    /// dificuldade ou dos mundos muda os numeros aqui, sem mexer no gerador.
    /// </summary>
    public event Action<ParametrosDoAndar> AoPrepararGeracao;

    /// <summary>Distancia entre o centro de duas salas vizinhas: o tamanho total de uma sala.</summary>
    public static Vector2 Passo => Sala.TamanhoPadrao + Vector2.one * 2f;

    /// <summary>Um andar novo foi montado (no Play e a cada <see cref="ProximoAndar"/>).</summary>
    public event Action<MapaDoAndar> AoGerar;

    /// <summary>O jogador entrou numa sala (a propria sala ja tranca as portas se tiver inimigo).</summary>
    public event Action<Sala> AoEntrarNaSala;

    /// <summary>O jogador morreu: a partida acabou. A <see cref="TelaDeFimDeJogo"/> aparece sozinha.</summary>
    public event Action AoMorrerJogador;

    /// <summary>O jogador deste andar (null se a cena nao tiver ninguem com a tag Player).</summary>
    public Transform Jogador => jogador;

    // ---------------- ciclo de vida ----------------
    private void Awake()
    {
        Atual = this;

        // Visto de cima nada cai. A gravidade e global: devolvida no OnDestroy.
        gravidadeAnterior = Physics2D.gravity;
        Physics2D.gravity = Vector2.zero;
        mexeuNaGravidade = true;

        if (mostrarMinimapa && GetComponent<Minimapa>() == null)
            gameObject.AddComponent<Minimapa>();

        if (GetComponent<TelaDePausa>() == null)
            gameObject.AddComponent<TelaDePausa>();
    }

    private void Start()
    {
        Gerar();

        if (mostrarMenu)
            TelaDeInicio.Mostrar(this, nomeDoJogo);
    }

    private void LateUpdate()
    {
        // A janela mudou de tamanho (tela cheia, outra resolucao, maximizar): refaz o zoom. Antes
        // ele ficava com o tamanho da janela do comeco e mostrava as salas vizinhas.
        if (cam != null && ajustarCamera && (Screen.width != telaEnquadrada.x || Screen.height != telaEnquadrada.y))
            EnquadrarCamera();
    }

    private void OnDestroy()
    {
        if (Atual == this)
        {
            Atual = null;
            TemaDoAndar.Usar(null);
        }

        if (mexeuNaGravidade)
            Physics2D.gravity = gravidadeAnterior;

        if (vidaDoJogador != null)
        {
            vidaDoJogador.AoMorrer.RemoveListener(MorreuOJogador);
            vidaDoJogador.AoTomarDano.RemoveListener(DoeuNoJogador);
        }
    }

    private void DoeuNoJogador(DanoInfo _) => Sons.Tocar(Som.DanoJogador);

    // ---------------- api ----------------
    /// <summary>
    /// Desce pra proxima fase (depois da ultima fase de um mundo, a primeira do mundo
    /// seguinte): mais salas, semente nova. O jogador continua o mesmo, com a
    /// vida, os itens e o inventario que tinha. O <see cref="Alcapao"/> chama isto.
    /// </summary>
    public void ProximoAndar()
    {
        if (vidaDoJogador != null && vidaDoJogador.EstaMorto)
            return;

        // Depois da ultima fase do ultimo mundo nao tem mais nada: o chefe final termina a partida.
        if (UltimoAndar)
            return;

        numeroDoAndar++;
        semente = 0;
        Gerar();
    }

    /// <summary>A sala montada no mundo que corresponde a esta casa do mapa.</summary>
    public Sala NoMundo(SalaDoAndar sala) => sala != null ? noMundo[sala.X, sala.Y] : null;

    /// <summary>Sorteia e monta o andar do zero, apagando o anterior.</summary>
    public void Gerar()
    {
        if (raizDasSalas != null)
        {
            // Destroy so apaga no fim do quadro: desliga ja, pra as salas velhas (portas,
            // sensores, alcapao) nao reagirem ao jogador que acabou de ser teleportado.
            raizDasSalas.gameObject.SetActive(false);
            Destroy(raizDasSalas.gameObject);
        }

        // Chao, paredes, enfeites e inimigos do mundo (TemaDoAndar). Antes de montar as salas.
        Dificuldade = new DificuldadeDaFase(Mundo, Fase, fasesPorMundo);
        Tema = TemaDoAndar.DoMundo(Mundo, quantidadeDeMundos);
        TemaDoAndar.Usar(Tema);

        // O mapa cresce a cada duas fases (DificuldadeDaFase.TamanhoDoMapa), nao a cada uma:
        // com 12 fases, contar todas deixaria as do fim enormes.
        SementeUsada = semente != 0 ? semente : Environment.TickCount;
        ParametrosDoAndar parametros = ParametrosDoAndar.Padrao(Dificuldade.TamanhoDoMapa, larguraDaGrade, alturaDaGrade);
        parametros.Desenhos = DisposicoesDaSala.Permitidos(numeroDoAndar >= espinhosAPartirDoAndar);
        parametros.ChanceDeSalaVazia = Mathf.Max(0.05f, chanceDeSalaVazia - Dificuldade.MenosSalasVazias);
        AoPrepararGeracao?.Invoke(parametros);
        ParametrosUsados = parametros;
        Mapa = GeradorDeAndar.Gerar(parametros, SementeUsada);

        // Mesma semente = mesmos inimigos nos mesmos lugares, nao so a mesma planta.
        UnityEngine.Random.InitState(SementeUsada);

        raizDasSalas = new GameObject("Salas").transform;
        raizDasSalas.SetParent(transform, false);
        noMundo = new Sala[Mapa.Largura, Mapa.Altura];
        salaDoMapa.Clear();
        SortearBausTrancados();

        foreach (SalaDoAndar sala in Mapa.Salas)
            noMundo[sala.X, sala.Y] = MontarSala(sala);

        Debug.Log($"[Andar] {NomeDaFase} ({Tema.Nome}), {Mapa.Salas.Count} salas, semente {SementeUsada}. {Dificuldade}");

        GarantirJogador();

        // Do mundo 3 em diante cada golpe no jogador pesa mais (DificuldadeDaFase.DanoNoJogador).
        if (vidaDoJogador != null)
            vidaDoJogador.MultiplicadorDeDanoRecebido = Dificuldade.DanoNoJogador;

        GarantirCamera();

        SalaAtual = null;
        AoGerar?.Invoke(Mapa);
        Entrar(Mapa.Inicio, null);

        if (avisarAndarNovo)
            AvisoDoAndar.Mostrar(UltimoAndar ? $"{NomeDaFase}: {Tema.Nome} (última fase)" : $"{NomeDaFase}: {Tema.Nome}");
    }

    // ---------------- troca de sala ----------------
    private void AoAtravessar(Porta porta)
    {
        if (porta.Sala == null || !salaDoMapa.TryGetValue(porta.Sala, out SalaDoAndar de))
            return;

        // So vale a porta da sala onde o jogador esta (a vizinha tem uma porta colada nesta).
        if (de != SalaAtual)
            return;

        Direcao direcao = ParaDirecao(porta.Lado);
        SalaDoAndar para = Mapa.Vizinha(de, direcao);

        if (para == null)
            return;

        Entrar(para, porta.Lado);

        // Porta da sala amaldicoada: cobra meio coracao na ida e na volta.
        if (de.Tipo == TipoDeSala.Amaldicoada || para.Tipo == TipoDeSala.Amaldicoada)
            SalaAmaldicoada.Ferir(jogador, porta.Lado.Direcao());
    }

    /// <param name="saiuPor">Porta por onde o jogador saiu da sala anterior. Null = comeco do andar, fica no centro.</param>
    private void Entrar(SalaDoAndar sala, LadoDaPorta? saiuPor)
    {
        SalaAtual = sala;

        if (!sala.Visitada)
            ResumoDaPartida.ContarSala();

        Mapa.Visitar(sala);
        Sala destino = NoMundo(sala);

        if (jogador != null)
        {
            Vector2 ponto = saiuPor.HasValue
                ? destino.PortaEm(saiuPor.Value.Oposto()).PontoDeChegada
                : (Vector2)destino.transform.position;

            Teleportar(ponto);
        }

        // Saiu da secreta por uma porta que do outro lado ainda estava escondida: abre.
        if (saiuPor.HasValue)
        {
            Porta chegada = destino.PortaEm(saiuPor.Value.Oposto());

            if (chegada != null && chegada.Escondida)
                chegada.Revelar();
        }

        Musica.Tocar(sala.Tipo == TipoDeSala.Chefe && !destino.Limpa ? MusicaDoChefe
                     : sala.Tipo == TipoDeSala.Loja ? TemaMusical.Loja
                     : MusicaDoAndar);

        MoverCamera(destino.transform.position, saiuPor.HasValue ? tempoDaTransicao : 0f);
        AoEntrarNaSala?.Invoke(destino);
    }

    private void Teleportar(Vector2 ponto)
    {
        if (corpoDoJogador != null)
        {
            corpoDoJogador.position = ponto;
            corpoDoJogador.linearVelocity = Vector2.zero;
        }

        jogador.position = new Vector3(ponto.x, ponto.y, jogador.position.z);
    }

    private static Direcao ParaDirecao(LadoDaPorta lado)
    {
        switch (lado)
        {
            case LadoDaPorta.Cima: return Direcao.Cima;
            case LadoDaPorta.Baixo: return Direcao.Baixo;
            case LadoDaPorta.Esquerda: return Direcao.Esquerda;
            default: return Direcao.Direita;
        }
    }

    private static LadoDaPorta ParaLado(Direcao direcao)
    {
        switch (direcao)
        {
            case Direcao.Cima: return LadoDaPorta.Cima;
            case Direcao.Baixo: return LadoDaPorta.Baixo;
            case Direcao.Esquerda: return LadoDaPorta.Esquerda;
            default: return LadoDaPorta.Direita;
        }
    }

    // ---------------- camera ----------------
    private void GarantirCamera()
    {
        cam = Camera.main;

        if (cam == null || !ajustarCamera)
            return;

        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        EnquadrarCamera();
        MontarMoldura();
    }

    /// <summary>
    /// A sala (15x9 com as paredes) e mais estreita que a tela 16:9: cabendo a altura inteira,
    /// sobrava meia unidade preta de cada lado. Cabe a LARGURA inteira e a parede de cima e de
    /// baixo corta um pouco, como no Isaac. So nunca deixa de mostrar o chao todo.
    /// </summary>
    private void EnquadrarCamera()
    {
        telaEnquadrada = new Vector2Int(Screen.width, Screen.height);
        float cabeLargura = Passo.x * 0.5f / Mathf.Max(cam.aspect, 0.1f);
        float chaoInteiro = Sala.TamanhoPadrao.y * 0.5f + 0.5f;
        cam.orthographicSize = Mathf.Max(cabeLargura, chaoInteiro);
    }

    /// <summary>
    /// Faixas pretas presas na camera, em volta de uma janela do tamanho de uma sala. Numa tela
    /// mais quadrada que 16:9 a camera mostra mais que a sala: sem isto aparecia a sala vizinha
    /// (ate a secreta, entregando o segredo) e o vazio preto onde nao tem sala. Com a camera
    /// parada na sala elas ficam exatamente na borda dela; na troca de sala andam junto.
    /// </summary>
    private void MontarMoldura()
    {
        if (cam.transform.Find("Moldura") != null)
            return;

        Transform moldura = new GameObject("Moldura").transform;
        moldura.SetParent(cam.transform, false);
        moldura.localPosition = new Vector3(0f, 0f, 1f);

        const float longe = 200f;
        Vector2 janela = Passo;
        float meiaLarg = janela.x * 0.5f + longe * 0.5f;
        float meiaAlt = janela.y * 0.5f + longe * 0.5f;

        Faixa(moldura, new Vector2(0f, meiaAlt), new Vector2(janela.x + longe * 2f, longe));
        Faixa(moldura, new Vector2(0f, -meiaAlt), new Vector2(janela.x + longe * 2f, longe));
        Faixa(moldura, new Vector2(-meiaLarg, 0f), new Vector2(longe, janela.y));
        Faixa(moldura, new Vector2(meiaLarg, 0f), new Vector2(longe, janela.y));
    }

    private static void Faixa(Transform moldura, Vector2 posicao, Vector2 tamanho)
    {
        // Por cima de tudo do mundo (a HUD e de outra camada e fica por cima dela).
        FormasDaSala.Desenho(moldura, "Faixa", Fosso.Pixel(), Color.black, posicao, tamanho, 1000);
    }

    private void MoverCamera(Vector2 alvo, float tempo)
    {
        if (cam == null || !ajustarCamera)
            return;

        if (transicao != null)
            StopCoroutine(transicao);

        if (tempo <= 0f)
        {
            cam.transform.position = new Vector3(alvo.x, alvo.y, cam.transform.position.z);
            return;
        }

        transicao = StartCoroutine(DeslizarCamera(alvo, tempo));
    }

    private IEnumerator DeslizarCamera(Vector2 alvo, float tempo)
    {
        Vector3 inicio = cam.transform.position;
        Vector3 fim = new Vector3(alvo.x, alvo.y, inicio.z);

        for (float t = 0f; t < tempo; t += Time.unscaledDeltaTime)
        {
            cam.transform.position = Vector3.Lerp(inicio, fim, Mathf.SmoothStep(0f, 1f, t / tempo));
            yield return null;
        }

        cam.transform.position = fim;
        transicao = null;
    }

    // ---------------- jogador ----------------
    private void GarantirJogador()
    {
        if (jogador != null)
            return;

        GameObject encontrado = GameObject.FindWithTag("Player");

        if (encontrado == null && criarJogador)
            encontrado = BootstrapTopDown.CriarJogador(transform.position, corDoJogador);

        if (encontrado == null)
        {
            Debug.LogWarning("[Andar] nenhum objeto com a tag Player. O andar foi montado, mas ninguem anda nele.");
            return;
        }

        jogador = encontrado.transform;
        corpoDoJogador = encontrado.GetComponent<Rigidbody2D>();
        vidaDoJogador = encontrado.GetComponent<Vida>();

        if (vidaDoJogador != null)
        {
            vidaDoJogador.AoMorrer.AddListener(MorreuOJogador);
            vidaDoJogador.AoTomarDano.AddListener(DoeuNoJogador);
        }

        if (encontrado.GetComponent<Inventario>() == null)
            encontrado.AddComponent<Inventario>();

        if (encontrado.GetComponent<EstatisticasDoJogador>() == null)
            encontrado.AddComponent<EstatisticasDoJogador>();

        if (montarHud && FindAnyObjectByType<Hud>() == null)
        {
            Hud hud = new GameObject("Hud").AddComponent<Hud>();
            hud.Configurar(encontrado.GetComponent<Vida>(), CONTROLES);
            hud.UsarCoracoes();
        }

        if (montarHud && FindAnyObjectByType<HudDoInventario>() == null)
            HudDoInventario.Criar(encontrado);
    }

    /// <summary>
    /// Fim da partida: o boneco para de obedecer e, depois de um instante pra ver a morte,
    /// aparece a tela de fim de jogo. O jogador nao renasce: no Isaac morreu, recomeca.
    /// </summary>
    private void MorreuOJogador()
    {
        if (jogador != null)
        {
            if (jogador.TryGetComponent(out Entrada entrada))
            {
                entrada.Esquecer();
                entrada.enabled = false;
            }

            if (jogador.TryGetComponent(out MovimentoTopDown movimento))
                movimento.Parar();
        }

        Sons.Tocar(Som.MorteJogador, 1f, 0f);
        Musica.Tocar(TemaMusical.FimDeJogo);

        AoMorrerJogador?.Invoke();
        TelaDeFimDeJogo.Mostrar(this);
    }

    // ---------------- montagem das salas ----------------
    private Sala MontarSala(SalaDoAndar casa)
    {
        List<LadoDaPorta> portas = new List<LadoDaPorta>();

        foreach (Direcao d in Direcoes.Todas)
            if (Mapa.TemPorta(casa, d))
                portas.Add(ParaLado(d));

        Vector2 centro = (Vector2)transform.position + new Vector2(casa.X * Passo.x, casa.Y * Passo.y);
        string nome = casa.Tipo == TipoDeSala.Normal ? $"Sala {DisposicoesDaSala.Nome(casa.Desenho)}" : $"Sala {casa.Tipo}";
        Sala sala = Sala.Criar($"{nome} ({casa.X},{casa.Y})", centro, portas, raizDasSalas);
        salaDoMapa[sala] = casa;

        sala.Pintar(Tema.CorDoChao, Tema.CorDaParede, Tema.ForcaDaCor);

        // So sala comum ganha obstaculo: inicio, item e chefe ficam com o chao livre. O
        // desenho vem do gerador, que nao repete o de uma vizinha.
        if (casa.Tipo == TipoDeSala.Normal)
            DisposicoesDaSala.Aplicar(sala, casa.Desenho, casa.EspelharX, casa.EspelharY);

        foreach (Porta porta in sala.Portas)
        {
            porta.AoAtravessar.AddListener(AoAtravessar);

            if (!porta.Existe)
                continue;

            SalaDoAndar vizinha = Mapa.Vizinha(casa, ParaDirecao(porta.Lado));

            // Porta pra secreta: parece parede ate uma bomba abrir (e sem batente colorido,
            // que entregaria o segredo). De dentro da secreta as portas sao normais.
            if (vizinha.Tipo == TipoDeSala.Secreta && casa.Tipo != TipoDeSala.Secreta)
            {
                porta.Esconder();
                porta.AoRevelar += _ => Sons.Tocar(Som.Segredo);
                continue;
            }

            if (casa.Tipo == TipoDeSala.Amaldicoada || vizinha.Tipo == TipoDeSala.Amaldicoada)
                SalaAmaldicoada.MarcarPorta(porta);
            else
                MarcarPortaEspecial(porta, casa, vizinha);
        }

        PintarChao(sala, casa.Tipo);

        // Toda sala ganha tochas; loja e chefe ficam sem enfeite de chao (a loja tem as
        // mercadorias, o chefe precisa do chao todo).
        // Sala comum sem obstaculo ganha mais enfeite, pra nao parecer um chao vazio.
        bool chaoLimpo = casa.Tipo == TipoDeSala.Loja || casa.Tipo == TipoDeSala.Chefe;
        int extras = casa.Tipo == TipoDeSala.Normal && casa.Desenho < 0 ? 3 : 0;
        sala.Enfeitar(chaoLimpo ? 0 : UnityEngine.Random.Range(enfeitesPorSala.x, enfeitesPorSala.y + 1) + extras);

        Povoar(sala, casa);
        PorPremios(sala, casa);
        return sala;
    }

    private void Povoar(Sala sala, SalaDoAndar casa)
    {
        int quantos;

        switch (casa.Tipo)
        {
            case TipoDeSala.Normal:
                quantos = UnityEngine.Random.Range(inimigosPorSala.x, inimigosPorSala.y + 1) + Dificuldade.InimigosExtras;

                // A fase esquenta no caminho: perto da inicial e mais leve, perto do chefe
                // vem um a mais.
                if (dosarPelaDistancia)
                {
                    if (casa.Distancia <= 1)
                        quantos = Mathf.Max(1, quantos - 1);
                    else if (casa.Profundidade >= 0.75f)
                        quantos++;
                }

                break;
            case TipoDeSala.Chefe:
                // No meio da sala, longe de todas as portas. O pedestal do premio nasce no
                // mesmo lugar quando ele morre (PorPremios). Qual chefe: ChefeDaFase.
                InimigoDeSala chefe = sala.CriarInimigo(UltimoAndar ? TipoDeInimigo.ChefeFinal : ChefeDaFase(Mundo, Fase), Vector2.zero);

                if (chefe != null && chefe.Vida != null)
                    chefe.Vida.AumentarVidaMaxima(chefe.Vida.VidaMaxima * ((UltimoAndar ? Dificuldade.VidaDoChefeFinal : Dificuldade.VidaDoChefe) - 1f));

                return;
            default:
                return; // inicio e item: sala tranquila, como no Isaac
        }

        int sentinelas = 0;

        for (int i = 0; i < quantos; i++)
        {
            TipoDeInimigo tipo = SortearInimigo();

            if (tipo == TipoDeInimigo.Sentinela && ++sentinelas > maximoDeSentinelas)
                tipo = TipoDeInimigo.Perseguidor;

            InimigoDeSala inimigo = sala.CriarInimigo(tipo, PontoLongeDasPortas(sala));

            if (inimigo is InimigoSentinela sentinela)
                sentinela.UsarOitoDirecoes(Dificuldade.SentinelaEmOitoDirecoes);

            if (inimigo is InimigoDeSangue sangue && Dificuldade.SangueEndurecido)
                sangue.Endurecer();

            Fortalecer(inimigo, sala);
        }
    }

    /// <summary>
    /// Aplica a dificuldade da fase num inimigo comum recem-criado: mais vida e velocidade,
    /// as vezes vira campeao (que sempre solta premio), e a chance de drop da fase.
    /// </summary>
    private void Fortalecer(InimigoDeSala inimigo, Sala sala)
    {
        if (inimigo == null)
            return;

        if (inimigo.Vida != null)
            inimigo.Vida.AumentarVidaMaxima(inimigo.Vida.VidaMaxima * (Dificuldade.VidaDosInimigos - 1f));

        inimigo.DefinirVelocidade(inimigo.Velocidade * Dificuldade.VelocidadeDosInimigos);

        bool campeao = UnityEngine.Random.value < Dificuldade.ChanceDeCampeao && Campeao.Aplicar(inimigo, Mundo) != null;
        float chance = campeao ? 1f : chanceDeDropDoInimigo + Dificuldade.BonusDeDrop;

        if (chance > 0f)
            inimigo.gameObject.AddComponent<SoltaColetavel>().Configurar(chance, sala.transform);
    }

    /// <summary>
    /// Os chefes de cada mundo. Nas fases do comeco sai um da lista de "comuns" (a fase 2
    /// nunca repete o da fase 1); na ultima fase vem o chefe do mundo, o mais forte, com
    /// 15% a mais de vida (DificuldadeDaFase.VidaDoChefe):
    ///   Mundo 1 Porao      -> Golem de Magma ou Minotauro Furioso; fecha com o Demonio do Martelo
    ///   Mundo 2 Catacumbas -> Golem de Magma ou Demonio do Martelo; fecha com o Minotauro Furioso (chama orcs)
    ///   Mundo 3 Cripta     -> Demonio do Martelo ou Minotauro Furioso; fecha com o Rei Necromante (chama esqueletos)
    ///   Mundo 4 Abismo     -> Minotauro Furioso ou Rei Necromante; fecha com o Olho do Abismo (chefe final)
    /// Com mais de 4 mundos, os do meio repetem a lista.
    /// </summary>
    private static readonly (TipoDeInimigo[] comuns, TipoDeInimigo final)[] ChefesPorMundo =
    {
        (new[] { TipoDeInimigo.Chefe, TipoDeInimigo.ChefeMinotauro }, TipoDeInimigo.ChefeSaltador),
        (new[] { TipoDeInimigo.Chefe, TipoDeInimigo.ChefeSaltador }, TipoDeInimigo.ChefeMinotauro),
        (new[] { TipoDeInimigo.ChefeSaltador, TipoDeInimigo.ChefeMinotauro }, TipoDeInimigo.ChefeNecromante),
        (new[] { TipoDeInimigo.ChefeMinotauro, TipoDeInimigo.ChefeNecromante }, TipoDeInimigo.ChefeFinal),
    };

    // O chefe comum que ja saiu neste mundo: a fase seguinte sorteia o outro.
    private TipoDeInimigo? chefeComumDoMundo;
    private int mundoDoChefeComum;

    private TipoDeInimigo ChefeDaFase(int mundo, int fase)
    {
        // O ultimo mundo usa sempre a ultima linha (Abismo); os outros vao repetindo as de antes.
        int linha = mundo >= quantidadeDeMundos ? ChefesPorMundo.Length - 1 : (mundo - 1) % (ChefesPorMundo.Length - 1);
        var (comuns, final) = ChefesPorMundo[linha];

        if (fase >= fasesPorMundo)
            return final;

        List<TipoDeInimigo> opcoes = new List<TipoDeInimigo>(comuns);

        if (mundoDoChefeComum == mundo && chefeComumDoMundo.HasValue && opcoes.Count > 1)
            opcoes.Remove(chefeComumDoMundo.Value);

        TipoDeInimigo escolhido = opcoes[UnityEngine.Random.Range(0, opcoes.Count)];
        chefeComumDoMundo = escolhido;
        mundoDoChefeComum = mundo;
        return escolhido;
    }

    /// <summary>
    /// Um inimigo comum da fase: cada tema tem a sua lista (ver <see cref="TemaDoAndar"/>).
    /// Na primeira fase do mundo so os do comeco da lista; do mundo 2 em diante, as vezes um
    /// do mundo anterior aparece no meio.
    /// </summary>
    private TipoDeInimigo SortearInimigo()
    {
        if (Mundo >= 2 && UnityEngine.Random.value < Dificuldade.ChanceDeInimigoDoMundoAnterior)
            return TemaDoAndar.DoMundo(Mundo - 1, quantidadeDeMundos).SortearInimigo(1f);

        return Tema.SortearInimigo(Dificuldade.VariedadeDoTema);
    }

    // ---------------- itens e coletaveis ----------------
    /// <summary>Letreiro no meio da tela pra cada heroi que o chefe liberou.</summary>
    private static void AnunciarLiberados(List<Herois.Heroi> liberados)
    {
        if (liberados.Count == 0)
            return;

        List<string> nomes = liberados.ConvertAll(h => h.Nome);
        AvisoDoAndar.Mostrar((nomes.Count > 1 ? "Heróis liberados: " : "Herói liberado: ") + string.Join(", ", nomes) + "!");
        Sons.Tocar(Som.Aviso);
    }

    /// <summary>
    /// O que cada tipo de sala guarda, como no Isaac:
    ///   Item   -> pedestal com um item passivo no meio (porta trancada a partir do andar 2);
    ///             as vezes dois pedestais, escolha um
    ///   Desafio -> pedestal com item; pegou, as portas fecham e vem ondas de inimigos
    ///   Amaldicoada -> item ou bau; a porta com espinhos custa meio coracao por passagem
    ///   Chefe  -> pedestal com item e a chave dourada quando a sala e limpa (a chave abre a
    ///             sala do tesouro trancada do andar seguinte, ou um bau de ferro)
    ///   Normal -> chance de um coletavel (ou, as vezes, um bau de madeira) quando a sala e
    ///             limpa; algumas tem um bau de ferro trancado
    /// Chave e rara de proposito: vem do chefe, da loja, do bau da sala amaldicoada e,
    /// raramente, de um inimigo.
    /// </summary>
    private void PorPremios(Sala sala, SalaDoAndar casa)
    {
        Vector2 centro = sala.transform.position;

        switch (casa.Tipo)
        {
            case TipoDeSala.Item:
                PorTesouro(sala);
                break;

            case TipoDeSala.Desafio:
                int ondas = (numeroDoAndar <= 1 ? ondasDoDesafio.x : ondasDoDesafio.y) + Dificuldade.OndasExtrasDoDesafio;
                SalaDeDesafio.Montar(sala, CatalogoDeItens.Sortear(itensQueJaSairam), ondas,
                                     inimigosPorOnda + Dificuldade.InimigosExtrasPorOnda, InimigoDoDesafio);
                break;

            case TipoDeSala.Amaldicoada:
                SalaAmaldicoada.Montar(sala, CatalogoDeItens.Sortear(itensQueJaSairam));
                break;

            case TipoDeSala.Chefe:
                // Sorteia ja na montagem, pra semente do andar decidir o item e nao a hora da luta.
                // O alcapao abre junto, acima do pedestal: pega o item e desce.
                // No ultimo andar nao tem alcapao: venceu o chefe final, venceu o jogo.
                if (UltimoAndar)
                {
                    sala.AoLimpar.AddListener(() =>
                    {
                        Musica.Tocar(TemaMusical.Vitoria);
                        TelaDeFimDeJogo.MostrarVitoria(this, Progresso.Zerou(Herois.Atual));
                    });
                    break;
                }

                // Chefe da ultima fase do mundo: e ele que libera herois (Progresso.VenceuMundo).
                ItemPassivo doChefe = CatalogoDeItens.Sortear(itensQueJaSairam);
                int mundo = Mundo;
                bool fechouOMundo = Dificuldade.FaseFinalDoMundo;
                int moedas = Dificuldade.MoedasDoChefe;
                sala.AoLimpar.AddListener(() =>
                {
                    Pedestal.Criar(doChefe, centro, sala.transform);
                    Alcapao.Criar(centro + LongeDaPorta(sala, distanciaDoAlcapao), sala.transform);
                    PorMoedasDoChefe(sala, moedas);

                    // A chave dourada fica entre a porta e o pedestal: no caminho de quem vem pegar o item.
                    ChaveDoChefe.Criar(centro - LongeDaPorta(sala, distanciaDoAlcapao), sala.transform);
                    Musica.Tocar(MusicaDoAndar);

                    List<Herois.Heroi> liberados = fechouOMundo ? Progresso.VenceuMundo(mundo) : new List<Herois.Heroi>();

                    if (liberados.Count == 0)
                        AvisoDoAndar.Mostrar("O chefe deixou uma chave dourada!");

                    AnunciarLiberados(liberados);
                });
                break;

            case TipoDeSala.Normal:
                if (comBauTrancado.Contains(casa))
                    PorBauTrancado(sala);

                // Beco que nao virou sala especial: premio garantido pra quem explorou ate ali.
                float chanceDePremio = casa.Recompensa ? 1f : Mathf.Min(0.75f, chanceDePremioDaSala + Dificuldade.BonusDePremioDaSala);
                sala.AoLimpar.AddListener(() => PremioDaSala(sala, chanceDePremio));
                break;

            case TipoDeSala.Loja:
                Loja.Montar(sala.transform, itensQueJaSairam);
                break;

            case TipoDeSala.Secreta:
                PorTesouroSecreto(sala);
                break;

        }

        // A porta da sala vizinha que leva ao item ganha um cadeado.
        if (ItemTrancado && casa.Tipo != TipoDeSala.Item)
        {
            foreach (Porta porta in sala.Portas)
            {
                SalaDoAndar vizinha = porta.Existe ? Mapa.Vizinha(casa, ParaDirecao(porta.Lado)) : null;

                if (vizinha != null && vizinha.Tipo == TipoDeSala.Item)
                    Tranca.Criar(porta);
            }
        }
    }

    /// <summary>
    /// Ponto a esta distancia do centro, do lado oposto a porta da sala: quem volta pra
    /// sala do chefe pela porta nao cai no alcapao sem querer ao chegar.
    /// </summary>
    private static Vector2 LongeDaPorta(Sala sala, float distancia)
    {
        foreach (Porta porta in sala.Portas)
            if (porta.Existe)
                return -porta.Lado.Direcao() * distancia;

        return Vector2.up * distancia;
    }

    private bool ItemTrancado => numeroDoAndar >= trancarItemAPartirDoAndar;

    /// <summary>
    /// Escolhe as salas comuns que ganham bau de ferro: de preferencia as mais longe do
    /// inicio (premio pra quem explora), sem repetir sala.
    /// </summary>
    private void SortearBausTrancados()
    {
        comBauTrancado.Clear();
        int quantos = bausTrancadosPorAndar + (numeroDoAndar >= bauExtraAPartirDoAndar ? 1 : 0);

        List<SalaDoAndar> comuns = Mapa.Salas.FindAll(s => s.Tipo == TipoDeSala.Normal);
        List<SalaDoAndar> longe = comuns.FindAll(s => s.Distancia >= 2);

        if (longe.Count >= quantos)
            comuns = longe;

        for (int i = 0; i < quantos && comuns.Count > 0; i++)
        {
            int sorteada = UnityEngine.Random.Range(0, comuns.Count);
            comBauTrancado.Add(comuns[sorteada]);
            comuns.RemoveAt(sorteada);
        }
    }

    /// <summary>Bau de ferro num canto livre da sala, com um tesouro sorteado ja na montagem.</summary>
    private void PorBauTrancado(Sala sala)
    {
        ItemPassivo item = UnityEngine.Random.value < chanceDeItemNoBau ? CatalogoDeItens.Sortear(itensQueJaSairam) : null;
        TipoDeColetavel[] tesouro = item != null ? new TipoDeColetavel[0] : Bau.SortearTesouro(numeroDoAndar);
        Bau.CriarTrancado((Vector2)sala.transform.position + PontoParaBau(sala), sala.transform, item, tesouro);
    }

    /// <summary>
    /// Premio da sala comum limpa: as vezes um bau de madeira com dois ou tres coletaveis,
    /// senao a chance de sempre de um coletavel no meio.
    /// </summary>
    private void PremioDaSala(Sala sala, float chanceDePremio)
    {
        Vector2 centro = sala.transform.position;

        if (UnityEngine.Random.value < chanceDeBauNaSala)
        {
            TipoDeColetavel[] dentro = new TipoDeColetavel[UnityEngine.Random.Range(2, 4)];

            for (int i = 0; i < dentro.Length; i++)
                dentro[i] = TabelaDeDrops.Sortear();

            Vector2 ponto = sala.Livre(Vector2.zero, 0.8f) ? Vector2.zero : PontoParaBau(sala);
            Bau.Criar(centro + ponto, sala.transform, dentro);
            return;
        }

        TabelaDeDrops.TalvezSoltar(chanceDePremio, centro, sala.transform);
    }

    /// <summary>
    /// Ponto livre pra um bau: longe das paredes (sobra lugar pro pedestal e pros coletaveis
    /// em volta), das pedras e das portas.
    /// </summary>
    private Vector2 PontoParaBau(Sala sala)
    {
        Vector2 centro = sala.transform.position;
        Vector2 melhor = Vector2.zero;

        for (int tentativa = 0; tentativa < 30; tentativa++)
        {
            Vector2 ponto = sala.PontoLivreAleatorio(2.2f);

            if (!sala.Livre(ponto, 0.8f))
                continue;

            melhor = ponto;
            bool longe = true;

            foreach (Porta porta in sala.Portas)
                if (porta.Existe && Vector2.Distance(centro + ponto, porta.PontoDeChegada) < distanciaDasPortas)
                    longe = false;

            if (longe)
                break;
        }

        return melhor;
    }

    private TemaMusical MusicaDoChefe => UltimoAndar ? TemaMusical.ChefeFinal : TemaMusical.Chefe;

    /// <summary>A musica do mundo atual (cada tema tem a sua; ver <see cref="TemaDoAndar.Musica"/>).</summary>
    public TemaMusical MusicaDoAndar => TemaDoAndar.Atual != null ? TemaDoAndar.Atual.Musica : Musica.DoAndar(Mundo);

    /// <summary>A ultima fase do ultimo mundo, a do chefe final: vencer ele termina a partida.</summary>
    public bool UltimoAndar => numeroDoAndar >= AndarFinal;

    /// <summary>Quantas fases o jogo tem ao todo (mundos x fases por mundo).</summary>
    public int AndarFinal => quantidadeDeMundos * fasesPorMundo;

    /// <summary>
    /// As moedas que o chefe deixa alem do item (DificuldadeDaFase.MoedasDoChefe), num arco
    /// em volta do pedestal do lado da porta, longe do alcapao (que fica do lado oposto).
    /// </summary>
    private static void PorMoedasDoChefe(Sala sala, int quantas)
    {
        Vector2 centro = sala.transform.position;
        Vector2 praPorta = -LongeDaPorta(sala, 1f);
        float meio = Mathf.Atan2(praPorta.y, praPorta.x) * Mathf.Rad2Deg;

        for (int i = 0; i < quantas; i++)
        {
            float angulo = (meio - 70f + 140f * (quantas == 1 ? 0.5f : i / (quantas - 1f))) * Mathf.Deg2Rad;
            Coletavel.Criar(TipoDeColetavel.Moeda, centro + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * 1.6f, sala.transform);
        }
    }

    /// <summary>
    /// O premio de quem acha a sala secreta: as vezes um item, senao um monte de moedas
    /// com uma bomba (pra achar a proxima) e um coracao.
    /// </summary>
    private void PorTesouroSecreto(Sala sala)
    {
        Vector2 centro = sala.transform.position;

        if (UnityEngine.Random.value < 0.4f)
        {
            Pedestal.Criar(CatalogoDeItens.Sortear(itensQueJaSairam), centro, sala.transform);
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            float angulo = i * 72f * Mathf.Deg2Rad;
            Coletavel.Criar(TipoDeColetavel.Moeda, centro + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * 1.2f, sala.transform);
        }

        Coletavel.Criar(TipoDeColetavel.Bomba, centro + Vector2.left * 0.3f, sala.transform);
        Coletavel.Criar(TipoDeColetavel.Coracao, centro + Vector2.right * 0.3f, sala.transform);
    }

    /// <summary>
    /// A sala do tesouro: o pedestal no meio, entre dois candelabros. As vezes vem com duas
    /// opcoes (escolha um: o outro item some).
    /// </summary>
    private void PorTesouro(Sala sala)
    {
        Vector2 centro = sala.transform.position;

        if (UnityEngine.Random.value < chanceDeDuasOpcoes)
        {
            Pedestal esquerda = Pedestal.Criar(CatalogoDeItens.Sortear(itensQueJaSairam), centro + Vector2.left * 1.5f, sala.transform);
            Pedestal direita = Pedestal.Criar(CatalogoDeItens.Sortear(itensQueJaSairam), centro + Vector2.right * 1.5f, sala.transform);
            Pedestal.EscolhaUm(esquerda, direita);
        }
        else
        {
            Pedestal.Criar(CatalogoDeItens.Sortear(itensQueJaSairam), centro, sala.transform);
        }

        Sprite[] candelabro = ArteImportada.Candelabro(16f);

        for (int lado = -1; lado <= 1 && candelabro != null; lado += 2)
            EfeitoDeQuadros.Criar(candelabro, 6f, centro + new Vector2(lado * 3f, 0.4f), -8, sala.transform)?.EmLoop();
    }

    /// <summary>Um inimigo das ondas da sala de desafio, com a mesma tabela e os drops do andar.</summary>
    private InimigoDeSala InimigoDoDesafio(Sala sala, Vector2 ponto)
    {
        TipoDeInimigo tipo = SortearInimigo();

        // Sentinela e parada: numa onda so faz a sala virar tiroteio.
        if (tipo == TipoDeInimigo.Sentinela)
            tipo = TipoDeInimigo.Perseguidor;

        InimigoDeSala inimigo = sala.CriarInimigo(tipo, ponto);
        Fortalecer(inimigo, sala);
        return inimigo;
    }

    /// <summary>Ponto livre da sala longe de toda porta, pra ninguem nascer em cima de quem entra.</summary>
    private Vector2 PontoLongeDasPortas(Sala sala)
    {
        Vector2 centro = sala.transform.position;
        Vector2 ponto = sala.PontoLivreAleatorio();

        for (int tentativa = 0; tentativa < 20; tentativa++)
        {
            bool longe = true;

            foreach (Porta porta in sala.Portas)
                if (porta.Existe && Vector2.Distance(centro + ponto, porta.PontoDeChegada) < distanciaDasPortas)
                    longe = false;

            if (longe)
                break;

            ponto = sala.PontoLivreAleatorio();
        }

        return ponto;
    }

    private void PintarChao(Sala sala, TipoDeSala tipo)
    {
        Color cor;

        switch (tipo)
        {
            case TipoDeSala.Item: cor = corDoChaoDoItem; break;
            case TipoDeSala.Chefe: cor = corDoChaoDoChefe; break;
            case TipoDeSala.Loja: cor = corDoChaoDaLoja; break;
            case TipoDeSala.Secreta: cor = corDoChaoDaSecreta; break;
            case TipoDeSala.Desafio: cor = corDoChaoDoDesafio; break;
            case TipoDeSala.Amaldicoada: cor = corDoChaoDaAmaldicoada; break;
            default: return;
        }

        Transform chao = sala.transform.Find("Cenario/Chao");

        // Os ladrilhos do pacote ja tem cor: o tom da sala especial entra pela metade.
        // Na sala pronta do Old Prison so o matiz entra, de leve, sem escurecer a sala.
        if (sala.ComFundo)
        {
            float maior = Mathf.Max(cor.r, Mathf.Max(cor.g, cor.b), 0.01f);
            cor = Color.Lerp(Color.white, new Color(cor.r / maior, cor.g / maior, cor.b / maior), 0.25f);
        }
        else if (ArteGerada.CenarioDoPacote)
            cor = Color.Lerp(Color.white, cor, 0.5f);

        if (chao != null && chao.TryGetComponent(out SpriteRenderer sr))
            sr.color = cor;

        // O veu que baixa o contraste do piso leva o mesmo tom, senao apagava parte dele.
        Transform veu = sala.transform.Find("Cenario/VeuDoChao");

        if (veu != null && veu.TryGetComponent(out SpriteRenderer srVeu))
        {
            Color tom = srVeu.color;
            srVeu.color = new Color(tom.r * cor.r, tom.g * cor.g, tom.b * cor.b, tom.a);
        }
    }

    /// <summary>
    /// Porta que leva a (ou sai de) sala do item ou do chefe ganha dois batentes coloridos,
    /// um de cada lado do vao. O desenho da porta em si e da <see cref="Porta"/>, que muda
    /// de cor ao trancar, entao o aviso vai em pecas separadas.
    /// </summary>
    private void MarcarPortaEspecial(Porta porta, SalaDoAndar casa, SalaDoAndar vizinha)
    {
        Color cor;

        if (casa.Tipo == TipoDeSala.Chefe || vizinha.Tipo == TipoDeSala.Chefe)
            cor = corDoBatenteDoChefe;
        else if (casa.Tipo == TipoDeSala.Item || vizinha.Tipo == TipoDeSala.Item)
            cor = corDoBatenteDoItem;
        else if (casa.Tipo == TipoDeSala.Loja || vizinha.Tipo == TipoDeSala.Loja)
            cor = corDoBatenteDaLoja;
        else if (casa.Tipo == TipoDeSala.Desafio || vizinha.Tipo == TipoDeSala.Desafio)
            cor = corDoBatenteDoDesafio;
        else
            return;

        Vector2 eixo = porta.Lado.Horizontal() ? Vector2.right : Vector2.up;
        const float MEIA_PORTA = 0.75f;
        const float LADO = 0.4f;

        // Com o pacote: caveira no chefe, estandarte vermelho na loja e azul no item.
        Sprite estandarte = cor == corDoBatenteDoChefe ? ArteImportada.Objeto(2, 3)
            : cor == corDoBatenteDaLoja ? ArteImportada.Objeto(1, 3)
            : cor == corDoBatenteDoDesafio ? ArteImportada.Trofeu(true)
            : ArteImportada.Objeto(1, 4);

        // Old Prison: bandeira de cada tipo (chefe vermelha escura, item azul, loja dourada, desafio laranja).
        Sprite bandeira = ArteImportada.BandeiraDaPrisao(cor == corDoBatenteDoChefe ? 0 : cor == corDoBatenteDaLoja ? 3 : cor == corDoBatenteDoDesafio ? 5 : 2);

        if (bandeira != null)
            estandarte = bandeira;

        for (int s = -1; s <= 1; s += 2)
        {
            if (estandarte != null)
            {
                Vector2 junto = eixo * (s * (MEIA_PORTA + 0.3f));
                FormasDaSala.Desenho(porta.transform, "Batente", estandarte, Color.white, junto, Vector2.one * 0.7f, 2);
                continue;
            }

            Vector2 posicao = eixo * (s * (MEIA_PORTA + LADO * 0.5f));
            FormasDaSala.Desenho(porta.transform, "Batente", FormasDaSala.Quadrado(), cor, posicao, Vector2.one * LADO, 2);
        }
    }
}
