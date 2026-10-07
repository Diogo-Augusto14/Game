using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Os itens que o jogador pegou e o que eles mudam (vieram do jogo antigo): dano, ritmo, velocidade,
/// tiros a mais, tiros que atravessam ou perseguem, escudo, fenix, orbes... Quem usa os numeros: a
/// <see cref="ArmaDoJogador"/> (ritmo, tiros a mais, tiro pra tras), o <see cref="Projetil"/> (dano,
/// alcance, tamanho, atravessar, perseguir, explodir), a <see cref="Bomba"/> (polvora), a loja
/// (desconto) e os <see cref="Coletavel"/> (ima, sorte). Juntar os dois itens de uma sinergia
/// (<see cref="Sinergias"/>) da o bonus dela.
/// </summary>
[DisallowMultipleComponent]
public class EstatisticasDoJogador : MonoBehaviour, IBloqueioDeDano
{
    private readonly List<ItemPassivo> itens = new List<ItemPassivo>();

    public IReadOnlyList<ItemPassivo> Itens => itens;

    // ---------------- os numeros ----------------
    public float DanoVezes = 1f;
    public float DanoMais;
    public float CadenciaVezes = 1f;
    public float VelocidadeMais;
    public float AlcanceVezes = 1f;
    public float VelocidadeDoTiroVezes = 1f;
    public float TamanhoVezes = 1f;
    public int TirosExtras;
    public bool TiroPraTras;
    public bool Atravessa;
    public bool Persegue;
    public bool ExplodeAoAcertar;
    public bool Fenix;
    public bool Escudo;
    public bool Vampiro;
    public int VampiroCada = 12;
    public bool Prego;
    public bool Ima;
    public float Sorte = 1f;
    public float Desconto = 1f;
    public bool Brasa;
    public float BrasaVezes = 1.6f;
    public float NevoaExtra;
    public int Orbes;
    public float PolvoraVezes = 1f;
    public float CuraAoLimpar;

    private Vida vida;
    private MovimentoDoJogador movimento;
    private bool escudoPronto = true;
    private bool fenixUsada;
    private int mortesProVampiro;
    private readonly List<OrbeGuardiao> orbes = new List<OrbeGuardiao>();

    private Bolsa bolsa;

    public Bolsa Bolsa => bolsa != null ? bolsa : (bolsa = GetComponent<Bolsa>());

    private void Awake()
    {
        vida = GetComponent<Vida>();
        movimento = GetComponent<MovimentoDoJogador>();

        if (vida != null)
        {
            vida.SegundaChance = Renascer;
            vida.AoTomarDano += Apanhou;
        }

        GeradorDoAndar.AoMatarInimigo += Matou;
        GeradorDoAndar.AoLimparAndar += Limpou;
        GeradorDoAndar.AoComecarAndar += ComecouAndar;
    }

    private void OnDestroy()
    {
        GeradorDoAndar.AoMatarInimigo -= Matou;
        GeradorDoAndar.AoLimparAndar -= Limpou;
        GeradorDoAndar.AoComecarAndar -= ComecouAndar;
    }

    public bool Tem(ItemPassivo item) => item != null && itens.Contains(item);

    public bool Tem(string nome) => itens.Exists(i => i.Nome == nome);

    /// <summary>Pega um item passivo: aplica, confere as sinergias e avisa na tela.</summary>
    public void Pegar(ItemPassivo item, bool avisar = true)
    {
        if (item == null || item.EhAtivo || Tem(item))
            return;

        itens.Add(item);
        item.Aplicar?.Invoke(this);

        if (avisar)
        {
            Sons.Tocar(Som.Item, 0.8f, 0f);
            Anunciar(item.Nome, item.Resumo, new Color(1f, 0.9f, 0.55f));
        }

        // Sinergias: os dois itens juntos dao o bonus (uma vez).
        foreach (Sinergias.Sinergia s in Sinergias.Todas)
        {
            if (!Tem(s.Bonus) && Tem(s.A) && Tem(s.B))
            {
                itens.Add(s.Bonus);
                s.Bonus.Aplicar?.Invoke(this);

                if (avisar)
                    Anunciar("Sinergia: " + s.Bonus.Nome + "!", s.Bonus.Resumo, new Color(0.6f, 1f, 0.9f));
            }
        }

        Atualizar();
    }

