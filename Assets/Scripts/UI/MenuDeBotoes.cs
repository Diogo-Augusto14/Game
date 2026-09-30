using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Desenho redondo que vai no canto esquerdo de um botao do <see cref="MenuDeBotoes"/>.</summary>
public enum IconeDoBotao { Nenhum, Configuracoes, Sair }

/// <summary>
/// Uma coluna de botoes de menu (menu inicial, pausa, fim de jogo), com a arte do Dragon
/// Regalia: botao cinza, laranja no escolhido, ferrugem no de sair; o ponteiro dourado
/// aponta o escolhido e o atalho do teclado/controle fica do lado direito.
///
///   W/S, Cima/Baixo, cruz ou analogico   escolhe        Enter, Espaco ou A   aperta
///   mouse em cima                         escolhe        clique               aperta
///
/// Nao e componente: a tela que monta chama <see cref="Atualizar"/> no Update dela, e
/// antes disso trata os atalhos proprios (Esc, R, ...). Tudo em tempo real
/// (unscaledTime): funciona com o jogo congelado.
/// </summary>
public class MenuDeBotoes
{
    private class Botao
    {
        public RectTransform Raiz;
        public CanvasGroup Grupo;
        public Image Fundo;
        public Text Rotulo;
        public Image Icone;
        public IconeDoBotao TipoDoIcone;
        public bool Perigo;
        public bool Apagado;
        public Action Acao;
        public float Y;
    }

    private static readonly Color CorDoRotulo = new Color(1f, 0.97f, 0.92f);
    private static readonly Color CorDoEscolhido = Color.white;
    private static readonly Color CorApagada = new Color(1f, 0.6f, 0.45f);
    private static readonly Color CorDoAtalho = new Color(0.85f, 0.85f, 0.9f);

    private readonly Transform pai;
    private readonly float x;
    private readonly Vector2 tamanho;
    private readonly int tamanhoDoTexto;
    private readonly List<Botao> botoes = new List<Botao>();
    private readonly Image ponteiro;

    private Vector3 ultimoMouse;
    private float criadoEm;
    private float apertouEm = -10f;
    private int apertado = -1;

    /// <summary>O botao escolhido agora.</summary>
    public int Escolhido { get; private set; }

    /// <summary>Desligado, nao le teclado, controle nem mouse (as animacoes continuam).</summary>
    public bool Ligado { get; set; } = true;

    /// <summary>Segundos entre a entrada de um botao e a do proximo.</summary>
    public float IntervaloDaEntrada { get; set; } = 0.07f;

    /// <summary>Segundos ate o primeiro botao comecar a entrar.</summary>
    public float AtrasoDaEntrada { get; set; }

    /// <summary>Todos os botoes ja terminaram de entrar na tela.</summary>
    public bool Pronto => Time.unscaledTime >= criadoEm + AtrasoDaEntrada + IntervaloDaEntrada * botoes.Count + DuracaoDaEntrada;

    private const float DuracaoDaEntrada = 0.25f;

    public MenuDeBotoes(Transform pai, float x, Vector2 tamanho, int tamanhoDoTexto = 36)
    {
        this.pai = pai;
        this.x = x;
        this.tamanho = tamanho;
        this.tamanhoDoTexto = tamanhoDoTexto;
        criadoEm = Time.unscaledTime;
        ultimoMouse = Input.mousePosition;

        ponteiro = Imagem("Ponteiro", pai, Vector2.one * 70f);
        ponteiro.sprite = ArteDaInterface.Ponteiro(0);
        ponteiro.enabled = false;
    }

    /// <summary>Recomeca a animacao de entrada (os botoes sobem um a um).</summary>
    public void Reentrar(float atraso = 0f)
    {
        criadoEm = Time.unscaledTime;
        AtrasoDaEntrada = atraso;
    }

