using UnityEngine;

/// <summary>
/// O ataque extra de padrao de bala de um chefe: cada chefe tem o seu (<see cref="PerfilDeBalas.ExtraDoChefe"/>),
/// de um desenho que os outros nao tem. O chefe ja tinha os ataques dele, com aviso e duas fases; este
/// e mais um na roda, e na segunda fase fica mais denso.
///
/// O chefe so precisa de cinco coisas: criar isto no Awake (<see cref="Para"/>), ter um valor
/// <c>Extra</c> na lista de ataques, avisar usando <see cref="Aviso"/> e <see cref="Cor"/>, chamar
/// <see cref="Soltar"/> quando o aviso acaba e esperar <see cref="Ocupado"/> virar falso antes de recuperar.
/// Quem solta as balas e o <see cref="AtiradorDePadroes"/> do chefe.
/// </summary>
public class ExtraDoChefe
{
    private readonly AtiradorDePadroes atirador;
    private readonly TipoDeInimigo chefe;

    private ExtraDoChefe(AtiradorDePadroes atirador, TipoDeInimigo chefe)
    {
        this.atirador = atirador;
        this.chefe = chefe;
    }

    /// <summary>Liga o ataque extra do chefe <paramref name="chefe"/> ao inimigo <paramref name="dono"/> (chame no Awake dele).</summary>
    public static ExtraDoChefe Para(InimigoDeSala dono, TipoDeInimigo chefe) => new ExtraDoChefe(AtiradorDePadroes.Em(dono), chefe);

    /// <summary>Segundos do aviso antes do ataque.</summary>
    public float Aviso => PerfilDeBalas.AvisoDoExtra;

    /// <summary>A cor que os olhos (ou o corpo) do chefe vao tomando durante o aviso.</summary>
    public Color Cor => PerfilDeBalas.CorDoExtra(chefe);

    /// <summary>Ainda esta soltando a fila de padroes.</summary>
    public bool Ocupado => atirador != null && atirador.Ocupado;

    /// <summary>Comeca a fila de padroes. <paramref name="fase"/>: 1 = a primeira da luta, 2 = abaixo da metade da vida, 3 = a do fim do Olho.</summary>
    public void Soltar(int fase)
    {
        if (atirador != null)
            atirador.Sequencia(PerfilDeBalas.ExtraDoChefe(chefe, atirador.Dificuldade, fase));
    }
}
