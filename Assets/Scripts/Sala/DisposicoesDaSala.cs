using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Os desenhos de obstaculo que uma sala comum pode ter (pedras e espinhos), no estilo do
/// Isaac: cada sala recebe um, e a mesma planta de andar fica com salas bem diferentes.
///
/// Dois jeitos de escrever um desenho:
///   - QUARTO: so um quarto da sala (x de 1 a 6, y de 1 a 3, em ladrilhos a partir do
///     centro), espelhado pros outros tres. Como x e y nunca sao 0, a cruz do meio fica livre.
///   - SALA INTEIRA: 7 linhas de 13 letras, a de cima primeiro ('P' pedra, 'E' espinhos, 'M' bloco de parede que nao quebra (da forma a sala), 'F' buraco no chao,
///     '.' livre). Da pra fazer desenho torto; o gerador ainda espelha na horizontal e na
///     vertical, entao cada um vira ate quatro.
///
/// Todo desenho passa por uma conferencia quando o jogo abre: as quatro portas, o centro
/// (onde cai o premio da sala) e todo ladrilho livre precisam se ligar sem pisar em pedra
/// nem espinho. Desenho que tranca caminho ou deixa um canto fechado (inimigo preso) e
/// descartado com um aviso no Console, entao nenhum desenho quebra a sala.
/// </summary>
public static class DisposicoesDaSala
{
    private struct Peca
    {
        public TipoDeObstaculo Tipo;
        public Vector2Int Celula;

        public Peca(TipoDeObstaculo tipo, int x, int y)
        {
            Tipo = tipo;
            Celula = new Vector2Int(x, y);
        }
    }

    private class Planta
    {
        public string Nome;
        public readonly List<Peca> Pecas = new List<Peca>();
        public bool TemEspinho;

        /// <summary>Tem bloco de parede ou buraco: muda o formato da sala, nao so enfeita.</summary>
        public bool DaForma;
    }

    private const TipoDeObstaculo P = TipoDeObstaculo.Pedra;
    private const TipoDeObstaculo E = TipoDeObstaculo.Espinhos;
    private const TipoDeObstaculo M = TipoDeObstaculo.Muro;
    private const TipoDeObstaculo F = TipoDeObstaculo.Fosso;

    /// <summary>Metade da sala em ladrilhos (sala 13x7: x de -6 a 6, y de -3 a 3).</summary>
    private const int MEIA_LARGURA = 6;
    private const int MEIA_ALTURA = 3;

    private static readonly (string nome, Peca[] pecas)[] Quartos =
    {
        ("Pilares", new[] { new Peca(P, 3, 2) }),
        ("Cantos cheios", new[] { new Peca(P, 6, 3), new Peca(P, 5, 3), new Peca(P, 6, 2) }),
        ("Barras deitadas", new[] { new Peca(P, 2, 2), new Peca(P, 3, 2), new Peca(P, 4, 2) }),
        ("Anel", new[] { new Peca(P, 1, 2), new Peca(P, 2, 2), new Peca(P, 2, 1) }),
        ("Colunas", new[] { new Peca(P, 3, 1), new Peca(P, 3, 2), new Peca(P, 3, 3) }),
        ("Canteiros de espinho", new[] { new Peca(E, 3, 1), new Peca(E, 3, 2), new Peca(E, 4, 1), new Peca(E, 4, 2) }),
        ("Pedra cercada", new[] { new Peca(P, 6, 3), new Peca(E, 5, 3), new Peca(E, 6, 2), new Peca(E, 5, 2) }),
        ("Misto", new[] { new Peca(P, 5, 1), new Peca(P, 5, 2), new Peca(E, 2, 2), new Peca(E, 2, 3) }),
    };

