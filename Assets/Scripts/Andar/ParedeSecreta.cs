using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A parede que esconde a passagem pra sala secreta (<see cref="TipoDeSala.Secreta"/>): por fora e uma
/// parede como as outras, so com uma rachadura e umas pedrinhas caidas no pe. Tiro nao faz nada; uma
/// bomba encostada nela abre a passagem (o <see cref="GeradorDoAndar"/> refaz os ladrilhos e os
/// colisores daquele pedaco, e o minimapa passa a mostrar a sala).
///
/// O colisor e um gatilho escondido dentro da parede (so a explosao enxerga ele). Monte com
/// <see cref="Criar"/>.
/// </summary>
public class ParedeSecreta : MonoBehaviour, IBloqueioDeDano
{
    private static Sprite rachadura;

    private System.Action aoAbrir;
    private readonly List<GameObject> pistas = new List<GameObject>();
    private bool aberta;

    /// <param name="passagem">As celulas que a bomba abre (o corredor ate a sala secreta).</param>
    /// <param name="dona">A sala de onde se chega na passagem (a rachadura fica do lado dela).</param>
    /// <param name="aoAbrir">Chamado uma vez, quando a bomba abre.</param>
    public static ParedeSecreta Criar(HashSet<Vector2Int> passagem, SalaDaPlanta dona, Transform pai, System.Action aoAbrir)
    {
        if (passagem.Count == 0)
            return null;

        // A boca: as celulas da passagem encostadas na sala.
        List<Vector2Int> boca = new List<Vector2Int>();
        Vector2Int paraDentro = Vector2Int.zero;
        Vector2Int[] lados = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int c in passagem)
        {
            foreach (Vector2Int d in lados)
            {
                if (dona.celulas.Contains(c - d))
                {
                    boca.Add(c);
                    paraDentro = d;
                    break;
                }
            }
        }

        if (boca.Count == 0)
        {
            // Sem encostar direito (nao deveria): a celula da passagem mais perto do meio da sala.
            Vector2Int perto = default;
            float menor = float.MaxValue;

            foreach (Vector2Int c in passagem)
            {
                float d = (c - dona.Meio).sqrMagnitude;

                if (d < menor)
                {
                    menor = d;
                    perto = c;
                }
            }

            boca.Add(perto);
            Vector2 rumo = (Vector2)perto - dona.Meio;
            paraDentro = Mathf.Abs(rumo.x) > Mathf.Abs(rumo.y) ? new Vector2Int(rumo.x > 0 ? 1 : -1, 0) : new Vector2Int(0, rumo.y > 0 ? 1 : -1);
        }

        Vector2 meioDaBoca = Vector2.zero;

        foreach (Vector2Int c in boca)
            meioDaBoca += c;

        meioDaBoca /= boca.Count;

        // O gatilho cobre a passagem toda, um pouco pra dentro (o tiro para na parede antes de tocar nele).
        RectInt caixa = Caverna.Limites(passagem);
        GameObject obj = new GameObject("Parede secreta");
        obj.transform.SetParent(pai, false);
        obj.transform.position = new Vector3(caixa.xMin - 0.5f + caixa.width * 0.5f, caixa.yMin - 0.5f + caixa.height * 0.5f, 0f);

        ParedeSecreta parede = obj.AddComponent<ParedeSecreta>();
        parede.aoAbrir = aoAbrir;

        BoxCollider2D colisor = obj.AddComponent<BoxCollider2D>();
        colisor.isTrigger = true;
        colisor.size = new Vector2(Mathf.Max(0.4f, caixa.width - 0.6f), Mathf.Max(0.4f, caixa.height - 0.6f));

        Vida vida = obj.AddComponent<Vida>();
        vida.Configurar(Lado.Neutro, 1f);
        vida.AoMorrer += parede.Abrir;

