using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pixel art de 16x16 desenhada por codigo: o jogador, cada inimigo, os coletaveis, a
/// pedra, os espinhos e os ladrilhos do chao e da parede. Nenhuma imagem no projeto.
///
/// Cada desenho tem 1 unidade de lado (16 pixels por unidade), igual ao circulo e ao
/// quadrado que ele substitui, entao quem desenha nao precisa mudar tamanho nenhum.
///
/// Os inimigos sao uma bola sombreada na cor do bicho com o rosto por cima. Os rostos,
/// o coracao, a chave e a pedra estao escritos como "desenhos em texto" logo abaixo:
/// cada letra e uma cor da paleta, ponto e transparente (ou "nao mexe", no rosto).
/// Pra mudar a cara de alguem, e so editar as letras.
///
/// Os ladrilhos sao cinza claro de proposito: a cor do andar vem do SpriteRenderer.
/// </summary>
public static class ArteGerada
{
    private const int Lado = 16;

    private static readonly Dictionary<string, Sprite> guardados = new Dictionary<string, Sprite>();

    // ================================================================ paleta
    private static readonly Dictionary<char, Color32> Paleta = new Dictionary<char, Color32>
    {
        { 'k', new Color32(24, 16, 20, 255) },     // contorno
        { 'w', new Color32(255, 255, 255, 255) },  // branco
        { 'r', new Color32(220, 40, 55, 255) },    // vermelho
        { 'R', new Color32(140, 20, 35, 255) },    // vermelho escuro
        { 'y', new Color32(255, 214, 60, 255) },   // amarelo
        { 'Y', new Color32(190, 140, 20, 255) },   // amarelo escuro
        { 'h', new Color32(235, 225, 200, 255) },  // osso (chifre)
        { 'g', new Color32(150, 146, 140, 255) },  // pedra
        { 'l', new Color32(196, 192, 184, 255) },  // pedra clara
        { 'd', new Color32(98, 94, 90, 255) },     // pedra escura
        { 'c', new Color32(120, 230, 255, 255) },  // ciano (lagrima, olho da sentinela)
        { 'o', new Color32(255, 140, 40, 255) },   // laranja (pavio aceso)
        { 'b', new Color32(55, 55, 70, 255) },     // bomba
        { 'B', new Color32(95, 95, 115, 255) },    // bomba clara
    };

    // ================================================================ desenhos em texto
    // Primeira linha = topo da imagem.

    private static readonly string[] RostoDoJogador =
    {
        "................",
        "................",
        "................",
        "................",
        "................",
        "...kkk....kkk...",
        "..kkwkk..kkwkk..",
        "..kkkkk..kkkkk..",
        "...kkk....kkk...",
        "...c.......c....",
        "...c.......c....",
        "......kkkk......",
        ".......kk.......",
        "................",
        "................",
        "................",
    };

    private static readonly string[] RostoDoPerseguidor =
    {
        "................",
        "................",
        "................",
        "...kk......kk...",
        "....kkk..kkk....",
        "....kwk..kwk....",
        "....kkk..kkk....",
        "................",
        "................",
        "....kkkkkkkk....",
        "....kwkwkwkk....",
        "....kkkkkkkk....",
        "................",
        "................",
        "................",
        "................",
    };

    private static readonly string[] RostoDoAtirador =
    {
        "................",
        "................",
        "......kkkk......",
        ".....kwwwwk.....",
        ".....kwkkwk.....",
        ".....kwkkwk.....",
        ".....kwwwwk.....",
        "......kkkk......",
        "................",
        "......kkkk......",
        ".....kRRRRk.....",
        ".....kRRRRk.....",
        "......kkkk......",
        "................",
        "................",
        "................",
    };

    private static readonly string[] RostoDoInvestidor =
    {
        "..h..........h..",
        "..hh........hh..",
        "...hh......hh...",
        "....h......h....",
        "................",
        "....kk....kk....",
        "....kr....rk....",
        "....kk....kk....",
        "................",
        "................",
        ".....kkkkkk.....",
        "....k......k....",
        "................",
        "................",
        "................",
        "................",
    };

    private static readonly string[] RostoDoSaltador =
    {
        "................",
        "...kkk....kkk...",
        "..kwwwk..kwwwk..",
        "..kwkwk..kwkwk..",
        "...kkk....kkk...",
        "................",
        "................",
        "................",
        "................",
        "...k........k...",
        "....kkkkkkkk....",
        "................",
        "................",
        "................",
        "................",
        "................",
    };