    private static readonly (string nome, string[] linhas)[] Inteiras =
    {
        ("Sala em L", new[]
        {
            ".......MMMMMM",
            ".......MMMMMM",
            ".......MMMMMM",
            ".............",
            ".............",
            ".............",
            ".............",
        }),
        ("Corredor", new[]
        {
            "MMMMM...MMMMM",
            "MMMMM...MMMMM",
            ".............",
            ".............",
            ".............",
            "MMMMM...MMMMM",
            "MMMMM...MMMMM",
        }),
        ("Cruz", new[]
        {
            "MMM.......MMM",
            "MMM.......MMM",
            ".............",
            ".............",
            ".............",
            "MMM.......MMM",
            "MMM.......MMM",
        }),
        ("Cruzeiro", new[]
        {
            "MMMM.....MMMM",
            "MMMM.....MMMM",
            "MMMM.....MMMM",
            ".............",
            "MMMM.....MMMM",
            "MMMM.....MMMM",
            "MMMM.....MMMM",
        }),
        ("Tres naves", new[]
        {
            "....M...M....",
            "....M...M....",
            ".............",
            ".............",
            ".............",
            "....M...M....",
            "....M...M....",
        }),
        ("Diagonal", new[]
        {
            "MMMM.........",
            "MMM..........",
            "MM...........",
            ".............",
            "...........MM",
            "..........MMM",
            ".........MMMM",
        }),
        ("Fossos laterais", new[]
        {
            ".............",
            ".............",
            "..FFF...FFF..",
            "..FFF...FFF..",
            "..FFF...FFF..",
            ".............",
            ".............",
        }),
        ("Fendas", new[]
        {
            "....F...F....",
            "....F...F....",
            ".............",
            ".............",
            ".............",
            "....F...F....",
            "....F...F....",
        }),
        ("Lagoa", new[]
        {
            ".FFF.........",
            ".FFFF........",
            "..FFF........",
            ".............",
            "........FFF..",
            "........FFFF.",
            ".........FF..",
        }),
        ("Ilhas de fosso", new[]
        {
            ".............",
            ".FFF.....FFF.",
            ".F.........F.",
            ".............",
            ".F.........F.",
            ".FFF.....FFF.",
            ".............",
        }),
        ("Muros e fosso", new[]
        {
            "MM.........MM",
            "M..FFF.FFF..M",
            ".............",
            ".............",
            ".............",
            "M..FFF.FFF..M",
            "MM.........MM",
        }),
        ("Xis", new[]
        {
            ".............",
            "..P.......P..",
            "...P.....P...",
            ".............",
            "...P.....P...",
            "..P.......P..",
            ".............",
        }),
        ("Ilha", new[]
        {
            ".............",
            "....PP.PP....",
            "....P...P....",
            ".............",
            "....P...P....",
            "....PP.PP....",
            ".............",
        }),
        ("Blocos", new[]
        {
            ".............",
            ".PP.......PP.",
            ".PP.......PP.",
            ".............",
            ".PP.......PP.",
            ".PP.......PP.",
            ".............",
        }),
        ("Muralhas", new[]
        {
            "...P.....P...",
            "...P.....P...",
            "...E.....E...",
            ".............",
            "...E.....E...",
            "...P.....P...",
            "...P.....P...",
        }),
        ("Tabuleiro", new[]
        {
            ".............",
            ".P...P.P...P.",
            ".............",
            "...P.....P...",
            ".............",
            ".P...P.P...P.",
            ".............",
        }),
        ("Campo de espinhos", new[]
        {
            ".............",
            ".EEEE...EEEE.",
            ".............",
            ".............",
            ".............",
            ".EEEE...EEEE.",
            ".............",
        }),
        ("Cantos em L", new[]
        {
            "PPP.......PPP",
            "P...........P",
            ".............",
            ".............",
            ".............",
            "P...........P",
            "PPP.......PPP",
        }),
        ("Ganchos", new[]
        {
            ".............",
            ".PPP.....PPP.",
            "...P.....P...",
            ".............",
            "...P.....P...",
            ".PPP.....PPP.",
            ".............",
        }),
        ("Losango", new[]
        {
            ".............",
            ".....P.P.....",
            "....P...P....",
            "...P.....P...",
            "....P...P....",
            ".....P.P.....",
            ".............",
        }),
        ("Arena", new[]
        {
            ".PP.PP.PP.PP.",
            ".............",
            "P...........P",
            ".............",
            "P...........P",
            ".............",
            ".PP.PP.PP.PP.",
        }),
        ("Guarda do centro", new[]
        {
            ".............",
            ".....P.P.....",
            "..P.......P..",
            ".............",
            "..P.......P..",
            ".....P.P.....",
            ".............",
        }),
        ("Salao", new[]
        {
            ".............",
            "..P..P.P..P..",
            ".............",
            ".............",
            ".............",
            "..P..P.P..P..",
            ".............",
        }),
        ("Trincheiras", new[]
        {
            ".............",
            "..PEEP.PEEP..",
            ".............",
            ".............",
            ".............",
            "..PEEP.PEEP..",
            ".............",
        }),
        ("Canto quebrado", new[]
        {
            ".............",
            ".PPP.........",
            ".P.......E...",
            ".............",
            "...E.......P.",
            ".........PPP.",
            ".............",
        }),
        ("Escada", new[]
        {
            ".............",
            ".PP..........",
            "...PP........",
            ".............",
            "........PP...",
            "..........PP.",
            ".............",
        }),
        ("Corredor de espinhos", new[]
        {
            ".............",
            "..EEE...EEE..",
            "..E.......E..",
            ".............",
            "..E.......E..",
            "..EEE...EEE..",
            ".............",
        }),
        ("Meia lua", new[]
        {
            ".............",
            "..PPP........",
            ".P...........",
            ".............",
            ".P...........",
            "..PPP........",
            ".............",
        }),
    };

