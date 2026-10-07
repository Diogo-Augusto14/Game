using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Os padroes de bala. Anel, leque, cruz, cortina e rosa saem de uma vez; rajada e espiral, ao longo do tempo.</summary>
public enum PadraoDeBala
{
    /// <summary>Balas lado a lado, apontadas pro jogador.</summary>
    Leque,

    /// <summary>Balas igualmente espacadas em volta do corpo.</summary>
    Anel,

    /// <summary>Um anel com um buraco pra passar, perto de onde o jogador esta.</summary>
    AnelComBuraco,

    /// <summary>Varios tiros em fila, cada um mirado de novo no jogador.</summary>
    Rajada,

    /// <summary>Fios de bala girando, soltos ao longo de um ou dois segundos.</summary>
    Espiral,

    /// <summary>Quatro balas, em "+" e em "x" alternados.</summary>
    Cruz,

    /// <summary>Uma fileira de balas indo pro jogador, com um buraco.</summary>
    Cortina,

    /// <summary>Varias petalas de tres balas em volta do corpo.</summary>
    Rosa,
}

/// <summary>
/// Um ataque de balas: qual padrao, quantas balas, quao rapidas e quanto tempo de aviso antes.
/// O que cada campo quer dizer depende do <see cref="padrao"/>.
/// </summary>
public class AtaqueDeBalas
{
    public PadraoDeBala padrao;

    /// <summary>
    /// Leque, anel e cortina: balas. Rajada: tiros em fila. Espiral: balas soltas por braco.
    /// Rosa: petalas. Cruz: ignora (sao sempre quatro).
    /// </summary>
    public int quantidade = 8;

    /// <summary>Fios da espiral.</summary>
    public int bracos = 2;

    public float velocidade = 3.6f;

    /// <summary>
    /// Leque: graus entre uma bala e a outra. Anel com buraco: largura do buraco, em graus.
    /// Espiral: graus que o fio gira a cada bala.
    /// </summary>
    public float abertura = 18f;

    public Color cor = Color.white;
    public float dano = 10f;
    public float diametro = 0.3f;

    /// <summary>Segundos do anel que fecha em volta do inimigo antes de atirar (0 = sem aviso).</summary>
    public float aviso = 0.5f;

    /// <summary>So ataca com o jogador a vista (sem parede no meio). Pro que mira.</summary>
    public bool exigeVisao;

    /// <summary>Angulo em que o anel, a rosa ou a espiral comecam (graus). Negativo = sorteia.</summary>
    public float anguloInicial = -1f;

    /// <summary>Bala de chefe (<see cref="BalaDeInimigo.deChefe"/>): vai alem do teto dos bichos comuns.</summary>
    public bool deChefe;

    public AtaqueDeBalas Copia() => (AtaqueDeBalas)MemberwiseClone();
}

/// <summary>Um passo de uma sequencia de padroes: o ataque e quanto esperar depois dele.</summary>
public struct PassoDeBalas
{
    public AtaqueDeBalas ataque;
    public float espera;

    public PassoDeBalas(AtaqueDeBalas ataque, float espera)
    {
        this.ataque = ataque;
        this.espera = espera;
    }
}

/// <summary>
/// Faz um inimigo soltar padroes de bala, como no Gungeon. Fica no inimigo e funciona de dois jeitos:
///
///   sozinho  -> conta o proprio intervalo (com o inimigo acordado e no estado Agindo, entao nao
///               atira no meio do golpe), solta o aviso e o proximo ataque do ciclo. E o jeito dos
///               bichos que andam atras do jogador (ver <see cref="PerfilDeBalas"/>);
///   chamado  -> quem manda e o inimigo (a sentinela, o bruxo...): ele ja tem o aviso dele e chama
///               <see cref="Atacar"/> na hora de atirar.
///
/// O aviso e um anel claro que fecha em volta do inimigo. Quem morre no meio de uma espiral para de
/// atirar; as balas que ja sairam continuam. Respeita o teto de balas no ar (<see cref="PadroesDeBala.TetoDeBalas"/>).
/// </summary>
[DisallowMultipleComponent]
public class AtiradorDePadroes : MonoBehaviour
{
    private readonly List<AtaqueDeBalas> ciclo = new List<AtaqueDeBalas>();
    private InimigoDeSala inimigo;
    private Rigidbody2D rb;
    private float raio = 0.3f;
    private bool sozinho;
    private float intervalo = 4f;
    private float atrasoInicial = 1.5f;
    private float proximoEm = -1f;
    private int vez;
    private bool cruzEmX;

    /// <summary>Esta no meio de um ataque (aviso, rajada, espiral ou sequencia).</summary>
    public bool Ocupado { get; private set; }

    /// <summary>
    /// A dificuldade da fase (o <see cref="Andar"/> poe nos chefes; os bichos comuns ja recebem os numeros
    /// prontos pelo <see cref="PerfilDeBalas"/>). Null = a da primeira fase.
    /// </summary>
    public DificuldadeDaFase Dificuldade { get; set; }

