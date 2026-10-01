using UnityEngine;

/// <summary>
/// O tiro do Isaac: segurou uma seta, sai lagrima naquela direcao, no ritmo da cadencia.
/// A direcao do tiro nao depende de pra onde o boneco anda — da pra fugir pra um lado
/// atirando pro outro. So as quatro direcoes retas, como no jogo original.
///
/// Os tres numeros que definem a "arma" ficam aqui: dano, alcance e cadencia. Os itens
/// mexem neles por <see cref="Configurar"/> e <see cref="ConfigurarLagrima"/>.
///
/// Precisa de <see cref="Entrada"/> no mesmo objeto. Se tiver <see cref="MovimentoTopDown"/>,
/// a lagrima herda um pouco da velocidade do boneco.
/// </summary>
[DisallowMultipleComponent]
public class AtiradorTopDown : MonoBehaviour
{
    [Header("Arma")]
    [Tooltip("Dano de cada lagrima (o Isaac comeca com 3.5)")]
    [SerializeField, Min(0f)] private float dano = 3.5f;

    [Tooltip("Distancia que a lagrima voa antes de cair, em unidades")]
    [SerializeField, Min(0.1f)] private float alcance = 6.5f;

    [Tooltip("Lagrimas por segundo segurando a seta")]
    [SerializeField, Min(0.1f)] private float tirosPorSegundo = 2.7f;

    [Tooltip("Velocidade da lagrima, em unidades por segundo")]
    [SerializeField, Min(0.1f)] private float velocidadeDoTiro = 9f;

    [Tooltip("Quanto da velocidade do boneco a lagrima herda (0 = nada, 1 = tudo)")]
    [SerializeField, Range(0f, 1f)] private float herancaDaVelocidade = 0.35f;

    [Tooltip("Lagrimas por disparo. Mais de uma sai em leque (itens tipo olho triplo)")]
    [SerializeField, Min(1)] private int lagrimasPorDisparo = 1;

    [Tooltip("Graus entre uma lagrima e a vizinha quando sai mais de uma")]
    [SerializeField, Range(0f, 45f)] private float aberturaDoLeque = 10f;

    [Tooltip("Empurrao que a lagrima da em quem acerta")]
    [SerializeField, Min(0f)] private float forcaEmpurrao = 2.5f;

    [Header("Aparencia")]
    [Tooltip("Diametro da lagrima, em unidades")]
    [SerializeField, Min(0.05f)] private float tamanho = 0.28f;

    [SerializeField] private Color cor = new Color(0.55f, 0.8f, 1f);

    [Tooltip("Sprite da lagrima. Vazio = a flecha do Tiny Swords (ou um circulo gerado, sem ela)")]
    [SerializeField] private Sprite sprite;

    [Tooltip("Distancia do centro do boneco de onde a lagrima nasce")]
    [SerializeField, Min(0f)] private float distanciaDoCorpo = 0.3f;

    [Tooltip("Alterna olho esquerdo/direito a cada tiro, como o Isaac")]
    [SerializeField, Min(0f)] private float afastamentoDosOlhos = 0.1f;

    [Tooltip("Filho que mostra pra onde o boneco olha (opcional)")]
    [SerializeField] private Transform olho;

    [SerializeField, Min(0f)] private float distanciaDoOlho = 0.18f;

    // ---------------- efeitos de item ----------------
    private bool atravessa;
    private bool teleguiada;
    private bool paraTras;
    private Color corOriginal;
    private bool guardouCor;

    // ---------------- tipo de flecha ----------------
    private DefinicaoDeFlecha flecha;

    // ---------------- tiro de espada (onda de corte) ----------------
    private Sprite[] quadrosDoTiro;
    private float quadrosPorSegundoDoTiro = 12f;
    private float raioDoTiro = 0.5f;
    private bool sempreAtravessa;
    private Som somDoTiro = Som.Tiro;

    // A flecha especial muda a cor e os efeitos do tiro, mas o desenho continua o do heroi
    // (cavaleiro nao vira arqueiro, mago nao solta flecha). Uma aparencia pronta por tipo.
    private readonly System.Collections.Generic.Dictionary<TipoDeFlecha, AparenciaDoProjetil> rajadas =
        new System.Collections.Generic.Dictionary<TipoDeFlecha, AparenciaDoProjetil>();

    // ---------------- estado ----------------
    private Entrada entrada;
    private MovimentoTopDown movimento;
    private Cronometro recarga;
    private bool olhoDireito;
    private Vector2 olhando = Vector2.down;
    private bool apontarLagrima;

    /// <summary>Pixels por unidade da flecha: com a lagrima de 0.28, fica com ~0.55 de comprimento.</summary>
    private const float PixelsDaFlecha = 24f;