    private static readonly string[] RostoDaSentinela =
    {
        "................",
        "................",
        "................",
        "................",
        "..kkkkk..kkkkk..",
        "..kcccc..cccck..",
        "..kkkkk..kkkkk..",
        "................",
        "................",
        "....kkkkkkkk....",
        "....kdkdkdkk....",
        "....kkkkkkkk....",
        "................",
        "................",
        "................",
        "................",
    };

    private static readonly string[] RostoDoDivisor =
    {
        "................",
        "................",
        "................",
        "....ww..........",
        "....w...........",
        "................",
        ".....k....k.....",
        ".....k....k.....",
        "................",
        "......kkkk......",
        "................",
        "................",
        "................",
        "................",
        "................",
        "................",
    };

    private static readonly string[] Coracao =
    {
        "................",
        "................",
        "...kkk....kkk...",
        "..krrrk..krrrk..",
        ".krwwrrkkrrrrrk.",
        ".krwrrrrrrrrrrk.",
        ".krrrrrrrrrrrrk.",
        ".krrrrrrrrrrrRk.",
        "..krrrrrrrrrRk..",
        "...krrrrrrrRk...",
        "....krrrrrRk....",
        ".....krrrRk.....",
        "......krRk......",
        ".......kk.......",
        "................",
        "................",
    };

    private static readonly string[] Chave =
    {
        "................",
        "......kkkk......",
        ".....kyyyyk.....",
        ".....kykkyk.....",
        ".....kyyyYk.....",
        "......kyYk......",
        "......kyYk......",
        "......kyYk......",
        "......kyYkk.....",
        "......kyyyYk....",
        "......kyYkk.....",
        "......kyyYk.....",
        "......kyYkk.....",
        ".......kk.......",
        "................",
        "................",
    };

    private static readonly string[] Moeda =
    {
        "................",
        "................",
        "......kkkk......",
        "....kkyyyykk....",
        "...kyywyyyyYk...",
        "...kywyyyyyYk...",
        "..kyyyykkyyyYk..",
        "..kyyyykkyyyYk..",
        "..kyyyykkyyyYk..",
        "..kyyyykkyyyYk..",
        "...kyyyyyyyYk...",
        "...kyyyyyyYYk...",
        "....kkYYYYkk....",
        "......kkkk......",
        "................",
        "................",
    };

    private static readonly string[] Bomba =
    {
        "..........o.....",
        ".........oyo....",
        "..........o.....",
        ".........k......",
        "........kk......",
        ".....kkkkkk.....",
        "...kkbbbbbbkk...",
        "..kbBBbbbbbbbk..",
        "..kbBbbbbbbbbk..",
        ".kbbbbbbbbbbbbk.",
        ".kbbbbbbbbbbbbk.",
        ".kbbbbbbbbbbbbk.",
        "..kbbbbbbbbbbk..",
        "..kkbbbbbbbbkk..",
        "....kkkkkkkk....",
        "................",
    };

    private static readonly string[] Pedra =
    {
        "................",
        "................",
        "....kkkkkkk.....",
        "...kllllgggk....",
        "..kllggggggdk...",
        "..klgggggggddk..",
        ".klggggggggggdk.",
        ".klggglgggggddk.",
        ".kgggggggggggddk",
        ".kgggggggggddddk",
        ".kgggggdggggdddk",
        "..kggggggggdddk.",
        "..kddggggddddk..",
        "...kkddddddkk...",
        ".....kkkkkk.....",
        "................",
    };

    private static readonly string[] Espinhos =
    {
        "................",
        "...k.......k....",
        "..klk.....klk...",
        "..klk.....klk...",
        ".kglgk...kglgk..",
        ".kggdk...kggdk..",
        ".kkkkk...kkkkk..",
        "................",
        "................",
        "......k.......k.",
        ".....klk.....klk",
        ".....klk.....klk",
        "....kglgk...kglg",
        "....kggdk...kggd",
        "....kkkkk...kkkk",
        "................",
    };

    // ================================================================ api
    /// <summary>O rosto do jogador sobre uma bola na cor da pele.</summary>
    public static Sprite Jogador(Color pele) => Guardado("jogador", () => BolaComRosto(pele, RostoDoJogador));

