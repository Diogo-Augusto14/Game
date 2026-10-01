using UnityEngine;

/// <summary>
/// Bruxo: nao anda, se teletransporta. Aparece num canto da sala, ergue o cajado (o aviso),
/// solta tres orbes em leque no jogador e some numa fumaca; um instante depois aparece em
/// outro lugar e repete. Sumido ele nao apanha nem machuca: a hora de acertar e entre ele
/// aparecer e sumir de novo.
///
///   Agindo      -> visivel, parado, um tiquinho antes de erguer o cajado
///   Preparando  -> ergue o cajado (a animacao de ataque) e atira no fim
///   Recuperando -> some (desbota), fica sumido e reaparece em outro ponto (aparece desbotado)
/// </summary>
public class InimigoBruxo : InimigoDeSala
{
    [SerializeField, Min(0f)] private float esperaAntesDoTiro = 0.45f;

    [SerializeField, Min(0f)] private float tempoDePreparo = 0.6f;

    [SerializeField, Min(0f)] private float tempoSumido = 1.1f;

    [Tooltip("Segundos desbotando pra sumir e pra aparecer")]
    [SerializeField, Min(0.05f)] private float tempoDoDesbote = 0.35f;

    [SerializeField, Min(0f)] private float danoDoTiro = 10f;

    [SerializeField, Min(0.1f)] private float velocidadeDoTiro = 4.5f;

    [SerializeField, Range(0f, 60f)] private float aberturaDoLeque = 18f;

    [SerializeField] private Color corDoTiro = new Color(0.75f, 0.4f, 1f);

    private enum Fase { Sumindo, Sumido, Aparecendo }

    private Cronometro espera;
    private Cronometro preparo;
    private Cronometro relogio;
    private Fase fase;

    protected override void Awake()
    {
        base.Awake();
        espera.Forcar(esperaAntesDoTiro + Random.Range(0.3f, 0.9f));
    }

    protected override void AtualizarAgindo(float dt)
    {
        Frear();
        espera.Contar(dt);

        if (espera.Ativo)
            return;

        EstadoAtual = Estado.Preparando;
        preparo.Forcar(tempoDePreparo);
    }

    protected override void AtualizarPreparando(float dt)
    {
        Frear();
        preparo.Contar(dt);

        if (preparo.Ativo)
            return;

        float angulo = AnguloDoJogador();

        for (int i = -1; i <= 1; i++)
            Disparar(angulo + i * aberturaDoLeque, velocidadeDoTiro, danoDoTiro, corDoTiro);

        // Atirou: some.
        fase = Fase.Sumindo;
        relogio.Forcar(tempoDoDesbote);
        EstadoAtual = Estado.Recuperando;
    }

    protected override void AtualizarRecuperando(float dt)
    {
        Frear();
        relogio.Contar(dt);
        float t = 1f - relogio.Restante / Mathf.Max(0.01f, fase == Fase.Sumido ? tempoSumido : tempoDoDesbote);

        switch (fase)
        {
            case Fase.Sumindo:
                Intangivel(true, Mathf.Lerp(1f, 0f, t));

                if (relogio.Ativo)
                    return;

                EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Poeira, rb.position, new Color(0.7f, 0.5f, 1f, 0.8f), 1.1f);
                fase = Fase.Sumido;
                relogio.Forcar(tempoSumido);
                return;

            case Fase.Sumido:
                Intangivel(true, 0f);

                if (relogio.Ativo)
                    return;

                Teletransportar();
                EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Poeira, rb.position, new Color(0.7f, 0.5f, 1f, 0.8f), 1.1f);
                fase = Fase.Aparecendo;
                relogio.Forcar(tempoDoDesbote);
                return;

            default:
                // Volta a ter colisor so quando aparece por inteiro.
                Intangivel(relogio.Ativo, Mathf.Lerp(0f, 1f, t));

                if (relogio.Ativo)
                    return;

                espera.Forcar(esperaAntesDoTiro);
                EstadoAtual = Estado.Agindo;
                return;
        }
    }

    /// <summary>Um ponto livre da sala, longe do jogador e nao muito colado no ponto de onde saiu.</summary>
    private void Teletransportar()
    {
        Sala sala = SalaDoInimigo;

        if (sala == null)
            return;

        Vector2 melhor = rb.position;
        float melhorNota = float.MinValue;
        Vector2 jogadorAgora = jogador != null ? (Vector2)jogador.position : rb.position;

        for (int i = 0; i < 12; i++)
        {
            Vector2 ponto = (Vector2)sala.transform.position + sala.PontoLivreAleatorio(1.5f);
            float longeDoJogador = Vector2.Distance(ponto, jogadorAgora);
            float longeDaqui = Vector2.Distance(ponto, rb.position);

            // Nem em cima do jogador (injusto) nem longe demais (vira tiro de outro mundo).
            float nota = -Mathf.Abs(longeDoJogador - 4.5f) + Mathf.Min(longeDaqui, 4f) * 0.3f;

            if (longeDoJogador > 2.5f && nota > melhorNota)
            {
                melhorNota = nota;
                melhor = ponto;
            }
        }

        rb.position = melhor;
        transform.position = melhor;
        rb.linearVelocity = Vector2.zero;
    }

    protected override void Morrer()
    {
        Intangivel(false, 1f);
        base.Morrer();
    }
}
