using System.Collections;
using UnityEngine;

/// <summary>
/// Um bau fechado na caverna. Interagir abre (a animacao do bau do Old Prison) e ele solta uma arma
/// sorteada, diferente das que o jogador tem na mao, e as vezes uma caixa de municao. Aberto, fica
/// ali vazio. Monte com <see cref="Criar"/>.
/// </summary>
[DisallowMultipleComponent]
public class Bau : Interativo
{
    private const float QuadrosPorSegundo = 14f;

    private Sprite[] quadros;
    private SpriteRenderer desenho;
    private DadosDaArma[] armas;
    private Sprite caixaDeMunicao;
    private AudioClip somAbrindo;
    private AudioClip somDaMunicao;
    private bool aberto;

    public override bool Disponivel => !aberto;

    public override string Dica => "abrir o bau";

    public static Bau Criar(Sprite[] quadros, Vector2 onde, Transform pai, DadosDaArma[] armas,
                            Sprite caixaDeMunicao, AudioClip somAbrindo, AudioClip somDaMunicao)
    {
        GameObject obj = new GameObject("Bau");
        obj.transform.SetParent(pai, false);
        obj.transform.position = onde;

        // O desenho do pacote tem o bau na parte de baixo do quadro: sobe um pouco pra ele ficar no lugar.
        GameObject filho = new GameObject("Desenho");
        filho.transform.SetParent(obj.transform, false);
        filho.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        SpriteRenderer sprite = filho.AddComponent<SpriteRenderer>();
        sprite.sortingOrder = 10;

        if (quadros.Length > 0)
            sprite.sprite = quadros[0];

        // O bau conta como parede: ninguem atravessa, segura tiro e os inimigos dao a volta.
        obj.layer = Pedreiro.CamadaDaParede;
        BoxCollider2D colisor = obj.AddComponent<BoxCollider2D>();
        colisor.size = new Vector2(1.5f, 0.7f);

        Bau bau = obj.AddComponent<Bau>();
        bau.quadros = quadros;
        bau.desenho = sprite;
        bau.armas = armas;
        bau.caixaDeMunicao = caixaDeMunicao;
        bau.somAbrindo = somAbrindo;
        bau.somDaMunicao = somDaMunicao;
        return bau;
    }

    public override void Usar(GameObject jogador)
    {
        if (aberto)
            return;

        aberto = true;

        if (somAbrindo != null && jogador.TryGetComponent(out AudioSource audioSource))
            audioSource.PlayOneShot(somAbrindo, 0.7f);

        StartCoroutine(Abrir(jogador.GetComponent<ArmaDoJogador>()));
    }

    private IEnumerator Abrir(ArmaDoJogador doJogador)
    {
        for (int i = 0; i < quadros.Length; i++)
        {
            desenho.sprite = quadros[i];
            yield return new WaitForSeconds(1f / QuadrosPorSegundo);
        }

        Vector2 frente = (Vector2)transform.position + Vector2.down * 0.9f;
        DadosDaArma sorteada = Sortear(doJogador);

        if (sorteada != null)
            ArmaNoChao.Criar(new ArmaCarregada(sorteada), frente, transform.parent);

        if (caixaDeMunicao != null && Random.value < 0.4f)
            CaixaDeMunicao.Criar(caixaDeMunicao, frente + Vector2.right * 0.9f, transform.parent, somDaMunicao);
    }

    // Pelo peso de cada uma, sem repetir as que o jogador ja tem (se sobrar alguma).
    private DadosDaArma Sortear(ArmaDoJogador doJogador)
    {
        float total = 0f;

        foreach (DadosDaArma a in armas)
            if (Serve(a, doJogador, true))
                total += a.pesoNoBau;

        bool semRepetir = total > 0f;

        if (!semRepetir)
        {
            foreach (DadosDaArma a in armas)
                if (Serve(a, doJogador, false))
                    total += a.pesoNoBau;
        }

        float ponto = Random.value * total;

        foreach (DadosDaArma a in armas)
        {
            if (!Serve(a, doJogador, semRepetir))
                continue;

            ponto -= a.pesoNoBau;

            if (ponto <= 0f)
                return a;
        }

        return null;
    }

    private static bool Serve(DadosDaArma a, ArmaDoJogador doJogador, bool semRepetir)
    {
        if (a == null || a.pesoNoBau <= 0f || a.desenhoNaMao == null)
            return false;

        if (!semRepetir || doJogador == null)
            return true;

        return (doJogador.Atual == null || doJogador.Atual.Dados != a) && (doJogador.Outra == null || doJogador.Outra.Dados != a);
    }
}
