using UnityEngine;

/// <summary>
/// Os numeros e o desenho de uma arma: o arco do arqueiro, e depois as outras. E um asset (fica em
/// Assets/Dados/Armas): da pra mexer no Inspector com o jogo rodando e criar uma arma nova pelo menu
/// Create ▸ Jogo ▸ Arma, sem tocar no codigo.
///
/// Quem atira com ela e o <see cref="ArmaDoJogador"/> (ou um inimigo, como o <see cref="InimigoAtirador"/>);
/// cada tiro e um <see cref="Projetil"/>.
///
/// Toda arma tem um tiro especial (G), feito a partir dela mesma (<see cref="Especial"/>): um leque, um
/// anel, um tiro carregado, um giro de cortes ou um tiro que explode (<see cref="especial"/>), com
/// recarga e sem gastar municao. So o especial quebra a pedra rachada.
/// </summary>
[CreateAssetMenu(fileName = "NovaArma", menuName = "Jogo/Arma")]
public class DadosDaArma : ScriptableObject
{
    public string nome = "Arma";

    [Header("Na mao")]
    [Tooltip("O desenho da arma, olhando pra direita (tambem e o que aparece no chao). Vazio = a arma e do " +
             "proprio personagem e ja esta no desenho dele, como o arco do Arqueiro")]
    public Sprite desenhoNaMao;

    [Tooltip("O desenho no chao e na moldura quando nao tem desenho na mao (o arco, que na mao ja esta no desenho do Arqueiro)")]
    public Sprite desenhoNoChao;

    /// <summary>O desenho da arma solta (no chao, na moldura): o da mao, ou o do chao.</summary>
    public Sprite Desenho => desenhoNaMao != null ? desenhoNaMao : desenhoNoChao;

    [Tooltip("Graus pra girar o desenho na mao ate ele apontar pra direita (o desenho na diagonal: -45)")]
    public float giroNaMao;

    [Tooltip("Distancia do centro do corpo ate a arma, na direcao da mira")]
    [Min(0f)] public float distanciaDaMao = 0.4f;

    [Tooltip("Quanto a arma recua a cada tiro, em unidades")]
    [Min(0f)] public float coice = 0.08f;

    [Header("Municao")]
    [Tooltip("Nunca acaba (a arma do personagem)")]
    public bool infinita;

    [Tooltip("Sem pente nem recarga: atira direto da municao toda (os machados: e so pegar o proximo)")]
    public bool semPente;

    [Tooltip("Tiros no pente; acabou, recarrega")]
    [Min(1)] public int pente = 12;

    [Tooltip("Municao total que a arma carrega (pente + reserva)")]
    [Min(1)] public int municaoMaxima = 120;

    [Tooltip("Segundos recarregando")]
    [Min(0f)] public float recarga = 1f;

    public AudioClip somDaRecarga;

    [Tooltip("Chance de sair num bau, comparada com as outras armas (0 = nunca)")]
    [Min(0f)] public float pesoNoBau = 1f;

    [Tooltip("Comum, incomum, rara, epica ou lendaria: a cor do nome, o brilho no chao e a chance no bau " +
             "(as raras saem mais nos andares fundos)")]
    public Raridade raridade;

    /// <summary>O peso no bau neste andar: as raras ficam mais comuns quanto mais fundo.</summary>
    public float PesoNoAndar(int andar) => pesoNoBau * Raridades.Fator(raridade, andar);

    [Header("O tiro")]
    [Tooltip("O desenho do tiro (flecha, bala...)")]
    public Sprite desenhoDoTiro;

    [Tooltip("O desenho aponta pra onde o tiro vai (flecha: sim; bala redonda: tanto faz)")]
    public bool apontarODesenho = true;

    public Color cor = Color.white;

    [Tooltip("Raio do tiro que acerta as coisas, em unidades")]
    [Min(0.01f)] public float raio = 0.12f;

    [Header("Ritmo")]
    [Tooltip("Segurando o botao continua atirando. Desligado: um tiro por clique")]
    public bool automatica = true;

    [Min(0.1f)] public float tirosPorSegundo = 5f;

    [Header("Voo")]
    [Min(0.1f)] public float velocidade = 14f;

    [Tooltip("Distancia que o tiro voa antes de cair, em unidades")]
    [Min(0.1f)] public float alcance = 11f;

    [Min(0f)] public float dano = 3f;

    [Tooltip("Velocidade do empurraozinho em quem leva o tiro, em unidades por segundo (0 = nao empurra)")]
    [Min(0f)] public float empurrao = 3f;

    [Tooltip("Quanto a velocidade muda por segundo em voo (negativo: freia ate quase parar; positivo: acelera)")]
    public float aceleracao;

    [Tooltip("Graus por segundo que o tiro vira em voo (faz curva; 0 = reto)")]
    public float curva;

