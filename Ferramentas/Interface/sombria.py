"""Faz a versao sombria da interface (Assets/Arte/Resources/InterfaceSombria) a partir da do pacote
Dragon Regalia (InterfaceDragao), que e colorida demais pro clima da prisao.

Cada pixel e classificado pela cor (dourado, vermelho/rosa, azul, roxo, verde ou cinza) e a claridade
dele passa por uma rampa de cores escuras: ferro velho no lugar do dourado, pedra escura no lugar do
azul, pano cor de sangue no lugar do rosa e brasa no lugar do verde. O desenho (contornos, cantos,
brilhos) fica igual; so a paleta muda. Os botoes tem uma rampa por estado (normal, escolhido, perigo,
apagado).

Rode da raiz do repositorio: python3 Ferramentas/Interface/sombria.py
"""
import colorsys
import os
import re
import uuid

from PIL import Image

ORIGEM = os.path.join('Assets', 'Arte', 'Resources', 'InterfaceDragao')
DESTINO = os.path.join('Assets', 'Arte', 'Resources', 'InterfaceSombria')

# As rampas: da sombra (claridade 0) ao brilho (claridade 1).
FERRO = [(12, 10, 12), (38, 32, 30), (74, 62, 52), (118, 100, 78), (160, 140, 108), (204, 188, 150)]
PEDRA = [(8, 7, 9), (20, 18, 21), (30, 27, 30), (42, 38, 40), (58, 53, 54)]
PEDRA_CLARA = [(10, 9, 11), (34, 30, 32), (62, 56, 56), (98, 90, 86), (140, 130, 120), (186, 176, 160)]
SANGUE = [(12, 4, 6), (40, 10, 12), (74, 18, 20), (110, 30, 28), (150, 56, 44), (190, 104, 84)]
BRASA = [(14, 6, 4), (50, 18, 10), (92, 34, 16), (138, 58, 24), (180, 96, 44), (222, 160, 96)]
PEDRA_MEDIA = [(8, 7, 9), (26, 23, 26), (46, 42, 44), (70, 64, 64), (100, 92, 88), (140, 130, 118)]
SOMBRA = [(6, 5, 7), (16, 14, 17), (26, 23, 27), (38, 34, 38), (52, 47, 50)]


def rampa(stops, t):
    t = max(0.0, min(1.0, t))
    x = t * (len(stops) - 1)
    i = min(int(x), len(stops) - 2)
    f = x - i
    a, b = stops[i], stops[i + 1]
    return tuple(int(round(a[k] + (b[k] - a[k]) * f)) for k in range(3))


def grupo(r, g, b):
    h, l, s = colorsys.rgb_to_hls(r / 255, g / 255, b / 255)
    h *= 360
    if s < 0.18 or l < 0.08:
        return 'cinza', l
    if 15 <= h < 70:
        return 'dourado', l
    if h >= 330 or h < 15:
        return 'vermelho', l
    if 70 <= h < 165:
        return 'verde', l
    if 165 <= h < 240:
        return 'azul', l
    return 'roxo', l


def recolorir(im, mapa, faixas=None):
    """mapa: grupo -> rampa. faixas: [(x0, x1, mapa)] troca o mapa numa faixa de colunas (os estados do botao)."""
    im = im.convert('RGBA')
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            m = mapa
            for (x0, x1, outro) in faixas or []:
                if x0 <= x < x1:
                    m = outro
            gr, l = grupo(r, g, b)
            # A claridade puxada um pouco pra cima: os contornos escuros continuam escuros, o resto aparece.
            cor = rampa(m.get(gr, m['cinza']), l ** 0.85)
            px[x, y] = cor + (a,)
    return im


MOLDURA = {'dourado': FERRO, 'vermelho': SANGUE, 'verde': BRASA, 'azul': PEDRA, 'roxo': SOMBRA, 'cinza': FERRO}
ENFEITE = {'dourado': FERRO, 'vermelho': SANGUE, 'verde': BRASA, 'azul': PEDRA_CLARA, 'roxo': SOMBRA, 'cinza': FERRO}
BOTAO_NORMAL = {'dourado': FERRO, 'vermelho': SANGUE, 'verde': BRASA, 'azul': PEDRA_MEDIA, 'roxo': SOMBRA, 'cinza': PEDRA_MEDIA}
BOTAO_ESCOLHIDO = {'dourado': BRASA, 'vermelho': BRASA, 'verde': BRASA, 'azul': FERRO, 'roxo': SOMBRA, 'cinza': FERRO}
BOTAO_PERIGO = {'dourado': SANGUE, 'vermelho': SANGUE, 'verde': SANGUE, 'azul': FERRO, 'roxo': SOMBRA, 'cinza': FERRO}
BOTAO_APAGADO = {'dourado': SOMBRA, 'vermelho': SOMBRA, 'verde': SOMBRA, 'azul': SOMBRA, 'roxo': SOMBRA, 'cinza': SOMBRA}
FAIXA_TITULO = {'dourado': FERRO, 'vermelho': SANGUE, 'verde': BRASA, 'azul': PEDRA_CLARA, 'roxo': SOMBRA, 'cinza': SANGUE}
FAIXA_FIM = {'dourado': FERRO, 'vermelho': SANGUE, 'verde': BRASA, 'azul': PEDRA_CLARA, 'roxo': SOMBRA, 'cinza': PEDRA_CLARA}

ARQUIVOS = {
    'MolduraGrande': (MOLDURA, None),
    'MolduraPequena': (MOLDURA, None),
    'BarraDourada': (MOLDURA, None),
    'FaixaRosa': (FAIXA_TITULO, None),
    'FaixaAzul': (FAIXA_FIM, None),
    'BotaoLosango': (BOTAO_NORMAL, [(104, 208, BOTAO_ESCOLHIDO), (208, 312, BOTAO_PERIGO), (312, 416, BOTAO_APAGADO)]),
    'Ponteiro': (ENFEITE, None),
    'Cursor': (ENFEITE, None),
    'SetaEsquerda': (ENFEITE, None),
    'SetaDireita': (ENFEITE, None),
    'BotaoSair': (ENFEITE, None),
    'BotaoOpcoes': (ENFEITE, None),
}


def main():
    os.makedirs(DESTINO, exist_ok=True)
    for nome, (mapa, faixas) in ARQUIVOS.items():
        im = recolorir(Image.open(os.path.join(ORIGEM, nome + '.png')), mapa, faixas)
        im.save(os.path.join(DESTINO, nome + '.png'))
        meta = os.path.join(DESTINO, nome + '.png.meta')
        if not os.path.exists(meta):
            base = open(os.path.join(ORIGEM, nome + '.png.meta'), encoding='utf-8').read()
            open(meta, 'w', encoding='utf-8').write(re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, base, count=1))
    pasta_meta = DESTINO + '.meta'
    if not os.path.exists(pasta_meta):
        open(pasta_meta, 'w').write('fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
                                    '  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % uuid.uuid4().hex)


if __name__ == '__main__':
    main()
