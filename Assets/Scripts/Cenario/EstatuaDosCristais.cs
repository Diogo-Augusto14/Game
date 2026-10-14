using UnityEngine;

/// <summary>
/// A estatua dos cristais: onde se gasta os cristais (<see cref="Melhorias"/>) nas melhorias que ficam
/// entre partidas. Fica na sala do comeco do primeiro andar e nas areas seguras. E a estatua de pedra
/// azul do pacote Crypt, com uma luz roxa e cristais em volta; interagir abre a <see cref="TelaDaEstatua"/>.
/// Monte com <see cref="Criar"/>.
/// </summary>
public class EstatuaDosCristais : Interativo
{
    private static readonly Color Roxo = new Color(0.72f, 0.5f, 1f);

    private FonteDeLuz luz;
    private bool podeComprar;
    private float proximaConta;

    public override float Alcance => 1.8f;

    public override string Dica => $"usar a estátua dos cristais ({Melhorias.Cristais} cristais)";

    /// <param name="pe">Onde fica o pe da estatua.</param>
    public static EstatuaDosCristais Criar(Vector2 pe, Transform pai)
    {
        Sprite desenho = Resources.Load<Sprite>("Decoracao/Cripta/Estatua/05");

        if (desenho == null)
            return null;

        GameObject obj = new GameObject("Estatua dos cristais");
        obj.transform.SetParent(pai, false);
        obj.transform.position = pe;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = desenho;
        sr.sortingOrder = 10;

        // Segura gente e tiro, como o resto da mobilia.
        obj.layer = Pedreiro.CamadaDaParede;
        BoxCollider2D c = obj.AddComponent<BoxCollider2D>();
        c.size = new Vector2(desenho.bounds.size.x * 0.8f, 0.6f);
        c.offset = new Vector2(0f, 0.3f);

        // Dois cristaizinhos no pe.
        Sprite cristal = Coletavel.Desenho(TipoDeColetavel.Cristal);

        foreach (float lado in new[] { -0.75f, 0.75f })
        {
            GameObject pedra = new GameObject("Cristal");
            pedra.transform.SetParent(obj.transform, false);
            pedra.transform.localPosition = new Vector3(lado, 0.15f, 0f);
            SpriteRenderer p = pedra.AddComponent<SpriteRenderer>();
            p.sprite = cristal;
            p.sortingOrder = 11;
            p.flipX = lado > 0f;
        }

        EstatuaDosCristais e = obj.AddComponent<EstatuaDosCristais>();
        e.luz = Iluminacao.Luz(obj.transform, new Vector2(0f, 1.2f), Roxo, 3.4f, 1f);
        return e;
    }

    private void Update()
    {
        // A luz respira mais forte quando da pra comprar alguma coisa (conta duas vezes por segundo).
        if (Time.time >= proximaConta)
        {
            proximaConta = Time.time + 0.5f;
            podeComprar = TemOQueComprar();
        }

        if (luz != null)
            luz.intensidade = (podeComprar ? 1.1f : 0.75f) + 0.15f * Mathf.Sin(Time.time * 2.5f);
    }

    private static bool TemOQueComprar()
    {
        foreach (Melhorias.Dados d in Melhorias.Todas)
        {
            if (!Melhorias.NoMaximo(d.Qual) && Melhorias.Cristais >= Melhorias.Preco(d.Qual))
                return true;
        }

        return false;
    }

    public override void Usar(GameObject jogador) => TelaDaEstatua.Abrir(jogador);
}
