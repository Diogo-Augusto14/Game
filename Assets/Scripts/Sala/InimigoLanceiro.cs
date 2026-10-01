using UnityEngine;

/// <summary>
/// Cavaleiro da lanca: anda pra ficar na mesma linha ou coluna do jogador e, alinhado, abaixa a
/// lanca (treme, o aviso) e investe reto naquela linha ate chegar perto, parede ou o fim do
/// folego; chegando perto, golpeia. Sair da linha durante o aviso faz ele passar direto.
/// </summary>
public class InimigoLanceiro : InimigoDeGolpe
{
    [SerializeField, Min(0.05f)] private float toleranciaDeAlinhamento = 0.45f;

    [SerializeField, Min(1f)] private float alcanceDaInvestida = 6.5f;

    [SerializeField, Min(0f)] private float avisoDaInvestida = 0.45f;

    [SerializeField, Min(0.5f)] private float velocidadeDaInvestida = 7.5f;

    [SerializeField, Min(0.05f)] private float duracaoDaInvestida = 0.7f;

    [SerializeField, Min(0f)] private float intervaloEntreInvestidas = 1.5f;

    private Cronometro aviso;
    private Cronometro investida;
    private Cronometro descanso;
    private Vector2 rumo;
    private Vector3 posicaoDoDesenho;

    protected override void Awake()
    {
        base.Awake();

        if (desenho != null)
            posicaoDoDesenho = desenho.transform.localPosition;

        descanso.Forcar(intervaloEntreInvestidas * 0.5f);
    }

    protected override void Mover(Vector2 alvo, float distancia, float dt)
    {
        descanso.Contar(dt);

        if (investida.Ativo)
        {
            investida.Contar(dt);
            bool bateu = Physics2D.CircleCast(rb.position, Raio * 0.9f, rumo, velocidadeDaInvestida * dt + 0.05f,
                                              Camadas.MascaraDeParede).collider != null;

            if (bateu)
            {
                investida.Forcar(0f);
                rb.linearVelocity = Vector2.zero;
                descanso.Forcar(intervaloEntreInvestidas);
                return;
            }

            rb.linearVelocity = rumo * velocidadeDaInvestida;

            if (!investida.Ativo)
                descanso.Forcar(intervaloEntreInvestidas);

            return;
        }

        if (aviso.Ativo)
        {
            Frear();
            aviso.Contar(dt);
            Tremer(aviso.Ativo ? 0.05f : 0f);

            if (!aviso.Ativo)
                investida.Forcar(duracaoDaInvestida);

            return;
        }

        // Alinhado (linha ou coluna), perto o bastante e sem nada no meio: abaixa a lanca.
        bool emLinha = Mathf.Abs(alvo.y) < toleranciaDeAlinhamento;
        bool emColuna = Mathf.Abs(alvo.x) < toleranciaDeAlinhamento;

        if (!descanso.Ativo && (emLinha || emColuna) && distancia < alcanceDaInvestida && VeOJogador())
        {
            rumo = emLinha ? new Vector2(Mathf.Sign(alvo.x), 0f) : new Vector2(0f, Mathf.Sign(alvo.y));
            aviso.Forcar(avisoDaInvestida);
            animacao?.OlharPara(rumo);
            Frear();
            return;
        }

        // Ja alinhado (mas longe ou descansando): chega perto pela propria linha. Senao, anda
        // pra entrar na linha ou na coluna mais perto (o eixo que falta menos).
        Vector2 passo;

        if (emLinha)
            passo = new Vector2(Mathf.Sign(alvo.x), 0f);
        else if (emColuna)
            passo = new Vector2(0f, Mathf.Sign(alvo.y));
        else
            passo = Mathf.Abs(alvo.x) < Mathf.Abs(alvo.y) ? new Vector2(Mathf.Sign(alvo.x), 0f) : new Vector2(0f, Mathf.Sign(alvo.y));

        Andar(PeloCaminhoAte(rb.position + passo * 2f, passo), velocidade * 0.9f);
    }

    // Chegou golpeando: a investida acabou ali.
    protected override void AoGolpear(Vector2 direcao)
    {
        investida.Forcar(0f);
        aviso.Forcar(0f);
        Tremer(0f);
        descanso.Forcar(intervaloEntreInvestidas);
    }

    private void Tremer(float forca)
    {
        if (desenho != null)
            desenho.transform.localPosition = posicaoDoDesenho + (Vector3)(Random.insideUnitCircle * forca);
    }
}
