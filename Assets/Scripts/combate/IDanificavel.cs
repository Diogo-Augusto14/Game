/// <summary>
/// Qualquer coisa que possa tomar dano (inimigo, chefe, jogador, vaso, parede quebrável).
/// A espada só conhece esta interface — nunca a classe concreta.
/// </summary>
public interface IDanificavel
{
    void TomarDano(DanoInfo info);
}
