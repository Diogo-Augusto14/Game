using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Monta e troca os andares: cada andar e um punhado de salas ligadas por corredores curtos, ou a
/// arena de um chefe (<see cref="andares"/> diz a ordem).
///
/// A planta sai na hora (<see cref="PlantaDeSalas"/>) e e construida pelo <see cref="Pedreiro"/> com
/// a arte do pacote Old Prison (paredes de tijolo, buracos de abismo, pocas de sangue, ossos), com o
/// jogador na sala do comeco, no centro do mundo. Cada sala de luta (<see cref="SalaDeLuta"/>) guarda
/// os inimigos dela dormindo, em grupos (cada grupo com um bicho principal); entrou, as grades fecham,
/// todos acordam juntos, e so abrem com todos mortos (as vezes depois de mais ondas), com um premio. As salas do lado tem loja, tesouro, altar ou desafio. Quando o
/// sala do fim fica limpa (ou o ultimo inimigo do andar morre), o vortice da saida abre; as salas do lado
/// ficam opcionais, pelos premios. Pisar no vortice escurece a tela e monta o proximo andar, com mais
/// salas e mais inimigos. O andar do chefe e um salao so dele
/// (<see cref="Arena"/>): matou o chefe, o portal abre e cai um bau. Depois do ultimo andar, a partida
/// acaba em vitoria e recomeca.
///
/// Na tela, a <see cref="TelaDoJogo"/> desenha o nome do andar ao chegar, quantos inimigos faltam e o
/// escuro da troca (daqui ela le); aqui fica so a seta na beirada apontando pra sala que falta (ou
/// pro inimigo mais perto), quando sobram poucas. Ao abrir o jogo, mostra o menu inicial (<see cref="TelaDeInicio"/>); a
/// pausa (<see cref="TelaDePausa"/>) fica neste mesmo objeto. Cada andar toca a musica dele.
/// </summary>
[DisallowMultipleComponent]
public class GeradorDoAndar : MonoBehaviour
{
    [Header("Andares")]
    [Tooltip("Os andares da partida, na ordem. Sem chefe: uma caverna. Com chefe: a arena so dele. " +
             "Depois do ultimo, vitoria")]
    [SerializeField] private AndarDaPartida[] andares;

    [Header("Salas")]
    [Tooltip("Salas de luta do comeco ate a do fim, no primeiro andar (contando a do fim)")]
    [SerializeField, Min(1)] private int salasNoCaminho = 3;

    [Tooltip("Mais uma sala no caminho a cada tantos andares de salas")]
    [SerializeField, Min(1)] private int andaresPorSalaAMais = 3;

    [Tooltip("Salas de luta fora do caminho, no primeiro andar (mais uma a cada 4 andares de salas)")]
    [SerializeField, Min(0)] private int lutasDoLado = 1;

    [Tooltip("Metade da largura e da altura da sala do comeco, em celulas")]
    [SerializeField] private Vector2Int clareira = new Vector2Int(5, 4);

    [Header("Inimigos")]
    [Tooltip("Os inimigos que aparecem: cada um a partir de um andar, sorteado pelo peso")]
    [SerializeField] private InimigoDoAndar[] inimigos;

    [Tooltip("Inimigos em cada sala de luta, no primeiro andar (a do fim tem 2 a mais)")]
    [SerializeField] private Vector2Int inimigosPorSala = new Vector2Int(4, 6);

    [Tooltip("Inimigos a mais por sala em cada andar de salas seguinte")]
    [SerializeField, Min(0f)] private float inimigosAMaisPorSala = 0.5f;

    [Tooltip("Chance de uma sala de luta ter uma segunda onda (a do fim sempre tem)")]
    [SerializeField, Range(0f, 1f)] private float chanceDeOnda = 0.3f;

    [Tooltip("Chance a mais de segunda onda em cada andar de salas seguinte")]
    [SerializeField, Range(0f, 1f)] private float chanceDeOndaAMaisPorAndar = 0.05f;