    private static List<Planta> plantas;

    private static List<Planta> Plantas
    {
        get
        {
            if (plantas == null)
                Montar();

            return plantas;
        }
    }

    /// <summary>Quantos desenhos existem (sem contar a sala vazia).</summary>
    public static int Quantidade => Plantas.Count;

    /// <summary>O nome do desenho (pra depurar: aparece no nome do objeto da sala).</summary>
    public static string Nome(int indice) => indice >= 0 && indice < Plantas.Count ? Plantas[indice].Nome : "Vazia";

    /// <summary>Os indices dos desenhos que podem sair, com ou sem espinhos.</summary>
    public static List<int> Permitidos(bool comEspinhos)
    {
        List<int> lista = new List<int>();

        for (int i = 0; i < Plantas.Count; i++)
        {
            if (!comEspinhos && Plantas[i].TemEspinho)
                continue;

            lista.Add(i);

            // Os desenhos que mudam o formato da sala entram em dobro: sao o que mais tira
            // a cara de "mesmo quadrado de sempre".
            if (Plantas[i].DaForma)
                lista.Add(i);
        }

        return lista;
    }

    /// <summary>
    /// Sorteia um desenho e poe na sala. <paramref name="chanceDeVazia"/> e a chance de a
    /// sala ficar sem obstaculo nenhum. Espinhos so a partir de <paramref name="comEspinhos"/>.
    /// </summary>
    public static void Sortear(Sala sala, float chanceDeVazia, bool comEspinhos)
    {
        if (Random.value < chanceDeVazia)
            return;

        List<int> opcoes = Permitidos(comEspinhos);

        if (opcoes.Count > 0)
            Aplicar(sala, opcoes[Random.Range(0, opcoes.Count)], Random.value < 0.5f, Random.value < 0.5f);
    }

    /// <summary>Poe o desenho de numero <paramref name="indice"/>, do jeito que foi escrito.</summary>
    public static void Aplicar(Sala sala, int indice) => Aplicar(sala, indice, false, false);

    /// <summary>Poe o desenho de numero <paramref name="indice"/>, espelhado se pedido.</summary>
    public static void Aplicar(Sala sala, int indice, bool espelharX, bool espelharY)
    {
        if (Plantas.Count == 0 || indice < 0)
            return;

        Planta planta = Plantas[Mathf.Clamp(indice, 0, Plantas.Count - 1)];

        foreach (Peca peca in planta.Pecas)
        {
            Vector2Int c = peca.Celula;
            sala.PorObstaculo(peca.Tipo, new Vector2Int(espelharX ? -c.x : c.x, espelharY ? -c.y : c.y));
        }

        sala.AcabarObstaculos();
    }

