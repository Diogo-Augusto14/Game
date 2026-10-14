using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A parede de ilusao que esconde a passagem pra sala secreta (<see cref="TipoDeSala.Secreta"/>): e
/// desenhada igual as outras, mas nao segura ninguem (o <see cref="Pedreiro"/> faz os colisores com a
/// passagem aberta). Quem anda contra ela entra; ai ela se revela: um sopro de poeira, o som do segredo,
/// e o <see cref="GeradorDoAndar"/> redesenha aquele pedaco como corredor (o minimapa passa a mostrar a
/// sala). O tiro tambem atravessa: atirar na parede e um jeito de achar.
///
/// Pistas, so as que a parede tem por natureza: nada de tocha nem mobilia na frente dela. Monte com
/// <see cref="Criar"/>.
/// </summary>
public class ParedeSecreta : MonoBehaviour
{
    private HashSet<Vector2Int> passagem;
    private List<Vector2> boca;
    private System.Action aoRevelar;
    private Transform jogador;
    private bool revelada;

    /// <param name="passagem">As celulas da passagem (o corredor ate a sala secreta).</param>
    /// <param name="dona">A sala de onde se chega na passagem.</param>
    /// <param name="aoRevelar">Chamado uma vez, quando o jogador entra na parede.</param>
    public static ParedeSecreta Criar(HashSet<Vector2Int> passagem, SalaDaPlanta dona, Transform pai, System.Action aoRevelar)
    {
        if (passagem.Count == 0)
            return null;

        GameObject obj = new GameObject("Parede secreta");
        obj.transform.SetParent(pai, false);

        ParedeSecreta parede = obj.AddComponent<ParedeSecreta>();
        parede.passagem = passagem;
        parede.aoRevelar = aoRevelar;

        // A boca: as celulas da passagem encostadas na sala (a poeira sai dali).
        parede.boca = new List<Vector2>();
        Vector2Int[] lados = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int c in passagem)
        {
            foreach (Vector2Int d in lados)
            {
                if (dona.celulas.Contains(c - d))
                {
                    parede.boca.Add(c);
                    break;
                }
            }
        }

        return parede;
    }

    private void Update()
    {
        if (revelada)
            return;

        if (jogador == null)
        {
            GameObject j = GameObject.FindWithTag("Player");

            if (j == null)
                return;

            jogador = j.transform;
        }

        if (passagem.Contains(MapaDeCaminhos.Celula(jogador.position)))
            Revelar();
    }

    private void Revelar()
    {
        revelada = true;
        Sons.Tocar(Som.Segredo, 0.9f);
        CameraDoJogo.Tremer(0.1f, 0.2f);

        Texture2D poeira = Resources.Load<Texture2D>("Efeitos/Poeira");

        if (poeira != null)
        {
            foreach (Vector2 c in boca)
                EfeitoDeFolha.Tocar(poeira, new Vector2Int(64, 64), 20f, c + Random.insideUnitCircle * 0.3f, 18f);
        }

        aoRevelar?.Invoke();
        Destroy(gameObject);
    }
}
