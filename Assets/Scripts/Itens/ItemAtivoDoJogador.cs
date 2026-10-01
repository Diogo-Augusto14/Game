using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>O que um item ativo faz ao ser usado (ver <see cref="ItemAtivoDoJogador"/>).</summary>
public enum TipoDeAtivo
{
    Nenhum,
    EspiralDoTempo,
    CristaisDoTrovao,
    MoedaDoDestino,
    CometaDeFogo,
    PessegoEncantado,
    BombaEterna,
}

/// <summary>Os itens ativos que entram no sorteio junto com os passivos (<see cref="CatalogoDeItens"/>).</summary>
public static class CatalogoDeAtivos
{
    public static IEnumerable<ItemPassivo> Todos()
    {
        yield return new ItemPassivo("Espiral do Tempo", "Ativo: o tempo se arrasta", new Color(0.3f, 0.95f, 0.8f))
        {
            Descricao = "Item ativo (Espaço): por 6 segundos os inimigos andam a um terço da velocidade. Recarrega em 3 salas.",
            ativo = TipoDeAtivo.EspiralDoTempo, cargas = 3
        };

        yield return new ItemPassivo("Cristais do Trovão", "Ativo: raio em todos", new Color(0.75f, 0.85f, 1f))
        {
            Descricao = "Item ativo (Espaço): um raio cai em cada inimigo da sala (30 de dano). Recarrega em 3 salas.",
            ativo = TipoDeAtivo.CristaisDoTrovao, cargas = 3
        };

        yield return new ItemPassivo("Moeda do Destino", "Ativo: troca os itens", new Color(1f, 0.75f, 0.2f))
        {
            Descricao = "Item ativo (Espaço): troca por outros os itens dos pedestais da sala. Recarrega em 4 salas.",
            ativo = TipoDeAtivo.MoedaDoDestino, cargas = 4
        };

        yield return new ItemPassivo("Cometa de Fogo", "Ativo: anel de chamas", new Color(1f, 0.5f, 0.15f))
        {
            Descricao = "Item ativo (Espaço): solta 12 bolas de fogo em volta de você. Recarrega em 2 salas.",
            ativo = TipoDeAtivo.CometaDeFogo, cargas = 2
        };

        yield return new ItemPassivo("Pêssego Encantado", "Ativo: cura um coração", new Color(1f, 0.7f, 0.55f))
        {
            Descricao = "Item ativo (Espaço): recupera um coração. Recarrega em 4 salas.",
            ativo = TipoDeAtivo.PessegoEncantado, cargas = 4
        };

        yield return new ItemPassivo("Bomba Eterna", "Ativo: bomba de graça", new Color(0.45f, 0.75f, 0.35f))
        {
            Descricao = "Item ativo (Espaço): solta uma bomba sem gastar as suas. Recarrega a cada sala.",
            ativo = TipoDeAtivo.BombaEterna, cargas = 1
        };
    }

    /// <summary>Icone do item ativo (Raven Fantasy Icons), ou null se nao for um deles.</summary>
    public static Sprite Icone(string nome)
    {
        switch (FonteDoJogo.SemAcentos(nome))
        {
            case "Espiral do Tempo": return ArteImportada.IconeDoPacote(720);
            case "Cristais do Trovao": return ArteImportada.IconeDoPacote(159);
            case "Moeda do Destino": return ArteImportada.IconeDoPacote(131);
            case "Cometa de Fogo": return ArteImportada.IconeDoPacote(1002);
            case "Pessego Encantado": return ArteImportada.IconeDoPacote(440);
            case "Bomba Eterna": return ArteImportada.IconeDoPacote(778);
            default: return null;
        }
    }
}

/// <summary>
/// O item ativo do jogador, como no Isaac: um so de cada vez, usado com Espaco (RT no
/// controle). Usar gasta a carga inteira; cada sala limpa devolve uma. Pegar outro ativo troca:
/// o antigo fica no pedestal (ou no chao) pra quem mudar de ideia.
///
/// Fica no jogador; o <see cref="EstatisticasDoJogador"/> poe sozinho na hora de pegar.
/// A HUD (<see cref="HudDoInventario"/>) desenha o icone e as cargas.
/// </summary>
[DisallowMultipleComponent]
public class ItemAtivoDoJogador : MonoBehaviour
{
    [SerializeField] private KeyCode tecla = KeyCode.Space;

