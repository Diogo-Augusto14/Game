using UnityEngine;

/// <summary>
/// Traduz o ESTADO do boneco em CLIPE de animacao. E o unico arquivo que sabe os nomes
/// dos clipes do jogador — Movimento, Ataque e Cura nao encostam em animacao nenhuma.
///
/// Duas ideias resolvem 90% dos problemas de animacao de plataforma:
///
///   1. PRIORIDADE. Todo quadro a gente recalcula o clipe certo, de cima pra baixo:
///      morto ganha de apanhar, que ganha de atacar, que ganha de andar. Nao existe
///      "transicao pendente" nem estado preso.
///   2. TRAVA CURTA. Clipes de transicao (comecar a correr, frear, virar, pousar rolando,
///      pular da parede) precisam terminar pra fazer sentido. Ao disparar um desses, ele
///      fica travado pela duracao — mas so contra clipes de prioridade MENOR. Levar um
///      golpe no meio de uma freada continua cortando a freada, como deve ser.
/// </summary>
[DisallowMultipleComponent]
public class AnimacaoDoJogador : MonoBehaviour
{
    [Header("Referencias (vazio = procura sozinho)")]
    [SerializeField] private AnimadorDeSprites animador;
    [SerializeField] private Movimento movimento;
    [SerializeField] private Ataque ataque;
    [SerializeField] private Cura cura;
    [SerializeField] private Vida vida;

    [Header("Ajustes")]
    [Tooltip("Abaixo desta velocidade ele conta como parado")]
    [SerializeField, Min(0f)] private float velocidadeParaAndar = 0.12f;

    [Tooltip("A partir desta fracao da velocidade maxima, usa o clipe de CORRER em vez de ANDAR")]
    [SerializeField, Range(0.1f, 1f)] private float fracaoParaCorrer = 0.62f;

    [Tooltip("Liga a velocidade do clipe de andar/correr a velocidade real do boneco")]
    [SerializeField] private bool clipeAcompanhaVelocidade = true;

    [Tooltip("Velocidade minima do clipe quando ele acompanha o boneco")]
    [SerializeField, Range(0.2f, 1f)] private float velocidadeMinimaDoClipe = 0.55f;

    // ---------------- estado ----------------
    private string clipeTravado;
    private float travaAte;
    private bool travaEhDeDano;

    private float velocidadeAnterior;
    private float ladoAnterior;
    private bool noChaoAnterior;

    // ---------------- ciclo de vida ----------------
    private void Awake()
    {
        if (animador == null) animador = GetComponentInChildren<AnimadorDeSprites>();
        if (movimento == null) movimento = GetComponent<Movimento>();
        if (ataque == null) ataque = GetComponent<Ataque>();
        if (cura == null) cura = GetComponent<Cura>();
        if (vida == null) vida = GetComponent<Vida>();

        if (animador == null)
            Debug.LogError($"[AnimacaoDoJogador] {name}: sem AnimadorDeSprites neste objeto nem nos filhos.", this);
    }

    private void OnEnable()
    {
        if (movimento != null)
        {
            movimento.aoPularDaParede.AddListener(AoPularDaParede);
            movimento.aoPousarRolando.AddListener(AoPousarRolando);
            movimento.aoAgarrarBeirada.AddListener(AoAgarrarBeirada);
        }

        if (vida != null)
        {
            vida.AoTomarDano.AddListener(AoTomarDano);
            vida.AoMorrer.AddListener(AoMorrer);
        }
    }

    private void OnDisable()
    {
        if (movimento != null)
        {
            movimento.aoPularDaParede.RemoveListener(AoPularDaParede);
            movimento.aoPousarRolando.RemoveListener(AoPousarRolando);
            movimento.aoAgarrarBeirada.RemoveListener(AoAgarrarBeirada);
        }

        if (vida != null)
        {
            vida.AoTomarDano.RemoveListener(AoTomarDano);
            vida.AoMorrer.RemoveListener(AoMorrer);
        }
    }

    private void Update()
    {
        if (animador == null || movimento == null)
            return;

        DetectarTransicoes();
        Aplicar();

        velocidadeAnterior = movimento.VelocidadeHorizontal;
        ladoAnterior = movimento.LadoPedido;
        noChaoAnterior = movimento.NoChao;
    }

