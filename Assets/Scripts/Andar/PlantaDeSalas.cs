using System.Collections.Generic;
using UnityEngine;

/// <summary>O que cada sala da planta e.</summary>
public enum TipoDeSala
{
    /// <summary>Onde o jogador nasce (no centro do mundo). Sem inimigos.</summary>
    Comeco,

    /// <summary>Uma sala de luta: as portas fecham quando o jogador entra e so abrem com todos mortos.</summary>
    Luta,

    /// <summary>A sala de luta mais longe do comeco, maior e com uma onda a mais.</summary>
    Fim,

    Loja,
    Tesouro,
    Altar,
    Desafio,
}

/// <summary>Uma sala da planta: o retangulo dela, as celulas de chao e as portas.</summary>
public class SalaDaPlanta
{
    public TipoDeSala tipo;

    /// <summary>O retangulo da sala. As celulas podem sair um pouco dele (nichos) ou faltar (cantos e pilares).</summary>
    public RectInt area;

    /// <summary>Onde a sala fica na grade das salas (o comeco e 0, 0).</summary>
    public Vector2Int naGrade;

    /// <summary>Quantas salas ate o comeco.</summary>
    public int distancia;

    public readonly HashSet<Vector2Int> celulas = new HashSet<Vector2Int>();
    public readonly List<PortaDaPlanta> portas = new List<PortaDaPlanta>();

    public Vector2 Meio => new Vector2(area.x + (area.width - 1) * 0.5f, area.y + (area.height - 1) * 0.5f);

    /// <summary>Tem inimigos e porta que fecha.</summary>
    public bool DeLuta => tipo == TipoDeSala.Luta || tipo == TipoDeSala.Fim;

    /// <summary>A celula esta dentro do retangulo, longe das beiradas (as portas ficam do lado de fora).</summary>
    public bool BemDentro(Vector2 ponto, float folga) =>
        ponto.x >= area.xMin - 0.5f + folga && ponto.x <= area.xMax - 0.5f - folga &&
        ponto.y >= area.yMin - 0.5f + folga && ponto.y <= area.yMax - 0.5f - folga;
}

/// <summary>Uma porta de sala de luta: a fileira de celulas do corredor encostada na sala.</summary>
public class PortaDaPlanta
{
    public readonly List<Vector2Int> celulas = new List<Vector2Int>();

    /// <summary>Pra onde fica a sala, saindo da porta.</summary>
    public Vector2Int paraDentro;

    /// <summary>A porta atravessa um corredor que sobe ou desce (fica de lado a lado, vista de frente).</summary>
    public bool DeFrente => paraDentro.y != 0;
}

/// <summary>
/// Monta a planta de um andar em salas ligadas por corredores curtos, como no Enter the Gungeon: a sala
/// do comeco no centro do mundo, um caminho de salas de luta ate a do fim, e salas do lado (mais lutas
/// e as especiais: loja, tesouro, altar, desafio). As salas ficam numa grade; cada uma liga com a de
/// onde saiu, e as vezes duas lutas vizinhas tambem se ligam (um atalho, pra nao ter que voltar tudo).
///
/// Cada sala e um retangulo com os cantos as vezes cortados, nichos nas paredes sem corredor e, nas
/// grandes, pilares pra se esconder dos tiros. Os corredores tem 3 de largura; a ponta de cada um que
/// encosta numa sala de luta e a porta dela.
/// </summary>
public static class PlantaDeSalas
{
    // A distancia entre os meios de duas salas vizinhas na grade. Com as salas de ate 15 x 12, sobra
    // parede de 4 entre duas do lado (o corredor) e de 5 entre duas uma em cima da outra.
    public const int PassoX = 19;
    public const int PassoY = 17;
    private const int LarguraDoCorredor = 3;

