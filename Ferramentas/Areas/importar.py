"""
Tira dos pacotes Grass Land 2.0 (demo), Ancient Ruins (demo) e Village (interiors) a arte das areas
seguras entre os mundos e a mobilia das salas especiais:

  Assets/Arte/Resources/Areas/Grama      a clareira (Grass Land): grama, arvore, arbustos,
                                         tufos de capim, pedras
  Assets/Arte/Resources/Areas/Ruinas     as ruinas (Ancient Ruins): grama, piso de pedra, arvores de
                                         outono, arbustos, plantas, pilares, pedras, muros, a casinha; e
                                         as animadas: fonte, altar, calice dos espiritos, particulas no ar,
                                         o mercador e a criatura da sorte
  Assets/Arte/Resources/Decoracao/Vila   a mobilia do Village: armarios de pocoes, estantes de livros,
                                         prateleiras, barris, caixotes, sacos, mesas, cadeiras, bancos,
                                         castiçais, lustre, estandartes, escudos, quadros, trofeus,
                                         armaduras, armas, livros, globo, plantas, tapetes, teias; os potes
                                         que quebram (com os cacos), o bau que abre, a vela e a tocha

As animadas ficam numa folha so, os quadros do mesmo tamanho lado a lado (o tamanho esta no codigo,
em AreaSegura.cs e Decoracao.cs). As pecas soltas sao recortadas rente ao desenho.

Uso (da raiz do projeto):  python Ferramentas/Areas/importar.py [pasta com os pacotes]   (padrao: Pacotes)
"""
import os
import re
import sys
from collections import deque

import numpy as np
from PIL import Image

PACOTES = sys.argv[1] if len(sys.argv) > 1 else 'Pacotes'
ARTE = os.path.join('Assets', 'Arte', 'Resources')
GL = os.path.join(PACOTES, 'Grass Land 2.0 (demo gratis)', 'Tilesets and props', 'Tilesets and props Demo.png')
AR = os.path.join(PACOTES, 'Ancient Ruins (demo gratis)')
VI = os.path.join(PACOTES, 'Village (interiors) V1.3', 'assets')
LADO = 32


def abrir(caminho):
    return Image.open(caminho).convert('RGBA')


def pasta(*partes):
    p = os.path.join(ARTE, *partes)
    os.makedirs(p, exist_ok=True)
    return p


def rente(im):
    caixa = im.getbbox()
    return im.crop(caixa) if caixa else None


def ladrilhos(im, x0, y0, x1, y1):
    """Os ladrilhos 32x32 inteiros (sem transparencia) do retangulo."""
    a = np.array(im)[:, :, 3]
    saida = []
    for y in range(y0, y1, LADO):
        for x in range(x0, x1, LADO):
            if a[y:y + LADO, x:x + LADO].min() == 255:
                saida.append(im.crop((x, y, x + LADO, y + LADO)))
    return saida


def tira(imagens, salvar):
    w, h = imagens[0].size
    folha = Image.new('RGBA', (w * len(imagens), h), (0, 0, 0, 0))
    for i, im in enumerate(imagens):
        folha.paste(im, (i * w, 0))
    folha.save(salvar)
    return len(imagens)


