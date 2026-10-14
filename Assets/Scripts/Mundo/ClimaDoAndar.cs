using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O clima de cada andar (1.19.0), alem da musica e do escuro:
///
///   o som de fundo do mundo em loop (porao umido, vento nos tuneis das catacumbas, zumbido de pedra na cripta,
///   rumor da montanha nas profundezas, vento nas folhas na area segura), e de vez em quando, longe e de um
///   lado, uma coisa do mundo: gota pingando, corrente balancando, madeira rangendo, um gemido, uma rajada
///   de vento, pedrinhas caindo (cada mundo com as suas, ver <see cref="Eventos"/>);
///   poeira flutuando no ar e nevoa passando devagar rente ao chao: ficam embaixo do veu do escuro, entao so
///   aparecem perto das tochas e do heroi (a poeira brilhando na luz);
///   as bordas da tela mais escuras (a vinheta).
///
/// Os sons vem de <c>Resources/Ambiente</c> (feitos pelo Ferramentas/Audio/ambiente.py) e passam pelo volume
/// dos efeitos. Fica no mesmo objeto do <see cref="GeradorDoAndar"/>, que chama <see cref="Mudar"/> a cada
/// andar novo.
/// </summary>
[DisallowMultipleComponent]
public class ClimaDoAndar : MonoBehaviour
{
    private const int QuantaPoeira = 40;
    private const int QuantaNevoa = 7;
    private const float VolumeDoFundo = 0.55f;
    private const float TempoDaTroca = 2f;

    // Ordem no desenho: logo abaixo do veu do escuro (32000), por cima do resto do mundo.
    private const int OrdemDaNevoa = 31900;
    private const int OrdemDaPoeira = 31950;

    private static readonly string[] Fundos = { "Porao", "Catacumbas", "Cripta", "Profundezas" };

    // O que pode acontecer em cada mundo e quanto sai (0 Porao, 1 Catacumbas, 2 Cripta, 3 Profundezas).
    private static readonly (string nome, float peso)[][] Eventos =
    {
        new[] { ("Gota", 3f), ("Rangido", 2f), ("Corrente", 2f), ("Gemido", 1f), ("Pedrinhas", 1f) },
        new[] { ("Gota", 2f), ("Vento", 3f), ("Gemido", 2f), ("Corrente", 1f), ("Pedrinhas", 1f) },
        new[] { ("Gemido", 3f), ("Corrente", 2f), ("Vento", 1f), ("Rangido", 1f), ("Gota", 1f) },
        new[] { ("Pedrinhas", 3f), ("Gota", 2f), ("Vento", 2f), ("Gemido", 1f) },
    };

    // A cor da nevoa e da poeira de cada mundo.
    private static readonly Color[] CorDaNevoa =
    {
        new Color(0.75f, 0.68f, 0.6f), new Color(0.6f, 0.7f, 0.85f), new Color(0.6f, 0.8f, 0.75f), new Color(0.65f, 0.85f, 0.7f),
    };

    private class Particula
    {
        public SpriteRenderer desenho;
        public Vector2 velocidade;
        public float fase;
        public float alfa;
    }

    private AudioSource fundo;
    private AudioSource fundoSaindo;
    private float trocouEm = -10f;
    private readonly AudioSource[] longe = new AudioSource[3];
    private int proximaFonte;
    private float proximoEvento;
    private int tema = -1;
    private bool descanso;
    private bool montado;

    private Transform raizDasParticulas;
    private readonly List<Particula> poeira = new List<Particula>();
    private readonly List<Particula> nevoa = new List<Particula>();
    private Camera cam;

    private static Texture2D vinheta;
    private static Sprite pontinho;
    private static Sprite nuvem;
    private static readonly Dictionary<string, AudioClip[]> clips = new Dictionary<string, AudioClip[]>();

    /// <summary>Um andar novo: troca o som de fundo, os eventos, a nevoa e a poeira pro mundo dele.</summary>
    public void Mudar(int temaDoMundo, bool areaSegura)
    {
        Montar();
        tema = Mathf.Clamp(temaDoMundo, 0, Fundos.Length - 1);
        descanso = areaSegura;
        proximoEvento = Time.time + Random.Range(2f, 5f);

        AudioClip novo = Clip("Base_" + (descanso ? "Descanso" : Fundos[tema]));

        if (fundo.clip != novo)
        {
            // O de antes some aos poucos enquanto o novo entra.
            (fundo, fundoSaindo) = (fundoSaindo, fundo);
            fundo.clip = novo;
            fundo.volume = 0f;

            // Cada andar comeca o loop num ponto diferente.
            if (novo != null)
            {
                fundo.Play();
                fundo.time = Random.Range(0f, novo.length * 0.9f);
            }

            trocouEm = Time.unscaledTime;
        }

        Color cor = descanso ? new Color(1f, 0.95f, 0.8f) : CorDaNevoa[tema];

        foreach (Particula p in nevoa)
        {
            p.alfa = descanso ? 0f : Random.Range(0.07f, 0.13f);
            p.desenho.color = new Color(cor.r, cor.g, cor.b, p.alfa);
        }

        foreach (Particula p in poeira)
        {
            p.alfa = Random.Range(0.25f, 0.6f) * (descanso ? 0.5f : 1f);
            p.desenho.color = new Color(Mathf.Lerp(cor.r, 1f, 0.6f), Mathf.Lerp(cor.g, 0.92f, 0.6f), Mathf.Lerp(cor.b, 0.8f, 0.6f), p.alfa);
        }
    }

