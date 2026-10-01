using UnityEngine;

/// <summary>
/// Urso: lento e pesado, nao e empurrado por tiro. Persegue e golpeia como os outros, mas de
/// tempos em tempos para, se ergue nas patas (treme, o aviso) e bate no chao: um anel de pedras
/// sai dele pra todo lado. Ficar longe nao livra; e preciso passar no vao entre as pedras.
/// </summary>
public class InimigoUrso : InimigoDeGolpe
{
    [SerializeField, Min(1f)] private float intervaloEntrePatadas = 4.5f;

    [SerializeField, Min(0f)] private float avisoDaPatada = 0.7f;

    [SerializeField, Min(3)] private int pedrasPorAnel = 10;

    [SerializeField, Min(0f)] private float danoDaPedra = 10f;

    private Cronometro recargaDaPatada;
    private Cronometro aviso;
    private Vector3 posicaoDoDesenho;

    protected override bool Imparavel => true;

    protected override void Awake()
    {
        base.Awake();
        recargaDaPatada.Forcar(intervaloEntrePatadas * Random.Range(0.5f, 0.9f));

        if (desenho != null)
            posicaoDoDesenho = desenho.transform.localPosition;
    }

    protected override void Mover(Vector2 alvo, float distancia, float dt)
    {
        recargaDaPatada.Contar(dt);

        if (aviso.Ativo)
        {
            Frear();
            aviso.Contar(dt);

            if (desenho != null)
                desenho.transform.localPosition = posicaoDoDesenho + (Vector3)(Random.insideUnitCircle * (aviso.Ativo ? 0.07f : 0f));

            if (!aviso.Ativo)
                BaterNoChao();

            return;
        }

        if (!recargaDaPatada.Ativo && distancia < 6f)
        {
            aviso.Forcar(avisoDaPatada);
            Sons.Tocar(Som.Rugido, 0.6f);
            return;
        }

        base.Mover(alvo, distancia, dt);
    }

    private void BaterNoChao()
    {
        Sons.Tocar(Som.Pancada, 0.8f);
        EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Poeira, rb.position, new Color(0.85f, 0.75f, 0.6f), 1.6f);

        float giro = Random.value * 360f;

        for (int i = 0; i < pedrasPorAnel; i++)
            Disparar(giro + i * 360f / pedrasPorAnel, 3.2f, danoDaPedra, new Color(0.8f, 0.7f, 0.55f), 0.35f);

        recargaDaPatada.Forcar(intervaloEntrePatadas);
    }
}
