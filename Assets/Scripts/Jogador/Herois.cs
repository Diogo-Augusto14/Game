using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Os herois que da pra escolher no menu inicial (<see cref="TelaDeInicio"/>): nome, descricao, os
/// numeros e as folhas de animacao (o retrato no menu e na tela do fim). Por enquanto so o Arqueiro;
/// os outros entram na etapa 8, cada um com a habilidade e a arma do comeco dele.
///
/// A escolha fica salva entre partidas.
/// </summary>
public static class Herois
{
    private const string ChaveDoEscolhido = "heroi-escolhido";

    public class Heroi
    {
        public string Nome;
        public string Descricao;

        /// <summary>A linha dos numeros no menu ("Vida 3 corações   Arma ...").</summary>
        public string Numeros;

        /// <summary>A pasta das folhas dentro de Resources (Idle.png, Death.png...).</summary>
        public string Pasta;
    }

    public static readonly Heroi[] Todos =
    {
        new Heroi
        {
            Nome = "Arqueiro",
            Descricao = "Atira flechas sem parar com o arco que nunca acaba.",
            Numeros = "Vida: 3 corações     Arma: Arco do Arqueiro (infinito)     Esquiva rolando",
            Pasta = "Personagens/Herois/Arqueiro",
        },
    };

    private static int escolhido = -1;
    private static readonly Dictionary<string, Sprite[]> folhas = new Dictionary<string, Sprite[]>();

    public static int Escolhido
    {
        get
        {
            if (escolhido < 0)
                escolhido = Mathf.Clamp(PlayerPrefs.GetInt(ChaveDoEscolhido, 0), 0, Todos.Length - 1);

            return escolhido;
        }
    }

    public static Heroi Atual => Todos[Escolhido];

    /// <summary>Escolhe o heroi pelo numero (da a volta nas pontas).</summary>
    public static void Escolher(int indice)
    {
        escolhido = ((indice % Todos.Length) + Todos.Length) % Todos.Length;
        PlayerPrefs.SetInt(ChaveDoEscolhido, escolhido);
        PlayerPrefs.Save();
    }

    /// <summary>Todos liberados, por enquanto (no jogo antigo alguns precisavam ser conquistados).</summary>
    public static bool Liberado(Heroi heroi) => heroi != null;

    /// <summary>Os quadros do heroi parado (o retrato).</summary>
    public static Sprite[] Parado(Heroi heroi) => Folha(heroi, "Idle");

    /// <summary>Os quadros do heroi caindo (a tela do fim, quando morre).</summary>
    public static Sprite[] Morte(Heroi heroi) => Folha(heroi, "Death");

    private static Sprite[] Folha(Heroi heroi, string nome)
    {
        if (heroi == null)
            return new Sprite[0];

        string caminho = heroi.Pasta + "/" + nome;

        if (!folhas.TryGetValue(caminho, out Sprite[] quadros) || quadros.Length == 0 || quadros[0] == null)
        {
            Texture2D textura = Resources.Load<Texture2D>(caminho);
            quadros = FolhaDeSprites.Cortar(textura, new Vector2Int(100, 100), 20f);
            folhas[caminho] = quadros;
        }

        return quadros;
    }
}
