using System;
using System.Collections.Generic;

/// <summary>Papel de cada sala no andar. A cor da porta e o icone do minimapa saem daqui.</summary>
public enum TipoDeSala
{
    Normal,
    Inicio,
    Item,
    Chefe,
    Loja,
    Secreta,
    Desafio,
    Amaldicoada
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
/// Uma sala que ocupa mais de uma casa da grade, como as do Isaac: corredor comprido (2x1),
/// sala alta (1x2), sala grande (2x2) ou em L (3 casas de um 2x2). Entre as casas dela nao tem
/// parede nem porta; cada casa continua com as suas portas pro lado de fora.
/// </summary>
public class FormaDaSala
{
    public readonly List<SalaDoAndar> Casas = new List<SalaDoAndar>();

    /// <summary>"Corredor", "Sala alta", "Sala grande" ou "Sala em L" (pro log e pro nome no mundo).</summary>
    public string Nome;

    public bool Contem(SalaDoAndar casa) => casa != null && casa.Forma == this;
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

    /// <summary>
    /// De 0 (sala inicial) a 1 (sala do chefe): quanto do caminho ate o chefe esta sala
    /// representa. Serve pra dosar a sala (mais inimigos perto do chefe, menos perto do inicio).
    /// </summary>
    public float Profundidade;

    /// <summary>Esta sala fica no caminho mais curto da sala inicial ate a do chefe.</summary>
    public bool NoCaminhoDoChefe;

    /// <summary>
    /// Beco (sala com uma porta so) que nao virou sala especial. Quem explora ate aqui
    /// ganha premio garantido ao limpar.
    /// </summary>
    public bool Recompensa;

    /// <summary>
    /// Desenho de pedras e espinhos da sala comum (indice de <see cref="DisposicoesDaSala"/>);
    /// -1 = sala sem obstaculo. Vizinhas nunca repetem o mesmo desenho.
    /// </summary>
    public int Desenho = -1;

    /// <summary>O desenho vem espelhado na horizontal / na vertical (a mesma planta fica diferente).</summary>
    public bool EspelharX;
    public bool EspelharY;

    /// <summary>A sala grande de que esta casa faz parte, ou null (sala de uma casa so).</summary>
    public FormaDaSala Forma;

    /// <summary>Esta casa e a outra sao a mesma sala (a mesma casa conta).</summary>
    public bool MesmaSala(SalaDoAndar outra) => outra == this || (Forma != null && Forma.Contem(outra));

    /// <summary>As casas da sala inteira (so esta, se ela nao for grande).</summary>
    public IEnumerable<SalaDoAndar> CasasDaSala
    {
        get
        {
            if (Forma == null)
            {
                yield return this;
                yield break;
            }

            foreach (SalaDoAndar casa in Forma.Casas)
                yield return casa;
        }
    }

    /// <summary>O jogador ja entrou aqui.</summary>
    public bool Visitada;

    /// <summary>Aparece no minimapa: foi visitada ou tem porta pra uma visitada.</summary>
    public bool Descoberta;

    private readonly bool[] portas = new bool[4];

