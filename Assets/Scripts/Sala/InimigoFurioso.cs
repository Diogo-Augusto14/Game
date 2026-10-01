using UnityEngine;

/// <summary>
/// Orc de elite: luta normal ate perder metade da vida; ai entra em FURIA (grita, fica vermelho)
/// e passa a correr muito mais e golpear mais rapido ate morrer. Melhor acabar com ele de uma vez
/// do que deixar ele pela metade.
/// </summary>
public class InimigoFurioso : InimigoDeGolpe
{
    [SerializeField, Range(0.1f, 0.9f)] private float vidaParaAFuria = 0.5f;

    [SerializeField, Min(1f)] private float pressaNaFuria = 1.6f;

    private bool furioso;

    protected override void Awake()
    {
        base.Awake();
        JeitoDeChegar = Aproximacao.Flanco;   // o elite nao vem de frente
    }

    protected override void Mover(Vector2 alvo, float distancia, float dt)
    {
        if (!furioso && vida != null && vida.VidaAtual <= vida.VidaMaxima * vidaParaAFuria)
            EntrarEmFuria();

        base.Mover(alvo, distancia, dt);
    }

    private void EntrarEmFuria()
    {
        furioso = true;
        velocidade *= pressaNaFuria;
        intervaloEntreGolpes *= 0.6f;
        tempoDePreparo *= 0.7f;
        Tingir(new Color(1f, 0.55f, 0.5f));
        Sons.Tocar(Som.Rugido, 0.7f);
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 0.8f, "Fúria!", new Color(1f, 0.4f, 0.3f));
    }
}
