using UnityEngine;

/// <summary>
/// O calice dourado da area segura (pacote Ancient Ruins), com os espiritos saindo dele: tocar da um
/// coracao e um punhado de moedas, uma vez. Depois os espiritos somem.
/// </summary>
public class CaliceDosEspiritos : Interativo
{
    private Sprite[] quadros;
    private SpriteRenderer desenho;
    private FonteDeLuz luz;
    private bool usado;

    public override float Alcance => 1.3f;

    public override bool Disponivel => !usado;

    public override string Dica => "receber a bênção dos espíritos";

    public static CaliceDosEspiritos Criar(Vector2 pe, Transform pai)
    {
        Sprite[] quadros = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Areas/Ruinas/Calice"), new Vector2Int(64, 64), 32f);

        if (quadros.Length == 0)
            return null;

        GameObject obj = new GameObject("Calice");
        obj.transform.SetParent(pai, false);
        obj.transform.position = pe + Vector2.up * 0.8f;

        CaliceDosEspiritos c = obj.AddComponent<CaliceDosEspiritos>();
        c.quadros = quadros;
        c.desenho = obj.AddComponent<SpriteRenderer>();
        c.desenho.sprite = quadros[0];
        c.desenho.sortingOrder = 10;
        c.desenho.spriteSortPoint = SpriteSortPoint.Pivot;
        c.luz = Iluminacao.Luz(obj.transform, Vector2.zero, new Color(1f, 0.95f, 0.6f), 3f, 0.8f, 0.1f);

        obj.layer = Pedreiro.CamadaDaParede;
        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.4f);
        col.offset = new Vector2(0f, -0.6f);
        return c;
    }

    private void Update()
    {
        // Os espiritos so saem enquanto o calice tem a bencao; depois fica o primeiro quadro, parado.
        if (!usado)
            desenho.sprite = quadros[Mathf.FloorToInt(Time.time * 8f) % quadros.Length];
    }

    public override void Usar(GameObject jogador)
    {
        if (usado)
            return;

        usado = true;
        desenho.sprite = quadros[0];

        if (luz != null)
            luz.intensidade = 0.3f;

        Vector2 aqui = transform.position + Vector3.down * 1.2f;
        Coletavel.Criar(TipoDeColetavel.Coracao, aqui);

        for (int i = 0; i < 4; i++)
            Coletavel.Criar(TipoDeColetavel.Moeda, aqui + Random.insideUnitCircle * 0.9f);

        Sons.Tocar(Som.Segredo, 0.8f);
    }
}
