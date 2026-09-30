using UnityEngine;

/// <summary>
/// Feedback do disparo dos herois de espada e machado: a cada golpe, o brilho de lamina do
/// Tiny RPG pisca na ponta da arma, na direcao do tiro, com a cor da onda de corte. A onda
/// em si sai do <see cref="AtiradorTopDown"/> e o golpe do corpo (combo de ataques) e do
/// <see cref="ArqueiroDoJogador"/>.
///
/// <see cref="Herois.Aplicar"/> poe este componente nos herois de espada e tira dos outros.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AtiradorTopDown))]
public class GolpeDeEspada : MonoBehaviour
{
    /// <summary>Pixels por unidade do brilho (uns 13 px de largura).</summary>
    private const float PixelsDoBrilho = 18f;

    /// <summary>Distancia do centro do heroi ate a ponta da arma.</summary>
    private const float AlcanceDaLamina = 0.55f;

    private AtiradorTopDown atirador;
    private SpriteRenderer corpo;
    private Color cor = Color.white;

    public void Configurar(Color corDaOnda)
    {
        cor = corDaOnda;
    }

    private void Awake()
    {
        atirador = GetComponent<AtiradorTopDown>();
        corpo = GetComponent<SpriteRenderer>();
    }

    private void OnEnable() => atirador.AoAtirar += Golpeou;

    private void OnDisable() => atirador.AoAtirar -= Golpeou;

    private void Golpeou(Vector2 direcao)
    {
        Vector2 ponta = (Vector2)transform.position + direcao.normalized * AlcanceDaLamina;
        int ordem = corpo != null ? corpo.sortingOrder + 2 : 12;

        EfeitoDeQuadros brilho = EfeitoDeQuadros.Criar(ArteImportada.Brilho(PixelsDoBrilho), 20f, ponta, ordem);

        if (brilho != null)
            brilho.GetComponent<SpriteRenderer>().color = Color.Lerp(cor, Color.white, 0.4f);
    }
}
