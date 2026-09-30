using System.Collections.Generic;
using UnityEngine;

/// <summary>As musicas do jogo: uma por tema de andar, a do chefe, a do final e a do menu.</summary>
public enum TemaMusical
{
    Porao,
    Cavernas,
    Esgoto,
    Chefe,
    ChefeFinal,
    Menu,
    Vitoria
}

/// <summary>
/// Toca musica em loop, com troca suave entre uma e outra: <c>Musica.Tocar(TemaMusical.Chefe)</c>.
///
/// Cada musica e composta por codigo (<see cref="Compor"/>): uma sequencia de acordes,
/// baixo em colcheias, arpejo por cima e bateria. Muda a escala, o andamento e a
/// sequencia de tema pra tema; a "melodia" e sorteada com semente fixa, entao a musica e
/// sempre a mesma. Gerada na primeira vez que toca e guardada.
/// </summary>
public class Musica : MonoBehaviour
{
    private const string ChaveDoVolume = "volume-da-musica";
    private const float TempoDaTroca = 1.2f;

    private static Musica instancia;
    private static readonly Dictionary<TemaMusical, AudioClip> clips = new Dictionary<TemaMusical, AudioClip>();
    private static float volume = -1f;

    private AudioSource tocando;
    private AudioSource saindo;
    private TemaMusical? atual;
    private float troca = 1f;

    /// <summary>Volume da musica, de 0 a 1. Fica salvo entre partidas.</summary>
    public static float Volume
    {
        get
        {
            if (volume < 0f)
                volume = PlayerPrefs.GetFloat(ChaveDoVolume, 0.45f);

            return volume;
        }
        set
        {
            volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(ChaveDoVolume, volume);
        }
    }

    public static TemaMusical? Atual => instancia != null ? instancia.atual : null;

    /// <summary>A musica de cada mundo (o Andar passa o numero do mundo): os temas se repetem de tres em tres.</summary>
    public static TemaMusical DoAndar(int andar)
    {
        switch ((Mathf.Max(1, andar) - 1) % 3)
        {
            case 0: return TemaMusical.Porao;
            case 1: return TemaMusical.Cavernas;
            default: return TemaMusical.Esgoto;
        }
    }

    public static void Tocar(TemaMusical tema)
    {
        if (!Application.isPlaying)
            return;

        if (instancia == null)
        {
            GameObject obj = new GameObject("Musica");
            DontDestroyOnLoad(obj);
            instancia = obj.AddComponent<Musica>();
        }

        instancia.Trocar(tema);
    }

    public static void Parar()
    {
        if (instancia != null)
            instancia.Trocar(null);
    }

    private void Awake()
    {
        tocando = Fonte();
        saindo = Fonte();
    }

    private AudioSource Fonte()
    {
        AudioSource f = gameObject.AddComponent<AudioSource>();
        f.loop = true;
        f.playOnAwake = false;
        f.spatialBlend = 0f;
        f.ignoreListenerPause = true;
        f.volume = 0f;
        return f;
    }

    private void Trocar(TemaMusical? tema)
    {
        if (tema == atual)
            return;

        atual = tema;

        // A que tocava vai sumindo; a nova entra por cima.
        (tocando, saindo) = (saindo, tocando);
        tocando.Stop();

        if (tema.HasValue)
        {
            tocando.clip = Clip(tema.Value);
            tocando.volume = 0f;
            tocando.Play();
        }

        troca = 0f;
    }

    private void Update()
    {
        troca = Mathf.MoveTowards(troca, 1f, Time.unscaledDeltaTime / TempoDaTroca);
        tocando.volume = Volume * troca;
        saindo.volume = Volume * (1f - troca);

        if (troca >= 1f && saindo.isPlaying)
            saindo.Stop();
    }

    private static AudioClip Clip(TemaMusical tema)
    {
        if (!clips.TryGetValue(tema, out AudioClip clip) || clip == null)
        {
            clip = Compor(tema);
            clips[tema] = clip;
        }

        return clip;
    }

    // ================================================================ composicao
    private struct Receita
    {
        public int Tonica;          // nota MIDI da tonica (o baixo toca uma oitava abaixo)
        public int[] Escala;        // intervalos da escala a partir da tonica
        public int[] Acordes;       // graus da escala, um por compasso (0 = tonica)
        public float Andamento;     // batidas por minuto
        public int Bateria;         // 0 nada, 1 leve, 2 cheia
        public uint Semente;
        public float Brilho;        // largura do pulso do arpejo (0.5 = quadrada cheia)
        public bool ArpejoRapido;
    }

    private static readonly int[] Menor = { 0, 2, 3, 5, 7, 8, 10 };
    private static readonly int[] Dorica = { 0, 2, 3, 5, 7, 9, 10 };
    private static readonly int[] Frigia = { 0, 1, 3, 5, 7, 8, 10 };
    private static readonly int[] MenorHarmonica = { 0, 2, 3, 5, 7, 8, 11 };
    private static readonly int[] Maior = { 0, 2, 4, 5, 7, 9, 11 };

