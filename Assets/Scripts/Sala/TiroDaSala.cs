using UnityEngine;

/// <summary>
/// Bolinha que anda reto e da dano no primeiro alvo valido. Some ao bater em parede ou
/// depois de um tempo.
///
/// Serve pros dois lados: <c>atingeJogador</c> ligado = tiro de inimigo (so acerta quem tem
/// a tag do jogador); desligado = tiro do jogador (so acerta <see cref="InimigoDeSala"/>).
/// Assim inimigo nao mata inimigo e o jogador nao se acerta.
///
/// O tiro de inimigo sai no estilo de quem atirou (<see cref="EstilosDeTiro"/>): bola de
/// fogo, raio verde, bala de canhao, flecha, pedra... Quem nao tem estilo usa a gema.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class TiroDaSala : MonoBehaviour
{
    [SerializeField, Min(0f)] private float dano = 10f;

    [SerializeField, Min(0f)] private float empurrao = 2f;

    [SerializeField, Min(0.1f)] private float duracao = 3f;

    [SerializeField] private bool atingeJogador = true;

    [SerializeField] private string tagDoJogador = "Player";

    private Rigidbody2D rb;
    private GameObject dono;
    private float nascimento;
    private bool gasto;
    private bool contado;
    private bool balaDePadrao;
    private Color corDaBala;

    // As balas inimigas no ar: o teto dos padroes (PadroesDeBala.TetoDeBalas) e a limpeza da sala.
    private static readonly System.Collections.Generic.List<TiroDaSala> dosInimigos = new System.Collections.Generic.List<TiroDaSala>();

    /// <summary>Quantas balas inimigas estao voando agora.</summary>
    public static int NoAr => dosInimigos.Count;

    // Com "Enter Play Mode" sem recarregar o dominio, a lista sobreviveria entre Plays.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar() => dosInimigos.Clear();

    /// <summary>
    /// Apaga toda bala inimiga no ar, cada uma com o seu estourinho (sala limpa: o Gungeon nao deixa
    /// bala sobrando matar quem ja ganhou). As do jogador ficam.
    /// </summary>
    public static void LimparDosInimigos()
    {
        foreach (TiroDaSala tiro in dosInimigos.ToArray())
            if (tiro != null)
                tiro.Anular();
    }

    /// <summary>
    /// Cria e dispara um projetil. Diametro padrao 0.3 unidade. <paramref name="balaDePadrao"/>: bala
    /// dos padroes (<see cref="PadroesDeBala"/>), sempre com a mesma cara legível (miolo claro, contorno
    /// escuro e um brilho na cor dela) em vez do estilo de quem atirou: o estilo das balas de ferro e
    /// das chamas escuras some no chao escuro, e num padrao denso a bala tem que se ver de longe.
    /// </summary>
    public static TiroDaSala Disparar(Vector2 origem, Vector2 velocidade, float dano, GameObject dono,
                                    bool atingeJogador, Color cor, float diametro = 0.3f, bool balaDePadrao = false)
    {
        GameObject obj = new GameObject(atingeJogador ? "TiroDoInimigo" : "TiroDoJogador");
        obj.transform.position = origem;
        obj.transform.localScale = Vector3.one * diametro;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();

        if (balaDePadrao)
        {
            sr.sprite = ArteDasArmas.Bala();
            sr.color = Color.Lerp(cor, Color.white, 0.25f);
            sr.sortingOrder = 21;

            // O brilho atras, na cor da bala: separa ela do chao mesmo com a bala escura ao lado.
            SpriteRenderer brilho = FormasDaSala.Desenho(obj.transform, "Brilho", HaloCintilante.Suave(),
                                                         new Color(cor.r, cor.g, cor.b, 0.55f), Vector2.zero, Vector2.one * 2.6f, 19);
            brilho.transform.localPosition = Vector3.zero;
        }
        else
        {
            // A gema do pacote ja tem cor: a do tiro so tinge pela metade.
            Sprite gema = ArteImportada.TiroMagico;
            sr.sprite = gema != null ? gema : ArteGerada.Bola();
            sr.color = gema != null ? Color.Lerp(Color.white, cor, 0.5f) : cor;
            sr.sortingOrder = 20;
        }

        Rigidbody2D corpo = obj.AddComponent<Rigidbody2D>();
        corpo.gravityScale = 0f;
        corpo.freezeRotation = true;
        corpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D circulo = obj.AddComponent<CircleCollider2D>();
        circulo.isTrigger = true;
        circulo.radius = 0.5f;

        TiroDaSala p = obj.AddComponent<TiroDaSala>();
        p.dano = dano;
        p.dono = dono;
        p.atingeJogador = atingeJogador;
        p.balaDePadrao = balaDePadrao;
        p.corDaBala = cor;

        corpo.linearVelocity = velocidade;

        if (atingeJogador)
        {
            dosInimigos.Add(p);
            p.contado = true;

            // Bala de padrao: o som de cada volta e do AtiradorDePadroes (uma vez so, nao por bala).
            if (!balaDePadrao)
            {
                EstiloDeTiro estilo = EstilosDeTiro.DoAtirador(dono);
                EstilosDeTiro.Aplicar(p, estilo);
                Sons.Tocar(EstilosDeTiro.SomDoDisparo(estilo), 0.45f);

                // O estilo (pedra, bala de ferro, brasa escura) some no chao escuro: um brilho fraco na cor
                // do tiro, atras dele, deixa toda bala inimiga visivel (as dos chefes tambem).
                FormasDaSala.Desenho(obj.transform, "Brilho", HaloCintilante.Suave(), new Color(cor.r, cor.g, cor.b, 0.4f),
                                     Vector2.zero, Vector2.one * 2.4f, 19);
            }
        }

        return p;
    }

    private void OnDestroy()
    {
        if (contado)
            dosInimigos.Remove(this);
    }

    /// <summary>O desenho do tiro (o do estilo, se tiver; senao o da raiz).</summary>
    public SpriteRenderer Desenho
    {
        get
        {
            if (TryGetComponent(out VisualDoProjetil visual) && visual.Desenho != null)
                return visual.Desenho;

            return GetComponent<SpriteRenderer>();
        }
    }

    /// <summary>Troca o estilo deste tiro (a bolha do chefe saltador, por exemplo).</summary>
    public void Vestir(EstiloDeTiro estilo)
    {
        if (TryGetComponent(out ComportamentoDoTiro antigo))
        {
            antigo.enabled = false;
            Destroy(antigo);
        }

        EstilosDeTiro.Aplicar(this, estilo);
    }

    /// <summary>Mostra o efeito de impacto do estilo, onde o tiro esta (a bala de padrao estoura em poeira na cor dela).</summary>
    public void MostrarImpacto()
    {
        if (TryGetComponent(out VisualDoProjetil visual))
            visual.MostrarImpacto();
        else if (balaDePadrao)
            EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Poeira, transform.position, new Color(corDaBala.r, corDaBala.g, corDaBala.b, 0.8f), 0.4f);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        nascimento = Time.time;
    }

    private void Update()
    {
        if (Time.time - nascimento >= duracao)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (gasto)
            return;

        GameObject quem = outro.attachedRigidbody != null ? outro.attachedRigidbody.gameObject : outro.gameObject;

        if (quem == dono || outro.isTrigger)
            return;

        // Parede (ou porta fechada): some.
        if ((Camadas.MascaraDeParede & (1 << outro.gameObject.layer)) != 0)
        {
            // Bicho tambem gasta a mesa que o heroi usa de barricada.
            if (outro.TryGetComponent(out Mesa mesa))
                mesa.Acertar(rb.position);

            Gastar();
            return;
        }

        if (!EhAlvo(quem))
            return;

        IDanificavel alvo = quem.GetComponentInParent<IDanificavel>();

        if (alvo == null)
            return;

        Vector2 direcao = rb.linearVelocity;
        alvo.TomarDano(new DanoInfo(dano, direcao, empurrao, rb.position, dono));
        Gastar();
    }

    /// <summary>Tiro de inimigo (so acerta o jogador)?</summary>
    public bool AtingeJogador => atingeJogador;

    /// <summary>Some sem acertar ninguem (bloqueado por um orbe, por exemplo).</summary>
    public void Anular()
    {
        if (!gasto)
            Gastar();
    }

    private bool EhAlvo(GameObject quem)
    {
        if (atingeJogador)
            return quem.CompareTag(tagDoJogador);

        return quem.GetComponentInParent<InimigoDeSala>() != null;
    }

    private void Gastar()
    {
        gasto = true;
        MostrarImpacto();
        Destroy(gameObject);
    }
}
