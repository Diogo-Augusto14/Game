using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A interface que veio de pacote, lida de <c>Assets/Arte/Resources</c>:
///
///   InterfaceDragao/...  (Tiny RPG - Dragon Regalia GUI, CC0)
///       molduras douradas, faixas de titulo, botao, setas, ponteiro, barra e o cursor
///   Teclas/Teclado.png  (Controllers and Keyboard, da Vryell)
///       teclas de 16x16 em grade: coluna 0 solta, coluna 2 apertada
///   Teclas/ControleXbox.png, ControlePlayStation.png, Analogicos.png  (o mesmo pacote)
///       botoes de 16x16: coluna 0 solto, coluna 1 apertado
///
/// Mesmo esquema da <see cref="ArteImportada"/>: recorta com Sprite.Create e, se a imagem
/// sumir, devolve null e quem pediu volta pro desenho antigo.
/// </summary>
public static class ArteDaInterface
{
    private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    // ================================================================ molduras e faixas
    // Borda = quanto nao estica (esq, baixo, dir, cima), em pixels da imagem.

    /// <summary>Moldura dourada grande, cantos com enfeite e miolo azul. Desenhada 3x.</summary>
    public static Sprite MolduraGrande => Inteira("MolduraGrande", new Vector4(34, 48, 34, 47), 100f / 3f);

    /// <summary>A mesma moldura, menor (cantos menores). Desenhada 2x.</summary>
    public static Sprite MolduraPequena => Inteira("MolduraPequena", new Vector4(24, 25, 24, 24), 50f);

    /// <summary>
    /// Faixa rosa de pano, pra titulo. Desenhada 6x (<see cref="AlturaDaFaixa"/>): so as
    /// pontas nao esticam. O pano fica um pouco acima do meio (<see cref="MeioDoPanoDaFaixa"/>).
    /// </summary>
    public static Sprite FaixaRosa => Inteira("FaixaRosa", new Vector4(21, 0, 21, 0), 100f / 6f);

    /// <summary>Placa azul com pontas de losango, pra titulo. Desenhada 6x.</summary>
    public static Sprite FaixaAzul => Inteira("FaixaAzul", new Vector4(18, 0, 18, 0), 100f / 6f);

    /// <summary>Altura das faixas na tela: 32 px desenhados 6x.</summary>
    public const float AlturaDaFaixa = 192f;

    /// <summary>Quanto o meio do pano fica acima do meio da imagem da faixa, na tela.</summary>
    public const float MeioDoPanoDaFaixa = 20f;

    /// <summary>Moldura dourada da barra (a barra entra 7/5/8/4 px). Desenhada 4x.</summary>
    public static Sprite MolduraDaBarra => Inteira("BarraDourada", new Vector4(7, 5, 8, 4), 25f);

    /// <summary>Quanto a <see cref="MolduraDaBarra"/> cobre em volta da barra (esq, baixo, dir, cima).</summary>
    public static readonly Vector4 BordaDaBarra = new Vector4(7, 5, 8, 4);

    /// <summary>Botao com pontas de losango: 0 cinza, 1 laranja, 2 ferrugem, 3 apagado. 3x.</summary>
    public static Sprite Botao(int estado) => Quadro("BotaoLosango", 104, 35, estado, new Vector4(20, 0, 20, 0), 100f / 3f);

    /// <summary>Setas do menu: 0 solta, 1 acesa (verde), 2 apertada, 3 apagada.</summary>
    public static Sprite SetaEsquerda(int estado) => Quadro("SetaEsquerda", 16, 16, estado, Vector4.zero, 100f);

    public static Sprite SetaDireita(int estado) => Quadro("SetaDireita", 16, 16, estado, Vector4.zero, 100f);

    /// <summary>Os 5 quadros do ponteiro dourado que aponta pra direita.</summary>
    public static Sprite Ponteiro(int quadro) => Quadro("Ponteiro", 26, 26, quadro % 5, Vector4.zero, 100f);

    public const int QuadrosDoPonteiro = 5;

