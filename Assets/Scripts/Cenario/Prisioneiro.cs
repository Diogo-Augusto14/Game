using System.Collections;
using UnityEngine;

/// <summary>
/// O prisioneiro preso na cela (<see cref="TipoDeSala.Cela"/>): o mercador das ruinas, apagado atras do
/// portao da cela do Old Prison. Com uma chave (interagir), o portao sobe, ele agradece com um cristal e
/// abre uma loja ali mesmo, 40% mais barata (<see cref="Loja"/>). Sem chave, fica esperando: da pra
/// voltar depois. Monte com <see cref="Criar"/>.
/// </summary>
public class Prisioneiro : Interativo
{
    private const float QuadrosPorSegundo = 16f;

    // O portao fechado tem o pe 69 px abaixo do meio do quadro (quadros de 128 x 160, 32 px por unidade).
    private const float PeDoPortao = 69f / 32f;

    private Sprite[] portao;
    private SpriteRenderer desenhoDoPortao;
    private SpriteRenderer desenhoDoPreso;
    private Collider2D grade;
    private Vector2 pe;
    private bool solto;

    public override float Alcance => 2.4f;

    public override bool Disponivel => !solto;

    public override string Dica => "soltar o prisioneiro (1 chave)";

    /// <param name="pe">O pe do portao (a loja abre embaixo dele).</param>
    public static Prisioneiro Criar(Vector2 pe, Transform pai)
    {
        Sprite[] portao = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Masmorra/Prisao/PortaoDaCela"), new Vector2Int(128, 160), 32f);

        if (portao.Length == 0)
            return null;

        GameObject obj = new GameObject("Prisioneiro");
        obj.transform.SetParent(pai, false);
        obj.transform.position = pe;

        Prisioneiro p = obj.AddComponent<Prisioneiro>();
        p.pe = pe;
        p.portao = portao;

        // O preso, um pouco atras do portao, no escuro e meio cabisbaixo (sem acenar ate sair).
        Mercador mercador = Mercador.Criar(pe + Vector2.up * 0.35f, obj.transform);

        if (mercador != null)
        {
            p.desenhoDoPreso = mercador.GetComponent<SpriteRenderer>();
            p.desenhoDoPreso.color = new Color(0.55f, 0.55f, 0.65f);
            mercador.enabled = false;
        }

        GameObject g = new GameObject("Portao da cela");
        g.transform.SetParent(obj.transform, false);
        g.transform.position = pe + Vector2.up * PeDoPortao;
        p.desenhoDoPortao = g.AddComponent<SpriteRenderer>();
        p.desenhoDoPortao.sprite = portao[0];
        p.desenhoDoPortao.sortingOrder = 12;

        // As grades seguram tiro e gente ate abrir.
        obj.layer = Pedreiro.CamadaDaParede;
        BoxCollider2D c = obj.AddComponent<BoxCollider2D>();
        c.size = new Vector2(2.2f, 0.5f);
        c.offset = new Vector2(0f, 0.3f);
        p.grade = c;

        // Velas dos lados da cela.
        Vela.Criar(pe + new Vector2(-1.8f, -0.2f), pai);
        Vela.Criar(pe + new Vector2(1.8f, -0.2f), pai);
        return p;
    }

    public override void Usar(GameObject jogador)
    {
        if (solto)
            return;

        if (!jogador.TryGetComponent(out Bolsa bolsa) || !bolsa.Gastar(0, 1))
        {
            Sons.Tocar(Som.Negado, 0.7f, 0f);
            TextoFlutuante.Mostrar(transform.position + Vector3.up * 2.6f, "Precisa de uma chave", new Color(1f, 0.6f, 0.55f), 1f);
            return;
        }

        solto = true;
        Conquistas.Contar("prisioneiros", 5, Conquistas.Libertador);
        Sons.Tocar(Som.Destranca, 0.9f, 0f);
        StartCoroutine(Abrir(jogador.GetComponent<EstatisticasDoJogador>()));
    }

    private IEnumerator Abrir(EstatisticasDoJogador itens)
    {
        Sons.Tocar(Som.PortaoDaCela, 0.9f);

        for (int i = 0; i < portao.Length; i++)
        {
            desenhoDoPortao.sprite = portao[i];
            yield return new WaitForSeconds(1f / QuadrosPorSegundo);
        }

        grade.enabled = false;

        // Sai do escuro, acena e agradece: um cristal e a loja barata ali na frente.
        if (desenhoDoPreso != null)
        {
            desenhoDoPreso.color = Color.white;

            if (desenhoDoPreso.TryGetComponent(out Mercador mercador))
                mercador.enabled = true;
        }

        TextoFlutuante.Mostrar(transform.position + Vector3.up * 2.4f, "Obrigado! Pra você, mais barato.", new Color(1f, 0.9f, 0.55f), 2.5f);
        Sons.Tocar(Som.Segredo, 0.8f);
        Coletavel.Criar(TipoDeColetavel.Cristal, pe + Vector2.down * 1.2f, Partida.CristaisVezes);
        Loja.Criar(pe + Vector2.down * 2.4f, transform.parent, itens, false, 0.6f, false);
    }
}
