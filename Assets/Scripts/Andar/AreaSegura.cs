using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// A area segura entre os mundos: depois de vencer o chefe, antes do proximo mundo, um lugar aberto e
/// claro (sem escuro e sem inimigos), cercado de arvores. Tem:
///
///   o mercador (pacote Ancient Ruins) com a barraca dele (caixotes, sacos, barris e o expositor do
///   pacote Village) e tres coisas a venda (<see cref="Loja"/>);
///   a fonte que cura tudo (<see cref="FonteDaCura"/>), uma vez;
///   o calice dos espiritos (<see cref="CaliceDosEspiritos"/>), que da um coracao e moedas, uma vez;
///   a criatura da sorte (<see cref="CriaturaDaSorte"/>), que anda por ali e solta moedas num carinho;
///   um bau de graca (o bau do Village) e potes que quebram;
///   e o portal pro proximo mundo: na clareira, no meio de um circulo de pedras; nas ruinas, em cima do
///   altar, que acende quando o jogador chega perto (<see cref="AltarDoPortal"/>).
///
/// Depois do mundo 1, a Clareira (pacote Grass Land); depois dos outros, as Ruinas Antigas (pacote
/// Ancient Ruins), ao entardecer depois do mundo 3. O jogador chega no meio (0, 0).
/// </summary>
public static class AreaSegura
{
    private const float Pixels = 32f;
    private const int RaioX = 17, RaioY = 11;

    /// <summary>O nome que aparece ao chegar.</summary>
    public static string Nome(int mundoQueAcabou) =>
        mundoQueAcabou <= 1 ? "Clareira (área segura)" : mundoQueAcabou == 2 ? "Ruínas Antigas (área segura)" : "Ruínas ao Entardecer (área segura)";

    /// <summary>A luz e o tom do lugar (as ruinas do mundo 3 ficam no fim da tarde).</summary>
    public static float Luz(int mundoQueAcabou) => mundoQueAcabou >= 3 ? 0.72f : 1f;

    public static Color Tom(int mundoQueAcabou) => mundoQueAcabou >= 3 ? new Color(1f, 0.8f, 0.62f) : Color.white;

    /// <summary>A cor do fundo da camera (alem das arvores).</summary>
    public static Color Fundo(int mundoQueAcabou) => mundoQueAcabou <= 1 ? new Color32(28, 56, 30, 255) : new Color32(46, 52, 26, 255);

    private class Lugar
    {
        public Transform pai;
        public HashSet<Vector2Int> chao;
        public List<Vector3> ocupado = new List<Vector3>();   // (x, y, raio)

        public bool Livre(Vector2 p, float raio)
        {
            foreach (Vector3 o in ocupado)
                if (Vector2.Distance(o, p) < o.z + raio)
                    return false;

            return true;
        }

        public void Ocupar(Vector2 p, float raio) => ocupado.Add(new Vector3(p.x, p.y, raio));
    }