        parede.Pistas(meioDaBoca, paraDentro, pai);
        return parede;
    }

    // So a bomba abre: o resto bate e nao faz nada.
    public bool Bloqueia(Dano dano) => aberta || dano.fonte == null || !dano.fonte.TryGetComponent(out Bomba _);

    // A rachadura na parede e as pedrinhas no chao, na frente da boca.
    private void Pistas(Vector2 boca, Vector2Int paraDentro, Transform pai)
    {
        if (rachadura == null)
            rachadura = DesenharRachadura();

        // Na parede de cima a rachadura fica na face de tijolo (as 2 celulas acima do chao); nas outras,
        // no topo da parede, logo depois do chao.
        Vector2 onde = boca + (paraDentro == Vector2Int.up ? new Vector2(0f, 0.5f) : Vector2.zero);
        GameObject racha = new GameObject("Rachadura");
        racha.transform.SetParent(pai, false);
        racha.transform.position = onde;

        if (paraDentro.x != 0)
            racha.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

        // Na parede de baixo a beirada e desenhada por cima dos bonecos; nas outras, a parede fica atras.
        SpriteRenderer sr = racha.AddComponent<SpriteRenderer>();
        sr.sprite = rachadura;
        sr.sortingOrder = paraDentro == Vector2Int.down ? Pedreiro.OrdemDaFrente + 1 : Pedreiro.OrdemDasParedes + 1;
        pistas.Add(racha);

        Sprite[] pedras = ArteDoAntigo.Pedras;

        for (int i = 0; i < 3 && pedras.Length > 0; i++)
        {
            Vector2 lado = paraDentro.y != 0 ? Vector2.right : Vector2.up;
            Vector2 p = boca - (Vector2)paraDentro * 0.9f + lado * Random.Range(-1f, 1f) + Random.insideUnitCircle * 0.15f;
            GameObject pedra = new GameObject("Pedrinha");
            pedra.transform.SetParent(pai, false);
            pedra.transform.position = p;
            pedra.transform.localScale = Vector3.one * Random.Range(0.45f, 0.65f);
            SpriteRenderer desenho = pedra.AddComponent<SpriteRenderer>();
            desenho.sprite = pedras[Random.Range(0, pedras.Length)];
            desenho.sortingOrder = Pedreiro.OrdemDosEnfeites + 1;
            desenho.color = new Color(0.8f, 0.78f, 0.82f);
            pistas.Add(pedra);
        }
    }

    private void Abrir()
    {
        if (aberta)
            return;

        aberta = true;
        Sons.Tocar(Som.Segredo, 0.9f);
        CameraDoJogo.Tremer(0.25f, 0.35f);

        foreach (GameObject pista in pistas)
        {
            if (pista != null)
            {
                EfeitoDeFolha.Tocar(ArteDoAntigo.ExplosaoPequena, pista.transform.position, 24f, 25);
                Destroy(pista);
            }
        }

        aoAbrir?.Invoke();
        Destroy(gameObject);
    }

    // Uma rachadura escura de 3 x 1,5 celulas (32 pixels por celula): um risco torto que se divide.
    private static Sprite DesenharRachadura()
    {
        const int largura = 96, altura = 48;
        Texture2D textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color32[] pixels = new Color32[largura * altura];
        Color32 escuro = new Color32(14, 10, 20, 230);
        Color32 meio = new Color32(40, 32, 46, 150);
        // A beirada clara embaixo do risco: a rachadura aparece ate no topo escuro das paredes dos lados.
        Color32 claro = new Color32(176, 160, 150, 170);
        System.Random sorte = new System.Random(7);

        void Pintar(int x, int y, Color32 cor)
        {
            if (x >= 0 && y >= 0 && x < largura && y < altura && pixels[y * largura + x].a < cor.a)
                pixels[y * largura + x] = cor;
        }

        void Risco(int x, int y, int dx, int passos)
        {
            for (int i = 0; i < passos; i++)
            {
                Pintar(x, y, escuro);
                Pintar(x, y + 1, meio);
                Pintar(x, y - 1, claro);
                x += dx;
                y += sorte.Next(-1, 2);
                y = Mathf.Clamp(y, 4, altura - 5);

                // Galhos curtos de vez em quando.
                if (sorte.Next(9) == 0)
                {
                    int gx = x, gy = y, sobe = sorte.Next(2) == 0 ? 1 : -1;

                    for (int k = 0; k < 4 + sorte.Next(5); k++)
                    {
                        gx += dx;
                        gy += sobe;
                        Pintar(gx, gy, meio);
                    }
                }
            }
        }

        Risco(largura / 2, altura / 2, 1, largura / 2 - 6);
        Risco(largura / 2, altura / 2, -1, largura / 2 - 6);

        textura.SetPixels32(pixels);
        textura.Apply();
        return Sprite.Create(textura, new Rect(0, 0, largura, altura), new Vector2(0.5f, 0.5f), 32f);
    }
}
