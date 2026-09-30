using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Escurece a tela, faz a troca (recarregar a cena, voltar ao menu) e clareia de novo, em
/// vez de cortar seco. Vive fora da cena (DontDestroyOnLoad) pra atravessar o recarregamento.
///
///   TransicaoDeTela.Trocar(TelaDeFimDeJogo.RecarregarCena);
///
/// Em tempo real: funciona com o jogo congelado. Enquanto escurece, <see cref="Ocupada"/>
/// fica ligada e os menus param de ler teclas (ninguem aperta duas vezes).
/// </summary>
public class TransicaoDeTela : MonoBehaviour
{
    private const float TempoDeEscurecer = 0.35f;
    private const float TempoDeClarear = 0.45f;

    private static TransicaoDeTela atual;

    private CanvasGroup grupo;

    /// <summary>Escurecendo, trocando ou clareando.</summary>
    public static bool Ocupada => atual != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar()
    {
        atual = null;
    }

    /// <summary>Escurece, roda <paramref name="troca"/> e clareia. Se ja estiver trocando, ignora.</summary>
    public static void Trocar(Action troca)
    {
        if (atual != null)
            return;

        Criar(0f).StartCoroutine(atual.Rodar(troca));
    }

    /// <summary>So clareia, a partir do preto (a abertura do jogo).</summary>
    public static void Revelar(float tempo = 0.6f)
    {
        if (atual != null)
            return;

        Criar(1f).StartCoroutine(atual.Clarear(tempo));
    }

    private static TransicaoDeTela Criar(float alfa)
    {
        GameObject obj = new GameObject("Transicao de tela");
        DontDestroyOnLoad(obj);

        Canvas canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // por cima de tudo, ate das opcoes

        atual = obj.AddComponent<TransicaoDeTela>();
        atual.grupo = obj.AddComponent<CanvasGroup>();
        atual.grupo.alpha = alfa;
        atual.grupo.blocksRaycasts = false;
        atual.grupo.interactable = false;

        GameObject preto = new GameObject("Preto", typeof(RectTransform));
        preto.transform.SetParent(obj.transform, false);
        RectTransform rt = (RectTransform)preto.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        Image imagem = preto.AddComponent<Image>();
        imagem.color = Color.black;
        imagem.raycastTarget = false;
        return atual;
    }

    private IEnumerator Rodar(Action troca)
    {
        for (float t = 0f; t < TempoDeEscurecer; t += Time.unscaledDeltaTime)
        {
            grupo.alpha = t / TempoDeEscurecer;
            yield return null;
        }

        grupo.alpha = 1f;
        troca?.Invoke();

        // A cena nova carrega no fim do quadro: espera ela montar antes de clarear.
        yield return null;
        yield return null;
        yield return Clarear(TempoDeClarear);
    }

    private IEnumerator Clarear(float tempo)
    {
        for (float t = 0f; t < tempo; t += Time.unscaledDeltaTime)
        {
            grupo.alpha = 1f - t / tempo;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (atual == this)
            atual = null;
    }
}
