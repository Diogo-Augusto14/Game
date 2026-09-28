using UnityEngine;
using UnityEngine.Events;

/// <summary>Em qual parede da sala a porta fica.</summary>
public enum LadoDaPorta
{
    Cima,
    Baixo,
    Esquerda,
    Direita
}

public static class LadoDaPortaExtensoes
{
    /// <summary>Pra onde a porta "aponta", saindo da sala.</summary>
    public static Vector2 Direcao(this LadoDaPorta lado)
    {
        switch (lado)
        {
            case LadoDaPorta.Cima: return Vector2.up;
            case LadoDaPorta.Baixo: return Vector2.down;
            case LadoDaPorta.Esquerda: return Vector2.left;
            default: return Vector2.right;
        }
    }

    /// <summary>A porta do outro lado (sair pela de Cima = entrar na proxima pela de Baixo).</summary>
    public static LadoDaPorta Oposto(this LadoDaPorta lado)
    {
        switch (lado)
        {
            case LadoDaPorta.Cima: return LadoDaPorta.Baixo;
            case LadoDaPorta.Baixo: return LadoDaPorta.Cima;
            case LadoDaPorta.Esquerda: return LadoDaPorta.Direita;
            default: return LadoDaPorta.Esquerda;
        }
    }

    public static bool Horizontal(this LadoDaPorta lado) => lado == LadoDaPorta.Cima || lado == LadoDaPorta.Baixo;
}

/// <summary>
/// Uma porta no vao da parede. Tres jeitos de estar:
///
///   Nao existe  -> vira parede (a sala nao tem vizinho desse lado)
///   Fechada     -> bloqueia, desenho vermelho (tem inimigo vivo na sala)
///   Aberta      -> passa; encostar no fundo do vao dispara <see cref="AoAtravessar"/>
///
/// A porta NAO decide quando abrir: quem manda e a Sala. E ela NAO troca de sala:
/// so avisa que o jogador passou. Quem gera o andar escuta o aviso e leva o jogador
/// (e a camera) pra sala vizinha.
/// </summary>
[DisallowMultipleComponent]
public class Porta : MonoBehaviour
{
    [SerializeField] private LadoDaPorta lado;

    [SerializeField] private bool existe = true;

    [SerializeField] private string tagDoJogador = "Player";

    [Header("Cores")]
    [SerializeField] private Color corFechada = new Color(0.55f, 0.12f, 0.1f);

    [SerializeField] private Color corAberta = new Color(0.05f, 0.04f, 0.04f);

    [SerializeField] private Color corDeParede = new Color(0.35f, 0.3f, 0.28f);

    [Tooltip("Disparado quando o jogador entra no vao com a porta aberta")]
    public UnityEvent<Porta> AoAtravessar = new UnityEvent<Porta>();

    private BoxCollider2D bloqueio;
    private BoxCollider2D passagem;
    private SpriteRenderer desenho;

    public LadoDaPorta Lado => lado;

    public bool Existe => existe;

    public bool Aberta { get; private set; } = true;

    /// <summary>A sala dona desta porta (pode ser null se a porta foi montada solta).</summary>
    public Sala Sala { get; private set; }

    /// <summary>
    /// Ponto logo DENTRO da sala, na frente da porta. E onde o jogador aparece quando
    /// chega por aqui vindo da sala vizinha.
    /// </summary>
    public Vector2 PontoDeChegada => (Vector2)transform.position - lado.Direcao() * 1.3f;

    /// <summary>
    /// Monta as pecas da porta. Chamado pela Sala; existe separado pra da pra montar uma
    /// porta solta num teste.
    /// </summary>
    public void Configurar(Sala dona, LadoDaPorta novoLado, bool porta, float largura, float espessura)
    {
        Sala = dona;
        lado = novoLado;
        existe = porta;

        Vector2 tamanho = novoLado.Horizontal()
            ? new Vector2(largura, espessura)
            : new Vector2(espessura, largura);

        // O que bloqueia: ocupa o vao inteiro. Camada de parede, pra projetil e visao
        // tratarem a porta fechada igual a uma parede.
        GameObject objBloqueio = new GameObject("Bloqueio");
        objBloqueio.transform.SetParent(transform, false);
        objBloqueio.layer = Sala.CamadaDeParede;

        bloqueio = objBloqueio.AddComponent<BoxCollider2D>();
        bloqueio.size = tamanho;

        // O sensor: so a metade de FORA do vao. Assim o aviso sai quando o jogador ja
        // entrou na porta, e nao so de encostar no batente.
        passagem = gameObject.AddComponent<BoxCollider2D>();
        passagem.isTrigger = true;
        passagem.size = tamanho * (novoLado.Horizontal() ? new Vector2(0.8f, 0.5f) : new Vector2(0.5f, 0.8f));
        passagem.offset = novoLado.Direcao() * espessura * 0.25f;

        desenho = FormasDaSala.Desenho(transform, "Desenho", FormasDaSala.Quadrado(), corDeParede, Vector2.zero, tamanho, 1);

        if (existe)
            Abrir();
        else
            Emparedar();
    }

    public void Abrir()
    {
        if (!existe)
            return;

        Aberta = true;
        AplicarEstado();
    }

    public void Fechar()
    {
        if (!existe)
            return;

        Aberta = false;
        AplicarEstado();
    }

    private void Emparedar()
    {
        Aberta = false;
        AplicarEstado();
    }

    private void AplicarEstado()
    {
        if (bloqueio != null)
            bloqueio.enabled = !Aberta;

        if (passagem != null)
            passagem.enabled = existe && Aberta;

        if (desenho != null)
            desenho.color = !existe ? corDeParede : Aberta ? corAberta : corFechada;
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (!existe || !Aberta)
            return;

        GameObject quem = outro.attachedRigidbody != null ? outro.attachedRigidbody.gameObject : outro.gameObject;

        if (!quem.CompareTag(tagDoJogador))
            return;

        AoAtravessar?.Invoke(this);
    }
}
