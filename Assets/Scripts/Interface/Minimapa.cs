using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O mapa das salas do andar, como no Enter the Gungeon: no canto de cima a direita (embaixo do andar), um
/// pedaco do mapa em volta do jogador; com <c>Tab</c> (ou o Select/Back do controle), o mapa inteiro no
/// meio da tela, com o jogo parado. Fecha com <c>Tab</c>, <c>Esc</c> ou o B/Select/Start.
///
/// So aparece o que o jogador ja conhece: as salas em que entrou e, apagadas, as vizinhas (as que um
/// corredor liga a uma sala visitada), com os corredores entre elas. As salas de luta que ainda tem
/// inimigos ficam vermelhas; as especiais tem o icone delas (loja, tesouro, altar, desafio, a sala do fim
/// e a secreta, que so aparece depois que o jogador acha a parede de ilusao). O portal aberto e o jogador piscam.
///
/// Fica no mesmo objeto do <see cref="GeradorDoAndar"/> (ele poe sozinho) e le a planta dele; na arena
/// do chefe e na area segura nao tem mapa. Desenhado no OnGUI, em pixels inteiros (<see cref="Desenho"/>).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(GeradorDoAndar))]
public class Minimapa : MonoBehaviour
{
    private static readonly Color Visitada = new Color(0.78f, 0.74f, 0.68f, 0.95f);
    private static readonly Color ComInimigos = new Color(0.82f, 0.3f, 0.3f, 0.95f);
    private static readonly Color Vizinha = new Color(0.42f, 0.42f, 0.5f, 0.85f);
    private static readonly Color Beirada = new Color(0.06f, 0.05f, 0.09f, 0.9f);
    private static readonly Color Fundo = new Color(0.04f, 0.03f, 0.07f, 0.6f);

    private static int fechouNoQuadro = -10;

    private GeradorDoAndar gerador;
    private PlantaDeSalas.Planta planta;
    private readonly Dictionary<Vector2Int, SalaDaPlanta> salaDaCelula = new Dictionary<Vector2Int, SalaDaPlanta>();
    private readonly HashSet<SalaDaPlanta> visitadas = new HashSet<SalaDaPlanta>();
    private readonly HashSet<SalaDaPlanta> vistas = new HashSet<SalaDaPlanta>();
    private Texture2D textura;
    private RectInt limites;
    private int assinatura = -1;
    private Vida vidaDoJogador;

    /// <summary>O mapa grande esta aberto (o jogo fica parado e o HUD some).</summary>
    public static bool Aberto { get; private set; }

    /// <summary>Aberto, ou fechou neste quadro: o Esc que fechou o mapa nao abre a pausa.</summary>
    public static bool Ocupado => Aberto || Time.frameCount - fechouNoQuadro <= 1;

    private void Awake()
    {
        gerador = GetComponent<GeradorDoAndar>();
    }

    private void OnDisable()
    {
        if (Aberto)
            Fechar();
    }

    private void Update()
    {
        if (gerador.Planta != planta)
            Recomecar(gerador.Planta);

        if (planta == null)
        {
            if (Aberto)
                Fechar();

            return;
        }

        Transform jogador = gerador.Jogador;

        if (jogador != null && vidaDoJogador == null)
            vidaDoJogador = jogador.GetComponent<Vida>();

        // A sala onde o jogador esta fica conhecida, e as vizinhas dela aparecem apagadas.
        if (jogador != null && salaDaCelula.TryGetValue(MapaDeCaminhos.Celula(jogador.position), out SalaDaPlanta aqui) && visitadas.Add(aqui))
            Conhecer(aqui);

        if (planta.secreta != null && gerador.SegredoAberto && visitadas.Contains(planta.donaDoSegredo))
            vistas.Add(planta.secreta);

        if (Aberto)
        {
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape) || Controle.Apertou(BotaoDoControle.Select)
                || Controle.Apertou(BotaoDoControle.B) || Controle.Apertou(BotaoDoControle.Start))
                Fechar();

