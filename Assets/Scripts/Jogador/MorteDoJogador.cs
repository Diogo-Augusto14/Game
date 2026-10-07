using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quando a vida do jogador acaba, a partida acaba: os controles desligam, o corpo para e cai (a
/// animacao de morte, na <see cref="AnimacaoDoJogador"/>), o tempo fica lento um instante, a tela
/// escurece e a partida recomeca do zero (a cena carrega de novo).
///
/// A tela de fim de jogo de verdade, com menu, vem na etapa da interface.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Vida))]
public class MorteDoJogador : MonoBehaviour
{
    [Tooltip("Velocidade do tempo logo depois de morrer (1 = normal)")]
    [SerializeField, Range(0.05f, 1f)] private float camaraLenta = 0.3f;

    [Tooltip("Segundos (de verdade) em camera lenta")]
    [SerializeField, Min(0f)] private float duracaoDaCamaraLenta = 0.8f;

    [Tooltip("Segundos que a tela leva pra escurecer")]
    [SerializeField, Min(0.01f)] private float escurecer = 1.2f;

    [Tooltip("Segundos com a tela escura antes de recomecar")]
    [SerializeField, Min(0f)] private float esperaNoEscuro = 0.8f;

    private Vida vida;
    private float escuro;
    private bool mexeuNoTempo;
    private GUIStyle estiloDoTexto;

    private void Awake()
    {
        vida = GetComponent<Vida>();
    }

    private void OnEnable()
    {
        vida.AoMorrer += Morreu;
    }

    private void OnDisable()
    {
        vida.AoMorrer -= Morreu;
    }

    private void OnDestroy()
    {
        // Saiu no meio da camera lenta (recarregou a cena, parou o Play): o tempo volta ao normal.
        if (mexeuNoTempo)
            Time.timeScale = 1f;
    }

    private void Morreu()
    {
        // Nada de andar, atirar ou esquivar; o corpo para e deixa de bater nas coisas.
        DesligarSeTiver<ControlesDoJogador>();
        DesligarSeTiver<MovimentoDoJogador>();
        DesligarSeTiver<ArmaDoJogador>();

        if (TryGetComponent(out Rigidbody2D corpo))
        {
            corpo.linearVelocity = Vector2.zero;
            corpo.simulated = false;
        }

        StartCoroutine(AcabarAPartida());
    }

    private void DesligarSeTiver<T>() where T : Behaviour
    {
        if (TryGetComponent(out T peca))
            peca.enabled = false;
    }

    private IEnumerator AcabarAPartida()
    {
        mexeuNoTempo = true;
        Time.timeScale = camaraLenta;
        yield return new WaitForSecondsRealtime(duracaoDaCamaraLenta);
        Time.timeScale = 1f;
        mexeuNoTempo = false;

        for (float t = 0f; t < escurecer; t += Time.unscaledDeltaTime)
        {
            escuro = t / escurecer;
            yield return null;
        }

        escuro = 1f;
        yield return new WaitForSecondsRealtime(esperaNoEscuro);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnGUI()
    {
        if (escuro <= 0f)
            return;

        GUI.color = new Color(0f, 0f, 0f, escuro);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

        if (estiloDoTexto == null)
        {
            estiloDoTexto = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(24, Screen.height / 14),
            };
        }

        GUI.color = new Color(1f, 1f, 1f, escuro);
        GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height), "Fim da partida", estiloDoTexto);
    }
}
