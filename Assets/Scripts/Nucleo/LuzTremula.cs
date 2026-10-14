using UnityEngine;

/// <summary>
/// Faz uma luz tremular como fogo: a intensidade e o raio variam (dois ruidos, um lento e um rapido), o
/// centro da luz balanca um pouquinho (as sombras mexem) e, nas chamas mais vivas (tochas), de vez em
/// quando a chama engasga e a luz cai por um instante.
/// </summary>
public class LuzTremula : MonoBehaviour
{
    private FonteDeLuz luz;
    private float intensidade;
    private float raio;
    private float tremor;
    private float fase;
    private Vector3 lugar;
    private float engasgoAte = -1f;
    private float proximoEngasgo;

    public void Comecar(FonteDeLuz luz, float tremor)
    {
        this.luz = luz;
        this.tremor = tremor;
        intensidade = luz.intensidade;
        raio = luz.raio;
        fase = Random.value * 100f;
        lugar = luz.transform.localPosition;
        proximoEngasgo = Time.time + Random.Range(2f, 6f);
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
        float ruido = (Mathf.PerlinNoise(t, fase) - 0.5f) + (Mathf.PerlinNoise(t * 3.1f, fase + 17f) - 0.5f) * 0.5f;

        // A chama engasga de vez em quando (so as vivas: tochas, fogueiras).
        if (tremor >= 0.15f && Time.time >= proximoEngasgo)
        {
            engasgoAte = Time.time + Random.Range(0.06f, 0.16f);
            proximoEngasgo = Time.time + Random.Range(2.5f, 7f);
        }

        float engasgo = Time.time < engasgoAte ? 0.6f : 1f;
        luz.intensidade = Mathf.Max(0f, intensidade * (1f + ruido * tremor * 2f) * engasgo);
        luz.raio = raio * (1f + ruido * tremor * 0.5f);

        // O centro da luz balanca um pouco: as sombras mexem junto com o fogo.
        Vector2 balanco = new Vector2(Mathf.PerlinNoise(t * 0.8f, fase + 5f) - 0.5f, Mathf.PerlinNoise(fase + 9f, t * 0.8f) - 0.5f);
        luz.transform.localPosition = lugar + (Vector3)(balanco * tremor * 0.5f);
    }
}
