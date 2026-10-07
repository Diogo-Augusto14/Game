using UnityEngine;

/// <summary>
/// Troca o cursor do mouse por uma mira enquanto o jogo roda e e o mouse que mira. Nos menus, na
/// pausa, no fim de jogo, com a mira nas setas ou no controle, e depois da morte, volta o cursor
/// dourado de sempre. Fica no jogador; precisa da <see cref="Entrada"/>.
///
/// A mira e uma textura de 32 x 32 desenhada em <see cref="ArteDasArmas.TexturaDaMira"/>: o cursor
/// do sistema, sem nada na cena, entao ela nao atrasa nem fica atras da HUD.
/// </summary>
[DisallowMultipleComponent]
public class MiraNaTela : MonoBehaviour
{
    private const int LadoDaMira = 32;

    private static Texture2D mira;

    private Entrada entrada;
    private Vida vida;
    private bool miraLigada;

    private void Awake()
    {
        entrada = GetComponent<Entrada>();
        vida = GetComponent<Vida>();
    }

    private void Update()
    {
        bool querMira = entrada != null && entrada.MouseNaMira && Time.timeScale > 0f && (vida == null || !vida.EstaMorto);

        if (querMira == miraLigada)
            return;

        miraLigada = querMira;

        if (querMira)
        {
            if (mira == null)
                mira = ArteDasArmas.TexturaDaMira();

            Cursor.SetCursor(mira, new Vector2(LadoDaMira * 0.5f, LadoDaMira * 0.5f), CursorMode.Auto);
        }
        else
        {
            VoltarAoCursorDoMenu();
        }
    }

    private void OnDisable()
    {
        if (miraLigada)
        {
            miraLigada = false;
            VoltarAoCursorDoMenu();
        }
    }

    // O mesmo cursor que a ArteDaInterface poe ao abrir o jogo (sem ele, o cursor do sistema).
    private static void VoltarAoCursorDoMenu()
    {
        Texture2D menu = Resources.Load<Texture2D>("InterfaceDragao/Cursor");
        Cursor.SetCursor(menu, Vector2.zero, CursorMode.Auto);
    }
}
