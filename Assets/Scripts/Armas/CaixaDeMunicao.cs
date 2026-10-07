using UnityEngine;

/// <summary>
/// Uma caixa de municao no chao: encostando, enche metade da municao da arma achada (a que esta na
/// mao, ou a outra se a da mao e a do personagem). Se a arma ja esta cheia, a caixa fica no chao
/// esperando. Cai de alguns inimigos e de alguns baus. Monte com <see cref="Criar"/>.
/// </summary>
[DisallowMultipleComponent]
public class CaixaDeMunicao : MonoBehaviour
{
    private const float Alcance = 0.75f;

    private ArmaDoJogador armas;
    private Transform jogador;
    private AudioClip som;
    private float fase;

    public static CaixaDeMunicao Criar(Sprite desenho, Vector2 onde, Transform pai, AudioClip som)
    {
        GameObject obj = new GameObject("Caixa de municao");
        obj.transform.SetParent(pai, false);
        obj.transform.position = onde;

        SpriteRenderer sprite = obj.AddComponent<SpriteRenderer>();
        sprite.sprite = desenho;
        sprite.sortingOrder = -99;

        CaixaDeMunicao caixa = obj.AddComponent<CaixaDeMunicao>();
        caixa.som = som;
        caixa.fase = Random.value * 10f;
        return caixa;
    }

    private void Update()
    {
        transform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin((Time.time + fase) * 4f));

        if (jogador == null)
        {
            GameObject obj = GameObject.FindWithTag("Player");

            if (obj == null)
                return;

            jogador = obj.transform;
            armas = obj.GetComponent<ArmaDoJogador>();
        }

        if (armas == null || Vector2.Distance(jogador.position, transform.position) > Alcance)
            return;

        ArmaCarregada alvo = armas.Atual != null && !armas.Atual.Infinita ? armas.Atual : armas.Outra;

        if (alvo == null || alvo.Infinita || alvo.Falta == 0)
            return;

        alvo.Ganhar(Mathf.CeilToInt(alvo.Dados.municaoMaxima * 0.5f));

        if (som != null && jogador.TryGetComponent(out AudioSource audioSource))
            audioSource.PlayOneShot(som, 0.6f);

        Destroy(gameObject);
    }
}
