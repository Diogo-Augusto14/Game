using UnityEngine;

/// <summary>
/// Esqueleto do espadao: lento, ergue a espada por mais tempo e o golpe solta uma ONDA de corte
/// que segue reta pela sala. Escapar do golpe de perto nao basta: tem que sair da linha dele.
/// De longe ele tambem ataca (golpeia o ar e a onda vem), entao ficar parado longe nao e seguro.
/// </summary>
public class InimigoEspadao : InimigoDeGolpe
{
    [SerializeField, Min(0.5f)] private float velocidadeDaOnda = 5.5f;

    [SerializeField, Min(0f)] private float danoDaOnda = 12f;

    [Tooltip("De longe (ate esta distancia, vendo o jogador) ele golpeia o ar so pra soltar a onda")]
    [SerializeField, Min(0f)] private float alcanceDaOnda = 6f;

    [SerializeField, Min(0.5f)] private float intervaloDaOndaDeLonge = 3f;

    private Cronometro ondaDeLonge;

    protected override void Awake()
    {
        base.Awake();
        ondaDeLonge.Forcar(intervaloDaOndaDeLonge * Random.Range(0.5f, 1f));
    }

    protected override void Mover(Vector2 alvo, float distancia, float dt)
    {
        ondaDeLonge.Contar(dt);

        if (!ondaDeLonge.Ativo && !recarga.Ativo && distancia <= alcanceDaOnda && VeOJogador())
        {
            ondaDeLonge.Forcar(intervaloDaOndaDeLonge);
            ComecarGolpe(alvo / distancia);
            return;
        }

        base.Mover(alvo, distancia, dt);
    }

    protected override void AoGolpear(Vector2 direcao)
    {
        Disparar(Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg, velocidadeDaOnda, danoDaOnda, new Color(0.85f, 0.95f, 1f), 0.45f);
    }
}
