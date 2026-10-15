using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A mesa do apostador (<see cref="Apostador"/>): escolhe quanto apostar (5, 10 ou 20 moedas), os dois
/// rolam dois dados e quem somar mais leva. Ganhou, a aposta volta em dobro; empatou, volta igual;
/// perdeu, fica com ele. Os dados giram um pouco antes de parar (com o jogo parado). Cada mesa aceita
/// <see cref="Apostador.ApostasPorMesa"/> apostas. Esc ou B fecha.
/// </summary>
[DisallowMultipleComponent]
public class TelaDasApostas : MonoBehaviour
{
    private static readonly int[] Valores = { 5, 10, 20 };
    private static readonly Color Ouro = new Color(1f, 0.85f, 0.4f);
    private static readonly Color Claro = new Color(1f, 0.97f, 0.92f);
    private const float TempoRolando = 1.1f;

    private static TelaDasApostas atual;
    private static int quadroQueFechou = -1;

    /// <summary>Apostas ganhas em seguida (a conquista e com tres).</summary>
    private static int seguidas;

    private GameObject jogador;
    private Apostador apostador;
    private Bolsa bolsa;
    private MenuDeBotoes menu;
    private Text moedas, resultado, placarDoJogador, placarDele;
    private readonly Image[] dadosDoJogador = new Image[2];
    private readonly Image[] dadosDele = new Image[2];
    private Sprite[] faces;
    private int quadroQueAbriu;
    private int saldo;

    // A rolada em andamento.
    private bool rolando;
    private float rolouEm, proximaTroca;
    private int aposta;
    private readonly int[] meus = new int[2], deles = new int[2];

    public static bool Aberta => atual != null;

    public static bool Ocupada => atual != null || Time.frameCount == quadroQueFechou;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar()
    {
        atual = null;
        quadroQueFechou = -1;
        seguidas = 0;
    }

    public static void Abrir(GameObject jogador, Apostador apostador)
    {
        if (atual != null || apostador == null)
            return;

        TelaDasApostas tela = new GameObject("Mesa de apostas").AddComponent<TelaDasApostas>();
        tela.jogador = jogador;
        tela.apostador = apostador;
        tela.bolsa = jogador != null ? jogador.GetComponent<Bolsa>() : null;
        tela.Montar();
    }

    private void Montar()
    {
        atual = this;
        quadroQueAbriu = Time.frameCount;
        faces = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Cenario/Dados"), new Vector2Int(24, 24), 24f);

        TelaSimples.Montar(gameObject, 40, new Color(0.02f, 0.01f, 0.02f, 0.86f));
        TelaSimples.Painel(transform, "Moldura", ArteDaInterface.MolduraGrande, -30f, new Vector2(1240f, 760f));
        TelaSimples.Faixa(transform, "Faixa", ArteDaInterface.FaixaRosa, 390f, 900f);
        TelaSimples.Titulo(transform, "Titulo", 96, new Color(1f, 0.95f, 0.85f), 390f, "Os dados do apostador");
        moedas = TelaSimples.Texto(transform, "Moedas", 34, Ouro, 290f, "");

        // Os dados: os seus na esquerda, os dele na direita.
        Text voce = TelaSimples.Texto(transform, "Voce", 32, Claro, 215f, "Você");
        voce.rectTransform.anchoredPosition = new Vector2(-260f, 215f);
        Text ele = TelaSimples.Texto(transform, "Ele", 32, Claro, 215f, "Apostador");
        ele.rectTransform.anchoredPosition = new Vector2(260f, 215f);

        for (int i = 0; i < 2; i++)
        {
            dadosDoJogador[i] = Dado("Seu dado " + i, new Vector2(-320f + 120f * i, 120f));
            dadosDele[i] = Dado("Dado dele " + i, new Vector2(200f + 120f * i, 120f));
        }

        placarDoJogador = TelaSimples.Texto(transform, "Seu placar", 30, Claro, 40f, "");
        placarDoJogador.rectTransform.anchoredPosition = new Vector2(-260f, 40f);
        placarDele = TelaSimples.Texto(transform, "Placar dele", 30, Claro, 40f, "");
        placarDele.rectTransform.anchoredPosition = new Vector2(260f, 40f);
        resultado = TelaSimples.Texto(transform, "Resultado", 34, Ouro, -30f, "");
        resultado.rectTransform.sizeDelta = new Vector2(1100f, 60f);

        menu = new MenuDeBotoes(transform, 0f, new Vector2(560f, 58f), 28) { IntervaloDaEntrada = 0.03f };
        float y = -110f;

        foreach (int v in Valores)
        {
            int valor = v;
            menu.Adicionar(Idioma.F("Apostar {0} moedas", valor), y, () => Apostar(valor));
            y -= 66f;
        }

        menu.Adicionar("Sair", y - 6f, Fechar, "[Esc] || [Pad B]");

        TelaSimples.LinhaDeTeclas(transform, "Teclas", -470f,
            "[W][S] escolher | [Enter] apostar | [Esc] sair || [Pad CruzCima][Pad CruzBaixo] escolher | [Pad A] apostar | [Pad B] sair",
            24, new Color(0.85f, 0.85f, 0.9f));

        for (int i = 0; i < 2; i++)
        {
            meus[i] = Random.Range(1, 7);
            deles[i] = Random.Range(1, 7);
        }

        MostrarDados();
        resultado.text = Idioma.T("Quem somar mais nos dois dados leva. Empate: a aposta volta.");
        Atualizar();
        Time.timeScale = 0f;
        TelaSimples.TravarJogador(jogador != null ? jogador.transform : null, true);
        Sons.Tocar(Som.MenuAbrir, 1f, 0f);
    }

