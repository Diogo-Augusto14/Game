using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Monta um andar inteiro no estilo do Isaac: sorteia a grade de salas
/// (<see cref="GeradorDeAndar"/>), constroi cada sala no mundo com chao, paredes e portas,
/// poe o jogador na sala inicial e cuida da troca de sala e da camera.
///
/// COMO AS SALAS FICAM NO MUNDO: cada casa da grade vira uma sala de verdade, lado a lado,
/// com as paredes coladas. Onde ha porta, as duas paredes tem um vao alinhado, entao o
/// jogador atravessa andando mesmo, sem trigger nenhum. O Andar so percebe que o jogador
/// cruzou a linha do meio do vao, empurra ele pra dentro da sala nova (como o Isaac faz)
/// e desliza a camera ate o centro dela.
///
/// Pra usar: um objeto vazio com este componente numa cena sem o Bootstrap de plataforma.
/// O jeito mais rapido e <c>Tools ▸ Jogo ▸ Andar ▸ Criar cena de teste do andar</c>.
/// Se a cena nao tiver ninguem com a tag Player, ele cria um jogador de teste (WASD).
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

    [Header("Tamanho da sala (unidades)")]
    [Tooltip("Chao da sala, sem as paredes. O Isaac usa 13 x 7")]
    [SerializeField] private Vector2 interior = new Vector2(13f, 7f);

    [SerializeField, Min(0.25f)] private float espessuraDaParede = 1f;

    [SerializeField, Min(0.5f)] private float larguraDaPorta = 1.5f;

    [Tooltip("Ao trocar de sala, o jogador aparece esta distancia pra dentro da porta")]
    [SerializeField, Min(0f)] private float recuoAoEntrar = 0.8f;

    [Header("Jogador")]
    [Tooltip("Sem ninguem com a tag Player na cena, cria um jogador simples de teste (WASD)")]
    [SerializeField] private bool criarJogadorDeTeste = true;

    [Header("Camera")]
    [Tooltip("Forca ortografica, com zoom pra caber uma sala inteira, e desliga o Cameramov")]
    [SerializeField] private bool ajustarCamera = true;

    [SerializeField, Min(0f)] private float tempoDaTransicao = 0.3f;

    [Header("Minimapa")]
    [SerializeField] private bool mostrarMinimapa = true;

    [Header("Cores")]
    [SerializeField] private Color corDoChao = new Color(0.27f, 0.22f, 0.18f);
    [SerializeField] private Color corDoChaoDoItem = new Color(0.32f, 0.28f, 0.14f);
    [SerializeField] private Color corDoChaoDoChefe = new Color(0.32f, 0.14f, 0.14f);
    [SerializeField] private Color corDaParede = new Color(0.45f, 0.38f, 0.32f);
    [SerializeField] private Color corDaPorta = new Color(0.12f, 0.08f, 0.06f);
    [SerializeField] private Color corDaPortaDoItem = new Color(0.95f, 0.78f, 0.25f);
    [SerializeField] private Color corDaPortaDoChefe = new Color(0.8f, 0.15f, 0.15f);

    // ---------------- estado ----------------
    private SalaNoMundo[,] noMundo;
    private Transform raizDasSalas;
    private Transform jogador;
    private Rigidbody2D corpoDoJogador;
    private Camera cam;
    private Coroutine transicao;

    public static Andar Atual { get; private set; }

    public MapaDoAndar Mapa { get; private set; }

    public SalaDoAndar SalaAtual { get; private set; }

    public int NumeroDoAndar => numeroDoAndar;

    /// <summary>A semente que gerou este andar. Anote quando achar um andar com problema.</summary>
    public int SementeUsada { get; private set; }

    /// <summary>Distancia entre o centro de duas salas vizinhas.</summary>
    public Vector2 Passo => interior + Vector2.one * (2f * espessuraDaParede);

    /// <summary>Um andar novo foi montado (no Play e a cada <see cref="ProximoAndar"/>).</summary>
    public event Action<MapaDoAndar> AoGerar;

    /// <summary>
    /// O jogador entrou numa sala. E aqui que a sala com inimigos se pendura: tranca as
    /// portas com <see cref="SalaNoMundo.Trancar"/> e destranca quando limpar.
    /// </summary>
    public event Action<SalaNoMundo> AoEntrarNaSala;

    // ---------------- ciclo de vida ----------------
    private void Awake()
    {
        Atual = this;

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
    }

    private void LateUpdate()
    {
        if (jogador == null || SalaAtual == null)
            return;

        SalaDoAndar sala = SalaNaPosicao(jogador.position);

        if (sala == null || sala == SalaAtual)
            return;

        Entrar(sala, DirecaoEntre(SalaAtual, sala));
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
    public SalaNoMundo NoMundo(SalaDoAndar sala) => sala != null ? noMundo[sala.X, sala.Y] : null;

    /// <summary>Sorteia e monta o andar do zero, apagando o anterior.</summary>
    public void Gerar()
    {
        if (raizDasSalas != null)
            Destroy(raizDasSalas.gameObject);

        SementeUsada = semente != 0 ? semente : Environment.TickCount;
        Mapa = GeradorDeAndar.Gerar(numeroDoAndar, SementeUsada, larguraDaGrade, alturaDaGrade);

        raizDasSalas = new GameObject("Salas").transform;
        raizDasSalas.SetParent(transform, false);
        noMundo = new SalaNoMundo[Mapa.Largura, Mapa.Altura];

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
    /// <param name="andouPara">Direcao em que o jogador andou pra chegar aqui. Null = teleporte, fica no centro.</param>
    private void Entrar(SalaDoAndar sala, Direcao? andouPara)
    {
        SalaAtual = sala;
        Mapa.Visitar(sala);
        SalaNoMundo destino = NoMundo(sala);

        if (jogador != null)
        {
            Vector2 ponto = andouPara.HasValue
                ? destino.PontoNaPorta(Direcoes.Oposta(andouPara.Value), recuoAoEntrar)
                : destino.Centro;

            Teleportar(ponto);
        }

        MoverCamera(destino.Centro, andouPara.HasValue ? tempoDaTransicao : 0f);
        AoEntrarNaSala?.Invoke(destino);
    }

    private void Teleportar(Vector2 ponto)
    {
        if (corpoDoJogador != null)
            corpoDoJogador.position = ponto;

        jogador.position = new Vector3(ponto.x, ponto.y, jogador.position.z);
    }

    private SalaDoAndar SalaNaPosicao(Vector2 posicao)
    {
        Vector2 local = posicao - (Vector2)transform.position;
        int x = Mathf.RoundToInt(local.x / Passo.x);
        int y = Mathf.RoundToInt(local.y / Passo.y);
        return Mapa.Em(x, y);
    }

    private static Direcao? DirecaoEntre(SalaDoAndar de, SalaDoAndar para)
    {
        foreach (Direcao d in Direcoes.Todas)
            if (de.X + Direcoes.Dx(d) == para.X && de.Y + Direcoes.Dy(d) == para.Y)
                return d;

        return null; // nao sao vizinhas: foi teleporte
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
        Vector2 sala = Passo;
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(sala.y * 0.5f, sala.x * 0.5f / Mathf.Max(cam.aspect, 0.1f));
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

        if (encontrado == null && criarJogadorDeTeste)
            encontrado = JogadorDeTeste.Criar();

        if (encontrado == null)
        {
            Debug.LogWarning("[Andar] nenhum objeto com a tag Player. O andar foi montado, mas ninguem anda nele.");
            return;
        }

        jogador = encontrado.transform;
        corpoDoJogador = encontrado.GetComponent<Rigidbody2D>();
    }

    // ---------------- montagem das salas ----------------
    private SalaNoMundo MontarSala(SalaDoAndar sala)
    {
        GameObject raiz = new GameObject($"Sala {sala.Tipo} ({sala.X},{sala.Y})");
        raiz.transform.SetParent(raizDasSalas, false);
        raiz.transform.localPosition = new Vector3(sala.X * Passo.x, sala.Y * Passo.y, 0f);

        SalaNoMundo componente = raiz.AddComponent<SalaNoMundo>();
        componente.Iniciar(sala, interior);

        Bloco(raiz.transform, "Chao", Vector2.zero, interior, CorDoChao(sala.Tipo), -10, false);

        foreach (Direcao d in Direcoes.Todas)
            MontarParede(raiz.transform, componente, sala, d);

        return componente;
    }

    /// <summary>
    /// Uma parede inteira ou, se tiver porta, dois pedacos com o vao no meio. As paredes de
    /// cima e de baixo vao de quina a quina; as laterais cobrem so a altura do chao.
    /// </summary>
    private void MontarParede(Transform pai, SalaNoMundo componente, SalaDoAndar sala, Direcao lado)
    {
        bool horizontal = lado == Direcao.Cima || lado == Direcao.Baixo;
        float t = espessuraDaParede;
        float comprimento = horizontal ? interior.x + 2f * t : interior.y;

        Vector2 normal = new Vector2(Direcoes.Dx(lado), Direcoes.Dy(lado));
        Vector2 eixo = horizontal ? Vector2.right : Vector2.up;
        Vector2 meio = normal * ((horizontal ? interior.y : interior.x) * 0.5f + t * 0.5f);

        SalaDoAndar vizinha = Mapa.Vizinha(sala, lado);

        if (vizinha == null)
        {
            Bloco(pai, $"Parede {lado}", meio, Tamanho(horizontal, comprimento), corDaParede, 0, true);
            return;
        }

        float pedaco = (comprimento - larguraDaPorta) * 0.5f;
        float desvio = larguraDaPorta * 0.5f + pedaco * 0.5f;
        Bloco(pai, $"Parede {lado} A", meio - eixo * desvio, Tamanho(horizontal, pedaco), corDaParede, 0, true);
        Bloco(pai, $"Parede {lado} B", meio + eixo * desvio, Tamanho(horizontal, pedaco), corDaParede, 0, true);

        // O vao da porta: desenho colorido pelo tipo da sala do outro lado + tranca desligada.
        SpriteRenderer porta = Bloco(pai, $"Porta {lado}", meio, Tamanho(horizontal, larguraDaPorta),
                                     CorDaPorta(sala, vizinha), -5, true);
        BoxCollider2D tranca = porta.GetComponent<BoxCollider2D>();
        tranca.enabled = false;
        componente.RegistrarPorta(lado, tranca, porta);
    }

    private Vector2 Tamanho(bool horizontal, float comprimento)
    {
        return horizontal ? new Vector2(comprimento, espessuraDaParede) : new Vector2(espessuraDaParede, comprimento);
    }

    private static SpriteRenderer Bloco(Transform pai, string nome, Vector2 posicao, Vector2 tamanho,
                                        Color cor, int ordem, bool solido)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(pai, false);
        obj.transform.localPosition = posicao;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = Construtor.SpriteDeBloco();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = tamanho;
        sr.color = cor;
        sr.sortingOrder = ordem;

        if (solido)
            obj.AddComponent<BoxCollider2D>().size = tamanho;

        return sr;
    }

    private Color CorDoChao(TipoDeSala tipo)
    {
        switch (tipo)
        {
            case TipoDeSala.Item: return corDoChaoDoItem;
            case TipoDeSala.Chefe: return corDoChaoDoChefe;
            default: return corDoChao;
        }
    }

    /// <summary>A porta avisa o que tem do outro lado — ou o que e a sala onde voce esta.</summary>
    private Color CorDaPorta(SalaDoAndar sala, SalaDoAndar vizinha)
    {
        if (sala.Tipo == TipoDeSala.Chefe || vizinha.Tipo == TipoDeSala.Chefe)
            return corDaPortaDoChefe;

        if (sala.Tipo == TipoDeSala.Item || vizinha.Tipo == TipoDeSala.Item)
            return corDaPortaDoItem;

        return corDaPorta;
    }
}