    public SalaDoAndar(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>Tem porta pra este lado (a sala do outro lado existe e tem porta de volta).</summary>
    public bool TemPortaPara(Direcao d) => portas[(int)d];

    /// <summary>Quantas portas a sala tem (contando as escondidas da secreta).</summary>
    public int QuantasPortas
    {
        get
        {
            int n = 0;

            foreach (bool porta in portas)
                if (porta)
                    n++;

            return n;
        }
    }

    internal void AbrirPorta(Direcao d) => portas[(int)d] = true;

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
    public SalaDoAndar Loja { get; internal set; }
    public SalaDoAndar Secreta { get; internal set; }
    public SalaDoAndar Desafio { get; internal set; }
    public SalaDoAndar Amaldicoada { get; internal set; }

    /// <summary>As salas do caminho mais curto do inicio ao chefe, na ordem (inicio e chefe inclusos).</summary>
    public readonly List<SalaDoAndar> CaminhoAteOChefe = new List<SalaDoAndar>();

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

    /// <summary>
    /// Se ha porta entre esta sala e a vizinha desse lado. Duas salas coladas podem ter
    /// parede entre elas: so tem porta onde o gerador ligou (a sala que criou a outra, e os
    /// atalhos que fecham circuito). As portas da secreta existem, mas escondidas.
    /// </summary>
    public bool TemPorta(SalaDoAndar sala, Direcao d) => sala.TemPortaPara(d) && Vizinha(sala, d) != null && !Juntas(sala, d);

    /// <summary>A casa desse lado e da mesma sala grande: ali nao tem parede nem porta, so chao.</summary>
    public bool Juntas(SalaDoAndar sala, Direcao d)
    {
        SalaDoAndar outra = Vizinha(sala, d);
        return sala.Forma != null && outra != null && sala.Forma.Contem(outra);
    }

    /// <summary>A sala do outro lado da porta desse lado, ou null se ali e parede.</summary>
    public SalaDoAndar PelaPorta(SalaDoAndar sala, Direcao d) => TemPorta(sala, d) ? Vizinha(sala, d) : null;

    /// <summary>Poe porta entre a sala e a vizinha desse lado (nos dois sentidos).</summary>
    internal void Ligar(SalaDoAndar sala, Direcao d)
    {
        SalaDoAndar outra = Vizinha(sala, d);

        if (outra == null)
            return;

        sala.AbrirPorta(d);
        outra.AbrirPorta(Direcoes.Oposta(d));
    }

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
    /// minimapa as salas do outro lado das portas das salas que ja visitou, mesmo sem
    /// entrar nelas.
    /// </summary>
    public void Visitar(SalaDoAndar sala)
    {
        // Sala grande: entrar numa casa e entrar na sala toda.
        foreach (SalaDoAndar casa in sala.CasasDaSala)
        {
            casa.Visitada = true;
            casa.Descoberta = true;

            // A secreta so aparece no mapa depois que alguem entra nela.
            foreach (Direcao d in Direcoes.Todas)
            {
                SalaDoAndar vizinha = PelaPorta(casa, d);

                if (vizinha == null || vizinha.Tipo == TipoDeSala.Secreta)
                    continue;

                foreach (SalaDoAndar dela in vizinha.CasasDaSala)
                    dela.Descoberta = true;
            }
        }
    }

    /// <summary>
    /// Mostra o andar inteiro no minimapa (menos a secreta, se <paramref name="comSecreta"/>
    /// for falso). Pra itens tipo mapa do tesouro.
    /// </summary>
    public void RevelarTudo(bool comSecreta = false)
    {
        foreach (SalaDoAndar sala in Salas)
            if (comSecreta || sala.Tipo != TipoDeSala.Secreta)
                sala.Descoberta = true;
    }

    /// <summary>Mostra so as salas especiais (chefe, item, loja...) no minimapa. Pra itens tipo bussola.</summary>
    public void RevelarEspeciais()
    {
        foreach (SalaDoAndar sala in Salas)
            if (sala.Tipo != TipoDeSala.Normal && sala.Tipo != TipoDeSala.Secreta)
                sala.Descoberta = true;
    }
}

/// <summary>
/// O que o <see cref="GeradorDeAndar"/> precisa saber pra montar um andar. Tudo aqui pode
/// ser mexido antes de gerar (o <see cref="Andar"/> avisa em <c>AoPrepararGeracao</c>), entao
/// quem cuida da dificuldade escala o andar sem mexer no gerador.
/// </summary>
public class ParametrosDoAndar
{
    /// <summary>1 = primeiro andar.</summary>
    public int NumeroDoAndar = 1;

    /// <summary>Tamanho da grade em casas. A sala inicial nasce no meio.</summary>
    public int Largura = 9;
    public int Altura = 8;

    /// <summary>Quantas salas o andar tem (sem a secreta): sorteado entre os dois, inclusive.</summary>
    public int SalasMinimas = 8;
    public int SalasMaximas = 9;

    /// <summary>Chance de uma sala abrir vizinha em cada lado. Menos = andar mais comprido e cheio de becos.</summary>
    public double ChanceDeRamo = 0.5;

    /// <summary>
    /// Chance de aceitar uma sala nova encostada em mais de uma (ela so ganha porta pra sala
    /// que a criou; pras outras e parede). Mais = andar mais compacto. Nunca forma bloco 2x2.
    /// </summary>
    public double ChanceDeEncostar = 0.3;

    /// <summary>
    /// Quantos circuitos o andar pode ganhar: sala-ponte entre duas salas que estavam longe
    /// uma da outra pelo caminho, pra dar a volta em vez de voltar pelas mesmas salas.
    /// </summary>
    public int Lacos = 1;

    /// <summary>So vira ponte se o caminho entre as duas salas tinha pelo menos isto de portas.</summary>
    public int PortasParaValerAtalho = 4;

    /// <summary>Quantos andares validos sortear antes de ficar com o de melhor nota.</summary>
    public int Candidatos = 16;

    /// <summary>O caminho ate o chefe tem pelo menos isto de portas (se a grade deixar).</summary>
    public int DistanciaMinimaDoChefe = 3;

    public bool ComLoja = true;
    public bool ComDesafio = true;
    public bool ComAmaldicoada;
    public bool ComSecreta = true;

    /// <summary>
    /// Desenhos de obstaculo que as salas comuns podem sortear (indices de
    /// <see cref="DisposicoesDaSala"/>). Vazio = o gerador nao escolhe desenho.
    /// </summary>
    public List<int> Desenhos = new List<int>();

    /// <summary>Chance de uma sala comum ficar sem obstaculo (nunca duas vizinhas vazias).</summary>
    public double ChanceDeSalaVazia = 0.15;

