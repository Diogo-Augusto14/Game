using UnityEngine;

/// <summary>
/// Base dos inimigos com arte do Tiny Swords (goblins, barril, arqueiro). Guarda a
/// animacao, troca a morte encolhida por uma caveira que sobe e some, e tem a ajuda de
/// manter distancia que os de longe usam.
/// </summary>
public abstract class InimigoComArte : InimigoDeSala
{
    /// <summary>Pixels por unidade da arte do Tiny Swords nos inimigos (o corpo fica com ~0.9 unidade).</summary>
    public const float PixelsDoTinySwords = 72f;

    protected AnimacaoDePersonagem animacao;
    protected ClipesDePersonagem clipes;

    private float ladoDoPasso = 1f;

    public void UsarArte(AnimacaoDePersonagem novaAnimacao, ClipesDePersonagem novosClipes)
    {
        animacao = novaAnimacao;
        clipes = novosClipes;
    }

    protected override void Awake()
    {
        base.Awake();
        ladoDoPasso = Random.value < 0.5f ? -1f : 1f;
    }

    /// <summary>
    /// Toca um ataque de modo que o quadro <paramref name="quadroDoGolpe"/> apareca
    /// exatamente quando <paramref name="segundos"/> passarem.
    /// </summary>
    protected void TocarAtaque(Sprite[] quadros, int quadroDoGolpe, float segundos)
    {
        if (quadros != null)
            animacao?.TocarUmaVez(quadros, quadroDoGolpe / Mathf.Max(0.05f, segundos));
    }

    /// <summary>
    /// Fica entre <paramref name="minima"/> e <paramref name="maxima"/> do jogador: recua se
    /// ele chegou perto, chega perto se esta longe, anda de lado no meio-termo.
    /// </summary>
    protected void ManterDistancia(float minima, float maxima)
    {
        Vector2 alvo = ParaOJogador();
        float distancia = alvo.magnitude;

        if (distancia < 0.0001f)
        {
            Frear();
            return;
        }

        Vector2 frente = alvo / distancia;
        Vector2 lado = new Vector2(-frente.y, frente.x) * ladoDoPasso;

        if (distancia < minima)
            Andar(-frente + lado * 0.3f, velocidade);
        else if (distancia > maxima)
            Andar(frente, velocidade);
        else
            Andar(lado, velocidade * 0.5f);
    }

    /// <summary>Troca o lado do passo lateral (chame depois de cada ataque).</summary>
    protected void TrocarLado() => ladoDoPasso = -ladoDoPasso;

    /// <summary>Desligado: quem herda cuida da propria morte (o barril explode).</summary>
    protected virtual bool DeixaCaveira => true;

    protected override void Morrer()
    {
        Vector3 escala = transform.localScale;
        base.Morrer();
        transform.localScale = escala;

        if (!DeixaCaveira)
            return;

        // A caveira toma o lugar do bicho. Sem a arte, fica o encolhido da base.
        Sprite[] caveira = ArteImportada.Caveira(PixelsDoTinySwords);

        if (caveira == null)
        {
            transform.localScale = escala * 0.6f;
            return;
        }

        EfeitoDeQuadros.Criar(caveira, 14f, transform.position, 11, transform.parent);

        if (animacao != null)
            animacao.enabled = false;

        if (desenho != null)
            desenho.enabled = false;
    }
}
