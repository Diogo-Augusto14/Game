using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Os herois que da pra escolher no menu inicial (<see cref="TelaDeInicio"/>), os nove do Tiny RPG
/// (vieram do jogo antigo). Cada um tem vida, velocidade, as folhas de animacao, a arma do comeco
/// (infinita, e que tambem pode ser trocada) e uma habilidade (<see cref="HabilidadeDoHeroi"/>, tecla F).
///
/// O Arqueiro comeca livre; os outros se liberam vencendo chefes e zerando o jogo (<see cref="Progresso"/>).
/// <see cref="Aplicar"/> poe o heroi no jogador. A escolha fica salva entre partidas.
/// </summary>
public static class Herois
{
    private const string ChaveDoEscolhido = "heroi-escolhido";

    public class Heroi
    {
        public string Nome;
        public string Descricao;

        /// <summary>A pasta das folhas dentro de Resources (Idle.png, Walk.png, Attack01.png, Death.png).</summary>
        public string Pasta;

        /// <summary>Vida maxima (2 = um coracao).</summary>
        public int Vida = 6;

        public float Velocidade = 5.5f;

        /// <summary>A arma do comeco, em Resources/ArmasDosHerois.</summary>
        public string Arma;

        /// <summary>A folha do ataque que toca a cada tiro, e o quadro em que o tiro sai.</summary>
        public string FolhaDoAtaque = "Attack01";
        public int QuadroDoDisparo = 3;

        /// <summary>Pixels por unidade das folhas (quadros de 100 x 100). Os do Tiny RPG sao 20; o Mineiro, 40.</summary>
        public float Pixels = 20f;

        public TipoDeHabilidade Habilidade;
        public string NomeDaHabilidade;
        public float Recarga = 10f;

        /// <summary>Vencer o chefe deste mundo libera (0 = nao e por chefe).</summary>
        public int LiberaNoMundo;

        /// <summary>Zerar o jogo com este heroi libera (null = nao e por ele).</summary>
        public string LiberaZerandoCom;

        /// <summary>Zerar o jogo esta quantidade de vezes, com qualquer um, libera (0 = nao e por isso).</summary>
        public int LiberaComVitorias;

        public bool Livre => LiberaNoMundo <= 0 && LiberaZerandoCom == null && LiberaComVitorias <= 0;

        /// <summary>O que falta fazer, pro menu mostrar no heroi bloqueado.</summary>
        public string Requisito =>
            LiberaNoMundo > 0 ? Idioma.F("Vença o chefe do mundo {0}", LiberaNoMundo)
            : LiberaZerandoCom != null ? Idioma.F("Zere o jogo com o {0}", Idioma.T(LiberaZerandoCom))
            : LiberaComVitorias > 1 ? Idioma.F("Zere o jogo {0} vezes", LiberaComVitorias)
            : LiberaComVitorias == 1 ? Idioma.T("Zere o jogo")
            : "";

        /// <summary>A linha dos numeros no menu.</summary>
        public string Numeros
        {
            get
            {
                string coracoes = Vida % 2 == 0 ? $"{Vida / 2}" : $"{Vida / 2}{(Idioma.Ingles ? "." : ",")}5";
                DadosDaArma arma = Herois.Arma(this);
                return Idioma.F("Vida: {0} corações     Arma: {1}     F: {2}", coracoes, arma != null ? Idioma.T(arma.nome) : "?",
                                Idioma.T(NomeDaHabilidade));
            }
        }
    }

