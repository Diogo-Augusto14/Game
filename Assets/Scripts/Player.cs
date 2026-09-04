using UnityEngine;
public class Player : MonoBehaviour
{
    public float vida = 100f;
    public float defesa = 3f;



    void Update()

    {


    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Inimigo"))
        {
            Inimigo inimigo = collision.GetComponent<Inimigo>();
            if (inimigo != null)
            {
                vida = vida - inimigo.dano;
            }
        }
    }
}
