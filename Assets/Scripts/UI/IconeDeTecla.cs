using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Desenho de uma tecla nas dicas de controle (menu, pausa, HUD). Afunda enquanto a tecla
/// esta segurada, pra quem esta aprendendo ver o que apertou. Montado pela
/// <see cref="TelaSimples.LinhaDeTeclas"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class IconeDeTecla : MonoBehaviour
{
    private KeyCode tecla;
    private Image imagem;
    private Sprite solta;
    private Sprite apertada;

    public void Configurar(KeyCode novaTecla)
    {
        tecla = novaTecla;
        imagem = GetComponent<Image>();
        solta = ArteDaInterface.Tecla(tecla);
        apertada = ArteDaInterface.Tecla(tecla, true);
        imagem.sprite = solta;
    }

    private void Update()
    {
        if (imagem == null || apertada == null)
            return;

        imagem.sprite = Input.GetKey(tecla) ? apertada : solta;
    }
}
