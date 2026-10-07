using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>As habilidades dos herois (uma pra cada um).</summary>
public enum TipoDeHabilidade
{
    ChuvaDeFlechas,
    Rajada,
    Escudo,
    AnelDeCortes,
    Investida,
    Redemoinho,
    Furia,
    Meteoro,
    Cura,
}

/// <summary>
/// A habilidade do heroi (tecla F ou o X do controle), com a recarga dela. Cada heroi tem uma
/// (<see cref="Herois"/> diz qual); o <see cref="Herois.Aplicar"/> poe esta peca no jogador.
///
/// Os tiros das habilidades saem de copias da arma do heroi, mudadas aqui (um anel, um leque...),
/// entao as flechas, cortes e bolas de fogo sao as mesmas da arma dele.
/// </summary>
[DisallowMultipleComponent]
public class HabilidadeDoHeroi : MonoBehaviour
{
    private ControlesDoJogador controles;
    private MovimentoDoJogador movimento;
    private Vida vida;
    private AudioSource audioSource;
    private AnimacaoDoJogador animacao;

    private Herois.Heroi heroi;
    private DadosDaArma armaDoHeroi;
    private float prontaEm;
    private float furiaAte = -1f;
    private float escudoAte = -1f;

    /// <summary>O nome da habilidade (pra tela).</summary>
    public string Nome => heroi != null ? heroi.NomeDaHabilidade : "";

    public bool Pronta => heroi != null && Time.time >= prontaEm;

    /// <summary>0 logo depois de usar, 1 pronta.</summary>
    public float Carga => heroi == null ? 0f : Pronta ? 1f : 1f - (prontaEm - Time.time) / Mathf.Max(0.1f, heroi.Recarga);

    /// <summary>Na furia do Machadeiro os tiros dele batem em dobro (o <see cref="Projetil"/> pergunta).</summary>
    public float MultiplicadorDeDano => Time.time < furiaAte ? 2f : 1f;

    private void Awake()
    {
        controles = GetComponent<ControlesDoJogador>();
        movimento = GetComponent<MovimentoDoJogador>();
        vida = GetComponent<Vida>();
        audioSource = GetComponent<AudioSource>();
        animacao = GetComponent<AnimacaoDoJogador>();
    }

    public void Configurar(Herois.Heroi qual)
    {
        heroi = qual;
        armaDoHeroi = Herois.Arma(qual);

        // Comeca pronta: da pra usar logo no primeiro andar.
        prontaEm = 0f;
        furiaAte = escudoAte = -1f;
    }

    private void Update()
    {
        Pintar();

        if (heroi == null || controles == null || !controles.isActiveAndEnabled || !controles.UsouHabilidade)
            return;

        if (vida != null && vida.Morto)
            return;

        if (!Pronta)
        {
            Tocar("Negado", 0.4f);
            return;
        }

        prontaEm = Time.time + heroi.Recarga;
        Registro.UsouHabilidade();
        StartCoroutine(Usar(heroi.Habilidade));
    }

    // A cor do corpo: dourado no escudo, vermelho na furia.
    private void Pintar()
    {
        if (animacao == null || animacao.Corpo == null)
            return;

        Color cor = Time.time < escudoAte ? new Color(1f, 0.9f, 0.45f)
                  : Time.time < furiaAte ? new Color(1f, 0.55f, 0.5f)
                  : Color.white;
        cor.a = animacao.Corpo.color.a;
        animacao.Corpo.color = cor;
    }

    private Vector2 Mira => controles != null ? controles.Mira : Vector2.right;

    private Vector2 Centro => (Vector2)transform.position + Vector2.up * 0.3f;

    private IEnumerator Usar(TipoDeHabilidade qual)
    {
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.3f, heroi.NomeDaHabilidade + "!", new Color(1f, 0.9f, 0.5f), 0.9f);

        switch (qual)
        {
            case TipoDeHabilidade.ChuvaDeFlechas:
            {
                // Tres aneis de flechas, cada um girado um pouco.
                DadosDaArma anel = Copia(16, true, 0f, 1f);

                for (int i = 0; i < 3; i++)
                {
                    anel.Disparar(Centro, Mira, gameObject, Lado.Jogador, i * 7.5f);
                    TocarArma();
                    yield return new WaitForSeconds(0.15f);
                }

                break;
            }

            case TipoDeHabilidade.Rajada:
            {
                DadosDaArma leque = Copia(5, false, 9f, 1.2f);

                for (int i = 0; i < 4; i++)
                {
                    leque.Disparar(Centro, Mira, gameObject, Lado.Jogador);
                    TocarArma();
                    yield return new WaitForSeconds(0.12f);
                }

                break;
            }

            case TipoDeHabilidade.Escudo:
            {
                const float duracao = 3f;
                escudoAte = Time.time + duracao;
                movimento?.Proteger(duracao);
                Tocar("Item", 0.6f);

                // O escudo tambem desmancha os tiros inimigos que chegam perto.
                while (Time.time < escudoAte && !vida.Morto)
                {
                    foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, 1.6f))
                    {
                        if (c != null && c.TryGetComponent(out Projetil tiro) && tiro.Lado != Lado.Jogador)
                            tiro.Sumir();
                    }

                    yield return null;
                }

