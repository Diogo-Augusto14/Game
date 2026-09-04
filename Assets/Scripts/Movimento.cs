using System;
using UnityEngine;

public class Movimento : MonoBehaviour
{
    public Rigidbody2D rb;
    public float velocidade = 5f;
    public float forcaPulo = 10f;
    private bool estaNoChao = false;

    private float direcao = 1f;
    private bool pequeno = false;
    public float escalaBase = 4f;
    public float escalaAgachado = 2f;
    public float distanciaDash = 2f;
    public float velocidadeDash = 30f;
    private bool estaComDash = false;
    private Animator animator;
    public Transform checadorDeChao; 
    public float raioChecagem = 0.2f;
    public LayerMask camadaChao;     


    void Start()
    { animator = GetComponent<Animator>(); }



    void Update()
    {
        estaNoChao = Physics2D.OverlapCircle(checadorDeChao.position, raioChecagem, camadaChao);
        if(estaNoChao != true)
        {
            if(rb.linearVelocity.y > 0)
            {animator.SetBool("IsJump", true);
                animator.SetBool("IsFall", false);
            }
         if (rb.linearVelocity.y < 0){
                animator.SetBool("IsJump", false);
                animator.SetBool("IsFall", true);
            }
        }
        else
        {
            animator.SetBool("IsJump", false);
            animator.SetBool("IsFall", false);
        }

         

        float lado = Input.GetAxisRaw("Horizontal");
        if (estaComDash == false)
        { rb.linearVelocity = new Vector2(velocidade * lado, rb.linearVelocity.y); }
        if (Input.GetButtonDown("Jump") && estaNoChao)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, forcaPulo);
            estaNoChao = false;
        }


        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {

            StartCoroutine(Dash());

        }
        float horizontal = Input.GetAxisRaw("Horizontal");
        bool isMoving = horizontal != 0;
        if (estaNoChao != false)
        {
            animator.SetBool("isRunning", isMoving);
        }
        else
        { animator.SetBool("isRunning", false); }


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
    
    

    System.Collections.IEnumerator Dash()
    {
        estaComDash = true;
        rb.linearVelocity = new Vector2(velocidadeDash * direcao, 0f);
        yield return new WaitForSeconds(0.2f);
        rb.linearVelocity = Vector2.zero;
        estaComDash = false;
    }





}