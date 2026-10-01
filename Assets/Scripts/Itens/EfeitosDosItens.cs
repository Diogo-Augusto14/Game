using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Os itens que nao sao so numero: escudo, renascer, curar ao matar, espinhos, ima, sorte,
/// desconto na loja, furia, invencibilidade maior, orbes, bomba forte e cura por sala.
///
/// Fica no jogador (o <see cref="EstatisticasDoJogador"/> poe sozinho). A cada item novo o
/// <see cref="EstatisticasDoJogador"/> chama <see cref="Aplicar"/> com a lista toda, e aqui
/// se somam os efeitos: dois itens de ima somam o raio, dois orbes giram juntos.
/// </summary>
[DisallowMultipleComponent]
public class EfeitosDosItens : MonoBehaviour
{
    [Tooltip("Cura de cada 'meio coracao' (20 de vida = um coracao)")]
    [SerializeField, Min(0f)] private float meioCoracao = 10f;

    [Tooltip("Com a vida nesta fracao ou menos (ou um coracao ou menos), a furia liga")]
    [SerializeField, Range(0f, 1f)] private float fracaoDaFuria = 0.34f;

    [SerializeField, Min(0.5f)] private float raioDosEspinhos = 2.2f;

    [SerializeField, Min(1f)] private float velocidadeDoIma = 7f;

    private Vida vida;
    private EstatisticasDoJogador estatisticas;
    private Inventario inventario;
    private Andar andar;

    // Somas dos itens (Aplicar).
    private bool renasce;
    private bool temEscudo;
    private int inimigosParaCurar;
    private float danoDeEspinhos;
    private float raioDoIma;
    private float furia;
    private float somaInvencibilidade;
    private float invencibilidadeBase = -1f;
    private float curaAoLimparSala;
    private bool fogoAoRenascer;
    private bool espinhosNoEscudo;

    // Estado da partida.
    private bool renasceuJa;
    private bool escudoPronto;
    private bool furioso;
    private int mortesContadas;
    private float proximaBuscaDoIma;
    private readonly List<Coletavel> coletaveisPerto = new List<Coletavel>();
    private readonly HashSet<Sala> salasComCura = new HashSet<Sala>();
    private readonly List<OrbeGuardiao> orbes = new List<OrbeGuardiao>();
    private SpriteRenderer marcaDoEscudo;

    /// <summary>Multiplica o dano das flechas (a furia, com a vida baixa).</summary>
    public float MultiplicadorDeDano => furioso ? 1f + furia : 1f;

    /// <summary>Multiplica raio e dano das bombas do jogador (<see cref="Bomba.Criar"/> le aqui).</summary>
    public float PotenciaDaBomba { get; private set; } = 1f;

    private void Awake()
    {
        vida = GetComponent<Vida>();
        estatisticas = GetComponent<EstatisticasDoJogador>();
        inventario = GetComponent<Inventario>();

        // Partida nova: nada de sorte nem desconto herdado da anterior.
        TabelaDeDrops.Sorte = 1f;
        Loja.Desconto = 0f;
    }

    private void OnEnable()
    {
        InimigoDeSala.AlgumMorreu += AoMorrerInimigo;

        if (vida != null)
        {
            vida.Bloquear = Bloquear;
            vida.AntesDeMorrer = Renascer;
            vida.AoTomarDano.AddListener(AoApanhar);
            vida.AoMudarVida.AddListener(ConferirFuria);
        }
    }

    private void OnDisable()
    {
        InimigoDeSala.AlgumMorreu -= AoMorrerInimigo;

        if (vida != null)
        {
            if (vida.Bloquear == (System.Func<DanoInfo, bool>)Bloquear)
                vida.Bloquear = null;

            if (vida.AntesDeMorrer == (System.Func<bool>)Renascer)
                vida.AntesDeMorrer = null;

            vida.AoTomarDano.RemoveListener(AoApanhar);
            vida.AoMudarVida.RemoveListener(ConferirFuria);
        }

        if (andar != null)
            andar.AoEntrarNaSala -= AoEntrarNaSala;

        andar = null;
    }

    private void OnDestroy()
    {
        TabelaDeDrops.Sorte = 1f;
        Loja.Desconto = 0f;

        foreach (OrbeGuardiao orbe in orbes)
            if (orbe != null)
                Destroy(orbe.gameObject);
    }

