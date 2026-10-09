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
/// Cada peca so vai onde o desenho inteiro cabe (<see cref="Cabe"/>): sem encostar no desenho de outra, com o
/// pe no chao livre da sala e sem passar da parede do lado nem da de baixo (as pecas altas so sobem pela face
/// da parede de cima). Nada fica nos corredores, na frente das portas, em cima das armadilhas nem colado no
/// que ja estava no chao (baus, armas, pedras, mesas); o que segura gente sai do mapa de caminhos.
/// </summary>
public static class Decoracao
{
    private static readonly Dictionary<string, Sprite[]> grupos = new Dictionary<string, Sprite[]>();
    private static Sprite[] tocha, chama, velas;

    /// <summary>O que cada desenho ja posto ocupa na tela (vale durante o <see cref="Espalhar"/>).</summary>
    private static readonly List<Rect> desenhos = new List<Rect>();

    /// <summary>Quanto dois desenhos podem se encostar (as beiradas dos desenhos sao quase transparentes).</summary>
    private const float Folga = 0.1f;

    /// <summary>
    /// Quanto a parede do lado e desenhada pra dentro da celula de chao vizinha: 0,2 a 0,3 na maioria dos
    /// ladrilhos, quase 0,5 nas quinas dos degraus da parede. Conta o pior caso, sem folga.
    /// </summary>
    private const float ParedeDoLado = 0.45f;

    /// <summary>Nas Profundezas o chao acaba no meio da celula da beirada (o resto e o penhasco).</summary>
    private const float BeiradaDoVazio = 0.5f;

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

        // As celulas livres encostadas numa parede (so a de cima, se pedir), longe dos outros cantos, embaralhadas.
        public List<Vector2Int> Encostadas(float espaco, bool soEmCima)
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

