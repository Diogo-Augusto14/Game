using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Catálogo de todos os clipes do jogo, num único asset. O Construtor de Animações
/// (Tools ▸ Jogo ▸ Reconstruir animações) lê as folhas de sprites das pastas
/// <c>Assets/player</c> e <c>Assets/Bandits</c> e escreve este asset em
/// <c>Assets/Resources</c> — de onde qualquer script consegue carregar sem precisar
/// arrastar nada no Inspector. É isso que permite o Bootstrap montar o jogo inteiro
/// só apertando Play.
/// </summary>
[CreateAssetMenu(fileName = "BibliotecaDeAnimacoes", menuName = "Jogo/Biblioteca de Animações")]
public class BibliotecaDeAnimacoes : ScriptableObject
{
    /// <summary>Nome do arquivo dentro de Resources (sem extensão).</summary>
    public const string NomeDoAsset = "BibliotecaDeAnimacoes";

    [Tooltip("Gerado pela ferramenta. Dá pra ajustar fps e loop na mão depois")]
    [SerializeField] private List<ClipeDeSprites> clipes = new List<ClipeDeSprites>();

    private Dictionary<string, ClipeDeSprites> indice;

    public IReadOnlyList<ClipeDeSprites> Clipes => clipes;

    public int Quantidade => clipes.Count;

    // ---------------------------------------------------------------- carga
    private static BibliotecaDeAnimacoes padrao;

    /// <summary>
    /// A biblioteca de Resources. Carrega uma vez e guarda. Devolve null (com aviso no
    /// Console) se a ferramenta ainda não tiver rodado.
    /// </summary>
    public static BibliotecaDeAnimacoes Padrao
    {
        get
        {
            if (padrao == null)
            {
                padrao = Resources.Load<BibliotecaDeAnimacoes>(NomeDoAsset);

                if (padrao == null)
                {
                    Debug.LogError(
                        $"[BibliotecaDeAnimacoes] não achei Assets/Resources/{NomeDoAsset}.asset. " +
                        "Rode Tools ▸ Jogo ▸ Reconstruir animações.");
                }
            }

            return padrao;
        }
    }

    // ---------------------------------------------------------------- busca
    /// <summary>O clipe com esse nome, ou null se não existir.</summary>
    public ClipeDeSprites Buscar(string nome)
    {
        if (string.IsNullOrEmpty(nome))
            return null;

        GarantirIndice();
        return indice.TryGetValue(nome, out ClipeDeSprites clipe) ? clipe : null;
    }

    public bool Tem(string nome) => Buscar(nome) != null;

    /// <summary>
    /// O primeiro nome da lista que existe de verdade na biblioteca. Serve pra pedir
    /// "toca o pulo duplo, ou o pulo normal se eu não tiver pulo duplo" numa linha só.
    /// </summary>
    public string Primeiro(params string[] nomes)
    {
        for (int i = 0; i < nomes.Length; i++)
        {
            if (Tem(nomes[i]))
                return nomes[i];
        }

        return null;
    }

    private void GarantirIndice()
    {
        if (indice != null && indice.Count == clipes.Count)
            return;

        indice = new Dictionary<string, ClipeDeSprites>(clipes.Count);

        for (int i = 0; i < clipes.Count; i++)
        {
            ClipeDeSprites clipe = clipes[i];

            if (clipe == null || string.IsNullOrEmpty(clipe.nome))
                continue;

            indice[clipe.nome] = clipe;
        }
    }

    // ---------------------------------------------------------------- escrita (ferramenta)
    /// <summary>Troca a lista inteira. Só a ferramenta de editor chama isto.</summary>
    public void Substituir(List<ClipeDeSprites> novos)
    {
        clipes = novos ?? new List<ClipeDeSprites>();
        indice = null;
    }

    private void OnDisable()
    {
        for (int i = 0; i < clipes.Count; i++)
            clipes[i]?.LimparCache();
    }
}
