using UnityEngine;

/// <summary>
/// O vazio (o "blank" do Gungeon): um estouro que apaga todo tiro inimigo que estiver perto,
/// empurra os inimigos pra longe e deixa o jogador sem tomar dano por um instante. Gasta um
/// vazio do <see cref="Inventario"/> (tecla F). So o tiro inimigo some: o do jogador continua.
/// </summary>
public static class Vazio
{
    /// <summary>Raio em que os tiros inimigos somem (cobre quase a sala toda).</summary>
    public const float RaioDosTiros = 9f;

    /// <summary>Raio em que os inimigos sao empurrados.</summary>
    public const float RaioDoEmpurrao = 5.5f;

    private const float ForcaDoEmpurrao = 16f;
    private const float ProtecaoDoJogador = 0.6f;

    public static void Estourar(Vector2 centro, GameObject dono)
    {
        // So os tiros que acertam o jogador somem; os do proprio jogador continuam.
        foreach (TiroDaSala tiro in Object.FindObjectsByType<TiroDaSala>())
        {
            if (tiro != null && tiro.AtingeJogador && ((Vector2)tiro.transform.position - centro).sqrMagnitude <= RaioDosTiros * RaioDosTiros)
                tiro.Anular();
        }

        // Inimigos acordados: 1 de dano (o minimo da Vida) so pra o empurrao valer, e eles saem de
        // perto. A lista e copiada porque um inimigo que morre sai dela no meio da volta.
        foreach (InimigoDeSala inimigo in new System.Collections.Generic.List<InimigoDeSala>(InimigoDeSala.Ativos))
        {
            if (inimigo == null || inimigo.EstaMorto || inimigo.EstadoAtual == InimigoDeSala.Estado.Dormindo
                || !inimigo.TryGetComponent(out Vida vida))
                continue;

            Vector2 afastar = (Vector2)inimigo.transform.position - centro;

            if (afastar.sqrMagnitude > RaioDoEmpurrao * RaioDoEmpurrao)
                continue;

            if (afastar.sqrMagnitude < 0.01f)
                afastar = Random.insideUnitCircle.normalized;

            vida.TomarDano(new DanoInfo(1f, afastar, ForcaDoEmpurrao, centro, dono));
        }

        if (dono != null && dono.TryGetComponent(out MovimentoTopDown movimento))
            movimento.DarProtecao(ProtecaoDoJogador);

        Sons.Tocar(Som.Feitico, 0.9f, 0.04f);
        Impacto.Tremer(0.12f, 0.3f);
        ClaraoDoVazio.Criar(centro);
    }
}

/// <summary>O anel claro que abre a partir do jogador e some, no estouro do <see cref="Vazio"/>.</summary>
public class ClaraoDoVazio : MonoBehaviour
{
    private const float Duracao = 0.45f;

    private SpriteRenderer desenho;
    private float inicio;

    public static void Criar(Vector2 centro)
    {
        GameObject obj = new GameObject("Clarao do vazio");
        obj.transform.position = centro;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = ArteDasArmas.Anel();
        sr.color = new Color(0.65f, 0.92f, 1f, 0.9f);
        sr.sortingOrder = 40;

        obj.AddComponent<ClaraoDoVazio>().desenho = sr;
    }

    private void Awake()
    {
        inicio = Time.time;
    }

    private void Update()
    {
        float t = (Time.time - inicio) / Duracao;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // Abre depressa e vai devagar no fim, sumindo.
        float aberto = 1f - (1f - t) * (1f - t);
        transform.localScale = Vector3.one * Mathf.Lerp(1f, Vazio.RaioDosTiros * 2f, aberto);

        Color cor = desenho.color;
        cor.a = 0.9f * (1f - t);
        desenho.color = cor;
    }
}
