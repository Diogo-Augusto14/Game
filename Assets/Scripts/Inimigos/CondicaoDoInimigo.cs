using UnityEngine;

/// <summary>
/// O que as flechas especiais deixam no inimigo (vieram do jogo antigo): o gelo deixa lento e azulado,
/// o veneno tira vida aos pouquinhos e deixa esverdeado. Tambem o tempo lento da Espiral do Tempo, que
/// vale pra todos. O <see cref="InimigoAtirador"/> e o <see cref="Chefe"/> perguntam o
/// <see cref="Fator"/> pra andar.
/// </summary>
[DisallowMultipleComponent]
public class CondicaoDoInimigo : MonoBehaviour
{
    /// <summary>A Espiral do Tempo: ate quando todos os inimigos andam devagar.</summary>
    public static float TempoLentoAte = -1f;

    private Vida vida;
    private SpriteRenderer corpo;
    private Color corOriginal = Color.white;
    private float geladoAte = -1f;
    private float venenoAte = -1f;
    private float proximoVeneno;
    private float danoDoVeneno;
    private bool pintado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar() => TempoLentoAte = -1f;

    /// <summary>Quanto da velocidade sobra (1 = normal).</summary>
    public static float Fator(Component inimigo)
    {
        float fator = Time.time < TempoLentoAte ? 0.35f : 1f;

        if (inimigo != null && inimigo.TryGetComponent(out CondicaoDoInimigo c) && Time.time < c.geladoAte)
            fator *= 0.45f;

        return fator;
    }

    public static void Gelar(GameObject inimigo, float segundos) => Pegar(inimigo)?.Gelo(segundos);

    public static void Envenenar(GameObject inimigo, float segundos, float danoPorSegundo) => Pegar(inimigo)?.Veneno(segundos, danoPorSegundo);

    private static CondicaoDoInimigo Pegar(GameObject inimigo)
    {
        if (inimigo == null || !inimigo.TryGetComponent(out Vida v) || v.Morto || v.Lado != Lado.Inimigos)
            return null;

        return inimigo.TryGetComponent(out CondicaoDoInimigo c) ? c : inimigo.AddComponent<CondicaoDoInimigo>();
    }

    private void Awake()
    {
        vida = GetComponent<Vida>();

        foreach (SpriteRenderer d in GetComponentsInChildren<SpriteRenderer>())
        {
            if (corpo == null || d.sortingOrder > corpo.sortingOrder)
                corpo = d;
        }
    }

    private void Gelo(float segundos)
    {
        geladoAte = Mathf.Max(geladoAte, Time.time + segundos);
    }

    private void Veneno(float segundos, float danoPorSegundo)
    {
        venenoAte = Mathf.Max(venenoAte, Time.time + segundos);
        danoDoVeneno = Mathf.Max(danoDoVeneno, danoPorSegundo);
    }

    private void Update()
    {
        if (vida == null || vida.Morto)
            return;

        bool gelado = Time.time < geladoAte;
        bool envenenado = Time.time < venenoAte;

        if (envenenado && Time.time >= proximoVeneno)
        {
            proximoVeneno = Time.time + 0.5f;
            vida.ReceberDano(new Dano(danoDoVeneno * 0.5f, Vector2.zero, 0f, null));
        }

        if (corpo == null)
            return;

        // Pinta por cima da cor de agora e devolve a cor de antes no fim (a furia continua vermelha).
        if (gelado || envenenado)
        {
            if (!pintado)
            {
                corOriginal = corpo.color;
                pintado = true;
            }

            Color tom = gelado ? new Color(0.6f, 0.8f, 1f) : new Color(0.6f, 1f, 0.55f);
            corpo.color = new Color(corOriginal.r * tom.r, corOriginal.g * tom.g, corOriginal.b * tom.b, corpo.color.a);
        }
        else if (pintado)
        {
            pintado = false;
            corpo.color = new Color(corOriginal.r, corOriginal.g, corOriginal.b, corpo.color.a);
        }
    }
}
