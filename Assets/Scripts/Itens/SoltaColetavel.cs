using UnityEngine;

/// <summary>
/// Que coletavel cai quando algo e derrotado. Um lugar so pras chances, pra dar pra
/// equilibrar sem cacar numero espalhado.
/// </summary>
public static class TabelaDeDrops
{
    /// <summary>
    /// Sorteia um tipo, com peso: moeda e o mais comum, chave bem rara (a chave de verdade
    /// vem do chefe; ver <see cref="ChaveDoChefe"/>).
    /// </summary>
    public static TipoDeColetavel Sortear()
    {
        float r = Random.value;

        if (r < 0.45f) return TipoDeColetavel.Moeda;
        if (r < 0.72f) return TipoDeColetavel.Coracao;
        if (r < 0.95f) return TipoDeColetavel.Bomba;
        return TipoDeColetavel.Chave;
    }

    /// <summary>Com a chance dada, poe um coletavel sorteado no ponto. Devolve null se nao caiu nada.</summary>
    public static Coletavel TalvezSoltar(float chance, Vector2 ponto, Transform pai)
    {
        if (Random.value >= chance)
            return null;

        return Coletavel.Criar(Sortear(), ponto, pai);
    }
}

/// <summary>
/// Poe num inimigo pra ele ter chance de soltar um coletavel ao morrer.
/// Chame <see cref="Configurar"/> logo depois do AddComponent.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Vida))]
public class SoltaColetavel : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float chance = 0.2f;

    [Tooltip("Onde o coletavel fica pendurado. Vazio = na raiz da cena")]
    [SerializeField] private Transform pai;

    private Vida vida;

    public void Configurar(float novaChance, Transform novoPai)
    {
        chance = Mathf.Clamp01(novaChance);
        pai = novoPai;
    }

    private void Awake()
    {
        vida = GetComponent<Vida>();
    }

    private void OnEnable()
    {
        vida.AoMorrer.AddListener(Soltar);
    }

    private void OnDisable()
    {
        vida.AoMorrer.RemoveListener(Soltar);
    }

    private void Soltar()
    {
        TabelaDeDrops.TalvezSoltar(chance, transform.position, pai);
    }
}