    /// <summary>Monta o lugar dentro de <paramref name="raiz"/> e devolve o chao andavel.</summary>
    public static HashSet<Vector2Int> Montar(int mundoQueAcabou, Transform raiz, GeradorDoAndar gerador)
    {
        bool ruinas = mundoQueAcabou >= 2;
        string tema = ruinas ? "Areas/Ruinas/" : "Areas/Grama/";
        float semente = Random.value * 1000f;

        Lugar lugar = new Lugar { pai = raiz, chao = new HashSet<Vector2Int>() };

        // O chao andavel: uma elipse torta.
        for (int x = -RaioX - 2; x <= RaioX + 2; x++)
        {
            for (int y = -RaioY - 2; y <= RaioY + 2; y++)
            {
                float d = (x * x) / (float)(RaioX * RaioX) + (y * y) / (float)(RaioY * RaioY);
                float torto = (Mathf.PerlinNoise(semente + x * 0.15f, semente + y * 0.15f) - 0.5f) * 0.35f;

                if (d + torto < 1f)
                    lugar.chao.Add(new Vector2Int(x, y));
            }
        }

        // O comeco e o caminho ate o portal ficam livres.
        lugar.Ocupar(Vector2.zero, 2.2f);

        Chao(lugar, tema, ruinas);
        Limites(lugar);
        Arvores(lugar, tema, ruinas);

        // ---- o que tem no lugar
        Vector2 portal = ruinas ? new Vector2(0f, 8.6f) : new Vector2(0f, 7.5f);

        if (ruinas)
            AltarDoPortal.Criar(new Vector2(0f, 8f), raiz, gerador, portal);
        else
        {
            gerador.AbrirPortalDoDescanso(portal);
            CirculoDePedras(lugar, portal);
        }

        lugar.Ocupar(portal, ruinas ? 3.8f : 2f);
        for (float y = 1.5f; y < portal.y - 1f; y += 1.5f)
            lugar.Ocupar(new Vector2(0f, y), 1.3f);

        Barraca(lugar, new Vector2(-9f, 0.5f), gerador);
        FonteDaCura.Criar(new Vector2(9f, 0.5f), raiz);
        lugar.Ocupar(new Vector2(9f, 0.8f), 3f);
        CaliceDosEspiritos.Criar(new Vector2(6f, 6.5f), raiz);
        lugar.Ocupar(new Vector2(6f, 6.5f), 1.5f);
        CriaturaDaSorte.Criar(new Vector2(5f, -6f), raiz);
        lugar.Ocupar(new Vector2(5f, -6f), 1.5f);

        // Um bau de graca.
        Sprite[] bau = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Decoracao/Vila/BauAbrindo"), new Vector2Int(192, 192), Pixels);

        if (bau.Length > 0)
        {
            Bau.Criar(bau, new Vector2(-6f, 6f), raiz, gerador.ArmasDoBau, null, null, null);
            lugar.Ocupar(new Vector2(-6f, 6f), 1.5f);
        }

        if (ruinas)
            Ruinas(lugar);

        Miudezas(lugar, tema, ruinas);
        Particulas(lugar);
        return lugar.chao;
    }

    // ---------------------------------------------------------------- chao

    private static void Chao(Lugar lugar, string tema, bool ruinas)
    {
        Tile[] grama = Ladrilhos(Resources.Load<Texture2D>(tema + "Chao"));
        Tile[] piso = ruinas ? Ladrilhos(Resources.Load<Texture2D>("Areas/Ruinas/Piso")) : new Tile[0];

        if (grama.Length == 0)
            return;

        GameObject grid = new GameObject("Chao");
        grid.transform.SetParent(lugar.pai, false);
        grid.AddComponent<Grid>();
        Tilemap mapa = new GameObject("Grama").AddComponent<Tilemap>();
        mapa.transform.SetParent(grid.transform, false);
        mapa.gameObject.AddComponent<TilemapRenderer>().sortingOrder = Pedreiro.OrdemDoChao;

        // A grama vai alem do chao andavel (embaixo das arvores).
        for (int x = -RaioX - 12; x <= RaioX + 12; x++)
        {
            for (int y = -RaioY - 10; y <= RaioY + 12; y++)
            {
                Vector3Int c = new Vector3Int(x, y, 0);
                Tile[] de = ruinas && piso.Length > 0 && NoPiso(x, y) ? piso : grama;
                mapa.SetTile(c, de[Random.Range(0, de.Length)]);
            }
        }

        // O ladrilho (x, y) cobre do ponto (x, y) ao (x + 1, y + 1): a celula c fica no meio dele.
        mapa.transform.localPosition = new Vector3(-0.5f, -0.5f, 0f);
    }

