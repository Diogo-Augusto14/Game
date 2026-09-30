using UnityEngine;

/// <summary>
/// Os personagens jogaveis: o arqueiro azul do Tiny Swords e os nove herois do Tiny RPG
/// Pack 01. Cada um tem vida, velocidade e um tiro proprios, como os personagens do Isaac.
///
/// O menu inicial escolhe (<see cref="Escolher"/>) e aplica no jogador que ja esta na sala;
/// o <see cref="BootstrapTopDown.CriarJogador"/> aplica o escolhido ao montar, entao
/// recomecar depois de morrer (R) continua com o mesmo heroi.
/// </summary>
public static class Herois
{
    public enum Tiro { FlechaAzul, FlechaDoSoldado, FlechaDoArqueiro, Dardo, BolaDeFogo, Estrela, Corte }

    public class Heroi
    {
        public string Nome;

        /// <summary>Pasta em Personagens/Herois. Null = o arqueiro azul do Tiny Swords.</summary>
        public string Pasta;

        public string Descricao;

        /// <summary>Vida maxima (20 = um coracao).</summary>
        public float Vida = 100f;
        public float Velocidade = 4.5f;
        public float Dano = 3.5f;
        public float Cadencia = 2.7f;
        public float Alcance = 6.5f;
        public float VelocidadeDoTiro = 9f;
        public float TamanhoDoTiro = 0.28f;
        public Tiro TipoDoTiro = Tiro.FlechaAzul;
        public Color CorDoTiro = Color.white;

        /// <summary>
        /// Vencer o chefe da ultima fase deste mundo libera o heroi (0 = nao e por chefe).
        /// Mundo 1 = Porao, 2 = Catacumbas, 3 = Cripta.
        /// </summary>
        public int LiberaNoMundo;

        /// <summary>Zerar o jogo com este heroi libera (null = nao e por ele).</summary>
        public string LiberaZerandoCom;

        /// <summary>Zerar o jogo esta quantidade de vezes, com qualquer um, libera (0 = nao e por isso).</summary>
        public int LiberaComVitorias;

        /// <summary>Livre desde o comeco: nenhuma condicao acima.</summary>
        public bool Livre => LiberaNoMundo <= 0 && LiberaZerandoCom == null && LiberaComVitorias <= 0;

        /// <summary>O que falta fazer, pro menu mostrar no heroi bloqueado.</summary>
        public string Requisito =>
            LiberaNoMundo > 0 ? $"Venca o chefe final do mundo {LiberaNoMundo}"
            : LiberaZerandoCom != null ? $"Zere o jogo com o {LiberaZerandoCom}"
            : LiberaComVitorias > 1 ? $"Zere o jogo {LiberaComVitorias} vezes"
            : LiberaComVitorias == 1 ? "Zere o jogo"
            : "";

        /// <summary>Vida que recupera sozinho a cada <see cref="CuraACada"/> segundos (0 = nada).</summary>
        public float Cura;
        public float CuraACada = 20f;
    }

    /// <summary>Pixels por unidade dos herois do Tiny RPG: o corpo (uns 20 px) fica com ~0.8 unidade.</summary>
    private const float PixelsDoHeroi = 20f;

    /// <summary>Pixels por unidade do arqueiro azul: o corpo (uns 75 px) fica com ~1 unidade na escala 0.8.</summary>
    private const float PixelsDoArqueiroAzul = 60f;