    [Tooltip("Inimigos que saem do chao na segunda onda")]
    [SerializeField] private Vector2Int inimigosPorOnda = new Vector2Int(3, 4);

    [Tooltip("Nenhum buraco fica mais perto do comeco que isto, em unidades")]
    [SerializeField, Min(0f)] private float longeDoComeco = 14f;

    [Tooltip("Ate quantos passos do jogador os inimigos acham caminho contornando paredes")]
    [SerializeField, Min(1)] private int distanciaDosCaminhos = 40;

    [Tooltip("Com esta quantidade de salas (ou inimigos soltos) ou menos, uma seta aponta pra mais perto")]
    [SerializeField, Min(0)] private int setaQuandoFaltarem = 3;

    [Header("Armas")]
    [Tooltip("As armas que saem dos baus e aparecem no chao (sorteadas pelo peso de cada uma)")]
    [SerializeField] private DadosDaArma[] armas;

    [SerializeField, Min(0)] private int bausPorAndar = 3;

    [Tooltip("Armas largadas no chao das salas, por andar")]
    [SerializeField, Min(0)] private int armasNoChaoPorAndar = 1;

    [Tooltip("No primeiro andar, um bau ja na clareira do comeco")]
    [SerializeField] private bool bauNoComeco;

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

    [SerializeField, Min(0)] private int buracosAMaisPorAndar = 2;

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
        atual = this;
        audioSource = GetComponent<AudioSource>();

