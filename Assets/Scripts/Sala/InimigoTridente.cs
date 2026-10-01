using UnityEngine;

/// <summary>
/// Demonio do tridente: a meia distancia, para, recua o braco (treme, o aviso) e ARREMESSA o
/// tridente reto no jogador, um tiro rapido que atravessa a sala. Sem o tridente fica um tempo
/// so perseguindo e golpeando de mao. Perto demais, golpeia em vez de arremessar.
/// </summary>
public class InimigoTridente : InimigoDeGolpe
{
    [SerializeField] private Vector2 distanciaDoArremesso = new Vector2(2.5f, 7f);

    [SerializeField, Min(0.5f)] private float intervaloEntreArremessos = 3.5f;

    [SerializeField, Min(0f)] private float avisoDoArremesso = 0.5f;

    [SerializeField, Min(0.5f)] private float velocidadeDoTridente = 9f;

    [SerializeField, Min(0f)] private float danoDoTridente = 15f;

    private Cronometro recargaDoArremesso;
    private Cronometro aviso;
    private Vector3 posicaoDoDesenho;

    protected override void Awake()
    {
        base.Awake();
        JeitoDeChegar = Aproximacao.Vaivem;   // chega perto e se afasta
        recargaDoArremesso.Forcar(intervaloEntreArremessos * Random.Range(0.3f, 0.7f));

        if (desenho != null)
            posicaoDoDesenho = desenho.transform.localPosition;
    }

    protected override void Mover(Vector2 alvo, float distancia, float dt)
    {
        recargaDoArremesso.Contar(dt);

        if (aviso.Ativo)
        {
            Frear();
            aviso.Contar(dt);
            animacao?.OlharPara(alvo);

            if (desenho != null)
                desenho.transform.localPosition = posicaoDoDesenho + (Vector3)(Random.insideUnitCircle * (aviso.Ativo ? 0.05f : 0f));

            if (!aviso.Ativo)
            {
                Disparar(AnguloDoJogador(), velocidadeDoTridente, danoDoTridente, new Color(1f, 0.6f, 0.4f), 0.3f);
                recargaDoArremesso.Forcar(intervaloEntreArremessos);
            }

            return;
        }

        if (!recargaDoArremesso.Ativo && distancia >= distanciaDoArremesso.x && distancia <= distanciaDoArremesso.y && VeOJogador())
        {
            aviso.Forcar(avisoDoArremesso);
            return;
        }

        base.Mover(alvo, distancia, dt);
    }
}