    /// <summary>Soma os efeitos de todos os itens pegos. Chamado pelo <see cref="EstatisticasDoJogador"/>.</summary>
    public void Aplicar(IReadOnlyList<ItemPassivo> itens)
    {
        bool tinhaEscudo = temEscudo;
        renasce = false;
        temEscudo = false;
        inimigosParaCurar = 0;
        danoDeEspinhos = 0f;
        raioDoIma = 0f;
        furia = 0f;
        somaInvencibilidade = 0f;
        curaAoLimparSala = 0f;
        fogoAoRenascer = false;
        espinhosNoEscudo = false;
        float sorte = 1f, desconto = 0f, bomba = 1f;
        int quantosOrbes = 0;

        foreach (ItemPassivo i in itens)
        {
            renasce |= i.renasce;
            temEscudo |= i.escudoPorSala;

            // Dois itens de curar ao matar: cura mais cedo (5, depois 3...).
            if (i.inimigosParaCurar > 0)
                inimigosParaCurar = inimigosParaCurar == 0
                    ? i.inimigosParaCurar
                    : Mathf.Max(2, Mathf.Min(inimigosParaCurar, i.inimigosParaCurar) - 2);

            danoDeEspinhos += i.danoDeEspinhos;
            raioDoIma += i.raioDoIma;
            furia += i.furia;
            somaInvencibilidade += i.somaInvencibilidade;
            curaAoLimparSala += i.curaAoLimparSala;
            fogoAoRenascer |= i.fogoAoRenascer;
            espinhosNoEscudo |= i.espinhosNoEscudo;
            sorte *= i.multiplicaSorte <= 0f ? 1f : i.multiplicaSorte;
            desconto = 1f - (1f - desconto) * (1f - Mathf.Clamp01(i.descontoNaLoja));
            bomba *= i.multiplicaBomba <= 0f ? 1f : i.multiplicaBomba;
            quantosOrbes += i.orbes;
        }

        TabelaDeDrops.Sorte = sorte;
        Loja.Desconto = Mathf.Min(desconto, 0.8f);
        PotenciaDaBomba = bomba;

        if (vida != null)
        {
            if (invencibilidadeBase < 0f)
                invencibilidadeBase = vida.TempoInvencivel;

            vida.DefinirInvencibilidade(invencibilidadeBase + somaInvencibilidade);
        }

        // Escudo novo ja vem carregado.
        if (temEscudo && !tinhaEscudo)
            escudoPronto = true;

        MostrarEscudo();
        AjustarOrbes(quantosOrbes);
        furioso = CalcularFuria();
    }

    private void Start()
    {
        EscutarAndar();
    }

    private void EscutarAndar()
    {
        if (andar != null || Andar.Atual == null)
            return;

        andar = Andar.Atual;
        andar.AoEntrarNaSala += AoEntrarNaSala;
    }

    private void Update()
    {
        EscutarAndar();
        PuxarColetaveis();
    }

    // ================================================================ escudo
    private bool Bloquear(DanoInfo info)
    {
        if (!temEscudo || !escudoPronto)
            return false;

        escudoPronto = false;
        MostrarEscudo();
        Sons.Tocar(Som.Pancada);
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 0.9f, "Bloqueou!", new Color(1f, 0.85f, 0.35f));

        // Bastiao de Espinhos: o bloqueio revida.
        if (espinhosNoEscudo && danoDeEspinhos > 0f)
            SoltarEspinhos();

