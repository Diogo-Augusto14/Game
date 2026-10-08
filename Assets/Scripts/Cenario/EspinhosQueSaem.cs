using System.Collections.Generic;
using UnityEngine;

/// <summary>Espinhos que saem do chao: escondidos, um aviso (sobem um pouco) e depois em pe, ferindo.</summary>
public class EspinhosQueSaem : MonoBehaviour
{
    private SpriteRenderer desenho;
    private static Sprite baixos;
    private static Sprite aviso;
    private static Sprite emPe;
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
        if (baixos == null)
        {
            baixos = Resources.Load<Sprite>("Itens/EspinhosBaixos");
            aviso = Resources.Load<Sprite>("Itens/EspinhosAviso");
            emPe = Resources.Load<Sprite>("Itens/EspinhosEmPe");
        }

        e.desenho.sprite = baixos;
        e.desenho.sortingOrder = Pedreiro.OrdemDosEnfeites + 1;
        e.fase = fase;
        return e;
    }

    private void Update()
    {
        float ciclo = Escondido + Aviso + EmPe;
        float t = (Time.time + fase) % ciclo;

        // Uma placa de metal com furos: as pontas aparecem um pouco (o aviso) e depois sobem inteiras.
        if (t < Escondido)
        {
            desenho.sprite = baixos;
        }
        else if (t < Escondido + Aviso)
        {
            desenho.sprite = aviso;
        }
        else
        {
            desenho.sprite = emPe;
            GolpeDeArmadilha.Ferir(transform.position, 0.45f, gameObject, 5f, atingidos);
        }
    }
}
