using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A decoracao das salas, com as pecas dos pacotes (Resources/Decoracao, ver Ferramentas/Temas):
///
///   tochas acesas na parede de cima de cada sala (nos mundos com paredes) e, na Cripta, estandartes;
///   pecas grandes encostadas nas paredes, que seguram gente e tiro: caixoes, estatuas, cruzes e
///   candelabros na Cripta; estatuas douradas, cristais e candelabros nas Profundezas;
///   miudezas no chao: vasos na Cripta; montes de ouro e potes que quebram nas Profundezas;
///   nas Profundezas, rochas, pilares e estatuas saindo do vazio em volta das salas;
///   e, nas salas de luta da Cripta, o lancador de fogo na parede (<see cref="LancadorDeFogo"/>).
///
/// As tochas, os candelabros e os cristais tem luz (<see cref="Iluminacao"/>): o resto do andar e escuro.
///
/// Nada fica nos corredores nem na frente das portas; o que segura gente sai do mapa de caminhos.
/// </summary>
public static class Decoracao
{
    private static readonly Dictionary<string, Sprite[]> grupos = new Dictionary<string, Sprite[]>();
    private static Sprite[] tocha;

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

    /// <param name="mundo">1 Porao, 2 Catacumbas, 3 Cripta, 4 Profundezas.</param>
    /// <param name="chao">O chao andavel (o mesmo do mapa de caminhos: o que segura gente sai dele).</param>
    public static void Espalhar(int mundo, EstiloDeLadrilhos estilo, PlantaDeSalas.Planta salas, HashSet<Vector2Int> chao, Transform pai)
    {
        Transform grupo = new GameObject("Decoracao").transform;
        grupo.SetParent(pai, false);

        // Longe dos corredores e da frente das portas.
        HashSet<Vector2Int> livre = new HashSet<Vector2Int>(chao);

        foreach (Vector2Int c in salas.corredores)
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    livre.Remove(c + new Vector2Int(dx, dy));

        bool cripta = mundo == 3, profundezas = mundo == 4;
        List<Vector2> usados = new List<Vector2>();

        foreach (SalaDaPlanta sala in salas.salas)
        {
            // O meio das salas especiais e do comeco fica pro que elas tem (loja, item, o jogador).
            float longeDoMeio = sala.DeLuta ? 0f : 3f;

            if (!estilo.flutuante)
                NaParedeDeCima(sala, salas.chao, grupo, cripta);

            if (cripta || profundezas)
            {
                int grandes = sala.DeLuta ? Random.Range(2, 4) : (sala.tipo == TipoDeSala.Loja ? 0 : Random.Range(1, 3));

                for (int i = 0; i < grandes; i++)
                {
                    if (!Lugar(sala, livre, salas.chao, usados, 2.5f, longeDoMeio, true, estilo.flutuante, out Vector2Int c))
                        break;

                    string qual = cripta ? Sortear("Cripta/Caixao", "Cripta/Estatua", "Cripta/Cruz", "Cripta/Candelabro")
                                         : Sortear("Profundezas/Estatua", "Profundezas/Cristal", "Profundezas/Cristal", "Profundezas/Candelabro");
                    GameObject peca = Grande(Um(qual), c, grupo);
                    Acender(peca, qual);
                    chao.Remove(c);
                    livre.Remove(c);
                }

                int miudas = Random.Range(2, 5);

                for (int i = 0; i < miudas; i++)
                {
                    if (!Lugar(sala, livre, salas.chao, usados, 1.6f, longeDoMeio, false, estilo.flutuante, out Vector2Int c))
                        break;

                    if (cripta)
                        Pequena(Um("Cripta/Vaso"), (Vector2)c + Torto(), grupo, 9);
                    else if (Random.value < 0.6f)
                    {
                        Sprite pote = Um("Profundezas/Pote");

                        if (pote != null)
                            Quebravel.Vaso((Vector2)c + Torto() + Vector2.down * 0.3f, grupo, pote);
                    }
                    else
                        Pequena(Um("Profundezas/Ouro"), (Vector2)c + Torto(), grupo, Pedreiro.OrdemDosEnfeites + 1);
                }
            }

            // O lancador de fogo: numa sala de luta da Cripta (as vezes), no meio da parede de cima.
            if (cripta && sala.DeLuta && Random.value < 0.55f)
                Lancador(sala, salas, grupo);
        }

        if (profundezas)
            NoVazio(salas.chao, grupo);
    }

    private static string Sortear(params string[] opcoes) => opcoes[Random.Range(0, opcoes.Length)];