    // ---------------- prioridades ----------------
    private void Aplicar()
    {
        // 1. Morto e 2. atacando/apanhando ganham de qualquer trava.
        if (movimento.Morto)
        {
            TocarDireto(NomesDeAnimacao.Morrer);
            return;
        }

        if (ataque != null && ataque.EstaAtacando)
        {
            // O Ataque ja mandou o clipe e a velocidade — aqui so nao atropelamos.
            LimparTrava();
            return;
        }

        if (movimento.Atordoado)
        {
            TocarDireto(ClipeDeDano());
            return;
        }

        // A animacao de apanhar costuma ser mais longa que o atordoamento; ela segura o
        // desenho ate terminar. Toda OUTRA trava (arranque, freada, virar) perde de um
        // dash ou de uma esquiva, por isso e checada la embaixo.
        if (travaEhDeDano && TravaValendo())
            return;

        // Entrou num estado exclusivo (cura, dash, escada, parede...)? Uma trava de
        // transicao que tenha sobrado nao pode ressuscitar depois — joga fora agora.
        if (EmEstadoExclusivo())
            LimparTrava();

        if (cura != null && cura.Curando)
        {
            TocarDireto(NomesDeAnimacao.Curar);
            return;
        }

        if (movimento.SubindoBeirada)
        {
            TocarSubidaDaBeirada();
            return;
        }

        if (movimento.Pendurado)
        {
            // Agarrou agora: o clipe de agarrar toca inteiro antes do "pendurado parado".
            if (TravaValendo())
                return;

            TocarDireto(NomesDeAnimacao.BeiradaParado);
            return;
        }

        if (movimento.NaEscada)
        {
            TocarEscada();
            return;
        }

        if (movimento.Escorregando)
        {
            TocarDireto(NomesDeAnimacao.Escorregar);
            return;
        }

        if (movimento.Esquivando)
        {
            TocarDireto(NomesDeAnimacao.EsquivaTras);
            return;
        }

        if (movimento.Dashando)
        {
            TocarDireto(NomesDeAnimacao.DashAereo);
            return;
        }

        if (movimento.Deslizando)
        {
            TocarDireto(NomesDeAnimacao.ParedeDeslizar);
            return;
        }

        // Daqui pra baixo e locomocao comum — e so aqui que os clipes de transicao
        // (arrancar, frear, virar, pousar rolando, pular da parede) seguram o desenho.
        if (TravaValendo())
            return;

        if (!movimento.NoChao)
        {
            TocarNoAr();
            return;
        }

        TocarNoChao();
    }

    /// <summary>
    /// Estados que tem clipe proprio e mandam mais que qualquer transicao. "Pendurado"
    /// fica de fora de proposito: e ele que consome a trava do clipe de agarrar.
    /// </summary>
    private bool EmEstadoExclusivo()
    {
        return (cura != null && cura.Curando)
            || movimento.SubindoBeirada
            || movimento.NaEscada
            || movimento.Escorregando
            || movimento.Esquivando
            || movimento.Dashando
            || movimento.Deslizando;
    }

    private string ClipeDeDano()
    {
        bool forte = vida != null && vida.UltimoGolpe.Peso == PesoDoGolpe.Forte;
        return forte ? NomesDeAnimacao.DanoForte : NomesDeAnimacao.Dano;
    }

    private void TocarSubidaDaBeirada()
    {
        Tocar(NomesDeAnimacao.BeiradaSubir);

        // A subida dura o que o Movimento disser; o clipe se estica/encurta pra caber,
        // senao o boneco chega em cima antes (ou depois) do desenho terminar.
        float duracaoDoClipe = animador.DuracaoDe(NomesDeAnimacao.BeiradaSubir);
        float duracaoDoEstado = movimento.duracaoDaSubida;

        animador.Velocidade = duracaoDoEstado > 0.01f && duracaoDoClipe > 0.01f
            ? duracaoDoClipe / duracaoDoEstado
            : 1f;
    }

    private void TocarEscada()
    {
        if (movimento.EscorregandoNaEscada)
        {
            Tocar(NomesDeAnimacao.EscadaEscorregar);
            animador.Velocidade = 1f;
            return;
        }

        Tocar(NomesDeAnimacao.EscadaSubir);

        // Parado na escada: o clipe congela em vez de o boneco subir sem sair do lugar.
        float vertical = Mathf.Abs(movimento.VelocidadeVertical);
        animador.Velocidade = vertical > 0.05f ? 1f : 0f;
    }

    private void TocarNoAr()
    {
        animador.Velocidade = 1f;

        bool subindo = movimento.VelocidadeVertical > 0.05f;

        if (subindo && movimento.UltimoPuloFoiDuplo)
        {
            bool comDirecao = Mathf.Abs(movimento.LadoPedido) > 0.1f;

            animador.TocarPrimeiroQueExistir(
                comDirecao ? NomesDeAnimacao.PuloDuploFrente : NomesDeAnimacao.PuloDuplo,
                NomesDeAnimacao.PuloDuplo,
                NomesDeAnimacao.Pular);

            return;
        }

        if (subindo)
        {
            Tocar(NomesDeAnimacao.Pular);
            return;
        }

        animador.TocarPrimeiroQueExistir(NomesDeAnimacao.Cair, NomesDeAnimacao.Pular);
    }

