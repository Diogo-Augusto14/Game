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
    private Vector2 tamanhoDoVao;

    public LadoDaPorta Lado => lado;

    public bool Existe => existe;

    public bool Aberta { get; private set; } = true;

    /// <summary>Porta secreta ainda nao descoberta: parece parede ate uma bomba explodir perto.</summary>
    public bool Escondida { get; private set; }

    /// <summary>Uma bomba abriu esta porta secreta. O andar abre a do outro lado junto.</summary>
    public event System.Action<Porta> AoRevelar;

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

        tamanhoDoVao = tamanho;
        desenho = FormasDaSala.Desenho(transform, "Desenho", FormasDaSala.Quadrado(), corDeParede, Vector2.zero, tamanho, 1);

        if (existe)
            Abrir();
        else
            Emparedar();
    }

    public void Abrir()
    {
        if (!existe || Escondida)
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

    /// <summary>Cor de quando a porta nao existe (vira parede). A sala chama ao pintar o andar.</summary>
    public void PintarParede(Color cor)
    {
        corDeParede = cor;
        AplicarEstado();
    }

    /// <summary>Vira porta secreta: fechada e com cara de parede, com uma rachadura discreta.</summary>
    public void Esconder()
    {
        if (!existe)
            return;

        Escondida = true;
        Aberta = false;
        AplicarEstado();

        // A dica do Isaac: uma rachadura na parede, pra quem prestar atencao.
        Vector2 eixo = lado.Horizontal() ? Vector2.right : Vector2.up;
        Vector2 cruzado = lado.Horizontal() ? Vector2.up : Vector2.right;

        for (int i = -1; i <= 1; i++)
        {
            SpriteRenderer risco = FormasDaSala.Desenho(transform, "Rachadura", FormasDaSala.Quadrado(),
                new Color(0.12f, 0.1f, 0.09f, 0.8f), eixo * (i * 0.22f) + cruzado * (i % 2 == 0 ? 0.08f : -0.08f),
                Vector2.one * 0.1f, 3);
            risco.transform.localScale = new Vector3(lado.Horizontal() ? 2.4f : 0.9f, lado.Horizontal() ? 0.9f : 2.4f, 1f);
            risco.transform.localRotation = Quaternion.Euler(0f, 0f, i * 35f);
        }
    }

    /// <summary>
    /// Uma bomba explodiu perto: a porta secreta aparece. Abre na hora se a sala nao estiver
    /// em luta; senao abre junto com as outras quando a sala for limpa.
    /// </summary>
    public void Revelar()
    {
        if (!Escondida)
            return;

        Escondida = false;

        foreach (Transform filho in transform)
            if (filho.name == "Rachadura")
                Destroy(filho.gameObject);

        if (Sala == null || !Sala.Ativa || Sala.Limpa)
            Abrir();
        else
            AplicarEstado();

        AoRevelar?.Invoke(this);
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

        if (desenho == null)
            return;

        // Sem porta (ou porta secreta), o vao vira tijolo igual ao resto da parede.
        bool parede = !existe || Escondida;
        Sprite portao = parede ? null : ArteImportada.Portao(Aberta);

        if (portao != null)
        {
            // Portao de grade do pacote: desenhado de frente, virado pra dentro da sala.
            desenho.drawMode = SpriteDrawMode.Simple;
            desenho.sprite = portao;
            desenho.color = Color.white;
            desenho.transform.localRotation = Quaternion.Euler(0f, 0f, RotacaoDoPortao());
            desenho.transform.localScale = new Vector3(Mathf.Max(tamanhoDoVao.x, tamanhoDoVao.y), Mathf.Min(tamanhoDoVao.x, tamanhoDoVao.y), 1f);
            return;
        }

        desenho.transform.localRotation = Quaternion.identity;
        desenho.transform.localScale = Vector3.one;
        desenho.drawMode = SpriteDrawMode.Tiled;
        desenho.sprite = parede ? ArteGerada.Tijolo() : FormasDaSala.Quadrado();
        desenho.size = tamanhoDoVao;
        desenho.color = parede ? corDeParede : Aberta ? corAberta : corFechada;
    }

    /// <summary>O portao e desenhado na parede de cima; nas outras gira pra a frente dar pra sala.</summary>
    private float RotacaoDoPortao()
    {
        switch (lado)
        {
            case LadoDaPorta.Baixo: return 180f;
            case LadoDaPorta.Esquerda: return 90f;
            case LadoDaPorta.Direita: return -90f;
            default: return 0f;
        }
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
