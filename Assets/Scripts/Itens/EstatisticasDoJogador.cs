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
            AoPegarItem?.Invoke(item);
            return antigo;
        }

        GuardarBase();
        itens.Add(item);
        Recalcular();

        // Vida maxima e brindes nao sao "de base": aplicam uma vez, na hora de pegar.
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

        // Flecha nova vai pra aljava e ja entra em uso.
        if (item.flecha != TipoDeFlecha.Normal)
            TrocaDeFlecha.Em(gameObject).Ganhar(item.flecha);

        Sons.Tocar(Som.Item);
        Debug.Log($"[Itens] pegou {item.nome}: {item.descricao}");
        AoPegarItem?.Invoke(item);
        return null;
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

        foreach (ItemPassivo i in itens)
        {
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
            efeitos.Aplicar(itens);
            multDano *= efeitos.MultiplicadorDeDano;
        }

        if (atirador != null)
        {
            atirador.Configurar(
                Mathf.Max(danoMinimo, (danoBase + somaDano) * multDano),
                alcanceBase + somaAlcance,
                Mathf.Max(cadenciaMinima, (cadenciaBase + somaCadencia) * multCadencia));

            atirador.ConfigurarLagrima(
                Mathf.Max(3f, velocidadeDoTiroBase + somaVelTiro),
                tamanhoBase + somaTamanho,
                Mathf.Min(lagrimasMaximas, lagrimasBase + extras));

            atirador.DefinirEfeitos(atravessa, teleguiada, paraTras, corDaLagrima);
        }

        if (movimento != null)
            movimento.DefinirVelocidadeMaxima(Mathf.Max(velocidadeMinima, velocidadeBase + somaVelocidade));
    }
}
