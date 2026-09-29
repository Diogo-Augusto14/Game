using UnityEngine;

public enum TipoDeInimigo
{
    Perseguidor,
    Atirador,
    Chefe,
    Investidor,
    Saltador,
    Sentinela,
    Divisor,
    DivisorPequeno,
    ChefeSaltador,
    ChefeFinal,
    Demonio,
    MonstroDeSangue,
    GoblinTocha,
    GoblinDinamite,
    Barril,
    Arqueiro,
    Esqueleto,
    EsqueletoFoice,
    Vampiro
}

/// <summary>
/// A receita de cada inimigo de sala: corpo, colisor, desenho, vida e comportamento.
///
/// O objeto e montado DESLIGADO e so liga no fim. Motivo: o Vida procura o controlador de
/// movimento no proprio Awake, uma vez so. Ligando depois de tudo adicionado, todos os
/// Awake rodam com todos os componentes ja presentes — o empurrao sai na direcao certa.
/// </summary>
public static class FabricaDeInimigos
{
    public static InimigoDeSala Criar(TipoDeInimigo tipo, Vector2 posicao, Transform pai = null)
    {
        switch (tipo)
        {
            case TipoDeInimigo.Chefe:
                ChefeDoAndar chefe = Montar<ChefeDoAndar>("Chefe", posicao, pai, 0.75f, new Color(0.55f, 0.12f, 0.16f), 120f, ArteGerada.Bola());
                chefe.Enfeitar(0.75f);
                return chefe;

            case TipoDeInimigo.ChefeSaltador:
                ChefeSaltador sapao = Montar<ChefeSaltador>("Chefe Saltador", posicao, pai, 0.8f, new Color(0.25f, 0.5f, 0.22f), 150f, ArteGerada.Bola());
                sapao.Enfeitar();
                return sapao;

            case TipoDeInimigo.ChefeFinal:
                ChefeFinal olho = Montar<ChefeFinal>("Chefe Final", posicao, pai, 1f, new Color(0.32f, 0.1f, 0.28f), 450f, ArteGerada.Bola());
                olho.Enfeitar(1f);
                return olho;

            case TipoDeInimigo.Atirador:
                return Montar<InimigoAtirador>("Atirador", posicao, pai, 0.32f, Rosto(tipo, new Color(0.62f, 0.35f, 0.85f)), 30f);

            case TipoDeInimigo.Investidor:
                return Montar<InimigoInvestidor>("Investidor", posicao, pai, 0.33f, Rosto(tipo, new Color(0.95f, 0.55f, 0.15f)), 35f);

            case TipoDeInimigo.Saltador:
                return Montar<InimigoSaltador>("Saltador", posicao, pai, 0.27f, Rosto(tipo, new Color(0.6f, 0.85f, 0.25f)), 20f);

            case TipoDeInimigo.Sentinela:
                // Quadrada: da pra reconhecer de longe que ela nao anda.
                return Montar<InimigoSentinela>("Sentinela", posicao, pai, 0.36f, Rosto(tipo, new Color(0.45f, 0.55f, 0.72f)), 40f);

            case TipoDeInimigo.Divisor:
                return Montar<InimigoDivisor>("Divisor", posicao, pai, 0.42f, Rosto(tipo, new Color(0.2f, 0.72f, 0.62f)), 30f);

            case TipoDeInimigo.DivisorPequeno:
                InimigoDivisor pedaco = Montar<InimigoDivisor>("Divisor pequeno", posicao, pai, 0.24f, Rosto(tipo, new Color(0.35f, 0.85f, 0.75f)), 10f);
                pedaco.VirarPedaco(2.3f);
                return pedaco;

            case TipoDeInimigo.Demonio:
            {
                // Arte importada (Tiny RPG pack). Sem a imagem, vira uma bola vermelha escura.
                ClipesDePersonagem clipes = ArteImportada.Personagem("Demonio", PixelsDoPersonagem);
                InimigoDemonio demonio = Montar<InimigoDemonio>("Demonio", posicao, pai, 0.34f,
                    (clipes?.Parado[0], clipes != null ? Color.white : new Color(0.6f, 0.1f, 0.1f)), 40f);
                demonio.UsarArte(Animar(demonio, clipes), clipes);
                return demonio;
            }

            case TipoDeInimigo.MonstroDeSangue:
            {
                ClipesDePersonagem clipes = ArteImportada.Personagem("MonstroDeSangue", PixelsDoPersonagem);
                InimigoDeSangue monstro = Montar<InimigoDeSangue>("Monstro de sangue", posicao, pai, 0.36f,
                    (clipes?.Parado[0], clipes != null ? Color.white : new Color(0.55f, 0.08f, 0.2f)), 55f);
                monstro.UsarArte(Animar(monstro, clipes), clipes);
                return monstro;
            }

            // Arte do Tiny Swords. Sem a imagem, cada um vira uma bola da sua cor.
            case TipoDeInimigo.GoblinTocha:
                return ComArte<InimigoDeGolpe>("Goblin da tocha", posicao, pai, 0.3f, 35f,
                    ArteImportada.GoblinDaTocha(InimigoComArte.PixelsDoTinySwords), new Color(0.75f, 0.3f, 0.15f));

            case TipoDeInimigo.GoblinDinamite:
                return ComArte<InimigoGoblinDinamite>("Goblin da dinamite", posicao, pai, 0.32f, 30f,
                    ArteImportada.GoblinDaDinamite(InimigoComArte.PixelsDoTinySwords), new Color(0.3f, 0.6f, 0.3f));

            case TipoDeInimigo.Barril:
                return ComArte<InimigoBarril>("Barril", posicao, pai, 0.32f, 20f,
                    ArteImportada.Barril(InimigoComArte.PixelsDoTinySwords), new Color(0.7f, 0.25f, 0.2f));

            case TipoDeInimigo.Arqueiro:
                return ComArte<InimigoArqueiro>("Arqueiro", posicao, pai, 0.3f, 30f,
                    ArteImportada.Arqueiro(InimigoComArte.PixelsDoTinySwords), new Color(0.3f, 0.3f, 0.4f));

            // Enemy Animations Set: quadros de 32 px com o corpo (uns 16 px) no meio.
            case TipoDeInimigo.Esqueleto:
            {
                InimigoDeGolpe esqueleto = ComArte<InimigoDeGolpe>("Esqueleto", posicao, pai, 0.3f, 35f,
                    ArteImportada.Masmorra("Esqueleto", new Vector2(15f, 22f), PixelsDaMasmorra), new Color(0.85f, 0.82f, 0.7f));
                esqueleto.Ajustar(1.7f, 6, 0.5f, 15f);
                return esqueleto;
            }

            case TipoDeInimigo.EsqueletoFoice:
                return ComArte<InimigoEsqueletoFoice>("Esqueleto da foice", posicao, pai, 0.32f, 45f,
                    ArteImportada.Masmorra("EsqueletoFoice", new Vector2(16f, 22f), PixelsDaMasmorra), new Color(0.75f, 0.72f, 0.65f));

            case TipoDeInimigo.Vampiro:
                return ComArte<InimigoVampiro>("Vampiro", posicao, pai, 0.3f, 40f,
                    ArteImportada.Masmorra("Vampiro", new Vector2(13f, 21f), PixelsDaMasmorra), new Color(0.4f, 0.4f, 0.55f));

            default:
                return Montar<InimigoPerseguidor>("Perseguidor", posicao, pai, 0.3f, Rosto(TipoDeInimigo.Perseguidor, new Color(0.85f, 0.25f, 0.25f)), 25f);
        }
    }

