using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Uma sala do andar ja montada no mundo: chao, paredes e as portas. Quem monta e o
/// <see cref="Andar"/>; este componente so guarda as pecas e sabe trancar as portas.
///
/// Trancar e o gancho pra sala com inimigos: ao entrar, tranque; quando o ultimo inimigo
/// morrer, destranque. Igual ao Isaac, a porta fechada vira parede de verdade (collider
/// solido) e muda de cara.
/// </summary>
[DisallowMultipleComponent]
public class SalaNoMundo : MonoBehaviour
{
    /// <summary>A porta de um lado: a tranca (collider solido) e o desenho.</summary>
    private class Porta
    {
        public Direcao lado;
        public BoxCollider2D tranca;
        public SpriteRenderer desenho;
        public Color corAberta;
    }

    [SerializeField] private Color corDaPortaTrancada = new Color(0.12f, 0.1f, 0.1f);

    private readonly List<Porta> portas = new List<Porta>();

    public SalaDoAndar Sala { get; private set; }

    public bool Trancada { get; private set; }

    /// <summary>Tamanho do chao, sem as paredes.</summary>
    public Vector2 Interior { get; private set; }

    public Vector2 Centro => transform.position;

    public void Iniciar(SalaDoAndar sala, Vector2 interior)
    {
        Sala = sala;
        Interior = interior;
    }

    public void RegistrarPorta(Direcao lado, BoxCollider2D tranca, SpriteRenderer desenho)
    {
        portas.Add(new Porta { lado = lado, tranca = tranca, desenho = desenho, corAberta = desenho.color });
    }

    /// <summary>Fecha ou abre todas as portas da sala.</summary>
    public void Trancar(bool trancar)
    {
        Trancada = trancar;

        foreach (Porta porta in portas)
        {
            porta.tranca.enabled = trancar;
            porta.desenho.color = trancar ? corDaPortaTrancada : porta.corAberta;
        }
    }

    /// <summary>Ponto dentro da sala, colado na porta do lado pedido. E onde o jogador aparece ao entrar.</summary>
    public Vector2 PontoNaPorta(Direcao lado, float recuo)
    {
        Vector2 para = new Vector2(Direcoes.Dx(lado), Direcoes.Dy(lado));
        Vector2 meio = Interior * 0.5f;
        float distancia = (para.x != 0f ? meio.x : meio.y) - recuo;
        return Centro + para * distancia;
    }

    /// <summary>O ponto esta no chao desta sala (nao conta paredes nem vao da porta).</summary>
    public bool Contem(Vector2 ponto)
    {
        Vector2 d = ponto - Centro;
        return Mathf.Abs(d.x) <= Interior.x * 0.5f && Mathf.Abs(d.y) <= Interior.y * 0.5f;
    }
}