    // ================================================================ teclas
    /// <summary>
    /// O desenho da tecla, ou null se nao tiver. <paramref name="apertada"/> = a mesma
    /// tecla afundada (coluna 2 da folha).
    /// </summary>
    public static Sprite Tecla(KeyCode tecla, bool apertada = false)
    {
        if (!PosicaoDaTecla(tecla, out Vector2Int celula))
            return null;

        if (apertada)
            celula.x += 2;

        string chave = $"Tecla {celula.x},{celula.y}";

        if (sprites.TryGetValue(chave, out Sprite guardado))
            return guardado;

        Texture2D textura = Textura("Teclas/Teclado");
        Sprite sprite = null;

        if (textura != null)
        {
            Rect r = new Rect(celula.x * 16, textura.height - (celula.y + 1) * 16, 16, 16);
            sprite = Sprite.Create(textura, r, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = chave;
        }

        sprites[chave] = sprite;
        return sprite;
    }

    // ================================================================ botoes do controle
    /// <summary>
    /// O desenho do botao no controle de Xbox ou de PlayStation (Controllers and Keyboard),
    /// ou null. <paramref name="apertado"/> = o mesmo botao afundado (coluna 1 da folha).
    /// </summary>
    public static Sprite DesenhoDoBotao(BotaoDoControle botao, bool playStation, bool apertado = false)
    {
        string folha;
        Vector2Int celula;

        if (botao == BotaoDoControle.AnalogicoEsquerdo || botao == BotaoDoControle.AnalogicoDireito)
        {
            folha = "Teclas/Analogicos";
            celula = new Vector2Int(0, botao == BotaoDoControle.AnalogicoEsquerdo ? 3 : 7);
        }
        else
        {
            folha = playStation ? "Teclas/ControlePlayStation" : "Teclas/ControleXbox";

            if (!PosicaoDoBotao(botao, playStation, out celula))
                return null;
        }

        // A cruz com uma direcao ja vem acesa: nao tem versao apertada.
        if (apertado && (botao < BotaoDoControle.Cruz || botao > BotaoDoControle.CruzDireita))
            celula.x += 1;

        string chave = $"{folha} {celula.x},{celula.y}";

        if (sprites.TryGetValue(chave, out Sprite guardado))
            return guardado;

        Texture2D textura = Textura(folha);
        Sprite sprite = null;

        if (textura != null)
        {
            Rect r = new Rect(celula.x * 16, textura.height - (celula.y + 1) * 16, 16, 16);
            sprite = Sprite.Create(textura, r, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = chave;
        }

        sprites[chave] = sprite;
        return sprite;
    }

    /// <summary>
    /// Coluna e linha do botao na folha do controle (linha 0 em cima). As duas folhas tem
    /// os quatro botoes da frente e a cruz no mesmo lugar; o resto muda.
    /// </summary>
    private static bool PosicaoDoBotao(BotaoDoControle botao, bool playStation, out Vector2Int celula)
    {
        switch (botao)
        {
            case BotaoDoControle.Y: celula = new Vector2Int(0, 0); return true;
            case BotaoDoControle.X: celula = new Vector2Int(0, 2); return true;
            case BotaoDoControle.A: celula = new Vector2Int(0, 4); return true;
            case BotaoDoControle.B: celula = new Vector2Int(0, 6); return true;
            case BotaoDoControle.Cruz: celula = new Vector2Int(0, 8); return true;
            case BotaoDoControle.CruzCima: celula = new Vector2Int(1, 10); return true;
            case BotaoDoControle.CruzEsquerda: celula = new Vector2Int(2, 10); return true;
            case BotaoDoControle.CruzBaixo: celula = new Vector2Int(3, 10); return true;
            case BotaoDoControle.CruzDireita: celula = new Vector2Int(4, 10); return true;
            case BotaoDoControle.Start: celula = new Vector2Int(0, playStation ? 17 : 11); return true;
            case BotaoDoControle.Select: celula = new Vector2Int(0, 13); return true;
            case BotaoDoControle.LB: celula = new Vector2Int(0, playStation ? 24 : 16); return true;
            case BotaoDoControle.LT: celula = new Vector2Int(0, playStation ? 27 : 19); return true;
            case BotaoDoControle.RB: celula = new Vector2Int(0, playStation ? 30 : 22); return true;
            case BotaoDoControle.RT: celula = new Vector2Int(0, playStation ? 33 : 25); return true;
        }

        celula = default;
        return false;
    }

    /// <summary>Coluna e linha da tecla na folha (linha 0 em cima).</summary>
    private static bool PosicaoDaTecla(KeyCode tecla, out Vector2Int celula)
    {
        if (tecla >= KeyCode.A && tecla <= KeyCode.Z)
        {
            celula = new Vector2Int(0, tecla - KeyCode.A);
            return true;
        }

        switch (tecla)
        {
            case KeyCode.Escape: celula = new Vector2Int(8, 0); return true;
            case KeyCode.Tab: celula = new Vector2Int(8, 2); return true;
            case KeyCode.LeftShift:
            case KeyCode.RightShift: celula = new Vector2Int(8, 5); return true;
            case KeyCode.Space: celula = new Vector2Int(8, 8); return true;
            case KeyCode.Backspace: celula = new Vector2Int(8, 23); return true;
            case KeyCode.Return: celula = new Vector2Int(8, 25); return true;
            case KeyCode.UpArrow: celula = new Vector2Int(12, 12); return true;
            case KeyCode.LeftArrow: celula = new Vector2Int(12, 13); return true;
            case KeyCode.DownArrow: celula = new Vector2Int(12, 14); return true;
            case KeyCode.RightArrow: celula = new Vector2Int(12, 15); return true;
        }

        celula = default;
        return false;
    }

    // ================================================================ cursor
    // O cursor dourado do pacote no lugar da setinha do Windows (so aparece em janela).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void TrocarCursor()
    {
        Texture2D cursor = Resources.Load<Texture2D>("InterfaceDragao/Cursor");

        if (cursor != null)
            Cursor.SetCursor(cursor, Vector2.zero, CursorMode.Auto);
    }

    // ================================================================ ajudas
    private static Sprite Inteira(string nome, Vector4 borda, float pixelsPorUnidade)
    {
        Texture2D textura = Textura("InterfaceDragao/" + nome);
        return textura == null ? null : Recortar(nome, textura, new Rect(0, 0, textura.width, textura.height), borda, pixelsPorUnidade);
    }

    /// <summary>Um quadro de uma folha deitada (quadros lado a lado, da esquerda pra direita).</summary>
    private static Sprite Quadro(string nome, int largura, int altura, int indice, Vector4 borda, float pixelsPorUnidade)
    {
        Texture2D textura = Textura("InterfaceDragao/" + nome);

        if (textura == null)
            return null;

        indice = Mathf.Clamp(indice, 0, textura.width / largura - 1);
        return Recortar($"{nome}#{indice}", textura, new Rect(indice * largura, textura.height - altura, largura, altura), borda, pixelsPorUnidade);
    }

    private static Sprite Recortar(string chave, Texture2D textura, Rect recorte, Vector4 borda, float pixelsPorUnidade)
    {
        chave += "@" + pixelsPorUnidade;

        if (sprites.TryGetValue(chave, out Sprite guardado))
            return guardado;

        Sprite sprite = Sprite.Create(textura, recorte, new Vector2(0.5f, 0.5f), pixelsPorUnidade, 0, SpriteMeshType.FullRect, borda);
        sprite.name = chave;
        sprites[chave] = sprite;
        return sprite;
    }

    private static readonly Dictionary<string, Texture2D> texturas = new Dictionary<string, Texture2D>();

    private static Texture2D Textura(string caminho)
    {
        if (texturas.TryGetValue(caminho, out Texture2D guardada))
            return guardada;

        Texture2D textura = Resources.Load<Texture2D>(caminho);

        if (textura != null)
        {
            textura.filterMode = FilterMode.Point;
            textura.wrapMode = TextureWrapMode.Clamp;
        }
        else
        {
            Debug.LogWarning($"[ArteDaInterface] nao achei Resources/{caminho}. Usando o desenho antigo.");
        }

        texturas[caminho] = textura;
        return textura;
    }
}
