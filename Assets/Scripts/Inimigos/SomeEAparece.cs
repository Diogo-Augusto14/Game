using System.Collections;
using UnityEngine;

/// <summary>
/// De vez em quando o inimigo some e reaparece em outro lugar perto do jogador (veio do jogo
/// antigo: o Bruxo e o Fogo-fatuo). Enquanto esta sumido nao leva tiro. So some acordado e fora
/// do meio de um ataque (<see cref="InimigoAtirador.Ocupado"/>).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(InimigoAtirador), typeof(Vida))]
public class SomeEAparece : MonoBehaviour
{
    [Tooltip("Segundos entre um sumico e o proximo (varia um pouco)")]
    [SerializeField, Min(0.5f)] private float intervalo = 5f;

    [Tooltip("Segundos sumindo e aparecendo (cada um)")]
    [SerializeField, Min(0.05f)] private float desvanecer = 0.25f;

    [Tooltip("Segundos sumido de vez, antes de aparecer no lugar novo")]
    [SerializeField, Min(0f)] private float sumido = 0.35f;

    [Tooltip("Reaparece a esta distancia do jogador (minimo e maximo)")]
    [SerializeField] private Vector2 distancia = new Vector2(3f, 6f);

    [SerializeField] private AudioClip som;
    [SerializeField, Range(0f, 1f)] private float volume = 0.4f;

    private InimigoAtirador inimigo;
    private Vida vida;
    private Rigidbody2D corpo;
    private Collider2D[] colisores;
    private SpriteRenderer[] desenhos;
    private AudioSource audioSource;
    private Transform alvo;

    private void Awake()
    {
        inimigo = GetComponent<InimigoAtirador>();
        vida = GetComponent<Vida>();
        corpo = GetComponent<Rigidbody2D>();
        colisores = GetComponents<Collider2D>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        desenhos = GetComponentsInChildren<SpriteRenderer>();
        GameObject jogador = GameObject.FindWithTag("Player");

        if (jogador != null)
        {
            alvo = jogador.transform;
            StartCoroutine(Sumir());
        }
    }

    private IEnumerator Sumir()
    {
        while (!vida.Morto)
        {
            yield return new WaitForSeconds(intervalo * Random.Range(0.8f, 1.25f));

            // Espera ficar livre (acordado e fora de ataque) pra sumir.
            while (!vida.Morto && (!inimigo.Acordado || inimigo.Ocupado))
                yield return null;

            if (vida.Morto || alvo == null || !Lugar(out Vector2 novo))
                continue;

            if (audioSource != null && som != null)
                audioSource.PlayOneShot(som, volume);

            yield return Desvanecer(1f, 0f);
            Ligar(false);
            yield return new WaitForSeconds(sumido);

            if (vida.Morto)
                break;

            corpo.position = novo;
            transform.position = novo;
            corpo.linearVelocity = Vector2.zero;
            yield return Desvanecer(0f, 1f);
            Ligar(true);
        }

        Ligar(true);
    }

    private IEnumerator Desvanecer(float de, float ate)
    {
        for (float t = 0f; t < 1f && !vida.Morto; t += Time.deltaTime / desvanecer)
        {
            Alfa(Mathf.Lerp(de, ate, t));
            yield return null;
        }

        Alfa(vida.Morto ? 1f : ate);
    }

    private void Alfa(float a)
    {
        foreach (SpriteRenderer d in desenhos)
        {
            if (d == null)
                continue;

            Color c = d.color;
            c.a = a;
            d.color = c;
        }
    }

    private void Ligar(bool ligado)
    {
        foreach (Collider2D c in colisores)
            c.enabled = ligado;
    }

    // Um lugar com chao livre perto do jogador, sem parede entre os dois.
    private bool Lugar(out Vector2 onde)
    {
        int paredes = 1 << Pedreiro.CamadaDaParede;
        int bloqueia = paredes | (Pedreiro.CamadaDoBuraco >= 0 ? 1 << Pedreiro.CamadaDoBuraco : 0);

        for (int i = 0; i < 16; i++)
        {
            Vector2 ponto = (Vector2)alvo.position + Random.insideUnitCircle.normalized * Random.Range(distancia.x, distancia.y);

            if (Physics2D.OverlapCircle(ponto, 0.5f, bloqueia) != null)
                continue;

            if (Physics2D.Linecast(alvo.position, ponto, paredes).collider != null)
                continue;

            // Dentro do andar: o mapa de caminhos conhece o chao.
            if (MapaDeCaminhos.Atual != null && !MapaDeCaminhos.Atual.TemChao(ponto))
                continue;

            onde = ponto;
            return true;
        }

        onde = default;
        return false;
    }
}
