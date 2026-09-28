using UnityEngine;

/// <summary>
/// A RECEITA de como cada objeto do jogo e montado: o boneco, o inimigo e as pecas de
/// cenario. Uma classe estatica, sem estado.
///
/// Por que ela existe separada do Bootstrap: duas coisas montam esses objetos — o
/// Bootstrap (ao apertar Play) e o Montador de Cena (o menu Tools ▸ Jogo ▸ Montar na cena,
/// que deixa os objetos na cena pra voce editar a mao). Se cada um tivesse a sua copia da
/// receita, um dia o boneco do Play e o boneco do prefab iam divergir num detalhe e a
/// caca ao bug levaria uma tarde. Aqui existe UMA receita.
///
/// A ORDEM em que os componentes sao adicionados e parte da receita, nao capricho: fora do
/// editor, AddComponent chama Awake na hora, e varios componentes procuram os vizinhos no
/// proprio Awake. O Vida, por exemplo, guarda o IControladorDeMovimento uma vez so — se o
/// Movimento nao existir ainda, o empurrao do dano nunca vai funcionar naquele objeto.
/// </summary>
public static class Construtor
{
    /// <summary>Que papel a peca cumpre — decide a camada e, com ela, o comportamento.</summary>
    public enum TipoDeBloco
    {
        /// <summary>Da pra ficar em pe em cima. Camada de chao.</summary>
        Chao,

        /// <summary>Da pra deslizar, dar wall jump e pendurar na quina. Camada de parede.</summary>
        Parede,

        /// <summary>Trigger com o componente Escada: da pra subir.</summary>
        Escada
    }

    // ---------------- medidas do boneco (sprite de 46x55 px a 100 pixels por unidade) ----------------
    public const float LARGURA_DO_JOGADOR = 0.26f;
    public const float ALTURA_DO_JOGADOR = 0.52f;
    public const float LARGURA_DO_INIMIGO = 0.30f;
    public const float ALTURA_DO_INIMIGO = 0.40f;

    // ---------------- cores da paleta ----------------
    // Familia cinza = chao (pisa em cima). Familia azul = parede (desliza, wall jump).
    // Ver a cor e saber o comportamento economiza abrir o Inspector a cada bloco.
    public static readonly Color COR_CHAO = new Color(0.42f, 0.44f, 0.47f);
    public static readonly Color COR_PLATAFORMA = new Color(0.55f, 0.58f, 0.61f);
    public static readonly Color COR_TUNEL = new Color(0.40f, 0.50f, 0.44f);
    public static readonly Color COR_PAREDE = new Color(0.28f, 0.40f, 0.62f);
    public static readonly Color COR_BEIRADA = new Color(0.22f, 0.32f, 0.50f);
    public static readonly Color COR_ESCADA = new Color(0.62f, 0.45f, 0.24f);

    private static Sprite spriteDeBlocoEmMemoria;

