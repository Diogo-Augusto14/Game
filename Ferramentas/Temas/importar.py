"""
Importa dos pacotes da serie EPIC RPG World (descompactados em Pacotes/, ver Pacotes/LEIAME.md no
repositorio privado ThePrettie-Pacotes) a arte dos mundos que nao sao o Old Prison:

  Cripta       (pacote Crypt)
    Assets/Arte/Resources/Temas/Cripta/Paredes.png   as paredes do pacote (wall-1)
    Assets/Arte/Resources/Temas/Cripta/Chao.png      o chao de pedra (ladrilhos inteiros do terreno)

  Profundezas  (pacote The Depths of the Mountain): salas flutuando sobre o vazio, sem paredes
    Assets/Arte/Resources/Temas/Profundezas/Chao.png    a plataforma de pedra marrom (Tileset 2)
    Assets/Arte/Resources/Temas/Profundezas/Abismo.png  o vazio escuro (o fundo do mapa de exemplo)

a decoracao (Assets/Arte/Resources/Decoracao: caixoes, estatuas, cristais, tochas, a porta grande de madeira e o
lancador de fogo) e escreve Assets/Scripts/Andar/DadosDosTemas.cs com as tabelas de cantos e as regras de variacao de
cada um. As regras que poem as faces sao as mesmas do Old Prison em todos (a ferramenta confere), e as
paredes do Crypt tem ate as mesmas variacoes.

Uso (da raiz do projeto):  python Ferramentas/Temas/importar.py [pasta com os pacotes]   (padrao: Pacotes)
Precisa do Pillow (pip install pillow).
"""
import os
import re
import sys
import importlib.util
from PIL import Image

RAIZ = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
PACOTES = sys.argv[1] if len(sys.argv) > 1 else os.path.join(RAIZ, 'Pacotes')
TEMAS = os.path.join(RAIZ, 'Assets', 'Arte', 'Resources', 'Temas')
DADOS = os.path.join(RAIZ, 'Assets', 'Scripts', 'Andar', 'DadosDosTemas.cs')
LADO = 32

OLD_PRISON = os.path.join(PACOTES, 'Old Prison V1.7.1', 'TiledMap Editor New')
CRYPT = os.path.join(PACOTES, 'Crypt V1.6')
DEPTHS = os.path.join(PACOTES, 'The Depths of the Mountain V1.5.1')

# A leitura das tabelas e das regras do Tiled e a mesma do Old Prison.
_spec = importlib.util.spec_from_file_location('op', os.path.join(RAIZ, 'Ferramentas', 'OldPrison', 'importar.py'))
op = importlib.util.module_from_spec(_spec)
_argv, sys.argv = sys.argv, ['importar.py', '.']
_spec.loader.exec_module(op)
sys.argv = _argv

# O chao da Cripta: os ladrilhos inteiros da "stone ground 1" (e, raros, os rachados com terra).
CHAO_DA_CRIPTA = [(517, 1), (518, 1), (568, 1), (468, 0.1), (519, 0.1),
                  (1068, 0.35), (1018, 0.08), (1067, 0.08), (1069, 0.08), (1118, 0.08)]

# O vazio das Profundezas: o ladrilho do fundo (camada "bg" do mapa de exemplo).
VAZIO_DAS_PROFUNDEZAS = 3


