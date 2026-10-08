// Gerado por Ferramentas/Temas/importar.py a partir dos pacotes Crypt e The Depths of the Mountain.
// Nao edite a mao: rode a ferramenta de novo.
using UnityEngine;

/// <summary>
/// As tabelas e as variacoes dos mundos com arte propria (ver <see cref="EstiloDeLadrilhos"/>). As regras
/// que poem as faces sao as do Old Prison em todos (<see cref="DadosDoOldPrison.RegrasQuePoem"/>).
/// </summary>
public static class DadosDosTemas
{
    // ---- Cripta (Crypt): as paredes tem as mesmas tabelas e variacoes do Old Prison.
    /// <summary>O chao da Cripta e so de ladrilhos inteiros (o mundo nao tem buracos).</summary>
    public static readonly int[] ChaoDaCripta = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    public static readonly float[] PesoDoChaoDaCripta = { 1f, 1f, 1f, 0.1f, 0.1f, 0.35f, 0.08f, 0.08f, 0.08f, 0.08f };

    // ---- Profundezas (The Depths of the Mountain): plataformas sobre o vazio.
    public static readonly int[] CantosDoChaoDasProfundezas = { -1, 20, 16, 2, 48, -1, 32, 17, 52, 36, -1, 19, 66, 51, 49, 18 };
    public static readonly int[] ChaoDasProfundezas = { 18, 33, 34, 35, 50, 256, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 283, 284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 294, 295, 296, 297, 298, 299, 300, 301, 302, 303, 304, 305, 306, 307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 317, 318, 319 };
    public static readonly float[] PesoDoChaoDasProfundezas = { 0.3f, 0.3f, 1f, 0.6f, 1f, 1f, 1f, 1f, 1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 1f, 1f, 1f, 1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 1f, 1f, 1f, 1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 1f, 1f, 1f, 1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };

    /// <summary>A cor do vazio (o fundo da camera fica igual, pra nao ter emenda).</summary>
    public static readonly Color VazioDasProfundezas = new Color(0.0824f, 0.1412f, 0.1137f);

    public static readonly Regra[] VariacaoDoChaoDasProfundezas =
    {
        new Regra(0.15f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 144, 0, 1, 160, 0, 2, 176 }),
        new Regra(0.15f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 145, 0, 1, 161, 0, 2, 177 }),
        new Regra(0.15f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 146, 0, 1, 162, 0, 2, 178 }),
        new Regra(0.15f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 147, 0, 1, 163, 0, 2, 179 }),
        new Regra(0.15f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 148, 0, 1, 164, 0, 2, 180 }),
        new Regra(0.15f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 149, 0, 1, 165, 0, 2, 181 }),
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
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 205 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 206 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 49 }, new int[] { 0, 0, 224 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 51 }, new int[] { 0, 0, 225 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 32 }, new int[] { 0, 0, 210 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 36 }, new int[] { 0, 0, 211 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 212 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 20 }, new int[] { 0, 0, 221 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 20 }, new int[] { 0, 0, 222 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 32 }, new int[] { 0, 0, 226 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 36 }, new int[] { 0, 0, 227 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 228 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 16 }, new int[] { 0, 0, 237 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 16 }, new int[] { 0, 0, 238 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 336 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 337 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 338 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 339 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 340 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 341 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 342 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 343 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 344 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 345 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 352, 0, 1, 368, 0, 2, 384 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 353, 0, 1, 369, 0, 2, 385 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 354, 0, 1, 370, 0, 2, 386 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 355, 0, 1, 371, 0, 2, 387 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 356, 0, 1, 372, 0, 2, 388 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 357, 0, 1, 373, 0, 2, 389 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 358, 0, 1, 374, 0, 2, 390 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 359, 0, 1, 375, 0, 2, 391 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 360, 0, 1, 376, 0, 2, 392 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 361, 0, 1, 377, 0, 2, 393 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 400 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 401 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 402 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 403 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 2 }, new int[] { 0, 0, 404 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 416, 0, 1, 432, 0, 2, 448 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 417, 0, 1, 433, 0, 2, 449 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 418, 0, 1, 434, 0, 2, 450 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 419, 0, 1, 435, 0, 2, 451 }),
        new Regra(0.25f, new int[] { 0, 0, 0, 66, 0, 1, 0, 82, 0, 2, 0, 98 }, new int[] { 0, 0, 420, 0, 1, 436, 0, 2, 452 }),
    };
}