            return Embaralhar(servem);
        }

        // As celulas livres a ate "raio" de "perto" (pra montar a cena em volta do canto), embaralhadas.
        public List<Vector2Int> Pertos(Vector2Int perto, int raio)
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

            return Embaralhar(servem);
        }

        // As celulas no meio do chao, com tudo em volta livre, longe das outras pecas do meio.
        public List<Vector2Int> NoMeio(float espaco)
        {
            List<Vector2Int> servem = new List<Vector2Int>();

            foreach (Vector2Int c in sala.celulas)
            {
                if (!livre.Contains(c) || Vector2.Distance(c, sala.Meio) < longeDoMeio || !Cercado(c, livre))
                    continue;

                if (usados.TrueForAll(u => Vector2.Distance(u, c) >= espaco))
                    servem.Add(c);
            }

            return Embaralhar(servem);
        }

        public void Tomar(Vector2Int c, bool seguraGente)
        {
            livre.Remove(c);

            if (seguraGente)
                chao.Remove(c);
        }
    }

    private static List<Vector2Int> Embaralhar(List<Vector2Int> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }

        return lista;
    }

    /// <param name="mundo">1 Porao, 2 Catacumbas, 3 Cripta, 4 Profundezas.</param>
    /// <param name="chao">O chao andavel (o mesmo do mapa de caminhos: o que segura gente sai dele).</param>
    /// <param name="reservado">Onde ja tem algo que nao sai do chao (armadilhas, o caminho do tronco, velas).</param>
    public static void Espalhar(int mundo, EstiloDeLadrilhos estilo, PlantaDeSalas.Planta salas, HashSet<Vector2Int> chao,
                                HashSet<Vector2Int> reservado, Transform pai)
    {
        CarregarAnimadas();
        desenhos.Clear();
        Transform grupo = new GameObject("Decoracao").transform;
        grupo.SetParent(pai, false);

        // Longe dos corredores e da frente das portas.
        HashSet<Vector2Int> livre = new HashSet<Vector2Int>(chao);

        foreach (Vector2Int c in salas.corredores)
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    livre.Remove(c + new Vector2Int(dx, dy));

        // Nem colado no que ja ocupa o chao (baus, armas, pedras, mesas, buracos, as coisas das salas especiais).
        foreach (Vector2Int c in salas.chao)
        {
            if (chao.Contains(c))
                continue;

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    livre.Remove(c + new Vector2Int(dx, dy));
        }

        // O que e alto tambem nao cobre essas celulas por cima (uma estatua na frente do tronco parado).
        if (reservado != null)
        {
            livre.ExceptWith(reservado);

            foreach (Vector2Int c in reservado)
                desenhos.Add(new Rect(c.x - 0.5f, c.y - 0.5f, 1f, 1f));
        }

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

            // O lancador de fogo primeiro (numa sala de luta da Cripta, as vezes, no meio da parede de cima):
            // o resto da parede se arruma em volta dele.
            if (cripta && sala.DeLuta && Random.value < 0.55f)
                Lancador(q);

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
        }

        if (profundezas)
            NoVazio(salas.chao, grupo);

        desenhos.Clear();
    }

    private static string Sortear(params string[] opcoes) => opcoes[Random.Range(0, opcoes.Length)];

    private static Vector2 Torto(float quanto = 0.25f) => new Vector2(Random.Range(-quanto, quanto), Random.Range(-quanto * 0.8f, quanto * 0.8f));

    // ------------------------------------------------------------------ onde cada peca cabe

    /// <summary>Como a peca fica na celula.</summary>
    private enum Jeito
    {
        /// <summary>De pe, segura gente e tiro (camada das paredes).</summary>
        Solida,
        /// <summary>Barril ou caixote que quebra.</summary>
        Quebra,
        /// <summary>Pote do Village que quebra (o desenho e sorteado pelo Quebravel).</summary>
        Pote,
        /// <summary>De pe, ninguem esbarra (cadeira, balde, candelabro).</summary>
        EmPe,
        /// <summary>De pe, colada na parede de cima (o esqueleto acorrentado).</summary>
        NaParede,
        /// <summary>Deitada no chao (ossos, papel, correntes): nao sobe na parede.</summary>
        Deitada,
        /// <summary>O monte de velas acesas (Cripta).</summary>
        Velas,
        /// <summary>A chama magica azul.</summary>
        Chama,
    }

    // Onde fica o pe do desenho, a partir do meio da celula.
    private static Vector2 Desvio(Jeito jeito)
    {
        switch (jeito)
        {
            case Jeito.Solida: return Vector2.down * 0.4f;
            case Jeito.Quebra: return Vector2.down * 0.4f + Torto(0.1f);
            case Jeito.Pote: return Vector2.down * 0.4f + Torto(0.15f);
            case Jeito.EmPe: return Vector2.down * 0.35f + Torto(0.2f);
            case Jeito.NaParede: return new Vector2(0f, 0.1f);
            case Jeito.Velas: return new Vector2(0f, 0.3f);
            case Jeito.Chama: return Torto(0.2f) + Vector2.up * 0.6f;
            default: return Torto(0.25f);
        }
    }

    // O que o desenho ocupa com o pe (o pivo) em "onde". As folhas animadas tem muita sobra: so a parte desenhada.
    private static Rect Retangulo(Sprite s, Vector2 onde, Jeito jeito = Jeito.EmPe)
    {
        if (jeito == Jeito.Velas)
            return new Rect(onde.x - 0.35f, onde.y - 0.55f, 0.7f, 0.8f);

        if (jeito == Jeito.Chama)
            return new Rect(onde.x - 0.3f, onde.y - 0.5f, 0.6f, 0.6f);

        if (jeito == Jeito.Pote)
            return new Rect(onde.x - 0.4f, onde.y, 0.8f, 0.8f);

        Bounds b = s.bounds;
        return new Rect(onde + (Vector2)b.min, b.size);
    }

    private static Rect Encolher(Rect r, float quanto)
    {
        float w = Mathf.Max(0.02f, r.width - quanto * 2f), h = Mathf.Max(0.02f, r.height - quanto * 2f);
        return new Rect(r.center.x - w * 0.5f, r.center.y - h * 0.5f, w, h);
    }

    /// <summary>Nao encosta em nenhum desenho ja posto.</summary>
    private static bool Sobra(Rect r)
    {
        Rect menor = Encolher(r, Folga);

        foreach (Rect outro in desenhos)
            if (outro.Overlaps(menor))
                return false;

        return true;
    }

    /// <summary>
    /// O desenho com o pe na celula <paramref name="pe"/> cabe: nao encosta em outro, o pe fica so em chao
    /// livre da sala e nada passa da parede do lado nem da de baixo. A parte de cima pode ficar na frente de
    /// ate <paramref name="sobe"/> celulas da face da parede de cima, como quem esta encostado nela (o que e
    /// mais alto que isso vai pras paredes do lado, senao parece em pe em cima da parede).
    /// </summary>
    private static bool Cabe(Quarto q, Rect r, Vector2Int pe, int sobe)
    {
        Rect menor = Encolher(r, Folga);

        // O pe: as celulas embaixo dele livres.
        for (int x = Mathf.FloorToInt(menor.xMin + 0.5f); x <= Mathf.FloorToInt(menor.xMax + 0.5f); x++)
            if (!q.livre.Contains(new Vector2Int(x, pe.y)))
                return false;

        // Tudo o que o desenho cobre e chao (ou a face da parede de cima, logo acima do chao). A parede do
        // lado comeca "ParedeDoLado" pra dentro da celula de chao vizinha; aqui o desenho conta inteiro, sem
        // folga. Nas Profundezas, o que fica deitado no chao tambem nao passa da beirada de cima nem da de baixo.
        float lado = q.flutuante ? BeiradaDoVazio : ParedeDoLado;
        float emPe = q.flutuante && sobe == 0 ? BeiradaDoVazio : 0f;
        int x0 = Mathf.FloorToInt(r.xMin + 0.5f - lado + 0.001f);
        int x1 = Mathf.CeilToInt(r.xMax - 0.5f + lado - 0.001f);
        int y0 = Mathf.Min(pe.y, Mathf.FloorToInt(menor.yMin + 0.5f - emPe + 0.001f));
        int y1 = Mathf.CeilToInt(menor.yMax - 0.5f + emPe - 0.001f);

        for (int x = x0; x <= x1; x++)
        {
            for (int y = y0; y <= y1; y++)
            {
                Vector2Int c = new Vector2Int(x, y);

                if (q.planta.Contains(c))
                    continue;

                // A face so vale longe das quinas: parede com chao do lado, na mesma fileira, e parede do lado
                // (a quina do degrau), e a beirada dela entra no chao.
                bool face = false;
                bool quina = q.planta.Contains(c + Vector2Int.left) || q.planta.Contains(c + Vector2Int.right);

                for (int k = 1; k <= sobe && y - k >= pe.y && !quina; k++)
                    face |= q.planta.Contains(c + Vector2Int.down * k);

                if (!face)
                    return false;
            }
        }

        return Sobra(r);
    }

    // Quanto empurrar o desenho pro lado pra ele nao entrar na parede do lado da celula (o barril largo
    // encostado na parede da direita vai um pouco pra esquerda). Olha a parede do lado em todas as fileiras
    // que o desenho cobre (nas quinas dos degraus, a parede so comeca mais pra cima).
    private static float Afastar(Quarto q, Rect r, Vector2Int c)
    {
        float lado = q.flutuante ? BeiradaDoVazio : ParedeDoLado;
        float esquerda = c.x - 0.5f + lado, direita = c.x + 0.5f - lado;
        bool paredeEsquerda = false, paredeDireita = false;

        // Parede do lado numa fileira: parede ao lado e chao na coluna da celula.
        for (int y = c.y; y <= Mathf.CeilToInt(r.yMax - 0.5f) && !q.Parede(new Vector2Int(c.x, y)); y++)
        {
            paredeEsquerda |= q.Parede(new Vector2Int(c.x - 1, y));
            paredeDireita |= q.Parede(new Vector2Int(c.x + 1, y));
        }

        if (paredeEsquerda && r.xMin < esquerda)
            return esquerda - r.xMin;

        if (paredeDireita && r.xMax > direita)
            return direita - r.xMax;

        return 0f;
    }

    // Marca o desenho como posto e tira do chao livre as celulas embaixo do pe dele.
    private static void Ocupar(Quarto q, Rect r, Vector2Int pe, bool seguraGente)
    {
        desenhos.Add(r);
        Rect menor = Encolher(r, Folga);

        for (int x = Mathf.FloorToInt(menor.xMin + 0.5f); x <= Mathf.FloorToInt(menor.xMax + 0.5f); x++)
            q.Tomar(new Vector2Int(x, pe.y), seguraGente);
    }

    /// <summary>
    /// Poe o desenho na primeira das <paramref name="celulas"/> onde ele cabe inteiro. Devolve o objeto
    /// (null se nao coube em nenhuma) e a celula em <paramref name="onde"/>.
    /// Na <see cref="Jeito.Solida"/>, <paramref name="largura"/> e a do colisor (0 = a do desenho).
    /// </summary>
    private static GameObject Por(Quarto q, IEnumerable<Vector2Int> celulas, Sprite desenho, Jeito jeito, out Vector2Int onde, float largura = 0f)
    {
        onde = default;

        if (desenho == null && jeito != Jeito.Pote && jeito != Jeito.Velas && jeito != Jeito.Chama)
            return null;

        foreach (Vector2Int c in celulas)
        {
            if (!q.livre.Contains(c))
                continue;

            Vector2 pe = (Vector2)c + Desvio(jeito);
            Rect r = Retangulo(desenho, pe, jeito);
            float empurra = Afastar(q, r, c);
            pe.x += empurra;
            r.x += empurra;

            if (!Cabe(q, r, c, jeito == Jeito.Deitada ? 0 : jeito == Jeito.NaParede ? 2 : 1))
                continue;

            GameObject obj = Criar(q, desenho, pe, jeito, largura);

            if (obj == null)
                return null;

            Ocupar(q, r, c, jeito == Jeito.Solida || jeito == Jeito.Quebra || jeito == Jeito.Pote);
            onde = c;
            return obj;
        }

        return null;
    }

    private static GameObject Por(Quarto q, IEnumerable<Vector2Int> celulas, Sprite desenho, Jeito jeito, float largura = 0f) =>
        Por(q, celulas, desenho, jeito, out _, largura);

    // Uma peca encostada na parede, longe dos outros cantos (o canto vira o lugar de uma cena).
    private static GameObject Encostar(Quarto q, float espaco, bool soEmCima, Sprite desenho, Jeito jeito, out Vector2Int onde, float largura = 0f)
    {
        GameObject obj = Por(q, q.Encostadas(espaco, soEmCima), desenho, jeito, out onde, largura);

        if (obj != null)
            q.usados.Add(onde);

        return obj;
    }

    private static GameObject Criar(Quarto q, Sprite desenho, Vector2 pe, Jeito jeito, float largura)
    {
        switch (jeito)
        {
            case Jeito.Solida:
            {
                GameObject obj = Pequena(desenho, pe, q.pai, 10);
                obj.layer = Pedreiro.CamadaDaParede;
                BoxCollider2D colisor = obj.AddComponent<BoxCollider2D>();

                if (largura <= 0f)
                    largura = Mathf.Clamp(desenho.bounds.size.x * 0.8f, 0.5f, 1.6f);

                colisor.size = new Vector2(largura, 0.6f);
                colisor.offset = new Vector2(0f, 0.3f);
                return obj;
            }

            case Jeito.Quebra:
                return Quebravel.Vaso(pe, q.pai, desenho).gameObject;

            case Jeito.Pote:
            {
                Quebravel pote = Quebravel.Pote(pe, q.pai);
                return pote != null ? pote.gameObject : null;
            }

            case Jeito.Deitada:
                return Pequena(desenho, pe, q.pai, Pedreiro.OrdemDosEnfeites + 1);

            case Jeito.Velas:
            {
                if (velas.Length == 0)
                    return null;

                GameObject obj = Pequena(velas[0], pe, q.pai, 10);
                obj.name = "Velas";
                obj.AddComponent<EnfeiteAnimado>().Comecar(velas, 10f);
                Iluminacao.Luz(obj.transform, Vector2.zero, Iluminacao.Vela, 3.8f, 0.9f, 0.15f);
                return obj;
            }

            case Jeito.Chama:
                return chama.Length > 0 ? ChamaMagica(pe, q.pai) : null;

            default:
                return Pequena(desenho, pe, q.pai, 10);
        }
    }

    // Uma peca pendurada ou presa na parede (nao tem pe no chao): so precisa nao encostar em nada.
    private static GameObject Pendurar(Sprite desenho, Vector2 onde, Transform pai, int ordem)
    {
        if (desenho == null)
            return null;

        Rect r = Retangulo(desenho, onde);

        if (!Sobra(r))
            return null;

        desenhos.Add(r);
        return Pequena(desenho, onde, pai, ordem);
    }

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

            switch (cena)
            {
                case Cena.Deposito: Deposito(q); break;
                case Cena.Tortura: Tortura(q); break;
                case Cena.Cela: Cela(q); break;
                case Cena.Mesa: MesaPosta(q); break;
                default: Ossario(q); break;
            }
        }

        // Um candelabro aceso num canto.
        if (sala.DeLuta && Random.value < 0.6f)
        {
            GameObject cand = Encostar(q, 3f, false, Um("Prisao/Candelabro"), Jeito.EmPe, out _);
            AcenderEmCima(cand, Iluminacao.Vela, 4f, 0.9f);
        }

        // Uma gaiola pendurada perto da parede de cima (uma celula pra baixo, se der).
        if (Random.value < 0.4f)
        {
            Sprite gaiola = Um("Prisao/Gaiola");

            foreach (Vector2Int c in q.Encostadas(3f, true))
            {
                Vector2Int baixo = c + Vector2Int.down;
                Vector2Int onde = q.livre.Contains(baixo) ? baixo : c;

                if (Pendurar(gaiola, (Vector2)onde + new Vector2(0f, -0.2f), q.pai, 10) != null)
                {
                    q.usados.Add(c);
                    q.Tomar(onde, false);
                    break;
                }
            }
        }

        // Correntes caindo do teto, na frente da parede de cima.
        if (Random.value < 0.35f)
        {
            Sprite corrente = Um("Prisao/CorrenteDoTeto");

            foreach (Vector2Int c in q.Encostadas(4f, true))
            {
                if (Pendurar(corrente, (Vector2)c + new Vector2(0f, 1.2f), q.pai, Pedreiro.OrdemDasParedes + 1) != null)
                {
                    q.usados.Add(c);
                    break;
                }
            }
        }
    }

    // Toneis ou barris (que quebram), caixotes, sacos e baldes.
    private static void Deposito(Quarto q)
    {
        GameObject primeiro = null;
        Vector2Int c = default;

        if (Random.value < 0.5f && Grupo("Prisao/Tonel").Length > 0)
            primeiro = Encostar(q, 4f, false, Um("Prisao/Tonel"), Jeito.Solida, out c, 1.5f);

        if (primeiro == null)
            primeiro = Encostar(q, 4f, false, Um(Random.value < 0.3f ? "Prisao/BarrilDeMoedas" : "Prisao/Barril"), Jeito.Quebra, out c);

        if (primeiro == null)
            return;

        int mais = Random.Range(3, 6);

        for (int i = 0; i < mais; i++)
        {
            List<Vector2Int> perto = q.Pertos(c, i < 2 ? 1 : 2);
            float qual = Random.value;

            if (qual < 0.4f)
                Por(q, perto, Um("Prisao/Barril"), Jeito.Quebra);
            else if (qual < 0.6f)
                Por(q, perto, Um("Prisao/Caixote"), Jeito.Quebra);
            else if (qual < 0.7f)
                Por(q, perto, null, Jeito.Pote);
            else if (qual < 0.75f)
                Por(q, perto, Um("Prisao/Saco"), Jeito.EmPe);
            else if (qual < 0.88f)
                Por(q, perto, Um("Prisao/Balde"), Jeito.EmPe);
            else
                Por(q, perto, Um("Prisao/BarrilCaido"), Jeito.EmPe);
        }
    }

    // Uma maquina de tortura, com correntes, ossos, baldes e bolas de espinhos em volta.
    private static void Tortura(Quarto q)
    {
        string qual = Sortear("Prisao/DamaDeFerro", "Prisao/DamaDeFerro", "Prisao/Guilhotina", "Prisao/Tronco");

        if (Encostar(q, 4f, false, Um(qual), Jeito.Solida, out Vector2Int c, qual.EndsWith("Tronco") ? 1.8f : 1.2f) == null)
            return;

        for (int i = 0; i < Random.Range(3, 6); i++)
        {
            string miudo = Sortear("Prisao/Corrente", "Prisao/Corrente", "Prisao/Ossos", "Prisao/Ossos", "Prisao/BolaDeEspinhos", "Prisao/Balde");
            Por(q, q.Pertos(c, 2), Um(miudo), miudo.EndsWith("Balde") ? Jeito.EmPe : Jeito.Deitada);
        }
    }

    // Um esqueleto acorrentado na parede de cima, uma gaiola no chao e o que sobrou do preso.
    private static void Cela(Quarto q)
    {
        if (Encostar(q, 4f, true, Um("Prisao/Acorrentado"), Jeito.NaParede, out Vector2Int c) == null)
            return;

        Por(q, q.Pertos(c, 2), Um("Prisao/GaiolaNoChao"), Jeito.Solida, 1f);

        for (int i = 0; i < Random.Range(2, 5); i++)
        {
            string miudo = Sortear("Prisao/Ossos", "Prisao/Ossos", "Prisao/Papel", "Prisao/Corrente", "Prisao/Balde");
            Por(q, q.Pertos(c, 2), Um(miudo), miudo.EndsWith("Balde") ? Jeito.EmPe : Jeito.Deitada);
        }
    }

    // Uma mesa com cadeiras, uma caneca e uma vela acesa em cima.
    private static void MesaPosta(Quarto q)
    {
        GameObject mesa = Encostar(q, 4f, false, Um("Prisao/Mesa"), Jeito.Solida, out Vector2Int c, 2.2f);

        if (mesa == null)
        {
            Ossario(q);
            return;
        }

        Pequena(Um("Prisao/Caneca"), (Vector2)c + new Vector2(Random.Range(-0.8f, -0.1f), 0.75f), mesa.transform, 11);
        GameObject vela = Pequena(Um("Prisao/Vela"), (Vector2)c + new Vector2(Random.Range(0.2f, 0.8f), 0.7f), mesa.transform, 11);
        Iluminacao.Luz(vela.transform, new Vector2(0f, 0.4f), Iluminacao.Vela, 3.5f, 0.85f, 0.15f);

        foreach (int lado in new[] { -2, 2 })
        {
            if (Random.value >= 0.75f)
                continue;

            GameObject cad = Por(q, new[] { c + new Vector2Int(lado, 0) }, Um("Prisao/Cadeira"), Jeito.EmPe);

            if (cad != null)
                cad.GetComponent<SpriteRenderer>().flipX = lado > 0;
        }

        Por(q, q.Pertos(c, 2), Um("Prisao/Papel"), Jeito.Deitada);
    }

    // Um esqueleto no chao, ossos e velas (ou a chama magica azul de um ritual).
    private static void Ossario(Quarto q)
    {
        if (Encostar(q, 4f, false, Um("Prisao/Esqueleto"), Jeito.Deitada, out Vector2Int c) == null)
            return;

        bool ritual = Random.value < 0.4f && chama.Length > 0;

        for (int i = 0; i < Random.Range(3, 6); i++)
        {
            List<Vector2Int> perto = q.Pertos(c, 2);

            if (i >= 2)
                Por(q, perto, Um("Prisao/Ossos"), Jeito.Deitada);
            else if (ritual)
                Por(q, perto, null, Jeito.Chama);
            else
            {
                GameObject vela = Por(q, perto, Um("Prisao/Vela"), Jeito.EmPe);

                if (vela != null)
                    Iluminacao.Luz(vela.transform, new Vector2(0f, 0.3f), Iluminacao.Vela, 3f, 0.75f, 0.15f);
            }
        }
    }

    private static GameObject ChamaMagica(Vector2 onde, Transform pai)
    {
        GameObject obj = new GameObject("Chama magica");
        obj.transform.SetParent(pai, false);
        obj.transform.position = onde;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = chama[0];
        sr.sortingOrder = 10;
        obj.AddComponent<EnfeiteAnimado>().Comecar(chama, 10f);
        Iluminacao.Luz(obj.transform, new Vector2(0f, -0.3f), Iluminacao.Magica, 4f, 1f, 0.25f);
        return obj;
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
                encostadas = new[] { "Vila/Cabide", "Vila/Armadura", "Vila/BarrilDeArmas", "Vila/Cabide", "Vila/Armadura",
                                     "Vila/BarrilDeArmas", "Vila/Caixotes", "Vila/Barris" };
                soltas = new[] { "Vila/Elmo", "Vila/Elmo", "Vila/Sacos", "Vila/Elmo" };
                tapete = false;
                potes = true;
                break;
            default:
                return false;
        }

        Vector2 meio = q.sala.tipo == TipoDeSala.Loja ? MeioDaLoja(q.sala, q.planta) : MapaDeCaminhos.Celula(q.sala.Meio);

        // O que a sala tem no meio (os pedestais da loja, o item, o altar, a emboscada): nada encosta nele.
        desenhos.Add(q.sala.tipo == TipoDeSala.Loja
            ? new Rect(meio.x - 2.4f, meio.y - 1f, 4.8f, 2f)
            : new Rect(meio.x - 1.2f, meio.y - 0.8f, 2.4f, 2.4f));

        // O tapete embaixo do que a sala tem.
        Sprite desenhoDoTapete = tapete ? Um("Vila/Tapete") : null;

        if (desenhoDoTapete != null)
        {
            Vector2 onde = meio + new Vector2(0f, -1.4f);
            desenhos.Add(Retangulo(desenhoDoTapete, onde));
            Pequena(desenhoDoTapete, onde, q.pai, Pedreiro.OrdemDosEnfeites + 1).GetComponent<SpriteRenderer>().flipX = false;
        }

        foreach (string grupo in encostadas)
        {
            Sprite s = Um(grupo);

            if (s == null)
                continue;

            float largura = Mathf.Max(0.6f, s.bounds.size.x * 0.85f);

            if (Encostar(q, 2.4f, true, s, Jeito.Solida, out _, largura) == null)
                Encostar(q, 2.4f, false, s, Jeito.Solida, out _, largura);
        }

        // Candelabros de pe, acesos.
        for (int i = 0; i < 2; i++)
        {
            GameObject cand = Encostar(q, 2f, false, Um("Vila/CandelabroDePe"), Jeito.EmPe, out _);
            AcenderEmCima(cand, Iluminacao.Vela, 3.8f, 0.9f);
        }

        foreach (string grupo in soltas)
            Encostar(q, 1.3f, false, Um(grupo), Jeito.EmPe, out _);

        for (int i = 0; potes && i < Random.Range(2, 4); i++)
            Encostar(q, 1.2f, false, null, Jeito.Pote, out _);

        return true;
    }

    // A loja da caverna: o mercador atras de uma mesa (os pedestais ficam na frente).
    private static void Balcao(Quarto q)
    {
        Vector2 meio = MeioDaLoja(q.sala, q.planta);
        Sprite mesa = Um("Vila/Mesa");

        if (mesa != null)
        {
            Vector2 onde = meio + new Vector2(0f, 0.9f);
            GameObject obj = Pequena(mesa, onde, q.pai, 10);
            obj.GetComponent<SpriteRenderer>().flipX = false;
            obj.layer = Pedreiro.CamadaDaParede;
            BoxCollider2D c = obj.AddComponent<BoxCollider2D>();
            c.size = new Vector2(mesa.bounds.size.x * 0.9f, 0.8f);
            c.offset = new Vector2(0f, 0.4f);
            desenhos.Add(Retangulo(mesa, onde));
        }

        Vector2 mercador = meio + new Vector2(0f, 2.3f);
        Mercador.Criar(mercador, q.pai);
        desenhos.Add(new Rect(mercador.x - 0.6f, mercador.y - 0.8f, 1.2f, 1.8f));
    }

    /// <summary>
    /// Onde fica o meio da loja da caverna (os pedestais; a mesa e o mercador logo acima). O meio da sala, ou
    /// mais pra baixo quando a mesa nao cabe ali (o alto da sala e um nicho mais estreito que ela). O
    /// <see cref="RecheioDaCaverna"/> poe a loja no mesmo lugar.
    /// </summary>
    public static Vector2Int MeioDaLoja(SalaDaPlanta sala, HashSet<Vector2Int> chao)
    {
        Vector2Int meio = MapaDeCaminhos.Celula(sala.Meio);

        for (int desce = 0; desce <= 3; desce++)
        {
            Vector2Int m = meio + Vector2Int.down * desce;
            bool cabe = true;

            // A mesa (2,8 de largura, com a beirada das paredes) nas fileiras de cima; os pedestais embaixo. O
            // alto da mesa pode ficar na frente da parede de cima, mas nao de uma quina (parede com chao do lado).
            for (int x = -2; x <= 2 && cabe; x++)
            {
                for (int y = -1; y <= 2 && cabe; y++)
                    cabe = chao.Contains(m + new Vector2Int(x, y));

                Vector2Int alto = m + new Vector2Int(x, 3);
                cabe &= chao.Contains(alto) || (!chao.Contains(alto + Vector2Int.left) && !chao.Contains(alto + Vector2Int.right));
            }

            if (cabe)
                return m;
        }

        return meio;
    }

    // ------------------------------------------------------------------ Cripta e Profundezas

    private static void Cripta(Quarto q)
    {
        Grandes(q, true);

        // Bancos, livros e montes de velas acesas encostados nas paredes.
        int coisas = q.sala.DeLuta ? Random.Range(2, 4) : Random.Range(1, 3);

        for (int i = 0; i < coisas; i++)
        {
            float qual = Random.value;

            if (qual < 0.45f)
                Encostar(q, 2.5f, false, null, Jeito.Velas, out _);
            else if (qual < 0.75f)
                Encostar(q, 2.5f, false, Um("Cripta/Banco"), Jeito.Solida, out _, 1.6f);
            else
                Encostar(q, 2.5f, false, Um("Cripta/Livro"), Jeito.Solida, out _, 0.9f);
        }

        Miudas(q, true);
    }

    private static void Profundezas(Quarto q)
    {
        Grandes(q, false);

        if (Random.value < 0.25f)
            Encostar(q, 3f, false, Um("Profundezas/Espada"), Jeito.Solida, out _, 0.6f);

        Miudas(q, false);
    }

    // Pecas grandes encostadas nas paredes (ou na beirada, nas Profundezas): seguram gente e tiro.
    private static void Grandes(Quarto q, bool cripta)
    {
        int grandes = q.sala.DeLuta ? Random.Range(2, 4) : (q.sala.tipo == TipoDeSala.Loja ? 0 : Random.Range(1, 3));

        for (int i = 0; i < grandes; i++)
        {
            string qual = cripta ? Sortear("Cripta/Caixao", "Cripta/Estatua", "Cripta/Cruz", "Cripta/Candelabro")
                                 : Sortear("Profundezas/Estatua", "Profundezas/Cristal", "Profundezas/Cristal", "Profundezas/Candelabro");
            GameObject peca = Encostar(q, 2.5f, false, Um(qual), Jeito.Solida, out _);
            Acender(peca, qual);
        }
    }

    // Vasos na Cripta; montes de ouro e potes que quebram nas Profundezas. No meio do chao, com tudo em volta livre.
    private static void Miudas(Quarto q, bool cripta)
    {
        int miudas = Random.Range(2, 5);

        for (int i = 0; i < miudas; i++)
        {
            List<Vector2Int> lugares = q.NoMeio(1.6f);
            GameObject obj;
            Vector2Int onde;

            if (cripta)
                obj = Por(q, lugares, Um("Cripta/Vaso"), Jeito.EmPe, out onde);
            else if (Random.value < 0.6f)
                obj = Por(q, lugares, Um("Profundezas/Pote"), Jeito.Quebra, out onde);
            else
                obj = Por(q, lugares, Um("Profundezas/Ouro"), Jeito.Deitada, out onde);

            if (obj != null)
                q.usados.Add(onde);
        }
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

        // Teias nos cantos de cima (nos mundos com parede), onde nao tiver outra coisa na parede.
        if (mundo != 4)
        {
            foreach (Vector2Int c in q.sala.celulas)
            {
                bool esquerda = q.Parede(c + Vector2Int.left), direita = q.Parede(c + Vector2Int.right);

                if (q.planta.Contains(c) && q.Parede(c + Vector2Int.up) && (esquerda || direita) && Random.value < 0.6f)
                {
                    GameObject teia = Pendurar(Um("Vila/Teia"), (Vector2)c + new Vector2(esquerda ? -0.25f : 0.25f, 0.55f), q.pai, Pedreiro.OrdemDasParedes + 1);

                    if (teia != null)
                        teia.GetComponent<SpriteRenderer>().flipX = direita;
                }
            }
        }

        float noMeio = mundo == 4 ? 0.03f : 0.07f;
        float naBeirada = mundo == 4 ? 0.08f : 0.2f;

        foreach (Vector2Int c in new List<Vector2Int>(q.sala.celulas))
        {
            if (!q.livre.Contains(c) || Vector2.Distance(c, q.sala.Meio) < q.longeDoMeio)
                continue;

            if (Random.value >= (q.NaBeirada(c) ? naBeirada : noMeio))
                continue;

            GameObject obj = Por(q, new[] { c }, Um(tipos[Random.Range(0, tipos.Length)]), Jeito.Deitada);

            if (obj != null && mundo == 4)
                obj.GetComponent<SpriteRenderer>().color = new Color(0.75f, 0.85f, 0.8f);
        }
    }

    // Tochas na face da parede de cima, a cada 4 celulas, longe dos corredores; entre elas, o que o mundo tem.
    // Nada encosta no que ja esta na parede (o lancador de fogo).
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
                    Pendurar(Um("Cripta/Estandarte"), (Vector2)c + new Vector2(0f, 0.75f), pai, naParede);
                else if (tocha.Length > 0)
                    Tocha(c, pai);
            }
            else if (coluna == 0 && NaParedeDaSala(sala.tipo) != null)
            {
                string[] opcoes = NaParedeDaSala(sala.tipo);
                Pendurar(Um(opcoes[Random.Range(0, opcoes.Length)]), (Vector2)c + new Vector2(0f, 0.9f), pai, naParede);
            }
            else if (coluna == 0 && mundo <= 2)
            {
                float qual = Random.value;

                if (qual < 0.3f)
                    Pendurar(Um("Prisao/Estandarte"), (Vector2)c + new Vector2(0f, 0.95f), pai, naParede);
                else if (qual < 0.5f)
                    Pendurar(Um("Prisao/Retrato"), (Vector2)c + new Vector2(0f, 0.9f), pai, naParede);
                else if (qual < 0.72f)
                    Corrente(c, pai);
                else if (qual < 0.85f)
                    Pendurar(Um("Prisao/Acorrentado"), (Vector2)c + new Vector2(0f, 0.35f), pai, naParede);
            }
            else if (coluna == 0 && mundo == 3 && Random.value < 0.4f)
                Corrente(c, pai);
        }
    }

    // Uma tocha acesa na face da parede (o quadro da folha tem muita sobra: conta so a tocha).
    private static void Tocha(Vector2Int c, Transform pai)
    {
        Vector2 onde = (Vector2)c + new Vector2(0f, 1.15f);
        Rect r = new Rect(onde.x - 0.3f, onde.y - 0.6f, 0.6f, 1.4f);

        if (!Sobra(r))
            return;

        desenhos.Add(r);
        Animada(tocha, onde, pai, Pedreiro.OrdemDasParedes + 1, 10f);
    }

    // Uma corrente na parede de cima: as curtas presas na parede; as compridas descem do teto, na frente dela.
    private static void Corrente(Vector2Int c, Transform pai)
    {
        Sprite s = Um("Prisao/CorrenteDaParede");

        if (s == null)
            return;

        if (s.bounds.size.y > 2f)
            Pendurar(s, (Vector2)c + new Vector2(0f, -0.2f), pai, 10);
        else
            Pendurar(s, (Vector2)c + new Vector2(0f, 0.45f), pai, Pedreiro.OrdemDasParedes + 1);
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

    // O lancador de fogo na face da parede de cima, perto do meio. A frente dele fica livre (o fogo desce por ali).
    private static void Lancador(Quarto q)
    {
        SalaDaPlanta sala = q.sala;
        List<Vector2Int> servem = new List<Vector2Int>();

        foreach (Vector2Int c in sala.celulas)
        {
            if (q.planta.Contains(c) && !q.planta.Contains(c + Vector2Int.up) && !q.planta.Contains(c + Vector2Int.up * 2)
                && Mathf.Abs(c.x - sala.Meio.x) <= 3 && Mathf.Abs(c.x - sala.Meio.x) >= 1 && !PertoDeCorredor(c, q.planta))
                servem.Add(c);
        }

        if (servem.Count == 0)
            return;

        Vector2Int onde = servem[Random.Range(0, servem.Count)];
        Vector2 estatua = (Vector2)onde + new Vector2(0f, LancadorDeFogo.DoChao + 0.3f);

        if (LancadorDeFogo.Criar(estatua, q.pai, sala) == null)
            return;

        desenhos.Add(new Rect(estatua.x - 0.6f, estatua.y - 0.65f, 1.2f, 1.3f));

        // O caminho do fogo fica livre: o leque (3 bolas, 20 graus entre elas) ate o alcance, com uma
        // celula de folga pros lados.
        for (int dy = 0; dy >= -AlcanceDoFogo; dy--)
        {
            int largura = Mathf.CeilToInt(1.5f - dy * Mathf.Tan(20f * Mathf.Deg2Rad));

            for (int dx = -largura; dx <= largura; dx++)
                q.livre.Remove(onde + new Vector2Int(dx, dy));
        }
    }

    /// <summary>Ate onde vai o fogo do lancador (o alcance do FogoDaEstatua), em celulas.</summary>
    private const int AlcanceDoFogo = 9;

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
