"""Sons de ambiente dos mundos e os efeitos das coisas novas da 1.19.0 (sintese em numpy, com as ajudas do
sfx.py). Nao precisa do Freedoom.

  ambiente_wav/Base_<Mundo>.wav   o fundo que toca em loop em cada mundo (emenda sem corte)
  ambiente_wav/<Evento>_<n>.wav   o que acontece de vez em quando, longe: gota, corrente, rangido, gemido,
                                  vento, pedrinhas
  sfx_wav/<Som>.wav               Escudo, Raio, Bumerangue, CorteDeTiro, PortaoDaCela, Campeao, Cristal

Rodar: python3 ambiente.py   (depois converter pra OGG, ver LEIAME.md)
"""
import os

import numpy as np
from scipy import signal
from scipy.io import wavfile

from sfx import (SR, t_, vazio, ruido, passa_baixa, passa_alta, passa_banda, decai, sino, varredura_seno,
                 svf_curva, modal, reverb, em, salvar, clique_metal, baque)

AMB = 'ambiente_wav'
os.makedirs(AMB, exist_ok=True)


def gravar(nome, x, pico_db=-6.0):
    x = np.asarray(x, dtype=float)
    x = x / (np.abs(x).max() + 1e-12) * 10 ** (pico_db / 20)
    wavfile.write(os.path.join(AMB, nome + '.wav'), SR, (np.clip(x, -1, 1) * 32767).astype(np.int16))
    print(f'{nome:18s} {len(x) / SR:5.1f}s')


def marrom(n, semente):
    """Ruido marrom (grave, como um rumor de caverna)."""
    b = np.cumsum(ruido(n, semente))
    b = passa_alta(b, 20, 2)
    return b / (np.abs(b).max() + 1e-12)


def lfo(n, freq, fase=0.0):
    return np.sin(2 * np.pi * freq * np.arange(n) / SR + fase)


def em_loop(x, cruz=2.0):
    """Funde o fim no comeco: tocado em loop, nao tem emenda."""
    k = int(cruz * SR)
    w = np.linspace(0, 1, k)
    y = x[:-k].copy()
    y[:k] = x[-k:] * (1 - w) + x[:k] * w
    return y


# ------------------------------------------------------------------ os fundos
def base(nome, dur=24.0, semente=1):
    n = int(dur * SR)
    if nome == 'Porao':
        # Porao umido: rumor grave que respira devagar e um chiado de agua bem baixo.
        x = passa_baixa(marrom(n, semente), 180) * (0.75 + 0.25 * lfo(n, 1 / 8))
        x += passa_banda(ruido(n, semente + 1), 900, 2600) * 0.012 * (0.6 + 0.4 * lfo(n, 1 / 6, 1.0))
    elif nome == 'Catacumbas':
        # Tuneis frios: o vento uivando baixinho pelos corredores.
        curva = 380 + 220 * (0.5 + 0.5 * lfo(n, 1 / 7)) + 90 * lfo(n, 1 / 2.3, 2.0)
        x = svf_curva(ruido(n, semente), curva, 6.0) * 0.5 * (0.5 + 0.5 * lfo(n, 1 / 5, 0.5)) ** 2
        x += passa_baixa(marrom(n, semente + 2), 150) * 0.5
    elif nome == 'Cripta':
        # Cripta: um zumbido grave de pedra (duas notas que batem) e um sopro.
        t = np.arange(n) / SR
        zum = np.sin(2 * np.pi * 55 * t) + 0.6 * np.sin(2 * np.pi * 55.6 * t) + 0.3 * np.sin(2 * np.pi * 82.4 * t)
        x = zum * 0.12 * (0.7 + 0.3 * lfo(n, 1 / 9))
        x += passa_baixa(marrom(n, semente), 220) * 0.6
        x += passa_banda(ruido(n, semente + 3), 300, 900) * 0.03 * (0.5 + 0.5 * lfo(n, 1 / 11))
    elif nome == 'Profundezas':
        # Fundo da montanha: um rumor bem grave, como pedra assentando, e um borbulhar longe.
        x = passa_baixa(marrom(n, semente), 90) * 1.2 * (0.7 + 0.3 * lfo(n, 1 / 10))
        rng = np.random.default_rng(semente)
        for _ in range(int(dur * 1.5)):
            bolha = varredura_seno(0.08, rng.uniform(140, 260), rng.uniform(60, 100)) * decai(0.08, 0.025) * 0.08
            x = em(x, bolha, rng.uniform(0, dur - 0.2))
        x = x[:n]
    else:
        # Descanso (ao ar livre): vento manso nas folhas.
        curva = 1800 + 900 * (0.5 + 0.5 * lfo(n, 1 / 6))
        x = svf_curva(ruido(n, semente), curva, 0.8) * 0.25 * (0.5 + 0.5 * lfo(n, 1 / 4.5)) ** 1.5
        x += passa_baixa(marrom(n, semente + 1), 300) * 0.25
    x = reverb(x, 1.2, 0.3)[:n]
    return em_loop(x)