    private void Anunciar(string titulo, string resumo, Color cor)
    {
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.6f, titulo, cor, 2f);
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.15f, resumo, new Color(0.9f, 0.9f, 0.95f), 2f);
    }

    /// <summary>Muda a vida maxima (Coracao de Leao, Pacto de Sangue).</summary>
    public void MudarVidaMaxima(float quanto)
    {
        if (vida != null)
            vida.MudarMaxima(quanto);
    }

    /// <summary>Poe na velocidade, na vida e nos orbes o que os itens mudaram.</summary>
    public void Atualizar()
    {
        Herois.Heroi heroi = Herois.Atual;

        if (movimento != null && heroi != null)
            movimento.VelocidadeDeAndar = Mathf.Max(2f, heroi.Velocidade + VelocidadeMais);

        if (vida != null)
            vida.TempoSemDanoExtra = NevoaExtra;

        while (orbes.Count < Orbes)
            orbes.Add(OrbeGuardiao.Criar(transform, orbes.Count));
    }

    /// <summary>O multiplicador do dano dos tiros agora (a brasa conta a vida de agora).</summary>
    public float MultiplicadorDoDano(float danoDaArma)
    {
        float brasa = Brasa && vida != null && vida.Atual <= 2f ? BrasaVezes : 1f;
        return danoDaArma <= 0f ? DanoVezes * brasa : (danoDaArma + DanoMais) / danoDaArma * DanoVezes * brasa;
    }

    // ---------------- escudo, fenix, prego, vampiro, carne ----------------
    public bool Bloqueia(Dano dano)
    {
        if (!Escudo || !escudoPronto)
            return false;

        escudoPronto = false;
        Sons.Tocar(Som.Pancada, 0.7f);
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.2f, "Escudo!", new Color(1f, 0.85f, 0.4f), 1f);
        return true;
    }

    private bool Renascer()
    {
        if (!Fenix || fenixUsada)
            return false;

        fenixUsada = true;
        vida.DefinirAtual(Mathf.Max(1f, vida.Maxima * 0.5f));
        Sons.Tocar(Som.Cura, 1f, 0f);
        CameraDoJogo.Tremer(0.3f, 0.3f);
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.4f, "Renasceu!", new Color(1f, 0.55f, 0.3f), 2f).Pular(1.5f);

        // Renascer em chamas (Fenix com Oleo Ardente): um anel de fogo em volta.
        if (Tem("Óleo Ardente"))
            Explosao.Criar(transform.position, 3f, 20f, Lado.Jogador, gameObject);

        return true;
    }

    private void Apanhou(Dano dano)
    {
        if (!Prego || vida.Morto)
            return;

        foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, 2.2f))
        {
            Vida dele = c != null ? c.GetComponentInParent<Vida>() : null;

            if (dele != null && dele.Lado == Lado.Inimigos)
                dele.ReceberDano(new Dano(6f, (Vector2)(dele.transform.position - transform.position), 5f, gameObject));
        }
    }

    private void Matou(Vector2 onde)
    {
        if (!Vampiro || vida == null || vida.Morto)
            return;

        if (++mortesProVampiro >= VampiroCada)
        {
            mortesProVampiro = 0;
            vida.Curar(1f);
            TextoFlutuante.Mostrar(transform.position + Vector3.up * 1f, "+ vida", new Color(1f, 0.35f, 0.4f), 1f);
        }
    }

    private void Limpou()
    {
        if (CuraAoLimpar > 0f && vida != null && !vida.Morto)
            vida.Curar(CuraAoLimpar);
    }

    private void ComecouAndar() => escudoPronto = true;
}
