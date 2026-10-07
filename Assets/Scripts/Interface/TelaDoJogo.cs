using UnityEngine;

/// <summary>
/// O que fica na tela durante o jogo (o objeto "Interface" da cena): o HUD (<see cref="HudDoJogo"/>:
/// vida, arma, andar, chefe) e o escuro da troca de andar, desenhados no OnGUI com a arte de interface
/// e as fontes em pixel, em tamanhos inteiros (<see cref="Desenho"/>).
///
/// Os menus (inicio, pausa, configuracoes, fim de jogo, novidades) sao os do jogo antigo, cada um na
/// sua tela (<see cref="TelaDeInicio"/>, <see cref="TelaDePausa"/>...). Com qualquer um aberto, o HUD
/// some: o OnGUI desenha por cima dos menus.
/// </summary>
[DisallowMultipleComponent]
public class TelaDoJogo : MonoBehaviour
{
    [Header("Fontes")]
    [Tooltip("Texto comum (Jersey 15)")]
    public Font fonteTexto;

    [Tooltip("Titulos (Jacquard 12)")]
    public Font fonteTitulo;

    [Header("Imagens")]
    public Texture2D coracao;
    public Texture2D bolsa;
    public Texture2D molduraPequena;
    public Texture2D barraDourada;

    private HudDoJogo hud;
    private GeradorDoAndar gerador;
    private Vida vidaDoJogador;
    private ArmaDoJogador armasDoJogador;
    private InteracaoDoJogador interacao;
    private HabilidadeDoHeroi habilidade;

    /// <summary>Algum menu (ou a transicao escura entre eles) na tela: o HUD e a seta do andar somem.</summary>
    public static bool MenuAberto =>
        TelaDeInicio.Aberta || TelaDePausa.Aberta || TelaDeFimDeJogo.Atual != null || TelaDeOpcoes.Aberta
        || TelaDeNovidades.Ocupada || TransicaoDeTela.Ocupada || TelaDeProgresso.Aberta;

    private void Awake()
    {
        hud = new HudDoJogo(this);
    }

    private void Start()
    {
        gerador = FindAnyObjectByType<GeradorDoAndar>();
        GameObject jogador = GameObject.FindWithTag("Player");

        if (jogador != null)
        {
            vidaDoJogador = jogador.GetComponent<Vida>();
            armasDoJogador = jogador.GetComponent<ArmaDoJogador>();
            interacao = jogador.GetComponent<InteracaoDoJogador>();
        }
    }

    private void Update()
    {
        // A habilidade entra no jogador depois (o heroi e aplicado no Start do andar).
        if (habilidade == null && vidaDoJogador != null)
            habilidade = vidaDoJogador.GetComponent<HabilidadeDoHeroi>();
    }

    private void OnGUI()
    {
        if (MenuAberto)
            return;

        // Por cima do que os outros desenham no OnGUI (a seta do andar).
        GUI.depth = -10;
        hud.Desenhar(gerador, vidaDoJogador, armasDoJogador, interacao, habilidade);

        // O escuro da troca de andar.
        if (gerador != null && gerador.Escuro > 0f)
            Desenho.Cor(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, gerador.Escuro));
    }
}
