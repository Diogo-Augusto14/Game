// Gerado por Ferramentas/OldPrison/importar.py a partir do pacote "EPIC RPG World Pack - Old Prison".
// Nao edite a mao: rode a ferramenta de novo.

/// <summary>
/// As tabelas e as regras do pacote Old Prison, tiradas dos arquivos do Tiled Map Editor que vem com ele.
///
/// Cantos: qual ladrilho vai em cada combinacao dos 4 cantos de um ladrilho (o indice e
/// cima-esquerda * 8 + cima-direita * 4 + baixo-direita * 2 + baixo-esquerda, cada um 1 quando e
/// "dentro": parede, chao ou poca). -1 = nada.
///
/// Regras: o automapping do pacote (ver <see cref="Automapa"/>). <see cref="RegrasQuePoem"/> poe as
/// faces das paredes e os encontros; <see cref="RegrasDeVariacao"/> troca alguns ladrilhos por variacoes
/// (tijolo rachado, musgo), cada uma com a sua chance. Servem pro chao e pras paredes: as duas folhas
/// tem os mesmos ladrilhos nos mesmos lugares.
/// </summary>
public static class DadosDoOldPrison
{
    public const int Lado = 32;

    public static readonly int[] CantosDoChao = { -1, 20, 16, 2, 48, -1, 32, 17, 52, 36, -1, 19, 66, 51, 49, 34 };
    public static readonly int[] CantosDasParedes = { -1, 20, 16, 2, 48, -1, 32, 17, 52, 36, -1, 19, 66, 51, 49, 34 };
    public static readonly int[] CantosDoSangue = { -1, 35, 33, 34, 125, -1, 94, 64, 129, 98, -1, 66, 158, 128, 126, 65 };

    /// <summary>O chao inteiro (os 4 cantos dentro) tem varios desenhos; cada um com o seu peso.</summary>
    public static readonly int[] ChaoInteiro = { 34, 256, 257, 258, 260, 261, 262, 264, 265, 266, 268, 269, 272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 283, 284, 285, 288, 289, 290, 291, 292, 293, 294, 295, 296, 297, 298, 299, 300, 301, 304, 305, 306, 308, 309, 310, 312, 313, 314, 316, 317, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 332, 333, 336, 337, 338, 339, 340, 341, 342, 343, 344, 345, 347, 348, 349 };
    public static readonly float[] PesoDoChaoInteiro = { 0.5f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.005f, 0.01f, 0.01f, 0.01f, 0.005f, 0.01f, 0.01f, 0.01f, 0.005f, 0.01f, 0.005f, 0.01f, 0.01f, 0.005f, 0.01f, 0.01f, 0.01f, 0.005f, 0.01f, 0.01f, 0.01f, 0.005f, 0.01f, 0.005f, 0.01f, 0.3f, 0.3f, 0.3f, 1f, 1f, 1f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.3f, 0.3f, 0.3f, 0.05f, 1f, 1f, 1f, 0.1f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.3f, 0.3f, 0.05f, 0.3f, 1f, 1f, 0.1f, 1f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f };

    public static readonly int[] SangueInteiro = { 65, 95, 96, 97, 127 };

    public const int LadrilhosDoAbismo = 16;
    public const int Enfeites = 94;

