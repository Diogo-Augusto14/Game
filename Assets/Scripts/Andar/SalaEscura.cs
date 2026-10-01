using UnityEngine;

/// <summary>
/// Sala escura: o breu cobre a sala inteira e so se enxerga em volta do heroi (um circulo
/// limpo e uma faixa de penumbra). Os inimigos aparecem quando chegam perto. Quando a sala
/// e limpa, a escuridao some devagar.
///
/// Sem shader: duas camadas pretas com <see cref="SpriteMask"/> redondas que seguem o
/// jogador. A de dentro abre o circulo limpo, a de fora a penumbra; cada mascara so recorta
/// a sua camada (faixa de ordem propria), entao nao mexe no contorno dos inimigos.
/// </summary>
[DisallowMultipleComponent]
public class SalaEscura : MonoBehaviour
{
    private const int OrdemDaPenumbra = 36;
    private const int OrdemDoBreu = 37;

    [SerializeField, Min(0.5f)] private float raioLimpo = 2.3f;
    [SerializeField, Min(0.5f)] private float raioDaPenumbra = 3.4f;

    private Sala sala;
    private SpriteRenderer penumbra;
    private SpriteRenderer breu;
    private Transform luzDeDentro;
    private Transform luzDeFora;
    private float sumindo = -1f;

    public static SalaEscura Montar(Sala sala)
    {
        SalaEscura escura = sala.gameObject.AddComponent<SalaEscura>();
        escura.sala = sala;
        escura.Construir();
        sala.AoLimpar.AddListener(escura.Clarear);
        return escura;
    }

    private void Construir()
    {
        Vector2 tamanho = sala.TamanhoInterno + Vector2.one * 0.2f;
        penumbra = Camada("Penumbra", tamanho, new Color(0f, 0f, 0.02f, 0.55f), OrdemDaPenumbra);
        breu = Camada("Breu", tamanho, new Color(0f, 0f, 0.02f, 0.6f), OrdemDoBreu);

        // A penumbra some no circulo de dentro; o breu, no de fora. Fora dos dois: as duas juntas.
        luzDeDentro = Mascara("Luz de dentro", raioLimpo, OrdemDaPenumbra);
        luzDeFora = Mascara("Luz de fora", raioDaPenumbra, OrdemDoBreu);
    }

    private SpriteRenderer Camada(string nome, Vector2 tamanho, Color cor, int ordem)
    {
        SpriteRenderer sr = FormasDaSala.Desenho(transform, nome, Fosso.Pixel(), cor, Vector2.zero, tamanho, ordem);
        sr.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        return sr;
    }

    private Transform Mascara(string nome, float raio, int ordem)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(transform, false);
        obj.transform.localScale = Vector3.one * raio * 2f;

        SpriteMask mascara = obj.AddComponent<SpriteMask>();
        mascara.sprite = FormasDaSala.Circulo();
        mascara.isCustomRangeActive = true;
        mascara.backSortingOrder = ordem - 1;
        mascara.frontSortingOrder = ordem;
        return obj.transform;
    }

    private void Clarear() => sumindo = 0f;

    private void LateUpdate()
    {
        Transform jogador = Andar.Atual != null ? Andar.Atual.Jogador : null;

        if (jogador != null)
        {
            // Respira um pouco: luz de vela.
            float tremor = 1f + 0.03f * Mathf.Sin(Time.time * 7f) + 0.02f * Mathf.Sin(Time.time * 13f);
            luzDeDentro.position = jogador.position;
            luzDeFora.position = jogador.position;
            luzDeDentro.localScale = Vector3.one * raioLimpo * 2f * tremor;
            luzDeFora.localScale = Vector3.one * raioDaPenumbra * 2f * tremor;
        }

        if (sumindo < 0f)
            return;

        sumindo += Time.deltaTime;
        float alfa = Mathf.Clamp01(1f - sumindo / 1.2f);
        penumbra.color = new Color(0f, 0f, 0.02f, 0.55f * alfa);
        breu.color = new Color(0f, 0f, 0.02f, 0.6f * alfa);

        if (alfa <= 0f)
            Destroy(this);
    }

    private void OnDestroy()
    {
        if (penumbra != null)
            Destroy(penumbra.gameObject);

        if (breu != null)
            Destroy(breu.gameObject);

        if (luzDeDentro != null)
            Destroy(luzDeDentro.gameObject);

        if (luzDeFora != null)
            Destroy(luzDeFora.gameObject);
    }
}
