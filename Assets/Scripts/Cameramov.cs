using UnityEngine;
public class Cameramov : MonoBehaviour
{
    public Transform alvo;
    public float suavidade = 8f;
    public Vector2 deslocamento = new Vector2(0f, 1f);

    void LateUpdate()
    {
        Vector3 destino = new Vector3(alvo.position.x + deslocamento.x, alvo.position.y + deslocamento.y,
        transform.position.z);
        transform.position = Vector3.Lerp(transform.position, destino, suavidade * Time.deltaTime);

    }
    
}
