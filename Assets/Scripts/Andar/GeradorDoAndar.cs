using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Monta e troca os andares: cada andar e uma caverna gigante e aberta, sem salas nem portas, como
/// no Nuclear Throne, ou a arena de um chefe (<see cref="andares"/> diz a ordem).
///
/// A caverna e cavada na hora (<see cref="Caverna"/>) e construida pelo <see cref="Pedreiro"/> com a
/// arte do pacote Old Prison (paredes de tijolo, buracos de abismo, pocas de sangue, ossos), com
/// o jogador numa clareira no centro do mundo. Os inimigos ficam espalhados em grupos, longe do
/// comeco, parados ate verem o jogador. Quando o ultimo morre, o vortice da saida abre ali mesmo;
/// pisar nele escurece a tela e monta o proximo andar, maior e com mais inimigos. O andar do chefe e
/// um salao so dele (<see cref="Arena"/>): matou o chefe, o portal abre e cai um bau. Depois do ultimo
/// andar, a partida acaba em vitoria e recomeca.
///
/// Na tela, a <see cref="TelaDoJogo"/> desenha o nome do andar ao chegar, quantos inimigos faltam e o
/// escuro da troca (daqui ela le); aqui fica so a seta na beirada apontando pro inimigo mais perto,
/// quando sobram poucos. Ao abrir o jogo, mostra o menu inicial (<see cref="TelaDeInicio"/>); a
/// pausa (<see cref="TelaDePausa"/>) fica neste mesmo objeto. Cada andar toca a musica dele.
/// </summary>
[DisallowMultipleComponent]
public class GeradorDoAndar : MonoBehaviour
{
    [Header("Andares")]
    [Tooltip("Os andares da partida, na ordem. Sem chefe: uma caverna. Com chefe: a arena so dele. " +
             "Depois do ultimo, vitoria")]
    [SerializeField] private AndarDaPartida[] andares;

    [Tooltip("Tamanho da primeira caverna, em celulas de chao (1 celula = 1 unidade)")]
    [SerializeField, Min(50)] private int celulasNoPrimeiroAndar = 1500;

    [Tooltip("Celulas a mais em cada caverna seguinte")]
    [SerializeField, Min(0)] private int celulasAMaisPorAndar = 400;

    [Tooltip("A caverna nao passa desta distancia do comeco, em celulas")]
    [SerializeField, Min(10)] private int raioMaximo = 60;

    [Tooltip("Metade da largura e da altura da clareira do comeco, em celulas")]
    [SerializeField] private Vector2Int clareira = new Vector2Int(5, 4);

    [Header("Inimigos")]
    [Tooltip("Os inimigos que aparecem: cada um a partir de um andar, sorteado pelo peso")]
    [SerializeField] private InimigoDoAndar[] inimigos;

    [Tooltip("Inimigos na primeira caverna")]
    [SerializeField, Min(0)] private int inimigosNoPrimeiroAndar = 24;

    [Tooltip("Inimigos a mais em cada caverna seguinte")]

    [SerializeField, Min(0)] private int inimigosAMaisPorAndar = 6;

    [Tooltip("Os inimigos ficam em grupos deste tamanho")]
    [SerializeField] private Vector2Int tamanhoDoGrupo = new Vector2Int(2, 4);

    [Tooltip("Nenhum inimigo fica mais perto do comeco que isto, em unidades")]
    [SerializeField, Min(0f)] private float longeDoComeco = 14f;

    [Tooltip("Ate quantos passos do jogador os inimigos acham caminho contornando paredes")]
    [SerializeField, Min(1)] private int distanciaDosCaminhos = 40;

    [Tooltip("Com esta quantidade de inimigos ou menos, uma seta aponta pro mais perto")]
    [SerializeField, Min(0)] private int setaQuandoFaltarem = 3;

    [Header("Armas")]
    [Tooltip("As armas que saem dos baus e aparecem no chao (sorteadas pelo peso de cada uma)")]
    [SerializeField] private DadosDaArma[] armas;

    [SerializeField, Min(0)] private int bausPorAndar = 3;