    public static readonly Heroi[] Todos =
    {
        // O heroi principal (1.21.0, feito no PixelLab): um cavaleiro que perdeu tudo e virou mineiro.
        new Heroi
        {
            Nome = "Mineiro", Pasta = "Personagens/Herois/Mineiro", Pixels = 40f,
            Descricao = "Um cavaleiro que perdeu tudo e virou mineiro. Arremessa picaretas que giram.",
            Vida = 8, Velocidade = 5.2f, Arma = "PicaretaDoMineiro", QuadroDoDisparo = 3,
            Habilidade = TipoDeHabilidade.Furia, NomeDaHabilidade = "Fúria", Recarga = 16f,
        },
        new Heroi
        {
            Nome = "Arqueiro", Pasta = "Personagens/Herois/Arqueiro",
            Descricao = "Atira flechas sem parar com o arco que nunca acaba.",
            Vida = 6, Velocidade = 5.5f, Arma = "ArcoDoArqueiro", QuadroDoDisparo = 5,
            Habilidade = TipoDeHabilidade.ChuvaDeFlechas, NomeDaHabilidade = "Chuva de flechas", Recarga = 10f,
        },
        new Heroi
        {
            Nome = "Soldado", Pasta = "Personagens/Herois/Soldado", LiberaNoMundo = 1,
            Descricao = "Aguenta mais pancada; flechas mais fortes e mais lentas.",
            Vida = 8, Velocidade = 5.2f, Arma = "ArcoDoSoldado", FolhaDoAtaque = "Attack03", QuadroDoDisparo = 6,
            Habilidade = TipoDeHabilidade.Rajada, NomeDaHabilidade = "Rajada de flechas", Recarga = 8f,
        },
        new Heroi
        {
            Nome = "Lanceiro", Pasta = "Personagens/Herois/Lanceiro", LiberaNoMundo = 2,
            Descricao = "A cavalo: o mais rápido. Lanças que atravessam tudo.",
            Vida = 8, Velocidade = 6.3f, Arma = "LancaDoLanceiro", QuadroDoDisparo = 3,
            Habilidade = TipoDeHabilidade.Investida, NomeDaHabilidade = "Investida", Recarga = 6f,
        },
        new Heroi
        {
            Nome = "Cavaleiro", Pasta = "Personagens/Herois/Cavaleiro", LiberaNoMundo = 3,
            Descricao = "Muita vida. Onda de corte forte, mas curta.",
            Vida = 10, Velocidade = 4.4f, Arma = "EspadaDoCavaleiro", QuadroDoDisparo = 3,
            Habilidade = TipoDeHabilidade.Escudo, NomeDaHabilidade = "Escudo", Recarga = 14f,
        },
        new Heroi
        {
            Nome = "Mago", Pasta = "Personagens/Herois/Mago", LiberaComVitorias = 1,
            Descricao = "Pouca vida, mas bolas de fogo fortes.",
            Vida = 5, Velocidade = 5.2f, Arma = "CajadoDoMago", QuadroDoDisparo = 3,
            Habilidade = TipoDeHabilidade.Meteoro, NomeDaHabilidade = "Meteoro", Recarga = 12f,
        },
        new Heroi
        {
            Nome = "Espadachim", Pasta = "Personagens/Herois/Espadachim", LiberaZerandoCom = "Cavaleiro",
            Descricao = "Rápido e com cortes rápidos, mas pouca vida.",
            Vida = 6, Velocidade = 6f, Arma = "SabreDoEspadachim", QuadroDoDisparo = 3,
            Habilidade = TipoDeHabilidade.Redemoinho, NomeDaHabilidade = "Redemoinho", Recarga = 10f,
        },
        new Heroi
        {
            Nome = "Machadeiro", Pasta = "Personagens/Herois/Machadeiro", LiberaZerandoCom = "Arqueiro",
            Descricao = "O golpe mais forte do jogo, bem devagar.",
            Vida = 8, Velocidade = 4.4f, Arma = "MachadoDoMachadeiro", QuadroDoDisparo = 4,
            Habilidade = TipoDeHabilidade.Furia, NomeDaHabilidade = "Fúria", Recarga = 18f,
        },
        new Heroi
        {
            Nome = "Padre", Pasta = "Personagens/Herois/Padre", LiberaZerandoCom = "Mago",
            Descricao = "Tiro fraco, mas cura um coração com a reza.",
            Vida = 6, Velocidade = 5.2f, Arma = "CetroDoPadre", QuadroDoDisparo = 4,
            Habilidade = TipoDeHabilidade.Cura, NomeDaHabilidade = "Reza", Recarga = 25f,
        },
        new Heroi
        {
            Nome = "Templário", Pasta = "Personagens/Herois/Templario", LiberaComVitorias = 3,
            Descricao = "O que mais aguenta, e é o mais lento.",
            Vida = 12, Velocidade = 4f, Arma = "EspadaSagrada", QuadroDoDisparo = 4,
            Habilidade = TipoDeHabilidade.AnelDeCortes, NomeDaHabilidade = "Anel sagrado", Recarga = 10f,
        },
    };

    private static int escolhido = -1;
    private static readonly Dictionary<string, Sprite[]> folhas = new Dictionary<string, Sprite[]>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar() => escolhido = -1;

    public static int Escolhido
    {
        get
        {
            if (escolhido < 0)
                escolhido = Mathf.Clamp(PlayerPrefs.GetInt(ChaveDoEscolhido, 0), 0, Todos.Length - 1);

            return escolhido;
        }
    }

    public static Heroi Atual => Todos[Escolhido];

    /// <summary>O heroi pelo nome (o jogo salvo guarda o nome). Nulo se nao existe.</summary>
    public static Heroi PeloNome(string nome) => System.Array.Find(Todos, h => h.Nome == nome);

