using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Confere a arte dos bonecos sem dar Play (Window ▸ General ▸ Test Runner, aba EditMode):
/// - toda folha de animacao dos prefabs e dos herois corta em pelo menos um quadro;
/// - nenhuma imagem do jogo e maior que o tamanho maximo da importacao. A Unity encolhe a imagem e o
///   corte em quadros sai errado: as tres folhas de ataque do Rei Esqueleto (ate 6288 px de largura, com
///   limite de 4096) davam 0 quadros ate a 1.22.3, e o chefe atacava sem animacao.
/// </summary>
public class FolhasDeAnimacao
{
    // Quem corta folha com um tamanho de quadro: o tipo e o campo do quadro. Toda imagem guardada nesses
    // componentes e cortada com esse quadro.
    private static readonly (string tipo, string quadro)[] QuemCorta =
    {
        ("AnimacaoDoInimigo", "tamanhoDoQuadro"),
        ("AnimacaoDoJogador", "tamanhoDoQuadro"),
        ("MorteDoInimigo", "tamanhoDoQuadroDoEfeito"),
    };

    // Quantos quadros a folha da, com a mesma conta do FolhaDeSprites.Cortar (colunas vezes linhas inteiras).
    // A conta e feita aqui porque o Cortar guarda o corte: depois de reimportar a imagem, ele devolveria o velho.
    private static int Quadros(Texture2D folha, Vector2Int quadro) =>
        quadro.x <= 0 || quadro.y <= 0 ? 0 : (folha.width / quadro.x) * (folha.height / quadro.y);

    [Test]
    public void TodaFolhaDosPrefabsTemQuadros()
    {
        List<string> problemas = new List<string>();
        int conferidas = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);

            if (prefab == null)
                continue;

            foreach (MonoBehaviour peca in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (peca == null)
                    continue;

                foreach ((string tipo, string campoDoQuadro) in QuemCorta)
                {
                    if (peca.GetType().Name != tipo)
                        continue;

                    SerializedObject dados = new SerializedObject(peca);
                    Vector2Int quadro = dados.FindProperty(campoDoQuadro).vector2IntValue;
                    SerializedProperty campo = dados.GetIterator();

                    while (campo.Next(true))
                    {
                        if (campo.propertyType != SerializedPropertyType.ObjectReference || !(campo.objectReferenceValue is Texture2D folha))
                            continue;

                        conferidas++;

                        if (Quadros(folha, quadro) == 0)
                            problemas.Add($"{caminho} ({tipo}.{campo.propertyPath}): {folha.name} {folha.width}x{folha.height} com quadro {quadro.x}x{quadro.y} da 0 quadros");
                    }
                }
            }
        }

        Assert.Greater(conferidas, 0, "Nenhuma folha achada nos prefabs: o teste nao conferiu nada.");
        Assert.IsEmpty(problemas, string.Join("\n", problemas));
    }

    [Test]
    public void TodaFolhaDosHeroisTemQuadros()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Jogador.prefab");
        Assert.IsNotNull(prefab, "Sem o prefab do Jogador.");

        // Os herois trocam so as folhas e os pixels por unidade: o quadro e o do prefab do Jogador.
        Vector2Int quadro = new SerializedObject(prefab.GetComponent<AnimacaoDoJogador>()).FindProperty("tamanhoDoQuadro").vector2IntValue;
        List<string> problemas = new List<string>();

        foreach (Herois.Heroi heroi in Herois.Todos)
        {
            // As mesmas folhas que o Herois.Aplicar usa.
            List<string> nomes = new List<string> { "Idle", "Walk", heroi.FolhaDoAtaque, "Death" };

            if (heroi.DeCima)
            {
                foreach (string jeito in new[] { "Idle", "Walk", "Dash", "Hurt" })
                {
                    nomes.Add(jeito + "_Baixo");
                    nomes.Add(jeito + "_Cima");
                    nomes.Add(jeito + "_Esquerda");
                }

                nomes.Add("Dash");
                nomes.Add("Hurt");
            }

            Texture2D parado = Resources.Load<Texture2D>(heroi.Pasta + "/Idle");

            if (parado == null)
                problemas.Add($"{heroi.Nome}: sem a folha {heroi.Pasta}/Idle");

            foreach (string nome in nomes)
            {
                foreach (string pasta in new[] { heroi.Pasta + "/", heroi.Pasta + "/SemArma/" })
                {
                    Texture2D folha = Resources.Load<Texture2D>(pasta + nome);

                    if (folha != null && Quadros(folha, quadro) == 0)
                        problemas.Add($"{heroi.Nome}: {pasta}{nome} {folha.width}x{folha.height} com quadro {quadro.x}x{quadro.y} da 0 quadros");
                }
            }
        }

        Assert.IsEmpty(problemas, string.Join("\n", problemas));
    }

    [Test]
    public void NenhumaImagemPassaDoTamanhoMaximo()
    {
        List<string> problemas = new List<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Arte" }))
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);

            if (!(AssetImporter.GetAtPath(caminho) is TextureImporter importador))
                continue;

            importador.GetSourceTextureWidthAndHeight(out int largura, out int altura);

            // O limite que vale no Windows: o proprio, se a plataforma tiver um, senao o padrao.
            TextureImporterPlatformSettings windows = importador.GetPlatformTextureSettings("Standalone");
            int limite = windows != null && windows.overridden ? windows.maxTextureSize : importador.maxTextureSize;

            if (Mathf.Max(largura, altura) > limite)
                problemas.Add($"{caminho}: {largura}x{altura}, mas o tamanho maximo da importacao e {limite} (a Unity encolhe)");
        }

        Assert.IsEmpty(problemas, string.Join("\n", problemas));
    }
}
