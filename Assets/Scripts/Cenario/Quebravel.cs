using System.Collections;
using UnityEngine;

/// <summary>
/// Uma coisa do cenario que quebra (veio do jogo antigo): mesa, barril, caixote e a pedra rachada.
/// Segura tiro e ninguem atravessa; qualquer tiro acerta (lado <see cref="Lado.Neutro"/>). A mesa vira
/// de lado no primeiro golpe (vira um escudo) e se despedaca no fim. As vezes solta uma moeda. A
/// pedra rachada so quebra com bomba e guarda um premio.
/// </summary>
public class Quebravel : MonoBehaviour, IBloqueioDeDano
{
    private Vida vida;
    private SpriteRenderer desenho;
    private Sprite[] quadros;
    private bool soBomba;
    private System.Action<Vector2> premio;
    private bool virou;

    public static Quebravel Mesa(Vector2 onde, Transform pai) =>
        Criar("Mesa", onde, pai, ArteDoAntigo.Mesa, 14f, new Vector2(1.3f, 0.7f), false);

    public static Quebravel Barril(Vector2 onde, Transform pai) =>
        Criar("Barril", onde, pai, new[] { ArteDoAntigo.Barril }, 8f, new Vector2(0.7f, 0.6f), false);

    public static Quebravel Caixote(Vector2 onde, Transform pai) =>
        Criar("Caixote", onde, pai, new[] { ArteDoAntigo.Caixote }, 6f, new Vector2(0.8f, 0.6f), false);

    /// <summary>Um monte de pedras rachadas: so bomba quebra; quebrando, chama <paramref name="premio"/>.</summary>
    public static Quebravel PedraRachada(Vector2 onde, Transform pai, System.Action<Vector2> premio)
    {
        Sprite[] pedras = ArteDoAntigo.Pedras;
        Sprite uma = pedras.Length > 0 ? pedras[Random.Range(0, pedras.Length)] : null;
        Quebravel q = Criar("Pedra rachada", onde, pai, new[] { uma }, 1f, new Vector2(1.6f, 1.4f), true);
        q.premio = premio;
        q.transform.localScale = Vector3.one * 1.8f;

        // Rachaduras: duas pedras a mais por cima, tortas.
        for (int i = 0; i < 2 && pedras.Length > 0; i++)
        {
            GameObject mais = new GameObject("Pedra");
            mais.transform.SetParent(q.transform, false);
            mais.transform.localPosition = new Vector3(i == 0 ? -0.25f : 0.25f, 0.15f, 0f);
            SpriteRenderer sr = mais.AddComponent<SpriteRenderer>();
            sr.sprite = pedras[Random.Range(0, pedras.Length)];
            sr.sortingOrder = 9;
            sr.color = new Color(0.85f, 0.85f, 0.9f);
        }

        return q;
    }

    private static Quebravel Criar(string nome, Vector2 onde, Transform pai, Sprite[] quadros, float aguenta, Vector2 tamanho, bool soBomba)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(pai, false);
        obj.transform.position = onde;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = quadros.Length > 0 ? quadros[0] : null;
        sr.sortingOrder = 8;

        BoxCollider2D colisor = obj.AddComponent<BoxCollider2D>();
        colisor.size = tamanho;

        Quebravel q = obj.AddComponent<Quebravel>();
        q.desenho = sr;
        q.quadros = quadros;
        q.soBomba = soBomba;
        q.vida = obj.AddComponent<Vida>();
        q.vida.Configurar(Lado.Neutro, aguenta);
        q.vida.AoTomarDano += q.Apanhou;
        q.vida.AoMorrer += q.Quebrou;
        return q;
    }

    public bool Bloqueia(Dano dano) => soBomba && (dano.fonte == null || !dano.fonte.TryGetComponent(out Bomba _));

    private void Apanhou(Dano dano)
    {
        // A mesa vira de lado no primeiro golpe (os quadros 0 a 6 do pacote).
        if (!virou && quadros.Length >= 10)
        {
            virou = true;
            StartCoroutine(Tocar(0, 6, 20f));
        }
    }

    private void Quebrou()
    {
        foreach (Collider2D c in GetComponents<Collider2D>())
            c.enabled = false;

        Sons.Tocar(soBomba ? Som.Segredo : Som.Pancada, 0.6f);
        premio?.Invoke(transform.position);

        if (!soBomba && Random.value < 0.25f)
            Coletavel.Criar(TipoDeColetavel.Moeda, transform.position);

        StartCoroutine(Sumir());
    }

    private IEnumerator Tocar(int de, int ate, float qps)
    {
        for (int i = de; i <= ate && i < quadros.Length; i++)
        {
            desenho.sprite = quadros[i];
            yield return new WaitForSeconds(1f / qps);
        }
    }

    private IEnumerator Sumir()
    {
        // A mesa toca os quadros de quebrar (7 a 9); o resto so desbota.
        if (quadros.Length >= 10)
            yield return Tocar(7, 9, 14f);
        else
            EfeitoDeFolha.Tocar(ArteDoAntigo.ExplosaoPequena, transform.position, 26f, 25);

        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f)
        {
            foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f - t);

            yield return null;
        }

        Destroy(gameObject);
    }
}
