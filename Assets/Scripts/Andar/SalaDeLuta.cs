using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Uma sala de luta do andar (o ritmo das salas do jogo antigo, na planta nova): os inimigos dormem
/// la dentro ate o jogador entrar. Entrou, as grades fecham (<see cref="PortaDaSala"/>), todo mundo
/// acorda, e so abre com todos mortos. Algumas salas tem uma segunda onda, que sai do chao quando a
/// primeira acaba. Limpou, cai o premio da sala (moedas, as vezes chave, bomba, coracao ou um bau).
///
/// Quem nasce no meio da luta (o esqueleto do Necromante, as bolinhas da Bolha) entra na conta da
/// sala fechada (<see cref="Adotar"/>).
/// </summary>
public class SalaDeLuta : MonoBehaviour
{
    private static readonly List<SalaDeLuta> todas = new List<SalaDeLuta>();

    private SalaDaPlanta planta;
    private GeradorDoAndar gerador;
    private Sprite[] quadrosDoBau;
    private readonly List<Vida> inimigos = new List<Vida>();
    private readonly List<PortaDaSala> portas = new List<PortaDaSala>();
    private List<Vector2Int> lugares;
    private Transform jogador;
    private int ondasAMais;
    private int porOnda;
    private bool vindoOnda;

    /// <summary>A sala fechada agora (nula fora de luta).</summary>
    public static SalaDeLuta Fechada { get; private set; }

    /// <summary>As salas de luta do andar.</summary>
    public static IReadOnlyList<SalaDeLuta> Todas => todas;

    public bool Limpa { get; private set; }

    public Vector2 Meio => planta.Meio;

    public static SalaDeLuta Criar(SalaDaPlanta planta, Transform pai, GeradorDoAndar gerador, HashSet<Vector2Int> chao,
                                   int ondasAMais, int porOnda, Sprite[] quadrosDoBau)
    {
        GameObject obj = new GameObject(planta.tipo == TipoDeSala.Fim ? "Sala do fim" : "Sala de luta");
        obj.transform.SetParent(pai, false);
        obj.transform.position = planta.Meio;

        SalaDeLuta sala = obj.AddComponent<SalaDeLuta>();
        sala.planta = planta;
        sala.gerador = gerador;
        sala.ondasAMais = ondasAMais;
        sala.porOnda = porOnda;
        sala.quadrosDoBau = quadrosDoBau;
        sala.lugares = new List<Vector2Int>();

        foreach (Vector2Int c in planta.celulas)
        {
            if (chao.Contains(c) && planta.BemDentro(c, 1.5f) && Cercada(chao, c))
                sala.lugares.Add(c);
        }

        foreach (PortaDaPlanta porta in planta.portas)
            sala.portas.Add(PortaDaSala.Criar(porta, obj.transform));

        todas.Add(sala);
        return sala;
    }

    /// <summary>Um inimigo desta sala (dorme ate o jogador entrar).</summary>
    public void Adicionar(Vida inimigo)
    {
        if (inimigo == null)
            return;

        inimigos.Add(inimigo);

        if (inimigo.TryGetComponent(out InimigoAtirador atirador))
            atirador.EsperaASala = true;
    }

    /// <summary>Quem nasceu no meio da luta conta pra sala fechada (se tiver uma).</summary>
    public static void Adotar(Vida inimigo)
    {
        if (Fechada != null && inimigo != null && !Fechada.inimigos.Contains(inimigo))
            Fechada.inimigos.Add(inimigo);
    }

    private void OnDestroy()
    {
        todas.Remove(this);

        if (Fechada == this)
            Fechada = null;
    }

    private void Update()
    {
        if (Limpa)
            return;

        inimigos.RemoveAll(v => v == null || v.Morto);

        if (Fechada != this)
        {
            if (jogador == null)
            {
                GameObject j = GameObject.FindWithTag("Player");
                jogador = j != null ? j.transform : null;
            }

            // Mataram todo mundo de fora (pela porta): limpa sem fechar.
            if (inimigos.Count == 0 && ondasAMais == 0)
            {
                Limpar(false);
                return;
            }

            if (jogador != null && Fechada == null && planta.BemDentro(jogador.position, 1.2f))
                Fechar();

            return;
        }

        if (inimigos.Count > 0 || vindoOnda)
            return;

        if (ondasAMais > 0)
            StartCoroutine(Onda());
        else
            Limpar(true);
    }