    /// <summary>Cada disparo, com a direcao (a animacao do arqueiro escuta).</summary>
    public event System.Action<Vector2> AoAtirar;

    public float Dano => dano;

    public float Alcance => alcance;

    public float TirosPorSegundo => tirosPorSegundo;

    public float VelocidadeDoTiro => velocidadeDoTiro;

    public float Tamanho => tamanho;

    public int LagrimasPorDisparo => lagrimasPorDisparo;

    /// <summary>A flecha que sai agora (a <see cref="TrocaDeFlecha"/> escolhe).</summary>
    public TipoDeFlecha TipoDeFlecha => flecha != null ? flecha.tipo : TipoDeFlecha.Normal;

    /// <summary>Tiros por segundo de verdade, ja com o tipo de flecha.</summary>
    public float CadenciaAtual => tirosPorSegundo * (flecha != null ? flecha.multiplicaCadencia : 1f);

    /// <summary>Pra onde o boneco olha: o tiro manda, senao o andar.</summary>
    public Vector2 Olhando => olhando;

    // ---------------- ciclo de vida ----------------
    private void Awake()
    {
        entrada = GetComponent<Entrada>();
        movimento = GetComponent<MovimentoTopDown>();

        if (entrada == null)
            Debug.LogWarning("[AtiradorTopDown] sem Entrada no objeto — o boneco nao vai atirar.", this);

        if (sprite == null)
        {
            // A flecha e branca de fabrica e voa apontada pro rumo; a bola e azul e redonda.
            sprite = ArteImportada.Flecha(PixelsDaFlecha);
            apontarLagrima = sprite != null;

            if (apontarLagrima)
                cor = Color.white;
            else
                sprite = ArteGerada.Bola();
        }
    }

    private void Update()
    {
        recarga.Contar(Time.deltaTime);

        if (entrada == null)
            return;

        if (entrada.Atirando)
            olhando = entrada.Tiro;
        else if (entrada.Andar != Vector2.zero)
            olhando = entrada.Andar;

        if (olho != null)
            olho.localPosition = olhando * distanciaDoOlho;

        if (entrada.Atirando && !recarga.Ativo)
        {
            Atirar(entrada.Tiro);
            recarga.Forcar(1f / CadenciaAtual);
        }
    }

    // ---------------- tiro ----------------
    /// <summary>
    /// Solta as lagrimas de um disparo na direcao pedida (uma so, ou um leque se algum item
    /// deu lagrimas extras). Devolve a do meio. Publico pra dar pra testar/roteirizar.
    /// </summary>
    public Lagrima Atirar(Vector2 direcao)
    {
        direcao = direcao.sqrMagnitude > 0.0001f ? direcao.normalized : Vector2.down;

        // Olho esquerdo, olho direito: desloca um pouquinho pro lado da direcao do tiro.
        Vector2 lado = new Vector2(-direcao.y, direcao.x) * (olhoDireito ? afastamentoDosOlhos : -afastamentoDosOlhos);
        olhoDireito = !olhoDireito;

        Vector2 origem = (Vector2)transform.position + direcao * distanciaDoCorpo + lado;
        Vector2 heranca = movimento != null ? movimento.Velocidade * herancaDaVelocidade : Vector2.zero;
        float velocidade = velocidadeDoTiro * (flecha != null ? flecha.multiplicaVelocidade : 1f);

        Sons.Tocar(somDoTiro, 0.55f);
        AoAtirar?.Invoke(direcao);

        Lagrima doMeio = null;
        float primeiroAngulo = -aberturaDoLeque * (lagrimasPorDisparo - 1) * 0.5f;

        for (int i = 0; i < lagrimasPorDisparo; i++)
        {
            Vector2 rumo = Quaternion.Euler(0f, 0f, primeiroAngulo + aberturaDoLeque * i) * direcao;
            Lagrima lagrima = Soltar(origem, rumo * velocidade + heranca);

            if (i == lagrimasPorDisparo / 2)
                doMeio = lagrima;
        }

        // Elmo de Duas Faces: um tiro pra tras, do outro lado do corpo.
        if (paraTras)
            Soltar((Vector2)transform.position - direcao * distanciaDoCorpo, -direcao * velocidade + heranca);

        return doMeio;
    }

    /// <summary>Efeitos que so as sinergias dao: golpe pesado e explosaozinha ao acertar.</summary>
    public void DefinirSinergias(bool pesado, float raioDaExplosao)
    {
        golpePesado = pesado;
        explosaoAoAcertar = raioDaExplosao;
    }

    private bool golpePesado;
    private float explosaoAoAcertar;

