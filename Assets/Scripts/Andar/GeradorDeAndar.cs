using System;
using System.Collections.Generic;

/// <summary>Papel de cada sala no andar. A cor da porta e o icone do minimapa saem daqui.</summary>
public enum TipoDeSala
{
    Normal,
    Inicio,
    Item,
    Chefe
}

/// <summary>As quatro saidas de uma sala. Y cresce pra cima, igual ao mundo da Unity.</summary>
public enum Direcao
{
    Cima,
    Baixo,
    Esquerda,
    Direita
}

public static class Direcoes
{
    public static readonly Direcao[] Todas = { Direcao.Cima, Direcao.Baixo, Direcao.Esquerda, Direcao.Direita };

    public static int Dx(Direcao d) => d == Direcao.Direita ? 1 : d == Direcao.Esquerda ? -1 : 0;

    public static int Dy(Direcao d) => d == Direcao.Cima ? 1 : d == Direcao.Baixo ? -1 : 0;

    public static Direcao Oposta(Direcao d)
    {
        switch (d)
        {
            case Direcao.Cima: return Direcao.Baixo;
            case Direcao.Baixo: return Direcao.Cima;
            case Direcao.Esquerda: return Direcao.Direita;
            default: return Direcao.Esquerda;
        }
    }
}

/// <summary>
/// Uma casa ocupada da grade. So dados: quem desenha a sala no mundo e o
/// <see cref="Andar"/>, quem desenha no canto da tela e o <see cref="Minimapa"/>.
/// </summary>
public class SalaDoAndar
{
    public readonly int X;
    public readonly int Y;

    public TipoDeSala Tipo = TipoDeSala.Normal;

    /// <summary>Quantas portas separam esta sala da sala inicial.</summary>
    public int Distancia;

    /// <summary>O jogador ja entrou aqui.</summary>
    public bool Visitada;

    /// <summary>Aparece no minimapa: foi visitada ou e vizinha de uma visitada.</summary>
    public bool Descoberta;

    public SalaDoAndar(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override string ToString() => $"{Tipo} ({X},{Y})";
}

/// <summary>O andar pronto: a grade com as salas e atalhos pras salas especiais.</summary>
public class MapaDoAndar
{
    public readonly int Largura;
    public readonly int Altura;
    public readonly List<SalaDoAndar> Salas = new List<SalaDoAndar>();

    public SalaDoAndar Inicio { get; internal set; }
    public SalaDoAndar Item { get; internal set; }
    public SalaDoAndar Chefe { get; internal set; }

    private readonly SalaDoAndar[,] grade;

    public MapaDoAndar(int largura, int altura)
    {
        Largura = largura;
        Altura = altura;
        grade = new SalaDoAndar[largura, altura];
    }

    public bool DentroDaGrade(int x, int y) => x >= 0 && y >= 0 && x < Largura && y < Altura;

    /// <summary>A sala nessa casa, ou null (fora da grade conta como vazio).</summary>
    public SalaDoAndar Em(int x, int y) => DentroDaGrade(x, y) ? grade[x, y] : null;

    public SalaDoAndar Vizinha(SalaDoAndar sala, Direcao d) => Em(sala.X + Direcoes.Dx(d), sala.Y + Direcoes.Dy(d));

    /// <summary>Toda sala vizinha tem porta: o andar e uma arvore, entao nao ha parede entre salas coladas.</summary>
    public bool TemPorta(SalaDoAndar sala, Direcao d) => Vizinha(sala, d) != null;

    public int QuantasVizinhas(int x, int y)
    {
        int n = 0;

        foreach (Direcao d in Direcoes.Todas)
            if (Em(x + Direcoes.Dx(d), y + Direcoes.Dy(d)) != null)
                n++;

        return n;
    }

    internal SalaDoAndar Criar(int x, int y)
    {
        SalaDoAndar sala = new SalaDoAndar(x, y);
        grade[x, y] = sala;
        Salas.Add(sala);
        return sala;
    }

    /// <summary>
    /// Marca a sala como visitada e descobre as vizinhas. Regra do Isaac: voce ve no
    /// minimapa as salas coladas nas que ja visitou, mesmo sem entrar nelas.
    /// </summary>
    public void Visitar(SalaDoAndar sala)
    {
        sala.Visitada = true;
        sala.Descoberta = true;

        foreach (Direcao d in Direcoes.Todas)
        {
            SalaDoAndar vizinha = Vizinha(sala, d);

            if (vizinha != null)
                vizinha.Descoberta = true;
        }
    }
}

/// <summary>
/// Gera o andar no estilo do Binding of Isaac. Nao depende da Unity (so System), entao da
/// pra testar e entender sem abrir o editor.
///
/// O algoritmo e o mesmo do jogo original:
///   1. Comeca com a sala inicial no meio da grade e poe ela numa fila.
///   2. Tira uma sala da fila e tenta abrir uma vizinha em cada direcao. A vizinha e
///      recusada se a casa ja estiver ocupada, se o andar ja tiver salas suficientes, se
///      ela ficaria colada em mais de uma sala (isso evita blocos 2x2 e mantem o andar
///      com cara de corredores), ou simplesmente numa moeda de 50%.
///   3. Toda vizinha aceita entra na fila. Repete ate a fila esvaziar.
///   4. Se nao deu o numero de salas, ou nao sobraram becos (salas com uma porta so)
///      pra por o chefe e o item, joga fora e tenta de novo.
///   5. O chefe vai no beco mais longe da sala inicial; o item, em outro beco sorteado.
///
/// Como cada sala nova so encosta na sala que a criou, o andar sai como uma arvore: nao
/// tem ciclos, e toda sala e alcancavel a partir da inicial.
/// </summary>
public static class GeradorDeAndar
{
    /// <summary>Tentativas antes de desistir das regras e aceitar o melhor andar que saiu.</summary>
    private const int TENTATIVAS = 500;

