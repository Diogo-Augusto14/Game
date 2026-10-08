using UnityEngine;

/// <summary>Faz uma luz tremular como fogo (a intensidade e o raio variam devagar, cada uma num ritmo).</summary>
public class LuzTremula : MonoBehaviour
{
    private FonteDeLuz luz;
    private float intensidade;
    private float raio;
    private float tremor;
    private float fase;

    public void Comecar(FonteDeLuz luz, float tremor)
    {
        this.luz = luz;
        this.tremor = tremor;
        intensidade = luz.intensidade;
        raio = luz.raio;
        fase = Random.value * 100f;
    }

    /// <summary>Muda a intensidade de base (a estatua de fogo acende ao disparar).</summary>
    public float Intensidade
    {
        get => intensidade;
        set => intensidade = value;
    }

    private void Update()
    {
        if (luz == null)
            return;

        float t = Time.time * 7f + fase;
        float ruido = Mathf.PerlinNoise(t, fase) - 0.5f;
        luz.intensidade = Mathf.Max(0f, intensidade * (1f + ruido * tremor * 2f));
        luz.raio = raio * (1f + ruido * tremor * 0.5f);
    }
}
