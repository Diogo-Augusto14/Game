using UnityEngine;

/// <summary>
/// Pula atras do jogador (tipo o Hopper do Isaac): agacha, salta num arco ate perto de
/// onde o jogador esta e para um instante no chao. Parado no chao e a hora de acertar.
/// A sombra fica no chao durante o pulo, entao da pra ver onde ele vai cair.
///
///   Agindo      -> parado, esperando o proximo pulo
///   Preparando  -> agacha (aviso) e depois voa ate o ponto escolhido
///
/// Com <see cref="VirarGeleia"/> ele pula SEM RUMO pela sala, mais vezes e mais curto: nao
/// persegue, mas atravessa o caminho do jogador quando menos se espera.
/// </summary>
public class InimigoSaltador : InimigoDeSala
{
    private bool geleia;

    public void VirarGeleia()
    {
        geleia = true;
        esperaEntrePulos = new Vector2(0.25f, 0.6f);
        alcanceDoPulo = 2.2f;
    }

    [Header("Pulo")]
    [Tooltip("Segundos parado entre um pulo e outro (sorteado entre os dois)")]
    [SerializeField] private Vector2 esperaEntrePulos = new Vector2(0.5f, 1.1f);

    [Tooltip("Aviso: segundos agachado antes de saltar")]
    [SerializeField, Min(0f)] private float tempoAgachado = 0.2f;

    [SerializeField, Min(0.1f)] private float duracaoDoPulo = 0.5f;

    [Tooltip("Distancia maxima de um pulo")]
    [SerializeField, Min(0.5f)] private float alcanceDoPulo = 3f;

    [Tooltip("Quanto ele erra de proposito o ponto do jogador")]
    [SerializeField, Min(0f)] private float imprecisao = 0.8f;

    [SerializeField, Min(0f)] private float alturaDoPulo = 0.45f;

    private Cronometro espera;
    private Cronometro agachado;
    private Cronometro voo;
    private bool voando;
    private Vector2 velocidadeDoPulo;
    private Transform corpo;
    private Vector3 posicaoDoCorpo;
    private Vector3 escalaDoCorpo;

    protected override void Awake()
    {
        base.Awake();

        if (desenho != null)
        {
            corpo = desenho.transform;
            posicaoDoCorpo = corpo.localPosition;
            escalaDoCorpo = corpo.localScale;

            // Sombra no chao, embaixo do corpo: fica parada enquanto o corpo sobe.
            FormasDaSala.Desenho(transform, "Sombra", FormasDaSala.Circulo(), new Color(0f, 0f, 0f, 0.35f),
                new Vector2(0f, -Raio * 0.55f), new Vector2(Raio * 1.8f, Raio * 0.7f), desenho.sortingOrder - 1);
        }

        espera.Forcar(Random.Range(esperaEntrePulos.x, esperaEntrePulos.y));
    }

    protected override void AtualizarAgindo(float dt)
    {
        Frear();
        espera.Contar(dt);
        Pousar();

        if (espera.Ativo || jogador == null)
            return;

        agachado.Forcar(tempoAgachado);
        voando = false;
        EstadoAtual = Estado.Preparando;
    }

    protected override void AtualizarPreparando(float dt)
    {
        if (!voando)
        {
            Frear();
            agachado.Contar(dt);

            // Achata: aviso de que vai pular.
            if (corpo != null)
                corpo.localScale = new Vector3(escalaDoCorpo.x * 1.2f, escalaDoCorpo.y * 0.75f, 1f);

            if (agachado.Ativo)
                return;

            Vector2 alvo = geleia
                ? Random.insideUnitCircle.normalized * alcanceDoPulo
                : ParaOJogador() + Random.insideUnitCircle * imprecisao;
            alvo = Vector2.ClampMagnitude(alvo, alcanceDoPulo);
            velocidadeDoPulo = alvo / duracaoDoPulo;
            voo.Forcar(duracaoDoPulo);
            voando = true;
            return;
        }

        voo.Contar(dt);
        rb.linearVelocity = velocidadeDoPulo;

        // Arco: o corpo sobe e desce; a sombra (e o colisor) seguem no chao.
        float t = 1f - voo.Restante / duracaoDoPulo;

        if (corpo != null)
        {
            corpo.localPosition = posicaoDoCorpo + Vector3.up * Mathf.Sin(t * Mathf.PI) * alturaDoPulo;
            corpo.localScale = escalaDoCorpo;
        }

        if (voo.Ativo)
            return;

        Pousar();
        rb.linearVelocity = Vector2.zero;
        voando = false;

        espera.Forcar(Random.Range(esperaEntrePulos.x, esperaEntrePulos.y));
        EstadoAtual = Estado.Agindo;
    }

    private void Pousar()
    {
        if (corpo == null)
            return;

        corpo.localPosition = posicaoDoCorpo;
        corpo.localScale = escalaDoCorpo;
    }

    protected override void Morrer()
    {
        Pousar();
        base.Morrer();
    }
}
