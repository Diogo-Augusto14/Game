using UnityEngine;

/// <summary>
/// Ao morrer, estoura em pedacos menores que continuam a briga (a Bolha do jogo antigo vira
/// bolinhas). Os pedacos contam pro andar (<see cref="GeradorDoAndar.Registrar"/>) e ja nascem
/// acordados.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Vida))]
public class DivideAoMorrer : MonoBehaviour
{
    [Tooltip("O prefab do pedaco (que nao divide de novo)")]
    [SerializeField] private GameObject pedaco;

    [SerializeField, Min(1)] private int quantos = 2;

    [Tooltip("Velocidade com que os pedacos saem voando pros lados")]
    [SerializeField, Min(0f)] private float espalhar = 5f;

    private Vida vida;

    private void Awake()
    {
        vida = GetComponent<Vida>();
    }

    private void OnEnable() => vida.AoMorrer += Morreu;

    private void OnDisable() => vida.AoMorrer -= Morreu;

    private void Morreu()
    {
        if (pedaco == null)
            return;

        float giro = Random.Range(0f, 360f);

        for (int i = 0; i < quantos; i++)
        {
            Vector2 rumo = Quaternion.Euler(0f, 0f, giro + 360f * i / quantos) * Vector2.right;
            Vector2 onde = (Vector2)transform.position + rumo * 0.3f;
            GameObject novo = Instantiate(pedaco, onde, Quaternion.identity, GeradorDoAndar.Raiz);

            if (novo.TryGetComponent(out Rigidbody2D corpo))
                corpo.linearVelocity = rumo * espalhar;

            if (novo.TryGetComponent(out InimigoAtirador inimigo))
                inimigo.Acordar();

            if (novo.TryGetComponent(out Vida dele))
                GeradorDoAndar.Registrar(dele);
        }
    }
}
