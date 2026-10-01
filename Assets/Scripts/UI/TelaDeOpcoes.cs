using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A tela de opcoes, aberta com O (X no controle) pelo menu inicial e pela pausa: volume da musica e dos
/// efeitos, tela cheia, resolucao, tremor da tela e numeros de dano. Tudo fica salvo entre partidas (ver <see cref="Opcoes"/>).
///
///   W/S ou Cima/Baixo  escolhe a linha      A/D ou Esquerda/Direita  muda o valor
///   Enter              alterna / volta      Esc ou O                 volta
///
/// No controle: cruz ou analogico escolhe e muda, A alterna, B (ou X, ou Start) volta.
///
/// M e N continuam valendo aqui e as barras acompanham. Enquanto esta aberta, quem abriu
/// (menu ou pausa) deve ignorar as teclas: ver <see cref="Ocupada"/>.
/// </summary>
[DisallowMultipleComponent]
public class TelaDeOpcoes : MonoBehaviour
{
    private enum Linha { Musica, Efeitos, TelaCheia, Resolucao, Tremor, Numeros, Voltar }

    private static readonly string[] Nomes = { "Música", "Efeitos", "Tela cheia", "Resolução", "Tremor da tela", "Números de dano", "Voltar" };
    private static readonly float[] Alturas = { 185f, 120f, 55f, -10f, -75f, -140f, -232f };

    private const float XDoNome = -330f;
    private const float XDoValor = 190f;
    private const float LarguraDaBarra = 360f;

    private static readonly Color CorNormal = new Color(1f, 0.97f, 0.92f);
    private static readonly Color CorEscolhida = new Color(1f, 0.85f, 0.35f);

    private static TelaDeOpcoes atual;
    private static int quadroQueFechou = -1;

    private Action aoFechar;
    private int escolhida;
    private int quadroQueAbriu;

    private readonly Text[] nomes = new Text[Nomes.Length];
    private readonly Text[] valores = new Text[Nomes.Length];
    private readonly RectTransform[] barras = new RectTransform[2];
    private Image setaEsquerda;
    private Image setaDireita;
    private Image ponteiro;
    private Image botaoDeVoltar;
    private float acesoAte;
    private int ladoAceso;

    public static bool Aberta => atual != null;

    /// <summary>
    /// Aberta, ou fechou neste quadro: o Esc que fechou as opcoes nao pode tambem fechar
    /// a pausa ou sair do jogo pelo menu inicial.
    /// </summary>
    public static bool Ocupada => atual != null || Time.frameCount == quadroQueFechou;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar()
    {
        atual = null;
        quadroQueFechou = -1;
    }

    /// <summary>Abre por cima de tudo. <paramref name="aoFechar"/> roda quando voltar.</summary>
    public static void Abrir(Action aoFechar = null)
    {
        if (atual != null)
            return;

        TelaDeOpcoes tela = new GameObject("Opcoes").AddComponent<TelaDeOpcoes>();
        tela.aoFechar = aoFechar;
        tela.Montar();
    }

