using UnityEngine;

/// <summary>
/// Machuca quem encosta no corpo e e do outro lado (o jogador encostando na Gosma, o Esqueleto
/// trombando nele na investida), e empurra pra longe. O tempinho sem dano depois de um golpe (na
/// <see cref="Vida"/> de quem apanhou) segura pra nao tirar vida a cada quadro; a esquiva protege.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Vida))]
public class DanoAoEncostar : MonoBehaviour
{
    [SerializeField, Min(0f)] private float dano = 1f;

    [Tooltip("Velocidade do empurrao em quem encostou")]
    [SerializeField, Min(0f)] private float empurrao = 6f;

    private Vida vida;

    private void Awake()
    {
        vida = GetComponent<Vida>();
    }

    private void OnCollisionEnter2D(Collision2D contato) => Encostou(contato.collider);

    private void OnCollisionStay2D(Collision2D contato) => Encostou(contato.collider);

    private void Encostou(Collider2D outro)
    {
        if (vida.Morto)
            return;

        Vida dele = outro.GetComponentInParent<Vida>();

        // Mesa, barril e pedra (lado Neutro) nao apanham de quem so encosta.
        if (dele == null || dele.Lado == vida.Lado || dele.Lado == Lado.Neutro || dele.Protegido)
            return;

        Vector2 pralonge = (Vector2)dele.transform.position - (Vector2)transform.position;
        dele.ReceberDano(new Dano(dano, pralonge.sqrMagnitude > 0.0001f ? pralonge.normalized : Vector2.up, empurrao, gameObject));
    }
}
