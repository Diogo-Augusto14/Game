using UnityEngine;

/// <summary>
/// O mercador da area segura (pacote Ancient Ruins): fica parado na barraca e acena quando o jogador
/// chega perto. A loja e a <see cref="Loja"/> na frente dele.
/// </summary>
public class Mercador : MonoBehaviour
{
    private const float Perto = 4f;

    private SpriteRenderer desenho;
    private Sprite[] parado, acenando;
    private Transform jogador;

    public static Mercador Criar(Vector2 pe, Transform pai)
    {
        Sprite[] parado = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Areas/Ruinas/Mercador"), new Vector2Int(110, 110), 32f);

        if (parado.Length == 0)
            return null;

        GameObject obj = new GameObject("Mercador");
        obj.transform.SetParent(pai, false);
        // O desenho tem o pe 0,75 abaixo do meio do quadro.
        obj.transform.position = pe + Vector2.up * 0.75f;

        Mercador m = obj.AddComponent<Mercador>();
        m.desenho = obj.AddComponent<SpriteRenderer>();
        m.desenho.sprite = parado[0];
        m.desenho.sortingOrder = 10;
        m.parado = parado;
        m.acenando = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Areas/Ruinas/MercadorAcenando"), new Vector2Int(110, 110), 32f);

        // Ninguem atravessa o mercador.
        obj.layer = Pedreiro.CamadaDaParede;
        BoxCollider2D c = obj.AddComponent<BoxCollider2D>();
        c.size = new Vector2(0.9f, 0.5f);
        c.offset = new Vector2(0f, -0.5f);
        return m;
    }

    private void Update()
    {
        if (jogador == null)
        {
            GameObject j = GameObject.FindWithTag("Player");
            jogador = j != null ? j.transform : null;
        }

        bool perto = jogador != null && Vector2.Distance(jogador.position, transform.position) < Perto;
        Sprite[] quadros = perto && acenando.Length > 0 ? acenando : parado;
        desenho.sprite = quadros[Mathf.FloorToInt(Time.time * 8f) % quadros.Length];

        if (jogador != null)
            desenho.flipX = jogador.position.x < transform.position.x;
    }
}