# ------------------------------------------------------------------ o que acontece longe
def gota(semente):
    rng = np.random.default_rng(semente)
    f0 = rng.uniform(1300, 2100)
    x = varredura_seno(0.05, f0, f0 * 0.55) * decai(0.05, 0.012) * 0.8
    x = em(vazio(0.3), x, 0)
    if rng.random() < 0.6:
        x = em(x, varredura_seno(0.04, f0 * 1.1, f0 * 0.6) * decai(0.04, 0.01) * 0.4, rng.uniform(0.12, 0.2))
    return reverb(x, 1.8, 0.55, semente, 3500)


def corrente(semente):
    rng = np.random.default_rng(semente)
    x = vazio(1.4)
    t = 0.0
    for k in range(rng.integers(6, 11)):
        t += rng.uniform(0.04, 0.14)
        x = em(x, clique_metal(rng.uniform(1600, 2800), semente + k, 0.02, rng.uniform(0.3, 0.8)), t)
    rangido = passa_banda(ruido(int(0.6 * SR), semente), 400, 900) * sino(0.6, 2) * 0.15
    x = em(x, rangido, 0.1)
    return reverb(passa_baixa(x, 5000), 1.4, 0.45, semente, 3000)


def rangido(semente):
    # Madeira velha rangendo: pulsos (gruda e solta) num ritmo que muda, passados num filtro.
    rng = np.random.default_rng(semente)
    dur = rng.uniform(0.9, 1.5)
    n = int(dur * SR)
    taxa = rng.uniform(25, 45) * (1 + 0.6 * np.sin(np.pi * np.arange(n) / n))
    fase = np.cumsum(taxa) / SR
    pulsos = (np.diff(np.floor(fase), prepend=0) > 0).astype(float)
    x = signal.lfilter([1], [1, -0.97], pulsos)
    x = passa_banda(x, 350, 1500) * sino(dur, 0.8)
    return reverb(x, 1.2, 0.45, semente, 2500)


def gemido(semente):
    # Uma voz longe, sem palavra: um "uuuh" que sobe e cai, filtrado como vogal e afogado no eco.
    rng = np.random.default_rng(semente)
    dur = rng.uniform(2.4, 3.4)
    n = int(dur * SR)
    f0 = rng.uniform(95, 140)
    curva = f0 * (1 + 0.25 * np.sin(np.pi * np.arange(n) / n) - 0.1 * np.arange(n) / n)
    fase = 2 * np.pi * np.cumsum(curva) / SR
    serra = signal.sawtooth(fase) + 0.3 * ruido(n, semente)
    voz = 0.0
    for (fc, bw, g) in ((320, 90, 1.0), (800, 140, 0.5), (2500, 300, 0.12)):
        voz = voz + passa_banda(serra, fc - bw / 2, fc + bw / 2) * g
    voz = voz * sino(dur, 1.6) * (1 + 0.15 * lfo(n, 5.5))
    voz = passa_baixa(voz, 1800)
    return reverb(voz, 2.5, 0.65, semente, 2000)


def vento(semente):
    rng = np.random.default_rng(semente)
    dur = rng.uniform(3.0, 4.5)
    n = int(dur * SR)
    curva = 300 + 700 * sino(dur, 1.2) + 120 * lfo(n, rng.uniform(0.6, 1.2))
    x = svf_curva(ruido(n, semente), curva, 5.0) * sino(dur, 1.5)
    return reverb(x, 1.5, 0.4, semente, 3000)


def pedrinhas(semente):
    rng = np.random.default_rng(semente)
    x = vazio(1.2)
    t = 0.0
    for k in range(rng.integers(4, 8)):
        t += rng.uniform(0.05, 0.18) * (1 + k * 0.15)
        tic = passa_banda(ruido(int(0.03 * SR), semente + k), 1200, 4500) * decai(0.03, 0.006) * rng.uniform(0.3, 0.9)
        x = em(x, tic + varredura_seno(0.03, rng.uniform(500, 900), 300) * decai(0.03, 0.008) * 0.3, t)
    return reverb(x, 1.6, 0.5, semente, 3000)