    private static readonly Vector2Int[] Direcoes = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    public class Planta
    {
        public readonly HashSet<Vector2Int> chao = new HashSet<Vector2Int>();
        public readonly List<SalaDaPlanta> salas = new List<SalaDaPlanta>();
        public readonly HashSet<Vector2Int> corredores = new HashSet<Vector2Int>();
        public readonly HashSet<Vector2Int> portas = new HashSet<Vector2Int>();
    }

    /// <param name="noCaminho">Salas de luta do comeco ate o fim (contando a do fim).</param>
    /// <param name="lutasDoLado">Salas de luta fora do caminho.</param>
    /// <param name="especiais">As salas especiais deste andar (loja, tesouro...), sempre do lado.</param>
    /// <param name="clareira">Metade da largura e da altura da sala do comeco.</param>
    public static Planta Montar(int noCaminho, int lutasDoLado, List<TipoDeSala> especiais, Vector2Int clareira)
    {
        Planta planta = new Planta();
        Dictionary<Vector2Int, SalaDaPlanta> grade = new Dictionary<Vector2Int, SalaDaPlanta>();
        List<KeyValuePair<SalaDaPlanta, SalaDaPlanta>> ligacoes = new List<KeyValuePair<SalaDaPlanta, SalaDaPlanta>>();

        SalaDaPlanta comeco = Nova(planta, grade, TipoDeSala.Comeco, Vector2Int.zero, 0);
        SalaDaPlanta atual = comeco;

        // O caminho: cada sala nova encosta na anterior, de preferencia num lugar com um vizinho so
        // (assim o caminho nao se embola).
        for (int i = 0; i < noCaminho; i++)
        {
            Vector2Int onde;

            if (!Livre(grade, atual.naGrade, true, out onde) && !Livre(grade, atual.naGrade, false, out onde))
            {
                // Sem saida: volta pra uma sala qualquer que ainda tenha lado livre.
                atual = QualquerComLadoLivre(planta, grade, null);

                if (atual == null || !Livre(grade, atual.naGrade, false, out onde))
                    break;
            }

            SalaDaPlanta nova = Nova(planta, grade, i == noCaminho - 1 ? TipoDeSala.Fim : TipoDeSala.Luta, onde, atual.distancia + 1);
            ligacoes.Add(new KeyValuePair<SalaDaPlanta, SalaDaPlanta>(atual, nova));
            atual = nova;
        }

        // As do lado: penduradas numa sala do caminho (nunca na do fim), de preferencia num beco.
        List<TipoDeSala> doLado = new List<TipoDeSala>(especiais);

        for (int i = 0; i < lutasDoLado; i++)
            doLado.Insert(Random.Range(0, doLado.Count + 1), TipoDeSala.Luta);

        foreach (TipoDeSala tipo in doLado)
        {
            SalaDaPlanta dona = QualquerComLadoLivre(planta, grade, s => s.tipo != TipoDeSala.Fim && (s.DeLuta || s.tipo == TipoDeSala.Comeco));

            if (dona == null)
                continue;

            Vector2Int onde;

            if (!Livre(grade, dona.naGrade, true, out onde) && !Livre(grade, dona.naGrade, false, out onde))
                continue;

            SalaDaPlanta nova = Nova(planta, grade, tipo, onde, dona.distancia + 1);
            ligacoes.Add(new KeyValuePair<SalaDaPlanta, SalaDaPlanta>(dona, nova));
        }

        // Um atalho as vezes: duas lutas vizinhas na grade que ainda nao se ligam.
        if (Random.value < 0.5f)
        {
            List<KeyValuePair<SalaDaPlanta, SalaDaPlanta>> atalhos = new List<KeyValuePair<SalaDaPlanta, SalaDaPlanta>>();

            foreach (SalaDaPlanta a in planta.salas)
            {
                foreach (Vector2Int d in Direcoes)
                {
                    if (d.x < 0 || d.y < 0 || !grade.TryGetValue(a.naGrade + d, out SalaDaPlanta b))
                        continue;

                    if ((a.DeLuta || a.tipo == TipoDeSala.Comeco) && b.DeLuta && !Ligadas(ligacoes, a, b))
                        atalhos.Add(new KeyValuePair<SalaDaPlanta, SalaDaPlanta>(a, b));
                }
            }

            if (atalhos.Count > 0)
                ligacoes.Add(atalhos[Random.Range(0, atalhos.Count)]);
        }

        // Os tamanhos e as celulas de cada sala (sabendo de que lado chegam corredores).
        foreach (SalaDaPlanta sala in planta.salas)
            Desenhar(sala, clareira, LadosComVizinho(grade, sala));

        foreach (KeyValuePair<SalaDaPlanta, SalaDaPlanta> ligacao in ligacoes)
            Corredor(planta, ligacao.Key, ligacao.Value);

        foreach (SalaDaPlanta sala in planta.salas)
            planta.chao.UnionWith(sala.celulas);

        planta.chao.UnionWith(planta.corredores);

        // As paredes do Old Prison so abrem chao: as portas continuam no lugar, mas podem crescer.
        Caverna.Ajeitar(planta.chao);

        foreach (SalaDaPlanta sala in planta.salas)
        {
            foreach (PortaDaPlanta porta in sala.portas)
            {
                Completar(planta.chao, porta);
                planta.portas.UnionWith(porta.celulas);
            }
        }

        return planta;
    }

