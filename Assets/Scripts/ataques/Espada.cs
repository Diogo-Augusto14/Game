using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hitbox permanente da espada. Fica como filho do boneco com o collider DESLIGADO;
/// o Ataque liga e desliga nos momentos certos. Zero Instantiate/Destroy.
/// Aplica dano uma única vez por golpe em cada alvo que implementa IDanificavel.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class Espada : MonoBehaviour
{
    [Header("Golpe")]
    [Tooltip("Dano aplicado a cada alvo atingido")]
    [SerializeField, Min(0f)] private float dano = 5f;

    [Tooltip("Força do empurrão no alvo (impulso). 0 = não empurra")]
    [SerializeField, Min(0f)] private float forcaEmpurrao = 6f;

    [Header("Referências")]
    [Tooltip("Quem dá o golpe. Vazio = o pai deste objeto (o boneco)")]
    [SerializeField] private Transform dono;

    // Quem já levou dano neste golpe. Evita acertar duas vezes o mesmo alvo
    // quando ele tem mais de um collider ou quando os colliders se cruzam de novo.
    private readonly HashSet<IDanificavel> jaAtingidos = new HashSet<IDanificavel>();

    private Collider2D col;

    /// <summary>True enquanto a hitbox está ligada.</summary>
    public bool Ativa => col != null && col.enabled;

    private void Awake()
    {
        col = GetComponent<Collider2D>();

        if (!col.isTrigger)
            Debug.LogWarning($"{name}: o Collider2D da espada precisa estar com 'Is Trigger' marcado.", this);

        if (dono == null)
            dono = transform.parent != null ? transform.parent : transform;

        // Começa sempre desligada. Só o Ataque liga.
        col.enabled = false;
    }

    private void OnDisable()
    {
        // Se o objeto for desativado no meio do golpe, não deixa a hitbox presa ligada.
        if (col != null)
            col.enabled = false;
    }

    /// <summary>Liga a hitbox e zera a lista de atingidos (começo do golpe).</summary>
    public void Ligar()
    {
        jaAtingidos.Clear();
        col.enabled = true;
    }

    /// <summary>Desliga a hitbox (fim do golpe).</summary>
    public void Desligar()
    {
        col.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        IDanificavel alvo = EncontrarDanificavel(outro);
        if (alvo == null)
            return;

        // Add retorna false se já estava na lista — já foi atingido neste golpe.
        if (!jaAtingidos.Add(alvo))
            return;

        alvo.TomarDano(MontarDano(outro));
    }

    private DanoInfo MontarDano(Collider2D outro)
    {
        Vector2 origem = dono.position;
        Vector2 centroDoAlvo = outro.bounds.center;

        // Direção do boneco pro alvo. Se estiverem no mesmo ponto, usa pra onde o boneco olha.
        Vector2 direcao = centroDoAlvo - origem;
        if (direcao.sqrMagnitude < 0.0001f)
            direcao = Vector2.right * Mathf.Sign(dono.localScale.x);
        direcao.Normalize();

        Vector2 pontoDeImpacto = col.ClosestPoint(centroDoAlvo);

        return new DanoInfo(dano, direcao, forcaEmpurrao, pontoDeImpacto, dono.gameObject);
    }

    private static IDanificavel EncontrarDanificavel(Collider2D outro)
    {
        // Procura no objeto atingido; se o collider for de um filho (ex: hitbox da cabeça),
        // procura no Rigidbody2D, que é o "dono" real do collider.
        if (outro.TryGetComponent(out IDanificavel direto))
            return direto;

        if (outro.attachedRigidbody != null && outro.attachedRigidbody.TryGetComponent(out IDanificavel noCorpo))
            return noCorpo;

        return null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Collider2D c = col != null ? col : GetComponent<Collider2D>();
        if (c == null)
            return;

        // Vermelho quando ligada, cinza quando desligada.
        Gizmos.color = c.enabled ? new Color(1f, 0.2f, 0.2f, 0.9f) : new Color(0.6f, 0.6f, 0.6f, 0.6f);
        Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
    }
#endif
}