    private void Montar()
    {
        atual = this;
        quadroQueAbriu = Time.frameCount;

        // Fundo opaco: o menu ou a pausa de baixo nao aparecem embolados com as linhas daqui.
        TelaSimples.Montar(gameObject, 120, new Color(0.03f, 0.02f, 0.03f, 1f));
        TelaSimples.Titulo(transform, "Titulo", 110, new Color(1f, 0.95f, 0.85f), 330f, "Configurações");

        for (int i = 0; i < Nomes.Length; i++)
        {
            bool voltar = i == (int)Linha.Voltar;
            nomes[i] = TextoNaLinha("Nome " + Nomes[i], voltar ? 0f : XDoNome, Alturas[i], voltar ? 40 : 38,
                                    voltar ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft, Nomes[i]);

            // Nas barras de volume o numero fica a direita da barra.
            if (!voltar)
            {
                float x = i <= (int)Linha.Efeitos ? XDoValor + LarguraDaBarra / 2f + 70f : XDoValor;
                valores[i] = TextoNaLinha("Valor " + Nomes[i], x, Alturas[i], 32, TextAnchor.MiddleCenter, "");
            }
        }

        barras[0] = Barra("Barra da musica", Alturas[(int)Linha.Musica]);
        barras[1] = Barra("Barra dos efeitos", Alturas[(int)Linha.Efeitos]);

        setaEsquerda = Imagem("Seta esquerda", Vector2.zero, 48f);
        setaDireita = Imagem("Seta direita", Vector2.zero, 48f);
        ponteiro = Imagem("Ponteiro", Vector2.zero, 64f);

        TelaSimples.LinhaDeTeclas(transform, "Teclas", -345f,
            "[W][S] escolher | [A][D] mudar | [Esc] voltar || " +
            "[Pad CruzCima][Pad CruzBaixo] escolher | [Pad CruzEsquerda][Pad CruzDireita] mudar | [Pad B] voltar", 28, new Color(0.92f, 0.92f, 0.95f));

        if (Application.isEditor)
        {
            TelaSimples.Texto(transform, "Aviso do editor", 20, new Color(0.65f, 0.6f, 0.6f), -400f,
                "No editor a janela não muda de tamanho: tela cheia e resolução valem no jogo compilado.");
        }

        // Moldura, botao de voltar e faixa do Dragon Regalia atras dos textos.
        botaoDeVoltar = TelaSimples.Painel(transform, "Botao de voltar", ArteDaInterface.Botao(0),
                                           Alturas[(int)Linha.Voltar], new Vector2(340f, 95f));
        TelaSimples.Painel(transform, "Painel", ArteDaInterface.MolduraGrande, -20f, new Vector2(1250f, 580f));
        TelaSimples.Faixa(transform, "Faixa", ArteDaInterface.FaixaRosa, 330f, 820f);

        Atualizar();
        Sons.Tocar(Som.MenuAbrir, 1f, 0f);
    }

    private void Update()
    {
        // O O que abriu esta tela nao pode fecha-la no mesmo quadro.
        if (Time.frameCount == quadroQueAbriu)
            return;

        bool mudouSom = Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.N)
                        || Controle.Apertou(BotaoDoControle.LB) || Controle.Apertou(BotaoDoControle.RB);
        Opcoes.LerTeclas();

