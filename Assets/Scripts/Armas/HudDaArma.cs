using UnityEngine;

/// <summary>
/// No canto de baixo da tela: a arma na mao e a municao (pente / reserva, ou infinita), a recarga
/// e a outra arma pra trocar. Provisorio: a interface de verdade vem na etapa 7.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ArmaDoJogador))]
public class HudDaArma : MonoBehaviour
{
    private ArmaDoJogador armas;
    private Vida vida;
    private GUIStyle grande;
    private GUIStyle pequeno;

    private void Awake()
    {
        armas = GetComponent<ArmaDoJogador>();
        vida = GetComponent<Vida>();
    }

    private void OnGUI()
    {
        ArmaCarregada atual = armas.Atual;

        if (atual == null || (vida != null && vida.Morto))
            return;

        if (grande == null)
        {
            grande = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(16, Screen.height / 30), alignment = TextAnchor.LowerLeft };
            pequeno = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(12, Screen.height / 45), alignment = TextAnchor.LowerLeft };
        }

        string municao = atual.Infinita ? "infinita" : $"{atual.NoPente} / {atual.Reserva}";

        if (armas.Recarregando)
            municao = "recarregando " + new string('|', Mathf.RoundToInt(armas.ProgressoDaRecarga * 10f));
        else if (atual.Vazia)
            municao = "sem municao";

        float altura = Screen.height;
        GUI.color = atual.Vazia ? new Color(1f, 0.5f, 0.5f) : Color.white;
        GUI.Label(new Rect(16f, altura - 70f, Screen.width * 0.6f, 40f), atual.Dados.nome + "   " + municao, grande);

        if (armas.Outra != null)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.7f);
            GUI.Label(new Rect(16f, altura - 36f, Screen.width * 0.6f, 30f), "Q: " + armas.Outra.Dados.nome, pequeno);
        }
    }
}