    public static readonly Heroi[] Todos =
    {
        new Heroi
        {
            Nome = "Arqueiro Azul", Pasta = null,
            Descricao = "Equilibrado. Flechas em linha reta.",
        },
        new Heroi
        {
            Nome = "Soldado", Pasta = "Soldado",
            LiberaNoMundo = 1,
            Descricao = "Aguenta mais pancada e atira um pouco mais rapido.",
            Vida = 120f, Dano = 3f, Cadencia = 3f, Alcance = 6f,
            TipoDoTiro = Tiro.FlechaDoSoldado,
        },
        new Heroi
        {
            Nome = "Cavaleiro", Pasta = "Cavaleiro",
            LiberaNoMundo = 3,
            Descricao = "Muita vida. Corte de espada forte, mas curto.",
            Vida = 140f, Velocidade = 3.8f, Dano = 5f, Cadencia = 2f, Alcance = 3.5f,
            VelocidadeDoTiro = 8f, TamanhoDoTiro = 0.45f, TipoDoTiro = Tiro.Corte,
        },
        new Heroi
        {
            Nome = "Templario", Pasta = "Templario",
            LiberaComVitorias = 3,
            Descricao = "O que mais aguenta, e o mais lento.",
            Vida = 160f, Velocidade = 3.5f, Dano = 4f, Cadencia = 2.2f, Alcance = 4f,
            VelocidadeDoTiro = 8f, TamanhoDoTiro = 0.45f, TipoDoTiro = Tiro.Corte,
            CorDoTiro = new Color(1f, 0.85f, 0.45f),
        },
        new Heroi
        {
            Nome = "Lanceiro", Pasta = "Lanceiro",
            LiberaZerandoCom = "Soldado",
            Descricao = "A cavalo: o mais rapido. Dardos fortes e velozes.",
            Velocidade = 5.5f, Dano = 4f, Cadencia = 2f, Alcance = 5.5f,
            VelocidadeDoTiro = 11f, TamanhoDoTiro = 0.3f, TipoDoTiro = Tiro.Dardo,
        },
        new Heroi
        {
            Nome = "Espadachim", Pasta = "Espadachim",
            LiberaZerandoCom = "Cavaleiro",
            Descricao = "Rapido e com cortes rapidos, mas pouca vida.",
            Vida = 80f, Velocidade = 5.2f, Dano = 4f, Cadencia = 3.2f, Alcance = 3.2f,
            TamanhoDoTiro = 0.4f, TipoDoTiro = Tiro.Corte,
            CorDoTiro = new Color(1f, 0.7f, 0.75f),
        },
        new Heroi
        {
            Nome = "Machadeiro", Pasta = "Machadeiro",
            LiberaZerandoCom = "Arqueiro",
            Descricao = "O golpe mais forte do jogo, bem devagar.",
            Vida = 120f, Velocidade = 3.8f, Dano = 6.5f, Cadencia = 1.5f, Alcance = 3.8f,
            VelocidadeDoTiro = 8f, TamanhoDoTiro = 0.5f, TipoDoTiro = Tiro.Corte,
            CorDoTiro = new Color(1f, 0.6f, 0.4f),
        },
        new Heroi
        {
            Nome = "Arqueiro", Pasta = "Arqueiro",
            LiberaNoMundo = 2,
            Descricao = "Pouca vida, mas atira rapido e longe.",
            Vida = 80f, Velocidade = 4.8f, Dano = 3f, Cadencia = 4f, Alcance = 8.5f,
            VelocidadeDoTiro = 11f, TamanhoDoTiro = 0.26f, TipoDoTiro = Tiro.FlechaDoArqueiro,
        },
        new Heroi
        {
            Nome = "Mago", Pasta = "Mago",
            LiberaComVitorias = 1,
            Descricao = "So tres coracoes, mas bolas de fogo fortes.",
            Vida = 60f, Dano = 5.5f, Cadencia = 2f, Alcance = 7f,
            VelocidadeDoTiro = 7f, TamanhoDoTiro = 0.35f, TipoDoTiro = Tiro.BolaDeFogo,
        },
        new Heroi
        {
            Nome = "Padre", Pasta = "Padre",
            LiberaZerandoCom = "Mago",
            Descricao = "Tiro fraco, mas cura meio coracao a cada 20 segundos.",
            Vida = 80f, Dano = 3f, VelocidadeDoTiro = 8f, TamanhoDoTiro = 0.35f,
            TipoDoTiro = Tiro.Estrela, CorDoTiro = new Color(1f, 0.95f, 0.6f),
            Cura = 10f, CuraACada = 20f,
        },
    };

    public static int Escolhido { get; private set; }

    public static Heroi Atual => Todos[Escolhido];

    // Com "Enter Play Mode" sem recarregar o dominio, o estatico sobreviveria entre Plays.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar()
    {
        Escolhido = 0;
    }

