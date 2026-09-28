using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Monta o jogo ao apertar Play: boneco completo, inimigos completos, camera, HUD e as
/// pecas de fase que faltam pra testar cada mecanica. Ele existe pra o jogo funcionar sem
/// ninguem preparar a cena — nao pra tomar conta dela pra sempre.
///
/// COMO DESLIGAR, do mais leve ao mais definitivo:
///
///   1. Caixinhas: desmarque so o que voce ja montou a mao (Montar Jogador, Montar
///      Inimigos, Completar Fase, Montar Hud, Ajustar Camera, Ajustar Gravidade).
///   2. Chave mestra: desmarque <c>Ativo</c>. Ele nao faz absolutamente nada — mas,
///      existindo na cena, impede a instalacao automatica de um outro.
///   3. Definitivo no projeto: adicione o simbolo <c>JOGO_SEM_BOOTSTRAP</c> em
///      Project Settings ▸ Player ▸ Scripting Define Symbols. Ai nem a instalacao
///      automatica existe, e o Bootstrap so roda se voce colocar o componente na cena.
///
/// Pra sair do automatico com o boneco pronto na mao, rode
/// <c>Tools ▸ Jogo ▸ Montar na cena ▸ Kit completo</c>: ele deixa Jogador, Inimigo e as
/// pecas de cenario como objetos de verdade na cena (e salva os prefabs), e ja desliga
/// este componente. Dali pra frente a cena e sua.
///
/// POR QUE ELE CONSTROI UM BONECO NOVO em vez de consertar o que esta na cena: os Awake
/// dos componentes antigos ja rodaram quando o Bootstrap entra em acao, com referencias
/// resolvidas pela metade. Montar do zero, na ordem certa de dependencia, e a unica forma
/// de garantir que TODA mecanica funciona no primeiro Play. O boneco velho da cena nao e
/// apagado: so fica desativado durante o jogo, e o arquivo da cena continua intacto.
/// </summary>
[DisallowMultipleComponent]
public class Bootstrap : MonoBehaviour
{
    [Header("Chave mestra")]
    [Tooltip("Desmarcado = este componente nao faz NADA. Deixe ele na cena assim pra montar " +
             "tudo a mao sem o automatico voltar")]
    [SerializeField] private bool ativo = true;

    [Header("O que montar")]
    [Tooltip("Monta um boneco novo, completo. Desligue se voce ja montou o seu na mao")]
    [SerializeField] private bool montarJogador = true;

    [Tooltip("Troca os inimigos da cena por inimigos completos (nas mesmas posicoes)")]
    [SerializeField] private bool montarInimigos = true;

    [Tooltip("Quantidade minima de inimigos na fase. Se a cena tiver menos, o resto e colocado")]
    [SerializeField, Min(0)] private int inimigosNovos = 3;

    [Tooltip("Acrescenta plataformas, parede de wall jump, beirada, escada e tunel de escorregar")]
    [SerializeField] private bool completarFase = true;

    [Tooltip("Monta a barra de vida, os frascos e a lista de controles")]
    [SerializeField] private bool montarHud = true;

    [Tooltip("Cena sem chao nenhum embaixo do nascimento: cria um chao pra o boneco nao cair pra sempre")]
    [SerializeField] private bool chaoDeEmergencia = true;

    [Header("Camera")]
    [Tooltip("Desligue se voce mesmo ajusta a camera. Ligado, ele forca ortografica, o zoom " +
             "abaixo e o componente Cameramov")]
    [SerializeField] private bool ajustarCamera = true;

    [Tooltip("Meia altura da camera em unidades. 2.8 deixa o boneco com um bom tamanho na tela")]
    [SerializeField, Min(0.5f)] private float tamanhoDaCamera = 2.8f;

    [Tooltip("Trava a camera dentro dos limites da fase")]
    [SerializeField] private bool limitarCamera = true;

    [Header("Fisica")]
    [Tooltip("Desligue pra manter a gravidade de Project Settings ▸ Physics 2D")]
    [SerializeField] private bool ajustarGravidade = true;

    [Tooltip("Gravidade do mundo. O boneco tem meio metro de altura, entao a padrao (-9.81) serve")]
    [SerializeField] private Vector2 gravidade = new Vector2(0f, -9.81f);

    [Header("Diagnostico")]
    [Tooltip("Escreve no Console o que foi montado")]
    [SerializeField] private bool relatorioNoConsole = true;

    private readonly List<string> relatorio = new List<string>();

    private Bounds limitesDaFase;
    private bool temLimites;

