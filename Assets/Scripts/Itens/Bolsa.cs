using UnityEngine;

/// <summary>
/// As moedas e chaves do jogador (vieram do jogo antigo). Moeda compra na loja, chave abre bau
/// trancado. Cai dos inimigos (<see cref="Coletavel"/>). O <see cref="Herois.Aplicar"/> poe no jogador.
/// (As bombas sairam na 1.18.0: no lugar delas, cada arma tem um tiro especial, ver
/// <see cref="DadosDaArma.especial"/>.)
/// </summary>
[DisallowMultipleComponent]
public class Bolsa : MonoBehaviour
{
    public int Moedas { get; private set; }
    public int Chaves { get; private set; }

    public void Ganhar(int moedas, int chaves)
    {
        Moedas = Mathf.Min(99, Moedas + moedas);
        Chaves = Mathf.Min(99, Chaves + chaves);
    }

    /// <summary>Poe os numeros de volta (o "Continuar").</summary>
    public void Definir(int moedas, int chaves)
    {
        Moedas = moedas;
        Chaves = chaves;
    }

    public bool Gastar(int moedas, int chaves = 0)
    {
        if (Moedas < moedas || Chaves < chaves)
            return false;

        Moedas -= moedas;
        Chaves -= chaves;
        return true;
    }
}
