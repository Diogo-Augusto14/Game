using UnityEngine;

/// <summary>
/// O orbe que gira em volta do jogador (o item Orbe Guardiao): desmancha os tiros inimigos que
/// encosta e machuca o inimigo que encosta nele.
/// </summary>
public class OrbeGuardiao : MonoBehaviour
{
    private const float Raio = 1.3f;
    private const float Giro = 200f;

    private Transform dono;
    private int numero;
    private float proximoGolpe;

    public static OrbeGuardiao Criar(Transform dono, int numero)
    {
        GameObject obj = new GameObject("Orbe guardiao");
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = ArteDoAntigo.Icone(335, 48f);
        sr.sortingOrder = 15;
        OrbeGuardiao orbe = obj.AddComponent<OrbeGuardiao>();
        orbe.dono = dono;
        orbe.numero = numero;
        return orbe;
    }

    private void Update()
    {
        if (dono == null)
        {
            Destroy(gameObject);
            return;
        }

        float angulo = (Time.time * Giro + numero * 180f) * Mathf.Deg2Rad;
        transform.position = dono.position + new Vector3(Mathf.Cos(angulo), Mathf.Sin(angulo) + 0.3f, 0f) * Raio;

        foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, 0.35f))
        {
            if (c == null)
                continue;

            if (c.TryGetComponent(out Projetil tiro) && tiro.Lado == Lado.Inimigos)
            {
                tiro.Sumir();
                continue;
            }

            Vida vida = c.GetComponentInParent<Vida>();

            if (vida != null && vida.Lado == Lado.Inimigos && Time.time >= proximoGolpe)
            {
                proximoGolpe = Time.time + 0.4f;
                vida.ReceberDano(new Dano(4f, (Vector2)(vida.transform.position - transform.position), 4f, dono.gameObject));
            }
        }
    }
}
