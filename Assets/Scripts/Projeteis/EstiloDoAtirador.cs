using UnityEngine;

/// <summary>
/// Marca no inimigo qual estilo de tiro ele usa, passando por cima do que o nome dele diria
/// (<see cref="EstilosDeTiro.DoAtirador"/>). Pra inimigo novo ou variacao que atira diferente.
/// </summary>
[DisallowMultipleComponent]
public class EstiloDoAtirador : MonoBehaviour
{
    [SerializeField] private EstiloDeTiro estilo = EstiloDeTiro.Gema;

    public EstiloDeTiro Estilo => estilo;

    public static void Marcar(GameObject inimigo, EstiloDeTiro novo)
    {
        EstiloDoAtirador marca = inimigo.GetComponent<EstiloDoAtirador>();

        if (marca == null)
            marca = inimigo.AddComponent<EstiloDoAtirador>();

        marca.estilo = novo;
    }
}
