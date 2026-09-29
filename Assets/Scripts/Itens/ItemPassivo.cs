using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Um item passivo, do tipo que fica no pedestal da sala do item. Pegou, vale pro resto da
/// partida. Cada campo e um ajuste nos numeros do jogador; o que fica em zero (ou em 1, nos
/// multiplicadores) nao mexe em nada.
///
/// Quem aplica e o <see cref="EstatisticasDoJogador"/>: ele guarda os numeros de base e
/// recalcula tudo a cada item, entao a ordem em que os itens foram pegos nao importa.
/// </summary>
[System.Serializable]
public class ItemPassivo
{
    public string nome;

    [Tooltip("Uma frase curta, a que aparece na tela ao pegar (como no Isaac)")]
    public string descricao;

    public Color cor = Color.white;

    [Header("Tiro")]
    public float somaDano;
    public float multiplicaDano = 1f;
    public float somaCadencia;
    public float multiplicaCadencia = 1f;
    public float somaAlcance;
    public float somaVelocidadeDoTiro;
    public float somaTamanhoDaLagrima;
    public int lagrimasExtras;

    [Header("Lagrima especial")]
    [Tooltip("Atravessa inimigos (continua voando depois de acertar)")]
    public bool atravessa;

    [Tooltip("Curva sozinha na direcao do inimigo mais perto")]
    public bool teleguiada;

    [Tooltip("Solta uma lagrima pra tras tambem")]
    public bool paraTras;

    [Tooltip("Muda a cor da lagrima (alfa 0 = nao muda)")]
    public Color corDaLagrima = new Color(0f, 0f, 0f, 0f);

    [Header("Corpo")]
    public float somaVelocidade;
    public float somaVidaMaxima;

    [Header("Coletaveis de brinde")]
    public int moedas;
    public int chaves;
    public int bombas;

    public ItemPassivo(string nome, string descricao, Color cor)
    {
        this.nome = nome;
        this.descricao = descricao;
        this.cor = cor;
    }
}

/// <summary>
/// Todos os itens do jogo. Por enquanto em codigo, pra dar pra montar tudo sem asset;
/// pra criar um item novo, e so acrescentar uma entrada em <see cref="Todos"/>.
/// </summary>
public static class CatalogoDeItens
{
    private static List<ItemPassivo> todos;

    public static IReadOnlyList<ItemPassivo> Todos => todos ?? (todos = Montar());

    private static List<ItemPassivo> Montar()
    {
        return new List<ItemPassivo>
        {
            new ItemPassivo("Cebola Triste", "Chora mais rapido", new Color(0.85f, 0.75f, 0.95f))
                { somaCadencia = 0.7f },

            new ItemPassivo("Seringa Vermelha", "Dano para cima", new Color(0.9f, 0.2f, 0.2f))
                { somaDano = 1.5f },

            new ItemPassivo("Tenis Velho", "Velocidade para cima", new Color(0.3f, 0.6f, 0.95f))
                { somaVelocidade = 1f },

            new ItemPassivo("Olho Triplo", "Tres lagrimas por vez", new Color(0.95f, 0.95f, 0.8f))
                { lagrimasExtras = 2, multiplicaCadencia = 0.7f },

            new ItemPassivo("Olho Gemeo", "Duas lagrimas por vez", new Color(0.7f, 0.9f, 1f))
                { lagrimasExtras = 1, somaDano = 0.3f },

            new ItemPassivo("Luneta", "Alcance e tiro mais rapido", new Color(0.55f, 0.5f, 0.35f))
                { somaAlcance = 3f, somaVelocidadeDoTiro = 3f },

            new ItemPassivo("Coracao Extra", "Vida maxima para cima", new Color(1f, 0.35f, 0.45f))
                { somaVidaMaxima = 20f },

            new ItemPassivo("Lagrima de Chumbo", "Lagrimas grandes e pesadas", new Color(0.45f, 0.45f, 0.5f))
                { multiplicaDano = 1.5f, somaTamanhoDaLagrima = 0.12f, somaVelocidadeDoTiro = -2f, somaVelocidade = -0.4f },

            new ItemPassivo("Cafe", "Tudo mais rapido", new Color(0.45f, 0.28f, 0.15f))
                { somaVelocidade = 0.6f, somaCadencia = 0.4f },

            new ItemPassivo("Saco de Moedas", "Moedas, chave e bombas", new Color(0.95f, 0.8f, 0.2f))
                { moedas = 10, chaves = 1, bombas = 3 },

            new ItemPassivo("Lagrima Fantasma", "Lagrimas atravessam inimigos", new Color(0.85f, 0.95f, 1f))
                { atravessa = true, corDaLagrima = new Color(0.85f, 0.95f, 1f, 0.55f) },

            new ItemPassivo("Bussola Maldita", "Lagrimas perseguem inimigos", new Color(0.7f, 0.35f, 0.95f))
                { teleguiada = true, corDaLagrima = new Color(0.75f, 0.45f, 1f) },

            new ItemPassivo("Olho na Nuca", "Chora pra tras tambem", new Color(0.4f, 0.8f, 0.55f))
                { paraTras = true },

            new ItemPassivo("Pimenta", "Lagrimas de fogo, dano para cima", new Color(1f, 0.35f, 0.1f))
                { multiplicaDano = 1.3f, somaVelocidadeDoTiro = 1f, corDaLagrima = new Color(1f, 0.45f, 0.15f) },
        };
    }

    /// <summary>
    /// Sorteia um item que ainda nao saiu nesta partida. Se todos ja sairam, repete.
    /// Usa o <see cref="Random"/> da Unity, entao respeita a semente do andar.
    /// </summary>
    public static ItemPassivo Sortear(ICollection<ItemPassivo> jaSairam)
    {
        List<ItemPassivo> sobra = new List<ItemPassivo>();

        foreach (ItemPassivo item in Todos)
            if (jaSairam == null || !jaSairam.Contains(item))
                sobra.Add(item);

        if (sobra.Count == 0)
            sobra.AddRange(Todos);

        ItemPassivo sorteado = sobra[Random.Range(0, sobra.Count)];
        jaSairam?.Add(sorteado);
        return sorteado;
    }
}
