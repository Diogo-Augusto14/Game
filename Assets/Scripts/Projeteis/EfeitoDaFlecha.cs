using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O que uma flecha especial faz alem do dano: explodir, gelar, envenenar, quicar na
/// parede e mostrar o proprio impacto. Fica na <see cref="Lagrima"/>, que chama
/// <see cref="AoAcertar"/>, <see cref="Ricochetear"/> e <see cref="AoEstourar"/>.
/// </summary>
[DisallowMultipleComponent]
public class EfeitoDaFlecha : MonoBehaviour
{
    private DefinicaoDeFlecha definicao;
    private float dano;
    private GameObject dono;
    private int ricochetesQueSobram;
    private Rigidbody2D rb;
    private VisualDoProjetil visual;

    public DefinicaoDeFlecha Definicao => definicao;

    /// <summary>Liga os efeitos desta flecha. <paramref name="danoDaFlecha"/> ja com os itens.</summary>
    public void Configurar(DefinicaoDeFlecha nova, float danoDaFlecha, GameObject quemAtirou)
    {
        definicao = nova;
        dano = danoDaFlecha;
        dono = quemAtirou;
        ricochetesQueSobram = nova.ricochetes;
        rb = GetComponent<Rigidbody2D>();
        visual = GetComponent<VisualDoProjetil>();
    }

    /// <summary>A flecha acabou de machucar este alvo (inimigo, pedra que apanha, alvo de treino).</summary>
    public void AoAcertar(IDanificavel alvo)
    {
        if (definicao == null || !(alvo is Component componente))
            return;

        InimigoDeSala inimigo = componente.GetComponentInParent<InimigoDeSala>();

        if (inimigo == null || inimigo.EstaMorto)
            return;

        if (definicao.tempoDeLentidao > 0f)
            CondicaoDoInimigo.Em(inimigo).Gelar(definicao.lentidao, definicao.tempoDeLentidao);

        if (definicao.mordidasDeVeneno > 0)
            CondicaoDoInimigo.Em(inimigo).Envenenar(definicao.mordidasDeVeneno, dano * definicao.danoDoVeneno,
                                                    definicao.intervaloDoVeneno, dono);
    }

    /// <summary>
    /// Bateu em parede: se ainda pode quicar, espelha a velocidade na parede e devolve true
    /// (a flecha continua voando). Sem ricochete sobrando, devolve false e ela quebra.
    /// </summary>
    public bool Ricochetear(Collider2D parede)
    {
        if (definicao == null || ricochetesQueSobram <= 0 || rb == null)
            return false;

        Vector2 v = rb.linearVelocity;

        if (v.sqrMagnitude < 0.0001f)
            return false;

        Vector2 normal = NormalDaParede(parede, v);
        rb.linearVelocity = Vector2.Reflect(v, normal);
        ricochetesQueSobram--;

        // Um passinho pra fora da parede, pra nao ficar raspando nela.
        rb.position += normal * 0.05f;

        EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Poeira, transform.position, definicao.aparencia != null
            ? definicao.aparencia.corDoImpacto : Color.white, 0.35f);
        Sons.Tocar(Som.Pancada, 0.25f, 0.15f);
        return true;
    }

    private Vector2 NormalDaParede(Collider2D parede, Vector2 v)
    {
        // Volta um pouco e mira na parede: o raio diz de que lado ela foi atingida.
        Vector2 rumo = v.normalized;
        Vector2 de = rb.position - rumo * 0.6f;
        RaycastHit2D[] batidas = Physics2D.RaycastAll(de, rumo, 1.2f, Camadas.MascaraDeParede);

        foreach (RaycastHit2D b in batidas)
            if (b.collider == parede && b.normal.sqrMagnitude > 0.5f)
                return b.normal;

        // Sem raio (quina, pedra redonda): pelo ponto mais perto do colisor.
        Vector2 perto = parede.ClosestPoint(rb.position);
        Vector2 fora = rb.position - perto;

        if (fora.sqrMagnitude > 0.0001f)
            return Mathf.Abs(fora.x) > Mathf.Abs(fora.y) ? new Vector2(Mathf.Sign(fora.x), 0f) : new Vector2(0f, Mathf.Sign(fora.y));

        return -rumo;
    }

    /// <summary>A flecha quebrou (bateu, caiu no fim do alcance): explosao e efeito de impacto.</summary>
    public void AoEstourar()
    {
        if (definicao == null)
            return;

        if (definicao.raioDaExplosao > 0f)
            Explodir(transform.position);
        else if (visual != null)
            visual.MostrarImpacto();
    }

    /// <summary>
    /// Machuca so inimigos (nunca o jogador, a pedra ou a porta secreta: isso e da bomba).
    /// Ignora a invencibilidade pos-golpe, senao o inimigo que levou a flecha nao sentia a
    /// explosao dela.
    /// </summary>
    private void Explodir(Vector2 centro)
    {
        float raio = definicao.raioDaExplosao;
        HashSet<InimigoDeSala> atingidos = new HashSet<InimigoDeSala>();

        foreach (Collider2D c in Physics2D.OverlapCircleAll(centro, raio))
        {
            InimigoDeSala inimigo = c.GetComponentInParent<InimigoDeSala>();

            if (inimigo == null || inimigo.EstaMorto || !atingidos.Add(inimigo))
                continue;

            Vida vida = inimigo.GetComponent<Vida>();

            if (vida == null || vida.EstaMorto)
                continue;

            Vector2 direcao = (Vector2)inimigo.transform.position - centro;
            vida.TomarDano(new DanoInfo(dano * definicao.danoDaExplosao, direcao, 3f, centro, dono,
                                        PesoDoGolpe.Leve, true));
        }

        Sons.Tocar(Som.Explosao, 0.45f, 0.12f);
        EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Explosao, centro, Color.white, raio * 2.2f);
    }
}
