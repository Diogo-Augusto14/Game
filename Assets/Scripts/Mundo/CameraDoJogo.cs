using UnityEngine;

/// <summary>
/// A camera segue o jogador com um pouco de atraso e olha pra frente, na direcao da mira (com o
/// mouse, ate um pedaco do caminho ate o cursor), como no Gungeon: da pra ver mais do lado pra onde
/// se atira. Tambem treme quando alguem pede (<see cref="Tremer"/>: tiro, explosao, pancada).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraDoJogo : MonoBehaviour
{
    [SerializeField] private Transform alvo;

    [Tooltip("Segundos que a camera leva pra alcancar o alvo (mais = mais macia)")]
    [SerializeField, Min(0f)] private float suavidade = 0.12f;

    [Tooltip("Fracao do caminho ate o cursor que a camera anda pra frente")]
    [SerializeField, Range(0f, 0.5f)] private float olharAFrente = 0.22f;

    [Tooltip("Distancia maxima que a camera anda pra frente, em unidades")]
    [SerializeField, Min(0f)] private float maximoAFrente = 2.2f;

    [Tooltip("Metade da altura da tela, em unidades (quanto do mundo aparece)")]
    [SerializeField, Min(1f)] private float tamanho = 5.5f;

    private static CameraDoJogo atual;

    private Camera cam;
    private ControlesDoJogador controles;
    private Vector3 velocidade;
    private Vector3 semTremor;
    private float forcaDoTremor;
    private float duracaoDoTremor;
    private float tremorAte;

    private void Awake()
    {
        atual = this;
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = tamanho;
        semTremor = transform.position;

        if (alvo != null)
            controles = alvo.GetComponent<ControlesDoJogador>();
    }

    private void OnDestroy()
    {
        if (atual == this)
            atual = null;
    }

    /// <summary>Treme a camera. Um tremor fraco nao apaga um forte que ainda esta acontecendo.</summary>
    public static void Tremer(float forca, float segundos)
    {
        if (atual == null || forca <= 0f || segundos <= 0f)
            return;

        float restante = Mathf.Max(0f, atual.tremorAte - Time.time);
        float agora = atual.duracaoDoTremor > 0f ? atual.forcaDoTremor * restante / atual.duracaoDoTremor : 0f;

        if (forca < agora)
            return;

        atual.forcaDoTremor = forca;
        atual.duracaoDoTremor = segundos;
        atual.tremorAte = Time.time + segundos;
    }

    /// <summary>Vai direto pro alvo, sem deslizar ate la (o jogador foi levado pra outro lugar).</summary>
    public static void Pular()
    {
        if (atual == null || atual.alvo == null)
            return;

        atual.semTremor = new Vector3(atual.alvo.position.x, atual.alvo.position.y, atual.transform.position.z);
        atual.velocidade = Vector3.zero;
        atual.transform.position = atual.semTremor;
    }

    private void LateUpdate()
    {
        if (alvo == null)
            return;

        Vector2 aFrente = Vector2.zero;

        if (controles != null)
        {
            Vector2 ateOMouse = controles.PontoDoMouse - (Vector2)alvo.position;
            aFrente = controles.MiraPeloMouse ? ateOMouse * olharAFrente : controles.Mira * maximoAFrente * 0.5f;
            aFrente = Vector2.ClampMagnitude(aFrente, maximoAFrente);
        }

        Vector3 destino = new Vector3(alvo.position.x + aFrente.x, alvo.position.y + aFrente.y, transform.position.z);
        semTremor = Vector3.SmoothDamp(semTremor, destino, ref velocidade, suavidade);

        Vector3 tremor = Vector3.zero;

        if (Time.time < tremorAte && duracaoDoTremor > 0f)
            tremor = (Vector3)(Random.insideUnitCircle * forcaDoTremor * (tremorAte - Time.time) / duracaoDoTremor);

        transform.position = semTremor + tremor;
    }
}
