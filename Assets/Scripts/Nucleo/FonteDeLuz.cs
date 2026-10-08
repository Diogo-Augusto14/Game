using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Uma luz redonda do andar: abre um buraco claro na <see cref="Escuridao"/> (o heroi, as tochas, as velas,
/// os candelabros, os cristais, o portal...). Criada por <see cref="Iluminacao.Luz"/>.
/// </summary>
public class FonteDeLuz : MonoBehaviour
{
    /// <summary>As luzes acesas agora.</summary>
    public static readonly List<FonteDeLuz> Acesas = new List<FonteDeLuz>();

    public Color cor = Color.white;
    public float raio = 4f;
    public float intensidade = 1f;

    private void OnEnable() => Acesas.Add(this);

    private void OnDisable() => Acesas.Remove(this);
}
