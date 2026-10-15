using System.Collections;
using UnityEngine;

/// <summary>
/// Um bau fechado na caverna. Interagir abre (a animacao do bau do Old Prison) e ele solta uma arma
/// sorteada, diferente das que o jogador tem na mao, e as vezes uma bolsa de municao. Aberto, fica
/// ali vazio. Monte com <see cref="Criar"/>.
///
/// Dois baus especiais (do jogo antigo) dao um item num pedestal em vez de arma: o trancado (dourado,
/// gasta uma chave) e o amaldicoado (roxo, custa meio coracao pra abrir).
///
/// O bau maldito (vermelho-escuro, tremendo de leve) e um evento: abre de graca e da um item bom (o melhor
/// de tres sorteados) e uma arma rara ou melhor, mas acorda uma emboscada de duas ondas em volta
/// (<see cref="Emboscada.Soltar"/>).
///
/// O bau mimico: as vezes (do andar 2 em diante) um bau comum espalhado pela caverna e um monstro
/// (<see cref="TalvezMimico"/>). Parece igual; o que entrega e uma tremidinha de vez em quando, mais
/// vezes com o jogador perto. Tentando abrir, ele acorda (<see cref="MimicoDoBau"/>).
/// </summary>
public enum TipoDeBau
{
    Comum,
    Trancado,
    Amaldicoado,
    Maldito,
}

[DisallowMultipleComponent]
public class Bau : Interativo
{
    private const float QuadrosPorSegundo = 14f;

    private Sprite[] quadros;
    private SpriteRenderer desenho;
    private DadosDaArma[] armas;
    private Sprite caixaDeMunicao;
    private AudioClip somAbrindo;
    private AudioClip somDaMunicao;
    private bool aberto;
    private TipoDeBau tipo;
    private bool mimico;
    private float proximaTremida;
    private Transform jogador;

    /// <summary>A chance de um bau espalhado pela caverna ser um mimico (do andar 2 em diante).</summary>
    public static float ChanceDeMimico(int andar) => andar < 2 ? 0f : Mathf.Min(0.2f, 0.08f + 0.015f * andar);

    /// <summary>Sorteia se este bau (comum) e um mimico.</summary>
    public Bau TalvezMimico(int andar)
    {
        mimico = tipo == TipoDeBau.Comum && Random.value < ChanceDeMimico(andar);
        proximaTremida = Time.time + Random.Range(2f, 5f);
        return this;
    }

    /// <summary>Vira bau trancado ou amaldicoado (logo depois do <see cref="Criar"/>).</summary>
    public Bau ComoTipo(TipoDeBau qual)
    {
        tipo = qual;
        desenho.color = qual == TipoDeBau.Trancado ? new Color(1f, 0.85f, 0.4f)
                      : qual == TipoDeBau.Amaldicoado ? new Color(0.75f, 0.45f, 1f)
                      : qual == TipoDeBau.Maldito ? new Color(0.85f, 0.3f, 0.3f)
                      : Color.white;

        // O maldito brilha vermelho no escuro: da pra ver que e diferente.
        if (qual == TipoDeBau.Maldito)
            Iluminacao.Luz(transform, new Vector2(0f, 0.5f), new Color(1f, 0.25f, 0.2f), 2.6f, 0.9f, 0.08f);

        return this;
    }

    public override bool Disponivel => !aberto;

    public override string Dica =>
        tipo == TipoDeBau.Trancado ? "abrir o baú trancado (1 chave)"
        : tipo == TipoDeBau.Amaldicoado ? "abrir o baú amaldiçoado (meio coração)"
        : tipo == TipoDeBau.Maldito ? "abrir o baú maldito (prêmio bom, mas algo vai acordar...)"
        : "abrir o baú";

    public static Bau Criar(Sprite[] quadros, Vector2 onde, Transform pai, DadosDaArma[] armas,
                            Sprite caixaDeMunicao, AudioClip somAbrindo, AudioClip somDaMunicao)
    {
        GameObject obj = new GameObject("Bau");
        obj.transform.SetParent(pai, false);
        obj.transform.position = onde;

        // O desenho do pacote tem o bau na parte de baixo do quadro: sobe um pouco pra ele ficar no lugar.
        GameObject filho = new GameObject("Desenho");
        filho.transform.SetParent(obj.transform, false);
        filho.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        SpriteRenderer sprite = filho.AddComponent<SpriteRenderer>();
        sprite.sortingOrder = 10;

        if (quadros.Length > 0)
            sprite.sprite = quadros[0];

        // O bau conta como parede: ninguem atravessa, segura tiro e os inimigos dao a volta.
        obj.layer = Pedreiro.CamadaDaParede;
        BoxCollider2D colisor = obj.AddComponent<BoxCollider2D>();
        colisor.size = new Vector2(1.5f, 0.7f);

        Bau bau = obj.AddComponent<Bau>();
        bau.quadros = quadros;
        bau.desenho = sprite;
        bau.armas = armas;
        bau.caixaDeMunicao = caixaDeMunicao;
        bau.somAbrindo = somAbrindo;
        bau.somDaMunicao = somDaMunicao;
        return bau;
    }

