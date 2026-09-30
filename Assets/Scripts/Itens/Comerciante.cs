using UnityEngine;

/// <summary>
/// O comerciante da loja: um homem de chapeu de aba larga e casaco (2D Pixel Dungeon Asset
/// Pack v2.0) atras de um balcao, com saco de ouro, barril e placa de LOJA. Nao e inimigo:
/// nao tem vida nem colisor, so enfeita e conversa.
///
/// Parado ele respira, vira pro lado do jogador e de vez em quando da um pulinho. Fala num
/// balao: cumprimenta quando o jogador chega, agradece a compra e avisa quando falta moeda.
/// Monte por <see cref="Criar"/>.
/// </summary>
public class Comerciante : MonoBehaviour
{
    private static readonly string[] Saudacoes =
    {
        "Bem-vindo, heroi!",
        "Tenho coisas boas hoje!",
        "Olha so que belezas!",
        "Entre, entre! Pode olhar.",
    };

    private static readonly string[] Agradecimentos =
    {
        "Obrigado!",
        "Otima escolha!",
        "Volte sempre!",
        "Bom proveito!",
    };

    /// <summary>Quanto o pe do comerciante fica acima do pe do balcao (so as pernas ficam atras dele).</summary>
    private const float AlturaDoCorpo = 0.3f;

    private Transform corpo;
    private SpriteRenderer desenho;
    private Transform jogador;
    private Transform sala;
    private TextMesh textoDoBalao;
    private SpriteRenderer fundoDoBalao;
    private float fimDoBalao;
    private float proximoPulo;
    private float inicioDoPulo = -10f;
    private bool cumprimentou;

    public static Comerciante Criar(Vector2 pe, Transform sala)
    {
        GameObject obj = new GameObject("Comerciante");
        obj.transform.SetParent(sala, true);
        obj.transform.position = pe;

        Comerciante c = obj.AddComponent<Comerciante>();
        c.sala = sala;
        c.Montar();
        return c;
    }

    private void Montar()
    {
        // O corpo (so ele respira e pula); o resto fica parado.
        corpo = new GameObject("Corpo").transform;
        corpo.SetParent(transform, false);

        Sprite pessoa = ArteImportada.Comerciante;

        if (pessoa != null)
        {
            // Grande e acima do balcao: o balcao so cobre as pernas, o corpo todo aparece.
            desenho = FormasDaSala.Desenho(corpo, "Desenho", pessoa, Color.white, new Vector2(0f, AlturaDoCorpo), Vector2.one * 1.5f, 8);
        }
        else
        {
            // Sem o pacote: um bonequinho de chapeu, de cor de gente (nada de olho brilhando).
            desenho = FormasDaSala.Desenho(corpo, "Corpo", FormasDaSala.Circulo(), new Color(0.45f, 0.3f, 0.2f), new Vector2(0f, 0.4f), Vector2.one * 0.7f, 8);
            FormasDaSala.Desenho(corpo, "Rosto", FormasDaSala.Circulo(), new Color(0.95f, 0.75f, 0.55f), new Vector2(0f, 0.8f), Vector2.one * 0.4f, 9);
            FormasDaSala.Desenho(corpo, "Chapeu", FormasDaSala.Quadrado(), new Color(0.35f, 0.22f, 0.12f), new Vector2(0f, 1f), new Vector2(0.7f, 0.12f), 10);
        }

        // Balcao na frente dele (esconde as pernas), com o saco de ouro em cima.
        Sprite balcao = ArteImportada.Objeto(0, 2);

        if (balcao != null)
            FormasDaSala.Desenho(transform, "Balcao", balcao, Color.white, new Vector2(0f, -0.15f), Vector2.one * 1.2f, 9);

        Sprite ouro = ArteImportada.SacoDeOuro;

        if (ouro != null)
            FormasDaSala.Desenho(transform, "Saco de ouro", ouro, Color.white, new Vector2(0.5f, 0.3f), Vector2.one * 0.5f, 10);

        Sprite moeda = ArteImportada.Objeto(3, 3);

        if (moeda != null)
            FormasDaSala.Desenho(transform, "Moedas", moeda, Color.white, new Vector2(-0.45f, 0.2f), Vector2.one * 0.55f, 10);

        // Enfeites de mercador dos lados.
        Sprite barril = ArteImportada.Objeto(4, 4);

        if (barril != null)
            FormasDaSala.Desenho(transform, "Barril", barril, Color.white, new Vector2(-1.5f, 0.1f), Vector2.one * 1.1f, 7);

        Sprite caixa = ArteImportada.Objeto(7, 2);

        if (caixa != null)
            FormasDaSala.Desenho(transform, "Caixa", caixa, Color.white, new Vector2(1.5f, 0.1f), Vector2.one * 1.1f, 7);

        // Placa de LOJA acima da cabeca.
        Placa("LOJA", new Vector2(0f, 2.05f), new Color(1f, 0.85f, 0.35f));

        // O balao de fala comeca escondido.
        GameObject balao = new GameObject("Balao");
        balao.transform.SetParent(transform, false);
        // O balao fala pro lado do meio da sala (a banca pode estar encostada na direita).
        bool bancaNaDireita = sala != null && transform.position.x > sala.position.x + 0.5f;
        // Na direita, a porta de cima fica logo ao lado: o balao desce um pouco pra nao
        // encostar no heroi que acabou de entrar por ela.
        balao.transform.localPosition = bancaNaDireita ? new Vector3(-2.1f, 0.75f, 0f) : new Vector3(1.9f, 1.4f, 0f);

        Sprite placa = ArteImportada.PlacaNoMundo;

        if (placa != null)
        {
            fundoDoBalao = FormasDaSala.Desenho(balao.transform, "Fundo", placa, Color.white, Vector2.zero, Vector2.one, 30);
            fundoDoBalao.drawMode = SpriteDrawMode.Sliced;
        }

        textoDoBalao = ProdutoDaLoja.Texto(balao.transform, "", Vector2.zero);
        textoDoBalao.characterSize = 0.045f;
        textoDoBalao.GetComponent<MeshRenderer>().sortingOrder = 31;
        balao.SetActive(false);

        proximoPulo = Time.time + Random.Range(3f, 6f);
    }

