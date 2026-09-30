using UnityEngine;

/// <summary>
/// Inimigo campeao, como no Isaac: o mesmo bicho, mas com uma cor que entrega o que ele
/// tem de diferente, e sempre solta um premio ao morrer. Aparece mais a cada fase
/// (<see cref="DificuldadeDaFase.ChanceDeCampeao"/>).
///   Vermelho -> o dobro da vida
///   Amarelo  -> bem mais rapido, um pouco mais de vida
///   Roxo     -> mais vida e mais rapido, so do mundo 3 em diante
/// A cor tinge a arte do pacote; o flash de dano e a sala acordando respeitam a cor nova.
/// </summary>
[DisallowMultipleComponent]
public class Campeao : MonoBehaviour
{
    public enum Tipo { Forte, Rapido, Sombrio }

    public Tipo Qual { get; private set; }

    /// <summary>Transforma o inimigo em campeao. Chamar logo depois de criar, antes da sala acordar.</summary>
    public static Campeao Aplicar(InimigoDeSala inimigo, int mundo)
    {
        if (inimigo == null || inimigo.GetComponent<Campeao>() != null)
            return null;

        int tipos = mundo >= 3 ? 3 : 2;
        Campeao campeao = inimigo.gameObject.AddComponent<Campeao>();
        campeao.Qual = (Tipo)Random.Range(0, tipos);

        Vida vida = inimigo.Vida;

        switch (campeao.Qual)
        {
            case Tipo.Forte:
                vida?.AumentarVidaMaxima(vida.VidaMaxima);
                inimigo.Tingir(new Color(1f, 0.45f, 0.45f));
                break;

            case Tipo.Rapido:
                vida?.AumentarVidaMaxima(vida.VidaMaxima * 0.3f);
                inimigo.DefinirVelocidade(inimigo.Velocidade * 1.35f);
                inimigo.Tingir(new Color(1f, 0.9f, 0.35f));
                break;

            default:
                vida?.AumentarVidaMaxima(vida.VidaMaxima * 0.6f);
                inimigo.DefinirVelocidade(inimigo.Velocidade * 1.2f);
                inimigo.Tingir(new Color(0.75f, 0.5f, 1f));
                break;
        }

        inimigo.name += " (campeao)";
        return campeao;
    }
}