    /// <summary>O desenho de um inimigo comum, ou null pros que nao tem (chefes usam a bola sombreada).</summary>
    public static Sprite Inimigo(TipoDeInimigo tipo, Color cor)
    {
        switch (tipo)
        {
            case TipoDeInimigo.Perseguidor: return Guardado("perseguidor", () => BolaComRosto(cor, RostoDoPerseguidor));
            case TipoDeInimigo.Atirador: return Guardado("atirador", () => BolaComRosto(cor, RostoDoAtirador));
            case TipoDeInimigo.Investidor: return Guardado("investidor", () => BolaComRosto(cor, RostoDoInvestidor, 0.9f, 7.5f, 6.8f));
            case TipoDeInimigo.Saltador: return Guardado("saltador", () => BolaComRosto(cor, RostoDoSaltador, 1f, 7.5f, 7f));
            case TipoDeInimigo.Sentinela: return Guardado("sentinela", () => BlocoComRosto(cor, RostoDaSentinela));
            case TipoDeInimigo.Divisor:
            case TipoDeInimigo.DivisorPequeno: return Guardado("divisor " + tipo, () => BolaComRosto(cor, RostoDoDivisor));
            default: return null;
        }
    }

    /// <summary>Bola branca com luz em cima e sombra embaixo. Pinte pelo SpriteRenderer.</summary>
    public static Sprite Bola() => Guardado("bola", () => BolaComRosto(Color.white, null));

    /// <summary>
    /// Coracao, moeda, chave e bomba. Com a arte dos pacotes: coracao, moeda, chave dourada e bomba
    /// do Raven Fantasy Icons. Sem ela, o desenho daqui.
    /// </summary>
    public static Sprite Coletavel(TipoDeColetavel tipo)
    {
        switch (tipo)
        {
            case TipoDeColetavel.Coracao: return ArteImportada.IconeDoPacote(659) ?? Guardado("coracao", () => Texto(Coracao));
            case TipoDeColetavel.Moeda: return ArteImportada.IconeDoPacote(131) ?? Guardado("moeda", () => Texto(Moeda));
            case TipoDeColetavel.Chave: return ArteImportada.IconeDoPacote(179) ?? Guardado("chave", () => Texto(Chave));
            default: return ArteImportada.IconeDoPacote(764) ?? Guardado("bomba", () => Texto(Bomba));
        }
    }

    /// <summary>Pedra de sala: uma das pedras do Tiny Swords (sorteada), ou a desenhada aqui.</summary>
    public static Sprite PedraSolta() => ArteImportada.PedraAleatoria() ?? Guardado("pedra", () => Texto(Pedra));

    /// <summary>Espinhos: os da masmorra, todos pra fora, ou os desenhados aqui.</summary>
    public static Sprite EspinhosNoChao()
    {
        Sprite[] pacote = ArteImportada.EspinhosDoChao;
        return pacote != null ? pacote[pacote.Length - 1] : Guardado("espinhos", () => Texto(Espinhos));
    }

    /// <summary>True quando o chao e a parede vem do pacote da masmorra (a cor do andar so tinge de leve).</summary>
    public static bool CenarioDoPacote => ArteImportada.ChaoDaMasmorra != null;

    /// <summary>Ladrilho do chao: o do tema do andar, o da masmorra ou (sem arte) pedra lisa com pintinhas e uma junta escura. Para modo Tiled.</summary>
    public static Sprite Chao() => ArteImportada.ChaoDoTema ?? ArteImportada.ChaoDaMasmorra ?? ChaoGerado();

    private static Sprite ChaoGerado() => Guardado("chao", () =>
    {
        Color32[] px = new Color32[Lado * Lado];
        Sintetizador.Ruido ruido = new Sintetizador.Ruido(12345);

        for (int y = 0; y < Lado; y++)
        {
            for (int x = 0; x < Lado; x++)
            {
                float tom = 0.86f + ruido.Proximo() * 0.05f;

                if (x == Lado - 1 || y == 0)
                    tom = 0.66f;
                else if (x == 0 || y == Lado - 1)
                    tom = 0.94f;

                px[y * Lado + x] = Cinza(tom);
            }
        }

        return px;
    });

    /// <summary>Ladrilho da parede: tijolos com rejunte. Para modo Tiled.</summary>
    public static Sprite Tijolo() => ArteImportada.ParedeDoTema ?? ArteImportada.ParedeDaMasmorra ?? TijoloGerado();

    private static Sprite TijoloGerado() => Guardado("tijolo", () =>
    {
        Color32[] px = new Color32[Lado * Lado];
        Sintetizador.Ruido ruido = new Sintetizador.Ruido(777);

        for (int y = 0; y < Lado; y++)
        {
            // Duas fileiras de tijolo de 8 de altura, a de cima deslocada meio tijolo.
            int fileira = y / 8;
            int deslocamento = fileira == 0 ? 0 : 4;

            for (int x = 0; x < Lado; x++)
            {
                bool junta = y % 8 == 0 || (x + deslocamento) % 8 == 0;
                float tom = junta ? 0.55f : 0.88f + ruido.Proximo() * 0.06f;

                if (!junta && y % 8 == 7)
                    tom += 0.06f; // borda de cima do tijolo pega luz

                px[y * Lado + x] = Cinza(tom);
            }
        }

        return px;
    });

