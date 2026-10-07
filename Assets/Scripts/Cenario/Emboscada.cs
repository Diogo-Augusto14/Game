using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Uma emboscada (veio das salas especiais do jogo antigo): um monte de caveiras no chao; chegando
/// perto, ondas de inimigos aparecem em volta. Vencidas as ondas, sai um bau. O desafio e a mesma
/// coisa, mas o jogador escolhe comecar (E na placa) e as ondas sao mais; o premio e um item.
/// </summary>
public class Emboscada : Interativo
{
    private GeradorDoAndar gerador;
    private bool desafio;
    private bool comecou;
    private int ondas;
    private Sprite[] quadrosDoBau;

    public override bool Disponivel => desafio && !comecou;

    public override string Dica => "aceitar o desafio (3 ondas de inimigos, prêmio: item)";

    public static Emboscada Criar(Vector2 onde, Transform pai, GeradorDoAndar gerador, bool desafio, Sprite[] quadrosDoBau)
    {
        GameObject obj = new GameObject(desafio ? "Desafio" : "Emboscada");
        obj.transform.SetParent(pai, false);
        obj.transform.position = onde;

        // Caveiras no chao (a emboscada) ou o estandarte (o desafio).
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = ArteDoAntigo.Peca(desafio ? "Estandarte" : "Caveiras" + Random.Range(1, 10));
        sr.sortingOrder = desafio ? 9 : Pedreiro.OrdemDosEnfeites + 2;

        Emboscada e = obj.AddComponent<Emboscada>();
        e.gerador = gerador;
        e.desafio = desafio;
        e.ondas = desafio ? 3 : 2;
        e.quadrosDoBau = quadrosDoBau;
        return e;
    }

    public override void Usar(GameObject jogador)
    {
        if (desafio && !comecou)
            StartCoroutine(Lutar());
    }

    private void Update()
    {
        if (desafio || comecou)
            return;

        GameObject jogador = GameObject.FindWithTag("Player");

        if (jogador != null && Vector2.Distance(jogador.transform.position, transform.position) < 3f)
            StartCoroutine(Lutar());
    }

    private IEnumerator Lutar()
    {
        comecou = true;
        GameObject jogador = GameObject.FindWithTag("Player");
        Sons.Tocar(Som.Rugido, 0.6f);
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.6f, desafio ? "Desafio!" : "Emboscada!", new Color(1f, 0.45f, 0.35f), 1.5f).Pular(1.8f);

        for (int onda = 0; onda < ondas; onda++)
        {
            List<Vida> desta = new List<Vida>();
            int quantos = 3 + gerador.Andar / 3 + onda;
            Vector2 centro = jogador != null ? (Vector2)jogador.transform.position : (Vector2)transform.position;

            for (int i = 0; i < quantos; i++)
            {
                Vector2 onde = centro + (Vector2)(Quaternion.Euler(0f, 0f, 360f * i / quantos + Random.Range(-15f, 15f)) * Vector2.right) * Random.Range(3.5f, 5.5f);

                if (MapaDeCaminhos.Atual != null && !MapaDeCaminhos.Atual.TemChao(onde))
                    onde = (Vector2)transform.position + Random.insideUnitCircle * 1.5f;

                Vida novo = gerador.CriarInimigo(onde);

                if (novo != null)
                    desta.Add(novo);
            }

            yield return new WaitForSeconds(1f);

            while (desta.Exists(v => v != null && !v.Morto))
                yield return new WaitForSeconds(0.3f);

            yield return new WaitForSeconds(0.6f);
        }

        Sons.Tocar(Som.Segredo, 0.8f, 0f);

        if (desafio)
        {
            GameObject j = GameObject.FindWithTag("Player");
            Pedestal.Criar((Vector2)transform.position + Vector2.down * 1.2f, transform.parent,
                           CatalogoDeItens.Sortear(j != null ? j.GetComponent<EstatisticasDoJogador>() : null));

            for (int i = 0; i < 4; i++)
                Coletavel.Criar(TipoDeColetavel.Moeda, (Vector2)transform.position + Vector2.right);
        }
        else if (quadrosDoBau != null && quadrosDoBau.Length > 0)
        {
            Bau.Criar(quadrosDoBau, transform.position, transform.parent, gerador.ArmasDoBau, null, null, null);
        }
    }
}
