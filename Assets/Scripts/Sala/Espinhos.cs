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

        // A placa escura so faz falta no desenho gerado: o do pacote ja vem com a base.
        if (!ArteGerada.CenarioDoPacote)
            FormasDaSala.Desenho(obj.transform, "Base", FormasDaSala.Quadrado(), new Color(0.16f, 0.13f, 0.12f, 0.6f),
                Vector2.zero, Vector2.one * 0.9f, -9);
        FormasDaSala.Desenho(obj.transform, "Pontas", ArteGerada.EspinhosNoChao(), Color.white, Vector2.zero, Vector2.one, -8);

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
