using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hitbox de golpe. Fica como filho de quem bate, com o collider DESLIGADO; quem liga e
/// desliga nos momentos certos e o <see cref="Ataque"/> (ou o <see cref="Inimigo"/>).
/// Zero Instantiate/Destroy: a hitbox e permanente, entao nao gera lixo de memoria no
/// meio do combate.
///
/// Aplica dano uma unica vez por golpe em cada alvo que implementa IDanificavel — mesmo
/// que o alvo tenha varios colliders, ou que os colliders se cruzem de novo no mesmo golpe.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class Espada : MonoBehaviour
{
    [Header("Golpe (valores padrao)")]
    [Tooltip("Dano de cada alvo atingido. O Ataque pode mandar outro valor por golpe do combo")]
    [SerializeField, Min(0f)] private float dano = 12f;

    [Tooltip("Forca do empurrao no alvo. 0 = nao empurra")]
    [SerializeField, Min(0f)] private float forcaEmpurrao = 4f;

    [Tooltip("Peso padrao do golpe")]
    [SerializeField] private PesoDoGolpe peso = PesoDoGolpe.Leve;

    [Header("Referencias")]
    [Tooltip("Quem da o golpe. Vazio = o pai deste objeto")]
    [SerializeField] private Transform dono;

    [Tooltip("Camadas que este golpe machuca. Vazio = tudo, menos quem esta na MESMA camada do dono " +
             "(e o que impede inimigo de matar inimigo e o boneco de se cortar sozinho)")]
    [SerializeField] private LayerMask alvos;

    [Header("Efeito")]
    [Tooltip("Tremida de camera ao acertar. 0 = nada")]
    [SerializeField, Min(0f)] private float tremorAoAcertar = 0.12f;

    // Quem ja levou dano NESTE golpe.
    private readonly HashSet<IDanificavel> jaAtingidos = new HashSet<IDanificavel>();

    private Collider2D col;
    private float danoAtual;
    private float empurraoAtual;
    private PesoDoGolpe pesoAtual;

    /// <summary>True enquanto a hitbox esta ligada.</summary>
    public bool Ativa => col != null && col.enabled;

    /// <summary>Quantos alvos este golpe acertou. Zera a cada Ligar().</summary>
    public int Acertos { get; private set; }

    public Transform Dono => dono;

    private void Awake()
    {
        col = GetComponent<Collider2D>();

        if (!col.isTrigger)
        {
            // Sem trigger a hitbox empurraria o inimigo fisicamente em vez de machucar.
            col.isTrigger = true;
            Debug.LogWarning($"[Espada] {name}: marquei Is Trigger no collider.", this);
        }

        if (dono == null)
            dono = transform.parent != null ? transform.parent : transform;

        danoAtual = dano;
        empurraoAtual = forcaEmpurrao;
        pesoAtual = peso;

        col.enabled = false;   // comeca sempre desligada
    }

    private void OnDisable()
    {
        if (col != null)
            col.enabled = false;
    }

    /// <summary>Liga a hitbox com os valores padrao do Inspector.</summary>
    public void Ligar()
    {
        Ligar(dano, forcaEmpurrao, peso);
    }

    /// <summary>
    /// Liga a hitbox com valores proprios deste golpe. E assim que o terceiro hit do
    /// combo dói mais que o primeiro sem precisar de tres hitboxes diferentes.
    /// </summary>
    public void Ligar(float danoDoGolpe, float empurraoDoGolpe, PesoDoGolpe pesoDoGolpe)
    {
        jaAtingidos.Clear();
        Acertos = 0;

        danoAtual = danoDoGolpe;
        empurraoAtual = empurraoDoGolpe;
        pesoAtual = pesoDoGolpe;

        if (col != null)
            col.enabled = true;
    }

    /// <summary>Desliga a hitbox (fim da janela do golpe).</summary>
    public void Desligar()
    {
        if (col != null)
            col.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (!AlvoValido(outro))
            return;

        IDanificavel alvo = EncontrarDanificavel(outro);

        if (alvo == null)
            return;

        // Add devolve false se ja estava na lista: ja foi atingido neste golpe.
        if (!jaAtingidos.Add(alvo))
            return;

        alvo.TomarDano(MontarDano(outro));
        Acertos++;

        if (tremorAoAcertar > 0f)
            Cameramov.Tremer(tremorAoAcertar);
    }

    /// <summary>
    /// Filtro de quem pode levar o golpe. Sem ele, a hitbox do inimigo mataria os outros
    /// inimigos e a do jogador machucaria o proprio jogador — os dois bugs classicos de
    /// hitbox por trigger.
    /// </summary>
    private bool AlvoValido(Collider2D outro)
    {
        // Nao bate em si mesmo nem nos proprios filhos.
        if (dono != null && outro.transform.IsChildOf(dono))
            return false;

        if (alvos.value != 0)
            return (alvos.value & (1 << outro.gameObject.layer)) != 0;

        // Sem lista de alvos: qualquer um que nao esteja no mesmo time (mesma camada).
        return dono == null || outro.gameObject.layer != dono.gameObject.layer;
    }

    private DanoInfo MontarDano(Collider2D outro)
    {
        Vector2 origem = dono.position;
        Vector2 centroDoAlvo = outro.bounds.center;

        Vector2 direcao = centroDoAlvo - origem;

        if (direcao.sqrMagnitude < 0.0001f)
            direcao = Vector2.right * Mathf.Sign(dono.localScale.x);

        Vector2 pontoDeImpacto = col.ClosestPoint(centroDoAlvo);

        return new DanoInfo(danoAtual, direcao, empurraoAtual, pontoDeImpacto, dono.gameObject, pesoAtual);
    }

    private static IDanificavel EncontrarDanificavel(Collider2D outro)
    {
        // Procura no objeto atingido; se o collider for de um filho, procura no
        // Rigidbody2D, que e o dono real do collider.
        if (outro.TryGetComponent(out IDanificavel direto))
            return direto;

        if (outro.attachedRigidbody != null && outro.attachedRigidbody.TryGetComponent(out IDanificavel noCorpo))
            return noCorpo;

        return null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Collider2D c = col != null ? col : GetComponent<Collider2D>();

        if (c == null)
            return;

        // Vermelho cheio quando ligada, cinza vazio quando desligada.
        Gizmos.color = c.enabled ? new Color(1f, 0.2f, 0.2f, 0.9f) : new Color(0.6f, 0.6f, 0.6f, 0.4f);
        Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
    }
#endif
}
