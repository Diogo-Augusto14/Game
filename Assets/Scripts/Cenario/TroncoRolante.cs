using System.Collections.Generic;
using UnityEngine;

/// <summary>O tronco com estacas que rola de um lado a outro do corredor, de tempos em tempos.</summary>
public class TroncoRolante : MonoBehaviour
{
    private Vector2 de;
    private Vector2 ate;
    private SpriteRenderer desenho;
    private Sprite[] quadros;
    private float proximo;
    private float comecou = -1f;
    private float duracao;
    private readonly Dictionary<Vida, float> atingidos = new Dictionary<Vida, float>();

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
        t.desenho.enabled = false;
        t.duracao = Vector2.Distance(de, ate) / 6f;
        t.proximo = Time.time + Random.Range(2f, 5f);

        // O tronco em pe rola pros lados; deitado no corredor de cima pra baixo, gira 90 graus.
        if (Mathf.Abs(ate.y - de.y) > Mathf.Abs(ate.x - de.x))
            obj.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

        return t;
    }

    private void Update()
    {
        if (comecou < 0f)
        {
            if (Time.time < proximo)
                return;

            comecou = Time.time;
            desenho.enabled = true;
            Sons.Tocar(Som.Queda, 0.4f);
        }

        float t = (Time.time - comecou) / Mathf.Max(0.1f, duracao);

        if (t >= 1f)
        {
            comecou = -1f;
            desenho.enabled = false;
            proximo = Time.time + Random.Range(3f, 6f);
            return;
        }

        transform.position = Vector2.Lerp(de, ate, t);

        if (quadros.Length > 0)
            desenho.sprite = quadros[Mathf.FloorToInt(t * 30f) % quadros.Length];

        GolpeDeArmadilha.Ferir(transform.position, 0.9f, gameObject, 12f, atingidos);
    }
}
