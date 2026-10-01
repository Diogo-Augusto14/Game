using System.Collections;
using UnityEngine;

/// <summary>
/// Bau dos pacotes de masmorra. Dois jeitos:
///
///   Bau de madeira (<see cref="Criar"/>)          -> encostou, abre
///   Bau de ferro trancado (<see cref="CriarTrancado"/>) -> gasta uma chave pra abrir;
///       sem chave, treme e faz o som de negado. Tem uma chavinha flutuando em cima
///       pra dizer o que pede.
///
/// Abrindo, a tampa sobe quadro a quadro, toca o rangido com o brilho e so no fim o que
/// tem dentro espalha em volta (coletaveis) ou aparece num pedestal logo abaixo (item).
/// Depois fica no lugar, aberto.
/// </summary>
[DisallowMultipleComponent]
public class Bau : MonoBehaviour
{
    [SerializeField] private string tagDoJogador = "Player";

    [Tooltip("Segundos da tampa subindo")]
    [SerializeField, Min(0.05f)] private float tempoDeAbrir = 0.45f;

    private TipoDeColetavel[] conteudo;
    private ItemPassivo item;
    private SpriteRenderer desenho;
    private Sprite[] parado;
    private Sprite[] abrindo;
    private SpriteRenderer dica;
    private Vector3 escala;
    private float negadoEm = -10f;

    public bool Aberto { get; private set; }

    /// <summary>Precisa de chave pra abrir (e ainda nao foi aberto).</summary>
    public bool Trancado { get; private set; }

    /// <summary>Bau de madeira: abre so de encostar.</summary>
    public static Bau Criar(Vector2 posicao, Transform pai, params TipoDeColetavel[] conteudo)
        => Montar(posicao, pai, false, null, conteudo);

    /// <summary>
    /// Bau de ferro trancado: gasta uma chave. Com <paramref name="item"/>, o premio e o item
    /// num pedestal (mais os coletaveis, se tiver); sem, so os coletaveis.
    /// </summary>
    public static Bau CriarTrancado(Vector2 posicao, Transform pai, ItemPassivo item, params TipoDeColetavel[] conteudo)
        => Montar(posicao, pai, true, item, conteudo);

    private static Bau Montar(Vector2 posicao, Transform pai, bool trancado, ItemPassivo item, TipoDeColetavel[] conteudo)
    {
        GameObject obj = new GameObject(trancado ? "Bau trancado" : "Bau");
        obj.transform.SetParent(pai, true);
        obj.transform.position = posicao;

        Sprite[] parado = trancado ? ArteImportada.BauDeFerroParado : null;
        Sprite[] abrindo = trancado ? ArteImportada.BauDeFerroAbrindo : ArteImportada.BauAbrindo;
        Sprite primeiro = parado != null ? parado[0] : abrindo != null ? abrindo[0] : ArteImportada.Bau;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = primeiro != null ? primeiro : FormasTopDown.Quadrado();
        sr.color = primeiro != null ? Color.white
            : trancado ? new Color(0.45f, 0.47f, 0.52f) : new Color(0.5f, 0.33f, 0.18f);
        sr.sortingOrder = 4;
        obj.transform.localScale = Vector3.one * (primeiro != null ? 1f : 0.8f);

        // Solido (nao da pra atravessar) e um sensor um pouco maior pra abrir encostando.
        GameObject corpo = new GameObject("Corpo");
        corpo.transform.SetParent(obj.transform, false);
        corpo.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 0.6f);

        CircleCollider2D sensor = obj.AddComponent<CircleCollider2D>();
        sensor.isTrigger = true;
        sensor.radius = 0.7f;

        Bau bau = obj.AddComponent<Bau>();
        bau.conteudo = conteudo;
        bau.item = item;
        bau.desenho = sr;
        bau.parado = parado;
        bau.abrindo = abrindo;
        bau.Trancado = trancado;
        bau.escala = obj.transform.localScale;

        // A chave dourada flutuando em cima diz que precisa de chave.
        if (trancado)
        {
            Sprite chave = ArteImportada.Objeto(9, 4);
            bau.dica = chave != null
                ? FormasDaSala.Desenho(obj.transform, "Dica", chave, new Color(1f, 1f, 1f, 0.9f), new Vector2(0f, 0.75f), Vector2.one * 0.45f, 6)
                : FormasDaSala.Desenho(obj.transform, "Dica", FormasDaSala.Circulo(), Coletavel.CorDe(TipoDeColetavel.Chave),
                                       new Vector2(0f, 0.75f), Vector2.one * 0.25f, 6);
        }

