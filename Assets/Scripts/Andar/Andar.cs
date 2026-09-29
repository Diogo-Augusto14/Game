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
    [Tooltip("1 = primeiro andar. Cada andar tem umas 3 salas a mais")]
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

    [Tooltip("A cada tantos andares, um inimigo a mais por sala (0 = nunca)")]
    [SerializeField, Min(0)] private int andaresPorInimigoExtra = 2;

    [Tooltip("Sentinelas no maximo por sala: sao paradas, em excesso a sala vira tiroteio")]
    [SerializeField, Min(0)] private int maximoDeSentinelas = 2;

    [Header("Variedade das salas")]
    [Tooltip("Chance de uma sala comum nao ter pedra nem espinho")]
    [SerializeField, Range(0f, 1f)] private float chanceDeSalaVazia = 0.25f;

    [Tooltip("Espinhos so aparecem a partir deste andar")]
    [SerializeField, Min(1)] private int espinhosAPartirDoAndar = 2;

    [Tooltip("Cor do chao de cada andar (o andar 4 volta pra primeira, e assim por diante)")]
    [SerializeField] private Color[] coresDoChao =
    {
        new Color(0.22f, 0.17f, 0.14f),   // porao: terra
        new Color(0.14f, 0.17f, 0.22f),   // cavernas: pedra azulada
        new Color(0.15f, 0.2f, 0.13f),    // esgoto: musgo
    };

    [Tooltip("Cor das paredes de cada andar, na mesma ordem do chao")]
    [SerializeField] private Color[] coresDaParede =
    {
        new Color(0.35f, 0.3f, 0.28f),
        new Color(0.28f, 0.32f, 0.4f),
        new Color(0.27f, 0.34f, 0.25f),
    };

    [Header("Itens e coletaveis")]
    [Tooltip("Chance de cada inimigo soltar coracao, moeda, bomba ou chave ao morrer")]
    [SerializeField, Range(0f, 1f)] private float chanceDeDropDoInimigo = 0.15f;

    [Tooltip("Chance de cair um premio no meio da sala quando ela e limpa")]
    [SerializeField, Range(0f, 1f)] private float chanceDePremioDaSala = 0.5f;

    [Tooltip("A partir deste andar a porta da sala do item fica trancada (precisa de chave)")]
    [SerializeField, Min(1)] private int trancarItemAPartirDoAndar = 2;

    [Header("Fim do andar")]
    [Tooltip("Distancia do centro da sala do chefe ate o alcapao, pro lado oposto da porta (o pedestal fica no centro)")]
    [SerializeField, Min(0f)] private float distanciaDoAlcapao = 2f;

    [Tooltip("O andar do chefe final. Vencer ele termina a partida (sem alcapao)")]
    [SerializeField, Min(1)] private int andarFinal = 4;

    [Tooltip("Mostra 'Andar N' no meio da tela a cada andar novo")]
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
    [SerializeField] private Color corDoBatenteDaLoja = new Color(0.4f, 0.85f, 0.45f);
    [SerializeField] private Color corDoBatenteDoItem = new Color(0.95f, 0.78f, 0.25f);
    [SerializeField] private Color corDoBatenteDoChefe = new Color(0.8f, 0.15f, 0.15f);

    // Teclas entre colchetes aparecem desenhadas na HUD (TelaSimples.LinhaDeTeclas); depois
    // do "||" vem a mesma linha com os botoes do controle.
    private const string CONTROLES =
        "[W][A][S][D] andar | [Cima][Esquerda][Baixo][Direita] atirar | [E] bomba | [Esc] pausa || " +
        "[Pad AnalogicoEsquerdo] andar | [Pad Y][Pad X][Pad A][Pad B] atirar | [Pad LB] bomba | [Pad Start] pausa";

    // ---------------- estado ----------------
    private Sala[,] noMundo;
    private readonly Dictionary<Sala, SalaDoAndar> salaDoMapa = new Dictionary<Sala, SalaDoAndar>();
    private Transform raizDasSalas;
    private Transform jogador;
    private Rigidbody2D corpoDoJogador;
    private Vida vidaDoJogador;
    private Camera cam;
    private Coroutine transicao;
    private Vector2 gravidadeAnterior;
    private bool mexeuNaGravidade;

    // Itens que ja apareceram nesta partida: o proximo pedestal sorteia outro.
    private readonly HashSet<ItemPassivo> itensQueJaSairam = new HashSet<ItemPassivo>();

    public static Andar Atual { get; private set; }

    public MapaDoAndar Mapa { get; private set; }

    public SalaDoAndar SalaAtual { get; private set; }

    public int NumeroDoAndar => numeroDoAndar;

    /// <summary>A semente que gerou este andar. Anote quando achar um andar com problema.</summary>
    public int SementeUsada { get; private set; }

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

    private void OnDestroy()
    {
        if (Atual == this)
            Atual = null;

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
    /// Desce pro proximo andar: mais salas, semente nova. O jogador continua o mesmo, com a
    /// vida, os itens e o inventario que tinha. O <see cref="Alcapao"/> chama isto.
    /// </summary>
    public void ProximoAndar()
    {
        if (vidaDoJogador != null && vidaDoJogador.EstaMorto)
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

        SementeUsada = semente != 0 ? semente : Environment.TickCount;
        Mapa = GeradorDeAndar.Gerar(numeroDoAndar, SementeUsada, larguraDaGrade, alturaDaGrade);

        // Mesma semente = mesmos inimigos nos mesmos lugares, nao so a mesma planta.
        UnityEngine.Random.InitState(SementeUsada);

        raizDasSalas = new GameObject("Salas").transform;
        raizDasSalas.SetParent(transform, false);
        noMundo = new Sala[Mapa.Largura, Mapa.Altura];
        salaDoMapa.Clear();

        foreach (SalaDoAndar sala in Mapa.Salas)
            noMundo[sala.X, sala.Y] = MontarSala(sala);

        Debug.Log($"[Andar] andar {numeroDoAndar}, {Mapa.Salas.Count} salas, semente {SementeUsada}");

        GarantirJogador();
        GarantirCamera();

        SalaAtual = null;
        AoGerar?.Invoke(Mapa);
        Entrar(Mapa.Inicio, null);

        if (avisarAndarNovo)
            AvisoDoAndar.Mostrar(UltimoAndar ? "Ultimo andar" : $"Andar {numeroDoAndar}");
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

        if (para != null)
            Entrar(para, porta.Lado);
    }

    /// <param name="saiuPor">Porta por onde o jogador saiu da sala anterior. Null = comeco do andar, fica no centro.</param>
    private void Entrar(SalaDoAndar sala, LadoDaPorta? saiuPor)
    {
        SalaAtual = sala;
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

        Musica.Tocar(sala.Tipo == TipoDeSala.Chefe && !destino.Limpa ? MusicaDoChefe : Musica.DoAndar(numeroDoAndar));

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

        // A sala (15x9 com as paredes) e mais estreita que a tela 16:9: cabendo a altura inteira,
        // sobrava meia unidade preta de cada lado. Agora cabe a LARGURA inteira e a parede de
        // cima e de baixo corta um pouco, como no Isaac. So nunca deixa de mostrar o chao todo.
        cam.orthographic = true;
        float cabeLargura = Passo.x * 0.5f / Mathf.Max(cam.aspect, 0.1f);
        float chaoInteiro = Sala.TamanhoPadrao.y * 0.5f + 0.5f;
        cam.orthographicSize = Mathf.Max(cabeLargura, chaoInteiro);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
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
        Sala sala = Sala.Criar($"Sala {casa.Tipo} ({casa.X},{casa.Y})", centro, portas, raizDasSalas);
        salaDoMapa[sala] = casa;

        if (coresDoChao.Length > 0 && coresDaParede.Length > 0)
            sala.Pintar(coresDoChao[(numeroDoAndar - 1) % coresDoChao.Length],
                        coresDaParede[(numeroDoAndar - 1) % coresDaParede.Length]);

        // So sala comum ganha obstaculo: inicio, item e chefe ficam com o chao livre.
        if (casa.Tipo == TipoDeSala.Normal)
            DisposicoesDaSala.Sortear(sala, chanceDeSalaVazia, numeroDoAndar >= espinhosAPartirDoAndar);

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

            MarcarPortaEspecial(porta, casa, vizinha);
        }

        PintarChao(sala, casa.Tipo);

        // Toda sala ganha tochas; loja e chefe ficam sem enfeite de chao (a loja tem as
        // mercadorias, o chefe precisa do chao todo).
        bool chaoLimpo = casa.Tipo == TipoDeSala.Loja || casa.Tipo == TipoDeSala.Chefe;
        sala.Enfeitar(chaoLimpo ? 0 : UnityEngine.Random.Range(enfeitesPorSala.x, enfeitesPorSala.y + 1));

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
                quantos = UnityEngine.Random.Range(inimigosPorSala.x, inimigosPorSala.y + 1);

                if (andaresPorInimigoExtra > 0)
                    quantos += Mathf.Min(2, (numeroDoAndar - 1) / andaresPorInimigoExtra);

                break;
            case TipoDeSala.Chefe:
                // No meio da sala, longe de todas as portas. O pedestal do premio nasce no
                // mesmo lugar quando ele morre (PorPremios). Andar par = o segundo chefe.
                sala.CriarInimigo(UltimoAndar ? TipoDeInimigo.ChefeFinal : ChefeDoNumero(numeroDoAndar), Vector2.zero);
                return;
            default:
                return; // inicio e item: sala tranquila, como no Isaac
        }

        int sentinelas = 0;

        for (int i = 0; i < quantos; i++)
        {
            TipoDeInimigo tipo = SortearInimigo(numeroDoAndar);

            if (tipo == TipoDeInimigo.Sentinela && ++sentinelas > maximoDeSentinelas)
                tipo = TipoDeInimigo.Perseguidor;

            InimigoDeSala inimigo = sala.CriarInimigo(tipo, PontoLongeDasPortas(sala));

            if (inimigo is InimigoSentinela sentinela)
                sentinela.UsarOitoDirecoes(numeroDoAndar >= 3);

            if (inimigo is InimigoDeSangue sangue && numeroDoAndar >= 3)
                sangue.Endurecer();

            if (inimigo != null && chanceDeDropDoInimigo > 0f)
                inimigo.gameObject.AddComponent<SoltaColetavel>().Configurar(chanceDeDropDoInimigo, sala.transform);
        }
    }

    /// <summary>Andar impar: o Monstrao. Andar par: o Sapao.</summary>
    public static TipoDeInimigo ChefeDoNumero(int andar)
    {
        return andar % 2 == 0 ? TipoDeInimigo.ChefeSaltador : TipoDeInimigo.Chefe;
    }

    /// <summary>
    /// Quem aparece em cada andar, com peso. Cada andar traz gente nova, pra o andar
    /// seguinte nao parecer o mesmo com mais salas:
    ///   1  -> perseguidor, atirador, investidor, divisor, demonio, monstro de sangue,
    ///         goblin da tocha, barril, esqueleto
    ///   2  -> + saltador, sentinela, goblin da dinamite, arqueiro, esqueleto da foice e
    ///         vampiro (e o perseguidor fica mais raro)
    ///   3+ -> todos, com mais investidor, divisor e sentinela
    /// </summary>
    private static TipoDeInimigo SortearInimigo(int andar)
    {
        (TipoDeInimigo tipo, float peso)[] tabela;

        if (andar <= 1)
        {
            tabela = new[]
            {
                (TipoDeInimigo.Perseguidor, 4f), (TipoDeInimigo.Atirador, 2.5f),
                (TipoDeInimigo.Investidor, 2f), (TipoDeInimigo.Divisor, 1.5f),
                (TipoDeInimigo.Demonio, 2f), (TipoDeInimigo.MonstroDeSangue, 1f),
                (TipoDeInimigo.GoblinTocha, 2f), (TipoDeInimigo.Barril, 1f),
                (TipoDeInimigo.Esqueleto, 2f), (TipoDeInimigo.Morcego, 1.5f),
                (TipoDeInimigo.DemonioTridente, 1f),
                (TipoDeInimigo.Orc, 2f), (TipoDeInimigo.EsqueletoGuerreiro, 2f), (TipoDeInimigo.Geleia, 1.5f),
                (TipoDeInimigo.Morceguinho, 1.5f),
            };
        }
        else if (andar == 2)
        {
            tabela = new[]
            {
                (TipoDeInimigo.Perseguidor, 2f), (TipoDeInimigo.Atirador, 2f),
                (TipoDeInimigo.Investidor, 1.5f), (TipoDeInimigo.Divisor, 1.5f),
                (TipoDeInimigo.Saltador, 3f), (TipoDeInimigo.Sentinela, 1.5f),
                (TipoDeInimigo.Demonio, 2f), (TipoDeInimigo.MonstroDeSangue, 1.5f),
                (TipoDeInimigo.GoblinTocha, 2f), (TipoDeInimigo.Barril, 1.5f),
                (TipoDeInimigo.GoblinDinamite, 1.5f), (TipoDeInimigo.Arqueiro, 1.5f),
                (TipoDeInimigo.Esqueleto, 2f), (TipoDeInimigo.EsqueletoFoice, 1f), (TipoDeInimigo.Vampiro, 1f),
                (TipoDeInimigo.Morcego, 1.5f), (TipoDeInimigo.DemonioTridente, 1.5f), (TipoDeInimigo.CavaleiroEscudo, 1f),
                (TipoDeInimigo.DemonioArqueiro, 1f), (TipoDeInimigo.Demonia, 1f), (TipoDeInimigo.FogoFatuo, 1f),
                (TipoDeInimigo.Orc, 1.5f), (TipoDeInimigo.OrcBlindado, 1f), (TipoDeInimigo.OrcMontado, 1f),
                (TipoDeInimigo.EsqueletoGuerreiro, 1.5f), (TipoDeInimigo.EsqueletoBlindado, 1f), (TipoDeInimigo.EsqueletoArqueiro, 1.5f),
                (TipoDeInimigo.Geleia, 1.5f), (TipoDeInimigo.Morceguinho, 1.5f), (TipoDeInimigo.Lobisomem, 1f), (TipoDeInimigo.Necromante, 1f),
            };
        }
        else
        {
            tabela = new[]
            {
                (TipoDeInimigo.Perseguidor, 1.5f), (TipoDeInimigo.Atirador, 2f),
                (TipoDeInimigo.Investidor, 2.5f), (TipoDeInimigo.Divisor, 2f),
                (TipoDeInimigo.Saltador, 2f), (TipoDeInimigo.Sentinela, 2f),
                (TipoDeInimigo.Demonio, 2.5f), (TipoDeInimigo.MonstroDeSangue, 2f),
                (TipoDeInimigo.GoblinTocha, 2f), (TipoDeInimigo.Barril, 2f),
                (TipoDeInimigo.GoblinDinamite, 2f), (TipoDeInimigo.Arqueiro, 2f),
                (TipoDeInimigo.Esqueleto, 2f), (TipoDeInimigo.EsqueletoFoice, 2f), (TipoDeInimigo.Vampiro, 2f),
                (TipoDeInimigo.Morcego, 1.5f), (TipoDeInimigo.DemonioTridente, 1.5f), (TipoDeInimigo.CavaleiroEscudo, 1.5f),
                (TipoDeInimigo.CavaleiroLanca, 1.5f), (TipoDeInimigo.DemonioLaminas, 1.5f), (TipoDeInimigo.DemoniaFoice, 1.5f),
                (TipoDeInimigo.DemonioArqueiro, 1.5f), (TipoDeInimigo.Demonia, 1.5f), (TipoDeInimigo.FogoFatuo, 1.5f),
                (TipoDeInimigo.Orc, 1f), (TipoDeInimigo.OrcBlindado, 1.5f), (TipoDeInimigo.OrcElite, 1.5f), (TipoDeInimigo.OrcMontado, 1.5f),
                (TipoDeInimigo.EsqueletoBlindado, 1.5f), (TipoDeInimigo.EsqueletoEspadao, 1.5f), (TipoDeInimigo.EsqueletoArqueiro, 1.5f),
                (TipoDeInimigo.Lobisomem, 1.5f), (TipoDeInimigo.Urso, 1f), (TipoDeInimigo.Necromante, 1.5f),
            };
        }

        float total = 0f;
        foreach (var linha in tabela)
            total += linha.peso;

        float sorteio = UnityEngine.Random.value * total;

        foreach (var linha in tabela)
        {
            sorteio -= linha.peso;

            if (sorteio <= 0f)
                return linha.tipo;
        }

        return tabela[tabela.Length - 1].tipo;
    }

    // ---------------- itens e coletaveis ----------------
    /// <summary>Letreiro no meio da tela pra cada heroi que o chefe liberou.</summary>
    private static void AnunciarLiberados(List<Herois.Heroi> liberados)
    {
        if (liberados.Count == 0)
            return;

        List<string> nomes = liberados.ConvertAll(h => h.Nome);
        AvisoDoAndar.Mostrar("Heroi liberado: " + string.Join(", ", nomes) + "!");
        Sons.Tocar(Som.Aviso);
    }

    /// <summary>
    /// O que cada tipo de sala guarda, como no Isaac:
    ///   Item   -> pedestal com um item passivo no meio (porta trancada a partir do andar 2)
    ///   Chefe  -> pedestal com item quando a sala e limpa
    ///   Normal -> chance de um coletavel no meio quando a sala e limpa
    ///   Inicio -> uma chave de brinde nos andares com sala do item trancada
    /// </summary>
    private void PorPremios(Sala sala, SalaDoAndar casa)
    {
        Vector2 centro = sala.transform.position;

        switch (casa.Tipo)
        {
            case TipoDeSala.Item:
                Pedestal.Criar(CatalogoDeItens.Sortear(itensQueJaSairam), centro, sala.transform);
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

                ItemPassivo doChefe = CatalogoDeItens.Sortear(itensQueJaSairam);
                sala.AoLimpar.AddListener(() =>
                {
                    Pedestal.Criar(doChefe, centro, sala.transform);
                    Alcapao.Criar(centro + LongeDaPorta(sala, distanciaDoAlcapao), sala.transform);
                    Musica.Tocar(Musica.DoAndar(numeroDoAndar));
                    AnunciarLiberados(Progresso.VenceuChefe(numeroDoAndar));
                });
                break;

            case TipoDeSala.Normal:
                sala.AoLimpar.AddListener(() => TabelaDeDrops.TalvezSoltar(chanceDePremioDaSala, centro, sala.transform));
                break;

            case TipoDeSala.Loja:
                Loja.Montar(sala.transform, itensQueJaSairam);
                break;

            case TipoDeSala.Secreta:
                PorTesouroSecreto(sala);
                break;

            case TipoDeSala.Inicio:
                if (ItemTrancado)
                    Coletavel.Criar(TipoDeColetavel.Chave, centro + Vector2.down * 1.5f, sala.transform);
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

    private TemaMusical MusicaDoChefe => UltimoAndar ? TemaMusical.ChefeFinal : TemaMusical.Chefe;

    /// <summary>O andar do chefe final: vencer ele termina a partida.</summary>
    public bool UltimoAndar => numeroDoAndar >= andarFinal;

    public int AndarFinal => andarFinal;

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
            default: return;
        }

        Transform chao = sala.transform.Find("Cenario/Chao");

        // Os ladrilhos do pacote ja tem cor: o tom da sala especial entra pela metade.
        if (ArteGerada.CenarioDoPacote)
            cor = Color.Lerp(Color.white, cor, 0.5f);

        if (chao != null && chao.TryGetComponent(out SpriteRenderer sr))
            sr.color = cor;
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
        else
            return;

        Vector2 eixo = porta.Lado.Horizontal() ? Vector2.right : Vector2.up;
        const float MEIA_PORTA = 0.75f;
        const float LADO = 0.4f;

        // Com o pacote: caveira no chefe, estandarte vermelho na loja e azul no item.
        Sprite estandarte = cor == corDoBatenteDoChefe ? ArteImportada.Objeto(2, 3)
            : cor == corDoBatenteDaLoja ? ArteImportada.Objeto(1, 3)
            : ArteImportada.Objeto(1, 4);

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
