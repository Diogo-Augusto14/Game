using UnityEngine;

/// <summary>
/// O altar das ruinas (pacote Ancient Ruins), no fundo da area segura: quando o jogador chega perto, ele
/// acende (o cristal desce num raio) e o portal pro proximo mundo abre em cima dele. O altar e baixo:
/// fica no chao, o jogador sobe nele.
/// </summary>
public class AltarDoPortal : MonoBehaviour
{
    private const float QuadrosPorSegundo = 14f;
    private const float Perto = 5f;
    // O quadro em que o cristal chega no altar: o portal abre ali.
    private const int QuadroDoPortal = 20;

    private Sprite[] quadros;
    private SpriteRenderer desenho;
    private GeradorDoAndar gerador;
    private Vector2 portal;
    private float comecou = -1f;
    private bool abriu;

    /// <param name="meio">O meio do desenho.</param>
    /// <param name="portal">Onde o portal abre (o meio do patio do altar).</param>
    public static AltarDoPortal Criar(Vector2 meio, Transform pai, GeradorDoAndar gerador, Vector2 portal)
    {
        Sprite[] quadros = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Areas/Ruinas/Altar"), new Vector2Int(224, 288), 32f);

        if (quadros.Length == 0)
        {
            gerador.AbrirPortalDoDescanso(portal);
            return null;
        }

        GameObject obj = new GameObject("Altar");
        obj.transform.SetParent(pai, false);
        obj.transform.position = meio;

        AltarDoPortal a = obj.AddComponent<AltarDoPortal>();
        a.quadros = quadros;
        a.gerador = gerador;
        a.portal = portal;
        a.desenho = obj.AddComponent<SpriteRenderer>();
        a.desenho.sprite = quadros[0];
        a.desenho.sortingOrder = Pedreiro.OrdemDosEnfeites + 1;
        return a;
    }

    private void Update()
    {
        Transform jogador = gerador != null ? gerador.Jogador : null;

        if (comecou < 0f)
        {
            if (jogador != null && Vector2.Distance(jogador.position, portal) < Perto)
            {
                comecou = Time.time;
                Sons.Tocar(Som.Segredo, 0.8f);
            }

            return;
        }

        int q = Mathf.FloorToInt((Time.time - comecou) * QuadrosPorSegundo);
        desenho.sprite = quadros[Mathf.Min(q, quadros.Length - 1)];

        if (!abriu && q >= QuadroDoPortal)
        {
            abriu = true;
            gerador.AbrirPortalDoDescanso(portal);
        }
    }
}
