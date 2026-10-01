using UnityEngine;

/// <summary>
/// Arqueiro sombrio (Tiny Swords Free Pack): mantem distancia, puxa o arco e solta uma
/// flecha rapida e reta no jogador. A puxada do arco e o aviso: saia da linha.
///
///   Agindo      -> mantem distancia, anda de lado
///   Preparando  -> puxa o arco (a flecha sai no sexto quadro)
///   Recuperando -> abaixa o arco
///
/// Cada especie atira do seu jeito (<see cref="Mira"/>):
///   Reta     -> uma flecha no jogador (arqueiro sombrio)
///   Alinhada -> corre pra mesma linha ou coluna do jogador e so atira dali, reto pelo
///               corredor (esqueleto arqueiro): sair da linha dele e o que salva
///   Leque    -> tres flechas abertas de uma vez, mais devagar (demonio arqueiro)
/// </summary>
public class InimigoArqueiro : InimigoComArte
{
    public enum Mira { Reta, Alinhada, Leque }

    [SerializeField] private Mira mira = Mira.Reta;

    [Tooltip("Leque: graus entre uma flecha e a outra")]
    [SerializeField, Range(5f, 45f)] private float aberturaDoLeque = 18f;

    [Tooltip("Alinhada: quao perto da linha/coluna do jogador ele precisa estar pra atirar")]
    [SerializeField, Min(0.05f)] private float folgaDaLinha = 0.35f;

    [Tooltip("Alinhada: depois deste tempo sem conseguir alinhar, atira de onde estiver")]
    [SerializeField, Min(0.5f)] private float paciencia = 3f;

    private float tentandoAlinhar;

    public void UsarMira(Mira nova)
    {
        mira = nova;

        if (mira == Mira.Leque)
            intervaloEntreFlechas *= 1.35f;
        else if (mira == Mira.Alinhada)
            velocidade = 2.4f;
    }

    [Header("Distancia")]
    [SerializeField, Min(0f)] private float distanciaMinima = 3f;
    [SerializeField, Min(0f)] private float distanciaMaxima = 9f;

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

        if (mira == Mira.Alinhada && !Alinhado(dt))
            return;

        if (!recarga.Ativo && VeOJogador())
        {
            tentandoAlinhar = 0f;
            EstadoAtual = Estado.Preparando;
            recarga.Forcar(intervaloEntreFlechas);   // levar tiro no meio nao faz ele atacar de novo na hora
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

    /// <summary>
    /// Modo alinhado: anda ate a linha ou a coluna do jogador (a que estiver mais perto),
    /// mantendo distancia dele. Devolve true quando ja esta na linha (ou cansou de tentar).
    /// </summary>
    private bool Alinhado(float dt)
    {
        Vector2 alvo = ParaOJogador();
        float dx = Mathf.Abs(alvo.x);
        float dy = Mathf.Abs(alvo.y);

        if (Mathf.Min(dx, dy) <= folgaDaLinha || tentandoAlinhar >= paciencia)
            return true;

        if (recarga.Ativo && recarga.Restante > 0.8f)
        {
            ManterDistancia(distanciaMinima, distanciaMaxima);
            return false;
        }

        tentandoAlinhar += dt;

        // Fecha o eixo que esta mais perto de bater; no outro eixo, fica longe o bastante.
        Vector2 passo = dx < dy ? new Vector2(Mathf.Sign(alvo.x), 0f) : new Vector2(0f, Mathf.Sign(alvo.y));

        if (alvo.magnitude < distanciaMinima)
            passo -= alvo.normalized * 0.6f;

        Andar(PeloCaminhoAte(rb.position + passo * 1.5f, passo), velocidade);
        animacao?.OlharPara(alvo);
        return false;
    }

    private void Atirar()
    {
        Vector2 alvo = ParaOJogador();

        if (alvo.sqrMagnitude < 0.0001f)
            return;

        // Alinhado, a flecha sai reta pelo eixo: e o corredor que ele estava mirando.
        if (mira == Mira.Alinhada)
            alvo = Mathf.Abs(alvo.x) > Mathf.Abs(alvo.y) ? new Vector2(Mathf.Sign(alvo.x), 0f) : new Vector2(0f, Mathf.Sign(alvo.y));

        if (mira == Mira.Leque)
        {
            float angulo = Mathf.Atan2(alvo.y, alvo.x) * Mathf.Rad2Deg;

            for (int i = -1; i <= 1; i++)
            {
                float a = (angulo + i * aberturaDoLeque) * Mathf.Deg2Rad;
                Flecha(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 0.8f);
            }

            return;
        }

        Flecha(alvo.normalized, 1f);
    }

    private void Flecha(Vector2 direcao, float pressa)
    {
        TiroDaSala flecha = TiroDaSala.Disparar(rb.position + direcao * 0.4f, direcao * velocidadeDaFlecha * pressa,
                                                danoDaFlecha, gameObject, true, new Color(0.9f, 0.85f, 0.7f),
                                                DiametroDaFlecha);

        // Cada arqueiro ja sai com a flecha dele (EstilosDeTiro). Variacao sem estilo: troca a
        // bolinha pela flecha do Tiny Swords, apontada pra onde voa. O objeto ja esta escalado
        // pelo diametro, entao os pixels por unidade compensam essa escala.
        if (flecha.TryGetComponent(out VisualDoProjetil _))
            return;

        Sprite sprite = ArteImportada.Flecha(48f * DiametroDaFlecha / ComprimentoDaFlecha);

        if (sprite != null && flecha.TryGetComponent(out SpriteRenderer sr))
        {
            sr.sprite = sprite;
            sr.color = Color.white;
            flecha.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg);
        }
    }
}
