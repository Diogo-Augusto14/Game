using System.Collections.Generic;
using UnityEngine;

/// <summary>As musicas do jogo: as das cavernas, as dos chefes, a do menu, da vitoria e do fim de jogo.</summary>
public enum TemaMusical
{
    Porao,
    Catacumbas,
    Cripta,
    Chefe,
    ChefeFinal,
    Menu,
    Vitoria,
    Abismo,
    Loja,
    FimDeJogo
}

/// <summary>
/// Toca musica em loop, com troca suave entre uma e outra: <c>Musica.Tocar(TemaMusical.Chefe)</c>.
///
/// Cada musica e um arquivo em <c>Resources/Musica/&lt;tema&gt;</c> (Porao.ogg, Chefe.ogg...),
/// composto pro jogo e gravado com instrumentos de orquestra. Trocar uma musica e so trocar o
/// arquivo com o mesmo nome. Veio do jogo antigo; sem o arquivo, fica sem musica.
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

    /// <summary>A musica da caverna pelo numero dela (a primeira, a segunda...), em roda.</summary>
    public static TemaMusical DaCaverna(int caverna)
    {
        switch ((Mathf.Max(1, caverna) - 1) % 4)
        {
            case 0: return TemaMusical.Porao;
            case 1: return TemaMusical.Catacumbas;
            case 2: return TemaMusical.Cripta;
            default: return TemaMusical.Abismo;
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
        // O volume dos efeitos (o do AudioListener) nao mexe na musica: ela tem o dela.
        f.ignoreListenerVolume = true;
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
            clip = Resources.Load<AudioClip>("Musica/" + tema);
            clips[tema] = clip;
        }

        return clip;
    }
}
