using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O que se ve e se ouve no dash do <see cref="MovimentoTopDown"/>: uma nuvem de poeira do
/// Tiny Swords onde a arrancada comeca, copias azuladas do heroi que ficam para tras e
/// somem (o rastro) e, quando a recarga acaba, o brilho de lamina do Tiny RPG piscando no
/// heroi pra avisar que da pra dar outro.
///
/// A animacao do corpo durante o dash (a corrida acelerada) fica no <see cref="ArqueiroDoJogador"/>.
/// Nao mexe na cor do corpo: quem pisca o heroi e o <see cref="Vida"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MovimentoTopDown))]
public class RastroDoDash : MonoBehaviour
{
    [Tooltip("Segundos entre uma copia do rastro e a proxima")]
    [SerializeField, Min(0.005f)] private float intervaloDoRastro = 0.025f;

    [Tooltip("Segundos que cada copia leva pra sumir")]
    [SerializeField, Min(0.01f)] private float duracaoDoRastro = 0.22f;

    [SerializeField] private Color corDoRastro = new Color(0.55f, 0.8f, 1f, 0.6f);

    /// <summary>Pixels por unidade da poeira: a nuvem (uns 30 px) fica com ~0.6 unidade.</summary>
    private const float PixelsDaPoeira = 50f;

    /// <summary>Pixels por unidade do brilho de recarga (o brilho tem uns 13 px).</summary>
    private const float PixelsDoBrilho = 22f;

    private struct Copia
    {
        public SpriteRenderer Desenho;
        public float Nasceu;
    }

    private MovimentoTopDown movimento;
    private SpriteRenderer corpo;
    private Vida vida;
    private readonly List<Copia> copias = new List<Copia>();
    private float proximaCopia;

    private void Awake()
    {
        movimento = GetComponent<MovimentoTopDown>();
        corpo = GetComponent<SpriteRenderer>();
        vida = GetComponent<Vida>();
    }

    private void OnEnable()
    {
        movimento.AoDash += Comecou;
        movimento.AoDashPronto += Recarregou;
    }

    private void OnDisable()
    {
        movimento.AoDash -= Comecou;
        movimento.AoDashPronto -= Recarregou;
    }

    private void OnDestroy()
    {
        foreach (Copia copia in copias)
        {
            if (copia.Desenho != null)
                Destroy(copia.Desenho.gameObject);
        }

        copias.Clear();
    }

    private void Comecou(Vector2 rumo)
    {
        Sons.Tocar(Som.Dash, 0.6f);
        proximaCopia = 0f;

        // A poeira nasce nos pes, um pouco atras de quem arrancou.
        Vector2 pes = (Vector2)transform.position + new Vector2(0f, -0.3f) - rumo * 0.25f;
        EfeitoDeQuadros poeira = EfeitoDeQuadros.Criar(ArteImportada.Poeira(PixelsDaPoeira), 24f, pes, OrdemDoCorpo - 1);

        if (poeira != null)
            poeira.Virar(rumo.x < 0f);
    }

    private void Recarregou()
    {
        if (vida != null && vida.EstaMorto)
            return;

        // Segue o heroi (filho dele) pra nao ficar pra tras se ele estiver andando.
        EfeitoDeQuadros brilho = EfeitoDeQuadros.Criar(ArteImportada.Brilho(PixelsDoBrilho), 14f,
                                                        (Vector2)transform.position + new Vector2(0.25f, 0.3f),
                                                        OrdemDoCorpo + 2, transform);

        if (brilho != null)
            brilho.GetComponent<SpriteRenderer>().color = new Color(0.8f, 0.95f, 1f);
    }

    private int OrdemDoCorpo => corpo != null ? corpo.sortingOrder : 10;

    private void Update()
    {
        if (movimento.Dashando && corpo != null && corpo.sprite != null && Time.time >= proximaCopia)
        {
            proximaCopia = Time.time + intervaloDoRastro;
            DeixarCopia();
        }

        // Cada copia vai ficando transparente ate sumir.
        for (int i = copias.Count - 1; i >= 0; i--)
        {
            Copia copia = copias[i];
            float t = (Time.time - copia.Nasceu) / duracaoDoRastro;

            if (copia.Desenho == null || t >= 1f)
            {
                if (copia.Desenho != null)
                    Destroy(copia.Desenho.gameObject);

                copias.RemoveAt(i);
                continue;
            }

            Color cor = corDoRastro;
            cor.a *= 1f - t;
            copia.Desenho.color = cor;
        }
    }

    private void DeixarCopia()
    {
        GameObject obj = new GameObject("Rastro do dash");
        obj.transform.position = transform.position;
        obj.transform.rotation = transform.rotation;
        obj.transform.localScale = transform.lossyScale;

        SpriteRenderer desenho = obj.AddComponent<SpriteRenderer>();
        desenho.sprite = corpo.sprite;
        desenho.flipX = corpo.flipX;
        desenho.color = corDoRastro;
        desenho.sortingOrder = OrdemDoCorpo - 1;

        copias.Add(new Copia { Desenho = desenho, Nasceu = Time.time });
    }
}
