using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A Fonte dos Desejos: o jogador joga nela a arma que esta na mao e ela devolve outra, de uma raridade
/// sorteada a partir da que foi jogada: 45% uma acima, 35% a mesma (outra arma), 20% uma abaixo. Da pra
/// jogar a arma do heroi tambem (ela e comum). Uma vez so: depois ela seca.
///
/// E a fonte das ruinas (pacote Ancient Ruins) num tom roxo, com uma luz da mesma cor. Fica na area
/// segura e, as vezes, numa sala so dela no andar (<see cref="TipoDeSala.Fonte"/>). Monte com
/// <see cref="Criar"/>.
/// </summary>
public class FonteDosDesejos : Interativo
{
    private static readonly Color Tom = new Color(0.78f, 0.62f, 1f);

    private Sprite[] quadros;
    private SpriteRenderer desenho;
    private FonteDeLuz luz;
    private DadosDaArma[] armas;
    private Transform jogador;
    private bool usada;

    public override float Alcance => 2.2f;

    public override bool Disponivel => !usada && ArmaNaMao() != null;

    public override string Dica
    {
        get
        {
            ArmaCarregada arma = ArmaNaMao();
            return arma != null ? Idioma.F("jogar {0} na fonte (pode vir melhor ou pior)", Idioma.T(arma.Dados.nome)) : "";
        }
    }

    /// <param name="armas">As armas que podem sair (as dos baus do andar).</param>
    public static FonteDosDesejos Criar(Vector2 pe, Transform pai, DadosDaArma[] armas)
    {
        Sprite[] quadros = FolhaDeSprites.Cortar(Resources.Load<Texture2D>("Areas/Ruinas/Fonte"), new Vector2Int(160, 128), 32f);

        if (quadros.Length == 0)
            return null;

        GameObject obj = new GameObject("Fonte dos Desejos");
        obj.transform.SetParent(pai, false);
        obj.transform.position = pe + Vector2.up * 1.9f;

        FonteDosDesejos f = obj.AddComponent<FonteDosDesejos>();
        f.quadros = quadros;
        f.armas = armas;
        f.desenho = obj.AddComponent<SpriteRenderer>();
        f.desenho.sprite = quadros[0];
        f.desenho.color = Tom;
        // A fonte e baixa: fica no chao, o jogador anda em volta.
        f.desenho.sortingOrder = Pedreiro.OrdemDosEnfeites + 1;

        obj.layer = Pedreiro.CamadaDaParede;
        CircleCollider2D c = obj.AddComponent<CircleCollider2D>();
        c.radius = 1.4f;
        f.luz = Iluminacao.Luz(obj.transform, Vector2.zero, Tom, 4f, 0.8f, 0.06f);
        return f;
    }

    private void Update()
    {
        if (!usada)
            desenho.sprite = quadros[Mathf.FloorToInt(Time.time * 8f) % quadros.Length];
    }

    private ArmaCarregada ArmaNaMao()
    {
        if (jogador == null)
        {
            GameObject j = GameObject.FindWithTag("Player");
            jogador = j != null ? j.transform : null;
        }

        return jogador != null && jogador.TryGetComponent(out ArmaDoJogador armas) ? armas.Atual : null;
    }

    public override void Usar(GameObject quem)
    {
        if (usada || !quem.TryGetComponent(out ArmaDoJogador armas) || armas.Atual == null)
            return;

        ArmaCarregada jogada = armas.Largar();
        usada = true;
        StartCoroutine(Responder(jogada.Dados, quem.transform.position));
    }

    // A arma voa ate a agua, a fonte borbulha e devolve outra na frente.
    private IEnumerator Responder(DadosDaArma jogada, Vector2 de)
    {
        GameObject voando = new GameObject("Arma jogada");
        SpriteRenderer sr = voando.AddComponent<SpriteRenderer>();
        sr.sprite = jogada.Desenho;
        sr.sortingOrder = 30;
        Vector2 ate = (Vector2)transform.position + Vector2.down * 0.3f;

        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f)
        {
            voando.transform.position = Vector2.Lerp(de, ate, t) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 1.5f;
            voando.transform.rotation = Quaternion.Euler(0f, 0f, t * 540f);
            yield return null;
        }

        Destroy(voando);
        Sons.Tocar(Som.Item, 0.8f);
        CameraDoJogo.Tremer(0.08f, 0.3f);
        yield return new WaitForSeconds(0.6f);

        // A raridade da resposta: 45% uma acima, 35% a mesma, 20% uma abaixo.
        float sorte = Random.value;
        int mudou = sorte < 0.45f ? 1 : sorte < 0.8f ? 0 : -1;
        int alvo = Mathf.Clamp((int)jogada.raridade + mudou, 0, Raridades.Quantas - 1);
        DadosDaArma nova = Sortear(alvo, jogada);

        if (nova == null)
            nova = jogada;

        int diferenca = (int)nova.raridade - (int)jogada.raridade;
        Vector2 frente = (Vector2)transform.position + Vector2.down * 2.1f;
        ArmaNoChao.Criar(new ArmaCarregada(nova), frente, transform.parent);
        EfeitoDeFolha.Tocar(ArteDoAntigo.ExplosaoPequena, frente, 24f, 25);

        string frase = diferenca > 0 ? Idioma.T("Sorte!") + " " : diferenca < 0 ? Idioma.T("Que azar...") + " " : "";
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.2f, frase + Idioma.T(nova.nome) + " [" + Raridades.Nome(nova.raridade) + "]",
                               Raridades.Cor(nova.raridade), 2.5f);
        Sons.Tocar(diferenca >= 0 ? Som.Segredo : Som.Negado, 0.8f);

        // Secou.
        desenho.sprite = quadros[0];
        desenho.color = new Color(0.55f, 0.52f, 0.6f);

        if (luz != null)
            luz.enabled = false;
    }

    // Uma arma da raridade pedida (outra que nao a jogada); sem nenhuma dessa, a mais perto dela.
    private DadosDaArma Sortear(int raridade, DadosDaArma jogada)
    {
        for (int distancia = 0; distancia < Raridades.Quantas; distancia++)
        {
            foreach (int r in new[] { raridade + distancia, raridade - distancia })
            {
                List<DadosDaArma> servem = new List<DadosDaArma>();

                if (armas != null)
                {
                    foreach (DadosDaArma a in armas)
                    {
                        if (a != null && a != jogada && a.desenhoNaMao != null && a.pesoNoBau > 0f && (int)a.raridade == r && Melhorias.Liberada(a))
                            servem.Add(a);
                    }
                }

                if (servem.Count > 0)
                    return servem[Random.Range(0, servem.Count)];
            }
        }

        return null;
    }
}
