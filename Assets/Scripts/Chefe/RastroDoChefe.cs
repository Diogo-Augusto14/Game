using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O que se ve quando um chefe arranca (investida, chifrada): poeira do Tiny Swords nos pes
/// enquanto ele se prepara e quando sai, e copias apagadas do proprio sprite que ficam para
/// tras e somem rapido (o rastro). Igual ao dash do heroi, so que sem cor: nada de faixa vermelha.
/// O aviso de pra onde ele vai e a linha de mira no chao, ate a parede: nasce fina e amarela e
/// vai engrossando e ficando vermelha conforme o golpe chega.
/// O chefe liga e desliga com <see cref="Ligado"/>, pede poeira com <see cref="Poeira"/> e mostra
/// o aviso com <see cref="MostrarMira"/>.
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

    [Tooltip("Comprimento maximo da linha de mira (ela para antes, na parede)")]
    [SerializeField, Min(0.5f)] private float alcanceDaMira = 14f;

    [SerializeField] private Color corDoComeco = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color corDoPerigo = new Color(1f, 0.15f, 0.1f);

    private static Sprite spriteDaMira;

    private SpriteRenderer corpo;
    private SpriteRenderer mira;
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
    /// Linha de mira no chao, saindo do chefe para <paramref name="rumo"/> ate a parede.
    /// <paramref name="t"/> (0 a 1) e o quanto o aviso ja andou: de fina e amarela a grossa e vermelha.
    /// </summary>
    public void MostrarMira(Vector2 rumo, float t)
    {
        if (rumo == Vector2.zero)
            return;

        if (mira == null)
        {
            GameObject obj = new GameObject("Mira da arrancada");
            obj.transform.SetParent(transform, false);
            mira = obj.AddComponent<SpriteRenderer>();
            mira.sprite = SpriteDaMira();
            mira.sortingOrder = -8;   // no chao: abaixo das portas e de tudo que anda, acima do piso
        }

        rumo.Normalize();
        t = Mathf.Clamp01(t);

        // Para na parede, em vez de atravessar a sala e as portas.
        RaycastHit2D parede = Physics2D.Raycast(transform.position, rumo, alcanceDaMira, Camadas.MascaraDeParede);
        float comprimento = parede.collider != null ? parede.distance : alcanceDaMira;
        float largura = Mathf.Lerp(0.12f, raio * 1.4f, t * t);

        mira.enabled = true;
        mira.transform.localRotation = Quaternion.identity;
        mira.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(rumo.y, rumo.x) * Mathf.Rad2Deg);
        mira.transform.position = transform.position + (Vector3)(rumo * (comprimento * 0.5f));

        // O chefe pode estar escalado (incha, encolhe): a linha compensa pra ter o tamanho certo no mundo.
        Vector3 escala = transform.lossyScale;
        Vector2 tamanho = mira.sprite.bounds.size;
        mira.transform.localScale = new Vector3(comprimento / (tamanho.x * Mathf.Max(0.01f, Mathf.Abs(escala.x))),
                                                largura / (tamanho.y * Mathf.Max(0.01f, Mathf.Abs(escala.y))), 1f);

        // Amarelo -> vermelho, ficando mais forte; no fim pisca de leve.
        Color cor = Color.Lerp(corDoComeco, corDoPerigo, t);
        float pisca = t > 0.75f ? 0.85f + 0.15f * Mathf.Sin(Time.time * 40f) : 1f;
        cor.a = Mathf.Lerp(0.3f, 0.6f, t) * pisca;
        mira.color = cor;
    }

    public void EsconderMira()
    {
        if (mira != null)
            mira.enabled = false;
    }

    /// <summary>Faixa com as bordas macias e a ponta apagando um pouco, pra nao acabar seca.</summary>
    private static Sprite SpriteDaMira()
    {
        if (spriteDaMira != null)
            return spriteDaMira;

        const int largura = 64;
        const int altura = 16;
        Texture2D textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < largura; x++)
        {
            float u = x / (float)(largura - 1);
            float aoLongo = Mathf.Clamp01(u / 0.08f) * Mathf.Clamp01((1f - u) / 0.15f);

            for (int y = 0; y < altura; y++)
            {
                float doMeio = Mathf.Abs(y - (altura - 1) * 0.5f) / ((altura - 1) * 0.5f);
                float borda = Mathf.Clamp01((1f - doMeio) / 0.45f);
                float miolo = doMeio < 0.3f ? 1f : 0.75f;   // o meio um pouco mais forte
                textura.SetPixel(x, y, new Color(1f, 1f, 1f, aoLongo * borda * miolo));
            }
        }

        textura.Apply();
        spriteDaMira = Sprite.Create(textura, new Rect(0, 0, largura, altura), new Vector2(0.5f, 0.5f), largura, 0, SpriteMeshType.FullRect);
        spriteDaMira.name = "Mira da arrancada";
        return spriteDaMira;
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
