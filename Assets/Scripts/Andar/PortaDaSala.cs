using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A porta de uma sala de luta, na ponta do corredor encostada na sala. Fechada e parede (camada
/// "Wall"): ninguem passa, nem tiro, e os inimigos de fora nao veem quem esta dentro.
///
/// Na parede de cima da sala (o corredor sobe) e o portao de caveira do pacote Old Prison: um arco de
/// pedra da largura do corredor, que fica sempre ali; as duas folhas fecham quando a sala fecha e abrem
/// quando ela fica limpa. Na parede de baixo e nas dos lados (onde o arco nao se ve de frente), uma grade
/// de ferro que sobe do chao.
/// </summary>
public class PortaDaSala : MonoBehaviour
{
    private const float QuadrosPorSegundo = 24f;
    private const float Pixels = 32f;

    // A linha de baixo do arco (em pixels, de baixo pra cima): o resto pra baixo e das folhas abertas.
    private const float PeDoPortao = 22f;

    private static Sprite[] portao;
    private static Sprite grade, gradeDeLado, gradeDeLadoTopo;
    private static bool carregou;

    private SpriteRenderer folha;
    private Sprite[] quadros;
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

        // O portao so na parede de cima da sala: a porta fica em cima dela e a sala e pra baixo.
        bool naParedeDeCima = planta.paraDentro == Vector2Int.down;

        if (naParedeDeCima && portao != null && portao.Length > 0)
        {
            // O pe do arco na beirada de baixo da linha da porta (a beirada da sala), da largura do
            // corredor (a arte tem 3).
            porta.Folha(obj.transform, new Vector2(meio.x, meio.y - 0.5f), quantas / 3f, portao);
        }
        else
        {
            porta.Grades(planta, obj.transform);
        }

        return porta;
    }

    private void Folha(Transform pai, Vector2 pe, float largura, Sprite[] desenhos)
    {
        GameObject obj = new GameObject("Porta de madeira");
        obj.transform.SetParent(pai, false);
        obj.transform.position = pe;
        obj.transform.localScale = new Vector3(largura, 1f, 1f);
        // Comeca aberta: o arco com as folhas abertas (o ultimo quadro).
        folha = obj.AddComponent<SpriteRenderer>();
        folha.sprite = desenhos[desenhos.Length - 1];
        folha.sortingOrder = 10;
        folha.spriteSortPoint = SpriteSortPoint.Pivot;
        quadros = desenhos;
    }

    private static void Carregar()
    {
        if (carregou)
            return;

        carregou = true;
        portao = Cortar(Resources.Load<Texture2D>("Masmorra/Prisao/PortaoDeCaveira"), 96, PeDoPortao);
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

    // Sem a arte do Crypt: a grade de ferro que sobe do chao.
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
