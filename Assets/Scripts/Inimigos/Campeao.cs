using UnityEngine;

/// <summary>O poder extra de um campeao (<see cref="Campeao"/>).</summary>
public enum TipoDeCampeao
{
    /// <summary>Ataca mais rapido e solta mais tiros por vez. Vermelho.</summary>
    Furioso,

    /// <summary>Deixa um rastro de fogo por onde anda (fere o jogador). Laranja.</summary>
    Flamejante,

    /// <summary>De tempos em tempos levanta um escudo que segura todos os golpes. Azul.</summary>
    Blindado,

    /// <summary>Anda bem mais rapido. Amarelo.</summary>
    Veloz,

    /// <summary>Ao morrer, estoura num anel de tiros. Roxo.</summary>
    Estourado,
}

/// <summary>
/// Um inimigo campeao (elite): as vezes um inimigo de sala vem mais forte, sem arte nova. Fica um pouco
/// maior, com uma aura da cor do poder dele e uma luz da mesma cor, tem 2,5 vezes a vida e um poder extra
/// (<see cref="TipoDeCampeao"/>). Ao acordar, o nome aparece em cima dele ("Campeão furioso").
///
/// Morto, solta um premio melhor (<see cref="Premiar"/>): moedas, as vezes chave ou coracao, e um cristal
/// (a moeda das melhorias entre partidas, ver <see cref="Melhorias"/>).
///
/// O <see cref="GeradorDoAndar"/> sorteia quem vira campeao ao montar as salas
/// (<see cref="Partida.ChanceDeCampeao"/>: sobe com o andar e dobra no modo maldito). Chefe nao vira.
///
/// O **campeao-chefe** (<see cref="SortearChefe"/>, as vezes na sala do fim): dois poderes juntos, 6 vezes a
/// vida, maior ainda, e uma barra de vida no alto da tela enquanto esta acordado (<see cref="ChefeAtivo"/>).
/// Morto, solta o premio em dobro e mais cristais.
/// </summary>
[DisallowMultipleComponent]
public class Campeao : MonoBehaviour
{
    private const float VidaVezes = 2.5f;
    private const float Tamanho = 1.2f;
    private const float RecargaDoEscudo = 6.5f;
    private const float DuracaoDoEscudo = 2.2f;

    private static Sprite anel;
    private static DadosDaArma estouro;

    private TipoDeCampeao tipo;
    private TipoDeCampeao? segundo;
    private bool ehChefe;
    private Vida vida;
    private InimigoAtirador atirador;
    private SpriteRenderer aura;
    private SpriteRenderer bolha;
    private FonteDeLuz luz;
    private Vector2 ultimaChama;
    private float escudoAte = -1f;
    private float proximoEscudo;
    private bool avisou;

    public TipoDeCampeao Tipo => tipo;

    /// <summary>E o campeao-chefe da sala do fim (dois poderes e a barra de vida).</summary>
    public bool EhChefe => ehChefe;

    /// <summary>O campeao-chefe acordado e vivo agora (a HUD desenha a barra dele), ou nulo.</summary>
    public static Campeao ChefeAtivo { get; private set; }

    /// <summary>A vida dele (a barra).</summary>
    public Vida Vida => vida;

    /// <summary>"Campeão-chefe furioso e blindado" (ou o nome do campeao comum).</summary>
    public string NomeCompleto => ehChefe && segundo.HasValue
        ? $"Campeão-chefe {Adjetivo(tipo)} e {Adjetivo(segundo.Value)}"
        : Nome(tipo);

    private bool Tem(TipoDeCampeao t) => tipo == t || segundo == t;

    private static string Adjetivo(TipoDeCampeao t) => Nome(t).Substring("Campeão ".Length);

    /// <summary>A cor de cada poder (a aura, a luz e o nome).</summary>
    public static Color Cor(TipoDeCampeao tipo)
    {
        switch (tipo)
        {
            case TipoDeCampeao.Furioso: return new Color(1f, 0.32f, 0.28f);
            case TipoDeCampeao.Flamejante: return new Color(1f, 0.6f, 0.2f);
            case TipoDeCampeao.Blindado: return new Color(0.4f, 0.7f, 1f);
            case TipoDeCampeao.Veloz: return new Color(1f, 0.95f, 0.35f);
            default: return new Color(0.8f, 0.45f, 1f);
        }
    }

    public static string Nome(TipoDeCampeao tipo)
    {
        switch (tipo)
        {
            case TipoDeCampeao.Furioso: return "Campeão furioso";
            case TipoDeCampeao.Flamejante: return "Campeão flamejante";
            case TipoDeCampeao.Blindado: return "Campeão blindado";
            case TipoDeCampeao.Veloz: return "Campeão veloz";
            default: return "Campeão estourado";
        }
    }

