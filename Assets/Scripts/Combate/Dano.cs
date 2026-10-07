using UnityEngine;

/// <summary>De que lado alguem esta. Tiro do mesmo lado atravessa sem machucar.</summary>
public enum Lado
{
    Jogador,
    Inimigos,
}

/// <summary>
/// Um golpe: quanto tira, pra onde empurra e quem mandou. Quem machuca monta um e entrega pra
/// <see cref="Vida.ReceberDano"/>.
/// </summary>
public struct Dano
{
    public float quantidade;

    /// <summary>Pra onde o golpe ia (o empurrao vai pra la).</summary>
    public Vector2 direcao;

    /// <summary>Velocidade do empurrao, em unidades por segundo (0 = nao empurra).</summary>
    public float empurrao;

    /// <summary>Quem machucou (o atirador, nao a bala). Pode ser nulo.</summary>
    public GameObject fonte;

    public Dano(float quantidade, Vector2 direcao, float empurrao, GameObject fonte)
    {
        this.quantidade = quantidade;
        this.direcao = direcao;
        this.empurrao = empurrao;
        this.fonte = fonte;
    }
}

/// <summary>
/// Algo no mesmo objeto que, as vezes, deixa a <see cref="Vida"/> sem tomar dano: a esquiva do
/// jogador. A <see cref="Vida"/> pergunta pra ele antes de cada golpe.
/// </summary>
public interface IInvulneravel
{
    bool Invulneravel { get; }
}

/// <summary>
/// Um escudo: segura alguns golpes inteiros (o cavaleiro do escudo, de frente). Fica no mesmo
/// objeto da <see cref="Vida"/>, que pergunta antes de cada golpe.
/// </summary>
public interface IBloqueioDeDano
{
    bool Bloqueia(Dano dano);
}