        if (!TryGetComponent(out TelaDePausa _))
            gameObject.AddComponent<TelaDePausa>();
        pedreiro = new Pedreiro(chao, paredes, abismo, sangue, enfeites, chanceDeEnfeite);
        quadrosDoVortice = FolhaDeSprites.Cortar(vortice, quadroDoVortice, pixelsPorUnidade);
        quadrosDoBau = FolhaDeSprites.Cortar(bau, quadroDoBau, DadosDoOldPrison.Lado);
    }

    private void OnDestroy()
    {
        if (atual == this)
            atual = null;
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

        // O heroi escolhido (vida, velocidade, boneco, arma e habilidade): no "tentar de novo" ja e ele.
        if (jogador != null)
            Herois.Aplicar(jogador, Herois.Atual);

        GerarSemTravar(1);

        // Na primeira vez, o menu inicial por cima do andar 1; "tentar de novo" vem direto pro jogo.
        if (!TelaDeInicio.JaPassou)
            TelaDeInicio.Mostrar(this);
        else
        {
            Musica.Tocar(MusicaDoAndar);
            AvisoDoAndar.Mostrar(nome);
            Salvamento.Salvar(this, jogador);
        }
    }

    /// <summary>A musica deste andar: a do chefe (a do final, no ultimo) ou a da caverna.</summary>
    public TemaMusical MusicaDoAndar =>
        chefe != null ? (Andar >= Andares ? TemaMusical.ChefeFinal : TemaMusical.Chefe) : Musica.DaCaverna(Mundo);

    /// <summary>
    /// Em que mundo o andar esta: cada chefe fecha um mundo (o Mundo 1 vai ate o primeiro chefe).
    /// A musica das cavernas muda por mundo.
    /// </summary>
    public int Mundo
    {
        get
        {
            int mundo = 1;

            for (int i = 0; andares != null && i < Andar - 1 && i < andares.Length; i++)
            {
                if (andares[i] != null && andares[i].chefe != null)
                    mundo++;
            }

            return mundo;
        }
    }

    private void Update()
    {
        if (MapaDeCaminhos.Atual != null && Jogador != null)
            MapaDeCaminhos.Atual.Atualizar(Jogador.position);

        // Continua contando depois do portal aberto: a emboscada pode trazer mais gente.
        if (raiz == null || chaoDoAndar == null)
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
                bool eraOChefe = chefe != null && vida.gameObject == chefe.gameObject;
                Registro.Derrotou(vida.gameObject, eraOChefe);
                AoMatarInimigo?.Invoke(ondeMorreuOUltimo);

                // Moeda, chave, bomba ou coracao (o Amuleto da Sorte ajuda).
                float sorte = Jogador != null && Jogador.TryGetComponent(out EstatisticasDoJogador itens) ? itens.Sorte : 1f;
                Coletavel.SoltarDoInimigo(ondeMorreuOUltimo, eraOChefe, sorte);

                if (eraOChefe)
                {
                    ResumoDaPartida.ContarChefe();
                    Progresso.VencerMundo(Mundo);
                }

                if (caixaDeMunicao != null && Random.value < chanceDeMunicao)
                    CaixaDeMunicao.Criar(caixaDeMunicao, ondeMorreuOUltimo, raiz.transform, somDaMunicao);
            }

            vivos.RemoveAt(i);
        }

        // Andar limpo (todas as salas, inclusive as do lado, que sao opcionais): a Carne Assada cura.
        if (vivos.Count == 0 && !avisouLimpo)
        {
            avisouLimpo = true;
            AoLimparAndar?.Invoke();
        }

        if (vivos.Count == 0 && saida == null)
        {
            AbrirSaida(ondeMorreuOUltimo);

            // O premio do chefe: um bau no meio do salao (um pouco abaixo de onde o portal costuma abrir).
            if (chefe != null && bauDepoisDoChefe && quadrosDoBau.Length > 0)
            {
                Vector2 meio = Arena.OndeOChefeFica(raioDaArena) - new Vector2Int(0, 4);

                if (Vector2.Distance(meio, ondeMorreuOUltimo) < 2f)
                    meio += Vector2.left * 3f;

                Bau.Criar(quadrosDoBau, meio, raiz.transform, armas, caixaDeMunicao, somDoBau, somDaMunicao);

                // E um item de premio, num pedestal do lado do bau.
                Pedestal.Criar(meio + Vector2.right * 2.5f, raiz.transform,
                               CatalogoDeItens.Sortear(Jogador != null ? Jogador.GetComponent<EstatisticasDoJogador>() : null));
            }
        }
    }

    private void AbrirSaida(Vector2 onde)
    {
        saida = Saida.Criar(raiz.transform, onde, quadrosDoVortice, 12f, this);

        if (audioSource != null && portalAbrindo != null)
            audioSource.PlayOneShot(portalAbrindo, volume);
    }

    /// <summary>
    /// Uma sala de luta acabou de ficar limpa. A do fim ja abre o portal, mesmo com salas do lado
    /// faltando: quem quiser volta e limpa elas pelos premios.
    /// </summary>
    public void SalaLimpa(SalaDeLuta sala, bool doFim)
    {
        if (doFim && saida == null && raiz != null && !trocando && sala != null)
            AbrirSaida(sala.LugarDoPortal());
    }

    // ---------------- trocar de andar ----------------
    /// <summary>Mostra o nome do andar de novo (ao sair do menu inicial, que escondia ele).</summary>
    public void MostrarNome()
    {
        nomeAte = Time.unscaledTime + tempoDoNome;
        AvisoDoAndar.Mostrar(nome);
    }

    /// <summary>
    /// Mais um inimigo que conta pro andar (os que nascem no meio: esqueleto levantado pelo
    /// Necromante, bolinha da Bolha que estourou). Ganha o contorno claro.
    /// </summary>
    public static void Registrar(Vida inimigo)
    {
        if (atual == null || inimigo == null || atual.vivos.Contains(inimigo))
            return;

        atual.vivos.Add(inimigo);
        ContornoClaro.Colocar(inimigo.gameObject, CorDoContorno);
        SalaDeLuta.Adotar(inimigo);
    }

    /// <summary>Onde os inimigos que nascem no meio vao morar (somem junto com o andar).</summary>
    public static Transform Raiz => atual != null && atual.raiz != null ? atual.raiz.transform : null;

    private static GeradorDoAndar atual;

    /// <summary>Morreu um inimigo do andar (o vampiro e o item ativo contam), com o lugar.</summary>
    public static event System.Action<Vector2> AoMatarInimigo;

    /// <summary>O andar ficou sem inimigos (a Carne Assada cura).</summary>
    public static event System.Action AoLimparAndar;

    /// <summary>Um andar novo foi montado (o Escudo Sagrado volta).</summary>
    public static event System.Action AoComecarAndar;

    private static readonly List<Vida> nenhum = new List<Vida>();

    /// <summary>Os inimigos vivos do andar (a Bussola Maldita persegue, os Cristais do Trovao acertam).</summary>
    public static IReadOnlyList<Vida> Vivos => atual != null ? atual.vivos : nenhum;

    /// <summary>As armas que saem dos baus (os baus do recheio e das emboscadas usam).</summary>
    public DadosDaArma[] ArmasDoBau => armas;

    private bool avisouLimpo;

    // Os mundos: o Porao (o Old Prison recolorido, com a musica do jogo antigo), as Catacumbas (o Old
    // Prison original), a Cripta (pacote Crypt) e as Profundezas (pacote The Depths of the Mountain).
    private static readonly string[] NomesDosMundos = { "Porão", "Catacumbas", "Cripta", "Profundezas" };
    private readonly Dictionary<int, Pedreiro> pedreiros = new Dictionary<int, Pedreiro>();
    private Color? fundoDaCena;

    /// <summary>O nome do mundo do andar (Porao, Catacumbas, Cripta, Profundezas).</summary>
    public string NomeDoMundo => NomesDosMundos[Mathf.Clamp(Mundo - 1, 0, NomesDosMundos.Length - 1)];

    // Quem constroi o andar no jeito do mundo (sem a arte do mundo, o Old Prison original).
    private Pedreiro PedreiroDoMundo()
    {
        int i = Mathf.Clamp(Mundo - 1, 0, NomesDosMundos.Length - 1);

        if (!pedreiros.TryGetValue(i, out Pedreiro feito))
        {
            EstiloDeLadrilhos estilo = null;

            if (i == 0)
            {
                Texture2D c = Resources.Load<Texture2D>("Temas/Porao/Chao");
                Texture2D p = Resources.Load<Texture2D>("Temas/Porao/Paredes");

                if (c != null && p != null)
                    estilo = EstiloDeLadrilhos.OldPrison(c, p);
            }
            else if (i == 2)
                estilo = EstiloDeLadrilhos.Cripta();
            else if (i == 3)
                estilo = EstiloDeLadrilhos.Profundezas();

            feito = estilo != null ? new Pedreiro(estilo, abismo, sangue, enfeites, chanceDeEnfeite) : pedreiro;
            pedreiros[i] = feito;
        }

        // O fundo da camera acompanha o mundo (o vazio das Profundezas nao tem emenda).
        if (cam == null)
            cam = Camera.main;

        if (cam != null)
        {
            if (fundoDaCena == null)
                fundoDaCena = cam.backgroundColor;

            cam.backgroundColor = feito.Estilo.corDoFundo ?? fundoDaCena.Value;
        }

        return feito;
    }

    /// <summary>
    /// Um inimigo deste andar, sorteado da lista, que sai do chao ja acordado e conta pro andar (as
    /// ondas da emboscada e do desafio).
    /// </summary>
    public Vida CriarInimigo(Vector2 onde)
    {
        List<InimigoDoAndar> possiveis = new List<InimigoDoAndar>();
        float pesoTotal = 0f;

        foreach (InimigoDoAndar inimigo in inimigos)
        {
            if (inimigo != null && inimigo.prefab != null && inimigo.peso > 0f && Andar >= inimigo.primeiroAndar
                && (inimigo.ultimoAndar <= 0 || Andar <= inimigo.ultimoAndar))
            {
                possiveis.Add(inimigo);
                pesoTotal += inimigo.peso;
            }
        }

        if (possiveis.Count == 0 || raiz == null)
            return null;

        GameObject novo = Instantiate(Sortear(possiveis, pesoTotal), onde, Quaternion.identity, raiz.transform);

        if (!novo.TryGetComponent(out Vida vida))
            return null;

        Registrar(vida);
        avisouLimpo = false;

        if (novo.TryGetComponent(out InimigoAtirador atirador))
        {
            atirador.Acordar();
            novo.AddComponent<SaindoDoChao>().Comecar(0.5f);
        }

        return vida;
    }

    /// <summary>A cor do contorno claro em volta dos inimigos (a mesma do jogo antigo).</summary>
    public static readonly Color CorDoContorno = new Color(1f, 0.95f, 0.85f, 0.6f);

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
            Registro.Venceu();
            Progresso.Zerar(Herois.Atual.Nome);
            Salvamento.Apagar();
            TelaDeFimDeJogo.MostrarVitoria(this);
            yield break;
        }

        yield return IrPara(Andar + 1);
        trocando = false;
    }

    /// <summary>O "Continuar" do menu: escurece, monta o andar salvo e poe o jogador no comeco dele.</summary>
    public void Continuar(int andar)
    {
        if (!trocando)
            StartCoroutine(ContinuarEm(andar));
    }

    private IEnumerator ContinuarEm(int andar)
    {
        trocando = true;
        escuro = 1f;
        yield return IrPara(andar);
        trocando = false;
    }

    /// <summary>A arma com este nome (de arquivo): uma das que caem no andar ou a de um heroi.</summary>
    public DadosDaArma ArmaPeloNome(string nomeDoArquivo)
    {
        if (string.IsNullOrEmpty(nomeDoArquivo))
            return null;

        if (armas != null)
        {
            foreach (DadosDaArma arma in armas)
            {
                if (arma != null && arma.name == nomeDoArquivo)
                    return arma;
            }
        }

        return Resources.Load<DadosDaArma>("ArmasDosHerois/" + nomeDoArquivo);
    }

    // Monta o andar, toca a musica, mostra o nome, volta o jogador pro comeco, salva e clareia.
    private IEnumerator IrPara(int andar)
    {
        GerarSemTravar(andar);
        Musica.Tocar(MusicaDoAndar);
        AvisoDoAndar.Mostrar(nome);
        Registro.ChegouNoAndar(andar);

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

        // O comeco de cada andar fica salvo (o "Continuar" do menu volta pra ca).
        Salvamento.Salvar(this, Jogador != null ? Jogador.gameObject : null);
        yield return Escurecer(1f, 0f);
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
        GameObject chefeDoAndar = esse != null ? esse.Sortear() : null;
        Andar = andar;

        // O nome: o do andar, ou o lugar do chefe sorteado ("Covil do Minotauro").
        string titulo = esse != null ? esse.nome : null;

        if (string.IsNullOrEmpty(titulo) && chefeDoAndar != null && chefeDoAndar.TryGetComponent(out Chefe dono))
            titulo = dono.Lugar;

        if (string.IsNullOrEmpty(titulo))
            titulo = NomeDoMundo;

        nome = !string.IsNullOrEmpty(titulo) ? $"Andar {andar} de {Andares}: {titulo}" : $"Andar {andar} de {Andares}";
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

        avisouLimpo = false;

        if (chefeDoAndar != null)
            GerarArena(chefeDoAndar);
        else
            GerarCaverna(andar, CavernasAte(andar));

        AoComecarAndar?.Invoke();
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
        Pedreiro construtor = PedreiroDoMundo();
        HashSet<Vector2Int> pocas = construtor.Estilo.temSangue ? Caverna.EspalharPocas(planta, semBuracos, pocasPorAndar / 2) : semBuracos;
        construtor.Construir(raiz.transform, planta, semBuracos, pocas, new System.Random(Random.Range(int.MinValue, int.MaxValue)));
        MapaDeCaminhos.Atual = new MapaDeCaminhos(planta, distanciaDosCaminhos);

        GameObject novo = Instantiate(prefabDoChefe, (Vector2)Arena.OndeOChefeFica(raioDaArena), Quaternion.identity, raiz.transform);
        chefe = novo.GetComponent<Chefe>();
        ContornoClaro.Colocar(novo, CorDoContorno);

        if (novo.TryGetComponent(out Vida vida))
            vivos.Add(vida);
        else
            AbrirSaida(Arena.OndeOChefeFica(raioDaArena));

        chaoDoAndar = planta;
    }

    private void GerarCaverna(int andar, int caverna)
    {
        PlantaDeSalas.Planta salas = PlantaDeSalas.Montar(salasNoCaminho + (caverna - 1) / Mathf.Max(1, andaresPorSalaAMais),
                                                          lutasDoLado + (caverna - 1) / 4, SortearEspeciais(), clareira);
        HashSet<Vector2Int> planta = salas.chao;

        // Onde nada se espalha: corredores, a volta das portas e as salas sem luta.
        HashSet<Vector2Int> proibido = new HashSet<Vector2Int>(salas.corredores);

        foreach (Vector2Int porta in salas.portas)
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    proibido.Add(porta + new Vector2Int(dx, dy));

        foreach (SalaDaPlanta sala in salas.salas)
        {
            if (!sala.DeLuta)
                proibido.UnionWith(sala.celulas);
        }

        Pedreiro construtor = PedreiroDoMundo();
        int quantosBuracos = construtor.Estilo.temBuracos ? buracosNoPrimeiroAndar + (caverna - 1) * buracosAMaisPorAndar : 0;
        HashSet<Vector2Int> buracos = Caverna.AbrirBuracos(planta, quantosBuracos, longeDoComeco * 0.7f, proibido);
        HashSet<Vector2Int> pocas = construtor.Estilo.temSangue
            ? Caverna.EspalharPocas(planta, buracos, pocasPorAndar)
            : new HashSet<Vector2Int>();
        construtor.Construir(raiz.transform, planta, buracos, pocas, new System.Random(Random.Range(int.MinValue, int.MaxValue)));

        // Daqui pra frente so interessa onde da pra pisar.
        HashSet<Vector2Int> chaoDaCaverna = new HashSet<Vector2Int>(planta);
        chaoDaCaverna.ExceptWith(buracos);
        MapaDeCaminhos.Atual = new MapaDeCaminhos(chaoDaCaverna, distanciaDosCaminhos);

        // As salas especiais, os baus e o resto primeiro: as celulas deles saem do chao, e ninguem nasce dentro.
        foreach (SalaDaPlanta sala in salas.salas)
        {
            if (!sala.DeLuta && sala.tipo != TipoDeSala.Comeco)
                RecheioDaCaverna.EncherEspecial(sala, this, chaoDaCaverna, raiz.transform, quadrosDoBau);
        }

        EspalharArmas(chaoDaCaverna, proibido, andar);
        RecheioDaCaverna.Espalhar(this, chaoDaCaverna, proibido, raiz.transform, andar, Mundo, quadrosDoBau);
        Decoracao.Espalhar(Mundo, construtor.Estilo, salas, chaoDaCaverna, raiz.transform);
        EspalharInimigos(salas, chaoDaCaverna, andar, caverna);

        // Sem ninguem pra matar (lista de inimigos vazia, por exemplo), a saida ja nasce aberta, mas no
        // ponto mais longe do comeco: nunca embaixo do jogador.
        if (vivos.Count == 0)
            AbrirSaida(MaisLongeDoComeco(chaoDaCaverna));

        chaoDoAndar = chaoDaCaverna;
    }

    // As salas especiais do andar (cada uma sorteada).
    private static List<TipoDeSala> SortearEspeciais()
    {
        List<TipoDeSala> especiais = new List<TipoDeSala>();

        if (Random.value < 0.65f)
            especiais.Add(TipoDeSala.Loja);

        if (Random.value < 0.5f)
            especiais.Add(TipoDeSala.Tesouro);

        if (Random.value < 0.25f)
            especiais.Add(TipoDeSala.Altar);

        if (Random.value < 0.25f)
            especiais.Add(TipoDeSala.Desafio);

        return especiais;
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

    // Cada sala de luta com os inimigos dela (dormindo ate o jogador entrar) e, as vezes, uma segunda onda.
    private void EspalharInimigos(PlantaDeSalas.Planta salas, HashSet<Vector2Int> chaoDaCaverna, int andar, int caverna)
    {
        // So os que ja aparecem neste andar.
        List<InimigoDoAndar> possiveis = new List<InimigoDoAndar>();
        float pesoTotal = 0f;

        if (inimigos != null)
        {
            foreach (InimigoDoAndar inimigo in inimigos)
            {
                if (inimigo != null && inimigo.prefab != null && inimigo.peso > 0f && andar >= inimigo.primeiroAndar
                    && (inimigo.ultimoAndar <= 0 || andar <= inimigo.ultimoAndar))
                {
                    possiveis.Add(inimigo);
                    pesoTotal += inimigo.peso;
                }
            }
        }

        if (possiveis.Count == 0)
            Debug.LogWarning("[Andar] nenhum inimigo pra espalhar: confira a lista Inimigos do objeto Andar", this);

        Transform pai = new GameObject("Salas de luta").transform;
        pai.SetParent(raiz.transform, false);
        int aMais = Mathf.FloorToInt((caverna - 1) * inimigosAMaisPorSala);

        foreach (SalaDaPlanta sala in salas.salas)
        {
            if (!sala.DeLuta)
                continue;

            bool fim = sala.tipo == TipoDeSala.Fim;
            int quantos = Random.Range(inimigosPorSala.x, Mathf.Max(inimigosPorSala.x, inimigosPorSala.y) + 1) + aMais + (fim ? 2 : 0);
            int porOnda = Random.Range(inimigosPorOnda.x, Mathf.Max(inimigosPorOnda.x, inimigosPorOnda.y) + 1) + aMais / 2;

            // Ondas a mais: as vezes uma; a sala do fim sempre uma, e duas do quarto andar de salas em diante.
            int ondas = fim ? (caverna >= 4 ? 2 : 1) : (Random.value < chanceDeOnda + (caverna - 1) * chanceDeOndaAMaisPorAndar ? 1 : 0);

            if (possiveis.Count == 0)
            {
                quantos = 0;
                ondas = 0;
            }

            SalaDeLuta luta = SalaDeLuta.Criar(sala, pai, this, chaoDaCaverna, ondas, porOnda, quadrosDoBau);

            // Lugares dentro da sala, longe das portas, com chao em volta e um pouco separados.
            List<Vector2Int> lugares = new List<Vector2Int>();

            foreach (Vector2Int c in sala.celulas)
            {
                if (chaoDaCaverna.Contains(c) && sala.BemDentro(c, 2f) && CercadaDeChao(chaoDaCaverna, c))
                    lugares.Add(c);
            }

            if (quantos <= 0 || lugares.Count == 0)
                continue;

            // Em grupos (um nas salas pequenas, dois nas cheias), longe um do outro. Cada grupo tem um
            // bicho principal e, as vezes, um ou outro diferente no meio: da pra ler a luta de longe.
            int grupos = quantos >= 6 && lugares.Count >= 40 ? 2 : 1;
            List<Vector2Int> centros = new List<Vector2Int>();
            List<GameObject> principais = new List<GameObject>();

            for (int tentativa = 0; centros.Count < grupos && tentativa < 60; tentativa++)
            {
                Vector2Int c = lugares[Random.Range(0, lugares.Count)];

                if (centros.TrueForAll(u => (u - c).sqrMagnitude >= 36))
                {
                    centros.Add(c);
                    principais.Add(Sortear(possiveis, pesoTotal));
                }
            }

            List<Vector2Int> usados = new List<Vector2Int>();

            for (int tentativa = 0; usados.Count < quantos && tentativa < quantos * 40; tentativa++)
            {
                int grupo = usados.Count % centros.Count;
                Vector2Int c = lugares[Random.Range(0, lugares.Count)];

                // Perto do centro do grupo (as ultimas tentativas aceitam qualquer lugar da sala).
                if (tentativa < quantos * 30 && (c - centros[grupo]).sqrMagnitude > 9)
                    continue;

                if (!usados.TrueForAll(u => (u - c).sqrMagnitude >= 2))
                    continue;

                usados.Add(c);
                GameObject qual = Random.value < 0.3f ? Sortear(possiveis, pesoTotal) : principais[grupo];
                GameObject novo = Instantiate(qual, (Vector2)c, Quaternion.identity, luta.transform);

                if (novo.TryGetComponent(out Vida vida))
                {
                    vivos.Add(vida);
                    luta.Adicionar(vida);
                }

                ContornoClaro.Colocar(novo, CorDoContorno);
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

    // Baus e armas no chao das salas de luta, longe uns dos outros. No primeiro andar, um bau ja na sala do comeco.
    private void EspalharArmas(HashSet<Vector2Int> chaoDaCaverna, HashSet<Vector2Int> proibido, int andar)
    {
        if (armas == null || armas.Length == 0)
            return;

        Transform pai = new GameObject("Armas e baus").transform;
        pai.SetParent(raiz.transform, false);
        List<Vector2Int> lugares = new List<Vector2Int>();

        foreach (Vector2Int c in chaoDaCaverna)
        {
            if (((Vector2)c).magnitude >= 10f && CercadaDeChao(chaoDaCaverna, c) && !proibido.Contains(c))
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

    // Quando sobram poucas salas (ou poucos inimigos soltos) e a mais perto esta fora da tela: uma seta
    // na beirada, apontando pra ela. Nunca no meio de uma luta.
    private void DesenharSeta()
    {
        if (saida != null || Jogador == null || SalaDeLuta.Fechada != null)
            return;

        if (cam == null)
            cam = Camera.main;

        Vector2 alvo = Vector2.zero;
        float menor = float.MaxValue;
        int salasQueFaltam = 0;

        foreach (SalaDeLuta sala in SalaDeLuta.Todas)
        {
            if (sala == null || sala.Limpa)
                continue;

            salasQueFaltam++;
            float d = (sala.Meio - (Vector2)Jogador.position).sqrMagnitude;

            if (d < menor)
            {
                menor = d;
                alvo = sala.Meio;
            }
        }

        if (salasQueFaltam > setaQuandoFaltarem)
            return;

        // Sem sala faltando: algum inimigo solto (quem fugiu de uma sala, quem nasceu no meio).
        if (salasQueFaltam == 0)
        {
            if (vivos.Count == 0 || vivos.Count > setaQuandoFaltarem)
                return;

            foreach (Vida vida in vivos)
            {
                if (vida == null)
                    continue;

                float d = (vida.transform.position - Jogador.position).sqrMagnitude;

                if (d < menor)
                {
                    menor = d;
                    alvo = vida.transform.position;
                }
            }
        }

        if (cam == null || menor == float.MaxValue)
            return;

        Vector3 naTela = cam.WorldToScreenPoint(alvo);

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

    [Tooltip("Ate este andar (0 = ate o fim): cada mundo tem os seus bichos")]
    [Min(0)] public int ultimoAndar;

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

    [Tooltip("Outros chefes que podem sair no lugar dele (sorteado a cada partida)")]
    public GameObject[] ouEntao;

    /// <summary>O chefe deste andar nesta partida (nulo = caverna).</summary>
    public GameObject Sortear()
    {
        if (chefe == null)
            return null;

        int quantos = 1;

        if (ouEntao != null)
        {
            foreach (GameObject outro in ouEntao)
            {
                if (outro != null)
                    quantos++;
            }
        }

        int sorteado = Random.Range(0, quantos);

        if (sorteado == 0)
            return chefe;

        foreach (GameObject outro in ouEntao)
        {
            if (outro != null && --sorteado == 0)
                return outro;
        }

        return chefe;
    }
}
