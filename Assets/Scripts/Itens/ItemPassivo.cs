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

    [Tooltip("O efeito explicado por inteiro: aparece ao passar o mouse no item da lateral (e na loja)")]
    public string Descricao;

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

    [Header("Flecha")]
    [Tooltip("Tipo de flecha que o item da (fica na aljava; Q/LT troca). Normal = nenhum")]
    public TipoDeFlecha flecha;

    [Header("Corpo")]
    public float somaVelocidade;
    public float somaVidaMaxima;

    [Header("Efeitos especiais (EfeitosDosItens)")]
    [Tooltip("Uma vez por partida: em vez de morrer, volta com metade da vida")]
    public bool renasce;

    [Tooltip("Bloqueia o primeiro golpe de cada sala")]
    public bool escudoPorSala;

    [Tooltip("A cada tantos inimigos derrotados, cura meio coracao (0 = nao cura)")]
    public int inimigosParaCurar;

    [Tooltip("Dano nos inimigos em volta sempre que o jogador leva dano (0 = nada)")]
    public float danoDeEspinhos;

    [Tooltip("Raio em que moedas, chaves, bombas e coracoes vem sozinhos ate o jogador")]
    public float raioDoIma;

    [Tooltip("Multiplica a chance de inimigos e salas soltarem premio")]
    public float multiplicaSorte = 1f;

    [Tooltip("Fracao do preco que a loja desconta (0.3 = 30% mais barato)")]
    public float descontoNaLoja;

    [Tooltip("Dano extra (0.5 = +50%) enquanto a vida estiver baixa")]
    public float furia;

    [Tooltip("Segundos a mais de invencibilidade depois de levar dano")]
    public float somaInvencibilidade;

    [Tooltip("Orbes que giram em volta do jogador, bloqueiam tiros e ferem inimigos")]
    public int orbes;

    [Tooltip("Multiplica raio e dano das bombas do jogador")]
    public float multiplicaBomba = 1f;

    [Tooltip("Vida recuperada ao limpar uma sala (20 = um coracao)")]
    public float curaAoLimparSala;

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

    /// <summary>O efeito completo; item sem <see cref="Descricao"/> usa a frase curta.</summary>
    public string Efeito => string.IsNullOrEmpty(Descricao) ? descricao : Descricao;
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
        List<ItemPassivo> lista = new List<ItemPassivo>
        {
            new ItemPassivo("Cebola Triste", "Chora mais rápido", new Color(0.85f, 0.75f, 0.95f))
                {
                    Descricao = "Atira mais rápido: +0.7 lágrima por segundo.",
                    somaCadencia = 0.7f
                },

            new ItemPassivo("Seringa Vermelha", "Dano para cima", new Color(0.9f, 0.2f, 0.2f))
                {
                    Descricao = "Cada lágrima causa +1.5 de dano.",
                    somaDano = 1.5f
                },

            new ItemPassivo("Tênis Velho", "Velocidade para cima", new Color(0.3f, 0.6f, 0.95f))
                {
                    Descricao = "Anda mais rápido: +1 de velocidade.",
                    somaVelocidade = 1f
                },

            new ItemPassivo("Olho Triplo", "Três lágrimas por vez", new Color(0.95f, 0.95f, 0.8f))
                {
                    Descricao = "Solta três lágrimas de uma vez, mas atira 30% mais devagar.",
                    lagrimasExtras = 2, multiplicaCadencia = 0.7f
                },

            new ItemPassivo("Olho Gêmeo", "Duas lágrimas por vez", new Color(0.7f, 0.9f, 1f))
                {
                    Descricao = "Solta duas lágrimas de uma vez e ganha +0.3 de dano.",
                    lagrimasExtras = 1, somaDano = 0.3f
                },

            new ItemPassivo("Luneta", "Alcance e tiro mais rápido", new Color(0.55f, 0.5f, 0.35f))
                {
                    Descricao = "Lágrimas vão mais longe (+3 de alcance) e voam mais rápido.",
                    somaAlcance = 3f, somaVelocidadeDoTiro = 3f
                },

            new ItemPassivo("Coração Extra", "Vida máxima para cima", new Color(1f, 0.35f, 0.45f))
                {
                    Descricao = "Ganha um coração a mais de vida máxima, já cheio.",
                    somaVidaMaxima = 20f
                },

            new ItemPassivo("Lágrima de Chumbo", "Lágrimas grandes e pesadas", new Color(0.45f, 0.45f, 0.5f))
                {
                    Descricao = "Dano x1.5 e lágrimas maiores, mas o tiro e o herói ficam mais lentos.",
                    multiplicaDano = 1.5f, somaTamanhoDaLagrima = 0.12f, somaVelocidadeDoTiro = -2f, somaVelocidade = -0.4f
                },

            new ItemPassivo("Café", "Tudo mais rápido", new Color(0.45f, 0.28f, 0.15f))
                {
                    Descricao = "Anda mais rápido (+0.6) e atira mais rápido (+0.4 lágrima por segundo).",
                    somaVelocidade = 0.6f, somaCadencia = 0.4f
                },

            new ItemPassivo("Saco de Moedas", "Moedas, chave e bombas", new Color(0.95f, 0.8f, 0.2f))
                {
                    Descricao = "Ganha na hora 10 moedas, 1 chave e 3 bombas.",
                    moedas = 10, chaves = 1, bombas = 3
                },

            new ItemPassivo("Lágrima Fantasma", "Lágrimas atravessam inimigos", new Color(0.85f, 0.95f, 1f))
                {
                    Descricao = "As lágrimas atravessam os inimigos e acertam quem estiver atrás.",
                    atravessa = true, corDaLagrima = new Color(0.85f, 0.95f, 1f, 0.55f)
                },

            new ItemPassivo("Bússola Maldita", "Lágrimas perseguem inimigos", new Color(0.7f, 0.35f, 0.95f))
                {
                    Descricao = "As lágrimas fazem curva sozinhas atrás do inimigo mais perto.",
                    teleguiada = true, corDaLagrima = new Color(0.75f, 0.45f, 1f)
                },

            new ItemPassivo("Olho na Nuca", "Chora pra trás também", new Color(0.4f, 0.8f, 0.55f))
                {
                    Descricao = "Cada disparo solta também uma lágrima para trás.",
                    paraTras = true
                },

            new ItemPassivo("Pimenta", "Lágrimas de fogo, dano para cima", new Color(1f, 0.35f, 0.1f))
                {
                    Descricao = "Lágrimas de fogo: dano x1.3 e tiro um pouco mais rápido.",
                    multiplicaDano = 1.3f, somaVelocidadeDoTiro = 1f, corDaLagrima = new Color(1f, 0.45f, 0.15f)
                },

            // ---------------- efeitos especiais (EfeitosDosItens) ----------------
            new ItemPassivo("Pena da Fênix", "Uma segunda chance", new Color(1f, 0.45f, 0.3f))
                {
                    Descricao = "Uma vez por partida: quando a vida acabaria, você renasce com metade da vida.",
                    renasce = true
                },

            new ItemPassivo("Escudo Sagrado", "Bloqueia o primeiro golpe", new Color(0.95f, 0.8f, 0.35f))
                {
                    Descricao = "O primeiro golpe que você levaria em cada sala é bloqueado. O escudo volta ao entrar em outra sala.",
                    escudoPorSala = true
                },

            new ItemPassivo("Sangue de Vampiro", "Matar cura", new Color(0.75f, 0.1f, 0.2f))
                {
                    Descricao = "A cada 5 inimigos derrotados, recupera meio coração.",
                    inimigosParaCurar = 5
                },

            new ItemPassivo("Prego Enferrujado", "Quem bate, apanha", new Color(0.7f, 0.7f, 0.75f))
                {
                    Descricao = "Sempre que você leva dano, espinhos saem de você e ferem os inimigos em volta.",
                    danoDeEspinhos = 12f
                },

            new ItemPassivo("Pedra-Ímã", "Coletáveis vêm até você", new Color(1f, 0.55f, 0.15f))
                {
                    Descricao = "Moedas, chaves, bombas e corações perto de você são puxados sozinhos.",
                    raioDoIma = 3.5f
                },

            new ItemPassivo("Amuleto da Sorte", "Mais prêmios", new Color(0.4f, 0.85f, 0.35f))
                {
                    Descricao = "Inimigos e salas limpas soltam prêmios com bem mais frequência.",
                    multiplicaSorte = 1.75f
                },

            new ItemPassivo("Bolsa do Mercador", "Loja mais barata", new Color(1f, 0.85f, 0.3f))
                {
                    Descricao = "Tudo na loja custa 35% menos. Vem com 5 moedas.",
                    descontoNaLoja = 0.35f, moedas = 5
                },

            new ItemPassivo("Brasa da Fúria", "Mais forte ferido", new Color(1f, 0.5f, 0.25f))
                {
                    Descricao = "Com um coração de vida ou menos, suas flechas causam 60% mais dano.",
                    furia = 0.6f
                },

            new ItemPassivo("Elixir de Névoa", "Mais tempo invencível", new Color(0.6f, 0.8f, 1f))
                {
                    Descricao = "Depois de levar dano, fica invencível por bem mais tempo.",
                    somaInvencibilidade = 0.7f
                },

            new ItemPassivo("Orbe Guardião", "Um orbe te protege", new Color(0.3f, 0.75f, 1f))
                {
                    Descricao = "Um orbe gira em volta de você: bloqueia tiros inimigos e fere quem encostar.",
                    orbes = 1
                },

            new ItemPassivo("Barril de Pólvora", "Bombas mais fortes", new Color(0.55f, 0.35f, 0.2f))
                {
                    Descricao = "Suas bombas explodem numa área 50% maior e com 50% mais dano. Vem com 2 bombas.",
                    multiplicaBomba = 1.5f, bombas = 2
                },

            new ItemPassivo("Carne Assada", "Cura a cada sala", new Color(0.85f, 0.4f, 0.35f))
                {
                    Descricao = "Ao limpar uma sala de inimigos, recupera meio coração.",
                    curaAoLimparSala = 10f
                },

            new ItemPassivo("Pacto de Sangue", "Poder por um preço", new Color(0.5f, 0.05f, 0.1f))
                {
                    Descricao = "Suas flechas causam 60% mais dano, mas você perde um coração de vida máxima.",
                    multiplicaDano = 1.6f, somaVidaMaxima = -20f
                },
        };

        // Um item pra cada flecha especial, com o nome e o efeito dela (ver CatalogoDeFlechas).
        foreach (DefinicaoDeFlecha f in CatalogoDeFlechas.Especiais())
            lista.Add(new ItemPassivo(f.nome, f.descricao, f.cor) { flecha = f.tipo });

        return lista;
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
