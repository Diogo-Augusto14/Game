"""
Importa do pacote "EPIC RPG World Pack - Old Prison" o que a caverna do jogo usa:

  Assets/Arte/OldPrison/Chao.png     a plataforma de pedra (o "wall-1" do pacote)
  Assets/Arte/OldPrison/Paredes.png  as paredes altas de tijolo (o "wall-2")
  Assets/Arte/OldPrison/Abismo.png   os ladrilhos do fundo roxo dos buracos (tirados do terreno)
  Assets/Arte/OldPrison/Sangue.png   as pocas de sangue (estilo 2, sem espinhos)
  Assets/Arte/OldPrison/Enfeites.png ossos, pedrinhas e papeis de 32 x 32, lado a lado

e escreve Assets/Scripts/Andar/DadosDoOldPrison.cs: as tabelas de cantos (qual ladrilho vai em
cada combinacao de cantos) e as regras de encaixe do Tiled (o "automapping" do pacote) ja
convertidas, pra o jogo montar as paredes sozinho, igual o Tiled monta.

Uso (da raiz do projeto):  python Ferramentas/OldPrison/importar.py "<pasta do pacote descompactado>"
Precisa do Pillow (pip install pillow).
"""
import os
import re
import sys
import glob
import xml.etree.ElementTree as ET
from PIL import Image

PACOTE = sys.argv[1] if len(sys.argv) > 1 else '.'
RAIZ = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
ARTE = os.path.join(RAIZ, 'Assets', 'Arte', 'OldPrison')
DADOS = os.path.join(RAIZ, 'Assets', 'Scripts', 'Andar', 'DadosDoOldPrison.cs')
TILED = os.path.join(PACOTE, 'TiledMap Editor New')
LADO = 32

# Celulas especiais do automapping (ordem do automap-tiles.tsx do Tiled).
VAZIO, IGNORA, NAOVAZIO, OUTRO = 0, 1, 2, 3
# Como o jogo chama cada tipo de celula de entrada (ver Automapa.cs).
TIPO_LADRILHO, TIPO_OUTRO, TIPO_VAZIO, TIPO_NAOVAZIO = 0, 1, 2, 3


# ---------------------------------------------------------------- leitura do Tiled
def ler_camadas(caminho):
    mapa = ET.parse(caminho).getroot()
    largura, altura = int(mapa.get('width')), int(mapa.get('height'))
    camadas = {}
    for camada in mapa.findall('layer'):
        valores = [int(v) for v in camada.find('data').text.replace('\n', '').split(',') if v.strip()]
        camadas[camada.get('name')] = [[valores[y * largura + x] for x in range(largura)] for y in range(altura)]
    return largura, altura, camadas, mapa


def regioes(largura, altura, camadas):
    """As regras de um arquivo de regras: grupos de celulas ocupadas que se tocam."""
    ocupada = [[any(c[y][x] for c in camadas.values()) for x in range(largura)] for y in range(altura)]
    vistas, grupos = set(), []
    for y in range(altura):
        for x in range(largura):
            if not ocupada[y][x] or (x, y) in vistas:
                continue
            pilha, grupo = [(x, y)], []
            vistas.add((x, y))
            while pilha:
                a, b = pilha.pop()
                grupo.append((a, b))
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    n = (a + dx, b + dy)
                    if 0 <= n[0] < largura and 0 <= n[1] < altura and ocupada[n[1]][n[0]] and n not in vistas:
                        vistas.add(n)
                        pilha.append(n)
            grupos.append(grupo)
    return grupos


def ler_regras(caminho):
    """Lista de regras na ordem em que o Tiled aplica (de cima pra baixo, da esquerda pra direita)."""
    largura, altura, camadas, mapa = ler_camadas(caminho)
    primeiros = sorted((int(t.get('firstgid')), t.get('source')) for t in mapa.findall('tileset'))
    automap = next((f for f, s in primeiros if s.startswith(':')), None)
    tileset = next(f for f, s in primeiros if not s.startswith(':'))
    entrada = camadas[next(n for n in camadas if n.startswith('input'))]
    saida = camadas[next(n for n in camadas if n.startswith('output'))]

    opcoes = []
    for grupo in mapa.findall('objectgroup'):
        if grupo.get('name') != 'rule_options':
            continue
        ox, oy = float(grupo.get('offsetx', 0)), float(grupo.get('offsety', 0))
        for o in grupo.findall('object'):
            props = {p.get('name'): p.get('value') for p in o.findall('properties/property')}
            x, y, w, h = (float(o.get(k)) for k in ('x', 'y', 'width', 'height'))
            opcoes.append(((x + ox) / LADO, (y + oy) / LADO, (x + ox + w) / LADO, (y + oy + h) / LADO,
                           float(props.get('Probability', 1)), props.get('Disabled') == 'true'))

    regras = []
    for celulas in regioes(largura, altura, {'e': entrada, 's': saida}):
        x0, y0 = min(p[0] for p in celulas), min(p[1] for p in celulas)
        e, s = [], []
        for x, y in celulas:
            g = entrada[y][x]
            if g:
                if automap and g >= automap:
                    especial = g - automap
                    tipo = {OUTRO: TIPO_OUTRO, VAZIO: TIPO_VAZIO, NAOVAZIO: TIPO_NAOVAZIO}.get(especial)
                    if tipo is not None:
                        e.append((x - x0, y - y0, tipo, 0))
                else:
                    e.append((x - x0, y - y0, TIPO_LADRILHO, g - tileset))
            g = saida[y][x]
            if g:
                s.append((x - x0, y - y0, -1 if (automap and g >= automap) else g - tileset))
        cx = sum(p[0] + .5 for p in celulas) / len(celulas)
        cy = sum(p[1] + .5 for p in celulas) / len(celulas)
        chance, desligada = 1.0, False
        for a, b, c, d, p, dis in opcoes:
            if a <= cx <= c and b <= cy <= d:
                chance, desligada = p, dis
        if not desligada:
            regras.append((y0, x0, chance, e, s))
    regras.sort(key=lambda r: (r[0], r[1]))
    return [(chance, e, s) for _, _, chance, e, s in regras]