    public bool TemCiclo => ciclo.Count > 0;

    public int TamanhoDoCiclo => ciclo.Count;

    public AtaqueDeBalas Ataque(int indice) => ciclo[indice];

    public static AtiradorDePadroes Em(InimigoDeSala inimigo)
    {
        AtiradorDePadroes atirador = inimigo.GetComponent<AtiradorDePadroes>();
        return atirador != null ? atirador : inimigo.gameObject.AddComponent<AtiradorDePadroes>();
    }

    private void Awake()
    {
        inimigo = GetComponent<InimigoDeSala>();
        rb = GetComponent<Rigidbody2D>();

        if (TryGetComponent(out CircleCollider2D corpo))
            raio = corpo.radius * transform.lossyScale.x;
    }

    /// <summary>
    /// Poe o ciclo de ataques (cada vez que ataca, vai pro proximo). <paramref name="atuaSozinho"/>
    /// liga a contagem propria; desligado, o inimigo chama <see cref="Atacar"/>.
    /// </summary>
    public void Configurar(IEnumerable<AtaqueDeBalas> ataques, bool atuaSozinho, float segundosEntreAtaques, float atraso)
    {
        ciclo.Clear();
        ciclo.AddRange(ataques);
        sozinho = atuaSozinho;
        intervalo = Mathf.Max(0.5f, segundosEntreAtaques);
        atrasoInicial = Mathf.Max(0f, atraso);
        proximoEm = -1f;
        vez = 0;
    }

    /// <summary>Acrescenta um ataque ao fim do ciclo (o campeao ganha o dele). Sem ciclo, passa a atuar sozinho.</summary>
    public void Adicionar(AtaqueDeBalas ataque, float segundosEntreAtaques, float atraso)
    {
        if (ciclo.Count == 0)
        {
            sozinho = true;
            intervalo = Mathf.Max(0.5f, segundosEntreAtaques);
            atrasoInicial = Mathf.Max(0f, atraso);
        }

        ciclo.Add(ataque);
    }

    /// <summary>O proximo ataque do ciclo, e passa pro seguinte. Null sem ciclo.</summary>
    public AtaqueDeBalas ProximoDoCiclo()
    {
        if (ciclo.Count == 0)
            return null;

        return ciclo[vez++ % ciclo.Count];
    }

    private bool Vivo => inimigo != null && !inimigo.EstaMorto && inimigo.EstadoAtual != InimigoDeSala.Estado.Dormindo;

    private void Update()
    {
        if (!sozinho || ciclo.Count == 0 || Ocupado || inimigo == null)
            return;

        // So conta quando o inimigo esta livre: acordado e agindo, nao no meio de um golpe ou atordoado.
        if (inimigo.EstadoAtual != InimigoDeSala.Estado.Agindo)
            return;

        if (proximoEm < 0f)
        {
            proximoEm = Time.time + atrasoInicial * Random.Range(0.6f, 1.4f);
            return;
        }

        if (Time.time < proximoEm)
            return;

        AtaqueDeBalas ataque = ciclo[vez % ciclo.Count];

        if (ataque.exigeVisao && !inimigo.VeJogador)
        {
            proximoEm = Time.time + 0.5f;
            return;
        }

        vez++;
        Atacar(ataque, true);
    }

    /// <summary>
    /// Solta o ataque. <paramref name="comAviso"/> falso quando o proprio inimigo ja avisou
    /// (a sentinela incha, o bruxo ergue o cajado). Nao faz nada se ja estiver atacando.
    /// </summary>
    public void Atacar(AtaqueDeBalas ataque, bool comAviso)
    {
        if (ataque == null || Ocupado || !isActiveAndEnabled)
            return;

        StartCoroutine(Rotina(ataque, comAviso));
    }

    /// <summary>
    /// Solta uma fila de ataques, um depois do outro, esperando <see cref="PassoDeBalas.espera"/> entre
    /// eles (o ataque extra dos chefes). O aviso e do proprio chefe.
    /// </summary>
    public void Sequencia(IList<PassoDeBalas> passos)
    {
        if (passos == null || passos.Count == 0 || Ocupado || !isActiveAndEnabled)
            return;

        StartCoroutine(RotinaDaSequencia(passos));
    }

    private IEnumerator RotinaDaSequencia(IList<PassoDeBalas> passos)
    {
        Ocupado = true;

        for (int i = 0; i < passos.Count && Vivo; i++)
        {
            yield return StartCoroutine(Disparar(passos[i].ataque));

            if (passos[i].espera > 0f)
                yield return new WaitForSeconds(passos[i].espera);
        }

        Ocupado = false;
    }

