using UnityEngine;
public class Espada : MonoBehaviour
{
    public float dano = 5f;

    void Update()
    {


    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Inimigo"))
        {
            Inimigo inimigo = collision.GetComponent<Inimigo>();
            if (inimigo != null)
            {
                inimigo.TomarDano(dano);
            }
        }
    }
}
