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

    [Header("Varios tiros")]
    [Min(1)] public int tirosPorDisparo = 1;

    [Tooltip("Graus entre um tiro e o vizinho, quando sai mais de um")]
    [Range(0f, 45f)] public float abertura = 10f;

    [Tooltip("Cada tiro sai torto ate esses graus pra cada lado")]
    [Range(0f, 30f)] public float dispersao = 2f;

    [Header("Sensacao")]
    [Tooltip("Tremor da camera a cada disparo (0 = nada)")]
    [Min(0f)] public float tremor = 0.02f;

    public AudioClip som;

    [Range(0f, 1f)] public float volume = 0.45f;

    /// <summary>
    /// Solta um disparo: um tiro, ou o leque de <see cref="tirosPorDisparo"/>, cada um um pouco torto
    /// (<see cref="dispersao"/>). O <paramref name="lado"/> e de quem atira: o tiro nao machuca esse lado.
    /// </summary>
    public void Disparar(Vector2 origem, Vector2 rumo, GameObject dono, Lado lado)
    {
        float angulo = Mathf.Atan2(rumo.y, rumo.x) * Mathf.Rad2Deg;
        float primeiro = -abertura * (tirosPorDisparo - 1) * 0.5f;

        for (int i = 0; i < tirosPorDisparo; i++)
        {
            float torto = Random.Range(-dispersao, dispersao);
            float a = (angulo + primeiro + abertura * i + torto) * Mathf.Deg2Rad;
            Projetil.Disparar(origem, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), this, dono, lado);
        }
    }
}
