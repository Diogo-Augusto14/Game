using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O tronco com estacas, parado encostado numa parede da sala: de tempos em tempos rola ate a parede da
/// frente e fica la, ate rolar de volta. Parado, segura gente e tiro (como uma parede); rolando, machuca
/// em todo o comprimento.
/// </summary>
public class TroncoRolante : MonoBehaviour
{
    /// <summary>Quanto o tronco em pe fica longe da celula encostada na parede (nao cobre a beirada dela).</summary>
    public const float LongeDaParede = 0.55f;

    // O desenho tem 3,3 de comprido e 1,15 de grossura (ArteDoAntigo.Tronco).
    private const float MeioComprimento = 1.6f;
    private const float Grossura = 1f;

    private Vector2 de;
    private Vector2 ate;
    private SpriteRenderer desenho;
    private BoxCollider2D colisor;
    private Sprite[] quadros;
    private float proximo;
    private float comecou = -1f;
    private float duracao;
    private bool volta;
    private readonly Dictionary<Vida, float> atingidos = new Dictionary<Vida, float>();

    /// <param name="de">A ponta encostada na parede onde ele comeca parado.</param>
    /// <param name="ate">A ponta encostada na parede da frente.</param>
    public static TroncoRolante Criar(Vector2 de, Vector2 ate, Transform pai)
    {
        GameObject obj = new GameObject("Tronco rolante");
        obj.transform.SetParent(pai, false);
        obj.transform.position = de;

        TroncoRolante t = obj.AddComponent<TroncoRolante>();
        t.de = de;
        t.ate = ate;
        t.quadros = ArteDoAntigo.Tronco;
        t.desenho = obj.AddComponent<SpriteRenderer>();
        t.desenho.sortingOrder = 9;
        t.desenho.sprite = t.quadros.Length > 0 ? t.quadros[0] : null;
        t.duracao = Vector2.Distance(de, ate) / 6f;
        t.proximo = Time.time + Random.Range(2f, 5f);

        // O tronco em pe rola pros lados; deitado no corredor de cima pra baixo, gira 90 graus.
        if (Mathf.Abs(ate.y - de.y) > Mathf.Abs(ate.x - de.x))
            obj.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

        // Parado, segura gente e tiro (o colisor gira junto com o tronco).
        obj.layer = Pedreiro.CamadaDaParede;
        t.colisor = obj.AddComponent<BoxCollider2D>();
        t.colisor.size = new Vector2(Grossura, MeioComprimento * 2f);
        return t;
    }

    private void Update()
    {
        if (comecou < 0f)
        {
            if (Time.time < proximo)
                return;

            comecou = Time.time;
            colisor.enabled = false;
            Sons.Tocar(Som.Queda, 0.4f);
        }

        float t = (Time.time - comecou) / Mathf.Max(0.1f, duracao);

        // Chegou na outra parede: fica parado la ate a proxima.
        if (t >= 1f)
        {
            transform.position = volta ? de : ate;
            volta = !volta;
            comecou = -1f;
            colisor.enabled = true;
            proximo = Time.time + Random.Range(3f, 6f);
            return;
        }

        transform.position = volta ? Vector2.Lerp(ate, de, t) : Vector2.Lerp(de, ate, t);

        // Na volta, a animacao de rolar anda ao contrario.
        if (quadros.Length > 0)
        {
            int q = Mathf.FloorToInt(t * 30f) % quadros.Length;
            desenho.sprite = quadros[volta ? quadros.Length - 1 - q : q];
        }

        // Machuca em todo o comprimento: tres circulos ao longo do tronco (cada um so apanha uma vez por golpe).
        Vector2 meio = transform.position;
        Vector2 eixo = transform.up * (MeioComprimento - Grossura * 0.5f);
        GolpeDeArmadilha.Ferir(meio, Grossura * 0.6f, gameObject, 12f, atingidos);
        GolpeDeArmadilha.Ferir(meio + eixo, Grossura * 0.6f, gameObject, 12f, atingidos);
        GolpeDeArmadilha.Ferir(meio - eixo, Grossura * 0.6f, gameObject, 12f, atingidos);
    }
}
