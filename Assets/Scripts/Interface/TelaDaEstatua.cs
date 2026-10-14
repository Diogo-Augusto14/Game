using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A tela da estatua dos cristais (<see cref="EstatuaDosCristais"/>): as melhorias (<see cref="Melhorias"/>)
/// em botoes, com o nivel e o preco, e embaixo o que a escolhida faz. Comprar gasta os cristais e ja vale
/// (o coracao entra na hora; a chave do Chaveiro tambem). Com o jogo parado; Esc ou B fecha.
/// </summary>
[DisallowMultipleComponent]
public class TelaDaEstatua : MonoBehaviour
{
    private static readonly Color Roxo = new Color(0.8f, 0.6f, 1f);

    private static TelaDaEstatua atual;
    private static int quadroQueFechou = -1;

    private GameObject jogador;
    private MenuDeBotoes menu;
    private Text cristais;
    private Text descricao;
    private int quadroQueAbriu;

    public static bool Aberta => atual != null;

    public static bool Ocupada => atual != null || Time.frameCount == quadroQueFechou;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar()
    {
        atual = null;
        quadroQueFechou = -1;
    }

    public static void Abrir(GameObject jogador)
    {
        if (atual != null)
            return;

        TelaDaEstatua tela = new GameObject("Estatua dos cristais").AddComponent<TelaDaEstatua>();
        tela.jogador = jogador;
        tela.Montar();
    }

    private void Montar()
    {
        atual = this;
        quadroQueAbriu = Time.frameCount;

        TelaSimples.Montar(gameObject, 40, new Color(0.02f, 0f, 0.05f, 0.85f));
        TelaSimples.Painel(transform, "Moldura", ArteDaInterface.MolduraGrande, -30f, new Vector2(1240f, 760f));
        TelaSimples.Faixa(transform, "Faixa", ArteDaInterface.FaixaRosa, 390f, 900f);
        TelaSimples.Titulo(transform, "Titulo", 96, new Color(1f, 0.95f, 0.85f), 390f, "Estátua dos cristais");
        cristais = TelaSimples.Texto(transform, "Cristais", 36, Roxo, 285f, "");

        menu = new MenuDeBotoes(transform, 0f, new Vector2(820f, 60f), 28) { IntervaloDaEntrada = 0.03f };
        float y = 200f;

        foreach (Melhorias.Dados d in Melhorias.Todas)
        {
            Melhoria qual = d.Qual;
            menu.Adicionar("", y, () => Comprar(qual));
            y -= 70f;
        }

        menu.Adicionar("Sair", y - 10f, Fechar, "[Esc] || [Pad B]");

        descricao = TelaSimples.Texto(transform, "Descricao", 26, new Color(1f, 0.97f, 0.92f), -300f, "");
        descricao.rectTransform.sizeDelta = new Vector2(1100f, 90f);
        descricao.horizontalOverflow = HorizontalWrapMode.Wrap;

        TelaSimples.LinhaDeTeclas(transform, "Teclas", -470f,
            "[W][S] escolher | [Enter] comprar | [Esc] sair || [Pad CruzCima][Pad CruzBaixo] escolher | [Pad A] comprar | [Pad B] sair",
            24, new Color(0.85f, 0.85f, 0.9f));

        Atualizar();
        Time.timeScale = 0f;
        TelaSimples.TravarJogador(jogador != null ? jogador.transform : null, true);
        Sons.Tocar(Som.MenuAbrir, 1f, 0f);
    }

    private void Update()
    {
        if (Time.frameCount == quadroQueAbriu)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) || Controle.Apertou(BotaoDoControle.B))
        {
            Fechar();
            return;
        }

        menu.Atualizar();

        if (atual == this)
            descricao.text = Descricao(menu.Escolhido);
    }

    private void Atualizar()
    {
        cristais.text = Idioma.F("Você tem {0} cristais", Melhorias.Cristais);

        for (int i = 0; i < Melhorias.Todas.Length; i++)
        {
            Melhorias.Dados d = Melhorias.Todas[i];
            int nivel = Melhorias.Nivel(d.Qual), maximo = d.Precos.Length;
            string preco = Melhorias.NoMaximo(d.Qual) ? Idioma.T("completa") : Idioma.F("{0} cristais", Melhorias.Preco(d.Qual));
            menu.TrocarRotulo(i, $"{Idioma.T(d.Nome)}  {nivel}/{maximo}  -  {preco}");
            menu.Apagar(i, Melhorias.NoMaximo(d.Qual) || Melhorias.Cristais < Melhorias.Preco(d.Qual));
        }
    }

    private string Descricao(int botao)
    {
        if (botao < 0 || botao >= Melhorias.Todas.Length)
            return Idioma.T("Os cristais caem dos campeões (os inimigos com aura) e dos chefes, e ficam guardados mesmo se você morrer.");

        Melhorias.Dados d = Melhorias.Todas[botao];
        string texto = Idioma.F("Cada nível: {0}.", Idioma.T(d.PorNivel));

        if (d.Qual == Melhoria.Arsenal)
        {
            int nivel = Melhorias.Nivel(Melhoria.Arsenal);
            texto += "\n" + (nivel < Melhorias.NoArsenal.Length ? Idioma.F("Próximo: {0}.", Idioma.T(Melhorias.NoArsenal[nivel]))
                                                               : Idioma.T("Todas as armas já saem dos baús."));
        }

        if (Partida.Diario)
            texto += "\n" + Idioma.T("(No desafio do dia as melhorias não valem: só a partir da próxima partida.)");

        return texto;
    }

    private void Comprar(Melhoria qual)
    {
        if (!Melhorias.Comprar(qual))
        {
            Sons.Tocar(Som.MenuNegado, 0.8f, 0f);
            return;
        }

        Sons.Tocar(Som.Compra, 1f, 0f);
        CameraDoJogo.Tremer(0.08f, 0.2f);

        // Ja vale nesta partida (menos no desafio do dia).
        if (jogador != null && Melhorias.Valendo)
        {
            if (qual == Melhoria.Vida && jogador.TryGetComponent(out Vida vida))
                vida.MudarMaxima(2f);
            else if (qual == Melhoria.Chaveiro && jogador.TryGetComponent(out Bolsa bolsa))
                bolsa.Ganhar(5, 1);
        }

        Atualizar();
    }

    private void Fechar()
    {
        if (atual != this)
            return;

        Sons.Tocar(Som.MenuFechar, 1f, 0f);
        quadroQueFechou = Time.frameCount;
        Time.timeScale = 1f;
        TelaSimples.TravarJogador(jogador != null ? jogador.transform : null, false);
        atual = null;
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
