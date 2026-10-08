"""
Importa dos pacotes da serie EPIC RPG World (descompactados em Pacotes/, ver Pacotes/LEIAME.md no
repositorio privado ThePrettie-Pacotes) a arte dos mundos que nao sao o Old Prison:

  Cripta       (pacote Crypt)
    Assets/Arte/Resources/Temas/Cripta/Paredes.png   as paredes do pacote (wall-1)
    Assets/Arte/Resources/Temas/Cripta/Chao.png      o chao de pedra (ladrilhos inteiros do terreno)

  Profundezas  (pacote The Depths of the Mountain): salas flutuando sobre o vazio, sem paredes
    Assets/Arte/Resources/Temas/Profundezas/Chao.png    a plataforma de pedra marrom (Tileset 2)
    Assets/Arte/Resources/Temas/Profundezas/Abismo.png  o vazio escuro (o fundo do mapa de exemplo)

e escreve Assets/Scripts/Andar/DadosDosTemas.cs com as tabelas de cantos e as regras de variacao de
cada um. As regras que poem as faces sao as mesmas do Old Prison em todos (a ferramenta confere), e as
paredes do Crypt tem ate as mesmas variacoes.

Uso (da raiz do projeto):  python Ferramentas/Temas/importar.py [pasta com os pacotes]   (padrao: Pacotes)
Precisa do Pillow (pip install pillow).
"""
import os
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
    print(f'ok: Cripta {len(CHAO_DA_CRIPTA)} chaos; Profundezas {len(cheios_p)} chaos, {len(varia_p)} variacoes, vazio {cor}')


if __name__ == '__main__':
    main()
