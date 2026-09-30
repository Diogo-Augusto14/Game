using UnityEngine;

/// <summary>
/// O efeito do Prego Enferrujado: oito espinhos (o prego da folha de objetos) saem do
/// jogador em volta e somem. So o desenho; o dano e dado pelo <see cref="EfeitosDosItens"/>.
/// </summary>
public class EspinhosQueSaem : MonoBehaviour
{
    private const int Quantos = 8;
    private const float Duracao = 0.3f;

    private Transform[] espinhos;
    private SpriteRenderer[] desenhos;
    private float raio;
    private float nasceu;

    public static void Criar(Vector2 centro, float raio)
    {
        Sprite prego = ArteImportada.Objeto(0, 3);

        if (prego == null)
            return;

        GameObject obj = new GameObject("Espinhos");
        obj.transform.position = centro;

        EspinhosQueSaem e = obj.AddComponent<EspinhosQueSaem>();
        e.raio = raio;
        e.nasceu = Time.time;
        e.espinhos = new Transform[Quantos];
        e.desenhos = new SpriteRenderer[Quantos];

        for (int i = 0; i < Quantos; i++)
        {
            float angulo = i * 360f / Quantos;
            GameObject um = new GameObject("Espinho");
            um.transform.SetParent(obj.transform, false);
            // O prego aponta pra baixo (-90 graus): gira pra apontar pra fora.
            um.transform.localRotation = Quaternion.Euler(0f, 0f, angulo + 90f);
            um.transform.localScale = Vector3.one * 0.8f;

            SpriteRenderer sr = um.AddComponent<SpriteRenderer>();
            sr.sprite = prego;
            sr.sortingOrder = 21;

            e.espinhos[i] = um.transform;
            e.desenhos[i] = sr;
        }

        e.Update();
    }

    private void Update()
    {
        float t = Mathf.Clamp01((Time.time - nasceu) / Duracao);

        for (int i = 0; i < Quantos; i++)
        {
            float angulo = i * Mathf.PI * 2f / Quantos;
            Vector2 lado = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
            espinhos[i].localPosition = lado * Mathf.Lerp(0.3f, raio, t);
            desenhos[i].color = new Color(1f, 1f, 1f, 1f - t);
        }

        if (t >= 1f)
            Destroy(gameObject);
    }
}
