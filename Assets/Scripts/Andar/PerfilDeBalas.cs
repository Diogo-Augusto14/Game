using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quem atira o que. Um lugar so pra decidir os padroes de bala de cada inimigo e como eles ficam
/// mais densos a cada fase, pra dar pra equilibrar sem cacar numero espalhado. O
/// <see cref="Andar"/> chama <see cref="Equipar"/> em cada inimigo comum que cria.
///
///   - Os atiradores de verdade (bruxo, sentinela, fogo-fatuo, demonia) continuam mandando no
///     proprio aviso e no proprio andar; aqui so se define o ciclo de padroes que eles soltam.
///   - Alguns bichos que so perseguiam ganham um ataque de bala, no ritmo deles (gosma: anel;
///     orc: rajada; demonia da foice: espiral...). Os outros ficam como eram, pra a sala nao virar
///     so tiro: a variedade de salas e o que faz o desvio valer.
///   - Todo campeao (<see cref="Campeao"/>) ganha um padrao da cor dele.
///
/// O que sobe com a dificuldade (<see cref="DificuldadeDaFase.DensidadeDeBalas"/>,
/// <see cref="DificuldadeDaFase.PressaDasBalas"/>, <see cref="DificuldadeDaFase.RitmoDeTiro"/>) entra
/// nas contas daqui; os chefes tem os padroes deles e nao passam por esta tabela.
/// </summary>
public static class PerfilDeBalas
{
    // As cores das balas, as mesmas dos tiros que cada inimigo ja tinha.
    private static readonly Color CorDoBruxo = new Color(0.75f, 0.4f, 1f);
    private static readonly Color CorDaSentinela = new Color(0.55f, 0.8f, 1f);
    private static readonly Color CorDoFogoFatuo = new Color(0.4f, 0.85f, 1f);
    private static readonly Color CorDaDemonia = new Color(1f, 0.5f, 0.3f);
    private static readonly Color CorDoOrc = new Color(0.95f, 0.62f, 0.25f);
    private static readonly Color CorDaGosma = new Color(0.6f, 0.9f, 0.3f);
    private static readonly Color CorDaBolha = new Color(0.3f, 0.85f, 0.75f);
    private static readonly Color CorDaLanca = new Color(0.8f, 0.85f, 0.95f);
    private static readonly Color CorDoOrcDeElite = new Color(1f, 0.45f, 0.2f);
    private static readonly Color CorDoDemonio = new Color(0.9f, 0.2f, 0.25f);
    private static readonly Color CorDaFoice = new Color(0.95f, 0.25f, 0.4f);

    /// <summary>Poe no inimigo o que a tabela manda pra especie dele nesta fase (nada, pra quem nao atira).</summary>
    public static void Equipar(InimigoDeSala inimigo, DificuldadeDaFase d)
    {
        if (inimigo == null || d == null)
            return;

        // Os atiradores de verdade: o ciclo de padroes, chamado por eles (nao conta o proprio intervalo).
        List<AtaqueDeBalas> doAtirador = CicloDoAtirador(inimigo, d);

        if (doAtirador != null)
        {
            AtiradorDePadroes.Em(inimigo).Configurar(doAtirador, false, 0f, 0f);
            return;
        }

        // Os bichos de perto que ganham um ataque de bala.
        List<AtaqueDeBalas> doBicho = CicloDoBicho(inimigo.Tipo, d, out float pausa, out float atraso);

        if (doBicho != null)
            AtiradorDePadroes.Em(inimigo).Configurar(doBicho, true, Pausa(pausa, d), atraso);
    }

