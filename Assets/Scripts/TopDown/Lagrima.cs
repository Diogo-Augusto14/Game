using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O projetil do Isaac. Voa reto, e quando percorre o alcance "cai" (encolhe e some).
/// Bate em qualquer <see cref="IDanificavel"/> e estoura; bate em parede/pedra e estoura.
///
/// Quem cria e o <see cref="AtiradorTopDown"/>, que chama <see cref="Disparar"/> logo
/// depois de montar o objeto. Sozinha ela nao sabe dano, alcance nem quem atirou.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Lagrima : MonoBehaviour
{
    [Tooltip("Segundos da animacao de estourar (encolher e sumir)")]
    [SerializeField, Min(0f)] private float duracaoDoEstouro = 0.08f;

    // ---------------- estado ----------------
    private Rigidbody2D rb;
    private Collider2D corpo;
    private GameObject dono;
    private float dano;
    private float alcance;
    private float forcaEmpurrao;
    private float percorrido;
    private bool acabou;
    private bool atravessa;
    private bool teleguiada;
    private bool apontar;
    private readonly HashSet<IDanificavel> acertados = new HashSet<IDanificavel>();
    private EfeitoDaFlecha efeito;

    /// <summary>Graus por segundo que a lagrima teleguiada consegue virar.</summary>
    private const float GiroDaTeleguiada = 300f;

    /// <summary>Distancia maxima em que a teleguiada "ve" um inimigo.</summary>
    private const float VisaoDaTeleguiada = 5f;

    public GameObject Dono => dono;

    /// <summary>Sinergia Golpe de Bigorna: o acerto conta como golpe forte.</summary>
    public bool Pesada { get; set; }

    /// <summary>Sinergia Polvora em Chamas: raio da explosaozinha ao acertar (0 = nao explode).</summary>
    public float RaioDaExplosao { get; set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        corpo = GetComponent<CircleCollider2D>();

        // Dynamic + trigger: detecta tudo (parede estatica, inimigo, pedra) sem empurrar
        // nada por colisao — quem empurra e o DanoInfo, pelo Vida.
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        corpo.isTrigger = true;
    }

    /// <summary>Solta a lagrima. <paramref name="velocidade"/> ja vem com direcao.</summary>
    public void Disparar(GameObject quemAtirou, Vector2 velocidade, float quantoDano, float ateOnde, float empurrao)
    {
        dono = quemAtirou;
        dano = quantoDano;
        alcance = Mathf.Max(0.1f, ateOnde);
        forcaEmpurrao = empurrao;
        percorrido = 0f;
        acabou = false;

        rb.linearVelocity = velocidade;
        Apontar();
    }

    private void Apontar()
    {
        if (!apontar || rb.linearVelocity.sqrMagnitude < 0.0001f)
            return;

        float angulo = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        rb.rotation = angulo;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo);
    }

    /// <summary>Gira o desenho pro rumo do voo (flecha). Sem isto fica parado (lagrima redonda).</summary>
    public void ApontarProRumo()
    {
        apontar = true;
    }

    /// <summary>Flecha especial (explosiva, gelo, veneno, ricochete...): ela cuida do resto.</summary>
    public void UsarEfeito(EfeitoDaFlecha novo)
    {
        efeito = novo;
    }

    /// <summary>Efeitos de item: atravessar inimigos e/ou curvar atras do mais perto.</summary>
    public void DefinirEfeitos(bool atravessaInimigos, bool perseguir)
    {
        atravessa = atravessaInimigos;
        teleguiada = perseguir;
    }

    private void FixedUpdate()
    {
        if (acabou)
            return;

        if (teleguiada)
        {
            Curvar();
            Apontar();
        }

        // Conta a distancia de verdade percorrida: se herdou a velocidade do jogador,
        // o alcance continua o mesmo em qualquer direcao.
        percorrido += rb.linearVelocity.magnitude * Time.fixedDeltaTime;

        if (percorrido >= alcance)
            Estourar();
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (acabou)
            return;

        // Nao acerta quem atirou nem as outras lagrimas.
        if (dono != null && outro.transform.IsChildOf(dono.transform))
            return;

        if (outro.GetComponentInParent<Lagrima>() != null)
            return;

        // Buraco no chao: a lagrima voa por cima.
        if (Fosso.EhBuraco(outro))
            return;

        // Parede e pedra primeiro: a pedra tem Vida (pra bomba quebrar), mas lagrima nao
        // pode quebrar pedra.
        if (!outro.isTrigger && (Camadas.MascaraDeParede & (1 << outro.gameObject.layer)) != 0)
        {
            // Mesa do Old Prison: o tiro tomba e racha ela.
            if (outro.TryGetComponent(out Mesa mesa))
                mesa.Acertar(rb.position);

            // Flecha ricochete: quica em vez de quebrar.
            if (efeito != null && efeito.Ricochetear(outro))
                return;

            Estourar();
            return;
        }

        IDanificavel alvo = outro.GetComponentInParent<IDanificavel>();

        if (alvo != null)
        {
            // Atravessando, cada inimigo leva um golpe so desta lagrima.
            if (!acertados.Add(alvo))
                return;

            Vector2 direcao = rb.linearVelocity.sqrMagnitude > 0.0001f ? rb.linearVelocity : Vector2.right;
            alvo.TomarDano(new DanoInfo(dano, direcao, Pesada ? forcaEmpurrao * 1.6f : forcaEmpurrao, transform.position, dono,
                                        Pesada ? PesoDoGolpe.Forte : PesoDoGolpe.Leve));

            if (RaioDaExplosao > 0f)
                Estilhacar(alvo);

            if (efeito != null)
                efeito.AoAcertar(alvo);

            if (!atravessa)
                Estourar();

            return;
        }

        // Trigger de cenario (zona, porta) nao para a lagrima; so coisa solida.
        if (!outro.isTrigger)
            Estourar();
    }

    /// <summary>
    /// Polvora em Chamas: fogo em volta do acerto, metade do dano em quem estiver perto (menos
    /// no que ja levou o tiro). Sem o som da bomba: com tiro rapido virava barulheira.
    /// </summary>
    private void Estilhacar(IDanificavel atingido)
    {
        Vector2 centro = transform.position;
        EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Fogo, centro, new Color(1f, 0.6f, 0.2f), RaioDaExplosao * 1.4f);

        foreach (Collider2D c in Physics2D.OverlapCircleAll(centro, RaioDaExplosao))
        {
            InimigoDeSala inimigo = c.GetComponentInParent<InimigoDeSala>();

            if (inimigo == null || inimigo.EstaMorto || !inimigo.TryGetComponent(out Vida v) || (IDanificavel)v == atingido)
                continue;

            if (!acertados.Add(v))
                continue;

            Vector2 lado = (Vector2)inimigo.transform.position - centro;
            v.TomarDano(new DanoInfo(dano * 0.5f, lado, 2f, centro, dono));
        }
    }

    /// <summary>Vira a velocidade aos poucos na direcao do inimigo acordado mais perto.</summary>
    private void Curvar()
    {
        InimigoDeSala alvo = null;
        float melhor = VisaoDaTeleguiada * VisaoDaTeleguiada;

        foreach (InimigoDeSala inimigo in InimigoDeSala.Ativos)
        {
            if (inimigo == null || inimigo.EstaMorto || inimigo.EstadoAtual == InimigoDeSala.Estado.Dormindo)
                continue;

            float d = ((Vector2)inimigo.transform.position - rb.position).sqrMagnitude;

            if (d < melhor)
            {
                melhor = d;
                alvo = inimigo;
            }
        }

        if (alvo == null)
            return;

        Vector2 v = rb.linearVelocity;
        Vector2 querido = ((Vector2)alvo.transform.position - rb.position).normalized * v.magnitude;
        float angulo = Vector2.SignedAngle(v, querido);
        float giro = Mathf.Clamp(angulo, -GiroDaTeleguiada * Time.fixedDeltaTime, GiroDaTeleguiada * Time.fixedDeltaTime);
        rb.linearVelocity = Quaternion.Euler(0f, 0f, giro) * v;
    }

    /// <summary>Para, desliga a colisao, encolhe e some.</summary>
    public void Estourar()
    {
        if (acabou)
            return;

        acabou = true;
        rb.linearVelocity = Vector2.zero;
        corpo.enabled = false;
        Sons.Tocar(Som.Respingo, 0.35f, 0.15f);

        if (efeito != null)
            efeito.AoEstourar();

        StartCoroutine(RotinaEstouro());
    }

    private IEnumerator RotinaEstouro()
    {
        Vector3 escalaInicial = transform.localScale;
        float tempo = 0f;

        while (tempo < duracaoDoEstouro)
        {
            tempo += Time.deltaTime;
            float t = tempo / duracaoDoEstouro;

            // Espalha um pouco e encolhe: le como "splash" mesmo sem arte.
            transform.localScale = escalaInicial * Mathf.Lerp(1.4f, 0f, t);
            yield return null;
        }

        Destroy(gameObject);
    }
}