    private void Montar()
    {
        if (montado)
            return;

        montado = true;
        fundo = NovaFonte(true);
        fundoSaindo = NovaFonte(true);

        for (int i = 0; i < longe.Length; i++)
            longe[i] = NovaFonte(false);

        raizDasParticulas = new GameObject("Clima (poeira e nevoa)").transform;
        raizDasParticulas.SetParent(transform, false);

        for (int i = 0; i < QuantaNevoa; i++)
        {
            Particula p = Nova(Nuvem(), OrdemDaNevoa, Random.Range(6f, 11f));
            p.velocidade = new Vector2(Random.Range(0.12f, 0.3f) * (Random.value < 0.5f ? -1f : 1f), Random.Range(-0.03f, 0.03f));
            nevoa.Add(p);
        }

        for (int i = 0; i < QuantaPoeira; i++)
        {
            Particula p = Nova(Pontinho(), OrdemDaPoeira, Random.value < 0.25f ? 2f : 1f);
            p.velocidade = new Vector2(Random.Range(-0.12f, 0.12f), Random.Range(-0.1f, 0.04f));
            poeira.Add(p);
        }
    }

    private AudioSource NovaFonte(bool emLoop)
    {
        AudioSource a = gameObject.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.loop = emLoop;
        a.spatialBlend = 0f;
        a.volume = 0f;
        return a;
    }

    private Particula Nova(Sprite desenho, int ordem, float escala)
    {
        GameObject obj = new GameObject("Particula");
        obj.transform.SetParent(raizDasParticulas, false);
        obj.transform.localScale = Vector3.one * escala;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = desenho;
        sr.sortingOrder = ordem;
        sr.color = new Color(1f, 1f, 1f, 0f);
        return new Particula { desenho = sr, fase = Random.value * 100f, alfa = 0f };
    }

    private void Update()
    {
        if (!montado)
            return;

        AtualizarSom();
        AtualizarParticulas();
    }

    private void AtualizarSom()
    {
        // No menu inicial o andar 1 esta montado por baixo, mas o ambiente fica calado.
        float alvo = TelaDeInicio.Aberta ? 0f : VolumeDoFundo * (descanso ? 0.7f : 1f);
        float t = Mathf.Clamp01((Time.unscaledTime - trocouEm) / TempoDaTroca);
        fundo.volume = Mathf.MoveTowards(fundo.volume, alvo * t, Time.unscaledDeltaTime * 0.5f);
        fundoSaindo.volume = Mathf.MoveTowards(fundoSaindo.volume, 0f, Time.unscaledDeltaTime * 0.5f);

        if (fundoSaindo.volume <= 0f && fundoSaindo.isPlaying)
            fundoSaindo.Stop();

        if (tema < 0 || TelaDeInicio.Aberta || Time.time < proximoEvento)
            return;

        // Na area segura, so uma rajada de vento de vez em quando.
        proximoEvento = Time.time + (descanso ? Random.Range(12f, 22f) : Random.Range(3.5f, 9f));
        string qual = descanso ? "Vento" : Sortear(Eventos[tema]);
        AudioClip clip = Clip(qual);

        if (clip == null)
            return;

        AudioSource fonte = longe[proximaFonte];
        proximaFonte = (proximaFonte + 1) % longe.Length;
        fonte.panStereo = Random.Range(-0.8f, 0.8f);
        fonte.pitch = Random.Range(0.88f, 1.1f);
        fonte.PlayOneShot(clip, Random.Range(0.45f, 0.8f) * (descanso ? 0.5f : 1f));
    }

    private static string Sortear((string nome, float peso)[] lista)
    {
        float total = 0f;

        foreach (var e in lista)
            total += e.peso;

        float ponto = Random.value * total;

        foreach (var e in lista)
        {
            ponto -= e.peso;

            if (ponto <= 0f)
                return e.nome;
        }

        return lista[lista.Length - 1].nome;
    }

