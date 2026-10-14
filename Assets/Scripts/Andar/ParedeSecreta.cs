using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A parede de ilusao que esconde a passagem pra sala secreta (<see cref="TipoDeSala.Secreta"/>): e
/// desenhada igual as outras, mas nao segura ninguem (o <see cref="Pedreiro"/> faz os colisores com a
/// passagem aberta). Quem anda contra ela entra; ai ela se revela: um sopro de poeira, o som do segredo,
/// e o <see cref="GeradorDoAndar"/> redesenha aquele pedaco como corredor (o minimapa passa a mostrar a
/// sala). O tiro tambem atravessa: atirar na parede e um jeito de achar.
///
/// A pista: com o jogador perto (ate <see cref="DistanciaDaPista"/>), de tempos em tempos cai um fiozinho
/// de poeira da boca da passagem (na 1.19.0; antes nao tinha pista nenhuma e quase ninguem achava). Nada
/// de tocha nem mobilia na frente dela. Monte com <see cref="Criar"/>.
/// </summary>
public class ParedeSecreta : MonoBehaviour
{
    private HashSet<Vector2Int> passagem;
    private List<Vector2> boca;
    private System.Action aoRevelar;
    private Transform jogador;
    private bool revelada;
    private float proximaPista;
    private Texture2D poeira;

    /// <summary>Ate esta distancia (unidades) da parede, a poeira cai como pista.</summary>
    public const float DistanciaDaPista = 6f;

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
        {
            Revelar();
            return;
        }

        if (Time.time >= proximaPista)
            DarPista();
    }

    // Um fiozinho de poeira caindo de um ponto da boca, pequeno e meio apagado: so quem olha percebe.
    private void DarPista()
    {
        proximaPista = Time.time + Random.Range(1.6f, 3f);

        if (boca.Count == 0)
            return;

        Vector2 c = boca[Random.Range(0, boca.Count)];

        if (Vector2.Distance(c, jogador.position) > DistanciaDaPista)
            return;

        if (poeira == null)
            poeira = Resources.Load<Texture2D>("Efeitos/Poeira");

        if (poeira == null)
            return;

        EfeitoDeFolha fio = EfeitoDeFolha.Tocar(poeira, new Vector2Int(64, 64), 20f, c + new Vector2(Random.Range(-0.35f, 0.35f), 0.4f), 10f);

        if (fio != null)
        {
            fio.transform.localScale = Vector3.one * 0.45f;

            if (fio.TryGetComponent(out SpriteRenderer sr))
                sr.color = new Color(1f, 0.95f, 0.85f, 0.7f);
        }
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
