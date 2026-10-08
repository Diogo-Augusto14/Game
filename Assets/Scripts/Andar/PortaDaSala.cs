using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A porta de uma sala de luta: a porta grande de madeira do pacote Crypt, na ponta do corredor
/// encostada na sala. Aberta, fica afundada no chao (nao aparece); ao fechar, sobe do chao e e parede
/// (camada "Wall"): ninguem passa, nem tiro, e os inimigos de fora nao veem quem esta dentro.
///
/// Num corredor que sobe ela aparece de frente, da largura do corredor; num corredor deitado, de lado
/// (uma tabua em pe). Sem a arte, uma grade de ferro desenhada.
/// </summary>
public class PortaDaSala : MonoBehaviour
{
    private const float QuadrosPorSegundo = 24f;
    private const float Pixels = 32f;

    private static Sprite[] deFrente, deLado;
    private static Sprite grade, gradeDeLado, gradeDeLadoTopo;
    private static bool carregou;

    private SpriteRenderer folha;
    private Sprite[] quadros;
    private readonly List<Transform> barras = new List<Transform>();
    private BoxCollider2D colisor;
    private Coroutine mexendo;

    /// <summary>A porta esta fechada.</summary>
    public bool Fechada { get; private set; }

    public static PortaDaSala Criar(PortaDaPlanta planta, Transform pai)
    {
        Carregar();

        Vector2 soma = Vector2.zero;
        int menor = int.MaxValue;

        foreach (Vector2Int c in planta.celulas)
        {
            soma += c;
            menor = Mathf.Min(menor, planta.DeFrente ? c.x : c.y);
        }

        int quantas = Mathf.Max(1, planta.celulas.Count);
        Vector2 meio = soma / quantas;
        GameObject obj = new GameObject("Porta");
        obj.transform.SetParent(pai, false);
        obj.transform.position = meio;
        obj.layer = Pedreiro.CamadaDaParede;

        PortaDaSala porta = obj.AddComponent<PortaDaSala>();
        porta.colisor = obj.AddComponent<BoxCollider2D>();
        porta.colisor.size = planta.DeFrente ? new Vector2(quantas, 1f) : new Vector2(1f, quantas);
        porta.colisor.enabled = false;

        Sprite[] desenhos = planta.DeFrente ? deFrente : deLado;

        if (desenhos != null && desenhos.Length > 0)
        {
            // De frente: o pe na beirada de baixo da linha da porta, da largura do corredor (a arte tem 3).
            // De lado: o pe na celula de baixo; a tabua cobre o corredor e sobe um pouco.
            Vector2 pe = planta.DeFrente ? new Vector2(meio.x, meio.y - 0.5f) : new Vector2(meio.x, menor - 0.5f);
            float largura = planta.DeFrente ? quantas / 3f : 1f;
            porta.Folha(obj.transform, pe, largura, desenhos);
        }
        else
        {
            porta.Grades(planta, obj.transform);
        }

        return porta;
    }

    private void Folha(Transform pai, Vector2 pe, float largura, Sprite[] desenhos)
    {
        GameObject obj = new GameObject("Porta de madeira");
        obj.transform.SetParent(pai, false);
        obj.transform.position = pe;
        obj.transform.localScale = new Vector3(largura, 1f, 1f);
        folha = obj.AddComponent<SpriteRenderer>();
        folha.sprite = desenhos[desenhos.Length - 1];
        folha.sortingOrder = 10;
        folha.spriteSortPoint = SpriteSortPoint.Pivot;
        folha.enabled = false;
        quadros = desenhos;
    }

    private static void Carregar()
    {
        if (carregou)
            return;

        carregou = true;
        deFrente = Cortar(Resources.Load<Texture2D>("Decoracao/PortaGrande"), 96);
        deLado = Cortar(Resources.Load<Texture2D>("Decoracao/PortaGrandeDeLado"), 16);
        grade = Resources.Load<Sprite>("Itens/Grade");
        gradeDeLado = Resources.Load<Sprite>("Itens/GradeDeLado");
        gradeDeLadoTopo = Resources.Load<Sprite>("Itens/GradeDeLadoTopo");
    }

    // Quadros lado a lado, com o pivo no pe (embaixo, no meio).
    private static Sprite[] Cortar(Texture2D textura, int largura)
    {
        if (textura == null)
            return null;

        Sprite[] lista = new Sprite[textura.width / largura];

        for (int i = 0; i < lista.Length; i++)
            lista[i] = Sprite.Create(textura, new Rect(i * largura, 0, largura, textura.height), new Vector2(0.5f, 0f), Pixels, 0, SpriteMeshType.FullRect);

        return lista;
    }

    // Sem a arte do Crypt: a grade de ferro que sobe do chao.
    private void Grades(PortaDaPlanta planta, Transform pai)
    {
        int maisAlta = int.MinValue;

        foreach (Vector2Int c in planta.celulas)
            maisAlta = Mathf.Max(maisAlta, c.y);

        foreach (Vector2Int c in planta.celulas)
        {
            GameObject barra = new GameObject("Grade");
            barra.transform.SetParent(pai, false);
            barra.transform.position = (Vector2)c + new Vector2(0f, -0.5f);
            SpriteRenderer desenho = barra.AddComponent<SpriteRenderer>();
            desenho.sprite = planta.DeFrente ? grade : (c.y == maisAlta ? gradeDeLadoTopo : gradeDeLado);
            desenho.sortingOrder = 10;
            desenho.spriteSortPoint = SpriteSortPoint.Pivot;
            barra.transform.localScale = new Vector3(1f, 0f, 1f);
            barra.SetActive(false);
            barras.Add(barra.transform);
        }
    }

    /// <summary>Fecha (ja e parede no primeiro quadro: ninguem escapa durante a animacao).</summary>
    public void Fechar()
    {
        if (Fechada)
            return;

        Fechada = true;
        colisor.enabled = true;
        Mexer(true);
    }

    public void Abrir()
    {
        if (!Fechada)
            return;

        Fechada = false;
        colisor.enabled = false;
        Mexer(false);
    }

    private void Mexer(bool fechar)
    {
        if (mexendo != null)
            StopCoroutine(mexendo);

        mexendo = StartCoroutine(folha != null ? Subir(fechar) : SubirGrade(fechar ? 1f : 0f));
    }

    // Os quadros vao do fechado (0) ao quase todo afundado (o ultimo): fechar toca de tras pra frente.
    private IEnumerator Subir(bool fechar)
    {
        folha.enabled = true;

        for (int i = 0; i < quadros.Length; i++)
        {
            folha.sprite = quadros[fechar ? quadros.Length - 1 - i : i];
            yield return new WaitForSeconds(1f / QuadrosPorSegundo);
        }

        // Aberta, afunda de vez.
        folha.sprite = quadros[0];
        folha.enabled = fechar;
        mexendo = null;
    }

    private IEnumerator SubirGrade(float ate)
    {
        foreach (Transform barra in barras)
            barra.gameObject.SetActive(true);

        float de = barras.Count > 0 ? barras[0].localScale.y : 0f;

        for (float t = 0f; t < 0.18f; t += Time.deltaTime)
        {
            float y = Mathf.Lerp(de, ate, t / 0.18f);

            foreach (Transform barra in barras)
                barra.localScale = new Vector3(1f, y, 1f);

            yield return null;
        }

        foreach (Transform barra in barras)
        {
            barra.localScale = new Vector3(1f, ate, 1f);
            barra.gameObject.SetActive(ate > 0f);
        }

        mexendo = null;
    }
}
