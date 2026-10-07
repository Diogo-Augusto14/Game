using UnityEngine;

/// <summary>
/// As moedas, chaves e bombas do jogador (vieram do jogo antigo). Moeda compra na loja, chave abre
/// bau trancado, bomba (G) explode inimigo, mesa e pedra rachada. Cai dos inimigos
/// (<see cref="Coletavel"/>). O <see cref="Herois.Aplicar"/> poe no jogador.
/// </summary>
[DisallowMultipleComponent]
public class Bolsa : MonoBehaviour
{
    public int Moedas { get; private set; }
    public int Chaves { get; private set; }
    public int Bombas { get; private set; } = 1;

    private ControlesDoJogador controles;
    private EstatisticasDoJogador estatisticas;
    private Vida vida;
    private float proximaBomba;

    private void Awake()
    {
        controles = GetComponent<ControlesDoJogador>();
        vida = GetComponent<Vida>();
    }

    public void Ganhar(int moedas, int chaves, int bombas)
    {
        Moedas = Mathf.Min(99, Moedas + moedas);
        Chaves = Mathf.Min(99, Chaves + chaves);
        Bombas = Mathf.Min(99, Bombas + bombas);
    }

    /// <summary>Poe os numeros de volta (o "Continuar").</summary>
    public void Definir(int moedas, int chaves, int bombas)
    {
        Moedas = moedas;
        Chaves = chaves;
        Bombas = bombas;
    }

    public bool Gastar(int moedas, int chaves = 0, int bombas = 0)
    {
        if (Moedas < moedas || Chaves < chaves || Bombas < bombas)
            return false;

        Moedas -= moedas;
        Chaves -= chaves;
        Bombas -= bombas;
        return true;
    }

    private void Update()
    {
        if (controles == null || !controles.isActiveAndEnabled || !controles.SoltouBomba || (vida != null && vida.Morto))
            return;

        if (Time.time < proximaBomba)
            return;

        if (Bombas <= 0)
        {
            Sons.Tocar(Som.Negado, 0.6f);
            return;
        }

        Bombas--;
        proximaBomba = Time.time + 0.4f;
        SoltarBomba();
    }

    /// <summary>Uma bomba acesa no pe do jogador (a Bomba Eterna tambem chama).</summary>
    public void SoltarBomba()
    {
        if (estatisticas == null)
            TryGetComponent(out estatisticas);

        float polvora = estatisticas != null ? estatisticas.PolvoraVezes : 1f;
        Bomba.Criar(transform.position, gameObject, 2f * Mathf.Sqrt(polvora), 25f * polvora);
    }
}