    /// <summary>Le a chave mestra. O Montador de Cena usa pra saber se precisa desligar.</summary>
    public bool Ativo
    {
        get => ativo;
        set => ativo = value;
    }

    // ================================================================ instalacao automatica
#if !JOGO_SEM_BOOTSTRAP
    /// <summary>
    /// Roda depois de a cena carregar, em qualquer cena, sem ninguem chamar. E isto que
    /// faz o "so dar play" ser verdade mesmo numa cena vazia.
    ///
    /// Se ja existe um Bootstrap na cena — inclusive um com a chave mestra DESLIGADA —
    /// ele nao cria outro. E assim que se desliga o automatico sem mexer em codigo.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void GarantirNaCena()
    {
        if (FindAnyObjectByType<Bootstrap>(FindObjectsInactive.Include) != null)
            return;

        // Cena top-down tem o proprio montador: o do plataforma aqui so atrapalharia
        // (gravidade, chao de emergencia, boneco de plataforma).
        if (FindAnyObjectByType<BootstrapTopDown>(FindObjectsInactive.Include) != null)
            return;

        GameObject obj = new GameObject("Bootstrap (automatico)");
        obj.AddComponent<Bootstrap>();
    }
#endif

    // ================================================================ ciclo de vida
    private void Awake()
    {
        if (!ativo)
            return;

        Montar();
    }

    /// <summary>Monta o que as caixinhas pedirem. Separado do Awake pra dar pra chamar a mao.</summary>
    public void Montar()
    {
        if (ajustarGravidade)
            Physics2D.gravity = gravidade;

        // As mascaras sao guardadas em cache estatico; entre um Play e outro as camadas
        // podem ter mudado, entao esquecemos o que sabiamos.
        Camadas.Esquecer();

        if (BibliotecaDeAnimacoes.Padrao == null)
        {
            Debug.LogError(
                "[Bootstrap] a biblioteca de animacoes nao existe. Rode " +
                "Tools > Jogo > Reconstruir animacoes (ou Preparar projeto) e aperte Play de novo.");
        }

        Vector2 nascimento = DescobrirNascimento();

        Player jogador = montarJogador ? MontarJogador(nascimento) : Player.Atual;

        float chaoY = AcharTopoDoChao(nascimento, out bool achouChao);

        if (!achouChao)
        {
            chaoY = nascimento.y;

            if (chaoDeEmergencia)
                MontarChaoDeEmergencia(nascimento);
        }

        if (completarFase)
            CompletarFase(new Vector2(nascimento.x, chaoY));

        if (montarInimigos)
            MontarInimigos(chaoY);

        if (ajustarCamera)
            MontarCamera(jogador);

        if (montarHud)
            MontarHud();

        if (jogador != null && montarJogador)
            jogador.DefinirAlturaDaMorte(chaoY - 12f);

        if (relatorioNoConsole && relatorio.Count > 0)
            Debug.Log("[Bootstrap] " + string.Join(" | ", relatorio));
    }

    // ================================================================ jogador
    private Vector2 DescobrirNascimento()
    {
        // 1. Um objeto chamado PontoDeNascimento manda em tudo.
        GameObject marcado = GameObject.Find("PontoDeNascimento");

        if (marcado != null)
            return marcado.transform.position;

        // 2. Senao, onde o boneco da cena estava.
        Player existente = FindAnyObjectByType<Player>(FindObjectsInactive.Include);

        if (existente != null)
            return existente.transform.position;

        GameObject porTag = AcharPorTagSemErro("Player");

        if (porTag != null)
            return porTag.transform.position;

        // 3. Senao, onde a camera esta olhando.
        Camera cam = Camera.main;
        return cam != null ? (Vector2)cam.transform.position : Vector2.zero;
    }

    private Player MontarJogador(Vector2 posicao)
    {
        DesativarAntigos<Player>("boneco antigo");
        DesativarPorTag("Player");

        GameObject raiz = Construtor.MontarJogador(posicao);

        Anotar("boneco montado");
        return raiz.GetComponent<Player>();
    }

