using UnityEngine;

/// <summary>
/// O vortice da saida: abre onde morreu o ultimo inimigo do andar, gira no chao e, quando o jogador
/// pisa nele, leva pro proximo andar (o <see cref="GeradorDoAndar"/> cuida da troca).
/// Na area segura, um portal pode levar pra um tema (a escolha do caminho): ele fica da cor do tema,
/// com o nome em cima. Monte com <see cref="Criar"/>.
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
    private int tema = -1;

    /// <param name="tema">O tema do mundo pra onde leva (-1 = o proximo andar, sem escolha).</param>
    public static Saida Criar(Transform pai, Vector2 onde, Sprite[] quadros, float quadrosPorSegundo, GeradorDoAndar gerador, int tema = -1)
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
        saida.tema = tema;
        Iluminacao.Brilhar(saida.desenho);

        Color luz = new Color(0.75f, 0.5f, 1f);

        if (tema >= 0)
        {
            luz = GeradorDoAndar.CorDoTema(tema);
            saida.desenho.color = luz;
            Placa(obj.transform, GeradorDoAndar.NomeDoTema(tema), GeradorDoAndar.ResumoDoTema(tema), luz);
        }

        Iluminacao.Luz(obj.transform, Vector2.zero, luz, 5f, 1.3f, 0.08f);

        if (quadros.Length > 0)
            saida.desenho.sprite = quadros[0];

        return saida;
    }

    // O nome do mundo em cima do portal e, embaixo dele, o que tem la (fora da escala do vortice).
    private static void Placa(Transform portal, string nome, string resumo, Color cor)
    {
        GameObject placa = new GameObject("Placa");
        placa.transform.SetParent(portal.parent, false);
        placa.transform.position = portal.position + new Vector3(0f, 1.9f, 0f);
        Texto(placa.transform, nome, 0.1f, new Color(Mathf.Lerp(cor.r, 1f, 0.5f), Mathf.Lerp(cor.g, 1f, 0.5f), Mathf.Lerp(cor.b, 1f, 0.5f)), 0f);

        if (!string.IsNullOrEmpty(resumo))
            Texto(placa.transform, resumo, 0.06f, new Color(0.92f, 0.9f, 0.85f), -0.55f);
    }

    private static void Texto(Transform pai, string conteudo, float tamanho, Color cor, float altura)
    {
        TextMesh texto = new GameObject("Texto").AddComponent<TextMesh>();
        texto.transform.SetParent(pai, false);
        texto.transform.localPosition = new Vector3(0f, altura, 0f);
        texto.text = conteudo;
        texto.font = FonteDoJogo.Texto;
        texto.fontSize = 64;
        texto.characterSize = tamanho;
        texto.anchor = TextAnchor.MiddleCenter;
        texto.alignment = TextAlignment.Center;
        texto.color = cor;

        MeshRenderer desenho = texto.GetComponent<MeshRenderer>();

        if (texto.font != null)
            desenho.sharedMaterial = texto.font.material;

        desenho.sortingOrder = 30;
        Iluminacao.Brilhar(desenho);
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
            gerador.ProximoAndar(tema);
        }
    }
}
