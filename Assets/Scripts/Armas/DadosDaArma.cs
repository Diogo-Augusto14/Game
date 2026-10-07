using UnityEngine;

/// <summary>
/// Os numeros e o desenho de uma arma: o arco do arqueiro, e depois as outras. E um asset (fica em
/// Assets/Dados/Armas): da pra mexer no Inspector com o jogo rodando e criar uma arma nova pelo menu
/// Create ▸ Jogo ▸ Arma, sem tocar no codigo.
///
/// Quem atira com ela e o <see cref="ArmaDoJogador"/> (ou um inimigo, como o <see cref="InimigoAtirador"/>);
/// cada tiro e um <see cref="Projetil"/>.
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
