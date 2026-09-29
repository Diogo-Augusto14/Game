using UnityEngine;

/// <summary>
/// Barril de TNT (Tiny Swords): parece so um barril parado. Quando o jogador chega perto,
/// um goblin sai de dentro, corre com o barril ate ele, acende o pavio e explode.
///
///   Escondido -> barril fechado, parado
///   Saindo    -> a animacao do goblin saindo (o aviso)
///   Correndo  -> corre atras do jogador por um tempo
///   Pavio     -> para e pisca; explode no fim
///
/// Morto a tiro, explode na hora: acertar de longe e a resposta, e a explosao tambem
/// machuca os outros inimigos em volta.
/// </summary>
public class InimigoBarril : InimigoComArte
{
    private enum Fase { Escondido, Saindo, Correndo, Pavio }

    [Header("Acordar")]
    [Tooltip("Mais perto que isto, o goblin sai do barril")]
    [SerializeField, Min(0f)] private float distanciaParaSair = 3.2f;

    [SerializeField, Min(0f)] private float tempoSaindo = 0.5f;

    [Header("Corrida")]
    [SerializeField, Min(0f)] private float velocidadeCorrendo = 3.2f;

    [Tooltip("Segundos correndo antes de acender o pavio de qualquer jeito")]
    [SerializeField, Min(0f)] private float tempoMaximoCorrendo = 2.5f;

    [Tooltip("Mais perto que isto, acende o pavio")]
    [SerializeField, Min(0f)] private float distanciaParaAcender = 0.9f;

    [Header("Explosao")]
    [SerializeField, Min(0f)] private float tempoDePavio = 0.7f;

    [SerializeField, Min(0.1f)] private float raioDaExplosao = 1.2f;

    [Tooltip("Um coracao inteiro")]
    [SerializeField, Min(0f)] private float danoNoJogador = 20f;

    [SerializeField, Min(0f)] private float danoNosOutros = 30f;

    private Fase fase = Fase.Escondido;
    private Cronometro relogio;
    private bool explodiu;

    protected override void Awake()
    {
        base.Awake();
        velocidade = velocidadeCorrendo;
        danoDeContato = 5f;
    }

    protected override void AtualizarAgindo(float dt)
    {
        relogio.Contar(dt);
        Vector2 alvo = ParaOJogador();
        float distancia = alvo.magnitude;

        switch (fase)
        {
            case Fase.Escondido:
                Frear();

                if (jogador != null && distancia <= distanciaParaSair && VeOJogador())
                {
                    fase = Fase.Saindo;
                    relogio.Forcar(tempoSaindo);
                    animacao?.OlharPara(alvo);

                    if (clipes != null && clipes.AtaqueEspecial != null)
                        animacao?.TocarUmaVez(clipes.AtaqueEspecial, clipes.AtaqueEspecial.Length / Mathf.Max(0.05f, tempoSaindo));
                }

                break;

            case Fase.Saindo:
                Frear();

                if (!relogio.Ativo)
                {
                    fase = Fase.Correndo;
                    relogio.Forcar(tempoMaximoCorrendo);
                }

                break;

            case Fase.Correndo:
                if (distancia <= distanciaParaAcender || !relogio.Ativo)
                {
                    AcenderPavio();
                    break;
                }

                Andar(alvo, velocidadeCorrendo);
                break;

            case Fase.Pavio:
                Frear();

                if (!relogio.Ativo)
                    vida.MatarAgora();   // o Morrer explode e a sala conta a morte

                break;
        }
    }

    private void AcenderPavio()
    {
        fase = Fase.Pavio;
        relogio.Forcar(tempoDePavio);
        Frear();
        Sons.Tocar(Som.Negado, 0.5f);

        // O pavio queimando fica em loop ate explodir.
        if (clipes != null && clipes.Ataque != null)
            animacao?.TocarUmaVez(Repetir(clipes.Ataque, 4), 4 * clipes.Ataque.Length / Mathf.Max(0.05f, tempoDePavio));
    }

    private static Sprite[] Repetir(Sprite[] quadros, int vezes)
    {
        Sprite[] todos = new Sprite[quadros.Length * vezes];

        for (int i = 0; i < todos.Length; i++)
            todos[i] = quadros[i % quadros.Length];

        return todos;
    }

    private void LateUpdate()
    {
        // Pisca vermelho enquanto o pavio queima (mais rapido perto do fim).
        if (fase != Fase.Pavio || desenho == null || EstaMorto)
            return;

        float ritmo = Mathf.Lerp(6f, 16f, 1f - relogio.Restante / Mathf.Max(0.05f, tempoDePavio));
        desenho.color = Mathf.Repeat(Time.time * ritmo, 1f) < 0.5f ? Color.white : new Color(1f, 0.45f, 0.35f);
    }

    /// <summary>O barril explode em vez de deixar caveira.</summary>
    protected override bool DeixaCaveira => false;

    protected override void Morrer()
    {
        base.Morrer();

        if (explodiu)
            return;

        explodiu = true;

        // No lugar da caveira, a explosao (o barril voou longe).
        if (desenho != null)
            desenho.enabled = false;

        if (!Explosao.Estourar(transform.position, raioDaExplosao, danoNosOutros, danoNoJogador, 7f, gameObject))
            transform.localScale *= 0.6f;
    }
}
