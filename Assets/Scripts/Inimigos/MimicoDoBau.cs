using UnityEngine;

/// <summary>
/// O bau mimico acordado (<see cref="Bau.TalvezMimico"/>): o bau de madeira da prisao com dentes, olhos
/// vermelhos e lingua (Resources/Cenario/MimicoDoBau, feito pelo Ferramentas/Cenario/desenhos.py). Pula
/// atras do jogador mordendo (machuca quem encosta) e, de tempos em tempos, para, escancara a boca e
/// cospe um leque de moedas douradas. Morto, solta o que o bau ia dar e mais: uma arma incomum ou
/// melhor, moedas e as vezes uma chave. Feito todo no codigo: monte com <see cref="Criar"/>.
/// </summary>
[DisallowMultipleComponent]
public class MimicoDoBau : MonoBehaviour
{
    private const float QuadrosPorSegundo = 10f;

    private static DadosDaArma cuspe;

    private Sprite[] quadros;
    private SpriteRenderer desenho;
    private Rigidbody2D corpo;
    private Vida vida;
    private Transform jogador;
    private DadosDaArma[] armas;
    private float proximoPulo, proximoCuspe, cuspindoAte = -1f, piscandoAte;
    private bool cuspiu;

    /// <summary>O mimico nasce acordado onde estava o bau.</summary>
    public static MimicoDoBau Criar(Vector2 onde, Transform pai, DadosDaArma[] armas)
    {
        Sprite[] quadros = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Cenario/MimicoDoBau"), new Vector2Int(128, 160), DadosDoOldPrison.Lado);

        if (quadros.Length < 3)
            return null;

        GameObject obj = new GameObject("MimicoDoBau");
        obj.transform.SetParent(GeradorDoAndar.Raiz != null ? GeradorDoAndar.Raiz : pai, false);
        obj.transform.position = onde;

        GameObject filho = new GameObject("Corpo");
        filho.transform.SetParent(obj.transform, false);
        filho.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        SpriteRenderer sr = filho.AddComponent<SpriteRenderer>();
        sr.sprite = quadros[1];
        sr.sortingOrder = 10;

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearDamping = 5f;
        rb.mass = 2f;

        CircleCollider2D c = obj.AddComponent<CircleCollider2D>();
        c.radius = 0.55f;
        c.offset = new Vector2(0f, 0.15f);

        int andar = GeradorDoAndar.AndarAtual;
        Vida v = obj.AddComponent<Vida>();
        v.Configurar(Lado.Inimigos, (28f + 7f * andar) * (Partida.Maldito ? 1.25f : 1f), 0.4f);
        obj.AddComponent<DanoAoEncostar>();
        obj.AddComponent<MorteDoInimigo>();

        MimicoDoBau m = obj.AddComponent<MimicoDoBau>();
        m.quadros = quadros;
        m.desenho = sr;
        m.corpo = rb;
        m.vida = v;
        m.armas = armas;
        m.proximoPulo = Time.time + 0.3f;
        m.proximoCuspe = Time.time + 2.5f;

        v.AoTomarDano += m.Apanhou;
        v.AoMorrer += m.Morreu;
        GeradorDoAndar.Registrar(v);
        Iluminacao.Luz(obj.transform, new Vector2(0f, 0.4f), new Color(1f, 0.3f, 0.2f), 2f, 0.6f, 0.1f);
        return m;
    }