    /// <summary>
    /// Quantas salas o andar deve ter. Formula do Isaac: 8 ou 9 no primeiro andar, cerca de
    /// 3 a mais por andar, com teto.
    /// </summary>
    public static int SalasParaOAndar(int numeroDoAndar, Random sorteio, int maximo = 20)
    {
        int andar = Math.Max(1, numeroDoAndar);
        return Math.Min(maximo, sorteio.Next(2) + 5 + andar * 10 / 3);
    }

    /// <param name="numeroDoAndar">1 = primeiro andar. Define quantas salas o andar tem.</param>
    /// <param name="semente">Mesma semente = mesmo andar. Use pra repetir um andar bugado.</param>
    public static MapaDoAndar Gerar(int numeroDoAndar, int semente, int largura = 9, int altura = 8)
    {
        if (largura < 3 || altura < 3)
            throw new ArgumentException("A grade precisa ter pelo menos 3x3 casas.");

        Random sorteio = new Random(semente);

        // A grade limita o tamanho: um andar maior que ~metade dela quase nunca fecha.
        int alvo = Math.Min(SalasParaOAndar(numeroDoAndar, sorteio), largura * altura / 2);
        alvo = Math.Max(alvo, 3); // inicio + item + chefe

        MapaDoAndar melhor = null;

        for (int tentativa = 0; tentativa < TENTATIVAS; tentativa++)
        {
            MapaDoAndar mapa = Espalhar(largura, altura, alvo, sorteio);
            List<SalaDoAndar> becos = Becos(mapa);

            if (mapa.Salas.Count == alvo && becos.Count >= 2)
            {
                Decorar(mapa, becos, sorteio);
                return mapa;
            }

            if (melhor == null || mapa.Salas.Count > melhor.Salas.Count)
                melhor = mapa;
        }

        // Grade apertada demais pro alvo. Nao trava o jogo: usa o maior andar que saiu.
        Decorar(melhor, Becos(melhor), sorteio);
        return melhor;
    }

    private static MapaDoAndar Espalhar(int largura, int altura, int alvo, Random sorteio)
    {
        MapaDoAndar mapa = new MapaDoAndar(largura, altura);
        SalaDoAndar inicio = mapa.Criar(largura / 2, altura / 2);
        inicio.Tipo = TipoDeSala.Inicio;
        mapa.Inicio = inicio;

        Queue<SalaDoAndar> fila = new Queue<SalaDoAndar>();
        fila.Enqueue(inicio);

        while (fila.Count > 0)
        {
            SalaDoAndar atual = fila.Dequeue();

            foreach (Direcao d in Direcoes.Todas)
            {
                int x = atual.X + Direcoes.Dx(d);
                int y = atual.Y + Direcoes.Dy(d);

                if (mapa.Salas.Count >= alvo)
                    break;

                if (!mapa.DentroDaGrade(x, y) || mapa.Em(x, y) != null)
                    continue;

                // A casa nova so pode encostar na sala que a esta criando.
                if (mapa.QuantasVizinhas(x, y) > 1)
                    continue;

                if (sorteio.Next(2) == 0)
                    continue;

                fila.Enqueue(mapa.Criar(x, y));
            }
        }

        return mapa;
    }

    /// <summary>Salas com uma porta so, tirando a inicial. E onde vao as salas especiais.</summary>
    private static List<SalaDoAndar> Becos(MapaDoAndar mapa)
    {
        List<SalaDoAndar> becos = new List<SalaDoAndar>();

        foreach (SalaDoAndar sala in mapa.Salas)
            if (sala != mapa.Inicio && mapa.QuantasVizinhas(sala.X, sala.Y) == 1)
                becos.Add(sala);

        return becos;
    }

    private static void Decorar(MapaDoAndar mapa, List<SalaDoAndar> becos, Random sorteio)
    {
        CalcularDistancias(mapa);

        // Pior caso (grade minuscula): sem becos, usa a sala mais longe que nao seja a inicial.
        List<SalaDoAndar> candidatas = becos.Count > 0 ? becos : new List<SalaDoAndar>(mapa.Salas);
        candidatas.Remove(mapa.Inicio);

        if (candidatas.Count == 0)
            return;

        SalaDoAndar chefe = candidatas[0];

        foreach (SalaDoAndar sala in candidatas)
            if (sala.Distancia > chefe.Distancia)
                chefe = sala;

        chefe.Tipo = TipoDeSala.Chefe;
        mapa.Chefe = chefe;
        candidatas.Remove(chefe);

        if (candidatas.Count == 0)
            return;

        SalaDoAndar item = candidatas[sorteio.Next(candidatas.Count)];
        item.Tipo = TipoDeSala.Item;
        mapa.Item = item;
    }

    /// <summary>Busca em largura a partir da inicial: Distancia = numero de portas ate la.</summary>
    private static void CalcularDistancias(MapaDoAndar mapa)
    {
        foreach (SalaDoAndar sala in mapa.Salas)
            sala.Distancia = -1;

        Queue<SalaDoAndar> fila = new Queue<SalaDoAndar>();
        mapa.Inicio.Distancia = 0;
        fila.Enqueue(mapa.Inicio);

        while (fila.Count > 0)
        {
            SalaDoAndar atual = fila.Dequeue();

            foreach (Direcao d in Direcoes.Todas)
            {
                SalaDoAndar vizinha = mapa.Vizinha(atual, d);

                if (vizinha == null || vizinha.Distancia >= 0)
                    continue;

                vizinha.Distancia = atual.Distancia + 1;
                fila.Enqueue(vizinha);
            }
        }
    }
}
