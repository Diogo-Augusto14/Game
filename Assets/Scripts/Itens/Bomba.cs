using System.Collections;
using UnityEngine;

/// <summary>
/// A bomba do jogador (G): a dinamite acesa pisca uns instantes e explode. Fere inimigos, o proprio
/// jogador se estiver perto, quebra mesas, barris e as pedras rachadas (que so bomba quebra).
/// </summary>
public class Bomba : MonoBehaviour
{
    private const float Pavio = 1.5f;

    private float raio;
    private float dano;

    public static Bomba Criar(Vector2 onde, GameObject dono, float raio, float dano)
    {
        GameObject obj = new GameObject("Bomba");
        obj.transform.SetParent(GeradorDoAndar.Raiz, true);
        obj.transform.position = onde;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 12;
        Bomba bomba = obj.AddComponent<Bomba>();
        bomba.raio = raio;
        bomba.dano = dano;
        Sons.Tocar(Som.BombaAcesa, 0.7f);
        return bomba;
    }

    private void Start() => StartCoroutine(Queimar());

    private IEnumerator Queimar()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Sprite[] quadros = ArteDoAntigo.Dinamite;

        for (float t = 0f; t < Pavio; t += Time.deltaTime)
        {
            if (quadros.Length > 0)
                sr.sprite = quadros[Mathf.FloorToInt(t * 10f) % quadros.Length];

            // Pisca vermelho cada vez mais rapido no fim.
            float pisca = Mathf.Sin(t * t * 18f);
            sr.color = pisca > 0.6f ? new Color(1f, 0.5f, 0.45f) : Color.white;
            yield return null;
        }

        Explosao.Criar(transform.position, raio, dano, null, gameObject);
        Destroy(gameObject);
    }
}
