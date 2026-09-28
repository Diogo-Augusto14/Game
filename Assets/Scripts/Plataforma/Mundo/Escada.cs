using UnityEngine;

/// <summary>
/// Zona de escada. Um trigger alto e estreito: quando o boneco esta dentro dela e
/// aperta pra cima, o Movimento entra no estado Escada.
///
/// O componente nao faz nada por conta propria — so responde "onde e o meio do degrau"
/// e "onde e o topo", que e o que o Movimento precisa pra centralizar e pra saber
/// quando a escada acabou.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class Escada : MonoBehaviour
{
    private Collider2D area;

    private Collider2D Area
    {
        get
        {
            if (area == null)
                area = GetComponent<Collider2D>();

            return area;
        }
    }

    /// <summary>X do meio da escada: o boneco sobe centralizado nela.</summary>
    public float CentroX => Area.bounds.center.x;

    /// <summary>Y do topo: chegando aqui subindo, ele sai pro chao de cima.</summary>
    public float TopoY => Area.bounds.max.y;

    /// <summary>Y da base.</summary>
    public float BaseY => Area.bounds.min.y;

    private void Awake()
    {
        if (!Area.isTrigger)
        {
            // Sem trigger a escada viraria uma parede e o boneco nem entraria nela.
            Area.isTrigger = true;
            Debug.LogWarning($"[Escada] {name}: marquei Is Trigger no collider (escada precisa ser trigger).", this);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Collider2D c = GetComponent<Collider2D>();

        if (c == null)
            return;

        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.35f);
        Gizmos.DrawCube(c.bounds.center, c.bounds.size);

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        Gizmos.DrawLine(new Vector3(c.bounds.min.x, c.bounds.max.y), new Vector3(c.bounds.max.x, c.bounds.max.y));
    }
#endif
}
