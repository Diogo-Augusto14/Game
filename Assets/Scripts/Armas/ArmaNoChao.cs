using UnityEngine;

/// <summary>
/// Uma arma caida no chao, com a municao que tinha. Flutua de leve, com um brilho da cor da raridade
/// (as comuns, nenhum); chegando perto, a dica aparece com o nome e a raridade, e interagir pega (a
/// arma que estava no lugar cai aqui). Monte com <see cref="Criar"/>.
/// </summary>
[DisallowMultipleComponent]
public class ArmaNoChao : Interativo
{
    private ArmaCarregada arma;
    private Transform desenho;
    private float fase;

    public override string Dica
    {
        get
        {
            string municao = arma.Infinita ? "" : $" ({arma.NoPente + arma.Reserva})";
            return Idioma.F("pegar {0}{1} [{2}]", Idioma.T(arma.Dados.nome), municao, Raridades.Nome(arma.Dados.raridade));
        }
    }

    public static ArmaNoChao Criar(ArmaCarregada arma, Vector2 onde, Transform pai)
    {
        GameObject obj = new GameObject(arma.Dados.nome + " (no chao)");
        obj.transform.SetParent(pai, false);
        obj.transform.position = onde;

        GameObject filho = new GameObject("Desenho");
        filho.transform.SetParent(obj.transform, false);
        SpriteRenderer sprite = filho.AddComponent<SpriteRenderer>();
        sprite.sprite = arma.Dados.Desenho;
        sprite.sortingOrder = -99;
        Iluminacao.Brilhar(sprite);

        // O brilho da raridade.
        if (arma.Dados.raridade != Raridade.Comum)
            Iluminacao.Luz(obj.transform, Vector2.zero, Raridades.Cor(arma.Dados.raridade), 1.6f + 0.3f * (int)arma.Dados.raridade, 0.9f, 0.05f);

        ArmaNoChao noChao = obj.AddComponent<ArmaNoChao>();
        noChao.arma = arma;
        noChao.desenho = filho.transform;
        noChao.fase = Random.value * 10f;
        return noChao;
    }

    private void Update()
    {
        desenho.localPosition = new Vector3(0f, 0.08f * Mathf.Sin((Time.time + fase) * 3f), 0f);
    }

    public override void Usar(GameObject jogador)
    {
        if (!jogador.TryGetComponent(out ArmaDoJogador armas))
            return;

        ArmaCarregada velha = armas.Pegar(arma);

        if (velha != null)
            Criar(velha, transform.position, transform.parent);

        Destroy(gameObject);
    }
}