    /// <summary>
    /// Chance de cada sala comum tentar virar sala grande com as vizinhas (corredor, sala alta,
    /// 2x2 ou L). So junta casas comuns ja ligadas por porta.
    /// </summary>
    public double ChanceDeSalaGrande = 0.45;

    /// <summary>
    /// Os numeros de sempre, pelo andar: 8 ou 9 salas no primeiro, cerca de 3 a mais por
    /// andar, ate 20; amaldicoada a partir do andar 2; ate dois atalhos depois do primeiro.
    /// </summary>
    public static ParametrosDoAndar Padrao(int numeroDoAndar, int largura = 9, int altura = 8)
    {
        int andar = Math.Max(1, numeroDoAndar);
        int minimo = Math.Min(20, 5 + andar * 10 / 3);

        return new ParametrosDoAndar
        {
            NumeroDoAndar = andar,
            Largura = largura,
            Altura = altura,
            SalasMinimas = minimo,
            SalasMaximas = Math.Min(20, minimo + 1),
            ComAmaldicoada = andar >= GeradorDeAndar.AmaldicoadaAPartirDoAndar,
            Lacos = andar >= 2 ? 2 : 1,
            DistanciaMinimaDoChefe = andar >= 2 ? 4 : 3,
        };
    }
}

/// <summary>
/// Gera o andar no estilo do Binding of Isaac. Nao depende da Unity (so System), entao da
/// pra testar e entender sem abrir o editor.
///
///   1. Comeca com a sala inicial no meio da grade e poe ela numa fila.
///   2. Tira uma sala da fila e tenta abrir uma vizinha em cada direcao, com porta entre
///      as duas. A vizinha e recusada se a casa ja estiver ocupada, se o andar ja tiver
///      salas suficientes, ou numa moeda (<see cref="ParametrosDoAndar.ChanceDeRamo"/>).
///      Casa encostada em mais de uma sala so passa de vez em quando
///      (<see cref="ParametrosDoAndar.ChanceDeEncostar"/>, com parede pras outras) e nunca
///      se formar um bloco 2x2: assim o andar tem cara de corredores.
///   3. Toda vizinha aceita entra na fila. Repete ate a fila esvaziar. No meio, uma ou
///      duas salas-PONTE: numa casa vazia entre duas salas que estavam longe pelo caminho,
///      com porta pras duas, fechando um circuito (<see cref="ParametrosDoAndar.Lacos"/>).
///   4. Andar sem o numero de salas ou sem becos (salas com uma porta so) pras salas
///      especiais e jogado fora.
///   5. Dos andares que passaram, fica o de melhor NOTA: caminho comprido ate o chefe,
///      bastante beco pra explorar, formato espalhado (nem linguica, nem bolo), pouco
///      corredor reto repetido e um ou outro circuito.
///   6. O chefe vai no beco mais longe da sala inicial. Item, loja, desafio e (a partir do
///      andar 2) amaldicoada vao nos outros becos, sempre o mais longe possivel das salas
///      especiais ja postas, pra espalhar os motivos de explorar. Beco que sobra vira sala
///      de recompensa. A secreta vai numa casa vazia colada em varias salas.
///   7. Cada sala comum ganha um desenho de pedras e espinhos, sem repetir o da vizinha.
///
/// Toda sala nasce ligada por porta a sala que a criou, entao sempre existe caminho da
/// inicial ate o chefe e ate cada sala especial.
/// </summary>
public static class GeradorDeAndar
{
    /// <summary>Tentativas antes de desistir das regras e aceitar o melhor andar que saiu.</summary>
    private const int TENTATIVAS = 600;

    /// <summary>A sala amaldicoada so aparece a partir deste andar.</summary>
    public const int AmaldicoadaAPartirDoAndar = 2;

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
        => Gerar(ParametrosDoAndar.Padrao(numeroDoAndar, largura, altura), semente);