    private void Update()
    {
        if (vida.Morto)
            return;

        if (jogador == null)
        {
            GameObject j = GameObject.FindWithTag("Player");
            jogador = j != null ? j.transform : null;

            if (jogador == null)
                return;
        }

        float ritmo = Partida.RitmoDosInimigos;
        Vector2 ate = (Vector2)jogador.position - (Vector2)transform.position;
        Vector2 rumo = ate.sqrMagnitude > 0.001f ? ate.normalized : Vector2.down;
        desenho.flipX = ate.x < 0f;

        // Escancarado, tremendo, e cospe no meio.
        if (cuspindoAte > 0f)
        {
            desenho.sprite = quadros[2];
            desenho.transform.localPosition = new Vector3(Mathf.Sin(Time.time * 60f) * 0.05f, 0.3f, 0f);

            if (!cuspiu && Time.time >= cuspindoAte - 0.25f)
            {
                cuspiu = true;
                Cuspir(rumo);
            }

            if (Time.time >= cuspindoAte)
            {
                cuspindoAte = -1f;
                desenho.transform.localPosition = new Vector3(0f, 0.3f, 0f);
                proximoCuspe = Time.time + Random.Range(3f, 4.5f) / ritmo;
                proximoPulo = Time.time + 0.3f;
            }

            return;
        }

        if (Time.time >= proximoCuspe && ate.magnitude < 9f)
        {
            cuspindoAte = Time.time + 0.7f / ritmo;
            cuspiu = false;
            corpo.linearVelocity = Vector2.zero;
            Sons.Tocar(Som.Mimico, 0.7f);
            return;
        }

        // Pulando: a boca abre e fecha (mordendo o ar); parado, fecha.
        bool noAr = corpo.linearVelocity.sqrMagnitude > 1f;
        desenho.sprite = noAr ? quadros[1 + Mathf.FloorToInt(Time.time * QuadrosPorSegundo) % 2] : quadros[0];
        desenho.transform.localPosition = new Vector3(0f, 0.3f + (noAr ? 0.15f : 0f), 0f);

        if (Time.time >= proximoPulo)
        {
            corpo.linearVelocity = rumo * 8.5f * ritmo;
            proximoPulo = Time.time + Random.Range(0.75f, 1f) / ritmo;

            if (ate.magnitude < 3f)
                Sons.Tocar(Som.Mordida, 0.6f);
        }

        if (piscandoAte > 0f && Time.time >= piscandoAte)
        {
            piscandoAte = 0f;
            desenho.color = Color.white;
        }
    }

    // Um leque de moedas douradas (sete tiros).
    private void Cuspir(Vector2 rumo)
    {
        if (cuspe == null)
        {
            DadosDaArma fogo = Resources.Load<DadosDaArma>("Armadilhas/FogoDaEstatua");

            if (fogo == null)
                return;

            cuspe = Instantiate(fogo);
            cuspe.name = "Cuspe do mimico";
            cuspe.anel = false;
            cuspe.tirosPorDisparo = 7;
            cuspe.abertura = 14f;
            cuspe.dispersao = 3f;
            cuspe.velocidade = Mathf.Min(fogo.velocidade, 6.5f);
            cuspe.alcance = 8f;
            cuspe.cor = new Color(1f, 0.82f, 0.3f);
        }

        cuspe.Disparar((Vector2)transform.position + Vector2.up * 0.35f, rumo, gameObject, Lado.Inimigos);
        Sons.Tocar(Som.Moeda, 0.6f);
        CameraDoJogo.Tremer(0.06f, 0.15f);
    }

    private void Apanhou(Dano dano)
    {
        desenho.color = new Color(1f, 0.55f, 0.55f);
        piscandoAte = Time.time + 0.08f;
    }

    private void Morreu()
    {
        desenho.sprite = quadros[2];
        desenho.color = Color.white;
        desenho.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        Vector2 aqui = transform.position;
        Transform pai = transform.parent;

        // O premio do bau, melhorado: uma arma incomum ou melhor, moedas e as vezes uma chave.
        GameObject j = GameObject.FindWithTag("Player");
        ArmaDoJogador doJogador = j != null ? j.GetComponent<ArmaDoJogador>() : null;
        DadosDaArma arma = Bau.Sortear(armas, doJogador, Raridade.Incomum) ?? Bau.Sortear(armas, doJogador);

        if (arma != null)
            ArmaNoChao.Criar(new ArmaCarregada(arma), aqui + Vector2.down * 0.6f, pai);

        for (int i = 0; i < 6; i++)
            Coletavel.Criar(TipoDeColetavel.Moeda, aqui + Random.insideUnitCircle * 0.8f);

        if (Random.value < 0.4f)
            Coletavel.Criar(TipoDeColetavel.Chave, aqui + Vector2.right * 0.8f);

        Conquistas.Conquistar(Conquistas.Mimico);
        Sons.Tocar(Som.Mimico, 0.8f, 0.15f);
    }

    private void OnDestroy()
    {
        if (vida != null)
        {
            vida.AoTomarDano -= Apanhou;
            vida.AoMorrer -= Morreu;
        }
    }
}
