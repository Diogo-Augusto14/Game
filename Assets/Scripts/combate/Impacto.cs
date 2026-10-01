using System.Collections;
using UnityEngine;

/// <summary>
/// O "peso" dos golpes: a tela treme, o jogo congela uns centesimos de segundo no acerto
/// (hitstop) e o numero do dano pula do inimigo. Sao coisas pequenas que fazem o golpe parecer
/// que encostou de verdade. Tremor e numeros podem ser desligados nas opcoes
/// (<see cref="Opcoes.TremorLigado"/>, <see cref="Opcoes.NumerosDeDano"/>).
///
///   Impacto.Tremer(0.15f, 0.2f);   // forca (unidades) e duracao (segundos)
///   Impacto.Congelar(0.05f);       // segundos reais com o jogo quase parado
///   Impacto.Numero(ponto, 12f);    // numero de dano subindo
/// </summary>
[DefaultExecutionOrder(1000)]   // depois do Andar mexer na camera
public class Impacto : MonoBehaviour
{
    /// <summary>Quanto o tempo anda durante o congelamento (0 parava a fisica e os timers de vez).</summary>
    private const float TempoCongelado = 0.04f;

    /// <summary>Teto do congelamento acumulado: varias mortes juntas nao podem travar o jogo.</summary>
    private const float CongelamentoMaximo = 0.12f;

    private static Impacto instancia;

    private float forca;
    private float duracao;
    private float restante;
    private Vector3 desvio;
    private Vector3 posicaoComDesvio;
    private Transform cameraAtual;
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

    /// <summary>Treme a camera. Um tremor mais forte por cima de um fraco ganha; dois fracos nao somam.</summary>
    public static void Tremer(float forcaDoTremor, float segundos)
    {
        if (!Opcoes.TremorLigado || forcaDoTremor <= 0f || segundos <= 0f)
            return;

        Impacto i = Instancia;

        if (i.restante > 0f && i.forca * (i.restante / i.duracao) >= forcaDoTremor)
            return;

        i.forca = forcaDoTremor;
        i.duracao = segundos;
        i.restante = segundos;
    }

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

    private void LateUpdate()
    {
        Camera cam = Camera.main;

        if (cam == null)
            return;

        if (cameraAtual != cam.transform)
        {
            cameraAtual = cam.transform;
            desvio = Vector3.zero;
            posicaoComDesvio = cameraAtual.position;
        }

        // Se alguem moveu a camera neste quadro (troca de sala, sala grande seguindo o jogador),
        // a posicao dele e a base; senao, tira o desvio do quadro anterior.
        Vector3 baseDaCamera = cameraAtual.position == posicaoComDesvio ? cameraAtual.position - desvio : cameraAtual.position;

        if (restante > 0f && Time.timeScale > 0f)
        {
            restante -= Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(restante / duracao);
            desvio = (Vector3)(Random.insideUnitCircle * forca * t * t);
        }
        else
        {
            desvio = Vector3.zero;
        }

        cameraAtual.position = baseDaCamera + desvio;
        posicaoComDesvio = cameraAtual.position;
    }
}