    /// <param name="semente">Mesma semente e mesmos parametros = mesmo andar.</param>
    public static MapaDoAndar Gerar(ParametrosDoAndar p, int semente)
    {
        if (p == null)
            throw new ArgumentNullException(nameof(p));

        if (p.Largura < 3 || p.Altura < 3)
            throw new ArgumentException("A grade precisa ter pelo menos 3x3 casas.");

        Random sorteio = new Random(semente);

        // A grade limita o tamanho: um andar maior que ~metade dela quase nunca fecha.
        int minimo = Math.Min(p.SalasMinimas, p.SalasMaximas);
        int maximo = Math.Max(p.SalasMinimas, p.SalasMaximas);
        int alvo = Math.Min(sorteio.Next(minimo, maximo + 1), p.Largura * p.Altura / 2);
        alvo = Math.Max(alvo, 3); // inicio + item + chefe

        int especiais = 2 + (p.ComLoja ? 1 : 0) + (p.ComDesafio ? 1 : 0) + (p.ComAmaldicoada ? 1 : 0);
        int candidatos = Math.Max(1, p.Candidatos);

        MapaDoAndar escolhido = null;
        double melhorNota = double.MinValue;
        MapaDoAndar maior = null;
        int validos = 0;

        for (int tentativa = 0; tentativa < TENTATIVAS && validos < candidatos; tentativa++)
        {
            MapaDoAndar mapa = Espalhar(p, alvo, sorteio);
            List<SalaDoAndar> becos = Becos(mapa);

            // No comeco exige beco pra toda sala especial; a exigencia cai aos poucos ate
            // so chefe e item.
            int becosPedidos = Math.Max(2, especiais - tentativa * (especiais - 1) / TENTATIVAS);

            if (mapa.Salas.Count == alvo && becos.Count >= becosPedidos)
            {
                validos++;
                double nota = Nota(mapa, becos, especiais, alvo, p, sorteio);

                if (nota > melhorNota)
                {
                    melhorNota = nota;
                    escolhido = mapa;
                }

                continue;
            }

            if (maior == null || mapa.Salas.Count > maior.Salas.Count)
                maior = mapa;
        }

        // Grade apertada demais pro alvo. Nao trava o jogo: usa o maior andar que saiu.
        MapaDoAndar final = escolhido ?? maior;
        Decorar(final, p, sorteio);
        return final;
    }

    private static MapaDoAndar Espalhar(ParametrosDoAndar p, int alvo, Random sorteio)
    {
        MapaDoAndar mapa = new MapaDoAndar(p.Largura, p.Altura);
        SalaDoAndar inicio = mapa.Criar(p.Largura / 2, p.Altura / 2);
        inicio.Tipo = TipoDeSala.Inicio;
        mapa.Inicio = inicio;

        // Guarda umas salas pras pontes; o que nao virar ponte cresce normal depois.
        int lacos = Math.Max(0, Math.Min(p.Lacos, alvo - 3));
        Crescer(mapa, new List<SalaDoAndar> { inicio }, alvo - lacos, p, sorteio);
        PorPontes(mapa, lacos, p, sorteio);

        if (mapa.Salas.Count < alvo)
        {
            List<SalaDoAndar> todas = new List<SalaDoAndar>(mapa.Salas);
            Embaralhar(todas, sorteio);
            Crescer(mapa, todas, alvo, p, sorteio);
        }

        return mapa;
    }

    /// <summary>O passo do Isaac: cada sala da fila tenta abrir uma vizinha em cada lado.</summary>
    private static void Crescer(MapaDoAndar mapa, List<SalaDoAndar> comeco, int alvo, ParametrosDoAndar p, Random sorteio)
    {
        Queue<SalaDoAndar> fila = new Queue<SalaDoAndar>(comeco);

        while (fila.Count > 0 && mapa.Salas.Count < alvo)
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

                // Casa encostada em mais de uma sala: so de vez em quando, e nunca virando
                // bloco 2x2 (quatro salas numa praca).
                if (mapa.QuantasVizinhas(x, y) > 1 && (sorteio.NextDouble() >= p.ChanceDeEncostar || FormariaBloco(mapa, x, y)))
                    continue;

                if (sorteio.NextDouble() >= p.ChanceDeRamo)
                    continue;

                fila.Enqueue(mapa.Criar(x, y));
                mapa.Ligar(atual, d);
            }
        }
    }

    /// <summary>
    /// Ate <paramref name="quantas"/> salas-ponte: numa casa vazia entre duas salas que
    /// estavam longe uma da outra pelo caminho, com porta pras duas. Fecha um circuito, e
    /// o jogador pode dar a volta em vez de voltar pelas mesmas salas. Prefere nao ligar
    /// em beco, que e onde vao as salas especiais.
    /// </summary>
    private static void PorPontes(MapaDoAndar mapa, int quantas, ParametrosDoAndar p, Random sorteio)
    {
        for (int ponte = 0; ponte < quantas; ponte++)
        {
            int melhorX = -1, melhorY = -1;
            Direcao ladoA = Direcao.Cima, ladoB = Direcao.Cima;
            double melhorNota = double.MinValue;

            for (int x = 0; x < mapa.Largura; x++)
            {
                for (int y = 0; y < mapa.Altura; y++)
                {
                    if (mapa.Em(x, y) != null || mapa.QuantasVizinhas(x, y) < 2 || FormariaBloco(mapa, x, y))
                        continue;

                    for (int i = 0; i < 4; i++)
                    {
                        for (int j = i + 1; j < 4; j++)
                        {
                            Direcao di = Direcoes.Todas[i], dj = Direcoes.Todas[j];
                            SalaDoAndar a = mapa.Em(x + Direcoes.Dx(di), y + Direcoes.Dy(di));
                            SalaDoAndar b = mapa.Em(x + Direcoes.Dx(dj), y + Direcoes.Dy(dj));

                            if (a == null || b == null)
                                continue;

                            int caminho = CaminhoEntre(mapa, a, b);

                            if (caminho < p.PortasParaValerAtalho)
                                continue;

                            int becos = (a.QuantasPortas == 1 ? 1 : 0) + (b.QuantasPortas == 1 ? 1 : 0);
                            double nota = Math.Min(caminho, 8) - becos * 3.0 + sorteio.NextDouble() * 2.0;

                            if (nota > melhorNota)
                            {
                                melhorNota = nota;
                                melhorX = x;
                                melhorY = y;
                                ladoA = di;
                                ladoB = dj;
                            }
                        }
                    }
                }
            }

            if (melhorX < 0)
                return;

            SalaDoAndar nova = mapa.Criar(melhorX, melhorY);
            mapa.Ligar(nova, ladoA);
            mapa.Ligar(nova, ladoB);
        }
    }