    public static readonly Regra[] RegrasQuePoem =
    {
        new Regra(1f, new int[] { 0, 0, 0, 66, 0, 1, 1, 0, 0, 2, 1, 0 }, new int[] { 0, 1, 82, 0, 2, 98 }),
        new Regra(1f, new int[] { 0, 0, 0, 49, 0, 1, 0, 48, 0, 2, 1, 0, 0, 3, 1, 0 }, new int[] { 0, 1, 65, 0, 2, 81, 0, 3, 97 }),
        new Regra(1f, new int[] { 0, 0, 0, 32, 0, 1, 0, 48, 0, 2, 1, 0, 0, 3, 1, 0 }, new int[] { 0, 2, 64, 0, 3, 80 }),
        new Regra(1f, new int[] { 0, 0, 0, 16, 0, 1, 0, 48, 0, 2, 1, 0, 0, 3, 1, 0 }, new int[] { 0, 2, 64, 0, 3, 80 }),
        new Regra(1f, new int[] { 0, 0, 0, 49, 0, 1, 0, 32, 0, 2, 0, 32 }, new int[] { 0, 1, 25, 0, 2, 41 }),
        new Regra(1f, new int[] { 0, 0, 0, 51, 0, 1, 0, 52, 0, 2, 1, 0, 0, 3, 1, 0 }, new int[] { 0, 1, 67, 0, 2, 83, 0, 3, 99 }),
        new Regra(1f, new int[] { 0, 0, 0, 36, 0, 1, 0, 52, 0, 2, 1, 0, 0, 3, 1, 0 }, new int[] { 0, 2, 68, 0, 3, 84 }),
        new Regra(1f, new int[] { 0, 0, 0, 20, 0, 1, 0, 52, 0, 2, 1, 0, 0, 3, 1, 0 }, new int[] { 0, 2, 68, 0, 3, 84 }),
        new Regra(1f, new int[] { 0, 0, 0, 51, 0, 1, 0, 36, 0, 2, 0, 36 }, new int[] { 0, 1, 26, 0, 2, 42 }),
        new Regra(1f, new int[] { 0, 0, 0, 66, 0, 2, 0, 2 }, new int[] { 0, 1, 82, 0, 2, 88 }),
        new Regra(1f, new int[] { 0, 0, 0, 66, 0, 1, 0, 2 }, new int[] { 0, 1, 87 }),
        new Regra(1f, new int[] { 0, 0, 0, 66, 0, 2, 0, 16 }, new int[] { 0, 1, 82, 0, 2, 39 }),
        new Regra(1f, new int[] { 0, 0, 0, 66, 0, 1, 0, 16, 0, 2, 0, 32 }, new int[] { 0, 1, 7, 0, 2, 23 }),
        new Regra(1f, new int[] { 0, 0, 0, 66, 0, 1, 0, 16, 0, 2, 0, 17 }, new int[] { 0, 1, 7, 0, 2, 55 }),
        new Regra(1f, new int[] { 0, 0, 0, 66, 0, 1, 0, 16, 0, 2, 0, 48 }, new int[] { 0, 1, 7, 0, 2, 90 }),
        new Regra(1f, new int[] { 0, 0, 0, 66, 0, 2, 0, 20 }, new int[] { 0, 1, 82, 0, 2, 37 }),
        new Regra(1f, new int[] { 0, 0, 0, 66, 0, 1, 0, 20, 0, 2, 0, 36 }, new int[] { 0, 1, 5, 0, 2, 21 }),
        new Regra(1f, new int[] { 0, 0, 0, 66, 0, 1, 0, 20, 0, 2, 0, 19 }, new int[] { 0, 1, 5, 0, 2, 56 }),
        new Regra(1f, new int[] { 0, 0, 0, 66, 0, 1, 0, 20, 0, 2, 0, 52 }, new int[] { 0, 1, 5, 0, 2, 89 }),
        new Regra(1f, new int[] { 0, 0, 0, 32, 0, 1, 0, 48, 0, 3, 0, 2 }, new int[] { 0, 2, 64, 0, 3, 71 }),
        new Regra(1f, new int[] { 0, 0, 0, 32, 0, 1, 0, 48, 0, 2, 0, 2 }, new int[] { 0, 2, 85 }),
        new Regra(1f, new int[] { 0, 0, 0, 49, 0, 1, 0, 48, 0, 3, 0, 2 }, new int[] { 0, 1, 65, 0, 2, 81, 0, 3, 30 }),
        new Regra(1f, new int[] { 0, 0, 0, 49, 0, 1, 0, 48, 0, 2, 0, 2 }, new int[] { 0, 1, 65, 0, 2, 69 }),
        new Regra(1f, new int[] { 0, 0, 0, 16, 0, 1, 0, 48, 0, 3, 0, 2 }, new int[] { 0, 2, 64, 0, 3, 71 }),
        new Regra(1f, new int[] { 0, 0, 0, 16, 0, 1, 0, 48, 0, 2, 0, 2 }, new int[] { 0, 2, 85 }),
        new Regra(1f, new int[] { 0, 0, 0, 32, 0, 1, 0, 48, 0, 3, 0, 16 }, new int[] { 0, 2, 64, 0, 3, 103 }),
        new Regra(1f, new int[] { 0, 0, 0, 32, 0, 1, 0, 48, 0, 2, 0, 16 }, new int[] { 0, 2, 119 }),
        new Regra(1f, new int[] { 0, 0, 0, 49, 0, 1, 0, 48, 0, 3, 0, 16 }, new int[] { 0, 1, 65, 0, 2, 81, 0, 3, 14 }),
        new Regra(1f, new int[] { 0, 0, 0, 49, 0, 1, 0, 48, 0, 2, 0, 16 }, new int[] { 0, 1, 65, 0, 2, 73 }),
        new Regra(1f, new int[] { 0, 0, 0, 16, 0, 1, 0, 48, 0, 3, 0, 16 }, new int[] { 0, 2, 64, 0, 3, 103 }),
        new Regra(1f, new int[] { 0, 0, 0, 16, 0, 1, 0, 48, 0, 2, 0, 16 }, new int[] { 0, 2, 119 }),
        new Regra(1f, new int[] { 0, 0, 0, 32, 0, 1, 0, 48, 0, 3, 0, 20 }, new int[] { 0, 2, 64, 0, 3, 107 }),
        new Regra(1f, new int[] { 0, 0, 0, 32, 0, 1, 0, 48, 0, 2, 0, 20, 0, 3, 0, 36 }, new int[] { 0, 2, 124, 0, 3, 93 }),
        new Regra(1f, new int[] { 0, 0, 0, 32, 0, 1, 0, 48, 0, 2, 0, 20, 0, 3, 0, 19 }, new int[] { 0, 2, 124, 0, 3, 77 }),
        new Regra(1f, new int[] { 0, 0, 0, 32, 0, 1, 0, 48, 0, 2, 0, 20, 0, 3, 0, 52 }, new int[] { 0, 2, 124, 0, 3, 61 }),
        new Regra(1f, new int[] { 0, 0, 0, 16, 0, 1, 0, 48, 0, 3, 0, 20 }, new int[] { 0, 2, 64, 0, 3, 107 }),
        new Regra(1f, new int[] { 0, 0, 0, 16, 0, 1, 0, 48, 0, 2, 0, 20, 0, 3, 0, 36 }, new int[] { 0, 2, 124, 0, 3, 93 }),
        new Regra(1f, new int[] { 0, 0, 0, 16, 0, 1, 0, 48, 0, 2, 0, 20, 0, 3, 0, 19 }, new int[] { 0, 2, 124, 0, 3, 77 }),
        new Regra(1f, new int[] { 0, 0, 0, 16, 0, 1, 0, 48, 0, 2, 0, 20, 0, 3, 0, 52 }, new int[] { 0, 2, 124, 0, 3, 61 }),
        new Regra(1f, new int[] { 0, 0, 0, 49, 0, 1, 0, 48, 0, 3, 0, 20 }, new int[] { 0, 1, 65, 0, 2, 81, 0, 3, 28 }),
        new Regra(1f, new int[] { 0, 0, 0, 49, 0, 1, 0, 48, 0, 2, 0, 20, 0, 3, 0, 36 }, new int[] { 0, 1, 65, 0, 2, 43, 0, 3, 79 }),
        new Regra(1f, new int[] { 0, 0, 0, 49, 0, 1, 0, 48, 0, 2, 0, 20, 0, 3, 0, 19 }, new int[] { 0, 1, 65, 0, 2, 43, 0, 3, 63 }),
        new Regra(1f, new int[] { 0, 0, 0, 49, 0, 1, 0, 48, 0, 2, 0, 20, 0, 3, 0, 52 }, new int[] { 0, 1, 65, 0, 2, 43, 0, 3, 47 }),
        new Regra(1f, new int[] { 0, 0, 0, 48, 0, 1, 0, 16, 0, 2, 0, 32 }, new int[] {  }),
        new Regra(1f, new int[] { 0, 0, 0, 48, 0, 1, 0, 16, 0, 2, 0, 17 }, new int[] {  }),
        new Regra(1f, new int[] { 0, 0, 0, 48, 0, 1, 0, 16, 0, 2, 0, 48 }, new int[] {  }),
        new Regra(1f, new int[] { 0, 0, 0, 36, 0, 1, 0, 52, 0, 3, 0, 2 }, new int[] { 0, 2, 68, 0, 3, 72 }),
        new Regra(1f, new int[] { 0, 0, 0, 36, 0, 1, 0, 52, 0, 2, 0, 2 }, new int[] { 0, 2, 86 }),
        new Regra(1f, new int[] { 0, 0, 0, 51, 0, 1, 0, 52, 0, 3, 0, 2 }, new int[] { 0, 1, 67, 0, 2, 83, 0, 3, 31 }),
        new Regra(1f, new int[] { 0, 0, 0, 51, 0, 1, 0, 52, 0, 2, 0, 2 }, new int[] { 0, 1, 67, 0, 2, 70 }),
        new Regra(1f, new int[] { 0, 0, 0, 20, 0, 1, 0, 52, 0, 3, 0, 2 }, new int[] { 0, 2, 68, 0, 3, 72 }),
        new Regra(1f, new int[] { 0, 0, 0, 20, 0, 1, 0, 52, 0, 2, 0, 2 }, new int[] { 0, 2, 86 }),
        new Regra(1f, new int[] { 0, 0, 0, 36, 0, 1, 0, 52, 0, 3, 0, 20 }, new int[] { 0, 2, 68, 0, 3, 101 }),
        new Regra(1f, new int[] { 0, 0, 0, 36, 0, 1, 0, 52, 0, 2, 0, 20 }, new int[] { 0, 2, 117 }),
        new Regra(1f, new int[] { 0, 0, 0, 51, 0, 1, 0, 52, 0, 3, 0, 20 }, new int[] { 0, 1, 67, 0, 2, 83, 0, 3, 12 }),
        new Regra(1f, new int[] { 0, 0, 0, 51, 0, 1, 0, 52, 0, 2, 0, 20 }, new int[] { 0, 1, 67, 0, 2, 57 }),
        new Regra(1f, new int[] { 0, 0, 0, 20, 0, 1, 0, 52, 0, 3, 0, 20 }, new int[] { 0, 2, 68, 0, 3, 101 }),
        new Regra(1f, new int[] { 0, 0, 0, 20, 0, 1, 0, 52, 0, 2, 0, 20 }, new int[] { 0, 2, 117 }),
        new Regra(1f, new int[] { 0, 0, 0, 52, 0, 1, 0, 20, 0, 2, 0, 36 }, new int[] {  }),
        new Regra(1f, new int[] { 0, 0, 0, 52, 0, 1, 0, 20, 0, 2, 0, 19 }, new int[] {  }),
        new Regra(1f, new int[] { 0, 0, 0, 52, 0, 1, 0, 20, 0, 2, 0, 52 }, new int[] {  }),
        new Regra(1f, new int[] { 0, 0, 0, 36, 0, 1, 0, 52, 0, 3, 0, 16 }, new int[] { 0, 2, 68, 0, 3, 29 }),
        new Regra(1f, new int[] { 0, 0, 0, 36, 0, 1, 0, 52, 0, 2, 0, 16, 0, 3, 0, 32 }, new int[] { 0, 2, 108, 0, 3, 78 }),
        new Regra(1f, new int[] { 0, 0, 0, 36, 0, 1, 0, 52, 0, 2, 0, 16, 0, 3, 0, 17 }, new int[] { 0, 2, 108, 0, 3, 62 }),
        new Regra(1f, new int[] { 0, 0, 0, 36, 0, 1, 0, 52, 0, 2, 0, 16, 0, 3, 0, 48 }, new int[] { 0, 2, 108, 0, 3, 46 }),
        new Regra(1f, new int[] { 0, 0, 0, 20, 0, 1, 0, 52, 0, 3, 0, 16 }, new int[] { 0, 2, 68, 0, 3, 29 }),
        new Regra(1f, new int[] { 0, 0, 0, 20, 0, 1, 0, 52, 0, 2, 0, 16, 0, 3, 0, 32 }, new int[] { 0, 2, 108, 0, 3, 78 }),
        new Regra(1f, new int[] { 0, 0, 0, 20, 0, 1, 0, 52, 0, 2, 0, 16, 0, 3, 0, 17 }, new int[] { 0, 2, 108, 0, 3, 62 }),
        new Regra(1f, new int[] { 0, 0, 0, 20, 0, 1, 0, 52, 0, 2, 0, 16, 0, 3, 0, 48 }, new int[] { 0, 2, 108, 0, 3, 46 }),
        new Regra(1f, new int[] { 0, 0, 0, 51, 0, 1, 0, 52, 0, 3, 0, 16 }, new int[] { 0, 1, 67, 0, 2, 83, 0, 3, 29 }),
        new Regra(1f, new int[] { 0, 0, 0, 51, 0, 1, 0, 52, 0, 2, 0, 16, 0, 3, 0, 32 }, new int[] { 0, 1, 67, 0, 2, 11, 0, 3, 78 }),
        new Regra(1f, new int[] { 0, 0, 0, 51, 0, 1, 0, 52, 0, 2, 0, 16, 0, 3, 0, 17 }, new int[] { 0, 1, 67, 0, 2, 11, 0, 3, 62 }),
        new Regra(1f, new int[] { 0, 0, 0, 51, 0, 1, 0, 52, 0, 2, 0, 16, 0, 3, 0, 48 }, new int[] { 0, 1, 67, 0, 2, 11, 0, 3, 46 }),
        new Regra(1f, new int[] { 0, 0, 0, 49, 0, 1, 0, 32, 0, 2, 0, 17 }, new int[] { 0, 1, 25, 0, 2, 55 }),
        new Regra(1f, new int[] { 0, 0, 0, 49, 0, 1, 0, 17 }, new int[] { 0, 1, 53 }),
        new Regra(1f, new int[] { 0, 0, 0, 49, 0, 1, 0, 32, 0, 2, 0, 48 }, new int[] { 0, 1, 25, 0, 2, 90 }),
        new Regra(1f, new int[] { 0, 0, 0, 51, 0, 1, 0, 36, 0, 2, 0, 19 }, new int[] { 0, 1, 26, 0, 2, 56 }),
        new Regra(1f, new int[] { 0, 0, 0, 51, 0, 1, 0, 19 }, new int[] { 0, 1, 54 }),
        new Regra(1f, new int[] { 0, 0, 0, 51, 0, 1, 0, 36, 0, 2, 0, 52 }, new int[] { 0, 1, 26, 0, 2, 89 }),
    };

