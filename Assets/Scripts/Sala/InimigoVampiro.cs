using UnityEngine;

/// <summary>
/// Vampiro (Enemy Animations Set): vem devagar e, a meia distancia, se encolhe na capa
/// e da um bote rapido na direcao do jogador. Se o bote acerta, ele suga vida e se cura.
///
///   Agindo      -> se aproxima devagar
///   Preparando  -> se encolhe na capa (o telegrafo); a direcao do bote fica marcada aqui
///   Recuperando -> o bote e o fim da animacao
/// </summary>
public class InimigoVampiro : InimigoComArte
{
    [Header("Bote")]
    [SerializeField, Min(0f)] private float distanciaDoBote = 3.5f;

    [Tooltip("Telegrafo: segundos se encolhendo antes do bote")]
    [SerializeField, Min(0.05f)] private float tempoDePreparo = 0.75f;

    [SerializeField, Min(0.1f)] private float intervaloEntreBotes = 2.8f;

    [SerializeField, Min(0f)] private float velocidadeDoBote = 8f;

    [SerializeField, Min(0.05f)] private float duracaoDoBote = 0.35f;

    [SerializeField, Min(0f)] private float tempoDeRecuperacao = 0.5f;

    [SerializeField, Min(0f)] private float danoDoBote = 15f;

    [Tooltip("Vida que ele recupera quando o bote acerta")]
    [SerializeField, Min(0f)] private float vidaSugada = 10f;

    /// <summary>A tira tem 16 quadros; a capa se abre pro bote no indice 9.</summary>
    private const int QuadroDoBote = 9;

    private Cronometro recarga;
    private Cronometro preparo;
    private Cronometro bote;
    private Cronometro recuperacao;
    private Vector2 direcaoDoBote = Vector2.right;
    private bool acertou;

    protected override void Awake()
    {
        base.Awake();
        velocidade = 1.3f;
        recarga.Forcar(intervaloEntreBotes * 0.5f);
    }

    protected override void AtualizarAgindo(float dt)
    {
        recarga.Contar(dt);

        Vector2 alvo = ParaOJogador();
        float distancia = alvo.magnitude;

        if (!recarga.Ativo && distancia <= distanciaDoBote && distancia > 0.0001f && VeOJogador())
        {
            direcaoDoBote = alvo / distancia;
            EstadoAtual = Estado.Preparando;
            recarga.Forcar(intervaloEntreBotes);   // levar tiro no meio nao faz ele atacar de novo na hora
            preparo.Forcar(tempoDePreparo);
            Frear();

            animacao?.OlharPara(direcaoDoBote);
            TocarAtaque(clipes?.Ataque, QuadroDoBote, tempoDePreparo);
            return;
        }

        Andar(alvo, distancia > 0.5f ? velocidade : 0f);
    }

    protected override void AtualizarPreparando(float dt)
    {
        Frear();
        preparo.Contar(dt);

        if (preparo.Ativo)
            return;

        Sons.Tocar(Som.Pulo, 0.5f);
        acertou = false;
        bote.Forcar(duracaoDoBote);
        recuperacao.Forcar(duracaoDoBote + tempoDeRecuperacao);
        EstadoAtual = Estado.Recuperando;
    }

    protected override void AtualizarRecuperando(float dt)
    {
        bote.Contar(dt);
        recuperacao.Contar(dt);

        if (bote.Ativo)
        {
            rb.linearVelocity = direcaoDoBote * velocidadeDoBote;

            if (!acertou)
                Morder();
        }
        else
        {
            Frear();
        }

        if (recuperacao.Ativo)
            return;

        recarga.Forcar(intervaloEntreBotes);
        EstadoAtual = Estado.Agindo;
    }

    private void Morder()
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(rb.position + direcaoDoBote * Raio, Raio * 1.3f))
        {
            GameObject quem = c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.gameObject;

            if (quem == gameObject || !quem.CompareTag("Player"))
                continue;

            quem.GetComponentInParent<IDanificavel>()?.TomarDano(
                new DanoInfo(danoDoBote, direcaoDoBote, 5f, rb.position, gameObject));

            acertou = true;
            vida.Curar(vidaSugada);
            Sons.Tocar(Som.Coracao, 0.4f);
            break;
        }
    }
}
