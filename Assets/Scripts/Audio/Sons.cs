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
    BauAbre,

    // Sons novos (o valor dos antigos nao muda): mortes por tipo de bicho, tiros por estilo e o que era mudo.
    MorteJogador,
    MorteChefe,
    MorteOssos,
    MorteGosma,
    MorteDemonio,
    MorteFera,
    Respingo,
    BombaAcesa,
    FlechaInimigo,
    TiroDeFogo,
    Canhao,
    Gosma,
    Magia,
    Mordida,
    Feitico,
    Espinhos,
    Alcapao,
    Cura,
    Arremesso
}

/// <summary>
/// Toca efeito sonoro de qualquer lugar: <c>Sons.Tocar(Som.Moeda)</c>. Cada som e o arquivo
/// <c>Resources/Sons/&lt;nome do Som&gt;</c>; variacoes com <c>_2</c>, <c>_3</c>... no nome
/// (Acerto_2) sao sorteadas a cada toque. Os de menu vem do Universal UI Soundpack (Nathan
/// Gibson, CC BY 4.0); os outros foram feitos pro jogo, alguns a partir do Freedoom. Veio do
/// jogo antigo (os menus usam); sem o arquivo, fica mudo.
///
/// O <see cref="Volume"/> dos efeitos tambem e o volume geral do AudioListener: vale pros sons que
/// as pecas do jogo tocam direto nas AudioSources delas. A <see cref="Musica"/> fica de fora.
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

    private const int MaximoDeVariacoes = 4;

    private static readonly Dictionary<Som, AudioClip[]> clips = new Dictionary<Som, AudioClip[]>();
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
                volume = PlayerPrefs.GetFloat(ChaveDoVolume, 0.8f);

            return volume;
        }
        set
        {
            volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(ChaveDoVolume, volume);
            AudioListener.volume = volume;
        }
    }

    // O volume salvo vale desde o comeco (os sons do jogo passam pelo AudioListener).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AplicarVolume() => AudioListener.volume = Volume;

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

        // Vinheta (melodia) desafinada soa errada: essas tocam sempre no tom certo.
        if (Musical(som))
            variacaoDeTom = 0f;

        AudioClip clip = Clip(som);

        if (clip == null)
            return;

        fonte.pitch = 1f + Random.Range(-variacaoDeTom, variacaoDeTom);
        fonte.PlayOneShot(clip, Volume * volumeRelativo);
    }

    /// <summary>O som (uma das variacoes, sorteada, quando tem mais de uma).</summary>
    public static AudioClip Clip(Som som)
    {
        if (!clips.TryGetValue(som, out AudioClip[] variacoes))
        {
            List<AudioClip> achados = new List<AudioClip>();
            AudioClip arquivo = Resources.Load<AudioClip>("Sons/" + som);

            if (arquivo != null)
            {
                achados.Add(arquivo);

                for (int i = 2; i <= MaximoDeVariacoes; i++)
                {
                    AudioClip outro = Resources.Load<AudioClip>("Sons/" + som + "_" + i);

                    if (outro == null)
                        break;

                    achados.Add(outro);
                }
            }
            else
            {
                // Sem o arquivo, fica mudo (o jogo antigo sintetizava; aqui os arquivos vem todos no projeto).
                achados.Add(null);
            }

            variacoes = achados.ToArray();
            clips[som] = variacoes;
        }

        return variacoes.Length == 1 ? variacoes[0] : variacoes[Random.Range(0, variacoes.Length)];
    }

    private static bool Musical(Som som)
    {
        switch (som)
        {
            case Som.Item:
            case Som.Vitoria:
            case Som.Segredo:
            case Som.Coracao:
            case Som.Cura:
            case Som.BauAbre:
                return true;
            default:
                return false;
        }
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
            // Toca mesmo com o jogo pausado (menu, compra na tela de pausa). O volume ja vem do
            // Volume no PlayOneShot: o do AudioListener nao conta de novo.
            fontes[i].ignoreListenerPause = true;
            fontes[i].ignoreListenerVolume = true;
        }
    }
}
