using UnityEngine;

/// <summary>
/// Toca uma lista de quadros uma vez, num objeto solto no mundo, e se apaga no fim.
/// Serve pra efeito de arte importada que nao esta na biblioteca de animacoes:
/// explosao, caveira de morte.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class EfeitoDeQuadros : MonoBehaviour
{
    private Sprite[] quadros;
    private float quadrosPorSegundo;
    private float inicio;
    private SpriteRenderer desenho;

    /// <summary>Cria o efeito na posicao dada. Sem quadros, nao cria nada e devolve null.</summary>
    public static EfeitoDeQuadros Criar(Sprite[] quadros, float quadrosPorSegundo, Vector2 posicao, int ordem,
                                        Transform pai = null)
    {
        if (quadros == null || quadros.Length == 0)
            return null;

        GameObject obj = new GameObject("Efeito");
        obj.transform.SetParent(pai, true);
        obj.transform.position = posicao;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = quadros[0];
        sr.sortingOrder = ordem;

        EfeitoDeQuadros efeito = obj.AddComponent<EfeitoDeQuadros>();
        efeito.desenho = sr;
        efeito.quadros = quadros;
        efeito.quadrosPorSegundo = Mathf.Max(1f, quadrosPorSegundo);
        efeito.inicio = Time.time;
        return efeito;
    }

    private void Update()
    {
        int quadro = Mathf.FloorToInt((Time.time - inicio) * quadrosPorSegundo);

        if (quadro >= quadros.Length)
        {
            Destroy(gameObject);
            return;
        }

        desenho.sprite = quadros[quadro];
    }
}
