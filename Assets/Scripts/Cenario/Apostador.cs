using UnityEngine;

/// <summary>
/// O apostador (<see cref="TipoDeSala.Apostas"/>): um mago de capuz atras de uma mesinha com dados. Joga
/// dados valendo moedas na <see cref="TelaDasApostas"/>: cada um rola dois dados, quem tirar mais leva;
/// ganhando, a aposta volta em dobro; empatando, volta igual. Aceita ate <see cref="ApostasPorMesa"/>
/// apostas, depois vai embora contente (ou nao). Monte com <see cref="Criar"/>.
/// </summary>
public class Apostador : Interativo
{
    /// <summary>Quantas apostas cada apostador aceita.</summary>
    public const int ApostasPorMesa = 5;

    private static readonly Color Tom = new Color(0.72f, 0.62f, 0.86f);

    private Sprite[] parado;
    private SpriteRenderer desenho;
    private Transform jogador;

    /// <summary>Quantas apostas ainda da pra fazer nesta mesa.</summary>
    public int Restantes { get; set; } = ApostasPorMesa;

    public override float Alcance => 2.4f;

    public override bool Disponivel => Restantes > 0 && !TelaDasApostas.Ocupada;

    public override string Dica => Idioma.F("jogar dados com o apostador (faltam {0} apostas)", Restantes);

    /// <param name="mesa">O pe da mesa (o apostador fica logo atras).</param>
    public static Apostador Criar(Vector2 mesa, Transform pai)
    {
        Sprite[] parado = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Personagens/Herois/Mago/Idle"), new Vector2Int(100, 100), 20f);

        if (parado.Length == 0)
            return null;

        GameObject obj = new GameObject("Apostador");
        obj.transform.SetParent(pai, false);
        obj.transform.position = mesa + Vector2.up * (0.55f + 0.35f);

        Apostador a = obj.AddComponent<Apostador>();
        a.parado = parado;
        a.desenho = obj.AddComponent<SpriteRenderer>();
        a.desenho.sprite = parado[0];
        a.desenho.color = Tom;
        a.desenho.sortingOrder = 9;

        // A mesa na frente dele: ninguem atravessa, e e por ela que o jogador chega.
        GameObject m = new GameObject("Mesa de dados");
        m.transform.SetParent(pai, false);
        m.transform.position = mesa;
        SpriteRenderer sm = m.AddComponent<SpriteRenderer>();
        sm.sprite = ArteDoAntigo.Inteira("Cenario/MesaDeDados", 32f, true);
        sm.sortingOrder = 10;
        m.layer = Pedreiro.CamadaDaParede;
        BoxCollider2D c = m.AddComponent<BoxCollider2D>();
        c.size = new Vector2(1.8f, 0.4f);
        c.offset = new Vector2(0f, 0.5f);

        // Um lampiao baixo em cima da mesa.
        Iluminacao.Luz(m.transform, new Vector2(0f, 0.8f), new Color(1f, 0.8f, 0.5f), 3f, 0.75f, 0.08f);
        return a;
    }

    private void Update()
    {
        desenho.sprite = parado[Mathf.FloorToInt(Time.time * 6f) % parado.Length];

        if (jogador == null)
        {
            GameObject j = GameObject.FindWithTag("Player");
            jogador = j != null ? j.transform : null;
        }

        if (jogador != null)
            desenho.flipX = jogador.position.x < transform.position.x;
    }

    public override void Usar(GameObject quem)
    {
        if (Restantes > 0)
            TelaDasApostas.Abrir(quem, this);
    }

    /// <summary>A mesa fechou (as apostas acabaram): ele se despede.</summary>
    public void Despedir(int saldo)
    {
        string frase = saldo > 0 ? "Hoje a sorte foi sua..." : saldo < 0 ? "Volte sempre!" : "Empatamos. Até a próxima.";
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.3f, frase, new Color(0.85f, 0.75f, 1f), 2.2f);
    }
}
