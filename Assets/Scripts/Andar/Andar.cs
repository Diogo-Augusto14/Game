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
/// Pra usar: um objeto vazio com este componente numa cena sem o Bootstrap de plataforma.
/// O jeito mais rapido e <c>Tools ▸ Jogo ▸ Andar ▸ Criar cena de teste do andar</c>.
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

    [Header("Inimigos")]
    [Tooltip("Inimigos numa sala comum: sorteado entre o minimo e o maximo")]
    [SerializeField] private Vector2Int inimigosPorSala = new Vector2Int(2, 4);

    [Tooltip("Inimigo nao nasce mais perto que isto de uma porta")]
    [SerializeField, Min(0f)] private float distanciaDasPortas = 3f;

    [Header("Itens e coletaveis")]
    [Tooltip("Chance de cada inimigo soltar coracao, moeda, bomba ou chave ao morrer")]
    [SerializeField, Range(0f, 1f)] private float chanceDeDropDoInimigo = 0.15f;

    [Tooltip("Chance de cair um premio no meio da sala quando ela e limpa")]
    [SerializeField, Range(0f, 1f)] private float chanceDePremioDaSala = 0.5f;

    [Tooltip("A partir deste andar a porta da sala do item fica trancada (precisa de chave)")]
    [SerializeField, Min(1)] private int trancarItemAPartirDoAndar = 2;

    [Header("Jogador")]
    [Tooltip("Sem ninguem com a tag Player na cena, monta o jogador top-down (WASD anda, setas atiram)")]
    [SerializeField] private bool criarJogador = true;

    [SerializeField] private Color corDoJogador = new Color(1f, 0.85f, 0.75f);

    [Tooltip("Monta a barra de vida se a cena nao tiver HUD")]
    [SerializeField] private bool montarHud = true;

    [Header("Camera")]
    [Tooltip("Forca ortografica, com zoom pra caber uma sala inteira, e desliga o Cameramov")]
    [SerializeField] private bool ajustarCamera = true;

    [SerializeField, Min(0f)] private float tempoDaTransicao = 0.3f;

    [Header("Minimapa")]
    [SerializeField] private bool mostrarMinimapa = true;

    [Header("Cores")]
    [SerializeField] private Color corDoChaoDoItem = new Color(0.32f, 0.28f, 0.14f);
    [SerializeField] private Color corDoChaoDoChefe = new Color(0.32f, 0.14f, 0.14f);
    [SerializeField] private Color corDoBatenteDoItem = new Color(0.95f, 0.78f, 0.25f);
    [SerializeField] private Color corDoBatenteDoChefe = new Color(0.8f, 0.15f, 0.15f);

    private const string CONTROLES =
        "W A S D  andar    Setas  atirar    E  bomba\n" +
        "Limpe a sala pra abrir as portas";

    // ---------------- estado ----------------
    private Sala[,] noMundo;
    private readonly Dictionary<Sala, SalaDoAndar> salaDoMapa = new Dictionary<Sala, SalaDoAndar>();
    private Transform raizDasSalas;
    private Transform jogador;
    private Rigidbody2D corpoDoJogador;
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
    }

    private void Start()
    {
        Gerar();
    }

    private void OnDestroy()
    {
        if (Atual == this)
            Atual = null;

        if (mexeuNaGravidade)
            Physics2D.gravity = gravidadeAnterior;
    }

    // ---------------- api ----------------
    /// <summary>Desce pro proximo andar: mais salas, semente nova.</summary>
    public void ProximoAndar()
    {
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
            Destroy(raizDasSalas.gameObject);

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

        Cameramov seguidora = cam.GetComponent<Cameramov>();

        if (seguidora != null)
            seguidora.enabled = false;

        // Cabe a sala inteira, paredes incluidas, na altura e na largura.
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(Passo.y * 0.5f, Passo.x * 0.5f / Mathf.Max(cam.aspect, 0.1f));
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

        if (encontrado.GetComponent<Inventario>() == null)
            encontrado.AddComponent<Inventario>();

        if (encontrado.GetComponent<EstatisticasDoJogador>() == null)
            encontrado.AddComponent<EstatisticasDoJogador>();

        if (montarHud && FindAnyObjectByType<Hud>() == null)
            new GameObject("Hud").AddComponent<Hud>().Configurar(encontrado.GetComponent<Vida>(), null, CONTROLES);

        if (montarHud && FindAnyObjectByType<HudDoInventario>() == null)
            HudDoInventario.Criar(encontrado);
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

        foreach (Porta porta in sala.Portas)
        {
            porta.AoAtravessar.AddListener(AoAtravessar);

            if (porta.Existe)
                MarcarPortaEspecial(porta, casa, Mapa.Vizinha(casa, ParaDirecao(porta.Lado)));
        }

        PintarChao(sala, casa.Tipo);
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
                break;
            case TipoDeSala.Chefe:
                // No meio da sala, longe de todas as portas. O pedestal do premio nasce no
                // mesmo lugar quando ele morre (PorPremios).
                sala.CriarInimigo(TipoDeInimigo.Chefe, Vector2.zero);
                return;
            default:
                return; // inicio e item: sala tranquila, como no Isaac
        }

        for (int i = 0; i < quantos; i++)
        {
            TipoDeInimigo tipo = UnityEngine.Random.value < 0.35f ? TipoDeInimigo.Atirador : TipoDeInimigo.Perseguidor;
            InimigoDeSala inimigo = sala.CriarInimigo(tipo, PontoLongeDasPortas(sala));

            if (inimigo != null && chanceDeDropDoInimigo > 0f)
                inimigo.gameObject.AddComponent<SoltaColetavel>().Configurar(chanceDeDropDoInimigo, sala.transform);
        }
    }

    // ---------------- itens e coletaveis ----------------
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
                ItemPassivo doChefe = CatalogoDeItens.Sortear(itensQueJaSairam);
                sala.AoLimpar.AddListener(() => Pedestal.Criar(doChefe, centro, sala.transform));
                break;

            case TipoDeSala.Normal:
                sala.AoLimpar.AddListener(() => TabelaDeDrops.TalvezSoltar(chanceDePremioDaSala, centro, sala.transform));
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

    private bool ItemTrancado => numeroDoAndar >= trancarItemAPartirDoAndar;

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
        if (tipo != TipoDeSala.Item && tipo != TipoDeSala.Chefe)
            return;

        Transform chao = sala.transform.Find("Cenario/Chao");

        if (chao != null && chao.TryGetComponent(out SpriteRenderer sr))
            sr.color = tipo == TipoDeSala.Item ? corDoChaoDoItem : corDoChaoDoChefe;
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
        else
            return;

        Vector2 eixo = porta.Lado.Horizontal() ? Vector2.right : Vector2.up;
        const float MEIA_PORTA = 0.75f;
        const float LADO = 0.4f;

        for (int s = -1; s <= 1; s += 2)
        {
            Vector2 posicao = eixo * (s * (MEIA_PORTA + LADO * 0.5f));
            FormasDaSala.Desenho(porta.transform, "Batente", FormasDaSala.Quadrado(), cor, posicao, Vector2.one * LADO, 2);
        }
    }
}
