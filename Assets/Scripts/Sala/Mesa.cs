using System.Collections;
using UnityEngine;

/// <summary>
/// A mesa interativa do Old Prison: um obstaculo que a briga gasta. De pe, bloqueia andar e
/// tiro como uma pedra. O primeiro tiro (do heroi ou de um bicho) derruba a mesa de lado, pra
/// longe de quem atirou, e ela vira uma barricada fina; cada tiro depois racha mais um pouco
/// (os tres quadros de quebra da arte) ate ela virar lasca e sumir. Bomba quebra de uma vez.
///
/// O tiro nao procura alvo em coisa da camada de parede: <see cref="Lagrima"/> e
/// <see cref="TiroDaSala"/> chamam <see cref="Acertar"/> quando batem nela.
/// Monte por <see cref="Criar"/>.
/// </summary>
[DisallowMultipleComponent]
public class Mesa : MonoBehaviour
{
    /// <summary>Quadros da arte: 0 a 6 tombando, 7 a 9 quebrando.</summary>
    private const int UltimoTombando = 6;
    private const int PrimeiroQuebrado = 7;

    /// <summary>Tiros que a mesa deitada aguenta antes de sumir (um por quadro de quebra, mais o final).</summary>
    private const int TirosDeitada = 4;

    private SpriteRenderer desenho;
    private BoxCollider2D caixa;
    private Sprite[] quadros;
    private bool deitada;
    private bool tombando;
    private bool quebrada;
    private int tirosLevados;

    public static Mesa Criar(Transform pai, Vector2 posicaoLocal)
    {
        Sprite[] quadros = ArteImportada.MesaQueVira;

        if (quadros == null || quadros.Length <= PrimeiroQuebrado + 2)
            return null;

        SpriteRenderer sr = FormasDaSala.Desenho(pai, "Mesa", quadros[0], Color.white, posicaoLocal, Vector2.one, -1);
        GameObject obj = sr.gameObject;
        obj.layer = Sala.CamadaDeParede;

        // De pe: o tampo inteiro (0.8 x 1.5), mas so a casa dela bloqueia (o resto e desenho).
        BoxCollider2D caixa = obj.AddComponent<BoxCollider2D>();
        caixa.size = new Vector2(0.8f, 1f);

        // Vida so pra bomba achar (como a pedra); quem quebra de verdade e o Quebrar.
        Vida vida = obj.AddComponent<Vida>();
        vida.Configurar(1f, 0f, false, 0f, false);

        Mesa mesa = obj.AddComponent<Mesa>();
        mesa.desenho = sr;
        mesa.caixa = caixa;
        mesa.quadros = quadros;
        vida.AoMorrer.AddListener(mesa.Quebrar);
        return mesa;
    }

    /// <summary>Um tiro bateu na mesa, vindo de <paramref name="deOnde"/>.</summary>
    public void Acertar(Vector2 deOnde)
    {
        if (quebrada || tombando)
            return;

        if (!deitada)
        {
            StartCoroutine(Tombar(deOnde.x > transform.position.x));
            return;
        }

        tirosLevados++;
        StartCoroutine(Tremer());

        if (tirosLevados >= TirosDeitada)
        {
            Quebrar();
            return;
        }

        desenho.sprite = quadros[PrimeiroQuebrado + Mathf.Min(tirosLevados - 1, quadros.Length - 1 - PrimeiroQuebrado)];
        Sons.Tocar(Som.Pancada, 0.5f);
    }

    /// <summary>Vira de lado, pra longe do tiro. A arte tomba pra direita; vindo da direita, espelha.</summary>
    private IEnumerator Tombar(bool praEsquerda)
    {
        tombando = true;
        desenho.flipX = praEsquerda;
        Sons.Tocar(Som.Pancada, 0.8f);

        for (int i = 1; i <= UltimoTombando; i++)
        {
            desenho.sprite = quadros[i];
            yield return new WaitForSeconds(0.05f);
        }

        // Deitada: a tabua em pe, fina e comprida (16 x 91 px a 48 por unidade), deslocada pro lado em que caiu.
        float lado = praEsquerda ? -1f : 1f;
        caixa.size = new Vector2(0.34f, 1.6f);
        caixa.offset = new Vector2(lado * 0.51f, 0.15f);
        deitada = true;
        tombando = false;
        Impacto.Tremer(0.08f, 0.12f);
    }

    private IEnumerator Tremer()
    {
        Transform t = desenho.transform;
        Vector3 inicio = t.localPosition;

        for (float tempo = 0f; tempo < 0.12f; tempo += Time.deltaTime)
        {
            t.localPosition = inicio + (Vector3)(Random.insideUnitCircle * 0.04f);
            yield return null;
        }

        t.localPosition = inicio;
    }

    /// <summary>Vira lasca: poeira, barulho e some (a casa fica livre).</summary>
    private void Quebrar()
    {
        if (quebrada)
            return;

        quebrada = true;
        Sons.Tocar(Som.Pancada);
        Vector2 meio = (Vector2)transform.position + (deitada ? caixa.offset : Vector2.zero);
        EfeitoDeQuadros.Criar(ArteImportada.Poeira(48f), 20f, meio, 12);
        Destroy(gameObject);
    }
}
