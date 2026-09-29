using UnityEngine;

public enum TipoDeObstaculo
{
    Pedra,
    Espinhos
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
        SpriteRenderer sr = FormasDaSala.Desenho(pai, "Pedra", FormasDaSala.Quadrado(), cor, posicaoLocal, Vector2.one * 0.9f, -1);
        GameObject obj = sr.gameObject;
        obj.layer = Sala.CamadaDeParede;

        BoxCollider2D caixa = obj.AddComponent<BoxCollider2D>();
        caixa.size = Vector2.one;   // um ladrilho inteiro: duas pedras coladas nao deixam fresta

        // Topo mais claro: da volume sem precisar de arte.
        FormasDaSala.Desenho(obj.transform, "Topo", FormasDaSala.Quadrado(), Color.Lerp(cor, Color.white, 0.18f),
            new Vector2(0f, 0.1f), new Vector2(0.7f, 0.55f), 0);

        Vida vida = obj.AddComponent<Vida>();
        vida.Configurar(1f, 0f, true, 0f, false);

        return obj.AddComponent<Pedra>();
    }
}
