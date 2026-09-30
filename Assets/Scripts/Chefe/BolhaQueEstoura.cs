using UnityEngine;

/// <summary>
/// Vai num tiro grande e lento: depois de um tempo ele estoura num anel de tiros menores.
/// Se bater em parede ou no jogador antes, some sem estourar (o tiro se destroi sozinho).
/// Pisca rapido pouco antes de estourar, pra dar tempo de sair de perto.
/// </summary>
[RequireComponent(typeof(TiroDaSala))]
public class BolhaQueEstoura : MonoBehaviour
{
    private float tempo;
    private int estilhacos;
    private float velocidade;
    private float dano;
    private Color cor;
    private GameObject dono;
    private float nascimento;
    private SpriteRenderer desenho;

    public static void Colocar(TiroDaSala tiro, float tempoAteEstourar, int estilhacos, float velocidade,
                               float dano, Color cor, GameObject dono)
    {
        tiro.Vestir(EstiloDeTiro.Bolha);
        BolhaQueEstoura bolha = tiro.gameObject.AddComponent<BolhaQueEstoura>();
        bolha.tempo = tempoAteEstourar;
        bolha.estilhacos = estilhacos;
        bolha.velocidade = velocidade;
        bolha.dano = dano;
        bolha.cor = cor;
        bolha.dono = dono;
    }

    private void Start()
    {
        nascimento = Time.time;
        desenho = GetComponent<TiroDaSala>().Desenho;
    }

    private void Update()
    {
        float idade = Time.time - nascimento;

        if (desenho != null && idade > tempo - 0.3f)
            desenho.color = Mathf.Repeat(idade * 12f, 1f) < 0.5f ? cor : Color.white;

        if (idade < tempo)
            return;

        Vector2 centro = transform.position;
        float inicio = Random.value * 360f;

        for (int i = 0; i < estilhacos; i++)
        {
            float angulo = (inicio + i * 360f / estilhacos) * Mathf.Deg2Rad;
            Vector2 rumo = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
            TiroDaSala.Disparar(centro + rumo * 0.2f, rumo * velocidade, dano, dono, true, cor);
        }

        GetComponent<TiroDaSala>().MostrarImpacto();
        Destroy(gameObject);
    }
}