    /// <summary>O heroi ja pode ser jogado (livre ou liberado no <see cref="Progresso"/>).</summary>
    public static bool Liberado(Heroi heroi) => heroi.Livre || Progresso.HeroiLiberado(heroi.Nome);

    /// <summary>Escolhe pelo indice; da a volta nas pontas (o menu anda com esquerda e direita).</summary>
    public static void Escolher(int indice)
    {
        Escolhido = ((indice % Todos.Length) + Todos.Length) % Todos.Length;
    }

    /// <summary>As animacoes do heroi, ou null se a arte nao estiver no projeto.</summary>
    public static ClipesDePersonagem Clipes(Heroi heroi)
    {
        return heroi.Pasta == null
            ? ArteImportada.ArqueiroAzul(PixelsDoArqueiroAzul)
            : ArteImportada.Personagem("Herois/" + heroi.Pasta, PixelsDoHeroi);
    }

    /// <summary>O desenho do tiro. <paramref name="apontar"/> = gira pro rumo (flechas e bola de fogo).</summary>
    public static Sprite SpriteDoTiro(Tiro tiro, out bool apontar)
    {
        apontar = true;

        switch (tiro)
        {
            case Tiro.FlechaDoSoldado: return ArteImportada.FlechaDoHeroi("FlechaDoSoldado", 11f);
            case Tiro.FlechaDoArqueiro: return ArteImportada.FlechaDoHeroi("FlechaDoArqueiro", 11f);
            case Tiro.Dardo: return ArteImportada.FlechaDoHeroi("Dardo", 11f);
            case Tiro.BolaDeFogo: return ArteImportada.BolaDeFogo(10f);
        }

        apontar = false;

        switch (tiro)
        {
            case Tiro.Estrela: return ArteImportada.Estrela(14f);
            case Tiro.Corte: return ArteImportada.Corte(16f);
        }

        apontar = true;
        return ArteImportada.Flecha(24f);
    }

    /// <summary>
    /// Poe o heroi no jogador: vida cheia, velocidade, a arma, o desenho do tiro, as
    /// animacoes e a cura do padre. Pode chamar de novo pra trocar de heroi (o menu faz isso).
    /// </summary>
    public static void Aplicar(GameObject jogador, Heroi heroi)
    {
        if (jogador == null || heroi == null)
            return;

        if (jogador.TryGetComponent(out Vida vida))
            vida.Configurar(heroi.Vida, 0f, false, 0f, true);

        if (jogador.TryGetComponent(out MovimentoTopDown movimento))
            movimento.DefinirVelocidadeMaxima(heroi.Velocidade);

        if (jogador.TryGetComponent(out AtiradorTopDown atirador))
        {
            atirador.Configurar(heroi.Dano, heroi.Alcance, heroi.Cadencia);
            atirador.ConfigurarLagrima(heroi.VelocidadeDoTiro, heroi.TamanhoDoTiro, 1);

            Sprite desenhoDoTiro = SpriteDoTiro(heroi.TipoDoTiro, out bool apontar);

            if (desenhoDoTiro != null)
                atirador.DefinirVisual(desenhoDoTiro, apontar, heroi.CorDoTiro);
        }

        CuraDoHeroi cura = jogador.GetComponent<CuraDoHeroi>();

        if (heroi.Cura > 0f)
        {
            if (cura == null)
                cura = jogador.AddComponent<CuraDoHeroi>();

            cura.Configurar(heroi.Cura, heroi.CuraACada);
        }
        else if (cura != null)
        {
            Object.Destroy(cura);
        }

        ClipesDePersonagem clipes = Clipes(heroi);
        SpriteRenderer corpo = jogador.GetComponent<SpriteRenderer>();

        if (clipes == null || corpo == null)
            return;

        corpo.sprite = clipes.Parado[0];

        ArqueiroDoJogador animacao = jogador.GetComponent<ArqueiroDoJogador>();

        if (animacao == null)
            animacao = jogador.AddComponent<ArqueiroDoJogador>();

        // O arqueiro azul comeca o tiro com o arco puxado; os do Tiny RPG no meio do golpe.
        animacao.Configurar(clipes, corpo, heroi.Pasta == null ? 3 : -1);
    }
}
