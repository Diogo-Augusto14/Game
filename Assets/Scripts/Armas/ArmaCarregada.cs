/// <summary>
/// Uma arma de verdade, com a municao dela: os numeros vem do <see cref="DadosDaArma"/> (o asset,
/// igual pra todas as copias), e o pente e a reserva sao desta arma. Quando cai no chao, vai junto
/// com a municao que tinha.
/// </summary>
public class ArmaCarregada
{
    public readonly DadosDaArma Dados;

    /// <summary>Tiros no pente agora.</summary>
    public int NoPente { get; private set; }

    /// <summary>Tiros guardados fora do pente.</summary>
    public int Reserva { get; private set; }

    public ArmaCarregada(DadosDaArma dados)
    {
        Dados = dados;
        // Sem pente, tudo fica "no pente": atira ate acabar, sem recarregar.
        NoPente = dados.semPente ? dados.municaoMaxima : dados.pente;
        Reserva = dados.semPente ? 0 : UnityEngine.Mathf.Max(0, dados.municaoMaxima - dados.pente);
    }

    public bool Infinita => Dados.infinita;

    public bool TemNoPente => Infinita || NoPente > 0;

    /// <summary>Da pra recarregar: o pente nao esta cheio e tem reserva.</summary>
    public bool PodeRecarregar => !Infinita && !SemPente && NoPente < Dados.pente && Reserva > 0;

    /// <summary>Atira direto da municao toda, sem recarga (o contador mostra so o total).</summary>
    public bool SemPente => Dados.semPente;

    /// <summary>Sem nada no pente e nada pra recarregar.</summary>
    public bool Vazia => !Infinita && NoPente == 0 && Reserva == 0;

    /// <summary>Quanto falta pra encher (pente + reserva no maximo).</summary>
    public int Falta => Infinita ? 0 : UnityEngine.Mathf.Max(0, Dados.municaoMaxima - NoPente - Reserva);

    public void Gastar()
    {
        if (!Infinita && NoPente > 0)
            NoPente--;
    }

    /// <summary>Passa da reserva pro pente o que couber.</summary>
    public void Recarregar()
    {
        if (SemPente)
            return;

        int passa = UnityEngine.Mathf.Max(0, UnityEngine.Mathf.Min(Dados.pente - NoPente, Reserva));
        NoPente += passa;
        Reserva -= passa;
    }

    /// <summary>Ganha municao (caixa), ate o maximo. Devolve quanto entrou.</summary>
    /// <summary>Poe a municao num valor (o "Continuar" devolve a que a arma tinha).</summary>
    public void Definir(int noPente, int reserva)
    {
        if (Infinita)
            return;

        NoPente = UnityEngine.Mathf.Clamp(noPente, 0, SemPente ? Dados.municaoMaxima : Dados.pente);
        Reserva = UnityEngine.Mathf.Max(0, reserva);
    }

    public int Ganhar(int quanto)
    {
        int entra = UnityEngine.Mathf.Min(quanto, Falta);
        if (SemPente)
            NoPente += entra;
        else
            Reserva += entra;
        return entra;
    }
}
