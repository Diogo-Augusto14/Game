using UnityEngine;

/// <summary>
/// O item ativo (um so de cada vez), usado no R ou no clique do analogico direito. Recarrega
/// derrotando inimigos (cada item diz quantos). Pegar outro ativo troca: o velho fica num pedestal no
/// chao. Veio do jogo antigo (la recarregava por sala).
/// </summary>
[DisallowMultipleComponent]
public class ItemAtivoDoJogador : MonoBehaviour
{
    private ControlesDoJogador controles;
    private Vida vida;

    public ItemPassivo Item { get; private set; }

    /// <summary>Inimigos derrotados desde o ultimo uso.</summary>
    public int Carga { get; private set; }

    public bool Pronto => Item != null && Carga >= Item.Recarga;

    public float Fracao => Item == null ? 0f : Mathf.Clamp01((float)Carga / Item.Recarga);

    private void Awake()
    {
        controles = GetComponent<ControlesDoJogador>();
        vida = GetComponent<Vida>();
        GeradorDoAndar.AoMatarInimigo += Matou;
    }

    private void OnDestroy() => GeradorDoAndar.AoMatarInimigo -= Matou;

    /// <summary>Equipa o ativo (cheio); devolve o que estava, pra cair no chao.</summary>
    public ItemPassivo Equipar(ItemPassivo novo, int carga = -1)
    {
        ItemPassivo velho = Item;
        Item = novo;
        Carga = carga < 0 && novo != null ? novo.Recarga : Mathf.Max(0, carga);
        return velho;
    }

    private void Matou(Vector2 onde)
    {
        if (Item == null || Carga >= Item.Recarga)
            return;

        Carga++;

        if (Carga == Item.Recarga)
            TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.1f, "Item pronto!", new Color(0.6f, 1f, 0.7f), 1f);
    }

    private void Update()
    {
        if (controles == null || !controles.isActiveAndEnabled || !controles.UsouAtivo || Item == null || (vida != null && vida.Morto))
            return;

        if (!Pronto || !Usar(Item.Ativo))
        {
            Sons.Tocar(Som.Negado, 0.6f);
            return;
        }

        Carga = 0;
    }

    private bool Usar(TipoDeAtivo tipo)
    {
        switch (tipo)
        {
            case TipoDeAtivo.EspiralDoTempo:
                CondicaoDoInimigo.TempoLentoAte = Time.time + 6f;
                Sons.Tocar(Som.Magia, 0.8f, 0f);
                Dizer("O tempo se arrasta...", new Color(0.4f, 1f, 0.85f));
                return true;

            case TipoDeAtivo.CristaisDoTrovao:
            {
                int acertou = 0;

                foreach (Vida inimigo in GeradorDoAndar.Vivos)
                {
                    if (inimigo == null || inimigo.Morto || Vector2.Distance(inimigo.transform.position, transform.position) > 14f)
                        continue;

                    inimigo.ReceberDano(new Dano(30f, Vector2.up, 2f, gameObject));
                    EfeitoDeFolha.Tocar(ArteDoAntigo.ExplosaoPequena, inimigo.transform.position, 20f, 25);
                    acertou++;
                }

                if (acertou == 0)
                {
                    Dizer("Nenhum inimigo", new Color(0.8f, 0.8f, 0.9f));
                    return false;
                }

                Sons.Tocar(Som.Explosao, 0.8f);
                CameraDoJogo.Tremer(0.3f, 0.3f);
                return true;
            }

            case TipoDeAtivo.MoedaDoDestino:
            {
                int trocou = 0;

                foreach (Pedestal p in FindObjectsByType<Pedestal>(FindObjectsSortMode.None))
                {
                    if (p != null && Vector2.Distance(p.transform.position, transform.position) < 12f && p.Trocar())
                        trocou++;
                }

                if (trocou == 0)
                {
                    Dizer("Nada para trocar", new Color(0.8f, 0.8f, 0.9f));
                    return false;
                }

                Sons.Tocar(Som.Moeda, 1f, 0f);
                return true;
            }

            case TipoDeAtivo.CometaDeFogo:
            {
                DadosDaArma fogo = Resources.Load<DadosDaArma>("ArmasDosHerois/CajadoDoMago");

                if (fogo != null)
                {
                    DadosDaArma anel = Instantiate(fogo);
                    anel.tirosPorDisparo = 12;
                    anel.anel = true;
                    anel.dano = 8f;
                    anel.dispersao = 0f;
                    anel.Disparar(transform.position, Vector2.right, gameObject, Lado.Jogador);
                }

                Sons.Tocar(Som.TiroDeFogo, 1f);
                return true;
            }

            case TipoDeAtivo.PessegoEncantado:
                if (vida == null || vida.Atual >= vida.Maxima)
                {
                    Dizer("Vida cheia", new Color(0.8f, 0.8f, 0.9f));
                    return false;
                }

                vida.Curar(2f);
                Sons.Tocar(Som.Cura, 1f, 0f);
                Dizer("+ vida", new Color(1f, 0.35f, 0.4f));
                return true;

            case TipoDeAtivo.BombaEterna:
                if (TryGetComponent(out Bolsa bolsa))
                    bolsa.SoltarBomba();

                return true;
        }

        return false;
    }

    private void Dizer(string texto, Color cor) =>
        TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.1f, texto, cor, 1.2f);
}
