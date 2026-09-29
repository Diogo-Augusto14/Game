using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// O menu inicial: nome do jogo, os controles e "Enter pra jogar". Aparece so na primeira
/// vez que a cena abre; recomecar depois de morrer (R) vai direto pro andar 1, como no
/// Isaac. Com o menu na tela o jogo fica congelado e o jogador nao anda.
///
/// O <see cref="Andar"/> monta por <see cref="Mostrar"/>. Sair pro menu pela pausa chama
/// <see cref="VoltarAoMenu"/>, que recarrega a cena com o menu de novo.
/// </summary>
[DisallowMultipleComponent]
public class TelaDeInicio : MonoBehaviour
{
    private static readonly KeyCode[] TeclasDeJogar = { KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space };

    private string titulo = "THE PRETTIE";
    private Transform jogador;
    private Andar andar;
    private Text textoDoTitulo;
    private Text textoDeJogar;
    private float abriu;

    /// <summary>O menu ja passou nesta sessao: recomecar nao mostra de novo.</summary>
    public static bool JaPassou { get; private set; }

    public static bool Aberta => atual != null;

    private static TelaDeInicio atual;

    // Com "Enter Play Mode" sem recarregar o dominio, o estatico sobreviveria entre Plays.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar()
    {
        JaPassou = false;
        atual = null;
    }

    public static void Mostrar(Andar andar, string titulo)
    {
        if (JaPassou || atual != null)
            return;

        TelaDeInicio tela = new GameObject("Menu inicial").AddComponent<TelaDeInicio>();
        tela.andar = andar;
        tela.titulo = string.IsNullOrEmpty(titulo) ? tela.titulo : titulo;
        tela.jogador = andar != null ? andar.Jogador : null;
        tela.Montar();
    }

    /// <summary>Pela pausa: recarrega a cena e mostra o menu de novo.</summary>
    public static void VoltarAoMenu()
    {
        JaPassou = false;
        TelaDeFimDeJogo.RecarregarCena();
    }

    private void Montar()
    {
        atual = this;
        abriu = Time.unscaledTime;

        TelaSimples.Montar(gameObject, 100, new Color(0.03f, 0.02f, 0.03f, 0.97f));

        textoDoTitulo = TelaSimples.Texto(transform, "Titulo", 120, new Color(0.95f, 0.85f, 0.75f), 250f, titulo);
        TelaSimples.Texto(transform, "Subtitulo", 30, new Color(0.7f, 0.6f, 0.6f), 150f,
            $"Desca {(andar != null ? andar.AndarFinal : 4)} andares e derrote o Olho do Porao");

        TelaSimples.Texto(transform, "Controles", 30, new Color(0.85f, 0.85f, 0.85f), -30f,
            "W A S D  andar        Setas  atirar        E  bomba\n" +
            "Esc  pausar        M  musica        N  efeitos\n\n" +
            "Bomba abre parede rachada. Moeda compra na loja.");

        textoDeJogar = TelaSimples.Texto(transform, "Jogar", 44, new Color(1f, 0.85f, 0.35f), -260f, "Enter  jogar");

        // Paineis do Pixel UI pack atras dos controles e do "botao" de jogar.
        TelaSimples.Painel(transform, "Painel do botao", ArteImportada.PainelAzul, -260f, new Vector2(520f, 110f));
        TelaSimples.Painel(transform, "Painel dos controles", ArteImportada.PainelMarrom, -30f, new Vector2(1180f, 250f));

        // Congela o jogo e segura o jogador ate apertar Enter.
        Time.timeScale = 0f;
        TelaSimples.TravarJogador(jogador, true);
        Musica.Tocar(TemaMusical.Menu);
    }

    private void Update()
    {
        // O jogador pode ter sido montado depois do menu (mesmo quadro do Start do Andar).
        if (jogador == null && andar != null && andar.Jogador != null)
        {
            jogador = andar.Jogador;
            TelaSimples.TravarJogador(jogador, true);
        }

        float t = Time.unscaledTime;
        textoDoTitulo.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 1.5f) * 0.03f);
        textoDeJogar.color = new Color(1f, 0.85f, 0.35f, 0.55f + Mathf.Sin(t * 4f) * 0.45f);

        Opcoes.LerTeclas();

        // Meio segundo de respiro: o Enter que abriu o Play nao pula o menu.
        if (t - abriu < 0.4f)
            return;

        foreach (KeyCode tecla in TeclasDeJogar)
        {
            if (Input.GetKeyDown(tecla))
            {
                Comecar();
                return;
            }
        }
    }

    private void Comecar()
    {
        JaPassou = true;
        Time.timeScale = 1f;
        TelaSimples.TravarJogador(jogador, false);
        Sons.Tocar(Som.Menu);

        if (andar != null)
            Musica.Tocar(Musica.DoAndar(andar.NumeroDoAndar));

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (atual == this)
            atual = null;

        // Sair do Play com o menu aberto nao pode deixar o tempo parado.
        if (Time.timeScale == 0f && TelaDeFimDeJogo.Atual == null)
            Time.timeScale = 1f;
    }
}
