using UnityEngine;

/// <summary>
/// O desenho fica branco por um instante a cada golpe. No jogador, tambem pisca (some e volta)
/// durante o tempinho sem dano, pra dar pra ver que esta protegido.
///
/// O branco vem do shader Jogo/Silhueta (Assets/Shaders): no golpe o desenho troca de material por
/// um instante e depois volta pro dele.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Vida))]
public class PiscarAoTomarDano : MonoBehaviour
{
    [Tooltip("Os desenhos que piscam (o corpo; a sombra nao)")]
    [SerializeField] private SpriteRenderer[] desenhos;

    [Tooltip("O shader que pinta o desenho de uma cor so (Jogo/Silhueta)")]
    [SerializeField] private Shader silhueta;

    [SerializeField] private Color cor = Color.white;

    [Tooltip("Segundos que fica branco a cada golpe")]
    [SerializeField, Min(0f)] private float duracao = 0.08f;

    [Tooltip("Pisca durante o tempinho sem dano depois do golpe (bom no jogador)")]
    [SerializeField] private bool piscarNoTempoSemDano;

    [SerializeField, Min(1f)] private float piscadasPorSegundo = 12f;

    [Tooltip("Transparencia do desenho na metade 'apagada' da piscada")]
    [SerializeField, Range(0f, 1f)] private float alfaApagado = 0.3f;

    private Vida vida;
    private Material branco;
    private Material[] originais;
    private float brancoAte = -10f;
    private bool estaBranco;
    private bool estaApagado;

    private void Awake()
    {
        vida = GetComponent<Vida>();

        if (desenhos == null || desenhos.Length == 0)
            desenhos = GetComponentsInChildren<SpriteRenderer>();

        originais = new Material[desenhos.Length];

        for (int i = 0; i < desenhos.Length; i++)
            originais[i] = desenhos[i] != null ? desenhos[i].sharedMaterial : null;

        if (silhueta != null)
        {
            branco = new Material(silhueta) { name = "Branco do golpe" };
            branco.SetColor("_Cor", cor);
        }
    }

    private void OnEnable()
    {
        vida.AoTomarDano += Tomou;
    }

    private void OnDisable()
    {
        vida.AoTomarDano -= Tomou;
        brancoAte = -10f;
        Aplicar(false, false);
    }

    private void OnDestroy()
    {
        if (branco != null)
            Destroy(branco);
    }

    private void Tomou(Dano dano)
    {
        brancoAte = Time.time + duracao;
    }

    private void LateUpdate()
    {
        bool querBranco = branco != null && Time.time < brancoAte;
        bool querApagado = !querBranco && piscarNoTempoSemDano && !vida.Morto && vida.NoTempoSemDano
                        && Mathf.Repeat(Time.time * piscadasPorSegundo, 1f) < 0.5f;

        Aplicar(querBranco, querApagado);
    }

    // So mexe quando muda: a cor do desenho fica livre pra outras pecas (o inimigo desbotando ao morrer).
    private void Aplicar(bool querBranco, bool querApagado)
    {
        for (int i = 0; i < desenhos.Length; i++)
        {
            SpriteRenderer desenho = desenhos[i];

            if (desenho == null)
                continue;

            if (querBranco != estaBranco)
                desenho.sharedMaterial = querBranco ? branco : originais[i];

            if (querApagado != estaApagado)
            {
                Color c = desenho.color;
                c.a = querApagado ? alfaApagado : 1f;
                desenho.color = c;
            }
        }

        estaBranco = querBranco;
        estaApagado = querApagado;
    }
}
