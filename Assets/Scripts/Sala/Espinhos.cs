using UnityEngine;

/// <summary>
/// Espinhos no chao: da pra passar por cima, mas machuca o jogador. Nao bloqueiam tiro
/// nem inimigo (inimigo nao se machuca). A invencibilidade do jogador depois do golpe
/// segura o dano, entao ficar parado em cima tira vida aos poucos, nao de uma vez.
/// </summary>
[DisallowMultipleComponent]
public class Espinhos : MonoBehaviour
{
    [SerializeField, Min(0f)] private float dano = 10f;

    [SerializeField, Min(0f)] private float empurrao = 3f;

    [SerializeField] private string tagDoJogador = "Player";

    public static Espinhos Criar(Transform pai, Vector2 posicaoLocal)
    {
        GameObject obj = new GameObject("Espinhos");
        obj.transform.SetParent(pai, false);
        obj.transform.localPosition = posicaoLocal;

        FormasDaSala.Desenho(obj.transform, "Base", FormasDaSala.Quadrado(), new Color(0.16f, 0.13f, 0.12f),
            Vector2.zero, Vector2.one * 0.9f, -9);

        // Quatro pontas em losango.
        for (int i = 0; i < 4; i++)
        {
            Vector2 canto = new Vector2(i % 2 == 0 ? -0.2f : 0.2f, i < 2 ? -0.2f : 0.2f);
            SpriteRenderer ponta = FormasDaSala.Desenho(obj.transform, "Ponta", FormasDaSala.Circulo(),
                new Color(0.78f, 0.78f, 0.82f), canto, new Vector2(0.14f, 0.3f), -8);
            ponta.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        BoxCollider2D area = obj.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = Vector2.one * 0.7f;

        return obj.AddComponent<Espinhos>();
    }

    private void OnTriggerStay2D(Collider2D outro)
    {
        GameObject quem = outro.attachedRigidbody != null ? outro.attachedRigidbody.gameObject : outro.gameObject;

        if (!quem.CompareTag(tagDoJogador))
            return;

        Vida vida = quem.GetComponentInParent<Vida>();

        if (vida == null || vida.EstaMorto || vida.EstaInvencivel)
            return;

        Vector2 direcao = (Vector2)quem.transform.position - (Vector2)transform.position;
        vida.TomarDano(new DanoInfo(dano, direcao, empurrao, transform.position, gameObject));
    }
}