    private Andar andar;
    private Sala salaAtual;
    private readonly HashSet<Sala> salasOuvidas = new HashSet<Sala>();

    public ItemPassivo Item { get; private set; }

    public int Cargas { get; private set; }

    public int CargasMaximas => Item != null ? Mathf.Max(1, Item.cargas) : 0;

    public bool Pronto => Item != null && Cargas >= CargasMaximas;

    /// <summary>Mudou o item ou as cargas (a HUD escuta).</summary>
    public event System.Action AoMudar;

    /// <summary>Usou o item agora (pra conquista e estatistica).</summary>
    public static event System.Action<ItemPassivo> AoUsar;

    public static ItemAtivoDoJogador Em(GameObject jogador)
    {
        ItemAtivoDoJogador ativo = jogador.GetComponent<ItemAtivoDoJogador>();
        return ativo != null ? ativo : jogador.AddComponent<ItemAtivoDoJogador>();
    }

    /// <summary>Equipa o item, carregado. Devolve o ativo que estava antes (ou null).</summary>
    public ItemPassivo Equipar(ItemPassivo novo, int cargas = -1)
    {
        ItemPassivo antes = Item;
        Item = novo;
        Cargas = cargas < 0 ? CargasMaximas : Mathf.Clamp(cargas, 0, CargasMaximas);
        AoMudar?.Invoke();
        return antes;
    }

    private void OnDisable()
    {
        if (andar != null)
            andar.AoEntrarNaSala -= AoEntrarNaSala;

        andar = null;
    }

    private void Update()
    {
        if (andar == null && Andar.Atual != null)
        {
            andar = Andar.Atual;
            andar.AoEntrarNaSala += AoEntrarNaSala;

            // Pegou o primeiro ativo no meio de uma sala: ela conta como a sala atual.
            AoEntrarNaSala(andar.NoMundo(andar.SalaAtual));
        }

        if (Time.timeScale <= 0f || Item == null)
            return;

        if (Input.GetKeyDown(tecla) || Controle.Apertou(BotaoDoControle.RT))
            Usar();
    }

    private void AoEntrarNaSala(Sala sala)
    {
        salaAtual = sala;

        if (sala == null || sala.Limpa || !salasOuvidas.Add(sala))
            return;

        sala.AoLimpar.AddListener(Recarregar);
    }

    /// <summary>Uma carga a mais (sala limpa).</summary>
    public void Recarregar()
    {
        if (Item == null || Cargas >= CargasMaximas)
            return;

        Cargas++;

        if (Cargas >= CargasMaximas)
        {
            Sons.Tocar(Som.Item, 0.6f);
            TextoFlutuante.Mostrar(transform.position + Vector3.up * 1f, "Item pronto!", new Color(0.6f, 1f, 0.7f));
        }

        AoMudar?.Invoke();
    }

    public void Usar()
    {
        Vida vida = GetComponent<Vida>();

        if (vida != null && vida.EstaMorto)
            return;

        if (!Pronto)
        {
            Sons.Tocar(Som.Menu, 0.5f);
            return;
        }

        if (!Efeito(Item.ativo))
            return;

        Cargas = 0;
        AoMudar?.Invoke();
        AoUsar?.Invoke(Item);
    }

    // ================================================================ efeitos
    /// <summary>Faz o efeito. False = nao tinha o que fazer (a carga nao e gasta).</summary>
    private bool Efeito(TipoDeAtivo tipo)
    {
        switch (tipo)
        {
            case TipoDeAtivo.EspiralDoTempo: return EspiralDoTempo();
            case TipoDeAtivo.CristaisDoTrovao: return CristaisDoTrovao();
            case TipoDeAtivo.MoedaDoDestino: return MoedaDoDestino();
            case TipoDeAtivo.CometaDeFogo: return CometaDeFogo();
            case TipoDeAtivo.PessegoEncantado: return Pessego();
            case TipoDeAtivo.BombaEterna:
                Bomba.Criar(transform.position, gameObject);
                return true;
            default:
                return false;
        }
    }

    /// <summary>Os inimigos vivos da sala onde o jogador esta (todos, se nao souber a sala).</summary>
    private List<InimigoDeSala> InimigosDaSala()
    {
        List<InimigoDeSala> lista = new List<InimigoDeSala>();

        foreach (InimigoDeSala inimigo in InimigoDeSala.Ativos)
        {
            if (inimigo == null || inimigo.EstadoAtual == InimigoDeSala.Estado.Morto)
                continue;

            if (salaAtual == null || inimigo.GetComponentInParent<Sala>() == salaAtual)
                lista.Add(inimigo);
        }

        return lista;
    }

