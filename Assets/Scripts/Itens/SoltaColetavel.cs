using UnityEngine;

/// <summary>
/// Que coletavel cai quando algo e derrotado. Um lugar so pras chances, pra dar pra
/// equilibrar sem cacar numero espalhado.
/// </summary>
public static class TabelaDeDrops
{
    /// <summary>
    /// Sorteia um tipo, com peso: moeda e o mais comum, vazio e chave bem raros (a chave de
    /// verdade vem do chefe; ver <see cref="ChaveDoChefe"/>). Com arma de fogo de municao limitada no
    /// arsenal, a caixa de municao entra na roda, e com mais chance quando a municao esta baixa.
    /// </summary>
    public static TipoDeColetavel Sortear()
    {
        float r = Random.value;

        ArsenalDoJogador arsenal = ArsenalDoJogador.Atual;

        if (arsenal != null && arsenal.TemMunicaoLimitada)
        {
            float chanceDaCaixa = arsenal.PrecisaDeMunicao ? 0.28f : 0.12f;

            if (r < chanceDaCaixa)
                return TipoDeColetavel.Municao;

            // O resto da roda continua como era.
            r = (r - chanceDaCaixa) / (1f - chanceDaCaixa);
        }

        if (r < 0.45f) return TipoDeColetavel.Moeda;
        if (r < 0.72f) return TipoDeColetavel.Coracao;
        if (r < 0.92f) return TipoDeColetavel.Bomba;
        if (r < 0.95f) return TipoDeColetavel.Vazio;
        return TipoDeColetavel.Chave;
    }

    /// <summary>
    /// Multiplica toda chance de soltar premio (item Amuleto da Sorte). Quem ajusta e o
    /// <see cref="EfeitosDosItens"/>, que volta pra 1 quando o jogador some.
    /// </summary>
    public static float Sorte = 1f;

    /// <summary>Com a chance dada, poe um coletavel sorteado no ponto. Devolve null se nao caiu nada.</summary>
    public static Coletavel TalvezSoltar(float chance, Vector2 ponto, Transform pai)
    {
        if (Random.value >= Mathf.Clamp01(chance * Sorte))
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
