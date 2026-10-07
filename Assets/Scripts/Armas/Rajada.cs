using UnityEngine;

/// <summary>
/// Solta os disparos de uma arma em rajada: <see cref="DadosDaArma.rajada"/> disparos seguidos, um a
/// cada <see cref="DadosDaArma.intervaloDaRajada"/>, cada um girado <see cref="DadosDaArma.giroNaRajada"/>
/// graus a mais que o anterior (com um anel, sai uma espiral). Quem atira chama
/// <see cref="Comecar"/> e depois <see cref="Atualizar"/> todo quadro, com a origem e o rumo de agora.
/// </summary>
public class Rajada
{
    private DadosDaArma arma;
    private int feitos;
    private float proximo;
    private float giroInicial;

    /// <summary>Ainda tem disparo pra sair.</summary>
    public bool Atirando => arma != null && feitos < arma.rajada;

    /// <summary>Comeca uma rajada (o primeiro disparo sai no proximo <see cref="Atualizar"/>).</summary>
    public void Comecar(DadosDaArma arma)
    {
        this.arma = arma;
        feitos = 0;
        proximo = 0f;
        // Cada rajada comeca num giro diferente: o anel nao cai sempre no mesmo lugar.
        giroInicial = arma != null && arma.anel && arma.tirosPorDisparo > 0 ? Random.Range(0f, 360f / arma.tirosPorDisparo) : 0f;
    }

    public void Parar() => arma = null;

    /// <summary>Solta o disparo da vez, se ja for hora. Devolve se saiu (pra tocar o som).</summary>
    public bool Atualizar(Vector2 origem, Vector2 rumo, GameObject dono, Lado lado)
    {
        if (!Atirando || Time.time < proximo)
            return false;

        arma.Disparar(origem, rumo, dono, lado, giroInicial + arma.giroNaRajada * feitos);
        feitos++;
        proximo = Time.time + arma.intervaloDaRajada;
        return true;
    }
}
