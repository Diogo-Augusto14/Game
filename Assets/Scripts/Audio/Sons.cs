using System.Collections.Generic;
using UnityEngine;

/// <summary>Os efeitos sonoros do jogo.</summary>
public enum Som
{
    Tiro,
    Acerto,
    MorteInimigo,
    DanoJogador,
    TiroInimigo,
    PortaFecha,
    PortaAbre,
    Explosao,
    Moeda,
    Coracao,
    Chave,
    Item,
    Compra,
    Negado,
    Pulo,
    Pancada,
    Queda,
    Segredo,
    Rugido,
    Vitoria,
    Menu,
    MenuConfirmar,
    MenuAbrir,
    MenuFechar,
    MenuNegado,
    Aviso,
    Dash,
    Corte,
    Destranca,
    BauAbre
}

/// <summary>
/// Toca efeito sonoro de qualquer lugar: <c>Sons.Tocar(Som.Moeda)</c>. Os sons sao
/// sintetizados na primeira vez (<see cref="Sintetizador"/>) e guardados. Se existir
/// <c>Resources/Sons/&lt;nome do Som&gt;</c>, toca o arquivo no lugar: os sons de menu vem do
/// Universal UI Soundpack (Nathan Gibson, CC BY 4.0).
///
/// Um objeto "Sons" com algumas AudioSources e criado sozinho e sobrevive a troca de cena.
/// O mesmo som tocado varias vezes no mesmo instante (dez lagrimas batendo juntas) toca
/// uma vez so, pra nao estourar o volume.
/// </summary>
public static class Sons
{
    private const int Canais = 12;
    private const float IntervaloMinimo = 0.035f;
    private const string ChaveDoVolume = "volume-dos-efeitos";

    private static readonly Dictionary<Som, AudioClip> clips = new Dictionary<Som, AudioClip>();
    private static readonly Dictionary<Som, float> ultimoToque = new Dictionary<Som, float>();
    private static AudioSource[] fontes;
    private static int proxima;
    private static float volume = -1f;

    /// <summary>Volume geral dos efeitos, de 0 a 1. Fica salvo entre partidas.</summary>
    public static float Volume
    {
        get
        {
            if (volume < 0f)
                volume = PlayerPrefs.GetFloat(ChaveDoVolume, 0.7f);

            return volume;
        }
        set
        {
            volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(ChaveDoVolume, volume);
        }
    }

    /// <param name="volumeRelativo">Multiplica o volume do som (1 = normal).</param>
    /// <param name="variacaoDeTom">Sorteia o tom em +-isto, pra repeticao nao cansar.</param>
    public static void Tocar(Som som, float volumeRelativo = 1f, float variacaoDeTom = 0.06f)
    {
        if (!Application.isPlaying || Volume <= 0f)
            return;

        float agora = Time.unscaledTime;

        if (ultimoToque.TryGetValue(som, out float antes) && agora - antes < IntervaloMinimo && agora >= antes)
            return;

        ultimoToque[som] = agora;
        GarantirFontes();

        AudioSource fonte = fontes[proxima];
        proxima = (proxima + 1) % fontes.Length;

        fonte.pitch = 1f + Random.Range(-variacaoDeTom, variacaoDeTom);
        fonte.PlayOneShot(Clip(som), Volume * volumeRelativo);
    }

    public static AudioClip Clip(Som som)
    {
        if (!clips.TryGetValue(som, out AudioClip clip) || clip == null)
        {
            clip = Resources.Load<AudioClip>("Sons/" + som);

            if (clip == null)
                clip = Gerar(som);

            clips[som] = clip;
        }

        return clip;
    }

    private static void GarantirFontes()
    {
        if (fontes != null && fontes[0] != null)
            return;

        GameObject obj = new GameObject("Sons");
        Object.DontDestroyOnLoad(obj);
        fontes = new AudioSource[Canais];

        for (int i = 0; i < Canais; i++)
        {
            fontes[i] = obj.AddComponent<AudioSource>();
            fontes[i].playOnAwake = false;
            fontes[i].spatialBlend = 0f;
            // Toca mesmo com o jogo pausado (menu, compra na tela de pausa).
            fontes[i].ignoreListenerPause = true;
        }
    }

