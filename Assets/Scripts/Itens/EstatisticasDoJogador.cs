using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Os itens passivos que o jogador ja pegou, e o que eles fazem com os numeros dele.
///
/// Guarda os numeros de BASE (os que o boneco tinha antes de qualquer item) e recalcula
/// tudo do zero a cada item novo: soma primeiro, multiplica depois. Assim um
/// multiplicador pego antes de uma soma vale o mesmo que pego depois.
///
/// Fica no jogador. Se nao tiver, o <see cref="Pedestal"/> poe na hora de dar o item.
/// </summary>
[DisallowMultipleComponent]
public class EstatisticasDoJogador : MonoBehaviour
{
    [Header("Limites")]
    [SerializeField, Min(0.1f)] private float danoMinimo = 0.5f;
    [SerializeField, Min(0.1f)] private float cadenciaMinima = 0.8f;
    [SerializeField, Min(1f)] private float velocidadeMinima = 2f;
    [SerializeField, Min(1)] private int lagrimasMaximas = 5;

    private readonly List<ItemPassivo> itens = new List<ItemPassivo>();
    private AtiradorTopDown atirador;
    private MovimentoTopDown movimento;
    private Vida vida;
    private Inventario inventario;
    private EfeitosDosItens efeitos;
    private bool temBase;

    private float danoBase, alcanceBase, cadenciaBase, velocidadeDoTiroBase, tamanhoBase, velocidadeBase;
    private int lagrimasBase;

    public IReadOnlyList<ItemPassivo> Itens => itens;

    /// <summary>Os bonus das duplas de itens completas (<see cref="Sinergias"/>).</summary>
    public IReadOnlyList<ItemPassivo> SinergiasAtivas => sinergias;

    private List<ItemPassivo> sinergias = new List<ItemPassivo>();

    /// <summary>Os itens voltaram de uma partida salva (o painel lateral refaz os icones).</summary>
    public event System.Action AoRestaurar;

    /// <summary>
    /// Devolve os itens de uma partida salva, sem os efeitos de uma vez so (vida maxima e
    /// brindes ja estao no que foi salvo) e sem avisos na tela.
    /// </summary>
    public void Restaurar(List<ItemPassivo> salvos)
    {
        GuardarBase();
        itens.Clear();
        itens.AddRange(salvos);
        sinergias = Sinergias.Ativas(itens);
        Recalcular();

        foreach (ItemPassivo item in itens)
        {
            if (item.flecha != TipoDeFlecha.Normal)
                TrocaDeFlecha.Em(gameObject).Ganhar(item.flecha);

            // A partida salva guarda so os itens: as armas voltam com o pente e a municao cheios.
            if (item.arma != null)
                ArsenalDoJogador.Em(gameObject).Ganhar(item.arma);
        }

        AoRestaurar?.Invoke();
    }

    /// <summary>Formou uma sinergia nova (a HUD anuncia).</summary>
    public event System.Action<ItemPassivo> AoFormarSinergia;

    /// <summary>Pegou um item. O HUD escuta pra mostrar o nome na tela.</summary>
    public event System.Action<ItemPassivo> AoPegarItem;

    private void Awake()
    {
        atirador = GetComponent<AtiradorTopDown>();
        movimento = GetComponent<MovimentoTopDown>();
        vida = GetComponent<Vida>();
        inventario = GetComponent<Inventario>();

        // Os itens de efeito especial (escudo, ima, orbe...) moram num componente proprio.
        if (!TryGetComponent(out efeitos))
            efeitos = gameObject.AddComponent<EfeitosDosItens>();
    }

    private void GuardarBase()
    {
        if (temBase)
            return;

        temBase = true;

        if (atirador != null)
        {
            danoBase = atirador.Dano;
            alcanceBase = atirador.Alcance;
            cadenciaBase = atirador.TirosPorSegundo;
            velocidadeDoTiroBase = atirador.VelocidadeDoTiro;
            tamanhoBase = atirador.Tamanho;
            lagrimasBase = atirador.LagrimasPorDisparo;
        }

        if (movimento != null)
            velocidadeBase = movimento.VelocidadeMaxima;
    }

    /// <summary>
    /// Da o item ao jogador: numeros, vida maxima e os brindes de uma vez so. Item ativo vai
    /// pro <see cref="ItemAtivoDoJogador"/>; se ja tinha um, o antigo e devolvido (e, com
    /// <paramref name="largarNoChao"/>, fica num pedestal no chao).
    /// </summary>
    public ItemPassivo Pegar(ItemPassivo item, bool largarNoChao = true)
    {
        if (item == null)
            return null;

        if (item.EAtivo)
        {
            ItemPassivo antigo = ItemAtivoDoJogador.Em(gameObject).Equipar(item);

            if (antigo != null && largarNoChao)
            {
                Sala sala = Andar.Atual != null ? Andar.Atual.NoMundo(Andar.Atual.SalaAtual) : null;
                Pedestal.Largar(antigo, (Vector2)transform.position + Vector2.down * 1.2f, sala != null ? sala.transform : null);
                antigo = null;
            }

            Sons.Tocar(Som.Item);
            Registro.PegouItem();
            AoPegarItem?.Invoke(item);
            return antigo;
        }

        GuardarBase();
        itens.Add(item);

        List<ItemPassivo> novas = Sinergias.Ativas(itens);
        List<ItemPassivo> formadas = novas.FindAll(s => !sinergias.Contains(s));
        sinergias = novas;
        Recalcular();
        AplicarUmaVez(item);

        // Flecha nova vai pra aljava e ja entra em uso.
        if (item.flecha != TipoDeFlecha.Normal)
            TrocaDeFlecha.Em(gameObject).Ganhar(item.flecha);

        // Arma de fogo nova vai pro arsenal e ja vai pra mao.
        if (item.arma != null)
            ArsenalDoJogador.Em(gameObject).Ganhar(item.arma);

        Sons.Tocar(Som.Item);
        Debug.Log($"[Itens] pegou {item.nome}: {item.descricao}");
        Registro.PegouItem();
        AoPegarItem?.Invoke(item);

        foreach (ItemPassivo sinergia in formadas)
        {
            AplicarUmaVez(sinergia);
            Sons.Tocar(Som.Feitico, 0.8f);
            Impacto.Tremer(0.1f, 0.25f);
            TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.2f, "Sinergia!", new Color(1f, 0.85f, 0.4f));
            AoFormarSinergia?.Invoke(sinergia);
            Registro.FormouSinergia();
        }

