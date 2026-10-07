"""
Desenha as armas do jogo em pixel art (uma letra por pixel) e salva em Assets/Arte/Armas.
Cada desenho ganha sozinho um contorno escuro de 1 pixel. Todas as armas olham pra direita, com o
cabo na esquerda (o jogo gira e espelha conforme a mira).

Uso, da raiz do projeto: python Ferramentas/Armas/desenhar.py   (precisa do Pillow)
Pra mudar um desenho: edite as letras aqui e rode de novo.
"""
import os
from PIL import Image

PASTA = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Arte', 'Armas')

CORES = {
    'D': (70, 74, 88),     # metal escuro
    'G': (120, 126, 142),  # metal
    'g': (176, 182, 198),  # metal claro
    'B': (104, 62, 36),    # madeira escura
    'b': (156, 98, 54),    # madeira clara
    'Y': (244, 204, 70),   # amarelo
    'O': (226, 128, 42),   # laranja
    'W': (246, 244, 236),  # branco
    'L': (98, 146, 66),    # verde
    'l': (62, 98, 44),     # verde escuro
    'R': (204, 60, 52),    # vermelho
}
CONTORNO = (24, 22, 30)

DESENHOS = {
    'Pistola': [
        ".gggggggggg",
        ".GGGGGGGGGG",
        ".DDDDDDDD..",
        ".DbbD......",
        ".bbb.......",
        ".bb........",
    ],
    'Escopeta': [
        "...........ggggggg",
        "bbbbbbDDDDDGGGGGGG",
        "BBBBbbDDDDDDDDDDDD",
        "...BBBDD.DDD......",
        "....BB............",
    ],
    'Metralhadora': [
        "....gggggggggg..",
        "..GGGGGGGGGGGGGG",
        "bbDDDDDDDDDDDD..",
        "bbDD.DDD........",
        ".b...DDD........",
        ".....DD.........",
        ".....DD.........",
    ],
    'Rifle': [
        "........DDDD..........",
        "....gggggggggggggggggg",
        "bbbbGGGGGGGGGGGGGGGGGG",
        "BBBBBBDDD.............",
        "..BBB..D..............",
    ],
    'Besta': [
        "..........b...",
        "..........bb..",
        "...........b..",
        "bbbbbbbbbbbGgg",
        "BBBBBDDDDDDGGG",
        "...........b..",
        "..........bb..",
        "..........b...",
    ],
    'Bala': [
        ".YYYY.",
        "YWWYYY",
        "YWYYYO",
        "YYYYYO",
        "YYYYOO",
        ".YOOO.",
    ],
    'Virote': [
        "RR.........",
        "bbbbbbbbbgG",
        "RR.........",
    ],
    'CaixaDeMunicao': [
        "..Y..Y..Y...",
        "..Y..Y..Y...",
        "llllllllllll",
        "lLLLLLLLLLLl",
        "lLWWWWWWWWLl",
        "lLLLLLLLLLLl",
        "llllllllllll",
    ],
}


def desenhar(linhas):
    largura, altura = max(len(l) for l in linhas) + 2, len(linhas) + 2
    cheio = {(x + 1, y + 1): CORES[c] for y, l in enumerate(linhas) for x, c in enumerate(l) if c in CORES}
    im = Image.new('RGBA', (largura, altura), (0, 0, 0, 0))
    for (x, y), cor in cheio.items():
        im.putpixel((x, y), cor + (255,))
    for y in range(altura):
        for x in range(largura):
            if (x, y) not in cheio and any((x + dx, y + dy) in cheio for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                im.putpixel((x, y), CONTORNO + (255,))
    return im


if __name__ == '__main__':
    os.makedirs(PASTA, exist_ok=True)
    for nome, linhas in DESENHOS.items():
        desenhar(linhas).save(os.path.join(PASTA, nome + '.png'))
        print(nome)
