using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Confere a arte dos bonecos sem dar Play (Window ▸ General ▸ Test Runner, aba EditMode):
/// - toda folha de animacao dos prefabs e dos herois corta em pelo menos um quadro;
/// - nenhuma imagem do jogo e maior que o tamanho maximo da importacao. A Unity encolhe a imagem e o
///   corte em quadros sai errado: as tres folhas de ataque do Rei Esqueleto (ate 6288 px de largura, com
///   limite de 4096) davam 0 quadros ate a 1.22.3, e o chefe atacava sem animacao;
/// - nenhum desenho atravessa a emenda entre dois quadros e, parado ou andando, nenhum bicho pula de lado.
///   E o que acontece quando o quadro do corte e menor que o desenhado: ate a 1.22.3, o importador cortou
///   as folhas do Assassino com a metade do quadro do pacote, e cada quadro do jogo mostrava meio bicho,
///   uma hora numa borda e outra hora na outra.
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

    // Alfa a partir do qual o pixel conta como desenho.
    private const byte Opaco = 16;

    // As mesmas folhas que o Herois.Aplicar usa.
    private static List<string> FolhasDoHeroi(Herois.Heroi heroi)
    {
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

        return nomes;
    }

    // Toda folha dos bichos (prefabs) e dos herois, com o quadro em que e cortada e se roda em loop parada
    // no lugar (parado e andando).
    private static IEnumerable<(string nome, Texture2D folha, Vector2Int quadro, bool noLugar)> FolhasDosBonecos()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);

            if (prefab == null)
                continue;

            foreach (AnimacaoDoInimigo animacao in prefab.GetComponentsInChildren<AnimacaoDoInimigo>(true))
            {
                SerializedObject dados = new SerializedObject(animacao);
                Vector2Int quadro = dados.FindProperty("tamanhoDoQuadro").vector2IntValue;
                SerializedProperty campo = dados.GetIterator();

                while (campo.Next(true))
                {
                    if (campo.propertyType == SerializedPropertyType.ObjectReference && campo.objectReferenceValue is Texture2D folha)
                        yield return ($"{caminho} ({campo.propertyPath})", folha, quadro,
                                      campo.propertyPath.StartsWith("parado") || campo.propertyPath.StartsWith("andando"));
                }
            }
        }

        GameObject jogador = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Jogador.prefab");
        Vector2Int quadroDoJogador = new SerializedObject(jogador.GetComponent<AnimacaoDoJogador>()).FindProperty("tamanhoDoQuadro").vector2IntValue;

        foreach (Herois.Heroi heroi in Herois.Todos)
        {
            foreach (string nome in FolhasDoHeroi(heroi))
            {
                foreach (string pasta in new[] { heroi.Pasta + "/", heroi.Pasta + "/SemArma/" })
                {
                    Texture2D folha = Resources.Load<Texture2D>(pasta + nome);

                    if (folha != null)
                        yield return ($"{heroi.Nome}: {pasta}{nome}", folha, quadroDoJogador, nome.StartsWith("Idle") || nome.StartsWith("Walk"));
                }
            }
        }
    }

    // Le a imagem do arquivo (a importacao nao deixa ler os pixels) e mede cada quadro, na ordem do
    // FolhaDeSprites.Cortar: o meio do desenho na horizontal (NaN no quadro vazio) e quantas emendas entre
    // quadros vizinhos o desenho atravessa.
    private static float[] MeiosDosQuadros(Texture2D folha, Vector2Int quadro, out int emendasAtravessadas)
    {
        Texture2D imagem = new Texture2D(2, 2);
        imagem.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(folha)));
        int largura = imagem.width, altura = imagem.height;
        Color32[] pixels = imagem.GetPixels32();
        Object.DestroyImmediate(imagem);

        int colunas = largura / quadro.x, linhas = altura / quadro.y;
        float[] meios = new float[colunas * linhas];
        emendasAtravessadas = 0;

        for (int linha = 0; linha < linhas; linha++)
        {
            // Na textura a linha 0 e embaixo; a primeira linha de quadros e a de cima.
            int y0 = altura - (linha + 1) * quadro.y;

            for (int coluna = 0; coluna < colunas; coluna++)
            {
                int x0 = coluna * quadro.x, menor = int.MaxValue, maior = -1;

                for (int y = y0; y < y0 + quadro.y; y++)
                {
                    for (int x = x0; x < x0 + quadro.x; x++)
                    {
                        if (pixels[y * largura + x].a >= Opaco)
                        {
                            menor = Mathf.Min(menor, x - x0);
                            maior = Mathf.Max(maior, x - x0);
                        }
                    }
                }

                meios[linha * colunas + coluna] = maior < 0 ? float.NaN : (menor + maior) / 2f;

                if (coluna + 1 < colunas && AtravessaAEmenda(pixels, largura, x0 + quadro.x, y0, quadro.y))
                    emendasAtravessadas++;
            }
        }

        return meios;
    }

    // Em alguma linha o desenho chega na emenda pelos dois lados? Olha 3 px de cada lado, porque o importador
    // deixa uma margem vazia em volta de cada quadro.
    private static bool AtravessaAEmenda(Color32[] pixels, int largura, int emenda, int y0, int altura)
    {
        for (int y = y0; y < y0 + altura; y++)
        {
            bool antes = false, depois = false;

            for (int m = 0; m < 3; m++)
            {
                antes |= pixels[y * largura + emenda - 1 - m].a >= Opaco;
                depois |= pixels[y * largura + emenda + m].a >= Opaco;
            }

            if (antes && depois)
                return true;
        }

        return false;
    }

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
            Texture2D parado = Resources.Load<Texture2D>(heroi.Pasta + "/Idle");

            if (parado == null)
                problemas.Add($"{heroi.Nome}: sem a folha {heroi.Pasta}/Idle");

            foreach (string nome in FolhasDoHeroi(heroi))
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

    [Test]
    public void NenhumDesenhoAtravessaAEmendaEntreQuadros()
    {
        List<string> problemas = new List<string>();
        int conferidas = 0;

        foreach (var (nome, folha, quadro, _) in FolhasDosBonecos())
        {
            // A folha que nao da quadro nenhum ja reclama nos testes de cima.
            if (Quadros(folha, quadro) == 0)
                continue;

            conferidas++;
            MeiosDosQuadros(folha, quadro, out int atravessadas);

            if (atravessadas > 0)
                problemas.Add($"{nome}: {folha.name} {folha.width}x{folha.height} cortada em {quadro.x}x{quadro.y}: o desenho atravessa {atravessadas} emendas (o quadro desenhado e maior que o do corte?)");
        }

        Assert.Greater(conferidas, 0, "Nenhuma folha achada: o teste nao conferiu nada.");
        Assert.IsEmpty(problemas, string.Join("\n", problemas));
    }

    [Test]
    public void ParadoOuAndandoNenhumBichoPulaDeLado()
    {
        // Nas folhas certas o meio do desenho anda no maximo uns 5% do quadro de um quadro pro outro; o
        // Assassino cortado errado pulava 80%.
        const float PuloMaximo = 0.25f;
        List<string> problemas = new List<string>();

        foreach (var (nome, folha, quadro, noLugar) in FolhasDosBonecos())
        {
            if (!noLugar || Quadros(folha, quadro) == 0)
                continue;

            float[] meios = MeiosDosQuadros(folha, quadro, out _);

            // Em loop, do ultimo quadro volta pro primeiro. Quadro vazio da NaN e nao conta.
            for (int i = 0; i < meios.Length; i++)
            {
                float pulo = Mathf.Abs(meios[(i + 1) % meios.Length] - meios[i]);

                if (pulo > PuloMaximo * quadro.x)
                {
                    problemas.Add($"{nome}: do quadro {i} pro {(i + 1) % meios.Length} o desenho pula {pulo:0} px de lado (quadro de {quadro.x})");
                    break;
                }
            }
        }

        Assert.IsEmpty(problemas, string.Join("\n", problemas));
    }
}
