using System;
using UnityEngine;

/// <summary>
/// Um clipe de animação quadro a quadro: a folha de sprites (textura), os retângulos
/// de cada quadro em pixels, o pivô e a velocidade.
///
/// Por que guardar retângulos em vez de <see cref="Sprite"/> prontos: os Sprites são
/// criados na hora (<see cref="Sprite.Create"/>) com o pivô EXATO calculado nos pés do
/// personagem. As fatias que a Unity gera sozinha ficam justas no desenho, então cada
/// quadro tem um tamanho diferente e o boneco "pula" de um quadro pro outro. Aqui todos
/// os quadros de um clipe dividem a mesma célula e o mesmo pivô: animação sem tremida.
/// </summary>
[Serializable]
public class ClipeDeSprites
{
    [Tooltip("Nome usado pra pedir o clipe (veja NomesDeAnimacao)")]
    public string nome = "";

    [Tooltip("A folha de sprites de onde os quadros são recortados")]
    public Texture2D textura;

    [Tooltip("Retângulo de cada quadro, em pixels, com Y contado de baixo pra cima")]
    public Rect[] quadros = Array.Empty<Rect>();

    [Tooltip("Pivô dentro do quadro (0..1). Calculado nos pés do boneco pelo Construtor de Animações")]
    public Vector2 pivo = new Vector2(0.5f, 0f);

    [Tooltip("Pixels por unidade do mundo. Tem que ser igual em todos os clipes")]
    [Min(1f)] public float pixelsPorUnidade = 100f;

    [Tooltip("Quadros por segundo")]
    [Min(0.1f)] public float quadrosPorSegundo = 12f;

    [Tooltip("Ligado: repete pra sempre. Desligado: toca uma vez")]
    public bool emLoop;

    [Tooltip("Sem loop: segura o último quadro no fim (em vez de sumir)")]
    public bool manterUltimoQuadro = true;

    // Sprites criados sob demanda. NÃO serializa: ao sair do Play a Unity destrói os
    // objetos criados em tempo de execução e sobrariam referências mortas dentro do asset.
    [NonSerialized] private Sprite[] cache;

    public int Quantidade => quadros != null ? quadros.Length : 0;

    public bool Valido => textura != null && Quantidade > 0;

    /// <summary>Duração em segundos de uma passada completa.</summary>
    public float Duracao => Quantidade / Mathf.Max(0.1f, quadrosPorSegundo);

    /// <summary>
    /// O Sprite do quadro pedido. O índice é grudado nos limites, então pedir um quadro
    /// que não existe devolve o primeiro/último em vez de estourar.
    /// </summary>
    public Sprite Quadro(int indice)
    {
        if (!Valido)
            return null;

        indice = Mathf.Clamp(indice, 0, Quantidade - 1);

        if (cache == null || cache.Length != Quantidade)
            cache = new Sprite[Quantidade];

        // O "== null" pega tanto o primeiro acesso quanto o Sprite destruído ao sair do Play.
        if (cache[indice] == null)
        {
            cache[indice] = Sprite.Create(
                textura,
                quadros[indice],
                pivo,
                pixelsPorUnidade,
                0,
                SpriteMeshType.FullRect);

            cache[indice].name = $"{nome}_{indice}";
        }

        return cache[indice];
    }

    /// <summary>Solta os Sprites criados. Chamado ao descarregar a biblioteca.</summary>
    public void LimparCache()
    {
        if (cache == null)
            return;

        for (int i = 0; i < cache.Length; i++)
        {
            if (cache[i] != null && !Application.isEditor)
                UnityEngine.Object.Destroy(cache[i]);

            cache[i] = null;
        }
    }
}
