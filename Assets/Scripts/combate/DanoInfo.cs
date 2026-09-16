using UnityEngine;

/// <summary>Peso do golpe. Decide a reacao: tropico curto ou voar pra tras.</summary>
public enum PesoDoGolpe
{
    Leve,
    Forte
}

/// <summary>
/// Tudo que um golpe carrega. Quem toma dano recebe isto em vez de um float solto,
/// entao sabe de onde veio, quanto foi, com que peso e pra onde empurrar.
///
/// E um struct readonly: uma vez montado ninguem altera no meio do caminho, e passar
/// adiante nao aloca nada (importante — em combate isso acontece muitas vezes por segundo).
/// </summary>
public readonly struct DanoInfo
{
    /// <summary>Quanto de vida tira, antes da defesa.</summary>
    public readonly float Quantidade;

    /// <summary>Direcao normalizada de quem bateu pra quem levou.</summary>
    public readonly Vector2 Direcao;

    /// <summary>Forca do empurrao (impulso). 0 = nao empurra.</summary>
    public readonly float ForcaEmpurrao;

    /// <summary>Onde o golpe encostou — usado pra posicionar a faisca.</summary>
    public readonly Vector2 PontoDeImpacto;

    /// <summary>Quem deu o golpe. Pode ser null (espinho do cenario, queda).</summary>
    public readonly GameObject Atacante;

    /// <summary>Leve ou forte. Escolhe a animacao de apanhar e o tempo de atordoamento.</summary>
    public readonly PesoDoGolpe Peso;

    /// <summary>True se o golpe ignora a invencibilidade (dano de queda, espinho, roteiro).</summary>
    public readonly bool IgnoraInvencibilidade;

    public DanoInfo(
        float quantidade,
        Vector2 direcao,
        float forcaEmpurrao,
        Vector2 pontoDeImpacto,
        GameObject atacante,
        PesoDoGolpe peso = PesoDoGolpe.Leve,
        bool ignoraInvencibilidade = false)
    {
        Quantidade = quantidade;
        Direcao = direcao.sqrMagnitude > 0.0001f ? direcao.normalized : Vector2.right;
        ForcaEmpurrao = forcaEmpurrao;
        PontoDeImpacto = pontoDeImpacto;
        Atacante = atacante;
        Peso = peso;
        IgnoraInvencibilidade = ignoraInvencibilidade;
    }

    /// <summary>Golpe simples sem atacante, pro cenario (espinho, lava, queda).</summary>
    public static DanoInfo DoCenario(float quantidade, Vector2 posicao, PesoDoGolpe peso = PesoDoGolpe.Leve)
    {
        return new DanoInfo(quantidade, Vector2.up, 0f, posicao, null, peso, true);
    }

    /// <summary>+1 se o golpe veio da esquerda (empurra pra direita), -1 se veio da direita.</summary>
    public float LadoDoEmpurrao => Direcao.x >= 0f ? 1f : -1f;
}
