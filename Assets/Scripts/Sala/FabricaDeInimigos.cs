using UnityEngine;

public enum TipoDeInimigo
{
    Perseguidor,
    Atirador,
    Chefe,
    Investidor,
    Saltador,
    Sentinela,
    Divisor,
    DivisorPequeno,
    ChefeSaltador,
    ChefeFinal
}

/// <summary>
/// A receita de cada inimigo de sala: corpo, colisor, desenho, vida e comportamento.
///
/// O objeto e montado DESLIGADO e so liga no fim. Motivo: o Vida procura o controlador de
/// movimento no proprio Awake, uma vez so. Ligando depois de tudo adicionado, todos os
/// Awake rodam com todos os componentes ja presentes — o empurrao sai na direcao certa.
/// </summary>
public static class FabricaDeInimigos
{
    public static InimigoDeSala Criar(TipoDeInimigo tipo, Vector2 posicao, Transform pai = null)
    {
        switch (tipo)
        {
            case TipoDeInimigo.Chefe:
                ChefeDoAndar chefe = Montar<ChefeDoAndar>("Chefe", posicao, pai, 0.75f, new Color(0.55f, 0.12f, 0.16f), 120f, ArteGerada.Bola());
                chefe.Enfeitar(0.75f);
                return chefe;

            case TipoDeInimigo.ChefeSaltador:
                ChefeSaltador sapao = Montar<ChefeSaltador>("Chefe Saltador", posicao, pai, 0.8f, new Color(0.25f, 0.5f, 0.22f), 150f, ArteGerada.Bola());
                sapao.Enfeitar();
                return sapao;

            case TipoDeInimigo.ChefeFinal:
                ChefeFinal olho = Montar<ChefeFinal>("Chefe Final", posicao, pai, 1f, new Color(0.32f, 0.1f, 0.28f), 450f, ArteGerada.Bola());
                olho.Enfeitar(1f);
                return olho;

            case TipoDeInimigo.Atirador:
                return Montar<InimigoAtirador>("Atirador", posicao, pai, 0.32f, Rosto(tipo, new Color(0.62f, 0.35f, 0.85f)), 30f);

            case TipoDeInimigo.Investidor:
                return Montar<InimigoInvestidor>("Investidor", posicao, pai, 0.33f, Rosto(tipo, new Color(0.95f, 0.55f, 0.15f)), 35f);

            case TipoDeInimigo.Saltador:
                return Montar<InimigoSaltador>("Saltador", posicao, pai, 0.27f, Rosto(tipo, new Color(0.6f, 0.85f, 0.25f)), 20f);

            case TipoDeInimigo.Sentinela:
                // Quadrada: da pra reconhecer de longe que ela nao anda.
                return Montar<InimigoSentinela>("Sentinela", posicao, pai, 0.36f, Rosto(tipo, new Color(0.45f, 0.55f, 0.72f)), 40f);

            case TipoDeInimigo.Divisor:
                return Montar<InimigoDivisor>("Divisor", posicao, pai, 0.42f, Rosto(tipo, new Color(0.2f, 0.72f, 0.62f)), 30f);

            case TipoDeInimigo.DivisorPequeno:
                InimigoDivisor pedaco = Montar<InimigoDivisor>("Divisor pequeno", posicao, pai, 0.24f, Rosto(tipo, new Color(0.35f, 0.85f, 0.75f)), 10f);
                pedaco.VirarPedaco(2.3f);
                return pedaco;

            default:
                return Montar<InimigoPerseguidor>("Perseguidor", posicao, pai, 0.3f, Rosto(TipoDeInimigo.Perseguidor, new Color(0.85f, 0.25f, 0.25f)), 25f);
        }
    }

    /// <summary>A pixel art do bicho (ja colorida): o SpriteRenderer fica branco.</summary>
    private static (Sprite, Color) Rosto(TipoDeInimigo tipo, Color cor) => (ArteGerada.Inimigo(tipo, cor), Color.white);

    private static T Montar<T>(string nome, Vector2 posicao, Transform pai, float raio, Color cor, float vidaMaxima, Sprite arte)
        where T : InimigoDeSala
        => Montar<T>(nome, posicao, pai, raio, (arte, cor), vidaMaxima);

    private static T Montar<T>(string nome, Vector2 posicao, Transform pai, float raio, (Sprite arte, Color cor) aparencia, float vidaMaxima)
        where T : InimigoDeSala
    {
        GameObject obj = new GameObject(nome);
        obj.SetActive(false);
        obj.transform.SetParent(pai, false);
        obj.transform.position = posicao;
        Camadas.Definir(obj, Camadas.Inimigo);

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        CircleCollider2D corpo = obj.AddComponent<CircleCollider2D>();
        corpo.radius = raio;

        Sprite forma = aparencia.arte != null ? aparencia.arte : ArteGerada.Bola();
        FormasDaSala.Desenho(obj.transform, "Desenho", forma, aparencia.cor, Vector2.zero, Vector2.one * raio * 2f, 10);

        Vida vida = obj.AddComponent<Vida>();
        vida.Configurar(vidaMaxima, 0f, true, 0.6f, false);

        T inimigo = obj.AddComponent<T>();

        obj.SetActive(true);
        return inimigo;
    }
}