    [Tooltip("Armas largadas no chao da caverna, por andar")]
    [SerializeField, Min(0)] private int armasNoChaoPorAndar = 1;

    [Tooltip("No primeiro andar, um bau ja na clareira do comeco")]
    [SerializeField] private bool bauNoComeco = true;

    [Tooltip("Chance de um inimigo soltar uma bolsa de municao ao morrer")]
    [SerializeField, Range(0f, 1f)] private float chanceDeMunicao = 0.12f;

    [Tooltip("Folha do bau (quadros lado a lado: fechado ate aberto)")]
    [SerializeField] private Texture2D bau;
    [SerializeField] private Vector2Int quadroDoBau = new Vector2Int(128, 160);

    [SerializeField] private Sprite caixaDeMunicao;
    [SerializeField] private AudioClip somDoBau;
    [SerializeField] private AudioClip somDaMunicao;

    [Header("Chefe")]
    [Tooltip("Metade da largura e da altura do salao do chefe, em celulas")]
    [SerializeField] private Vector2Int raioDaArena = new Vector2Int(12, 9);

    [Tooltip("Matou o chefe, cai um bau no meio do salao")]
    [SerializeField] private bool bauDepoisDoChefe = true;

    [Header("Buracos e enfeites")]
    [Tooltip("Buracos de abismo no primeiro andar (ninguem passa; o tiro passa por cima)")]
    [SerializeField, Min(0)] private int buracosNoPrimeiroAndar = 8;

    [SerializeField, Min(0)] private int buracosAMaisPorAndar = 3;

    [Tooltip("Pocas de sangue por andar (so enfeite)")]
    [SerializeField, Min(0)] private int pocasPorAndar = 10;

    [Tooltip("Chance de cada ladrilho de chao ganhar um osso, pedrinha ou papel")]
    [SerializeField, Range(0f, 1f)] private float chanceDeEnfeite = 0.05f;

    [Header("Desenhos (folhas de 32 x 32 do Old Prison; ver Ferramentas/OldPrison)")]
    [SerializeField] private Texture2D chao;
    [SerializeField] private Texture2D paredes;
    [SerializeField] private Texture2D abismo;
    [SerializeField] private Texture2D sangue;
    [SerializeField] private Texture2D enfeites;

    [Tooltip("Folha do vortice da saida")]
    [SerializeField] private Texture2D vortice;
    [SerializeField] private Vector2Int quadroDoVortice = new Vector2Int(96, 96);

    [SerializeField, Min(1f)] private float pixelsPorUnidade = 20f;

    [Header("Sons")]
    [Tooltip("Toca quando o ultimo inimigo morre e o vortice abre")]
    [SerializeField] private AudioClip portalAbrindo;
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;

    [Header("Troca de andar")]
    [Tooltip("Segundos escurecendo e clareando a tela")]
    [SerializeField, Min(0.01f)] private float escurecer = 0.5f;

    [Tooltip("Segundos que o nome do andar fica na tela")]
    [SerializeField, Min(0f)] private float tempoDoNome = 2.5f;

    private readonly List<Vida> vivos = new List<Vida>();
    private Pedreiro pedreiro;
    private Sprite[] quadrosDoVortice;
    private Sprite[] quadrosDoBau;
    private AudioSource audioSource;
    private Rigidbody2D corpoDoJogador;
    private Vida vidaDoJogador;
    private Camera cam;
    private GameObject raiz;
    private Saida saida;
    private HashSet<Vector2Int> chaoDoAndar;
    private Vector2 ondeMorreuOUltimo;
    private bool trocando;
    private Chefe chefe;
    private float escuro;
    private string nome;
    private float nomeAte;
    private Texture2D seta;

    /// <summary>O andar atual (o primeiro e 1).</summary>
    public int Andar { get; private set; }

    /// <summary>Quantos andares a partida tem.</summary>
    public int Andares => andares != null && andares.Length > 0 ? andares.Length : 3;

    /// <summary>O andar atual e o de um chefe.</summary>
    public bool AndarDoChefe => chefe != null;