def pecas(im, caixa, junta=2, minimo=12):
    """As pecas soltas (desenhos separados por transparencia) dentro da caixa, de cima pra baixo e da
    esquerda pra direita. 'junta' une pedacos a ate tantos pixels (a sombra da arvore, folhas soltas)."""
    x0, y0, x1, y1 = caixa
    sub = im.crop(caixa)
    a = np.array(sub)[:, :, 3] > 0
    h, w = a.shape
    cheio = a.copy()
    for _ in range(junta):
        c = cheio.copy()
        c[1:, :] |= cheio[:-1, :]
        c[:-1, :] |= cheio[1:, :]
        c[:, 1:] |= cheio[:, :-1]
        c[:, :-1] |= cheio[:, 1:]
        cheio = c
    visto = np.zeros_like(cheio)
    caixas = []
    for y in range(h):
        for x in range(w):
            if not cheio[y, x] or visto[y, x]:
                continue
            fila = deque([(y, x)])
            visto[y, x] = True
            ys, xs = [y], [x]
            while fila:
                cy, cx = fila.popleft()
                for ny, nx in ((cy + 1, cx), (cy - 1, cx), (cy, cx + 1), (cy, cx - 1)):
                    if 0 <= ny < h and 0 <= nx < w and cheio[ny, nx] and not visto[ny, nx]:
                        visto[ny, nx] = True
                        fila.append((ny, nx))
                        ys.append(ny)
                        xs.append(nx)
            bx0, by0, bx1, by1 = min(xs), min(ys), max(xs) + 1, max(ys) + 1
            if (bx1 - bx0) * (by1 - by0) >= minimo:
                caixas.append((by0, bx0, bx1, by1))
    caixas.sort()
    saida = []
    for by0, bx0, bx1, by1 in caixas:
        p = rente(sub.crop((bx0, by0, bx1, by1)))
        if p is not None:
            saida.append(p)
    return saida


def grupo(nome, imagens):
    p = pasta(*nome.split('/'))
    for i, im in enumerate(imagens):
        im.save(os.path.join(p, f'{i:02d}.png'))
    assert imagens, nome
    return len(imagens)


