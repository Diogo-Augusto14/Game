using UnityEngine;

/// <summary>
/// Faisca no ponto exato onde o golpe encostou. Escuta o Vida deste objeto e reaproveita
/// um punhado de instancias em vez de criar e destruir a cada acerto — mesma razao da
/// hitbox ser permanente: nada de lixo de memoria no meio do combate.
///
/// Nao precisa de prefab: as faiscas sao montadas na hora com os clipes de FX da
/// biblioteca. E o que permite o Bootstrap ligar o efeito sem ninguem arrastar nada.
///
/// Coloque no mesmo objeto que o Vida (no inimigo e no boneco).
/// </summary>
[RequireComponent(typeof(Vida))]
[DisallowMultipleComponent]
public class EfeitoDeImpacto : MonoBehaviour
{
    [Header("Efeito")]
    [Tooltip("Clipe da faisca de golpe leve")]
    [SerializeField] private string clipeLeve = NomesDeAnimacao.FxImpacto1;

    [Tooltip("Clipe da faisca de golpe forte")]
    [SerializeField] private string clipeForte = NomesDeAnimacao.FxImpacto2;

    [Tooltip("Quantas copias ficam prontas. 4 a 6 da e sobra pra um combate normal")]
    [SerializeField, Min(1)] private int tamanhoDoPool = 5;

    [Tooltip("Ordem de desenho da faisca (acima do boneco)")]
    [SerializeField] private int ordemNaCamada = 20;

    [Header("Posicao")]
    [Tooltip("Empurra a faisca um pouco na direcao de quem bateu, pra nao ficar dentro do corpo")]
    [SerializeField] private float afastamento = 0.06f;

    [Tooltip("Espelha a faisca conforme o lado de onde veio o golpe")]
    [SerializeField] private bool virarComOGolpe = true;

    [Tooltip("Prefab proprio. Vazio = a faisca e montada na hora com os clipes acima")]
    [SerializeField] private GameObject prefabDoEfeito;

    // ---------------- estado ----------------
    private Vida vida;
    private EfeitoAnimado[] pool;
    private Transform recipiente;
    private int proximo;

    // ---------------- ciclo de vida ----------------
    private void Awake()
    {
        vida = GetComponent<Vida>();
        MontarPool();
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

    private void MontarPool()
    {
        // As faiscas ficam num objeto solto na cena, nao como filhas deste — senao elas
        // andariam junto com quem apanhou, e a faisca tem que ficar onde bateu.
        GameObject raiz = new GameObject($"Faiscas ({name})");
        recipiente = raiz.transform;

        pool = new EfeitoAnimado[tamanhoDoPool];

        for (int i = 0; i < tamanhoDoPool; i++)
            pool[i] = CriarFaisca(i);
    }

    private EfeitoAnimado CriarFaisca(int indice)
    {
        GameObject obj;

        if (prefabDoEfeito != null)
        {
            obj = Instantiate(prefabDoEfeito, recipiente);
        }
        else
        {
            obj = new GameObject($"Faisca {indice}");
            obj.transform.SetParent(recipiente, false);
        }

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

        if (sr == null)
            sr = obj.AddComponent<SpriteRenderer>();

        sr.sortingOrder = ordemNaCamada;

        EfeitoAnimado efeito = obj.GetComponent<EfeitoAnimado>();

        if (efeito == null)
            efeito = obj.AddComponent<EfeitoAnimado>();

        obj.SetActive(false);
        return efeito;
    }

    // ---------------- uso ----------------
    /// <summary>Toca a faisca. Tambem pode ser ligado a mao num UnityEvent.</summary>
    public void Tocar(DanoInfo info)
    {
        if (pool == null || pool.Length == 0)
            return;

        EfeitoAnimado efeito = pool[proximo];
        proximo = (proximo + 1) % pool.Length;

        if (efeito == null)
            return;

        Transform t = efeito.transform;

        t.position = info.PontoDeImpacto - info.Direcao * afastamento;

        Vector3 escala = t.localScale;
        escala.x = Mathf.Abs(escala.x) * (virarComOGolpe && info.Direcao.x < 0f ? -1f : 1f);
        t.localScale = escala;

        efeito.Clipe = info.Peso == PesoDoGolpe.Forte ? clipeForte : clipeLeve;

        // Desliga e liga: o OnEnable do EfeitoAnimado reinicia o clipe do primeiro quadro.
        efeito.gameObject.SetActive(false);
        efeito.gameObject.SetActive(true);
    }
}