        return null;
    }

    /// <summary>
    /// Vida maxima e brindes nao sao "de base": aplicam uma vez, na hora de pegar (ou de
    /// formar a sinergia).
    /// </summary>
    private void AplicarUmaVez(ItemPassivo item)
    {
        // Item que tira vida maxima (Pacto de Sangue) nunca deixa menos de um coracao.
        if (vida != null && !Mathf.Approximately(item.somaVidaMaxima, 0f))
        {
            float quanto = item.somaVidaMaxima;

            if (quanto < 0f)
                quanto = Mathf.Max(quanto, Mathf.Min(0f, 20f - vida.VidaMaxima));

            vida.AumentarVidaMaxima(quanto);
        }

        if (inventario == null)
            inventario = GetComponent<Inventario>();

        if (inventario != null)
        {
            inventario.Adicionar(TipoDeColetavel.Moeda, item.moedas);
            inventario.Adicionar(TipoDeColetavel.Chave, item.chaves);
            inventario.Adicionar(TipoDeColetavel.Bomba, item.bombas);
        }
    }

    /// <summary>Refaz as contas sem item novo (a furia liga e desliga conforme a vida).</summary>
    public void Atualizar()
    {
        if (temBase)
            Recalcular();
    }

    private void Recalcular()
    {
        float somaDano = 0f, multDano = 1f, somaCadencia = 0f, multCadencia = 1f;
        float somaAlcance = 0f, somaVelTiro = 0f, somaTamanho = 0f, somaVelocidade = 0f;
        int extras = 0;
        bool atravessa = false, teleguiada = false, paraTras = false;
        Color? corDaLagrima = null;
        bool pesado = false;
        float explosao = 0f;

        List<ItemPassivo> todos = new List<ItemPassivo>(itens);
        todos.AddRange(sinergias);

        foreach (ItemPassivo i in todos)
        {
            pesado |= i.golpePesado;
            explosao = Mathf.Max(explosao, i.explodeAoAcertar);

            somaDano += i.somaDano;
            multDano *= i.multiplicaDano;
            somaCadencia += i.somaCadencia;
            multCadencia *= i.multiplicaCadencia;
            somaAlcance += i.somaAlcance;
            somaVelTiro += i.somaVelocidadeDoTiro;
            somaTamanho += i.somaTamanhoDaLagrima;
            somaVelocidade += i.somaVelocidade;
            extras += i.lagrimasExtras;
            atravessa |= i.atravessa;
            teleguiada |= i.teleguiada;
            paraTras |= i.paraTras;

            // O ultimo item com cor de lagrima manda na cor.
            if (i.corDaLagrima.a > 0f)
                corDaLagrima = i.corDaLagrima;
        }

        if (efeitos != null)
        {
            efeitos.Aplicar(todos);
            multDano *= efeitos.MultiplicadorDeDano;
        }

        if (atirador != null)
        {
            float danoFinal = Mathf.Max(danoMinimo, (danoBase + somaDano) * multDano);
            float alcanceFinal = alcanceBase + somaAlcance;
            float cadenciaFinal = Mathf.Max(cadenciaMinima, (cadenciaBase + somaCadencia) * multCadencia);

            atirador.Configurar(danoFinal, alcanceFinal, cadenciaFinal);

            // As armas de fogo usam os numeros delas, subidos ou descidos pelo que os itens fizeram
            // com os do heroi (o heroi de dano alto nao deixa a arma de fogo mais forte).
            atirador.DefinirFatores(danoFinal / Mathf.Max(0.01f, danoBase),
                                    cadenciaFinal / Mathf.Max(0.01f, cadenciaBase),
                                    alcanceFinal / Mathf.Max(0.01f, alcanceBase));

            atirador.ConfigurarLagrima(
                Mathf.Max(3f, velocidadeDoTiroBase + somaVelTiro),
                tamanhoBase + somaTamanho,
                Mathf.Min(lagrimasMaximas, lagrimasBase + extras));

            atirador.DefinirEfeitos(atravessa, teleguiada, paraTras, corDaLagrima);
            atirador.DefinirSinergias(pesado, explosao);
        }

        if (movimento != null)
            movimento.DefinirVelocidadeMaxima(Mathf.Max(velocidadeMinima, velocidadeBase + somaVelocidade));
    }
}