    /// <summary>Faz do inimigo um campeao com um poder sorteado (os de andar e atacar so em quem anda e ataca).</summary>
    public static Campeao Sortear(GameObject inimigo)
    {
        if (inimigo == null || inimigo.TryGetComponent(out Chefe _) || inimigo.TryGetComponent(out Campeao _))
            return null;

        bool anda = inimigo.TryGetComponent(out InimigoAtirador _);
        TipoDeCampeao tipo = (TipoDeCampeao)Random.Range(0, 5);

        if (!anda && (tipo == TipoDeCampeao.Furioso || tipo == TipoDeCampeao.Veloz))
            tipo = TipoDeCampeao.Blindado;

        Campeao c = inimigo.AddComponent<Campeao>();
        c.Montar(tipo, null, false);
        return c;
    }

    /// <summary>O campeao-chefe: dois poderes diferentes, sorteados (os de andar e atacar so em quem anda).</summary>
    public static Campeao SortearChefe(GameObject inimigo)
    {
        if (inimigo == null || inimigo.TryGetComponent(out Chefe _) || inimigo.TryGetComponent(out Campeao _))
            return null;

        bool anda = inimigo.TryGetComponent(out InimigoAtirador _);
        System.Collections.Generic.List<TipoDeCampeao> podem = new System.Collections.Generic.List<TipoDeCampeao>
            { TipoDeCampeao.Flamejante, TipoDeCampeao.Blindado, TipoDeCampeao.Estourado };

        if (anda)
        {
            podem.Add(TipoDeCampeao.Furioso);
            podem.Add(TipoDeCampeao.Veloz);
        }

        TipoDeCampeao a = podem[Random.Range(0, podem.Count)];
        podem.Remove(a);
        TipoDeCampeao b = podem[Random.Range(0, podem.Count)];

        Campeao c = inimigo.AddComponent<Campeao>();
        c.Montar(a, b, true);
        return c;
    }

    private void Montar(TipoDeCampeao qual, TipoDeCampeao? outro, bool chefe)
    {
        tipo = qual;
        segundo = outro;
        ehChefe = chefe;
        vida = GetComponent<Vida>();
        atirador = GetComponent<InimigoAtirador>();
        Color cor = Cor(tipo);

        if (vida != null)
            vida.MudarMaxima(vida.Maxima * ((chefe ? 6f : VidaVezes) - 1f));

        transform.localScale *= chefe ? 1.45f : Tamanho;

        // O corpo puxa um pouco pra cor (a furia e o gelo pintam por cima e devolvem esta).
        SpriteRenderer corpo = null;

        foreach (SpriteRenderer d in GetComponentsInChildren<SpriteRenderer>())
        {
            if (corpo == null || d.sortingOrder > corpo.sortingOrder)
                corpo = d;
        }

        if (corpo != null)
            corpo.color = Color.Lerp(corpo.color, new Color(cor.r, cor.g, cor.b, corpo.color.a), 0.3f);

        // A aura: um anel que pulsa atras do corpo, e uma luz da mesma cor (que acende o anel no escuro).
        GameObject obj = new GameObject("Aura do campeao");
        obj.transform.SetParent(transform, false);
        aura = obj.AddComponent<SpriteRenderer>();
        aura.sprite = Anel();
        aura.color = new Color(cor.r, cor.g, cor.b, 0.55f);
        aura.sortingOrder = corpo != null ? corpo.sortingOrder - 1 : 0;
        luz = Iluminacao.Luz(transform, Vector2.zero, cor, 2.4f, 0.9f, 0.05f);

        // O segundo poder do chefe tambem pinta: uma segunda aura, menor, da cor dele.
        if (segundo.HasValue)
        {
            Color cor2 = Cor(segundo.Value);
            GameObject obj2 = new GameObject("Segunda aura");
            obj2.transform.SetParent(transform, false);
            obj2.transform.localScale = Vector3.one * 0.75f;
            auraDoSegundo = obj2.AddComponent<SpriteRenderer>();
            auraDoSegundo.sprite = Anel();
            auraDoSegundo.color = new Color(cor2.r, cor2.g, cor2.b, 0.5f);
            auraDoSegundo.sortingOrder = aura.sortingOrder;
        }

        foreach (TipoDeCampeao t in segundo.HasValue ? new[] { tipo, segundo.Value } : new[] { tipo })
            Poder(t, corpo);

        ultimaChama = transform.position;
    }

    private SpriteRenderer auraDoSegundo;

