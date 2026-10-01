using UnityEngine;

/// <summary>
/// Orc e esqueleto blindados: a armadura segura um terco de cada golpe e nada empurra nem
/// atordoa eles (tiro nao interrompe o golpe). Marcham devagar e sem desviar do caminho reto
/// enquanto nao tem obstaculo; o jeito e manter distancia e ir tirando.
/// </summary>
public class InimigoBlindado : InimigoDeGolpe
{
    [SerializeField, Range(0.1f, 1f)] private float danoQuePassa = 0.65f;

    protected override bool Imparavel => true;

    protected override void Awake()
    {
        base.Awake();

        if (vida != null)
            vida.MultiplicadorDeDanoRecebido = danoQuePassa;

        // Marcha aos trancos, como quem carrega a armadura.
        JeitoDeChegar = Aproximacao.PassoPesado;
    }
}
