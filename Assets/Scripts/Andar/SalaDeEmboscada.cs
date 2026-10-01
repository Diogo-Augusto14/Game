using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Emboscada: a sala parece vazia. Entrou, as portas batem e os inimigos chegam em ondas,
/// saindo de nuvens de poeira pelos cantos (uma marca no chao avisa onde cada um vai nascer).
/// So abre quando a ultima onda cai.
///
/// Quem monta e o <see cref="Andar"/>: no lugar de povoar a sala, passa os bandos de cada
/// onda. A sala fica com <see cref="Sala.SegurarPortas"/> ligado desde o comeco, entao entrar
/// nela vazia fecha as portas (<see cref="Sala.AoEsvaziar"/>) em vez de dar a sala por limpa.
/// </summary>
[DisallowMultipleComponent]
public class SalaDeEmboscada : MonoBehaviour
{
    private const float LongeDoJogador = 3f;
    private const float AvisoDoNascimento = 0.8f;
    private const float PausaEntreOndas = 0.9f;

    private Sala sala;
    private List<List<TipoDeInimigo>> ondas;
    private Action<InimigoDeSala> fortalecer;
    private int ondaAtual;
    private bool comecou;

    public static SalaDeEmboscada Montar(Sala sala, List<List<TipoDeInimigo>> ondas, Action<InimigoDeSala> fortalecer)
    {
        SalaDeEmboscada e = sala.gameObject.AddComponent<SalaDeEmboscada>();
        e.sala = sala;
        e.ondas = ondas;
        e.fortalecer = fortalecer;
        sala.SegurarPortas = true;
        sala.AoEsvaziar.AddListener(e.Esvaziou);
        return e;
    }

    private void Esvaziou()
    {
        if (!comecou)
        {
            comecou = true;
            AvisoDoAndar.Mostrar("Emboscada!");
            Sons.Tocar(Som.Rugido);
            Impacto.Tremer(0.15f, 0.3f);
            StartCoroutine(ProximaOnda(0.5f));
            return;
        }

        StartCoroutine(ProximaOnda(PausaEntreOndas));
    }

    private IEnumerator ProximaOnda(float espera)
    {
        yield return new WaitForSeconds(espera);

        if (ondaAtual >= ondas.Count)
        {
            sala.AoEsvaziar.RemoveListener(Esvaziou);
            sala.Liberar();
            Conquistas.Conquistar(Conquistas.NinguemMePega);
            yield break;
        }

        List<TipoDeInimigo> onda = ondas[ondaAtual++];
        List<Vector2> pontos = new List<Vector2>();

        // Marca no chao onde cada um vai sair: da tempo de sair de perto.
        foreach (TipoDeInimigo _ in onda)
        {
            Vector2 ponto = PontoLongeDoJogador();
            pontos.Add(ponto);
            EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Poeira, (Vector2)sala.transform.position + ponto, new Color(0.6f, 0.55f, 0.5f), 1.2f);
        }

        yield return new WaitForSeconds(AvisoDoNascimento);

        int criados = 0;

        for (int i = 0; i < onda.Count; i++)
        {
            InimigoDeSala inimigo = sala.CriarInimigo(onda[i], pontos[i]);

            if (inimigo == null)
                continue;

            criados++;
            fortalecer?.Invoke(inimigo);
            inimigo.Acordar();
        }

        Sons.Tocar(Som.PortaFecha, 0.6f);

        // Nada nasceu (sem arte): nao prende o jogador.
        if (criados == 0 && sala.InimigosVivos == 0)
            Esvaziou();
    }

    private Vector2 PontoLongeDoJogador()
    {
        Vector2 centro = sala.transform.position;
        Transform jogador = Andar.Atual != null ? Andar.Atual.Jogador : null;
        Vector2 ponto = sala.PontoLivreAleatorio();

        for (int tentativa = 0; jogador != null && tentativa < 20; tentativa++)
        {
            if (Vector2.Distance(centro + ponto, jogador.position) >= LongeDoJogador)
                break;

            ponto = sala.PontoLivreAleatorio();
        }

        return ponto;
    }
}
