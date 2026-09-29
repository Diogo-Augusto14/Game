using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Desenho de uma tecla (ou de um botao do controle) nas dicas de controle (menu, pausa,
/// HUD). Afunda enquanto esta segurada, pra quem esta aprendendo ver o que apertou.
/// Montado pela <see cref="TelaSimples.LinhaDeTeclas"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class IconeDeTecla : MonoBehaviour
{
    private KeyCode tecla;
    private BotaoDoControle botao;
    private Image imagem;
    private Sprite solta;
    private Sprite apertada;

    public void Configurar(KeyCode novaTecla)
    {
        tecla = novaTecla;
        botao = BotaoDoControle.Nenhum;
        imagem = GetComponent<Image>();
        solta = ArteDaInterface.Tecla(tecla);
        apertada = ArteDaInterface.Tecla(tecla, true);
        imagem.sprite = solta;
    }

    /// <summary>Botao do controle: o desenho segue o controle ligado (Xbox ou PlayStation).</summary>
    public void Configurar(BotaoDoControle novoBotao)
    {
        tecla = KeyCode.None;
        botao = novoBotao;
        imagem = GetComponent<Image>();
        imagem.sprite = ArteDaInterface.DesenhoDoBotao(botao, Controle.PlayStation);
    }

    private void Update()
    {
        if (imagem == null)
            return;

        if (botao != BotaoDoControle.Nenhum)
        {
            Sprite desenho = ArteDaInterface.DesenhoDoBotao(botao, Controle.PlayStation, Controle.Segurando(botao));

            if (desenho != null)
                imagem.sprite = desenho;

            return;
        }

        if (apertada == null)
            return;

        imagem.sprite = Input.GetKey(tecla) ? apertada : solta;
    }
}
