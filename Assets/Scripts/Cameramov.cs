using UnityEngine;

public class Cameramov : MonoBehaviour
{
    [Tooltip("O personagem que a câmera vai seguir")]
    public Transform alvo;

    [Tooltip("Tempo (em segundos) que a câmera leva pra alcançar o alvo. Menor = mais rápida/seca; maior = mais suave/lenta")]
    public float tempoSuavizacao = 0.15f;

    [Tooltip("Deslocamento da câmera em relação ao alvo (ex: um pouco acima da cabeça)")]
    public Vector2 deslocamento = new Vector2(0f, 1f);

    // Guarda a velocidade atual do movimento da câmera. O SmoothDamp precisa
    // dessa variável pra "lembrar" como estava se movendo entre um quadro e outro
    // — não mexa nela na mão, ela é só uso interno do próprio SmoothDamp
    private Vector3 velocidadeAtual = Vector3.zero;

    void LateUpdate()
    {
        Vector3 destino = new Vector3(
            alvo.position.x + deslocamento.x,
            alvo.position.y + deslocamento.y,
            transform.position.z
        );

        // SmoothDamp NUNCA ultrapassa o alvo, diferente do Lerp com fator variável
        // (Lerp(a, b, suavidade * Time.deltaTime) pode "passar" do destino em quadros
        // mais lentos e voltar, causando o tremor/vibração que você estava vendo)
        transform.position = Vector3.SmoothDamp(transform.position, destino, ref velocidadeAtual, tempoSuavizacao);
    }
}