    /// <summary>O campeao ganha um padrao da cor dele, alem do que a especie ja tem.</summary>
    public static void DarPadraoDeCampeao(InimigoDeSala inimigo, Campeao.Tipo qual, DificuldadeDaFase d)
    {
        if (inimigo == null || d == null || inimigo.EstadoAtual == InimigoDeSala.Estado.Morto)
            return;

        AtaqueDeBalas ataque;
        float pausa;

        switch (qual)
        {
            case Campeao.Tipo.Forte:
                // O vermelho: um anel, de vez em quando.
                ataque = Anel(8, 3.2f, new Color(1f, 0.4f, 0.4f), d, 0.5f);
                pausa = 6f;
                break;

            case Campeao.Tipo.Rapido:
                // O amarelo: uma rajada mirada.
                ataque = Rajada(3, 5f, new Color(1f, 0.9f, 0.35f), d, 0.45f);
                pausa = 4.5f;
                break;

            default:
                // O roxo: espiral (so aparece do mundo 3 em diante).
                ataque = Espiral(14, 2, 18f, 3.2f, new Color(0.75f, 0.5f, 1f), d, 0.6f);
                pausa = 6.5f;
                break;
        }

        AtiradorDePadroes.Em(inimigo).Adicionar(ataque, Pausa(pausa, d), 2.5f);
    }

    // ---------------- os atiradores de verdade ----------------
    private static List<AtaqueDeBalas> CicloDoAtirador(InimigoDeSala inimigo, DificuldadeDaFase d)
    {
        // O aviso deles e o proprio (sentinela incha, bruxo ergue o cajado, demonia abre os bracos):
        // por isso aviso = 0 aqui.
        switch (inimigo)
        {
            case InimigoBruxo _:
            {
                // O leque de sempre, que cresce; do mundo 2 um anel com buraco; do 3 uma rajada.
                List<AtaqueDeBalas> ciclo = new List<AtaqueDeBalas> { Leque(3, 16f, 4.5f, CorDoBruxo, d) };

                if (d.PadroesComplicados)
                    ciclo.Add(AnelComBuraco(12, 3.2f, CorDoBruxo, d));

                if (d.Mundo >= 3)
                    ciclo.Add(Rajada(3, 5.2f, CorDoBruxo, d, 0f));

                return ciclo;
            }

            case InimigoSentinela _:
            {
                // A cruz que alterna "+" e "x"; nos mundos fundos um anel de oito e uma espiral lenta.
                List<AtaqueDeBalas> ciclo = new List<AtaqueDeBalas> { Cruz(3.8f, CorDaSentinela, d) };

                if (d.SentinelaEmOitoDirecoes)
                    ciclo.Insert(0, Anel(8, 3.6f, CorDaSentinela, d, 0f));

                if (d.PadroesComplicados)
                    ciclo.Add(Espiral(16, 2, 17f, 3.3f, CorDaSentinela, d, 0f));

                return ciclo;
            }

            case InimigoFogoFatuo _:
            {
                List<AtaqueDeBalas> ciclo = new List<AtaqueDeBalas> { Cruz(3.6f, CorDoFogoFatuo, d), AnelComBuraco(12, 3.2f, CorDoFogoFatuo, d) };

                if (d.PadroesComplicados)
                    ciclo.Add(Espiral(14, 2, 19f, 3.2f, CorDoFogoFatuo, d, 0f));

                return ciclo;
            }

            case InimigoDemonia _:
            {
                List<AtaqueDeBalas> ciclo = new List<AtaqueDeBalas> { Leque(3, 20f, 4.5f, CorDaDemonia, d) };

                if (d.PadroesComplicados)
                {
                    ciclo.Add(Cortina(7, 2.9f, CorDaDemonia, d));
                    ciclo.Add(Rosa(5, 3.1f, CorDaDemonia, d));
                }

                return ciclo;
            }

            default:
                return null;
        }
    }

