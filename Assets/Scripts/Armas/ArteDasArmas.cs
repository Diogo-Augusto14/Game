using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pixel art das armas, da bala, da caixa de municao e do icone do vazio, desenhada aqui em
/// texto (uma letra por pixel) e transformada em sprite na primeira vez que alguem pede. Nao
/// precisa de imagem no projeto. Cada desenho ganha um contorno escuro de 1 pixel sozinho.
///
/// Letras: K contorno, D metal escuro, G metal, g metal claro, W branco, B madeira escura,
/// b madeira clara, Y amarelo, R vermelho, O laranja, C azul-claro, L verde, P roxo. Ponto = vazio.
/// </summary>
public static class ArteDasArmas
{
    /// <summary>Pixels por unidade das armas: uma arma de 14 pixels fica com ~0.8 unidade (o jogador tem ~0.8).</summary>
    public const float PixelsPorUnidade = 18f;

    private static readonly Dictionary<string, Sprite> guardados = new Dictionary<string, Sprite>();

    // Com "Enter Play Mode" sem recarregar o dominio, o dicionario sobreviveria com sprites mortos.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar() => guardados.Clear();

    private static readonly Color32 Contorno = new Color32(24, 22, 30, 255);

    private static Color32 Cor(char letra)
    {
        switch (letra)
        {
            case 'D': return new Color32(70, 74, 88, 255);
            case 'G': return new Color32(120, 126, 142, 255);
            case 'g': return new Color32(176, 182, 198, 255);
            case 'W': return new Color32(240, 240, 245, 255);
            case 'B': return new Color32(110, 66, 38, 255);
            case 'b': return new Color32(158, 100, 56, 255);
            case 'Y': return new Color32(240, 200, 60, 255);
            case 'R': return new Color32(210, 60, 50, 255);
            case 'O': return new Color32(236, 128, 40, 255);
            case 'C': return new Color32(90, 200, 240, 255);
            case 'L': return new Color32(130, 220, 70, 255);
            case 'P': return new Color32(170, 90, 220, 255);
            default: return new Color32(0, 0, 0, 0);
        }
    }

    // ---------------- desenhos ----------------
    // Todas olham pra direita, com a empunhadura na esquerda. A primeira linha e o topo.
    private static readonly string[] Pistola =
    {
        "....GGGGGG..",
        "..DDDDDDDDD.",
        "..DDDDDDD...",
        ".BBDDD......",
        ".BB.........",
    };

    private static readonly string[] Revolver =
    {
        "....GGGGGGG.",
        "..DDDDDDDDDD",
        "..DDDYDDDD..",
        ".BBDDD......",
        ".BB.........",
        ".B..........",
    };

    private static readonly string[] Escopeta =
    {
        ".......GGGGGGGGGG",
        "BBBBBDDDDDDDDDDDD",
        "bBBBDDDDYYYDDDDD.",
        ".BBB.DDDDDDDDDDD.",
        "..B..............",
    };

    private static readonly string[] Submetralhadora =
    {
        "......GGGGGGG",
        "..DDDDDDDDDDD",
        "..DDDDDDDDDD.",
        ".BBDD.DDD....",
        ".BB...DD.....",
        ".B....DD.....",
        "......DD.....",
    };

    private static readonly string[] Fuzil =
    {
        "..........GGGGGGGG",
        "BBBBDDDDDDDDDDDDDD",
        "bBBBDDDDDGDDDDDDD.",
        ".BBB.DDDDDD.......",
        "........DD........",
        "........DD........",
    };

    private static readonly string[] Rifle =
    {
        "........CCCC.........",
        "......DDDDDDGGGGGGGGG",
        "BBBBBBDDDDDDDDDDDDDDD",
        "bBBBBB.DDDDDDDD......",
        ".BBBB................",
    };

    private static readonly string[] LancaGranadas =
    {
        "...OOOOOOOOOO..",
        "..DDDDDDDDDDDDD",
        "..DDDDDOODDDDDD",
        ".BBDDDDDDDDDDDD",
        ".BB.DD..........",
        ".B..............",
    };

    private static readonly string[] PistolaDeGelo =
    {
        "....CCCCCCC.",
        "..GGGGGGGGGG",
        "..GGgWGGGG..",
        ".BBGGG......",
        ".BB.........",
        ".B..........",
    };

    private static readonly string[] PistolaToxica =
    {
        "....LLLLLLL.",
        "..DDDDDDDDDD",
        "..DDLLDDDD..",
        ".BBDDD......",
        ".BB.........",
        ".B..........",
    };

