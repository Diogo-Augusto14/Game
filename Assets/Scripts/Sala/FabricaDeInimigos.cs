using UnityEngine;

public enum TipoDeInimigo
{
    Perseguidor,
    Atirador,
    Chefe
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
                ChefeDoAndar chefe = Montar<ChefeDoAndar>("Chefe", posicao, pai, 0.75f, new Color(0.55f, 0.12f, 0.16f), 120f);
                chefe.Enfeitar(0.75f);
                return chefe;

            case TipoDeInimigo.Atirador:
                return Montar<InimigoAtirador>("Atirador", posicao, pai, 0.32f, new Color(0.62f, 0.35f, 0.85f), 30f);

            default:
                return Montar<InimigoPerseguidor>("Perseguidor", posicao, pai, 0.3f, new Color(0.85f, 0.25f, 0.25f), 25f);
        }
    }

    private static T Montar<T>(string nome, Vector2 posicao, Transform pai, float raio, Color cor, float vidaMaxima)
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

        FormasDaSala.Desenho(obj.transform, "Desenho", FormasDaSala.Circulo(), cor, Vector2.zero, Vector2.one * raio * 2f, 10);

        Vida vida = obj.AddComponent<Vida>();
        vida.Configurar(vidaMaxima, 0f, true, 0.6f, false);

        T inimigo = obj.AddComponent<T>();

        obj.SetActive(true);
        return inimigo;
    }
}