    private void Placa(string texto, Vector2 posicao, Color cor)
    {
        GameObject obj = new GameObject("Placa " + texto);
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = posicao;

        Sprite placa = ArteImportada.PlacaNoMundo;

        if (placa != null)
        {
            SpriteRenderer sr = FormasDaSala.Desenho(obj.transform, "Fundo", placa, Color.white, Vector2.zero, Vector2.one, 11);
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(1.1f, 0.4f);
        }

        TextMesh t = ProdutoDaLoja.Texto(obj.transform, texto, Vector2.zero);
        t.color = cor;
        t.GetComponent<MeshRenderer>().sortingOrder = 12;
    }

    private void OnEnable()
    {
        ProdutoDaLoja.AoVender += AoVender;
        ProdutoDaLoja.AoRecusar += AoRecusar;
    }

    private void OnDisable()
    {
        ProdutoDaLoja.AoVender -= AoVender;
        ProdutoDaLoja.AoRecusar -= AoRecusar;
    }

    private bool DaMinhaLoja(ProdutoDaLoja p) => p != null && p.transform.parent == sala;

    private void AoVender(ProdutoDaLoja p)
    {
        if (!DaMinhaLoja(p))
            return;

        Falar(Agradecimentos[Random.Range(0, Agradecimentos.Length)]);
        Pular();
    }

    private void AoRecusar(ProdutoDaLoja p, bool faltouMoeda)
    {
        if (DaMinhaLoja(p))
            Falar(faltouMoeda ? "Faltam moedas, amigo..." : "Voce nao precisa disso agora.");
    }

    /// <summary>Mostra uma frase no balao por uns segundos.</summary>
    public void Falar(string frase)
    {
        if (textoDoBalao == null)
            return;

        textoDoBalao.transform.parent.gameObject.SetActive(true);
        textoDoBalao.text = frase;

        if (fundoDoBalao != null)
            fundoDoBalao.size = new Vector2(0.4f + frase.Length * 0.105f, 0.42f);

        fimDoBalao = Time.time + 2.6f;
    }

    private void Pular()
    {
        inicioDoPulo = Time.time;
        proximoPulo = Time.time + Random.Range(4f, 7f);
    }

    private void Update()
    {
        if (jogador == null)
        {
            GameObject achado = GameObject.FindWithTag("Player");
            jogador = achado != null ? achado.transform : null;
        }

        // Respira: estica e encolhe de leve. Pulinho de vez em quando (ou depois de vender).
        float respiro = Mathf.Sin(Time.time * 2.4f) * 0.04f;
        float tPulo = (Time.time - inicioDoPulo) / 0.35f;
        float altura = tPulo >= 0f && tPulo <= 1f ? Mathf.Sin(tPulo * Mathf.PI) * 0.18f : 0f;

        corpo.localScale = new Vector3(1f - respiro * 0.5f, 1f + respiro, 1f);
        corpo.localPosition = new Vector3(0f, altura, 0f);

        if (Time.time >= proximoPulo)
            Pular();

        if (jogador != null)
        {
            Vector2 lado = jogador.position - transform.position;

            // Olha pro jogador (o desenho olha pra direita).
            if (desenho != null && Mathf.Abs(lado.x) > 0.2f)
                desenho.flipX = lado.x < 0f;

            if (!cumprimentou && lado.sqrMagnitude < 5.5f * 5.5f)
            {
                cumprimentou = true;
                Falar(Saudacoes[Random.Range(0, Saudacoes.Length)]);
                Pular();
            }
        }

        if (textoDoBalao != null && textoDoBalao.transform.parent.gameObject.activeSelf && Time.time >= fimDoBalao)
            textoDoBalao.transform.parent.gameObject.SetActive(false);
    }
}
