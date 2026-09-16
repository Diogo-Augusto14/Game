/// <summary>
/// Os nomes de todos os clipes, em um lugar só. Usar as constantes daqui em vez de
/// digitar a string no meio do código evita o erro mais comum e mais chato de achar:
/// um clipe que "não toca" porque o nome tem um typo.
///
/// O Construtor de Animações (pasta Editor) gera a biblioteca usando exatamente
/// estes nomes — se mudar um aqui, mude lá também.
/// </summary>
public static class NomesDeAnimacao
{
    // ---------------- parado e andar ----------------
    public const string Parado          = "parado";
    public const string AndarInicio     = "andar_inicio";
    public const string Andar           = "andar";
    public const string CorrerInicio    = "correr_inicio";
    public const string Correr          = "correr";
    public const string CorrerParar     = "correr_parar";
    public const string CorrerVirar     = "correr_virar";

    // ---------------- agachar e escorregar ----------------
    public const string Agachar         = "agachar";
    public const string Escorregar      = "escorregar";

    // ---------------- pulo e queda ----------------
    public const string Pular           = "pular";
    public const string Cair            = "cair";
    public const string PuloDuplo       = "pulo_duplo";
    public const string PuloDuploFrente = "pulo_duplo_frente";
    public const string PousarRolando   = "pousar_rolando";

    // ---------------- parede ----------------
    public const string ParedeDeslizar  = "parede_deslizar";
    public const string ParedePular     = "parede_pular";

    // ---------------- dash e esquiva ----------------
    public const string DashAereo       = "dash_aereo";
    public const string EsquivaTras     = "esquiva_tras";

    // ---------------- beirada ----------------
    public const string BeiradaAgarrar  = "beirada_agarrar";
    public const string BeiradaParado   = "beirada_parado";
    public const string BeiradaSubir    = "beirada_subir";

    // ---------------- escada ----------------
    public const string EscadaEntrar        = "escada_entrar";
    public const string EscadaSubir         = "escada_subir";
    public const string EscadaSair          = "escada_sair";
    public const string EscadaEscorregar    = "escada_escorregar";
    public const string EscadaEscorregarFim = "escada_escorregar_fim";

    // ---------------- ataques ----------------
    public const string Ataque1          = "ataque1";
    public const string Ataque2          = "ataque2";
    public const string Ataque2a         = "ataque2a";
    public const string Ataque2b         = "ataque2b";
    public const string Ataque3          = "ataque3";
    public const string AtaqueAr1        = "ataque_ar1";
    public const string AtaqueAr2        = "ataque_ar2";
    public const string AtaqueDash1      = "ataque_dash1";
    public const string AtaqueDash2      = "ataque_dash2";
    public const string AtaqueMergulho   = "ataque_mergulho";
    public const string MergulhoQueda    = "ataque_mergulho_queda";

    // ---------------- reações ----------------
    public const string Dano       = "dano";
    public const string DanoForte  = "dano_forte";
    public const string Morrer     = "morrer";
    public const string Curar      = "curar";

    // ---------------- efeitos ----------------
    public const string FxImpacto1 = "fx_impacto1";
    public const string FxImpacto2 = "fx_impacto2";

    // ---------------- inimigo ----------------
    public const string InimigoParado      = "inimigo_parado";
    public const string InimigoAlerta      = "inimigo_alerta";
    public const string InimigoCorrer      = "inimigo_correr";
    public const string InimigoAtaque1     = "inimigo_ataque1";
    public const string InimigoAtaque2     = "inimigo_ataque2";
    public const string InimigoDano        = "inimigo_dano";
    public const string InimigoNoAr        = "inimigo_no_ar";
    public const string InimigoMorrer      = "inimigo_morrer";
}
