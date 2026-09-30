using UnityEngine;

/// <summary>
/// O orbe do item Orbe Guardiao: gira em volta do jogador, apaga os tiros dos inimigos que
/// encosta e machuca o inimigo em que bate. Nao fica pendurado no jogador (senao o colisor
/// viraria parte dele e compraria coisa na loja): segue o dono por conta propria.
/// </summary>
public class OrbeGuardiao : MonoBehaviour
{
    [SerializeField, Min(0.3f)] private float distancia = 1.1f;
    [SerializeField] private float voltasPorSegundo = 0.55f;
    [SerializeField, Min(0.05f)] private float raio = 0.28f;
    [SerializeField, Min(0f)] private float dano = 4f;
    [Tooltip("Segundos entre dois golpes no mesmo inimigo")]
    [SerializeField, Min(0.05f)] private float recarga = 0.35f;

    private Transform dono;
    private float faseInicial;
    private float proximoGolpe;

    public static OrbeGuardiao Criar(Transform dono)
    {
        GameObject obj = new GameObject("Orbe guardiao");
        obj.transform.position = dono.position;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        Sprite gema = ArteImportada.OrbeAzul;
        sr.sprite = gema != null ? gema : ArteGerada.Bola();
        sr.color = gema != null ? Color.white : new Color(0.3f, 0.75f, 1f);
        sr.sortingOrder = 19;
        obj.transform.localScale = Vector3.one * (gema != null ? 0.5f : 0.35f);

        OrbeGuardiao orbe = obj.AddComponent<OrbeGuardiao>();
        orbe.dono = dono;
        return orbe;
    }

    /// <summary>Espalha os orbes igualmente em volta (o 2o do outro lado do 1o).</summary>
    public void DefinirLugar(int indice, int total)
    {
        faseInicial = total > 0 ? indice * Mathf.PI * 2f / total : 0f;
    }

    private void LateUpdate()
    {
        if (dono == null)
        {
            Destroy(gameObject);
            return;
        }

        float angulo = faseInicial + Time.time * voltasPorSegundo * Mathf.PI * 2f;
        transform.position = dono.position + new Vector3(Mathf.Cos(angulo), Mathf.Sin(angulo) * 0.8f, 0f) * distancia;
        transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 4f) * 10f);
    }

    private void FixedUpdate()
    {
        if (dono == null)
            return;

        Vector2 aqui = transform.position;

        // Tiro de inimigo que encostar some.
        foreach (Collider2D c in Physics2D.OverlapCircleAll(aqui, raio))
        {
            TiroDaSala tiro = c.GetComponentInParent<TiroDaSala>();

            if (tiro != null && tiro.AtingeJogador)
                tiro.Anular();
        }

        if (Time.time < proximoGolpe)
            return;

        foreach (InimigoDeSala inimigo in InimigoDeSala.Ativos)
        {
            if (inimigo == null || inimigo.EstaMorto || inimigo.EstadoAtual == InimigoDeSala.Estado.Dormindo)
                continue;

            Vector2 lado = (Vector2)inimigo.transform.position - aqui;

            if (lado.sqrMagnitude > (raio + 0.45f) * (raio + 0.45f))
                continue;

            Vida vida = inimigo.GetComponent<Vida>();

            if (vida == null)
                continue;

            vida.TomarDano(new DanoInfo(dano, lado.sqrMagnitude > 0.0001f ? lado.normalized : Vector2.up, 2f, aqui, dono.gameObject));
            proximoGolpe = Time.time + recarga;
            break;
        }
    }
}
