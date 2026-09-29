using UnityEngine;

/// <summary>
/// A dinamite que o goblin joga: voa em arco por cima de tudo (pedra e parede nao
/// seguram) ate o ponto marcado e explode ao cair. Um circulo vermelho no chao mostra
/// onde vai cair durante o voo — da pra sair de baixo.
/// </summary>
public class DinamiteLancada : MonoBehaviour
{
    private Vector2 origem;
    private Vector2 destino;
    private float duracao;
    private float altura;
    private float raio;
    private float danoNoJogador;
    private float danoNosOutros;
    private GameObject dono;
    private float inicio;

    private Transform desenho;
    private SpriteRenderer sombra;

    public static DinamiteLancada Lancar(Vector2 origem, Vector2 destino, float duracao, float raio,
                                         float danoNoJogador, float danoNosOutros, GameObject dono)
    {
        GameObject obj = new GameObject("Dinamite");
        obj.transform.position = origem;

        DinamiteLancada d = obj.AddComponent<DinamiteLancada>();
        d.origem = origem;
        d.destino = destino;
        d.duracao = Mathf.Max(0.1f, duracao);
        d.altura = 0.6f + 0.15f * Vector2.Distance(origem, destino);
        d.raio = raio;
        d.danoNoJogador = danoNoJogador;
        d.danoNosOutros = danoNosOutros;
        d.dono = dono;
        d.inicio = Time.time;

        // O alvo no chao: circulo do tamanho da explosao, ficando mais forte ate cair.
        d.sombra = FormasDaSala.Desenho(null, "Alvo da dinamite", FormasTopDown.Circulo(),
                                        new Color(1f, 0.2f, 0.15f, 0f), destino, Vector2.one * raio * 2f, -5);
        d.sombra.transform.SetParent(obj.transform, true);

        Sprite[] quadros = ArteImportada.Dinamite(InimigoComArte.PixelsDoTinySwords);
        Sprite sprite = quadros != null ? quadros[0] : ArteGerada.Bola();
        Vector2 tamanho = quadros != null ? Vector2.one : Vector2.one * 0.25f;
        SpriteRenderer sr = FormasDaSala.Desenho(obj.transform, "Desenho", sprite,
                                                 quadros != null ? Color.white : new Color(0.85f, 0.2f, 0.25f),
                                                 Vector2.zero, tamanho, 25);
        d.desenho = sr.transform;
        return d;
    }

    private void Update()
    {
        float t = (Time.time - inicio) / duracao;

        if (t >= 1f)
        {
            Explosao.Estourar(destino, raio, danoNosOutros, danoNoJogador, 6f, dono);
            Destroy(gameObject);
            return;
        }

        // Anda em linha reta no chao; o desenho sobe e desce por cima (a parabola).
        Vector2 chao = Vector2.Lerp(origem, destino, t);
        transform.position = chao;
        desenho.localPosition = Vector3.up * (altura * 4f * t * (1f - t));
        desenho.localRotation = Quaternion.Euler(0f, 0f, -540f * t);

        sombra.transform.position = destino;
        sombra.color = new Color(1f, 0.2f, 0.15f, Mathf.Lerp(0.1f, 0.4f, t));
    }
}
