using UnityEngine;

/// <summary>
/// Traduz o estado do <see cref="Inimigo"/> em clipe, com a mesma regra de prioridade do
/// jogador: morrer ganha de apanhar, que ganha de atacar, que ganha de andar.
///
/// O golpe merece um cuidado extra: a velocidade do clipe e esticada pra caber exatamente
/// na duracao do golpe. Assim a lamina aparece no desenho no mesmo instante em que a
/// hitbox liga — se o clipe fosse mais lento que o golpe, o jogador levaria dano de um
/// golpe que ainda nem apareceu na tela.
/// </summary>
[RequireComponent(typeof(Inimigo))]
[DisallowMultipleComponent]
public class AnimacaoDoInimigo : MonoBehaviour
{
    [Header("Referencias (vazio = procura sozinho)")]
    [SerializeField] private AnimadorDeSprites animador;
    [SerializeField] private Inimigo inimigo;

    [Header("Ajustes")]
    [Tooltip("Abaixo desta velocidade ele conta como parado")]
    [SerializeField, Min(0f)] private float velocidadeParaAndar = 0.08f;

    [Tooltip("Velocidade de referencia pro clipe de correr acompanhar o passo")]
    [SerializeField, Min(0.1f)] private float velocidadeDeReferencia = 1.8f;

    private void Awake()
    {
        if (inimigo == null) inimigo = GetComponent<Inimigo>();
        if (animador == null) animador = GetComponentInChildren<AnimadorDeSprites>();

        if (animador == null)
            Debug.LogError($"[AnimacaoDoInimigo] {name}: sem AnimadorDeSprites neste objeto nem nos filhos.", this);
    }

    private void Update()
    {
        if (animador == null || inimigo == null)
            return;

        switch (inimigo.EstadoAtual)
        {
            case Inimigo.Estado.Morto:
                Tocar(NomesDeAnimacao.InimigoMorrer, 1f);
                return;

            case Inimigo.Estado.Atordoado:
                Tocar(NomesDeAnimacao.InimigoDano, 1f);
                return;

            case Inimigo.Estado.Atacando:
                TocarGolpe();
                return;

            case Inimigo.Estado.Preparando:
            case Inimigo.Estado.Alerta:
            case Inimigo.Estado.Recuperando:
                Tocar(NomesDeAnimacao.InimigoAlerta, 1f);
                return;
        }

        if (!inimigo.NoChao)
        {
            animador.TocarPrimeiroQueExistir(NomesDeAnimacao.InimigoNoAr, NomesDeAnimacao.InimigoParado);
            animador.Velocidade = 1f;
            return;
        }

        float velocidade = inimigo.VelocidadeHorizontal;

        if (velocidade < velocidadeParaAndar)
        {
            // Perseguindo mas parado (esperando na beirada): fica em guarda, nao relaxa.
            Tocar(inimigo.Perseguindo ? NomesDeAnimacao.InimigoAlerta : NomesDeAnimacao.InimigoParado, 1f);
            return;
        }

        Tocar(NomesDeAnimacao.InimigoCorrer, Mathf.Clamp(velocidade / velocidadeDeReferencia, 0.5f, 1.8f));
    }

    private void TocarGolpe()
    {
        string clipe = inimigo.GolpeAtual > 0
            ? NomesDeAnimacao.InimigoAtaque2
            : NomesDeAnimacao.InimigoAtaque1;

        string tocado = animador.TocarPrimeiroQueExistir(clipe, NomesDeAnimacao.InimigoAtaque1);

        if (tocado == null)
            return;

        // Estica/encurta o clipe pra durar exatamente o que o golpe dura.
        float duracaoDoClipe = animador.DuracaoDe(tocado);
        float duracaoDoGolpe = inimigo.DuracaoDoGolpe;

        animador.Velocidade = duracaoDoGolpe > 0.01f && duracaoDoClipe > 0.01f
            ? duracaoDoClipe / duracaoDoGolpe
            : 1f;
    }

    private void Tocar(string nome, float velocidade)
    {
        animador.Tocar(nome);
        animador.Velocidade = velocidade;
    }
}
