using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A porta de uma sala de luta, na ponta do corredor encostada na sala. Fechada e parede (camada
/// "Wall"): ninguem passa, nem tiro, e os inimigos de fora nao veem quem esta dentro.
///
/// Todas sao o portao de caveira do pacote Old Prison: um arco de pedra da largura do corredor, que fica
/// sempre ali; as duas folhas fecham quando a sala fecha e abrem quando ela fica limpa, sempre pra dentro
/// da sala:
///
///   na parede de cima, de frente, com o pe na beirada da sala, o arco subindo pelo corredor e as folhas
///   abrindo pra perto da camera (pra dentro da sala);
///   na de baixo, tambem de frente (a camera ve o lado de fora da parede de baixo: a caveira), todo
///   dentro do corredor, com o alto do arco na beirada da sala, e sem o pedaco das folhas que vem pra
///   perto da camera: elas parecem abrir pra dentro;
///   nas dos lados, deitado (como as portas do Isaac), com o pe na beirada da sala, o arco pra fora e as
///   folhas abrindo pra sala.
///
/// Quem esta atras do arco (no corredor, ou na sala atras da porta de baixo) ve atraves dele: o arco
/// fica meio transparente. Sem a arte, uma grade de ferro que sobe do chao.
/// </summary>
public class PortaDaSala : MonoBehaviour
{
    private const float QuadrosPorSegundo = 24f;
    private const float Pixels = 32f;

    // A linha de baixo do arco (em pixels, de baixo pra cima): o resto pra baixo e das folhas abertas.
    private const float PeDoPortao = 22f;

    private const float Transparente = 0.4f;

    private static Sprite[] portao, portaoSemFrente;
    private static Transform jogador;
    private static Sprite grade, gradeDeLado, gradeDeLadoTopo;
    private static bool carregou;

    private SpriteRenderer folha;
    private Sprite[] quadros;
    private Vector2 pe;
    private Vector2Int paraDentro;
    private readonly List<Transform> barras = new List<Transform>();
    private BoxCollider2D colisor;
    private Coroutine mexendo;

    /// <summary>A porta esta fechada.</summary>
    public bool Fechada { get; private set; }

    public static PortaDaSala Criar(PortaDaPlanta planta, Transform pai)
    {
        Carregar();

        Vector2 soma = Vector2.zero;

        foreach (Vector2Int c in planta.celulas)
            soma += c;

        int quantas = Mathf.Max(1, planta.celulas.Count);
        Vector2 meio = soma / quantas;
        GameObject obj = new GameObject("Porta");
        obj.transform.SetParent(pai, false);
        obj.transform.position = meio;
        obj.layer = Pedreiro.CamadaDaParede;

        PortaDaSala porta = obj.AddComponent<PortaDaSala>();
        porta.colisor = obj.AddComponent<BoxCollider2D>();
        porta.colisor.size = planta.DeFrente ? new Vector2(quantas, 1f) : new Vector2(1f, quantas);
        porta.colisor.enabled = false;

        if (portao != null && portao.Length > 0)
            porta.Portao(planta, meio, quantas, obj.transform);
        else
            porta.Grades(planta, obj.transform);

        return porta;
    }

    // O portao de caveira, com o pe na beirada da sala e o arco pro lado do corredor, da largura do
    // corredor (a arte tem 3).
    private void Portao(PortaDaPlanta planta, Vector2 meio, int quantas, Transform pai)
    {
        paraDentro = planta.paraDentro;
        Sprite[] desenhos = portao;
        float giro = 0f;

        if (paraDentro == Vector2Int.down)
            pe = new Vector2(meio.x, meio.y - 0.5f);
        else if (paraDentro == Vector2Int.up)
        {
            // Na parede de baixo, a camera ve o lado de fora: a caveira de frente, todo dentro do corredor
            // (o alto do arco na beirada da sala) e as folhas sem a parte que viria pra perto da camera.
            desenhos = portaoSemFrente;
            pe = new Vector2(meio.x, meio.y + 0.5f - desenhos[0].bounds.size.y);
        }
        else if (paraDentro == Vector2Int.right)
        {
            // Sala a direita: o arco deitado pra esquerda.
            pe = new Vector2(meio.x + 0.5f, meio.y);
            giro = 90f;
        }
        else
        {
            pe = new Vector2(meio.x - 0.5f, meio.y);
            giro = -90f;
        }

        GameObject obj = new GameObject("Portao de caveira");
        obj.transform.SetParent(pai, false);
        obj.transform.position = pe;
        obj.transform.rotation = Quaternion.Euler(0f, 0f, giro);
        obj.transform.localScale = new Vector3(quantas / 3f, 1f, 1f);

        // Comeca aberta: o arco com as folhas abertas (o ultimo quadro).
        folha = obj.AddComponent<SpriteRenderer>();
        folha.sprite = desenhos[desenhos.Length - 1];
        folha.sortingOrder = 10;
        folha.spriteSortPoint = SpriteSortPoint.Pivot;
        quadros = desenhos;
    }

    // Quem esta atras do arco ve atraves dele.
    private void Update()
    {
        if (folha == null)
            return;

        if (jogador == null)
        {
            GameObject j = GameObject.FindWithTag("Player");

            if (j == null)
                return;

            jogador = j.transform;
        }

        Vector2 p = jogador.position;
        Bounds caixa = folha.bounds;
        bool dentro = p.x > caixa.min.x && p.x < caixa.max.x && p.y > caixa.min.y && p.y < caixa.max.y + 0.5f;

        // De frente, so quem esta pra cima do pe fica atras (quem esta embaixo esta na frente do arco); de
        // lado, quem esta do lado do arco (fora da sala).
        bool atras = dentro && (paraDentro.y != 0 ? p.y > pe.y : (p.x - pe.x) * paraDentro.x < 0f);
        Color cor = folha.color;
        float alfa = Mathf.MoveTowards(cor.a, atras ? Transparente : 1f, Time.deltaTime * 4f);

        if (!Mathf.Approximately(alfa, cor.a))
            folha.color = new Color(cor.r, cor.g, cor.b, alfa);
    }

    private static void Carregar()
    {
        if (carregou)
            return;

        carregou = true;
        Texture2D folhaDoPortao = Resources.Load<Texture2D>("Masmorra/Prisao/PortaoDeCaveira");
        portao = Cortar(folhaDoPortao, 96, PeDoPortao);
        portaoSemFrente = CortarDoPe(folhaDoPortao, 96, PeDoPortao);
        grade = Resources.Load<Sprite>("Itens/Grade");
        gradeDeLado = Resources.Load<Sprite>("Itens/GradeDeLado");
        gradeDeLadoTopo = Resources.Load<Sprite>("Itens/GradeDeLadoTopo");
    }

    // Quadros lado a lado, com o pivo no pe (no meio, a "pe" pixels de baixo).
    private static Sprite[] Cortar(Texture2D textura, int largura, float pe)
    {
        if (textura == null)
            return null;

        Sprite[] lista = new Sprite[textura.width / largura];
        Vector2 pivo = new Vector2(0.5f, pe / textura.height);

        for (int i = 0; i < lista.Length; i++)
            lista[i] = Sprite.Create(textura, new Rect(i * largura, 0, largura, textura.height), pivo, Pixels, 0, SpriteMeshType.FullRect);

        return lista;
    }

    // Os mesmos quadros sem a faixa de baixo do pe (as folhas abertas vindo pra perto da camera).
    private static Sprite[] CortarDoPe(Texture2D textura, int largura, float pe)
    {
        if (textura == null)
            return null;

        Sprite[] lista = new Sprite[textura.width / largura];

        for (int i = 0; i < lista.Length; i++)
            lista[i] = Sprite.Create(textura, new Rect(i * largura, pe, largura, textura.height - pe), new Vector2(0.5f, 0f), Pixels, 0, SpriteMeshType.FullRect);

        return lista;
    }

    // Sem a arte do portao: a grade de ferro que sobe do chao.
    private void Grades(PortaDaPlanta planta, Transform pai)
    {
        int maisAlta = int.MinValue;

        foreach (Vector2Int c in planta.celulas)
            maisAlta = Mathf.Max(maisAlta, c.y);

        foreach (Vector2Int c in planta.celulas)
        {
            GameObject barra = new GameObject("Grade");
            barra.transform.SetParent(pai, false);
            barra.transform.position = (Vector2)c + new Vector2(0f, -0.5f);
            SpriteRenderer desenho = barra.AddComponent<SpriteRenderer>();
            desenho.sprite = planta.DeFrente ? grade : (c.y == maisAlta ? gradeDeLadoTopo : gradeDeLado);
            desenho.sortingOrder = 10;
            desenho.spriteSortPoint = SpriteSortPoint.Pivot;
            barra.transform.localScale = new Vector3(1f, 0f, 1f);
            barra.SetActive(false);
            barras.Add(barra.transform);
        }
    }

    /// <summary>Fecha (ja e parede no primeiro quadro: ninguem escapa durante a animacao).</summary>
    public void Fechar()
    {
        if (Fechada)
            return;

        Fechada = true;
        colisor.enabled = true;
        Mexer(true);
    }

    public void Abrir()
    {
        if (!Fechada)
            return;

        Fechada = false;
        colisor.enabled = false;
        Mexer(false);
    }

    private void Mexer(bool fechar)
    {
        if (mexendo != null)
            StopCoroutine(mexendo);

        mexendo = StartCoroutine(folha != null ? Subir(fechar) : SubirGrade(fechar ? 1f : 0f));
    }

    // Os quadros vao do fechado (0) ao aberto (o ultimo): fechar toca de tras pra frente. O arco fica.
    private IEnumerator Subir(bool fechar)
    {
        for (int i = 0; i < quadros.Length; i++)
        {
            folha.sprite = quadros[fechar ? quadros.Length - 1 - i : i];
            yield return new WaitForSeconds(1f / QuadrosPorSegundo);
        }

        mexendo = null;
    }

    private IEnumerator SubirGrade(float ate)
    {
        foreach (Transform barra in barras)
            barra.gameObject.SetActive(true);

        float de = barras.Count > 0 ? barras[0].localScale.y : 0f;

        for (float t = 0f; t < 0.18f; t += Time.deltaTime)
        {
            float y = Mathf.Lerp(de, ate, t / 0.18f);

            foreach (Transform barra in barras)
                barra.localScale = new Vector3(1f, y, 1f);

            yield return null;
        }

        foreach (Transform barra in barras)
        {
            barra.localScale = new Vector3(1f, ate, 1f);
            barra.gameObject.SetActive(ate > 0f);
        }

        mexendo = null;
    }
}
