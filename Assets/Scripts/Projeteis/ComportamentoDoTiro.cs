using UnityEngine;

/// <summary>
/// O movimento proprio de alguns tiros de inimigo: ondular (bruxo, fogo fatuo), acelerar
/// (bola de fogo, brasa), frear (gota de sangue) e perseguir o jogador um pouco (raio do
/// necromante). Quem nao tem movimento proprio voa reto, como sempre.
/// </summary>
[DisallowMultipleComponent]
public class ComportamentoDoTiro : MonoBehaviour
{
    private Rigidbody2D rb;
    private Transform jogador;
    private bool comecou;
    private float inicio;
    private Vector2 rumo;
    private float velocidadeBase;

    // Onda: graus pra cada lado e ondas por segundo.
    private float onda;
    private float ritmoDaOnda;

    // Velocidade vai de "de" ate "ate" (fracoes da original) em "tempoDaVelocidade" segundos.
    private float velocidadeDe = 1f;
    private float velocidadeAte = 1f;
    private float tempoDaVelocidade;

    // Perseguir: graus por segundo que consegue virar, e por quanto tempo tenta.
    private float giro;
    private float tempoPerseguindo;

    /// <summary>Poe o movimento do estilo no tiro (se o estilo tiver um).</summary>
    public static void Colocar(TiroDaSala tiro, EstiloDeTiro estilo)
    {
        ComportamentoDoTiro c;

        switch (estilo)
        {
            case EstiloDeTiro.OrbeSombrio:
                c = tiro.gameObject.AddComponent<ComportamentoDoTiro>();
                c.onda = 22f;
                c.ritmoDaOnda = 2.2f;
                break;

            case EstiloDeTiro.ChamaFantasma:
                c = tiro.gameObject.AddComponent<ComportamentoDoTiro>();
                c.onda = 38f;
                c.ritmoDaOnda = 1.4f;
                break;

            case EstiloDeTiro.BolaDeFogo:
                c = tiro.gameObject.AddComponent<ComportamentoDoTiro>();
                c.velocidadeDe = 0.6f;
                c.velocidadeAte = 1.4f;
                c.tempoDaVelocidade = 0.8f;
                break;

            case EstiloDeTiro.Brasa:
                c = tiro.gameObject.AddComponent<ComportamentoDoTiro>();
                c.velocidadeDe = 0.75f;
                c.velocidadeAte = 1.25f;
                c.tempoDaVelocidade = 0.7f;
                break;

            case EstiloDeTiro.GotaDeSangue:
                c = tiro.gameObject.AddComponent<ComportamentoDoTiro>();
                c.velocidadeDe = 1.15f;
                c.velocidadeAte = 0.55f;
                c.tempoDaVelocidade = 1f;
                break;

            case EstiloDeTiro.MaldicaoTeleguiada:
                c = tiro.gameObject.AddComponent<ComportamentoDoTiro>();
                c.giro = 70f;
                c.tempoPerseguindo = 1.4f;
                break;
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (!comecou)
        {
            // Le o disparo no primeiro passo: quem atirou ja deu rumo e velocidade.
            Vector2 v = rb.linearVelocity;

            if (v.sqrMagnitude < 0.0001f)
                return;

            comecou = true;
            inicio = Time.time;
            rumo = v.normalized;
            velocidadeBase = v.magnitude;

            if (giro > 0f)
            {
                GameObject j = GameObject.FindWithTag("Player");
                jogador = j != null ? j.transform : null;
            }
        }

        float idade = Time.time - inicio;

        if (giro > 0f && jogador != null && idade < tempoPerseguindo)
        {
            Vector2 querido = (Vector2)jogador.position - rb.position;

            if (querido.sqrMagnitude > 0.0001f)
            {
                float angulo = Vector2.SignedAngle(rumo, querido);
                float passo = Mathf.Clamp(angulo, -giro * Time.fixedDeltaTime, giro * Time.fixedDeltaTime);
                rumo = Quaternion.Euler(0f, 0f, passo) * rumo;
            }
        }

        float fracao = tempoDaVelocidade > 0f
            ? Mathf.Lerp(velocidadeDe, velocidadeAte, idade / tempoDaVelocidade)
            : 1f;

        Vector2 agora = rumo;

        if (onda > 0f)
            agora = Quaternion.Euler(0f, 0f, Mathf.Sin(idade * ritmoDaOnda * Mathf.PI * 2f) * onda) * rumo;

        rb.linearVelocity = agora * velocidadeBase * fracao;
    }
}
