"""
Tira dos pacotes da serie EPIC RPG World (em Pacotes/) as folhas dos inimigos que o jogo usa e grava em
Assets/Arte/Resources/Personagens/<Nome>/ as quatro que a AnimacaoDoInimigo le: Idle, Walk, Attack01 e
Death (quadros lado a lado, todos do mesmo tamanho).

Os quadros dos pacotes tem muita sobra e o boneco nem sempre no meio: aqui cada quadro e recortado em
volta do boneco (o meio do corpo parado/andando fica no meio do quadro) e, se o boneco olha pra
esquerda (ESPELHAR), a folha e espelhada: o jogo espera ele olhando pra direita. Grava tambem
Ferramentas/Personagens/quadros.json com o tamanho do quadro e onde ficam os pes de cada um.

Uso (da raiz do projeto):  python Ferramentas/Personagens/importar.py [pasta com os pacotes]
"""
import os
import sys
import json
from PIL import Image

RAIZ = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
PACOTES = sys.argv[1] if len(sys.argv) > 1 else os.path.join(RAIZ, 'Pacotes')
SAIDA = os.path.join(RAIZ, 'Assets', 'Arte', 'Resources', 'Personagens')
QUADROS = os.path.join(RAIZ, 'Ferramentas', 'Personagens', 'quadros.json')

CR = 'Crypt V1.6/Characters/'
DP = 'The Depths of the Mountain V1.5.1/Characters/'
OP = 'Old Prison V1.7.1/Characters/'

# Nome no jogo: (largura e altura do quadro no pacote, {folha do jogo: arquivo})
INIMIGOS = {
    'EsqueletoDaCripta': ((128, 128), {'Idle': CR + 'Skeleton/skeleton-variation1-idle.png', 'Walk': CR + 'Skeleton/skeleton-variation1-walk.png',
                                       'Attack01': CR + 'Skeleton/skeleton-variation1-attack.png', 'Death': CR + 'Skeleton/skeleton-variation1-death.png'}),
    'EsqueletoRubro': ((128, 128), {'Idle': CR + 'Skeleton/skeleton-variation2-idle.png', 'Walk': CR + 'Skeleton/skeleton-variation2-walk.png',
                                    'Attack01': CR + 'Skeleton/skeleton-variation2-attack.png', 'Death': CR + 'Skeleton/skeleton-variation2-death.png'}),
    'Aranha': ((64, 64), {'Idle': CR + 'Spider/Spider-variation1-idle.png', 'Walk': CR + 'Spider/Spider-variation1-walk.png',
                          'Attack01': CR + 'Spider/Spider-variation1-atk.png', 'Death': CR + 'Spider/Spider-variation1-death.png'}),
    'AranhaVenenosa': ((64, 64), {'Idle': CR + 'Spider/Spider-variation2-idle.png', 'Walk': CR + 'Spider/Spider-variation2-walk.png',
                                  'Attack01': CR + 'Spider/Spider-variation2-atk.png', 'Death': CR + 'Spider/Spider-variation2-death.png'}),
    'Minhocao': ((128, 128), {'Idle': CR + 'Big Worm 1/Big worm - 1-idle-8 frames.png', 'Walk': CR + 'Big Worm 1/Big worm - 1-idle-8 frames.png',
                              'Attack01': CR + 'Big Worm 1/Big worm - 1-appear-atk-29 frames.png', 'Death': CR + 'Big Worm 1/Big worm - 1-death-12 frames.png'}),
    'MinhocaoSombrio': ((128, 128), {'Idle': CR + 'Big Worm 2/Big worm - 2-idle-8 frames.png', 'Walk': CR + 'Big Worm 2/Big worm - 2-idle-8 frames.png',
                                     'Attack01': CR + 'Big Worm 2/Big worm - 2-appear-atk-29 frames.png', 'Death': CR + 'Big Worm 2/Big worm - 2-death-12 frames.png'}),
    'GoblinBrutamonte': ((256, 224), {'Idle': DP + 'Enemy 1/enemy 1-idle.png', 'Walk': DP + 'Enemy 1/enemy 1-walk.png',
                                      'Attack01': DP + 'Enemy 1/enemy 1-atk1.png', 'Death': DP + 'Enemy 1/enemy 1-death.png'}),
    'GoblinDaClava': ((256, 224), {'Idle': DP + 'Enemy 1/variation1/enemy 1 var1-idle.png', 'Walk': DP + 'Enemy 1/variation1/enemy 1 var1-walk.png',
                                   'Attack01': DP + 'Enemy 1/variation1/enemy 1 var1-atk1.png', 'Death': DP + 'Enemy 1/variation1/enemy 1 var1-death.png'}),
    'GoblinAssassino': ((160, 128), {'Idle': DP + 'Enemy 2/enemy 2-idle.png', 'Walk': DP + 'Enemy 2/enemy 2-walk.png',
                                     'Attack01': DP + 'Enemy 2/enemy 2-atk1-no combo.png', 'Death': DP + 'Enemy 2/enemy 2-death.png'}),
    'PoteMimico': ((160, 128), {'Idle': DP + 'Pot Creature/Pot Creature-idle.png', 'Walk': DP + 'Pot Creature/Pot Creature-walk.png',
                                'Attack01': DP + 'Pot Creature/Pot Creature-atk2.png', 'Death': DP + 'Pot Creature/Pot Creature-death.png'}),
    'EsqueletoDaPrisao': ((220, 220), {'Idle': OP + 'Skeleton 1/no shield/Skeleton 1 - animations-idle.png', 'Walk': OP + 'Skeleton 1/no shield/Skeleton 1 - animations-walk.png',
                                       'Attack01': OP + 'Skeleton 1/no shield/Skeleton 1 - animations-atk 1.png', 'Death': OP + 'Skeleton 1/no shield/Skeleton 1 - animations-death.png'}),
    'EsqueletoEscudeiro': ((220, 220), {'Idle': OP + 'Skeleton 1/with shield/Skeleton 1 - animations-idle.png', 'Walk': OP + 'Skeleton 1/with shield/Mage Skeleton - animations-walk.png',
                                        'Attack01': OP + 'Skeleton 1/with shield/Skeleton 1 - animations-atk 1.png', 'Death': OP + 'Skeleton 1/with shield/Skeleton 1 - animations-death.png'}),
    'EsqueletoMago': ((220, 220), {'Idle': OP + 'Mage Skeleton/Mage Skeleton no shield/Mage Skeleton - animations-idle.png', 'Walk': OP + 'Mage Skeleton/Mage Skeleton no shield/Mage Skeleton - animations-walk.png',
                                   'Attack01': OP + 'Mage Skeleton/Mage Skeleton no shield/Mage Skeleton - animations-atk2.png', 'Death': OP + 'Mage Skeleton/Mage Skeleton no shield/Mage Skeleton - animations-death.png'}),
    'Assassino': ((110, 96), {'Idle': OP + 'Assassin like enemy/Assassin like enemy - animations-idle.png', 'Walk': OP + 'Assassin like enemy/Assassin like enemy - animations-run.png',
                              'Attack01': OP + 'Assassin like enemy/Assassin like enemy - animations-atk1.png', 'Death': OP + 'Assassin like enemy/Assassin like enemy - animations-death.png'}),
    # Chefe: o Rei Esqueleto (o chefe do pacote The Depths of the Mountain)
    'ReiEsqueleto': ((351, 207), {'Idle': DP + 'Boss/boss anims-idle.png', 'Walk': DP + 'Boss/boss anims-walk.png',
                                  'Attack01': DP + 'Boss/boss anims-atk1.png', 'Attack02': DP + 'Boss/boss anims-atk2.png',
                                  'Attack03': DP + 'Boss/boss anims-atk3.png', 'Death': DP + 'Boss/boss anims-death.png'}),
}


