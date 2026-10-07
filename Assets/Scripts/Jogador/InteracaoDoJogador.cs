using UnityEngine;

/// <summary>
/// Acha a coisa usavel mais perto (<see cref="Interativo"/>: arma no chao, bau) e usa quando o
/// jogador aperta interagir. A <see cref="TelaDoJogo"/> mostra a dica em cima dela ("E  abrir o bau").
/// </summary>
[DisallowMultipleComponent]
public class InteracaoDoJogador : MonoBehaviour
{
    private ControlesDoJogador controles;
    private Vida vida;
    private Interativo perto;

    /// <summary>A coisa usavel mais perto agora (nula se nao tem nenhuma no alcance).</summary>
    public Interativo Perto => perto;

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
}
