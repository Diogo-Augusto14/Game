"""
Recolore uma imagem do Old Prison (azul) no tom do Porao (marrom), do mesmo jeito que as paredes do
Porao foram recoloridas: aprende a troca de cores comparando Assets/Arte/OldPrison/Paredes.png com
Assets/Arte/Resources/Temas/Porao/Paredes.png (pixel a pixel). As cores que aparecem nas paredes usam
a troca exata; as outras, uma aproximacao (minimos quadrados) feita com as mesmas cores.

Uso (da raiz do projeto):  python Ferramentas/Temas/recolorir_porao.py <entrada.png> <saida.png>
Ex.: python Ferramentas/Temas/recolorir_porao.py Assets/Arte/Resources/Masmorra/Prisao/PortaoDeCaveira.png \\
         Assets/Arte/Resources/Temas/Porao/PortaoDeCaveira.png
"""
import os
import sys
import numpy as np
from PIL import Image

RAIZ = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
AZUL = os.path.join(RAIZ, 'Assets', 'Arte', 'OldPrison', 'Paredes.png')
MARROM = os.path.join(RAIZ, 'Assets', 'Arte', 'Resources', 'Temas', 'Porao', 'Paredes.png')


def aprender():
    a = np.array(Image.open(AZUL).convert('RGBA')).reshape(-1, 4)
    b = np.array(Image.open(MARROM).convert('RGBA')).reshape(-1, 4)
    troca = {}
    for pa, pb in zip(a, b):
        if pa[3] > 0:
            troca[tuple(pa[:3])] = tuple(pb[:3])
    origem = np.array([list(k) + [1] for k in troca], dtype=float)
    destino = np.array(list(troca.values()), dtype=float)
    modelo, *_ = np.linalg.lstsq(origem, destino, rcond=None)
    return troca, modelo


def main():
    entrada, saida = sys.argv[1], sys.argv[2]
    troca, modelo = aprender()
    im = np.array(Image.open(entrada).convert('RGBA'))
    h, w, _ = im.shape
    for y in range(h):
        for x in range(w):
            r, g, b, a = im[y, x]
            if a == 0:
                continue
            cor = (int(r), int(g), int(b))
            nova = troca.get(cor)
            if nova is None:
                nova = np.clip(np.array([r, g, b, 1], dtype=float) @ modelo, 0, 255).round().astype(int)
            im[y, x, :3] = nova
    os.makedirs(os.path.dirname(saida), exist_ok=True)
    Image.fromarray(im).save(saida)
    print('salvo', saida)


if __name__ == '__main__':
    main()
