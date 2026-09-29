using UnityEngine;

/// <summary>
/// Toca uma lista de quadros uma vez, num objeto solto no mundo, e se apaga no fim.
/// Serve pra efeito de arte importada que nao esta na biblioteca de animacoes:
/// explosao, caveira de morte. Com <see cref="EmLoop"/>, repete pra sempre (tocha,
/// candelabro).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class EfeitoDeQuadros : MonoBehaviour
{
    private Sprite[] quadros;
    private float quadrosPorSegundo;
    private float inicio;
    private SpriteRenderer desenho;
    private bool emLoop;
    private float ultimoQuadroFica;

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

    /// <summary>Repete a animacao pra sempre, comecando num quadro sorteado (varias tochas nao piscam juntas).</summary>
    public EfeitoDeQuadros EmLoop()
    {
        emLoop = true;
        inicio -= Random.value * quadros.Length / quadrosPorSegundo;
        return this;
    }

    /// <summary>Segura o ultimo quadro por mais este tempo antes de sumir (o corpo caido).</summary>
    public EfeitoDeQuadros SegurarNoFim(float segundos)
    {
        ultimoQuadroFica = Mathf.Max(0f, segundos);
        return this;
    }

    /// <summary>Espelha o desenho (a arte olha pra direita; o bicho morreu olhando pra esquerda).</summary>
    public void Virar(bool praEsquerda) => desenho.flipX = praEsquerda;

    private void Update()
    {
        int quadro = Mathf.FloorToInt((Time.time - inicio) * quadrosPorSegundo);

        if (emLoop)
        {
            quadro %= quadros.Length;
        }
        else if (quadro >= quadros.Length)
        {
            if (Time.time - inicio >= quadros.Length / quadrosPorSegundo + ultimoQuadroFica)
                Destroy(gameObject);

            return;
        }

        desenho.sprite = quadros[quadro];
    }
}
