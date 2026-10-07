using UnityEngine;

/// <summary>
/// Contorno claro de 1 pixel da arte em volta do desenho de um inimigo, pra ele nao sumir no
/// chao escuro (veio do jogo antigo). O <see cref="GeradorDoAndar"/> poe em todo inimigo e chefe
/// (<see cref="Colocar"/>).
///
/// Sem shader: quatro mascaras com o mesmo quadro do desenho, cada uma deslocada 1 pixel pra
/// um lado, recortam um retangulo claro que fica logo atras dele. O desenho tampa o miolo e
/// so sobra a borda. Some na morte e quando o desenho fica transparente (sumico).
/// </summary>
[DisallowMultipleComponent]
public class ContornoClaro : MonoBehaviour
{
    private static readonly Vector2[] Lados = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };
    private static Sprite pixel;

    private readonly SpriteMask[] mascaras = new SpriteMask[4];
    private SpriteRenderer alvo;
    private SpriteRenderer fundo;
    private Vida dono;
    private Color cor;
    private Sprite ultimo;
    private bool ultimoFlipX;
    private bool ultimoFlipY;
    private bool ligado = true;

    /// <summary>Poe o contorno no desenho do corpo do inimigo (o SpriteRenderer de ordem mais alta).</summary>
    public static ContornoClaro Colocar(GameObject inimigo, Color cor)
    {
        SpriteRenderer corpo = null;

        foreach (SpriteRenderer desenho in inimigo.GetComponentsInChildren<SpriteRenderer>())
        {
            if (corpo == null || desenho.sortingOrder > corpo.sortingOrder)
                corpo = desenho;
        }

        return corpo != null && corpo.GetComponent<ContornoClaro>() == null ? Criar(corpo, cor) : null;
    }

    /// <summary>Poe o contorno no desenho <paramref name="alvo"/> (que tem de estar numa ordem acima de tudo do chao).</summary>
    public static ContornoClaro Criar(SpriteRenderer alvo, Color cor)
    {
        if (alvo == null)
            return null;

        ContornoClaro contorno = alvo.gameObject.AddComponent<ContornoClaro>();
        contorno.alvo = alvo;
        contorno.cor = cor;
        contorno.dono = alvo.GetComponentInParent<Vida>();

        int ordem = alvo.sortingOrder - 1;

        for (int i = 0; i < contorno.mascaras.Length; i++)
        {
            GameObject obj = new GameObject("Mascara do contorno");
            obj.transform.SetParent(alvo.transform, false);

            SpriteMask mascara = obj.AddComponent<SpriteMask>();
            mascara.isCustomRangeActive = true;
            mascara.frontSortingLayerID = alvo.sortingLayerID;
            mascara.backSortingLayerID = alvo.sortingLayerID;
            mascara.frontSortingOrder = ordem;       // inclui a ordem do retangulo...
            mascara.backSortingOrder = ordem - 1;    // ...e nenhuma abaixo dela
            contorno.mascaras[i] = mascara;
        }

        GameObject objFundo = new GameObject("Contorno");
        objFundo.transform.SetParent(alvo.transform, false);
        contorno.fundo = objFundo.AddComponent<SpriteRenderer>();
        contorno.fundo.sprite = Pixel();
        contorno.fundo.color = cor;
        contorno.fundo.sortingLayerID = alvo.sortingLayerID;
        contorno.fundo.sortingOrder = ordem;
        contorno.fundo.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        contorno.LateUpdate();
        return contorno;
    }

    // Um quadrado branco de 1 unidade (esticado no tamanho do desenho).
    private static Sprite Pixel()
    {
        if (pixel == null)
        {
            Texture2D branco = Texture2D.whiteTexture;
            pixel = Sprite.Create(branco, new Rect(0f, 0f, branco.width, branco.height), new Vector2(0.5f, 0.5f), branco.width);
        }

        return pixel;
    }

    private void LateUpdate()
    {
        if (alvo == null || fundo == null)
            return;

        Sprite quadro = alvo.sprite;
        bool mostrar = alvo.enabled && quadro != null && (dono == null || !dono.Morto);

        if (mostrar != ligado)
        {
            ligado = mostrar;
            fundo.enabled = mostrar;

            foreach (SpriteMask mascara in mascaras)
                mascara.enabled = mostrar;
        }

        if (!mostrar)
            return;

        // Some junto quando o desenho fica transparente (sumico, fim da morte).
        Color agora = cor;
        agora.a *= alvo.color.a;
        fundo.color = agora;

        if (quadro == ultimo && alvo.flipX == ultimoFlipX && alvo.flipY == ultimoFlipY)
            return;

        ultimo = quadro;
        ultimoFlipX = alvo.flipX;
        ultimoFlipY = alvo.flipY;

        // A mascara nao tem flipX: espelha pela escala, que vira em volta do pivo igual ao desenho.
        Vector3 espelho = new Vector3(alvo.flipX ? -1f : 1f, alvo.flipY ? -1f : 1f, 1f);
        float umPixel = 1f / quadro.pixelsPerUnit;

        for (int i = 0; i < mascaras.Length; i++)
        {
            mascaras[i].sprite = quadro;
            mascaras[i].transform.localPosition = Lados[i] * umPixel;
            mascaras[i].transform.localScale = espelho;
        }

        Bounds area = quadro.bounds;
        fundo.transform.localPosition = new Vector3(area.center.x * espelho.x, area.center.y * espelho.y, 0f);
        fundo.transform.localScale = new Vector3(area.size.x + umPixel * 2f, area.size.y + umPixel * 2f, 1f);
    }
}
