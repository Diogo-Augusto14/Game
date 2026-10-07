using UnityEngine;

/// <summary>
/// Escudo na frente do corpo (o Cavaleiro do escudo, do jogo antigo): o golpe que vem de frente
/// bate e some, sem tirar vida. Pelas costas e pelos lados pega; e na hora do ataque (preparando
/// ou soltando o golpe) o escudo abre e pega de qualquer lado.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Vida))]
public class EscudoFrontal : MonoBehaviour, IBloqueioDeDano
{
    [Tooltip("Abertura do escudo, em graus (180 = a metade da frente inteira)")]
    [SerializeField, Range(10f, 270f)] private float abertura = 120f;

    [SerializeField] private AudioClip somDoBloqueio;
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;

    private IAnimavel corpo;
    private InimigoAtirador inimigo;
    private AudioSource audioSource;
    private float avisouEm = -1f;

    private void Awake()
    {
        corpo = GetComponent<IAnimavel>();
        inimigo = GetComponent<InimigoAtirador>();
        audioSource = GetComponent<AudioSource>();
    }

    public bool Bloqueia(Dano dano)
    {
        if (corpo == null || dano.direcao.sqrMagnitude < 0.0001f)
            return false;

        if (inimigo != null && inimigo.Ocupado)
            return false;

        // O golpe vem andando em "direcao": de frente e quando vem contra pra onde ele olha.
        Vector2 deOnde = -dano.direcao.normalized;

        if (Vector2.Angle(deOnde, corpo.OlhandoPara) > abertura * 0.5f)
            return false;

        if (audioSource != null && somDoBloqueio != null)
            audioSource.PlayOneShot(somDoBloqueio, volume);

        // Um aviso so de vez em quando (rajada de besta nao enche a tela de texto).
        if (Time.time - avisouEm > 0.6f)
        {
            avisouEm = Time.time;
            TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.1f, "bloqueou", new Color(0.75f, 0.8f, 0.9f), 0.6f);
        }

        return true;
    }
}
