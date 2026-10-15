using UnityEngine;

/// <summary>
/// Alguem do acampamento com quem da pra conversar (1.21.0): interagir abre a <see cref="Conversa"/> com o
/// que a pessoa tem pra falar agora (<see cref="Falas.DaVez"/>); interagir de novo passa a fala. Vai no
/// mesmo objeto do boneco (o mercador, o ferreiro, o apostador). Monte com <see cref="Por"/>.
/// </summary>
public class Falante : Interativo
{
    public QuemFala Quem { get; private set; }

    public override float Alcance => 2.3f;

    public override string Dica => Conversa.Com(this) ? Idioma.T("continuar a conversa") : Idioma.F("conversar com {0}", Falas.Curto(Quem));

    /// <summary>Poe a conversa no boneco.</summary>
    public static Falante Por(GameObject boneco, QuemFala quem)
    {
        if (boneco == null)
            return null;

        Falante f = boneco.AddComponent<Falante>();
        f.Quem = quem;
        return f;
    }

    private Transform jogador;

    private void Update()
    {
        if (jogador == null)
        {
            GameObject j = GameObject.FindWithTag("Player");
            jogador = j != null ? j.transform : null;
        }
        else if (Vector2.Distance(jogador.position, transform.position) < Alcance + 1.5f)
        {
            // Chegou perto do primeiro: o que e o acampamento.
            DicasDaPrimeiraVez.Mostrar("acampamento");
        }
    }

    public override void Usar(GameObject jogador)
    {
        if (Conversa.Com(this))
            Conversa.Avancar();
        else
            Conversa.Abrir(this, Falas.DaVez(Quem));
    }
}
