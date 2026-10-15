using UnityEngine;

/// <summary>
/// Uma planta caida (<see cref="Desbloqueios"/>): o pergaminho azul (Resources/Cenario/Planta) boiando, com
/// uma luz azul. Encostou, aprendeu: a arma ou o item da planta passa a sair nas partidas, pra sempre.
/// Monte com <see cref="Criar"/> (ou <see cref="Desbloqueios.Soltar"/>, que sorteia a do mundo).
/// </summary>
public class PlantaNoChao : MonoBehaviour
{
    private const float Alcance = 0.8f;

    private Desbloqueios.Planta planta;
    private SpriteRenderer desenho;
    private Transform jogador;
    private float pegavelEm;
    private float fase;

    public static PlantaNoChao Criar(Desbloqueios.Planta planta, Vector2 onde)
    {
        if (planta == null)
            return null;

        GameObject obj = new GameObject("Planta: " + planta.Id);
        obj.transform.SetParent(GeradorDoAndar.Raiz, true);
        obj.transform.position = onde;

        PlantaNoChao p = obj.AddComponent<PlantaNoChao>();
        p.planta = planta;
        p.desenho = obj.AddComponent<SpriteRenderer>();
        p.desenho.sprite = ArteDoAntigo.Inteira("Cenario/Planta", 26f);
        p.desenho.sortingOrder = 12;
        Iluminacao.Brilhar(p.desenho);
        Iluminacao.Luz(obj.transform, Vector2.zero, new Color(0.45f, 0.65f, 1f), 2.2f, 0.9f, 0.05f);
        p.pegavelEm = Time.time + 0.5f;
        p.fase = Random.value * 10f;
        Desbloqueios.NoChao(planta, true);
        return p;
    }

    private void Update()
    {
        desenho.transform.localPosition = Vector3.up * (0.1f * Mathf.Sin((Time.time + fase) * 2.2f));

        if (Time.time < pegavelEm)
            return;

        if (jogador == null)
        {
            GameObject j = GameObject.FindWithTag("Player");
            jogador = j != null ? j.transform : null;

            if (jogador == null)
                return;
        }

        if (Vector2.Distance(jogador.position, transform.position) > Alcance)
            return;

        Desbloqueios.Aprender(planta);
        Sons.Tocar(Som.Segredo, 0.9f, 0f);
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 0.8f, Idioma.F("Planta: {0}", Idioma.T(planta.Nome)),
                               new Color(0.6f, 0.8f, 1f), 2.2f);
        EfeitoDeFolha.Tocar(ArteDoAntigo.ExplosaoPequena, transform.position, 24f, 25);
        DicasDaPrimeiraVez.Mostrar("planta");
        Destroy(gameObject);
    }

    private void OnDestroy() => Desbloqueios.NoChao(planta, false);
}