    // ================================================================ inimigos
    private void MontarInimigos(float chaoY)
    {
        List<Vector2> posicoes = new List<Vector2>();

        foreach (Inimigo antigo in FindObjectsByType<Inimigo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            posicoes.Add(antigo.transform.position);
            antigo.gameObject.SetActive(false);
        }

        // Objetos de demonstracao dos pacotes de arte que nao tem o componente Inimigo.
        foreach (GameObject candidato in AcharPorNome("LightBandit", "HeavyBandit", "Bandit"))
        {
            if (candidato.GetComponent<Inimigo>() != null || !candidato.activeInHierarchy)
                continue;

            posicoes.Add(candidato.transform.position);
            candidato.SetActive(false);
        }

        // Completa ate o minimo pedido: a cena costuma ter um inimigo de teste so, e um
        // inimigo nao mostra perseguicao, agro perdido nem combate com dois de uma vez.
        //
        // Os pontos sao escolhidos pra cair no chao livre, longe do tunel, das paredes e do
        // bloco da beirada — inimigo nascendo dentro de um bloco sai voando pro lado.
        float baseX = Player.Atual != null ? Player.Atual.transform.position.x : 0f;
        float[] pontosLivres = { 2.7f, -6.5f, 6.3f, -9.2f, 13.6f };

        for (int i = posicoes.Count; i < inimigosNovos; i++)
        {
            float deslocamento = pontosLivres[i % pontosLivres.Length] + (i / pontosLivres.Length) * 1.1f;
            posicoes.Add(new Vector2(baseX + deslocamento, chaoY + 0.05f));
        }

        for (int i = 0; i < posicoes.Count; i++)
            Construtor.MontarInimigo(posicoes[i], $"Inimigo {i + 1}");

        Anotar($"{posicoes.Count} inimigo(s)");
    }

    // ================================================================ fase
    /// <summary>
    /// Procura a superficie de chao mais alta embaixo do ponto de nascimento. Um raio longo
    /// pra baixo acha o chao da cena atual sem precisar saber nada sobre ela.
    /// </summary>
    private float AcharTopoDoChao(Vector2 de, out bool achou)
    {
        RaycastHit2D hit = Physics2D.Raycast(de + Vector2.up * 0.5f, Vector2.down, 40f, Camadas.MascaraDeSolido);

        achou = hit.collider != null;
        return achou ? hit.point.y : de.y;
    }

    private void MontarChaoDeEmergencia(Vector2 nascimento)
    {
        // Cena sem chao nenhum: sem isto o boneco cairia pra sempre e pareceria travado.
        Construtor.MontarBloco(
            "Chao", new Vector2(nascimento.x, nascimento.y - 0.4f), new Vector2(30f, 0.8f),
            Construtor.COR_CHAO, Construtor.TipoDeBloco.Chao);

        Anotar("chao de emergencia");
    }

    /// <summary>
    /// Acrescenta as pecas que faltam pra experimentar TODAS as mecanicas: plataformas em
    /// alturas de pulo e de pulo duplo, um tunel que so passa escorregando, um bloco alto
    /// pra pendurar na beirada, um corredor de wall jump e uma escada.
    ///
    /// Tudo posicionado em relacao ao chao que a cena ja tem, entao nada fica flutuando.
    /// </summary>
    private void CompletarFase(Vector2 origem)
    {
        Transform pai = new GameObject("Fase (Bootstrap)").transform;

        float x = origem.x;
        float y = origem.y;

        // --- tunel de escorregar: sobra pouco espaco, so passa deitado
        Bloco("Tunel", x + 1.6f, y + 0.62f, 1.6f, 0.4f, Construtor.COR_TUNEL, Construtor.TipoDeBloco.Chao, pai);

        // --- escada de plataformas (pulo simples e pulo duplo)
        Bloco("Plataforma 1", x + 3.6f, y + 0.8f, 1.4f, 0.25f, Construtor.COR_PLATAFORMA, Construtor.TipoDeBloco.Chao, pai);
        Bloco("Plataforma 2", x + 5.4f, y + 1.7f, 1.4f, 0.25f, Construtor.COR_PLATAFORMA, Construtor.TipoDeBloco.Chao, pai);

        // --- bloco alto na camada de parede: cair na quina agarra a beirada
        Bloco("Beirada", x + 7.4f, y + 1.1f, 1.2f, 2.2f, Construtor.COR_BEIRADA, Construtor.TipoDeBloco.Parede, pai);

        // --- corredor de wall jump: duas paredes de frente, subindo entre elas.
        // A plataforma de chegada fica ao LADO (nao em cima): tampar o corredor prenderia
        // o boneco embaixo dela justo quando ele acabou de subir.
        Bloco("Parede A", x + 9.4f, y + 1.6f, 0.3f, 3.2f, Construtor.COR_PAREDE, Construtor.TipoDeBloco.Parede, pai);
        Bloco("Parede B", x + 10.9f, y + 1.6f, 0.3f, 3.2f, Construtor.COR_PAREDE, Construtor.TipoDeBloco.Parede, pai);
        Bloco("Saida do corredor", x + 12.3f, y + 3f, 2f, 0.25f, Construtor.COR_PLATAFORMA, Construtor.TipoDeBloco.Chao, pai);

        // --- escada de subir, pro outro lado
        const float alturaDaEscada = 2.4f;
        Bloco("Escada", x - 3.2f, y + alturaDaEscada * 0.5f, 0.4f, alturaDaEscada,
            Construtor.COR_ESCADA, Construtor.TipoDeBloco.Escada, pai);

        // Piso do topo AO LADO da escada (nao em cima dela): em cima, o boneco bateria a
        // cabeca na plataforma antes de chegar no ultimo degrau. O topo do bloco fica na
        // mesma altura do topo da escada, e o Movimento acha esse piso ao sair.
        Bloco("Topo da escada", x - 2f, y + alturaDaEscada - 0.125f, 2f, 0.25f,
            Construtor.COR_PLATAFORMA, Construtor.TipoDeBloco.Chao, pai);

        Anotar("fase completada (tunel, plataformas, beirada, wall jump, escada)");

        CalcularLimites();
    }

