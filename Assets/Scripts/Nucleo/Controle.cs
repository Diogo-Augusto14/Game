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
/// Controle de Xbox ou PlayStation nos menus (veio do jogo antigo; jogando, quem le o controle e o
/// <see cref="ControlesDoJogador"/>). Nos menus: cruz ou analogico escolhe, A ou Start confirma,
/// B volta, Select sai; LB musica e RB efeitos.
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

    /// <summary>Confirmar num menu: A ou Start.</summary>
    public static bool ApertouConfirmar => Apertou(BotaoDoControle.A) || Apertou(BotaoDoControle.Start);
}
