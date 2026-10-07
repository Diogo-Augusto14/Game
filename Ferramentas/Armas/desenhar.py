"""
Desenha as armas do jogo, os tiros e a bolsa de municao em pixel art (uma letra por pixel) e salva em Assets/Arte/Armas.
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
    'Y': (244, 204, 70),   # amarelo / ouro
    'y': (190, 140, 40),   # ouro escuro
    'O': (236, 128, 42),   # laranja
    'W': (250, 248, 236),  # branco
    'R': (204, 60, 52),    # vermelho
    'r': (128, 34, 40),    # vermelho escuro
    'C': (92, 214, 240),   # ciano (raio, cristal)
    'c': (40, 130, 200),   # azul
    'P': (190, 96, 230),   # roxo
    'p': (110, 50, 150),   # roxo escuro
    'V': (120, 230, 110),  # verde claro (magia)
    'v': (40, 140, 70),    # verde escuro
    'o': (232, 220, 190),  # osso
}
CONTORNO = (24, 22, 30)

DESENHOS = {
    # ---------- as armas achadas (olhando pra direita, cabo na esquerda) ----------
    # Varinha de Faiscas: graveto com uma estrela na ponta.
    'Varinha': [
        "...........W..",
        "..........WYW.",
        "bbbbbbbbbbYWYW",
        "BBBBBBBBB.WYW.",
        "...........W..",
    ],
    # Tomo das Brasas: livro vermelho de capa dourada, com chamas saindo.
    'Tomo': [
        "....O.....",
        "...OYO.O..",
        "..OYWYOYO.",
        ".rRRRRRRRW",
        ".rRRRYRRRW",
        ".rRRYOYRRW",
        ".rRRRYRRRW",
        ".rRRRRRRRW",
        ".rrrrrrrrr",
    ],
    # Besta de Repeticao: besta com o carregador de virotes em cima.
    'BestaDeRepeticao': [
        "...bbbbbb...b...",
        "...BBBBBB...bb..",
        "....DDDD.....b..",
        "bbbbbbbbbbbbbGgg",
        "BBBBBDDDDDDDDGGG",
        ".............b..",
        "............bb..",
        "............b...",
    ],
    # Cajado do Trovao: cajado comprido com um cristal azul na ponta.
    'Cajado': [
        "...................CC.",
        ".................CCWCC",
        "bbbbbbbbbbbbbbbbYcCWWC",
        "BBBBBBBBBBBBBBBBy.cCC.",
    ],
    # Machado de Arremesso: cabo curto e lamina larga (tambem e o desenho do tiro, girando).
    'Machado': [
        ".....gggg..",
        "....gGGGGg.",
        "....gGGGg..",
        "bbbbDDGg...",
        "BBBBDD.....",
    ],
    # ---------- os tiros do jogador ----------
    'Faisca': [
        "..W..",
        ".WYW.",
        "WYWYW",
        ".WYW.",
        "..W..",
    ],
    'Brasa': [
        ".OO.",
        "OYYO",
        "OYWO",
        ".RO.",
    ],
    'Raio': [
        "...CC.....C..",
        "CCCWWCCCCWWCC",
        ".....CC...CC.",
    ],
    'Virote': [
        "RR.........",
        "bbbbbbbbbgG",
        "RR.........",
    ],
    # ---------- os tiros dos inimigos (grandes e claros no meio, pra ver e desviar) ----------
    'FlechaDeOsso': [
        "RR..........",
        "oooooooooogg",
        "RR..........",
    ],
    'OrbeVerde': [
        "..vvv..",
        ".vVVVv.",
        "vVWWWVv",
        "vVWWWVv",
        "vVWWWVv",
        ".vVVVv.",
        "..vvv..",
    ],
    'OrbeRoxo': [
        "..ppp..",
        ".pPPPp.",
        "pPWWWPp",
        "pPWWWPp",
        "pPWWWPp",
        ".pPPPp.",
        "..ppp..",
    ],
    'Gota': [
        "..r..",
        ".rRr.",
        "rRRRr",
        "rRWRr",
        ".rRr.",
    ],
    # ---------- a bolsa de municao ----------
    'CaixaDeMunicao': [
        "...bbbb....",
        "..b....b...",
        ".BbbbbbbbB.",
        "BbbbbYYbbbB",
        "BbbbbyybbbB",
        "BbbbbbbbbbB",
        ".BBBBBBBBB.",
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
