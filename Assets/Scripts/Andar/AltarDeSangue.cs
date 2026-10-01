using UnityEngine;

/// <summary>
/// A sala do sacrificio (uma das variacoes da sala amaldicoada): um altar com espinhos.
/// Pisar nele custa um coracao, e cada sacrificio paga mais que o anterior:
///   1o -> tres moedas          2o -> bomba e chave
///   3o -> um item              4o -> metade das vezes outro item, senao moedas; o altar racha
/// Nunca mata: com um coracao ou menos o altar recusa. Escudo que bloqueia o golpe tambem
/// nao vale (sem sangue, sem premio). Pra sacrificar de novo tem que sair de cima.
/// </summary>
[DisallowMultipleComponent]
public class AltarDeSangue : MonoBehaviour
{
    private const float Custo = 20f;
    private const int Maximo = 4;

    private Sala sala;
    private int sacrificios;
    private bool esperandoSair;
    private SpriteRenderer espinhos;

    public static AltarDeSangue Montar(Sala sala)
    {
        Vector2 centro = sala.transform.position;
        GameObject obj = new GameObject("Altar de sangue");
        obj.transform.SetParent(sala.transform, false);
        obj.transform.position = centro;

        Sprite altar = ArteImportada.Objeto(2, 2);
        FormasDaSala.Desenho(obj.transform, "Altar", altar != null ? altar : Fosso.Pixel(),
                             altar != null ? new Color(1f, 0.6f, 0.6f) : new Color(0.45f, 0.1f, 0.12f), Vector2.zero,
                             altar != null ? Vector2.one * 1.4f : new Vector2(1f, 0.6f), 4);

        AltarDeSangue a = obj.AddComponent<AltarDeSangue>();
        a.sala = sala;

        Sprite[] quadros = ArteImportada.EspinhosDoChao;
        a.espinhos = FormasDaSala.Desenho(obj.transform, "Espinhos",
                                          quadros != null ? quadros[quadros.Length - 1] : ArteGerada.EspinhosNoChao(),
                                          new Color(1f, 0.5f, 0.5f), new Vector2(0f, 0.15f), Vector2.one * 0.9f, 5);

        CircleCollider2D sensor = obj.AddComponent<CircleCollider2D>();
        sensor.isTrigger = true;
        sensor.radius = 0.55f;

        TextoFlutuante dica = TextoFlutuante.Mostrar(centro + Vector2.down * 1.1f, "Sangue por tesouro", new Color(1f, 0.5f, 0.5f));
        dica.transform.SetParent(obj.transform, true);
        return a;
    }

    private void OnTriggerEnter2D(Collider2D outro) => Tentar(outro);

    private void OnTriggerExit2D(Collider2D outro)
    {
        if (DoJogador(outro) != null)
            esperandoSair = false;
    }

    private static GameObject DoJogador(Collider2D outro)
    {
        GameObject quem = outro.attachedRigidbody != null ? outro.attachedRigidbody.gameObject : outro.gameObject;
        return quem.CompareTag("Player") ? quem : null;
    }

    private void Tentar(Collider2D outro)
    {
        GameObject jogador = DoJogador(outro);

        if (jogador == null || esperandoSair || sacrificios >= Maximo || !jogador.TryGetComponent(out Vida vida))
            return;

        esperandoSair = true;

        if (vida.VidaAtual <= Custo * vida.MultiplicadorDeDanoRecebido + 1f)
        {
            Sons.Tocar(Som.Negado);
            TextoFlutuante.Mostrar(transform.position + Vector3.up, "Sangue insuficiente", new Color(0.9f, 0.6f, 0.6f));
            return;
        }

        float antes = vida.VidaAtual;
        vida.TomarDano(new DanoInfo(Custo, Vector2.down, 0f, transform.position, gameObject, PesoDoGolpe.Leve, true));

        if (vida.VidaAtual >= antes)
        {
            TextoFlutuante.Mostrar(transform.position + Vector3.up, "O altar quer sangue", new Color(0.9f, 0.6f, 0.6f));
            return;
        }

        sacrificios++;
        Registro.Sacrificou();
        Sons.Tocar(Som.Espinhos);
        EfeitosDeImpacto.Mostrar(EfeitoDeImpacto.Respingo, transform.position, new Color(0.8f, 0.1f, 0.15f), 1.2f);
        Pagar(sacrificios);

        if (sacrificios >= Maximo)
        {
            Sons.Tocar(Som.Pancada);
            TextoFlutuante.Mostrar(transform.position + Vector3.up * 1.3f, "O altar rachou", new Color(0.8f, 0.8f, 0.8f));

            if (espinhos != null)
                espinhos.enabled = false;
        }
    }

    private void Pagar(int vez)
    {
        Vector2 centro = transform.position;
        Transform pai = sala != null ? sala.transform : transform.parent;

        switch (vez)
        {
            case 1:
                for (int i = -1; i <= 1; i++)
                    Coletavel.Criar(TipoDeColetavel.Moeda, centro + new Vector2(i * 0.6f, -1.1f), pai);
                break;

            case 2:
                Coletavel.Criar(TipoDeColetavel.Bomba, centro + new Vector2(-0.5f, -1.1f), pai);
                Coletavel.Criar(TipoDeColetavel.Chave, centro + new Vector2(0.5f, -1.1f), pai);
                break;

            case 3:
                Item(centro + new Vector2(-2f, 0f), pai);
                break;

            default:
                if (Random.value < 0.5f)
                {
                    Item(centro + new Vector2(2f, 0f), pai);
                }
                else
                {
                    for (int i = -2; i <= 2; i++)
                        Coletavel.Criar(TipoDeColetavel.Moeda, centro + new Vector2(i * 0.5f, -1.2f), pai);
                }

                break;
        }
    }

    private static void Item(Vector2 ponto, Transform pai)
    {
        ItemPassivo item = CatalogoDeItens.Sortear(Andar.Atual != null ? Andar.Atual.ItensQueJaSairam : null);
        Pedestal.Criar(item, ponto, pai);
        Sons.Tocar(Som.Item, 0.8f);
    }
}
