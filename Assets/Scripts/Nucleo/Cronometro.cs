using UnityEngine;

/// <summary>
/// Contador regressivo em segundos. Existe pra tirar do meio da maquina de estados a
/// dezena de "float xRestante" espalhados e o "xRestante -= dt" repetido pra cada um:
/// aqui a intencao fica no nome (Armar, Ativo, Consumir) em vez de no sinal do numero.
///
/// E um struct: nao aloca nada, pode ser campo de MonoBehaviour sem custo.
/// </summary>
[System.Serializable]
public struct Cronometro
{
    [SerializeField] private float restante;

    /// <summary>Liga o cronometro por tantos segundos. Valor menor que o atual nao encurta.</summary>
    public void Armar(float segundos)
    {
        restante = Mathf.Max(restante, segundos);
    }

    /// <summary>Liga por tantos segundos, mesmo que isso encurte o que faltava.</summary>
    public void Forcar(float segundos)
    {
        restante = segundos;
    }

    public void Zerar()
    {
        restante = 0f;
    }

    /// <summary>Desconta o tempo do quadro. Chame uma vez por FixedUpdate/Update.</summary>
    public void Contar(float delta)
    {
        if (restante > 0f)
            restante -= delta;
    }

    /// <summary>True enquanto ainda sobra tempo.</summary>
    public bool Ativo => restante > 0f;

    /// <summary>Segundos que faltam (nunca negativo).</summary>
    public float Restante => Mathf.Max(0f, restante);

    /// <summary>
    /// True (e zera) se estava ativo. E o jeito certo de gastar um input guardado:
    /// o aperto vale uma vez so, sem risco de pular duas vezes com o mesmo clique.
    /// </summary>
    public bool Consumir()
    {
        if (restante <= 0f)
            return false;

        restante = 0f;
        return true;
    }
}
