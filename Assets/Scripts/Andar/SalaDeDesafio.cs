using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// A sala de desafio do Isaac: um pedestal com item no meio, de graca... ate pegar. Pegou,
/// as portas batem e chegam ondas de inimigos, uma atras da outra. As portas so abrem
/// quando a ultima onda morre (e ainda cai um premio no chao).
///
/// Quem monta e o <see cref="Andar"/>, com <see cref="Montar"/>. A sala usa
/// <see cref="Sala.SegurarPortas"/> pra nao abrir entre uma onda e outra.
/// </summary>
[DisallowMultipleComponent]
public class SalaDeDesafio : MonoBehaviour
{
    /// <summary>Inimigo nao nasce mais perto que isto do jogador.</summary>
    private const float LONGE_DO_JOGADOR = 3f;

    /// <summary>Pausa entre uma onda limpa e a proxima, pra dar pra respirar.</summary>
    private const float PAUSA_ENTRE_ONDAS = 1f;

    private Sala sala;
    private int ondas;
    private int inimigosPorOnda;
    private Func<Sala, Vector2, InimigoDeSala> criarInimigo;
    private int ondaAtual;
    private bool comecou;

    /// <summary>Todas as ondas morreram: as portas abriram.</summary>
    public bool Vencido { get; private set; }

    /// <param name="criarInimigo">Cria um inimigo do andar na sala, no ponto dado (relativo ao centro).</param>
    public static SalaDeDesafio Montar(Sala sala, ItemPassivo premio, int ondas, int inimigosPorOnda,
                                       Func<Sala, Vector2, InimigoDeSala> criarInimigo)
    {
        SalaDeDesafio desafio = sala.gameObject.AddComponent<SalaDeDesafio>();
        desafio.sala = sala;
        desafio.ondas = Mathf.Max(1, ondas);
        desafio.inimigosPorOnda = Mathf.Max(1, inimigosPorOnda);
        desafio.criarInimigo = criarInimigo;

        Vector2 centro = sala.transform.position;
        Pedestal pedestal = Pedestal.Criar(premio, centro, sala.transform);
        pedestal.AoPegar += _ => desafio.Comecar();

        // Dois estandartes de guerra do Old Prison ao lado do pedestal: aqui tem briga.
        Sprite estandarte = ArteImportada.PecaDaPrisao("Estandarte");

        if (estandarte != null)
        {
            for (int lado = -1; lado <= 1; lado += 2)
                FormasDaSala.Desenho(sala.transform, "Estandarte", estandarte, Color.white, new Vector2(lado * 1.6f, -0.9f), Vector2.one, 3);
        }

        return desafio;
    }

    private void Comecar()
    {
        if (comecou)
            return;

        comecou = true;
        sala.SegurarPortas = true;
        sala.AoEsvaziar.AddListener(OndaLimpa);
        sala.Fechar();

        Musica.Tocar(TemaMusical.Chefe);
        AvisoDoAndar.Mostrar("Desafio!");
        StartCoroutine(ProximaOnda(0.8f));
    }

    private void OndaLimpa() => StartCoroutine(ProximaOnda(PAUSA_ENTRE_ONDAS));

    private IEnumerator ProximaOnda(float espera)
    {
        yield return new WaitForSeconds(espera);

        if (ondaAtual >= ondas)
        {
            Vencer();
            yield break;
        }

        ondaAtual++;
        AvisoDoAndar.Mostrar($"Onda {ondaAtual} de {ondas}");
        Sons.Tocar(Som.Rugido);

        int criados = 0;

        for (int i = 0; i < inimigosPorOnda; i++)
            if (criarInimigo?.Invoke(sala, PontoLongeDoJogador()) != null)
                criados++;

        // Nenhum nasceu (fabrica sem arte, por exemplo): nao trava a sala fechada.
        if (criados == 0 && sala.InimigosVivos == 0)
            OndaLimpa();
    }

    private void Vencer()
    {
        Vencido = true;
        sala.AoEsvaziar.RemoveListener(OndaLimpa);
        sala.Liberar();

        Andar andar = Andar.Atual;
        Musica.Tocar(andar != null ? andar.MusicaDoAndar : Musica.DoAndar(1));
        AvisoDoAndar.Mostrar("Desafio vencido!");
        Sons.Tocar(Som.Vitoria);

        Vector2 centro = sala.transform.position;
        Coletavel.Criar(TabelaDeDrops.Sortear(), centro + new Vector2(-0.8f, -1.5f), sala.transform);
        Coletavel.Criar(TabelaDeDrops.Sortear(), centro + new Vector2(0.8f, -1.5f), sala.transform);
    }

    /// <summary>Ponto livre da sala, longe do jogador, pra ninguem nascer em cima dele.</summary>
    private Vector2 PontoLongeDoJogador()
    {
        Vector2 centro = sala.transform.position;
        Transform jogador = Andar.Atual != null ? Andar.Atual.Jogador : null;
        Vector2 ponto = sala.PontoLivreAleatorio();

        for (int tentativa = 0; jogador != null && tentativa < 20; tentativa++)
        {
            if (Vector2.Distance(centro + ponto, jogador.position) >= LONGE_DO_JOGADOR)
                break;

            ponto = sala.PontoLivreAleatorio();
        }

        return ponto;
    }
}
