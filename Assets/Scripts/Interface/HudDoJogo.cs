using UnityEngine;

/// <summary>
/// O que fica na tela durante o jogo, desenhado com a arte de interface (pixel art, em multiplos
/// inteiros: ver <see cref="Desenho"/>):
/// - em cima a esquerda, a vida em coracoes (cada coracao vale 2; meio coracao vale 1) e, embaixo
///   deles, a habilidade do heroi (F) com a barra da recarga, as moedas, chaves e bombas, o item
///   ativo (R) com a carga e, embaixo a esquerda, os icones dos itens que pegou;
/// - em cima a direita, o andar e quantos inimigos faltam (ou "portal aberto");
/// - embaixo a direita, a arma na mao na moldura, o nome, a municao, a recarga e a outra arma;
/// - no alto, o nome e a barra de vida do chefe, no andar dele;
/// - a dica do que da pra usar perto ("E  abrir o bau"). O nome do andar ao chegar e o
///   <see cref="AvisoDoAndar"/> (a placa azul do jogo antigo).
/// Quem desenha e a <see cref="TelaDoJogo"/>, que guarda as imagens e as fontes.
/// </summary>
public class HudDoJogo
{
    private readonly TelaDoJogo tela;

    public HudDoJogo(TelaDoJogo tela)
    {
        this.tela = tela;
    }

    public void Desenhar(GeradorDoAndar gerador, Vida vida, ArmaDoJogador armas, InteracaoDoJogador interacao, HabilidadeDoHeroi habilidade,
                         Bolsa bolsa, ItemAtivoDoJogador ativo, EstatisticasDoJogador itens)
    {
        int u = Desenho.U;

        if (vida != null && !vida.Morto)
        {
            Coracoes(vida, u);

            if (habilidade != null)
                Habilidade(habilidade, u);

            if (bolsa != null)
                Bolsa(bolsa, ativo, u);

            if (itens != null)
                Itens(itens, u);

            if (armas != null)
                Arma(armas, u);

            if (interacao != null && interacao.Perto != null)
                Dica(interacao.Perto, u);
        }

        if (gerador != null)
        {
            Andar(gerador, u);

            if (gerador.ChefeAtual != null)
                BarraDoChefe(gerador.ChefeAtual, u);

        }
    }

    private void Coracoes(Vida vida, int u)
    {
        Texture2D coracao = tela.coracao;

        if (coracao == null)
            return;

        int total = Mathf.CeilToInt(vida.Maxima / 2f);
        int metades = Mathf.CeilToInt(vida.Atual);
        float lado = coracao.width * u;
        float passo = lado * 0.8f;
        float m = 8 * u;

        for (int i = 0; i < total; i++)
        {
            Rect onde = new Rect(m + i * passo, m, lado, coracao.height * u);
            int cheio = metades - i * 2;

            // Vazio (escuro) por baixo; por cima, o inteiro ou so a metade da esquerda.
            Desenho.Imagem(onde, coracao, new Color(0.18f, 0.16f, 0.24f, 0.85f));

            if (cheio >= 2)
                Desenho.Imagem(onde, coracao, Color.white);
            else if (cheio == 1)
                Desenho.Parte(new Rect(onde.x, onde.y, onde.width * 0.5f, onde.height), coracao,
                              new Rect(0f, 0f, coracao.width * 0.5f, coracao.height), Color.white);
        }
    }

    // Embaixo dos coracoes: "F  Chuva de flechas" e a barra enchendo ate ficar pronta.
    private void Habilidade(HabilidadeDoHeroi habilidade, int u)
    {
        float m = 8 * u;
        float y = m + (tela.coracao != null ? tela.coracao.height : 16) * u + 4 * u;
        GUIStyle estilo = Desenho.Estilo(tela.fonteTexto, 16 * u, TextAnchor.UpperLeft);
        bool pronta = habilidade.Pronta;
        Desenho.Texto(new Rect(m, y, 300 * u, 22 * u), "F  " + habilidade.Nome, estilo, pronta ? Desenho.Dourado : Desenho.Apagado);

        Rect barra = new Rect(m, y + 22 * u, 80 * u, 13 * u);
        Barra(barra, habilidade.Carga, pronta ? Desenho.Dourado : new Color(0.55f, 0.6f, 0.75f));
    }