        return bau;
    }

    private void Update()
    {
        if (Aberto)
            return;

        // O bau de ferro "respira" e a chavinha sobe e desce.
        if (parado != null && parado.Length > 0)
            desenho.sprite = parado[Mathf.FloorToInt(Time.time * 5f) % parado.Length];

        if (dica != null)
            dica.transform.localPosition = new Vector3(0f, 0.75f + Mathf.Sin(Time.time * 3f) * 0.08f, 0f);
    }

    private void OnTriggerEnter2D(Collider2D outro) => TentarAbrir(outro);

    private void OnTriggerStay2D(Collider2D outro) => TentarAbrir(outro);

    private void TentarAbrir(Collider2D outro)
    {
        if (Aberto)
            return;

        GameObject quem = outro.attachedRigidbody != null ? outro.attachedRigidbody.gameObject : outro.gameObject;

        if (!quem.CompareTag(tagDoJogador))
            return;

        if (Trancado)
        {
            Inventario inventario = quem.GetComponent<Inventario>();

            if (inventario == null || !inventario.Gastar(TipoDeColetavel.Chave))
            {
                // Sem chave: treme e avisa, sem repetir o som a cada quadro encostado.
                if (Time.time - negadoEm > 0.8f)
                {
                    negadoEm = Time.time;
                    Sons.Tocar(Som.Negado);
                    StartCoroutine(Tremer());
                }

                return;
            }

            Sons.Tocar(Som.Destranca);
            Trancado = false;
        }

        Aberto = true;
        StartCoroutine(Abrir());
    }

    private IEnumerator Abrir()
    {
        Sons.Tocar(Som.BauAbre);

        if (dica != null)
            Destroy(dica.gameObject);

        // A tampa sobe quadro a quadro, com um pulinho no bau inteiro.
        for (float t = 0f; t < 1f; t += Time.deltaTime / tempoDeAbrir)
        {
            if (abrindo != null && abrindo.Length > 0)
                desenho.sprite = abrindo[Mathf.Clamp(Mathf.FloorToInt(t * abrindo.Length), 0, abrindo.Length - 1)];

            transform.localScale = escala * (1f + Mathf.Sin(t * Mathf.PI) * 0.15f);
            yield return null;
        }

        transform.localScale = escala;

        if (abrindo != null && abrindo.Length > 0)
            desenho.sprite = abrindo[abrindo.Length - 1];
        else
            desenho.color = new Color(0.45f, 0.45f, 0.45f); // sem arte: escurece, como antes

        Espalhar();
    }

    private void Espalhar()
    {
        if (item != null)
        {
            Pedestal.Criar(item, (Vector2)transform.position + Vector2.down * 1.4f, transform.parent);
            Sons.Tocar(Som.Segredo);
        }

        // Em roda, abaixo do bau, pra nada cair em cima dele. Com pedestal (que fica embaixo),
        // os coletaveis vao pra cima.
        int n = conteudo != null ? conteudo.Length : 0;
        float de = item != null ? 20f : 200f;
        float ate = item != null ? 160f : 340f;

        for (int i = 0; i < n; i++)
        {
            float angulo = Mathf.Lerp(de, ate, n == 1 ? 0.5f : i / (float)(n - 1)) * Mathf.Deg2Rad;
            Vector2 ponto = (Vector2)transform.position + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * 1.2f;
            Coletavel.Criar(conteudo[i], ponto, transform.parent);
        }
    }

    private IEnumerator Tremer()
    {
        Vector3 inicio = transform.localPosition;

        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            transform.localPosition = inicio + Vector3.right * (Mathf.Sin(t * 80f) * 0.06f);
            yield return null;
        }

        transform.localPosition = inicio;
    }

    /// <summary>
    /// O que vai num bau de ferro, variado: bolsa de moedas, arsenal de bombas, kit de cura
    /// ou um pouco de tudo. Andares mais fundos poem mais moedas.
    /// </summary>
    public static TipoDeColetavel[] SortearTesouro(int andar)
    {
        var lista = new System.Collections.Generic.List<TipoDeColetavel>();
        int extra = Mathf.Clamp(andar - 1, 0, 3);

        switch (Random.Range(0, 4))
        {
            case 0: // bolsa de moedas
                for (int i = 0; i < 5 + extra; i++)
                    lista.Add(TipoDeColetavel.Moeda);
                break;
            case 1: // arsenal
                lista.Add(TipoDeColetavel.Bomba);
                lista.Add(TipoDeColetavel.Bomba);
                lista.Add(TipoDeColetavel.Bomba);
                lista.Add(TipoDeColetavel.Coracao);
                for (int i = 0; i < extra; i++)
                    lista.Add(TipoDeColetavel.Moeda);
                break;
            case 2: // kit de cura
                lista.Add(TipoDeColetavel.Coracao);
                lista.Add(TipoDeColetavel.Coracao);
                lista.Add(TipoDeColetavel.Coracao);
                for (int i = 0; i < 2 + extra; i++)
                    lista.Add(TipoDeColetavel.Moeda);
                break;
            default: // um pouco de tudo
                lista.Add(TipoDeColetavel.Moeda);
                lista.Add(TipoDeColetavel.Moeda);
                lista.Add(TipoDeColetavel.Bomba);
                lista.Add(TipoDeColetavel.Coracao);
                for (int i = 0; i < extra; i++)
                    lista.Add(TipoDeColetavel.Moeda);
                break;
        }

        return lista.ToArray();
    }
}
