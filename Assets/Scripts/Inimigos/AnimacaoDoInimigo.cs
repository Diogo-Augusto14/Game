using UnityEngine;

/// <summary>
/// O desenho de um inimigo: parado, andando, atacando e morrendo, sempre olhando pro lado do
/// jogador (espelhado). Como no jogador, cada animacao e uma folha (ver <see cref="FolhaDeSprites"/>):
/// pra trocar o bicho, e so arrastar as folhas novas e acertar o tamanho do quadro.
///
/// O ataque toca no ritmo do preparo (do <see cref="InimigoAtirador"/> ou do <see cref="Chefe"/>): o
/// quadro do disparo chega bem na hora em que a bala sai. Quem tem mais de um ataque (os chefes) poe
/// as outras folhas em <see cref="outrosAtaques"/>.
/// </summary>
[DisallowMultipleComponent]
public class AnimacaoDoInimigo : MonoBehaviour
{
    [Tooltip("O SpriteRenderer do corpo (um filho)")]
    [SerializeField] private SpriteRenderer corpo;

    [Header("Folhas (quadros lado a lado)")]
    [SerializeField] private Texture2D parado;
    [SerializeField] private Texture2D andando;

    [Tooltip("Toca a cada tiro. Vazio = fica parado enquanto prepara")]
    [SerializeField] private Texture2D ataque;

    [Tooltip("Toca uma vez ao morrer, e o desenho fica no ultimo quadro")]
    [SerializeField] private Texture2D morte;

    [Tooltip("Tamanho de cada quadro, em pixels")]
    [SerializeField] private Vector2Int tamanhoDoQuadro = new Vector2Int(100, 100);

    [Tooltip("Pixels por unidade: o mesmo do jogador, pra os pixels terem o mesmo tamanho")]
    [SerializeField, Min(1f)] private float pixelsPorUnidade = 20f;

    [Header("Ritmo")]
    [SerializeField, Min(1f)] private float quadrosPorSegundoParado = 8f;
    [SerializeField, Min(1f)] private float quadrosPorSegundoAndando = 10f;
    [SerializeField, Min(1f)] private float quadrosPorSegundoNaMorte = 12f;

    [Tooltip("Quadro do ataque em que a bala sai (o primeiro e 0)")]
    [SerializeField, Min(0)] private int quadroDoDisparo = 5;

    [Tooltip("Mais ataques (os chefes): o 1 e o primeiro daqui, o 2 o segundo...")]
    [SerializeField] private OutroAtaque[] outrosAtaques;

    [Tooltip("Abaixo desta velocidade conta como parado")]
    [SerializeField, Min(0f)] private float velocidadeParaAndar = 0.3f;

    private Sprite[] quadrosParado;
    private Sprite[] quadrosAndando;
    private Sprite[] quadrosAtaque;
    private Sprite[] quadrosMorte;

    private IAnimavel inimigo;
    private Sprite[][] quadrosDosOutros;
    private Vida vida;

    private Sprite[] tocando;
    private float comecou;
    private float quadrosPorSegundo;
    private bool soUmaVez;
    private float atacandoAte;

    private void Awake()
    {
        inimigo = GetComponent<IAnimavel>();
        vida = GetComponent<Vida>();

        quadrosParado = FolhaDeSprites.Cortar(parado, tamanhoDoQuadro, pixelsPorUnidade);
        quadrosAndando = FolhaDeSprites.Cortar(andando, tamanhoDoQuadro, pixelsPorUnidade);
        quadrosAtaque = FolhaDeSprites.Cortar(ataque, tamanhoDoQuadro, pixelsPorUnidade);
        quadrosMorte = FolhaDeSprites.Cortar(morte, tamanhoDoQuadro, pixelsPorUnidade);

        int outros = outrosAtaques != null ? outrosAtaques.Length : 0;
        quadrosDosOutros = new Sprite[outros][];

        for (int i = 0; i < outros; i++)
            quadrosDosOutros[i] = FolhaDeSprites.Cortar(outrosAtaques[i].folha, tamanhoDoQuadro, pixelsPorUnidade);

        if (quadrosAndando.Length == 0)
            quadrosAndando = quadrosParado;

        Tocar(quadrosParado, quadrosPorSegundoParado, false);
    }

    private void OnEnable()
    {
        if (inimigo != null)
            inimigo.AoAtacar += Atacou;
    }

    private void OnDisable()
    {
        if (inimigo != null)
            inimigo.AoAtacar -= Atacou;
    }

    private void Atacou(int qual, float ateOGolpe)
    {
        Sprite[] quadros = quadrosAtaque;
        int golpe = quadroDoDisparo;

        if (qual > 0 && qual <= quadrosDosOutros.Length && quadrosDosOutros[qual - 1].Length > 0)
        {
            quadros = quadrosDosOutros[qual - 1];
            golpe = outrosAtaques[qual - 1].quadroDoGolpe;
        }

        if (quadros.Length == 0)
            return;

        // Do comeco ao quadro do golpe cabe no preparo; o resto do ataque segue no mesmo ritmo.
        int ateOGolpeEmQuadros = Mathf.Clamp(golpe, 1, quadros.Length);
        float ritmo = ateOGolpeEmQuadros / Mathf.Max(0.05f, ateOGolpe);
        Tocar(quadros, ritmo, true);
        atacandoAte = Time.time + quadros.Length / ritmo;
    }

    private void LateUpdate()
    {
        if (corpo == null || quadrosParado.Length == 0)
            return;

        if (vida != null && vida.Morto)
        {
            if (quadrosMorte.Length > 0 && tocando != quadrosMorte)
                Tocar(quadrosMorte, quadrosPorSegundoNaMorte, true);
        }
        else
        {
            if (inimigo != null && Mathf.Abs(inimigo.OlhandoPara.x) > 0.05f)
                corpo.flipX = inimigo.OlhandoPara.x < 0f;

            if (Time.time >= atacandoAte)
            {
                bool andandoAgora = inimigo != null && inimigo.Velocidade.magnitude > velocidadeParaAndar;
                Tocar(andandoAgora ? quadrosAndando : quadrosParado,
                      andandoAgora ? quadrosPorSegundoAndando : quadrosPorSegundoParado, false);
            }
        }

        int quadro = Mathf.FloorToInt((Time.time - comecou) * quadrosPorSegundo);
        quadro = soUmaVez ? Mathf.Min(quadro, tocando.Length - 1) : quadro % tocando.Length;
        corpo.sprite = tocando[quadro];
    }

    private void Tocar(Sprite[] quadros, float novoRitmo, bool umaVez)
    {
        if (quadros == null || quadros.Length == 0)
            return;

        // A mesma animacao em loop continua de onde esta; so recomeca se mudou.
        if (quadros == tocando && !umaVez && !soUmaVez)
        {
            quadrosPorSegundo = novoRitmo;
            return;
        }

        tocando = quadros;
        quadrosPorSegundo = novoRitmo;
        soUmaVez = umaVez;
        comecou = Time.time;
    }
}

/// <summary>Mais uma animacao de ataque: a folha e o quadro em que o golpe acontece.</summary>
[System.Serializable]
public class OutroAtaque
{
    public Texture2D folha;

    [Tooltip("Quadro em que o golpe acontece (o primeiro e 0)")]
    [Min(0)] public int quadroDoGolpe = 5;
}