    private bool EspiralDoTempo()
    {
        List<InimigoDeSala> inimigos = InimigosDaSala();

        if (inimigos.Count == 0)
            return Nada("Nenhum inimigo");

        StartCoroutine(TempoLento(inimigos, 0.35f, 6f));
        Sons.Tocar(Som.Feitico);
        Impacto.Tremer(0.12f, 0.3f);
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 1f, "O tempo se arrasta...", new Color(0.4f, 1f, 0.85f));
        return true;
    }

    private static IEnumerator TempoLento(List<InimigoDeSala> inimigos, float lentidao, float segundos)
    {
        // Sem tingir: o Vida restaura a cor a cada golpe e o tom se perderia. A nuvem marca quem esta lento.
        foreach (InimigoDeSala inimigo in inimigos)
        {
            inimigo.MultiplicadorDeVelocidade = Mathf.Min(inimigo.MultiplicadorDeVelocidade, lentidao);
            EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.NuvemVerde, inimigo.transform.position, new Color(0.4f, 1f, 0.9f, 0.7f), 1f);
        }

        yield return new WaitForSeconds(segundos);

        foreach (InimigoDeSala inimigo in inimigos)
        {
            if (inimigo == null || inimigo.EstadoAtual == InimigoDeSala.Estado.Morto)
                continue;

            if (Mathf.Approximately(inimigo.MultiplicadorDeVelocidade, lentidao))
                inimigo.MultiplicadorDeVelocidade = 1f;
        }
    }

    private bool CristaisDoTrovao()
    {
        List<InimigoDeSala> inimigos = InimigosDaSala();

        if (inimigos.Count == 0)
            return Nada("Nenhum inimigo");

        foreach (InimigoDeSala inimigo in inimigos)
        {
            Vector2 ponto = inimigo.transform.position;
            EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Cristais, ponto, new Color(0.8f, 0.9f, 1f), 1.4f);

            if (inimigo.TryGetComponent(out Vida alvo))
                alvo.TomarDano(new DanoInfo(30f, ponto - (Vector2)transform.position, 2f, ponto, gameObject, PesoDoGolpe.Forte, true));
        }

        Sons.Tocar(Som.Explosao, 0.8f);
        Impacto.Tremer(0.35f, 0.4f);
        Impacto.Congelar(0.06f);
        return true;
    }

    private bool MoedaDoDestino()
    {
        if (salaAtual == null || Andar.Atual == null)
            return Nada("Nada para trocar");

        bool trocou = false;

        foreach (Pedestal pedestal in salaAtual.GetComponentsInChildren<Pedestal>())
        {
            if (pedestal.Vazio)
                continue;

            pedestal.Trocar(CatalogoDeItens.Sortear(Andar.Atual.ItensQueJaSairam));
            EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Poeira, pedestal.transform.position, new Color(1f, 0.85f, 0.4f), 1.2f);
            trocou = true;
        }

        if (!trocou)
            return Nada("Nada para trocar");

        Sons.Tocar(Som.Moeda);
        return true;
    }

    private bool CometaDeFogo()
    {
        AtiradorTopDown atirador = GetComponent<AtiradorTopDown>();
        float dano = Mathf.Max(12f, (atirador != null ? atirador.Dano : 10f) * 2f);

        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad;
            Vector2 rumo = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            TiroDaSala.Disparar((Vector2)transform.position + rumo * 0.5f, rumo * 7f, dano, gameObject, false,
                                new Color(1f, 0.55f, 0.15f), 0.4f);
        }

        Sons.Tocar(Som.TiroDeFogo);
        Impacto.Tremer(0.1f, 0.2f);
        return true;
    }

    private bool Pessego()
    {
        Vida vida = GetComponent<Vida>();

        if (vida == null || vida.VidaAtual >= vida.VidaMaxima)
            return Nada("Vida cheia");

        vida.Curar(20f);
        Sons.Tocar(Som.Cura);
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 0.9f, "+ vida", new Color(1f, 0.35f, 0.4f));
        return true;
    }

    private bool Nada(string motivo)
    {
        Sons.Tocar(Som.Menu, 0.5f);
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 0.9f, motivo, new Color(0.85f, 0.85f, 0.85f));
        return false;
    }
}