    // Nas ruinas: o patio de pedra do altar e o caminho do comeco ate ele.
    private static bool NoPiso(int x, int y) =>
        (Mathf.Abs(x) <= 1 && y >= 1 && y <= 4) || (Mathf.Abs(x) <= 4 && y >= 3 && y <= 12 && Mathf.Abs(x) + Mathf.Max(0, 5 - y) <= 5);

    private static Tile[] Ladrilhos(Texture2D folha)
    {
        if (folha == null)
            return new Tile[0];

        Sprite[] quadros = FolhaDeSprites.Cortar(folha, new Vector2Int(32, 32), Pixels);
        Tile[] saida = new Tile[quadros.Length];

        for (int i = 0; i < quadros.Length; i++)
        {
            saida[i] = ScriptableObject.CreateInstance<Tile>();
            saida[i].sprite = quadros[i];
        }

        return saida;
    }

    // Paredes invisiveis em volta do chao andavel (camada das paredes: segura gente e tiro).
    private static void Limites(Lugar lugar)
    {
        GameObject limites = new GameObject("Limites");
        limites.transform.SetParent(lugar.pai, false);
        limites.layer = Pedreiro.CamadaDaParede;

        foreach (Vector2Int c in lugar.chao)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    Vector2Int fora = c + new Vector2Int(dx, dy);

                    if (lugar.chao.Contains(fora))
                        continue;

                    BoxCollider2D parede = limites.AddComponent<BoxCollider2D>();
                    parede.offset = fora;
                    parede.size = Vector2.one;
                }
            }
        }
    }

    // A mata em volta: arvores bem juntas logo fora do chao, e mais esparsas mais longe.
    private static void Arvores(Lugar lugar, string tema, bool ruinas)
    {
        Sprite[] arvores = Resources.LoadAll<Sprite>(tema + "Arvore");
        Sprite[] arbustos = Resources.LoadAll<Sprite>(tema + "Arbusto");

        if (arvores.Length == 0)
            return;

        Transform mata = new GameObject("Mata").transform;
        mata.SetParent(lugar.pai, false);
        List<Vector2> postas = new List<Vector2>();

        for (int volta = 0; volta < 2; volta++)
        {
            float de = volta == 0 ? 0.95f : 1.25f, ate = volta == 0 ? 1.25f : 1.75f;
            float espaco = volta == 0 ? 2.1f : 2.6f;

            for (int tentativa = 0; tentativa < 1400; tentativa++)
            {
                float angulo = Random.value * Mathf.PI * 2f;
                float k = Random.Range(de, ate);
                Vector2 p = new Vector2(Mathf.Cos(angulo) * (RaioX + 1.5f) * k, Mathf.Sin(angulo) * (RaioY + 1.5f) * k);

                if (lugar.chao.Contains(Vector2Int.RoundToInt(p)) || !postas.TrueForAll(o => Vector2.Distance(o, p) >= espaco))
                    continue;

                postas.Add(p);
                Peca(arvores[Random.Range(0, arvores.Length)], p, mata, 10);

                // Arbustos no pe de algumas.
                if (arbustos.Length > 0 && Random.value < 0.35f)
                    Peca(arbustos[Random.Range(0, arbustos.Length)], p + new Vector2(Random.Range(-1.2f, 1.2f), -0.6f), mata, 10);
            }
        }
    }

    private static void CirculoDePedras(Lugar lugar, Vector2 meio)
    {
        Sprite[] pedras = Resources.LoadAll<Sprite>("Areas/Grama/Pedra");

        if (pedras.Length == 0)
            return;

        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2f / 8f;
            Peca(pedras[Random.Range(0, pedras.Length)], meio + new Vector2(Mathf.Cos(a) * 1.8f, Mathf.Sin(a) * 1.2f - 0.3f), lugar.pai, 10);
        }
    }

    // ---------------------------------------------------------------- a barraca do mercador

    private static void Barraca(Lugar lugar, Vector2 onde, GeradorDoAndar gerador)
    {
        Transform pai = lugar.pai;
        Mercador.Criar(onde + new Vector2(0f, 1.6f), pai);

        // O tapete embaixo das coisas a venda e a loja (sem o balcao do jogo antigo).
        Peca(Um("Decoracao/Vila/Tapete"), onde + new Vector2(0f, -1f), pai, Pedreiro.OrdemDosEnfeites + 1);
        GameObject jogador = gerador.Jogador != null ? gerador.Jogador.gameObject : null;
        Loja.Criar(onde + new Vector2(0f, -0.6f), pai, jogador != null ? jogador.GetComponent<EstatisticasDoJogador>() : null, false);

        // A mercadoria em volta: o expositor de potes, caixotes, sacos e barris.
        Solida(Um("Decoracao/Vila/Expositor"), onde + new Vector2(-2.6f, 1.2f), pai);
        Solida(Um("Decoracao/Vila/CaixotesDeSuprimento"), onde + new Vector2(2.6f, 1.2f), pai);
        Peca(Um("Decoracao/Vila/Sacos"), onde + new Vector2(-3.4f, -0.6f), pai, 10);
        Solida(Um("Decoracao/Vila/BarrisDeGrao"), onde + new Vector2(3.5f, -0.4f), pai);
        Peca(Um("Decoracao/Vila/Cesta"), onde + new Vector2(1.4f, 2.3f), pai, 10);

        // Potes que quebram (as vezes tem moeda).
        for (int i = 0; i < 4; i++)
            Quebravel.Pote(onde + new Vector2(-4.5f + i * 0.6f + Random.Range(-0.1f, 0.1f), 2.4f + Random.Range(-0.2f, 0.2f)), pai);

        lugar.Ocupar(onde + new Vector2(0f, 0.5f), 4.5f);
    }

    // ---------------------------------------------------------------- as ruinas

    private static void Ruinas(Lugar lugar)
    {
        Sprite[] pilares = Resources.LoadAll<Sprite>("Areas/Ruinas/Pilar");
        Sprite[] muros = Resources.LoadAll<Sprite>("Areas/Ruinas/Muro");

        // A casinha antiga, num canto.
        Sprite casa = Resources.Load<Sprite>("Areas/Ruinas/Estrutura");

        if (casa != null)
        {
            Vector2 onde = new Vector2(-12.5f, -5f);
            Solida(casa, onde, lugar.pai, 4.5f);
            lugar.Ocupar(onde + Vector2.up * 1.5f, 3.5f);
        }

        // Pilares e muros caidos espalhados.
        for (int i = 0; i < 9; i++)
        {
            Sprite s = i < 5 ? (pilares.Length > 0 ? pilares[Random.Range(0, pilares.Length)] : null)
                             : (muros.Length > 0 ? muros[Random.Range(0, muros.Length)] : null);

            if (s != null && Ponto(lugar, 1.6f, out Vector2 p))
                Solida(s, p, lugar.pai);
        }
    }

    // ---------------------------------------------------------------- miudezas

    private static void Miudezas(Lugar lugar, string tema, bool ruinas)
    {
        Sprite[] tufos = Resources.LoadAll<Sprite>("Areas/Grama/Tufo");
        Sprite[] arbustos = Resources.LoadAll<Sprite>(tema + "Arbusto");
        Sprite[] pedras = Resources.LoadAll<Sprite>(tema + "Pedra");
        Sprite[] plantas = Resources.LoadAll<Sprite>("Areas/Ruinas/Planta");

        // Capim alto: muito, em moitas (desenhado em pe, o jogador passa na frente ou atras).
        if (tufos.Length > 0)
        {
            for (int moita = 0; moita < 26; moita++)
            {
                if (!Ponto(lugar, 0.4f, out Vector2 meio, false))
                    continue;

                for (int i = Random.Range(4, 10); i > 0; i--)
                {
                    Vector2 p = meio + Random.insideUnitCircle * 1.3f;

                    if (lugar.chao.Contains(Vector2Int.RoundToInt(p)) && lugar.Livre(p, 0.2f))
                        Peca(tufos[Random.Range(0, tufos.Length)], p, lugar.pai, 10);
                }
            }
        }

        Espalhar(lugar, arbustos, ruinas ? 7 : 10, 1.2f);
        Espalhar(lugar, pedras, ruinas ? 12 : 7, 0.8f);

        if (ruinas)
            Espalhar(lugar, plantas, 8, 0.6f);
    }

    private static void Espalhar(Lugar lugar, Sprite[] pecas, int quantas, float espaco)
    {
        if (pecas.Length == 0)
            return;

        for (int i = 0; i < quantas; i++)
        {
            if (Ponto(lugar, espaco, out Vector2 p))
                Peca(pecas[Random.Range(0, pecas.Length)], p, lugar.pai, 10);
        }
    }

    // Particulas boiando no ar (folhinhas e po brilhando).
    private static void Particulas(Lugar lugar)
    {
        Sprite[] quadros = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Areas/Ruinas/Particulas"), new Vector2Int(64, 64), Pixels);

        if (quadros.Length == 0)
            return;

        for (int i = 0; i < 14; i++)
        {
            Vector2 p = new Vector2(Random.Range(-RaioX, RaioX), Random.Range(-RaioY, RaioY));
            GameObject obj = new GameObject("Particulas");
            obj.transform.SetParent(lugar.pai, false);
            obj.transform.position = p;
            obj.transform.localScale = Vector3.one * 1.5f;
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = quadros[0];
            sr.sortingOrder = 40;
            obj.AddComponent<EnfeiteAnimado>().Comecar(quadros, 8f);
        }
    }

    // Um ponto livre no chao andavel (longe do que ja esta la).
    private static bool Ponto(Lugar lugar, float raio, out Vector2 onde, bool ocupar = true)
    {
        List<Vector2Int> celulas = new List<Vector2Int>(lugar.chao);

        for (int tentativa = 0; tentativa < 60; tentativa++)
        {
            Vector2 p = celulas[Random.Range(0, celulas.Count)] + new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f));

            if (!lugar.Livre(p, raio) || Beirada(lugar, p))
                continue;

            if (ocupar)
                lugar.Ocupar(p, raio);

            onde = p;
            return true;
        }

        onde = default;
        return false;
    }

    private static bool Beirada(Lugar lugar, Vector2 p)
    {
        Vector2Int c = Vector2Int.RoundToInt(p);

        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (!lugar.chao.Contains(c + new Vector2Int(dx, dy)))
                    return true;

        return false;
    }

    // ---------------------------------------------------------------- pecas

    private static Sprite Um(string grupo)
    {
        Sprite[] s = Resources.LoadAll<Sprite>(grupo);
        return s.Length > 0 ? s[Random.Range(0, s.Length)] : null;
    }

    /// <summary>Uma peca de pe (o pivo da arte e o pe), ordenada pela altura do pe.</summary>
    public static GameObject Peca(Sprite desenho, Vector2 onde, Transform pai, int ordem)
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
        sr.flipX = Random.value < 0.5f;
        return obj;
    }

    // Uma peca que segura gente e tiro (o colisor no pe dela).
    private static GameObject Solida(Sprite desenho, Vector2 onde, Transform pai, float largura = 0f)
    {
        GameObject obj = Peca(desenho, onde, pai, 10);

        if (obj == null)
            return null;

        obj.layer = Pedreiro.CamadaDaParede;
        BoxCollider2D c = obj.AddComponent<BoxCollider2D>();
        c.size = new Vector2(largura > 0f ? largura : Mathf.Max(0.6f, desenho.bounds.size.x * 0.85f), 0.7f);
        c.offset = new Vector2(0f, 0.35f);
        return obj;
    }
}
