using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A decoracao das salas, com as pecas dos pacotes (Resources/Decoracao, ver Ferramentas/Temas):
///
///   a parede de cima de cada sala (nos mundos com paredes): tochas acesas a cada 4 celulas e, entre elas,
///   estandartes, retratos, correntes e esqueletos acorrentados (Old Prison) ou estandartes e correntes (Cripta);
///   cantos montados encostados nas paredes, cada um uma cena (<see cref="Prisao"/>): o deposito (toneis,
///   barris e caixotes que quebram, sacos, baldes), a tortura (dama de ferro, guilhotina, tronco, correntes e
///   ossos), a cela (esqueleto acorrentado, gaiola, balde, papel), a mesa (cadeiras, caneca e vela acesa) e o
///   ossario (esqueleto, ossos e velas, ou a chama magica azul);
///   gaiolas penduradas e correntes caindo do teto;
///   na Cripta, caixoes, estatuas, cruzes, candelabros, bancos, livros e montes de velas; nas Profundezas,
///   estatuas douradas, cristais, candelabros, ouro, potes que quebram e a espada fincada no chao;
///   as salas especiais mobiliadas com o pacote Village (<see cref="Mobiliar"/>): a loja com o mercador atras
///   da mesa, armarios de pocoes, prateleiras e barris; o tesouro com armaduras, cabides de armas, baus e
///   escudos; o altar com estantes de livros, escrivaninha, globo, quadros e trofeus; o desafio com armas;
///   potes que quebram (com os cacos do Village) e teias nos cantos de cima;
///   miudezas pelo chao (ossos, pedras, papel, correntes), mais perto das paredes;
///   nas Profundezas, rochas, pilares e estatuas saindo do vazio em volta das salas;
///   e, nas salas de luta da Cripta, o lancador de fogo na parede (<see cref="LancadorDeFogo"/>).
///
/// As tochas, velas, candelabros, cristais e chamas tem luz (<see cref="Iluminacao"/>): o resto do andar e escuro.
///
/// Nada fica nos corredores nem na frente das portas; o que segura gente sai do mapa de caminhos.
/// </summary>
public static class Decoracao
{
    private static readonly Dictionary<string, Sprite[]> grupos = new Dictionary<string, Sprite[]>();
    private static Sprite[] tocha, chama, velas;

    private static Sprite[] Grupo(string nome)
    {
        if (!grupos.TryGetValue(nome, out Sprite[] sprites))
        {
            sprites = Resources.LoadAll<Sprite>("Decoracao/" + nome);
            grupos[nome] = sprites;
        }

        return sprites;
    }

    private static Sprite Um(string nome)
    {
        Sprite[] s = Grupo(nome);
        return s.Length > 0 ? s[Random.Range(0, s.Length)] : null;
    }

    private static void CarregarAnimadas()
    {
        if (tocha != null)
            return;

        tocha = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Decoracao/Tocha"), new Vector2Int(64, 64), 32f);
        chama = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Decoracao/ChamaMagica"), new Vector2Int(32, 64), 32f);
        velas = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Decoracao/Velas"), new Vector2Int(64, 64), 32f);
    }

    /// <summary>Uma sala sendo decorada: onde ainda cabe coisa e o que ja foi usado.</summary>
    private class Quarto
    {
        public SalaDaPlanta sala;
        public HashSet<Vector2Int> planta, livre, chao;
        public List<Vector2> usados;
        public Transform pai;
        public float longeDoMeio;
        public bool flutuante;

        public bool Parede(Vector2Int c) => !planta.Contains(c);

        public bool NaBeirada(Vector2Int c)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (Parede(c + new Vector2Int(dx, dy)))
                        return true;

            return false;
        }

        // Uma celula livre encostada numa parede (so a de cima, se pedir), longe dos outros cantos.
        public bool Encostada(float espaco, bool soEmCima, out Vector2Int onde)
        {
            List<Vector2Int> servem = new List<Vector2Int>();

            foreach (Vector2Int c in sala.celulas)
            {
                if (!livre.Contains(c) || Vector2.Distance(c, sala.Meio) < longeDoMeio)
                    continue;

                bool emCima = Parede(c + Vector2Int.up);
                bool doLado = Parede(c + Vector2Int.left) || Parede(c + Vector2Int.right);

                if (!(emCima || (!soEmCima && doLado)) || (flutuante && !NaBeirada(c)))
                    continue;

                if (usados.TrueForAll(u => Vector2.Distance(u, c) >= espaco))
                    servem.Add(c);
            }

            onde = servem.Count > 0 ? servem[Random.Range(0, servem.Count)] : default;

            if (servem.Count > 0)
                usados.Add(onde);

            return servem.Count > 0;
        }