    // Um dos arquivos com esse nome (Gota_1, Gota_2...), ou o proprio (Base_Porao).
    private static AudioClip Clip(string nome)
    {
        if (!clips.TryGetValue(nome, out AudioClip[] achados))
        {
            List<AudioClip> lista = new List<AudioClip>();
            AudioClip unico = Resources.Load<AudioClip>("Ambiente/" + nome);

            if (unico != null)
                lista.Add(unico);

            for (int i = 1; i <= 4; i++)
            {
                AudioClip c = Resources.Load<AudioClip>($"Ambiente/{nome}_{i}");

                if (c != null)
                    lista.Add(c);
            }

            achados = lista.ToArray();
            clips[nome] = achados;
        }

        return achados.Length > 0 ? achados[Random.Range(0, achados.Length)] : null;
    }

    // A poeira e a nevoa ficam no mundo e andam devagar; quem sai da volta da camera entra do outro lado.
    private void AtualizarParticulas()
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        float altura = cam.orthographicSize + 2f;
        float largura = altura * cam.aspect + 2f;
        Vector2 meio = cam.transform.position;
        float t = Time.time;

        foreach (Particula p in nevoa)
            Mover(p, meio, largura + 5f, altura + 5f, t, 0.15f);

        foreach (Particula p in poeira)
        {
            Mover(p, meio, largura, altura, t, 0.25f);

            // A poeira pisca de leve, como se virasse na luz.
            Color c = p.desenho.color;
            c.a = p.alfa * (0.6f + 0.4f * Mathf.Sin(t * 1.7f + p.fase));
            p.desenho.color = c;
        }
    }

    private static void Mover(Particula p, Vector2 meio, float largura, float altura, float t, float balanco)
    {
        Transform tr = p.desenho.transform;
        Vector2 pos = tr.position;
        pos += (p.velocidade + new Vector2(Mathf.Sin(t * 0.5f + p.fase), Mathf.Cos(t * 0.37f + p.fase)) * balanco) * Time.deltaTime;

        // Longe demais (o jogador andou ou foi pra outro andar): reaparece do outro lado da camera.
        Vector2 d = pos - meio;

        if (Mathf.Abs(d.x) > largura)
            pos.x = meio.x - Mathf.Sign(d.x) * largura + Random.Range(-0.5f, 0.5f);

        if (Mathf.Abs(d.y) > altura)
            pos.y = meio.y - Mathf.Sign(d.y) * altura + Random.Range(-0.5f, 0.5f);

        if (Mathf.Abs(d.x) > largura * 3f || Mathf.Abs(d.y) > altura * 3f)
            pos = meio + new Vector2(Random.Range(-largura, largura), Random.Range(-altura, altura));

        tr.position = pos;
    }

    // ---------------- a vinheta ----------------
    private void OnGUI()
    {
        // Embaixo de tudo do OnGUI (a seta do andar e a HUD ficam por cima) e nunca por cima dos menus.
        GUI.depth = 20;

        if (!montado || TelaDoJogo.MenuAberto || Event.current.type != EventType.Repaint)
            return;

        if (vinheta == null)
            vinheta = FazerVinheta();

        GUI.color = new Color(1f, 1f, 1f, descanso ? 0.45f : 1f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), vinheta);
        GUI.color = Color.white;
    }

    // Transparente no meio e escurecendo nas bordas (mais nos cantos).
    private static Texture2D FazerVinheta()
    {
        const int lado = 128;
        Texture2D t = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        Color32[] px = new Color32[lado * lado];

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float dx = (x + 0.5f) / lado * 2f - 1f, dy = (y + 0.5f) / lado * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, d)) * 0.7f;
                px[y * lado + x] = new Color32(0, 0, 0, (byte)(a * 255f));
            }
        }

        t.SetPixels32(px);
        t.Apply();
        return t;
    }

    // ---------------- os desenhos ----------------
    private static Sprite Pontinho()
    {
        if (pontinho != null)
            return pontinho;

        Texture2D t = new Texture2D(1, 1) { filterMode = FilterMode.Point };
        t.SetPixel(0, 0, Color.white);
        t.Apply();
        pontinho = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 32f);
        return pontinho;
    }

    // Uma mancha redonda e macia (a nevoa), de 1 unidade.
    private static Sprite Nuvem()
    {
        if (nuvem != null)
            return nuvem;

        const int lado = 64;
        Texture2D t = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        Color32[] px = new Color32[lado * lado];

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float dx = (x + 0.5f) / lado * 2f - 1f, dy = (y + 0.5f) / lado * 2f - 1f;
                float a = Mathf.Exp(-(dx * dx + dy * dy * 2.2f) * 3f);
                px[y * lado + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        t.SetPixels32(px);
        t.Apply();
        nuvem = Sprite.Create(t, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), lado);
        return nuvem;
    }
}
