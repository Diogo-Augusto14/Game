using UnityEngine;

/// <summary>
/// Tudo que um golpe carrega. Quem toma dano recebe isto em vez de um float solto,
/// então sabe de onde veio, quanto foi e pra onde empurrar.
/// </summary>
public readonly struct DanoInfo
{
    /// <summary>Quanto de vida tira.</summary>
    public readonly float Quantidade;

    /// <summary>Direção normalizada de quem bateu pra quem levou.</summary>
    public readonly Vector2 Direcao;

    /// <summary>Força do empurrão (impulso). 0 = não empurra.</summary>
    public readonly float ForcaEmpurrao;

    /// <summary>Onde o golpe encostou (útil pra partículas/faísca).</summary>
    public readonly Vector2 PontoDeImpacto;

    /// <summary>Quem deu o golpe. Pode ser null (ex: espinho do cenário).</summary>
    public readonly GameObject Atacante;

    public DanoInfo(float quantidade, Vector2 direcao, float forcaEmpurrao, Vector2 pontoDeImpacto, GameObject atacante)
    {
        Quantidade = quantidade;
        Direcao = direcao;
        ForcaEmpurrao = forcaEmpurrao;
        PontoDeImpacto = pontoDeImpacto;
        Atacante = atacante;
    }
}
