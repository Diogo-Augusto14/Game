"""Desenha os sprites das armas novas da 1.19.0 (Assets/Arte/Armas), a partir dos que ja existem.

  Bumerangue / BumerangueDePrata  desenhados aqui (um V de madeira; o de prata, recolorido)
  CajadoDaTempestade / CetroDoAbismo  o CetroSagrado recolorido (azul e roxo)
  LaminaReal                      a EspadaSagrada com a lamina prateada

Rode da raiz do repositorio: python3 Ferramentas/Armas/armas_novas.py
"""
import math
import os
from PIL import Image

PASTA = os.path.join('Assets', 'Arte', 'Armas')
CONTORNO = (24, 22, 30, 255)


def recolorir(origem, destino, troca):
    im = Image.open(os.path.join(PASTA, origem + '.png')).convert('RGBA')
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            c = px[x, y]
            if c[3] and c[:3] in troca:
                px[x, y] = troca[c[:3]] + (c[3],)
    im.save(os.path.join(PASTA, destino + '.png'))
    return im


def dist_segmento(p, a, b):
    ax, ay = a
    bx, by = b
    px_, py_ = p
    dx, dy = bx - ax, by - ay
    t = max(0.0, min(1.0, ((px_ - ax) * dx + (py_ - ay) * dy) / float(dx * dx + dy * dy)))
    return math.hypot(px_ - (ax + t * dx), py_ - (ay + t * dy))


def bumerangue(madeira, clara, escura):
    lado = 14
    cotovelo = (10.5, 6.5)
    pontas = [(2.5, 1.5), (2.5, 11.5)]
    corpo = set()
    for y in range(lado):
        for x in range(lado):
            c = (x + 0.5, y + 0.5)
            if min(dist_segmento(c, cotovelo, p) for p in pontas) <= 1.15:
                corpo.add((x, y))
    im = Image.new('RGBA', (lado, lado), (0, 0, 0, 0))
    px = im.load()
    for (x, y) in corpo:
        cima = (x, y - 1) in corpo
        baixo = (x, y + 1) in corpo
        px[x, y] = clara if not cima else escura if not baixo else madeira
    for y in range(lado):
        for x in range(lado):
            if (x, y) in corpo:
                continue
            if any((x + dx, y + dy) in corpo for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                px[x, y] = CONTORNO
    return im


def main():
    bumerangue((168, 112, 36, 255), (232, 176, 74, 255), (104, 62, 36, 255)).save(os.path.join(PASTA, 'Bumerangue.png'))
    bumerangue((176, 182, 198, 255), (250, 248, 236, 255), (96, 110, 150, 255)).save(os.path.join(PASTA, 'BumerangueDePrata.png'))

    recolorir('CetroSagrado', 'CajadoDaTempestade', {
        (168, 112, 36): (40, 62, 128), (232, 176, 74): (84, 132, 214), (255, 224, 112): (120, 232, 255)})
    recolorir('CetroSagrado', 'CetroDoAbismo', {
        (168, 112, 36): (58, 28, 86), (232, 176, 74): (118, 58, 168), (255, 224, 112): (204, 112, 255),
        (250, 248, 236): (240, 222, 255)})
    recolorir('EspadaSagrada', 'LaminaReal', {
        (255, 224, 112): (214, 236, 255), (232, 176, 74): (118, 168, 230)})


if __name__ == '__main__':
    main()