    private void Poder(TipoDeCampeao qual, SpriteRenderer corpo)
    {
        switch (qual)
        {
            case TipoDeCampeao.Furioso:
                atirador.RitmoExtra = 1.35f;
                atirador.MaisTiros(2);
                break;

            case TipoDeCampeao.Veloz:
                atirador.VelocidadeExtra = ehChefe ? 1.45f : 1.75f;
                break;

            case TipoDeCampeao.Blindado:
            {
                GameObject b = new GameObject("Escudo do campeao");
                b.transform.SetParent(transform, false);
                b.transform.localScale = Vector3.one * 1.5f;
                bolha = b.AddComponent<SpriteRenderer>();
                bolha.sprite = Anel();
                bolha.sortingOrder = (corpo != null ? corpo.sortingOrder : 0) + 2;
                bolha.enabled = false;
                Iluminacao.Brilhar(bolha);
                proximoEscudo = Time.time + Random.Range(1.5f, 3f);

                if (vida != null)
                    vida.Escudo = _ => Time.time < escudoAte;
                break;
            }

            case TipoDeCampeao.Estourado:
                if (vida != null)
                    vida.AoMorrer += Estourar;
                break;
        }
    }

    private void OnDestroy()
    {
        if (vida != null)
            vida.AoMorrer -= Estourar;

        if (ChefeAtivo == this)
            ChefeAtivo = null;
    }

    private void Update()
    {
        if (vida == null || vida.Morto)
        {
            if (aura != null)
                aura.enabled = false;

            if (bolha != null)
                bolha.enabled = false;

            if (luz != null)
                luz.enabled = false;

            if (auraDoSegundo != null)
                auraDoSegundo.enabled = false;

            if (ChefeAtivo == this)
                ChefeAtivo = null;

            return;
        }

        if (auraDoSegundo != null)
        {
            float p2 = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f + 1.6f);
            auraDoSegundo.transform.localScale = Vector3.one * (0.75f + 0.12f * p2);
        }

        float pulso = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
        aura.transform.localScale = Vector3.one * (1.1f + 0.15f * pulso);
        Color cor = Cor(tipo);
        aura.color = new Color(cor.r, cor.g, cor.b, 0.35f + 0.3f * pulso);

        // Ao acordar, o nome em cima dele (uma vez).
        if (!avisou && (atirador == null || atirador.Acordado))
        {
            avisou = true;
            TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.5f, NomeCompleto, cor, 1.4f);

            if (ehChefe)
            {
                ChefeAtivo = this;
                CameraDoJogo.Tremer(0.2f, 0.4f);
            }
            Sons.Tocar(Som.Campeao, 0.6f);
            DicasDaPrimeiraVez.Mostrar("campeao");
        }

        if (Tem(TipoDeCampeao.Flamejante) && Vector2.Distance(transform.position, ultimaChama) > 0.8f)
        {
            ultimaChama = transform.position;
            ChamaNoChao.Criar(ultimaChama, GeradorDoAndar.Raiz, gameObject);
        }

        if (Tem(TipoDeCampeao.Blindado))
            Escudar();
    }

    // O escudo sobe sozinho de tempos em tempos (so acordado): a bolha azul, e o tiro bate e some.
    private void Escudar()
    {
        bool acordado = atirador == null || atirador.Acordado;

        if (acordado && Time.time >= proximoEscudo)
        {
            escudoAte = Time.time + DuracaoDoEscudo;
            proximoEscudo = escudoAte + RecargaDoEscudo;
            Sons.Tocar(Som.Escudo, 0.6f);
        }

        bool ligado = Time.time < escudoAte;
        bolha.enabled = ligado;

        if (ligado)
        {
            // Pisca no fim, pra avisar que vai cair.
            float falta = escudoAte - Time.time;
            float a = falta < 0.5f ? (Mathf.Sin(Time.time * 40f) > 0f ? 0.8f : 0.2f) : 0.75f;
            bolha.color = new Color(0.55f, 0.8f, 1f, a);
        }
    }

    // O estouro: um anel de bolas roxas, como a Gosma (desvia andando pra fora, nao atirando de perto).
    private void Estourar()
    {
        if (estouro == null)
        {
            DadosDaArma fogo = Resources.Load<DadosDaArma>("Armadilhas/FogoDaEstatua");

            if (fogo == null)
                return;

            estouro = Instantiate(fogo);
            estouro.name = "Estouro do campeao";
            estouro.anel = true;
            estouro.tirosPorDisparo = 12;
            estouro.dispersao = 0f;
            estouro.velocidade = Mathf.Min(fogo.velocidade, 6f);
            estouro.alcance = 7f;
            estouro.cor = new Color(0.85f, 0.5f, 1f);
        }

        estouro.Disparar(transform.position, Random.insideUnitCircle.normalized, gameObject, Lado.Inimigos);
        Sons.Tocar(Som.Explosao, 0.5f);
    }

    /// <summary>O premio de um campeao morto (o do chefe: em dobro, um coracao e mais dois cristais).</summary>
    public void Premiar(Vector2 onde)
    {
        Premiar(onde, ehChefe);
    }

    /// <summary>O premio de um campeao morto: moedas, as vezes chave ou coracao, e um cristal (dois no maldito).</summary>
    public static void Premiar(Vector2 onde, bool chefe)
    {
        if (chefe)
        {
            Premiar(onde, false);
            Coletavel.Criar(TipoDeColetavel.Coracao, onde + Vector2.right);
            Coletavel.Criar(TipoDeColetavel.Cristal, onde + Vector2.up, 2 * Partida.CristaisVezes);
        }

        for (int i = 0; i < 3; i++)
            Coletavel.Criar(TipoDeColetavel.Moeda, onde + Random.insideUnitCircle * 0.5f);

        float sorte = Random.value;

        if (sorte < 0.2f)
            Coletavel.Criar(TipoDeColetavel.Chave, onde);
        else if (sorte < 0.4f)
            Coletavel.Criar(TipoDeColetavel.Coracao, onde);

        Coletavel.Criar(TipoDeColetavel.Cristal, onde, Partida.CristaisVezes);
    }

    // Um anel redondo e macio, branco (pintado pela cor do campeao), de 1,2 unidade.
    private static Sprite Anel()
    {
        if (anel != null)
            return anel;

        const int lado = 48;
        Texture2D t = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        Color32[] px = new Color32[lado * lado];
        float meio = (lado - 1) * 0.5f;

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float d = Mathf.Sqrt((x - meio) * (x - meio) + (y - meio) * (y - meio)) / meio;
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.14f);
                a = Mathf.Max(a, d < 0.82f ? 0.18f * d : 0f);
                px[y * lado + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        t.SetPixels32(px);
        t.Apply();
        anel = Sprite.Create(t, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), 40f);
        return anel;
    }
}

