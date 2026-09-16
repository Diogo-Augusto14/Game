using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Vida de qualquer coisa que toma dano: inimigo, chefe, jogador, vaso.
/// Recebe o DanoInfo, desconta a defesa, aplica o empurrão, pisca o sprite,
/// dispara as animações de apanhar e morrer, segura um instante de invencibilidade
/// e avisa quem quiser ouvir (eventos no Inspector).
///
/// Se o objeto tiver um controlador de movimento (Movimento.cs, via IControladorDeMovimento),
/// o Vida conversa com ele: respeita a invencibilidade do dash e manda o empurrão por lá,
/// pra o controlador travar o próprio andar. Se não tiver, empurra o Rigidbody2D direto.
/// </summary>
[DisallowMultipleComponent]
public class Vida : MonoBehaviour, IDanificavel
{
    [Header("Vida")]
    [SerializeField, Min(1f)] private float vidaMaxima = 20f;

    [Tooltip("Quanto é descontado de cada golpe recebido. Todo golpe tira pelo menos 1, mesmo com defesa alta")]
    [SerializeField, Min(0f)] private float defesa = 0f;

    [Tooltip("Depois de tomar dano, ignora novos golpes por este tempo (evita tomar 3 hits do mesmo golpe)")]
    [SerializeField, Min(0f)] private float tempoInvencivel = 0.2f;

    [Header("Empurrão")]
    [Tooltip("Se ligado, o golpe empurra este objeto (precisa de Rigidbody2D Dynamic)")]
    [SerializeField] private bool recebeEmpurrao = true;

    [Tooltip("Quanto do empurrão vai pra cima (0 = só pro lado, 1 = 45°). Um pouco pra cima dá aquele 'quique' de plataforma")]
    [SerializeField, Range(0f, 1f)] private float componenteVertical = 0.4f;

    [Tooltip("Segundos sem controle horizontal depois do golpe. É isto que impede o Movimento de comer o empurrão")]
    [SerializeField, Min(0f)] private float travaAposGolpe = 0.25f;

    [Header("Animação (opcional — só dispara se o parâmetro existir)")]
    [Tooltip("Animator deste objeto. Vazio = procura no próprio objeto e nos filhos")]
    [SerializeField] private Animator animator;

    [Tooltip("Trigger tocado ao levar golpe. Vazio = não anima")]
    [SerializeField] private string triggerAoTomarDano = "Hurt";

    [Tooltip("Trigger tocado ao morrer. Vazio = não anima")]
    [SerializeField] private string triggerAoMorrer = "Die";

    [Header("Feedback")]
    [Tooltip("Sprite que pisca ao tomar dano. Vazio = procura nos filhos")]
    [SerializeField] private SpriteRenderer sprite;

    [SerializeField] private Color corDoFlash = new Color(1f, 0.3f, 0.3f);

    [SerializeField, Min(0f)] private float duracaoDoFlash = 0.08f;

    [Header("Morte")]
    [Tooltip("Destrói o objeto ao morrer. DESLIGUE no jogador e em tudo que renasce")]
    [SerializeField] private bool destruirAoMorrer = true;

    [Tooltip("Segundos até destruir — dê tempo pra animação de morte terminar")]
    [SerializeField, Min(0f)] private float atrasoParaDestruir = 0f;

    [Header("Eventos")]
    [Tooltip("Disparado a cada golpe recebido. Use pra som, partícula, tremer câmera...")]
    public UnityEvent<DanoInfo> AoTomarDano;

    [Tooltip("Disparado uma vez, quando a vida chega a zero")]
    public UnityEvent AoMorrer;

    [Tooltip("Disparado ao ser curado ou revivido — bom pra atualizar a barra de vida")]
    public UnityEvent AoMudarVida;

    // ---------- estado ----------
    private Rigidbody2D rb;
    private IControladorDeMovimento controlador;   // null em inimigos simples, vasos, etc.
    private readonly HashSet<int> parametrosDoAnimator = new HashSet<int>();
    private int hashDano, hashMorte;
    private Color corOriginal;
    private Coroutine rotinaFlash;
    private float fimDaInvencibilidade;

    public float VidaAtual { get; private set; }
    public float VidaMaxima => vidaMaxima;
    public float Defesa => defesa;
    public bool EstaMorto => VidaAtual <= 0f;

    /// <summary>Fração de 0 a 1 — pronta pra uma Image em Filled.</summary>
    public float Fracao => vidaMaxima <= 0f ? 0f : VidaAtual / vidaMaxima;