def ler_cantos(caminho_tsx):
    """Tabela de cantos do conjunto (wangset) do tipo corner: indice tl*8+tr*4+br*2+bl -> ladrilho.
    Entre os que servem, fica o de chance maior que zero (os de chance zero so as regras usam).
    O de tudo cheio (15) pode ter varios: volta a lista e o peso de cada um."""
    tsx = ET.parse(caminho_tsx).getroot()
    chances = {int(t.get('id')): float(t.get('probability', 1)) for t in tsx.findall('tile')}
    por_canto = {}
    for w in tsx.find('wangsets/wangset').findall('wangtile'):
        v = [int(n) for n in w.get('wangid').split(',')]
        tl, tr, br, bl = v[7], v[1], v[3], v[5]
        indice = (tl > 0) * 8 + (tr > 0) * 4 + (br > 0) * 2 + (bl > 0)
        por_canto.setdefault(indice, []).append(int(w.get('tileid')))
    tabela = [-1] * 16
    cheios = []
    for indice, ladrilhos in por_canto.items():
        bons = [t for t in ladrilhos if chances.get(t, 1.0) > 0]
        if indice == 15:
            cheios = [(t, chances.get(t, 1.0)) for t in bons]
        tabela[indice] = (bons or ladrilhos)[0]
    return tabela, cheios


# ---------------------------------------------------------------- imagens
def copiar(origem, destino):
    Image.open(os.path.join(PACOTE, origem)).convert('RGBA').save(os.path.join(ARTE, destino))