    private static readonly string[] Quicadora =
    {
        "....YYYYYYY.",
        "..DDDDDDDDDD",
        "..DDYYDDDD..",
        ".BBDDD......",
        ".BB.........",
        ".B..........",
    };

    private static readonly string[] CaixaDeMunicao =
    {
        "..O...O...O.",
        ".YYY.YYY.YYY",
        ".YYY.YYY.YYY",
        "DDDDDDDDDDDD",
        "DGGGGGGGGGGD",
        "DGGLLLLLLGGD",
        "DGGGGGGGGGGD",
        "DDDDDDDDDDDD",
    };

    private static readonly string[] IconeDoVazio =
    {
        "....CCCC....",
        "..CCWWWWCC..",
        ".CWW....WWC.",
        ".CW......WC.",
        "CW........WC",
        "CW........WC",
        "CW........WC",
        "CW........WC",
        ".CW......WC.",
        ".CWW....WWC.",
        "..CCWWWWCC..",
        "....CCCC....",
    };

    // ---------------- pedidos ----------------
    /// <summary>A arma de verdade na mao do jogador. Pivo na empunhadura, olhando pra direita.</summary>
    public static Sprite Arma(EstiloDeArma estilo)
    {
        string[] linhas = LinhasDe(estilo);
        return Guardar("arma " + estilo, () => Montar(linhas, new Vector2(2f, linhas.Length * 0.5f), PixelsPorUnidade));
    }

    /// <summary>A mesma arma com o pivo no meio, pro icone na HUD, no pedestal e na loja.</summary>
    public static Sprite Icone(EstiloDeArma estilo)
    {
        string[] linhas = LinhasDe(estilo);
        float lado = Mathf.Max(linhas[0].Length, linhas.Length);
        return Guardar("icone " + estilo, () => Montar(linhas, new Vector2(linhas[0].Length * 0.5f, linhas.Length * 0.5f), lado));
    }

    /// <summary>Comprimento da arma em unidades locais do jogador (a boca do cano fica nesta distancia).</summary>
    public static float Comprimento(EstiloDeArma estilo) => (LinhasDe(estilo)[0].Length - 2f) / PixelsPorUnidade;

    public static Sprite Municao() => Guardar("municao", () => Montar(CaixaDeMunicao, new Vector2(6f, 4f), 12f));

    public static Sprite VazioDoJogador() => Guardar("vazio", () => Montar(IconeDoVazio, new Vector2(6f, 6f), 12f));

