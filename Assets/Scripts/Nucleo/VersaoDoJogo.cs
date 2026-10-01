using System.IO;
using UnityEngine;

/// <summary>
/// A versao instalada do jogo, lida do <c>versao.txt</c> que fica ao lado do ThePrettie.exe
/// (o Lancador\gerar-versao.ps1 grava ao montar o zip e o lancador atualiza ao baixar uma nova).
/// No editor, ou sem o arquivo, e "dev".
/// </summary>
public static class VersaoDoJogo
{
    private static string lida;

    /// <summary>"v1.1.2", ou "dev" fora de um build gerado pelo script.</summary>
    public static string Texto
    {
        get
        {
            if (lida != null)
                return lida;

            lida = "dev";

            try
            {
                // Application.dataPath e a pasta ThePrettie_Data; o versao.txt fica na pasta de cima.
                string pasta = Path.GetDirectoryName(Application.dataPath);
                string arquivo = pasta != null ? Path.Combine(pasta, "versao.txt") : null;

                if (arquivo != null && File.Exists(arquivo))
                {
                    string numero = File.ReadAllText(arquivo).Trim();

                    if (numero.Length > 0)
                        lida = numero.StartsWith("v") ? numero : "v" + numero;
                }
            }
            catch (System.Exception)
            {
                // Sem permissao de leitura ou caminho estranho: fica "dev", o jogo segue.
            }

            return lida;
        }
    }
}
