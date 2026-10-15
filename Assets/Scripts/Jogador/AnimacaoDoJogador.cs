using UnityEngine;

/// <summary>
/// O desenho do jogador: parado, andando, atirando, rolando na esquiva e morrendo. O corpo sempre olha pro
/// lado da mira (espelhado), como no Gungeon, mesmo andando de costas. O heroi visto de cima (1.22.0,
/// <see cref="TrocarDirecoes"/>) tem tambem as folhas de frente, de costas e da esquerda, e olha pra
/// direcao da mira sem espelhar.
///
/// Cada animacao e uma folha (tira de quadros lado a lado, ver <see cref="FolhaDeSprites"/>). Pra
/// trocar o boneco: arrastar as folhas novas aqui e acertar o tamanho do quadro.
///
/// O desenho fica num filho ("Corpo"), pra girar na esquiva sem girar o colisor da raiz.
/// </summary>
[DisallowMultipleComponent]
public class AnimacaoDoJogador : MonoBehaviour
{
    [Tooltip("O SpriteRenderer do corpo (um filho)")]
    [SerializeField] private SpriteRenderer corpo;

    [Header("Folhas (quadros lado a lado)")]
    [SerializeField] private Texture2D parado;
    [SerializeField] private Texture2D andando;

    [Tooltip("O ataque: toca a cada tiro, do quadro do disparo pro fim. Vazio = so atira, sem animacao")]
    [SerializeField] private Texture2D ataque;

    [Tooltip("Toca uma vez quando a vida acaba, e o desenho fica no ultimo quadro")]
    [SerializeField] private Texture2D morte;

    [Tooltip("Tamanho de cada quadro, em pixels")]
    [SerializeField] private Vector2Int tamanhoDoQuadro = new Vector2Int(100, 100);

    [Tooltip("Pixels por unidade. Com 20, um boneco de 20 pixels de altura fica com 1 unidade")]
    [SerializeField, Min(1f)] private float pixelsPorUnidade = 20f;

    [Header("Ritmo")]
    [SerializeField, Min(1f)] private float quadrosPorSegundoParado = 8f;
    [SerializeField, Min(1f)] private float quadrosPorSegundoAndando = 12f;
    [SerializeField, Min(1f)] private float quadrosPorSegundoNaMorte = 8f;

    [Tooltip("Quadro do ataque em que o tiro sai (o primeiro e 0): a animacao do tiro comeca nele")]
    [SerializeField, Min(0)] private int quadroDoDisparo = 5;

    [Tooltip("Abaixo desta velocidade conta como parado")]
    [SerializeField, Min(0f)] private float velocidadeParaAndar = 0.3f;

    [Tooltip("Voltas que o corpo da em cada esquiva")]
    [SerializeField] private float voltasNaEsquiva = 1f;

    [Tooltip("Andando e atirando: altura do pulinho a cada passo, em pixels (0 = desliza parado no tiro)")]
    [SerializeField, Min(0)] private int pulinhoAoAndarAtirando = 1;

    [Tooltip("Passos por segundo do pulinho (o andar do pacote da 3 passos por segundo)")]
    [SerializeField, Min(0.1f)] private float passosPorSegundo = 3f;

    private Sprite[] quadrosParado;
    private Sprite[] quadrosAndando;
    private Sprite[] quadrosDoTiro;
    private Sprite[] quadrosMorte;

    // O heroi visto de cima: as folhas de frente, de costas e da esquerda (nulas = so a de lado, espelhada).
    private Texture2D[] paradoDeCima, andandoDeCima;

    // Os quadros por rumo: 0 direita, 1 baixo (de frente), 2 cima (de costas), 3 esquerda.
    private Sprite[][] paradoPorRumo, andandoPorRumo;
    private int rumo;

    private ControlesDoJogador controles;
    private MovimentoDoJogador movimento;
    private ArmaDoJogador arma;
    private Vida vida;

    private Sprite[] tocando;
    private float comecou;
    private float quadrosPorSegundo;
    private bool soUmaVez;
    private float atirandoAte;

    /// <summary>O SpriteRenderer do corpo (o rastro da esquiva copia ele).</summary>
    public SpriteRenderer Corpo => corpo;

