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
    public float escalaBase = 1f;
    public float escalaAgachado = 0.5f;
    public float distanciaDash = 2f;
    public float velocidadeDash = 30f;
    private bool estaComDash = false;
    private Animator animator;
    public Transform checadorDeChao;
    public float raioChecagem = 0.2f;
    public LayerMask camadaChao;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {

        
        estaNoChao = Physics2D.OverlapCircle(checadorDeChao.position, raioChecagem, camadaChao);

        animator.SetBool("IsChao", estaNoChao);
        float lado = Input.GetAxisRaw("Horizontal");

        if (!estaComDash)
        {
            rb.linearVelocity = new Vector2(velocidade * lado, rb.linearVelocity.y);
        }

        if (Input.GetButtonDown("Jump") && estaNoChao)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, forcaPulo);
            estaNoChao = false;
        }

        // Corrigido: corta a altura do pulo quando solta o botão NO AR (antes pedia estaNoChao == true, que nunca acontecia nesse momento)
        if (Input.GetButtonUp("Jump") && !estaNoChao)
        {
            animator.SetBool("IsJump", true);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            StartCoroutine(Dash());
        }

        bool isMoving = lado != 0;
        animator.SetBool("isRunning", isMoving); // Removido if/else redundante

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
        animator.SetBool("Dash", estaComDash);
        float velocidadeYAntesDoDash = rb.linearVelocity.y; // Preserva o Y para não cancelar a gravidade/queda durante o dash
        rb.linearVelocity = new Vector2(velocidadeDash * direcao, velocidadeYAntesDoDash);
        yield return new WaitForSeconds(0.2f);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        estaComDash = false;
        animator.SetBool("Dash", estaComDash);
    }


    void FixedUpdate()
    {
        float vy = rb.linearVelocity.y;
        float threshold = 1f; // ajuste conforme necessário

        animator.SetBool("jumpPico", false);

        if (vy > threshold)
        {
            animator.SetBool("IsJump", true);
            animator.SetBool("IsFall", false);
            animator.SetBool("jumpPico", false);
        }
        else if (vy < -threshold * 3 && !estaNoChao)
        {
            animator.SetBool("IsJump", false);
            animator.SetBool("IsFall", true);
            animator.SetBool("jumpPico", false);
        }
        else if (vy < -threshold && !estaNoChao)
        {
            animator.SetBool("IsJump", false);
            animator.SetBool("IsFall", false);
            animator.SetBool("jumpPico", true);
        }
        else if (estaNoChao)
        {
            animator.SetBool("IsJump", false);
            animator.SetBool("IsFall", false);
            animator.SetBool("jumpPico", false);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (checadorDeChao == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(checadorDeChao.position, raioChecagem);
    }
}