    // Moedas, chaves e bombas, uma do lado da outra, e o item ativo do lado.
    private void Bolsa(Bolsa bolsa, ItemAtivoDoJogador ativo, int u)
    {
        float m = 8 * u;
        float y = m + (tela.coracao != null ? tela.coracao.height : 16) * u + 44 * u;
        GUIStyle estilo = Desenho.Estilo(tela.fonteTexto, 18 * u, TextAnchor.MiddleLeft);
        float x = m;
        float lado = 16 * u;

        x = Contador(x, y, lado, u, estilo, Coletavel.Desenho(TipoDeColetavel.Moeda), bolsa.Moedas);
        x = Contador(x, y, lado, u, estilo, Coletavel.Desenho(TipoDeColetavel.Chave), bolsa.Chaves);
        x = Contador(x, y, lado, u, estilo, Coletavel.Desenho(TipoDeColetavel.Bomba), bolsa.Bombas);

        // O item ativo: o icone, a barra de carga e o R.
        if (ativo != null && ativo.Item != null)
        {
            Rect icone = new Rect(x + 4 * u, y - 6 * u, 26 * u, 26 * u);
            Desenho.Sprite(icone, ativo.Item.Desenho, ativo.Pronto ? Color.white : new Color(0.55f, 0.55f, 0.6f));
            Barra(new Rect(icone.xMax + 4 * u, y + 2 * u, 50 * u, 11 * u), ativo.Fracao, ativo.Pronto ? Desenho.Dourado : new Color(0.55f, 0.6f, 0.75f));
            GUIStyle tecla = Desenho.Estilo(tela.fonteTexto, 14 * u, TextAnchor.MiddleLeft);
            Desenho.Texto(new Rect(icone.xMax + 58 * u, y - 2 * u, 30 * u, 20 * u), "R", tecla, ativo.Pronto ? Desenho.Dourado : Desenho.Apagado);
        }
    }

    private static float Contador(float x, float y, float lado, int u, GUIStyle estilo, Sprite icone, int quanto)
    {
        if (icone != null)
            Desenho.Sprite(new Rect(x, y, lado, lado), icone, Color.white);

        Desenho.Texto(new Rect(x + lado + 3 * u, y - 2 * u, 40 * u, lado + 4 * u), quanto.ToString(), estilo, Desenho.Claro);
        return x + lado + 34 * u;
    }

    // Os itens que pegou, em fila no canto de baixo a esquerda (os mais novos no fim).
    private void Itens(EstatisticasDoJogador itens, int u)
    {
        float m = 8 * u;
        float lado = 18 * u;
        int cabem = Mathf.Max(1, Mathf.FloorToInt((Screen.width * 0.45f) / (lado + 3 * u)));
        int comeco = Mathf.Max(0, itens.Itens.Count - cabem);

        for (int i = comeco; i < itens.Itens.Count; i++)
        {
            ItemPassivo item = itens.Itens[i];
            Rect onde = new Rect(m + (i - comeco) * (lado + 3 * u), Screen.height - m - lado, lado, lado);
            Desenho.Sprite(onde, item.Desenho, Color.white);
        }
    }

    private void Arma(ArmaDoJogador armas, int u)
    {
        ArmaCarregada atual = armas.Atual;

        if (atual == null)
            return;

        float m = 8 * u;
        Texture2D moldura = tela.molduraPequena;
        float lado = (moldura != null ? moldura.width : 72) * u;
        Rect quadro = new Rect(Screen.width - m - lado, Screen.height - m - lado, lado, lado);

        // A arma na moldura, no maior tamanho inteiro que cabe.
        if (moldura != null)
            Desenho.Imagem(quadro, moldura, Color.white);

        Sprite desenho = atual.Dados.Desenho != null ? atual.Dados.Desenho : atual.Dados.desenhoDoTiro;

        if (desenho != null)
        {
            Rect r = desenho.textureRect;
            int vezes = Mathf.Max(1, Mathf.FloorToInt(44f / Mathf.Max(r.width, r.height)));
            Vector2 tamanho = new Vector2(r.width, r.height) * vezes * u;
            Desenho.Sprite(new Rect(quadro.center.x - tamanho.x * 0.5f, quadro.center.y - tamanho.y * 0.5f, tamanho.x, tamanho.y), desenho, Color.white);
        }

        // Do lado esquerdo da moldura: nome, municao e recarga, alinhados pela direita.
        GUIStyle medio = Desenho.Estilo(tela.fonteTexto, 22 * u, TextAnchor.LowerRight);
        float direita = quadro.x - 6 * u;
        float largura = 260 * u;

        float linhaDeTexto = 32 * u;
        Desenho.Texto(new Rect(direita - largura, quadro.y, largura, linhaDeTexto), atual.Dados.nome, medio, Desenho.Claro);

        string municao;
        Color cor = Desenho.Claro;

        if (armas.Recarregando)
        {
            municao = "recarregando";
            cor = Desenho.Dourado;
        }
        else if (atual.Infinita)
        {
            municao = "infinita";
            cor = Desenho.Apagado;
        }
        else if (atual.Vazia)
        {
            municao = "sem munição";
            cor = Desenho.Vermelho;
        }
        else
        {
            // Um numero so: quantos tiros a arma ainda tem. Pouca municao fica vermelha.
            int total = atual.NoPente + atual.Reserva;
            municao = $"{total}";
            cor = total <= atual.Dados.municaoMaxima / 5 ? Desenho.Vermelho : Desenho.Claro;
        }

        Rect linha = new Rect(direita - largura, quadro.y + linhaDeTexto, largura, linhaDeTexto);
        Desenho.Texto(linha, municao, medio, cor);

        // A bolsa do lado da municao (so pra arma que gasta).
        if (!atual.Infinita && tela.bolsa != null)
        {
            float w = Desenho.Largura(municao, medio);
            float icone = 16 * u;
            Desenho.Imagem(new Rect(direita - w - icone - 4 * u, linha.y + linha.height - icone - 2 * u, icone, icone), tela.bolsa, Color.white);
        }

        // A recarga: uma barra dourada que enche.
        if (armas.Recarregando)
        {
            Rect barra = new Rect(direita - 80 * u, linha.y + linha.height + 4 * u, 80 * u, 13 * u);
            Barra(barra, armas.ProgressoDaRecarga, Desenho.Dourado);
        }

        // A outra arma, em cima da moldura.
        if (armas.Outra != null)
        {
            GUIStyle outra = Desenho.Estilo(tela.fonteTexto, 16 * u, TextAnchor.LowerRight);
            Desenho.Texto(new Rect(Screen.width - m - largura, quadro.y - 26 * u, largura, 24 * u),
                          "Q  " + armas.Outra.Dados.nome, outra, Desenho.Apagado);
        }
    }