def folha_animada(origem, quadro, salvar, colunas=None, so=None):
    """Copia uma folha (quadros lado a lado). Com 'colunas', arruma em grade (folhas compridas demais)."""
    im = abrir(origem)
    w, h = quadro
    n = im.width // w
    quadros = [im.crop((i * w, 0, i * w + w, h)) for i in range(n)]
    if so:
        quadros = quadros[so[0]:so[1]]
    colunas = colunas or len(quadros)
    linhas = (len(quadros) + colunas - 1) // colunas
    assert linhas * colunas == len(quadros), (origem, len(quadros), colunas)
    folha = Image.new('RGBA', (w * colunas, h * linhas), (0, 0, 0, 0))
    for i, q in enumerate(quadros):
        folha.paste(q, ((i % colunas) * w, (i // colunas) * h))
    folha.save(salvar)
    return len(quadros)


# ---------------------------------------------------------------- Grass Land (a clareira)

def grama():
    im = abrir(GL)
    p = pasta('Areas', 'Grama')
    n = tira(ladrilhos(im, 448, 32, 544, 96), os.path.join(p, 'Chao.png'))
    n += grupo('Areas/Grama/Arvore', [rente(im.crop((262, 336, 372, 520)))])
    n += grupo('Areas/Grama/Arbusto', pecas(im, (196, 488, 280, 600)))
    # Os tufos de capim (os fiapos soltos e os montes grudados ficam de fora).
    n += grupo('Areas/Grama/Tufo', [t for t in pecas(im, (384, 250, 740, 476), junta=1, minimo=60) if t.width <= 40])
    n += grupo('Areas/Grama/Pedra', pecas(im, (608, 470, 740, 545), minimo=60))
    return n


# ---------------------------------------------------------------- Ancient Ruins (as ruinas)

def ruinas():
    terreno = abrir(os.path.join(AR, 'Tilesets', 'Tileset-Terrain2.png'))
    p = pasta('Areas', 'Ruinas')
    n = tira(ladrilhos(terreno, 576, 160, 736, 256), os.path.join(p, 'Chao.png'))
    n += tira(ladrilhos(terreno, 736, 384, 896, 448), os.path.join(p, 'Piso.png'))

    atlas = abrir(os.path.join(AR, 'Props', 'Atlas-Props.png'))
    n += grupo('Areas/Ruinas/Arvore', pecas(atlas, (0, 0, 250, 180), junta=3, minimo=400))
    n += grupo('Areas/Ruinas/Arbusto', pecas(atlas, (250, 0, 330, 100), junta=1, minimo=200))
    n += grupo('Areas/Ruinas/Planta', pecas(atlas, (320, 30, 400, 100), junta=1))
    n += grupo('Areas/Ruinas/Pilar', pecas(atlas, (255, 105, 450, 195)))
    n += grupo('Areas/Ruinas/Pedra', pecas(atlas, (30, 190, 195, 290), junta=1))
    n += grupo('Areas/Ruinas/Muro', pecas(atlas, (195, 195, 355, 285)))

    a = os.path.join(AR, 'Props')
    rente(abrir(os.path.join(a, 'generic_estructure1-1-on grass.png'))).save(os.path.join(p, 'Estrutura.png'))
    n += folha_animada(os.path.join(a, 'shrine or fountain 160x128-on grass.png'), (160, 128), os.path.join(p, 'Fonte.png'))
    n += folha_animada(os.path.join(a, 'altar 224x288 - standing on grass.png'), (224, 288), os.path.join(p, 'Altar.png'), colunas=13)
    n += folha_animada(os.path.join(a, 'golden chalice 64x64-spirits.png'), (64, 64), os.path.join(p, 'Calice.png'))
    n += folha_animada(os.path.join(a, 'atmospheric nature particles.png'), (64, 64), os.path.join(p, 'Particulas.png'))
    c = os.path.join(AR, 'Characters')
    n += folha_animada(os.path.join(c, 'NPC Merchant-idle.png'), (110, 110), os.path.join(p, 'Mercador.png'))
    n += folha_animada(os.path.join(c, 'NPC Merchant-interacting-loop.png'), (110, 110), os.path.join(p, 'MercadorAcenando.png'))
    n += folha_animada(os.path.join(c, 'luck creature-idle.png'), (96, 96), os.path.join(p, 'Sorte.png'))
    n += folha_animada(os.path.join(c, 'luck creature-run.png'), (96, 96), os.path.join(p, 'SorteCorrendo.png'))
    return n


# ---------------------------------------------------------------- Village (a mobilia)

def numero(nome):
    m = re.findall(r'(\d+)', nome)
    return int(m[-1]) if m else -1


# (grupo, categoria do arquivo, numeros; None = todos)
VILA = [
    ('ArmarioDePocoes', 'cabinet', [2, 3, 5, 6, 17]),
    ('Estante', 'cabinet', [20, 21, 22, 24, 25, 27]),
    ('Prateleira', 'shelves', [0, 1, 2, 3, 4, 5]),
    ('Expositor', 'storage_countertop', [0, 1, 2, 3]),
    ('Barris', 'barrels', None),
    ('BarrisDeGrao', 'barrels3', [0, 1, 2, 3, 5, 6, 7]),
    ('Caixotes', 'crates', None),
    ('CaixotesDeSuprimento', 'crates_supply', None),
    ('Sacos', 'supply_bags', None),
    ('Potes', 'pots_pack', None),
    ('Mesa', 'big_rect_table', None),
    ('MesaRedonda', 'round_table', None),
    ('MesaComGarrafas', 'tables', None),
    ('Escrivaninha', 'office_desk', [5, 6]),
    ('Cadeira', 'table_chairs', None),
    ('Banco', 'bench', None),
    ('Castical', 'candlesticks', [6, 7, 8, 9]),
    ('CandelabroDePe', 'candlesticks', [10, 11]),
    ('Lustre', 'chandelier', None),
    ('Estandarte', 'banner', None),
    ('Escudos', 'decor_shields', [0, 1, 2, 3, 4, 5, 7]),
    ('Trofeus', 'decor_animals', None),
    ('Quadros', 'decors_paintings', None),
    ('Armadura', 'decors_armor', [8, 9]),
    ('Elmo', 'decors_armor', [0, 1, 2, 3, 4, 5, 6, 7]),
    ('BarrilDeArmas', 'decors_weapon', [1, 2, 3, 4, 5, 6, 7]),
    ('Cabide', 'decors_weapon2', [7, 8, 9]),
    ('Livros', 'decors_books', None),
    ('Globo', 'decors_globe', None),
    ('Papel', 'decors_paper', None),
    ('Planta', 'plants', None),
    ('Lareira', 'fireplace', None),
    ('Cesta', 'kitchen_props4', [0, 1, 2, 3, 4, 7, 8, 9]),
    ('Garrafas', 'kitchen_props', list(range(0, 17))),
    ('Teia', 'Slice', [0, 1, 2, 3, 5, 6, 7, 8]),
    ('Bau', 'chests', None),
]


def vila():
    d = os.path.join(VI, 'furniture_and_props_sprites')
    por_categoria = {}
    for f in os.listdir(d):
        cat = re.sub(r'[_ -]*\d*\.png$', '', f)
        por_categoria.setdefault(cat, []).append(f)
    n = 0
    for nome, cat, numeros in VILA:
        arquivos = sorted(por_categoria.get(cat, []), key=numero)
        escolhidos = [f for f in arquivos if numeros is None or numero(f) in numeros]
        imagens = [rente(abrir(os.path.join(d, f))) for f in escolhidos]
        if nome == 'Teia':
            imagens = [im for im in imagens if im.width <= 40 and im.height <= 40]
        n += grupo('Decoracao/Vila/' + nome, imagens)

    # Os tapetes: a fileira de tapetes do tileset (do lado do "Tileable").
    tiles = abrir(os.path.join(VI, 'Interiors_tilesets.png'))
    n += grupo('Decoracao/Vila/Tapete', pecas(tiles, (900, 1255, 1810, 1325), junta=1, minimo=400))

    # Os potes que quebram: 6 cores (linhas) de 6 formatos; cada cor tem os cacos dela (11 quadros 96x96).
    an = os.path.join(VI, 'animations')
    primeiros = abrir(os.path.join(an, 'breakable-pots-first-frame.png'))
    p = pasta('Decoracao', 'Vila', 'Pote')
    for f in os.listdir(p):
        os.remove(os.path.join(p, f))
    # As 6 faixas da folha (a coluna da esquerda e o numero da animacao).
    for cor, y0 in enumerate((33, 65, 97, 129, 161, 193)):
        potes = pecas_com_x(primeiros.crop((34, y0, 256, y0 + 28)))
        assert len(potes) == 6, (cor, len(potes))
        for forma, im in enumerate(potes):
            im.save(os.path.join(p, f'{cor + 1}{forma}.png'))
            n += 1
        n += folha_animada(os.path.join(an, f'breakable-pots-anim-{cor + 1}-V1-11frames.png'), (96, 96),
                           os.path.join(ARTE, 'Decoracao', 'Vila', f'Cacos{cor + 1}.png'))

    n += folha_animada(os.path.join(an, 'chests-chest1-opening.png'), (192, 192), os.path.join(ARTE, 'Decoracao', 'Vila', 'BauAbrindo.png'))
    n += folha_animada(os.path.join(an, 'candle_and_torch-candle_burning.png'), (64, 64), os.path.join(ARTE, 'Decoracao', 'Vila', 'Vela.png'))
    n += folha_animada(os.path.join(an, 'torch.png'), (64, 96), os.path.join(ARTE, 'Decoracao', 'Vila', 'Tocha.png'))
    return n


def pecas_com_x(faixa):
    """As pecas de uma faixa, da esquerda pra direita."""
    a = np.array(faixa)[:, :, 3] > 0
    colunas = a.any(axis=0)
    saida, x = [], 0
    while x < len(colunas):
        if not colunas[x]:
            x += 1
            continue
        x0 = x
        while x < len(colunas) and colunas[x]:
            x += 1
        p = rente(faixa.crop((x0, 0, x, faixa.height)))
        if p is not None and p.width * p.height >= 20:
            saida.append(p)
    return saida


def main():
    for p in (GL, AR, VI):
        if not os.path.exists(p):
            sys.exit(f'Nao achei {p}: descompacte os pacotes em {PACOTES}.')
    print('grama:', grama())
    print('ruinas:', ruinas())
    print('vila:', vila())


if __name__ == '__main__':
    main()
