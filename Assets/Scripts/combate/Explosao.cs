using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Uma explosao: machuca tudo que tem <see cref="Vida"/> dentro do raio (o jogador com um
/// dano proprio), empurra pra fora, abre porta secreta e mostra a bola de fogo do Tiny Swords.
/// Usada pela bomba do jogador, pela dinamite do goblin e pelo barril.
/// </summary>
public static class Explosao
{
    private const string TagDoJogador = "Player";

    /// <summary>Faz o estrago e mostra o efeito. Devolve true se a arte da explosao existe.</summary>
    public static bool Estourar(Vector2 centro, float raio, float danoNosOutros, float danoNoJogador, float empurrao,
                                GameObject dono)
    {
        Sons.Tocar(Som.Explosao);
        Impacto.Tremer(Mathf.Clamp(raio * 0.12f, 0.15f, 0.4f), 0.35f);

        // Cada Vida uma vez so, mesmo que tenha varios colisores.
        HashSet<Vida> atingidos = new HashSet<Vida>();

        foreach (Collider2D c in Physics2D.OverlapCircleAll(centro, raio))
        {
            // Porta secreta escondida na parede: a explosao abre.
            Porta porta = c.GetComponentInParent<Porta>();

            if (porta != null && porta.Escondida)
                porta.Revelar();

            Vida vida = c.GetComponentInParent<Vida>();

            if (vida == null || vida.EstaMorto || !atingidos.Add(vida))
                continue;

            Vector2 direcao = (Vector2)vida.transform.position - centro;

            if (direcao.sqrMagnitude < 0.0001f)
                direcao = Vector2.up;

            float quanto = vida.CompareTag(TagDoJogador) ? danoNoJogador : danoNosOutros;

            if (quanto > 0f)
                vida.TomarDano(new DanoInfo(quanto, direcao.normalized, empurrao, centro, dono));
        }

        return Efeito(centro, raio);
    }

    /// <summary>So a bola de fogo, do tamanho do raio. False se a arte nao existe.</summary>
    public static bool Efeito(Vector2 centro, float raio)
    {
        Sprite[] quadros = ArteImportada.Explosao(ArteImportada.ExplosaoPixelsPorUnidade(raio));
        return EfeitoDeQuadros.Criar(quadros, 18f, centro, 30) != null;
    }
}
