using UnityEngine;

/// <summary>
/// Gelo e veneno num inimigo, vindos das flechas especiais (<see cref="EfeitoDaFlecha"/>).
///
/// Gelado: anda mais devagar (<see cref="InimigoDeSala.MultiplicadorDeVelocidade"/>) e fica
/// com um cristal girando em cima. Envenenado: perde vida a cada mordida e solta fumaca
/// verde. Os desenhos ficam num filho, entao o piscar de dano do Vida nao apaga nada.
/// Uma flecha nova renova o efeito em vez de somar.
/// </summary>
[DisallowMultipleComponent]
public class CondicaoDoInimigo : MonoBehaviour
{
    private InimigoDeSala inimigo;
    private Vida vida;

    private float fimDoGelo;
    private float lentidao = 1f;
    private EfeitoDeQuadros cristal;

    private int mordidas;
    private float danoDaMordida;
    private float intervaloDoVeneno;
    private float proximaMordida;
    private float proximaFumaca;
    private GameObject quemEnvenenou;

    public bool Gelado => Time.time < fimDoGelo;

    public bool Envenenado => mordidas > 0;

    public static CondicaoDoInimigo Em(InimigoDeSala alvo)
    {
        CondicaoDoInimigo c = alvo.GetComponent<CondicaoDoInimigo>();

        if (c == null)
        {
            c = alvo.gameObject.AddComponent<CondicaoDoInimigo>();
            c.inimigo = alvo;
            c.vida = alvo.GetComponent<Vida>();
        }

        return c;
    }

    public void Gelar(float fracaoDaVelocidade, float segundos)
    {
        lentidao = Mathf.Clamp(fracaoDaVelocidade, 0.05f, 1f);
        fimDoGelo = Mathf.Max(fimDoGelo, Time.time + segundos);
        inimigo.MultiplicadorDeVelocidade = lentidao;

        if (cristal == null)
        {
            Sprite[] quadros = ArteImportada.CristalGirando(100f);
            cristal = EfeitoDeQuadros.Criar(quadros, 8f, (Vector2)transform.position + Vector2.up * 0.55f, 15, transform);

            if (cristal != null)
            {
                cristal.EmLoop();
                cristal.transform.localScale = Vector3.one * EfeitosDeImpacto.EscalaPara(quadros[0], 0.4f)
                                               / Mathf.Max(0.01f, transform.lossyScale.x);
                cristal.GetComponent<SpriteRenderer>().color = new Color(0.8f, 0.95f, 1f, 0.9f);
            }
        }
    }

    public void Envenenar(int quantas, float dano, float intervalo, GameObject quem)
    {
        // Renova: fica com o que durar mais e com a mordida mais forte.
        if (mordidas == 0)
            proximaMordida = Time.time + intervalo;

        mordidas = Mathf.Max(mordidas, quantas);
        danoDaMordida = Mathf.Max(danoDaMordida, dano);
        intervaloDoVeneno = Mathf.Max(0.1f, intervalo);
        quemEnvenenou = quem;
    }

    private void Update()
    {
        if (inimigo == null || inimigo.EstaMorto)
        {
            Limpar();
            return;
        }

        if (cristal != null && !Gelado)
        {
            inimigo.MultiplicadorDeVelocidade = 1f;
            Destroy(cristal.gameObject);
            cristal = null;
        }

        if (mordidas > 0)
            Veneno();
    }

    private void Veneno()
    {
        if (Time.time >= proximaFumaca)
        {
            proximaFumaca = Time.time + 0.25f;
            Vector2 onde = (Vector2)transform.position + Random.insideUnitCircle * 0.25f + Vector2.up * 0.2f;
            EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Poeira, onde, new Color(0.45f, 0.85f, 0.3f, 0.65f), 0.3f);
        }

        if (Time.time < proximaMordida)
            return;

        proximaMordida = Time.time + intervaloDoVeneno;
        mordidas--;

        if (vida != null && !vida.EstaMorto)
            vida.TomarDano(new DanoInfo(danoDaMordida, Vector2.up, 0f, transform.position, quemEnvenenou,
                                        PesoDoGolpe.Leve, true));

        if (mordidas == 0)
            danoDaMordida = 0f;
    }

    private void Limpar()
    {
        if (cristal != null)
        {
            Destroy(cristal.gameObject);
            cristal = null;
        }

        mordidas = 0;
    }
}