# ------------------------------------------------------------------ os efeitos das coisas novas
def efeitos():
    R = {}

    # O escudo do campeao subindo: um "vuum" grave que abre e um brilho de vidro.
    dur = 0.6
    vuum = svf_curva(ruido(int(dur * SR), 81), np.geomspace(200, 1400, int(dur * SR)), 3.0) * decai(dur, 0.25, 0.05)
    t = t_(dur)
    vidro = sum(np.sin(2 * np.pi * f * t) for f in (1320, 1980, 2640)) * decai(dur, 0.18, 0.04) * 0.08
    R['Escudo'] = (reverb(vuum + vidro, 0.6, 0.3), -6)

    # O raio pulando: estalo eletrico, um chiado que crepita e some.
    dur = 0.35
    n = int(dur * SR)
    rng = np.random.default_rng(91)
    crepita = ruido(n, 91) * (rng.random(n) < 0.08) * 1.5
    zap = passa_alta(ruido(n, 92), 1800) * decai(dur, 0.06) * 0.6
    zum = signal.sawtooth(2 * np.pi * np.cumsum(np.geomspace(220, 90, n)) / SR) * decai(dur, 0.08) * 0.25
    R['Raio'] = (reverb(passa_baixa(crepita * decai(dur, 0.1) + zap + zum, 9000), 0.3, 0.15), -5)

    # O bumerangue saindo: vuuush girando (o filtro sobe e desce varias vezes).
    dur = 0.45
    n = int(dur * SR)
    curva = 900 + 1400 * (0.5 + 0.5 * np.sin(2 * np.pi * 14 * np.arange(n) / SR))
    R['Bumerangue'] = (reverb(svf_curva(ruido(n, 101), curva, 2.5) * sino(dur, 1.2), 0.25, 0.12), -7)

    # A lamina cortando um tiro no ar: um "ting" metalico curto e um chiado.
    ting = modal([2600, 3900, 5300, 7100], [0.12, 0.09, 0.06, 0.04], [0.6, 0.4, 0.25, 0.15], 0.4, 111)
    chiado = passa_alta(ruido(int(0.05 * SR), 112), 3000) * decai(0.05, 0.012) * 0.6
    R['CorteDeTiro'] = (reverb(em(ting, chiado, 0), 0.3, 0.15), -7)

    # O portao da cela subindo: corrente puxando (cliques) e o trinco no alto.
    x = vazio(1.5)
    rng = np.random.default_rng(121)
    tt = 0.0
    for k in range(14):
        tt += rng.uniform(0.06, 0.09)
        x = em(x, clique_metal(rng.uniform(1500, 2400), 121 + k, 0.018, 0.5), tt)
    x = em(x, passa_banda(ruido(int(1.1 * SR), 130), 250, 700) * sino(1.1, 1.5) * 0.25, 0.0)
    x = em(x, baque(120, 60, 0.3, 0.08, 0.7, 0.4, 131), tt + 0.05)
    R['PortaoDaCela'] = (reverb(x, 0.8, 0.3), -4)

    # O campeao acordando: um rugido grave e um acorde dissonante que sobe.
    dur = 0.9
    n = int(dur * SR)
    t = t_(dur)
    acorde = sum(signal.sawtooth(2 * np.pi * f * t) for f in (110, 116.5, 164.8)) / 3
    acorde = passa_baixa(acorde, 1200) * decai(dur, 0.35, 0.08) * 0.5
    rosnado = passa_banda(ruido(n, 141), 90, 400) * (1 + 0.5 * lfo(n, 23)) * decai(dur, 0.25, 0.03) * 0.6
    R['Campeao'] = (reverb(acorde + rosnado, 0.7, 0.3), -5)

    # O cristal pego: tres notas de vidro descendo, frias.
    x = vazio(0.8)
    for k, f in enumerate((1568, 1175, 932)):
        x = em(x, modal([f, f * 2.76, f * 5.4], [0.3, 0.15, 0.08], [0.6, 0.2, 0.08], 0.5, 151 + k), k * 0.07)
    R['Cristal'] = (reverb(x, 0.9, 0.35), -6)
    return R


def main():
    for nome in ('Porao', 'Catacumbas', 'Cripta', 'Profundezas', 'Descanso'):
        gravar('Base_' + nome, base(nome, semente=len(nome)), -10.0)

    for i in range(3):
        gravar(f'Gota_{i + 1}', gota(200 + i), -8.0)
    for i in range(2):
        gravar(f'Corrente_{i + 1}', corrente(300 + i), -8.0)
        gravar(f'Rangido_{i + 1}', rangido(400 + i), -9.0)
        gravar(f'Gemido_{i + 1}', gemido(500 + i), -9.0)
        gravar(f'Vento_{i + 1}', vento(600 + i), -9.0)
        gravar(f'Pedrinhas_{i + 1}', pedrinhas(700 + i), -8.0)

    for nome, (x, pico) in efeitos().items():
        salvar(nome, x, pico)


if __name__ == '__main__':
    main()