    // ---------------- os bichos de perto que ganham um ataque ----------------
    private static List<AtaqueDeBalas> CicloDoBicho(TipoDeInimigo tipo, DificuldadeDaFase d, out float pausa, out float atraso)
    {
        pausa = 5f;
        atraso = 2f;

        switch (tipo)
        {
            // Mundo 1 (Porao)
            case TipoDeInimigo.Orc:
                pausa = 5.2f;
                return new List<AtaqueDeBalas> { Rajada(2, 4.4f, CorDoOrc, d, 0.55f) };

            case TipoDeInimigo.Saltador:
                pausa = 4.4f;
                return new List<AtaqueDeBalas> { Anel(6, 2.8f, CorDaGosma, d, 0.5f) };

            case TipoDeInimigo.Divisor:
                pausa = 5.5f;
                return new List<AtaqueDeBalas> { Anel(6, 2.8f, CorDaBolha, d, 0.5f) };

            // Mundo 2 (Catacumbas)
            case TipoDeInimigo.CavaleiroLanca:
                pausa = 6f;
                return new List<AtaqueDeBalas> { Rajada(2, 5.5f, CorDaLanca, d, 0.5f) };

            case TipoDeInimigo.OrcElite:
                pausa = 4.8f;
                return new List<AtaqueDeBalas> { Leque(5, 16f, 4.2f, CorDoOrcDeElite, d, 0.55f) };

            // Mundo 4 (Abismo)
            case TipoDeInimigo.Demonio:
                pausa = 4.5f;
                return new List<AtaqueDeBalas> { Leque(3, 18f, 4.5f, CorDoDemonio, d, 0.5f) };

            case TipoDeInimigo.DemoniaFoice:
                pausa = 6f;
                atraso = 2.5f;
                return new List<AtaqueDeBalas> { Espiral(16, 2, 17f, 3.3f, CorDaFoice, d, 0.6f) };

            default:
                return null;
        }
    }

    // ---------------- o ataque extra de cada chefe ----------------
    /// <summary>Segundos do aviso do ataque extra de um chefe (o corpo incha e os olhos mudam de cor).</summary>
    public const float AvisoDoExtra = 0.9f;

    /// <summary>A cor do aviso (e das balas) do ataque extra de cada chefe.</summary>
    public static Color CorDoExtra(TipoDeInimigo chefe)
    {
        switch (chefe)
        {
            case TipoDeInimigo.Chefe: return new Color(1f, 0.35f, 0.2f);
            case TipoDeInimigo.ChefeSaltador: return new Color(0.55f, 0.95f, 0.35f);
            case TipoDeInimigo.ChefeNecromante: return new Color(0.75f, 0.4f, 1f);
            case TipoDeInimigo.ChefeMinotauro: return new Color(0.95f, 0.7f, 0.4f);
            case TipoDeInimigo.ChefeLobisomem: return new Color(0.85f, 0.8f, 1f);
            case TipoDeInimigo.ChefeOrc: return new Color(0.95f, 0.8f, 0.5f);
            default: return new Color(1f, 0.35f, 0.45f);
        }
    }

