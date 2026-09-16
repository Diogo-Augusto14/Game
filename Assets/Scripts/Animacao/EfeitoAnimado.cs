using UnityEngine;

/// <summary>
/// Faisca / poeira / brilho: toca um clipe uma vez e se desliga sozinho.
/// Feito pra viver dentro de uma pool (o objeto e reaproveitado, nunca destruido),
/// que e o que evita lixo de memoria no meio do combate.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class EfeitoAnimado : MonoBehaviour
{
    [Tooltip("Clipe tocado ao ligar o objeto")]
    [SerializeField] private string clipe = NomesDeAnimacao.FxImpacto1;

    [Tooltip("Segundos de vida se o clipe nao existir na biblioteca")]
    [SerializeField, Min(0.05f)] private float duracaoDeReserva = 0.25f;

    private AnimadorDeSprites animador;
    private float desligarEm;

    public string Clipe
    {
        get => clipe;
        set => clipe = value;
    }

    private void Awake()
    {
        animador = GetComponent<AnimadorDeSprites>();

        if (animador == null)
            animador = gameObject.AddComponent<AnimadorDeSprites>();
    }

    private void OnEnable()
    {
        // Reiniciar = true: a pool devolve o mesmo objeto, e o clipe precisa voltar
        // pro primeiro quadro em vez de continuar de onde parou.
        animador.Tocar(clipe, true);

        float duracao = animador.DuracaoDe(clipe);
        desligarEm = Time.time + (duracao > 0f ? duracao : duracaoDeReserva);
    }

    private void Update()
    {
        if (Time.time >= desligarEm)
            gameObject.SetActive(false);
    }
}
