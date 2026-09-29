using UnityEngine;

/// <summary>
/// Goblin da tocha (Tiny Swords): rapido, corre atras do jogador e, chegando perto, gira
/// a tocha num arco de fogo. Tem arte do golpe pro lado, pra baixo e pra cima.
///
///   Agindo      -> persegue
///   Preparando  -> ergue a tocha (a animacao ate o quadro do golpe)
///   golpe       -> dano num circulo a frente dele
///   Recuperando -> termina o giro parado; da tempo de revidar
///
/// O golpe sai na direcao em que o jogador ESTAVA quando ele ergueu a tocha.
/// </summary>
public class InimigoGoblinTocha : InimigoComArte
{
    [Header("Golpe de tocha")]
    [SerializeField, Min(0f)] private float alcanceParaGolpear = 1.2f;

    [Tooltip("Telegrafo: segundos erguendo a tocha")]
    [SerializeField, Min(0f)] private float tempoDePreparo = 0.35f;

    [SerializeField, Min(0f)] private float tempoDeRecuperacao = 0.4f;

    [SerializeField, Min(0.1f)] private float intervaloEntreGolpes = 1.1f;

    [SerializeField, Min(0f)] private float danoDoGolpe = 15f;

    [Tooltip("Raio da area do golpe, centrada a frente dele")]
    [SerializeField, Min(0.1f)] private float raioDoGolpe = 0.6f;

    /// <summary>A tira de ataque tem 6 quadros; o fogo aparece no quarto (indice 3).</summary>
    private const int QuadroDoGolpe = 3;

    private Cronometro recarga;
    private Cronometro preparo;
    private Cronometro recuperacao;
    private Vector2 direcaoDoGolpe = Vector2.right;

    protected override void Awake()
    {
        base.Awake();
        velocidade = 2.4f;
        recarga.Forcar(intervaloEntreGolpes * 0.5f);
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
            direcaoDoGolpe = alvo / distancia;
            EstadoAtual = Estado.Preparando;
            preparo.Forcar(tempoDePreparo);
            Frear();

            animacao?.OlharPara(direcaoDoGolpe);
            TocarAtaque(QuadrosDoGolpe(direcaoDoGolpe), QuadroDoGolpe, tempoDePreparo);
            return;
        }

        Andar(alvo / distancia, VeOJogador() ? velocidade : velocidade * 0.6f);
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