    private void Fechar()
    {
        Fechada = this;

        foreach (PortaDaSala porta in portas)
            porta.Fechar();

        if (portas.Count > 0)
            Sons.Tocar(Som.PortaFecha);

        foreach (Vida inimigo in inimigos)
        {
            // Quem saiu atras do jogador (acordou com um tiro pela porta) volta pra dentro.
            if (!planta.celulas.Contains(MapaDeCaminhos.Celula(inimigo.transform.position)) && lugares.Count > 0)
            {
                Vector2 dentro = Lugar(2f);
                inimigo.transform.position = dentro;

                if (inimigo.TryGetComponent(out Rigidbody2D corpo))
                    corpo.position = dentro;

                inimigo.gameObject.AddComponent<SaindoDoChao>().Comecar(0.4f);
            }

            if (inimigo.TryGetComponent(out InimigoAtirador atirador))
            {
                atirador.EsperaASala = false;
                atirador.Acordar();
            }
        }

        // Sem ninguem (so a onda): ela vem ja.
        if (inimigos.Count == 0 && ondasAMais > 0)
            StartCoroutine(Onda());
    }

    private IEnumerator Onda()
    {
        vindoOnda = true;
        ondasAMais--;
        yield return new WaitForSeconds(0.7f);

        for (int i = 0; i < porOnda && gerador != null; i++)
        {
            Vida nova = gerador.CriarInimigo(Lugar(3f));

            if (nova != null && !inimigos.Contains(nova))
                inimigos.Add(nova);
        }

        vindoOnda = false;
    }

    private void Limpar(bool estavaFechada)
    {
        Limpa = true;

        if (Fechada == this)
            Fechada = null;

        foreach (PortaDaSala porta in portas)
            porta.Abrir();

        if (estavaFechada && portas.Count > 0)
            Sons.Tocar(Som.PortaAbre);

        Premio();

        if (gerador != null)
            gerador.SalaLimpa(this, planta.tipo == TipoDeSala.Fim);
    }

    /// <summary>Onde o portal abre na sala do fim: embaixo do meio, longe do premio e do jogador (nunca leva sem querer).</summary>
    public Vector2 LugarDoPortal()
    {
        Vector2 alvo = planta.Meio + Vector2.down * 3f;
        Vector2 melhor = planta.Meio;
        float menor = float.MaxValue;

        foreach (Vector2Int c in lugares)
        {
            float d = Vector2.Distance(c, alvo);

            if (jogador != null && Vector2.Distance(c, jogador.position) < 2.5f)
                d += 100f;

            if (d < menor)
            {
                menor = d;
                melhor = c;
            }
        }

        return melhor;
    }

    // O premio da sala, no meio dela (ou do lado, se o jogador estiver em cima).
    private void Premio()
    {
        Vector2 onde = planta.Meio;

        if (lugares.Count > 0 && !lugares.Contains(MapaDeCaminhos.Celula(onde)))
            onde = lugares[lugares.Count / 2];

        if (jogador != null && Vector2.Distance(jogador.position, onde) < 1.5f)
            onde += Vector2.right * 2f;

        int moedas = Random.Range(2, 5);

        for (int i = 0; i < moedas; i++)
            Coletavel.Criar(TipoDeColetavel.Moeda, onde + Random.insideUnitCircle * 0.6f);

        float sorte = Random.value;

        if (sorte < 0.15f)
            Coletavel.Criar(TipoDeColetavel.Chave, onde);
        else if (sorte < 0.3f)
            Coletavel.Criar(TipoDeColetavel.Bomba, onde);
        else if (sorte < 0.5f)
            Coletavel.Criar(TipoDeColetavel.Coracao, onde);

        // As vezes um bau (sempre na sala do fim).
        if (quadrosDoBau != null && quadrosDoBau.Length > 0 && gerador != null
            && (planta.tipo == TipoDeSala.Fim || Random.value < 0.12f))
        {
            Vector2 doBau = onde + Vector2.up * 1.5f;

            if (!lugares.Contains(MapaDeCaminhos.Celula(doBau)))
                doBau = onde;

            Bau.Criar(quadrosDoBau, doBau, transform, gerador.ArmasDoBau, null, null, null);
        }
    }

    // Um lugar livre da sala, longe do jogador.
    private Vector2 Lugar(float longeDoJogador)
    {
        if (lugares.Count == 0)
            return planta.Meio;

        for (int tentativa = 0; tentativa < 30; tentativa++)
        {
            Vector2 c = lugares[Random.Range(0, lugares.Count)];

            if (jogador == null || Vector2.Distance(c, jogador.position) >= longeDoJogador)
                return c;
        }

        return lugares[Random.Range(0, lugares.Count)];
    }

    private static bool Cercada(HashSet<Vector2Int> chao, Vector2Int c)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (!chao.Contains(c + new Vector2Int(dx, dy)))
                    return false;

        return true;
    }
}
