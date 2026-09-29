using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Esc pausa o jogo: congela tudo e mostra o andar, os itens pegos e as opcoes.
///
///   Esc  continuar      R  recomecar do andar 1      Q  voltar ao menu
///   M    musica         N  efeitos          O  opcoes (volumes, tela)
///
/// No controle: Start (ou B) continua, Select recomeca, Y vai ao menu, LB e RB o som.
///
/// Fica no mesmo objeto do <see cref="Andar"/> (ele poe sozinho). Nao abre por cima do
/// menu inicial nem da tela de fim de jogo. M e N tambem funcionam jogando, sem pausar.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Andar))]
public class TelaDePausa : MonoBehaviour
{
    private Andar andar;
    private GameObject tela;
    private RectTransform opcoes;
    private Text resumo;

    public bool Pausado => tela != null;

    private void Awake()
    {
        andar = GetComponent<Andar>();
    }

    private void Update()
    {
        if (TelaDeInicio.Aberta || TelaDeFimDeJogo.Atual != null || TelaDeOpcoes.Ocupada)
            return;

        if (!Pausado)
        {
            Opcoes.LerTeclas();

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) || Controle.Apertou(BotaoDoControle.Start))
                Pausar();

            return;
        }

        bool mudou = Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.N)
                     || Controle.Apertou(BotaoDoControle.LB) || Controle.Apertou(BotaoDoControle.RB);
        Opcoes.LerTeclas();

        if (mudou)
            Atualizar();

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)
            || Controle.Apertou(BotaoDoControle.Start) || Controle.Apertou(BotaoDoControle.B))
        {
            Continuar();
            Sons.Tocar(Som.MenuFechar, 1f, 0f);
        }
        else if (Input.GetKeyDown(KeyCode.R) || Controle.Apertou(BotaoDoControle.Select))
        {
            Sons.Tocar(Som.MenuConfirmar, 1f, 0f);
            TelaDeFimDeJogo.RecarregarCena();
        }
        else if (Input.GetKeyDown(KeyCode.O) || Controle.Apertou(BotaoDoControle.X))
        {
            TelaDeOpcoes.Abrir(Atualizar);
        }
        else if (Input.GetKeyDown(KeyCode.Q) || Controle.Apertou(BotaoDoControle.Y))
        {
            Sons.Tocar(Som.MenuFechar, 1f, 0f);
            TelaDeInicio.VoltarAoMenu();
        }
    }

    private void OnDisable()
    {
        if (Pausado)
            Continuar();
    }

    public void Pausar()
    {
        tela = new GameObject("Pausa");
        TelaSimples.Montar(tela, 30, new Color(0f, 0f, 0f, 0.75f));

        TelaSimples.Texto(tela.transform, "Titulo", 80, new Color(1f, 0.95f, 0.85f), 290f, "PAUSADO");
        resumo = TelaSimples.Texto(tela.transform, "Resumo", 30, Color.white, 90f, "");
        TelaSimples.LinhaDeTeclas(tela.transform, "Teclas", -45f, "[Esc] continuar | [R] recomecar | [O] opcoes | [Q] menu || [Pad Start] continuar | [Pad Select] recomecar | [Pad X] opcoes | [Pad Y] menu", 32,
            new Color(1f, 0.85f, 0.4f));
        opcoes = TelaSimples.LinhaDeTeclas(tela.transform, "Opcoes", -110f, "", 30, new Color(1f, 0.85f, 0.4f));

        // Moldura do Dragon Regalia no meio e a faixa rosa atras do titulo.
        TelaSimples.Painel(tela.transform, "Painel", ArteDaInterface.MolduraGrande, 0f, new Vector2(1300f, 420f));
        TelaSimples.Faixa(tela.transform, "Faixa", ArteDaInterface.FaixaRosa, 290f, 760f);
        Atualizar();

        Time.timeScale = 0f;
        TelaSimples.TravarJogador(andar.Jogador, true);
        Sons.Tocar(Som.MenuAbrir, 1f, 0f);
    }

    public void Continuar()
    {
        if (tela != null)
            Destroy(tela);

        tela = null;
        Time.timeScale = 1f;
        TelaSimples.TravarJogador(andar.Jogador, false);
    }

    private void Atualizar()
    {
        if (tela == null)
            return;

        resumo.text = $"{(andar.UltimoAndar ? "Ultimo andar" : $"Andar {andar.NumeroDoAndar}")} de {andar.AndarFinal}: {andar.Tema.Nome}\n{Itens()}";
        TelaSimples.TrocarLinhaDeTeclas(opcoes,
            $"[M] musica: {Musica()} | [N] efeitos: {Efeitos()} || [Pad LB] musica: {Musica()} | [Pad RB] efeitos: {Efeitos()}",
            30, new Color(1f, 0.85f, 0.4f));
    }

    private static string Musica() => Opcoes.MusicaLigada ? "ligada" : "desligada";

    private static string Efeitos() => Opcoes.EfeitosLigados ? "ligados" : "desligados";

    private string Itens()
    {
        EstatisticasDoJogador estatisticas = andar.Jogador != null ? andar.Jogador.GetComponent<EstatisticasDoJogador>() : null;

        if (estatisticas == null || estatisticas.Itens.Count == 0)
            return "<size=24>Nenhum item ainda</size>";

        List<string> nomes = new List<string>();

        foreach (ItemPassivo item in estatisticas.Itens)
            nomes.Add($"<color=#{ColorUtility.ToHtmlStringRGB(item.cor)}>{item.nome}</color>");

        return "<size=24>" + string.Join("   ", nomes) + "</size>";
    }
}