    private static void Embaralhar<T>(List<T> lista, Random sorteio)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = sorteio.Next(i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }

    /// <summary>Quantas portas no caminho mais curto entre duas salas (int.MaxValue se nao ha caminho).</summary>
    private static int CaminhoEntre(MapaDoAndar mapa, SalaDoAndar de, SalaDoAndar para)
    {
        Dictionary<SalaDoAndar, int> passos = new Dictionary<SalaDoAndar, int> { { de, 0 } };
        Queue<SalaDoAndar> fila = new Queue<SalaDoAndar>();
        fila.Enqueue(de);

        while (fila.Count > 0)
        {
            SalaDoAndar atual = fila.Dequeue();

            if (atual == para)
                return passos[atual];

            foreach (Direcao d in Direcoes.Todas)
            {
                SalaDoAndar v = mapa.PelaPorta(atual, d);

                if (v != null && !passos.ContainsKey(v))
                {
                    passos[v] = passos[atual] + 1;
                    fila.Enqueue(v);
                }
            }
        }

        return int.MaxValue;
    }

    /// <summary>Ocupar esta casa fecharia um quadrado 2x2 de salas.</summary>
    private static bool FormariaBloco(MapaDoAndar mapa, int x, int y)
    {
        for (int dx = -1; dx <= 1; dx += 2)
            for (int dy = -1; dy <= 1; dy += 2)
                if (mapa.Em(x + dx, y) != null && mapa.Em(x, y + dy) != null && mapa.Em(x + dx, y + dy) != null)
                    return true;

        return false;
    }

    /// <summary>
    /// Quanto o andar e bom de jogar. Todos os candidatos tem o mesmo numero de salas; a
    /// nota decide entre eles:
    ///   + caminho ate o chefe comprido (ate um teto, senao vira maratona);
    ///   + becos pra todas as salas especiais e mais um ou dois de recompensa;
    ///   + formato espalhado (caixa em volta das salas sem lado muito fino);
    ///   + um ou dois circuitos;
    ///   - sala inicial com uma porta so (comeco em tubo);
    ///   - corredor reto de sala com duas portas seguidas (parece repetido).
    /// </summary>
    private static double Nota(MapaDoAndar mapa, List<SalaDoAndar> becos, int especiais, int alvo,
                               ParametrosDoAndar p, Random sorteio)
    {
        CalcularDistancias(mapa);

        int maisLonge = 0;

        foreach (SalaDoAndar beco in becos)
            maisLonge = Math.Max(maisLonge, beco.Distancia);

        double nota = Math.Min(maisLonge, Math.Max(p.DistanciaMinimaDoChefe, alvo * 0.45)) * 3.0;

        if (maisLonge < p.DistanciaMinimaDoChefe)
            nota -= 12.0;

        nota += Math.Min(becos.Count, especiais + 2) * 2.0;

        int xMin = int.MaxValue, xMax = int.MinValue, yMin = int.MaxValue, yMax = int.MinValue;
        int portas = 0;
        int tubos = 0;

        foreach (SalaDoAndar sala in mapa.Salas)
        {
            xMin = Math.Min(xMin, sala.X);
            xMax = Math.Max(xMax, sala.X);
            yMin = Math.Min(yMin, sala.Y);
            yMax = Math.Max(yMax, sala.Y);
            portas += sala.QuantasPortas;

            // Sala-tubo seguida de outra sala-tubo na mesma linha.
            if (Tubo(mapa, sala, out bool deitado))
            {
                SalaDoAndar seguinte = mapa.PelaPorta(sala, deitado ? Direcao.Direita : Direcao.Cima);

                if (seguinte != null && Tubo(mapa, seguinte, out bool tambemDeitado) && tambemDeitado == deitado)
                    tubos++;
            }
        }

        nota += Math.Min(xMax - xMin + 1, yMax - yMin + 1) * 1.5;
        nota -= tubos * 1.5;

        int lacos = portas / 2 - (mapa.Salas.Count - 1);
        nota += Math.Min(lacos, Math.Max(0, p.Lacos)) * 5.0;

        if (mapa.Inicio.QuantasPortas <= 1)
            nota -= 3.0;

        return nota + sorteio.NextDouble(); // desempate
    }

