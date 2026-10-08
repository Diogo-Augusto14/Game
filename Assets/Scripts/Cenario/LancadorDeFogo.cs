using UnityEngine;

/// <summary>
/// Uma armadilha do pacote Crypt: a estatua com cara, presa na parede de cima da sala, que cospe um leque
/// de bolas de fogo pra baixo de tempos em tempos, enquanto o jogador esta na sala dela. Acende antes
/// (a boca brilha), pra dar tempo de sair da frente. Fere so o jogador.
/// </summary>
public class LancadorDeFogo : MonoBehaviour
{
    private const int QuadroDoDisparo = 3;

    /// <summary>A estatua fica na face da parede; o fogo nasce ja no chao (senao bate na propria parede).</summary>
    public const float DoChao = 0.95f;

    private static Sprite[] quadros;
    private static DadosDaArma fogo;

    private SpriteRenderer desenho;
    private SalaDaPlanta sala;
    private Transform jogador;
    private float proximo;
    private float comecou = -1f;
    private bool disparou;
    private float intervalo;

    public static LancadorDeFogo Criar(Vector2 onde, Transform pai, SalaDaPlanta sala)
    {
        if (quadros == null)
        {
            quadros = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Decoracao/LancadorDeFogo"), new Vector2Int(100, 100), 32f);
            fogo = Resources.Load<DadosDaArma>("Armadilhas/FogoDaEstatua");
        }

        if (quadros.Length == 0 || fogo == null)
            return null;

        GameObject obj = new GameObject("Lancador de fogo");
        obj.transform.SetParent(pai, false);
        obj.transform.position = onde;

        LancadorDeFogo l = obj.AddComponent<LancadorDeFogo>();
        l.desenho = obj.AddComponent<SpriteRenderer>();
        l.desenho.sprite = quadros[0];
        l.desenho.sortingOrder = Pedreiro.OrdemDasParedes + 1;
        l.sala = sala;
        l.intervalo = Random.Range(2.6f, 3.6f);
        l.proximo = Time.time + Random.Range(1f, l.intervalo);
        return l;
    }

    private void Update()
    {
        if (jogador == null)
        {
            GameObject j = GameObject.FindWithTag("Player");
            jogador = j != null ? j.transform : null;

            if (jogador == null)
                return;
        }

        // Tocando: o fogo sai no quadro do disparo.
        if (comecou >= 0f)
        {
            int q = Mathf.FloorToInt((Time.time - comecou) * 12f);

            if (q >= quadros.Length)
            {
                comecou = -1f;
                desenho.sprite = quadros[0];
                return;
            }

            desenho.sprite = quadros[q];

            if (q >= QuadroDoDisparo && !disparou)
            {
                disparou = true;
                fogo.Disparar((Vector2)transform.position + Vector2.down * DoChao, Vector2.down, gameObject, Lado.Inimigos);
            }

            return;
        }

        if (Time.time < proximo)
            return;

        proximo = Time.time + intervalo;

        if (sala == null || sala.BemDentro(jogador.position, 0.5f))
        {
            comecou = Time.time;
            disparou = false;
        }
    }
}
