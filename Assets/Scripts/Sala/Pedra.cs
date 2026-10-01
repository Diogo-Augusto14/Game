using UnityEngine;

public enum TipoDeObstaculo
{
    Pedra,
    Espinhos,
    /// <summary>Bloco de parede que da forma a sala (nao quebra).</summary>
    Muro,
    /// <summary>Buraco no chao: bloqueia andar, nao bloqueia tiro.</summary>
    Fosso
}

/// <summary>
/// Pedra de um ladrilho. Bloqueia andar e tiro (fica na camada de parede, entao lagrima,
/// tiro inimigo e investida param nela), e bomba quebra. E o obstaculo classico do Isaac.
///
/// A vida existe so pra bomba achar: tiro nenhum machuca pedra, porque o tiro some ao
/// bater em qualquer coisa da camada de parede antes de procurar alvo.
/// </summary>
[DisallowMultipleComponent]
public class Pedra : MonoBehaviour
{
    public static Pedra Criar(Transform pai, Vector2 posicaoLocal, Color cor)
    {
        // Pedra do Old Prison, da paleta da sala: o tom do tema so puxa de leve.
        SpriteRenderer sr = FormasDaSala.Desenho(pai, "Pedra", ArteGerada.PedraSolta(), Color.Lerp(Color.white, cor, 0.3f),
            posicaoLocal, Vector2.one * 1.05f, -1);
        GameObject obj = sr.gameObject;
        obj.layer = Sala.CamadaDeParede;

        BoxCollider2D caixa = obj.AddComponent<BoxCollider2D>();
        caixa.size = Vector2.one / 1.05f;   // um ladrilho inteiro: duas pedras coladas nao deixam fresta


        Vida vida = obj.AddComponent<Vida>();
        vida.Configurar(1f, 0f, true, 0f, false);

        return obj.AddComponent<Pedra>();
    }
}