    [Tooltip("Bumerangue: depois de frear (aceleracao negativa) volta pra quem jogou")]
    public bool volta;

    [Tooltip("Atravessa os inimigos (a onda de corte das espadas): acerta cada um uma vez so e segue")]
    public bool atravessa;

    [Tooltip("O efeito da flecha especial: explode, gela, envenena ou quica nas paredes")]
    public EfeitoDoTiro efeito;

    [Tooltip("Graus por segundo que o desenho gira em voo (o machado rodando; 0 = nao gira)")]
    public float giroDoDesenho;

    [Header("Varios tiros")]
    [Min(1)] public int tirosPorDisparo = 1;

    [Tooltip("Graus entre um tiro e o vizinho, quando sai mais de um")]
    [Range(0f, 45f)] public float abertura = 10f;

    [Tooltip("Os tiros do disparo saem em volta toda, igualmente espalhados (um anel); a abertura nao conta")]
    public bool anel;

    [Tooltip("Cada tiro sai torto ate esses graus pra cada lado")]
    [Range(0f, 30f)] public float dispersao = 2f;

    [Header("Tiro especial (G)")]
    [Tooltip("O que o tiro especial faz (sai da propria arma, mais forte): leque, anel, carregado, giro ou explosivo")]
    public TipoDeEspecial especial = TipoDeEspecial.Leque;

    [Tooltip("Segundos ate o tiro especial ficar pronto de novo")]
    [Min(0.5f)] public float recargaDoEspecial = 6f;

    [Tooltip("O tamanho do desenho do tiro (1 = o da folha)")]
    [Min(0.1f)] public float escalaDoDesenho = 1f;

    /// <summary>E o tiro especial de alguma arma (o dano dele quebra a pedra rachada).</summary>
    [System.NonSerialized] public bool ehEspecial;

    [System.NonSerialized] private DadosDaArma especialPronto;

    /// <summary>O nome do tiro especial, pro HUD.</summary>
    public string NomeDoEspecial
    {
        get
        {
            switch (especial)
            {
                case TipoDeEspecial.Anel: return "Anel";
                case TipoDeEspecial.Carregado: return "Tiro carregado";
                case TipoDeEspecial.Giro: return "Giro de cortes";
                case TipoDeEspecial.Explosivo: return "Tiro explosivo";
                default: return "Leque";
            }
        }
    }

    /// <summary>
    /// O tiro especial desta arma: uma copia dela com o padrao mudado (feita uma vez e guardada).
    /// </summary>
    public DadosDaArma Especial
    {
        get
        {
            if (especialPronto != null)
                return especialPronto;

            DadosDaArma e = Instantiate(this);
            e.name = name + " (especial)";
            e.ehEspecial = true;
            e.rajada = 1;
            e.dispersao = 0f;
            e.tremor = Mathf.Max(tremor * 2f, 0.12f);
            e.volume = Mathf.Min(1f, volume * 1.4f);

            switch (especial)
            {
                case TipoDeEspecial.Anel:
                    e.anel = true;
                    e.tirosPorDisparo = 14;
                    e.dano = dano * 1.5f;
                    break;

                case TipoDeEspecial.Carregado:
                    e.anel = false;
                    e.tirosPorDisparo = 1;
                    e.dano = dano * 6f;
                    e.empurrao = empurrao * 3f + 4f;
                    e.raio = raio * 2.2f;
                    e.escalaDoDesenho = escalaDoDesenho * 2.2f;
                    e.velocidade = velocidade * 0.85f;
                    e.alcance = alcance * 1.3f;
                    e.atravessa = true;
                    break;

                case TipoDeEspecial.Giro:
                    e.anel = true;
                    e.tirosPorDisparo = 8;
                    e.dano = dano * 2f;
                    e.atravessa = true;
                    e.rajada = 2;
                    e.intervaloDaRajada = 0.12f;
                    e.giroNaRajada = 22.5f;
                    break;

                case TipoDeEspecial.Explosivo:
                    e.anel = false;
                    e.tirosPorDisparo = 1;
                    e.dano = dano * 3f;
                    e.raio = raio * 1.5f;
                    e.escalaDoDesenho = escalaDoDesenho * 1.5f;
                    e.efeito = EfeitoDoTiro.Explode;
                    break;

                default:
                    e.anel = false;
                    e.tirosPorDisparo = Mathf.Max(5, tirosPorDisparo + 4);
                    e.abertura = 9f;
                    e.dano = dano * 1.5f;
                    break;
            }

            especialPronto = e;
            return e;
        }
    }

    [Header("Rajada")]
    [Tooltip("Quantos disparos seguidos saem a cada vez que atira (1 = um so)")]
    [Min(1)] public int rajada = 1;