    /// <summary>O chefe deste andar (nulo nas cavernas).</summary>
    public Chefe ChefeAtual => chefe;

    /// <summary>O portal pro proximo andar ja abriu.</summary>
    public bool PortalAberto => saida != null;

    /// <summary>No meio da troca de andar (a tela escurecendo ou clareando).</summary>
    public bool Trocando => trocando;

    /// <summary>Quanto a tela esta escura na troca de andar (0 = nada, 1 = preta).</summary>
    public float Escuro => escuro;

    /// <summary>O nome do andar que aparece ao chegar ("Andar 3 de 6: Covil do Minotauro").</summary>
    public string NomeNaTela => nome;

    /// <summary>Quanto o nome do andar aparece (1 = inteiro; some aos poucos no ultimo segundo).</summary>
    public float AlfaDoNome => Mathf.Clamp01(nomeAte - Time.unscaledTime);

    public Transform Jogador { get; private set; }

    /// <summary>Inimigos vivos no andar.</summary>
    public int Faltam => vivos.Count;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (!TryGetComponent(out TelaDePausa _))
            gameObject.AddComponent<TelaDePausa>();
        pedreiro = new Pedreiro(chao, paredes, abismo, sangue, enfeites, chanceDeEnfeite);
        quadrosDoVortice = FolhaDeSprites.Cortar(vortice, quadroDoVortice, pixelsPorUnidade);
        quadrosDoBau = FolhaDeSprites.Cortar(bau, quadroDoBau, DadosDoOldPrison.Lado);
    }

    private void Start()
    {
        GameObject jogador = GameObject.FindWithTag("Player");

        if (jogador != null)
        {
            Jogador = jogador.transform;
            corpoDoJogador = jogador.GetComponent<Rigidbody2D>();
            vidaDoJogador = jogador.GetComponent<Vida>();
        }

        GerarSemTravar(1);

        // Na primeira vez, o menu inicial por cima do andar 1; "tentar de novo" vem direto pro jogo.
        if (!TelaDeInicio.JaPassou)
            TelaDeInicio.Mostrar(this);
        else
            Musica.Tocar(MusicaDoAndar);
    }

    /// <summary>A musica deste andar: a do chefe (a do final, no ultimo) ou a da caverna.</summary>
    public TemaMusical MusicaDoAndar =>
        chefe != null ? (Andar >= Andares ? TemaMusical.ChefeFinal : TemaMusical.Chefe) : Musica.DaCaverna(CavernasAte(Andar));

    private void Update()
    {
        if (MapaDeCaminhos.Atual != null && Jogador != null)
            MapaDeCaminhos.Atual.Atualizar(Jogador.position);

        if (raiz == null || saida != null || chaoDoAndar == null)
            return;

        // Guarda onde caiu quem acabou de morrer: o vortice abre onde morreu o ultimo.
        for (int i = vivos.Count - 1; i >= 0; i--)
        {
            Vida vida = vivos[i];

            if (vida != null && !vida.Morto)
                continue;

            if (vida != null)
            {
                ondeMorreuOUltimo = vida.transform.position;
                ResumoDaPartida.ContarInimigo();

                if (chefe != null && vida.gameObject == chefe.gameObject)
                    ResumoDaPartida.ContarChefe();

                if (caixaDeMunicao != null && Random.value < chanceDeMunicao)
                    CaixaDeMunicao.Criar(caixaDeMunicao, ondeMorreuOUltimo, raiz.transform, somDaMunicao);
            }

            vivos.RemoveAt(i);
        }

        if (vivos.Count == 0)
        {
            AbrirSaida(ondeMorreuOUltimo);

            // O premio do chefe: um bau no meio do salao (um pouco abaixo de onde o portal costuma abrir).
            if (chefe != null && bauDepoisDoChefe && quadrosDoBau.Length > 0)
            {
                Vector2 meio = Arena.OndeOChefeFica(raioDaArena) - new Vector2Int(0, 4);

                if (Vector2.Distance(meio, ondeMorreuOUltimo) < 2f)
                    meio += Vector2.left * 3f;

                Bau.Criar(quadrosDoBau, meio, raiz.transform, armas, caixaDeMunicao, somDoBau, somDaMunicao);
            }
        }
    }

    private void AbrirSaida(Vector2 onde)
    {
        saida = Saida.Criar(raiz.transform, onde, quadrosDoVortice, 12f, this);

        if (audioSource != null && portalAbrindo != null)
            audioSource.PlayOneShot(portalAbrindo, volume);
    }

    // ---------------- trocar de andar ----------------
    /// <summary>Mostra o nome do andar de novo (ao sair do menu inicial, que escondia ele).</summary>
    public void MostrarNome() => nomeAte = Time.unscaledTime + tempoDoNome;

    /// <summary>O jogador pisou na saida: vai pro proximo andar (ou vence, se era o ultimo).</summary>
    public void ProximoAndar()
    {
        if (!trocando && (vidaDoJogador == null || !vidaDoJogador.Morto))
            StartCoroutine(TrocarDeAndar());
    }

    private IEnumerator TrocarDeAndar()
    {
        trocando = true;
        yield return Escurecer(0f, 1f);

        if (Andar >= Andares)
        {
            // Venceu: a tela da vitoria escurece sozinha por cima do salao do chefe.
            escuro = 0f;
            TelaDeFimDeJogo.MostrarVitoria(this);
            yield break;
        }

        GerarSemTravar(Andar + 1);
        Musica.Tocar(MusicaDoAndar);

        // De volta a clareira do comeco, que fica sempre no centro do mundo.
        if (Jogador != null)
        {
            Jogador.position = Vector3.zero;

            if (corpoDoJogador != null)
            {
                corpoDoJogador.position = Vector2.zero;
                corpoDoJogador.linearVelocity = Vector2.zero;
            }

            CameraDoJogo.Pular();
        }

        yield return Escurecer(1f, 0f);
        trocando = false;
    }

    private IEnumerator Escurecer(float de, float ate)
    {
        for (float t = 0f; t < escurecer; t += Time.unscaledDeltaTime)
        {
            escuro = Mathf.Lerp(de, ate, t / escurecer);
            yield return null;
        }

        escuro = ate;
    }

    // ---------------- montar o andar ----------------
    // Um erro montando o andar nao pode deixar o jogo preso (na tela escura da troca, por exemplo):
    // fica no Console, e o jogo segue com o que deu pra montar.
    private void GerarSemTravar(int andar)
    {
        try
        {
            Gerar(andar);
        }
        catch (System.Exception erro)
        {
            Debug.LogException(erro, this);
        }
    }

    private void Gerar(int andar)
    {
        AndarDaPartida esse = andares != null && andar <= andares.Length ? andares[andar - 1] : null;
        Andar = andar;
        nome = esse != null && !string.IsNullOrEmpty(esse.nome) ? $"Andar {andar} de {Andares}: {esse.nome}" : $"Andar {andar} de {Andares}";
        nomeAte = Time.unscaledTime + tempoDoNome;
        saida = null;
        chefe = null;
        chaoDoAndar = null;
        MapaDeCaminhos.Atual = null;
        vivos.Clear();

        if (raiz != null)
            Destroy(raiz);

        raiz = new GameObject($"Andar {andar}");
        raiz.transform.SetParent(transform, false);

        if (esse != null && esse.chefe != null)
            GerarArena(esse.chefe);
        else
            GerarCaverna(andar, CavernasAte(andar));
    }

    // Quantas cavernas ate este andar, contando ele (o tamanho e os inimigos crescem por caverna).
    private int CavernasAte(int andar)
    {
        if (andares == null || andares.Length == 0)
            return andar;

        int cavernas = 0;

        for (int i = 0; i < andar && i < andares.Length; i++)
        {
            if (andares[i] == null || andares[i].chefe == null)
                cavernas++;
        }

        return Mathf.Max(1, cavernas);
    }

    private void GerarArena(GameObject prefabDoChefe)
    {
        HashSet<Vector2Int> planta = Arena.Montar(raioDaArena);
        HashSet<Vector2Int> semBuracos = new HashSet<Vector2Int>();
        HashSet<Vector2Int> pocas = Caverna.EspalharPocas(planta, semBuracos, pocasPorAndar / 2);
        pedreiro.Construir(raiz.transform, planta, semBuracos, pocas, new System.Random(Random.Range(int.MinValue, int.MaxValue)));
        MapaDeCaminhos.Atual = new MapaDeCaminhos(planta, distanciaDosCaminhos);

        GameObject novo = Instantiate(prefabDoChefe, (Vector2)Arena.OndeOChefeFica(raioDaArena), Quaternion.identity, raiz.transform);
        chefe = novo.GetComponent<Chefe>();

        if (novo.TryGetComponent(out Vida vida))
            vivos.Add(vida);
        else
            AbrirSaida(Arena.OndeOChefeFica(raioDaArena));

        chaoDoAndar = planta;
    }

    private void GerarCaverna(int andar, int caverna)
    {
        HashSet<Vector2Int> planta = Caverna.Cavar(celulasNoPrimeiroAndar + (caverna - 1) * celulasAMaisPorAndar, raioMaximo, clareira);
        Caverna.Ajeitar(planta);
        HashSet<Vector2Int> buracos = Caverna.AbrirBuracos(planta, buracosNoPrimeiroAndar + (caverna - 1) * buracosAMaisPorAndar, longeDoComeco * 0.7f);
        HashSet<Vector2Int> pocas = Caverna.EspalharPocas(planta, buracos, pocasPorAndar);
        pedreiro.Construir(raiz.transform, planta, buracos, pocas, new System.Random(Random.Range(int.MinValue, int.MaxValue)));

        // Daqui pra frente so interessa onde da pra pisar.
        HashSet<Vector2Int> chaoDaCaverna = new HashSet<Vector2Int>(planta);
        chaoDaCaverna.ExceptWith(buracos);
        MapaDeCaminhos.Atual = new MapaDeCaminhos(chaoDaCaverna, distanciaDosCaminhos);
        // Os baus primeiro: as celulas deles saem do chao, e ninguem nasce dentro de um.
        EspalharArmas(chaoDaCaverna, andar);
        EspalharInimigos(chaoDaCaverna, inimigosNoPrimeiroAndar + (caverna - 1) * inimigosAMaisPorAndar, andar);

        // Sem ninguem pra matar (lista de inimigos vazia, por exemplo), a saida ja nasce aberta, mas no
        // ponto mais longe do comeco: nunca embaixo do jogador.
        if (vivos.Count == 0)
            AbrirSaida(MaisLongeDoComeco(chaoDaCaverna));

        chaoDoAndar = chaoDaCaverna;
    }

    private static Vector2 MaisLongeDoComeco(HashSet<Vector2Int> chaoDaCaverna)
    {
        Vector2Int longe = Vector2Int.zero;

        foreach (Vector2Int c in chaoDaCaverna)
        {
            if (c.sqrMagnitude > longe.sqrMagnitude && CercadaDeChao(chaoDaCaverna, c))
                longe = c;
        }

        return longe;
    }

    private void EspalharInimigos(HashSet<Vector2Int> chaoDaCaverna, int quantos, int andar)
    {
        // So os que ja aparecem neste andar.
        List<InimigoDoAndar> possiveis = new List<InimigoDoAndar>();
        float pesoTotal = 0f;

        if (inimigos != null)
        {
            foreach (InimigoDoAndar inimigo in inimigos)
            {
                if (inimigo != null && inimigo.prefab != null && inimigo.peso > 0f && andar >= inimigo.primeiroAndar)
                {
                    possiveis.Add(inimigo);
                    pesoTotal += inimigo.peso;
                }
            }
        }

        if (possiveis.Count == 0 || quantos <= 0)
        {
            Debug.LogWarning("[Andar] nenhum inimigo pra espalhar: confira a lista Inimigos do objeto Andar", this);
            return;
        }

        // So celulas longe do comeco e com chao em volta (ninguem nasce grudado na parede).
        List<Vector2Int> lugares = new List<Vector2Int>();

        foreach (Vector2Int c in chaoDaCaverna)
        {
            if (((Vector2)c).magnitude >= longeDoComeco && CercadaDeChao(chaoDaCaverna, c))
                lugares.Add(c);
        }

        if (lugares.Count == 0)
            return;

        Transform pai = new GameObject("Inimigos").transform;
        pai.SetParent(raiz.transform, false);
        HashSet<Vector2Int> ocupadas = new HashSet<Vector2Int>();

        for (int tentativa = 0; vivos.Count < quantos && tentativa < quantos * 20; tentativa++)
        {
            // Um grupo em volta de um lugar sorteado.
            Vector2Int centro = lugares[Random.Range(0, lugares.Count)];
            int noGrupo = Random.Range(tamanhoDoGrupo.x, Mathf.Max(tamanhoDoGrupo.x, tamanhoDoGrupo.y) + 1);

            for (int i = 0; i < noGrupo * 6 && noGrupo > 0 && vivos.Count < quantos; i++)
            {
                Vector2Int c = centro + new Vector2Int(Random.Range(-2, 3), Random.Range(-2, 3));

                if (ocupadas.Contains(c) || !chaoDaCaverna.Contains(c) || !CercadaDeChao(chaoDaCaverna, c)
                    || ((Vector2)c).magnitude < longeDoComeco)
                    continue;

                ocupadas.Add(c);
                noGrupo--;

                GameObject novo = Instantiate(Sortear(possiveis, pesoTotal), (Vector2)c, Quaternion.identity, pai);

                if (novo.TryGetComponent(out Vida vida))
                    vivos.Add(vida);
            }
        }
    }

    private static GameObject Sortear(List<InimigoDoAndar> possiveis, float pesoTotal)
    {
        float ponto = Random.value * pesoTotal;

        foreach (InimigoDoAndar inimigo in possiveis)
        {
            ponto -= inimigo.peso;

            if (ponto <= 0f)
                return inimigo.prefab;
        }

        return possiveis[possiveis.Count - 1].prefab;
    }

    // Baus e armas no chao, longe do comeco e longe uns dos outros. No primeiro andar, um bau ja na clareira.
    private void EspalharArmas(HashSet<Vector2Int> chaoDaCaverna, int andar)
    {
        if (armas == null || armas.Length == 0)
            return;

        Transform pai = new GameObject("Armas e baus").transform;
        pai.SetParent(raiz.transform, false);
        List<Vector2Int> lugares = new List<Vector2Int>();

        foreach (Vector2Int c in chaoDaCaverna)
        {
            if (((Vector2)c).magnitude >= 10f && CercadaDeChao(chaoDaCaverna, c))
                lugares.Add(c);
        }

        List<Vector2> usados = new List<Vector2>();

        if (andar == 1 && bauNoComeco)
        {
            Vector2 comeco = new Vector2(-3f, 1f);
            Bau.Criar(quadrosDoBau, comeco, pai, armas, caixaDeMunicao, somDoBau, somDaMunicao);
            chaoDaCaverna.Remove(MapaDeCaminhos.Celula(comeco));
            usados.Add(comeco);
        }

        for (int i = 0; i < bausPorAndar + armasNoChaoPorAndar && lugares.Count > 0; i++)
        {
            Vector2 lugar = Vector2.zero;
            bool achou = false;

            for (int tentativa = 0; tentativa < 40 && !achou; tentativa++)
            {
                lugar = lugares[Random.Range(0, lugares.Count)];
                achou = usados.TrueForAll(u => Vector2.Distance(u, lugar) >= 8f);
            }

            if (!achou)
                continue;

            usados.Add(lugar);

            if (i < bausPorAndar)
            {
                // A celula do bau sai do mapa de caminhos (e o mesmo conjunto): os inimigos contornam.
                Bau.Criar(quadrosDoBau, lugar, pai, armas, caixaDeMunicao, somDoBau, somDaMunicao);
                chaoDaCaverna.Remove(MapaDeCaminhos.Celula(lugar));
            }
            else
            {
                DadosDaArma sorteada = armas[Random.Range(0, armas.Length)];

                if (sorteada != null && sorteada.desenhoNaMao != null)
                    ArmaNoChao.Criar(new ArmaCarregada(sorteada), lugar, pai);
            }
        }
    }

    private static bool CercadaDeChao(HashSet<Vector2Int> chaoDaCaverna, Vector2Int c)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (!chaoDaCaverna.Contains(c + new Vector2Int(dx, dy)))
                    return false;

        return true;
    }

    // ---------------- na tela ----------------
    private void OnGUI()
    {
        // Embaixo da interface (que desenha o resto e o escuro da troca por cima).
        GUI.depth = 10;

        // Nenhum menu aberto e o jogo andando (os menus do jogo antigo sao canvas: o OnGUI fica por cima deles).
        if (raiz != null && escuro <= 0f && chefe == null && Time.timeScale > 0f && !TelaDoJogo.MenuAberto)
            DesenharSeta();
    }

    // Quando sobram poucos e o mais perto esta fora da tela: uma seta na beirada, apontando pra ele.
    private void DesenharSeta()
    {
        if (saida != null || vivos.Count == 0 || vivos.Count > setaQuandoFaltarem || Jogador == null)
            return;

        if (cam == null)
            cam = Camera.main;

        Vida maisPerto = null;
        float menor = float.MaxValue;

        foreach (Vida vida in vivos)
        {
            if (vida == null)
                continue;

            float d = (vida.transform.position - Jogador.position).sqrMagnitude;

            if (d < menor)
            {
                menor = d;
                maisPerto = vida;
            }
        }

        if (cam == null || maisPerto == null)
            return;

        Vector3 naTela = cam.WorldToScreenPoint(maisPerto.transform.position);

        if (naTela.x > 0f && naTela.x < Screen.width && naTela.y > 0f && naTela.y < Screen.height)
            return;

        // Da beirada da tela, na direcao dele (no GUI o y cresce pra baixo).
        Vector2 meio = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 rumo = new Vector2(naTela.x - meio.x, meio.y - naTela.y).normalized;
        float folga = 36f;
        float escala = Mathf.Min((meio.x - folga) / Mathf.Max(0.001f, Mathf.Abs(rumo.x)),
                                 (meio.y - folga) / Mathf.Max(0.001f, Mathf.Abs(rumo.y)));
        Vector2 ponto = meio + rumo * escala;

        if (seta == null)
            seta = DesenharTriangulo(24);

        Matrix4x4 antes = GUI.matrix;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(rumo.y, rumo.x) * Mathf.Rad2Deg, ponto);
        GUI.color = new Color(1f, 0.35f, 0.45f, 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)));
        GUI.DrawTexture(new Rect(ponto.x - 12f, ponto.y - 12f, 24f, 24f), seta);
        GUI.matrix = antes;
    }

    // Um triangulo branco apontando pra direita.
    private static Texture2D DesenharTriangulo(int lado)
    {
        Texture2D textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color32[] pixels = new Color32[lado * lado];

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float meiaAltura = (lado - x) * 0.5f;
                bool dentro = Mathf.Abs(y + 0.5f - lado * 0.5f) <= meiaAltura;
                pixels[y * lado + x] = dentro ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        }

        textura.SetPixels32(pixels);
        textura.Apply();
        return textura;
    }
}

/// <summary>Um inimigo da lista do andar: o prefab, a partir de que andar aparece e quanto sai.</summary>
[System.Serializable]
public class InimigoDoAndar
{
    public GameObject prefab;

    [Tooltip("Aparece deste andar em diante")]
    [Min(1)] public int primeiroAndar = 1;

    [Tooltip("Quanto sai, comparado com os outros (0 = nunca)")]
    [Min(0f)] public float peso = 1f;
}

/// <summary>Um andar da partida: uma caverna (sem chefe) ou a arena de um chefe.</summary>
[System.Serializable]
public class AndarDaPartida
{
    [Tooltip("O nome que aparece ao chegar (opcional), ex.: Covil do Minotauro")]
    public string nome;

    [Tooltip("Vazio = caverna. Com o prefab de um chefe = o andar e a arena dele")]
    public GameObject chefe;
}
