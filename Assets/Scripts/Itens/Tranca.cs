using System.Collections;
using UnityEngine;

/// <summary>
/// Cadeado no vao de uma porta: bloqueia a passagem ate o jogador encostar com uma chave.
/// Gasta a chave e some. O <see cref="Andar"/> tranca a porta da sala do item a partir do
/// andar 2, como no Isaac.
///
/// E uma peca separada da <see cref="Porta"/> de proposito: a sala abre e fecha as portas
/// sozinha (luta), e a tranca tem que continuar la mesmo com a sala limpa.
///
/// Com chave: o cadeado estala e sobe, a grade da cela (Old Prison) sobe quadro a quadro e so no fim a
/// passagem libera. Sem chave: o cadeado treme e faz o som de negado.
/// </summary>
[DisallowMultipleComponent]
public class Tranca : MonoBehaviour
{
    [SerializeField] private string tagDoJogador = "Player";

    [SerializeField, Min(0.05f)] private float tempoDoPortao = 0.4f;

    private SpriteRenderer grade;
    private SpriteRenderer cadeado;
    private bool abrindo;
    private float negadoEm = -10f;

    /// <summary>A chave ja foi gasta e o portao esta subindo.</summary>
    public bool Abrindo => abrindo;

    public static Tranca Criar(Porta porta)
    {
        GameObject obj = new GameObject("Tranca");
        obj.transform.SetParent(porta.transform, false);
        obj.layer = Sala.CamadaDeParede;

        // Cobre o vao inteiro (1.5 de largura por 1 de espessura, o padrao da Sala).
        Vector2 tamanho = porta.Lado.Horizontal() ? new Vector2(1.5f, 1f) : new Vector2(1f, 1.5f);

        BoxCollider2D solido = obj.AddComponent<BoxCollider2D>();
        solido.size = tamanho;

        Sprite[] cela = ArteImportada.PortaoDaCela;
        Sprite chave = ArteImportada.Objeto(9, 4);
        Tranca tranca = obj.AddComponent<Tranca>();

        if (cela != null && chave != null)
        {
            // A grade da cela do Old Prison, puxada pro dourado, com a chave na frente: ja diz o
            // que abre. Desenhada de frente (1.5x1 sem esticar) e girada pro lado da porta.
            tranca.grade = FormasDaSala.Desenho(obj.transform, "Grade", cela[0], new Color(1f, 0.85f, 0.45f), Vector2.zero,
                Vector2.one, 3);
            tranca.grade.transform.localRotation = Quaternion.Euler(0f, 0f, porta.Lado == LadoDaPorta.Baixo ? 180f
                : porta.Lado == LadoDaPorta.Esquerda ? 90f : porta.Lado == LadoDaPorta.Direita ? -90f : 0f);
            tranca.cadeado = FormasDaSala.Desenho(obj.transform, "Cadeado", chave, Color.white, Vector2.zero, Vector2.one * 0.7f, 4);
        }
        else
        {
            tranca.grade = FormasDaSala.Desenho(obj.transform, "Grade", FormasDaSala.Quadrado(), new Color(0.75f, 0.6f, 0.25f), Vector2.zero, tamanho, 3);
            tranca.cadeado = FormasDaSala.Desenho(obj.transform, "Cadeado", FormasDaSala.Circulo(), Coletavel.CorDe(TipoDeColetavel.Chave), Vector2.zero, Vector2.one * 0.45f, 4);
        }

        return tranca;
    }

    private void OnCollisionEnter2D(Collision2D contato) => TentarAbrir(contato);

    // Stay: quem encostou sem chave e pegou uma depois nao precisa se afastar e voltar.
    private void OnCollisionStay2D(Collision2D contato) => TentarAbrir(contato);

    private void TentarAbrir(Collision2D contato)
    {
        if (abrindo)
            return;

        GameObject quem = contato.rigidbody != null ? contato.rigidbody.gameObject : contato.gameObject;

        if (!quem.CompareTag(tagDoJogador))
            return;

        Inventario inventario = quem.GetComponent<Inventario>();

        if (inventario != null && inventario.Gastar(TipoDeColetavel.Chave))
        {
            abrindo = true;
            StartCoroutine(Destrancar());
        }
        else if (Time.time - negadoEm > 0.8f)
        {
            negadoEm = Time.time;
            Sons.Tocar(Som.Negado);
            StartCoroutine(Tremer(cadeado != null ? cadeado.transform : null));
        }
    }

    private IEnumerator Destrancar()
    {
        Sons.Tocar(Som.Destranca);

        // O cadeado da um pulo pra cima e some.
        if (cadeado != null)
        {
            Vector3 inicio = cadeado.transform.localPosition;
            Vector3 escala = cadeado.transform.localScale;
            Color cor = cadeado.color;

            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.25f)
            {
                cadeado.transform.localPosition = inicio + Vector3.up * (t * 0.5f);
                cadeado.transform.localScale = escala * (1f + t * 0.4f);
                cadeado.color = new Color(cor.r, cor.g, cor.b, 1f - t);
                yield return null;
            }

            cadeado.enabled = false;
        }

        // A grade da cela sobe quadro a quadro.
        Sons.Tocar(Som.PortaAbre);
        Sprite[] quadros = ArteImportada.PortaoDaCela;

        for (float t = 0f; t < 1f; t += Time.deltaTime / tempoDoPortao)
        {
            if (grade != null && quadros != null)
                grade.sprite = quadros[Mathf.Clamp(Mathf.RoundToInt(t * (quadros.Length - 1)), 0, quadros.Length - 1)];
            else if (grade != null)
                grade.color = new Color(grade.color.r, grade.color.g, grade.color.b, 1f - t);

            yield return null;
        }

        // So agora a passagem fica livre (o colisor some junto com a tranca).
        Destroy(gameObject);
    }

    private static IEnumerator Tremer(Transform alvo)
    {
        if (alvo == null)
            yield break;

        Vector3 inicio = alvo.localPosition;

        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            alvo.localPosition = inicio + Vector3.right * (Mathf.Sin(t * 80f) * 0.08f);
            yield return null;
        }

        alvo.localPosition = inicio;
    }
}
