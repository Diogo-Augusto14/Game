using UnityEngine;

/// <summary>
/// Demonio das laminas: nao anda, avanca em arrancadas. Da tres passos rapidos seguidos na
/// direcao do jogador (cada um mirando de novo), com uma pausinha entre eles, e entao descansa.
/// Perto, golpeia. O ritmo "tchuc, tchuc, tchuc ... parado" e o que da pra ler e desviar.
/// </summary>
public class InimigoDuelista : InimigoDeGolpe
{
    [SerializeField, Min(1)] private int passosPorSerie = 3;

    [SerializeField, Min(0.5f)] private float velocidadeDoPasso = 7f;

    [SerializeField, Min(0.05f)] private float duracaoDoPasso = 0.18f;

    [SerializeField, Min(0f)] private float pausaEntrePassos = 0.22f;

    [SerializeField, Min(0f)] private float descansoEntreSeries = 0.9f;

    private Cronometro passo;
    private Cronometro pausa;
    private int passosQueFaltam;
    private Vector2 rumo;

    protected override void Awake()
    {
        base.Awake();
        pausa.Forcar(descansoEntreSeries * Random.Range(0.3f, 1f));
    }

    protected override void Mover(Vector2 alvo, float distancia, float dt)
    {
        if (passo.Ativo)
        {
            passo.Contar(dt);
            rb.linearVelocity = rumo * velocidadeDoPasso;
            return;
        }

        Frear();
        pausa.Contar(dt);

        if (pausa.Ativo)
            return;

        if (passosQueFaltam <= 0)
            passosQueFaltam = passosPorSerie;

        // Cada passo mira de novo (e contorna obstaculo pelo caminho da sala).
        rumo = PeloCaminho(alvo / distancia).normalized;
        passo.Forcar(duracaoDoPasso);
        passosQueFaltam--;
        pausa.Forcar(passosQueFaltam > 0 ? pausaEntrePassos : descansoEntreSeries);
        animacao?.OlharPara(rumo);
    }

    protected override void AoGolpear(Vector2 direcao)
    {
        passo.Forcar(0f);
        passosQueFaltam = 0;
        pausa.Forcar(descansoEntreSeries);
    }
}
