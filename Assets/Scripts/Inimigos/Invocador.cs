using System.Collections;
using UnityEngine;

/// <summary>
/// Levanta ajudantes do chao de tempos em tempos (o Necromante do jogo antigo ergue esqueletos),
/// no maximo uns tantos vivos de cada vez. Os levantados contam pro andar
/// (<see cref="GeradorDoAndar.Registrar"/>), nascem acordados e saem do chao crescendo.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(InimigoAtirador), typeof(Vida))]
public class Invocador : MonoBehaviour
{
    [SerializeField] private GameObject invocado;

    [Tooltip("Quantos dos levantados por ele podem estar vivos ao mesmo tempo")]
    [SerializeField, Min(1)] private int maximo = 2;

    [Tooltip("Segundos entre uma invocacao e a proxima")]
    [SerializeField, Min(1f)] private float intervalo = 7f;

    [Tooltip("Segundos que o levantado leva pra sair do chao (parado e sem atacar)")]
    [SerializeField, Min(0.05f)] private float erguer = 0.6f;

    [SerializeField] private AudioClip som;
    [SerializeField, Range(0f, 1f)] private float volume = 0.45f;

    private readonly Vida[] levantados = new Vida[8];
    private InimigoAtirador inimigo;
    private Vida vida;
    private AudioSource audioSource;

    private void Awake()
    {
        inimigo = GetComponent<InimigoAtirador>();
        vida = GetComponent<Vida>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (invocado != null)
            StartCoroutine(Invocar());
    }

    private IEnumerator Invocar()
    {
        yield return new WaitForSeconds(intervalo * Random.Range(0.4f, 0.7f));

        while (!vida.Morto)
        {
            if (inimigo.Acordado && !inimigo.Ocupado && Vivos() < Mathf.Min(maximo, levantados.Length))
            {
                Levantar();
                yield return new WaitForSeconds(intervalo * Random.Range(0.85f, 1.15f));
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    private int Vivos()
    {
        int n = 0;

        foreach (Vida v in levantados)
        {
            if (v != null && !v.Morto)
                n++;
        }

        return n;
    }

    private void Levantar()
    {
        Vector2 onde = (Vector2)transform.position + Random.insideUnitCircle.normalized * 1.4f;
        int bloqueia = (1 << Pedreiro.CamadaDaParede) | (Pedreiro.CamadaDoBuraco >= 0 ? 1 << Pedreiro.CamadaDoBuraco : 0);

        if (Physics2D.OverlapCircle(onde, 0.4f, bloqueia) != null)
            onde = (Vector2)transform.position - inimigo.OlhandoPara * 0.9f;

        GameObject novo = Instantiate(invocado, onde, Quaternion.identity, GeradorDoAndar.Raiz);

        if (audioSource != null && som != null)
            audioSource.PlayOneShot(som, volume);

        if (novo.TryGetComponent(out Vida dele))
        {
            for (int i = 0; i < levantados.Length; i++)
            {
                if (levantados[i] == null || levantados[i].Morto)
                {
                    levantados[i] = dele;
                    break;
                }
            }

            GeradorDoAndar.Registrar(dele);
        }

        if (novo.TryGetComponent(out InimigoAtirador levantado))
        {
            levantado.Acordar();
            novo.AddComponent<SaindoDoChao>().Comecar(erguer);
        }
    }
}
