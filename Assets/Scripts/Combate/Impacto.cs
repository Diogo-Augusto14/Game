using System.Collections;
using UnityEngine;

/// <summary>
/// O "peso" dos golpes (veio do jogo antigo): o jogo congela uns centesimos de segundo no acerto
/// (hitstop) e o numero do dano pula do inimigo. Sao coisas pequenas que fazem o golpe parecer
/// que encostou de verdade. Os numeros desligam nas configuracoes (<see cref="Opcoes.NumerosDeDano"/>);
/// o tremor da tela e o da <see cref="CameraDoJogo"/>.
///
///   Impacto.Congelar(0.05f);       // segundos reais com o jogo quase parado
///   Impacto.Numero(ponto, 12f);    // numero de dano subindo
/// </summary>
public class Impacto : MonoBehaviour
{
    /// <summary>Quanto o tempo anda durante o congelamento (0 parava a fisica e os timers de vez).</summary>
    private const float TempoCongelado = 0.04f;

    /// <summary>Teto do congelamento acumulado: varias mortes juntas nao podem travar o jogo.</summary>
    private const float CongelamentoMaximo = 0.12f;

    private static Impacto instancia;

    private float congelarAte;
    private bool congelando;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar() => instancia = null;

    private static Impacto Instancia
    {
        get
        {
            if (instancia == null)
            {
                GameObject obj = new GameObject("Impacto");
                DontDestroyOnLoad(obj);
                instancia = obj.AddComponent<Impacto>();
            }

            return instancia;
        }
    }

    /// <summary>Treme a camera (atalho pra <see cref="CameraDoJogo.Tremer"/>).</summary>
    public static void Tremer(float forca, float segundos) => CameraDoJogo.Tremer(forca, segundos);

    /// <summary>Hitstop: o jogo quase para por <paramref name="segundos"/> de tempo real.</summary>
    public static void Congelar(float segundos)
    {
        // Pausa, menu e camera lenta da morte mexem no timeScale: nao briga com eles.
        if (segundos <= 0f || (Time.timeScale < 0.999f && !Instancia.congelando))
            return;

        Impacto i = Instancia;
        i.congelarAte = Mathf.Min(Mathf.Max(i.congelarAte, Time.unscaledTime + segundos), Time.unscaledTime + CongelamentoMaximo);

        if (!i.congelando)
            i.StartCoroutine(i.Congelamento());
    }

    /// <summary>Numero de dano saindo do ponto, com um pulinho pro lado. Forte = maior e amarelo.</summary>
    public static void Numero(Vector2 ponto, float dano, bool forte = false)
    {
        if (!Opcoes.NumerosDeDano || dano <= 0f)
            return;

        string texto = dano >= 10f ? Mathf.RoundToInt(dano).ToString() : dano.ToString("0.#");
        Color cor = forte ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 1f, 1f, 0.95f);
        TextoFlutuante numero = TextoFlutuante.Mostrar((Vector3)ponto + new Vector3(Random.Range(-0.2f, 0.2f), 0.35f, 0f), texto, cor);
        numero.Pular(forte ? 1.25f : 0.9f);
    }

    private IEnumerator Congelamento()
    {
        congelando = true;
        Time.timeScale = TempoCongelado;

        while (Time.unscaledTime < congelarAte)
        {
            // Alguem pausou no meio (menu, morte): solta sem mexer no timeScale dele.
            if (!Mathf.Approximately(Time.timeScale, TempoCongelado))
            {
                congelando = false;
                yield break;
            }

            yield return null;
        }

        if (Mathf.Approximately(Time.timeScale, TempoCongelado))
            Time.timeScale = 1f;

        congelando = false;
    }
}
