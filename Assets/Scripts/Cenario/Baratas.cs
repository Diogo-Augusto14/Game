using System.Collections.Generic;
using UnityEngine;

/// <summary>Um bando de baratas que anda a esmo e foge do jogador (so enfeite, como no jogo antigo).</summary>
public class Baratas : MonoBehaviour
{
    private SpriteRenderer desenho;
    private Sprite[] quadros;
    private Vector2 rumo;
    private float trocaEm;
    private Transform jogador;
    private HashSet<Vector2Int> chao;

    public static Baratas Criar(Vector2 onde, Transform pai, HashSet<Vector2Int> chao)
    {
        GameObject obj = new GameObject("Baratas");
        obj.transform.SetParent(pai, false);
        obj.transform.position = onde;
        Baratas b = obj.AddComponent<Baratas>();
        b.desenho = obj.AddComponent<SpriteRenderer>();
        b.desenho.sortingOrder = Pedreiro.OrdemDosEnfeites + 2;
        b.quadros = ArteDoAntigo.Baratas;
        b.chao = chao;
        return b;
    }

    private void Update()
    {
        if (jogador == null)
        {
            GameObject j = GameObject.FindWithTag("Player");
            jogador = j != null ? j.transform : null;
        }

        float rapidez = 0.8f;

        if (jogador != null && Vector2.Distance(jogador.position, transform.position) < 3f)
        {
            rumo = ((Vector2)(transform.position - jogador.position)).normalized;
            rapidez = 3.5f;
        }
        else if (Time.time >= trocaEm)
        {
            trocaEm = Time.time + Random.Range(0.6f, 2f);
            rumo = Random.value < 0.3f ? Vector2.zero : Random.insideUnitCircle.normalized;
        }

        Vector2 proximo = (Vector2)transform.position + rumo * rapidez * Time.deltaTime;

        if (chao == null || chao.Contains(MapaDeCaminhos.Celula(proximo)))
            transform.position = proximo;
        else
            rumo = -rumo;

        if (quadros.Length > 0)
            desenho.sprite = quadros[rumo.sqrMagnitude > 0.01f ? Mathf.FloorToInt(Time.time * 12f) % quadros.Length : 0];

        if (Mathf.Abs(rumo.x) > 0.1f)
            desenho.flipX = rumo.x < 0f;
    }
}