        if (mudouSom)
            Atualizar();

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.O) || Controle.Apertou(BotaoDoControle.B)
            || Controle.Apertou(BotaoDoControle.X) || Controle.Apertou(BotaoDoControle.Start))
        {
            Fechar();
            return;
        }

        if (Apertou(KeyCode.UpArrow, KeyCode.W) || Controle.Apertou(BotaoDoControle.CruzCima))
            Escolher(-1);
        else if (Apertou(KeyCode.DownArrow, KeyCode.S) || Controle.Apertou(BotaoDoControle.CruzBaixo))
            Escolher(1);
        else if (Apertou(KeyCode.LeftArrow, KeyCode.A) || Controle.Apertou(BotaoDoControle.CruzEsquerda))
            Mudar(-1);
        else if (Apertou(KeyCode.RightArrow, KeyCode.D) || Controle.Apertou(BotaoDoControle.CruzDireita))
            Mudar(1);
        else if (Apertou(KeyCode.Return, KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space) || Controle.Apertou(BotaoDoControle.A))
            Confirmar();

        AnimarPonteiro(Time.unscaledTime);
    }

    private static bool Apertou(KeyCode a, KeyCode b) => Input.GetKeyDown(a) || Input.GetKeyDown(b);

    private void Escolher(int passo)
    {
        escolhida = (escolhida + passo + Nomes.Length) % Nomes.Length;
        Sons.Tocar(Som.Menu, 0.6f);
        Atualizar();
    }

    private void Mudar(int passo)
    {
        switch ((Linha)escolhida)
        {
            case Linha.Musica: Opcoes.MudarMusica(passo); break;
            case Linha.Efeitos: Opcoes.MudarEfeitos(passo); break;
            case Linha.TelaCheia: Opcoes.AlternarTelaCheia(); break;
            case Linha.Resolucao: Opcoes.MudarResolucao(passo); break;
            case Linha.Tremor: Opcoes.AlternarTremor(); break;
            case Linha.Numeros: Opcoes.AlternarNumeros(); break;
            default: return;
        }

        ladoAceso = passo;
        acesoAte = Time.unscaledTime + 0.15f;

        // Nos efeitos o proprio som ja mostra o volume novo.
        Sons.Tocar(Som.Menu, 0.8f, 0f);
        Atualizar();
    }

    private void Confirmar()
    {
        switch ((Linha)escolhida)
        {
            case Linha.Voltar:
                Fechar();
                break;
            case Linha.TelaCheia:
            case Linha.Tremor:
            case Linha.Numeros:
                Mudar(1);
                break;
            default:
                Escolher(1);
                break;
        }
    }

    private void Fechar()
    {
        Sons.Tocar(Som.MenuFechar, 1f, 0f);
        quadroQueFechou = Time.frameCount;
        Destroy(gameObject);
        aoFechar?.Invoke();
    }

    private void OnDestroy()
    {
        if (atual == this)
            atual = null;
    }

    // ---------------- desenho ----------------
    private void Atualizar()
    {
        for (int i = 0; i < Nomes.Length; i++)
            nomes[i].color = i == escolhida ? CorEscolhida : CorNormal;

        valores[(int)Linha.Musica].text = Porcento(Musica.Volume);
        valores[(int)Linha.Efeitos].text = Porcento(Sons.Volume);
        valores[(int)Linha.TelaCheia].text = Opcoes.TelaCheia ? "Ligada" : "Desligada";
        valores[(int)Linha.Tremor].text = Opcoes.TremorLigado ? "Ligado" : "Desligado";
        valores[(int)Linha.Numeros].text = Opcoes.NumerosDeDano ? "Ligados" : "Desligados";

        Vector2Int resolucao = Opcoes.Resolucao;
        valores[(int)Linha.Resolucao].text = $"{resolucao.x} x {resolucao.y}";

        EncherBarra(barras[0], Musica.Volume);
        EncherBarra(barras[1], Sons.Volume);

        bool naLinhaDeValor = escolhida != (int)Linha.Voltar;
        float y = Alturas[escolhida];
        bool naBarra = escolhida <= (int)Linha.Efeitos;

        // Nas barras, a seta direita vem depois do numero do volume.
        float esquerda = naBarra ? XDoValor - LarguraDaBarra / 2f - 50f : XDoValor - 190f;
        float direita = naBarra ? XDoValor + LarguraDaBarra / 2f + 140f : XDoValor + 190f;
        setaEsquerda.rectTransform.anchoredPosition = new Vector2(esquerda, y);
        setaDireita.rectTransform.anchoredPosition = new Vector2(direita, y);
        setaEsquerda.gameObject.SetActive(naLinhaDeValor);
        setaDireita.gameObject.SetActive(naLinhaDeValor);

        ponteiro.rectTransform.anchoredPosition = new Vector2(XDoPonteiro, y);
        ponteiro.sprite = ArteDaInterface.Ponteiro(0);
        ponteiro.enabled = ponteiro.sprite != null;

        if (botaoDeVoltar != null)
            botaoDeVoltar.sprite = ArteDaInterface.Botao(escolhida == (int)Linha.Voltar ? 1 : 0);

        AnimarPonteiro(Time.unscaledTime);
    }

    /// <summary>O ponteiro fica antes do nome; no Voltar, antes do botao.</summary>
    private float XDoPonteiro => escolhida != (int)Linha.Voltar ? XDoNome - 60f : -230f;

    private static string Porcento(float volume) => volume <= 0f ? "Mudo" : $"{Mathf.RoundToInt(volume * 100f)}%";

    private void AnimarPonteiro(float tempo)
    {
        bool aceso = tempo < acesoAte;
        setaEsquerda.sprite = ArteDaInterface.SetaEsquerda(aceso && ladoAceso < 0 ? 1 : 0);
        setaDireita.sprite = ArteDaInterface.SetaDireita(aceso && ladoAceso > 0 ? 1 : 0);
        setaEsquerda.enabled = setaEsquerda.sprite != null;
        setaDireita.enabled = setaDireita.sprite != null;

        if (!ponteiro.enabled)
            return;

        ponteiro.sprite = ArteDaInterface.Ponteiro(Mathf.FloorToInt(tempo * 8f));
        RectTransform rt = ponteiro.rectTransform;
        rt.anchoredPosition = new Vector2(XDoPonteiro + Mathf.Abs(Mathf.Sin(tempo * 4f)) * 10f, rt.anchoredPosition.y);
    }

    private Text TextoNaLinha(string nome, float x, float y, int tamanho, TextAnchor alinhamento, string conteudo)
    {
        Text texto = TelaSimples.Texto(transform, nome, tamanho, CorNormal, y, conteudo);
        texto.alignment = alinhamento;

        RectTransform rt = texto.rectTransform;
        rt.sizeDelta = new Vector2(alinhamento == TextAnchor.MiddleLeft ? 420f : 360f, tamanho * 2f);

        // Alinhado a esquerda: o x e onde o texto comeca.
        rt.pivot = new Vector2(alinhamento == TextAnchor.MiddleLeft ? 0f : 0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        return texto;
    }

    /// <summary>
    /// Barra de volume: moldura dourada do Dragon Regalia com a barra vazia e a amarela do
    /// Pixel UI por dentro. Sem os pacotes, retangulos lisos. Devolve o preenchimento.
    /// </summary>
    private RectTransform Barra(string nome, float y)
    {
        Sprite desenhoDaMoldura = ArteDaInterface.MolduraDaBarra;
        Vector4 borda = desenhoDaMoldura != null ? ArteDaInterface.BordaDaBarra * 4f : Vector4.one * 4f;
        const float altura = 20f;

        RectTransform moldura = Retangulo(nome, transform, new Color(0f, 0f, 0f, 0.75f), desenhoDaMoldura);
        moldura.anchorMin = moldura.anchorMax = new Vector2(0.5f, 0.5f);
        moldura.pivot = new Vector2(0.5f, 0.5f);
        moldura.anchoredPosition = new Vector2(XDoValor, y);
        moldura.sizeDelta = new Vector2(LarguraDaBarra + borda.x + borda.z, altura + borda.y + borda.w);

        if (desenhoDaMoldura != null)
            moldura.GetComponent<Image>().color = Color.white;

        Sprite vazia = ArteImportada.BarraVazia;
        RectTransform fundo = Retangulo("Fundo", moldura, new Color(0.15f, 0.1f, 0.1f), vazia);
        fundo.anchorMin = Vector2.zero;
        fundo.anchorMax = Vector2.one;
        fundo.offsetMin = new Vector2(borda.x, borda.y);
        fundo.offsetMax = new Vector2(-borda.z, -borda.w);

        if (vazia != null)
            fundo.GetComponent<Image>().color = Color.white;

        Sprite cheia = ArteImportada.BarraAmarela;
        RectTransform preenchimento = Retangulo("Volume", fundo, CorEscolhida, cheia);
        preenchimento.anchorMin = Vector2.zero;
        preenchimento.anchorMax = Vector2.one;
        preenchimento.offsetMin = preenchimento.offsetMax = Vector2.zero;

        if (cheia != null)
            preenchimento.GetComponent<Image>().color = Color.white;

        return preenchimento;
    }

    private static void EncherBarra(RectTransform preenchimento, float volume)
    {
        preenchimento.anchorMax = new Vector2(Mathf.Clamp01(volume), 1f);
        preenchimento.gameObject.SetActive(volume > 0f);
    }

    private static RectTransform Retangulo(string nome, Transform pai, Color cor, Sprite sprite)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);

        Image imagem = obj.AddComponent<Image>();
        imagem.color = cor;
        imagem.sprite = sprite;
        imagem.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        imagem.raycastTarget = false;
        return (RectTransform)obj.transform;
    }

    private Image Imagem(string nome, Vector2 posicao, float lado)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(transform, false);
        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = Vector2.one * lado;

        Image imagem = obj.AddComponent<Image>();
        imagem.raycastTarget = false;
        return imagem;
    }
}