    private void Awake()
    {
        controles = GetComponent<ControlesDoJogador>();
        movimento = GetComponent<MovimentoDoJogador>();
        arma = GetComponent<ArmaDoJogador>();
        vida = GetComponent<Vida>();

        Cortar();
    }

    /// <summary>Troca o boneco (cada heroi tem as suas folhas e o quadro em que o tiro sai).</summary>
    public void TrocarFolhas(Texture2D novoParado, Texture2D novoAndando, Texture2D novoAtaque, Texture2D novaMorte, int quadro,
                             float pixels = 20f)
    {
        if (novoParado == null)
            return;

        pixelsPorUnidade = pixels;
        parado = novoParado;
        andando = novoAndando != null ? novoAndando : novoParado;
        ataque = novoAtaque;
        morte = novaMorte;
        quadroDoDisparo = quadro;
        paradoDeCima = andandoDeCima = null;
        tocando = null;
        Cortar();
    }

    /// <summary>
    /// O heroi visto de cima: as folhas de frente, de costas e da esquerda (nessa ordem), parado e
    /// andando. Chamar depois de <see cref="TrocarFolhas"/> (que volta pro boneco so de lado).
    /// </summary>
    public void TrocarDirecoes(Texture2D[] novoParado, Texture2D[] novoAndando)
    {
        paradoDeCima = novoParado;
        andandoDeCima = novoAndando;
        tocando = null;
        Cortar();
    }

    private bool DeCima => paradoDeCima != null;

    private Sprite[][] PorRumo(Sprite[] deLado, Texture2D[] outras)
    {
        Sprite[][] porRumo = { deLado, deLado, deLado, deLado };

        for (int i = 0; outras != null && i < outras.Length && i < 3; i++)
        {
            Sprite[] quadros = FolhaDeSprites.Cortar(outras[i], tamanhoDoQuadro, pixelsPorUnidade);

            if (quadros.Length > 0)
                porRumo[i + 1] = quadros;
        }

        return porRumo;
    }

    // Os quadros parados ou andando de um mesmo jeito em outro rumo: virar continua o passo de onde estava.
    private Sprite[][] Familia(Sprite[] quadros)
    {
        if (quadros == null)
            return null;

        if (System.Array.IndexOf(paradoPorRumo, quadros) >= 0)
            return paradoPorRumo;

        return System.Array.IndexOf(andandoPorRumo, quadros) >= 0 ? andandoPorRumo : null;
    }

    private void Cortar()
    {
        quadrosParado = FolhaDeSprites.Cortar(parado, tamanhoDoQuadro, pixelsPorUnidade);
        quadrosAndando = FolhaDeSprites.Cortar(andando, tamanhoDoQuadro, pixelsPorUnidade);
        quadrosMorte = FolhaDeSprites.Cortar(morte, tamanhoDoQuadro, pixelsPorUnidade);

        // Do tiro so interessa do quadro do disparo pro fim: o arco ja esta puxado quando a flecha sai.
        Sprite[] todosDoAtaque = FolhaDeSprites.Cortar(ataque, tamanhoDoQuadro, pixelsPorUnidade);
        int desde = Mathf.Clamp(quadroDoDisparo, 0, Mathf.Max(0, todosDoAtaque.Length - 1));
        quadrosDoTiro = new Sprite[Mathf.Max(0, todosDoAtaque.Length - desde)];
        System.Array.Copy(todosDoAtaque, desde, quadrosDoTiro, 0, quadrosDoTiro.Length);

        if (quadrosAndando.Length == 0)
            quadrosAndando = quadrosParado;

        paradoPorRumo = PorRumo(quadrosParado, paradoDeCima);
        andandoPorRumo = PorRumo(quadrosAndando, andandoDeCima);

        Tocar(quadrosParado, quadrosPorSegundoParado, false);
    }

    private void OnEnable()
    {
        if (arma != null)
            arma.AoAtirar += Atirou;
    }

    private void OnDisable()
    {
        if (arma != null)
            arma.AoAtirar -= Atirou;
    }

