using UnityEngine;

/// <summary>
/// Torre parada que atira em cruz. Alterna entre "+" (cima, baixo, lados) e "x"
/// (diagonais), entao o lugar seguro muda a cada disparo. Nao anda e nao e empurrada:
/// e um obstaculo que atira. Da pra se esconder atras de pedra, mas ela nao precisa ver
/// o jogador pra atirar.
///
///   Agindo      -> conta ate o proximo disparo
///   Preparando  -> incha e clareia (aviso)
///   Recuperando -> pausa curta
/// </summary>
public class InimigoSentinela : InimigoDeSala
{
    [Header("Tiro")]
    [SerializeField, Min(0.1f)] private float intervaloEntreTiros = 1.5f;

    [SerializeField, Min(0f)] private float tempoDeAviso = 0.5f;

    [SerializeField, Min(0f)] private float tempoDeRecuperacao = 0.3f;

    [SerializeField, Min(0f)] private float danoDoTiro = 10f;

    [SerializeField, Min(0.1f)] private float velocidadeDoTiro = 3.8f;

    [SerializeField] private Color corDoTiro = new Color(0.55f, 0.8f, 1f);

    [Tooltip("Ligado: atira nas 8 direcoes de uma vez em vez de alternar + e x")]
    [SerializeField] private bool oitoDirecoes;

    private Cronometro recarga;
    private Cronometro aviso;
    private Cronometro recuperacao;
    private bool emX;
    private Vector3 escalaOriginal;

    protected override bool Imparavel => true;

    /// <summary>A fabrica liga nos andares mais fundos.</summary>
    public void UsarOitoDirecoes(bool ligado) => oitoDirecoes = ligado;

    protected override void Awake()
    {
        base.Awake();

        // Parada de verdade: o jogador tambem nao consegue empurrar.
        rb.bodyType = RigidbodyType2D.Kinematic;
        escalaOriginal = transform.localScale;
        emX = Random.value < 0.5f;
        recarga.Forcar(intervaloEntreTiros * Random.Range(0.4f, 1f));
    }

    protected override void AtualizarAgindo(float dt)
    {
        rb.linearVelocity = Vector2.zero;
        recarga.Contar(dt);

        if (recarga.Ativo)
            return;

        aviso.Forcar(tempoDeAviso);
        EstadoAtual = Estado.Preparando;
    }

    protected override void AtualizarPreparando(float dt)
    {
        aviso.Contar(dt);

        float t = tempoDeAviso <= 0f ? 1f : 1f - aviso.Restante / tempoDeAviso;
        transform.localScale = escalaOriginal * (1f + 0.2f * t);

        if (aviso.Ativo)
            return;

        transform.localScale = escalaOriginal;
        Atirar();

        recuperacao.Forcar(tempoDeRecuperacao);
        EstadoAtual = Estado.Recuperando;
    }

    protected override void AtualizarRecuperando(float dt)
    {
        // Uma espiral leva um ou dois segundos: so respira (e conta o proximo tiro) depois dela.
        if (Padroes != null && Padroes.Ocupado)
            return;

        recuperacao.Contar(dt);

        if (recuperacao.Ativo)
            return;

        recarga.Forcar(intervaloEntreTiros);
        EstadoAtual = Estado.Agindo;
    }

    private void Atirar()
    {
        // Com o perfil de balas da fase: a cruz, o anel de oito e a espiral, em ciclo.
        if (Padroes != null)
        {
            Padroes.Atacar(Padroes.ProximoDoCiclo(), false);
            return;
        }

        int quantos = oitoDirecoes ? 8 : 4;
        float inicio = oitoDirecoes || !emX ? 0f : 45f;
        emX = !emX;

        for (int i = 0; i < quantos; i++)
        {
            float angulo = (inicio + i * 360f / quantos) * Mathf.Deg2Rad;
            Vector2 rumo = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));

            TiroDaSala.Disparar(rb.position + rumo * (Raio + 0.15f), rumo * velocidadeDoTiro,
                              danoDoTiro, gameObject, true, corDoTiro);
        }
    }

    protected override void Morrer()
    {
        transform.localScale = escalaOriginal;
        base.Morrer();
    }
}
