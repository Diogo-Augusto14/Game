using UnityEngine;

/// <summary>
/// Cura sozinho um pouco de vida de tempos em tempos (o dom do padre). So conta com o
/// jogo andando: menu e pausa param o tempo, e o relogio para junto.
/// </summary>
[DisallowMultipleComponent]
public class CuraDoHeroi : MonoBehaviour
{
    [SerializeField, Min(0f)] private float quanto = 10f;
    [SerializeField, Min(1f)] private float aCada = 20f;

    private Vida vida;
    private float relogio;

    public void Configurar(float novaCura, float segundos)
    {
        quanto = Mathf.Max(0f, novaCura);
        aCada = Mathf.Max(1f, segundos);
        relogio = 0f;
    }

    private void Awake()
    {
        vida = GetComponent<Vida>();
    }

    private void Update()
    {
        if (vida == null || vida.EstaMorto)
            return;

        // Com a vida cheia o relogio nao anda: a cura vem inteira depois do primeiro golpe.
        if (vida.VidaAtual >= vida.VidaMaxima)
        {
            relogio = 0f;
            return;
        }

        relogio += Time.deltaTime;

        if (relogio < aCada)
            return;

        relogio = 0f;
        vida.Curar(quanto);
        Sons.Tocar(Som.Cura, 0.7f);
    }
}