# A decoracao: Resources/Decoracao/<Grupo>/<Nome>/NN.png (cada pasta e um sorteio). (pacote, pasta, prefixo, numeros)
CP = 'Crypt V1.6/Props/atlas props - individual sprites/'
DPP = 'The Depths of the Mountain V1.5.1/Props/Static/Props-individual sprites/'
PECAS = {
    'Cripta/Caixao': [(CP, 'coffin - vertical - {}.png', range(1, 7))],
    'Cripta/CaixaoDeitado': [(CP, 'coffin - horizontal - {}.png', range(1, 6))],
    'Cripta/Estatua': [(CP, 'statue with support {}.png', range(1, 7))],
    'Cripta/Cruz': [(CP, 'cross with support {}.png', range(1, 6))],
    'Cripta/Candelabro': [(CP, 'candelabrum {}.png', range(1, 5))],
    'Cripta/Estandarte': [(CP, 'banner - bigger - {}.png', range(1, 19))],
    'Cripta/Vaso': [(CP, 'vase color scheme 1 - {}.png', range(1, 11)), (CP, 'vase color scheme 4 - {}.png', range(1, 12))],
    'Profundezas/Estatua': [(DPP, 'golden statues_{}.png', range(0, 16)), (DPP, 'statues-men_{}.png', range(0, 8)), (DPP, 'statues-women_{}.png', range(0, 8))],
    'Profundezas/Cristal': [(DPP, 'Crystals1_{}.png', range(0, 9)), (DPP, 'Crystals4_{}.png', range(0, 9))],
    'Profundezas/Candelabro': [(DPP, 'candelabrum_{}.png', range(0, 3))],
    'Profundezas/Ouro': [(DPP, 'piles of gold_{}.png', range(0, 8))],
    'Profundezas/Pote': [(DPP, 'pots1_{}.png', range(0, 16)), (DPP, 'pots5_{}.png', range(0, 14))],
    'Profundezas/Vazio': [(DPP, 'rock pillars coming from darkness-bg_{}.png', range(0, 4)), (DPP, 'rocks coming from darkness-bg_{}.png', range(0, 5)),
                          (DPP, 'statues-far from platforms-bg_{}.png', range(0, 10)), (DPP, 'pillars-bg_{}.png', range(0, 8))],
}
# Mais do Crypt: ossos, bancos e livros no chao.
PECAS.update({
    'Cripta/Ossos': [(CP, 'bones - color scheme 1 - {}.png', range(1, 46)), (CP, 'bones - color scheme 2 - {}.png', range(1, 25))],
    'Cripta/Banco': [(CP, 'bench horizontal{}.png', ['', ' 2'])],
    'Cripta/Livro': [(CP, 'book - support {}.png', range(1, 4))],
    'Profundezas/Espada': [(DPP, 'sword stuck in the ground{}.png', [''])],
})

# O Old Prison (mundos 1 e 2): as pecas sao escolhidas pelo numero na lista do atlas (os nomes do pacote,
# em ordem alfabetica) ou por um filtro no nome e no tamanho.
OPP = 'Old Prison V1.7.1/Props/atlas props - individual sprites/'


def faixa(*partes):
    r = []
    for a in partes:
        r += list(range(a[0], a[1] + 1)) if isinstance(a, tuple) else [a]
    return r


def barril_em_pe(nome, im, moedas):
    if not re.match(r'^barrel - (color scheme 2 - )?\d+( (gold|silver))?\.png$', nome):
        return False
    w, h = im.size
    tem = ' gold' in nome or ' silver' in nome
    return tem == moedas and w <= 33 and 36 <= h <= 45


