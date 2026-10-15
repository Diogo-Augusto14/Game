"""
Desenhos do ferreiro, do apostador e do bau mimico (rodar da raiz do repositorio):

    python3 Ferramentas/Cenario/desenhos.py

Escreve em Assets/Arte/Resources/Cenario/:
  MimicoDoBau.png  o bau de madeira da prisao (128 x 160 por quadro, como o original) virado monstro:
                   0 fechado (igual ao bau), 1 meio aberto, 2 escancarado (dentes, olhos, lingua);
  Bigorna.png      a bigorna do ferreiro, com uma brasa em cima;
  MesaDeDados.png  a mesinha do apostador com dois dados e moedas;
  Dados.png        as seis faces do dado (24 x 24 cada), pra tela das apostas.
"""
from PIL import Image, ImageDraw
import os

RAIZ = os.path.join("Assets", "Arte", "Resources")
SAIDA = os.path.join(RAIZ, "Cenario")
os.makedirs(SAIDA, exist_ok=True)

DENTE = (232, 226, 205, 255)
DENTE_SOMBRA = (160, 150, 130, 255)
OLHO = (255, 60, 40, 255)
OLHO_BRILHO = (255, 210, 120, 255)
LINGUA = (176, 52, 78, 255)
LINGUA_CLARA = (220, 92, 112, 255)


def escuro(c):
    r, g, b, a = c
    return a > 0 and r + g + b < 110 and b >= r


def interior(q):
    """O retangulo escuro de dentro do bau aberto (o maior bloco de pixels azul-escuros)."""
    w, h = q.size
    px = q.load()
    xs, ys = [], []
    for y in range(h):
        linha = [x for x in range(w) if escuro(px[x, y])]
        if len(linha) > 25:
            ys.append(y)
            xs.extend(linha)
    if not ys:
        return None
    return min(xs), min(ys), max(xs), max(ys)


def dentes(d, x0, x1, y, para_baixo):
    x = x0 + 1
    while x + 3 <= x1:
        if para_baixo:
            d.polygon([(x, y), (x + 3, y), (x + 1.5, y + 4)], fill=DENTE)
            d.point((x + 2, y + 1), fill=DENTE_SOMBRA)
        else:
            d.polygon([(x, y), (x + 3, y), (x + 1.5, y - 4)], fill=DENTE)
            d.point((x + 2, y - 1), fill=DENTE_SOMBRA)
        x += 5


def boca(q, lingua_longa):
    caixa = interior(q)
    if caixa is None:
        return q
    x0, y0, x1, y1 = caixa
    d = ImageDraw.Draw(q)
    dentes(d, x0, x1, y0, True)
    dentes(d, x0, x1, y1, False)
    meio = (x0 + x1) // 2
    cy = (y0 + y1) // 2
    for ox in (-9, 7):
        d.rectangle([meio + ox, cy - 1, meio + ox + 2, cy], fill=OLHO)
        d.point((meio + ox, cy - 1), fill=OLHO_BRILHO)
    # A lingua sai pela frente e pendura.
    comp = 9 if lingua_longa else 5
    d.rounded_rectangle([meio - 4, y1 - 2, meio + 4, y1 + comp], radius=3, fill=LINGUA)
    d.line([(meio, y1), (meio, y1 + comp - 2)], fill=LINGUA_CLARA)
    return q


def mimico():
    bau = Image.open(os.path.join(RAIZ, "Masmorra", "Prisao", "BauDeMadeira.png")).convert("RGBA")
    quadro = lambda i: bau.crop((i * 128, 0, i * 128 + 128, 160))
    # Fechado e igual ao bau de verdade: o que entrega e so a tremidinha (no jogo).
    fechado = quadro(0)
    folha = Image.new("RGBA", (128 * 3, 160))
    folha.paste(fechado, (0, 0))
    folha.paste(boca(quadro(4), False), (128, 0))
    folha.paste(boca(quadro(6), True), (256, 0))
    folha.save(os.path.join(SAIDA, "MimicoDoBau.png"))


def bigorna():
    im = Image.new("RGBA", (48, 32))
    d = ImageDraw.Draw(im)
    ferro, claro, sombra = (70, 72, 82, 255), (120, 124, 138, 255), (38, 38, 46, 255)
    # O pe, a cintura e o tampo com o chifre.
    d.rectangle([14, 26, 33, 31], fill=sombra)
    d.rectangle([16, 24, 31, 26], fill=ferro)
    d.rectangle([19, 17, 28, 24], fill=ferro)
    d.rectangle([19, 17, 20, 24], fill=sombra)
    d.polygon([(6, 10), (40, 10), (40, 17), (12, 17)], fill=ferro)
    d.polygon([(40, 10), (46, 11), (40, 14)], fill=ferro)
    d.line([(7, 10), (40, 10)], fill=claro)
    d.line([(12, 17), (40, 17)], fill=sombra)
    # A lamina em brasa em cima.
    d.rectangle([16, 7, 30, 9], fill=(255, 140, 40, 255))
    d.line([(17, 7), (29, 7)], fill=(255, 230, 140, 255))
    d.rectangle([31, 8, 35, 8], fill=(90, 60, 40, 255))
    im.save(os.path.join(SAIDA, "Bigorna.png"))


