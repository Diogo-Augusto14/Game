/// <summary>
/// O que a <see cref="BarraDoChefe"/> precisa saber de um chefe. Cada chefe tem o seu
/// jeito de lutar; a barra so quer o nome, a vida e se ja entrou na segunda fase.
/// </summary>
public interface IChefe
{
    string Nome { get; }

    bool NaSegundaFase { get; }

    Vida Vida { get; }
}