    private static Vector2 Torto() => new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(-0.2f, 0.2f));

    // Tochas (e estandartes, na Cripta) na face da parede de cima, a cada 4 celulas, longe dos corredores.
    private static void NaParedeDeCima(SalaDaPlanta sala, HashSet<Vector2Int> planta, Transform pai, bool cripta)
    {
        if (tocha == null)
            tocha = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Decoracao/Tocha"), new Vector2Int(64, 64), 32f);

        int n = 0;

        foreach (Vector2Int c in sala.celulas)
        {
            if (!planta.Contains(c) || planta.Contains(c + Vector2Int.up) || planta.Contains(c + Vector2Int.up * 2)
                || (c.x - sala.area.xMin) % 4 != 2 || PertoDeCorredor(c, planta))
                continue;

            if (cripta && n++ % 2 == 1)
                Pequena(Um("Cripta/Estandarte"), (Vector2)c + new Vector2(0f, 0.75f), pai, Pedreiro.OrdemDasParedes + 1);
            else if (tocha.Length > 0)
                Animada(tocha, (Vector2)c + new Vector2(0f, 1.15f), pai, Pedreiro.OrdemDasParedes + 1, 10f);
        }
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

    // Um lugar na sala: encostado na parede (ou na beirada, nas Profundezas) ou no meio do chao.
    private static bool Lugar(SalaDaPlanta sala, HashSet<Vector2Int> livre, HashSet<Vector2Int> planta, List<Vector2> usados,
                              float espaco, float longeDoMeio, bool naParede, bool flutuante, out Vector2Int onde)
    {
        List<Vector2Int> servem = new List<Vector2Int>();

        foreach (Vector2Int c in sala.celulas)
        {
            if (!livre.Contains(c) || Vector2.Distance(c, sala.Meio) < longeDoMeio)
                continue;

            bool encostado = flutuante ? Beirada(c, planta) : !planta.Contains(c + Vector2Int.up);

            if (naParede != encostado)
                continue;

            if (!naParede && !Cercado(c, livre))
                continue;

            if (usados.TrueForAll(u => Vector2.Distance(u, c) >= espaco))
                servem.Add(c);
        }

        onde = servem.Count > 0 ? servem[Random.Range(0, servem.Count)] : default;

        if (servem.Count > 0)
            usados.Add(onde);

        return servem.Count > 0;
    }

    private static bool Beirada(Vector2Int c, HashSet<Vector2Int> planta)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (!planta.Contains(c + new Vector2Int(dx, dy)))
                    return true;

        return false;
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
        float altura = sr.sprite != null ? sr.sprite.bounds.size.y : 1f;

        if (grupo.EndsWith("Candelabro"))
            Iluminacao.Luz(peca.transform, new Vector2(0f, altura * 0.85f), Iluminacao.Vela, 4f, 0.95f, 0.15f);
        else if (grupo.EndsWith("Cristal"))
        {
            int numero;
            bool roxo = int.TryParse(sr.sprite.name, out numero) && numero < 9;
            Color cor = roxo ? new Color(0.65f, 0.45f, 1f) : new Color(0.4f, 1f, 0.6f);
            Iluminacao.Luz(peca.transform, new Vector2(0f, altura * 0.5f), cor, 3.6f, 0.9f, 0.03f);
        }
    }

    // Uma peca grande, de pe na celula: segura gente e tiro (camada das paredes).
    private static GameObject Grande(Sprite desenho, Vector2Int c, Transform pai)
    {
        if (desenho == null)
            return null;

        GameObject obj = Pequena(desenho, (Vector2)c + Vector2.down * 0.4f, pai, 10);
        obj.layer = Pedreiro.CamadaDaParede;
        BoxCollider2D colisor = obj.AddComponent<BoxCollider2D>();
        float largura = Mathf.Clamp(desenho.bounds.size.x * 0.8f, 0.5f, 1.6f);
        colisor.size = new Vector2(largura, 0.6f);
        colisor.offset = new Vector2(0f, 0.3f);
        return obj;
    }

    private static GameObject Pequena(Sprite desenho, Vector2 onde, Transform pai, int ordem)
    {
        GameObject obj = new GameObject(desenho != null ? desenho.name : "Peca");
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
        Iluminacao.Luz(obj.transform, new Vector2(0f, 0.25f), Iluminacao.Fogo, 6.5f, 1.15f, 0.18f);
    }

    /// <summary>Tochas na parede de cima do salao do chefe (a cada 4 celulas).</summary>
    public static void TochasDaArena(HashSet<Vector2Int> planta, Transform pai)
    {
        if (tocha == null)
            tocha = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Decoracao/Tocha"), new Vector2Int(64, 64), 32f);

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