    /// <summary>
    /// Poe um botao na altura <paramref name="y"/>. <paramref name="atalho"/> e a dica do
    /// lado direito, no formato da <see cref="TelaSimples.LinhaDeTeclas"/> ("[Esc] || [Pad B]").
    /// <paramref name="perigo"/> pinta de ferrugem (sair, abandonar a partida).
    /// </summary>
    public int Adicionar(string rotulo, float y, Action acao, string atalho = null,
                         IconeDoBotao icone = IconeDoBotao.Nenhum, bool perigo = false)
    {
        Botao b = new Botao { Acao = acao, Perigo = perigo, TipoDoIcone = icone, Y = y };

        GameObject obj = new GameObject("Botao " + rotulo, typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        b.Raiz = (RectTransform)obj.transform;
        b.Raiz.anchorMin = b.Raiz.anchorMax = new Vector2(0.5f, 0.5f);
        b.Raiz.pivot = new Vector2(0.5f, 0.5f);
        b.Raiz.anchoredPosition = new Vector2(x, y);
        b.Raiz.sizeDelta = tamanho;
        b.Grupo = obj.AddComponent<CanvasGroup>();
        b.Grupo.alpha = 0f;
        b.Grupo.interactable = false;
        b.Grupo.blocksRaycasts = false;

        b.Fundo = obj.AddComponent<Image>();
        b.Fundo.type = Image.Type.Sliced;
        b.Fundo.raycastTarget = false;

        // Sem a arte do pacote, um retangulo escuro com o texto por cima.
        if (ArteDaInterface.Botao(0) == null)
            b.Fundo.color = new Color(0.2f, 0.16f, 0.2f, 0.9f);

        b.Rotulo = TelaSimples.Texto(b.Raiz, "Rotulo", tamanhoDoTexto, CorDoRotulo, 0f, rotulo);
        b.Rotulo.rectTransform.sizeDelta = new Vector2(tamanho.x, tamanho.y);
        b.Rotulo.horizontalOverflow = HorizontalWrapMode.Overflow;

        if (icone != IconeDoBotao.Nenhum && DesenhoDoIcone(icone, 0) != null)
        {
            float lado = Mathf.Round(tamanho.y * 0.72f);
            b.Icone = Imagem("Icone", b.Raiz, Vector2.one * lado);
            b.Icone.rectTransform.anchoredPosition = new Vector2(-tamanho.x / 2f + lado * 0.5f + 34f, 0f);
            b.Icone.sprite = DesenhoDoIcone(icone, 0);

            // O texto sai um pouco pra direita pra nao encostar no icone.
            b.Rotulo.rectTransform.anchoredPosition = new Vector2(lado * 0.35f, 0f);
        }

        if (!string.IsNullOrEmpty(atalho))
        {
            RectTransform dica = TelaSimples.LinhaDeTeclas(b.Raiz, "Atalho", 0f, atalho, 22, CorDoAtalho);
            dica.anchorMin = dica.anchorMax = new Vector2(1f, 0.5f);
            dica.pivot = new Vector2(0f, 0.5f);
            dica.anchoredPosition = new Vector2(24f, 0f);
        }

        botoes.Add(b);
        Desenhar(Time.unscaledTime);
        return botoes.Count - 1;
    }

    /// <summary>Apagado: botao roxo-escuro, ainda escolhivel (quem apertar decide o que fazer).</summary>
    public void Apagar(int indice, bool apagado)
    {
        if (indice >= 0 && indice < botoes.Count)
            botoes[indice].Apagado = apagado;
    }

    public void TrocarRotulo(int indice, string rotulo)
    {
        if (indice >= 0 && indice < botoes.Count)
            botoes[indice].Rotulo.text = rotulo;
    }

    /// <summary>Escolhe um botao sem apertar (ex.: Esc no menu vai pro "Sair do jogo").</summary>
    public void Escolher(int indice, bool comSom = true)
    {
        if (indice < 0 || indice >= botoes.Count || indice == Escolhido)
            return;

        Escolhido = indice;

        if (comSom)
            Sons.Tocar(Som.Menu, 0.6f);
    }

    /// <summary>Aperta o botao (mesmo efeito do Enter em cima dele).</summary>
    public void Apertar(int indice)
    {
        if (indice < 0 || indice >= botoes.Count)
            return;

        Escolhido = indice;
        apertado = indice;
        apertouEm = Time.unscaledTime;
        botoes[indice].Acao?.Invoke();
    }

    /// <summary>Le teclado, controle e mouse e anima. Chame uma vez por quadro.</summary>
    public void Atualizar()
    {
        float agora = Time.unscaledTime;

        if (Ligado && Pronto && botoes.Count > 0 && !TransicaoDeTela.Ocupada)
            LerEntrada();

        Desenhar(agora);
    }

    private void LerEntrada()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Controle.Apertou(BotaoDoControle.CruzCima))
        {
            Escolher((Escolhido - 1 + botoes.Count) % botoes.Count);
            return;
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Controle.Apertou(BotaoDoControle.CruzBaixo))
        {
            Escolher((Escolhido + 1) % botoes.Count);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)
            || Controle.Apertou(BotaoDoControle.A))
        {
            Apertar(Escolhido);
            return;
        }

        // Mouse: so escolhe quando mexe (parado em cima de um botao nao briga com o teclado).
        Vector3 mouse = Input.mousePosition;
        bool mexeu = (mouse - ultimoMouse).sqrMagnitude > 1f;
        ultimoMouse = mouse;
        int emCima = BotaoSobOMouse(mouse);

        if (emCima < 0)
            return;

        if (mexeu)
            Escolher(emCima);

        if (Input.GetMouseButtonDown(0))
            Apertar(emCima);
    }

    private int BotaoSobOMouse(Vector3 mouse)
    {
        for (int i = 0; i < botoes.Count; i++)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(botoes[i].Raiz, mouse, null))
                return i;
        }

        return -1;
    }

    // ---------------- desenho ----------------
    private void Desenhar(float agora)
    {
        for (int i = 0; i < botoes.Count; i++)
        {
            Botao b = botoes[i];
            bool escolhido = i == Escolhido;

            // Entrada: cada botao sobe e aparece, um pouco depois do anterior.
            float t = Mathf.Clamp01((agora - criadoEm - AtrasoDaEntrada - IntervaloDaEntrada * i) / DuracaoDaEntrada);
            float suave = 1f - (1f - t) * (1f - t) * (1f - t);
            b.Grupo.alpha = suave;

            // Escolhido pulsa de leve; o que acabou de ser apertado da um pulo.
            float escala = escolhido ? 1.04f + Mathf.Sin(agora * 5f) * 0.015f : 1f;
            float desdeOAperto = agora - apertouEm;

            if (i == apertado && desdeOAperto < 0.2f)
                escala += 0.08f * (1f - desdeOAperto / 0.2f);

            b.Raiz.localScale = Vector3.one * escala;
            b.Raiz.anchoredPosition = new Vector2(x, b.Y - (1f - suave) * 40f);

            Sprite fundo = ArteDaInterface.Botao(b.Apagado ? 3 : escolhido ? 1 : b.Perigo ? 2 : 0);

            if (fundo != null)
                b.Fundo.sprite = fundo;

            b.Rotulo.color = b.Apagado ? CorApagada : escolhido ? CorDoEscolhido : CorDoRotulo;

            if (b.Icone != null)
                b.Icone.sprite = DesenhoDoIcone(b.TipoDoIcone, escolhido ? 1 : 0);
        }

        DesenharPonteiro(agora);
    }

    private void DesenharPonteiro(float agora)
    {
        if (botoes.Count == 0 || ponteiro.sprite == null)
            return;

        Botao b = botoes[Escolhido];
        ponteiro.enabled = b.Grupo.alpha > 0.5f;
        ponteiro.sprite = ArteDaInterface.Ponteiro(Mathf.FloorToInt(agora * 8f));
        ponteiro.rectTransform.SetAsLastSibling();

        float balanco = Mathf.Abs(Mathf.Sin(agora * 4f)) * 12f;
        ponteiro.rectTransform.anchoredPosition = new Vector2(x - tamanho.x / 2f - 48f + balanco, b.Raiz.anchoredPosition.y);
    }

    private static Sprite DesenhoDoIcone(IconeDoBotao icone, int estado)
    {
        switch (icone)
        {
            case IconeDoBotao.Configuracoes: return ArteDaInterface.IconeOpcoes(estado);
            case IconeDoBotao.Sair: return ArteDaInterface.IconeSair(estado);
            default: return null;
        }
    }

    private static Image Imagem(string nome, Transform pai, Vector2 lado)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = lado;

        Image imagem = obj.AddComponent<Image>();
        imagem.preserveAspect = true;
        imagem.raycastTarget = false;
        return imagem;
    }
}