    public override void Usar(GameObject jogador)
    {
        if (aberto)
            return;

        if (tipo == TipoDeBau.Trancado && (!jogador.TryGetComponent(out Bolsa bolsa) || !bolsa.Gastar(0, 1)))
        {
            Sons.Tocar(Som.Negado, 0.7f, 0f);
            TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.2f, "Precisa de uma chave", new Color(1f, 0.6f, 0.55f), 1f);
            return;
        }

        if (tipo == TipoDeBau.Amaldicoado && jogador.TryGetComponent(out Vida vida))
        {
            vida.Pagar(1f);
            Sons.Tocar(Som.DanoJogador, 0.8f, 0f);
            CameraDoJogo.Tremer(0.15f, 0.2f);
        }

        if (tipo == TipoDeBau.Trancado)
            Sons.Tocar(Som.Destranca, 0.8f, 0f);

        aberto = true;

        if (mimico)
        {
            StartCoroutine(Acordar());
            return;
        }

        if (somAbrindo != null && jogador.TryGetComponent(out AudioSource audioSource))
            audioSource.PlayOneShot(somAbrindo, 0.7f);

        StartCoroutine(Abrir(jogador.GetComponent<ArmaDoJogador>()));
    }

    private void Update()
    {
        // O maldito treme de leve enquanto fechado.
        if (tipo == TipoDeBau.Maldito && !aberto)
            desenho.transform.localPosition = new Vector3(Mathf.Sin(Time.time * 40f) * 0.03f * (Mathf.Sin(Time.time * 1.7f) > 0.3f ? 1f : 0f), 0.3f, 0f);

        // O mimico da uma tremidinha (um pulinho torto) de vez em quando; com o jogador perto, mais vezes.
        if (mimico && !aberto && Time.time >= proximaTremida)
        {
            if (jogador == null)
            {
                GameObject j = GameObject.FindWithTag("Player");
                jogador = j != null ? j.transform : null;
            }

            bool perto = jogador != null && Vector2.Distance(jogador.position, transform.position) < 4f;
            proximaTremida = Time.time + (perto ? Random.Range(1.2f, 2.5f) : Random.Range(3f, 6f));
            StartCoroutine(Tremer(0.22f, 0.05f));
        }
    }

    private IEnumerator Tremer(float duracao, float forca)
    {
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float k = 1f - t / duracao;
            desenho.transform.localPosition = new Vector3(Mathf.Sin(t * 70f) * forca * k, 0.3f + Mathf.Abs(Mathf.Sin(t * 25f)) * forca * k, 0f);
            desenho.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 55f) * 4f * k);
            yield return null;
        }

        desenho.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        desenho.transform.localRotation = Quaternion.identity;
    }

    // Era um mimico: a tampa abre um pouco, ele treme forte e pula pra cima do jogador.
    private IEnumerator Acordar()
    {
        desenho.sprite = quadros.Length > 4 ? quadros[4] : desenho.sprite;
        Sons.Tocar(Som.Mimico, 1f, 0f);
        CameraDoJogo.Tremer(0.12f, 0.3f);
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.4f, "É um mímico!", new Color(1f, 0.4f, 0.35f), 1.6f).Pular(1.6f);
        yield return Tremer(0.45f, 0.1f);
        MimicoDoBau.Criar(transform.position, transform.parent, armas);
        Destroy(gameObject);
    }

    private IEnumerator Abrir(ArmaDoJogador doJogador)
    {
        for (int i = 0; i < quadros.Length; i++)
        {
            desenho.sprite = quadros[i];
            yield return new WaitForSeconds(1f / QuadrosPorSegundo);
        }

        Vector2 frente = (Vector2)transform.position + Vector2.down * 0.9f;

        // O maldito: o melhor de tres itens, uma arma rara ou melhor, e a emboscada acorda.
        if (tipo == TipoDeBau.Maldito)
        {
            desenho.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            EstatisticasDoJogador estatisticas = doJogador != null ? doJogador.GetComponent<EstatisticasDoJogador>() : null;
            Pedestal.Criar(frente + new Vector2(-0.9f, -0.4f), transform.parent, ItemBom(estatisticas));
            DadosDaArma rara = Sortear(doJogador, Raridade.Rara) ?? Sortear(doJogador);

            if (rara != null)
                ArmaNoChao.Criar(new ArmaCarregada(rara), frente + new Vector2(0.9f, -0.4f), transform.parent);

            CameraDoJogo.Tremer(0.2f, 0.4f);
            Sons.Tocar(Som.Rugido, 0.8f);
            Emboscada.Soltar(transform.position, transform.parent, GeradorDoAndar.Atual, 2);
            yield break;
        }

        // Os especiais dao um item num pedestal (e o trancado, umas moedas).
        if (tipo != TipoDeBau.Comum)
        {
            EstatisticasDoJogador estatisticas = doJogador != null ? doJogador.GetComponent<EstatisticasDoJogador>() : null;
            Pedestal.Criar(frente + Vector2.down * 0.4f, transform.parent, CatalogoDeItens.Sortear(estatisticas));

            if (tipo == TipoDeBau.Trancado)
            {
                for (int i = 0; i < 3; i++)
                    Coletavel.Criar(TipoDeColetavel.Moeda, frente + Vector2.right * 1.2f);
            }

            yield break;
        }

        DadosDaArma sorteada = Sortear(doJogador);

        if (sorteada != null)
            ArmaNoChao.Criar(new ArmaCarregada(sorteada), frente, transform.parent);

        if (caixaDeMunicao != null && Random.value < 0.4f)
            CaixaDeMunicao.Criar(caixaDeMunicao, frente + Vector2.right * 0.9f, transform.parent, somDaMunicao);
    }

    // O melhor (o mais caro) de tres itens sorteados.
    private static ItemPassivo ItemBom(EstatisticasDoJogador jogador)
    {
        ItemPassivo melhor = null;

        for (int i = 0; i < 3; i++)
        {
            ItemPassivo item = CatalogoDeItens.Sortear(jogador);

            if (melhor == null || (item != null && item.Preco > melhor.Preco))
                melhor = item;
        }

        return melhor;
    }

    // Pelo peso de cada uma neste andar (a raridade conta), sem repetir as que o jogador ja tem (se sobrar
    // alguma). Com <paramref name="noMinimo"/>, so dessa raridade pra cima (nula se nao tiver nenhuma).
    private DadosDaArma Sortear(ArmaDoJogador doJogador, Raridade noMinimo = Raridade.Comum) => Sortear(armas, doJogador, noMinimo);

    /// <summary>Uma arma das <paramref name="armas"/>, como o bau sorteia (o mimico usa pro premio dele).</summary>
    public static DadosDaArma Sortear(DadosDaArma[] armas, ArmaDoJogador doJogador, Raridade noMinimo = Raridade.Comum)
    {
        if (armas == null)
            return null;

        float total = 0f;
        int andar = GeradorDoAndar.AndarAtual;

        foreach (DadosDaArma a in armas)
            if (Serve(a, doJogador, true) && a.raridade >= noMinimo)
                total += a.PesoNoAndar(andar);

        bool semRepetir = total > 0f;

        if (!semRepetir)
        {
            foreach (DadosDaArma a in armas)
                if (Serve(a, doJogador, false) && a.raridade >= noMinimo)
                    total += a.PesoNoAndar(andar);
        }

        if (total <= 0f)
            return null;

        float ponto = Random.value * total;

        foreach (DadosDaArma a in armas)
        {
            if (!Serve(a, doJogador, semRepetir) || a.raridade < noMinimo)
                continue;

            ponto -= a.PesoNoAndar(andar);

            if (ponto <= 0f)
                return a;
        }

        return null;
    }

    private static bool Serve(DadosDaArma a, ArmaDoJogador doJogador, bool semRepetir)
    {
        if (a == null || a.pesoNoBau <= 0f || a.desenhoNaMao == null || !Melhorias.Liberada(a))
            return false;

        if (!semRepetir || doJogador == null)
            return true;

        return (doJogador.Atual == null || doJogador.Atual.Dados.Origem != a) && (doJogador.Outra == null || doJogador.Outra.Dados.Origem != a);
    }
}