    // ================================================================ montagem
    private static Sprite Guardado(string chave, System.Func<Color32[]> desenhar)
    {
        if (guardados.TryGetValue(chave, out Sprite s) && s != null)
            return s;

        Texture2D tex = new Texture2D(Lado, Lado, TextureFormat.RGBA32, false)
        {
            name = "Arte " + chave,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        tex.SetPixels32(desenhar());
        tex.Apply();

        s = Sprite.Create(tex, new Rect(0f, 0f, Lado, Lado), new Vector2(0.5f, 0.5f), Lado, 0, SpriteMeshType.FullRect);
        s.name = "Arte " + chave;
        guardados[chave] = s;
        return s;
    }

    /// <summary>Converte um desenho em texto (topo primeiro) em pixels.</summary>
    private static Color32[] Texto(string[] linhas)
    {
        Color32[] px = new Color32[Lado * Lado];
        Sobrepor(px, linhas);
        return px;
    }

    private static void Sobrepor(Color32[] px, string[] linhas)
    {
        if (linhas == null)
            return;

        for (int linha = 0; linha < Lado && linha < linhas.Length; linha++)
        {
            int y = Lado - 1 - linha; // textura cresce pra cima; o texto, pra baixo
            string texto = linhas[linha];

            for (int x = 0; x < Lado && x < texto.Length; x++)
            {
                if (Paleta.TryGetValue(texto[x], out Color32 cor))
                    px[y * Lado + x] = cor;
            }
        }
    }

    /// <summary>
    /// Bola sombreada (luz vindo de cima a esquerda) com contorno escuro, e o rosto por
    /// cima. <paramref name="escalaY"/> achata a bola; <paramref name="centroY"/> desce ela.
    /// </summary>
    private static Color32[] BolaComRosto(Color cor, string[] rosto, float escalaY = 1f, float centroX = 7.5f, float centroY = 7.5f)
    {
        Color32[] px = new Color32[Lado * Lado];
        float raio = 7.2f;

        for (int y = 0; y < Lado; y++)
        {
            for (int x = 0; x < Lado; x++)
            {
                float dx = x - centroX;
                float dy = (y - centroY) / escalaY;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                if (d > raio)
                    continue;

                if (d > raio - 1.1f)
                {
                    px[y * Lado + x] = Paleta['k'];
                    continue;
                }

                // Luz: mais claro pra cima e pra esquerda, mais escuro embaixo.
                float luz = Mathf.Clamp((-dx * 0.5f + dy) / raio, -1f, 1f);
                Color c = luz > 0f ? Color.Lerp(cor, Color.white, luz * 0.35f) : Color.Lerp(cor, Color.black, -luz * 0.35f);

                // Brilhinho no alto.
                if (Mathf.Abs(dx + 3f) < 1.1f && Mathf.Abs(dy - 3.5f) < 0.8f)
                    c = Color.Lerp(c, Color.white, 0.6f);

                c.a = 1f;
                px[y * Lado + x] = c;
            }
        }

        Sobrepor(px, rosto);
        return px;
    }

    /// <summary>Bloco de pedra chanfrado na cor dada, com o rosto por cima (a sentinela).</summary>
    private static Color32[] BlocoComRosto(Color cor, string[] rosto)
    {
        Color32[] px = new Color32[Lado * Lado];

        for (int y = 0; y < Lado; y++)
        {
            for (int x = 0; x < Lado; x++)
            {
                bool borda = x == 0 || y == 0 || x == Lado - 1 || y == Lado - 1;
                Color c;

                if (borda)
                    c = Paleta['k'];
                else if (x == 1 || y == Lado - 2)
                    c = Color.Lerp(cor, Color.white, 0.3f);
                else if (x == Lado - 2 || y == 1)
                    c = Color.Lerp(cor, Color.black, 0.35f);
                else
                    c = cor;

                c.a = 1f;
                px[y * Lado + x] = c;
            }
        }

        Sobrepor(px, rosto);
        return px;
    }

    private static Color32 Cinza(float tom)
    {
        byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(tom * 255f), 0, 255);
        return new Color32(b, b, b, 255);
    }
}
