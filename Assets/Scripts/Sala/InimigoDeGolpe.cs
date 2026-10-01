using UnityEngine;

/// <summary>
/// Inimigo corpo a corpo com arte importada: corre atras do jogador e, chegando perto,
/// ergue a arma e golpeia a frente. Usado pelo goblin da tocha (Tiny Swords, com golpe
/// pro lado, pra baixo e pra cima) e pelo esqueleto da espada (Enemy Animations Set).
///
///   Agindo      -> persegue
///   Preparando  -> ergue a tocha (a animacao ate o quadro do golpe)
///   golpe       -> dano num circulo a frente dele
///   Recuperando -> termina o giro parado; da tempo de revidar
///
/// O golpe sai na direcao em que o jogador ESTAVA quando ele ergueu a arma.
///
/// E tambem a base dos lutadores com jeito proprio (lobisomem, lanceiro, escudeiro, urso...):
/// eles trocam so o <see cref="Mover"/> (como chegam no jogador) e, se quiserem, o
/// <see cref="AoGolpear"/> (o que acontece junto com o golpe).
/// </summary>
public class InimigoDeGolpe : InimigoComArte
{
    [Header("Golpe")]
    [SerializeField, Min(0f)] protected float alcanceParaGolpear = 1.2f;

    [Tooltip("Telegrafo: segundos erguendo a arma")]
    [SerializeField, Min(0f)] protected float tempoDePreparo = 0.35f;

    [SerializeField, Min(0f)] private float tempoDeRecuperacao = 0.4f;

    [SerializeField, Min(0.1f)] protected float intervaloEntreGolpes = 1.1f;

    [SerializeField, Min(0f)] protected float danoDoGolpe = 15f;

    [Tooltip("Raio da area do golpe, centrada a frente dele")]
    [SerializeField, Min(0.1f)] protected float raioDoGolpe = 0.6f;

    [Tooltip("Quadro da animacao de ataque em que o golpe acerta")]
    [SerializeField, Min(0)] private int quadroDoGolpe = 3;

    protected Cronometro recarga;
    private Cronometro preparo;
    private Cronometro recuperacao;
    private Vector2 direcaoDoGolpe = Vector2.right;

    protected override void Awake()
    {
        base.Awake();
        velocidade = 2.4f;
        recarga.Forcar(intervaloEntreGolpes * 0.5f);
    }

    /// <summary>Ajusta o bicho (a fabrica chama logo depois de montar).</summary>
    public void Ajustar(float novaVelocidade, int novoQuadroDoGolpe, float novoPreparo, float novoDano)
    {
        velocidade = novaVelocidade;
        quadroDoGolpe = novoQuadroDoGolpe;
        tempoDePreparo = novoPreparo;
        danoDoGolpe = novoDano;
    }

    protected override void AtualizarAgindo(float dt)
    {
        recarga.Contar(dt);

        Vector2 alvo = ParaOJogador();
        float distancia = alvo.magnitude;

        if (distancia < 0.0001f)
        {
            Frear();
            return;
        }

        if (!recarga.Ativo && distancia <= alcanceParaGolpear && VeOJogador())
        {
            ComecarGolpe(alvo / distancia);
            return;
        }

        Mover(alvo, distancia, dt);
    }

    /// <summary>
    /// Como ele chega no jogador. O padrao e correr atras contornando obstaculo; os lutadores com
    /// jeito proprio trocam isto (o golpe, quando chegam perto, continua vindo da base).
    /// </summary>
    protected virtual void Mover(Vector2 alvo, float distancia, float dt)
    {
        Andar(PeloCaminho(alvo / distancia), VeOJogador() ? velocidade : velocidade * 0.6f);
    }

    /// <summary>Ergue a arma pra golpear na <paramref name="direcao"/> (o telegrafo; o golpe sai no fim).</summary>
    protected void ComecarGolpe(Vector2 direcao)
    {
        direcaoDoGolpe = direcao.normalized;
        EstadoAtual = Estado.Preparando;
        recarga.Forcar(intervaloEntreGolpes);   // levar tiro no meio nao faz ele atacar de novo na hora
        preparo.Forcar(tempoDePreparo);
        Frear();

        animacao?.OlharPara(direcaoDoGolpe);
        TocarAtaque(QuadrosDoGolpe(direcaoDoGolpe), quadroDoGolpe, tempoDePreparo);
    }

    /// <summary>Junto com o golpe (o espadao solta uma onda, por exemplo). <paramref name="direcao"/> e a do golpe.</summary>
    protected virtual void AoGolpear(Vector2 direcao)
    {
    }

    /// <summary>Golpe pra cima ou pra baixo quando o jogador esta mais na vertical.</summary>
    private Sprite[] QuadrosDoGolpe(Vector2 direcao)
    {
        if (clipes == null)
            return null;

        if (Mathf.Abs(direcao.y) > Mathf.Abs(direcao.x))
        {
            Sprite[] vertical = direcao.y > 0f ? clipes.AtaqueCima : clipes.AtaqueBaixo;

            if (vertical != null)
                return vertical;
        }

        return clipes.Ataque;
    }

    protected override void AtualizarPreparando(float dt)
    {
        Frear();
        preparo.Contar(dt);

        if (preparo.Ativo)
            return;

        Golpear();
        EstadoAtual = Estado.Recuperando;
        recuperacao.Forcar(tempoDeRecuperacao);
    }

    protected override void AtualizarRecuperando(float dt)
    {
        Frear();
        recuperacao.Contar(dt);

        if (recuperacao.Ativo)
            return;

        recarga.Forcar(intervaloEntreGolpes);
        EstadoAtual = Estado.Agindo;
    }

    private void Golpear()
    {
        Sons.Tocar(Som.Pancada, 0.55f);
        AoGolpear(direcaoDoGolpe);

        Vector2 centro = rb.position + direcaoDoGolpe * (Raio + raioDoGolpe * 0.6f);

        foreach (Collider2D c in Physics2D.OverlapCircleAll(centro, raioDoGolpe))
        {
            GameObject quem = c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.gameObject;

            if (quem == gameObject || !quem.CompareTag("Player"))
                continue;

            quem.GetComponentInParent<IDanificavel>()?.TomarDano(
                new DanoInfo(danoDoGolpe, direcaoDoGolpe, 5f, centro, gameObject));
            break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f);
        Vector2 origem = transform.position;
        Gizmos.DrawWireSphere(origem + direcaoDoGolpe * (Raio + raioDoGolpe * 0.6f), raioDoGolpe);
    }
}
