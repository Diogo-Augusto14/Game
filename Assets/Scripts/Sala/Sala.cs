using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Como uma casa de uma sala grande (corredor, 2x2, L) se emenda nas outras casas da mesma
/// sala. Lado aberto = sem parede nem porta, so chao ate a casa vizinha. Quem preenche e o
/// <see cref="Andar"/>; sala de uma casa so usa o padrao (tudo fechado).
/// </summary>
public struct Juncoes
{
    public bool Cima, Baixo, Esquerda, Direita;

    /// <summary>A casa da direita (da mesma sala) tambem e aberta pra cima / pra baixo.</summary>
    public bool DireitaAbreCima, DireitaAbreBaixo;

    /// <summary>
    /// As quatro casas em volta deste canto sao da mesma sala (o meio de um 2x2): ali e chao.
    /// Nos outros cantos de um lado aberto fica um pilar de parede.
    /// </summary>
    public bool CantoSupDir, CantoSupEsq, CantoInfDir, CantoInfEsq;

    public bool Aberto(LadoDaPorta lado)
    {
        switch (lado)
        {
            case LadoDaPorta.Cima: return Cima;
            case LadoDaPorta.Baixo: return Baixo;
            case LadoDaPorta.Esquerda: return Esquerda;
            default: return Direita;
        }
    }

    public bool Alguma => Cima || Baixo || Esquerda || Direita;
}

/// <summary>
/// Uma sala estilo Isaac: chao, quatro paredes, uma porta no meio de cada parede (ou
/// parede lisa, se nao tiver vizinho daquele lado) e os inimigos.
///
/// O ciclo:
///   1. Montada: portas abertas, inimigos dormindo.
///   2. O jogador entra (ou alguem chama <see cref="Ativar"/>): se tem inimigo vivo, as
///      portas FECHAM e os inimigos acordam.
///   3. Morreu o ultimo inimigo: portas abrem, <see cref="Limpa"/> vira true e sai o
///      evento <see cref="AoLimpar"/> (bom pra soltar premio, tocar som, marcar no mapa).
///
/// Sala sem inimigo ja nasce limpa e nunca fecha.
///
/// A sala NAO troca o jogador de sala: cada <see cref="Porta"/> avisa em
/// <see cref="Porta.AoAtravessar"/> e quem gera o andar decide pra onde ir.
///
/// Duas formas de usar:
///   • Por codigo: <see cref="Criar"/> e depois <see cref="CriarInimigo"/>.
///   • Na cena: objeto vazio + componente Sala, marque as portas no Inspector e arraste
///     inimigos (InimigoPerseguidor / InimigoAtirador) como filhos. Ela se monta no Start.
/// </summary>
[DisallowMultipleComponent]
public class Sala : MonoBehaviour
{
    // Tamanho do Isaac: 13 x 7 ladrilhos de chao.
    public static readonly Vector2 TamanhoPadrao = new Vector2(13f, 7f);

    [Header("Forma")]
    [Tooltip("Area de chao por dentro das paredes, em unidades")]
    [SerializeField] private Vector2 tamanhoInterno = new Vector2(13f, 7f);

    [SerializeField, Min(0.2f)] private float espessuraDaParede = 1f;

    [SerializeField, Min(0.5f)] private float larguraDaPorta = 1.5f;

    [Header("Portas")]
    [SerializeField] private bool portaCima = true;
    [SerializeField] private bool portaBaixo = true;
    [SerializeField] private bool portaEsquerda = true;
    [SerializeField] private bool portaDireita = true;

    [Header("Comportamento")]
    [Tooltip("Liga sozinha quando o jogador entra. Desligue se quem gera o andar for chamar Ativar()")]
    [SerializeField] private bool ativarAoEntrar = true;

    [SerializeField] private string tagDoJogador = "Player";

    [Header("Cores")]
    [SerializeField] private Color corDoChao = new Color(0.22f, 0.17f, 0.14f);

    [SerializeField] private Color corDaParede = new Color(0.35f, 0.3f, 0.28f);

    [Header("Eventos")]
    [Tooltip("As portas fecharam: comecou a luta")]
    public UnityEvent AoFechar = new UnityEvent();

    [Tooltip("Morreu o ultimo inimigo: portas abertas")]
    public UnityEvent AoLimpar = new UnityEvent();

    [Tooltip("Morreu o ultimo inimigo, mas SegurarPortas deixou as portas fechadas (a proxima onda do desafio)")]
    public UnityEvent AoEsvaziar = new UnityEvent();

    private readonly Dictionary<LadoDaPorta, Porta> portas = new Dictionary<LadoDaPorta, Porta>();
    private readonly List<InimigoDeSala> inimigos = new List<InimigoDeSala>();
    private readonly HashSet<Vector2Int> celulasOcupadas = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, Transform> fossos = new Dictionary<Vector2Int, Transform>();

    // Ladrilhos que ninguem atravessa andando (pedra, bloco de parede, buraco). Pedra quebrada por
    // bomba vira null e o ladrilho libera sozinho.
    private readonly Dictionary<Vector2Int, Object> bloqueios = new Dictionary<Vector2Int, Object>();

    private Transform cenario;
    private Transform pastaDeInimigos;
    private bool montada;
    private int vivos;
    private Juncoes juncoes;
    private Sala[] grupo;
    private SpriteRenderer cortina;

