using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O que se ve quando um chefe arranca (investida, chifrada): poeira do Tiny Swords nos pes
/// enquanto ele se prepara e quando sai, e copias apagadas do proprio sprite que ficam para
/// tras e somem rapido (o rastro). Igual ao dash do heroi, so que sem cor: nada de faixa vermelha.
/// O aviso de pra onde ele vai e uma sombra curta no chao (uns 3,5 m) que some na ponta.
/// O chefe liga e desliga com <see cref="Ligado"/>, pede poeira com <see cref="Poeira"/> e mostra
/// o aviso com <see cref="MostrarSombra"/>.
/// </summary>
[DisallowMultipleComponent]
public class RastroDoChefe : MonoBehaviour
{
    [Tooltip("Segundos entre uma copia do rastro e a proxima")]
    [SerializeField, Min(0.005f)] private float intervaloDoRastro = 0.045f;

    [Tooltip("Segundos que cada copia leva pra sumir")]
    [SerializeField, Min(0.01f)] private float duracaoDoRastro = 0.2f;

    [SerializeField] private Color corDoRastro = new Color(1f, 1f, 1f, 0.4f);

    [Tooltip("Segundos minimos entre duas nuvens de poeira")]
    [SerializeField, Min(0f)] private float intervaloDaPoeira = 0.22f;

    /// <summary>Pixels por unidade da poeira: a nuvem (uns 30 px) fica com ~0.75 unidade.</summary>
    private const float PixelsDaPoeira = 40f;

    private struct Copia
    {
        public SpriteRenderer Desenho;
        public float Nasceu;
    }

    [Tooltip("Comprimento da sombra de aviso no chao")]
    [SerializeField, Min(0.5f)] private float comprimentoDaSombra = 3.5f;

    private static Sprite spriteDaSombra;

    private SpriteRenderer corpo;
    private SpriteRenderer sombra;
    private float raio = 0.5f;
    private readonly List<Copia> copias = new List<Copia>();
    private float proximaCopia;
    private float proximaPoeira;

    /// <summary>Enquanto true, deixa copias do sprite para tras.</summary>
    public bool Ligado { get; set; }

    /// <summary>Pega (ou poe) o rastro no chefe. <paramref name="raioDoCorpo"/> acha os pes.</summary>
    public static RastroDoChefe Em(MonoBehaviour chefe, SpriteRenderer corpoDoChefe, float raioDoCorpo)
    {
        if (!chefe.TryGetComponent(out RastroDoChefe rastro))
            rastro = chefe.gameObject.AddComponent<RastroDoChefe>();

        rastro.corpo = corpoDoChefe;
        rastro.raio = raioDoCorpo > 0f ? raioDoCorpo : 0.5f;
        return rastro;
    }

    /// <summary>Uma nuvem de poeira nos pes, um pouco atras de <paramref name="rumo"/>. Ignora se veio cedo demais.</summary>
    public void Poeira(Vector2 rumo, bool forcar = false)
    {
        if (!forcar && Time.time < proximaPoeira)
            return;

        proximaPoeira = Time.time + intervaloDaPoeira;

        Vector2 pes = (Vector2)transform.position + new Vector2(0f, -raio * 0.7f) - rumo.normalized * raio * 0.5f;
        EfeitoDeQuadros poeira = EfeitoDeQuadros.Criar(ArteImportada.Poeira(PixelsDaPoeira), 24f, pes, OrdemDoCorpo - 1);

        if (poeira != null)
            poeira.Virar(rumo.x < 0f);
    }

    /// <summary>
    /// Sombra de aviso no chao, saindo do chefe para <paramref name="rumo"/> e sumindo na ponta.
    /// <paramref name="t"/> (0 a 1) e o quanto o aviso ja andou: ela engrossa e escurece.
    /// </summary>
    public void MostrarSombra(Vector2 rumo, float t)
    {
        if (rumo == Vector2.zero)
            return;

        if (sombra == null)
        {
            GameObject obj = new GameObject("Sombra da arrancada");
            obj.transform.SetParent(transform, false);
            sombra = obj.AddComponent<SpriteRenderer>();
            sombra.sprite = SpriteDaSombra();
            sombra.sortingOrder = -8;   // no chao: abaixo de tudo que anda, acima do piso
        }

        rumo.Normalize();
        float largura = raio * Mathf.Lerp(0.8f, 1.5f, t);
        sombra.enabled = true;
        sombra.transform.localPosition = rumo * (comprimentoDaSombra * 0.5f);
        sombra.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(rumo.y, rumo.x) * Mathf.Rad2Deg);

        // O chefe pode estar escalado (incha, encolhe): a sombra compensa pra ter o tamanho certo no mundo.
        Vector3 escala = transform.lossyScale;
        Vector2 tamanho = sombra.sprite.bounds.size;
        sombra.transform.localScale = new Vector3(comprimentoDaSombra / (tamanho.x * Mathf.Max(0.01f, Mathf.Abs(escala.x))),
                                                  largura / (tamanho.y * Mathf.Max(0.01f, Mathf.Abs(escala.y))), 1f);
        sombra.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.15f, 0.4f, t));
    }

    public void EsconderSombra()
    {
        if (sombra != null)
            sombra.enabled = false;
    }

    /// <summary>Faixa escura no comeco no comeco, apagando ate sumir na ponta, com as bordas macias.</summary>
    private static Sprite SpriteDaSombra()
    {
        if (spriteDaSombra != null)
            return spriteDaSombra;

        const int largura = 64;
        const int altura = 16;
        Texture2D textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < largura; x++)
        {
            float aoLongo = 1f - x / (float)(largura - 1);
            aoLongo *= aoLongo;

            for (int y = 0; y < altura; y++)
            {
                float doMeio = Mathf.Abs(y - (altura - 1) * 0.5f) / ((altura - 1) * 0.5f);
                float borda = 1f - doMeio * doMeio;
                textura.SetPixel(x, y, new Color(1f, 1f, 1f, aoLongo * borda));
            }
        }

        textura.Apply();
        spriteDaSombra = Sprite.Create(textura, new Rect(0, 0, largura, altura), new Vector2(0.5f, 0.5f), largura, 0, SpriteMeshType.FullRect);
        spriteDaSombra.name = "Sombra da arrancada";
        return spriteDaSombra;
    }

    private int OrdemDoCorpo => corpo != null ? corpo.sortingOrder : 10;

    private void OnDisable()
    {
        Ligado = false;
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

    private void Update()
    {
        if (Ligado && corpo != null && corpo.sprite != null && Time.time >= proximaCopia)
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
        Transform origem = corpo.transform;
        GameObject obj = new GameObject("Rastro do chefe");
        obj.transform.position = origem.position;
        obj.transform.rotation = origem.rotation;
        obj.transform.localScale = origem.lossyScale;

        SpriteRenderer desenho = obj.AddComponent<SpriteRenderer>();
        desenho.sprite = corpo.sprite;
        desenho.flipX = corpo.flipX;
        desenho.color = corDoRastro;
        desenho.sortingOrder = OrdemDoCorpo - 1;

        copias.Add(new Copia { Desenho = desenho, Nasceu = Time.time });
    }
}
