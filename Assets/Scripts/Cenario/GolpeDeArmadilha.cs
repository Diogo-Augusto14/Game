using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O golpe das armadilhas (serra, tronco, espinhos): meio coracao no jogador (a esquiva protege, como
/// sempre) e um dano bom nos inimigos, que tambem caem nelas. Cada um so apanha de novo depois de um
/// tempinho.
/// </summary>
public static class GolpeDeArmadilha
{
    public static void Ferir(Vector2 onde, float raio, GameObject fonte, float danoNoInimigo, Dictionary<Vida, float> proximo)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(onde, raio))
        {
            Vida vida = c != null ? c.GetComponentInParent<Vida>() : null;

            if (vida == null || vida.Morto || vida.Lado == Lado.Neutro)
                continue;

            if (proximo.TryGetValue(vida, out float quando) && Time.time < quando)
                continue;

            proximo[vida] = Time.time + 0.6f;
            Vector2 rumo = (Vector2)vida.transform.position - onde;
            float dano = vida.Lado == Lado.Jogador ? 1f : danoNoInimigo;
            vida.ReceberDano(new Dano(dano, rumo.sqrMagnitude > 0.001f ? rumo.normalized : Vector2.up, 7f, fonte));
        }
    }
}
