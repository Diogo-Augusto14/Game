using UnityEngine;

/// <summary>
/// O vortice da saida: abre onde morreu o ultimo inimigo do andar, gira no chao e, quando o jogador
/// pisa nele, leva pro proximo andar (o <see cref="GeradorDoAndar"/> cuida da troca).
/// Monte com <see cref="Criar"/>.
/// </summary>
[DisallowMultipleComponent]
public class Saida : MonoBehaviour
{
    /// <summary>Quao perto do centro o jogador precisa pisar, em unidades.</summary>
    private const float Raio = 0.7f;

    /// <summary>Segundos crescendo do nada ate o tamanho cheio, ao abrir.</summary>
    private const float Abrindo = 0.4f;

    private GeradorDoAndar gerador;
    private SpriteRenderer desenho;
    private Sprite[] quadros;
    private float quadrosPorSegundo;
    private float abriu;
    private bool usada;

    public static Saida Criar(Transform pai, Vector2 onde, Sprite[] quadros, float quadrosPorSegundo, GeradorDoAndar gerador)
    {
        GameObject obj = new GameObject("Saida");
        obj.transform.SetParent(pai, false);
        obj.transform.position = onde;

        Saida saida = obj.AddComponent<Saida>();
        saida.gerador = gerador;
        saida.quadros = quadros;
        saida.quadrosPorSegundo = quadrosPorSegundo;
        saida.abriu = Time.time;
        saida.desenho = obj.AddComponent<SpriteRenderer>();
        saida.desenho.sortingOrder = Pedreiro.OrdemDosEnfeites + 1;
        Iluminacao.Brilhar(saida.desenho);
        Iluminacao.Luz(obj.transform, Vector2.zero, new Color(0.75f, 0.5f, 1f), 5f, 1.3f, 0.08f);

        if (quadros.Length > 0)
            saida.desenho.sprite = quadros[0];

        return saida;
    }

    private void Update()
    {
        if (quadros.Length > 0)
            desenho.sprite = quadros[Mathf.FloorToInt(Time.time * quadrosPorSegundo) % quadros.Length];

        // Cresce ao abrir e depois pulsa de leve, pra chamar atencao no chao.
        float tamanho = Mathf.Clamp01((Time.time - abriu) / Abrindo) * (1f + Mathf.Sin(Time.time * 3f) * 0.06f);
        transform.localScale = new Vector3(tamanho, tamanho, 1f);

        Transform jogador = gerador.Jogador;

        if (usada || jogador == null || Time.time - abriu < Abrindo)
            return;

        if (Vector2.Distance(jogador.position, transform.position) < Raio)
        {
            usada = true;
            gerador.ProximoAndar();
        }
    }
}
