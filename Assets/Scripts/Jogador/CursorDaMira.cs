using UnityEngine;

/// <summary>
/// Troca o cursor do mouse por uma mira enquanto o jogo roda e e o mouse que mira. Com o controle
/// na mao o cursor some. Fica no jogador.
/// </summary>
[DisallowMultipleComponent]
public class CursorDaMira : MonoBehaviour
{
    [Tooltip("A imagem da mira (importada como Cursor). O ponto quente e o centro dela")]
    [SerializeField] private Texture2D mira;

    private ControlesDoJogador controles;
    private int estado = -1;   // -1 = ainda nao aplicou, 0 = cursor normal, 1 = mira, 2 = escondido

    private void Awake()
    {
        controles = GetComponent<ControlesDoJogador>();
    }

    private void Update()
    {
        int quer;

        if (Time.timeScale <= 0f || controles == null)
            quer = 0;
        else
            quer = controles.MiraPeloMouse ? 1 : 2;

        if (quer == estado)
            return;

        estado = quer;
        Cursor.visible = quer != 2;

        if (quer == 1 && mira != null)
            Cursor.SetCursor(mira, new Vector2(mira.width * 0.5f, mira.height * 0.5f), CursorMode.Auto);
        else
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private void OnDisable()
    {
        Cursor.visible = true;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        estado = -1;
    }
}