    private Lagrima Soltar(Vector2 origem, Vector2 velocidade)
    {
        Lagrima lagrima = CriarLagrima(origem);
        lagrima.Pesada = golpePesado;
        lagrima.RaioDaExplosao = explosaoAoAcertar;

        if (flecha == null || !flecha.Especial)
        {
            lagrima.Disparar(gameObject, velocidade, dano, alcance, forcaEmpurrao);
            lagrima.DefinirEfeitos(atravessa || sempreAtravessa, teleguiada);
            return lagrima;
        }

        // Flecha especial: os numeros dela por cima dos do heroi e dos itens.
        float danoDaFlecha = dano * flecha.multiplicaDano;
        lagrima.Disparar(gameObject, velocidade, danoDaFlecha, alcance * flecha.multiplicaAlcance,
                         forcaEmpurrao * flecha.multiplicaEmpurrao);
        lagrima.DefinirEfeitos(atravessa || sempreAtravessa || flecha.atravessa, teleguiada);

        VisualDoProjetil.Vestir(lagrima.gameObject, RajadaComEfeito(flecha));
        EfeitoDaFlecha efeito = lagrima.gameObject.AddComponent<EfeitoDaFlecha>();
        efeito.Configurar(flecha, danoDaFlecha, gameObject);
        lagrima.UsarEfeito(efeito);
        return lagrima;
    }

    /// <summary>
    /// Liga os efeitos especiais que os itens dao a lagrima. <paramref name="novaCor"/>
    /// null = volta a cor original.
    /// </summary>
    public void DefinirEfeitos(bool lagrimaAtravessa, bool lagrimaTeleguiada, bool tambemPraTras, Color? novaCor)
    {
        if (!guardouCor)
        {
            corOriginal = cor;
            guardouCor = true;
        }

        atravessa = lagrimaAtravessa;
        teleguiada = lagrimaTeleguiada;
        paraTras = tambemPraTras;
        cor = novaCor ?? corOriginal;
        rajadas.Clear();   // a cor da rajada especial parte da cor do tiro
    }

    private Lagrima CriarLagrima(Vector2 posicao)
    {
        bool especial = flecha != null && flecha.Especial;

        GameObject obj = new GameObject(especial ? flecha.nome : "Lagrima");
        obj.transform.position = posicao;
        obj.transform.localScale = Vector3.one * tamanho * (especial ? flecha.multiplicaTamanho : 1f);

        SpriteRenderer desenho = obj.AddComponent<SpriteRenderer>();
        desenho.sprite = sprite;
        desenho.color = cor;
        desenho.sortingOrder = 20;

        // O circulo gerado tem 1 unidade de diametro: raio 0.5 bate com o desenho. A onda de
        // corte e maior que a escala do tiro, entao tem raio proprio (com flecha especial tambem).
        CircleCollider2D colisor = obj.AddComponent<CircleCollider2D>();
        colisor.radius = raioDoTiro;

        obj.AddComponent<Rigidbody2D>();

        Lagrima lagrima = obj.AddComponent<Lagrima>();

        // Com flecha especial o desenho fica num filho que ja aponta sozinho.
        if (apontarLagrima && !especial)
            lagrima.ApontarProRumo();

        // Com flecha especial o mesmo desenho vem pelo VisualDoProjetil (RajadaComEfeito), com rastro e brilho.
        if (!especial && quadrosDoTiro != null && quadrosDoTiro.Length > 0)
            obj.AddComponent<AnimacaoDoTiro>().Configurar(quadrosDoTiro, quadrosPorSegundoDoTiro);

        return lagrima;
    }

