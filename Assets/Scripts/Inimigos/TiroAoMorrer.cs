using UnityEngine;

/// <summary>
/// Ao morrer, solta um disparo da arma (a Gosma estoura num anel de gotas): um ultimo perigo pra quem
/// mata de perto. So o disparo; rajada aqui nao conta.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Vida))]
public class TiroAoMorrer : MonoBehaviour
{
    [SerializeField] private DadosDaArma arma;

    [Tooltip("Altura de onde os tiros saem, em relacao ao centro do corpo")]
    [SerializeField] private float altura = -0.2f;

    private Vida vida;

    private void Awake()
    {
        vida = GetComponent<Vida>();
    }

    private void OnEnable() => vida.AoMorrer += Morreu;

    private void OnDisable() => vida.AoMorrer -= Morreu;

    private void Morreu()
    {
        if (arma == null)
            return;

        Vector2 origem = (Vector2)transform.position + Vector2.up * altura;
        arma.Disparar(origem, Random.insideUnitCircle.normalized, gameObject, vida.Lado);

        if (arma.som != null && TryGetComponent(out AudioSource audioSource))
            audioSource.PlayOneShot(arma.som, arma.volume);
    }
}
