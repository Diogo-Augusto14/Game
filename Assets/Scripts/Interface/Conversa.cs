using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A caixa de conversa do acampamento (1.21.0): o nome de quem fala e a fala, embaixo da tela, com as
/// letras aparecendo aos poucos. Interagir (E, ou B no controle) com a mesma pessoa passa pra proxima fala
/// (ou mostra a fala inteira, se ainda estiver escrevendo); a ultima fecha. Afastou, fecha. O jogo nao para.
/// Quem abre e o <see cref="Falante"/>.
/// </summary>
[DisallowMultipleComponent]
public class Conversa : MonoBehaviour
{
    private const float LetrasPorSegundo = 45f;
    private const float Longe = 4f;

    private static Conversa atual;

    private Falante quem;
    private readonly List<string> falas = new List<string>();
    private int indice;
    private float desde;
    private Transform jogador;
    private GUIStyle estiloDoTexto;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar() => atual = null;

    /// <summary>Esta pessoa esta falando agora.</summary>
    public static bool Com(Falante falante) => atual != null && atual.quem == falante;

    public static void Abrir(Falante falante, List<string> linhas)
    {
        if (falante == null || linhas == null || linhas.Count == 0)
            return;

        if (atual == null)
            atual = new GameObject("Conversa").AddComponent<Conversa>();

        atual.quem = falante;
        atual.falas.Clear();
        atual.falas.AddRange(linhas);
        atual.indice = 0;
        atual.desde = 0f;
        Sons.Tocar(Som.Menu, 0.5f);
    }

    /// <summary>Passa pra proxima fala (ou completa a de agora); depois da ultima, fecha.</summary>
    public static void Avancar()
    {
        if (atual == null)
            return;

        string fala = atual.falas[atual.indice];

        if (atual.desde * LetrasPorSegundo < fala.Length)
        {
            atual.desde = fala.Length / LetrasPorSegundo + 0.01f;
            return;
        }

        atual.indice++;
        atual.desde = 0f;

        if (atual.indice >= atual.falas.Count)
            Fechar();
        else
            Sons.Tocar(Som.Menu, 0.4f);
    }

    public static void Fechar()
    {
        if (atual != null)
            Destroy(atual.gameObject);

        atual = null;
    }

    private void Update()
    {
        if (quem == null)
        {
            Fechar();
            return;
        }

        if (jogador == null)
        {
            GameObject j = GameObject.FindWithTag("Player");
            jogador = j != null ? j.transform : null;
        }

        if (jogador != null && Vector2.Distance(jogador.position, quem.transform.position) > Longe)
        {
            Fechar();
            return;
        }

        desde += Time.unscaledDeltaTime;
    }

    private void OnDestroy()
    {
        if (atual == this)
            atual = null;
    }

    private void OnGUI()
    {
        if (quem == null || indice >= falas.Count || TelaDoJogo.MenuAberto || Event.current.type != EventType.Repaint)
            return;

        GUI.depth = -20;
        int u = Desenho.U;
        string fala = falas[indice];
        int letras = Mathf.Clamp(Mathf.FloorToInt(desde * LetrasPorSegundo), 0, fala.Length);
        string mostrar = fala.Substring(0, letras);

        if (estiloDoTexto == null)
            estiloDoTexto = new GUIStyle(Desenho.Estilo(FonteDoJogo.Texto, 20 * u, TextAnchor.UpperLeft)) { wordWrap = true };

        GUIStyle nome = Desenho.Estilo(FonteDoJogo.Titulo, 22 * u, TextAnchor.UpperLeft);
        GUIStyle dica = Desenho.Estilo(FonteDoJogo.Texto, 14 * u, TextAnchor.LowerRight);
        float largura = Mathf.Min(Screen.width - 40 * u, 900 * u);
        float alturaDoTexto = estiloDoTexto.CalcHeight(new GUIContent(fala), largura - 40 * u);
        float altura = 44 * u + alturaDoTexto + 20 * u;
        Rect caixa = new Rect((Screen.width - largura) * 0.5f, Screen.height - 150 * u - altura, largura, altura);

        Desenho.Cor(caixa, new Color(0.04f, 0.03f, 0.04f, 0.88f));
        Desenho.Cor(new Rect(caixa.x, caixa.y, caixa.width, 2 * u), new Color(0.55f, 0.2f, 0.12f, 0.95f));
        Desenho.Texto(new Rect(caixa.x + 20 * u, caixa.y + 10 * u, caixa.width - 40 * u, 28 * u), Falas.Nome(quem.Quem), nome, Desenho.Dourado);
        Desenho.Texto(new Rect(caixa.x + 20 * u, caixa.y + 44 * u, caixa.width - 40 * u, alturaDoTexto), mostrar, estiloDoTexto,
                      new Color(0.95f, 0.9f, 0.82f));

        string continuar = Controle.EmUso ? "[B] continuar" : "[E] continuar";
        Desenho.Texto(new Rect(caixa.x, caixa.y, caixa.width - 12 * u, caixa.height - 6 * u), continuar, dica, Desenho.Apagado);
    }
}