    /// <summary>Sala com exatamente duas portas, uma de frente pra outra.</summary>
    private static bool Tubo(MapaDoAndar mapa, SalaDoAndar sala, out bool deitado)
    {
        bool esquerda = mapa.TemPorta(sala, Direcao.Esquerda);
        bool direita = mapa.TemPorta(sala, Direcao.Direita);
        bool cima = mapa.TemPorta(sala, Direcao.Cima);
        bool baixo = mapa.TemPorta(sala, Direcao.Baixo);

        deitado = esquerda && direita && !cima && !baixo;
        return deitado || (cima && baixo && !esquerda && !direita);
    }

    /// <summary>Salas com uma porta so, tirando a inicial. E onde vao as salas especiais.</summary>
    private static List<SalaDoAndar> Becos(MapaDoAndar mapa)
    {
        List<SalaDoAndar> becos = new List<SalaDoAndar>();

        foreach (SalaDoAndar sala in mapa.Salas)
            if (sala != mapa.Inicio && sala.QuantasPortas == 1)
                becos.Add(sala);

        return becos;
    }

    private static void Decorar(MapaDoAndar mapa, ParametrosDoAndar p, Random sorteio)
    {
        CalcularDistancias(mapa);

        // Pior caso (grade minuscula): sem becos, usa a sala mais longe que nao seja a inicial.
        List<SalaDoAndar> becos = Becos(mapa);
        bool temBecos = becos.Count > 0;
        List<SalaDoAndar> candidatas = temBecos ? becos : new List<SalaDoAndar>(mapa.Salas);
        candidatas.Remove(mapa.Inicio);

        if (candidatas.Count > 0)
        {
            SalaDoAndar chefe = candidatas[0];

            foreach (SalaDoAndar sala in candidatas)
                if (sala.Distancia > chefe.Distancia)
                    chefe = sala;

            chefe.Tipo = TipoDeSala.Chefe;
            mapa.Chefe = chefe;
            candidatas.Remove(chefe);
            MarcarCaminho(mapa);

            // Cada especial vai longe das que ja estao postas (a inicial conta).
            List<SalaDoAndar> postas = new List<SalaDoAndar> { mapa.Inicio, chefe };

            mapa.Item = Por(TipoDeSala.Item, candidatas, postas, sorteio);

            if (p.ComLoja)
                mapa.Loja = Por(TipoDeSala.Loja, candidatas, postas, sorteio);

            // So em beco que sobrou: sala comum nunca vira desafio ou amaldicoada.
            if (temBecos && p.ComDesafio)
                mapa.Desafio = Por(TipoDeSala.Desafio, candidatas, postas, sorteio);

            if (temBecos && p.ComAmaldicoada)
                mapa.Amaldicoada = Por(TipoDeSala.Amaldicoada, candidatas, postas, sorteio);

            // Beco que sobrou: premio garantido pra quem vai ate o fim do corredor.
            if (temBecos)
                foreach (SalaDoAndar sala in candidatas)
                    sala.Recompensa = true;

            int ateOChefe = Math.Max(1, chefe.Distancia);

            foreach (SalaDoAndar sala in mapa.Salas)
                sala.Profundidade = Math.Min(1f, Math.Max(0f, sala.Distancia / (float)ateOChefe));
        }

        EscolherDesenhos(mapa, p, sorteio);

        if (p.ComSecreta)
            PorSecreta(mapa, sorteio);

        Juntar(mapa, p, sorteio);
    }

    // As formas de sala grande, em casas a partir de um canto: corredor, sala alta, 2x2 e os quatro L.
    private static readonly (string nome, int peso, (int x, int y)[] casas)[] Formas =
    {
        ("Corredor", 5, new[] { (0, 0), (1, 0) }),
        ("Sala alta", 4, new[] { (0, 0), (0, 1) }),
        ("Sala grande", 2, new[] { (0, 0), (1, 0), (0, 1), (1, 1) }),
        ("Sala em L", 2, new[] { (0, 0), (1, 0), (0, 1) }),
        ("Sala em L", 2, new[] { (0, 0), (1, 0), (1, 1) }),
        ("Sala em L", 2, new[] { (0, 0), (0, 1), (1, 1) }),
        ("Sala em L", 2, new[] { (1, 0), (0, 1), (1, 1) }),
    };

