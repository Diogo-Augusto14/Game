using UnityEngine;

/// <summary>
/// Peca de cenario feita pra ser clonada e esticada a mao.
///
/// O problema que ele resolve: um SpriteRenderer em modo Tiled/Sliced tem alca de
/// redimensionar na Scene (a ferramenta de retangulo), mas o BoxCollider2D NAO acompanha.
/// Esticar o desenho e deixar a colisao do tamanho antigo e o bug mais chato de montar
/// fase a mao — o boneco atravessa o bloco, ou bate no nada.
///
/// Com este componente, o colisor segue o tamanho do sprite a cada quadro no editor.
/// Voce estica o desenho e a colisao vai junto.
///
/// [ExecuteAlways] e o que faz ele rodar FORA do Play — sem isso, a sincronia so
/// aconteceria no Play, que e tarde demais pra quem esta desenhando a fase.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class BlocoDoCenario : MonoBehaviour
{
    [Tooltip("O BoxCollider2D copia o tamanho do sprite. Desligue pra ajustar a colisao na mao")]
    [SerializeField] private bool colisorAcompanhaOSprite = true;

    [Tooltip("Sobra de colisao em cada lado. Negativo deixa a colisao um tiquinho MENOR que o " +
             "desenho, o que ajuda o boneco a nao engatar nas quinas")]
    [SerializeField] private Vector2 folgaDaColisao = Vector2.zero;

    private SpriteRenderer renderizador;
    private BoxCollider2D caixa;

    public SpriteRenderer Renderizador
    {
        get
        {
            if (renderizador == null)
                renderizador = GetComponent<SpriteRenderer>();

            return renderizador;
        }
    }

    /// <summary>Tamanho do bloco em unidades do mundo. Ler e escrever aqui move o desenho e a colisao juntos.</summary>
    public Vector2 Tamanho
    {
        get => Renderizador != null ? Renderizador.size : Vector2.one;
        set
        {
            if (Renderizador == null)
                return;

            Renderizador.size = value;
            Sincronizar();
        }
    }

    private void OnEnable()
    {
        Sincronizar();
    }

    private void OnValidate()
    {
        Sincronizar();
    }

    private void Update()
    {
        // No Play o tamanho nao muda mais; sincronizar todo quadro seria desperdicio.
        if (!Application.isPlaying)
            Sincronizar();
    }

    /// <summary>Copia o tamanho do sprite pro colisor. Publico pra ferramentas de editor chamarem.</summary>
    public void Sincronizar()
    {
        if (!colisorAcompanhaOSprite)
            return;

        SpriteRenderer sr = Renderizador;

        if (sr == null || sr.sprite == null)
            return;

        // Em modo Simple o campo "size" nao e usado pelo desenho: sincronizar leria um
        // valor que nao tem nada a ver com o que aparece na tela.
        if (sr.drawMode == SpriteDrawMode.Simple)
            return;

        if (caixa == null)
            caixa = GetComponent<BoxCollider2D>();

        if (caixa == null)
            return;

        Vector2 alvo = sr.size + folgaDaColisao;
        alvo.x = Mathf.Max(0.01f, alvo.x);
        alvo.y = Mathf.Max(0.01f, alvo.y);

        if (caixa.size != alvo)
            caixa.size = alvo;

        if (caixa.offset != Vector2.zero)
            caixa.offset = Vector2.zero;
    }
}
