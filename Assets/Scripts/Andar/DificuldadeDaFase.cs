using UnityEngine;

/// <summary>
/// Os numeros da dificuldade de uma fase. O jogo e dividido em mundos (Porao, Catacumbas,
/// Cripta, Abismo) com 3 fases cada, e tudo aqui sobe aos poucos a cada fase e um pouco
/// mais a cada mundo novo, pra nunca dar um salto absurdo de uma fase pra outra.
///
///   Nivel 0 = Mundo 1 Fase 1 ... Nivel 11 = Mundo 4 Fase 3.
///
/// O <see cref="Andar"/> monta um destes a cada fase e usa pra povoar as salas, escolher
/// quem aparece, quanto os inimigos aguentam e quanto o jogador ganha.
/// </summary>
public sealed class DificuldadeDaFase
{
    public int Mundo { get; }
    public int Fase { get; }
    public int FasesPorMundo { get; }

    /// <summary>0 na primeira fase do jogo, +1 a cada fase.</summary>
    public int Nivel { get; }

    public DificuldadeDaFase(int mundo, int fase, int fasesPorMundo)
    {
        Mundo = Mathf.Max(1, mundo);
        FasesPorMundo = Mathf.Max(1, fasesPorMundo);
        Fase = Mathf.Clamp(fase, 1, FasesPorMundo);
        Nivel = (Mundo - 1) * FasesPorMundo + (Fase - 1);
    }

    /// <summary>A fase do chefe mais forte do mundo (a ultima).</summary>
    public bool FaseFinalDoMundo => Fase >= FasesPorMundo;

    // ================================================================ inimigos
    /// <summary>Vida dos inimigos comuns: +8% por fase (Mundo 4 Fase 3 = quase o dobro).</summary>
    public float VidaDosInimigos => 1f + 0.08f * Nivel;

    /// <summary>Velocidade dos inimigos: +2,5% por fase, pra ficar mais dificil sem ficar injusto.</summary>
    public float VelocidadeDosInimigos => 1f + 0.025f * Nivel;

    /// <summary>
    /// Vida dos chefes: +6% por fase, e o chefe da ultima fase do mundo vem 15% mais forte
    /// ainda (e o desafio final do mundo).
    /// </summary>
    public float VidaDoChefe => (1f + 0.06f * Nivel) * (FaseFinalDoMundo ? 1.15f : 1f);

    /// <summary>
    /// O Olho do Abismo ja e o chefe mais longo do jogo: ganha no maximo 50% a mais de vida,
    /// pra luta final ser dificil sem virar uma espera cansativa.
    /// </summary>
    public float VidaDoChefeFinal => Mathf.Min(1.5f, VidaDoChefe);

    /// <summary>
    /// Quanto o golpe no jogador pesa. Mundos 1 e 2: meio coracao, como sempre. Mundo 3: um
    /// pouco mais. Mundo 4: coracao inteiro, como nos andares do fim do Isaac. Vale pra
    /// tiro, encostada e armadilha.
    /// </summary>
    public float DanoNoJogador => Mundo >= 4 ? 2f : Mundo == 3 ? 1.5f : 1f;

    /// <summary>Inimigos a mais em cada sala comum: um por mundo, mais um na ultima fase (maximo 3).</summary>
    public int InimigosExtras => Mathf.Min(3, (Mundo - 1) + (FaseFinalDoMundo ? 1 : 0));

    /// <summary>
    /// Quanto da lista de inimigos do tema aparece: a primeira fase do mundo so tem os mais
    /// comuns, e a variedade abre ate a lista inteira na ultima.
    /// </summary>
    public float VariedadeDoTema => FasesPorMundo <= 1 ? 1f : Mathf.Lerp(0.6f, 1f, (Fase - 1f) / (FasesPorMundo - 1f));

    /// <summary>
    /// Chance de um inimigo vir campeao (maior, com cor, mais forte, solta premio). Nada na
    /// primeira fase; depois +3% por fase, no maximo 30%.
    /// </summary>
    public float ChanceDeCampeao => Mathf.Min(0.3f, 0.03f * Nivel);

    /// <summary>Sentinela atirando em oito direcoes: na ultima fase de cada mundo e do mundo 2 em diante.</summary>
    public bool SentinelaEmOitoDirecoes => Mundo >= 2 || FaseFinalDoMundo;

    /// <summary>Monstro de sangue com anel de mais gotas: do mundo 2 em diante.</summary>
    public bool SangueEndurecido => Mundo >= 2;

    // ================================================================ salas e armadilhas
    /// <summary>
    /// Tamanho do mapa que o gerador recebe: cresce a cada duas fases (1 a 6), pra as fases
    /// do fim nao virarem um labirinto sem fim.
    /// </summary>
    public int TamanhoDoMapa => 1 + Nivel / 2;

    /// <summary>
    /// Quanto a chance de uma sala comum vir sem obstaculo nem armadilha cai: 2% por fase.
    /// Mais salas com pedra e espinho no fim do jogo.
    /// </summary>
    public float MenosSalasVazias => 0.02f * Nivel;

    /// <summary>Uma onda a mais na sala de desafio do ultimo mundo.</summary>
    public int OndasExtrasDoDesafio => Mundo >= 4 ? 1 : 0;

    /// <summary>Inimigos a mais por onda do desafio: um a cada 3 fases (maximo 3).</summary>
    public int InimigosExtrasPorOnda => Mathf.Min(3, Nivel / 3);

    // ================================================================ recompensas
    /// <summary>Chance a mais de cada inimigo soltar coletavel: +1% por fase.</summary>
    public float BonusDeDrop => 0.01f * Nivel;

    /// <summary>Chance a mais de premio no meio da sala limpa: +2% por fase.</summary>
    public float BonusDePremioDaSala => 0.02f * Nivel;

    /// <summary>Moedas que o chefe deixa alem do item: uma por mundo, o dobro no chefe final do mundo.</summary>
    public int MoedasDoChefe => Mundo * (FaseFinalDoMundo ? 2 : 1);

    public override string ToString() =>
        $"Mundo {Mundo} Fase {Fase} (nivel {Nivel}): vida x{VidaDosInimigos:0.00}, velocidade x{VelocidadeDosInimigos:0.00}, " +
        $"chefe x{VidaDoChefe:0.00}, dano no jogador x{DanoNoJogador:0.0}, +{InimigosExtras} inimigos, campeao {ChanceDeCampeao:P0}";
}
