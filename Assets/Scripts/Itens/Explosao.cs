using UnityEngine;

/// <summary>
/// Uma explosao: fere tudo que tem <see cref="Vida"/> no raio (menos quem e do lado poupado), empurra,
/// treme a tela e toca a animacao. A bomba poupa ninguem (o jogador perto tambem leva meio coracao);
/// a flecha explosiva e a polvora em chamas poupam o jogador.
/// </summary>
public static class Explosao
{
    public static void Criar(Vector2 onde, float raio, float dano, Lado? poupa, GameObject fonte)
    {
        EfeitoDeFolha.Tocar(ArteDoAntigo.Explosao(raio), onde, 18f, 30);
        Sons.Tocar(Som.Explosao, Mathf.Clamp(raio / 2f, 0.4f, 1f));
        CameraDoJogo.Tremer(Mathf.Clamp(raio * 0.12f, 0.08f, 0.4f), 0.25f);

        foreach (Collider2D c in Physics2D.OverlapCircleAll(onde, raio))
        {
            Vida vida = c != null ? c.GetComponentInParent<Vida>() : null;

            if (vida == null || vida.Morto || (poupa.HasValue && vida.Lado == poupa.Value))
                continue;

            Vector2 rumo = (Vector2)vida.transform.position - onde;

            // No jogador, a explosao tira meio coracao (como no jogo antigo), nao o dano todo.
            float quanto = vida.Lado == Lado.Jogador ? 1f : dano;
            vida.ReceberDano(new Dano(quanto, rumo.sqrMagnitude > 0.0001f ? rumo.normalized : Vector2.up, 8f, fonte));
        }
    }
}
