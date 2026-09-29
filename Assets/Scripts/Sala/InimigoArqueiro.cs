using UnityEngine;

/// <summary>
/// Arqueiro sombrio (Tiny Swords Free Pack): mantem distancia, puxa o arco e solta uma
/// flecha rapida e reta no jogador. A puxada do arco e o aviso: saia da linha.
///
///   Agindo      -> mantem distancia, anda de lado
///   Preparando  -> puxa o arco (a flecha sai no sexto quadro)
///   Recuperando -> abaixa o arco
/// </summary>
public class InimigoArqueiro : InimigoComArte
{
    [Header("Distancia")]
    [SerializeField, Min(0f)] private float distanciaMinima = 3f;
    [SerializeField, Min(0f)] private float distanciaMaxima = 6f;

    [Header("Flecha")]
    [SerializeField, Min(0.1f)] private float intervaloEntreFlechas = 2.2f;

    [Tooltip("Telegrafo: segundos puxando o arco")]
    [SerializeField, Min(0f)] private float tempoDePreparo = 0.6f;

    [SerializeField, Min(0f)] private float tempoDeRecuperacao = 0.3f;

    [SerializeField, Min(0f)] private float danoDaFlecha = 10f;

    [SerializeField, Min(0.1f)] private float velocidadeDaFlecha = 7f;

    /// <summary>A tira do tiro tem 8 quadros; a flecha sai no indice 5.</summary>
    private const int QuadroDoTiro = 5;

    /// <summary>Diametro do colisor da flecha (o TiroDaSala escala o objeto por ele).</summary>
    private const float DiametroDaFlecha = 0.25f;

    /// <summary>Comprimento da flecha desenhada, em unidades.</summary>
    private const float ComprimentoDaFlecha = 0.6f;

    private Cronometro recarga;
    private Cronometro preparo;
    private Cronometro recuperacao;

    protected override void Awake()
    {
        base.Awake();
        velocidade = 1.7f;
        recarga.Forcar(intervaloEntreFlechas * 0.5f);
    }

    protected override void AtualizarAgindo(float dt)
    {
        recarga.Contar(dt);

        if (!recarga.Ativo && VeOJogador())
        {
            EstadoAtual = Estado.Preparando;
            preparo.Forcar(tempoDePreparo);
            Frear();

            animacao?.OlharPara(ParaOJogador());
            TocarAtaque(clipes?.Ataque, QuadroDoTiro, tempoDePreparo);
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

        Atirar();
        EstadoAtual = Estado.Recuperando;
        recuperacao.Forcar(tempoDeRecuperacao);
    }

    protected override void AtualizarRecuperando(float dt)
    {
        Frear();
        recuperacao.Contar(dt);

        if (recuperacao.Ativo)
            return;

        recarga.Forcar(intervaloEntreFlechas);
        TrocarLado();
        EstadoAtual = Estado.Agindo;
    }

    private void Atirar()
    {
        Vector2 alvo = ParaOJogador();

        if (alvo.sqrMagnitude < 0.0001f)
            return;

        Vector2 direcao = alvo.normalized;
        TiroDaSala flecha = TiroDaSala.Disparar(rb.position + direcao * 0.4f, direcao * velocidadeDaFlecha,
                                                danoDaFlecha, gameObject, true, new Color(0.9f, 0.85f, 0.7f),
                                                DiametroDaFlecha);

        // Troca a bolinha pela flecha, apontada pra onde voa. O objeto ja esta escalado
        // pelo diametro, entao os pixels por unidade compensam essa escala.
        Sprite sprite = ArteImportada.Flecha(48f * DiametroDaFlecha / ComprimentoDaFlecha);

        if (sprite != null && flecha.TryGetComponent(out SpriteRenderer sr))
        {
            sr.sprite = sprite;
            sr.color = Color.white;
            flecha.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg);
        }
    }
}