    private static void Bloco(
        string nome, float x, float y, float largura, float altura,
        Color cor, Construtor.TipoDeBloco tipo, Transform pai)
    {
        Construtor.MontarBloco(nome, new Vector2(x, y), new Vector2(largura, altura), cor, tipo, pai);
    }

    /// <summary>Retangulo que cobre todo o chao da cena — a camera nao passa daqui.</summary>
    private void CalcularLimites()
    {
        Bounds total = new Bounds();
        bool primeiro = true;

        foreach (Collider2D c in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (c.isTrigger || c.attachedRigidbody != null)
                continue;

            if (primeiro)
            {
                total = c.bounds;
                primeiro = false;
                continue;
            }

            total.Encapsulate(c.bounds);
        }

        if (primeiro)
            return;

        total.Expand(new Vector3(1.5f, 4f, 0f));
        limitesDaFase = total;
        temLimites = true;
    }

    // ================================================================ camera e HUD
    private void MontarCamera(Player jogador)
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            GameObject obj = new GameObject("Main Camera");
            cam = obj.AddComponent<Camera>();
            obj.tag = "MainCamera";
            Anotar("camera criada");
        }

        cam.orthographic = true;
        cam.orthographicSize = tamanhoDaCamera;

        Cameramov seguidor = cam.GetComponent<Cameramov>();

        if (seguidor == null)
            seguidor = cam.gameObject.AddComponent<Cameramov>();

        if (jogador != null)
            seguidor.DefinirAlvo(jogador.transform, true);

        if (limitarCamera && temLimites)
            seguidor.DefinirLimites(limitesDaFase);

        seguidor.PosicionarImediatamente();
    }

    private void MontarHud()
    {
        if (FindAnyObjectByType<Hud>() != null)
            return;

        GameObject obj = new GameObject("HUD");
        obj.AddComponent<Hud>();
        Anotar("HUD montada");
    }

    // ================================================================ utilidades
    private void DesativarAntigos<T>(string oQue) where T : Component
    {
        int quantos = 0;

        foreach (T antigo in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (antigo.gameObject == gameObject || !antigo.gameObject.activeInHierarchy)
                continue;

            antigo.gameObject.SetActive(false);
            quantos++;
        }

        if (quantos > 0)
            Anotar($"{oQue} desativado ({quantos})");
    }

    private void DesativarPorTag(string tag)
    {
        // FindWithTag so devolve objetos ativos, entao desativar um faz o proximo aparecer.
        // O contador e cinto de seguranca: nenhum laco meu vai travar o Play de ninguem.
        for (int voltas = 0; voltas < 64; voltas++)
        {
            GameObject obj = AcharPorTagSemErro(tag);

            if (obj == null || obj == gameObject)
                return;

            obj.SetActive(false);
        }
    }

    private static GameObject AcharPorTagSemErro(string tag)
    {
        // FindWithTag estoura excecao se a tag nao existir no projeto.
        try
        {
            return GameObject.FindWithTag(tag);
        }
        catch (UnityException)
        {
            return null;
        }
    }

    private static List<GameObject> AcharPorNome(params string[] pedacos)
    {
        List<GameObject> achados = new List<GameObject>();

        foreach (SpriteRenderer sr in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            for (int i = 0; i < pedacos.Length; i++)
            {
                if (sr.name.IndexOf(pedacos[i], System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                achados.Add(sr.gameObject);
                break;
            }
        }

        return achados;
    }

    private void Anotar(string texto)
    {
        relatorio.Add(texto);
    }
}
