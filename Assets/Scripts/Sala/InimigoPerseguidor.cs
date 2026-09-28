using UnityEngine;

/// <summary>
/// Vai reto pra cima do jogador e machuca encostando (tipo o Gaper do Isaac).
/// Um leve zigue-zague deixa o caminho menos previsivel e evita que dois perseguidores
/// fiquem empilhados no mesmo ponto.
/// </summary>
public class InimigoPerseguidor : InimigoDeSala
{
    [Header("Perseguidor")]
    [Tooltip("Quanto ele balanca pros lados enquanto persegue (0 = linha reta)")]
    [SerializeField, Range(0f, 1f)] private float zigueZague = 0.35f;

    [Tooltip("Balancos por segundo")]
    [SerializeField, Min(0f)] private float frequenciaDoZigueZague = 1.5f;

    [Tooltip("Sem ver o jogador (parede no meio), anda mais devagar")]
    [SerializeField, Range(0f, 1f)] private float velocidadeSemVer = 0.6f;

    private float fase;

    protected override void Awake()
    {
        base.Awake();

        // Cada um balanca num ritmo, pra um grupo nao andar em sincronia.
        fase = Random.value * Mathf.PI * 2f;
    }

    protected override void AtualizarAgindo(float dt)
    {
        Vector2 alvo = ParaOJogador();

        if (alvo.sqrMagnitude < 0.0001f)
        {
            Frear();
            return;
        }

        Vector2 frente = alvo.normalized;
        Vector2 lado = new Vector2(-frente.y, frente.x);
        float balanco = Mathf.Sin(Time.time * frequenciaDoZigueZague * Mathf.PI * 2f + fase) * zigueZague;

        float v = VeOJogador() ? velocidade : velocidade * velocidadeSemVer;
        Andar(frente + lado * balanco, v);
    }
}