def face(n, lado=24):
    im = Image.new("RGBA", (lado, lado))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, 0, lado - 1, lado - 1], radius=4, fill=(222, 212, 190, 255))
    d.rounded_rectangle([0, lado - 4, lado - 1, lado - 1], radius=3, fill=(170, 158, 136, 255))
    d.rectangle([1, 1, lado - 2, lado - 5], fill=(232, 224, 204, 255))
    pos = {1: [(1, 1)], 2: [(0, 0), (2, 2)], 3: [(0, 0), (1, 1), (2, 2)], 4: [(0, 0), (2, 0), (0, 2), (2, 2)],
           5: [(0, 0), (2, 0), (1, 1), (0, 2), (2, 2)], 6: [(0, 0), (2, 0), (0, 1), (2, 1), (0, 2), (2, 2)]}[n]
    for cx, cy in pos:
        x, y = 4 + cx * 6, 3 + cy * 6
        d.rectangle([x, y, x + 3, y + 3], fill=(120, 20, 26, 255) if n == 1 else (40, 30, 34, 255))
    return im


def dados():
    folha = Image.new("RGBA", (24 * 6, 24))
    for n in range(1, 7):
        folha.paste(face(n), ((n - 1) * 24, 0))
    folha.save(os.path.join(SAIDA, "Dados.png"))


def mesa():
    im = Image.new("RGBA", (64, 40))
    d = ImageDraw.Draw(im)
    madeira, escura, pano = (92, 60, 40, 255), (54, 34, 24, 255), (70, 22, 30, 255)
    d.rectangle([6, 28, 9, 39], fill=escura)
    d.rectangle([54, 28, 57, 39], fill=escura)
    d.rectangle([2, 16, 61, 28], fill=madeira)
    d.rectangle([2, 26, 61, 28], fill=escura)
    d.rectangle([5, 17, 58, 24], fill=pano)
    # Dois dados e um montinho de moedas.
    for x, pontos in ((18, ((2, 2), (6, 2), (4, 4), (2, 6), (6, 6))), (28, ((2, 2), (4, 4), (6, 6)))):
        d.rectangle([x, 10, x + 8, 18], fill=(232, 224, 204, 255))
        d.line([(x, 18), (x + 8, 18)], fill=(170, 158, 136, 255))
        for px, py in pontos:
            d.point((x + px, 10 + py), fill=(40, 30, 34, 255))
    for i, (x, y) in enumerate(((42, 18), (45, 16), (43, 14))):
        d.ellipse([x, y, x + 6, y + 3], fill=(240, 196, 70, 255), outline=(150, 100, 30, 255))
    im.save(os.path.join(SAIDA, "MesaDeDados.png"))


mimico()
bigorna()
dados()
mesa()
print("ok")


# ---------------- os icones dos itens da 1.20.0 (32 x 32, como os do pacote: Icones/fb9001..9003)
CONTORNO = (24, 18, 22, 255)


def contornar(im):
    """Poe o contorno escuro de 1 px em volta do desenho, como nos icones do pacote."""
    px = im.load()
    w, h = im.size
    borda = []
    for y in range(h):
        for x in range(w):
            if px[x, y][3] == 0 and any(0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] > 0 and px[x + dx, y + dy] != CONTORNO
                                        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                borda.append((x, y))
    for x, y in borda:
        px[x, y] = CONTORNO
    return im


def lamina_larga():
    im = Image.new("RGBA", (32, 32))
    d = ImageDraw.Draw(im)
    # Uma lamina larga na diagonal, de baixo-esquerda pra cima-direita, com o cabo dourado.
    d.polygon([(8, 21), (22, 5), (27, 4), (26, 9), (11, 24)], fill=(196, 206, 220, 255))
    d.polygon([(9, 21), (23, 6), (26, 5), (10, 22)], fill=(240, 246, 255, 255))
    d.line([(11, 23), (25, 8)], fill=(130, 140, 160, 255))
    d.line([(6, 19), (13, 26)], fill=(214, 168, 60, 255), width=2)
    d.line([(4, 28), (9, 23)], fill=(120, 70, 40, 255), width=2)
    d.point((3, 29), fill=(214, 168, 60, 255))
    return contornar(im)


def corda_encantada():
    im = Image.new("RGBA", (32, 32))
    d = ImageDraw.Draw(im)
    # Uma corda enrolada (tres voltas) com um brilho azul de encanto.
    for i, r in enumerate((11, 8, 5)):
        d.ellipse([16 - r, 16 - r, 16 + r, 16 + r], outline=(186, 142, 84, 255), width=2)
        d.arc([16 - r, 16 - r, 16 + r, 16 + r], 200, 290, fill=(230, 196, 130, 255), width=1)
    d.line([(26, 20), (30, 27)], fill=(186, 142, 84, 255), width=2)
    for x, y in ((6, 6), (25, 7), (8, 26)):
        d.point((x, y), fill=(150, 220, 255, 255))
        d.point((x + 1, y), fill=(90, 160, 255, 255))
        d.point((x, y + 1), fill=(90, 160, 255, 255))
    return contornar(im)


def bobina_de_cobre():
    im = Image.new("RGBA", (32, 32))
    d = ImageDraw.Draw(im)
    # O nucleo de ferro com voltas de fio de cobre e uma faisca saindo por cima.
    d.rectangle([13, 8, 18, 28], fill=(90, 92, 104, 255))
    for y in range(10, 27, 3):
        d.line([(9, y), (22, y + 1)], fill=(200, 110, 50, 255), width=2)
        d.point((10, y), fill=(250, 170, 100, 255))
    d.rectangle([10, 27, 21, 29], fill=(60, 60, 70, 255))
    d.line([(16, 7), (13, 4), (18, 3), (15, 0)], fill=(255, 245, 140, 255))
    d.line([(17, 7), (21, 5)], fill=(160, 220, 255, 255))
    return contornar(im)


for numero, desenho in ((9001, lamina_larga), (9002, corda_encantada), (9003, bobina_de_cobre)):
    desenho().save(os.path.join(RAIZ, "Icones", f"fb{numero}.png"))
print("icones ok")