PRISAO = {
    'Prisao/Barril': lambda n, im: barril_em_pe(n, im, False),
    'Prisao/BarrilDeMoedas': lambda n, im: barril_em_pe(n, im, True),
    'Prisao/Tonel': lambda n, im: re.match(r'^barrel - color scheme [12] - \d+\.png$', n) and im.size[0] >= 50 and im.size[1] > im.size[0],
    'Prisao/BarrilCaido': lambda n, im: ' - dropped' in n,
    'Prisao/Caixote': faixa((424, 431)),
    'Prisao/Saco': faixa((753, 764)),
    'Prisao/Balde': faixa((340, 351)),
    'Prisao/Candelabro': [353, 358, 361, 367, 370],
    'Prisao/Vela': faixa((375, 378)),
    'Prisao/Corrente': faixa((380, 389), (391, 397)),
    'Prisao/CorrenteDaParede': [379, 390, 398, 399, 400, 407, 408, 409, 410],
    'Prisao/CorrenteDoTeto': [405, 406, 411, 412],
    'Prisao/Acorrentado': faixa(281, 282, (284, 289), 326, 327, (329, 334)),
    'Prisao/Esqueleto': faixa((251, 256), 271, 272, 283, 290, (296, 301), 306, 316, 317, 328, 335),
    'Prisao/Ossos': faixa(250, (257, 270), (273, 280), (291, 295), (302, 305), (307, 315), (318, 325), (336, 339)),
    'Prisao/Pedras': faixa((677, 752)),
    'Prisao/Papel': faixa((485, 511)),
    'Prisao/Gaiola': faixa((765, 769)),
    'Prisao/GaiolaNoChao': [771, 774, 775, 777, 778, 780],
    'Prisao/Mesa': [785, 787, 809, 811],
    'Prisao/Cadeira': faixa((793, 808), (817, 832)),
    'Prisao/Caneca': faixa((781, 784)),
    'Prisao/DamaDeFerro': faixa((845, 850)),
    'Prisao/Guilhotina': [851, 853],
    'Prisao/Tronco': faixa((855, 858)),
    'Prisao/Retrato': faixa((568, 603)),
    'Prisao/Estandarte': faixa((606, 665)),
    'Prisao/BolaDeEspinhos': faixa((438, 457)),
    'Prisao/Ouro': [436, 437, 604, 605],
}

# As animadas (folhas inteiras): Resources/Decoracao/<nome>.png
ANIMADAS = {
    'Tocha': 'Crypt V1.6/Props/animated/torch_burning.png',
    'LancadorDeFogo': 'Crypt V1.6/Props/animated/face statue-fire projectile launcher-firing.png',
    'ChamaMagica': 'Old Prison V1.7.1/Props/magic flame.png',
    'Velas': 'Crypt V1.6/Props/animated/candle_burning.png',
}


def portas(base):
    """A porta grande de madeira das salas: tirada do mockup 'mockup new doors' do Crypt (a porta da
    animacao), com 3 celulas de largura; e a de lado, uma tabua em pe. Cada folha tem 8 quadros, do
    fechado (0) a quase toda afundada no chao."""
    from PIL import ImageEnhance
    g = Image.open(os.path.join(PACOTES, 'Crypt V1.6', 'Mockups', 'mockup new doors.gif'))
    g.seek(0)
    cru = g.convert('RGBA').crop((364, 102, 467, 232))
    px = cru.load()
    for y in range(cru.height):
        for x in range(cru.width):
            r, gg, b, a = px[x, y]
            if gg - r > 25:           # o fundo (chao e parede verde-agua) fica transparente
                px[x, y] = (0, 0, 0, 0)
    cru = cru.crop((3, 2, 99, 128))   # 96 x 126
    porta = Image.new('RGBA', (96, 90), (0, 0, 0, 0))
    porta.paste(cru.crop((0, 0, 96, 34)), (0, 0))      # a ponta de cima...
    porta.paste(cru.crop((0, 70, 96, 126)), (0, 34))   # ...e a de baixo, com a trava (mais baixa que a do mockup)
    porta = ImageEnhance.Brightness(porta).enhance(1.3)
    w, h = 16, 136
    lado = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    faixa = porta.crop((40, 6, 56, 90))
    for y in range(0, h, faixa.height):
        lado.paste(faixa, (0, y))
    for y in range(h):
        for x in range(w):
            r, gg, b, a = lado.getpixel((x, y))
            if a:
                k = 0.7 if x < 3 else (1.15 if x > w - 4 else 1.0)
                lado.putpixel((x, y), (min(255, int(r * k)), min(255, int(gg * k)), min(255, int(b * k)), a))
    lado.paste(porta.crop((40, 0, 56, 8)), (0, 0))

    def afundar(im, n=8):
        W, H = im.size
        folha = Image.new('RGBA', (W * n, H), (0, 0, 0, 0))
        for i in range(n):
            d = int(round(i * H / n))
            folha.paste(im.crop((0, 0, W, H - d)), (i * W, d))
        return folha

    afundar(porta).save(os.path.join(base, 'PortaGrande.png'))
    afundar(lado).save(os.path.join(base, 'PortaGrandeDeLado.png'))