    private Image Dado(string nome, Vector2 onde)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(transform, false);
        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = onde;
        rt.sizeDelta = new Vector2(96f, 96f);
        Image imagem = obj.AddComponent<Image>();
        imagem.raycastTarget = false;
        return imagem;
    }

    private void Update()
    {
        if (Time.frameCount == quadroQueAbriu)
            return;

        if (rolando)
        {
            Rolar();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Controle.Apertou(BotaoDoControle.B))
        {
            Fechar();
            return;
        }

        menu.Atualizar();
    }

    private void Atualizar()
    {
        int tem = bolsa != null ? bolsa.Moedas : 0;
        moedas.text = Idioma.F("Você tem {0} moedas  -  faltam {1} apostas", tem, apostador != null ? apostador.Restantes : 0);
        bool acabou = apostador == null || apostador.Restantes <= 0;

        for (int i = 0; i < Valores.Length; i++)
            menu.Apagar(i, acabou || tem < Valores[i]);
    }

    private void Apostar(int valor)
    {
        if (rolando || apostador == null || apostador.Restantes <= 0 || bolsa == null || !bolsa.Gastar(valor))
        {
            Sons.Tocar(Som.MenuNegado, 0.8f, 0f);
            return;
        }

        apostador.Restantes--;
        aposta = valor;
        rolando = true;
        menu.Ligado = false;
        rolouEm = Time.unscaledTime;
        proximaTroca = 0f;
        resultado.text = Idioma.F("Valendo {0} moedas...", valor);
        placarDoJogador.text = placarDele.text = "";
        Sons.Tocar(Som.Dados, 1f, 0.05f);
        Atualizar();
    }

    // Os dados trocam de face cada vez mais devagar ate parar; ai sai o resultado.
    private void Rolar()
    {
        float passou = Time.unscaledTime - rolouEm;

        if (passou < TempoRolando)
        {
            if (Time.unscaledTime >= proximaTroca)
            {
                for (int i = 0; i < 2; i++)
                {
                    meus[i] = Random.Range(1, 7);
                    deles[i] = Random.Range(1, 7);
                }

                MostrarDados();
                proximaTroca = Time.unscaledTime + Mathf.Lerp(0.04f, 0.18f, passou / TempoRolando);
            }

            return;
        }

        // A rolada de verdade.
        for (int i = 0; i < 2; i++)
        {
            meus[i] = Random.Range(1, 7);
            deles[i] = Random.Range(1, 7);
        }

        MostrarDados();
        rolando = false;
        menu.Ligado = true;

        int seu = meus[0] + meus[1], dele = deles[0] + deles[1];
        placarDoJogador.text = seu.ToString();
        placarDele.text = dele.ToString();

        if (seu > dele)
        {
            bolsa.Ganhar(aposta * 2, 0);
            saldo += aposta;
            seguidas++;
            resultado.text = Idioma.F("Você ganhou {0} moedas!", aposta);
            resultado.color = new Color(0.6f, 1f, 0.55f);
            Sons.Tocar(Som.Moeda, 1f, 0f);
            Sons.Tocar(Som.Segredo, 0.6f, 0f);

            if (seguidas >= 3)
                Conquistas.Conquistar(Conquistas.Apostador);
        }
        else if (seu < dele)
        {
            saldo -= aposta;
            seguidas = 0;
            resultado.text = Idioma.F("Ele levou suas {0} moedas.", aposta);
            resultado.color = new Color(1f, 0.5f, 0.45f);
            Sons.Tocar(Som.Negado, 0.8f, 0f);
        }
        else
        {
            bolsa.Ganhar(aposta, 0);
            resultado.text = Idioma.T("Empate: a aposta volta.");
            resultado.color = Ouro;
            Sons.Tocar(Som.Moeda, 0.6f, 0f);
        }

        Atualizar();

        if (apostador.Restantes <= 0)
            menu.Escolher(Valores.Length, false);
    }

    private void MostrarDados()
    {
        if (faces == null || faces.Length < 6)
            return;

        for (int i = 0; i < 2; i++)
        {
            dadosDoJogador[i].sprite = faces[meus[i] - 1];
            dadosDele[i].sprite = faces[deles[i] - 1];
        }
    }

    private void Fechar()
    {
        if (atual != this || rolando)
            return;

        Sons.Tocar(Som.MenuFechar, 1f, 0f);
        quadroQueFechou = Time.frameCount;
        Time.timeScale = 1f;
        TelaSimples.TravarJogador(jogador != null ? jogador.transform : null, false);
        atual = null;

        if (apostador != null && apostador.Restantes <= 0)
            apostador.Despedir(saldo);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (atual == this)
        {
            atual = null;
            Time.timeScale = 1f;
        }
    }
}
