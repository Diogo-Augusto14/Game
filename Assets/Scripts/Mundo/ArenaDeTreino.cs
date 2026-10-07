using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enquanto o jogo nao tem salas: mantem alguns inimigos por perto pra treinar no chao aberto.
/// Chama um de cada vez, a uma certa distancia do jogador, ate ter a quantidade; quando um morre,
/// chama outro depois de um tempo. Com o jogador morto, para de chamar.
/// </summary>
[DisallowMultipleComponent]
public class ArenaDeTreino : MonoBehaviour
{
    [Tooltip("O prefab do inimigo que aparece")]
    [SerializeField] private GameObject inimigo;

    [Tooltip("Quantos ficam vivos ao mesmo tempo")]
    [SerializeField, Min(0)] private int quantos = 3;

    [Tooltip("Segundos entre um inimigo chegar e o proximo")]
    [SerializeField, Min(0f)] private float intervalo = 2.5f;

    [Tooltip("Segundos antes do primeiro")]
    [SerializeField, Min(0f)] private float esperaInicial = 1.5f;

    [Tooltip("Distancia do jogador onde eles aparecem, em unidades (a tela tem uns 10 de altura)")]
    [SerializeField, Min(0f)] private float distanciaMinima = 7f;

    [SerializeField, Min(0f)] private float distanciaMaxima = 10f;

    private readonly List<Vida> vivos = new List<Vida>();
    private Transform jogador;
    private Vida vidaDoJogador;
    private float proximo;

    private void Start()
    {
        GameObject obj = GameObject.FindWithTag("Player");

        if (obj != null)
        {
            jogador = obj.transform;
            vidaDoJogador = obj.GetComponent<Vida>();
        }

        proximo = Time.time + esperaInicial;
    }

    private void Update()
    {
        if (inimigo == null || jogador == null || (vidaDoJogador != null && vidaDoJogador.Morto))
            return;

        vivos.RemoveAll(v => v == null || v.Morto);

        // Cheio: o relogio so comeca a contar quando alguem morre.
        if (vivos.Count >= quantos)
        {
            proximo = Time.time + intervalo;
            return;
        }

        if (Time.time < proximo)
            return;

        proximo = Time.time + intervalo;
        Chamar();
    }

    private void Chamar()
    {
        // Algumas tentativas de achar um lugar vazio (sem o boneco ou outro inimigo em cima).
        for (int tentativa = 0; tentativa < 8; tentativa++)
        {
            Vector2 lugar = (Vector2)jogador.position + Random.insideUnitCircle.normalized * Random.Range(distanciaMinima, distanciaMaxima);

            if (Physics2D.OverlapCircle(lugar, 0.6f) != null)
                continue;

            GameObject novo = Instantiate(inimigo, lugar, Quaternion.identity);

            if (novo.TryGetComponent(out Vida vida))
                vivos.Add(vida);

            return;
        }
    }
}