    private void TocarNoChao()
    {
        if (movimento.Agachado)
        {
            Tocar(NomesDeAnimacao.Agachar);
            animador.Velocidade = 1f;
            return;
        }

        float velocidade = movimento.VelocidadeHorizontal;

        if (velocidade < velocidadeParaAndar || Mathf.Approximately(movimento.LadoPedido, 0f))
        {
            Tocar(NomesDeAnimacao.Parado);
            animador.Velocidade = 1f;
            return;
        }

        float maxima = Mathf.Max(0.01f, movimento.velocidadeMaxima);
        float fracao = velocidade / maxima;

        bool correndo = fracao >= fracaoParaCorrer;
        Tocar(correndo ? NomesDeAnimacao.Correr : NomesDeAnimacao.Andar);

        if (!clipeAcompanhaVelocidade)
        {
            animador.Velocidade = 1f;
            return;
        }

        // Clipe na velocidade do boneco: sem isso, ele "patina" (anda rapido com animacao
        // devagar) ou "corre no lugar" (animacao rapida e deslocamento curto).
        float referencia = correndo ? maxima : Mathf.Max(0.01f, movimento.velocidadeDeCaminhada);
        animador.Velocidade = Mathf.Clamp(velocidade / referencia, velocidadeMinimaDoClipe, 1.6f);
    }

    // ---------------- transicoes ----------------
    private void DetectarTransicoes()
    {
        if (!movimento.NoChao || movimento.Morto || movimento.Atordoado)
            return;

        if (movimento.Agachado || movimento.Escorregando || movimento.Dashando || movimento.Esquivando)
            return;

        if (ataque != null && ataque.EstaAtacando)
            return;

        float lado = movimento.LadoPedido;
        float velocidade = movimento.VelocidadeHorizontal;
        float maxima = Mathf.Max(0.01f, movimento.velocidadeMaxima);

        // Saiu do zero: clipe de arranque (andar ou correr, conforme o pedido).
        bool arrancou = velocidadeAnterior < velocidadeParaAndar
                     && Mathf.Abs(lado) > 0.1f
                     && Mathf.Abs(ladoAnterior) < 0.1f;

        if (arrancou)
        {
            Travar(movimento.CorrendoSolto ? NomesDeAnimacao.CorrerInicio : NomesDeAnimacao.AndarInicio);
            return;
        }

        // Inverteu o lado em velocidade: clipe de virar (a derrapada do pe).
        bool inverteu = Mathf.Abs(lado) > 0.1f
                     && Mathf.Abs(ladoAnterior) > 0.1f
                     && !Mathf.Approximately(Mathf.Sign(lado), Mathf.Sign(ladoAnterior))
                     && velocidade > maxima * 0.5f;

        if (inverteu)
        {
            Travar(NomesDeAnimacao.CorrerVirar);
            return;
        }

        // Soltou a tecla correndo: clipe de frear.
        bool freou = Mathf.Abs(lado) < 0.1f
                  && Mathf.Abs(ladoAnterior) > 0.1f
                  && velocidadeAnterior > maxima * 0.55f;

        if (freou)
            Travar(NomesDeAnimacao.CorrerParar);
    }

    // ---------------- eventos ----------------
    private void AoPularDaParede()
    {
        Travar(NomesDeAnimacao.ParedePular);
    }

    private void AoPousarRolando()
    {
        Travar(NomesDeAnimacao.PousarRolando);
    }

    private void AoAgarrarBeirada()
    {
        Travar(NomesDeAnimacao.BeiradaAgarrar);
    }

    private void AoTomarDano(DanoInfo info)
    {
        string clipe = info.Peso == PesoDoGolpe.Forte ? NomesDeAnimacao.DanoForte : NomesDeAnimacao.Dano;
        Travar(clipe, true);
    }

    private void AoMorrer()
    {
        LimparTrava();
        TocarDireto(NomesDeAnimacao.Morrer);
    }

    // ---------------- trava ----------------
    private void Travar(string nome, bool deDano = false)
    {
        if (animador == null || !animador.TemClipe(nome))
            return;

        clipeTravado = nome;
        travaEhDeDano = deDano;
        travaAte = Time.time + Mathf.Max(0.05f, animador.DuracaoDe(nome));

        animador.Velocidade = 1f;
        animador.Tocar(nome, true);
    }

    private bool TravaValendo()
    {
        if (clipeTravado == null)
            return false;

        if (Time.time >= travaAte)
        {
            LimparTrava();
            return false;
        }

        // Saiu do chao (pulou, caiu) no meio de um clipe de chao: a trava perde sentido.
        bool clipeDeChao = clipeTravado == NomesDeAnimacao.CorrerInicio
                        || clipeTravado == NomesDeAnimacao.AndarInicio
                        || clipeTravado == NomesDeAnimacao.CorrerParar
                        || clipeTravado == NomesDeAnimacao.CorrerVirar
                        || clipeTravado == NomesDeAnimacao.PousarRolando;

        if (clipeDeChao && !movimento.NoChao && !travaEhDeDano)
        {
            LimparTrava();
            return false;
        }

        animador.Tocar(clipeTravado);
        return true;
    }

    private void LimparTrava()
    {
        clipeTravado = null;
        travaEhDeDano = false;
        travaAte = 0f;
    }

    private void Tocar(string nome)
    {
        animador.Tocar(nome);
    }

    private void TocarDireto(string nome)
    {
        animador.Velocidade = 1f;
        animador.Tocar(nome);
    }
}