    /// <summary>
    /// A fila de padroes do ataque extra de um chefe, nesta fase da luta (1 = a primeira, 2 = abaixo da
    /// metade da vida; o Olho do Abismo tem uma terceira). Cada chefe tem o seu, de um desenho que
    /// os outros nao tem: o Golem floresce, o Demonio levanta uma muralha de brasa, o Rei Necromante
    /// gira uma maldicao, o Minotauro faz ondas duplas, o Lobisomem uiva a lua, o Senhor da Guerra
    /// chove lancas e o Olho abre uma flor atras da outra. Montado de novo a cada vez (os angulos sao
    /// sorteados). <paramref name="d"/> null = a primeira fase.
    /// </summary>
    public static List<PassoDeBalas> ExtraDoChefe(TipoDeInimigo chefe, DificuldadeDaFase d, int fase)
    {
        if (d == null)
            d = new DificuldadeDaFase(1, 1, 3);

        Color cor = CorDoExtra(chefe);
        bool dura = fase >= 2;
        float comeco = Random.value * 360f;
        List<PassoDeBalas> fila = new List<PassoDeBalas>();

        switch (chefe)
        {
            case TipoDeInimigo.Chefe:
            {
                // Florescer de magma: uma rosa, e na segunda fase outra logo depois, encaixada nos vaos da primeira.
                int petalas = Mathf.Clamp(Mathf.RoundToInt((dura ? 6 : 5) * Mathf.Min(1.3f, d.DensidadeDeBalas)), 5, 8);
                fila.Add(Passo(RosaNoAngulo(petalas, 3.3f, cor, d, comeco), 0.65f));

                if (dura)
                    fila.Add(Passo(RosaNoAngulo(petalas, 3.3f, cor, d, comeco + 180f / petalas), 0.65f));

                break;
            }

            case TipoDeInimigo.ChefeSaltador:
            {
                // Muralha de brasa: uma cortina larga indo pro jogador, com o buraco em lugares diferentes.
                fila.Add(Passo(Cortina(9, 3f, cor, d), 0.9f));
                fila.Add(Passo(Cortina(9, 3f, cor, d), 0.9f));

                if (dura)
                    fila.Add(Passo(Cortina(11, 3.2f, cor, d), 0.5f));

                break;
            }

            case TipoDeInimigo.ChefeNecromante:
            {
                // Maldicao girando: tres fios lentos; na segunda fase quatro, e fecha com um anel com buraco.
                fila.Add(Passo(EspiralNoAngulo(20, dura ? 4 : 3, 15f, 3f, cor, d, comeco), 0.3f));

                if (dura)
                    fila.Add(Passo(AnelComBuraco(18, 3.2f, cor, d), 0.4f));

                break;
            }

            case TipoDeInimigo.ChefeMinotauro:
            {
                // Ondas duplas: aneis com buraco, um atras do outro, cada um com o buraco num lugar.
                int ondas = dura ? 3 : 2;

                for (int i = 0; i < ondas; i++)
                    fila.Add(Passo(AnelComBuraco(16, 3.4f, cor, d), 0.6f));

                break;
            }

            case TipoDeInimigo.ChefeLobisomem:
            {
                // Uivo da lua: uma espiral rapida; na segunda fase tres fios e uma rajada mirada no fim.
                fila.Add(Passo(EspiralNoAngulo(16, dura ? 3 : 2, 21f, 3.8f, cor, d, comeco), 0.3f));

                if (dura)
                    fila.Add(Passo(Rajada(4, 5.4f, cor, d, 0f), 0.2f));

                break;
            }

            case TipoDeInimigo.ChefeOrc:
            {
                // Chuva de lancas: leques seguidos, cada um mirado de novo no jogador.
                int leques = dura ? 4 : 3;
                int lancas = dura ? 7 : 5;

                for (int i = 0; i < leques; i++)
                    fila.Add(Passo(Leque(lancas, 15f, 4.4f, cor, d), 0.45f));

                break;
            }

            default:
            {
                // O Olho do Abismo: uma flor, um anel com buraco e outra flor; da segunda fase uma espiral junto.
                int petalas = Mathf.Clamp(Mathf.RoundToInt(7 * Mathf.Min(1.2f, d.DensidadeDeBalas)), 6, 8);
                fila.Add(Passo(RosaNoAngulo(petalas, 3.3f, cor, d, comeco), 0.6f));
                fila.Add(Passo(AnelComBuraco(20, 3.3f, cor, d), 0.6f));
                fila.Add(Passo(RosaNoAngulo(petalas, 3.3f, cor, d, comeco + 180f / petalas), dura ? 0.4f : 0f));

                if (dura)
                    fila.Add(Passo(EspiralNoAngulo(20, 3, 16f, 3.2f, cor, d, comeco), 0f));

                break;
            }
        }

        return fila;
    }

    // Os padroes dos chefes: balas um pouco maiores (0.36) e de chefe (passam do teto dos bichos comuns).
    private static PassoDeBalas Passo(AtaqueDeBalas ataque, float espera)
    {
        ataque.deChefe = true;
        ataque.diametro = 0.36f;
        ataque.aviso = 0f;
        return new PassoDeBalas(ataque, espera);
    }

