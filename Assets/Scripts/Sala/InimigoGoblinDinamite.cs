using UnityEngine;

/// <summary>
/// Goblin da dinamite (Tiny Swords): fica longe e joga dinamite onde o jogador esta. A
/// dinamite voa por cima das pedras e explode ao cair; um circulo vermelho no chao avisa
/// onde. Parado no lugar, o jogador leva; andando, escapa.
///
///   Agindo      -> mantem distancia, anda de lado
///   Preparando  -> a animacao do arremesso (a dinamite sai no sexto quadro)
///   Recuperando -> parado um instante depois de jogar
/// </summary>
public class InimigoGoblinDinamite : InimigoComArte
{
    [Header("Distancia")]
    [SerializeField, Min(0f)] private float distanciaMinima = 3f;
    [SerializeField, Min(0f)] private float distanciaMaxima = 5.5f;

    [Header("Dinamite")]
    [SerializeField, Min(0.1f)] private float intervaloEntreArremessos = 2.6f;

    [Tooltip("Telegrafo: segundos do arremesso ate a dinamite sair da mao")]
    [SerializeField, Min(0f)] private float tempoDePreparo = 0.5f;

    [SerializeField, Min(0f)] private float tempoDeRecuperacao = 0.35f;

    [Tooltip("Segundos da dinamite no ar")]
    [SerializeField, Min(0.1f)] private float tempoDeVoo = 0.8f;

    [SerializeField, Min(0.1f)] private float alcanceMaximo = 6f;

    [SerializeField, Min(0.1f)] private float raioDaExplosao = 0.9f;

    [Tooltip("Um coracao inteiro")]
    [SerializeField, Min(0f)] private float danoNoJogador = 20f;

    [Tooltip("Dano nos outros inimigos pegos na explosao")]
    [SerializeField, Min(0f)] private float danoNosOutros = 15f;

    /// <summary>A tira do arremesso tem 7 quadros; a dinamite sai no indice 5.</summary>
    private const int QuadroDoArremesso = 5;

    private Cronometro recarga;
    private Cronometro preparo;
    private Cronometro recuperacao;

    protected override void Awake()
    {
        base.Awake();
        velocidade = 1.5f;
        recarga.Forcar(intervaloEntreArremessos * 0.6f);
    }

    protected override void AtualizarAgindo(float dt)
    {
        recarga.Contar(dt);

        if (!recarga.Ativo && VeOJogador() && ParaOJogador().magnitude <= alcanceMaximo + 1f)
        {
            EstadoAtual = Estado.Preparando;
            recarga.Forcar(intervaloEntreArremessos);   // levar tiro no meio nao faz ele atacar de novo na hora
            preparo.Forcar(tempoDePreparo);
            Frear();

            animacao?.OlharPara(ParaOJogador());
            TocarAtaque(clipes?.Ataque, QuadroDoArremesso, tempoDePreparo);
            return;
        }

        ManterDistancia(distanciaMinima, distanciaMaxima);
    }

    protected override void AtualizarPreparando(float dt)
    {
        Frear();
        preparo.Contar(dt);

        if (preparo.Ativo)
            return;

        Arremessar();
        EstadoAtual = Estado.Recuperando;
        recuperacao.Forcar(tempoDeRecuperacao);
    }

    protected override void AtualizarRecuperando(float dt)
    {
        Frear();
        recuperacao.Contar(dt);

        if (recuperacao.Ativo)
            return;

        recarga.Forcar(intervaloEntreArremessos);
        TrocarLado();
        EstadoAtual = Estado.Agindo;
    }

    private void Arremessar()
    {
        // Mira onde o jogador esta agora; longe demais, cai no meio do caminho.
        Vector2 alvo = Vector2.ClampMagnitude(ParaOJogador(), alcanceMaximo);

        Sons.Tocar(Som.Pulo, 0.4f);
        DinamiteLancada.Lancar(rb.position + Vector2.up * 0.2f, rb.position + alvo, tempoDeVoo, raioDaExplosao,
                               danoNoJogador, danoNosOutros, gameObject);
    }
}