    public bool Ativa { get; private set; }

    public bool Limpa { get; private set; }

    public int InimigosVivos => vivos;

    /// <summary>
    /// Com isto ligado, a sala nao abre as portas quando o ultimo inimigo morre: avisa em
    /// <see cref="AoEsvaziar"/> e espera <see cref="Liberar"/>. E a sala de desafio entre ondas.
    /// </summary>
    public bool SegurarPortas { get; set; }

    public Vector2 TamanhoInterno => tamanhoInterno;

    /// <summary>Tamanho total, paredes incluidas. Bom pra enquadrar a camera.</summary>
    public Vector2 TamanhoTotal => tamanhoInterno + Vector2.one * espessuraDaParede * 2f;

    public IEnumerable<Porta> Portas => portas.Values;

    public IReadOnlyList<InimigoDeSala> Inimigos => inimigos;

    /// <summary>
    /// Quanto do contraste do piso da sala pronta sai (0 = arte pura, 1 = chao liso). As manchas de
    /// terra com folhinhas tinham o mesmo contraste das coisas do jogo e embolavam a leitura.
    /// </summary>
    private const float ContrasteTiradoDoPiso = 0.35f;

    /// <summary>O piso da sala pronta, sem as paredes (a de cima, em perspectiva, desce meia unidade).</summary>
    private static readonly Rect AreaDoPiso = new Rect(-6.5f, -3.5f, 13f, 6.5f);

    /// <summary>A sala usa a imagem pronta do Old Prison (paredes e portas desenhadas por cima dela).</summary>
    public bool ComFundo { get; private set; }

    /// <summary>Primeira camada que vale como parede no projeto (Parede, Wall...).</summary>
    public static int CamadaDeParede
    {
        get
        {
            int mascara = Camadas.MascaraDeParede;

            for (int i = 0; i < 32; i++)
            {
                if ((mascara & (1 << i)) != 0)
                    return i;
            }

            return 0;
        }
    }

    // ================================================================ criar por codigo
    /// <summary>
    /// Cria e monta uma sala. <paramref name="portasExistentes"/> diz quais lados tem porta
    /// (os que nao estiverem ali viram parede lisa). Nulo = porta nos quatro lados.
    /// </summary>
    public static Sala Criar(string nome, Vector2 centro, ICollection<LadoDaPorta> portasExistentes = null,
                             Transform pai = null)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(pai, false);
        obj.transform.position = centro;

        Sala sala = obj.AddComponent<Sala>();

        if (portasExistentes != null)
        {
            sala.portaCima = portasExistentes.Contains(LadoDaPorta.Cima);
            sala.portaBaixo = portasExistentes.Contains(LadoDaPorta.Baixo);
            sala.portaEsquerda = portasExistentes.Contains(LadoDaPorta.Esquerda);
            sala.portaDireita = portasExistentes.Contains(LadoDaPorta.Direita);
        }

