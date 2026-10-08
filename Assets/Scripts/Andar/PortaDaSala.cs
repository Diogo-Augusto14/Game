using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A porta de uma sala de luta: as portas de metal do pacote Crypt, na ponta do corredor encostada na
/// sala. Aberta, ficam encostadas na parede do corredor; fechada, e parede (camada "Wall"): ninguem
/// passa, nem tiro, e os inimigos de fora nao veem quem esta dentro.
///
/// Atravessando um corredor que sobe, sao duas folhas de frente (uma de cada lado, se encontrando no
/// meio); num corredor deitado, uma folha de lado, que so aparece fechando e fechada. Sem a arte, uma
/// grade de ferro desenhada.
/// </summary>
public class PortaDaSala : MonoBehaviour
{
    private const float QuadrosPorSegundo = 22f;
    private const float Pixels = 32f;

    // As folhas do Crypt: quadros de 96 x 128, do aberto (0) ao fechado (o ultimo). A dobradica e o pe da
    // porta em pixels (do canto de cima-esquerdo do quadro), pra girar e assentar no lugar certo.
    private static readonly Vector2 DobradicaDeFrente = new Vector2(20f, 114f);
    private static readonly Vector2 DobradicaDeLado = new Vector2(57f, 128f);

    private static Sprite[] deFrente, deLado;
    private static Sprite grade, gradeDeLado, gradeDeLadoTopo;
    private static bool carregou;

    private readonly List<SpriteRenderer> folhas = new List<SpriteRenderer>();
    private Sprite[] quadros;
    private readonly List<Transform> barras = new List<Transform>();
    private BoxCollider2D colisor;
    private Coroutine mexendo;
    private int quadro;

    // A de lado aberta ficaria de frente, no meio do corredor: aberta, nao aparece.
    private bool someAberta;

    /// <summary>A porta esta fechada.</summary>
    public bool Fechada { get; private set; }

    public static PortaDaSala Criar(PortaDaPlanta planta, Transform pai)
    {
        Carregar();

        Vector2 soma = Vector2.zero;
        int menor = int.MaxValue, maior = int.MinValue;

        foreach (Vector2Int c in planta.celulas)
        {
            soma += c;
            menor = Mathf.Min(menor, planta.DeFrente ? c.x : c.y);
            maior = Mathf.Max(maior, planta.DeFrente ? c.x : c.y);
        }

        Vector2 meio = soma / Mathf.Max(1, planta.celulas.Count);
        GameObject obj = new GameObject("Porta");
        obj.transform.SetParent(pai, false);
        obj.transform.position = meio;
        obj.layer = Pedreiro.CamadaDaParede;

        PortaDaSala porta = obj.AddComponent<PortaDaSala>();
        porta.colisor = obj.AddComponent<BoxCollider2D>();
        porta.colisor.size = planta.DeFrente ? new Vector2(planta.celulas.Count, 1f) : new Vector2(1f, planta.celulas.Count);
        porta.colisor.enabled = false;

        if (planta.DeFrente && deFrente != null && deFrente.Length > 0)
        {
            // Duas folhas: a da esquerda com a dobradica na beirada esquerda, a da direita espelhada.
            float y = meio.y - 0.5f;
            porta.Folha(obj.transform, new Vector2(menor - 0.5f, y), 1f, deFrente);
            porta.Folha(obj.transform, new Vector2(maior + 0.5f, y), -1f, deFrente);
            porta.quadros = deFrente;
        }
        else if (!planta.DeFrente && deLado != null && deLado.Length > 0)
        {
            // Uma folha de lado, em pe na celula de baixo; abre pro lado do corredor (longe da sala).
            float espelho = planta.paraDentro.x > 0 ? 1f : -1f;
            porta.Folha(obj.transform, new Vector2(meio.x, menor - 0.5f), espelho, deLado);
            porta.quadros = deLado;
            porta.someAberta = true;
            porta.Mostrar(false);
        }
        else
        {
            porta.Grades(planta, obj.transform);
        }

        return porta;
    }

    private void Folha(Transform pai, Vector2 onde, float espelho, Sprite[] desenhos)
    {
        GameObject obj = new GameObject("Folha");
        obj.transform.SetParent(pai, false);
        obj.transform.position = onde;
        obj.transform.localScale = new Vector3(espelho, 1f, 1f);
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = desenhos[0];
        sr.sortingOrder = 10;
        sr.spriteSortPoint = SpriteSortPoint.Pivot;
        folhas.Add(sr);
    }

    private static void Carregar()
    {
        if (carregou)
            return;

        carregou = true;
        deFrente = Cortar(Resources.Load<Texture2D>("Decoracao/PortaDeFrente"), DobradicaDeFrente);
        deLado = Cortar(Resources.Load<Texture2D>("Decoracao/PortaDeLado"), DobradicaDeLado);
        grade = Resources.Load<Sprite>("Itens/Grade");
        gradeDeLado = Resources.Load<Sprite>("Itens/GradeDeLado");
        gradeDeLadoTopo = Resources.Load<Sprite>("Itens/GradeDeLadoTopo");
    }

    // Quadros de 96 x 128, com o pivo na dobradica (o pe da porta).
    private static Sprite[] Cortar(Texture2D folha, Vector2 dobradica)
    {
        if (folha == null)
            return null;

        const int w = 96, h = 128;
        Sprite[] quadros = new Sprite[folha.width / w];
        Vector2 pivo = new Vector2(dobradica.x / w, 1f - dobradica.y / h);

        for (int i = 0; i < quadros.Length; i++)
            quadros[i] = Sprite.Create(folha, new Rect(i * w, 0, w, h), pivo, Pixels, 0, SpriteMeshType.FullRect);

        return quadros;
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

        mexendo = StartCoroutine(quadros != null ? Girar(fechar) : Subir(fechar ? 1f : 0f));
    }

    private void Mostrar(bool sim)
    {
        foreach (SpriteRenderer sr in folhas)
            sr.enabled = sim;
    }

    private IEnumerator Girar(bool fechar)
    {
        int ate = fechar ? quadros.Length - 1 : 0;
        Mostrar(true);

        while (quadro != ate)
        {
            quadro += fechar ? 1 : -1;

            foreach (SpriteRenderer sr in folhas)
                sr.sprite = quadros[quadro];

            yield return new WaitForSeconds(1f / QuadrosPorSegundo);
        }

        if (!fechar && someAberta)
            Mostrar(false);

        mexendo = null;
    }

    private IEnumerator Subir(float ate)
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
