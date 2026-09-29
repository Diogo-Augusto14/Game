using UnityEngine;

/// <summary>
/// Monta a cena top-down ao apertar Play: uma sala no formato do Isaac (paredes, algumas
/// pedras), o jogador com movimento em 8 direcoes e tiro de lagrimas, bonecos de treino
/// pra testar o dano, a camera parada enquadrando a sala e a HUD.
///
/// E o irmao do <see cref="Bootstrap"/> do plataforma, com a mesma ideia: a cena so
/// precisa ter este componente, e o resto e montado por codigo, sem prefab nem arte.
/// Estando na cena, ele tambem impede o Bootstrap do plataforma de se instalar.
///
/// Gravidade: zerada no mundo enquanto esta cena roda (e devolvida ao sair), e cada
/// corpo daqui ainda usa gravityScale 0 — visto de cima nada cai.
/// </summary>
[DisallowMultipleComponent]
public class BootstrapTopDown : MonoBehaviour
{
    [Header("Chave mestra")]
    [Tooltip("Desmarcado = nao monta nada")]
    [SerializeField] private bool ativo = true;

    [Header("Sala")]
    [Tooltip("Tamanho do chao andavel, em unidades. O Isaac usa 13 x 7")]
    [SerializeField] private Vector2Int tamanhoDaSala = new Vector2Int(13, 7);

    [Tooltip("Espessura das paredes, em unidades")]
    [SerializeField, Min(0.1f)] private float espessuraDaParede = 1f;

    [Tooltip("Coloca algumas pedras no meio da sala (bloqueiam andar e lagrima)")]
    [SerializeField] private bool colocarPedras = true;

    [Tooltip("Quantos bonecos de treino colocar pra testar o tiro")]
    [SerializeField, Min(0)] private int alvosDeTreino = 3;

    [Header("Cores")]
    [SerializeField] private Color corDoChao = new Color(0.30f, 0.24f, 0.20f);
    [SerializeField] private Color corDaParede = new Color(0.18f, 0.14f, 0.12f);
    [SerializeField] private Color corDaPedra = new Color(0.45f, 0.42f, 0.40f);
    [SerializeField] private Color corDoJogador = new Color(1f, 0.85f, 0.75f);
    [SerializeField] private Color corDoAlvo = new Color(0.75f, 0.35f, 0.30f);

    [Header("Outros")]
    [SerializeField] private bool montarHud = true;

    [Tooltip("Ajusta a camera principal pra enquadrar a sala inteira")]
    [SerializeField] private bool ajustarCamera = true;

    private const string CONTROLES =
        "W A S D  andar (8 direcoes)\n" +
        "Setas  atirar (cima, baixo, esquerda, direita)\n" +
        "Da pra andar pra um lado e atirar pro outro";

    private Vector2 gravidadeAnterior;
    private bool mexeuNaGravidade;

    /// <summary>O jogador montado por este bootstrap (null antes do Awake).</summary>
    public GameObject Jogador { get; private set; }

    // ================================================================ ciclo de vida
    private void Awake()
    {
        if (!ativo)
            return;

        gravidadeAnterior = Physics2D.gravity;
        Physics2D.gravity = Vector2.zero;
        mexeuNaGravidade = true;

        Transform sala = new GameObject("Sala").transform;

        MontarChaoEParedes(sala);

        if (colocarPedras)
            MontarPedras(sala);

        Jogador = MontarJogador(new Vector2(0f, -tamanhoDaSala.y * 0.5f + 1.5f));

        MontarAlvos();

        if (ajustarCamera)
            MontarCamera();

        if (montarHud)
            MontarHud();
    }

    private void OnDestroy()
    {
        // A gravidade e global: devolve a que estava pra nao vazar pra outra cena.
        if (mexeuNaGravidade)
            Physics2D.gravity = gravidadeAnterior;
    }

    // ================================================================ sala
    private void MontarChaoEParedes(Transform sala)
    {
        float largura = tamanhoDaSala.x;
        float altura = tamanhoDaSala.y;
        float e = espessuraDaParede;

        // Chao: so desenho, sem colisao.
        Retangulo("Chao", sala, Vector2.zero, new Vector2(largura, altura), corDoChao, -10, false);

        // Quatro paredes em volta do chao andavel.
        Retangulo("Parede (cima)", sala, new Vector2(0f, (altura + e) * 0.5f), new Vector2(largura + 2f * e, e), corDaParede, -5, true);
        Retangulo("Parede (baixo)", sala, new Vector2(0f, -(altura + e) * 0.5f), new Vector2(largura + 2f * e, e), corDaParede, -5, true);
        Retangulo("Parede (esquerda)", sala, new Vector2(-(largura + e) * 0.5f, 0f), new Vector2(e, altura), corDaParede, -5, true);
        Retangulo("Parede (direita)", sala, new Vector2((largura + e) * 0.5f, 0f), new Vector2(e, altura), corDaParede, -5, true);
    }

    private void MontarPedras(Transform sala)
    {
        // Posicoes na grade da sala (centro = 0,0). Longe do nascimento e dos alvos.
        Vector2[] pedras =
        {
            new Vector2(-4f, 1f),
            new Vector2(-4f, 0f),
            new Vector2(4f, 1f),
            new Vector2(4f, 0f)
        };

        for (int i = 0; i < pedras.Length; i++)
            Retangulo($"Pedra {i}", sala, pedras[i], Vector2.one * 0.9f, corDaPedra, -4, true);
    }