    private static AtaqueDeBalas RosaNoAngulo(int petalas, float velocidade, Color cor, DificuldadeDaFase d, float angulo)
    {
        AtaqueDeBalas a = Rosa(petalas, velocidade, cor, d);
        a.anguloInicial = angulo;
        return a;
    }

    private static AtaqueDeBalas EspiralNoAngulo(int balas, int bracos, float giro, float velocidade, Color cor, DificuldadeDaFase d, float angulo)
    {
        AtaqueDeBalas a = Espiral(balas, bracos, giro, velocidade, cor, d, 0f);
        a.anguloInicial = angulo;
        return a;
    }

    // ---------------- os numeros que sobem com a fase ----------------
    private static int Quantas(int basico, DificuldadeDaFase d) => Mathf.Max(1, Mathf.RoundToInt(basico * d.DensidadeDeBalas));

    private static float Pressa(float basica, DificuldadeDaFase d) => basica * d.PressaDasBalas;

    private static float Pausa(float basica, DificuldadeDaFase d) => Mathf.Max(1.8f, basica / d.RitmoDeTiro);

    private static AtaqueDeBalas Leque(int balas, float abertura, float velocidade, Color cor, DificuldadeDaFase d, float aviso = 0f) => new AtaqueDeBalas
    {
        padrao = PadraoDeBala.Leque, quantidade = Quantas(balas, d) | 1, abertura = abertura,
        velocidade = Pressa(velocidade, d), cor = cor, aviso = aviso, exigeVisao = true,
    };

    private static AtaqueDeBalas Anel(int balas, float velocidade, Color cor, DificuldadeDaFase d, float aviso = 0f) => new AtaqueDeBalas
    {
        padrao = PadraoDeBala.Anel, quantidade = Quantas(balas, d), velocidade = Pressa(velocidade, d), cor = cor, aviso = aviso,
    };

    private static AtaqueDeBalas AnelComBuraco(int balas, float velocidade, Color cor, DificuldadeDaFase d, float aviso = 0f) => new AtaqueDeBalas
    {
        padrao = PadraoDeBala.AnelComBuraco, quantidade = Quantas(balas, d), abertura = 55f,
        velocidade = Pressa(velocidade, d), cor = cor, aviso = aviso,
    };

    private static AtaqueDeBalas Rajada(int tiros, float velocidade, Color cor, DificuldadeDaFase d, float aviso) => new AtaqueDeBalas
    {
        padrao = PadraoDeBala.Rajada, quantidade = Quantas(tiros, d), velocidade = Pressa(velocidade, d), cor = cor,
        aviso = aviso, exigeVisao = true,
    };

    private static AtaqueDeBalas Espiral(int balas, int bracos, float giro, float velocidade, Color cor, DificuldadeDaFase d, float aviso) => new AtaqueDeBalas
    {
        padrao = PadraoDeBala.Espiral, quantidade = Quantas(balas, d), bracos = bracos, abertura = giro,
        velocidade = Pressa(velocidade, d), cor = cor, aviso = aviso,
    };

    private static AtaqueDeBalas Cruz(float velocidade, Color cor, DificuldadeDaFase d) => new AtaqueDeBalas
    {
        padrao = PadraoDeBala.Cruz, velocidade = Pressa(velocidade, d), cor = cor, aviso = 0f,
    };

    private static AtaqueDeBalas Cortina(int balas, float velocidade, Color cor, DificuldadeDaFase d) => new AtaqueDeBalas
    {
        padrao = PadraoDeBala.Cortina, quantidade = Quantas(balas, d) | 1, velocidade = Pressa(velocidade, d), cor = cor,
        aviso = 0f, exigeVisao = true,
    };

    private static AtaqueDeBalas Rosa(int petalas, float velocidade, Color cor, DificuldadeDaFase d) => new AtaqueDeBalas
    {
        padrao = PadraoDeBala.Rosa, quantidade = petalas, velocidade = Pressa(velocidade, d), cor = cor, aviso = 0f,
    };
}
