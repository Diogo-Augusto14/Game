using UnityEngine;

/// <summary>
/// A sala amaldicoada do Isaac (a partir do andar 2): a porta dela tem espinhos e cada
/// passagem por ela, entrando ou saindo, custa meio coracao. La dentro, no escuro, tem
/// um premio melhor que o de uma sala comum: um item no pedestal ou um bau cheio.
///
/// Quem monta e o <see cref="Andar"/>: <see cref="Montar"/> poe o premio e os enfeites,
/// <see cref="MarcarPorta"/> poe os espinhos nas portas e <see cref="Ferir"/> cobra a passagem.
/// </summary>
public static class SalaAmaldicoada
{
    /// <summary>Meio coracao: o mesmo dano de um espinho do chao.</summary>
    public const float DANO_DA_PORTA = 10f;

    /// <summary>Chance do premio ser item no pedestal (senao e bau).</summary>
    private const float CHANCE_DE_ITEM = 0.5f;

    public static void Montar(Sala sala, ItemPassivo item)
    {
        Vector2 centro = sala.transform.position;

        if (item != null && Random.value < CHANCE_DE_ITEM)
            Pedestal.Criar(item, centro, sala.transform);
        else
            Bau.Criar(centro, sala.transform, TipoDeColetavel.Moeda, TipoDeColetavel.Moeda, TipoDeColetavel.Moeda,
                      TipoDeColetavel.Bomba, TipoDeColetavel.Chave, TipoDeColetavel.Coracao);

        // Idolos de olho vermelho nos quatro cantos.
        Sprite idolo = ArteImportada.IdoloMaldito;

        if (idolo == null)
            return;

        Vector2 meio = sala.TamanhoInterno * 0.5f - Vector2.one * 0.8f;

        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                FormasDaSala.Desenho(sala.transform, "Idolo", idolo, Color.white, new Vector2(x * meio.x, y * meio.y),
                                     Vector2.one * 1.2f, -8);
    }

    /// <summary>Espinhos no vao da porta que leva (ou sai) da sala amaldicoada: avisa que doi passar.</summary>
    public static void MarcarPorta(Porta porta)
    {
        Sprite[] espinhos = ArteImportada.EspinhosDoChao;
        Vector2 eixo = porta.Lado.Horizontal() ? Vector2.right : Vector2.up;

        for (int i = -1; i <= 1; i++)
        {
            Vector2 local = eixo * (i * 0.45f);

            if (espinhos != null)
                FormasDaSala.Desenho(porta.transform, "EspinhoDaPorta", espinhos[espinhos.Length - 1], Color.white, local,
                                     Vector2.one * 0.5f, 3);
            else
                FormasDaSala.Desenho(porta.transform, "EspinhoDaPorta", ArteGerada.EspinhosNoChao(), Color.white, local,
                                     Vector2.one * 0.5f, 3);
        }

        // E os idolos no lugar dos estandartes das outras salas especiais.
        Sprite idolo = ArteImportada.IdoloMaldito;

        if (idolo == null)
            return;

        for (int s = -1; s <= 1; s += 2)
            FormasDaSala.Desenho(porta.transform, "Batente", idolo, Color.white, eixo * (s * 1.05f), Vector2.one * 0.7f, 2);
    }

    /// <summary>Cobra a passagem: meio coracao, empurrando pra dentro da sala onde chegou.</summary>
    public static void Ferir(Transform jogador, Vector2 paraDentro)
    {
        if (jogador == null || !jogador.TryGetComponent(out Vida vida) || vida.EstaMorto)
            return;

        vida.TomarDano(new DanoInfo(DANO_DA_PORTA, paraDentro, 2f, jogador.position, null));
    }
}