    /// <summary>Invencível por tempo (pós-golpe) ou porque o controlador mandou (dash).</summary>
    public bool EstaInvencivel => Time.time < fimDaInvencibilidade
                               || (controlador != null && controlador.IgnorandoDano);

    // ---------- ciclo de vida ----------
    private void Awake()
    {
        VidaAtual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();

        // Busca UMA vez. Se este objeto tiver Movimento.cs, ele aparece aqui.
        TryGetComponent(out controlador);

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
                parametrosDoAnimator.Add(p.nameHash);
        }

        hashDano  = string.IsNullOrEmpty(triggerAoTomarDano) ? 0 : Animator.StringToHash(triggerAoTomarDano);
        hashMorte = string.IsNullOrEmpty(triggerAoMorrer) ? 0 : Animator.StringToHash(triggerAoMorrer);

        if (sprite == null)
            sprite = GetComponentInChildren<SpriteRenderer>();

        if (sprite != null)
            corOriginal = sprite.color;
    }

    // ---------- IDanificavel ----------
    public void TomarDano(DanoInfo info)
    {
        if (EstaMorto || EstaInvencivel)
            return;

        float danoReal = Mathf.Max(1f, info.Quantidade - defesa);

        VidaAtual = Mathf.Max(0f, VidaAtual - danoReal);
        fimDaInvencibilidade = Time.time + tempoInvencivel;

        AplicarEmpurrao(info);
        Piscar();

        if (!EstaMorto)
            Animar(hashDano);

        AoTomarDano?.Invoke(info);
        AoMudarVida?.Invoke();

        if (EstaMorto)
            Morrer();
    }

    /// <summary>Recupera vida (poção, checkpoint). Não passa do máximo.</summary>
    public void Curar(float quantidade)
    {
        if (EstaMorto)
            return;

        VidaAtual = Mathf.Min(vidaMaxima, VidaAtual + Mathf.Max(0f, quantidade));
        AoMudarVida?.Invoke();
    }

    /// <summary>Volta com a vida cheia e um instante de invencibilidade. Usado no renascimento.</summary>
    public void Reviver(float invencibilidadeInicial = 1f)
    {
        VidaAtual = vidaMaxima;
        fimDaInvencibilidade = Time.time + invencibilidadeInicial;

        if (sprite != null)
            sprite.color = corOriginal;

        AoMudarVida?.Invoke();
    }

    // ---------- interno ----------
    private void Animar(int hash)
    {
        if (animator != null && hash != 0 && parametrosDoAnimator.Contains(hash))
            animator.SetTrigger(hash);
    }

    private void AplicarEmpurrao(DanoInfo info)
    {
        if (!recebeEmpurrao || info.ForcaEmpurrao <= 0f)
            return;

        // Só o lado importa (esquerda/direita) + um pouco pra cima.
        Vector2 lado = new Vector2(Mathf.Sign(info.Direcao.x), componenteVertical).normalized;
        Vector2 impulso = lado * info.ForcaEmpurrao;

        // Tem controlador de movimento? Ele que aplica — e trava o próprio andar,
        // senão o impulso seria desfeito no FixedUpdate seguinte.
        if (controlador != null)
        {
            controlador.AplicarImpulsoExterno(impulso, travaAposGolpe);
            return;
        }

        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic)
            return;

        rb.linearVelocity = Vector2.zero;   // cancela o que estava fazendo pra o empurrão ser sentido
        rb.AddForce(impulso, ForceMode2D.Impulse);
    }

    private void Piscar()
    {
        if (sprite == null || duracaoDoFlash <= 0f)
            return;

        if (rotinaFlash != null)
            StopCoroutine(rotinaFlash);

        rotinaFlash = StartCoroutine(RotinaFlash());
    }

    private IEnumerator RotinaFlash()
    {
        sprite.color = corDoFlash;
        yield return new WaitForSeconds(duracaoDoFlash);
        sprite.color = corOriginal;
        rotinaFlash = null;
    }

    private void Morrer()
    {
        Animar(hashMorte);
        AoMorrer?.Invoke();

        if (destruirAoMorrer)
            Destroy(gameObject, atrasoParaDestruir);
    }

    private void OnDisable()
    {
        // Não deixa o sprite preso na cor do flash se o objeto for desativado no meio.
        if (sprite != null)
            sprite.color = corOriginal;
    }
}