    private void Atirou(Vector2 rumo, float intervalo)
    {
        // Desde a 1.18.0 o boneco nao tem a arma desenhada: toda arma aparece na mao (ArmaNaMao), e quem
        // mexe no tiro e ela (o coice). O ataque do corpo so toca se a arma nao tiver desenho nenhum.
        if (quadrosDoTiro.Length == 0 || (arma != null && arma.Arma != null && arma.Arma.Desenho != null))
            return;

        float duracao = Mathf.Min(0.35f, intervalo);
        Tocar(quadrosDoTiro, quadrosDoTiro.Length / duracao, true);
        atirandoAte = Time.time + duracao;
    }

    private bool Andando => movimento != null && movimento.Velocidade.magnitude > velocidadeParaAndar;

    private void LateUpdate()
    {
        if (corpo == null || quadrosParado.Length == 0)
            return;

        if (vida != null && vida.Morto)
        {
            Morto();
            return;
        }

        // Olha pro lado da mira (o visto de cima, pra direcao dela: frente, costas ou um lado).
        if (controles != null && DeCima)
        {
            Vector2 mira = controles.Mira;

            if (mira.sqrMagnitude > 0.0025f)
                rumo = Mathf.Abs(mira.y) > Mathf.Abs(mira.x) ? (mira.y < 0f ? 1 : 2) : (mira.x < 0f ? 3 : 0);

            corpo.flipX = false;
        }
        else if (controles != null && Mathf.Abs(controles.Mira.x) > 0.05f)
        {
            corpo.flipX = controles.Mira.x < 0f;
        }

        Sprite[] parados = paradoPorRumo[rumo];
        Sprite[] andandos = andandoPorRumo[rumo];

        bool esquivando = movimento != null && movimento.Esquivando;
        bool atirando = Time.time < atirandoAte;

        if (esquivando)
            Tocar(andandos, quadrosPorSegundoAndando * 2f, false);
        else if (!atirando)
            Tocar(Andando ? andandos : parados, Andando ? quadrosPorSegundoAndando : quadrosPorSegundoParado, false);

        // Andando e atirando: um pulinho de pixel inteiro a cada passo, pra nao parecer que desliza.
        float pulinho = 0f;

        if (atirando && !esquivando && Andando && pulinhoAoAndarAtirando > 0)
        {
            float passo = Mathf.Abs(Mathf.Sin(Time.time * passosPorSegundo * Mathf.PI));
            pulinho = Mathf.Round(passo * pulinhoAoAndarAtirando) / pixelsPorUnidade;
        }

        corpo.transform.localPosition = new Vector3(0f, pulinho, 0f);
        MostrarQuadro();

        // Na esquiva o corpo rola: uma volta inteira pro lado em que vai.
        float angulo = 0f;

        if (esquivando && !movimento.Investindo)
        {
            float sentido = movimento.RumoDaEsquiva.x >= 0f ? -1f : 1f;
            angulo = 360f * voltasNaEsquiva * movimento.ProgressoDaEsquiva * sentido;
        }

        corpo.transform.localRotation = Quaternion.Euler(0f, 0f, angulo);
    }

    // Caido: a animacao de morte uma vez so, sem pulinho nem giro, olhando pro lado em que estava.
    private void Morto()
    {
        if (quadrosMorte.Length > 0 && tocando != quadrosMorte)
            Tocar(quadrosMorte, quadrosPorSegundoNaMorte, true);

        corpo.transform.localPosition = Vector3.zero;
        corpo.transform.localRotation = Quaternion.identity;
        MostrarQuadro();
    }

    private void MostrarQuadro()
    {
        int quadro = Mathf.FloorToInt((Time.time - comecou) * quadrosPorSegundo);
        quadro = soUmaVez ? Mathf.Min(quadro, tocando.Length - 1) : quadro % tocando.Length;
        corpo.sprite = tocando[quadro];
    }

    private void Tocar(Sprite[] quadros, float novoRitmo, bool umaVez)
    {
        if (quadros == null || quadros.Length == 0)
            return;

        // A mesma animacao em loop (ou ela em outro rumo) continua de onde esta; so recomeca se mudou.
        if (!umaVez && !soUmaVez && (quadros == tocando || (Familia(quadros) != null && Familia(quadros) == Familia(tocando))))
        {
            tocando = quadros;
            quadrosPorSegundo = novoRitmo;
            return;
        }

        tocando = quadros;
        quadrosPorSegundo = novoRitmo;
        soUmaVez = umaVez;
        comecou = Time.time;
    }
}