    // ---------------- montagem e conferencia ----------------
    private static void Montar()
    {
        plantas = new List<Planta>();

        foreach ((string nome, Peca[] pecas) in Quartos)
        {
            Planta planta = new Planta { Nome = nome };

            foreach (Peca peca in pecas)
                for (int sx = -1; sx <= 1; sx += 2)
                    for (int sy = -1; sy <= 1; sy += 2)
                        planta.Pecas.Add(new Peca(peca.Tipo, peca.Celula.x * sx, peca.Celula.y * sy));

            Guardar(planta);
        }

        foreach ((string nome, string[] linhas) in Inteiras)
        {
            Planta planta = new Planta { Nome = nome };
            bool errado = linhas.Length != MEIA_ALTURA * 2 + 1;

            for (int linha = 0; linha < linhas.Length && !errado; linha++)
            {
                if (linhas[linha].Length != MEIA_LARGURA * 2 + 1)
                {
                    errado = true;
                    break;
                }

                for (int coluna = 0; coluna < linhas[linha].Length; coluna++)
                {
                    char letra = linhas[linha][coluna];

                    if (letra == '.')
                        continue;

                    TipoDeObstaculo tipo = letra == 'E' ? E : letra == 'M' ? M : letra == 'F' ? F : P;
                    planta.Pecas.Add(new Peca(tipo, coluna - MEIA_LARGURA, MEIA_ALTURA - linha));
                }
            }

            if (errado)
            {
                Debug.LogWarning($"[DisposicoesDaSala] desenho '{nome}' precisa ter 7 linhas de 13 letras. Ficou de fora.");
                continue;
            }

            Guardar(planta);
        }
    }

    private static void Guardar(Planta planta)
    {
        foreach (Peca peca in planta.Pecas)
        {
            if (peca.Tipo == E)
                planta.TemEspinho = true;

            if (peca.Tipo == M || peca.Tipo == F)
                planta.DaForma = true;
        }

        if (Jogavel(planta))
            plantas.Add(planta);
        else
            Debug.LogWarning($"[DisposicoesDaSala] desenho '{planta.Nome}' tranca uma porta ou fecha um canto. Ficou de fora.");
    }

    /// <summary>
    /// Enchente a partir do centro, andando so por ladrilho livre (espinho conta como
    /// bloqueado, pra ninguem ser obrigado a se machucar): precisa chegar nas quatro portas
    /// (e no ladrilho logo dentro de cada uma, onde o jogador aparece) e em todo ladrilho livre.
    /// </summary>
    private static bool Jogavel(Planta planta)
    {
        HashSet<Vector2Int> ocupadas = new HashSet<Vector2Int>();

        foreach (Peca peca in planta.Pecas)
            ocupadas.Add(peca.Celula);

        Vector2Int[] obrigatorias =
        {
            Vector2Int.zero,
            new Vector2Int(0, MEIA_ALTURA), new Vector2Int(0, MEIA_ALTURA - 1),
            new Vector2Int(0, -MEIA_ALTURA), new Vector2Int(0, -MEIA_ALTURA + 1),
            new Vector2Int(-MEIA_LARGURA, 0), new Vector2Int(-MEIA_LARGURA + 1, 0),
            new Vector2Int(MEIA_LARGURA, 0), new Vector2Int(MEIA_LARGURA - 1, 0),
        };

        foreach (Vector2Int celula in obrigatorias)
            if (ocupadas.Contains(celula))
                return false;

        HashSet<Vector2Int> vistas = new HashSet<Vector2Int> { Vector2Int.zero };
        Queue<Vector2Int> fila = new Queue<Vector2Int>();
        fila.Enqueue(Vector2Int.zero);
        Vector2Int[] passos = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (fila.Count > 0)
        {
            Vector2Int atual = fila.Dequeue();

            foreach (Vector2Int passo in passos)
            {
                Vector2Int proxima = atual + passo;

                if (Mathf.Abs(proxima.x) > MEIA_LARGURA || Mathf.Abs(proxima.y) > MEIA_ALTURA)
                    continue;

                if (ocupadas.Contains(proxima) || !vistas.Add(proxima))
                    continue;

                fila.Enqueue(proxima);
            }
        }

        int livres = (MEIA_LARGURA * 2 + 1) * (MEIA_ALTURA * 2 + 1) - ocupadas.Count;
        return vistas.Count == livres;
    }
}
