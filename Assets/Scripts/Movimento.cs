using UnityEngine;

public class Movimento : MonoBehaviour
{
    public Rigidbody2D rb;
    public float velocidade = 5f;
    public float forcaPulo = 10f;
    private bool estaNoChao = false;

    private float direcao = 4f;
    private bool pequeno = false;
    public float escalaBase = 4f;      
    public float escalaAgachado = 2f;

    void Update()
    {
        float lado = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(velocidade * lado, rb.linearVelocity.y);
        if (Input.GetButtonDown("Jump") && estaNoChao)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, forcaPulo);
            estaNoChao = false;
        }
       if (lado > 0)
            direcao = 1f;
        else if (lado < 0)
            direcao = -1f;

        if (Input.GetKeyDown(KeyCode.S))
        {
            pequeno = true;
        }
        else if (Input.GetKeyUp(KeyCode.S))
        {
            pequeno = false;
        }
        float escalaY = pequeno ? escalaAgachado : escalaBase;
        transform.localScale = new Vector3(direcao * escalaBase, escalaY, escalaBase);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            estaNoChao = true;
        }
    }



}