/// <summary>
/// Uma chama no chao, do rastro do campeao flamejante: queima uns segundos e some. Fere so o jogador
/// (meio coracao, com o tempinho sem dano de sempre).
/// </summary>
public class ChamaNoChao : MonoBehaviour
{
    private const float Duracao = 2.6f;
    private const float Raio = 0.42f;

    private static Sprite[] quadros;

    private SpriteRenderer desenho;
    private GameObject dono;
    private float nasceu;
    private float proximaOlhada;

    public static ChamaNoChao Criar(Vector2 onde, Transform pai, GameObject dono)
    {
        if (quadros == null)
            quadros = ArteDoAntigo.ChamaMagica;

        if (quadros == null || quadros.Length == 0)
            return null;

        GameObject obj = new GameObject("Chama no chao");
        obj.transform.SetParent(pai, true);
        obj.transform.position = onde + new Vector2(0f, -0.3f);

        ChamaNoChao c = obj.AddComponent<ChamaNoChao>();
        c.desenho = obj.AddComponent<SpriteRenderer>();
        c.desenho.sprite = quadros[0];
        c.desenho.color = new Color(1f, 0.6f, 0.25f);
        c.desenho.sortingOrder = Pedreiro.OrdemDosEnfeites + 3;
        Iluminacao.Brilhar(c.desenho);
        obj.AddComponent<EnfeiteAnimado>().Comecar(quadros, 12f);
        c.dono = dono;
        c.nasceu = Time.time;
        return c;
    }

    private void Update()
    {
        float idade = Time.time - nasceu;

        if (idade >= Duracao)
        {
            Destroy(gameObject);
            return;
        }

        // Nasce crescendo e some encolhendo.
        float tamanho = Mathf.Min(1f, idade / 0.2f) * Mathf.Min(1f, (Duracao - idade) / 0.5f);
        transform.localScale = Vector3.one * Mathf.Max(0.05f, tamanho);

        if (Time.time < proximaOlhada || tamanho < 0.6f)
            return;

        proximaOlhada = Time.time + 0.1f;

        foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, Raio))
        {
            Vida vida = c != null ? c.GetComponentInParent<Vida>() : null;

            if (vida != null && vida.Lado == Lado.Jogador && !vida.Morto)
            {
                Vector2 rumo = (Vector2)vida.transform.position - (Vector2)transform.position;
                vida.ReceberDano(new Dano(1f, rumo.sqrMagnitude > 0.001f ? rumo.normalized : Vector2.up, 4f, dono));
                break;
            }
        }
    }
}