    /// <summary>
    /// O tiro do proprio heroi vestido com a flecha especial: os quadros e o tamanho dele, a cor
    /// puxada pra da flecha e o rastro, as faiscas, o pulso e o impacto dela.
    /// </summary>
    private AparenciaDoProjetil RajadaComEfeito(DefinicaoDeFlecha especial)
    {
        if (rajadas.TryGetValue(especial.tipo, out AparenciaDoProjetil pronta))
            return pronta;

        AparenciaDoProjetil base_ = especial.aparencia;
        Sprite[] quadros = quadrosDoTiro != null && quadrosDoTiro.Length > 0 ? quadrosDoTiro : new[] { sprite };

        // Mesmo tamanho na tela do tiro normal: o lado maior do sprite, em diametros (a escala da raiz).
        Vector2 lados = quadros[0] != null ? (Vector2)quadros[0].bounds.size : Vector2.one;

        AparenciaDoProjetil nova = new AparenciaDoProjetil(quadros, Color.Lerp(cor, especial.cor, 0.75f), Mathf.Max(lados.x, lados.y))
        {
            quadrosPorSegundo = quadrosPorSegundoDoTiro,
            apontar = apontarLagrima,
        };

        if (base_ != null)
        {
            nova.pulso = base_.pulso;
            nova.ritmoDoPulso = base_.ritmoDoPulso;
            nova.corDoPisca = base_.corDoPisca;
            nova.ritmoDoPisca = base_.ritmoDoPisca;
            nova.intervaloDoRastro = base_.intervaloDoRastro > 0f ? base_.intervaloDoRastro : 0.05f;
            nova.corDoRastro = base_.corDoRastro;
            nova.duracaoDoRastro = base_.duracaoDoRastro;
            nova.faiscas = base_.faiscas;
            nova.intervaloDasFaiscas = base_.intervaloDasFaiscas;
            nova.tamanhoDasFaiscas = base_.tamanhoDasFaiscas;
            nova.corDasFaiscas = base_.corDasFaiscas;
            nova.impacto = base_.impacto;
            nova.corDoImpacto = base_.corDoImpacto;
            nova.tamanhoDoImpacto = base_.tamanhoDoImpacto;
        }

        // Rastro sempre na cor da flecha: e o que mais mostra de longe que o tiro mudou.
        Color rastro = especial.cor;
        rastro.a = 0.45f;
        nova.corDoRastro = rastro;

        rajadas[especial.tipo] = nova;
        return nova;
    }

    /// <summary>Troca os numeros da arma (itens, power-ups). Valores fora do limite sao ajustados.</summary>
    public void Configurar(float novoDano, float novoAlcance, float novaCadencia)
    {
        dano = Mathf.Max(0f, novoDano);
        alcance = Mathf.Max(0.1f, novoAlcance);
        tirosPorSegundo = Mathf.Max(0.1f, novaCadencia);
    }

    /// <summary>Troca como a lagrima sai (itens). Valores fora do limite sao ajustados.</summary>
    public void ConfigurarLagrima(float novaVelocidade, float novoTamanho, int quantasPorDisparo)
    {
        velocidadeDoTiro = Mathf.Max(0.1f, novaVelocidade);
        tamanho = Mathf.Max(0.05f, novoTamanho);
        lagrimasPorDisparo = Mathf.Max(1, quantasPorDisparo);
    }

    /// <summary>
    /// Troca o desenho do tiro (cada heroi tem o seu). <paramref name="apontar"/> = o desenho
    /// gira pro rumo, como a flecha; senao fica parado, como uma bola.
    /// </summary>
    public void DefinirVisual(Sprite novoSprite, bool apontar, Color novaCor)
    {
        if (novoSprite == null)
            return;

        sprite = novoSprite;
        apontarLagrima = apontar;
        cor = novaCor;
        guardouCor = false;

        // Trocar de heroi tira a onda de corte do anterior; o de espada liga de novo.
        quadrosDoTiro = null;
        raioDoTiro = 0.5f;
        sempreAtravessa = false;
        somDoTiro = Som.Tiro;
        rajadas.Clear();
    }

    /// <summary>
    /// Tiro de heroi de espada: a onda de corte voa animada (<paramref name="quadros"/>),
    /// com colisor de <paramref name="raio"/> (na escala do tiro), atravessa os inimigos e
    /// tem som de lamina. Chame depois do <see cref="DefinirVisual"/>.
    /// </summary>
    public void DefinirOndaDeCorte(Sprite[] quadros, float quadrosPorSegundo, float raio)
    {
        if (quadros == null || quadros.Length == 0)
            return;

        quadrosDoTiro = quadros;
        quadrosPorSegundoDoTiro = Mathf.Max(1f, quadrosPorSegundo);
        raioDoTiro = Mathf.Max(0.1f, raio);
        sempreAtravessa = true;
        somDoTiro = Som.Corte;
        rajadas.Clear();
    }

    /// <summary>O som de cada tiro (flecha por padrao; magia pro mago e o padre). Chame depois do <see cref="DefinirVisual"/>.</summary>
    public void DefinirSomDoTiro(Som som) => somDoTiro = som;

    /// <summary>Troca o tipo de flecha (Normal = o tiro do heroi). Quem chama e a <see cref="TrocaDeFlecha"/>.</summary>
    public void DefinirTipoDeFlecha(TipoDeFlecha tipo)
    {
        flecha = tipo == TipoDeFlecha.Normal ? null : CatalogoDeFlechas.De(tipo);
    }

    /// <summary>Aponta o filho que mostra a direcao do olhar.</summary>
    public void DefinirOlho(Transform alvo)
    {
        olho = alvo;
    }
}
