using UnityEngine;

/// <summary>
/// Nomes das camadas e tags num lugar so, com resolucao tolerante.
///
/// O projeto tem camadas com nomes parecidos e escritos de formas diferentes
/// ("ground" e "Ground", "Wall" e "Walll"). Em vez de quebrar quando o nome nao bate
/// exatamente, aqui a busca tenta uma lista de apelidos e, se nao achar nenhum,
/// devolve um valor que faz o jogo continuar rodando (Default) com um aviso claro.
/// </summary>
public static class Camadas
{
    public const string Jogador = "Player";
    public const string Inimigo = "Inimigo";
    public const string Golpe = "Golpe";

    /// <summary>Apelidos aceitos pra cada conceito, em ordem de preferencia.</summary>
    private static readonly string[] ApelidosDeChao = { "Chao", "Ground", "ground", "Terreno" };
    private static readonly string[] ApelidosDeParede = { "Parede", "Wall", "Walll", "wall" };

    private static int mascaraChao = -1;
    private static int mascaraParede = -1;

    /// <summary>Mascara com todas as camadas que valem como chao.</summary>
    public static LayerMask MascaraDeChao
    {
        get
        {
            if (mascaraChao < 0)
                mascaraChao = MontarMascara(ApelidosDeChao, "chao");

            return mascaraChao;
        }
    }

    /// <summary>Mascara com todas as camadas que valem como parede.</summary>
    public static LayerMask MascaraDeParede
    {
        get
        {
            if (mascaraParede < 0)
                mascaraParede = MontarMascara(ApelidosDeParede, "parede");

            return mascaraParede;
        }
    }

    /// <summary>Chao + parede: o que o boneco nao atravessa.</summary>
    public static LayerMask MascaraDeSolido => MascaraDeChao | MascaraDeParede;

    /// <summary>Indice da camada, ou -1. Nao reclama se nao existir.</summary>
    public static int Indice(string nome)
    {
        return LayerMask.NameToLayer(nome);
    }

    /// <summary>
    /// Poe o objeto numa camada pelo nome. Se a camada nao existir, deixa como esta
    /// (o jogo continua funcionando; quem cria as camadas e o Configurador de Projeto).
    /// </summary>
    public static void Definir(GameObject alvo, string nomeDaCamada, bool incluirFilhos = false)
    {
        if (alvo == null)
            return;

        int indice = Indice(nomeDaCamada);

        if (indice < 0)
            return;

        alvo.layer = indice;

        if (!incluirFilhos)
            return;

        foreach (Transform filho in alvo.GetComponentsInChildren<Transform>(true))
            filho.gameObject.layer = indice;
    }

    /// <summary>Limpa o cache. O editor chama depois de criar camadas novas.</summary>
    public static void Esquecer()
    {
        mascaraChao = -1;
        mascaraParede = -1;
    }

    private static int MontarMascara(string[] apelidos, string oQue)
    {
        int mascara = 0;

        for (int i = 0; i < apelidos.Length; i++)
        {
            int indice = LayerMask.NameToLayer(apelidos[i]);

            if (indice >= 0)
                mascara |= 1 << indice;
        }

        if (mascara == 0)
        {
            Debug.LogWarning(
                $"[Camadas] nenhuma camada de {oQue} encontrada ({string.Join(", ", apelidos)}). " +
                "Usando a camada Default. Rode Tools > Jogo > Preparar projeto pra criar as camadas.");

            mascara = 1; // Default
        }

        return mascara;
    }
}