    // ================================================================ jogador
    /// <summary>
    /// Monta o boneco completo e devolve a raiz.
    ///
    /// <paramref name="tocarAnimacao"/> deve ser false quando quem chama e o editor: fora
    /// do Play o Awake do animador nao rodou, e mandar tocar um clipe criaria um Sprite
    /// temporario que nao sobrevive a salvar a cena. Quem monta no editor poe o sprite de
    /// previa depois, usando um Sprite de verdade da pasta de arte.
    /// </summary>
    public static GameObject MontarJogador(Vector3 posicao, bool tocarAnimacao = true)
    {
        GameObject raiz = new GameObject("Jogador");
        raiz.transform.position = posicao;
        Camadas.Definir(raiz, Camadas.Jogador);
        DefinirTagSeExistir(raiz, "Player");

        // --- corpo
        Rigidbody2D rb = raiz.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D corpo = raiz.AddComponent<BoxCollider2D>();
        corpo.size = new Vector2(LARGURA_DO_JOGADOR, ALTURA_DO_JOGADOR);
        corpo.offset = new Vector2(0f, ALTURA_DO_JOGADOR * 0.5f);

        // --- desenho num filho: o pivo do sprite fica nos pes, na origem da raiz
        SpriteRenderer sr = CriarVisual(raiz.transform, Camadas.Jogador, 10);
        AnimadorDeSprites animador = sr.gameObject.AddComponent<AnimadorDeSprites>();

        // As folhas de Assets/player foram desenhadas olhando pra ESQUERDA, e o codigo todo
        // trata +X como "olhando pra direita". Espelhamos o desenho aqui; a direcao logica
        // (localScale da raiz, hitbox, empurrao, sensores) continua como sempre foi.
        animador.ArteOlhaParaEsquerda = true;

        // --- sensores (os nomes sao procurados pelo Movimento; nao renomeie)
        CriarFilho(raiz.transform, "GroundCheck", new Vector3(0f, 0f, 0f));
        CriarFilho(raiz.transform, "WallCheck", new Vector3(LARGURA_DO_JOGADOR * 0.5f + 0.03f, ALTURA_DO_JOGADOR * 0.5f, 0f));
        CriarFilho(raiz.transform, "LedgeCheck", new Vector3(LARGURA_DO_JOGADOR * 0.5f + 0.03f, ALTURA_DO_JOGADOR, 0f));

        // --- hitbox do golpe
        CriarHitbox(raiz.transform, new Vector2(0.28f, 0.28f), new Vector2(0.45f, 0.35f));

        // --- logica, NA ORDEM DE DEPENDENCIA
        raiz.AddComponent<Entrada>();
        Movimento movimento = raiz.AddComponent<Movimento>();

        Vida vida = raiz.AddComponent<Vida>();
        // destruir = false: o boneco renasce, nao pode ser apagado da cena.
        vida.Configurar(maxima: 120f, novaDefesa: 0f, destruir: false, atrasoDaDestruicao: 0f, piscar: true);

        raiz.AddComponent<Ataque>();
        raiz.AddComponent<Cura>();
        raiz.AddComponent<EfeitoDeImpacto>();
        raiz.AddComponent<AnimacaoDoJogador>();
        raiz.AddComponent<Player>();

        // O dash atravessa inimigos: sem isso a arrancada para no primeiro corpo.
        int camadaDoInimigo = Camadas.Indice(Camadas.Inimigo);

        if (camadaDoInimigo >= 0)
            movimento.dashAtravessa = 1 << camadaDoInimigo;

        if (tocarAnimacao)
            animador.Tocar(NomesDeAnimacao.Parado);

        return raiz;
    }

    // ================================================================ inimigo
    /// <summary>Monta um inimigo completo e devolve a raiz.</summary>
    public static GameObject MontarInimigo(Vector3 posicao, string nome = "Inimigo", bool tocarAnimacao = true)
    {
        GameObject raiz = new GameObject(nome);
        raiz.transform.position = posicao;
        Camadas.Definir(raiz, Camadas.Inimigo);

        Rigidbody2D rb = raiz.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D corpo = raiz.AddComponent<BoxCollider2D>();
        corpo.size = new Vector2(LARGURA_DO_INIMIGO, ALTURA_DO_INIMIGO);
        corpo.offset = new Vector2(0f, ALTURA_DO_INIMIGO * 0.5f);

        SpriteRenderer sr = CriarVisual(raiz.transform, Camadas.Inimigo, 8);
        AnimadorDeSprites animador = sr.gameObject.AddComponent<AnimadorDeSprites>();

        CriarFilho(raiz.transform, "ChecadorDeBorda", new Vector3(LARGURA_DO_INIMIGO * 0.5f + 0.04f, 0.03f, 0f));

        // A hitbox e criada AQUI, e nao no Awake do Inimigo, pra o objeto ficar completo
        // tambem quando montado no editor (onde Awake nao roda) e salvo como prefab.
        CriarHitbox(raiz.transform, new Vector2(0.34f, 0.2f), new Vector2(0.6f, 0.36f));

        // Vida antes do Inimigo: o Inimigo faz GetComponent<Vida>() no proprio Awake.
        Vida vida = raiz.AddComponent<Vida>();
        vida.Configurar(maxima: 55f, novaDefesa: 1f, destruir: true, atrasoDaDestruicao: 1.1f, piscar: false);

        raiz.AddComponent<EfeitoDeImpacto>();
        raiz.AddComponent<Inimigo>();
        raiz.AddComponent<AnimacaoDoInimigo>();

        if (tocarAnimacao)
            animador.Tocar(NomesDeAnimacao.InimigoParado);

        return raiz;
    }

    // ================================================================ pecas de cenario
    /// <summary>
    /// Um bloco de cenario: desenho em modo Tiled (a textura repete em vez de esticar, que
    /// e o certo pra pixel art), colisor do mesmo tamanho e o <see cref="BlocoDoCenario"/>
    /// mantendo os dois em sincronia quando voce esticar.
    /// </summary>
    public static GameObject MontarBloco(
        string nome, Vector2 centro, Vector2 tamanho, Color cor,
        TipoDeBloco tipo = TipoDeBloco.Chao, Transform pai = null, Sprite sprite = null)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.position = centro;

