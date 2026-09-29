using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Esc pausa o jogo: congela tudo e mostra o andar, os itens pegos e as opcoes.
///
///   Esc  continuar      R  recomecar do andar 1      Q  voltar ao menu
///   M    musica         N  efeitos
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
    private Text opcoes;
    private Text resumo;

    public bool Pausado => tela != null;

    private void Awake()
    {
        andar = GetComponent<Andar>();
    }

    private void Update()
    {
        if (TelaDeInicio.Aberta || TelaDeFimDeJogo.Atual != null)
            return;

        if (!Pausado)
        {
            Opcoes.LerTeclas();

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
                Pausar();

            return;
        }

        bool mudou = Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.N);
        Opcoes.LerTeclas();

        if (mudou)
            Atualizar();

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            Continuar();
        else if (Input.GetKeyDown(KeyCode.R))
            TelaDeFimDeJogo.RecarregarCena();
        else if (Input.GetKeyDown(KeyCode.Q))
            TelaDeInicio.VoltarAoMenu();
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

        TelaSimples.Texto(tela.transform, "Titulo", 90, Color.white, 250f, "PAUSADO");
        resumo = TelaSimples.Texto(tela.transform, "Resumo", 30, new Color(0.85f, 0.85f, 0.85f), 90f, "");
        opcoes = TelaSimples.Texto(tela.transform, "Opcoes", 34, new Color(1f, 0.85f, 0.4f), -150f, "");
        TelaSimples.Painel(tela.transform, "Painel", ArteImportada.PainelCinza, -30f, new Vector2(1300f, 440f));
        Atualizar();

        Time.timeScale = 0f;
        TelaSimples.TravarJogador(andar.Jogador, true);
        Sons.Tocar(Som.Menu);
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
        resumo.text = $"{(andar.UltimoAndar ? "Ultimo andar" : $"Andar {andar.NumeroDoAndar}")} de {andar.AndarFinal}\n{Itens()}";
        opcoes.text =
            "Esc  continuar        R  recomecar        Q  menu\n" +
            $"M  musica: {(Opcoes.MusicaLigada ? "ligada" : "desligada")}        " +
            $"N  efeitos: {(Opcoes.EfeitosLigados ? "ligados" : "desligados")}";
    }

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
