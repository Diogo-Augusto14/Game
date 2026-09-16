using UnityEngine;

/// <summary>
/// Faísca / hit fx no ponto exato onde o golpe encostou. Escuta o Vida deste objeto
/// e reaproveita um punhado de instâncias em vez de criar e destruir a cada acerto
/// — mesma razão da Espada ser permanente: nada de lixo de memória no meio do combate.
///
/// Coloque no mesmo objeto que o Vida (no inimigo e no boneco).
/// </summary>
[RequireComponent(typeof(Vida))]
[DisallowMultipleComponent]
public class EfeitoDeImpacto : MonoBehaviour
{
    [Header("Efeito")]
    [Tooltip("Prefab da faísca: um objeto com SpriteRenderer + Animator (ou ParticleSystem)")]
    [SerializeField] private GameObject prefabDoEfeito;

    [Tooltip("Quantas cópias ficam prontas. 4 a 6 dá e sobra pra um combate normal")]
    [SerializeField, Min(1)] private int tamanhoDoPool = 5;

    [Tooltip("Segundos até a faísca sumir. Deixe igual (ou um tiquinho maior) que o clip da animação")]
    [SerializeField, Min(0.05f)] private float duracao = 0.35f;

    [Header("Posição")]
    [Tooltip("Empurra a faísca um pouco na direção de quem bateu, pra não ficar dentro do corpo")]
    [SerializeField] private float afastamento = 0.1f;

    [Tooltip("Espelha a faísca conforme o lado de onde veio o golpe")]
    [SerializeField] private bool virarComOGolpe = true;

    [Tooltip("Gira a faísca na direção do golpe (bom pra faíscas alongadas)")]
    [SerializeField] private bool girarComOGolpe = false;

    // ---------- estado ----------
    private Vida vida;
    private GameObject[] pool;
    private float[] desligarEm;
    private Transform recipiente;
    private int proximo;

    // ---------- ciclo de vida ----------
    private void Awake()
    {
        vida = GetComponent<Vida>();

        if (prefabDoEfeito == null)
            return;

        // As faíscas ficam num objeto solto na cena, não como filhas deste — senão
        // elas andariam junto com quem apanhou, e a faísca tem que ficar onde bateu.
        GameObject raiz = new GameObject($"EfeitosDeImpacto ({name})");
        recipiente = raiz.transform;

        pool = new GameObject[tamanhoDoPool];
        desligarEm = new float[tamanhoDoPool];

        for (int i = 0; i < tamanhoDoPool; i++)
        {
            pool[i] = Instantiate(prefabDoEfeito, recipiente);
            pool[i].SetActive(false);
        }
    }

    private void OnEnable()
    {
        vida.AoTomarDano.AddListener(Tocar);
    }

    private void OnDisable()
    {
        vida.AoTomarDano.RemoveListener(Tocar);
    }

    private void OnDestroy()
    {
        if (recipiente != null)
            Destroy(recipiente.gameObject);
    }

    private void Update()
    {
        if (pool == null)
            return;

        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i].activeSelf && Time.time >= desligarEm[i])
                pool[i].SetActive(false);
        }
    }

    // ---------- uso ----------
    /// <summary>Toca a faísca. Também pode ser ligado à mão num UnityEvent.</summary>
    public void Tocar(DanoInfo info)
    {
        if (pool == null || pool.Length == 0)
            return;

        int indice = proximo;
        GameObject efeito = pool[indice];
        proximo = (proximo + 1) % pool.Length;

        Vector2 posicao = info.PontoDeImpacto - info.Direcao * afastamento;
        efeito.transform.position = posicao;

        Vector3 escala = efeito.transform.localScale;
        escala.x = Mathf.Abs(escala.x) * (virarComOGolpe && info.Direcao.x < 0f ? -1f : 1f);
        efeito.transform.localScale = escala;

        efeito.transform.rotation = girarComOGolpe
            ? Quaternion.Euler(0f, 0f, Mathf.Atan2(info.Direcao.y, info.Direcao.x) * Mathf.Rad2Deg)
            : Quaternion.identity;

        // Desliga e liga pra a animação recomeçar do primeiro quadro.
        efeito.SetActive(false);
        efeito.SetActive(true);
        desligarEm[indice] = Time.time + duracao;
    }
}
