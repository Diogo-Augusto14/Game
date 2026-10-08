using UnityEngine;

public enum TipoDeColetavel
{
    Moeda,
    Chave,
    Bomba,
    Coracao,
}

/// <summary>
/// Moeda, chave, bomba ou coracao no chao (do jogo antigo): encostando, vai pra <see cref="Bolsa"/>
/// (o coracao cura meio coracao, e so se nao estiver com a vida cheia). Com a Pedra-Ima, vem sozinho.
/// <see cref="SoltarDoInimigo"/> sorteia o que cai de quem morre.
/// </summary>
public class Coletavel : MonoBehaviour
{
    private const float Alcance = 0.7f;

    private TipoDeColetavel tipo;
    private int quanto = 1;
    private Transform jogador;
    private float fase;
    private float pegavelEm;
    private Vector2 empurrao;

    public static Coletavel Criar(TipoDeColetavel tipo, Vector2 onde, int quanto = 1)
    {
        GameObject obj = new GameObject(tipo.ToString());
        obj.transform.SetParent(GeradorDoAndar.Raiz, true);
        obj.transform.position = onde;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -98;
        Iluminacao.Brilhar(sr);
        sr.sprite = Desenho(tipo, quanto);

        Coletavel c = obj.AddComponent<Coletavel>();
        c.tipo = tipo;
        c.quanto = quanto;
        c.fase = Random.value * 10f;
        c.pegavelEm = Time.time + 0.3f;
        c.empurrao = Random.insideUnitCircle * 3f;
        return c;
    }

    public static Sprite Desenho(TipoDeColetavel tipo, int quanto = 1)
    {
        switch (tipo)
        {
            case TipoDeColetavel.Chave:
                return ArteDoAntigo.Icone(179, 56f);

            case TipoDeColetavel.Bomba:
                return ArteDoAntigo.Icone(764, 52f);

            case TipoDeColetavel.Coracao:
                return ArteDoAntigo.Icone(659, 56f);

            default:
                return ArteDoAntigo.Icone(quanto >= 5 ? 158 : 131, quanto >= 5 ? 44f : 60f);
        }
    }

    /// <summary>
    /// O que cai de um inimigo: as vezes moeda (as vezes uma pilha), raramente chave, bomba ou coracao.
    /// O chefe solta um punhado de moedas. A sorte (Amuleto) aumenta as chances.
    /// </summary>
    public static void SoltarDoInimigo(Vector2 onde, bool chefe, float sorte)
    {
        if (chefe)
        {
            for (int i = 0; i < 6; i++)
                Criar(TipoDeColetavel.Moeda, onde + Random.insideUnitCircle);

            Criar(TipoDeColetavel.Chave, onde + Vector2.left);
            Criar(TipoDeColetavel.Coracao, onde + Vector2.right);
            return;
        }

        float sorteio = Random.value / Mathf.Max(0.1f, sorte);

        if (sorteio < 0.02f)
            Criar(TipoDeColetavel.Chave, onde);
        else if (sorteio < 0.045f)
            Criar(TipoDeColetavel.Bomba, onde);
        else if (sorteio < 0.075f)
            Criar(TipoDeColetavel.Coracao, onde);
        else if (sorteio < 0.1f)
            Criar(TipoDeColetavel.Moeda, onde, 5);
        else if (sorteio < 0.4f)
            Criar(TipoDeColetavel.Moeda, onde);
    }

    private void Update()
    {
        // Salta um pouco ao cair e depois fica pulsando.
        if (empurrao.sqrMagnitude > 0.01f)
        {
            transform.position += (Vector3)(empurrao * Time.deltaTime);
            empurrao = Vector2.MoveTowards(empurrao, Vector2.zero, 8f * Time.deltaTime);
        }

        transform.localScale = Vector3.one * (1f + 0.07f * Mathf.Sin((Time.time + fase) * 4f));

        if (jogador == null)
        {
            GameObject obj = GameObject.FindWithTag("Player");

            if (obj == null)
                return;

            jogador = obj.transform;
        }

        float distancia = Vector2.Distance(jogador.position, transform.position);

        if (jogador.TryGetComponent(out EstatisticasDoJogador e) && e.Ima && distancia < 4.5f)
            transform.position = Vector2.MoveTowards(transform.position, jogador.position, 7f * Time.deltaTime);

        if (Time.time < pegavelEm || distancia > Alcance)
            return;

        if (!jogador.TryGetComponent(out Bolsa bolsa))
            return;

        switch (tipo)
        {
            case TipoDeColetavel.Moeda:
                bolsa.Ganhar(quanto, 0, 0);
                Sons.Tocar(Som.Moeda, 0.6f);
                break;

            case TipoDeColetavel.Chave:
                bolsa.Ganhar(0, quanto, 0);
                Sons.Tocar(Som.Chave, 0.8f);
                break;

            case TipoDeColetavel.Bomba:
                bolsa.Ganhar(0, 0, quanto);
                Sons.Tocar(Som.Item, 0.6f);
                break;

            case TipoDeColetavel.Coracao:
                if (!jogador.TryGetComponent(out Vida vida) || vida.Atual >= vida.Maxima)
                    return;

                vida.Curar(1f);
                Sons.Tocar(Som.Coracao, 0.8f);
                break;
        }

        Destroy(gameObject);
    }
}
