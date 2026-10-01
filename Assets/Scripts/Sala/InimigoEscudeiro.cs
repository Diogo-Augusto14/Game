using UnityEngine;

/// <summary>
/// Cavaleiro do escudo: anda devagar de frente pro jogador com o escudo erguido. Tiro que vem
/// pela FRENTE bate no escudo e nao faz nada (faisca e "tlim"); so entra pelos lados, por tras
/// ou quando ele baixa o escudo pra golpear. Ou seja: rodeie ele, ou espere o golpe.
/// </summary>
public class InimigoEscudeiro : InimigoDeGolpe
{
    [Tooltip("Quao de frente o golpe precisa vir pra bater no escudo (1 = so bem de frente, 0 = meia volta)")]
    [SerializeField, Range(0f, 1f)] private float coberturaDoEscudo = 0.45f;

    private Vector2 frente = Vector2.down;

    protected override void Awake()
    {
        base.Awake();
        vida.Bloquear = BateNoEscudo;
    }

    protected override void Mover(Vector2 alvo, float distancia, float dt)
    {
        // Gira o escudo devagar pro jogador: rodeando rapido da pra pegar ele de lado.
        Vector2 desejada = alvo / distancia;
        frente = Vector2.Lerp(frente, desejada, 1f - Mathf.Exp(-3f * dt)).normalized;
        Andar(PeloCaminho(desejada), velocidade * 0.7f);
    }

    private bool BateNoEscudo(DanoInfo golpe)
    {
        // Golpeando, o escudo esta baixado.
        if (EstadoAtual == Estado.Preparando || EstadoAtual == Estado.Recuperando || EstaMorto)
            return false;

        Vector2 deOnde = golpe.Direcao.sqrMagnitude > 0.0001f ? -golpe.Direcao.normalized : Vector2.zero;

        if (Vector2.Dot(deOnde, frente) < coberturaDoEscudo)
            return false;

        Sons.Tocar(Som.Pancada, 0.35f);
        EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Poeira, rb.position + frente * (Raio + 0.1f), new Color(1f, 0.95f, 0.7f), 0.5f);
        return true;
    }
}
