using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Necromante: fica longe passeando pela sala e, de tempos em tempos, para, ergue as maos e
/// levanta um esqueleto do chao perto dele (no maximo dois dele vivos de uma vez). Quando ja
/// tem os dois, solta uma maldicao lenta no jogador. Matar ele primeiro e o que segura a sala.
///
///   Agindo      -> passeia longe do jogador
///   Preparando  -> ergue as maos (aviso, com a nuvem verde no chao onde o esqueleto vai sair)
///   Recuperando -> pausa curta
/// </summary>
public class InimigoNecromante : InimigoDeSala
{
    [SerializeField, Min(0.5f)] private float intervaloEntreFeiticos = 4.5f;

    [SerializeField, Min(0f)] private float tempoDePreparo = 0.9f;

    [SerializeField, Min(0)] private int esqueletosAoMesmoTempo = 2;

    [Tooltip("Com tanta gente viva na sala ele nao chama mais ninguem")]
    [SerializeField, Min(1)] private int tetoDaSala = 10;

    [SerializeField, Min(0f)] private float danoDaMaldicao = 10f;

    [SerializeField] private Color corDaMaldicao = new Color(0.55f, 1f, 0.5f);

    private readonly List<InimigoDeSala> meus = new List<InimigoDeSala>();
    private Cronometro recarga;
    private Cronometro preparo;
    private Cronometro pausa;
    private Vector2 pontoDoEsqueleto;
    private bool vaiChamar;

    protected override void Awake()
    {
        base.Awake();
        recarga.Forcar(intervaloEntreFeiticos * Random.Range(0.4f, 0.7f));
    }

    protected override void AtualizarAgindo(float dt)
    {
        recarga.Contar(dt);
        Vector2 alvo = ParaOJogador();

        if (!recarga.Ativo)
        {
            meus.RemoveAll(m => m == null || m.EstaMorto);
            vaiChamar = meus.Count < esqueletosAoMesmoTempo && VivosNaSala < tetoDaSala && SalaDoInimigo != null;

            // Ja tem os esqueletos dele: so atira se estiver vendo o jogador.
            if (vaiChamar || VeOJogador())
            {
                if (vaiChamar)
                {
                    Vector2 lado = Random.insideUnitCircle.normalized * 1.2f;
                    pontoDoEsqueleto = rb.position + lado;
                    EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.NuvemVerde, pontoDoEsqueleto, new Color(0.6f, 1f, 0.6f, 0.9f), 1.2f);
                }

                EstadoAtual = Estado.Preparando;
                preparo.Forcar(tempoDePreparo);
                Frear();
                return;
            }
        }

        // Vai pro canto mais longe do jogador (refaz a escolha de tempo em tempo), deixando os
        // esqueletos no meio do caminho. Nada de passear como os arqueiros.
        trocaDeCanto -= dt;

        if (trocaDeCanto <= 0f || !cantoEscolhido)
        {
            EscolherCanto();
            trocaDeCanto = 2.5f;
        }

        Vector2 falta = canto - rb.position;

        if (falta.magnitude > 0.4f)
            Andar(PeloCaminhoAte(canto, falta), alvo.magnitude < 3f ? velocidade * 1.3f : velocidade * 0.8f);
        else
            Frear();
    }

    private Vector2 canto;
    private bool cantoEscolhido;
    private float trocaDeCanto;

    private void EscolherCanto()
    {
        Sala sala = SalaDoInimigo;

        if (sala == null || jogador == null)
        {
            canto = rb.position;
            return;
        }

        Vector2 centro = sala.transform.position;
        Vector2 meio = sala.TamanhoInterno * 0.5f - Vector2.one * 1.2f;
        float melhor = -1f;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                Vector2 ponto = centro + new Vector2(x * meio.x, y * meio.y);
                float d = Vector2.Distance(ponto, jogador.position);

                if (d > melhor && sala.Livre(ponto - centro, 0.3f))
                {
                    melhor = d;
                    canto = ponto;
                }
            }
        }

        cantoEscolhido = melhor >= 0f;

        if (!cantoEscolhido)
            canto = rb.position;
    }

    protected override void AtualizarPreparando(float dt)
    {
        Frear();
        preparo.Contar(dt);

        if (preparo.Ativo)
            return;

        if (vaiChamar)
            Chamar();
        else
            Disparar(AnguloDoJogador(), 3.2f, danoDaMaldicao, corDaMaldicao, 0.4f);

        recarga.Forcar(intervaloEntreFeiticos);
        pausa.Forcar(0.4f);
        EstadoAtual = Estado.Recuperando;
    }

    protected override void AtualizarRecuperando(float dt)
    {
        Frear();
        pausa.Contar(dt);

        if (!pausa.Ativo)
            EstadoAtual = Estado.Agindo;
    }

    private void Chamar()
    {
        Sala sala = SalaDoInimigo;

        if (sala == null)
            return;

        // O esqueleto sai num ponto livre perto do previsto (nunca dentro de pedra ou parede).
        Vector2 local = pontoDoEsqueleto - (Vector2)sala.transform.position;

        Vector2 meio = sala.TamanhoInterno * 0.5f - Vector2.one;

        if (!sala.Livre(local) || Mathf.Abs(local.x) > meio.x || Mathf.Abs(local.y) > meio.y)
            local = sala.PontoLivreAleatorio(1.2f);

        EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.NuvemVerde, (Vector2)sala.transform.position + local, new Color(0.6f, 1f, 0.6f), 1.4f);
        Sons.Tocar(Som.Feitico, 0.7f);

        InimigoDeSala esqueleto = sala.CriarInimigo(TipoDeInimigo.EsqueletoGuerreiro, local);

        if (esqueleto != null)
            meus.Add(esqueleto);
    }
}
