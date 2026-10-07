using System.Collections.Generic;
using UnityEngine;

/// <summary>Uma vela acesa (a chama tremula).</summary>
public class Vela : MonoBehaviour
{
    private SpriteRenderer chama;
    private Sprite[] quadros;
    private float fase;

    public static Vela Criar(Vector2 onde, Transform pai)
    {
        GameObject obj = new GameObject("Vela");
        obj.transform.SetParent(pai, false);
        obj.transform.position = onde;
        SpriteRenderer corpo = obj.AddComponent<SpriteRenderer>();
        corpo.sprite = ArteDoAntigo.Peca("Vela" + Random.Range(1, 5));
        corpo.sortingOrder = Pedreiro.OrdemDosEnfeites + 2;

        GameObject fogo = new GameObject("Chama");
        fogo.transform.SetParent(obj.transform, false);
        fogo.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        fogo.transform.localScale = Vector3.one * 0.6f;

        Vela v = obj.AddComponent<Vela>();
        v.chama = fogo.AddComponent<SpriteRenderer>();
        v.chama.sortingOrder = Pedreiro.OrdemDosEnfeites + 3;
        v.quadros = ArteDoAntigo.ChamaMagica;
        v.fase = Random.value * 10f;
        return v;
    }

    private void Update()
    {
        if (quadros.Length > 0)
            chama.sprite = quadros[Mathf.FloorToInt((Time.time + fase) * 10f) % quadros.Length];
    }
}