    private static SalaDaPlanta Nova(Planta planta, Dictionary<Vector2Int, SalaDaPlanta> grade, TipoDeSala tipo, Vector2Int onde, int distancia)
    {
        SalaDaPlanta sala = new SalaDaPlanta { tipo = tipo, naGrade = onde, distancia = distancia };
        grade[onde] = sala;
        planta.salas.Add(sala);
        return sala;
    }

    // Um lugar vazio do lado (com soVizinho: que so encoste nesta sala).
    private static bool Livre(Dictionary<Vector2Int, SalaDaPlanta> grade, Vector2Int de, bool soVizinho, out Vector2Int onde)
    {
        List<Vector2Int> servem = new List<Vector2Int>();

        foreach (Vector2Int d in Direcoes)
        {
            Vector2Int c = de + d;

            if (grade.ContainsKey(c))
                continue;

            if (soVizinho && Vizinhos(grade, c) > 1)
                continue;

            servem.Add(c);
        }

        onde = servem.Count > 0 ? servem[Random.Range(0, servem.Count)] : default;
        return servem.Count > 0;
    }

    private static int Vizinhos(Dictionary<Vector2Int, SalaDaPlanta> grade, Vector2Int c)
    {
        int n = 0;

        foreach (Vector2Int d in Direcoes)
        {
            if (grade.ContainsKey(c + d))
                n++;
        }

        return n;
    }

    private static SalaDaPlanta QualquerComLadoLivre(Planta planta, Dictionary<Vector2Int, SalaDaPlanta> grade, System.Predicate<SalaDaPlanta> serve)
    {
        List<SalaDaPlanta> boas = new List<SalaDaPlanta>();
        List<SalaDaPlanta> servem = new List<SalaDaPlanta>();

        foreach (SalaDaPlanta sala in planta.salas)
        {
            if (serve != null && !serve(sala))
                continue;

            Vector2Int onde;

            if (Livre(grade, sala.naGrade, true, out onde))
                boas.Add(sala);
            else if (Livre(grade, sala.naGrade, false, out onde))
                servem.Add(sala);
        }

        List<SalaDaPlanta> lista = boas.Count > 0 ? boas : servem;
        return lista.Count > 0 ? lista[Random.Range(0, lista.Count)] : null;
    }

    private static bool Ligadas(List<KeyValuePair<SalaDaPlanta, SalaDaPlanta>> ligacoes, SalaDaPlanta a, SalaDaPlanta b)
    {
        foreach (KeyValuePair<SalaDaPlanta, SalaDaPlanta> l in ligacoes)
        {
            if ((l.Key == a && l.Value == b) || (l.Key == b && l.Value == a))
                return true;
        }

        return false;
    }

