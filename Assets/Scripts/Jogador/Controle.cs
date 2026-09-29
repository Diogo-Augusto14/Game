using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

/// <summary>Botoes do controle, com o nome do Xbox. No PlayStation: A = X, B = bola, X = quadrado, Y = triangulo.</summary>
public enum BotaoDoControle
{
    Nenhum,
    A, B, X, Y,
    Start, Select,
    LB, RB, LT, RT,
    Cruz, CruzCima, CruzBaixo, CruzEsquerda, CruzDireita,
    AnalogicoEsquerdo, AnalogicoDireito,
}

/// <summary>
/// Controle de Xbox ou PlayStation pelo Input System (o pacote ja esta no projeto e o
/// Player aceita os dois sistemas). Todo o resto pergunta pra ca:
///
///   analogico esquerdo / cruz   andar          analogico direito / A B X Y   atirar
///   LB ou LT                     bomba          Start                          pausa
///   menus: A ou Start confirma, cruz troca o heroi, Select sai; LB musica e RB efeitos
///
/// <see cref="EmUso"/> diz se o ultimo aparelho mexido foi o controle (as dicas trocam o
/// desenho das teclas pelo dos botoes). Sem controle ligado, tudo aqui devolve falso/zero.
/// </summary>
public static class Controle
{
    private const float ZonaMorta = 0.25f;

    private static bool emUso;
    private static int quadroLido = -1;

    /// <summary>O controle mexido por ultimo, ou null.</summary>
    public static Gamepad Atual => Gamepad.current;

    /// <summary>Ultima coisa mexida foi o controle (e ele continua ligado).</summary>
    public static bool EmUso
    {
        get
        {
            Ler();
            return emUso;
        }
    }

    /// <summary>O controle ligado e de PlayStation (DualShock ou DualSense): troca os desenhos.</summary>
    public static bool PlayStation => Atual is DualShockGamepad;

    // Com "Enter Play Mode" sem recarregar o dominio, o estatico sobreviveria entre Plays.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Zerar()
    {
        emUso = false;
        quadroLido = -1;
        primeiraLeitura = true;
    }

    private static bool primeiraLeitura = true;

    // Uma vez por quadro, na primeira pergunta. Olha o estado (segurado), e nao o aperto,
    // entao nao importa quem pergunta primeiro.
    private static void Ler()
    {
        if (quadroLido == Time.frameCount)
            return;

        quadroLido = Time.frameCount;
        Gamepad controle = Atual;

        if (controle == null)
        {
            emUso = false;
            return;
        }

        // Abriu o jogo com o controle ligado: ja comeca mostrando os botoes.
        if (primeiraLeitura)
        {
            primeiraLeitura = false;
            emUso = true;
        }

        if (MexeuNoControle(controle))
            emUso = true;
        else if (Keyboard.current != null && Keyboard.current.anyKey.isPressed)
            emUso = false;
    }

    private static bool MexeuNoControle(Gamepad c)
    {
        return c.leftStick.ReadValue().sqrMagnitude > 0.25f || c.rightStick.ReadValue().sqrMagnitude > 0.25f
            || c.dpad.ReadValue() != Vector2.zero
            || c.buttonSouth.isPressed || c.buttonEast.isPressed || c.buttonWest.isPressed || c.buttonNorth.isPressed
            || c.startButton.isPressed || c.selectButton.isPressed
            || c.leftShoulder.isPressed || c.rightShoulder.isPressed
            || c.leftTrigger.isPressed || c.rightTrigger.isPressed;
    }

    // ---------------- jogo ----------------
    /// <summary>Andar: analogico esquerdo (com zona morta) ou a cruz. Tamanho ate 1.</summary>
    public static Vector2 Andar
    {
        get
        {
            Gamepad c = Atual;

            if (c == null)
                return Vector2.zero;

            Vector2 cruz = c.dpad.ReadValue();

            if (cruz != Vector2.zero)
                return cruz.normalized;

            Vector2 analogico = c.leftStick.ReadValue();
            return analogico.magnitude < ZonaMorta ? Vector2.zero : Vector2.ClampMagnitude(analogico, 1f);
        }
    }

