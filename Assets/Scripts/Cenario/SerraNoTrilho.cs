using System.Collections.Generic;
using UnityEngine;

/// <summary>A serra que gira e corre num trilho, de uma ponta a outra (veio do jogo antigo).</summary>
public class SerraNoTrilho : MonoBehaviour
{
    private Vector2 de;
    private Vector2 ate;
    private float velocidade = 2.6f;
    private float fase;
    private Transform serra;
    private readonly Dictionary<Vida, float> proximo = new Dictionary<Vida, float>();

    public static SerraNoTrilho Criar(Vector2 de, Vector2 ate, Transform pai)
    {
        GameObject obj = new GameObject("Serra no trilho");
        obj.transform.SetParent(pai, false);

        // O trilho: um pedaco a cada celula, no chao.
        Sprite[] trilhos = ArteDoAntigo.Trilhos;
        int pedacos = Mathf.RoundToInt(Vector2.Distance(de, ate)) + 1;
        bool deitado = Mathf.Abs(ate.x - de.x) >= Mathf.Abs(ate.y - de.y);

        // A folha tem as pecas deitadas (0 o meio, 2 e 3 as pontas) e em pe (1 o meio, 4 e 5 as pontas):
        // uma por celula, sem girar, fechando nas pontas.
        Vector2 eixo = ate - de;

        for (int i = 0; i < pedacos && trilhos.Length >= 6; i++)
        {
            int peca = i == 0 ? (deitado ? (eixo.x > 0f ? 2 : 3) : (eixo.y > 0f ? 5 : 4))
                     : i == pedacos - 1 ? (deitado ? (eixo.x > 0f ? 3 : 2) : (eixo.y > 0f ? 4 : 5))
                     : (deitado ? 0 : 1);

            GameObject t = new GameObject("Trilho");
            t.transform.SetParent(obj.transform, false);
            t.transform.position = Vector2.Lerp(de, ate, pedacos <= 1 ? 0f : (float)i / (pedacos - 1));
            SpriteRenderer sr = t.AddComponent<SpriteRenderer>();
            sr.sprite = trilhos[peca];
            sr.sortingOrder = Pedreiro.OrdemDosEnfeites + 1;
        }

        GameObject lamina = new GameObject("Serra");
        lamina.transform.SetParent(obj.transform, false);
        SpriteRenderer desenho = lamina.AddComponent<SpriteRenderer>();
        desenho.sprite = ArteDoAntigo.Serra;
        desenho.sortingOrder = 9;

        SerraNoTrilho s = obj.AddComponent<SerraNoTrilho>();
        s.de = de;
        s.ate = ate;
        s.serra = lamina.transform;
        s.fase = Random.value * 10f;
        return s;
    }

    private void Update()
    {
        float comprimento = Mathf.Max(0.1f, Vector2.Distance(de, ate));
        float t = Mathf.PingPong((Time.time + fase) * velocidade / comprimento, 1f);
        serra.position = Vector2.Lerp(de, ate, Mathf.SmoothStep(0f, 1f, t));
        serra.Rotate(0f, 0f, -720f * Time.deltaTime);
        GolpeDeArmadilha.Ferir(serra.position, 0.55f, gameObject, 8f, proximo);
    }
}