        // Uma celula livre a ate "raio" de "perto" (pra montar a cena em volta do canto).
        public bool Perto(Vector2Int perto, int raio, out Vector2Int onde)
        {
            List<Vector2Int> servem = new List<Vector2Int>();

            for (int dx = -raio; dx <= raio; dx++)
            {
                for (int dy = -raio; dy <= raio; dy++)
                {
                    Vector2Int c = perto + new Vector2Int(dx, dy);

                    if ((dx != 0 || dy != 0) && livre.Contains(c) && sala.celulas.Contains(c))
                        servem.Add(c);
                }
            }

            onde = servem.Count > 0 ? servem[Random.Range(0, servem.Count)] : default;
            return servem.Count > 0;
        }

        public void Tomar(Vector2Int c, bool seguraGente)
        {
            livre.Remove(c);

            if (seguraGente)
                chao.Remove(c);
        }
    }

    /// <param name="mundo">1 Porao, 2 Catacumbas, 3 Cripta, 4 Profundezas.</param>
    /// <param name="chao">O chao andavel (o mesmo do mapa de caminhos: o que segura gente sai dele).</param>
    public static void Espalhar(int mundo, EstiloDeLadrilhos estilo, PlantaDeSalas.Planta salas, HashSet<Vector2Int> chao, Transform pai)
    {
        CarregarAnimadas();
        Transform grupo = new GameObject("Decoracao").transform;
        grupo.SetParent(pai, false);

        // Longe dos corredores e da frente das portas.
        HashSet<Vector2Int> livre = new HashSet<Vector2Int>(chao);

        foreach (Vector2Int c in salas.corredores)
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    livre.Remove(c + new Vector2Int(dx, dy));

        bool prisao = mundo <= 2, cripta = mundo == 3, profundezas = mundo == 4;
        List<Vector2> usados = new List<Vector2>();

        foreach (SalaDaPlanta sala in salas.salas)
        {
            // O meio das salas especiais e do comeco fica pro que elas tem (loja, item, o jogador).
            Quarto q = new Quarto
            {
                sala = sala, planta = salas.chao, livre = livre, chao = chao, usados = usados, pai = grupo,
                longeDoMeio = sala.DeLuta ? 0f : 3f, flutuante = estilo.flutuante,
            };

            if (!estilo.flutuante)
                NaParedeDeCima(sala, salas.chao, grupo, mundo);

            // As salas especiais ganham a mobilia do Village (loja, tesouro, biblioteca do altar, desafio).
            if (!Mobiliar(q))
            {
                if (prisao)
                    Prisao(q);
                else if (cripta)
                    Cripta(q);
                else if (profundezas)
                    Profundezas(q);
            }

            Miudezas(q, mundo);

            // O lancador de fogo: numa sala de luta da Cripta (as vezes), no meio da parede de cima.
            if (cripta && sala.DeLuta && Random.value < 0.55f)
                Lancador(sala, salas, grupo);
        }

        if (profundezas)
            NoVazio(salas.chao, grupo);
    }

    private static string Sortear(params string[] opcoes) => opcoes[Random.Range(0, opcoes.Length)];

    private static Vector2 Torto(float quanto = 0.25f) => new Vector2(Random.Range(-quanto, quanto), Random.Range(-quanto * 0.8f, quanto * 0.8f));

    // ------------------------------------------------------------------ Old Prison (Porao e Catacumbas)

    private enum Cena { Deposito, Tortura, Cela, Mesa, Ossario }

    private static void Prisao(Quarto q)
    {
        SalaDaPlanta sala = q.sala;
        int quantas = sala.DeLuta ? Random.Range(3, 5) : (sala.tipo == TipoDeSala.Loja ? 1 : Random.Range(2, 4));
        List<Cena> cenas = new List<Cena> { Cena.Deposito, Cena.Deposito, Cena.Tortura, Cena.Cela, Cena.Cela, Cena.Mesa, Cena.Ossario, Cena.Ossario };

        for (int i = 0; i < quantas && cenas.Count > 0; i++)
        {
            Cena cena = cenas[Random.Range(0, cenas.Count)];
            cenas.RemoveAll(x => x == cena);

            if (!q.Encostada(4f, cena == Cena.Cela, out Vector2Int c))
                break;

            switch (cena)
            {
                case Cena.Deposito: Deposito(q, c); break;
                case Cena.Tortura: Tortura(q, c); break;
                case Cena.Cela: Cela(q, c); break;
                case Cena.Mesa: MesaPosta(q, c); break;
                default: Ossario(q, c); break;
            }
        }

        // Um candelabro aceso num canto.
        if (sala.DeLuta && Random.value < 0.6f && q.Encostada(3f, false, out Vector2Int lugar))
        {
            GameObject cand = Pequena(Um("Prisao/Candelabro"), (Vector2)lugar + Vector2.down * 0.3f, q.pai, 10);
            AcenderEmCima(cand, Iluminacao.Vela, 4f, 0.9f);
            q.Tomar(lugar, false);
        }

        // Uma gaiola pendurada perto da parede de cima.
        if (Random.value < 0.4f && q.Encostada(3f, true, out Vector2Int gaiola))
        {
            Vector2Int baixo = gaiola + Vector2Int.down;
            Vector2Int onde = q.livre.Contains(baixo) ? baixo : gaiola;
            Pequena(Um("Prisao/Gaiola"), (Vector2)onde + new Vector2(0f, -0.2f), q.pai, 10);
        }

        // Correntes caindo do teto, na frente da parede de cima.
        if (Random.value < 0.35f && q.Encostada(4f, true, out Vector2Int teto))
            Pequena(Um("Prisao/CorrenteDoTeto"), (Vector2)teto + new Vector2(0f, 1.2f), q.pai, Pedreiro.OrdemDasParedes + 1);
    }

    // Toneis ou barris (que quebram), caixotes, sacos e baldes.
    private static void Deposito(Quarto q, Vector2Int c)
    {
        if (Random.value < 0.5f && Grupo("Prisao/Tonel").Length > 0)
            Solida(q, Um("Prisao/Tonel"), c, 1.5f);
        else
            Quebra(q, Um(Random.value < 0.3f ? "Prisao/BarrilDeMoedas" : "Prisao/Barril"), c);

        int mais = Random.Range(3, 6);

        for (int i = 0; i < mais; i++)
        {
            if (!q.Perto(c, i < 2 ? 1 : 2, out Vector2Int p))
                break;

            float qual = Random.value;

            if (qual < 0.4f)
                Quebra(q, Um("Prisao/Barril"), p);
            else if (qual < 0.6f)
                Quebra(q, Um("Prisao/Caixote"), p);
            else if (qual < 0.7f)
            {
                Quebravel.Pote((Vector2)p + Vector2.down * 0.4f + Torto(0.15f), q.pai);
                q.Tomar(p, true);
            }
            else if (qual < 0.75f)
                Solta(q, Um("Prisao/Saco"), p, 10);
            else if (qual < 0.88f)
                Solta(q, Um("Prisao/Balde"), p, 10);
            else
                Solta(q, Um("Prisao/BarrilCaido"), p, 10);
        }
    }

    // Uma maquina de tortura, com correntes, ossos, baldes e bolas de espinhos em volta.
    private static void Tortura(Quarto q, Vector2Int c)
    {
        string qual = Sortear("Prisao/DamaDeFerro", "Prisao/DamaDeFerro", "Prisao/Guilhotina", "Prisao/Tronco");
        Solida(q, Um(qual), c, qual.EndsWith("Tronco") ? 1.8f : 1.2f);

        for (int i = 0; i < Random.Range(3, 6); i++)
        {
            if (!q.Perto(c, 2, out Vector2Int p))
                break;

            string miudo = Sortear("Prisao/Corrente", "Prisao/Corrente", "Prisao/Ossos", "Prisao/Ossos", "Prisao/BolaDeEspinhos", "Prisao/Balde");
            Solta(q, Um(miudo), p, miudo.EndsWith("Balde") ? 10 : Pedreiro.OrdemDosEnfeites + 1);
        }
    }

    // Um esqueleto acorrentado na parede de cima, uma gaiola no chao e o que sobrou do preso.
    private static void Cela(Quarto q, Vector2Int c)
    {
        Pequena(Um("Prisao/Acorrentado"), (Vector2)c + new Vector2(0f, 0.1f), q.pai, 10);
        q.Tomar(c, false);

        if (q.Perto(c, 2, out Vector2Int g))
            Solida(q, Um("Prisao/GaiolaNoChao"), g, 1f);

        for (int i = 0; i < Random.Range(2, 5); i++)
        {
            if (!q.Perto(c, 2, out Vector2Int p))
                break;

            string miudo = Sortear("Prisao/Ossos", "Prisao/Ossos", "Prisao/Papel", "Prisao/Corrente", "Prisao/Balde");
            Solta(q, Um(miudo), p, miudo.EndsWith("Balde") ? 10 : Pedreiro.OrdemDosEnfeites + 1);
        }
    }

    // Uma mesa com cadeiras, uma caneca e uma vela acesa em cima.
    private static void MesaPosta(Quarto q, Vector2Int c)
    {
        // A mesa tem 2 celulas e meia de largura: precisa das do lado.
        Vector2Int esquerda = c + Vector2Int.left, direita = c + Vector2Int.right;

        if (!q.livre.Contains(esquerda) || !q.livre.Contains(direita))
        {
            Ossario(q, c);
            return;
        }

        GameObject mesa = Solida(q, Um("Prisao/Mesa"), c, 2.2f);
        q.Tomar(esquerda, true);
        q.Tomar(direita, true);

        if (mesa == null)
            return;

        Pequena(Um("Prisao/Caneca"), (Vector2)c + new Vector2(Random.Range(-0.8f, -0.1f), 0.75f), mesa.transform, 11);
        GameObject vela = Pequena(Um("Prisao/Vela"), (Vector2)c + new Vector2(Random.Range(0.2f, 0.8f), 0.7f), mesa.transform, 11);
        Iluminacao.Luz(vela.transform, new Vector2(0f, 0.4f), Iluminacao.Vela, 3.5f, 0.85f, 0.15f);

        foreach (int lado in new[] { -2, 2 })
        {
            Vector2Int cadeira = c + new Vector2Int(lado, 0);

            if (Random.value < 0.75f && q.livre.Contains(cadeira))
            {
                GameObject cad = Solta(q, Um("Prisao/Cadeira"), cadeira, 10);

                if (cad != null)
                    cad.GetComponent<SpriteRenderer>().flipX = lado > 0;
            }
        }

        if (q.Perto(c, 2, out Vector2Int papel))
            Solta(q, Um("Prisao/Papel"), papel, Pedreiro.OrdemDosEnfeites + 1);
    }

    // Um esqueleto no chao, ossos e velas (ou a chama magica azul de um ritual).
    private static void Ossario(Quarto q, Vector2Int c)
    {
        Solta(q, Um("Prisao/Esqueleto"), c, Pedreiro.OrdemDosEnfeites + 1);
        bool ritual = Random.value < 0.4f && chama.Length > 0;

        for (int i = 0; i < Random.Range(3, 6); i++)
        {
            if (!q.Perto(c, 2, out Vector2Int p))
                break;

            if (i < 2)
            {
                if (ritual)
                    ChamaMagica((Vector2)p + Torto(0.2f), q.pai);
                else
                {
                    GameObject vela = Solta(q, Um("Prisao/Vela"), p, 10);

                    if (vela != null)
                        Iluminacao.Luz(vela.transform, new Vector2(0f, 0.3f), Iluminacao.Vela, 3f, 0.75f, 0.15f);
                }

                q.Tomar(p, false);
            }
            else
                Solta(q, Um("Prisao/Ossos"), p, Pedreiro.OrdemDosEnfeites + 1);
        }
    }

    private static void ChamaMagica(Vector2 onde, Transform pai)
    {
        GameObject obj = new GameObject("Chama magica");
        obj.transform.SetParent(pai, false);
        obj.transform.position = onde + Vector2.up * 0.6f;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = chama[0];
        sr.sortingOrder = 10;
        obj.AddComponent<EnfeiteAnimado>().Comecar(chama, 10f);
        Iluminacao.Luz(obj.transform, new Vector2(0f, -0.3f), Iluminacao.Magica, 4f, 1f, 0.25f);
    }

    // ------------------------------------------------------------------ as salas especiais (Village)

    // O que vai na face da parede de cima de cada sala especial, entre as tochas.
    private static string[] NaParedeDaSala(TipoDeSala tipo)
    {
        switch (tipo)
        {
            case TipoDeSala.Loja: return new[] { "Vila/Prateleira" };
            case TipoDeSala.Tesouro: return new[] { "Vila/Escudos", "Vila/Estandarte" };
            case TipoDeSala.Altar: return new[] { "Vila/Quadros", "Vila/Trofeus" };
            case TipoDeSala.Desafio: return new[] { "Vila/Estandarte", "Vila/Escudos" };
            default: return null;
        }
    }

    // A mobilia de cada sala especial, encostada nas paredes (o meio fica pro que a sala tem).
    private static bool Mobiliar(Quarto q)
    {
        string[] encostadas, soltas;
        bool tapete = true, potes = false;

        switch (q.sala.tipo)
        {
            case TipoDeSala.Loja:
                encostadas = new[] { "Vila/ArmarioDePocoes", "Vila/ArmarioDePocoes", "Vila/Expositor", "Vila/BarrisDeGrao", "Vila/CaixotesDeSuprimento", "Vila/Barris" };
                soltas = new[] { "Vila/Sacos", "Vila/Sacos", "Vila/Cesta", "Vila/Potes" };
                potes = true;
                Balcao(q);
                break;
            case TipoDeSala.Tesouro:
                encostadas = new[] { "Vila/Armadura", "Vila/Cabide", "Vila/Bau", "Vila/BarrilDeArmas", "Vila/Bau" };
                soltas = new[] { "Vila/Elmo", "Vila/Sacos", "Vila/Elmo" };
                potes = true;
                break;
            case TipoDeSala.Altar:
                encostadas = new[] { "Vila/Estante", "Vila/Estante", "Vila/Escrivaninha", "Vila/Estante", "Vila/Planta" };
                soltas = new[] { "Vila/Livros", "Vila/Livros", "Vila/Papel", "Vila/Globo" };
                break;
            case TipoDeSala.Desafio:
                encostadas = new[] { "Vila/Cabide", "Vila/Armadura", "Vila/BarrilDeArmas", "Vila/Cabide" };
                soltas = new[] { "Vila/Elmo" };
                tapete = false;
                break;
            default:
                return false;
        }

        Vector2 meio = MapaDeCaminhos.Celula(q.sala.Meio);

        // O tapete embaixo do que a sala tem.
        GameObject tapeteNoChao = tapete ? Pequena(Um("Vila/Tapete"), meio + new Vector2(0f, -1.4f), q.pai, Pedreiro.OrdemDosEnfeites + 1) : null;

        if (tapeteNoChao != null)
            tapeteNoChao.GetComponent<SpriteRenderer>().flipX = false;

        foreach (string grupo in encostadas)
        {
            if (!q.Encostada(2.4f, true, out Vector2Int c) && !q.Encostada(2.4f, false, out c))
                break;

            Sprite s = Um(grupo);

            if (s != null)
                Solida(q, s, c, Mathf.Max(0.6f, s.bounds.size.x * 0.85f));
        }

        // Candelabros de pe, acesos.
        for (int i = 0; i < 2; i++)
        {
            if (!q.Encostada(2f, false, out Vector2Int c))
                break;

            GameObject cand = Solta(q, Um("Vila/CandelabroDePe"), c, 10);
            AcenderEmCima(cand, Iluminacao.Vela, 3.8f, 0.9f);
        }

        foreach (string grupo in soltas)
        {
            if (q.Encostada(1.3f, false, out Vector2Int c))
                Solta(q, Um(grupo), c, 10);
        }

        for (int i = 0; potes && i < Random.Range(2, 4); i++)
        {
            if (q.Encostada(1.2f, false, out Vector2Int c))
            {
                Quebravel.Pote((Vector2)c + Vector2.down * 0.4f + Torto(0.15f), q.pai);
                q.Tomar(c, true);
            }
        }

        return true;
    }

    // A loja da caverna: o mercador atras de uma mesa (os pedestais ficam na frente).
    private static void Balcao(Quarto q)
    {
        Vector2 meio = MapaDeCaminhos.Celula(q.sala.Meio);
        Sprite mesa = Um("Vila/Mesa");

        if (mesa != null)
        {
            GameObject obj = Pequena(mesa, meio + new Vector2(0f, 0.9f), q.pai, 10);
            obj.GetComponent<SpriteRenderer>().flipX = false;
            obj.layer = Pedreiro.CamadaDaParede;
            BoxCollider2D c = obj.AddComponent<BoxCollider2D>();
            c.size = new Vector2(mesa.bounds.size.x * 0.9f, 0.8f);
            c.offset = new Vector2(0f, 0.4f);
        }

        Mercador.Criar(meio + new Vector2(0f, 2.3f), q.pai);
    }

    // ------------------------------------------------------------------ Cripta e Profundezas

    private static void Cripta(Quarto q)
    {
        Grandes(q, true);

        // Bancos, livros e montes de velas acesas encostados nas paredes.
        int coisas = q.sala.DeLuta ? Random.Range(2, 4) : Random.Range(1, 3);

        for (int i = 0; i < coisas; i++)
        {
            if (!q.Encostada(2.5f, false, out Vector2Int c))
                break;

            float qual = Random.value;

            if (qual < 0.45f && velas.Length > 0)
            {
                GameObject obj = Pequena(velas[0], (Vector2)c + new Vector2(0f, 0.3f), q.pai, 10);
                obj.name = "Velas";
                obj.AddComponent<EnfeiteAnimado>().Comecar(velas, 10f);
                Iluminacao.Luz(obj.transform, Vector2.zero, Iluminacao.Vela, 3.8f, 0.9f, 0.15f);
                q.Tomar(c, false);
            }
            else if (qual < 0.75f)
                Solida(q, Um("Cripta/Banco"), c, 1.6f);
            else
                Solida(q, Um("Cripta/Livro"), c, 0.9f);
        }

        Miudas(q, () => Pequena(Um("Cripta/Vaso"), Vector2.zero, q.pai, 9));
    }

    private static void Profundezas(Quarto q)
    {
        Grandes(q, false);

        if (Random.value < 0.25f && q.Encostada(3f, false, out Vector2Int espada))
            Solida(q, Um("Profundezas/Espada"), espada, 0.6f);

        Miudas(q, null);
    }

    // Pecas grandes encostadas nas paredes (ou na beirada, nas Profundezas): seguram gente e tiro.
    private static void Grandes(Quarto q, bool cripta)
    {
        int grandes = q.sala.DeLuta ? Random.Range(2, 4) : (q.sala.tipo == TipoDeSala.Loja ? 0 : Random.Range(1, 3));

        for (int i = 0; i < grandes; i++)
        {
            if (!q.Encostada(2.5f, false, out Vector2Int c))
                break;

            string qual = cripta ? Sortear("Cripta/Caixao", "Cripta/Estatua", "Cripta/Cruz", "Cripta/Candelabro")
                                 : Sortear("Profundezas/Estatua", "Profundezas/Cristal", "Profundezas/Cristal", "Profundezas/Candelabro");
            GameObject peca = Solida(q, Um(qual), c, 0f);
            Acender(peca, qual);
        }
    }

    // Vasos na Cripta; montes de ouro e potes que quebram nas Profundezas.
    private static void Miudas(Quarto q, System.Func<GameObject> vaso)
    {
        int miudas = Random.Range(2, 5);

        for (int i = 0; i < miudas; i++)
        {
            if (!Lugar(q, 1.6f, out Vector2Int c))
                break;

            if (vaso != null)
            {
                GameObject v = vaso();

                if (v != null)
                    v.transform.position = (Vector2)c + Torto();
            }
            else if (Random.value < 0.6f)
            {
                Sprite pote = Um("Profundezas/Pote");

                if (pote != null)
                    Quebravel.Vaso((Vector2)c + Torto() + Vector2.down * 0.3f, q.pai, pote);
            }
            else
                Pequena(Um("Profundezas/Ouro"), (Vector2)c + Torto(), q.pai, Pedreiro.OrdemDosEnfeites + 1);
        }
    }

    // Um lugar no meio do chao, com tudo em volta livre.
    private static bool Lugar(Quarto q, float espaco, out Vector2Int onde)
    {
        List<Vector2Int> servem = new List<Vector2Int>();

        foreach (Vector2Int c in q.sala.celulas)
        {
            if (!q.livre.Contains(c) || Vector2.Distance(c, q.sala.Meio) < q.longeDoMeio || !Cercado(c, q.livre))
                continue;

            if (q.usados.TrueForAll(u => Vector2.Distance(u, c) >= espaco))
                servem.Add(c);
        }

        onde = servem.Count > 0 ? servem[Random.Range(0, servem.Count)] : default;

        if (servem.Count > 0)
            q.usados.Add(onde);

        return servem.Count > 0;
    }

    // ------------------------------------------------------------------ miudezas e paredes

    // Pelo chao: ossos, pedras, papel, correntes e destrocos, bem mais perto das paredes. Nao seguram ninguem.
    private static void Miudezas(Quarto q, int mundo)
    {
        string[] tipos;

        if (mundo <= 2)
            tipos = new[] { "Prisao/Ossos", "Prisao/Ossos", "Prisao/Ossos", "Prisao/Pedras", "Prisao/Pedras", "Prisao/Pedras",
                            "Prisao/Papel", "Prisao/Corrente", "Prisao/Esqueleto" };
        else if (mundo == 3)
            tipos = new[] { "Cripta/Ossos", "Cripta/Ossos", "Cripta/Ossos", "Prisao/Pedras", "Prisao/Pedras", "Prisao/Corrente" };
        else
            tipos = new[] { "Prisao/Pedras", "Prisao/Pedras", "Profundezas/Ouro" };

        // Teias nos cantos de cima (nos mundos com parede).
        if (mundo != 4)
        {
            foreach (Vector2Int c in q.sala.celulas)
            {
                bool esquerda = q.Parede(c + Vector2Int.left), direita = q.Parede(c + Vector2Int.right);

                if (q.planta.Contains(c) && q.Parede(c + Vector2Int.up) && (esquerda || direita) && Random.value < 0.6f)
                {
                    GameObject teia = Pequena(Um("Vila/Teia"), (Vector2)c + new Vector2(esquerda ? -0.25f : 0.25f, 0.55f), q.pai, Pedreiro.OrdemDasParedes + 1);

                    if (teia != null)
                        teia.GetComponent<SpriteRenderer>().flipX = direita;
                }
            }
        }

        float noMeio = mundo == 4 ? 0.03f : 0.07f;
        float naBeirada = mundo == 4 ? 0.08f : 0.2f;

        foreach (Vector2Int c in q.sala.celulas)
        {
            if (!q.livre.Contains(c) || Vector2.Distance(c, q.sala.Meio) < q.longeDoMeio)
                continue;

            if (Random.value >= (q.NaBeirada(c) ? naBeirada : noMeio))
                continue;

            GameObject obj = Pequena(Um(tipos[Random.Range(0, tipos.Length)]), (Vector2)c + Torto(0.3f), q.pai, Pedreiro.OrdemDosEnfeites + 1);

            if (obj != null && mundo == 4)
                obj.GetComponent<SpriteRenderer>().color = new Color(0.75f, 0.85f, 0.8f);
        }
    }

    // Tochas na face da parede de cima, a cada 4 celulas, longe dos corredores; entre elas, o que o mundo tem.
    private static void NaParedeDeCima(SalaDaPlanta sala, HashSet<Vector2Int> planta, Transform pai, int mundo)
    {
        int n = 0;
        int naParede = Pedreiro.OrdemDasParedes + 1;

        foreach (Vector2Int c in sala.celulas)
        {
            if (!planta.Contains(c) || planta.Contains(c + Vector2Int.up) || planta.Contains(c + Vector2Int.up * 2)
                || PertoDeCorredor(c, planta))
                continue;

            int coluna = (c.x - sala.area.xMin) % 4;

            if (coluna == 2)
            {
                if (mundo == 3 && n++ % 2 == 1)
                    Pequena(Um("Cripta/Estandarte"), (Vector2)c + new Vector2(0f, 0.75f), pai, naParede);
                else if (tocha.Length > 0)
                    Animada(tocha, (Vector2)c + new Vector2(0f, 1.15f), pai, naParede, 10f);
            }
            else if (coluna == 0 && NaParedeDaSala(sala.tipo) != null)
            {
                string[] opcoes = NaParedeDaSala(sala.tipo);
                Pequena(Um(opcoes[Random.Range(0, opcoes.Length)]), (Vector2)c + new Vector2(0f, 0.9f), pai, naParede);
            }
            else if (coluna == 0 && mundo <= 2)
            {
                float qual = Random.value;

                if (qual < 0.3f)
                    Pequena(Um("Prisao/Estandarte"), (Vector2)c + new Vector2(0f, 0.95f), pai, naParede);
                else if (qual < 0.5f)
                    Pequena(Um("Prisao/Retrato"), (Vector2)c + new Vector2(0f, 0.9f), pai, naParede);
                else if (qual < 0.72f)
                    Corrente(c, pai);
                else if (qual < 0.85f)
                    Pequena(Um("Prisao/Acorrentado"), (Vector2)c + new Vector2(0f, 0.35f), pai, naParede);
            }
            else if (coluna == 0 && mundo == 3 && Random.value < 0.4f)
                Corrente(c, pai);
        }
    }

    // Uma corrente na parede de cima: as curtas presas na parede; as compridas descem do teto, na frente dela.
    private static void Corrente(Vector2Int c, Transform pai)
    {
        Sprite s = Um("Prisao/CorrenteDaParede");

        if (s == null)
            return;

        if (s.bounds.size.y > 2f)
            Pequena(s, (Vector2)c + new Vector2(0f, -0.2f), pai, 10);
        else
            Pequena(s, (Vector2)c + new Vector2(0f, 0.45f), pai, Pedreiro.OrdemDasParedes + 1);
    }

    // A parede logo acima tem um buraco (o corredor que sobe) por perto: ali nao vai nada.
    private static bool PertoDeCorredor(Vector2Int c, HashSet<Vector2Int> planta)
    {
        for (int dx = -2; dx <= 2; dx++)
        {
            if (planta.Contains(c + new Vector2Int(dx, 1)) || planta.Contains(c + new Vector2Int(dx, 2)))
                return true;
        }

        return false;
    }

    private static void Lancador(SalaDaPlanta sala, PlantaDeSalas.Planta salas, Transform pai)
    {
        List<Vector2Int> servem = new List<Vector2Int>();

        foreach (Vector2Int c in sala.celulas)
        {
            if (salas.chao.Contains(c) && !salas.chao.Contains(c + Vector2Int.up) && !salas.chao.Contains(c + Vector2Int.up * 2)
                && Mathf.Abs(c.x - sala.Meio.x) <= 3 && Mathf.Abs(c.x - sala.Meio.x) >= 1 && !PertoDeCorredor(c, salas.chao))
                servem.Add(c);
        }

        if (servem.Count > 0)
        {
            Vector2Int c = servem[Random.Range(0, servem.Count)];
            LancadorDeFogo.Criar((Vector2)c + new Vector2(0f, LancadorDeFogo.DoChao + 0.3f), pai, sala);
        }
    }

    private static bool Cercado(Vector2Int c, HashSet<Vector2Int> livre)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (!livre.Contains(c + new Vector2Int(dx, dy)))
                    return false;

        return true;
    }

    // Candelabros acendem; os cristais brilham na cor deles (os primeiros roxos, os outros verdes).
    private static void Acender(GameObject peca, string grupo)
    {
        if (peca == null)
            return;

        SpriteRenderer sr = peca.GetComponent<SpriteRenderer>();

        if (grupo.EndsWith("Candelabro"))
            AcenderEmCima(peca, Iluminacao.Vela, 4f, 0.95f);
        else if (grupo.EndsWith("Cristal"))
        {
            float altura = sr.sprite != null ? sr.sprite.bounds.size.y : 1f;
            bool roxo = int.TryParse(sr.sprite.name, out int numero) && numero < 9;
            Color cor = roxo ? new Color(0.65f, 0.45f, 1f) : new Color(0.4f, 1f, 0.6f);
            Iluminacao.Luz(peca.transform, new Vector2(0f, altura * 0.5f), cor, 3.6f, 0.9f, 0.03f);
        }
    }

    private static void AcenderEmCima(GameObject peca, Color cor, float raio, float intensidade)
    {
        if (peca == null)
            return;

        Sprite s = peca.GetComponent<SpriteRenderer>().sprite;
        float altura = s != null ? s.bounds.size.y : 1f;
        Iluminacao.Luz(peca.transform, new Vector2(0f, altura * 0.85f), cor, raio, intensidade, 0.15f);
    }

    // Uma peca grande, de pe na celula: segura gente e tiro (camada das paredes). Largura 0 = a do desenho.
    private static GameObject Solida(Quarto q, Sprite desenho, Vector2Int c, float largura)
    {
        if (desenho == null)
            return null;

        GameObject obj = Pequena(desenho, (Vector2)c + Vector2.down * 0.4f, q.pai, 10);
        obj.layer = Pedreiro.CamadaDaParede;
        BoxCollider2D colisor = obj.AddComponent<BoxCollider2D>();

        if (largura <= 0f)
            largura = Mathf.Clamp(desenho.bounds.size.x * 0.8f, 0.5f, 1.6f);

        colisor.size = new Vector2(largura, 0.6f);
        colisor.offset = new Vector2(0f, 0.3f);
        q.Tomar(c, true);
        return obj;
    }

    // Um barril ou caixote que quebra (as vezes solta moeda).
    private static void Quebra(Quarto q, Sprite desenho, Vector2Int c)
    {
        if (desenho == null)
            return;

        Quebravel.Vaso((Vector2)c + Vector2.down * 0.4f + Torto(0.1f), q.pai, desenho);
        q.Tomar(c, true);
    }

    // Uma peca solta, que ninguem esbarra.
    private static GameObject Solta(Quarto q, Sprite desenho, Vector2Int c, int ordem)
    {
        if (desenho == null)
            return null;

        q.Tomar(c, false);
        return Pequena(desenho, (Vector2)c + Torto(0.2f) + (ordem >= 10 ? Vector2.down * 0.35f : Vector2.zero), q.pai, ordem);
    }

    private static GameObject Pequena(Sprite desenho, Vector2 onde, Transform pai, int ordem)
    {
        if (desenho == null)
            return null;

        GameObject obj = new GameObject(desenho.name);
        obj.transform.SetParent(pai, false);
        obj.transform.position = onde;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = desenho;
        sr.sortingOrder = ordem;
        sr.spriteSortPoint = SpriteSortPoint.Pivot;
        sr.flipX = Random.value < 0.5f && ordem != Pedreiro.OrdemDasParedes + 1;
        return obj;
    }

    private static void Animada(Sprite[] quadros, Vector2 onde, Transform pai, int ordem, float qps)
    {
        GameObject obj = new GameObject("Tocha");
        obj.transform.SetParent(pai, false);
        obj.transform.position = onde;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = quadros[0];
        sr.sortingOrder = ordem;
        obj.AddComponent<EnfeiteAnimado>().Comecar(quadros, qps);
        Iluminacao.Luz(obj.transform, new Vector2(0f, 0.25f), Iluminacao.Fogo, 5.5f, 1f, 0.18f);
    }

    /// <summary>Tochas na parede de cima do salao do chefe (a cada 4 celulas).</summary>
    public static void TochasDaArena(HashSet<Vector2Int> planta, Transform pai)
    {
        CarregarAnimadas();

        if (tocha.Length == 0)
            return;

        Transform grupo = new GameObject("Tochas").transform;
        grupo.SetParent(pai, false);

        foreach (Vector2Int c in planta)
        {
            if (!planta.Contains(c + Vector2Int.up) && !planta.Contains(c + Vector2Int.up * 2) && ((c.x % 4) + 4) % 4 == 0)
                Animada(tocha, (Vector2)c + new Vector2(0f, 1.15f), grupo, Pedreiro.OrdemDasParedes + 1, 10f);
        }
    }

    // Nas Profundezas: rochas, pilares e estatuas saindo do vazio, de 2 a 6 celulas das salas.
    private static void NoVazio(HashSet<Vector2Int> planta, Transform pai)
    {
        Sprite[] pecas = Grupo("Profundezas/Vazio");

        if (pecas.Length == 0)
            return;

        RectInt limites = Caverna.Limites(planta);
        List<Vector2Int> servem = new List<Vector2Int>();

        for (int x = limites.xMin - 6; x < limites.xMax + 6; x++)
        {
            for (int y = limites.yMin - 6; y < limites.yMax + 6; y++)
            {
                Vector2Int c = new Vector2Int(x, y);

                if (!planta.Contains(c) && PertoDe(planta, c, 6) && !PertoDe(planta, c, 2))
                    servem.Add(c);
            }
        }

        List<Vector2> usados = new List<Vector2>();
        int quantos = servem.Count / 35;

        for (int tentativa = 0; usados.Count < quantos && tentativa < quantos * 10 && servem.Count > 0; tentativa++)
        {
            Vector2Int c = servem[Random.Range(0, servem.Count)];

            if (!usados.TrueForAll(u => Vector2.Distance(u, c) >= 4.5f))
                continue;

            usados.Add(c);
            GameObject obj = Pequena(pecas[Random.Range(0, pecas.Length)], c, pai, Pedreiro.OrdemDoAbismo + 1);
            obj.GetComponent<SpriteRenderer>().color = new Color(0.8f, 0.8f, 0.8f);
        }
    }

    private static bool PertoDe(HashSet<Vector2Int> planta, Vector2Int c, int raio)
    {
        for (int dx = -raio; dx <= raio; dx++)
            for (int dy = -raio; dy <= raio; dy++)
                if (dx * dx + dy * dy <= raio * raio && planta.Contains(c + new Vector2Int(dx, dy)))
                    return true;

        return false;
    }
}
