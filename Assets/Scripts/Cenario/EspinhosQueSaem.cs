using System.Collections.Generic;
using UnityEngine;

/// <summary>Espinhos que saem do chao: escondidos, um aviso (sobem um pouco) e depois em pe, ferindo.</summary>
public class EspinhosQueSaem : MonoBehaviour
{
    private SpriteRenderer desenho;
    private float fase;
    private readonly Dictionary<Vida, float> atingidos = new Dictionary<Vida, float>();

    private const float Escondido = 2f;
    private const float Aviso = 0.6f;
    private const float EmPe = 1.1f;

    public static EspinhosQueSaem Criar(Vector2 onde, Transform pai, float fase)
    {
        GameObject obj = new GameObject("Espinhos");
        obj.transform.SetParent(pai, false);
        obj.transform.position = onde;
        EspinhosQueSaem e = obj.AddComponent<EspinhosQueSaem>();
        e.desenho = obj.AddComponent<SpriteRenderer>();
        e.desenho.sprite = ArteDoAntigo.Espinhos;
        e.desenho.sortingOrder = Pedreiro.OrdemDosEnfeites + 1;
        e.fase = fase;
        return e;
    }

    private void Update()
    {
        float ciclo = Escondido + Aviso + EmPe;
        float t = (Time.time + fase) % ciclo;

        if (t < Escondido)
        {
            desenho.color = new Color(1f, 1f, 1f, 0.25f);
            transform.localScale = new Vector3(1f, 0.4f, 1f);
        }
        else if (t < Escondido + Aviso)
        {
            desenho.color = new Color(1f, 0.7f, 0.65f, 0.7f);
            transform.localScale = new Vector3(1f, 0.6f, 1f);
        }
        else
        {
            desenho.color = Color.white;
            transform.localScale = Vector3.one;
            GolpeDeArmadilha.Ferir(transform.position, 0.45f, gameObject, 5f, atingidos);
        }
    }
}
