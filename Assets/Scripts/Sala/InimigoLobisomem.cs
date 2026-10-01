using UnityEngine;

/// <summary>
/// Lobisomem: nao vem direto. Rodeia o jogador a meia distancia, como quem espreita, e depois
/// de um tempo vendo ele dispara numa corrida muito rapida ate ficar colado e golpeia. Errou o
/// golpe, volta a rodear. A corrida avisa com um uivo curto.
/// </summary>
public class InimigoLobisomem : InimigoDeGolpe
{
    [SerializeField, Min(0.5f)] private float raioDaEspreita = 3.4f;

    [SerializeField] private Vector2 tempoEspreitando = new Vector2(1.2f, 2.4f);

    [SerializeField, Min(1f)] private float multiplicadorDaCorrida = 2.6f;

    [SerializeField, Min(0.1f)] private float duracaoMaximaDaCorrida = 1f;

    private Cronometro espreita;
    private Cronometro corrida;
    private bool correndo;
    private float sentido = 1f;

    protected override void Awake()
    {
        base.Awake();
        sentido = Random.value < 0.5f ? -1f : 1f;
        espreita.Forcar(Random.Range(tempoEspreitando.x, tempoEspreitando.y));
    }

    protected override void Mover(Vector2 alvo, float distancia, float dt)
    {
        if (correndo)
        {
            corrida.Contar(dt);
            Andar(PeloCaminho(alvo / distancia), velocidade * multiplicadorDaCorrida);

            if (!corrida.Ativo)
                Espreitar();

            return;
        }

        if (VeOJogador())
            espreita.Contar(dt);

        if (!espreita.Ativo)
        {
            correndo = true;
            corrida.Forcar(duracaoMaximaDaCorrida);
            Sons.Tocar(Som.Rugido, 0.4f);
            return;
        }

        // Rodeia: de lado, corrigindo a distancia pro raio da espreita.
        Vector2 frente = alvo / distancia;
        Vector2 lado = new Vector2(-frente.y, frente.x) * sentido;
        Vector2 rumo = lado + frente * Mathf.Clamp(distancia - raioDaEspreita, -1f, 1f);

        if (Physics2D.CircleCast(rb.position, Raio * 0.9f, rumo.normalized, 0.45f, Camadas.MascaraDeParede).collider != null)
            sentido = -sentido;

        Andar(rumo, velocidade * 0.8f);
    }

    protected override void AoGolpear(Vector2 direcao) => Espreitar();

    private void Espreitar()
    {
        correndo = false;
        sentido = Random.value < 0.5f ? -1f : 1f;
        espreita.Forcar(Random.Range(tempoEspreitando.x, tempoEspreitando.y));
    }
}
