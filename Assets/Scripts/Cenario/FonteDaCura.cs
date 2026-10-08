using UnityEngine;

/// <summary>A fonte da area segura (pacote Ancient Ruins): beber dela cura a vida toda, uma vez.</summary>
public class FonteDaCura : Interativo
{
    private Sprite[] quadros;
    private SpriteRenderer desenho;
    private bool usada;

    public override float Alcance => 2.2f;

    public override bool Disponivel => !usada;

    public override string Dica => "beber da fonte (cura tudo)";

    public static FonteDaCura Criar(Vector2 pe, Transform pai)
    {
        Sprite[] quadros = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Areas/Ruinas/Fonte"), new Vector2Int(160, 128), 32f);

        if (quadros.Length == 0)
            return null;

        GameObject obj = new GameObject("Fonte");
        obj.transform.SetParent(pai, false);
        obj.transform.position = pe + Vector2.up * 1.9f;

        FonteDaCura f = obj.AddComponent<FonteDaCura>();
        f.quadros = quadros;
        f.desenho = obj.AddComponent<SpriteRenderer>();
        f.desenho.sprite = quadros[0];
        // A fonte e baixa: fica no chao, o jogador anda em volta.
        f.desenho.sortingOrder = Pedreiro.OrdemDosEnfeites + 1;

        obj.layer = Pedreiro.CamadaDaParede;
        CircleCollider2D c = obj.AddComponent<CircleCollider2D>();
        c.radius = 1.4f;
        Iluminacao.Luz(obj.transform, Vector2.zero, new Color(0.5f, 0.9f, 1f), 3.5f, 0.5f, 0.05f);
        return f;
    }

    private void Update()
    {
        desenho.sprite = quadros[Mathf.FloorToInt(Time.time * 8f) % quadros.Length];
    }

    public override void Usar(GameObject jogador)
    {
        if (usada || !jogador.TryGetComponent(out Vida vida))
            return;

        usada = true;
        vida.Curar(vida.Maxima);
        Sons.Tocar(Som.Cura, 0.9f);
        TextoFlutuante.Mostrar(jogador.transform.position + Vector3.up, "Vida cheia!", new Color(0.5f, 1f, 0.7f));
    }
}