    /// <summary>
    /// Tiro numa das quatro direcoes, como no Isaac: A B X Y (A baixo, B direita, X
    /// esquerda, Y cima) ou o analogico direito, pelo eixo mais empurrado. Zero = nao atira.
    /// </summary>
    public static Vector2 Tiro
    {
        get
        {
            Gamepad c = Atual;

            if (c == null)
                return Vector2.zero;

            if (c.buttonNorth.isPressed) return Vector2.up;
            if (c.buttonSouth.isPressed) return Vector2.down;
            if (c.buttonWest.isPressed) return Vector2.left;
            if (c.buttonEast.isPressed) return Vector2.right;

            Vector2 analogico = c.rightStick.ReadValue();

            if (analogico.magnitude < 0.5f)
                return Vector2.zero;

            if (Mathf.Abs(analogico.x) > Mathf.Abs(analogico.y))
                return analogico.x > 0f ? Vector2.right : Vector2.left;

            return analogico.y > 0f ? Vector2.up : Vector2.down;
        }
    }

    // ---------------- botoes ----------------
    /// <summary>O botao desceu neste quadro.</summary>
    public static bool Apertou(BotaoDoControle botao)
    {
        Gamepad c = Atual;

        if (c == null)
            return false;

        switch (botao)
        {
            // Nos menus, o analogico esquerdo tambem vale como cruz.
            case BotaoDoControle.CruzCima: return c.dpad.up.wasPressedThisFrame || c.leftStick.up.wasPressedThisFrame;
            case BotaoDoControle.CruzBaixo: return c.dpad.down.wasPressedThisFrame || c.leftStick.down.wasPressedThisFrame;
            case BotaoDoControle.CruzEsquerda: return c.dpad.left.wasPressedThisFrame || c.leftStick.left.wasPressedThisFrame;
            case BotaoDoControle.CruzDireita: return c.dpad.right.wasPressedThisFrame || c.leftStick.right.wasPressedThisFrame;
        }

        UnityEngine.InputSystem.Controls.ButtonControl controle = Botao(c, botao);
        return controle != null && controle.wasPressedThisFrame;
    }

    /// <summary>O botao esta segurado agora (os desenhos das dicas afundam).</summary>
    public static bool Segurando(BotaoDoControle botao)
    {
        Gamepad c = Atual;

        if (c == null)
            return false;

        switch (botao)
        {
            case BotaoDoControle.Cruz: return c.dpad.ReadValue() != Vector2.zero;
            case BotaoDoControle.AnalogicoEsquerdo: return c.leftStick.ReadValue().magnitude > ZonaMorta;
            case BotaoDoControle.AnalogicoDireito: return c.rightStick.ReadValue().magnitude > ZonaMorta;
        }

        UnityEngine.InputSystem.Controls.ButtonControl controle = Botao(c, botao);
        return controle != null && controle.isPressed;
    }

    private static UnityEngine.InputSystem.Controls.ButtonControl Botao(Gamepad c, BotaoDoControle botao)
    {
        switch (botao)
        {
            case BotaoDoControle.A: return c.buttonSouth;
            case BotaoDoControle.B: return c.buttonEast;
            case BotaoDoControle.X: return c.buttonWest;
            case BotaoDoControle.Y: return c.buttonNorth;
            case BotaoDoControle.Start: return c.startButton;
            case BotaoDoControle.Select: return c.selectButton;
            case BotaoDoControle.LB: return c.leftShoulder;
            case BotaoDoControle.RB: return c.rightShoulder;
            case BotaoDoControle.LT: return c.leftTrigger;
            case BotaoDoControle.RT: return c.rightTrigger;
            case BotaoDoControle.CruzCima: return c.dpad.up;
            case BotaoDoControle.CruzBaixo: return c.dpad.down;
            case BotaoDoControle.CruzEsquerda: return c.dpad.left;
            case BotaoDoControle.CruzDireita: return c.dpad.right;
            case BotaoDoControle.AnalogicoEsquerdo: return c.leftStickButton;
            case BotaoDoControle.AnalogicoDireito: return c.rightStickButton;
            default: return null;
        }
    }

    /// <summary>Soltar bomba: LB ou LT, neste quadro.</summary>
    public static bool ApertouBomba => Apertou(BotaoDoControle.LB) || Apertou(BotaoDoControle.LT);

    /// <summary>Confirmar num menu: A ou Start.</summary>
    public static bool ApertouConfirmar => Apertou(BotaoDoControle.A) || Apertou(BotaoDoControle.Start);
}
