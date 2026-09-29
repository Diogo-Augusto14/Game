using UnityEngine;

/// <summary>
/// Gera som por codigo, no estilo chiptune: nenhum arquivo de audio no projeto. Os
/// efeitos (<see cref="Sons"/>) e as musicas (<see cref="Musica"/>) sao montados daqui,
/// amostra por amostra, e viram AudioClip na primeira vez que alguem pede.
///
/// Troque por arquivos de verdade quando tiver: quem toca so pede um AudioClip.
/// </summary>
public static class Sintetizador
{
    public const int Taxa = 22050;

    /// <summary>Formas de onda basicas. <paramref name="fase"/> em ciclos (0 a 1 e um ciclo).</summary>
    public static float Seno(float fase) => Mathf.Sin(fase * Mathf.PI * 2f);

    public static float Quadrada(float fase) => Pulso(fase, 0.5f);

    /// <summary>Onda quadrada com a parte de cima mais estreita: soa mais fina, mais "NES".</summary>
    public static float Pulso(float fase, float largura) => Mathf.Repeat(fase, 1f) < largura ? 1f : -1f;

    public static float Triangulo(float fase)
    {
        float f = Mathf.Repeat(fase, 1f);
        return f < 0.5f ? f * 4f - 1f : 3f - f * 4f;
    }

    public static float Serra(float fase) => Mathf.Repeat(fase, 1f) * 2f - 1f;

    /// <summary>Frequencia de uma nota MIDI (69 = La 440 Hz).</summary>
    public static float Frequencia(int nota) => 440f * Mathf.Pow(2f, (nota - 69) / 12f);

    /// <summary>Cria o AudioClip a partir das amostras (mono, valores de -1 a 1).</summary>
    public static AudioClip Clip(string nome, float[] amostras)
    {
        AudioClip clip = AudioClip.Create(nome, amostras.Length, 1, Taxa, false);
        clip.SetData(amostras, 0);
        return clip;
    }

    /// <summary>Ruido branco reproduzivel: a mesma semente da sempre o mesmo chiado.</summary>
    public class Ruido
    {
        private uint estado;

        public Ruido(uint semente) => estado = semente == 0 ? 1u : semente;

        public float Proximo()
        {
            // xorshift: rapido e bom o bastante pra chiado.
            estado ^= estado << 13;
            estado ^= estado >> 17;
            estado ^= estado << 5;
            return (estado & 0xFFFF) / 32767.5f - 1f;
        }
    }

    /// <summary>
    /// Um "bip" com a frequencia indo de <paramref name="de"/> a <paramref name="ate"/>,
    /// volume caindo ate zero. Base de quase todo efeito.
    /// </summary>
    public static void Varredura(float[] destino, int inicio, float duracao, float de, float ate,
                                 System.Func<float, float> onda, float volume, float curva = 1f)
    {
        int n = Mathf.Min(destino.Length - inicio, Mathf.RoundToInt(duracao * Taxa));
        float fase = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float f = Mathf.Lerp(de, ate, t);
            fase += f / Taxa;

            // Ataque de 2 ms pra nao estalar, depois decai.
            float ataque = Mathf.Clamp01(i / (Taxa * 0.002f));
            float envelope = ataque * Mathf.Pow(1f - t, curva);
            destino[inicio + i] += onda(fase) * envelope * volume;
        }
    }

    /// <summary>Chiado que decai (explosao, batida, caixa da bateria).</summary>
    public static void Chiado(float[] destino, int inicio, float duracao, float volume, uint semente,
                              float curva = 2f, float filtro = 0.5f)
    {
        int n = Mathf.Min(destino.Length - inicio, Mathf.RoundToInt(duracao * Taxa));
        Ruido ruido = new Ruido(semente);
        float anterior = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;

            // Filtro passa-baixa simples: filtro perto de 1 = chiado grave e abafado.
            anterior = Mathf.Lerp(ruido.Proximo(), anterior, filtro);
            destino[inicio + i] += anterior * Mathf.Pow(1f - t, curva) * volume;
        }
    }

    /// <summary>Segura os picos em -1..1 sem cortar seco (soa menos estourado).</summary>
    public static void Suavizar(float[] amostras, float ganho = 1f)
    {
        for (int i = 0; i < amostras.Length; i++)
            amostras[i] = (float)System.Math.Tanh(amostras[i] * ganho);
    }
}
