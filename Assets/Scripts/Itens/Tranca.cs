using UnityEngine;

/// <summary>
/// Cadeado no vao de uma porta: bloqueia a passagem ate o jogador encostar com uma chave.
/// Gasta a chave e some. O <see cref="Andar"/> tranca a porta da sala do item a partir do
/// andar 2, como no Isaac.
///
/// E uma peca separada da <see cref="Porta"/> de proposito: a sala abre e fecha as portas
/// sozinha (luta), e a tranca tem que continuar la mesmo com a sala limpa.
/// </summary>
[DisallowMultipleComponent]
public class Tranca : MonoBehaviour
{
    [SerializeField] private string tagDoJogador = "Player";

    public static Tranca Criar(Porta porta)
    {
        GameObject obj = new GameObject("Tranca");
        obj.transform.SetParent(porta.transform, false);
        obj.layer = Sala.CamadaDeParede;

        // Cobre o vao inteiro (1.5 de largura por 1 de espessura, o padrao da Sala).
        Vector2 tamanho = porta.Lado.Horizontal() ? new Vector2(1.5f, 1f) : new Vector2(1f, 1.5f);

        BoxCollider2D solido = obj.AddComponent<BoxCollider2D>();
        solido.size = tamanho;

        FormasDaSala.Desenho(obj.transform, "Grade", FormasDaSala.Quadrado(), new Color(0.75f, 0.6f, 0.25f), Vector2.zero, tamanho, 3);
        FormasDaSala.Desenho(obj.transform, "Cadeado", FormasDaSala.Circulo(), Coletavel.CorDe(TipoDeColetavel.Chave), Vector2.zero, Vector2.one * 0.45f, 4);

        return obj.AddComponent<Tranca>();
    }

    private void OnCollisionEnter2D(Collision2D contato) => TentarAbrir(contato);

    // Stay: quem encostou sem chave e pegou uma depois nao precisa se afastar e voltar.
    private void OnCollisionStay2D(Collision2D contato) => TentarAbrir(contato);

    private void TentarAbrir(Collision2D contato)
    {
        GameObject quem = contato.rigidbody != null ? contato.rigidbody.gameObject : contato.gameObject;

        if (!quem.CompareTag(tagDoJogador))
            return;

        Inventario inventario = quem.GetComponent<Inventario>();

        if (inventario != null && inventario.Gastar(TipoDeColetavel.Chave))
            Destroy(gameObject);
    }
}