    private static T ComArte<T>(string nome, Vector2 posicao, Transform pai, float raio, float vidaMaxima,
                                ClipesDePersonagem clipes, Color corSemArte)
        where T : InimigoComArte
    {
        T inimigo = Montar<T>(nome, posicao, pai, raio,
            (clipes?.Parado[0], clipes != null ? Color.white : corSemArte), vidaMaxima);
        inimigo.UsarArte(Animar(inimigo, clipes), clipes);
        return inimigo;
    }

    /// <summary>Pixels por unidade do Enemy Animations Set: o corpo (uns 16 px) fica com ~0.9 unidade.</summary>
    private const float PixelsDaMasmorra = 18f;

    /// <summary>Pixels da arte importada por unidade: o corpo (uns 20 px) fica com ~0.9 unidade.</summary>
    private const float PixelsDoPersonagem = 22f;

    /// <summary>
    /// Troca o desenho parado por um animado. A arte importada ja vem no tamanho certo
    /// (pixels por unidade), entao o desenho volta pra escala 1.
    /// </summary>
    private static AnimacaoDePersonagem Animar(InimigoDeSala inimigo, ClipesDePersonagem clipes)
    {
        if (clipes == null)
            return null;

        SpriteRenderer desenho = inimigo.GetComponentInChildren<SpriteRenderer>();
        desenho.transform.localScale = Vector3.one;

        AnimacaoDePersonagem animacao = inimigo.gameObject.AddComponent<AnimacaoDePersonagem>();
        animacao.Configurar(clipes, desenho);
        return animacao;
    }

    /// <summary>A pixel art do bicho (ja colorida): o SpriteRenderer fica branco.</summary>
    private static (Sprite, Color) Rosto(TipoDeInimigo tipo, Color cor) => (ArteGerada.Inimigo(tipo, cor), Color.white);

    private static T Montar<T>(string nome, Vector2 posicao, Transform pai, float raio, Color cor, float vidaMaxima, Sprite arte)
        where T : InimigoDeSala
        => Montar<T>(nome, posicao, pai, raio, (arte, cor), vidaMaxima);

    private static T Montar<T>(string nome, Vector2 posicao, Transform pai, float raio, (Sprite arte, Color cor) aparencia, float vidaMaxima)
        where T : InimigoDeSala
    {
        GameObject obj = new GameObject(nome);
        obj.SetActive(false);
        obj.transform.SetParent(pai, false);
        obj.transform.position = posicao;
        Camadas.Definir(obj, Camadas.Inimigo);

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        CircleCollider2D corpo = obj.AddComponent<CircleCollider2D>();
        corpo.radius = raio;

        Sprite forma = aparencia.arte != null ? aparencia.arte : ArteGerada.Bola();
        FormasDaSala.Desenho(obj.transform, "Desenho", forma, aparencia.cor, Vector2.zero, Vector2.one * raio * 2f, 10);

        Vida vida = obj.AddComponent<Vida>();
        vida.Configurar(vidaMaxima, 0f, true, 0.6f, false);

        T inimigo = obj.AddComponent<T>();

        obj.SetActive(true);
        return inimigo;
    }
}
