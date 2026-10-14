using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O raio que pula (o Cajado do Trovao, o Cetro do Abismo, ver <see cref="DadosDaArma.encadeia"/>): do
/// inimigo acertado, salta pro vivo mais perto que ainda nao levou (ate <see cref="Alcance"/>), um depois
/// do outro, cada salto um pouco mais fraco. Cada salto e um risco torto e brilhante que some rapido.
/// </summary>
public class Raio : MonoBehaviour
{
    /// <summary>Distancia maxima de um salto, em unidades.</summary>
    public const float Alcance = 4.5f;

    private static Sprite branco;

    /// <summary>Comeca a corrente a partir de <paramref name="primeiro"/> (que ja levou o golpe do tiro).</summary>
    public static void Encadear(Vida primeiro, float dano, int saltos, GameObject dono, Lado lado, Color cor)
    {
        if (primeiro == null || saltos <= 0 || dano <= 0f)
            return;

        Raio r = new GameObject("Raio").AddComponent<Raio>();
        r.StartCoroutine(r.Pular(primeiro, dano, saltos, dono, lado, Color.Lerp(cor, Color.white, 0.4f)));
    }

    private IEnumerator Pular(Vida de, float dano, int saltos, GameObject dono, Lado lado, Color cor)
    {
        HashSet<Vida> ja = new HashSet<Vida> { de };
        Vector2 ponto = de.transform.position;

        for (int i = 0; i < saltos; i++)
        {
            Vida alvo = MaisPerto(ponto, ja, lado);

            if (alvo == null)
                break;

            Vector2 ate = alvo.transform.position;
            Desenhar(ponto, ate, cor);
            ja.Add(alvo);
            Vector2 rumo = ate - ponto;
            alvo.ReceberDano(new Dano(dano, rumo.sqrMagnitude > 0.001f ? rumo.normalized : Vector2.up, 1.5f, dono));
            Sons.Tocar(Som.Raio, 0.5f);
            ponto = ate;
            dano *= 0.85f;
            yield return new WaitForSeconds(0.07f);
        }

        yield return new WaitForSeconds(0.2f);
        Destroy(gameObject);
    }

    private static Vida MaisPerto(Vector2 de, HashSet<Vida> ja, Lado lado)
    {
        Vida melhor = null;
        float menor = Alcance * Alcance;

        foreach (Vida v in GeradorDoAndar.Vivos)
        {
            if (v == null || v.Morto || ja.Contains(v) || v.Lado == lado)
                continue;

            float d = ((Vector2)v.transform.position - de).sqrMagnitude;

            if (d < menor)
            {
                menor = d;
                melhor = v;
            }
        }

        return melhor;
    }

    // Um risco torto de tres pedacos (com um mais fino e claro por cima), que apaga em 0,18 s.
    private void Desenhar(Vector2 de, Vector2 ate, Color cor)
    {
        Vector2 lado = Vector2.Perpendicular((ate - de).normalized);
        Vector2 a = Vector2.Lerp(de, ate, 0.33f) + lado * Random.Range(-0.35f, 0.35f);
        Vector2 b = Vector2.Lerp(de, ate, 0.66f) + lado * Random.Range(-0.35f, 0.35f);
        Vector2[] pontos = { de, a, b, ate };

        for (int i = 0; i < 3; i++)
        {
            Risco(pontos[i], pontos[i + 1], 0.12f, new Color(cor.r, cor.g, cor.b, 0.85f));
            Risco(pontos[i], pontos[i + 1], 0.05f, Color.white);
        }

        EfeitoDeFolha faisca = EfeitoDeFolha.Tocar(ArteDoAntigo.ExplosaoPequena, ate, 30f, 25);

        if (faisca != null)
            faisca.transform.localScale = Vector3.one * 0.4f;
    }

    private void Risco(Vector2 de, Vector2 ate, float grossura, Color cor)
    {
        if (branco == null)
        {
            Texture2D t = new Texture2D(2, 2) { filterMode = FilterMode.Point };
            t.SetPixels32(new[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) });
            t.Apply();
            branco = Sprite.Create(t, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        }

        GameObject obj = new GameObject("Risco");
        obj.transform.SetParent(transform, false);
        Vector2 meio = (de + ate) * 0.5f;
        Vector2 d = ate - de;
        obj.transform.position = meio;
        obj.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        obj.transform.localScale = new Vector3(d.magnitude, grossura, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = branco;
        sr.color = cor;
        sr.sortingOrder = 26;
        Iluminacao.Brilhar(sr);
        StartCoroutine(Apagar(sr, 0.18f));
    }

    private static IEnumerator Apagar(SpriteRenderer sr, float tempo)
    {
        Color cor = sr.color;

        for (float t = 0f; t < tempo && sr != null; t += Time.deltaTime)
        {
            sr.color = new Color(cor.r, cor.g, cor.b, cor.a * (1f - t / tempo));
            yield return null;
        }

        if (sr != null)
            Destroy(sr.gameObject);
    }
}