        return true;
    }

    /// <summary>O escudinho em cima da cabeca: aparece so quando esta carregado.</summary>
    private void MostrarEscudo()
    {
        if (marcaDoEscudo == null && temEscudo)
        {
            Sprite desenho = ArteImportada.IconeDoItem("Escudo Sagrado");

            if (desenho == null)
                return;

            GameObject obj = new GameObject("Escudo do item");
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            obj.transform.localScale = Vector3.one * 0.45f;
            marcaDoEscudo = obj.AddComponent<SpriteRenderer>();
            marcaDoEscudo.sprite = desenho;
            marcaDoEscudo.sortingOrder = 22;
        }

        if (marcaDoEscudo != null)
            marcaDoEscudo.enabled = temEscudo && escudoPronto;
    }

    // ================================================================ renascer
    private bool Renascer()
    {
        if (!renasce || renasceuJa)
            return false;

        renasceuJa = true;
        Sons.Tocar(Som.Item);
        Explosao.Efeito(transform.position, 1.2f);
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 1f, "Renasceu!", new Color(1f, 0.55f, 0.3f));

        // Renascer em Chamas: anel de fogo e explosao que nao fere o jogador.
        if (fogoAoRenascer)
        {
            Explosao.Estourar(transform.position, 2.5f, 40f, 0f, 8f, gameObject);

            for (int i = 0; i < 16; i++)
            {
                float a = i * 22.5f * Mathf.Deg2Rad;
                Vector2 rumo = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                TiroDaSala.Disparar((Vector2)transform.position + rumo * 0.5f, rumo * 6.5f, 25f, gameObject, false,
                                    new Color(1f, 0.5f, 0.15f), 0.4f);
            }
        }

        return true;
    }

    /// <summary>A Pena da Fenix ja foi gasta nesta partida?</summary>
    public bool RenasceuJa => renasceuJa;

    /// <summary>Partida salva em que a Pena da Fenix ja foi gasta.</summary>
    public void MarcarRenascido() => renasceuJa = true;

    // ================================================================ curar ao matar
    private void AoMorrerInimigo(InimigoDeSala inimigo)
    {
        if (inimigosParaCurar <= 0 || vida == null || vida.EstaMorto)
            return;

        mortesContadas++;

        if (mortesContadas < inimigosParaCurar)
            return;

        mortesContadas = 0;

        if (vida.VidaAtual >= vida.VidaMaxima)
            return;

        vida.Curar(meioCoracao);
        Sons.Tocar(Som.Cura);
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 0.9f, "+ vida", new Color(1f, 0.35f, 0.4f));
    }

    // ================================================================ espinhos e furia
    private void AoApanhar(DanoInfo info)
    {
        if (danoDeEspinhos <= 0f)
            return;

        SoltarEspinhos();
    }

    private void SoltarEspinhos()
    {
        Vector2 centro = transform.position;

        foreach (InimigoDeSala inimigo in InimigoDeSala.Ativos.ToArray())
        {
            if (inimigo == null || inimigo.EstaMorto)
                continue;

            Vector2 lado = (Vector2)inimigo.transform.position - centro;

            if (lado.sqrMagnitude > raioDosEspinhos * raioDosEspinhos)
                continue;

            Vida vidaDoInimigo = inimigo.GetComponent<Vida>();

            if (vidaDoInimigo != null)
                vidaDoInimigo.TomarDano(new DanoInfo(danoDeEspinhos, lado.sqrMagnitude > 0.0001f ? lado.normalized : Vector2.up,
                                                     4f, centro, gameObject));
        }

        EspinhosQueSaem.Criar(centro, raioDosEspinhos);
    }

    private bool CalcularFuria()
        => furia > 0f && vida != null && !vida.EstaMorto
           && (vida.Fracao <= fracaoDaFuria || vida.VidaAtual <= 20f);

    private void ConferirFuria()
    {
        bool agora = CalcularFuria();

        if (agora == furioso)
            return;

        furioso = agora;

        if (estatisticas != null)
            estatisticas.Atualizar();

        if (furioso)
            TextoFlutuante.Mostrar(transform.position + Vector3.up * 1f, "Furia!", new Color(1f, 0.4f, 0.2f));
    }

    // ================================================================ sala
    private void AoEntrarNaSala(Sala sala)
    {
        if (temEscudo && !escudoPronto)
        {
            escudoPronto = true;
            MostrarEscudo();
        }

        if (sala == null || sala.Limpa || !salasComCura.Add(sala))
            return;

        sala.AoLimpar.AddListener(() =>
        {
            if (curaAoLimparSala <= 0f || vida == null || vida.EstaMorto || vida.VidaAtual >= vida.VidaMaxima)
                return;

            vida.Curar(curaAoLimparSala);
            Sons.Tocar(Som.Cura);
            TextoFlutuante.Mostrar(transform.position + Vector3.up * 0.9f, "+ vida", new Color(1f, 0.35f, 0.4f));
        });
    }

    // ================================================================ ima
    private void PuxarColetaveis()
    {
        if (raioDoIma <= 0f)
            return;

        // Procurar na cena toda e caro: a lista e refeita poucas vezes por segundo.
        if (Time.time >= proximaBuscaDoIma)
        {
            proximaBuscaDoIma = Time.time + 0.25f;
            coletaveisPerto.Clear();

            foreach (Coletavel c in FindObjectsByType<Coletavel>())
                if (((Vector2)(c.transform.position - transform.position)).sqrMagnitude <= raioDoIma * raioDoIma && Serve(c))
                    coletaveisPerto.Add(c);
        }

        foreach (Coletavel c in coletaveisPerto)
        {
            if (c == null)
                continue;

            c.transform.position = Vector3.MoveTowards(c.transform.position, transform.position, velocidadeDoIma * Time.deltaTime);
        }
    }

    /// <summary>So puxa o que o jogador consegue pegar (coracao com vida cheia fica onde esta).</summary>
    private bool Serve(Coletavel c)
    {
        if (c.Tipo == TipoDeColetavel.Coracao)
            return vida != null && !vida.EstaMorto && vida.VidaAtual < vida.VidaMaxima;

        if (inventario == null)
            inventario = GetComponent<Inventario>();

        return inventario == null || inventario.CabeMais(c.Tipo);
    }

    // ================================================================ orbes
    private void AjustarOrbes(int quantos)
    {
        orbes.RemoveAll(o => o == null);

        while (orbes.Count < quantos)
            orbes.Add(OrbeGuardiao.Criar(transform));

        while (orbes.Count > quantos)
        {
            Destroy(orbes[orbes.Count - 1].gameObject);
            orbes.RemoveAt(orbes.Count - 1);
        }

        for (int i = 0; i < orbes.Count; i++)
            orbes[i].DefinirLugar(i, orbes.Count);
    }
}
