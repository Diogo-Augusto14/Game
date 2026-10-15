"""
Monta as folhas do Mineiro (o heroi principal, feito no PixelLab) no formato dos herois do jogo:
quadros de 100 x 100, o pe na linha 64, 40 px por unidade (Herois.Pixels).

    python3 Ferramentas/Personagens/mineiro.py <pasta com idle/ walk/ attack/ hurt/ death/>

Cada subpasta tem os quadros 0.png, 1.png... (48 ou 64 px, de lado, olhando pra direita). Antes de montar, a
armadura e clareada (o aco escuro sumia no escuro do jogo) e a capa fica um pouco mais viva.
"""
import colorsys
import glob
import os
import sys

from PIL import Image

SAIDA = os.path.join("Assets", "Arte", "Resources", "Personagens", "Herois", "Mineiro")
FOLHAS = {"idle": "Idle", "walk": "Walk", "attack": "Attack01", "hurt": "Hurt", "death": "Death"}


def clarear(im):
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            hh, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if v < 0.12:          # o contorno preto fica
                continue
            if s < 0.28:          # aco, capacete: mais claro e um pouco frio
                v = min(1.0, v * 1.45 + 0.06)
            elif 0.9 < hh or hh < 0.05:  # capa vermelha: mais viva
                s = min(1.0, s * 1.1)
                v = min(1.0, v * 1.25 + 0.03)
            else:                 # pele, couro, cabo: um pouco mais claro
                v = min(1.0, v * 1.2 + 0.03)
            r2, g2, b2 = colorsys.hsv_to_rgb(hh, s, v)
            px[x, y] = (int(r2 * 255), int(g2 * 255), int(b2 * 255), a)
    return im


def main(pasta):
    os.makedirs(SAIDA, exist_ok=True)
    for sub, nome in FOLHAS.items():
        quadros = sorted(glob.glob(os.path.join(pasta, sub, "*.png")), key=lambda p: int(os.path.basename(p)[:-4]))
        if not quadros:
            continue
        ims = [clarear(Image.open(q).convert("RGBA")) for q in quadros]
        # O pe (a linha mais baixa do primeiro quadro) vai na linha 64 do quadro de 100; o boneco no meio.
        pe = ims[0].getbbox()[3]
        folha = Image.new("RGBA", (100 * len(ims), 100))
        for k, im in enumerate(ims):
            folha.alpha_composite(im, (k * 100 + (100 - im.width) // 2, 64 - pe))
        folha.save(os.path.join(SAIDA, nome + ".png"))
        print(nome, len(quadros), "quadros")


if __name__ == "__main__":
    main(sys.argv[1])
