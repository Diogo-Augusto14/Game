using System.Collections;
using UnityEngine;

/// <summary>
/// O buraco no chao que aparece na sala do chefe quando ele morre, como no Isaac. O
/// jogador pisa, o boneco encolhe caindo e o <see cref="Andar"/> monta o proximo andar.
///
/// Nasce "fechado" por um instante: quem estava em cima quando ele abriu tem que sair e
/// pisar de novo, pra ninguem cair sem querer no meio da luta.
/// Monte por <see cref="Criar"/>.
/// </summary>
[DisallowMultipleComponent]
public class Alcapao : MonoBehaviour
{
    private const float TAMANHO = 1.3f;

    [SerializeField] private string tagDoJogador = "Player";

    [Tooltip("Segundos da animacao de abrir; so aceita o jogador depois disso")]
    [SerializeField, Min(0f)] private float tempoParaAbrir = 0.6f;

    [Tooltip("Segundos do boneco encolhendo antes de trocar de andar")]
    [SerializeField, Min(0f)] private float tempoDaQueda = 0.45f;

    private Transform buraco;
    private float aberto;
    private bool jogadorEmCimaAoAbrir;
    private bool caindo;

    public static Alcapao Criar(Vector2 posicao, Transform pai = null)
    {
        GameObject obj = new GameObject("Alcapao");
        obj.transform.SetParent(pai, true);
        obj.transform.position = posicao;

        // Moldura de madeira e o buraco preto por cima. Abaixo de tudo que anda (ordem 1-2).
        // Com o pacote: o alcapao de madeira por baixo e o buraco em espiral do tileset abrindo.
        Sprite moldura = ArteImportada.Objeto(0, 4);
        Sprite espiral = ArteImportada.Ladrilho(3, 9);
        bool comArte = moldura != null && espiral != null;

        FormasDaSala.Desenho(obj.transform, "Moldura", comArte ? moldura : FormasDaSala.Quadrado(),
            comArte ? Color.white : new Color(0.35f, 0.24f, 0.14f), Vector2.zero, Vector2.one * (TAMANHO + 0.2f), 1);
        SpriteRenderer buraco = FormasDaSala.Desenho(obj.transform, "Buraco", comArte ? espiral : FormasDaSala.Quadrado(),
            comArte ? new Color(0.35f, 0.3f, 0.45f) : Color.black, Vector2.zero, Vector2.one * TAMANHO, 2);

        // Sensor menor que o desenho: tem que pisar de verdade, nao so raspar a borda.
        BoxCollider2D sensor = obj.AddComponent<BoxCollider2D>();
        sensor.isTrigger = true;
        sensor.size = Vector2.one * (TAMANHO * 0.6f);

        Alcapao alcapao = obj.AddComponent<Alcapao>();
        alcapao.buraco = buraco.transform;
        alcapao.buraco.localScale = Vector3.zero;
        Sons.Tocar(Som.Alcapao, 0.8f);
        return alcapao;
    }

    private void Update()
    {
        if (aberto >= 1f || buraco == null)
            return;

        aberto = tempoParaAbrir <= 0f ? 1f : Mathf.MoveTowards(aberto, 1f, Time.deltaTime / tempoParaAbrir);
        // O desenho ja tem o tamanho certo (modo Tiled): a escala vai so de 0 a 1.
        buraco.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, aberto);
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (DoJogador(outro) != null && aberto < 1f)
            jogadorEmCimaAoAbrir = true;
    }

    private void OnTriggerExit2D(Collider2D outro)
    {
        if (DoJogador(outro) != null)
            jogadorEmCimaAoAbrir = false;
    }

    private void OnTriggerStay2D(Collider2D outro)
    {
        if (caindo || aberto < 1f || jogadorEmCimaAoAbrir)
            return;

        GameObject jogador = DoJogador(outro);

        if (jogador == null)
            return;

        Vida vida = jogador.GetComponent<Vida>();

        if (vida != null && vida.EstaMorto)
            return;

        StartCoroutine(Cair(jogador));
    }

    private GameObject DoJogador(Collider2D outro)
    {
        GameObject quem = outro.attachedRigidbody != null ? outro.attachedRigidbody.gameObject : outro.gameObject;
        return quem.CompareTag(tagDoJogador) ? quem : null;
    }

    private IEnumerator Cair(GameObject jogador)
    {
        caindo = true;
        Sons.Tocar(Som.Queda);

        // Sem controle durante a queda: o boneco vai pro meio do buraco e encolhe.
        // Esquecer antes de desligar: desligada, a Entrada deixaria o ultimo "andar" apertado.
        Entrada entrada = jogador.GetComponent<Entrada>();
        Rigidbody2D corpo = jogador.GetComponent<Rigidbody2D>();

        if (entrada != null)
        {
            entrada.Esquecer();
            entrada.enabled = false;
        }

        if (jogador.TryGetComponent(out MovimentoTopDown movimento))
            movimento.Parar();

        Transform t = jogador.transform;
        Vector3 escala = t.localScale;
        Vector3 inicio = t.position;
        Vector3 centro = new Vector3(transform.position.x, transform.position.y, inicio.z);

        for (float tempo = 0f; tempo < tempoDaQueda; tempo += Time.deltaTime)
        {
            float k = tempo / tempoDaQueda;
            t.position = Vector3.Lerp(inicio, centro, k);
            t.localScale = escala * (1f - k);

            if (corpo != null)
                corpo.position = t.position;

            yield return null;
        }

        t.localScale = escala;

        if (entrada != null)
        {
            entrada.enabled = true;
            entrada.Esquecer();
        }

        // O Andar apaga a sala onde este alcapao mora: tem que ser a ultima coisa.
        if (Andar.Atual != null)
            Andar.Atual.ProximoAndar();
    }
}
