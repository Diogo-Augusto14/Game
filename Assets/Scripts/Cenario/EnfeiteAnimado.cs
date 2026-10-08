using UnityEngine;

/// <summary>Um enfeite que so repete os quadros (a tocha na parede), cada um numa fase diferente.</summary>
public class EnfeiteAnimado : MonoBehaviour
{
    private SpriteRenderer desenho;
    private Sprite[] quadros;
    private float quadrosPorSegundo;
    private float fase;

    public void Comecar(Sprite[] quadros, float quadrosPorSegundo)
    {
        desenho = GetComponent<SpriteRenderer>();
        this.quadros = quadros;
        this.quadrosPorSegundo = quadrosPorSegundo;
        fase = Random.value * 10f;
    }

    private void Update()
    {
        if (quadros != null && quadros.Length > 0)
            desenho.sprite = quadros[Mathf.FloorToInt((Time.time + fase) * quadrosPorSegundo) % quadros.Length];
    }
}
