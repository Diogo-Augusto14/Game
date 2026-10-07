using UnityEngine;

/// <summary>
/// A arma de fogo desenhada na mao do jogador: gira em volta do corpo pra onde ele mira (vira de
/// ponta-cabeca pro outro lado quando mira pra esquerda, pra nao ficar de cabeca pra baixo), coiceia
/// a cada tiro, solta um clarao na boca do cano e mostra uma barrinha em cima da cabeca enquanto
/// recarrega. O corpo do heroi tambem vira pro lado da mira.
///
/// Fica no jogador; o <see cref="ArsenalDoJogador"/> cria e manda mostrar a arma da vez.
/// </summary>
[DisallowMultipleComponent]
public class ArmaNaMao : MonoBehaviour
{
    [Tooltip("Distancia do centro do corpo ate a empunhadura, em unidades do jogador")]
    [SerializeField, Min(0f)] private float orbita = 0.3f;

    [SerializeField, Min(0f)] private float duracaoDoClarao = 0.05f;

    private ArsenalDoJogador arsenal;
    private AtiradorTopDown atirador;
    private SpriteRenderer corpo;
    private Vida vida;

    private Transform maoDaArma;
    private SpriteRenderer desenho;
    private SpriteRenderer clarao;
    private Transform barra;
    private Transform enchimento;
    private SpriteRenderer fundoDaBarra;
    private SpriteRenderer corDaBarra;

    private ArmaDeFogo arma;
    private float recuoAtual;
    private float fimDoClarao;

    private static readonly Color CorDaRecarga = new Color(1f, 0.85f, 0.3f);

    public static ArmaNaMao Criar(ArsenalDoJogador arsenal)
    {
        ArmaNaMao nova = arsenal.gameObject.AddComponent<ArmaNaMao>();
        nova.arsenal = arsenal;
        arsenal.AoDisparar += nova.Disparou;
        return nova;
    }

    private void Awake()
    {
        atirador = GetComponent<AtiradorTopDown>();
        corpo = GetComponent<SpriteRenderer>();
        vida = GetComponent<Vida>();

        GameObject mao = new GameObject("Arma na mao");
        mao.transform.SetParent(transform, false);
        maoDaArma = mao.transform;
        desenho = mao.AddComponent<SpriteRenderer>();
        desenho.sortingOrder = 11;

        GameObject flash = new GameObject("Clarao");
        flash.transform.SetParent(transform, false);
        flash.transform.localScale = Vector3.one * 0.38f;
        clarao = flash.AddComponent<SpriteRenderer>();
        clarao.sprite = FormasTopDown.Circulo();
        clarao.color = new Color(1f, 0.93f, 0.6f, 0.95f);
        clarao.sortingOrder = 12;
        clarao.enabled = false;

        MontarBarra();
        mao.SetActive(false);
        barra.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (arsenal != null)
            arsenal.AoDisparar -= Disparou;
    }

    // Duas barras sobre a cabeca: o fundo escuro e o enchimento, que cresce da esquerda pra direita.
    private void MontarBarra()
    {
        GameObject raiz = new GameObject("Barra de recarga");
        raiz.transform.SetParent(transform, false);
        raiz.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        barra = raiz.transform;

        GameObject fundo = new GameObject("Fundo");
        fundo.transform.SetParent(barra, false);
        fundo.transform.localScale = new Vector3(1.04f, 0.18f, 1f);
        fundoDaBarra = fundo.AddComponent<SpriteRenderer>();
        fundoDaBarra.sprite = FormasTopDown.Quadrado();
        fundoDaBarra.color = new Color(0.05f, 0.04f, 0.06f, 0.85f);
        fundoDaBarra.sortingOrder = 30;

        GameObject cheio = new GameObject("Enchimento");
        cheio.transform.SetParent(barra, false);
        enchimento = cheio.transform;
        corDaBarra = cheio.AddComponent<SpriteRenderer>();
        corDaBarra.sprite = FormasTopDown.Quadrado();
        corDaBarra.color = CorDaRecarga;
        corDaBarra.sortingOrder = 31;
    }

    /// <summary>Poe a arma na mao (null = a do heroi: some o desenho).</summary>
    public void Mostrar(ArmaDeFogo nova)
    {
        arma = nova;
        recuoAtual = 0f;

        if (maoDaArma == null)
            return;

        maoDaArma.gameObject.SetActive(nova != null);

        if (nova != null)
            desenho.sprite = nova.Desenho;
    }

    /// <summary>Onde a bala nasce: na boca do cano, em linha reta com a mira.</summary>
    public Vector2 PontaDoCano(Vector2 direcao)
    {
        float comprimento = arma != null ? ArteDasArmas.Comprimento(arma.estilo) : 0.3f;
        return (Vector2)transform.position + direcao * ((orbita + comprimento) * transform.lossyScale.x);
    }

    private void Disparou(ArmaDeFogo quem)
    {
        recuoAtual = quem.recuo;
        fimDoClarao = Time.time + duracaoDoClarao;
    }

    private void LateUpdate()
    {
        bool vivo = vida == null || !vida.EstaMorto;
        bool mostrarArma = arma != null && vivo;

        if (maoDaArma.gameObject.activeSelf != mostrarArma)
            maoDaArma.gameObject.SetActive(mostrarArma);

        if (!mostrarArma)
        {
            clarao.enabled = false;
            barra.gameObject.SetActive(false);
            return;
        }

        Vector2 direcao = atirador != null ? atirador.Olhando : Vector2.right;

        if (direcao.sqrMagnitude < 0.0001f)
            direcao = Vector2.right;

        direcao.Normalize();
        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        bool paraEsquerda = direcao.x < 0f;

        recuoAtual = Mathf.MoveTowards(recuoAtual, 0f, Time.deltaTime * 1.4f);

        maoDaArma.localPosition = direcao * (orbita - recuoAtual);
        maoDaArma.localRotation = Quaternion.Euler(0f, 0f, angulo);
        maoDaArma.localScale = new Vector3(1f, paraEsquerda ? -1f : 1f, 1f);

        // Mirando pra cima a arma passa por tras do corpo; nas outras direcoes, na frente.
        desenho.sortingOrder = direcao.y > 0.55f ? 9 : 11;

        // O corpo do heroi olha pro lado da mira, nao pro lado que anda.
        if (corpo != null && Mathf.Abs(direcao.x) > 0.1f)
            corpo.flipX = paraEsquerda;

        // Clarao na boca do cano.
        bool piscando = Time.time < fimDoClarao;
        clarao.enabled = piscando;

        if (piscando)
        {
            float comprimento = ArteDasArmas.Comprimento(arma.estilo);
            clarao.transform.localPosition = direcao * (orbita - recuoAtual + comprimento + 0.08f);
        }

        AtualizarBarra();
    }

    private void AtualizarBarra()
    {
        bool recarregando = arsenal != null && arsenal.Recarregando;

        if (barra.gameObject.activeSelf != recarregando)
            barra.gameObject.SetActive(recarregando);

        if (!recarregando)
            return;

        float progresso = arsenal.ProgressoDaRecarga;
        enchimento.localScale = new Vector3(Mathf.Max(0.001f, progresso), 0.1f, 1f);
        enchimento.localPosition = new Vector3(-0.5f + progresso * 0.5f, 0f, 0f);
        corDaBarra.color = Color.Lerp(CorDaRecarga, Color.white, progresso * progresso);
    }
}