    private static Receita ReceitaDe(TemaMusical tema)
    {
        switch (tema)
        {
            case TemaMusical.Cavernas:
                return new Receita { Tonica = 62, Escala = Dorica, Acordes = new[] { 0, 0, 3, 3, 6, 6, 4, 4 }, Andamento = 92, Bateria = 1, Semente = 7, Brilho = 0.5f };
            case TemaMusical.Esgoto:
                return new Receita { Tonica = 64, Escala = Frigia, Acordes = new[] { 0, 1, 0, 1, 5, 4, 1, 0 }, Andamento = 112, Bateria = 2, Semente = 13, Brilho = 0.25f };
            case TemaMusical.Chefe:
                return new Receita { Tonica = 60, Escala = MenorHarmonica, Acordes = new[] { 0, 0, 5, 5, 3, 3, 4, 4 }, Andamento = 144, Bateria = 2, Semente = 21, Brilho = 0.25f, ArpejoRapido = true };
            case TemaMusical.ChefeFinal:
                return new Receita { Tonica = 57, Escala = MenorHarmonica, Acordes = new[] { 0, 5, 3, 4, 0, 5, 1, 4 }, Andamento = 156, Bateria = 2, Semente = 34, Brilho = 0.125f, ArpejoRapido = true };
            case TemaMusical.Menu:
                return new Receita { Tonica = 57, Escala = Menor, Acordes = new[] { 0, 0, 5, 5, 3, 3, 4, 4 }, Andamento = 76, Bateria = 0, Semente = 3, Brilho = 0.5f };
            case TemaMusical.Vitoria:
                return new Receita { Tonica = 60, Escala = Maior, Acordes = new[] { 0, 3, 4, 0, 5, 3, 4, 0 }, Andamento = 120, Bateria = 1, Semente = 55, Brilho = 0.25f };
            default: // Porao
                return new Receita { Tonica = 57, Escala = Menor, Acordes = new[] { 0, 0, 5, 5, 2, 2, 6, 4 }, Andamento = 100, Bateria = 1, Semente = 1, Brilho = 0.5f };
        }
    }

    private static AudioClip Compor(TemaMusical tema)
    {
        Receita r = ReceitaDe(tema);

        float batida = 60f / r.Andamento;
        int compassos = r.Acordes.Length;
        int total = Mathf.CeilToInt(compassos * 4 * batida * Sintetizador.Taxa);
        float[] a = new float[total];
        Sintetizador.Ruido sorteio = new Sintetizador.Ruido(r.Semente);

        for (int c = 0; c < compassos; c++)
        {
            int grau = r.Acordes[c];
            int[] acorde = { Nota(r, grau), Nota(r, grau + 2), Nota(r, grau + 4) };
            float inicioDoCompasso = c * 4 * batida;

            // Baixo: colcheias na tonica do acorde, com a quinta no fim do compasso.
            for (int i = 0; i < 8; i++)
            {
                int nota = (i == 6 ? acorde[2] : acorde[0]) - 24;
                float f = Sintetizador.Frequencia(nota);
                Sintetizador.Varredura(a, Indice(inicioDoCompasso + i * batida * 0.5f), batida * 0.45f, f, f,
                                       Sintetizador.Triangulo, 0.32f, 0.6f);
            }

            // Arpejo: sobe e desce pelo acorde, de vez em quando pula uma nota da escala.
            int passos = r.ArpejoRapido ? 16 : 8;
            float duracao = 4f * batida / passos;

            for (int i = 0; i < passos; i++)
            {
                int[] ordem = { 0, 1, 2, 1 };
                int nota = acorde[ordem[i % 4]] + 12;

                if (sorteio.Proximo() > 0.75f)
                    nota = Nota(r, grau + 1 + Mathf.Abs(Mathf.RoundToInt(sorteio.Proximo() * 3f))) + 12;

                if (sorteio.Proximo() > 0.9f)
                    continue; // respiro

                float f = Sintetizador.Frequencia(nota);
                float brilho = r.Brilho;
                Sintetizador.Varredura(a, Indice(inicioDoCompasso + i * duracao), duracao * 0.9f, f, f,
                                       fase => Sintetizador.Pulso(fase, brilho), 0.11f, 1.2f);
            }

            // Bateria: bumbo no 1 e no 3, caixa no 2 e no 4, chimbal nas colcheias.
            for (int b = 0; b < 4 && r.Bateria > 0; b++)
            {
                float t = inicioDoCompasso + b * batida;

                if (b % 2 == 0)
                    Sintetizador.Varredura(a, Indice(t), 0.16f, 140f, 45f, Sintetizador.Seno, 0.55f, 2f);
                else
                    Sintetizador.Chiado(a, Indice(t), 0.12f, 0.22f, (uint)(c * 4 + b + 1), 2.5f, 0.2f);

                if (r.Bateria > 1)
                {
                    Sintetizador.Chiado(a, Indice(t), 0.03f, 0.07f, (uint)(c * 8 + b * 2 + 100), 3f, 0f);
                    Sintetizador.Chiado(a, Indice(t + batida * 0.5f), 0.03f, 0.07f, (uint)(c * 8 + b * 2 + 101), 3f, 0f);
                }
            }
        }

        Sintetizador.Suavizar(a, 1.2f);
        return Sintetizador.Clip("Musica " + tema, a);
    }

    /// <summary>Nota MIDI do grau da escala (graus acima de 6 sobem de oitava).</summary>
    private static int Nota(Receita r, int grau)
    {
        int oitava = Mathf.FloorToInt(grau / (float)r.Escala.Length);
        int resto = grau - oitava * r.Escala.Length;
        return r.Tonica + oitava * 12 + r.Escala[resto];
    }

    private static int Indice(float segundos) => Mathf.RoundToInt(segundos * Sintetizador.Taxa);
}
