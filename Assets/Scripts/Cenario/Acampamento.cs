using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O acampamento (1.21.0): a sala do comeco do andar 1, onde toda partida comeca, como a casa do Hades. Tem
/// a fogueira no meio, a estatua dos cristais (as melhorias que ficam pra sempre; no desafio do dia nao
/// tem) e tres pessoas pra conversar (<see cref="Falante"/>, <see cref="Falas"/>): Bento, o mercador; o
/// Mestre Ferraz, ferreiro (aqui ele nao forja, so conversa); e o Apostador (aqui sem apostas). Eles comentam
/// como foi a ultima partida e contam a historia aos poucos.
///
/// A sala e maior que a do comeco dos outros andares (<c>GeradorDoAndar.ClareiraDoAndar</c>): 15 x 11.
/// O jogador nasce no meio; as portas ficam no meio de cada lado, e nada fica na frente delas.
/// </summary>
public static class Acampamento
{
    public static void Montar(Transform pai, HashSet<Vector2Int> chao)
    {
        Vector2 fogo = new Vector2(0f, 2.2f);
        Fogueira.Criar(fogo, pai);
        Tirar(chao, fogo);

        if (!Partida.Diario)
        {
            Vector2 estatua = new Vector2(4.8f, 2.2f);
            EstatuaDosCristais.Criar(estatua, pai);
            Tirar(chao, estatua);
        }

        // Bento, o mercador (o mesmo das ruinas: acena quando o jogador chega perto).
        Vector2 doMercador = new Vector2(-4.8f, 2.2f);
        Mercador mercador = Mercador.Criar(doMercador, pai);

        if (mercador != null)
            Falante.Por(mercador.gameObject, QuemFala.Mercador);

        Tirar(chao, doMercador);

        // O Mestre Ferraz com a bigorna (a bigorna fica do lado direito dele).
        Vector2 doFerreiro = new Vector2(3.8f, -2.8f);
        Ferreiro ferreiro = Ferreiro.Criar(doFerreiro, pai);

        if (ferreiro != null)
            Falante.Por(ferreiro.SoConversa().gameObject, QuemFala.Ferreiro);

        Tirar(chao, doFerreiro);
        Tirar(chao, doFerreiro + Vector2.right * 1.3f);

        // O Apostador atras da mesinha de dados.
        Vector2 mesa = new Vector2(-4.6f, -3.4f);
        Apostador apostador = Apostador.Criar(mesa, pai);

        if (apostador != null)
        {
            apostador.Restantes = 0;
            Falante.Por(apostador.gameObject, QuemFala.Apostador);
        }

        Tirar(chao, mesa);
    }

    private static void Tirar(HashSet<Vector2Int> chao, Vector2 onde) => chao?.Remove(MapaDeCaminhos.Celula(onde));
}