    private static GameObject Retangulo(string nome, Transform pai, Vector2 posicao, Vector2 tamanho, Color cor, int ordem, bool solido)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(pai, false);
        obj.transform.position = posicao;
        obj.transform.localScale = new Vector3(tamanho.x, tamanho.y, 1f);

        SpriteRenderer desenho = obj.AddComponent<SpriteRenderer>();
        desenho.sprite = FormasTopDown.Quadrado();
        desenho.color = cor;
        desenho.sortingOrder = ordem;

        // BoxCollider2D pega o tamanho do sprite (1x1) e a escala faz o resto.
        if (solido)
        {
            obj.AddComponent<BoxCollider2D>();
            Camadas.Definir(obj, "Wall");
        }

        return obj;
    }

    // ================================================================ jogador
    private GameObject MontarJogador(Vector2 posicao) => CriarJogador(posicao, corDoJogador);

    /// <summary>
    /// Monta o jogador top-down completo (movimento, vida e tiro). Publico pra outras cenas
    /// montadas por codigo, como o andar, usarem o mesmo boneco.
    /// </summary>
    public static GameObject CriarJogador(Vector2 posicao, Color corDoJogador)
    {
        GameObject raiz = new GameObject("Jogador");
        raiz.transform.position = posicao;
        raiz.transform.localScale = Vector3.one * 0.8f;
        raiz.tag = "Player";
        Camadas.Definir(raiz, Camadas.Jogador);

        // O desenho fica na raiz: o Vida pisca o primeiro SpriteRenderer que achar.
        SpriteRenderer corpo = raiz.AddComponent<SpriteRenderer>();
        corpo.sprite = ArteGerada.Jogador(corDoJogador);
        corpo.color = Color.white;
        corpo.sortingOrder = 10;

        // Ordem importa: cada Awake procura quem veio antes.
        // Rigidbody2D -> colisor -> Entrada -> MovimentoTopDown -> Vida -> Atirador.
        raiz.AddComponent<Rigidbody2D>();

        CircleCollider2D colisor = raiz.AddComponent<CircleCollider2D>();
        colisor.radius = 0.4f;

        Entrada entrada = raiz.AddComponent<Entrada>();
        entrada.ModoTopDown = true;

        raiz.AddComponent<MovimentoTopDown>();

        Vida vida = raiz.AddComponent<Vida>();
        vida.Configurar(100f, 0f, false, 0f, true);
        vida.UsarEmpurraoTopDown();

        AtiradorTopDown atirador = raiz.AddComponent<AtiradorTopDown>();

        // Olho: bolinha escura que mostra pra onde o boneco olha / atira.
        GameObject olho = new GameObject("Olho");
        olho.transform.SetParent(raiz.transform, false);
        olho.transform.localScale = Vector3.one * 0.3f;

        SpriteRenderer desenhoDoOlho = olho.AddComponent<SpriteRenderer>();
        desenhoDoOlho.sprite = FormasTopDown.Circulo();
        desenhoDoOlho.color = new Color(0.15f, 0.1f, 0.1f);
        desenhoDoOlho.sortingOrder = 11;

        // O rosto ja tem olhos: a bolinha so marca a mira, entao fica escondida.
        desenhoDoOlho.enabled = false;

        atirador.DefinirOlho(olho.transform);

        return raiz;
    }

    // ================================================================ alvos
    private void MontarAlvos()
    {
        float y = tamanhoDaSala.y * 0.5f - 1.5f;

        for (int i = 0; i < alvosDeTreino; i++)
        {
            // Espalha em linha no alto da sala.
            float t = alvosDeTreino == 1 ? 0.5f : i / (float)(alvosDeTreino - 1);
            float x = Mathf.Lerp(-2.5f, 2.5f, t);
            MontarAlvo(new Vector2(x, y), i);
        }
    }

    private void MontarAlvo(Vector2 posicao, int indice)
    {
        GameObject raiz = new GameObject($"Alvo de treino {indice}");
        raiz.transform.position = posicao;
        Camadas.Definir(raiz, Camadas.Inimigo);

        SpriteRenderer corpo = raiz.AddComponent<SpriteRenderer>();
        corpo.sprite = FormasTopDown.Circulo();
        corpo.color = corDoAlvo;
        corpo.sortingOrder = 9;

        Rigidbody2D rb = raiz.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearDamping = 8f;
        rb.mass = 2f;

        CircleCollider2D colisor = raiz.AddComponent<CircleCollider2D>();
        colisor.radius = 0.45f;

        Vida vida = raiz.AddComponent<Vida>();
        vida.Configurar(20f, 0f, false, 0f, false);
        vida.UsarEmpurraoTopDown();
        vida.DefinirInvencibilidade(0f); // toda lagrima conta, como nos inimigos do Isaac

        raiz.AddComponent<AlvoDeTreino>();
    }

    // ================================================================ camera e hud
    private void MontarCamera()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            GameObject obj = new GameObject("Main Camera");
            obj.tag = "MainCamera";
            cam = obj.AddComponent<Camera>();
        }

        // Camera parada no centro, como uma sala do Isaac. Meia altura = sala + paredes
        // + um respiro pra HUD no topo.
        cam.orthographic = true;
        cam.orthographicSize = tamanhoDaSala.y * 0.5f + espessuraDaParede + 0.6f;
        cam.transform.position = new Vector3(0f, 0.3f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
    }

    private void MontarHud()
    {
        if (FindAnyObjectByType<Hud>() != null)
            return;

        GameObject obj = new GameObject("Hud");
        Hud hud = obj.AddComponent<Hud>();
        hud.Configurar(Jogador != null ? Jogador.GetComponent<Vida>() : null, null, CONTROLES);
    }
}
