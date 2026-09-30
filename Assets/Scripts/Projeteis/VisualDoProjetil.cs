using UnityEngine;

/// <summary>
/// Desenha um projetil com a sua <see cref="AparenciaDoProjetil"/>: um filho "Visual" com
/// os quadros animados, apontado pro rumo do voo (ou girando), pulsando, balancando,
/// piscando e deixando rastro e faiscas pelo caminho.
///
/// O desenho fica num filho pra poder girar e pulsar sem mexer no colisor do objeto. O
/// SpriteRenderer da raiz, se tiver, fica sem sprite. Quem veste e <see cref="Vestir"/>.
/// </summary>
[DisallowMultipleComponent]
public class VisualDoProjetil : MonoBehaviour
{
    private AparenciaDoProjetil aparencia;
    private SpriteRenderer desenho;
    private Transform visual;
    private Rigidbody2D rb;
    private float escalaBase;
    private float nascimento;
    private float fase;
    private float proximoRastro;
    private float proximaFaisca;
    private float anguloDoGiro;
    private Vector2 ultimoRumo = Vector2.right;

    /// <summary>O desenho de verdade (o filho). A bolha do chefe pisca por ele.</summary>
    public SpriteRenderer Desenho => desenho;

    public AparenciaDoProjetil Aparencia => aparencia;

    /// <summary>
    /// Troca o desenho do projetil pela aparencia dada. Sem arte, nao faz nada e devolve
    /// null: o projetil continua com o desenho que ja tinha.
    /// </summary>
    public static VisualDoProjetil Vestir(GameObject projetil, AparenciaDoProjetil aparencia, int ordem = 20)
    {
        if (projetil == null || aparencia == null || !aparencia.TemDesenho)
            return null;

        if (projetil.TryGetComponent(out SpriteRenderer daRaiz))
            daRaiz.sprite = null;

        VisualDoProjetil v = projetil.GetComponent<VisualDoProjetil>();

        if (v == null)
            v = projetil.AddComponent<VisualDoProjetil>();

        v.Montar(aparencia, ordem);
        return v;
    }

    private void Montar(AparenciaDoProjetil nova, int ordem)
    {
        aparencia = nova;
        rb = GetComponent<Rigidbody2D>();
        nascimento = Time.time;
        fase = Random.value * 10f;

        if (visual == null)
        {
            GameObject filho = new GameObject("Visual");
            filho.transform.SetParent(transform, false);
            visual = filho.transform;
            desenho = filho.AddComponent<SpriteRenderer>();
        }

        desenho.sprite = aparencia.quadros[0];
        desenho.color = aparencia.cor;
        desenho.sortingOrder = ordem;

        // O lado maior do desenho = tamanho x diametro. A escala da raiz ja e o diametro,
        // entao aqui so entra a conta do sprite.
        escalaBase = EfeitosDeImpacto.EscalaPara(aparencia.quadros[0], aparencia.tamanho);
        visual.localScale = Vector3.one * escalaBase;

        Rumo();
        Atualizar();
    }

    private void Update()
    {
        if (aparencia == null)
            return;

        Atualizar();
        Rastro();
        Faiscas();
    }

    private void Atualizar()
    {
        float idade = Time.time - nascimento;

        // Quadros
        Sprite[] quadros = aparencia.quadros;

        if (quadros.Length > 1)
            desenho.sprite = quadros[Mathf.FloorToInt((idade + fase) * aparencia.quadrosPorSegundo) % quadros.Length];

        // Rumo, giro e balanco
        float angulo;

        if (aparencia.apontar)
        {
            Vector2 r = Rumo();
            angulo = Mathf.Atan2(r.y, r.x) * Mathf.Rad2Deg - aparencia.anguloDoDesenho;
        }
        else
        {
            angulo = 0f;
        }

        if (!Mathf.Approximately(aparencia.giro, 0f))
        {
            anguloDoGiro += aparencia.giro * Time.deltaTime;
            angulo += anguloDoGiro;
        }

        if (aparencia.balanco > 0f)
            angulo += Mathf.Sin((idade + fase) * aparencia.ritmoDoBalanco) * aparencia.balanco;

        visual.rotation = Quaternion.Euler(0f, 0f, angulo);

        // Pulso
        float escala = escalaBase;

        if (aparencia.pulso > 0f)
            escala *= 1f + Mathf.Sin((idade + fase) * aparencia.ritmoDoPulso * Mathf.PI * 2f) * aparencia.pulso;

        visual.localScale = Vector3.one * escala;

        // Pisca
        if (aparencia.corDoPisca.a > 0f)
            desenho.color = Mathf.Repeat((idade + fase) * aparencia.ritmoDoPisca, 1f) < 0.5f ? aparencia.cor : aparencia.corDoPisca;
    }

    /// <summary>Pra onde o projetil voa agora (guarda o ultimo, pra nao virar quando para).</summary>
    private Vector2 Rumo()
    {
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f)
            ultimoRumo = rb.linearVelocity.normalized;

        return ultimoRumo;
    }

    private void Rastro()
    {
        if (aparencia.intervaloDoRastro <= 0f || Time.time < proximoRastro || !Voando())
            return;

        proximoRastro = Time.time + aparencia.intervaloDoRastro;
        RastroQueSome.Criar(desenho, aparencia.corDoRastro, aparencia.duracaoDoRastro);
    }

    private void Faiscas()
    {
        Sprite[] faiscas = aparencia.faiscas;

        if (faiscas == null || faiscas.Length == 0 || Time.time < proximaFaisca || !Voando())
            return;

        proximaFaisca = Time.time + aparencia.intervaloDasFaiscas;

        // Solta um pouco atras da ponta, com um tiquinho de espalhado.
        Vector2 atras = -ultimoRumo * transform.lossyScale.x * aparencia.tamanho * 0.35f;
        Vector2 onde = (Vector2)visual.position + atras + Random.insideUnitCircle * 0.04f;
        EfeitoDeQuadros faisca = EfeitoDeQuadros.Criar(faiscas, 22f, onde, desenho.sortingOrder - 1);

        if (faisca == null)
            return;

        faisca.transform.localScale = Vector3.one * EfeitosDeImpacto.EscalaPara(faiscas[0], aparencia.tamanhoDasFaiscas);
        faisca.GetComponent<SpriteRenderer>().color = aparencia.corDasFaiscas;
    }

    private bool Voando() => rb == null || rb.linearVelocity.sqrMagnitude > 0.01f;

    /// <summary>Mostra o efeito de impacto desta aparencia, onde o projetil esta.</summary>
    public void MostrarImpacto()
    {
        if (aparencia == null)
            return;

        EfeitosDeImpacto.Mostrar(aparencia.impacto, transform.position, aparencia.corDoImpacto, aparencia.tamanhoDoImpacto);
    }
}