    private void Andar(GeradorDoAndar gerador, int u)
    {
        float m = 8 * u;
        float largura = 300 * u;
        GUIStyle pequeno = Desenho.Estilo(tela.fonteTexto, 16 * u, TextAnchor.UpperRight);
        GUIStyle medio = Desenho.Estilo(tela.fonteTexto, 22 * u, TextAnchor.UpperRight);
        Rect onde = new Rect(Screen.width - m - largura, m, largura, 24 * u);

        Desenho.Texto(onde, $"Andar {gerador.Andar} de {gerador.Andares}", pequeno, Desenho.Apagado);
        onde.y += 22 * u;
        onde.height = 32 * u;

        if (gerador.PortalAberto)
            Desenho.Texto(onde, "O portal abriu!", medio, Desenho.Dourado);
        else if (gerador.ChefeAtual == null)
            Desenho.Texto(onde, $"Inimigos: {gerador.Faltam}", medio, Desenho.Claro);
    }

    private void BarraDoChefe(Chefe chefe, int u)
    {
        if (!chefe.Apresentou || chefe.Vida.Morto)
            return;

        float largura = Mathf.Min(Screen.width * 0.6f, 320 * u);
        Rect barra = new Rect((Screen.width - largura) * 0.5f, 40 * u, largura, 13 * u);
        GUIStyle nome = Desenho.Estilo(tela.fonteTitulo, 24 * u, TextAnchor.LowerCenter);

        Desenho.Texto(new Rect(barra.x, barra.y - 30 * u, barra.width, 28 * u), chefe.Nome, nome, chefe.NaFuria ? Desenho.Vermelho : Desenho.Dourado);
        Barra(barra, chefe.Vida.Fracao, chefe.NaFuria ? new Color(1f, 0.4f, 0.15f) : new Color(0.85f, 0.16f, 0.2f));
    }

    // A barra dourada do pacote, com o fundo escuro e a parte cheia por dentro.
    private void Barra(Rect onde, float fracao, Color cor)
    {
        int u = Desenho.U;
        Rect dentro = new Rect(onde.x + 3 * u, onde.y + 3 * u, onde.width - 6 * u, onde.height - 6 * u);
        Desenho.Cor(dentro, new Color(0.08f, 0.06f, 0.12f, 0.9f));
        Desenho.Cor(new Rect(dentro.x, dentro.y, dentro.width * Mathf.Clamp01(fracao), dentro.height), cor);

        Texture2D barra = tela.barraDourada;

        if (barra != null)
            Desenho.Moldura(onde, barra, new Rect(0f, 0f, barra.width, barra.height), 4, Color.white, false);
    }

    private void Dica(Interativo coisa, int u)
    {
        Camera cam = Camera.main;

        if (cam == null)
            return;

        Vector3 naTela = cam.WorldToScreenPoint(coisa.transform.position + Vector3.up * 0.9f);
        GUIStyle estilo = Desenho.Estilo(tela.fonteTexto, 16 * u, TextAnchor.LowerCenter);
        float largura = 300 * u;
        Desenho.Texto(new Rect(naTela.x - largura * 0.5f, Screen.height - naTela.y - 22 * u, largura, 20 * u), "E  " + coisa.Dica, estilo, Desenho.Claro);
    }
}
