using UnityEngine;

/// <summary>
/// Fogo-fatuo: flutua em volta do jogador num circulo que respira (chega perto e se afasta),
/// trocando de sentido de vez em quando. De tempos em tempos APAGA: fica quase invisivel e
/// intocavel por um instante, e reacende soltando uma cruz de chamas (em "+" e "x"
/// alternados). Voa: buraco nao segura.
///
///   Agindo      -> orbita acesa
///   Preparando  -> apagado (intocavel), deslizando devagar
///   Recuperando -> reacende e atira a cruz
/// </summary>
public class InimigoFogoFatuo : InimigoDeSala
{
    [SerializeField, Min(0.5f)] private float raioDaOrbita = 3.2f;

    [SerializeField, Min(0.5f)] private float intervaloEntreApagoes = 3.2f;

    [SerializeField, Min(0.1f)] private float tempoApagado = 1f;

    [SerializeField, Min(0f)] private float danoDaChama = 10f;

    [SerializeField] private Color corDaChama = new Color(0.4f, 0.85f, 1f);

    private Cronometro recarga;
    private Cronometro apagado;
    private Cronometro trocaDeSentido;
    private float sentido = 1f;
    private float fase;
    private bool emX;

    protected override void Awake()
    {
        base.Awake();
        fase = Random.value * 10f;
        sentido = Random.value < 0.5f ? -1f : 1f;
        recarga.Forcar(intervaloEntreApagoes * Random.Range(0.5f, 1f));
        trocaDeSentido.Forcar(Random.Range(2f, 4f));
    }

    protected override void AtualizarAgindo(float dt)
    {
        recarga.Contar(dt);
        Orbitar(dt, 1f);

        // Uma espiral anterior ainda saindo: espera ela acabar pra apagar de novo.
        if (recarga.Ativo || (Padroes != null && Padroes.Ocupado))
            return;

        EstadoAtual = Estado.Preparando;
        apagado.Forcar(tempoApagado);
    }

    protected override void AtualizarPreparando(float dt)
    {
        apagado.Contar(dt);
        Intangivel(true, 0.2f + 0.1f * Mathf.Sin(Time.time * 20f));
        Orbitar(dt, 0.5f);

        if (apagado.Ativo)
            return;

        // Reacende atirando: o proximo padrao do perfil da fase (cruz, anel com buraco, espiral); sem
        // perfil, a cruz de chamas de sempre.
        Intangivel(false, 1f);

        if (Padroes != null)
        {
            Padroes.Atacar(Padroes.ProximoDoCiclo(), false);
        }
        else
        {
            float inicio = emX ? 45f : 0f;
            emX = !emX;

            for (int i = 0; i < 4; i++)
                Disparar(inicio + i * 90f, 3.6f, danoDaChama, corDaChama);
        }

        Sons.Tocar(Som.TiroDeFogo, 0.5f);
        recarga.Forcar(intervaloEntreApagoes);
        EstadoAtual = Estado.Agindo;
    }

    /// <summary>Circula o jogador: anda de lado e corrige a distancia pra um raio que vai e volta.</summary>
    private void Orbitar(float dt, float fracaoDaVelocidade)
    {
        trocaDeSentido.Contar(dt);

        if (!trocaDeSentido.Ativo)
        {
            sentido = -sentido;
            trocaDeSentido.Forcar(Random.Range(2f, 4f));
        }

        Vector2 alvo = ParaOJogador();
        float distancia = alvo.magnitude;

        if (distancia < 0.0001f)
        {
            Frear();
            return;
        }

        Vector2 frente = alvo / distancia;
        Vector2 lado = new Vector2(-frente.y, frente.x) * sentido;
        float raio = raioDaOrbita + Mathf.Sin(Time.time * 0.9f + fase) * 1.2f;
        Vector2 rumo = lado + frente * Mathf.Clamp((distancia - raio) * 0.8f, -1f, 1f);

        // Batendo na parede, inverte o sentido em vez de ficar raspando.
        if (Physics2D.CircleCast(rb.position, Raio * 0.9f, rumo.normalized, 0.4f, Camadas.MascaraDeParede).collider != null)
            sentido = -sentido;

        Andar(rumo, velocidade * fracaoDaVelocidade);
    }

    protected override void Morrer()
    {
        Intangivel(false, 1f);
        base.Morrer();
    }
}