def pecas():
    import glob
    base = os.path.join(os.path.dirname(TEMAS), 'Decoracao')
    total = 0
    for grupo, fontes in PECAS.items():
        pasta = os.path.join(base, grupo)
        os.makedirs(pasta, exist_ok=True)
        n = 0
        for pacote, molde, numeros in fontes:
            for i in numeros:
                origem = os.path.join(PACOTES, pacote, molde.format(i))
                if not os.path.exists(origem):
                    continue
                im = Image.open(origem).convert('RGBA')
                caixa = im.getbbox()
                if caixa:
                    im = im.crop(caixa)
                im.save(os.path.join(pasta, f'{n:02d}.png'))
                n += 1
        total += n
    lista_op = sorted(os.listdir(os.path.join(PACOTES, OPP)))
    for grupo, escolha in PRISAO.items():
        pasta = os.path.join(base, grupo)
        os.makedirs(pasta, exist_ok=True)
        n = 0
        for i, nome in enumerate(lista_op):
            im = Image.open(os.path.join(PACOTES, OPP, nome)).convert('RGBA')
            caixa = im.getbbox()
            if not caixa:
                continue
            im = im.crop(caixa)
            if not (escolha(nome, im) if callable(escolha) else i in escolha):
                continue
            im.save(os.path.join(pasta, f'{n:02d}.png'))
            n += 1
        assert n > 0, grupo
        total += n
    for nome, origem in ANIMADAS.items():
        Image.open(os.path.join(PACOTES, origem)).convert('RGBA').save(os.path.join(base, nome + '.png'))
    portas(base)
    return total