    public static readonly Regra[] RegrasDeVariacao =
    {
        new Regra(0.4f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 144, 0, 1, 160, 0, 2, 176 }),
        new Regra(0.4f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 145, 0, 1, 161, 0, 2, 177 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 146, 0, 1, 162, 0, 2, 178 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 147, 0, 1, 163, 0, 2, 179 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 148, 0, 1, 164, 0, 2, 180 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 149, 0, 1, 165, 0, 2, 181 }),
        new Regra(0.15f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 151, 0, 1, 167, 0, 2, 183 }),
        new Regra(0.15f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 152, 0, 1, 168, 0, 2, 184 }),
        new Regra(0.15f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 153, 0, 1, 169, 0, 2, 185 }),
        new Regra(0.15f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 154, 0, 1, 170, 0, 2, 186 }),
        new Regra(0.15f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 155, 0, 1, 171, 0, 2, 187 }),
        new Regra(0.15f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 156, 0, 1, 172, 0, 2, 188 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 5, 0, 1, 0, 21 }, new int[] { 0, 0, 6, 0, 1, 22 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 7, 0, 1, 0, 23 }, new int[] { 0, 0, 8, 0, 1, 24 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 11 }, new int[] { 0, 0, 27 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 43 }, new int[] { 0, 0, 59 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 37 }, new int[] { 0, 0, 38 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 39 }, new int[] { 0, 0, 40 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 57 }, new int[] { 0, 0, 58 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 73 }, new int[] { 0, 0, 74 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 16 }, new int[] { 0, 0, 1 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 20 }, new int[] { 0, 0, 3 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 75 }, new int[] { 0, 0, 91 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 107 }, new int[] { 0, 0, 123 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 108 }, new int[] { 0, 0, 109 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 124 }, new int[] { 0, 0, 125 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 101 }, new int[] { 0, 0, 102 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 117 }, new int[] { 0, 0, 118 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 103 }, new int[] { 0, 0, 104 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 119 }, new int[] { 0, 0, 120 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 29 }, new int[] { 0, 0, 45 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 28 }, new int[] { 0, 0, 44 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 12 }, new int[] { 0, 0, 13 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 14 }, new int[] { 0, 0, 15 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 17 }, new int[] { 0, 0, 192 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 19 }, new int[] { 0, 0, 193 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 49 }, new int[] { 0, 0, 208 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 51 }, new int[] { 0, 0, 209 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 32 }, new int[] { 0, 0, 194 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 36 }, new int[] { 0, 0, 195 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 196 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 65, 0, 1, 0, 81, 0, 2, 0, 97 }, new int[] { 0, 0, 197, 0, 1, 213, 0, 2, 229 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 67, 0, 1, 0, 83, 0, 2, 0, 99 }, new int[] { 0, 0, 198, 0, 1, 214, 0, 2, 230 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 65, 0, 1, 0, 81, 0, 2, 0, 97 }, new int[] { 0, 0, 199, 0, 1, 215, 0, 2, 231 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 67, 0, 1, 0, 83, 0, 2, 0, 99 }, new int[] { 0, 0, 200, 0, 1, 216, 0, 2, 232 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 48, 0, 1, 0, 64, 0, 2, 0, 80 }, new int[] { 0, 0, 201, 0, 1, 217, 0, 2, 233 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 52, 0, 1, 0, 68, 0, 2, 0, 84 }, new int[] { 0, 0, 202, 0, 1, 218, 0, 2, 234 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 48, 0, 1, 0, 64, 0, 2, 0, 80 }, new int[] { 0, 0, 203, 0, 1, 219, 0, 2, 235 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 52, 0, 1, 0, 68, 0, 2, 0, 84 }, new int[] { 0, 0, 204, 0, 1, 220, 0, 2, 236 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 32 }, new int[] { 0, 0, 210 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 36 }, new int[] { 0, 0, 211 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 212 }),
    };
}
