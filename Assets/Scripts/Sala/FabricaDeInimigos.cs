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
    Vampiro,
    Morcego,
    CavaleiroLanca,
    CavaleiroEscudo,
    DemonioTridente,
    DemonioLaminas,
    DemonioArqueiro,
    Demonia,
    FogoFatuo,
    DemoniaFoice,
    Orc,
    OrcBlindado,
    OrcElite,
    OrcMontado,
    EsqueletoGuerreiro,
    EsqueletoBlindado,
    EsqueletoEspadao,
    EsqueletoArqueiro,
    Geleia,
    Morceguinho,
    Lobisomem,
    Urso,
    Necromante,
    ChefeNecromante,
    ChefeMinotauro,
    ChefeLobisomem,
    ChefeOrc
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
        InimigoDeSala inimigo = MontarPorTipo(tipo, posicao, pai);

        if (inimigo != null)
        {
            inimigo.SomDeMorte = SomDeMorte(tipo);
            inimigo.Tipo = tipo;

            // Quem voa passa por cima dos buracos do chao, como os tiros.
            if (Voa(tipo))
                Fosso.Sobrevoar(inimigo.GetComponent<Collider2D>());
        }

        return inimigo;
    }

    /// <summary>Bicho que voa (morcegos e fogo-fatuo): os buracos do chao nao seguram.</summary>
    public static bool Voa(TipoDeInimigo tipo)
        => tipo == TipoDeInimigo.Morcego || tipo == TipoDeInimigo.Morceguinho || tipo == TipoDeInimigo.FogoFatuo;

    /// <summary>Como o bicho morre no ouvido: ossos caindo, gosma, grito de demonio, fera, chefe...</summary>
    public static Som SomDeMorte(TipoDeInimigo tipo)
    {
        switch (tipo)
        {
            case TipoDeInimigo.Chefe:
            case TipoDeInimigo.ChefeSaltador:
            case TipoDeInimigo.ChefeFinal:
            case TipoDeInimigo.ChefeNecromante:
            case TipoDeInimigo.ChefeMinotauro:
            case TipoDeInimigo.ChefeLobisomem:
            case TipoDeInimigo.ChefeOrc:
                return Som.MorteChefe;

            case TipoDeInimigo.Esqueleto:
            case TipoDeInimigo.EsqueletoFoice:
            case TipoDeInimigo.EsqueletoGuerreiro:
            case TipoDeInimigo.EsqueletoBlindado:
            case TipoDeInimigo.EsqueletoEspadao:
            case TipoDeInimigo.EsqueletoArqueiro:
                return Som.MorteOssos;

            case TipoDeInimigo.Saltador:
            case TipoDeInimigo.Divisor:
            case TipoDeInimigo.DivisorPequeno:
            case TipoDeInimigo.Geleia:
            case TipoDeInimigo.MonstroDeSangue:
                return Som.MorteGosma;

            case TipoDeInimigo.Demonio:
            case TipoDeInimigo.DemonioTridente:
            case TipoDeInimigo.DemonioLaminas:
            case TipoDeInimigo.DemonioArqueiro:
            case TipoDeInimigo.Demonia:
            case TipoDeInimigo.DemoniaFoice:
            case TipoDeInimigo.FogoFatuo:
                return Som.MorteDemonio;

            case TipoDeInimigo.Perseguidor:
            case TipoDeInimigo.Investidor:
            case TipoDeInimigo.Lobisomem:
            case TipoDeInimigo.Urso:
            case TipoDeInimigo.Morcego:
            case TipoDeInimigo.Morceguinho:
            case TipoDeInimigo.Vampiro:
                return Som.MorteFera;

            default:
                return Som.MorteInimigo;
        }
    }

    private static InimigoDeSala MontarPorTipo(TipoDeInimigo tipo, Vector2 posicao, Transform pai)
    {
        switch (tipo)
        {
            // Chefes: bichos grandes do Tiny RPG. Sem a imagem, voltam a bola com olhos.
            case TipoDeInimigo.Chefe:
            {
                ClipesDePersonagem clipes = Tiny("Golem", 17f);
                ChefeDoAndar chefe = Montar<ChefeDoAndar>("Chefe", posicao, pai, 0.75f,
                    (ArteGerada.Bola(), new Color(0.55f, 0.12f, 0.16f)), 120f, clipes, clipes?.AtaqueEspecial);
                chefe.Enfeitar(0.75f, clipes != null);
                return chefe;
            }

            case TipoDeInimigo.ChefeSaltador:
            {
                ClipesDePersonagem clipes = Tiny("DemonioMartelo", 13f);
                ChefeSaltador demonio = Montar<ChefeSaltador>("Chefe Saltador", posicao, pai, 0.8f,
                    (ArteGerada.Bola(), new Color(0.25f, 0.5f, 0.22f)), 150f, clipes, clipes?.AtaqueForte);
                demonio.Enfeitar(clipes != null);
                return demonio;
            }

            case TipoDeInimigo.ChefeNecromante:
            {
                ClipesDePersonagem clipes = Tiny("Necromante", 14f);
                ChefeNecromante rei = Montar<ChefeNecromante>("Chefe Necromante", posicao, pai, 0.6f,
                    (ArteGerada.Bola(), new Color(0.35f, 0.2f, 0.55f)), 200f, clipes);
                rei.Enfeitar(clipes);
                return rei;
            }

            case TipoDeInimigo.ChefeMinotauro:
            {
                ClipesDePersonagem clipes = Tiny("Minotauro", 13f);
                ChefeMinotauro touro = Montar<ChefeMinotauro>("Chefe Minotauro", posicao, pai, 0.7f,
                    (ArteGerada.Bola(), new Color(0.6f, 0.35f, 0.2f)), 130f, clipes);
                touro.Enfeitar(clipes);
                return touro;
            }

            case TipoDeInimigo.ChefeLobisomem:
            {
                ClipesDePersonagem clipes = Tiny("Lobisomem", 11f);
                ChefeLobisomem lobo = Montar<ChefeLobisomem>("Chefe Lobisomem", posicao, pai, 0.6f,
                    (ArteGerada.Bola(), new Color(0.45f, 0.4f, 0.5f)), 140f, clipes);
                lobo.Enfeitar(clipes);
                return lobo;
            }

            case TipoDeInimigo.ChefeOrc:
            {
                ClipesDePersonagem clipes = Tiny("OrcElite", 11f);
                ChefeOrc orc = Montar<ChefeOrc>("Chefe Orc", posicao, pai, 0.7f,
                    (ArteGerada.Bola(), new Color(0.4f, 0.55f, 0.3f)), 170f, clipes);
                orc.Enfeitar(clipes);
                return orc;
            }

            case TipoDeInimigo.ChefeFinal:
            {
                ClipesDePersonagem clipes = Tiny("Olho", 9f);   // bicho de 16 px: maior que isto fica quadriculado
                ChefeFinal olho = Montar<ChefeFinal>("Chefe Final", posicao, pai, 1f,
                    (ArteGerada.Bola(), new Color(0.32f, 0.1f, 0.28f)), 450f, clipes, clipes?.AtaqueForte);
                olho.Enfeitar(1f, clipes != null);
                return olho;
            }

            // Os inimigos antigos ganharam bicho do Tiny RPG (o bruxo agora some e reaparece).
            case TipoDeInimigo.Atirador:
                return Antigo<InimigoBruxo>("Atirador", tipo, posicao, pai, 0.32f, new Color(0.62f, 0.35f, 0.85f), 30f, "Bruxo", 28f);

            case TipoDeInimigo.Investidor:
                return Antigo<InimigoInvestidor>("Investidor", tipo, posicao, pai, 0.33f, new Color(0.95f, 0.55f, 0.15f), 35f, "Minotauro", 28f);

            case TipoDeInimigo.Saltador:
                return Antigo<InimigoSaltador>("Saltador", tipo, posicao, pai, 0.27f, new Color(0.6f, 0.85f, 0.25f), 20f, "Gosma", 28f, false);

            case TipoDeInimigo.Sentinela:
                return Antigo<InimigoSentinela>("Sentinela", tipo, posicao, pai, 0.36f, new Color(0.45f, 0.55f, 0.72f), 40f, "CavaleiroCanhao", 34f);

            case TipoDeInimigo.Divisor:
                return Antigo<InimigoDivisor>("Divisor", tipo, posicao, pai, 0.42f, new Color(0.2f, 0.72f, 0.62f), 30f, "Bolha", 16f);

            case TipoDeInimigo.DivisorPequeno:
                InimigoDivisor pedaco = Antigo<InimigoDivisor>("Divisor pequeno", tipo, posicao, pai, 0.24f, new Color(0.35f, 0.85f, 0.75f), 10f, "Bolha", 28f);
                pedaco.VirarPedaco(2.3f);
                return pedaco;

            case TipoDeInimigo.Demonio:
            {
                // Arte importada (Tiny RPG pack). Sem a imagem, vira uma bola vermelha escura.
                ClipesDePersonagem clipes = Tiny("Demonio", PixelsDoPersonagem);
                InimigoDemonio demonio = Montar<InimigoDemonio>("Demonio", posicao, pai, 0.34f,
                    (null, new Color(0.6f, 0.1f, 0.1f)), 40f, clipes);
                demonio.UsarArte(demonio.GetComponent<AnimacaoDePersonagem>(), clipes);
                demonio.JeitoDeChegar = InimigoDeSala.Aproximacao.Flanco;
                return demonio;
            }

            case TipoDeInimigo.MonstroDeSangue:
            {
                ClipesDePersonagem clipes = Tiny("MonstroDeSangue", PixelsDoPersonagem);
                InimigoDeSangue monstro = Montar<InimigoDeSangue>("Monstro de sangue", posicao, pai, 0.36f,
                    (null, new Color(0.55f, 0.08f, 0.2f)), 55f, clipes);
                monstro.UsarArte(monstro.GetComponent<AnimacaoDePersonagem>(), clipes);
                monstro.JeitoDeChegar = InimigoDeSala.Aproximacao.PassoPesado;
                return monstro;
            }

            // Arte do Tiny Swords. Sem a imagem, cada um vira uma bola da sua cor.
            case TipoDeInimigo.GoblinTocha:
            {
                InimigoDeGolpe goblin = ComArte<InimigoDeGolpe>("Goblin da tocha", posicao, pai, 0.3f, 35f,
                    ArteImportada.GoblinDaTocha(InimigoComArte.PixelsDoTinySwords), new Color(0.75f, 0.3f, 0.15f));
                goblin.JeitoDeChegar = InimigoDeSala.Aproximacao.Ziguezague;
                return goblin;
            }

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
                esqueleto.JeitoDeChegar = InimigoDeSala.Aproximacao.Cambaleante;
                return esqueleto;
            }

            case TipoDeInimigo.EsqueletoFoice:
            {
                InimigoEsqueletoFoice foice = ComArte<InimigoEsqueletoFoice>("Esqueleto da foice", posicao, pai, 0.32f, 45f,
                    ArteImportada.Masmorra("EsqueletoFoice", new Vector2(16f, 22f), PixelsDaMasmorra), new Color(0.75f, 0.72f, 0.65f));
                foice.JeitoDeChegar = InimigoDeSala.Aproximacao.Cambaleante;
                return foice;
            }

            case TipoDeInimigo.Vampiro:
            {
                InimigoVampiro vampiro = ComArte<InimigoVampiro>("Vampiro", posicao, pai, 0.3f, 40f,
                    ArteImportada.Masmorra("Vampiro", new Vector2(13f, 21f), PixelsDaMasmorra), new Color(0.4f, 0.4f, 0.55f));
                vampiro.JeitoDeChegar = InimigoDeSala.Aproximacao.Flanco;
                return vampiro;
            }

            // O resto do Tiny RPG: cada especie com o seu jeito de lutar.
            case TipoDeInimigo.Morcego:
            {
                InimigoMorcego morcego = Antigo<InimigoMorcego>("Morcego", TipoDeInimigo.Perseguidor, posicao, pai, 0.26f,
                    new Color(0.7f, 0.2f, 0.3f), 15f, "Morcego", 30f);
                morcego.UsarVoo(InimigoMorcego.Voo.Mergulho);
                return morcego;
            }

            case TipoDeInimigo.CavaleiroLanca:
                return Golpe<InimigoLanceiro>("Cavaleiro da lanca", posicao, pai, 0.3f, 45f, "CavaleiroLanca", 30f, 1.5f, 12, 0.7f, 20f);

            case TipoDeInimigo.CavaleiroEscudo:
                return Golpe<InimigoEscudeiro>("Cavaleiro do escudo", posicao, pai, 0.32f, 70f, "CavaleiroEscudo", 28f, 1.3f, 5, 0.45f, 15f);

            case TipoDeInimigo.DemonioTridente:
                return Golpe<InimigoTridente>("Demonio do tridente", posicao, pai, 0.32f, 40f, "DemonioTridente", 30f, 2.1f, 4, 0.4f, 15f);

            case TipoDeInimigo.DemonioLaminas:
                return Golpe<InimigoDuelista>("Demonio das laminas", posicao, pai, 0.34f, 45f, "DemonioLaminas", 32f, 2.5f, 6, 0.4f, 12f);

            case TipoDeInimigo.DemonioArqueiro:
            {
                InimigoArqueiro arqueiro = ComArte<InimigoArqueiro>("Demonio arqueiro", posicao, pai, 0.3f, 30f,
                    Tiny("DemonioArqueiro", PixelsDoPersonagem), new Color(0.6f, 0.15f, 0.15f));
                arqueiro.UsarMira(InimigoArqueiro.Mira.Leque);
                return arqueiro;
            }

            case TipoDeInimigo.Demonia:
                return Antigo<InimigoDemonia>("Demonia", TipoDeInimigo.Atirador, posicao, pai, 0.32f,
                    new Color(0.7f, 0.2f, 0.5f), 35f, "Demonia", 26f);

            case TipoDeInimigo.FogoFatuo:
            {
                return Antigo<InimigoFogoFatuo>("Fogo fatuo", TipoDeInimigo.Atirador, posicao, pai, 0.28f,
                    new Color(0.3f, 0.8f, 1f), 25f, "FogoFatuo", 28f);
            }

            case TipoDeInimigo.DemoniaFoice:
            {
                // O giro duplo dela e o segundo ataque (15 quadros, igual ao do esqueleto da foice).
                ClipesDePersonagem clipes = Tiny("DemoniaFoice", 30f);

                if (clipes?.AtaqueEspecial != null)
                    clipes.Ataque = clipes.AtaqueEspecial;

                InimigoEsqueletoFoice demonia = ComArte<InimigoEsqueletoFoice>("Demonia da foice", posicao, pai, 0.34f, 55f, clipes, new Color(0.5f, 0.1f, 0.3f));
                demonia.JeitoDeChegar = InimigoDeSala.Aproximacao.Cerco;
                return demonia;
            }

            // Tiny RPG Pack 01: orcs, esqueletos e feras, cada um com o seu jeito.
            case TipoDeInimigo.Orc:
            {
                InimigoDeGolpe orc = Golpe("Orc", posicao, pai, 0.3f, 35f, "Orc", 26f, 2f, 3, 0.4f, 15f);
                orc.JeitoDeChegar = InimigoDeSala.Aproximacao.PassoPesado;
                return orc;
            }

            case TipoDeInimigo.OrcBlindado:
                return Golpe<InimigoBlindado>("Orc blindado", posicao, pai, 0.32f, 70f, "OrcBlindado", 26f, 1.4f, 4, 0.5f, 18f);

            case TipoDeInimigo.OrcElite:
                return Golpe<InimigoFurioso>("Orc de elite", posicao, pai, 0.34f, 60f, "OrcElite", 26f, 2f, 3, 0.4f, 20f);

            case TipoDeInimigo.OrcMontado:
                return Antigo<InimigoInvestidor>("Orc montado", TipoDeInimigo.Investidor, posicao, pai, 0.36f,
                    new Color(0.5f, 0.6f, 0.3f), 45f, "OrcMontado", 28f);

            case TipoDeInimigo.EsqueletoGuerreiro:
            {
                InimigoDeGolpe guerreiro = Golpe("Esqueleto guerreiro", posicao, pai, 0.3f, 30f, "EsqueletoGuerreiro", 26f, 1.9f, 3, 0.4f, 12f);
                guerreiro.JeitoDeChegar = InimigoDeSala.Aproximacao.Cerco;
                return guerreiro;
            }

            case TipoDeInimigo.EsqueletoBlindado:
                return Golpe<InimigoBlindado>("Esqueleto blindado", posicao, pai, 0.3f, 60f, "EsqueletoBlindado", 26f, 1.4f, 4, 0.5f, 15f);

            case TipoDeInimigo.EsqueletoEspadao:
                return Golpe<InimigoEspadao>("Esqueleto do espadao", posicao, pai, 0.33f, 55f, "EsqueletoEspadao", 26f, 1.6f, 5, 0.6f, 22f);

            case TipoDeInimigo.EsqueletoArqueiro:
            {
                InimigoArqueiro arqueiro = ComArte<InimigoArqueiro>("Esqueleto arqueiro", posicao, pai, 0.3f, 25f,
                    Tiny("EsqueletoArqueiro", 26f), new Color(0.8f, 0.8f, 0.7f));
                arqueiro.UsarMira(InimigoArqueiro.Mira.Alinhada);
                return arqueiro;
            }

            case TipoDeInimigo.Geleia:
            {
                InimigoSaltador geleia = Antigo<InimigoSaltador>("Geleia", TipoDeInimigo.Saltador, posicao, pai, 0.27f,
                    new Color(0.4f, 0.8f, 0.4f), 20f, "Geleia", 28f, false);
                geleia.VirarGeleia();
                return geleia;
            }

            case TipoDeInimigo.Morceguinho:
            {
                InimigoMorcego bicho = Antigo<InimigoMorcego>("Morceguinho", TipoDeInimigo.Perseguidor, posicao, pai, 0.24f,
                    new Color(0.4f, 0.3f, 0.5f), 10f, "Morceguinho", 32f);
                bicho.UsarVoo(InimigoMorcego.Voo.Enxame);
                return bicho;
            }

            case TipoDeInimigo.Lobisomem:
                return Golpe<InimigoLobisomem>("Lobisomem", posicao, pai, 0.32f, 45f, "Lobisomem", 26f, 2.8f, 5, 0.35f, 15f);

            case TipoDeInimigo.Urso:
                return Golpe<InimigoUrso>("Urso", posicao, pai, 0.38f, 90f, "Urso", 22f, 1.5f, 5, 0.55f, 25f);

            case TipoDeInimigo.Necromante:
                return Antigo<InimigoNecromante>("Necromante", TipoDeInimigo.Atirador, posicao, pai, 0.32f,
                    new Color(0.4f, 0.2f, 0.6f), 40f, "Necromante", 28f);

            default:
            {
                InimigoCao cao = Antigo<InimigoCao>("Perseguidor", TipoDeInimigo.Perseguidor, posicao, pai, 0.3f,
                    new Color(0.85f, 0.25f, 0.25f), 25f, "CaoInfernal", 30f);
                return cao;
            }
        }
    }

    private static ClipesDePersonagem Tiny(string pasta, float pixelsPorUnidade) => ArteImportada.Personagem(pasta, pixelsPorUnidade);

    /// <summary>
    /// Inimigo antigo com um bicho do Tiny RPG no lugar da pixel art gerada. O ataque toca
    /// sozinho quando ele entra no telegrafo. Sem a imagem, volta pro rosto gerado.
    /// </summary>
    /// <param name="comAtaque">
    /// Falso pra quem nao ataca com o corpo (gosma e geleia so pulam): sem isso a animacao de
    /// ataque tocava no meio do pulo.
    /// </param>
    private static T Antigo<T>(string nome, TipoDeInimigo tipo, Vector2 posicao, Transform pai, float raio, Color cor,
                               float vidaMaxima, string pasta, float pixelsPorUnidade, bool comAtaque = true)
        where T : InimigoDeSala
    {
        ClipesDePersonagem clipes = Tiny(pasta, pixelsPorUnidade);
        return Montar<T>(nome, posicao, pai, raio, Rosto(tipo, cor), vidaMaxima, clipes, comAtaque ? clipes?.Ataque : null);
    }

    private static InimigoDeGolpe Golpe(string nome, Vector2 posicao, Transform pai, float raio, float vidaMaxima,
                                        string pasta, float pixelsPorUnidade, float velocidade, int quadroDoGolpe,
                                        float preparo, float dano)
        => Golpe<InimigoDeGolpe>(nome, posicao, pai, raio, vidaMaxima, pasta, pixelsPorUnidade, velocidade, quadroDoGolpe, preparo, dano);

    /// <summary>Lutador de perto com arte do Tiny RPG, no comportamento <typeparamref name="T"/> da especie.</summary>
    private static T Golpe<T>(string nome, Vector2 posicao, Transform pai, float raio, float vidaMaxima,
                              string pasta, float pixelsPorUnidade, float velocidade, int quadroDoGolpe,
                              float preparo, float dano)
        where T : InimigoDeGolpe
    {
        T inimigo = ComArte<T>(nome, posicao, pai, raio, vidaMaxima,
            Tiny(pasta, pixelsPorUnidade), new Color(0.3f, 0.3f, 0.35f));
        inimigo.Ajustar(velocidade, quadroDoGolpe, preparo, dano);
        return inimigo;
    }

    private static T ComArte<T>(string nome, Vector2 posicao, Transform pai, float raio, float vidaMaxima,
                                ClipesDePersonagem clipes, Color corSemArte)
        where T : InimigoComArte
    {
        T inimigo = Montar<T>(nome, posicao, pai, raio, (null, corSemArte), vidaMaxima, clipes);
        inimigo.UsarArte(inimigo.GetComponent<AnimacaoDePersonagem>(), clipes);
        return inimigo;
    }

    /// <summary>Pixels por unidade do Enemy Animations Set: o corpo (uns 16 px) fica com ~0.9 unidade.</summary>
    private const float PixelsDaMasmorra = 18f;

    /// <summary>Pixels da arte importada por unidade: o corpo (uns 20 px) fica com ~0.9 unidade.</summary>
    private const float PixelsDoPersonagem = 22f;

    /// <summary>Cor do contorno de todo inimigo: clara e meio transparente, pra marcar sem brilhar.</summary>
    private static readonly Color CorDoContorno = new Color(1f, 0.95f, 0.85f, 0.6f);

    /// <summary>A pixel art do bicho (ja colorida): o SpriteRenderer fica branco.</summary>
    private static (Sprite, Color) Rosto(TipoDeInimigo tipo, Color cor) => (ArteGerada.Inimigo(tipo, cor), Color.white);

    /// <summary>
    /// Monta o inimigo. Com <paramref name="clipes"/>, o desenho e o bicho animado (ja no
    /// tamanho, escala 1) e <paramref name="ataqueSozinho"/> toca a cada telegrafo; sem, e
    /// a <paramref name="aparencia"/> esticada no diametro do colisor.
    /// </summary>
    private static T Montar<T>(string nome, Vector2 posicao, Transform pai, float raio, (Sprite arte, Color cor) aparencia,
                               float vidaMaxima, ClipesDePersonagem clipes = null, Sprite[] ataqueSozinho = null)
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

        Vida vida = obj.AddComponent<Vida>();
        vida.Configurar(vidaMaxima, 0f, true, 0.6f, false);

        // O tamanho do desenho fica certo ANTES de ligar: quem agacha ou estica (saltador,
        // divisor, chefe) guarda a escala do desenho no Awake.
        SpriteRenderer desenho;

        if (clipes != null)
        {
            desenho = FormasDaSala.Desenho(obj.transform, "Desenho", clipes.Parado[0], Color.white, Vector2.zero, Vector2.one, 10);
            AnimacaoDePersonagem animacao = obj.AddComponent<AnimacaoDePersonagem>();
            animacao.Configurar(clipes, desenho);

            if (ataqueSozinho != null)
                animacao.AtacarSozinho(ataqueSozinho);
        }
        else
        {
            Sprite forma = aparencia.arte != null ? aparencia.arte : ArteGerada.Bola();
            desenho = FormasDaSala.Desenho(obj.transform, "Desenho", forma, aparencia.cor, Vector2.zero, Vector2.one * raio * 2f, 10);
        }

        T inimigo = obj.AddComponent<T>();

        obj.SetActive(true);

        // Borda clara de 1 pixel: bicho escuro (morcego roxo, vampiro) nao some no chao.
        ContornoClaro.Criar(desenho, CorDoContorno);
        return inimigo;
    }
}
