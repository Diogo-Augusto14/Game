using UnityEngine;

/// <summary>
/// Bloco de parede dentro da sala: faz a sala ganhar forma (L, corredor, cruz). Bloqueia
/// andar e tiro como a parede de verdade e nao quebra com bomba. Usa os tijolos do tema.
/// </summary>
[DisallowMultipleComponent]
public class Muro : MonoBehaviour
{
    public static Muro Criar(Transform pai, Vector2 posicaoLocal)
    {
        SpriteRenderer sr = FormasDaSala.DesenhoLadrilhado(pai, "Muro", ArteGerada.Tijolo(), Color.white,
            posicaoLocal, Vector2.one, 0);
        GameObject obj = sr.gameObject;
        obj.layer = Sala.CamadaDeParede;

        // Sombra no pe do bloco, pra ele nao parecer um adesivo no chao.
        SpriteRenderer sombra = FormasDaSala.Desenho(obj.transform, "Sombra", Fosso.Pixel(), new Color(0f, 0f, 0f, 0.35f),
            new Vector2(0f, -0.4f), new Vector2(1f, 0.2f), 1);
        sombra.sortingOrder = 1;

        BoxCollider2D caixa = obj.AddComponent<BoxCollider2D>();
        caixa.size = Vector2.one;

        return obj.AddComponent<Muro>();
    }
}