    /// <summary>A bala: bolinha clara com contorno. Pinte pelo SpriteRenderer; 1 unidade de diametro.</summary>
    public static Sprite Bala()
    {
        return Guardar("bala", () =>
        {
            const int lado = 10;
            Texture2D textura = NovaTextura("Bala", lado);
            Color32[] pixels = new Color32[lado * lado];
            float centro = (lado - 1) * 0.5f;

            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float d = Mathf.Sqrt((x - centro) * (x - centro) + (y - centro) * (y - centro));

                    if (d <= 3.1f)
                        pixels[y * lado + x] = d <= 1.7f ? new Color32(255, 255, 255, 255) : new Color32(235, 235, 235, 255);
                    else if (d <= 4.4f)
                        pixels[y * lado + x] = Contorno;
                }
            }

            textura.SetPixels32(pixels);
            textura.Apply();
            return Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f), lado);
        });
    }

    /// <summary>Anel fino e claro, pro clarao do vazio. 1 unidade de diametro.</summary>
    public static Sprite Anel()
    {
        return Guardar("anel", () =>
        {
            const int lado = 128;
            Texture2D textura = NovaTextura("Anel", lado);
            textura.filterMode = FilterMode.Bilinear;
            Color32[] pixels = new Color32[lado * lado];
            float centro = (lado - 1) * 0.5f;
            float raio = lado * 0.5f - 1f;

            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float d = Mathf.Sqrt((x - centro) * (x - centro) + (y - centro) * (y - centro));
                    float borda = Mathf.Clamp01(1f - Mathf.Abs(d - (raio - 4f)) / 4f);
                    float miolo = d < raio ? 0.12f * (1f - d / raio) : 0f;
                    byte alfa = (byte)(Mathf.Clamp01(Mathf.Max(borda, miolo)) * 255f);
                    pixels[y * lado + x] = new Color32(255, 255, 255, alfa);
                }
            }

            textura.SetPixels32(pixels);
            textura.Apply();
            return Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f), lado);
        });
    }

    /// <summary>Mira do mouse: quatro tracos com o centro vazio, branca com contorno. Textura de 32x32 pro cursor.</summary>
    public static Texture2D TexturaDaMira()
    {
        const int lado = 32;
        Texture2D textura = NovaTextura("MiraDoJogo", lado);
        textura.filterMode = FilterMode.Point;
        bool[] cheio = new bool[lado * lado];

        void Marcar(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < lado && y < lado)
                cheio[y * lado + x] = true;
        }

        for (int d = 5; d <= 11; d++)
        {
            for (int e = 0; e < 2; e++)
            {
                Marcar(15 + e, 16 + d);   // cima
                Marcar(15 + e, 15 - d);   // baixo
                Marcar(16 + d, 15 + e);   // direita
                Marcar(15 - d, 15 + e);   // esquerda
            }
        }

        // Ponto no centro.
        Marcar(15, 15);
        Marcar(16, 16);

        Color32[] pixels = new Color32[lado * lado];

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                int i = y * lado + x;

                if (cheio[i])
                    pixels[i] = new Color32(255, 255, 255, 255);
                else if (VizinhoCheio(cheio, lado, lado, x, y))
                    pixels[i] = Contorno;
            }
        }

        textura.SetPixels32(pixels);
        textura.Apply();
        return textura;
    }

    // ---------------- montagem ----------------
    private static string[] LinhasDe(EstiloDeArma estilo)
    {
        switch (estilo)
        {
            case EstiloDeArma.Pistola: return Pistola;
            case EstiloDeArma.Escopeta: return Escopeta;
            case EstiloDeArma.Submetralhadora: return Submetralhadora;
            case EstiloDeArma.Fuzil: return Fuzil;
            case EstiloDeArma.Rifle: return Rifle;
            case EstiloDeArma.LancaGranadas: return LancaGranadas;
            case EstiloDeArma.PistolaDeGelo: return PistolaDeGelo;
            case EstiloDeArma.PistolaToxica: return PistolaToxica;
            case EstiloDeArma.Quicadora: return Quicadora;
            default: return Revolver;
        }
    }

    private static Sprite Guardar(string chave, System.Func<Sprite> fazer)
    {
        if (guardados.TryGetValue(chave, out Sprite pronto) && pronto != null)
            return pronto;

        Sprite novo = fazer();
        guardados[chave] = novo;
        return novo;
    }

    private static Texture2D NovaTextura(string nome, int lado)
    {
        return new Texture2D(lado, lado, TextureFormat.RGBA32, false)
        {
            name = nome,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
    }

    // Converte as linhas de texto num sprite com 1 pixel de folga em volta, pro contorno.
    private static Sprite Montar(string[] linhas, Vector2 pivoEmPixels, float pixelsPorUnidade)
    {
        int largura = linhas[0].Length;
        int altura = linhas.Length;
        int l = largura + 2;
        int a = altura + 2;

        Texture2D textura = new Texture2D(l, a, TextureFormat.RGBA32, false)
        {
            name = "Arte das armas",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        bool[] cheio = new bool[l * a];
        Color32[] pixels = new Color32[l * a];

        for (int y = 0; y < altura; y++)
        {
            for (int x = 0; x < largura; x++)
            {
                char letra = linhas[y][x];

                if (letra == '.' || letra == ' ')
                    continue;

                // A linha 0 do texto e o topo; na textura a linha 0 e embaixo.
                int i = (altura - y) * l + (x + 1);
                cheio[i] = true;
                pixels[i] = Cor(letra);
            }
        }

        for (int y = 0; y < a; y++)
        {
            for (int x = 0; x < l; x++)
            {
                int i = y * l + x;

                if (!cheio[i] && VizinhoCheio(cheio, l, a, x, y))
                    pixels[i] = Contorno;
            }
        }

        textura.SetPixels32(pixels);
        textura.Apply();

        // O pivo vem em pixels do desenho (linha 0 = topo); na textura entra a folga e o eixo y inverte.
        Vector2 pivo = new Vector2((pivoEmPixels.x + 1f) / l, (altura - pivoEmPixels.y + 1f) / a);
        return Sprite.Create(textura, new Rect(0f, 0f, l, a), pivo, pixelsPorUnidade);
    }

    private static bool VizinhoCheio(bool[] cheio, int largura, int altura, int x, int y)
    {
        return (x > 0 && cheio[y * largura + x - 1])
            || (x < largura - 1 && cheio[y * largura + x + 1])
            || (y > 0 && cheio[(y - 1) * largura + x])
            || (y < altura - 1 && cheio[(y + 1) * largura + x]);
    }
}
