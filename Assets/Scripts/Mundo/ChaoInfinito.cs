using UnityEngine;

/// <summary>
/// Um chao que nao acaba: um ladrilho repetido que sempre cobre a tela, por mais que o jogador ande.
/// E um SpriteRenderer so, em modo ladrilhado, do tamanho da tela mais uma folga, que acompanha a
/// camera pulando de ladrilho em ladrilho (o desenho fica parado no mundo, so a moldura anda).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class ChaoInfinito : MonoBehaviour
{
    [Tooltip("A imagem de um ladrilho do chao (repete nos quatro lados)")]
    [SerializeField] private Texture2D ladrilho;

    [Tooltip("Pixels por unidade: o mesmo do boneco, pra os pixels terem o mesmo tamanho")]
    [SerializeField, Min(1f)] private float pixelsPorUnidade = 20f;

    [SerializeField] private Color tom = new Color(0.85f, 0.85f, 0.9f);

    [Tooltip("Ladrilhos a mais em cada lado, fora da tela")]
    [SerializeField, Min(1)] private int folga = 1;

    private SpriteRenderer desenho;
    private Camera cam;
    private float lado;

    private void Awake()
    {
        desenho = GetComponent<SpriteRenderer>();

        if (ladrilho == null)
            return;

        // FullRect: o modo ladrilhado precisa do retangulo inteiro do sprite.
        desenho.sprite = Sprite.Create(ladrilho, new Rect(0f, 0f, ladrilho.width, ladrilho.height),
                                       new Vector2(0.5f, 0.5f), pixelsPorUnidade, 0, SpriteMeshType.FullRect);
        desenho.drawMode = SpriteDrawMode.Tiled;
        desenho.color = tom;
        desenho.sortingOrder = -100;
        lado = ladrilho.width / pixelsPorUnidade;
    }

    private void LateUpdate()
    {
        if (lado <= 0f)
            return;

        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        // Quantos ladrilhos cobrem a tela, sempre impar (assim o desenho nao pula quando a conta muda de par pra impar).
        float altura = cam.orthographicSize * 2f;
        float largura = altura * cam.aspect;
        int colunas = (Mathf.CeilToInt(largura / lado) + folga * 2) | 1;
        int linhas = (Mathf.CeilToInt(altura / lado) + folga * 2) | 1;
        desenho.size = new Vector2(colunas * lado, linhas * lado);

        // Pula de ladrilho em ladrilho atras da camera.
        Vector3 c = cam.transform.position;
        transform.position = new Vector3(Mathf.Round(c.x / lado) * lado, Mathf.Round(c.y / lado) * lado, 0f);
    }
}
