using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cena de teste da sala: monta uma sala com quatro portas, poe inimigos, enquadra a
/// camera e, quando o jogador sai por uma porta, troca por uma sala nova (com mais
/// inimigos) e poe o jogador entrando pela porta oposta. E um "andar infinito" de uma
/// sala so — o gerador de andar de verdade substitui isto.
///
/// Se a cena nao tiver objeto com a tag Player, cria um <see cref="JogadorDeTeste"/>.
///
/// Pra usar: <c>Tools ▸ Jogo ▸ Sala ▸ Criar cena de teste</c>, ou um objeto vazio com este
/// componente numa cena sem o Bootstrap de plataforma ligado.
/// </summary>
[DisallowMultipleComponent]
public class DemoDaSala : MonoBehaviour
{
    [Header("Inimigos")]
    [SerializeField, Min(0)] private int perseguidoresNaPrimeira = 2;

    [SerializeField, Min(0)] private int atiradoresNaPrimeira = 1;

    [Tooltip("A cada sala nova, mais este tanto de inimigos (alternando o tipo)")]
    [SerializeField, Min(0)] private int inimigosAMaisPorSala = 1;

    [SerializeField, Min(1)] private int maximoDeInimigos = 7;

    [Tooltip("Inimigo nao nasce mais perto que isto do jogador")]
    [SerializeField, Min(0f)] private float distanciaDoJogador = 3.5f;

    [Header("Camera")]
    [SerializeField] private bool ajustarCamera = true;

    [SerializeField] private Color fundo = new Color(0.05f, 0.04f, 0.04f);

    private Sala sala;
    private Transform jogador;
    private Vida vidaDoJogador;
    private int numeroDaSala;

    private void Start()
    {
        GameObject existente = GameObject.FindWithTag("Player");

        jogador = existente != null
            ? existente.transform
            : JogadorDeTeste.Criar(transform.position).transform;

        vidaDoJogador = jogador.GetComponentInParent<Vida>();

        NovaSala(null);

        if (ajustarCamera)
            EnquadrarCamera();
    }

    private void NovaSala(LadoDaPorta? entrouPor)
    {
        if (sala != null)
            Destroy(sala.gameObject);

        numeroDaSala++;
        sala = Sala.Criar($"Sala {numeroDaSala}", transform.position);

        foreach (Porta porta in sala.Portas)
            porta.AoAtravessar.AddListener(AoSairPelaPorta);

        Vector2 chegada = entrouPor.HasValue
            ? sala.PortaEm(entrouPor.Value).PontoDeChegada
            : (Vector2)transform.position;

        PorJogadorEm(chegada);
        PovoarSala(chegada);
    }

    private void PovoarSala(Vector2 chegada)
    {
        int extra = (numeroDaSala - 1) * inimigosAMaisPorSala;
        int perseguidores = perseguidoresNaPrimeira + (extra + 1) / 2;
        int atiradores = atiradoresNaPrimeira + extra / 2;

        List<TipoDeInimigo> fila = new List<TipoDeInimigo>();

        for (int i = 0; i < perseguidores; i++) fila.Add(TipoDeInimigo.Perseguidor);
        for (int i = 0; i < atiradores; i++) fila.Add(TipoDeInimigo.Atirador);

        for (int i = 0; i < fila.Count && i < maximoDeInimigos; i++)
            sala.CriarInimigo(fila[i], PontoLongeDe(chegada));
    }

    /// <summary>Ponto livre da sala longe do jogador (tenta algumas vezes, depois aceita o que tiver).</summary>
    private Vector2 PontoLongeDe(Vector2 chegada)
    {
        Vector2 centro = sala.transform.position;
        Vector2 ponto = sala.PontoLivreAleatorio();

        for (int tentativa = 0; tentativa < 20; tentativa++)
        {
            if (Vector2.Distance(centro + ponto, chegada) >= distanciaDoJogador)
                break;

            ponto = sala.PontoLivreAleatorio();
        }

        return ponto;
    }

    private void AoSairPelaPorta(Porta porta)
    {
        // Sai por cima = chega na sala nova pela porta de baixo.
        NovaSala(porta.Lado.Oposto());
    }

    private void PorJogadorEm(Vector2 ponto)
    {
        Rigidbody2D rb = jogador.GetComponentInParent<Rigidbody2D>();

        if (rb != null)
        {
            rb.position = ponto;
            rb.linearVelocity = Vector2.zero;
        }

        jogador.position = ponto;
    }

    private void EnquadrarCamera()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            GameObject obj = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = obj.AddComponent<Camera>();
        }

        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = fundo;

        Vector2 total = sala.TamanhoTotal;
        float aspecto = Mathf.Max(0.1f, cam.aspect);
        cam.orthographicSize = Mathf.Max(total.y * 0.5f, total.x * 0.5f / aspecto) + 0.3f;

        Vector3 pos = sala.transform.position;
        cam.transform.position = new Vector3(pos.x, pos.y, -10f);
    }

    private void OnGUI()
    {
        if (sala == null)
            return;

        string vida = vidaDoJogador != null ? $"Vida {Mathf.CeilToInt(vidaDoJogador.VidaAtual)}/{Mathf.CeilToInt(vidaDoJogador.VidaMaxima)}   " : "";
        string estado = sala.Limpa ? "limpa — saia por uma porta" : $"inimigos: {sala.InimigosVivos}";

        GUI.Label(new Rect(12, 8, 600, 24), $"{vida}Sala {numeroDaSala}: {estado}");
        GUI.Label(new Rect(12, 28, 600, 24), "WASD anda · setas atiram");
    }
}
