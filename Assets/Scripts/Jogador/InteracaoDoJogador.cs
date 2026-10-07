using UnityEngine;

/// <summary>
/// Acha a coisa usavel mais perto (<see cref="Interativo"/>: arma no chao, bau), mostra a dica em
/// cima dela ("E: pegar Tomo das Brasas") e usa quando o jogador aperta interagir.
/// </summary>
[DisallowMultipleComponent]
public class InteracaoDoJogador : MonoBehaviour
{
    private ControlesDoJogador controles;
    private Vida vida;
    private Interativo perto;
    private Camera cam;
    private GUIStyle estilo;

    private void Awake()
    {
        controles = GetComponent<ControlesDoJogador>();
        vida = GetComponent<Vida>();
    }

    private void Update()
    {
        perto = null;

        if (controles == null || !controles.enabled || (vida != null && vida.Morto))
            return;

        float menor = float.MaxValue;

        foreach (Interativo coisa in Interativo.Todos)
        {
            if (coisa == null || !coisa.Disponivel)
                continue;

            float d = Vector2.Distance(coisa.transform.position, transform.position);

            if (d <= coisa.Alcance && d < menor)
            {
                menor = d;
                perto = coisa;
            }
        }

        if (perto != null && controles.Interagiu)
            perto.Usar(gameObject);
    }

    private void OnGUI()
    {
        if (perto == null)
            return;

        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        if (estilo == null)
        {
            estilo = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter, fontSize = Mathf.Max(14, Screen.height / 40) };
        }

        Vector3 naTela = cam.WorldToScreenPoint(perto.transform.position + Vector3.up * 0.9f);
        Rect lugar = new Rect(naTela.x - 150f, Screen.height - naTela.y - 30f, 300f, 30f);
        GUI.color = Color.white;
        GUI.Label(lugar, "E: " + perto.Dica, estilo);
    }
}
