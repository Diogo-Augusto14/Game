using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A porta de uma sala de luta: uma grade de ferro que sobe do trilho no chao e fecha a ponta do
/// corredor. Aberta, so o trilho aparece; fechada, e parede (camada "Wall"): ninguem passa, nem tiro, e
/// os inimigos de fora nao veem quem esta dentro.
///
/// Atravessando um corredor que sobe, a grade aparece de frente; num corredor deitado, de lado.
/// </summary>
public class PortaDaSala : MonoBehaviour
{
    private const float TempoDeSubir = 0.18f;

    private static Sprite grade, gradeDeLado, gradeDeLadoTopo, aberta, abertaDeLado;

    private readonly List<Transform> barras = new List<Transform>();
    private BoxCollider2D colisor;
    private Coroutine mexendo;

    /// <summary>A grade esta em pe.</summary>
    public bool Fechada { get; private set; }

    public static PortaDaSala Criar(PortaDaPlanta planta, Transform pai)
    {
        Carregar();

        Vector2 soma = Vector2.zero;

        foreach (Vector2Int c in planta.celulas)
            soma += c;

        Vector2 meio = soma / Mathf.Max(1, planta.celulas.Count);
        GameObject obj = new GameObject("Porta");
        obj.transform.SetParent(pai, false);
        obj.transform.position = meio;
        obj.layer = Pedreiro.CamadaDaParede;

        PortaDaSala porta = obj.AddComponent<PortaDaSala>();
        porta.colisor = obj.AddComponent<BoxCollider2D>();
        porta.colisor.size = planta.DeFrente ? new Vector2(planta.celulas.Count, 1f) : new Vector2(1f, planta.celulas.Count);
        porta.colisor.enabled = false;

        int maisAlta = int.MinValue;

        foreach (Vector2Int c in planta.celulas)
            maisAlta = Mathf.Max(maisAlta, c.y);

        foreach (Vector2Int c in planta.celulas)
        {
            // O trilho no chao (sempre la).
            GameObject trilho = new GameObject("Trilho");
            trilho.transform.SetParent(obj.transform, false);
            trilho.transform.position = (Vector2)c;
            SpriteRenderer sr = trilho.AddComponent<SpriteRenderer>();
            sr.sprite = planta.DeFrente ? aberta : abertaDeLado;
            sr.sortingOrder = Pedreiro.OrdemDosEnfeites + 1;

            // A grade: o pe na beirada de baixo da celula, cresce pra cima ao fechar.
            GameObject barra = new GameObject("Grade");
            barra.transform.SetParent(obj.transform, false);
            barra.transform.position = (Vector2)c + new Vector2(0f, -0.5f);
            SpriteRenderer desenho = barra.AddComponent<SpriteRenderer>();
            desenho.sprite = planta.DeFrente ? grade : (c.y == maisAlta ? gradeDeLadoTopo : gradeDeLado);
            desenho.sortingOrder = 10;
            desenho.spriteSortPoint = SpriteSortPoint.Pivot;
            barra.transform.localScale = new Vector3(1f, 0f, 1f);
            barra.SetActive(false);
            porta.barras.Add(barra.transform);
        }

        return porta;
    }

    private static void Carregar()
    {
        if (grade != null)
            return;

        grade = Resources.Load<Sprite>("Itens/Grade");
        gradeDeLado = Resources.Load<Sprite>("Itens/GradeDeLado");
        gradeDeLadoTopo = Resources.Load<Sprite>("Itens/GradeDeLadoTopo");
        aberta = Resources.Load<Sprite>("Itens/GradeAberta");
        abertaDeLado = Resources.Load<Sprite>("Itens/GradeAbertaDeLado");
    }

    /// <summary>Sobe a grade (ja e parede no primeiro quadro: ninguem escapa durante a animacao).</summary>
    public void Fechar()
    {
        if (Fechada)
            return;

        Fechada = true;
        colisor.enabled = true;
        Mexer(1f);
    }

    public void Abrir()
    {
        if (!Fechada)
            return;

        Fechada = false;
        colisor.enabled = false;
        Mexer(0f);
    }

    private void Mexer(float altura)
    {
        if (mexendo != null)
            StopCoroutine(mexendo);

        mexendo = StartCoroutine(Subir(altura));
    }

    private IEnumerator Subir(float ate)
    {
        foreach (Transform barra in barras)
            barra.gameObject.SetActive(true);

        float de = barras.Count > 0 ? barras[0].localScale.y : 0f;

        for (float t = 0f; t < TempoDeSubir; t += Time.deltaTime)
        {
            float y = Mathf.Lerp(de, ate, t / TempoDeSubir);

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
