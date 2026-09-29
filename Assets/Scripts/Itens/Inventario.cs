using UnityEngine;

/// <summary>
/// Os contadores do Isaac: moedas, chaves e bombas. Fica no jogador.
///
///   Moeda  -> guardada pra loja (ainda nao existe)
///   Chave  -> abre a porta trancada da sala do item, a partir do andar 2
///   Bomba  -> <c>E</c> (LB/LT no controle) solta uma bomba no chao, que explode e machuca todo mundo perto
/// </summary>
[DisallowMultipleComponent]
public class Inventario : MonoBehaviour
{
    [Tooltip("O Isaac comeca com uma bomba")]
    [SerializeField, Min(0)] private int moedas;
    [SerializeField, Min(0)] private int chaves;
    [SerializeField, Min(0)] private int bombas = 1;

    [SerializeField, Min(1)] private int maximo = 99;

    [Tooltip("Solta uma bomba")]
    [SerializeField] private KeyCode teclaDaBomba = KeyCode.E;

    public int Moedas => moedas;
    public int Chaves => chaves;
    public int Bombas => bombas;

    /// <summary>Algum contador mudou. A HUD escuta.</summary>
    public event System.Action AoMudar;

    private void Update()
    {
        // Jogo pausado (menu, pausa, fim de jogo): a tecla nao solta bomba escondida.
        if (Time.timeScale > 0f && (Input.GetKeyDown(teclaDaBomba) || Controle.ApertouBomba))
            SoltarBomba();
    }

    public int Quantos(TipoDeColetavel tipo)
    {
        switch (tipo)
        {
            case TipoDeColetavel.Moeda: return moedas;
            case TipoDeColetavel.Chave: return chaves;
            case TipoDeColetavel.Bomba: return bombas;
            default: return 0;
        }
    }

    /// <summary>Soma (ou tira, se negativo) de um contador. Coracao nao e contador: ignora.</summary>
    public void Adicionar(TipoDeColetavel tipo, int quantos)
    {
        if (quantos == 0)
            return;

        switch (tipo)
        {
            case TipoDeColetavel.Moeda: moedas = Mathf.Clamp(moedas + quantos, 0, maximo); break;
            case TipoDeColetavel.Chave: chaves = Mathf.Clamp(chaves + quantos, 0, maximo); break;
            case TipoDeColetavel.Bomba: bombas = Mathf.Clamp(bombas + quantos, 0, maximo); break;
            default: return;
        }

        AoMudar?.Invoke();
    }

    /// <summary>Gasta um se tiver. False = nao tinha.</summary>
    public bool Gastar(TipoDeColetavel tipo)
    {
        if (Quantos(tipo) <= 0)
            return false;

        Adicionar(tipo, -1);
        return true;
    }

    public bool CabeMais(TipoDeColetavel tipo) => tipo == TipoDeColetavel.Coracao || Quantos(tipo) < maximo;

    public void SoltarBomba()
    {
        Vida vida = GetComponent<Vida>();

        if (vida != null && vida.EstaMorto)
            return;

        if (!Gastar(TipoDeColetavel.Bomba))
            return;

        Bomba.Criar(transform.position, gameObject);
    }
}