    [Tooltip("Segundos entre um disparo da rajada e o proximo")]
    [Min(0.01f)] public float intervaloDaRajada = 0.1f;

    [Tooltip("Graus que cada disparo da rajada gira em relacao ao anterior (com anel: uma espiral)")]
    public float giroNaRajada;

    [Header("Sensacao")]
    [Tooltip("Tremor da camera a cada disparo (0 = nada)")]
    [Min(0f)] public float tremor = 0.02f;

    public AudioClip som;

    [Range(0f, 1f)] public float volume = 0.45f;

    /// <summary>
    /// Solta um disparo: um tiro, o leque de <see cref="tirosPorDisparo"/> ou o anel, cada um um pouco
    /// torto (<see cref="dispersao"/>). <paramref name="giro"/> gira o disparo inteiro, em graus (a
    /// rajada usa). O <paramref name="lado"/> e de quem atira: o tiro nao machuca esse lado.
    /// A rajada (varios disparos seguidos) e com a <see cref="Rajada"/>.
    /// </summary>
    public void Disparar(Vector2 origem, Vector2 rumo, GameObject dono, Lado lado, float giro = 0f)
    {
        float angulo = Mathf.Atan2(rumo.y, rumo.x) * Mathf.Rad2Deg + giro;
        float passo = anel ? 360f / tirosPorDisparo : abertura;
        float primeiro = anel ? 0f : -abertura * (tirosPorDisparo - 1) * 0.5f;

        for (int i = 0; i < tirosPorDisparo; i++)
        {
            float torto = Random.Range(-dispersao, dispersao);
            float a = (angulo + primeiro + passo * i + torto) * Mathf.Deg2Rad;
            Projetil.Disparar(origem, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), this, dono, lado);
        }
    }
}

/// <summary>Quao rara e uma arma (a do heroi e comum).</summary>
public enum Raridade
{
    Comum,
    Incomum,
    Rara,
    Epica,
    Lendaria,
}

/// <summary>O nome, a cor e a chance de cada raridade.</summary>
public static class Raridades
{
    public const int Quantas = 5;

    public static string Nome(Raridade r)
    {
        switch (r)
        {
            case Raridade.Incomum: return "Incomum";
            case Raridade.Rara: return "Rara";
            case Raridade.Epica: return "Épica";
            case Raridade.Lendaria: return "Lendária";
            default: return "Comum";
        }
    }

    public static Color Cor(Raridade r)
    {
        switch (r)
        {
            case Raridade.Incomum: return new Color(0.45f, 0.9f, 0.4f);
            case Raridade.Rara: return new Color(0.4f, 0.65f, 1f);
            case Raridade.Epica: return new Color(0.8f, 0.45f, 1f);
            case Raridade.Lendaria: return new Color(1f, 0.75f, 0.25f);
            default: return new Color(0.92f, 0.9f, 0.86f);
        }
    }

    /// <summary>Quanto o peso no bau muda neste andar: as comuns somem aos poucos, as raras aparecem.</summary>
    public static float Fator(Raridade r, int andar)
    {
        float fundo = Mathf.Max(0, andar - 1);

        switch (r)
        {
            case Raridade.Comum: return Mathf.Max(0.25f, 1.2f - 0.1f * fundo);
            case Raridade.Incomum: return 1f;
            case Raridade.Rara: return 0.5f + 0.08f * fundo;
            case Raridade.Epica: return 0.2f + 0.06f * fundo;
            default: return 0.05f + 0.04f * fundo;
        }
    }
}

/// <summary>O tiro especial (G) de uma arma: o padrao dela, mudado e mais forte.</summary>
public enum TipoDeEspecial
{
    /// <summary>Um leque largo dos tiros da arma (as flechas, a besta).</summary>
    Leque,

    /// <summary>Um anel de tiros em volta (os cajados, o tomo, o cetro).</summary>
    Anel,

    /// <summary>Um tiro so, grande, que atravessa e empurra muito (a lanca, o trovao).</summary>
    Carregado,

    /// <summary>Dois aneis de cortes que atravessam (as espadas, o sabre).</summary>
    Giro,

    /// <summary>Um tiro maior que explode onde bate (o machado, o cajado de fogo).</summary>
    Explosivo,
}

/// <summary>O que o tiro faz alem do dano (as flechas especiais do jogo antigo, agora arcos achados).</summary>
public enum EfeitoDoTiro
{
    Nenhum,

    /// <summary>Explode ao acertar ou bater na parede (fere em volta).</summary>
    Explode,

    /// <summary>O inimigo fica lento e azulado por uns segundos.</summary>
    Gela,

    /// <summary>O inimigo perde vida aos poucos por uns segundos.</summary>
    Envenena,

    /// <summary>Quica nas paredes (ate 3 vezes) em vez de sumir.</summary>
    Quica,
}