        if (pai != null)
            obj.transform.SetParent(pai, true);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : SpriteDeBloco();
        sr.color = cor;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = tamanho;
        sr.sortingOrder = -5;

        if (tipo == TipoDeBloco.Escada)
        {
            BoxCollider2D area = obj.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = tamanho;

            obj.AddComponent<Escada>();
            obj.AddComponent<BlocoDoCenario>();

            return obj;
        }

        BoxCollider2D caixa = obj.AddComponent<BoxCollider2D>();
        caixa.size = tamanho;

        // A camada e o que define o comportamento: chao se pisa, parede se desliza.
        if (tipo == TipoDeBloco.Parede)
            DefinirCamadaComApelido(obj, "Parede", "Wall");
        else
            DefinirCamadaComApelido(obj, "Chao", "ground");

        obj.AddComponent<BlocoDoCenario>();

        return obj;
    }

    /// <summary>
    /// Quadradinho 16x16 com borda, gerado na memoria. Serve de textura pra todos os
    /// blocos no Play. No editor o Montador de Cena usa um PNG de verdade em
    /// Assets/Sprites — um Sprite gerado em memoria nao sobrevive a salvar a cena.
    /// </summary>
    public static Sprite SpriteDeBloco()
    {
        if (spriteDeBlocoEmMemoria != null)
            return spriteDeBlocoEmMemoria;

        Texture2D textura = new Texture2D(16, 16, TextureFormat.RGBA32, false)
        {
            name = "BlocoGerado",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };

        textura.SetPixels32(PixelsDoBloco(16));
        textura.Apply();

        spriteDeBlocoEmMemoria = Sprite.Create(
            textura, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f),
            16f, 0, SpriteMeshType.FullRect);

        spriteDeBlocoEmMemoria.name = "BlocoGerado";
        return spriteDeBlocoEmMemoria;
    }

    /// <summary>
    /// Os pixels do bloco: miolo claro com uma borda mais clara em volta. Publico porque o
    /// editor grava esses mesmos pixels num PNG, pra o desenho do Play e o do editor serem
    /// identicos.
    /// </summary>
    public static Color32[] PixelsDoBloco(int lado)
    {
        Color32[] pixels = new Color32[lado * lado];

        Color32 miolo = new Color32(184, 184, 184, 255);
        Color32 borda = new Color32(255, 255, 255, 255);

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                bool naBorda = x == 0 || y == 0 || x == lado - 1 || y == lado - 1;
                pixels[y * lado + x] = naBorda ? borda : miolo;
            }
        }

        return pixels;
    }

    // ================================================================ pecas soltas
    private static SpriteRenderer CriarVisual(Transform pai, string camada, int ordem)
    {
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(pai, false);
        Camadas.Definir(visual, camada);

        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sortingOrder = ordem;

        return sr;
    }

    private static void CriarHitbox(Transform pai, Vector2 centro, Vector2 tamanho)
    {
        GameObject hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(pai, false);
        hitbox.transform.localPosition = centro;
        Camadas.Definir(hitbox, Camadas.Golpe);

        BoxCollider2D caixa = hitbox.AddComponent<BoxCollider2D>();
        caixa.isTrigger = true;
        caixa.size = tamanho;

        // Desligado desde o inicio: quem liga e desliga e o Ataque, na janela do golpe.
        // O Awake da Espada tambem faz isso, mas no editor o Awake nao roda — e uma hitbox
        // salva ligada no prefab machucaria quem passasse perto.
        caixa.enabled = false;

        hitbox.AddComponent<Espada>();
    }

    public static Transform CriarFilho(Transform pai, string nome, Vector3 posicaoLocal)
    {
        GameObject novo = new GameObject(nome);
        novo.transform.SetParent(pai, false);
        novo.transform.localPosition = posicaoLocal;
        novo.layer = pai.gameObject.layer;
        return novo.transform;
    }

    private static void DefinirCamadaComApelido(GameObject obj, string preferido, string apelido)
    {
        Camadas.Definir(obj, preferido);

        if (Camadas.Indice(preferido) < 0)
            Camadas.Definir(obj, apelido);
    }

    private static void DefinirTagSeExistir(GameObject obj, string tag)
    {
        try
        {
            obj.tag = tag;
        }
        catch (UnityException)
        {
            Debug.LogWarning($"[Construtor] a tag \"{tag}\" nao existe no projeto. " +
                             "Rode Tools > Jogo > Preparar projeto.");
        }
    }
}