def recortar(folha, numero):
    colunas = folha.width // LADO
    x, y = (numero % colunas) * LADO, (numero // colunas) * LADO
    return folha.crop((x, y, x + LADO, y + LADO))


def tira(ladrilhos):
    saida = Image.new('RGBA', (LADO * len(ladrilhos), LADO), (0, 0, 0, 0))
    for i, im in enumerate(ladrilhos):
        saida.paste(im, (i * LADO, 0))
    return saida


def salvar(imagem, tema, nome):
    pasta = os.path.join(TEMAS, tema)
    os.makedirs(pasta, exist_ok=True)
    imagem.save(os.path.join(pasta, nome))


def lista(xs):
    return ', '.join(str(x) for x in xs)


def main():
    for p in (OLD_PRISON, CRYPT, DEPTHS):
        if not os.path.isdir(p):
            sys.exit(f'Nao achei {p}: descompacte os pacotes em {PACOTES}.')

    poe_op = op.ler_regras(os.path.join(OLD_PRISON, 'Rules', 'wall-2-rule1-places all tiles.tmx'))
    varia_op = op.ler_regras(os.path.join(OLD_PRISON, 'Rules', 'wall-2-rule2-applies variations.tmx'))
    cantos_op, _ = op.ler_cantos(os.path.join(OLD_PRISON, 'Tilesets', 'Tileset - wall 2.tsx'))

    # ---- Cripta: as paredes sao iguais as do Old Prison (tabela e regras); o chao, ladrilhos inteiros.
    tc = os.path.join(CRYPT, 'TiledMap Editor')
    cantos_c, _ = op.ler_cantos(os.path.join(tc, 'Tilesets', 'Tileset - wall 1.tsx'))
    assert cantos_c == cantos_op, 'as paredes do Crypt mudaram de tabela'
    assert op.ler_regras(os.path.join(tc, 'Rules', 'wall-1-rule1-places all tiles.tmx')) == poe_op
    assert op.ler_regras(os.path.join(tc, 'Rules', 'wall-1-rule2-applies variations.tmx')) == varia_op
    salvar(Image.open(os.path.join(CRYPT, 'Tilesets', 'wall-1.png')).convert('RGBA'), 'Cripta', 'Paredes.png')
    terreno = Image.open(os.path.join(CRYPT, 'Tilesets', 'Tileset-Terrain.png')).convert('RGBA')
    salvar(tira([recortar(terreno, n) for n, _ in CHAO_DA_CRIPTA]), 'Cripta', 'Chao.png')

    # ---- Profundezas: a plataforma 2 por chao (com a beirada caindo no vazio), sem paredes.
    td = os.path.join(DEPTHS, 'TiledMap Editor')
    cantos_p, cheios_p = op.ler_cantos(os.path.join(td, 'Tilesets', 'The depths-Tileset-Platform2.tsx'))
    assert op.ler_regras(os.path.join(td, 'Rules', 'plat-2-rule1-places all tiles.tmx')) == poe_op
    varia_p = op.ler_regras(os.path.join(td, 'Rules', 'plat-2-rule2-applies variations.tmx'))
    salvar(Image.open(os.path.join(DEPTHS, 'Tilesets', 'Tileset 2.png')).convert('RGBA'), 'Profundezas', 'Chao.png')
    fundo = Image.open(os.path.join(DEPTHS, 'Tilesets', 'Tileset-Terrain2.png')).convert('RGBA')
    salvar(tira([recortar(fundo, VAZIO_DAS_PROFUNDEZAS)]), 'Profundezas', 'Abismo.png')
    cor = recortar(fundo, VAZIO_DAS_PROFUNDEZAS).resize((1, 1), Image.BOX).getpixel((0, 0))

    codigo = f'''// Gerado por Ferramentas/Temas/importar.py a partir dos pacotes Crypt e The Depths of the Mountain.
// Nao edite a mao: rode a ferramenta de novo.
using UnityEngine;

/// <summary>
/// As tabelas e as variacoes dos mundos com arte propria (ver <see cref="EstiloDeLadrilhos"/>). As regras
/// que poem as faces sao as do Old Prison em todos (<see cref="DadosDoOldPrison.RegrasQuePoem"/>).
/// </summary>
public static class DadosDosTemas
{{
    // ---- Cripta (Crypt): as paredes tem as mesmas tabelas e variacoes do Old Prison.
    /// <summary>O chao da Cripta e so de ladrilhos inteiros (o mundo nao tem buracos).</summary>
    public static readonly int[] ChaoDaCripta = {{ {lista(range(len(CHAO_DA_CRIPTA)))} }};
    public static readonly float[] PesoDoChaoDaCripta = {{ {lista(f'{p:g}f' for _, p in CHAO_DA_CRIPTA)} }};

    // ---- Profundezas (The Depths of the Mountain): plataformas sobre o vazio.
    public static readonly int[] CantosDoChaoDasProfundezas = {{ {lista(cantos_p)} }};
    public static readonly int[] ChaoDasProfundezas = {{ {lista(t for t, _ in cheios_p)} }};
    public static readonly float[] PesoDoChaoDasProfundezas = {{ {lista(f'{p:g}f' for _, p in cheios_p)} }};

    /// <summary>A cor do vazio (o fundo da camera fica igual, pra nao ter emenda).</summary>
    public static readonly Color VazioDasProfundezas = new Color({cor[0] / 255:.4f}f, {cor[1] / 255:.4f}f, {cor[2] / 255:.4f}f);

{op.cs_regras('VariacaoDoChaoDasProfundezas', varia_p)}
}}
'''
    with open(DADOS, 'w', encoding='utf-8', newline='\n') as f:
        f.write(codigo)
    print(f'decoracao: {pecas()} pecas')
    print(f'ok: Cripta {len(CHAO_DA_CRIPTA)} chaos; Profundezas {len(cheios_p)} chaos, {len(varia_p)} variacoes, vazio {cor}')


if __name__ == '__main__':
    main()