# Os que olham pra esquerda nas folhas do pacote (todos os de agora olham pra direita).
ESPELHAR = set()


def quadros(caminho, tamanho):
    folha = Image.open(caminho).convert('RGBA')
    w, h = tamanho
    return [folha.crop((i * w, 0, i * w + w, h)) for i in range(folha.width // w)]


def caixa(imagens):
    caixas = [im.getbbox() for im in imagens if im.getbbox()]
    return (min(c[0] for c in caixas), min(c[1] for c in caixas), max(c[2] for c in caixas), max(c[3] for c in caixas))


def main():
    resumo = {}
    for nome, (tamanho, folhas) in INIMIGOS.items():
        anims = {k: quadros(os.path.join(PACOTES, v), tamanho) for k, v in folhas.items()}
        corpo = caixa(anims['Idle'] + anims['Walk'])
        tudo = caixa([q for qs in anims.values() for q in qs])
        meio = (corpo[0] + corpo[2]) / 2

        espelhar = nome in ESPELHAR

        meia = int(max(meio - tudo[0], tudo[2] - meio)) + 1
        topo, fundo = tudo[1] - 1, tudo[3] + 1
        largura, altura = meia * 2, fundo - topo
        pasta = os.path.join(SAIDA, nome)
        os.makedirs(pasta, exist_ok=True)
        for k, qs in anims.items():
            folha = Image.new('RGBA', (largura * len(qs), altura), (0, 0, 0, 0))
            for i, q in enumerate(qs):
                recorte = q.crop((int(meio) - meia, topo, int(meio) + meia, fundo))
                if espelhar:
                    recorte = recorte.transpose(Image.FLIP_LEFT_RIGHT)
                folha.paste(recorte, (i * largura, 0))
            folha.save(os.path.join(pasta, k + '.png'))
        # O golpe: o quadro do ataque em que o boneco (com a arma) vai mais longe do meio.
        alcances = [max(meio - c[0], c[2] - meio) if c else 0 for c in (q.getbbox() for q in anims['Attack01'])]
        golpe = alcances.index(max(alcances))
        # Os pes: o fundo do corpo, em pixels abaixo do meio do quadro.
        resumo[nome] = {'quadro': [largura, altura], 'pes': corpo[3] - (topo + altura / 2), 'golpe': golpe,
                        'altura': corpo[3] - corpo[1], 'quadros': {k: len(v) for k, v in anims.items()}, 'espelhado': espelhar}
        print(nome, resumo[nome])
    with open(QUADROS, 'w') as f:
        json.dump(resumo, f, indent=1)


if __name__ == '__main__':
    main()