        sala.Montar();
        return sala;
    }

    /// <summary>
    /// Como <see cref="Criar(string, Vector2, ICollection{LadoDaPorta}, Transform)"/>, pra uma casa
    /// de sala grande: os lados abertos de <paramref name="juncoes"/> ficam sem parede e sem porta.
    /// </summary>
    public static Sala Criar(string nome, Vector2 centro, ICollection<LadoDaPorta> portasExistentes, Juncoes juncoes,
                             Transform pai = null)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(pai, false);
        obj.transform.position = centro;

        Sala sala = obj.AddComponent<Sala>();
        sala.juncoes = juncoes;
        sala.portaCima = portasExistentes.Contains(LadoDaPorta.Cima);
        sala.portaBaixo = portasExistentes.Contains(LadoDaPorta.Baixo);
        sala.portaEsquerda = portasExistentes.Contains(LadoDaPorta.Esquerda);
        sala.portaDireita = portasExistentes.Contains(LadoDaPorta.Direita);
        sala.Montar();
        return sala;
    }

    /// <summary>
    /// Liga as casas de uma sala grande: entrar numa acorda todas, as portas de todas fecham
    /// juntas e so abrem quando o ultimo inimigo da sala inteira morre.
    /// </summary>
    public static void Agrupar(IList<Sala> casas)
    {
        if (casas == null || casas.Count < 2)
            return;

        Sala[] todas = new Sala[casas.Count];
        casas.CopyTo(todas, 0);

        foreach (Sala casa in todas)
            casa.grupo = todas;
    }

    /// <summary>As casas da mesma sala grande (so esta, se a sala for de uma casa).</summary>
    public IEnumerable<Sala> Grupo => grupo ?? new[] { this };

    /// <summary>Inimigos vivos na sala inteira (todas as casas, se for sala grande).</summary>
    public int VivosNoGrupo
    {
        get
        {
            if (grupo == null)
                return vivos;

            int n = 0;

            foreach (Sala casa in grupo)
                n += casa.vivos;

            return n;
        }
    }

    /// <summary>
    /// Tampa preta do tamanho da casa, por cima de tudo do mundo: a camera mostra so a sala onde
    /// o jogador esta (as vizinhas, a secreta e o vazio em volta ficam pretos).
    /// </summary>
    public bool Coberta
    {
        get => cortina != null && cortina.enabled;
        set
        {
            Montar();

            if (cortina != null)
                cortina.enabled = value;
        }
    }

    /// <summary>Atalho sem nome (o objeto se chama "Sala"). Pensado pro gerador de andar.</summary>
    public static Sala Criar(Vector2 centro, ICollection<LadoDaPorta> portasExistentes, Transform pai = null)
    {
        return Criar("Sala", centro, portasExistentes, pai);
    }

    /// <summary>Poe um inimigo novo na sala, na posicao dada (relativa ao centro da sala).</summary>
    public InimigoDeSala CriarInimigo(TipoDeInimigo tipo, Vector2 posicaoLocal)
    {
        Montar();

        InimigoDeSala novo = FabricaDeInimigos.Criar(tipo, (Vector2)transform.position + posicaoLocal, pastaDeInimigos);
        Registrar(novo);

        // Sala ja em luta: o recem-chegado acorda na hora.
        if (Ativa && !Limpa)
            novo.Acordar();

        return novo;
    }

    /// <summary>A porta de um lado (existindo ou nao). Null so se a sala ainda nao montou.</summary>
    public Porta PortaEm(LadoDaPorta lado)
    {
        portas.TryGetValue(lado, out Porta porta);
        return porta;
    }

    /// <summary>
    /// Ponto aleatorio dentro da sala, com uma margem das paredes e fora de pedra e
    /// espinho. Relativo ao centro.
    /// </summary>
    public Vector2 PontoLivreAleatorio(float margem = 1.5f)
    {
        Vector2 meio = tamanhoInterno * 0.5f - Vector2.one * margem;
        Vector2 ponto = Vector2.zero;

        for (int tentativa = 0; tentativa < 30; tentativa++)
        {
            ponto = new Vector2(Random.Range(-meio.x, meio.x), Random.Range(-meio.y, meio.y));

            if (Livre(ponto))
                break;
        }

        return ponto;
    }

    /// <summary>True se um corpo desta folga (raio) cabe no ponto sem encostar em obstaculo.</summary>
    public bool Livre(Vector2 posicaoLocal, float folga = 0.45f)
    {
        foreach (Vector2Int celula in celulasOcupadas)
        {
            if (Mathf.Abs(posicaoLocal.x - celula.x) < 0.5f + folga && Mathf.Abs(posicaoLocal.y - celula.y) < 0.5f + folga)
                return false;
        }

        return true;
    }

    // ================================================================ caminho dos inimigos
    private int[,] distancias;
    private Vector2Int destinoDoMapa = new Vector2Int(int.MinValue, 0);
    private float mapaFeitoEm = -10f;

    private static readonly Vector2Int[] Vizinhos =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
    };

    private int MeiaLargura => Mathf.FloorToInt(tamanhoInterno.x * 0.5f);
    private int MeiaAltura => Mathf.FloorToInt(tamanhoInterno.y * 0.5f);

    /// <summary>O ladrilho (do chao desta sala) em que um ponto do mundo cai.</summary>
    public Vector2Int Ladrilho(Vector2 pontoNoMundo)
    {
        Vector2 local = pontoNoMundo - (Vector2)transform.position;
        return new Vector2Int(Mathf.RoundToInt(local.x), Mathf.RoundToInt(local.y));
    }

    private bool DentroDoChao(Vector2Int c) => Mathf.Abs(c.x) <= MeiaLargura && Mathf.Abs(c.y) <= MeiaAltura;

    /// <summary>Pedra, bloco de parede ou buraco nesse ladrilho (fora do chao tambem conta).</summary>
    public bool Bloqueado(Vector2Int c) => !DentroDoChao(c) || (bloqueios.TryGetValue(c, out Object o) && o != null);

    /// <summary>
    /// Da pra ir em linha reta de um ponto ao outro sem passar por obstaculo (com a folga do
    /// corpo). Confere so o chao desta sala.
    /// </summary>
    public bool LinhaLivre(Vector2 de, Vector2 para, float folga)
    {
        if (bloqueios.Count == 0)
            return true;

        float distancia = Vector2.Distance(de, para);
        int passos = Mathf.CeilToInt(distancia / 0.25f);

        for (int i = 1; i <= passos; i++)
        {
            Vector2 p = Vector2.Lerp(de, para, i / (float)passos);
            Vector2 local = p - (Vector2)transform.position;

            foreach (KeyValuePair<Vector2Int, Object> b in bloqueios)
            {
                if (b.Value == null)
                    continue;

                if (Mathf.Abs(local.x - b.Key.x) < 0.5f + folga && Mathf.Abs(local.y - b.Key.y) < 0.5f + folga)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Pra onde andar (direcao normalizada) pra chegar de <paramref name="de"/> ate
    /// <paramref name="para"/> contornando pedra, bloco e buraco: um mapa de distancias pelos
    /// ladrilhos do chao, refeito quando o destino muda de ladrilho (no maximo ~4 vezes por
    /// segundo, e um so pra todos os inimigos da sala). Null se nao precisa (ou nao da pra
    /// calcular): ai o inimigo segue reto.
    /// </summary>
    public Vector2? ProximoPasso(Vector2 de, Vector2 para)
    {
        if (bloqueios.Count == 0)
            return null;

        Vector2Int alvo = Ladrilho(para);
        Vector2Int aqui = Ladrilho(de);

        if (!DentroDoChao(alvo) || !DentroDoChao(aqui) || alvo == aqui)
            return null;

        if (alvo != destinoDoMapa || Time.time - mapaFeitoEm > 0.25f)
            MontarMapa(alvo);

        int Dist(Vector2Int c) => DentroDoChao(c) ? distancias[c.x + MeiaLargura, c.y + MeiaAltura] : int.MaxValue;

        int melhor = Bloqueado(aqui) ? int.MaxValue : Dist(aqui);
        Vector2Int escolhido = aqui;

        foreach (Vector2Int v in Vizinhos)
        {
            Vector2Int c = aqui + v;

            if (Bloqueado(c))
                continue;

            // Diagonal so com os dois lados livres: senao raspa a quina da pedra e fica preso.
            if (v.x != 0 && v.y != 0 && (Bloqueado(aqui + new Vector2Int(v.x, 0)) || Bloqueado(aqui + new Vector2Int(0, v.y))))
                continue;

            int d = Dist(c);

            if (d < melhor)
            {
                melhor = d;
                escolhido = c;
            }
        }

        if (escolhido == aqui || melhor == int.MaxValue)
            return null;

        Vector2 rumo = (Vector2)transform.position + (Vector2)escolhido - de;
        return rumo.sqrMagnitude > 0.0001f ? rumo.normalized : (Vector2?)null;
    }

    private void MontarMapa(Vector2Int alvo)
    {
        int largura = MeiaLargura * 2 + 1;
        int altura = MeiaAltura * 2 + 1;

        if (distancias == null || distancias.GetLength(0) != largura || distancias.GetLength(1) != altura)
            distancias = new int[largura, altura];

        for (int x = 0; x < largura; x++)
            for (int y = 0; y < altura; y++)
                distancias[x, y] = int.MaxValue;

        destinoDoMapa = alvo;
        mapaFeitoEm = Time.time;

        Queue<Vector2Int> fila = new Queue<Vector2Int>();
        distancias[alvo.x + MeiaLargura, alvo.y + MeiaAltura] = 0;
        fila.Enqueue(alvo);

        while (fila.Count > 0)
        {
            Vector2Int atual = fila.Dequeue();
            int d = distancias[atual.x + MeiaLargura, atual.y + MeiaAltura];

            for (int i = 0; i < 4; i++)
            {
                Vector2Int c = atual + Vizinhos[i];

                if (Bloqueado(c) || distancias[c.x + MeiaLargura, c.y + MeiaAltura] != int.MaxValue)
                    continue;

                distancias[c.x + MeiaLargura, c.y + MeiaAltura] = d + 1;
                fila.Enqueue(c);
            }
        }
    }

    /// <summary>
    /// Poe uma pedra ou espinhos no ladrilho dado (x e y inteiros a partir do centro; numa
    /// sala 13x7, x vai de -6 a 6 e y de -3 a 3). Ladrilho ja ocupado fica como esta.
    /// </summary>
    public void PorObstaculo(TipoDeObstaculo tipo, Vector2Int celula)
    {
        Montar();

        if (!celulasOcupadas.Add(celula))
            return;

        Vector2 posicao = celula;

        switch (tipo)
        {
            case TipoDeObstaculo.Pedra:
                bloqueios[celula] = Pedra.Criar(cenario, posicao, TemaDoAndar.Atual != null ? TemaDoAndar.Atual.CorDaPedra : Color.white);
                break;
            case TipoDeObstaculo.Muro:
                bloqueios[celula] = Muro.Criar(cenario, posicao);
                break;
            case TipoDeObstaculo.Fosso:
                Fosso fosso = Fosso.Criar(cenario, posicao);
                fossos[celula] = fosso.transform;
                bloqueios[celula] = fosso;
                break;
            default:
                Espinhos.Criar(cenario, posicao);
                break;
        }
    }

    /// <summary>Chamar depois de por todos os obstaculos: desenha a borda dos buracos so onde dao pro chao.</summary>
    public void AcabarObstaculos()
    {
        Color borda = new Color(0.16f, 0.12f, 0.14f);

        foreach (KeyValuePair<Vector2Int, Transform> par in fossos)
        {
            Vector2Int c = par.Key;
            Fosso.DesenharBorda(par.Value, !fossos.ContainsKey(c + Vector2Int.up), !fossos.ContainsKey(c + Vector2Int.down),
                                !fossos.ContainsKey(c + Vector2Int.left), !fossos.ContainsKey(c + Vector2Int.right), borda);
        }
    }

    /// <summary>
    /// Enfeita a sala com arte importada, so desenho (nao bloqueia nada):
    ///   - <paramref name="quantos"/> enfeites de chao (cogumelo, pedrinha, osso) em pontos livres;
    ///   - duas tochas acesas na parede de cima, uma de cada lado da porta;
    ///   - as vezes um candelabro num canto livre.
    /// Com um <see cref="TemaDoAndar"/> em jogo, ele muda a mistura (ossos, runas, candelabro).
    /// Sem a arte, a parte que faltar simplesmente nao aparece.
    /// </summary>
    public void Enfeitar(int quantos)
    {
        Montar();

        // O tema do andar decide quanto osso, runa e candelabro aparece.
        TemaDoAndar tema = TemaDoAndar.Atual;

        // A sala pronta ja traz enfeites encostados nas paredes: no meio vai so metade.
        if (ComFundo)
            quantos /= 2;

        for (int i = 0; i < quantos; i++)
        {
            Sprite sprite = tema != null && Random.value < tema.ChanceDeRuna ? ArteImportada.Runa : null;

            if (sprite == null)
                sprite = tema != null ? ArteImportada.EnfeiteAleatorio(tema.ChanceDeOsso) : ArteImportada.EnfeiteAleatorio();

            if (sprite == null)
                break;

            // Longe das paredes, pra nao tampar porta.
            Vector2 ponto = PontoLivreAleatorio(1.2f);
            SpriteRenderer sr = FormasDaSala.Desenho(cenario, "Enfeite", sprite, CorDoEnfeite(sprite),
                                                     ponto, Vector2.one, -9);
            sr.flipX = Random.value < 0.5f;
        }

        Vector2 meio = tamanhoInterno * 0.5f;

        // Tochas na parte de baixo da parede de cima, a um quarto da largura de cada lado. A
        // camera corta o alto da parede (cabe a largura da sala): a chama tem de caber abaixo disso.
        // Sala pronta: as arandelas ja estao na parede de cima (x = +-3,25); aqui so acende a luz delas.
        // Casa de sala grande aberta pra cima: a parede (e as arandelas) ali virou chao.
        if (ComFundo && !juncoes.Cima)
        {
            for (int lado = -1; lado <= 1; lado += 2)
                HaloCintilante.Criar(cenario, new Vector2(lado * 3.25f, meio.y + 0.15f), 2f, new Color(1f, 0.72f, 0.35f, 0.16f));
        }

        Sprite[] tocha = ComFundo ? null : ArteImportada.TochaDeParede(32f);

        for (int lado = -1; lado <= 1 && tocha != null; lado += 2)
        {
            Vector2 local = new Vector2(lado * meio.x * 0.5f, meio.y + espessuraDaParede * 0.1f);
            EfeitoDeQuadros.Criar(tocha, 8f, (Vector2)transform.position + local, 1, cenario)?.EmLoop();
            HaloCintilante.Criar(cenario, local + Vector2.down * 0.2f, 3.4f, new Color(1f, 0.7f, 0.3f, 0.3f));
        }

        // Bandeiras do Old Prison penduradas na parede de cima, perto dos cantos.
        for (int lado = -1; lado <= 1 && !ComFundo; lado += 2)
        {
            Sprite bandeira = ArteImportada.BandeiraDaPrisao(6);

            if (bandeira == null)
                break;

            Vector2 local = new Vector2(lado * meio.x * 0.62f, meio.y + espessuraDaParede * 0.35f);
            FormasDaSala.Desenho(cenario, "Bandeira", bandeira, Color.white, local, Vector2.one, 1);
        }

        // Candelabro: meia chance, num canto que nao tenha pedra nem espinho.
        Sprite[] candelabro = ArteImportada.CandelabroDaPrisao() ?? ArteImportada.Candelabro(16f);

        if (candelabro != null && Random.value < (tema != null ? tema.ChanceDeCandelabro : 0.5f))
        {
            Vector2Int canto = new Vector2Int(Random.value < 0.5f ? -1 : 1, -1);   // so nos cantos de baixo: os de cima ficam sob a HUD
            Vector2 local = new Vector2(canto.x * (meio.x - 0.6f), canto.y * (meio.y - 0.6f));

            if (Livre(local, 0.2f))
            {
                EfeitoDeQuadros.Criar(candelabro, 6f, (Vector2)transform.position + local + Vector2.down * 0.45f, -8, cenario)?.EmLoop();
                HaloCintilante.Criar(cenario, local + new Vector2(-canto.x * 0.35f, 0.9f), 2f, new Color(1f, 0.75f, 0.35f, 0.26f));
            }
        }
    }

    /// <summary>
    /// Ossos sao so enfeite e nao podem disputar atencao com o que mexe: ficam um pouco apagados.
    /// O esqueleto inteiro, do tamanho de um bicho, fica bem mais (de pe parecia inimigo parado).
    /// </summary>
    private static Color CorDoEnfeite(Sprite enfeite)
    {
        if (ArteImportada.EsqueletoInteiro(enfeite))
            return new Color(0.5f, 0.48f, 0.5f);

        return ArteImportada.OssoDaPrisao(enfeite) ? new Color(0.78f, 0.76f, 0.78f) : Color.white;
    }

    /// <summary>
    /// Troca a cor do chao e das paredes (cada andar tem a sua). Com a arte do pacote,
    /// <paramref name="forca"/> diz quanto a cor puxa os ladrilhos pro tom dela.
    /// </summary>
    public void Pintar(Color chao, Color parede, float forca = 0.4f)
    {
        Montar();

        // Os ladrilhos do pacote ja tem cor: a do andar so puxa pro tom dele.
        if (ArteGerada.CenarioDoPacote)
        {
            chao = Color.Lerp(Color.white, chao, forca);
            parede = Color.Lerp(Color.white, parede, forca);
        }

        corDoChao = chao;
        corDaParede = parede;

        foreach (Transform peca in cenario)
        {
            if (!peca.TryGetComponent(out SpriteRenderer sr))
                continue;

            if (peca.name == "Chao")
                sr.color = chao;
            else if (peca.name.StartsWith("Parede"))
                sr.color = parede;
        }

        foreach (Porta porta in portas.Values)
            porta.PintarParede(parede);
    }

    // ================================================================ ciclo de vida
    private void Start()
    {
        Montar();

        // Sala montada na cena, com inimigos arrastados como filhos.
        foreach (InimigoDeSala inimigo in GetComponentsInChildren<InimigoDeSala>(true))
            Registrar(inimigo);

        if (vivos == 0)
            Limpa = true;
    }

    /// <summary>
    /// Fecha as portas e acorda os inimigos. Se nao tem inimigo vivo, so marca como limpa.
    /// Chamar de novo nao faz nada.
    /// </summary>
    public void Ativar()
    {
        if (Ativa)
            return;

        // Sala grande: as outras casas ligam junto (portas fecham e bichos acordam em todas).
        if (grupo != null)
        {
            foreach (Sala casa in grupo)
                casa.AtivarSo();

            return;
        }

        AtivarSo();
    }

    private void AtivarSo()
    {
        if (Ativa)
            return;

        Montar();
        Ativa = true;

        if (VivosNoGrupo == 0)
        {
            // Emboscada: vazia, mas segurando as portas. Fecha e avisa que esta vazia, pra
            // quem segura (SalaDeEmboscada) mandar a primeira onda.
            if (SegurarPortas)
            {
                Fechar();
                AoEsvaziar?.Invoke();
                return;
            }

            Limpar();
            return;
        }

        // Casa sem bicho de uma sala grande em luta: fecha junto e espera a sala toda.
        Limpa = false;

        foreach (Porta porta in portas.Values)
            porta.Fechar();

        Sons.Tocar(Som.PortaFecha);

        foreach (InimigoDeSala inimigo in inimigos)
        {
            if (inimigo != null && !inimigo.EstaMorto)
                inimigo.Acordar();
        }

        AoFechar?.Invoke();
    }

    /// <summary>
    /// Fecha as portas na hora, mesmo sem inimigo vivo (a emboscada da sala de desafio).
    /// Os inimigos criados depois ja nascem acordados.
    /// </summary>
    public void Fechar()
    {
        Montar();
        Ativa = true;
        Limpa = false;

        foreach (Porta porta in portas.Values)
            porta.Fechar();

        Sons.Tocar(Som.PortaFecha);
        AoFechar?.Invoke();
    }

    /// <summary>Desliga <see cref="SegurarPortas"/> e, sem inimigo vivo, abre as portas.</summary>
    public void Liberar()
    {
        SegurarPortas = false;

        if (vivos == 0 && Ativa)
            Limpar();
    }

    private void Registrar(InimigoDeSala inimigo)
    {
        if (inimigo == null || inimigos.Contains(inimigo))
            return;

        inimigos.Add(inimigo);

        if (inimigo.EstaMorto)
            return;

        vivos++;
        Limpa = false;
        inimigo.Vida.AoMorrer.AddListener(() => AoMorrerInimigo(inimigo));
    }

    private void AoMorrerInimigo(InimigoDeSala inimigo)
    {
        ResumoDaPartida.ContarInimigo();
        vivos = Mathf.Max(0, vivos - 1);

        if (vivos != 0 || !Ativa)
            return;

        if (SegurarPortas)
        {
            AoEsvaziar?.Invoke();
            return;
        }

        // Sala grande: so abre quando a sala inteira esvaziou, e entao abre todas as casas.
        if (grupo == null)
        {
            Limpar();
            return;
        }

        if (VivosNoGrupo > 0)
            return;

        foreach (Sala casa in grupo)
            if (casa.Ativa)
                casa.Limpar();
    }

    private void Limpar()
    {
        // So faz barulho quando teve luta: sala vazia abre calada.
        if (inimigos.Count > 0)
            Sons.Tocar(Som.PortaAbre);

        Limpa = true;

        foreach (Porta porta in portas.Values)
            porta.Abrir();

        AoLimpar?.Invoke();
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (!ativarAoEntrar || Ativa)
            return;

        GameObject quem = outro.attachedRigidbody != null ? outro.attachedRigidbody.gameObject : outro.gameObject;

        if (quem.CompareTag(tagDoJogador))
            Ativar();
    }

    // ================================================================ montagem
    /// <summary>Monta chao, paredes e portas. Chamar mais de uma vez nao duplica nada.</summary>
    public void Montar()
    {
        if (montada)
            return;

        montada = true;

        cenario = new GameObject("Cenario").transform;
        cenario.SetParent(transform, false);

        // Com tema do Old Prison a sala inteira (chao, paredes, sombra, enfeites) e uma imagem so;
        // as paredes continuam la, so sem desenho, pra colisao ficar igual.
        Sprite fundo = tamanhoInterno == TamanhoPadrao && espessuraDaParede == 1f ? ArteImportada.SalaDaPrisao() : null;
        ComFundo = fundo != null;

        if (ComFundo)
        {
            FormasDaSala.Desenho(cenario, "Chao", fundo, corDoChao, Vector2.zero, Vector2.one, -12);
            Costurar(fundo);

            // Veu por cima so do piso (sem as paredes): puxa cada pixel pra cor media do chao e
            // baixa o contraste das manchas de terra e folhinhas. Assim pedra, item e inimigo, que
            // ficam por cima dele, saltam mais. Um pouco mais escuro que a media, pelo mesmo motivo.
            Color veu = TemaDoAndar.Atual.CorMediaDoPiso * 0.9f;
            veu.a = ContrasteTiradoDoPiso;
            FormasDaSala.Desenho(cenario, "VeuDoChao", Fosso.Pixel(), veu, AreaDoPiso.center, AreaDoPiso.size, -10);
        }
        else
            FormasDaSala.DesenhoLadrilhado(cenario, "Chao", ArteGerada.Chao(), corDoChao, Vector2.zero, tamanhoInterno, -10);

        MontarLado(cenario, LadoDaPorta.Cima, portaCima);
        MontarLado(cenario, LadoDaPorta.Baixo, portaBaixo);
        MontarLado(cenario, LadoDaPorta.Esquerda, portaEsquerda);
        MontarLado(cenario, LadoDaPorta.Direita, portaDireita);
        PorPilares();

        cortina = FormasDaSala.Desenho(transform, "Cortina", Fosso.Pixel(), Color.black, Vector2.zero, TamanhoTotal, 1000);
        cortina.enabled = false;

        // Sensor de entrada: o interior menos uma margem, pra a porta so fechar quando o
        // jogador ja esta inteiro dentro da sala (e nao em cima do batente).
        BoxCollider2D sensor = gameObject.AddComponent<BoxCollider2D>();
        sensor.isTrigger = true;
        sensor.size = tamanhoInterno - Vector2.one * 1.2f;

        pastaDeInimigos = transform.Find("Inimigos");

        if (pastaDeInimigos == null)
        {
            pastaDeInimigos = new GameObject("Inimigos").transform;
            pastaDeInimigos.SetParent(transform, false);
        }
    }

    /// <summary>
    /// Uma parede inteira: dois pedacos com o vao da porta no meio. As paredes de cima e
    /// de baixo cobrem os cantos; as dos lados ficam so na altura do chao.
    /// </summary>
    private void MontarLado(Transform pai, LadoDaPorta lado, bool temPorta)
    {
        // Lado que da pra outra casa da mesma sala grande: nem parede nem porta.
        if (juncoes.Aberto(lado))
            return;

        float meiaLarg = tamanhoInterno.x * 0.5f;
        float meiaAlt = tamanhoInterno.y * 0.5f;
        float e = espessuraDaParede;
        float meiaPorta = larguraDaPorta * 0.5f;

        bool horizontal = lado.Horizontal();

        // Comprimento total da parede e onde ela fica.
        float comprimento = horizontal ? tamanhoInterno.x + e * 2f : tamanhoInterno.y;
        Vector2 centro = lado.Direcao() * ((horizontal ? meiaAlt : meiaLarg) + e * 0.5f);
        Vector2 eixo = horizontal ? Vector2.right : Vector2.up;

        // Pedaco de cada lado do vao.
        float pedaco = comprimento * 0.5f - meiaPorta;
        float deslocamento = meiaPorta + pedaco * 0.5f;

        for (int s = -1; s <= 1; s += 2)
        {
            Vector2 pos = centro + eixo * (deslocamento * s);
            Vector2 tamanho = horizontal ? new Vector2(pedaco, e) : new Vector2(e, pedaco);
            Parede(pai, $"Parede{lado}{(s < 0 ? "A" : "B")}", pos, tamanho);
        }

        GameObject objPorta = new GameObject($"Porta{lado}");
        objPorta.transform.SetParent(transform, false);
        objPorta.transform.localPosition = centro;

        Porta porta = objPorta.AddComponent<Porta>();
        porta.Configurar(this, lado, temPorta, larguraDaPorta, e);
        portas[lado] = porta;
    }

    /// <summary>
    /// Sala grande: as paredes de cima e de baixo cobrem os cantos. Tirando uma delas (lado aberto),
    /// o canto fica sem parede; ali vai um pilar invisivel, menos no meio de um 2x2, que e chao.
    /// </summary>
    private void PorPilares()
    {
        if (!juncoes.Alguma)
            return;

        float x = tamanhoInterno.x * 0.5f + espessuraDaParede * 0.5f;
        float y = tamanhoInterno.y * 0.5f + espessuraDaParede * 0.5f;

        if (juncoes.Cima && !juncoes.CantoSupDir) Pilar(new Vector2(x, y));
        if (juncoes.Cima && !juncoes.CantoSupEsq) Pilar(new Vector2(-x, y));
        if (juncoes.Baixo && !juncoes.CantoInfDir) Pilar(new Vector2(x, -y));
        if (juncoes.Baixo && !juncoes.CantoInfEsq) Pilar(new Vector2(-x, -y));
    }

    private void Pilar(Vector2 posicaoLocal)
    {
        GameObject obj = new GameObject("Pilar");
        obj.transform.SetParent(cenario, false);
        obj.transform.localPosition = posicaoLocal;
        obj.layer = CamadaDeParede;
        obj.AddComponent<BoxCollider2D>().size = Vector2.one * espessuraDaParede;
    }

    // Recortes da imagem da sala pronta (480x288 px, 32 por unidade), em pixels a partir do canto
    // de CIMA a esquerda: o meio do chao, das paredes de cima e de baixo e das paredes dos lados.
    private static readonly RectInt ChaoDoMeio = new RectInt(192, 48, 96, 208);
    private static readonly RectInt ChaoDoMeioBaixo = new RectInt(32, 88, 416, 112);
    private static readonly RectInt ChaoQuadrado = new RectInt(192, 88, 96, 112);
    private static readonly RectInt ParedeDeCima = new RectInt(192, 0, 96, 48);
    private static readonly RectInt ParedeDeCimaEstreita = new RectInt(208, 0, 64, 48);
    private static readonly RectInt ParedeDeBaixo = new RectInt(192, 256, 96, 32);
    private static readonly RectInt ParedeDeBaixoEstreita = new RectInt(208, 256, 64, 32);
    private static readonly RectInt ParedeEsquerda = new RectInt(0, 88, 32, 112);
    private static readonly RectInt ParedeDireita = new RectInt(448, 88, 32, 112);

    /// <summary>
    /// Sala grande com a imagem pronta do Old Prison: cada casa e uma imagem com as quatro
    /// paredes desenhadas. Nas emendas com a casa da direita e a de cima, pedacos recortados da
    /// propria imagem (chao, parede de cima, de baixo e dos lados) cobrem as paredes que sumiram,
    /// na mesma cor do tema. Cada emenda e desenhada uma vez so (pela casa da esquerda / de baixo).
    /// </summary>
    private void Costurar(Sprite fundo)
    {
        if (!juncoes.Alguma)
            return;

        if (juncoes.Direita)
        {
            Recorte(fundo, ChaoDoMeio, new Vector2(7.5f, -0.25f));

            // Em cima: parede inteira se as duas casas fecham em cima; um pilar se so uma abre (L);
            // nada se as duas abrem (o meio do 2x2 e chao, desenhado pela emenda de cima).
            if (!juncoes.Cima && !juncoes.DireitaAbreCima)
                Recorte(fundo, ParedeDeCima, new Vector2(7.5f, 3.75f));
            else if (juncoes.Cima != juncoes.DireitaAbreCima)
                Recorte(fundo, ParedeDeCimaEstreita, new Vector2(7.5f, 3.75f));

            if (!juncoes.Baixo && !juncoes.DireitaAbreBaixo)
                Recorte(fundo, ParedeDeBaixo, new Vector2(7.5f, -4f));
            else if (juncoes.Baixo != juncoes.DireitaAbreBaixo)
                Recorte(fundo, ParedeDeBaixoEstreita, new Vector2(7.5f, -4f));
        }

        if (juncoes.Cima)
        {
            Recorte(fundo, ChaoDoMeioBaixo, new Vector2(0f, 4f));

            if (!juncoes.CantoSupEsq)
                Recorte(fundo, ParedeEsquerda, new Vector2(-7f, 4f));

            if (!juncoes.CantoSupDir)
                Recorte(fundo, ParedeDireita, new Vector2(7f, 4f));
            else if (juncoes.Direita)
                Recorte(fundo, ChaoQuadrado, new Vector2(7.5f, 4f));
        }
    }

    /// <summary>Um pedaco da imagem da sala (em pixels de cima pra baixo), posto no meio dado, com o veu do piso se for chao.</summary>
    private void Recorte(Sprite fundo, RectInt pixels, Vector2 meio)
    {
        Texture2D textura = fundo.texture;
        Rect r = new Rect(fundo.rect.x + pixels.x, fundo.rect.y + fundo.rect.height - pixels.y - pixels.height, pixels.width, pixels.height);
        Sprite pedaco = Sprite.Create(textura, r, new Vector2(0.5f, 0.5f), fundo.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        pedaco.name = "Emenda";
        FormasDaSala.Desenho(cenario, "Emenda", pedaco, Color.white, meio, Vector2.one, -11);

        // Chao da emenda ganha o mesmo veu do piso (as paredes nao).
        bool chao = pixels.y >= 48 && pixels.y + pixels.height <= 256 && pixels.x >= 32 && pixels.x + pixels.width <= 448;

        if (chao && TemaDoAndar.Atual != null)
        {
            Color veu = TemaDoAndar.Atual.CorMediaDoPiso * 0.9f;
            veu.a = ContrasteTiradoDoPiso;
            FormasDaSala.Desenho(cenario, "VeuDaEmenda", Fosso.Pixel(), veu, meio, new Vector2(pixels.width, pixels.height) / fundo.pixelsPerUnit, -10);
        }
    }

    private void Parede(Transform pai, string nome, Vector2 posicaoLocal, Vector2 tamanho)
    {
        SpriteRenderer sr = FormasDaSala.DesenhoLadrilhado(pai, nome, ArteGerada.Tijolo(), corDaParede, posicaoLocal, tamanho, 0);
        sr.gameObject.layer = CamadaDeParede;
        sr.enabled = !ComFundo;

        BoxCollider2D caixa = sr.gameObject.AddComponent<BoxCollider2D>();
        caixa.size = tamanho;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (montada)
            return;

        Gizmos.color = new Color(0.9f, 0.6f, 0.3f, 0.8f);
        Gizmos.DrawWireCube(transform.position, tamanhoInterno);
        Gizmos.DrawWireCube(transform.position, TamanhoTotal);
    }
#endif
}
