"""Monta as folhas da Solenne (heroina vista de cima, 1.22.3) a partir dos quadros do PixelLab.

Ela foi criada no PixelLab (personagem "Solenne (traço do Old Prison)", 64 x 64, visto de cima) e
animada com o skeleton-v3: breathing-idle, walking-8-frames, taking-punch, running-slide e
falling-back-death. Os quadros baixados ficam fora do repositorio, numa pasta assim:

  <pasta>/parado/<rumo>/<i>.png, andar/..., dano/..., esquiva/..., morte/east/...
  (rumo: south, north, east, west; quadros de 80 x 80)

O PixelLab fez os olhos azuis; aqui eles viram roxos (ametista), como o Diogo pediu.

O jogo usa quadros de 100 x 100 com o pe 0,35 unidade abaixo do meio (com 46 pixels por unidade, a
linha 66). Sai assim, em Assets/Arte/Resources/Personagens/Herois/Solenne:

  Idle, Walk, Dash, Hurt (de lado, olhando pra direita) e Death;
  Idle_Baixo, Idle_Cima, Idle_Esquerda e o mesmo pras outras (menos a morte).

Uso: python3 Ferramentas/Personagens/solenne.py <pasta dos quadros>
"""

import colorsys
import os
import sys
from PIL import Image

RAIZ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SAIDA = os.path.join(RAIZ, "Assets", "Arte", "Resources", "Personagens", "Herois", "Solenne")

QUADRO = 100
# O pe do PixelLab fica na linha 67 do quadro de 80; no jogo vai na 66. No meio, na horizontal.
DESLOCAMENTO = (10, -1)

ANIMACOES = {"Idle": "parado", "Walk": "andar", "Hurt": "dano", "Dash": "esquiva"}
DIRECOES = {"": "east", "_Baixo": "south", "_Cima": "north", "_Esquerda": "west"}


def pele(p):
    r, g, b, a = p
    return a and r > 170 and 90 < g < 235 and b < 170 and r > g > b


def azul(p):
    r, g, b, a = p

    if not a:
        return False

    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return s > 0.45 and 0.52 < h < 0.72 and v > 0.12


def ametista(px, x, y):
    r, g, b, a = px[x, y]
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    nr, ng, nb = colorsys.hsv_to_rgb(0.78, min(1, s * 1.05), min(1, v * 1.1 + 0.04))
    px[x, y] = (int(nr * 255), int(ng * 255), int(nb * 255), a)


def olhos_no_rosto(im):
    """De pe: os azuis bem saturados nas primeiras linhas de pele da cabeca (os olhos)."""
    px = im.load()
    caixa = im.getbbox()

    if not caixa:
        return 0

    topo, altura = caixa[1], caixa[3] - caixa[1]
    rosto = [(x, y) for y in range(topo, topo + altura // 2) for x in range(im.width) if pele(px[x, y])]

    if not rosto:
        return 0

    x0, x1 = min(x for x, _ in rosto), max(x for x, _ in rosto)
    y0, y1 = min(y for _, y in rosto), max(y for _, y in rosto)
    pintados = 0

    # Os olhos ficam nas primeiras linhas de pele (a testa sob a franja); abaixo e a gola da capa.
    for y in range(y0, min(y1, y0 + 7) + 1):
        for x in range(x0, x1 + 1):
            if azul(px[x, y]):
                ametista(px, x, y)
                pintados += 1

    return pintados


def olhos_soltos(im):
    """Agachada (a esquiva): as manchinhas azuis (ate 8 pixels) com pele em volta, que so podem ser olho."""
    px = im.load()
    vistos = set()

    for y in range(im.height):
        for x in range(im.width):
            if (x, y) in vistos or not azul(px[x, y]):
                continue

            mancha, fila = [], [(x, y)]
            vistos.add((x, y))

            while fila:
                a, b = fila.pop()
                mancha.append((a, b))

                for q in ((a + dx, b + dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
                    if 0 <= q[0] < im.width and 0 <= q[1] < im.height and q not in vistos and azul(px[q]):
                        vistos.add(q)
                        fila.append(q)

            if len(mancha) > 8:
                continue

            volta = {(a + dx, b + dy) for a, b in mancha for dx in (-1, 0, 1) for dy in (-1, 0, 1)} - set(mancha)

            if sum(1 for q in volta if 0 <= q[0] < im.width and 0 <= q[1] < im.height and pele(px[q])) >= 2:
                for a, b in mancha:
                    ametista(px, a, b)


def olhos_roxos(im):
    """O PixelLab fez os olhos azuis: viram ametista."""
    if olhos_no_rosto(im) == 0:
        olhos_soltos(im)

    return im


def quadros(pasta):
    lista = []
    i = 0

    while os.path.exists(os.path.join(pasta, f"{i}.png")):
        q = olhos_roxos(Image.open(os.path.join(pasta, f"{i}.png")).convert("RGBA"))
        novo = Image.new("RGBA", (QUADRO, QUADRO), (0, 0, 0, 0))
        dx, dy = DESLOCAMENTO
        novo.alpha_composite(q, (max(0, dx), max(0, dy)), (max(0, -dx), max(0, -dy)))
        lista.append(novo)
        i += 1

    return lista


def salvar(lista, nome):
    folha = Image.new("RGBA", (QUADRO * len(lista), QUADRO), (0, 0, 0, 0))

    for i, q in enumerate(lista):
        folha.alpha_composite(q, (i * QUADRO, 0))

    folha.save(os.path.join(SAIDA, nome + ".png"))
    print(nome, len(lista), "quadros")


def main():
    entrada = sys.argv[1]
    os.makedirs(SAIDA, exist_ok=True)

    for nome, pasta in ANIMACOES.items():
        for sufixo, direcao in DIRECOES.items():
            lista = quadros(os.path.join(entrada, pasta, direcao))

            if lista:
                salvar(lista, nome + sufixo)

    salvar(quadros(os.path.join(entrada, "morte", "east")), "Death")
    print("folhas em", SAIDA)


if __name__ == "__main__":
    main()
