using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// A luz do jogo (luzes 2D do URP). O andar e escuro: a luz ambiente ("Luz global" da cena) fica bem
/// fraca, num tom de cada mundo (<see cref="Ambiente"/>), e so ilumina quem tem luz: o heroi (uma luz
/// fraca em volta dele), as tochas, velas, candelabros e cristais, o portal, a estatua de fogo.
///
/// O que nao pode sumir no escuro (os tiros, as moedas, os efeitos, as marcas dos chefes, os itens) vai
/// pra camada de desenho "Brilho" (<see cref="Brilhar"/>), que outra luz global (a "Luz do brilho") deixa
/// sempre acesa. Essa camada e desenhada por cima da "Default".
/// </summary>
public static class Iluminacao
{
    public const string CamadaDoBrilho = "Brilho";

    private static Light2D ambiente;

    /// <summary>Cores de luz que se repetem.</summary>
    public static readonly Color Fogo = new Color(1f, 0.66f, 0.36f);
    public static readonly Color Vela = new Color(1f, 0.78f, 0.5f);
    public static readonly Color Heroi = new Color(1f, 0.93f, 0.82f);

    /// <summary>Poe o desenho na camada que nao escurece.</summary>
    public static void Brilhar(Renderer desenho)
    {
        int camada = SortingLayer.NameToID(CamadaDoBrilho);

        if (desenho != null && SortingLayer.IsValid(camada))
            desenho.sortingLayerID = camada;
    }

    /// <summary>
    /// Uma luz redonda, filha de <paramref name="pai"/>. Com <paramref name="tremor"/> &gt; 0, ela
    /// tremula (fogo).
    /// </summary>
    public static Light2D Luz(Transform pai, Vector2 local, Color cor, float raio, float intensidade, float tremor = 0f)
    {
        GameObject obj = new GameObject("Luz");
        obj.transform.SetParent(pai, false);
        obj.transform.localPosition = local;

        Light2D luz = obj.AddComponent<Light2D>();
        luz.lightType = Light2D.LightType.Point;
        luz.color = cor;
        luz.intensity = intensidade;
        luz.pointLightOuterRadius = raio;
        luz.pointLightInnerRadius = raio * 0.15f;
        luz.falloffIntensity = 0.6f;
        luz.shadowsEnabled = false;

        if (tremor > 0f)
            obj.AddComponent<LuzTremula>().Comecar(luz, tremor);

        return luz;
    }

    /// <summary>A luz ambiente do andar (a "Luz global" da cena).</summary>
    public static void Ambiente(float intensidade, Color cor)
    {
        if (ambiente == null)
        {
            GameObject obj = GameObject.Find("Luz global");
            ambiente = obj != null ? obj.GetComponent<Light2D>() : null;
        }

        if (ambiente == null)
            return;

        ambiente.intensity = intensidade;
        ambiente.color = cor;
    }
}