            return;
        }

        bool podeAbrir = !TelaDoJogo.MenuAberto && !gerador.Trocando && Time.timeScale > 0f && gerador.Escuro <= 0f
                         && (vidaDoJogador == null || !vidaDoJogador.Morto);

        if (podeAbrir && (Input.GetKeyDown(KeyCode.Tab) || Controle.Apertou(BotaoDoControle.Select)))
        {
            Aberto = true;
            Time.timeScale = 0f;
            Sons.Tocar(Som.MenuAbrir, 0.6f);
        }
    }

    private void Fechar()
    {
        Aberto = false;
        fechouNoQuadro = Time.frameCount;
        Time.timeScale = 1f;
        Sons.Tocar(Som.MenuFechar, 0.6f);
    }

    // Andar novo (ou sem andar de salas): esquece tudo.
    private void Recomecar(PlantaDeSalas.Planta nova)
    {
        planta = nova;
        salaDaCelula.Clear();
        visitadas.Clear();
        vistas.Clear();
        assinatura = -1;

        if (planta == null)
            return;

        foreach (SalaDaPlanta sala in planta.salas)
            foreach (Vector2Int c in sala.celulas)
                salaDaCelula[c] = sala;

        RectInt l = Caverna.Limites(planta.ChaoAberto);
        limites = new RectInt(l.xMin - 1, l.yMin - 1, l.width + 2, l.height + 2);

        if (textura == null || textura.width != limites.width || textura.height != limites.height)
        {
            if (textura != null)
                Destroy(textura);

            textura = new Texture2D(limites.width, limites.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
        }
    }

    private void Conhecer(SalaDaPlanta sala)
    {
        vistas.Add(sala);

        foreach (LigacaoDaPlanta ligacao in planta.ligacoes)
        {
            if (ligacao.a == sala)
                vistas.Add(ligacao.b);
            else if (ligacao.b == sala)
                vistas.Add(ligacao.a);
        }
    }

    // ---------------------------------------------------------------- a imagem do mapa

    private bool Limpa(SalaDaPlanta sala)
    {
        if (!sala.DeLuta)
            return true;

        foreach (SalaDeLuta luta in SalaDeLuta.Todas)
        {
            if (luta != null && luta.Planta == sala)
                return luta.Limpa;
        }

        return true;
    }

    // Refaz a imagem so quando muda o que aparece (sala nova, sala limpa, passagem aberta).
    private void Atualizar()
    {
        int limpas = 0;

        foreach (SalaDaPlanta sala in vistas)
        {
            if (Limpa(sala))
                limpas++;
        }

        int agora = visitadas.Count * 7919 + vistas.Count * 131 + limpas * 3 + (gerador.SegredoAberto ? 1 : 0);

        if (agora == assinatura)
            return;

        assinatura = agora;
        int w = limites.width, h = limites.height;
        Color[] pixels = new Color[w * h];

        void Pintar(Vector2Int c, Color cor)
        {
            int x = c.x - limites.xMin, y = c.y - limites.yMin;

            if (x >= 0 && y >= 0 && x < w && y < h)
                pixels[y * w + x] = cor;
        }

        foreach (LigacaoDaPlanta ligacao in planta.ligacoes)
        {
            if (!visitadas.Contains(ligacao.a) && !visitadas.Contains(ligacao.b))
                continue;

            Color cor = visitadas.Contains(ligacao.a) && visitadas.Contains(ligacao.b) ? Visitada : Vizinha;

            foreach (Vector2Int c in ligacao.celulas)
                Pintar(c, cor);
        }

        if (gerador.SegredoAberto)
        {
            foreach (Vector2Int c in planta.passagemSecreta)
                Pintar(c, Visitada);
        }

        foreach (SalaDaPlanta sala in vistas)
        {
            Color cor = !visitadas.Contains(sala) ? Vizinha : Limpa(sala) ? Visitada : ComInimigos;

            foreach (Vector2Int c in sala.celulas)
                Pintar(c, cor);
        }

        // A beirada escura em volta do que aparece (le em cima de qualquer chao).
        Color[] comBeirada = (Color[])pixels.Clone();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (pixels[y * w + x].a > 0f)
                    continue;

                bool encosta = false;

                for (int dx = -1; dx <= 1 && !encosta; dx++)
                    for (int dy = -1; dy <= 1 && !encosta; dy++)
                    {
                        int vx = x + dx, vy = y + dy;
                        encosta = vx >= 0 && vy >= 0 && vx < w && vy < h && pixels[vy * w + vx].a > 0f;
                    }

                if (encosta)
                    comBeirada[y * w + x] = Beirada;
            }
        }

        textura.SetPixels(comBeirada);
        textura.Apply();
    }

    // ---------------------------------------------------------------- na tela

    private void OnGUI()
    {
        if (planta == null || textura == null)
            return;

        GUI.depth = -20;

        if (Aberto)
        {
            Atualizar();
            Grande();
            return;
        }

        if (TelaDoJogo.MenuAberto || gerador.Escuro > 0f || gerador.Jogador == null)
            return;

        Atualizar();
        Pequeno();
    }

    // No canto, embaixo do andar: um pedaco do mapa em volta do jogador.
    private void Pequeno()
    {
        int u = Desenho.U;
        float m = 8 * u;
        Rect caixa = new Rect(Screen.width - m - 116 * u, m + 62 * u, 116 * u, 84 * u);
        Desenho.Cor(caixa, Fundo);
        Contorno(caixa, u, new Color(0.6f, 0.58f, 0.66f, 0.6f));

        int escala = 2 * u;
        Vector2 jogador = (Vector2)gerador.Jogador.position - limites.position + new Vector2(0.5f, 0.5f);

        // A origem do mapa (o canto de cima a esquerda da imagem), com o jogador no meio da caixa.
        Vector2 origem = new Vector2(caixa.width * 0.5f - jogador.x * escala, caixa.height * 0.5f - (limites.height - jogador.y) * escala);
        origem = new Vector2(Mathf.Round(origem.x), Mathf.Round(origem.y));

        GUI.BeginGroup(caixa);
        Desenhar(origem, escala, 10 * u);
        GUI.EndGroup();
    }

    // O mapa inteiro no meio da tela, com o jogo parado.
    private void Grande()
    {
        int u = Desenho.U;
        Desenho.Cor(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.02f, 0.01f, 0.04f, 0.82f));

        GUIStyle titulo = Desenho.Estilo(FonteDoJogo.Titulo, 34 * u, TextAnchor.UpperCenter);
        Desenho.Texto(new Rect(0f, 14 * u, Screen.width, 40 * u), gerador.NomeNaTela ?? "Mapa", titulo, Desenho.Dourado);

        float cabeW = Screen.width * 0.86f, cabeH = Screen.height - 120 * u;
        int escala = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(cabeW / limites.width, cabeH / limites.height)), 1, 8 * u);
        Vector2 tamanho = new Vector2(limites.width, limites.height) * escala;
        Vector2 origem = new Vector2(Mathf.Round((Screen.width - tamanho.x) * 0.5f), Mathf.Round(58 * u + (cabeH - tamanho.y) * 0.5f));
        Desenhar(origem, escala, Mathf.Max(14 * u, escala * 3));

        // A legenda e como fechar.
        GUIStyle pequeno = Desenho.Estilo(FonteDoJogo.Texto, 16 * u, TextAnchor.MiddleLeft);
        (TipoDeSala tipo, string nome)[] legenda =
        {
            (TipoDeSala.Loja, "Loja"), (TipoDeSala.Tesouro, "Tesouro"), (TipoDeSala.Altar, "Altar"),
            (TipoDeSala.Desafio, "Desafio"), (TipoDeSala.Fim, "Sala do fim"), (TipoDeSala.Fonte, "Fonte"), (TipoDeSala.Cela, "Cela"), (TipoDeSala.Secreta, "Secreta"),
        };
        float lado = 16 * u, passo = 0f;

        foreach (var item in legenda)
            passo += lado + 6 * u + Desenho.Largura(item.nome, pequeno) + 18 * u;

        float x = (Screen.width - passo) * 0.5f, y = Screen.height - 50 * u;

        foreach (var item in legenda)
        {
            Desenho.Sprite(new Rect(x, y, lado, lado), Icone(item.tipo), Color.white);
            x += lado + 6 * u;
            float largura = Desenho.Largura(item.nome, pequeno);
            Desenho.Texto(new Rect(x, y - 2 * u, largura + 4 * u, lado + 4 * u), item.nome, pequeno, Desenho.Claro);
            x += largura + 18 * u;
        }

        GUIStyle dica = Desenho.Estilo(FonteDoJogo.Texto, 16 * u, TextAnchor.MiddleCenter);
        string fechar = Controle.EmUso ? "Select ou B  fechar" : "Tab ou Esc  fechar";
        Desenho.Texto(new Rect(0f, Screen.height - 28 * u, Screen.width, 22 * u), fechar, dica, Desenho.Apagado);
    }

    // A imagem, os icones das salas, o portal e o jogador, com o canto de cima a esquerda em "origem".
    private void Desenhar(Vector2 origem, int escala, float icone)
    {
        GUI.DrawTexture(new Rect(origem.x, origem.y, limites.width * escala, limites.height * escala), textura);

        foreach (SalaDaPlanta sala in vistas)
        {
            Sprite desenho = Icone(sala.tipo);

            if (desenho != null)
            {
                Vector2 p = NaTela(sala.Meio, origem, escala);
                Desenho.Sprite(new Rect(p.x - icone * 0.5f, p.y - icone * 0.5f, icone, icone), desenho,
                               visitadas.Contains(sala) ? Color.white : new Color(1f, 1f, 1f, 0.6f));
            }
        }

        float pisca = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
        float ponto = Mathf.Max(3f, escala * 1.5f);

        if (gerador.OndeEstaOPortal is Vector2 portal && salaDaCelula.TryGetValue(MapaDeCaminhos.Celula(portal), out SalaDaPlanta doPortal)
            && vistas.Contains(doPortal))
        {
            Vector2 p = NaTela(portal, origem, escala);
            float r = ponto * 1.6f;
            Desenho.Cor(new Rect(p.x - r * 0.5f, p.y - r * 0.5f, r, r), new Color(0.75f, 0.5f, 1f, pisca));
        }

        if (gerador.Jogador != null)
        {
            Vector2 p = NaTela(gerador.Jogador.position, origem, escala);
            Desenho.Cor(new Rect(p.x - ponto * 0.5f - 1f, p.y - ponto * 0.5f - 1f, ponto + 2f, ponto + 2f), Beirada);
            Desenho.Cor(new Rect(p.x - ponto * 0.5f, p.y - ponto * 0.5f, ponto, ponto), new Color(0.45f, 0.95f, 1f, pisca));
        }
    }

    // Um ponto do mundo na tela (o y da tela cresce pra baixo; a imagem tem a linha 0 embaixo).
    private Vector2 NaTela(Vector2 mundo, Vector2 origem, int escala) =>
        new Vector2(origem.x + (mundo.x - limites.xMin + 0.5f) * escala, origem.y + (limites.yMax - mundo.y - 0.5f) * escala);

    private static readonly Dictionary<TipoDeSala, Sprite> icones = new Dictionary<TipoDeSala, Sprite>();

    // Os icones das salas especiais (dos itens do jogo antigo).
    private static Sprite Icone(TipoDeSala tipo)
    {
        if (icones.TryGetValue(tipo, out Sprite pronto))
            return pronto;

        int numero;

        switch (tipo)
        {
            case TipoDeSala.Loja: numero = 160; break;
            case TipoDeSala.Tesouro: numero = 2129; break;
            case TipoDeSala.Altar: numero = 742; break;
            case TipoDeSala.Desafio: numero = 1444; break;
            case TipoDeSala.Fim: numero = 691; break;
            case TipoDeSala.Secreta: numero = 335; break;
            case TipoDeSala.Fonte: numero = 123; break;
            case TipoDeSala.Cela: numero = 179; break;
            default: return null;
        }

        Sprite desenho = ArteDoAntigo.Icone(numero);
        icones[tipo] = desenho;
        return desenho;
    }

    private static void Contorno(Rect r, int u, Color cor)
    {
        Desenho.Cor(new Rect(r.x, r.y, r.width, u), cor);
        Desenho.Cor(new Rect(r.x, r.yMax - u, r.width, u), cor);
        Desenho.Cor(new Rect(r.x, r.y, u, r.height), cor);
        Desenho.Cor(new Rect(r.xMax - u, r.y, u, r.height), cor);
    }
}
