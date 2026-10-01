using UnityEngine;

/// <summary>
/// Demonia da foice: anda devagar ate o jogador e, perto, gira
/// a foice em volta de si duas vezes. O giro acerta em todo o circulo, entao nao adianta
/// dar a volta nele: tem que se afastar durante a preparacao.
///
///   Agindo      -> persegue
///   Preparando  -> ergue a foice (a animacao ate o primeiro giro)
///   Recuperando -> os dois giros e o fim da animacao; o segundo giro sai no meio
/// </summary>
public class InimigoEsqueletoFoice : InimigoComArte
{
    [Header("Giro da foice")]
    [SerializeField, Min(0f)] private float alcanceParaGirar = 1.4f;

    [Tooltip("Telegrafo: segundos erguendo a foice ate o primeiro giro")]
    [SerializeField, Min(0.05f)] private float tempoDePreparo = 0.5f;

    [SerializeField, Min(0.1f)] private float intervaloEntreGiros = 1.6f;

    [SerializeField, Min(0f)] private float danoDoGiro = 15f;

    [Tooltip("Raio do giro, em volta dele")]
    [SerializeField, Min(0.1f)] private float raioDoGiro = 1.1f;

    /// <summary>A tira tem 15 quadros: o primeiro giro no indice 5, o segundo no 10.</summary>
    private const int PrimeiroGiro = 5;
    private const int SegundoGiro = 10;
    private const int QuadrosDoAtaque = 15;

    private Cronometro recarga;
    private Cronometro preparo;
    private float inicioDosGiros;
    private bool segundoFeito;

    private float QuadrosPorSegundo => PrimeiroGiro / tempoDePreparo;

    protected override void Awake()
    {
        base.Awake();
        velocidade = 1.5f;
        recarga.Forcar(intervaloEntreGiros * 0.5f);
    }

    protected override void AtualizarAgindo(float dt)
    {
        recarga.Contar(dt);

        Vector2 alvo = ParaOJogador();
        float distancia = alvo.magnitude;

        if (!recarga.Ativo && distancia <= alcanceParaGirar && VeOJogador())
        {
            EstadoAtual = Estado.Preparando;
            recarga.Forcar(intervaloEntreGiros);   // levar tiro no meio nao faz ele atacar de novo na hora
            preparo.Forcar(tempoDePreparo);
            Frear();

            animacao?.OlharPara(alvo);
            TocarAtaque(clipes?.Ataque, PrimeiroGiro, tempoDePreparo);
            return;
        }

        if (distancia > 0.3f)
            Aproximar(alvo, velocidade, dt);
        else
            Frear();
    }

    protected override void AtualizarPreparando(float dt)
    {
        Frear();
        preparo.Contar(dt);

        if (preparo.Ativo)
            return;

        Girar();
        segundoFeito = false;
        inicioDosGiros = Time.time;
        EstadoAtual = Estado.Recuperando;
    }

    protected override void AtualizarRecuperando(float dt)
    {
        Frear();
        float quadro = (Time.time - inicioDosGiros) * QuadrosPorSegundo + PrimeiroGiro;

        if (!segundoFeito && quadro >= SegundoGiro)
        {
            segundoFeito = true;
            Girar();
        }

        if (quadro < QuadrosDoAtaque)
            return;

        recarga.Forcar(intervaloEntreGiros);
        EstadoAtual = Estado.Agindo;
    }

    private void Girar()
    {
        Sons.Tocar(Som.Pancada, 0.5f);

        foreach (Collider2D c in Physics2D.OverlapCircleAll(rb.position, raioDoGiro))
        {
            GameObject quem = c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.gameObject;

            if (quem == gameObject || !quem.CompareTag("Player"))
                continue;

            Vector2 direcao = (Vector2)quem.transform.position - rb.position;
            quem.GetComponentInParent<IDanificavel>()?.TomarDano(
                new DanoInfo(danoDoGiro, direcao, 5f, rb.position, gameObject));
            break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.8f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, raioDoGiro);
    }
}
