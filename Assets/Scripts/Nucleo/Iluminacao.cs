using UnityEngine;

/// <summary>
/// A luz do jogo. O andar e escuro (<see cref="Escuridao"/>, um veu por cima do mundo): a luz de fundo e
/// bem fraca, num tom de cada mundo (<see cref="Ambiente"/>), e so ve bem quem esta perto de uma luz: o
/// heroi (uma luz fraca em volta dele), as tochas, velas, candelabros e cristais, o portal, a estatua de fogo.
///
/// O que nao pode sumir no escuro (os tiros, as moedas, os efeitos, as marcas dos chefes, os itens) vai
/// pra camada de desenho "Brilho" (<see cref="Brilhar"/>), que e desenhada por cima do veu.
/// </summary>
public static class Iluminacao
{
    public const string CamadaDoBrilho = "Brilho";

    /// <summary>Cores de luz que se repetem.</summary>
    public static readonly Color Fogo = new Color(1f, 0.62f, 0.3f);
    public static readonly Color Vela = new Color(1f, 0.78f, 0.5f);
    public static readonly Color Heroi = new Color(1f, 0.93f, 0.82f);
    public static readonly Color Magica = new Color(0.45f, 0.65f, 1f);

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
    public static FonteDeLuz Luz(Transform pai, Vector2 local, Color cor, float raio, float intensidade, float tremor = 0f)
    {
        GameObject obj = new GameObject("Luz");
        obj.transform.SetParent(pai, false);
        obj.transform.localPosition = local;

        FonteDeLuz luz = obj.AddComponent<FonteDeLuz>();
        luz.cor = cor;
        luz.raio = raio;
        luz.intensidade = intensidade;

        if (tremor > 0f)
            obj.AddComponent<LuzTremula>().Comecar(luz, tremor);

        return luz;
    }

    /// <summary>A luz de fundo do andar (o resto e escuro).</summary>
    public static void Ambiente(float intensidade, Color cor)
    {
        Escuridao.Criar();
        Escuridao.MudarAmbiente(intensidade, cor);
    }
}
