using UnityEngine;

/// <summary>
/// A criaturinha da sorte da area segura (pacote Ancient Ruins): anda devagar por perto de onde nasceu.
/// Um carinho e ela solta moedas (as vezes uma chave ou uma bomba) e sai correndo, sumindo na mata.
/// </summary>
public class CriaturaDaSorte : Interativo
{
    private const float Raio = 3f;

    private Sprite[] parada, correndo;
    private SpriteRenderer desenho;
    private Vector2 casa, alvo;
    private float trocaDeAlvo;
    private bool fugindo;
    private float fugiu;

    public override float Alcance => 1.2f;

    public override bool Disponivel => !fugindo;

    public override string Dica => "fazer carinho";

    public static CriaturaDaSorte Criar(Vector2 pe, Transform pai)
    {
        Sprite[] parada = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Areas/Ruinas/Sorte"), new Vector2Int(96, 96), 32f);

        if (parada.Length == 0)
            return null;

        GameObject obj = new GameObject("Criatura da sorte");
        obj.transform.SetParent(pai, false);
        obj.transform.position = pe;

        CriaturaDaSorte c = obj.AddComponent<CriaturaDaSorte>();
        c.parada = parada;
        c.correndo = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Areas/Ruinas/SorteCorrendo"), new Vector2Int(96, 96), 32f);
        c.casa = pe;
        c.alvo = pe;

        // O desenho num filho: o pe da arte fica 0,3 abaixo do meio do quadro.
        GameObject filho = new GameObject("Desenho");
        filho.transform.SetParent(obj.transform, false);
        filho.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        filho.transform.localScale = Vector3.one * 1.4f;
        c.desenho = filho.AddComponent<SpriteRenderer>();
        c.desenho.sprite = parada[0];
        c.desenho.sortingOrder = 10;
        Iluminacao.Luz(obj.transform, Vector2.up * 0.3f, new Color(1f, 0.95f, 0.5f), 1.6f, 0.6f, 0.1f);
        return c;
    }

    private void Update()
    {
        if (fugindo)
        {
            Andar(alvo, 5f);

            float t = (Time.time - fugiu) / 1.5f;
            desenho.color = new Color(1f, 1f, 1f, 1f - t);

            if (t >= 1f)
                Destroy(gameObject);

            return;
        }

        // Passeia: escolhe um ponto perto de casa, vai devagar, para um pouco.
        if (Time.time > trocaDeAlvo)
        {
            trocaDeAlvo = Time.time + Random.Range(2f, 4f);
            alvo = Random.value < 0.4f ? (Vector2)transform.position : casa + Random.insideUnitCircle * Raio;
        }

        Andar(alvo, 1.2f);
    }

    private void Andar(Vector2 para, float velocidade)
    {
        Vector2 aqui = transform.position;
        bool andando = Vector2.Distance(aqui, para) > 0.05f;

        if (andando)
        {
            transform.position = Vector2.MoveTowards(aqui, para, velocidade * Time.deltaTime);
            desenho.flipX = para.x < aqui.x;
        }

        Sprite[] quadros = andando && correndo.Length > 0 ? correndo : parada;
        desenho.sprite = quadros[Mathf.FloorToInt(Time.time * (andando ? 12f : 6f)) % quadros.Length];
    }

    public override void Usar(GameObject jogador)
    {
        if (fugindo)
            return;

        fugindo = true;
        fugiu = Time.time;
        Vector2 aqui = transform.position;

        for (int i = 0; i < Random.Range(3, 7); i++)
            Coletavel.Criar(TipoDeColetavel.Moeda, aqui + Random.insideUnitCircle * 0.8f);

        if (Random.value < 0.5f)
            Coletavel.Criar(Random.value < 0.5f ? TipoDeColetavel.Chave : TipoDeColetavel.Bomba, aqui + Random.insideUnitCircle * 0.6f);

        Sons.Tocar(Som.Moeda, 0.9f);

        // Foge pro lado contrario do jogador.
        Vector2 longe = (aqui - (Vector2)jogador.transform.position).normalized;
        alvo = aqui + (longe == Vector2.zero ? Vector2.right : longe) * 8f;
    }
}