                break;
            }

            case TipoDeHabilidade.AnelDeCortes:
            {
                DadosDaArma anel = Copia(12, true, 0f, 1.3f);
                anel.Disparar(Centro, Mira, gameObject, Lado.Jogador);
                TocarArma();
                yield return new WaitForSeconds(0.25f);
                anel.Disparar(Centro, Mira, gameObject, Lado.Jogador, 15f);
                TocarArma();
                CameraDoJogo.Tremer(0.15f, 0.2f);
                break;
            }

            case TipoDeHabilidade.Investida:
            {
                const float duracao = 0.35f;
                movimento?.Investir(Mira, 20f, duracao);
                Tocar("Dash", 0.6f);
                HashSet<Vida> atropelados = new HashSet<Vida>();

                // Quem esta no caminho apanha (uma vez cada).
                for (float fim = Time.time + duracao; Time.time < fim && !vida.Morto;)
                {
                    foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, 0.8f))
                    {
                        Vida dele = c != null ? c.GetComponentInParent<Vida>() : null;

                        if (dele != null && dele.Lado != Lado.Jogador && atropelados.Add(dele))
                            dele.ReceberDano(new Dano(12f, Mira, 8f, gameObject));
                    }

                    yield return null;
                }

                break;
            }

            case TipoDeHabilidade.Redemoinho:
            {
                DadosDaArma anel = Copia(6, true, 0f, 0.9f);

                for (int i = 0; i < 8 && !vida.Morto; i++)
                {
                    anel.Disparar(Centro, Mira, gameObject, Lado.Jogador, i * 20f);
                    TocarArma();
                    yield return new WaitForSeconds(0.13f);
                }

                break;
            }

            case TipoDeHabilidade.Furia:
            {
                furiaAte = Time.time + 6f;
                Tocar("Rugido", 0.5f);
                CameraDoJogo.Tremer(0.2f, 0.25f);
                break;
            }

            case TipoDeHabilidade.Meteoro:
            {
                // Cai onde o mouse aponta (ou a 5 passos na mira), com uma marca no chao antes.
                Vector2 alvo = controles != null && controles.MiraPeloMouse
                    ? (Vector2)transform.position + Vector2.ClampMagnitude(controles.PontoDoMouse - (Vector2)transform.position, 8f)
                    : (Vector2)transform.position + Mira * 5f;

                GameObject marca = Marca(alvo);
                yield return new WaitForSeconds(0.7f);

                if (marca != null)
                    Destroy(marca);

                foreach (Collider2D c in Physics2D.OverlapCircleAll(alvo, 2.2f))
                {
                    Vida dele = c != null ? c.GetComponentInParent<Vida>() : null;

                    if (dele != null && dele.Lado != Lado.Jogador)
                        dele.ReceberDano(new Dano(15f, (Vector2)dele.transform.position - alvo, 8f, gameObject));
                }

                Copia(16, true, 0f, 0.8f).Disparar(alvo, Vector2.right, gameObject, Lado.Jogador);
                Tocar("Explosao", 0.7f);
                CameraDoJogo.Tremer(0.35f, 0.3f);
                break;
            }

            case TipoDeHabilidade.Cura:
            {
                vida.Curar(2f);
                Tocar("Cura", 0.7f);
                TextoFlutuante.Mostrar(transform.position + Vector3.up * 0.9f, "+1 coração", new Color(0.5f, 1f, 0.55f), 1.2f).Pular(1.2f);
                break;
            }
        }
    }

    // Uma copia da arma do heroi com outro padrao: varios tiros num anel ou num leque.
    private DadosDaArma Copia(int tiros, bool anel, float abertura, float danoVezes)
    {
        DadosDaArma copia = armaDoHeroi != null ? Instantiate(armaDoHeroi) : ScriptableObject.CreateInstance<DadosDaArma>();
        copia.tirosPorDisparo = tiros;
        copia.anel = anel;
        copia.abertura = abertura;
        copia.dispersao = 0f;
        copia.rajada = 1;
        copia.dano *= danoVezes;
        copia.alcance = Mathf.Max(copia.alcance, 5f);
        return copia;
    }

    private GameObject Marca(Vector2 onde)
    {
        Sprite desenho = Resources.Load<Sprite>("Marca");
        GameObject obj = new GameObject("Marca do meteoro");
        obj.transform.position = onde;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = desenho;
        sr.color = new Color(1f, 0.5f, 0.2f, 0.8f);
        sr.sortingOrder = Pedreiro.OrdemDosEnfeites + 1;
        obj.transform.localScale = Vector3.one * 2.6f;
        obj.AddComponent<MarcaPiscando>();
        return obj;
    }

    private void TocarArma()
    {
        if (armaDoHeroi != null && audioSource != null && armaDoHeroi.som != null)
            audioSource.PlayOneShot(armaDoHeroi.som, armaDoHeroi.volume);
    }

    private void Tocar(string som, float volume)
    {
        AudioClip clip = Resources.Load<AudioClip>("Sons/" + som);

        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, volume);
    }
}
