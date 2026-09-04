using UnityEngine;

public class Inimigo : MonoBehaviour
{
    public float vidaAtual = 20f;
    public float velocidade = 2f;
    public GameObject jogador;
    public float dano = 2f;

    void Update()
    {   
        if (jogador != null)
        {
            IApeseguicao(jogador);
        }
    }


    public void IApeseguicao(GameObject jogador)
{
    
    transform.position = new Vector3(
        Mathf.MoveTowards(transform.position.x, jogador.transform.position.x, velocidade * Time.deltaTime), 
        transform.position.y, 
        transform.position.z
    );

    float direcao = Mathf.Sign(jogador.transform.position.x - transform.position.x);

    if (direcao != 0)
    {
        transform.localScale = new Vector3(direcao, 1, 1);
    }
}
    public void TomarDano(float quantidadeDano)
    {
        vidaAtual -= quantidadeDano;

        if (vidaAtual <= 0)
        {
            Destroy(gameObject);
        }
    }
}