    // Os lados da sala que tem outra sala na grade (ligada ou nao): ali nao cabe nicho.
    private static List<Vector2Int> LadosComVizinho(Dictionary<Vector2Int, SalaDaPlanta> grade, SalaDaPlanta sala)
    {
        List<Vector2Int> lados = new List<Vector2Int>();

        foreach (Vector2Int d in Direcoes)
        {
            if (grade.ContainsKey(sala.naGrade + d))
                lados.Add(d);
        }

        return lados;
    }

    // ---------------- cada sala ----------------
    private static void Desenhar(SalaDaPlanta sala, Vector2Int clareira, List<Vector2Int> ladosComVizinho)
    {
        Vector2Int tamanho;

        switch (sala.tipo)
        {
            case TipoDeSala.Comeco: tamanho = new Vector2Int(clareira.x * 2 + 1, clareira.y * 2 + 1); break;
            case TipoDeSala.Fim: tamanho = new Vector2Int(15, 12); break;
            case TipoDeSala.Luta: tamanho = new Vector2Int(Random.Range(11, 16), Random.Range(9, 13)); break;
            case TipoDeSala.Desafio: tamanho = new Vector2Int(13, 10); break;
            default: tamanho = new Vector2Int(Random.Range(9, 12), Random.Range(7, 10)); break;
        }

        // A sala do comeco fica centrada no 0, 0 (o jogador nasce la).
        Vector2Int meio = new Vector2Int(sala.naGrade.x * PassoX, sala.naGrade.y * PassoY);
        sala.area = new RectInt(meio.x - tamanho.x / 2, meio.y - tamanho.y / 2, tamanho.x, tamanho.y);

        for (int x = sala.area.xMin; x < sala.area.xMax; x++)
            for (int y = sala.area.yMin; y < sala.area.yMax; y++)
                sala.celulas.Add(new Vector2Int(x, y));

        if (sala.tipo == TipoDeSala.Comeco)
            return;

        // Cantos cortados (o meio de cada lado, por onde entram os corredores, fica inteiro).
        for (int canto = 0; canto < 4; canto++)
        {
            if (Random.value > 0.45f)
                continue;

            int w = Mathf.Min(Random.Range(2, 4), (sala.area.width - 3) / 2);
            int h = Mathf.Min(Random.Range(2, 4), (sala.area.height - 3) / 2);
            bool direita = canto % 2 == 1, cima = canto >= 2;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    int cx = direita ? sala.area.xMax - 1 - x : sala.area.xMin + x;
                    int cy = cima ? sala.area.yMax - 1 - y : sala.area.yMin + y;
                    sala.celulas.Remove(new Vector2Int(cx, cy));
                }
            }
        }

        // Nichos pra fora, nas paredes sem sala do outro lado (nunca chegam perto de outra sala).
        foreach (Vector2Int lado in Direcoes)
        {
            if (ladosComVizinho.Contains(lado) || Random.value > 0.5f)
                continue;

            bool deitado = lado.y != 0;
            int comprimento = deitado ? sala.area.width : sala.area.height;
            int largura = Random.Range(3, 6);

            if (comprimento < largura + 4)
                continue;

            int de = Random.Range(2, comprimento - largura - 1);

            for (int i = 0; i < largura; i++)
            {
                for (int fundo = 1; fundo <= 2; fundo++)
                {
                    Vector2Int c = deitado
                        ? new Vector2Int(sala.area.xMin + de + i, lado.y > 0 ? sala.area.yMax - 1 + fundo : sala.area.yMin - fundo)
                        : new Vector2Int(lado.x > 0 ? sala.area.xMax - 1 + fundo : sala.area.xMin - fundo, sala.area.yMin + de + i);
                    sala.celulas.Add(c);
                }
            }
        }

        // Pilares (blocos de parede de 2 x 3) nas salas de luta grandes, longe das beiradas e das
        // linhas do meio (por onde os corredores entram).
        if (sala.DeLuta && sala.area.width >= 13 && sala.area.height >= 10 && Random.value < 0.6f)
        {
            Vector2Int m = new Vector2Int(meio.x, meio.y);
            bool quatro = sala.area.height >= 11 && Random.value < 0.5f;
            int dx = sala.area.width >= 15 ? 4 : 3;

            if (quatro)
            {
                Pilar(sala, m + new Vector2Int(-dx - 1, 1));
                Pilar(sala, m + new Vector2Int(dx, 1));
                Pilar(sala, m + new Vector2Int(-dx - 1, -3));
                Pilar(sala, m + new Vector2Int(dx, -3));
            }
            else
            {
                Pilar(sala, m + new Vector2Int(-dx - 1, -1));
                Pilar(sala, m + new Vector2Int(dx, -1));
            }
        }
    }

    // Um pilar com o canto de baixo a esquerda aqui (so se sobrar chao em volta, dentro da sala).
    private static void Pilar(SalaDaPlanta sala, Vector2Int canto)
    {
        for (int x = -2; x < 4; x++)
        {
            for (int y = -2; y < 5; y++)
            {
                if (!sala.celulas.Contains(canto + new Vector2Int(x, y)))
                    return;
            }
        }

        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 3; y++)
                sala.celulas.Remove(canto + new Vector2Int(x, y));
    }

    // ---------------- corredores e portas ----------------
    private static void Corredor(Planta planta, SalaDaPlanta a, SalaDaPlanta b)
    {
        Vector2Int d = b.naGrade - a.naGrade;

        // a fica embaixo ou a esquerda.
        if (d.x < 0 || d.y < 0)
        {
            SalaDaPlanta t = a;
            a = b;
            b = t;
            d = -d;
        }

        bool deitado = d.x != 0;
        int meio = deitado
            ? Mathf.RoundToInt((Mathf.Max(a.area.yMin, b.area.yMin) + Mathf.Min(a.area.yMax, b.area.yMax) - 1) * 0.5f)
            : Mathf.RoundToInt((Mathf.Max(a.area.xMin, b.area.xMin) + Mathf.Min(a.area.xMax, b.area.xMax) - 1) * 0.5f);
        int de = deitado ? a.area.xMax : a.area.yMax;
        int ate = deitado ? b.area.xMin : b.area.yMin;

        PortaDaPlanta portaDeA = a.DeLuta ? new PortaDaPlanta { paraDentro = -d } : null;
        PortaDaPlanta portaDeB = b.DeLuta ? new PortaDaPlanta { paraDentro = d } : null;

        for (int passo = de; passo < ate; passo++)
        {
            for (int k = -(LarguraDoCorredor / 2); k <= LarguraDoCorredor / 2; k++)
            {
                Vector2Int c = deitado ? new Vector2Int(passo, meio + k) : new Vector2Int(meio + k, passo);
                planta.corredores.Add(c);

                if (passo == de && portaDeA != null)
                    portaDeA.celulas.Add(c);

                if (passo == ate - 1 && portaDeB != null)
                    portaDeB.celulas.Add(c);
            }
        }

        if (portaDeA != null)
            a.portas.Add(portaDeA);

        if (portaDeB != null)
            b.portas.Add(portaDeB);
    }

    // Se o ajeito das paredes alargou o corredor bem na porta, a porta cresce junto (nada passa pelo lado).
    private static void Completar(HashSet<Vector2Int> chao, PortaDaPlanta porta)
    {
        if (porta.celulas.Count == 0)
            return;

        Vector2Int ao = porta.DeFrente ? Vector2Int.right : Vector2Int.up;
        Vector2Int primeira = porta.celulas[0];
        Vector2Int ultima = porta.celulas[porta.celulas.Count - 1];

        for (Vector2Int c = primeira - ao; chao.Contains(c) && porta.celulas.Count < 8; c -= ao)
            porta.celulas.Insert(0, c);

        for (Vector2Int c = ultima + ao; chao.Contains(c) && porta.celulas.Count < 8; c += ao)
            porta.celulas.Add(c);
    }
}
