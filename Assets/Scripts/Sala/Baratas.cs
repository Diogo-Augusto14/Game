using UnityEngine;

/// <summary>
/// Um bando de baratas do Old Prison andando no pe de uma parede, de um ponto a outro e de
/// volta, parando de vez em quando. So enfeite: nao bloqueia nem machuca.
/// Monte por <see cref="Criar"/>.
/// </summary>
[DisallowMultipleComponent]
public class Baratas : MonoBehaviour
{
    private const float Velocidade = 0.7f;

    private SpriteRenderer desenho;
    private Sprite[] quadros;
    private Vector2 de;
    private Vector2 ate;
    private float t;
    private float sentido = 1f;
    private float paradaAte;
    private float passo;

    /// <summary>Baratas indo e vindo entre <paramref name="a"/> e <paramref name="b"/> (locais ao pai).</summary>
    public static Baratas Criar(Transform pai, Vector2 a, Vector2 b)
    {
        Sprite[] quadros = ArteImportada.Baratas;

        if (quadros == null)
            return null;

        SpriteRenderer sr = FormasDaSala.Desenho(pai, "Baratas", quadros[0], new Color(0.8f, 0.75f, 0.75f), a, Vector2.one * 0.6f, -7);
        Baratas baratas = sr.gameObject.AddComponent<Baratas>();
        baratas.desenho = sr;
        baratas.quadros = quadros;
        baratas.de = a;
        baratas.ate = b;
        baratas.t = Random.value;
        return baratas;
    }

    private void Update()
    {
        if (Time.time < paradaAte)
            return;

        float comprimento = Mathf.Max(0.1f, Vector2.Distance(de, ate));
        t += sentido * Velocidade / comprimento * Time.deltaTime;

        if (t >= 1f || t <= 0f)
        {
            t = Mathf.Clamp01(t);
            sentido = -sentido;
            paradaAte = Time.time + Random.Range(0.8f, 2.5f);
        }
        else if (Random.value < Time.deltaTime * 0.3f)
        {
            paradaAte = Time.time + Random.Range(0.3f, 1.2f);
        }

        passo += Time.deltaTime * 12f;
        desenho.sprite = quadros[Mathf.FloorToInt(passo) % quadros.Length];
        desenho.flipX = sentido < 0f;
        transform.localPosition = Vector2.Lerp(de, ate, t);
    }
}
