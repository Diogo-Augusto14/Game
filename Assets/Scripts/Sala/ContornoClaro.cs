using UnityEngine;

/// <summary>
/// Contorno claro de 1 pixel da arte em volta do desenho de um inimigo, pra ele nao sumir no
/// chao escuro (o morcego roxo-escuro quase desaparecia no meio da briga).
///
/// Sem shader: quatro mascaras com o mesmo quadro do desenho, cada uma deslocada 1 pixel pra
/// um lado, recortam um retangulo claro que fica logo atras dele. O desenho tampa o miolo e
/// so sobra a borda. As mascaras valem so pra ordem desse retangulo, nao mexem em mais nada.
/// </summary>
[DisallowMultipleComponent]
public class ContornoClaro : MonoBehaviour
{
    private static readonly Vector2[] Lados = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };

    private readonly SpriteMask[] mascaras = new SpriteMask[4];
    private SpriteRenderer alvo;
    private SpriteRenderer fundo;
    private InimigoDeSala dono;
    private Color cor;
    private Sprite ultimo;
    private bool ultimoFlipX;
    private bool ultimoFlipY;
    private bool ligado = true;

    /// <summary>Poe o contorno no desenho <paramref name="alvo"/> (que tem de estar numa ordem acima de tudo do chao).</summary>
    public static ContornoClaro Criar(SpriteRenderer alvo, Color cor)
    {
        if (alvo == null)
            return null;

        ContornoClaro contorno = alvo.gameObject.AddComponent<ContornoClaro>();
        contorno.alvo = alvo;
        contorno.cor = cor;
        contorno.dono = alvo.GetComponentInParent<InimigoDeSala>();

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

        contorno.fundo = FormasDaSala.Desenho(alvo.transform, "Contorno", Fosso.Pixel(), cor, Vector2.zero, Vector2.one, ordem);
        contorno.fundo.sortingLayerID = alvo.sortingLayerID;
        contorno.fundo.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        contorno.LateUpdate();
        return contorno;
    }

    private void LateUpdate()
    {
        if (alvo == null || fundo == null)
            return;

        Sprite quadro = alvo.sprite;
        bool mostrar = alvo.enabled && quadro != null && (dono == null || (!dono.EstaMorto && !dono.Disfarcado));

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
        float pixel = 1f / quadro.pixelsPerUnit;

        for (int i = 0; i < mascaras.Length; i++)
        {
            mascaras[i].sprite = quadro;
            mascaras[i].transform.localPosition = Lados[i] * pixel;
            mascaras[i].transform.localScale = espelho;
        }

        Bounds area = quadro.bounds;
        fundo.transform.localPosition = new Vector3(area.center.x * espelho.x, area.center.y * espelho.y, 0f);
        fundo.transform.localScale = new Vector3(area.size.x + pixel * 2f, area.size.y + pixel * 2f, 1f);
    }
}