    /// <summary>
    /// Junta casas comuns vizinhas em salas grandes, como as salas compridas, altas, 2x2 e em L
    /// do Isaac. So casas comuns (nada de inicio, chefe, item, recompensa...), so formas cujas
    /// casas ja estao todas ligadas por porta entre si (assim nao abre atalho novo no andar).
    /// Casa juntada fica sem desenho de obstaculo: o formato ja e a variedade dela.
    /// </summary>
    private static void Juntar(MapaDoAndar mapa, ParametrosDoAndar p, Random sorteio)
    {
        if (p.ChanceDeSalaGrande <= 0)
            return;

        List<SalaDoAndar> ordem = new List<SalaDoAndar>(mapa.Salas);
        Embaralhar(ordem, sorteio);
        int pesoTotal = 0;

        foreach (var f in Formas)
            pesoTotal += f.peso;

        foreach (SalaDoAndar semente in ordem)
        {
            if (!PodeJuntar(semente) || sorteio.NextDouble() >= p.ChanceDeSalaGrande)
                continue;

            // Sorteia a ordem das formas pelo peso e fica com a primeira que couber.
            List<int> tentativas = new List<int>();

            for (int i = 0; i < Formas.Length; i++)
                for (int k = 0; k < Formas[i].peso; k++)
                    tentativas.Add(i);

            Embaralhar(tentativas, sorteio);
            HashSet<int> vistas = new HashSet<int>();

            foreach (int indice in tentativas)
            {
                if (!vistas.Add(indice))
                    continue;

                List<SalaDoAndar> casas = Encaixar(mapa, semente, Formas[indice].casas);

                if (casas == null)
                    continue;

                FormaDaSala forma = new FormaDaSala { Nome = Formas[indice].nome };

                foreach (SalaDoAndar casa in casas)
                {
                    casa.Forma = forma;
                    casa.Desenho = -1;
                    forma.Casas.Add(casa);
                }

                break;
            }
        }
    }

    private static bool PodeJuntar(SalaDoAndar casa) => casa != null && casa.Tipo == TipoDeSala.Normal && casa.Forma == null && !casa.Recompensa;

    /// <summary>
    /// Tenta por a forma com a <paramref name="semente"/> em cada uma das casas dela. Devolve as
    /// casas se todas podem juntar e estao ligadas por porta entre si; senao null.
    /// </summary>
    private static List<SalaDoAndar> Encaixar(MapaDoAndar mapa, SalaDoAndar semente, (int x, int y)[] forma)
    {
        foreach (var ancora in forma)
        {
            int ox = semente.X - ancora.x;
            int oy = semente.Y - ancora.y;
            List<SalaDoAndar> casas = new List<SalaDoAndar>();
            bool cabe = true;

            foreach (var c in forma)
            {
                SalaDoAndar casa = mapa.Em(ox + c.x, oy + c.y);

                if (!PodeJuntar(casa))
                {
                    cabe = false;
                    break;
                }

                casas.Add(casa);
            }

            if (cabe && LigadasPorPorta(mapa, casas))
                return casas;
        }

        return null;
    }

    /// <summary>Da pra ir de qualquer casa a qualquer outra so pelas portas entre elas.</summary>
    private static bool LigadasPorPorta(MapaDoAndar mapa, List<SalaDoAndar> casas)
    {
        HashSet<SalaDoAndar> alcancadas = new HashSet<SalaDoAndar> { casas[0] };
        Queue<SalaDoAndar> fila = new Queue<SalaDoAndar>(alcancadas);

        while (fila.Count > 0)
        {
            SalaDoAndar atual = fila.Dequeue();

            foreach (Direcao d in Direcoes.Todas)
            {
                SalaDoAndar outra = atual.TemPortaPara(d) ? mapa.Vizinha(atual, d) : null;

                if (outra != null && casas.Contains(outra) && alcancadas.Add(outra))
                    fila.Enqueue(outra);
            }
        }

        return alcancadas.Count == casas.Count;
    }

    /// <summary>
    /// Transforma em <paramref name="tipo"/> a candidata mais longe (em casas) de todas as
    /// salas especiais ja postas, com um pouco de sorteio no meio pra nao ficar previsivel.
    /// </summary>
    private static SalaDoAndar Por(TipoDeSala tipo, List<SalaDoAndar> candidatas, List<SalaDoAndar> postas, Random sorteio)
    {
        if (candidatas.Count == 0)
            return null;

        SalaDoAndar melhor = null;
        double melhorNota = double.MinValue;

        foreach (SalaDoAndar sala in candidatas)
        {
            int perto = int.MaxValue;

            foreach (SalaDoAndar posta in postas)
                perto = Math.Min(perto, Math.Abs(sala.X - posta.X) + Math.Abs(sala.Y - posta.Y));

            double nota = Math.Min(perto, 4) + sorteio.NextDouble() * 2.0;

            if (nota > melhorNota)
            {
                melhorNota = nota;
                melhor = sala;
            }
        }

        melhor.Tipo = tipo;
        candidatas.Remove(melhor);
        postas.Add(melhor);
        return melhor;
    }

    /// <summary>Anota o caminho mais curto do inicio ao chefe (busca em largura guardando de onde veio).</summary>
    private static void MarcarCaminho(MapaDoAndar mapa)
    {
        mapa.CaminhoAteOChefe.Clear();

        if (mapa.Chefe == null)
            return;

        // Anda pra tras a partir do chefe: sempre ha uma vizinha com distancia um a menos.
        SalaDoAndar atual = mapa.Chefe;

        while (atual != null)
        {
            atual.NoCaminhoDoChefe = true;
            mapa.CaminhoAteOChefe.Insert(0, atual);

            if (atual == mapa.Inicio)
                break;

            SalaDoAndar anterior = null;

            foreach (Direcao d in Direcoes.Todas)
            {
                SalaDoAndar v = mapa.PelaPorta(atual, d);

                if (v != null && v.Distancia == atual.Distancia - 1)
                {
                    anterior = v;
                    break;
                }
            }

            atual = anterior;
        }
    }

