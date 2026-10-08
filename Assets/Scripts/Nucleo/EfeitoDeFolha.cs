using UnityEngine;

/// <summary>
/// Um efeito que toca uma vez e some: a poeira de quem morre, e depois explosoes, faiscas... A folha
/// e cortada como as do jogador (ver <see cref="FolhaDeSprites"/>). Monte com <see cref="Tocar"/>.
/// </summary>
[DisallowMultipleComponent]
public class EfeitoDeFolha : MonoBehaviour
{
    private SpriteRenderer desenho;
    private Sprite[] quadros;
    private float quadrosPorSegundo;
    private float comecou;

    public static EfeitoDeFolha Tocar(Texture2D folha, Vector2Int tamanhoDoQuadro, float pixelsPorUnidade,
                                      Vector2 onde, float quadrosPorSegundo, int ordem = 15)
    {
        Sprite[] quadros = FolhaDeSprites.Cortar(folha, tamanhoDoQuadro, pixelsPorUnidade);

        if (quadros.Length == 0)
            return null;

        GameObject obj = new GameObject(folha.name + " (efeito)");
        obj.transform.position = onde;

        EfeitoDeFolha efeito = obj.AddComponent<EfeitoDeFolha>();
        efeito.desenho = obj.AddComponent<SpriteRenderer>();
        efeito.desenho.sortingOrder = ordem;
        Iluminacao.Brilhar(efeito.desenho);
        efeito.desenho.sprite = quadros[0];
        efeito.quadros = quadros;
        efeito.quadrosPorSegundo = quadrosPorSegundo;
        efeito.comecou = Time.time;
        return efeito;
    }

    /// <summary>O mesmo, com os quadros ja recortados (a arte do jogo antigo, ver <see cref="ArteDoAntigo"/>).</summary>
    public static EfeitoDeFolha Tocar(Sprite[] quadros, Vector2 onde, float quadrosPorSegundo, int ordem = 15)
    {
        if (quadros == null || quadros.Length == 0)
            return null;

        GameObject obj = new GameObject("Efeito");
        obj.transform.position = onde;

        EfeitoDeFolha efeito = obj.AddComponent<EfeitoDeFolha>();
        efeito.desenho = obj.AddComponent<SpriteRenderer>();
        efeito.desenho.sortingOrder = ordem;
        Iluminacao.Brilhar(efeito.desenho);
        efeito.desenho.sprite = quadros[0];
        efeito.quadros = quadros;
        efeito.quadrosPorSegundo = quadrosPorSegundo;
        efeito.comecou = Time.time;
        return efeito;
    }

    private void Update()
    {
        int quadro = Mathf.FloorToInt((Time.time - comecou) * quadrosPorSegundo);

        if (quadro >= quadros.Length)
        {
            Destroy(gameObject);
            return;
        }

        desenho.sprite = quadros[quadro];
    }
}