def abismo():
    """Os ladrilhos do fundo roxo, na ordem em que aparecem no mapa de exemplo (camada pit)."""
    largura, altura, camadas, mapa = ler_camadas(os.path.join(TILED, 'Old Prison example map.tmx'))
    primeiros = sorted((int(t.get('firstgid')), t.get('source')) for t in mapa.findall('tileset'))
    terreno = next(f for f, s in primeiros if 'Terrain' in s)
    ids = sorted({g - terreno for linha in camadas['pit'] for g in linha if g})
    folha = Image.open(os.path.join(PACOTE, 'Tilesets', 'Tileset-Terrain-old prison.png')).convert('RGBA')
    colunas = folha.width // LADO
    tira = Image.new('RGBA', (LADO * len(ids), LADO), (0, 0, 0, 0))
    for i, t in enumerate(ids):
        x, y = (t % colunas) * LADO, (t // colunas) * LADO
        tira.paste(folha.crop((x, y, x + LADO, y + LADO)), (i * LADO, 0))
    tira.save(os.path.join(ARTE, 'Abismo.png'))
    return len(ids)


def enfeites():
    """Ossos, pedrinhas e papeis de 32 x 32 (cabem num ladrilho), numa folha so."""
    pasta = os.path.join(PACOTE, 'Props', 'atlas props - individual sprites')
    escolhidos = []
    for grupo in ('bones - color scheme 1', 'stones - color scheme 1', 'paper - color scheme 1'):
        arquivos = sorted(glob.glob(os.path.join(pasta, grupo + ' - *.png')),
                          key=lambda f: int(re.findall(r'(\d+)\.png$', f)[0]))
        for f in arquivos:
            im = Image.open(f).convert('RGBA')
            if im.size == (LADO, LADO) and im.getbbox():
                escolhidos.append(im)
    colunas = 16
    linhas = (len(escolhidos) + colunas - 1) // colunas
    folha = Image.new('RGBA', (colunas * LADO, linhas * LADO), (0, 0, 0, 0))
    for i, im in enumerate(escolhidos):
        folha.paste(im, ((i % colunas) * LADO, (i // colunas) * LADO))
    folha.save(os.path.join(ARTE, 'Enfeites.png'))
    return len(escolhidos)


# ---------------------------------------------------------------- C#
def cs_regras(nome, regras):
    linhas = [f'    public static readonly Regra[] {nome} =', '    {']
    for chance, e, s in regras:
        ent = ', '.join(f'{dx}, {dy}, {t}, {v}' for dx, dy, t, v in e)
        sai = ', '.join(f'{dx}, {dy}, {t}' for dx, dy, t in s)
        linhas.append(f'        new Regra({chance:g}f, new int[] {{ {ent} }}, new int[] {{ {sai} }}),')
    linhas.append('    };')
    return '\n'.join(linhas)


def main():
    if not os.path.isdir(TILED):
        sys.exit(f'Nao achei "TiledMap Editor New" em {PACOTE}: passe a pasta do pacote descompactado.')
    os.makedirs(ARTE, exist_ok=True)

    copiar('Tilesets/wall-1- 3 tiles tall.png', 'Chao.png')
    copiar('Tilesets/wall-2- 3 tiles tall.png', 'Paredes.png')
    copiar('Tilesets/blood pool - style2 - transparency.png', 'Sangue.png')
    quantos_abismo = abismo()
    quantos_enfeites = enfeites()

    cantos_chao, cheios_chao = ler_cantos(os.path.join(TILED, 'Tilesets', 'Tileset - wall 1.tsx'))
    cantos_parede, _ = ler_cantos(os.path.join(TILED, 'Tilesets', 'Tileset - wall 2.tsx'))
    cantos_sangue, cheios_sangue = ler_cantos(os.path.join(TILED, 'Tilesets', 'Blood pool - style2 - with spikes - transparency.tsx'))
    regras = os.path.join(TILED, 'Rules')
    # As regras da parede 1 e da parede 2 sao as mesmas (o mesmo desenho em cada folha): usa as da 2.
    poe = ler_regras(os.path.join(regras, 'wall-2-rule1-places all tiles.tmx'))
    varia = ler_regras(os.path.join(regras, 'wall-2-rule2-applies variations.tmx'))
    assert ler_regras(os.path.join(regras, 'wall-1-rule1-places all tiles.tmx')) == poe

    lista = lambda xs: ', '.join(str(x) for x in xs)
    codigo = f'''// Gerado por Ferramentas/OldPrison/importar.py a partir do pacote "EPIC RPG World Pack - Old Prison".
// Nao edite a mao: rode a ferramenta de novo.

/// <summary>
/// As tabelas e as regras do pacote Old Prison, tiradas dos arquivos do Tiled Map Editor que vem com ele.
///
/// Cantos: qual ladrilho vai em cada combinacao dos 4 cantos de um ladrilho (o indice e
/// cima-esquerda * 8 + cima-direita * 4 + baixo-direita * 2 + baixo-esquerda, cada um 1 quando e
/// "dentro": parede, chao ou poca). -1 = nada.
///
/// Regras: o automapping do pacote (ver <see cref="Automapa"/>). <see cref="RegrasQuePoem"/> poe as
/// faces das paredes e os encontros; <see cref="RegrasDeVariacao"/> troca alguns ladrilhos por variacoes
/// (tijolo rachado, musgo), cada uma com a sua chance. Servem pro chao e pras paredes: as duas folhas
/// tem os mesmos ladrilhos nos mesmos lugares.
/// </summary>
public static class DadosDoOldPrison
{{
    public const int Lado = {LADO};

    public static readonly int[] CantosDoChao = {{ {lista(cantos_chao)} }};
    public static readonly int[] CantosDasParedes = {{ {lista(cantos_parede)} }};
    public static readonly int[] CantosDoSangue = {{ {lista(cantos_sangue)} }};

    /// <summary>O chao inteiro (os 4 cantos dentro) tem varios desenhos; cada um com o seu peso.</summary>
    public static readonly int[] ChaoInteiro = {{ {lista(t for t, _ in cheios_chao)} }};
    public static readonly float[] PesoDoChaoInteiro = {{ {lista(f'{p:g}f' for _, p in cheios_chao)} }};

    public static readonly int[] SangueInteiro = {{ {lista(t for t, _ in cheios_sangue)} }};

    public const int LadrilhosDoAbismo = {quantos_abismo};
    public const int Enfeites = {quantos_enfeites};

{cs_regras('RegrasQuePoem', poe)}

{cs_regras('RegrasDeVariacao', varia)}
}}
'''
    with open(DADOS, 'w', encoding='utf-8', newline='\n') as f:
        f.write(codigo)
    print(f'ok: {len(poe)} + {len(varia)} regras, {quantos_abismo} ladrilhos de abismo, {quantos_enfeites} enfeites')


if __name__ == '__main__':
    main()
