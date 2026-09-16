#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Mostra no topo do Inspector, COM O JOGO RODANDO, o estado ao vivo do boneco e do
/// inimigo: em que estado a maquina esta, se esta no chao, velocidade, pulos gastos,
/// progresso do golpe.
///
/// Por que precisa existir: esses valores sao PROPRIEDADES C# (<c>EstadoAtual</c>,
/// <c>NoChao</c>...), e o Inspector so desenha CAMPOS. Sem este painel, a unica forma de
/// acompanhar a maquina de estados seria encher o codigo de Debug.Log — que e justamente
/// o que a gente nao quer enquanto aprende como as pecas conversam.
///
/// Nao muda nada no jogo: e so leitura, e so aparece dentro do Play.
/// </summary>
public static class PainelDeEstado
{
    /// <summary>Uma linha do painel, no formato "rotulo: valor".</summary>
    public static void Linha(string rotulo, string valor)
    {
        EditorGUILayout.LabelField(rotulo, valor);
    }

    public static void Linha(string rotulo, bool valor)
    {
        Linha(rotulo, valor ? "sim" : "nao");
    }

    public static void Linha(string rotulo, float valor)
    {
        Linha(rotulo, valor.ToString("0.00"));
    }

    /// <summary>Abre a caixa do painel. Devolve false fora do Play (nao ha o que mostrar).</summary>
    public static bool Abrir(string titulo)
    {
        if (!Application.isPlaying)
            return false;

        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(titulo, EditorStyles.boldLabel);

        return true;
    }

    public static void Fechar()
    {
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4f);
    }
}

// ===================================================================== jogador
[CustomEditor(typeof(Movimento))]
public class EditorDeMovimento : Editor
{
    public override void OnInspectorGUI()
    {
        Movimento mov = (Movimento)target;

        if (PainelDeEstado.Abrir("Estado ao vivo"))
        {
            PainelDeEstado.Linha("Estado", mov.EstadoAtual.ToString());
            PainelDeEstado.Linha("No chao", mov.NoChao);
            PainelDeEstado.Linha("Na parede", mov.NaParede);
            PainelDeEstado.Linha("Invencivel", mov.Invencivel);
            PainelDeEstado.Linha("Pulos usados", $"{mov.PulosUsados} de {mov.maximoDePulo}");
            PainelDeEstado.Linha("Velocidade X", mov.VelocidadeHorizontal);
            PainelDeEstado.Linha("Velocidade Y", mov.VelocidadeVertical);
            PainelDeEstado.Linha("Olhando pra", mov.direcao > 0f ? "direita" : "esquerda");
            PainelDeEstado.Linha("Lado pedido", mov.LadoPedido);

            if (mov.EscadaEncostada != null)
                PainelDeEstado.Linha("Escada encostada", mov.EscadaEncostada.name);

            MostrarAtaque(mov);
            MostrarCura(mov);
            MostrarVida(mov);

            PainelDeEstado.Fechar();
        }

        DrawDefaultInspector();
    }

    private static void MostrarAtaque(Movimento mov)
    {
        Ataque ataque = mov.GetComponent<Ataque>();

        if (ataque == null)
            return;

        if (!ataque.EstaAtacando)
        {
            PainelDeEstado.Linha("Golpe", "nenhum");
            return;
        }

        PainelDeEstado.Linha("Golpe", ataque.GolpeAtual != null ? ataque.GolpeAtual.nome : "?");
        PainelDeEstado.Linha("Progresso do golpe", ataque.Progresso);
    }

    private static void MostrarCura(Movimento mov)
    {
        Cura cura = mov.GetComponent<Cura>();

        if (cura == null)
            return;

        PainelDeEstado.Linha("Frascos", $"{cura.Frascos} de {cura.FrascosMaximos}");

        if (cura.Curando)
            PainelDeEstado.Linha("Curando", cura.Progresso);
    }

    private static void MostrarVida(Movimento mov)
    {
        Vida vida = mov.GetComponent<Vida>();

        if (vida == null)
            return;

        PainelDeEstado.Linha("Vida", $"{vida.VidaAtual:0} de {vida.VidaMaxima:0}");
    }

    // Sem isto o painel so atualizaria quando o mouse passasse por cima do Inspector.
    public override bool RequiresConstantRepaint() => Application.isPlaying;
}

// ===================================================================== inimigo
[CustomEditor(typeof(Inimigo))]
public class EditorDeInimigo : Editor
{
    public override void OnInspectorGUI()
    {
        Inimigo inimigo = (Inimigo)target;

        if (PainelDeEstado.Abrir("Estado ao vivo"))
        {
            PainelDeEstado.Linha("Estado", inimigo.EstadoAtual.ToString());
            PainelDeEstado.Linha("No chao", inimigo.NoChao);
            PainelDeEstado.Linha("Perseguindo", inimigo.Perseguindo);
            PainelDeEstado.Linha("Velocidade X", inimigo.VelocidadeHorizontal);
            PainelDeEstado.Linha("Olhando pra", inimigo.Direcao > 0f ? "direita" : "esquerda");

            if (inimigo.EstadoAtual == Inimigo.Estado.Atacando)
            {
                PainelDeEstado.Linha("Golpe do combo", inimigo.GolpeAtual == 0 ? "primeiro" : "segundo");
                PainelDeEstado.Linha("Progresso do golpe", inimigo.ProgressoDoGolpe);
            }

            Vida vida = inimigo.GetComponent<Vida>();

            if (vida != null)
                PainelDeEstado.Linha("Vida", $"{vida.VidaAtual:0} de {vida.VidaMaxima:0}");

            PainelDeEstado.Fechar();
        }

        DrawDefaultInspector();
    }

    public override bool RequiresConstantRepaint() => Application.isPlaying;
}
#endif
