using UnityEngine;

/// <summary>
/// Faz um <see cref="TiroDaSala"/> virar machado arremessado: gira, vai freando ate parar
/// longe e volta pra quem jogou, acelerando. Quem desviou na ida tem que desviar de novo na
/// volta. Some ao chegar de volta no dono (ou em parede, como todo tiro).
/// </summary>
[RequireComponent(typeof(TiroDaSala))]
public class MachadoBumerangue : MonoBehaviour
{
    private Rigidbody2D rb;
    private Transform dono;
    private Vector2 ida;
    private float velocidade;
    private float tempo;
    private float tempoDaIda;

    public static void Em(TiroDaSala tiro, Transform quemJogou, float segundosDeIda)
    {
        // O estilo do tiro pode ter movimento proprio (pedra freando): aqui quem manda e o machado.
        if (tiro.TryGetComponent(out ComportamentoDoTiro outro))
        {
            outro.enabled = false;
            Destroy(outro);
        }

        MachadoBumerangue m = tiro.gameObject.AddComponent<MachadoBumerangue>();
        m.dono = quemJogou;
        m.tempoDaIda = Mathf.Max(0.2f, segundosDeIda);
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        velocidade = rb.linearVelocity.magnitude;
        ida = velocidade > 0.001f ? rb.linearVelocity / velocidade : Vector2.right;
    }

    private void FixedUpdate()
    {
        tempo += Time.fixedDeltaTime;
        transform.Rotate(0f, 0f, 900f * Time.fixedDeltaTime);

        if (tempo < tempoDaIda)
        {
            // Ida: freia ate quase parar.
            float t = tempo / tempoDaIda;
            rb.linearVelocity = ida * velocidade * (1f - t * t);
            return;
        }

        if (dono == null)
        {
            Destroy(gameObject);
            return;
        }

        // Volta: acelera em direcao ao dono.
        Vector2 paraODono = (Vector2)dono.position - rb.position;

        if (paraODono.sqrMagnitude < 0.36f)
        {
            Destroy(gameObject);
            return;
        }

        float volta = Mathf.Min(velocidade * 1.2f, velocidade * 0.3f + (tempo - tempoDaIda) * velocidade * 1.5f);
        rb.linearVelocity = paraODono.normalized * volta;
    }
}
