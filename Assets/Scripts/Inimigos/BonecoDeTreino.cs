using UnityEngine;

/// <summary>
/// O boneco de treino, pra testar as armas: apanha mas nao morre (a <see cref="Vida"/> dele e
/// imortal) e balanca no pe pro lado do golpe, como um boneco de palha numa estaca.
///
/// O desenho fica num filho com o pivo no pe, pra girar em volta da base.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Vida))]
public class BonecoDeTreino : MonoBehaviour
{
    [Tooltip("O desenho que balanca (um filho, com o pivo no pe)")]
    [SerializeField] private Transform corpo;

    [Tooltip("Empurrao de cada golpe no balanco, em graus por segundo")]
    [SerializeField, Min(0f)] private float forcaDoGolpe = 180f;

    [Tooltip("O balanco nunca passa disto, em graus")]
    [SerializeField, Min(0f)] private float balancoMaximo = 18f;

    [Tooltip("Rigidez da mola que endireita o boneco: mais = balanca mais rapido")]
    [SerializeField, Min(0f)] private float mola = 260f;

    [Tooltip("Quanto o balanco perde a cada segundo: mais = para antes")]
    [SerializeField, Min(0f)] private float amortecimento = 9f;

    private Vida vida;
    private float angulo;
    private float velocidadeAngular;

    private void Awake()
    {
        vida = GetComponent<Vida>();
    }

    private void OnEnable()
    {
        vida.AoTomarDano += Tomou;
    }

    private void OnDisable()
    {
        vida.AoTomarDano -= Tomou;
    }

    private void Tomou(Dano dano)
    {
        // Golpe vindo da esquerda (indo pra direita) tomba o boneco pra direita: angulo negativo.
        float lado = dano.direcao.x >= 0f ? -1f : 1f;
        velocidadeAngular += lado * forcaDoGolpe;
    }

    private void Update()
    {
        if (corpo == null)
            return;

        // Mola amortecida: o boneco volta pro lugar balancando cada vez menos.
        float aceleracao = -mola * angulo - amortecimento * velocidadeAngular;
        velocidadeAngular += aceleracao * Time.deltaTime;
        angulo = Mathf.Clamp(angulo + velocidadeAngular * Time.deltaTime, -balancoMaximo, balancoMaximo);

        corpo.localRotation = Quaternion.Euler(0f, 0f, angulo);
    }
}
