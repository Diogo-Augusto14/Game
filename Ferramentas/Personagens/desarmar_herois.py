"""
Tira a arma do desenho dos herois (as folhas do Tiny RPG ja vem com a arma na mao) pra arma aparecer
so na mao, girando com a mira (ArmaNaMao): sem isso, com uma arma achada ficavam duas no boneco.

Grava, pra cada heroi, Assets/Arte/Resources/Personagens/Herois/<Heroi>/SemArma/{Idle,Walk,Death}.png
(as folhas que o jogo usa; o retrato do menu continua com a arma).

Como separa a arma do corpo (sem a arte original sem arma, e no olho):
  1. o "nucleo" de cada quadro parado ou andando: o maior bloco grosso (abertura 5 x 5), que e o
     corpo; a caixa do corpo e a caixa de todos os nucleos, com uma folga;
  2. o que fica fora da caixa e arma (ou efeito); as cores que so aparecem fora (nunca no miolo do
     corpo) sao as da arma, e saem tambem dentro da caixa, fora do nucleo;
  3. ajustes a mao por heroi (LIMITE_DIREITA: a arma fica sempre do lado direito; APAGAR: a haste
     da lanca);
  4. na morte o corpo deita: a caixa e a de cada quadro;
  5. sobras soltas pequenas saem.

Uso (da raiz do projeto):  python Ferramentas/Personagens/desarmar_herois.py
"""
import os
import numpy as np
from PIL import Image
from scipy import ndimage

RAIZ = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
HEROIS = os.path.join(RAIZ, 'Assets', 'Arte', 'Resources', 'Personagens', 'Herois')
FOLHAS = ['Idle', 'Walk', 'Death']
Q = 100
UM = np.ones((3, 3), bool)

# A ultima coluna do corpo (o resto, pra direita, e arma), em pixels do quadro de 100.
LIMITE_DIREITA = {'Mago': 54, 'Machadeiro': 54, 'Padre': 55, 'Arqueiro': 54}
# Retangulos (x0, y0, x1, y1) apagados nos quadros parados e andando.
APAGAR = {'Lanceiro': [(55, 0, 59, 37)]}


def nucleo(M, k):
    O = ndimage.binary_opening(M, structure=np.ones((k, k), bool))
    lab, n = ndimage.label(O, structure=UM)
    if n == 0:
        return O
    tam = ndimage.sum(O, lab, range(1, n + 1))
    return lab == (np.argmax(tam) + 1)


def caixa(m, x0, y0, x1, y1):
    D = np.zeros(m.shape, bool)
    D[max(0, y0):y1 + 1, max(0, x0):x1 + 1] = True
    return D


def sobras(out, ate=8):
    resto = out[:, :, 3] > 0
    lab, n = ndimage.label(resto, structure=UM)
    if n > 1:
        tam = ndimage.sum(resto, lab, range(1, n + 1))
        maior = np.argmax(tam) + 1
        for i in range(1, n + 1):
            if i != maior and tam[i - 1] <= ate:
                out[lab == i] = 0


def heroi(nome):
    pasta = os.path.join(HEROIS, nome)
    folhas = {}
    for f in FOLHAS:
        sh = np.array(Image.open(os.path.join(pasta, f + '.png')).convert('RGBA'))
        folhas[f] = [sh[:, q * Q:(q + 1) * Q].copy() for q in range(sh.shape[1] // Q)]

    xs, ys, corpo = [], [], set()
    for f in ('Idle', 'Walk'):
        for a in folhas[f]:
            N = nucleo(a[:, :, 3] > 0, 5)
            yy, xx = np.nonzero(N)
            xs += [xx.min(), xx.max()]
            ys += [yy.min(), yy.max()]
            corpo |= set(map(tuple, a[ndimage.binary_erosion(N, structure=UM)][:, :3]))
    x0, x1, y0, y1 = min(xs) - 1, max(xs) + 1, min(ys) - 1, max(ys) + 2
    if nome in LIMITE_DIREITA:
        x1 = min(x1, LIMITE_DIREITA[nome])
    D = caixa(np.zeros((Q, Q)), x0, y0, x1, y1)

    fora = set()
    for f in ('Idle', 'Walk'):
        for a in folhas[f]:
            fora |= set(map(tuple, a[(a[:, :, 3] > 0) & ~D][:, :3]))
    arma = fora - corpo

    def cor_de_arma(a):
        flat = a[:, :, :3].reshape(-1, 3)
        return np.array([tuple(p) in arma for p in flat]).reshape(a.shape[:2])

    saida = {}
    for f, qs in folhas.items():
        novos = []
        for a in qs:
            M = a[:, :, 3] > 0
            out = a.copy()
            if f == 'Death':
                N = nucleo(M, 4)
                yy, xx = np.nonzero(N)
                Dq = caixa(M, xx.min() - 1, yy.min() - 2, xx.max() + 1, yy.max() + 1) if len(xx) else np.ones(M.shape, bool)
                out[M & (~Dq | (cor_de_arma(a) & ~N))] = 0
            else:
                N = nucleo(M, 5)
                out[M & (~D | (cor_de_arma(a) & ~N))] = 0
                for (ax0, ay0, ax1, ay1) in APAGAR.get(nome, []):
                    out[ay0:ay1 + 1, ax0:ax1 + 1] = 0
            sobras(out)
            novos.append(out)
        saida[f] = novos
    return saida


def main():
    for nome in sorted(os.listdir(HEROIS)):
        if not os.path.isdir(os.path.join(HEROIS, nome)):
            continue
        saida = heroi(nome)
        pasta = os.path.join(HEROIS, nome, 'SemArma')
        os.makedirs(pasta, exist_ok=True)
        for f, qs in saida.items():
            Image.fromarray(np.concatenate(qs, axis=1)).save(os.path.join(pasta, f + '.png'))
        print(nome, {f: len(q) for f, q in saida.items()})


if __name__ == '__main__':
    main()