    private IEnumerator Rotina(AtaqueDeBalas ataque, bool comAviso)
    {
        Ocupado = true;

        if (comAviso && ataque.aviso > 0f)
            yield return StartCoroutine(Aviso(ataque));

        if (Vivo)
            yield return StartCoroutine(Disparar(ataque));

        Ocupado = false;
        proximoEm = Time.time + intervalo * Random.Range(0.85f, 1.15f);
    }

    // O anel claro que fecha em volta do inimigo: a hora de se preparar pra desviar.
    private IEnumerator Aviso(AtaqueDeBalas ataque)
    {
        GameObject obj = new GameObject("Aviso de bala");
        obj.transform.SetParent(transform, false);

        SpriteRenderer anel = obj.AddComponent<SpriteRenderer>();
        anel.sprite = ArteDasArmas.Anel();
        anel.sortingOrder = 15;

        float escalaDoPai = Mathf.Max(0.01f, transform.lossyScale.x);
        Color cor = Color.Lerp(ataque.cor, Color.white, 0.4f);
        float t = 0f;

        while (t < ataque.aviso)
        {
            if (!Vivo)
                break;

            float k = t / ataque.aviso;
            obj.transform.localScale = Vector3.one * (Mathf.Lerp(raio * 8f, raio * 2.2f, k) / escalaDoPai);
            cor.a = Mathf.Lerp(0.15f, 0.9f, k);
            anel.color = cor;
            t += Time.deltaTime;
            yield return null;
        }

        Destroy(obj);
    }

    private IEnumerator Disparar(AtaqueDeBalas a)
    {
        BalaDeInimigo bala = new BalaDeInimigo(a.velocidade, a.cor, a.dano, a.diametro) { deChefe = a.deChefe };
        float inicio = a.anguloInicial >= 0f ? a.anguloInicial : Random.value * 360f;
        Vector2 origem = rb.position;
        float aoJogador = Mathf.Atan2(inimigo.DirecaoDoJogador.y, inimigo.DirecaoDoJogador.x) * Mathf.Rad2Deg;

        Sons.Tocar(Som.TiroInimigo, 0.5f);

        switch (a.padrao)
        {
            case PadraoDeBala.Leque:
                PadroesDeBala.Leque(origem, gameObject, raio, bala, a.quantidade, aoJogador, a.abertura);
                break;

            case PadraoDeBala.Anel:
                PadroesDeBala.Anel(origem, gameObject, raio, bala, a.quantidade, inicio);
                break;

            case PadraoDeBala.AnelComBuraco:
            {
                // O buraco fica de um lado do jogador, nao em cima dele: tem que andar pra passar.
                float lado = Random.value < 0.5f ? -1f : 1f;
                float buraco = aoJogador + lado * Random.Range(25f, 55f);
                PadroesDeBala.AnelComBuraco(origem, gameObject, raio, bala, a.quantidade, buraco, a.abertura);
                break;
            }

            case PadraoDeBala.Cruz:
                PadroesDeBala.Anel(origem, gameObject, raio, bala, 4, cruzEmX ? 45f : 0f);
                cruzEmX = !cruzEmX;
                break;

            case PadraoDeBala.Cortina:
                PadroesDeBala.Cortina(origem, gameObject, raio, bala, a.quantidade, aoJogador, 0.75f,
                                      Random.Range(1, Mathf.Max(2, a.quantidade - 2)));
                break;

            case PadraoDeBala.Rosa:
                PadroesDeBala.Rosa(origem, gameObject, raio, bala, a.quantidade, 3, inicio, 14f);
                break;

            case PadraoDeBala.Rajada:
            {
                for (int i = 0; i < a.quantidade && Vivo; i++)
                {
                    // Cada tiro mira de novo, com um pouco de erro: a fila acompanha o jogador que se mexe.
                    Vector2 rumo = inimigo.DirecaoDoJogador;
                    float angulo = Mathf.Atan2(rumo.y, rumo.x) * Mathf.Rad2Deg + Random.Range(-4f, 4f);
                    PadroesDeBala.Bala(rb.position, PadroesDeBala.Rumo(angulo), bala, gameObject, raio);

                    if (i > 0)
                        Sons.Tocar(Som.TiroInimigo, 0.4f);

                    yield return new WaitForSeconds(0.15f);
                }

                break;
            }

            case PadraoDeBala.Espiral:
            {
                int bracos = Mathf.Max(1, a.bracos);
                float angulo = inicio;
                float giro = a.abertura * (Random.value < 0.5f ? -1f : 1f);

                for (int i = 0; i < a.quantidade && Vivo; i++)
                {
                    for (int b = 0; b < bracos; b++)
                        PadroesDeBala.Bala(rb.position, PadroesDeBala.Rumo(angulo + b * 360f / bracos), bala, gameObject, raio);

                    angulo += giro;

                    if (i % 3 == 2)
                        Sons.Tocar(Som.TiroInimigo, 0.35f);

                    yield return new WaitForSeconds(0.09f);
                }

                break;
            }
        }
    }
}
