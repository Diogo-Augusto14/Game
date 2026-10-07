using System.Collections;
using UnityEngine;

/// <summary>O levantado sai do chao crescendo, parado (o InimigoAtirador fica desligado ate acabar).</summary>
public class SaindoDoChao : MonoBehaviour
{
    public void Comecar(float segundos) => StartCoroutine(Crescer(segundos));

    private IEnumerator Crescer(float segundos)
    {
        InimigoAtirador inimigo = GetComponent<InimigoAtirador>();
        Rigidbody2D corpo = GetComponent<Rigidbody2D>();
        Vector3 tamanho = transform.localScale;

        if (inimigo != null)
            inimigo.enabled = false;

        if (corpo != null)
            corpo.linearVelocity = Vector2.zero;

        for (float t = 0f; t < 1f; t += Time.deltaTime / segundos)
        {
            transform.localScale = new Vector3(tamanho.x, tamanho.y * Mathf.Lerp(0.2f, 1f, t), tamanho.z);
            yield return null;
        }

        transform.localScale = tamanho;

        if (inimigo != null)
            inimigo.enabled = true;

        Destroy(this);
    }
}
