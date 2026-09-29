using UnityEngine;

/// <summary>
/// Linha de dicas com duas versoes, teclado e controle (o " || " da
/// <see cref="TelaSimples.LinhaDeTeclas"/>). Redesenha sozinha quando o jogador troca de
/// um pro outro, ou liga um controle de outra marca (Xbox / PlayStation).
/// </summary>
[DisallowMultipleComponent]
public class DicaDupla : MonoBehaviour
{
    private string teclado;
    private string controle;
    private int tamanho;
    private Color cor;
    private bool desenhouControle;
    private bool desenhouPlayStation;

    /// <summary>Guarda as duas versoes e devolve a que vale agora.</summary>
    public string Guardar(string noTeclado, string noControle, int tamanhoDoTexto, Color corDoTexto)
    {
        teclado = noTeclado;
        controle = noControle;
        tamanho = tamanhoDoTexto;
        cor = corDoTexto;
        desenhouControle = Controle.EmUso;
        desenhouPlayStation = Controle.PlayStation;
        return desenhouControle ? controle : teclado;
    }

    private void Update()
    {
        if (teclado == null)
            return;

        bool usando = Controle.EmUso;
        bool playStation = Controle.PlayStation;

        // O desenho de cada botao ja segue a marca; so o texto de fallback precisa refazer.
        if (usando == desenhouControle && (!usando || playStation == desenhouPlayStation))
            return;

        desenhouControle = usando;
        desenhouPlayStation = playStation;
        TelaSimples.Desenhar((RectTransform)transform, usando ? controle : teclado, tamanho, cor);
    }
}