    /// <summary>
    /// Da um desenho de obstaculo a cada sala comum, da inicial pra fora: um saco embaralhado
    /// com todos os desenhos (so repete depois de usar todos), pulando o desenho que uma
    /// vizinha ja tem. Umas poucas salas ficam sem obstaculo, nunca duas coladas.
    /// </summary>
    private static void EscolherDesenhos(MapaDoAndar mapa, ParametrosDoAndar p, Random sorteio)
    {
        if (p.Desenhos == null || p.Desenhos.Count == 0)
            return;

        List<SalaDoAndar> ordem = new List<SalaDoAndar>(mapa.Salas);
        ordem.Sort((a, b) => a.Distancia.CompareTo(b.Distancia));

        HashSet<SalaDoAndar> prontas = new HashSet<SalaDoAndar>();
        List<int> saco = new List<int>();

        foreach (SalaDoAndar sala in ordem)
        {
            if (sala.Tipo != TipoDeSala.Normal)
                continue;

            bool vizinhaVazia = false;

            foreach (Direcao d in Direcoes.Todas)
            {
                SalaDoAndar v = mapa.Vizinha(sala, d);

                if (v != null && prontas.Contains(v) && v.Desenho < 0)
                    vizinhaVazia = true;
            }

            prontas.Add(sala);

            if (!vizinhaVazia && sorteio.NextDouble() < p.ChanceDeSalaVazia)
            {
                sala.Desenho = -1;
                continue;
            }

            if (saco.Count == 0)
            {
                saco.AddRange(p.Desenhos);

                Embaralhar(saco, sorteio);
            }

            int escolha = 0;

            for (int i = 0; i < saco.Count; i++)
            {
                bool repete = false;

                foreach (Direcao d in Direcoes.Todas)
                {
                    SalaDoAndar v = mapa.Vizinha(sala, d);

                    if (v != null && prontas.Contains(v) && v.Desenho == saco[i])
                        repete = true;
                }

                if (!repete)
                {
                    escolha = i;
                    break;
                }
            }

            sala.Desenho = saco[escolha];
            saco.RemoveAt(escolha);
            sala.EspelharX = sorteio.Next(2) == 0;
            sala.EspelharY = sorteio.Next(2) == 0;
        }
    }

    /// <summary>
    /// A sala secreta, como no Isaac: numa casa VAZIA encostada no maior numero possivel
    /// de salas (de preferencia tres ou mais), mas nunca colada na do chefe. Ela nao entra
    /// na arvore: as portas pra ela ficam escondidas na parede e so abrem com bomba.
    /// </summary>
    private static void PorSecreta(MapaDoAndar mapa, Random sorteio)
    {
        List<(int x, int y)> melhores = new List<(int x, int y)>();
        int maisVizinhas = 1;

        for (int x = 0; x < mapa.Largura; x++)
        {
            for (int y = 0; y < mapa.Altura; y++)
            {
                if (mapa.Em(x, y) != null)
                    continue;

                int vizinhas = 0;
                bool perigosa = false;

                foreach (Direcao d in Direcoes.Todas)
                {
                    SalaDoAndar v = mapa.Em(x + Direcoes.Dx(d), y + Direcoes.Dy(d));

                    if (v == null)
                        continue;

                    vizinhas++;

                    if (v.Tipo == TipoDeSala.Chefe)
                        perigosa = true;
                }

                if (perigosa || vizinhas < maisVizinhas)
                    continue;

                if (vizinhas > maisVizinhas)
                {
                    maisVizinhas = vizinhas;
                    melhores.Clear();
                }

                melhores.Add((x, y));
            }
        }

        // So casas com pelo menos duas vizinhas: senao seria so mais um beco escondido.
        if (melhores.Count == 0 || maisVizinhas < 2)
            return;

        (int sx, int sy) = melhores[sorteio.Next(melhores.Count)];
        SalaDoAndar secreta = mapa.Criar(sx, sy);
        secreta.Tipo = TipoDeSala.Secreta;

        // Porta (escondida do lado de fora) pra toda sala encostada.
        foreach (Direcao d in Direcoes.Todas)
            mapa.Ligar(secreta, d);
        secreta.Distancia = -1;
        mapa.Secreta = secreta;
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
                SalaDoAndar vizinha = mapa.PelaPorta(atual, d);

                if (vizinha == null || vizinha.Distancia >= 0)
                    continue;

                vizinha.Distancia = atual.Distancia + 1;
                fila.Enqueue(vizinha);
            }
        }
    }
}
