using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

    private readonly Dictionary<LadoDaPorta, Porta> portas = new Dictionary<LadoDaPorta, Porta>();
    private readonly List<InimigoDeSala> inimigos = new List<InimigoDeSala>();
    private readonly HashSet<Vector2Int> celulasOcupadas = new HashSet<Vector2Int>();

    private Transform cenario;
    private Transform pastaDeInimigos;
    private bool montada;
    private int vivos;

    public bool Ativa { get; private set; }

    public bool Limpa { get; private set; }

    public int InimigosVivos => vivos;

    public Vector2 TamanhoInterno => tamanhoInterno;

    /// <summary>Tamanho total, paredes incluidas. Bom pra enquadrar a camera.</summary>
    public Vector2 TamanhoTotal => tamanhoInterno + Vector2.one * espessuraDaParede * 2f;

    public IEnumerable<Porta> Portas => portas.Values;

    public IReadOnlyList<InimigoDeSala> Inimigos => inimigos;

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

        if (tipo == TipoDeObstaculo.Pedra)
            Pedra.Criar(cenario, posicao, Color.Lerp(corDaParede, new Color(0.55f, 0.55f, 0.55f), 0.35f));
        else
            Espinhos.Criar(cenario, posicao);
    }

    /// <summary>Troca a cor do chao e das paredes (cada andar tem a sua).</summary>
    public void Pintar(Color chao, Color parede)
    {
        Montar();
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

        Montar();
        Ativa = true;

        if (vivos == 0)
        {
            Limpar();
            return;
        }

        foreach (Porta porta in portas.Values)
            porta.Fechar();

        foreach (InimigoDeSala inimigo in inimigos)
        {
            if (inimigo != null && !inimigo.EstaMorto)
                inimigo.Acordar();
        }

        AoFechar?.Invoke();
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
        vivos = Mathf.Max(0, vivos - 1);

        if (vivos == 0 && Ativa)
            Limpar();
    }

    private void Limpar()
    {
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

        FormasDaSala.Desenho(cenario, "Chao", FormasDaSala.Quadrado(), corDoChao, Vector2.zero, tamanhoInterno, -10);

        MontarLado(cenario, LadoDaPorta.Cima, portaCima);
        MontarLado(cenario, LadoDaPorta.Baixo, portaBaixo);
        MontarLado(cenario, LadoDaPorta.Esquerda, portaEsquerda);
        MontarLado(cenario, LadoDaPorta.Direita, portaDireita);

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

    private void Parede(Transform pai, string nome, Vector2 posicaoLocal, Vector2 tamanho)
    {
        SpriteRenderer sr = FormasDaSala.Desenho(pai, nome, FormasDaSala.Quadrado(), corDaParede, posicaoLocal, tamanho, 0);
        sr.gameObject.layer = CamadaDeParede;

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