    /// <summary>Escolhe o heroi pelo numero (da a volta nas pontas).</summary>
    public static void Escolher(int indice)
    {
        escolhido = ((indice % Todos.Length) + Todos.Length) % Todos.Length;
        PlayerPrefs.SetInt(ChaveDoEscolhido, escolhido);
        PlayerPrefs.Save();
    }

    /// <summary>Ja pode ser jogado: livre ou liberado no <see cref="Progresso"/>.</summary>
    public static bool Liberado(Heroi heroi)
    {
        if (heroi == null)
            return false;

        if (heroi.Livre)
            return true;

        return (heroi.LiberaNoMundo > 0 && Progresso.VenceuOMundo(heroi.LiberaNoMundo))
               || (heroi.LiberaZerandoCom != null && Progresso.ZerouCom(heroi.LiberaZerandoCom))
               || (heroi.LiberaComVitorias > 0 && Progresso.Vitorias >= heroi.LiberaComVitorias);
    }

    /// <summary>A arma do comeco do heroi.</summary>
    public static DadosDaArma Arma(Heroi heroi) =>
        heroi != null && !string.IsNullOrEmpty(heroi.Arma) ? Resources.Load<DadosDaArma>("ArmasDosHerois/" + heroi.Arma) : null;

    /// <summary>Os quadros do heroi parado (o retrato).</summary>
    public static Sprite[] Parado(Heroi heroi) => Folha(heroi, "Idle");

    /// <summary>Os quadros do heroi caindo (a tela do fim, quando morre).</summary>
    public static Sprite[] Morte(Heroi heroi) => Folha(heroi, "Death");

    /// <summary>
    /// Poe o heroi no jogador: vida cheia, velocidade, as folhas de animacao, a arma do comeco (so ela
    /// na mao) e a habilidade. Chamado ao comecar a partida e ao trocar de heroi no menu.
    /// </summary>
    public static void Aplicar(GameObject jogador, Heroi heroi)
    {
        if (jogador == null || heroi == null)
            return;

        // A vida do heroi, mais o Coracao de pedra da estatua dos cristais.
        if (jogador.TryGetComponent(out Vida vida))
            vida.DefinirMaxima(heroi.Vida + Melhorias.VidaAMais);

        if (jogador.TryGetComponent(out MovimentoDoJogador movimento))
            movimento.VelocidadeDeAndar = heroi.Velocidade;

        if (jogador.TryGetComponent(out AnimacaoDoJogador animacao))
        {
            // No jogo, o boneco sem a arma desenhada (a arma aparece so na mao, ArmaNaMao); o retrato do
            // menu continua com ela.
            animacao.TrocarFolhas(SemArma(heroi, "Idle"), SemArma(heroi, "Walk"), Textura(heroi, heroi.FolhaDoAtaque),
                                  SemArma(heroi, "Death"), heroi.QuadroDoDisparo, heroi.Pixels);
        }

        DadosDaArma arma = Arma(heroi);

        if (arma != null && jogador.TryGetComponent(out ArmaDoJogador armas))
            armas.ComecarCom(arma);

        // A bolsa (moedas e chaves), os itens e o item ativo ficam a partida inteira.
        if (!jogador.TryGetComponent(out Bolsa bolsa))
            jogador.AddComponent<Bolsa>();

        if (!jogador.TryGetComponent(out EstatisticasDoJogador itens))
            itens = jogador.AddComponent<EstatisticasDoJogador>();

        if (!jogador.TryGetComponent(out ItemAtivoDoJogador ativo))
            jogador.AddComponent<ItemAtivoDoJogador>();

        // Os itens de velocidade continuam valendo com o heroi novo.
        itens.Atualizar();

        if (!jogador.TryGetComponent(out HabilidadeDoHeroi habilidade))
            habilidade = jogador.AddComponent<HabilidadeDoHeroi>();

        habilidade.Configurar(heroi);
    }

    private static Texture2D Textura(Heroi heroi, string nome) => Resources.Load<Texture2D>(heroi.Pasta + "/" + nome);

    // A folha sem a arma desenhada (Ferramentas/Personagens/desarmar_herois.py); sem ela, a original.
    private static Texture2D SemArma(Heroi heroi, string nome) =>
        Resources.Load<Texture2D>(heroi.Pasta + "/SemArma/" + nome) ?? Textura(heroi, nome);

    private static Sprite[] Folha(Heroi heroi, string nome)
    {
        if (heroi == null)
            return new Sprite[0];

        string caminho = heroi.Pasta + "/" + nome;

        if (!folhas.TryGetValue(caminho, out Sprite[] quadros) || quadros.Length == 0 || quadros[0] == null)
        {
            quadros = FolhaDeSprites.Cortar(Textura(heroi, nome), new Vector2Int(100, 100), heroi.Pixels);
            folhas[caminho] = quadros;
        }

        return quadros;
    }
}