    // ---------------- receitas ----------------
    private static AudioClip Gerar(Som som)
    {
        float[] a;

        switch (som)
        {
            case Som.Tiro:
                a = new float[Amostras(0.09f)];
                Sintetizador.Varredura(a, 0, 0.09f, 820f, 380f, f => Sintetizador.Pulso(f, 0.25f), 0.35f, 1.5f);
                break;

            case Som.Acerto:
                a = new float[Amostras(0.08f)];
                Sintetizador.Chiado(a, 0, 0.05f, 0.5f, 11, 2f, 0.3f);
                Sintetizador.Varredura(a, 0, 0.08f, 220f, 120f, Sintetizador.Seno, 0.6f);
                break;

            case Som.MorteInimigo:
                a = new float[Amostras(0.3f)];
                Sintetizador.Varredura(a, 0, 0.3f, 420f, 60f, Sintetizador.Quadrada, 0.35f, 1.2f);
                Sintetizador.Chiado(a, 0, 0.2f, 0.35f, 23, 2f, 0.6f);
                break;

            case Som.DanoJogador:
                a = new float[Amostras(0.25f)];
                Sintetizador.Varredura(a, 0, 0.25f, 300f, 110f, Sintetizador.Serra, 0.45f, 1f);
                Sintetizador.Varredura(a, 0, 0.25f, 305f, 112f, Sintetizador.Quadrada, 0.25f, 1f);
                break;

            case Som.TiroInimigo:
                a = new float[Amostras(0.07f)];
                Sintetizador.Varredura(a, 0, 0.07f, 480f, 700f, Sintetizador.Triangulo, 0.3f, 1.5f);
                break;

            case Som.PortaFecha:
                a = new float[Amostras(0.25f)];
                Sintetizador.Varredura(a, 0, 0.25f, 110f, 60f, Sintetizador.Quadrada, 0.4f, 2f);
                Sintetizador.Chiado(a, 0, 0.12f, 0.4f, 5, 2f, 0.8f);
                break;

            case Som.PortaAbre:
                a = new float[Amostras(0.25f)];
                Sintetizador.Varredura(a, 0, 0.12f, 330f, 330f, Sintetizador.Quadrada, 0.25f, 0.5f);
                Sintetizador.Varredura(a, Amostras(0.1f), 0.15f, 440f, 440f, Sintetizador.Quadrada, 0.25f, 0.7f);
                break;

            case Som.Explosao:
                a = new float[Amostras(0.7f)];
                Sintetizador.Chiado(a, 0, 0.7f, 0.9f, 77, 2.2f, 0.85f);
                Sintetizador.Varredura(a, 0, 0.4f, 90f, 30f, Sintetizador.Seno, 0.8f, 1.5f);
                break;

            case Som.Moeda:
                a = new float[Amostras(0.25f)];
                Sintetizador.Varredura(a, 0, 0.06f, 988f, 988f, Sintetizador.Quadrada, 0.25f, 0.3f);
                Sintetizador.Varredura(a, Amostras(0.06f), 0.19f, 1319f, 1319f, Sintetizador.Quadrada, 0.25f, 1.5f);
                break;

            case Som.Coracao:
                a = Arpejo(new[] { 72, 76, 79 }, 0.07f, Sintetizador.Triangulo, 0.45f);
                break;

            case Som.Chave:
                a = new float[Amostras(0.2f)];
                Sintetizador.Varredura(a, 0, 0.2f, 1500f, 1500f, Sintetizador.Seno, 0.3f, 2f);
                Sintetizador.Varredura(a, 0, 0.2f, 2250f, 2250f, Sintetizador.Seno, 0.2f, 3f);
                break;

            case Som.Item:
                a = Arpejo(new[] { 60, 64, 67, 72, 76, 79, 84 }, 0.07f, f => Sintetizador.Pulso(f, 0.25f), 0.3f);
                break;

            case Som.Compra:
                a = Arpejo(new[] { 76, 79, 84 }, 0.06f, f => Sintetizador.Pulso(f, 0.25f), 0.3f);
                break;

            case Som.Negado:
                a = new float[Amostras(0.18f)];
                Sintetizador.Varredura(a, 0, 0.18f, 140f, 120f, Sintetizador.Quadrada, 0.3f, 0.5f);
                break;

            case Som.Pulo:
                a = new float[Amostras(0.22f)];
                Sintetizador.Varredura(a, 0, 0.22f, 180f, 620f, Sintetizador.Quadrada, 0.3f, 1f);
                break;

            case Som.Pancada:
                a = new float[Amostras(0.5f)];
                Sintetizador.Varredura(a, 0, 0.5f, 80f, 35f, Sintetizador.Seno, 1f, 1.5f);
                Sintetizador.Chiado(a, 0, 0.3f, 0.5f, 99, 2f, 0.8f);
                break;

            case Som.Queda:
                a = new float[Amostras(0.6f)];
                Sintetizador.Varredura(a, 0, 0.6f, 800f, 90f, Sintetizador.Triangulo, 0.45f, 0.8f);
                break;

            case Som.Segredo:
                a = Arpejo(new[] { 79, 78, 75, 69, 68, 76, 80, 84 }, 0.09f, Sintetizador.Triangulo, 0.4f);
                break;

            case Som.Rugido:
                a = new float[Amostras(0.9f)];
                Sintetizador.Varredura(a, 0, 0.9f, 75f, 55f, Sintetizador.Serra, 0.5f, 0.8f);
                Sintetizador.Chiado(a, 0, 0.9f, 0.3f, 31, 1f, 0.9f);
                break;

            case Som.Vitoria:
                a = Arpejo(new[] { 60, 64, 67, 72, 67, 72, 76, 79, 84 }, 0.12f, f => Sintetizador.Pulso(f, 0.25f), 0.35f);
                break;

            case Som.MenuConfirmar:
                a = Arpejo(new[] { 72, 76, 79 }, 0.06f, f => Sintetizador.Pulso(f, 0.25f), 0.3f);
                break;

            case Som.MenuNegado:
                a = new float[Amostras(0.18f)];
                Sintetizador.Varredura(a, 0, 0.18f, 140f, 120f, Sintetizador.Quadrada, 0.3f, 0.5f);
                break;

            case Som.Dash:
                // Sopro curto que sobe: o vento da arrancada.
                a = new float[Amostras(0.18f)];
                Sintetizador.Chiado(a, 0, 0.18f, 0.55f, 23, 1.5f, 0.6f);
                Sintetizador.Varredura(a, 0, 0.14f, 180f, 420f, Sintetizador.Triangulo, 0.18f, 1.5f);
                break;

            case Som.Corte:
                // Lamina cortando o ar: chiado agudo e rapido que desce.
                a = new float[Amostras(0.12f)];
                Sintetizador.Chiado(a, 0, 0.12f, 0.5f, 41, 2.5f, 0.2f);
                Sintetizador.Varredura(a, 0, 0.1f, 1400f, 500f, Sintetizador.Serra, 0.12f, 2f);
                break;

            case Som.Aviso:
                a = Arpejo(new[] { 67, 72, 76, 79 }, 0.08f, Sintetizador.Triangulo, 0.4f);
                break;

            case Som.Destranca:
                // Clique do cadeado: dois estalos metalicos curtos, o segundo mais agudo.
                a = new float[Amostras(0.22f)];
                Sintetizador.Varredura(a, 0, 0.05f, 1800f, 1200f, Sintetizador.Quadrada, 0.3f, 2f);
                Sintetizador.Chiado(a, 0, 0.04f, 0.3f, 11, 3f, 0.9f);
                Sintetizador.Varredura(a, Amostras(0.09f), 0.08f, 2600f, 2200f, Sintetizador.Quadrada, 0.3f, 2f);
                Sintetizador.Chiado(a, Amostras(0.09f), 0.05f, 0.3f, 13, 3f, 0.9f);
                break;

            case Som.BauAbre:
            {
                // Rangido da tampa subindo e o brilho do tesouro.
                a = new float[Amostras(0.8f)];
                Sintetizador.Varredura(a, 0, 0.3f, 90f, 180f, Sintetizador.Serra, 0.3f, 0.6f);
                float[] brilho = Arpejo(new[] { 76, 79, 83, 88 }, 0.07f, Sintetizador.Triangulo, 0.35f);
                int depois = Amostras(0.28f);

                for (int i = 0; i < brilho.Length && depois + i < a.Length; i++)
                    a[depois + i] += brilho[i];

                break;
            }

            default: // Menu, MenuAbrir, MenuFechar
                a = new float[Amostras(0.08f)];
                Sintetizador.Varredura(a, 0, 0.08f, 660f, 660f, f => Sintetizador.Pulso(f, 0.25f), 0.25f, 1f);
                break;
        }

        Sintetizador.Suavizar(a);
        return Sintetizador.Clip("Som " + som, a);
    }

    private static int Amostras(float segundos) => Mathf.CeilToInt(segundos * Sintetizador.Taxa);

    /// <summary>Notas em sequencia, cada uma tocando ate o fim do arpejo e sumindo.</summary>
    private static float[] Arpejo(int[] notas, float passo, System.Func<float, float> onda, float volume)
    {
        float cauda = 0.2f;
        float[] a = new float[Amostras(notas.Length * passo + cauda)];

        for (int i = 0; i < notas.Length; i++)
        {
            float f = Sintetizador.Frequencia(notas[i]);
            Sintetizador.Varredura(a, Amostras(i * passo), passo + cauda, f, f, onda, volume, 2f);
        }

        return a;
    }
}
