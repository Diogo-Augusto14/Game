"""Efeitos sonoros: sintese (numpy), trechos do Freedoom (BSD) e vinhetas no soundfont (MIT)."""
import os
import subprocess
import sys

import mido
import numpy as np
from scipy import signal
from scipy.io import wavfile

SR = 44100
FD = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'fd', 'sfx')
SF = '/usr/share/sounds/sf3/MuseScore_General_Lite.sf3'
OUT = 'sfx_wav'
os.makedirs(OUT, exist_ok=True)


# ------------------------------------------------------------------ utilidades
def t_(dur):
    return np.arange(int(dur * SR)) / SR


def vazio(dur):
    return np.zeros(int(dur * SR))


def ruido(n, semente):
    return np.random.default_rng(semente).uniform(-1, 1, n)


def sos(tipo, f, ordem=2):
    return signal.butter(ordem, f, btype=tipo, fs=SR, output='sos')


def passa_baixa(x, f, ordem=2):
    return signal.sosfilt(sos('lowpass', f, ordem), x)


def passa_alta(x, f, ordem=2):
    return signal.sosfilt(sos('highpass', f, ordem), x)


def passa_banda(x, lo, hi, ordem=2):
    return signal.sosfilt(sos('bandpass', [lo, hi], ordem), x)


def decai(dur, tau, ataque=0.002):
    t = t_(dur)
    e = np.exp(-t / tau)
    a = int(ataque * SR)
    if a > 0:
        e[:a] *= np.linspace(0, 1, a)
    f = min(len(e) // 3, int(0.006 * SR))
    if f > 0:
        e[-f:] *= np.linspace(1, 0, f)
    return e


def sino(dur, potencia=1.5):
    t = t_(dur)
    return np.sin(np.pi * t / dur) ** potencia


def varredura_seno(dur, f0, f1, curva='exp'):
    t = t_(dur)
    if curva == 'exp':
        f = f0 * (f1 / f0) ** (t / dur)
    else:
        f = f0 + (f1 - f0) * t / dur
    return np.sin(2 * np.pi * np.cumsum(f) / SR)


def svf_banda(x, f0, f1, q=2.0):
    """Filtro de estado variavel com centro indo de f0 a f1 (exponencial): o 'vuuush'."""
    n = len(x)
    fc = f0 * (f1 / f0) ** (np.arange(n) / max(1, n - 1)) if np.isscalar(f0) else f0
    y = np.zeros(n)
    low = band = 0.0
    damp = 1.0 / q
    for i in range(n):
        f = 2 * np.sin(np.pi * min(fc[i], SR / 6) / SR)
        high = x[i] - low - damp * band
        band += f * high
        low += f * band
        y[i] = band
    return y


def svf_curva(x, curva, q=2.0):
    n = len(x)
    y = np.zeros(n)
    low = band = 0.0
    damp = 1.0 / q
    for i in range(n):
        f = 2 * np.sin(np.pi * min(curva[i], SR / 6) / SR)
        high = x[i] - low - damp * band
        band += f * high
        low += f * band
        y[i] = band
    return y


def modal(freqs, taus, amps, dur, semente=0):
    t = t_(dur)
    rng = np.random.default_rng(semente)
    y = np.zeros(len(t))
    for f, tau, a in zip(freqs, taus, amps):
        y += a * np.sin(2 * np.pi * f * t + rng.uniform(0, 2 * np.pi)) * np.exp(-t / tau)
    y[:int(0.0015 * SR)] *= np.linspace(0, 1, int(0.0015 * SR))
    f = min(len(y) // 3, int(0.006 * SR))
    y[-f:] *= np.linspace(1, 0, f)
    return y


def corda(freq, dur, amortecimento=0.996, brilho=0.5, semente=1):
    """Karplus-Strong: corda dedilhada."""
    n = int(dur * SR)
    p = int(SR / freq)
    buf = passa_baixa(ruido(p, semente), 2000 + 6000 * brilho)
    y = np.zeros(n)
    for i in range(n):
        y[i] = buf[i % p]
        buf[i % p] = amortecimento * 0.5 * (buf[i % p] + buf[(i + 1) % p])
    return y


def reverb(x, tamanho=0.4, mistura=0.2, semente=7, brilho=4000):
    n = int(tamanho * SR)
    t = np.arange(n) / SR
    ir = ruido(n, semente) * np.exp(-6.9 * t / tamanho)
    ir = passa_baixa(ir, brilho)
    ir[:int(0.008 * SR)] = 0
    ir /= np.sqrt(np.sum(ir ** 2)) + 1e-9
    molhado = signal.fftconvolve(x, ir)
    seco = np.concatenate([x, np.zeros(len(molhado) - len(x))])
    return seco * (1 - mistura) + molhado * mistura * 0.9


def em(base, x, inicio, ganho=1.0):
    i = int(round(inicio * SR))
    fim = i + len(x)
    if fim > len(base):
        base = np.concatenate([base, np.zeros(fim - len(base))])
    base[i:fim] += x * ganho
    return base


def acabamento(x, pico_db=-3.0, cauda_db=-60.0):
    x = np.asarray(x, dtype=float)
    x = x - np.mean(x[:64]) * 0  # sem DC dos trechos sintetizados
    x = passa_alta(x, 25, 2)
    lim = np.abs(x).max() * 10 ** (cauda_db / 20)
    vivo = np.where(np.abs(x) > lim)[0]
    x = x[: vivo[-1] + 1] if len(vivo) else x
    fade = min(len(x) // 4, int(0.015 * SR))
    x[-fade:] *= np.linspace(1, 0, fade)
    ini = int(0.0008 * SR)
    x[:ini] *= np.linspace(0, 1, ini)
    return x / (np.abs(x).max() + 1e-12) * 10 ** (pico_db / 20)


MAXIMO = {'Coracao': 1.2, 'Cura': 1.6, 'Item': 2.4, 'Segredo': 2.6, 'Vitoria': 3.0, 'BauAbre': 2.2,
          'MorteInimigo': 1.0, 'PortaAbre': 1.0, 'Compra': 0.9, 'Alcapao': 1.2, 'Queda': 1.3, 'Feitico': 1.4}


def salvar(nome, x, pico_db=-3.0):
    x = acabamento(x, pico_db, -50.0)
    m = MAXIMO.get(nome)
    if m and len(x) > m * SR:
        x = x[:int(m * SR)]
        f = int(min(0.4, m / 3) * SR)
        x[-f:] *= np.linspace(1, 0, f) ** 2
    caminho = os.path.join(OUT, nome + '.wav')
    wavfile.write(caminho, SR, (np.clip(x, -1, 1) * 32767).astype(np.int16))
    print(f'{nome:16s} {len(x) / SR:5.2f}s')


def freedoom(nome, ini=0.0, dur=None, tom=1.0):
    taxa, a = wavfile.read(os.path.join(FD, nome + '.wav'))
    x = (a.astype(float) - 128.0) / 128.0
    x = x - np.mean(x)
    taxa_eff = taxa * tom  # tocar mais rapido = mais agudo
    from fractions import Fraction
    fr = Fraction(SR / taxa_eff).limit_denominator(2000)
    y = signal.resample_poly(x, fr.numerator, fr.denominator)
    y = passa_baixa(y, min(0.45 * taxa_eff, 16000), 4)
    i0 = int(ini * SR)
    y = y[i0:] if dur is None else y[i0:i0 + int(dur * SR)]
    if dur is not None:
        f = int(0.06 * SR)
        y[-f:] *= np.linspace(1, 0, f)
    return y


def soundfont(notas, dur, nome_tmp='vinheta'):
    """notas: [(inicio_s, dur_s, nota_midi, vel, programa, banco)] -> mono."""
    mid = mido.MidiFile(ticks_per_beat=960)
    tr = mido.MidiTrack()
    mid.tracks.append(tr)
    tr.append(mido.MetaMessage('set_tempo', tempo=1000000))  # 1 batida = 1 s
    canais = {}
    eventos = []
    for (ini, d, n, v, prog, banco, *resto) in notas:
        chave = (prog, banco)
        bateria = banco == 128
        if chave not in canais:
            c = 9 if bateria else len([k for k in canais if k[1] != 128])
            if not bateria and c >= 9:
                c += 1
            canais[chave] = c
            if not bateria:
                eventos.append((0, 0, mido.Message('control_change', channel=c, control=0, value=banco)))
            eventos.append((0, 1, mido.Message('program_change', channel=c, program=prog)))
            eventos.append((0, 2, mido.Message('control_change', channel=c, control=91, value=resto[0] if resto else 60)))
        c = canais[chave]
        eventos.append((int(ini * 960), 4, mido.Message('note_on', channel=c, note=n, velocity=v)))
        eventos.append((int((ini + d) * 960), 3, mido.Message('note_off', channel=c, note=n, velocity=0)))
    eventos.sort(key=lambda e: (e[0], e[1]))
    agora = 0
    for (t, _, m) in eventos:
        m.time = t - agora
        agora = t
        tr.append(m)
    tr.append(mido.MetaMessage('end_of_track', time=int(dur * 960) - agora))
    mid.save(f'/tmp/{nome_tmp}.mid')
    subprocess.run(['fluidsynth', '-ni', '-q', '-O', 'float', '-T', 'wav', '-F', f'/tmp/{nome_tmp}.wav', '-r', str(SR),
                    '-g', '0.5', '-o', 'synth.reverb.room-size=0.6', '-o', 'synth.reverb.level=0.7', SF,
                    f'/tmp/{nome_tmp}.mid'], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    _, a = wavfile.read(f'/tmp/{nome_tmp}.wav')
    return a.astype(float).mean(axis=1)


def p(nome):
    import re
    m = re.fullmatch(r'([A-G])([#b]?)(-?\d)', nome)
    n = {'C': 0, 'D': 2, 'E': 4, 'F': 5, 'G': 7, 'A': 9, 'B': 11}[m.group(1)]
    n += {'#': 1, 'b': -1, '': 0}[m.group(2)]
    return 12 * (int(m.group(3)) + 1) + n


def clique_metal(freq, semente, tau=0.02, amp=1.0):
    r = np.random.default_rng(semente)
    f = freq * r.uniform(0.95, 1.05)
    x = modal([f, f * 1.63, f * 2.41, f * 3.17], [tau, tau * 0.8, tau * 0.6, tau * 0.4],
              [1, 0.6, 0.4, 0.25], tau * 6 + 0.01, semente)
    x[:int(0.004 * SR)] += passa_alta(ruido(int(0.004 * SR), semente), 3000) * 0.4
    return x * amp


def baque(f0=90, f1=40, dur=0.35, tau=0.12, amp=1.0, ruido_amp=0.6, semente=3):
    x = varredura_seno(dur, f0, f1) * decai(dur, tau) * amp
    n = passa_baixa(ruido(int(0.06 * SR), semente), 700) * decai(0.06, 0.02) * ruido_amp
    return em(x, n, 0)


# ------------------------------------------------------------------ receitas
def receitas():
    R = {}

    # Arco: estalo da corda + zunido da flecha saindo.
    def flecha(freq, semente, escuro=False):
        x = corda(freq, 0.22, 0.990, 0.4, semente) * decai(0.22, 0.05) * 0.55
        x = passa_baixa(x, 2500 if escuro else 3500)
        estalo = passa_alta(ruido(int(0.02 * SR), semente), 2500) * decai(0.02, 0.006) * 0.7
        zum = svf_banda(ruido(int(0.16 * SR), semente + 1), 3200 if not escuro else 2200, 1100, 2.5) * sino(0.16, 1.2) * 0.9
        y = em(vazio(0.25), x, 0)
        y = em(y, estalo, 0)
        y = em(y, zum, 0.01)
        return reverb(y, 0.25, 0.12)
    R['Tiro'] = (flecha(196, 11), -6)
    R['FlechaInimigo'] = (flecha(147, 21, escuro=True), -5)

    # Magia do mago e do padre: sopro que sobe com brilho por cima.
    def magia(semente):
        dur = 0.32
        sopro = svf_banda(ruido(int(dur * SR), semente), 500, 2200, 1.8) * decai(dur, 0.12, 0.012) * 0.9
        tom = varredura_seno(dur, 420, 880) * decai(dur, 0.09, 0.005) * 0.25
        t = t_(dur)
        brilho = sum(np.sin(2 * np.pi * f * t) for f in (1760, 2217, 2637)) * (0.5 + 0.5 * np.sin(2 * np.pi * 30 * t)) * decai(dur, 0.1, 0.02) * 0.07
        return reverb(sopro + tom + brilho, 0.35, 0.18)
    R['Magia'] = (magia(5), -6)

    # Lamina cortando o ar: vuuush que sobe e desce + um fio de metal.
    def corte(semente):
        dur = 0.22
        n = int(dur * SR)
        curva = np.concatenate([np.geomspace(1100, 4800, n // 2), np.geomspace(4800, 1400, n - n // 2)])
        x = svf_curva(ruido(n, semente), curva, 1.4) * sino(dur, 1.3)
        anel = modal([3150, 4720, 6300], [0.09, 0.07, 0.05], [0.12, 0.08, 0.05], 0.3, semente)
        y = em(vazio(0.3), x, 0)
        y = em(y, anel, 0.03)
        return reverb(y, 0.25, 0.12)
    R['Corte'] = (corte(31), -6)

    # Flecha/tiro acertando bicho: baque curto e estalo.
    def acerto(semente, f0):
        dur = 0.13
        corpo = varredura_seno(dur, f0, 55) * decai(dur, 0.03) * 0.9
        tapa = passa_banda(ruido(int(dur * SR), semente), 700, 2600) * decai(dur, 0.012) * 0.7
        massa = passa_baixa(ruido(int(dur * SR), semente + 9), 1200) * decai(dur, 0.04) * 0.35
        return reverb(corpo + tapa + massa, 0.18, 0.08)
    R['Acerto'] = (acerto(41, 170), -5)
    R['Acerto_2'] = (acerto(42, 150), -5)
    R['Acerto_3'] = (acerto(43, 190), -5)

    # O tiro do jogador estourando na parede ou no fim do alcance.
    def respingo(semente):
        dur = 0.08
        tic = passa_banda(ruido(int(dur * SR), semente), 1500, 5000) * decai(dur, 0.006) * 0.6
        plop = varredura_seno(dur, 520, 220) * decai(dur, 0.015) * 0.4
        return tic + plop
    R['Respingo'] = (respingo(51), -9)

    # Portao de grade descendo e batendo no chao (sala trancou).
    def portao_bate():
        y = vazio(0.9)
        y = em(y, baque(85, 42, 0.45, 0.13, 1.0, 0.8, 61), 0)
        metal = modal([233, 587, 1123, 1871, 2790, 3960], [0.35, 0.25, 0.18, 0.12, 0.08, 0.06],
                      [0.25, 0.4, 0.35, 0.3, 0.2, 0.15], 0.7, 62)
        y = em(y, metal, 0.003, 0.8)
        for k, t in enumerate((0.07, 0.12, 0.19)):
            y = em(y, clique_metal(2100 + 300 * k, 63 + k, 0.015, 0.25 / (k + 1)), t)
        return reverb(y, 0.7, 0.25)
    R['PortaFecha'] = (portao_bate(), -3)

    # Portao subindo: corrente chacoalhando e o tranco no alto (sala limpa).
    def portao_sobe():
        dur = 0.55
        y = vazio(0.95)
        r = np.random.default_rng(71)
        t = 0.0
        k = 0
        while t < dur - 0.04:
            y = em(y, clique_metal(r.uniform(1700, 2600), 100 + k, 0.012, r.uniform(0.25, 0.5)), t)
            t += r.uniform(0.025, 0.045)
            k += 1
        roda = passa_banda(ruido(int(dur * SR), 72), 150, 700)
        am = 0.6 + 0.4 * np.sin(2 * np.pi * 22 * t_(dur))
        y = em(y, roda * am * sino(dur, 0.6) * 0.35, 0)
        trava = em(modal([180, 460, 1050], [0.09, 0.06, 0.04], [0.6, 0.4, 0.3], 0.3, 73), baque(130, 70, 0.2, 0.05, 0.5, 0.4, 74), 0)
        y = em(y, trava, dur + 0.02)
        return reverb(y, 0.6, 0.22)
    R['PortaAbre'] = (portao_sobe(), -4)

    # Moeda: dois "tlim" de metal.
    def moeda(semente, base=2794):
        y = vazio(0.7)
        for k, (t, g, f) in enumerate(((0, 1.0, 1.0), (0.075, 0.6, 1.06))):
            m = modal([base * f, base * 1.5 * f, base * 2.12 * f, base * 2.67 * f], [0.35, 0.22, 0.15, 0.1],
                      [1, 0.6, 0.35, 0.2], 0.6, semente + k)
            m[:int(0.002 * SR)] += passa_alta(ruido(int(0.002 * SR), semente), 4000) * 0.5
            y = em(y, m, t, g)
        return reverb(y, 0.3, 0.12)
    R['Moeda'] = (moeda(81), -4)

    # Chave: molho de chaves chacoalhando.
    def chave(semente):
        y = vazio(0.6)
        r = np.random.default_rng(semente)
        for k, t in enumerate(sorted(r.uniform(0, 0.3, 7))):
            f = r.uniform(3000, 6200)
            y = em(y, modal([f, f * 1.48, f * 2.11], [0.09, 0.06, 0.04], [1, 0.5, 0.3], 0.3, semente + k), t, r.uniform(0.3, 0.8))
        return reverb(passa_alta(y, 1500), 0.3, 0.15)
    R['Chave'] = (chave(91), -5)

    # Compra: punhado de moedas.
    def compra():
        y = vazio(0.9)
        r = np.random.default_rng(95)
        for k, t in enumerate((0, 0.05, 0.09, 0.16, 0.22)):
            f = r.uniform(2400, 3400)
            y = em(y, modal([f, f * 1.5, f * 2.12, f * 2.67], [0.3, 0.2, 0.13, 0.09], [1, 0.6, 0.35, 0.2], 0.5, 96 + k), t, r.uniform(0.5, 0.9))
        y = em(y, moeda(99, 3136), 0.3, 0.9)
        return reverb(y, 0.3, 0.12)
    R['Compra'] = (compra(), -4)

    # Cadeado abrindo: dois estalos de metal.
    def destranca():
        y = vazio(0.4)
        y = em(y, clique_metal(1650, 111, 0.025, 0.7), 0)
        y = em(y, clique_metal(2300, 112, 0.03, 1.0), 0.12)
        y = em(y, baque(320, 150, 0.08, 0.02, 0.5, 0.3, 113), 0.12)
        return reverb(y, 0.3, 0.15)
    R['Destranca'] = (destranca(), -4)

    # Negado: dois tons abafados descendo ("hum-hum").
    def negado():
        y = vazio(0.4)
        for k, (t, nota) in enumerate(((0, 'G3'), (0.13, 'D3'))):
            f = 440 * 2 ** ((p(nota) - 69) / 12)
            tt = t_(0.14)
            tom = (np.sin(2 * np.pi * f * tt) + 0.3 * np.sin(2 * np.pi * 2 * f * tt) + 0.15 * np.sin(2 * np.pi * 3 * f * tt))
            tom *= decai(0.14, 0.06, 0.004)
            y = em(y, passa_baixa(tom, 1200), t, 0.8)
        return reverb(y, 0.2, 0.1)
    R['Negado'] = (negado(), -5)

    # Pulo: "vump" que sobe.
    def pulo(semente):
        dur = 0.24
        x = varredura_seno(dur, 110, 280) * decai(dur, 0.08, 0.01) * 0.8
        sopro = svf_banda(ruido(int(dur * SR), semente), 300, 1300, 1.5) * sino(dur, 1.2) * 0.6
        return reverb(x + sopro, 0.25, 0.12)
    R['Pulo'] = (pulo(121), -4)

    # Arremesso: vuuush curto.
    def arremesso(semente):
        dur = 0.16
        return svf_banda(ruido(int(dur * SR), semente), 900, 2600, 2.0) * sino(dur, 1.4)
    R['Arremesso'] = (arremesso(131), -6)

    # Dash: vento da arrancada.
    def dash(semente):
        dur = 0.26
        x = svf_banda(ruido(int(dur * SR), semente), 450, 2300, 1.3) * sino(dur, 1.0) * decai(dur, 0.2, 0.0)
        ronco = passa_baixa(ruido(int(dur * SR), semente + 1), 200) * decai(dur, 0.06, 0.01) * 0.5
        return reverb(x + ronco, 0.2, 0.08)
    R['Dash'] = (dash(141), -5)

    # Pancada pesada: chefe batendo no chao, golpe grande.
    def pancada(semente):
        y = vazio(1.0)
        y = em(y, varredura_seno(0.7, 75, 30) * decai(0.7, 0.25, 0.003), 0)
        y = em(y, passa_baixa(ruido(int(0.12 * SR), semente), 600) * decai(0.12, 0.04), 0, 0.9)
        grao = passa_banda(ruido(int(0.4 * SR), semente + 1), 300, 1500)
        trem = (np.random.default_rng(semente).uniform(0, 1, int(0.4 * SR)) > 0.985).astype(float)
        trem = np.convolve(trem, np.exp(-np.arange(200) / 40.0))[:len(grao)]
        y = em(y, grao * trem * decai(0.4, 0.12) * 0.6, 0.01)
        return reverb(y, 0.8, 0.28)
    R['Pancada'] = (pancada(151), -1.5)

    # Queda pelo alcapao: vento descendo e o baque la embaixo.
    def queda():
        y = vazio(1.1)
        y = em(y, svf_banda(ruido(int(0.75 * SR), 161), 2400, 260, 2.0) * sino(0.75, 0.8) * 0.9, 0)
        y = em(y, varredura_seno(0.7, 900, 300) * sino(0.7, 1) * 0.06, 0)
        y = em(y, baque(90, 45, 0.3, 0.1, 0.8, 0.6, 162), 0.78)
        return reverb(y, 0.6, 0.2)
    R['Queda'] = (queda(), -3)

    # Espinhos saindo do chao: "tchim" de metal.
    def espinhos():
        y = vazio(0.5)
        for k, t in enumerate((0, 0.018, 0.035)):
            f = 2950 * (1 + 0.04 * k)
            m = modal([f, f * 1.5, f * 2.11, f * 2.68], [0.16, 0.12, 0.09, 0.06], [0.5, 0.35, 0.25, 0.15], 0.4, 170 + k)
            y = em(y, m, t, 0.7)
            y = em(y, passa_banda(ruido(int(0.04 * SR), 175 + k), 3000, 8000) * decai(0.04, 0.012), t, 0.5)
        return reverb(y, 0.3, 0.15)
    R['Espinhos'] = (espinhos(), -6)

    # Alcapao abrindo: pedra arrastando e o tampo caindo.
    def alcapao():
        dur = 0.5
        y = vazio(1.0)
        grao = passa_banda(ruido(int(dur * SR), 181), 120, 900)
        pulsos = np.random.default_rng(182).uniform(0.3, 1.0, int(dur * 40) + 2)
        am = np.interp(t_(dur), np.linspace(0, dur, len(pulsos)), pulsos)
        y = em(y, grao * am * sino(dur, 0.5) * 0.7, 0)
        y = em(y, baque(75, 40, 0.35, 0.12, 0.9, 0.7, 183), dur)
        return reverb(y, 0.7, 0.25)
    R['Alcapao'] = (alcapao(), -3)

    # Bomba acesa: o pavio chiando e estalando (dura o pavio, 1,5 s).
    def pavio():
        dur = 1.5
        r = np.random.default_rng(191)
        chiado = passa_banda(ruido(int(dur * SR), 192), 2000, 7000) * 0.22
        chiado *= 0.7 + 0.3 * np.interp(t_(dur), np.linspace(0, dur, 60), r.uniform(0, 1, 60))
        y = chiado.copy()
        for t in np.cumsum(r.exponential(1 / 22, 60)):
            if t > dur - 0.02:
                break
            y = em(y, passa_banda(ruido(int(0.006 * SR), int(t * 1000)), 1500, 6000) * decai(0.006, 0.0015) * r.uniform(0.3, 0.9), t)
        y = em(y, passa_baixa(ruido(int(0.12 * SR), 193), 3000) * decai(0.12, 0.04, 0.003) * 0.7, 0)
        f = int(0.2 * SR)
        y[len(y) - f:] *= np.linspace(1, 0, f)
        return y[:int(dur * SR)]
    R['BombaAcesa'] = (pavio(), -6)

    # Ossos caindo: esqueleto desmontando.
    def ossos(semente):
        y = vazio(0.9)
        r = np.random.default_rng(semente)
        y = em(y, passa_banda(ruido(int(0.03 * SR), semente), 1000, 5000) * decai(0.03, 0.01), 0, 0.8)
        t, gap = 0.0, 0.035
        for k in range(11):
            f = r.uniform(900, 2400)
            osso = modal([f, f * 2.32, f * 4.25], [0.03, 0.018, 0.01], [1, 0.5, 0.25], 0.12, semente + k)
            osso[:int(0.003 * SR)] += passa_alta(ruido(int(0.003 * SR), semente + k), 2000) * 0.5
            y = em(y, osso, t, max(0.15, 1.0 - k * 0.08) * r.uniform(0.6, 1.0))
            t += gap * r.uniform(0.7, 1.3)
            gap *= 1.15
        return reverb(y, 0.35, 0.18)
    R['MorteOssos'] = (ossos(201), -4)

    # Feitico: brilho sombrio que cresce (necromante sumindo, invocando).
    def feitico(semente):
        dur = 1.0
        t = t_(dur)
        y = np.zeros(len(t))
        for k, f in enumerate((523, 659, 740, 932, 1175, 1480)):
            vib = 1 + 0.004 * np.sin(2 * np.pi * (5 + k * 0.7) * t)
            y += np.sin(2 * np.pi * np.cumsum(f * vib) / SR) * (0.6 + 0.4 * np.sin(2 * np.pi * (7 + k) * t + k))
        env = np.minimum(t / 0.25, 1) * np.exp(-np.maximum(t - 0.25, 0) / 0.25)
        y *= env * 0.12
        y += svf_banda(ruido(len(t), semente), 300, 3000, 2.0) * env * 0.5
        return reverb(y, 1.0, 0.35)
    R['Feitico'] = (feitico(211), -4)

    # Tiros dos inimigos, um tipo por estilo.
    def orbe(semente):
        dur = 0.22
        t = t_(dur)
        f = 640 * (300 / 640) ** (t / dur) * (1 + 0.08 * np.sin(2 * np.pi * 38 * t))
        x = np.sin(2 * np.pi * np.cumsum(f) / SR) * decai(dur, 0.07, 0.004) * 0.6
        x += svf_banda(ruido(len(t), semente), 1800, 600, 2.0) * decai(dur, 0.05, 0.003) * 0.5
        return reverb(x, 0.25, 0.15)
    R['TiroInimigo'] = (orbe(221), -6)

    def canhao(semente):
        y = vazio(0.5)
        y = em(y, baque(120, 50, 0.35, 0.08, 1.0, 0.9, semente), 0)
        y = em(y, passa_banda(ruido(int(0.05 * SR), semente + 1), 800, 3000) * decai(0.05, 0.01), 0, 0.5)
        return reverb(y, 0.4, 0.2)
    R['Canhao'] = (canhao(231), -5)

    def gosma(semente):
        dur = 0.2
        x = varredura_seno(dur, 260, 720) * decai(dur, 0.05, 0.004) * 0.7
        x += passa_baixa(ruido(int(dur * SR), semente), 900) * decai(dur, 0.03) * 0.5
        return reverb(x, 0.2, 0.1)
    R['Gosma'] = (gosma(241), -6)

    # ------------------------------ Freedoom (BSD-3)
    R['MorteInimigo'] = (freedoom('DSPODTH1'), -4)
    R['MorteInimigo_2'] = (freedoom('DSPODTH2'), -4)
    R['MorteInimigo_3'] = (freedoom('DSPODTH3'), -4)
    R['MorteDemonio'] = (freedoom('DSBGDTH1'), -4)
    R['MorteDemonio_2'] = (freedoom('DSBGDTH2'), -4)
    R['MorteFera'] = (freedoom('DSSGTDTH'), -4)
    R['MorteGosma'] = (freedoom('DSSLOP', dur=0.75), -4)
    R['DanoJogador'] = (freedoom('DSPLPAIN'), -3)
    R['MorteJogador'] = (freedoom('DSPLDETH'), -2)
    R['Mordida'] = (freedoom('DSSGTATK', dur=0.6), -4)
    R['TiroDeFogo'] = (freedoom('DSFIRSHT', dur=0.55), -6)
    R['Rugido'] = (freedoom('DSBRSSIT'), -1.5)
    R['Rugido_2'] = (freedoom('DSSGTSIT'), -1.5)
    R['Rugido_3'] = (freedoom('DSKNTSIT'), -1.5)
    exp = freedoom('DSBAREXP')
    exp = em(exp, varredura_seno(0.6, 70, 30) * decai(0.6, 0.2, 0.003) * 0.7, 0)
    R['Explosao'] = (exp, -1.5)
    chefe = freedoom('DSBRSDTH')
    chefe = em(chefe, pancada(152) * 0.6, 0)
    R['MorteChefe'] = (chefe, -1)

    # ------------------------------ vinhetas no soundfont (MIT)
    CEL, GLOCK, HARPA, CORO, METAIS, TROMPETE, TIMP, VOZES, SINOS = 8, 9, 46, 52, 61, 56, 47, 53, 14

    def harpa_glissando(inicio, notas, passo, vel=70):
        return [(inicio + i * passo, 1.2, n, vel, HARPA, 0) for i, n in enumerate(notas)]

    coracao = [(0.00, 0.6, p('C6'), 80, CEL, 0), (0.07, 0.6, p('E6'), 84, CEL, 0),
               (0.14, 0.8, p('G6'), 88, CEL, 0), (0.14, 0.8, p('C7'), 60, GLOCK, 0)]
    R['Coracao'] = (soundfont(coracao, 1.2), -4)

    cura = harpa_glissando(0, [p(n) for n in ('C5', 'E5', 'G5', 'C6', 'E6')], 0.05, 64) + \
        [(0.0, 0.9, p('C5'), 56, VOZES, 0), (0.0, 0.9, p('G5'), 50, VOZES, 0), (0.25, 0.7, p('E7'), 50, GLOCK, 0)]
    R['Cura'] = (soundfont(cura, 1.6), -5)

    item = harpa_glissando(0, [p(n) for n in ('G4', 'B4', 'D5', 'G5', 'B5', 'D6', 'G6')], 0.035, 74) + \
        [(0.0, 1.3, p(n), 82, CORO, 0, 100) for n in ('G4', 'B4', 'D5', 'G5')] + \
        [(0.3, 1.2, p('G6'), 70, GLOCK, 0), (0.3, 1.2, p('D6'), 60, GLOCK, 0)]
    R['Item'] = (soundfont(item, 2.2), -2)

    segredo = harpa_glissando(0, [p(n) for n in ('C5', 'D5', 'E5', 'F#5', 'G#5', 'A#5', 'C6', 'D6')], 0.045, 70) + \
        [(0.36, 1.2, p(n), 74, CEL, 0) for n in ('C6', 'E6', 'G#6')] + \
        [(0.36, 1.3, p('C5'), 58, VOZES, 0, 110), (0.36, 1.3, p('G#5'), 52, VOZES, 0, 110)]
    R['Segredo'] = (soundfont(segredo, 2.4), -2.5)

    vit = [(0.00, 0.12, p('F4'), 96, TROMPETE, 0), (0.13, 0.12, p('F4'), 96, TROMPETE, 0),
           (0.26, 0.12, p('F4'), 96, TROMPETE, 0), (0.40, 0.5, p('Bb4'), 104, TROMPETE, 0),
           (0.92, 0.2, p('D5'), 100, TROMPETE, 0), (1.12, 1.2, p('F5'), 110, TROMPETE, 0)]
    vit += [(1.12, 1.3, p(n), 92, METAIS, 0) for n in ('Bb3', 'D4', 'F4', 'Bb4')]
    vit += [(t, 0.1, p('Bb2'), 70 + int(t * 20), TIMP, 0) for t in np.arange(0.4, 1.1, 0.07)]
    vit += [(1.12, 1.0, p('Bb2'), 110, TIMP, 0)]
    R['Vitoria'] = (soundfont(vit, 3.2), -1.5)

    # Bau: rangido da tampa (atrito em pulsos) e o brilho do tesouro.
    def rangido(semente, dur=0.4):
        t = t_(dur)
        freq = 38 + 22 * np.sin(2 * np.pi * 1.6 * t) + np.random.default_rng(semente).normal(0, 2, len(t))
        fase = np.cumsum(freq) / SR
        pulsos = (np.diff(np.floor(fase), prepend=0) > 0).astype(float)
        x = signal.sosfilt(sos('bandpass', [400, 2200], 2), pulsos)
        x = svf_curva(x * 6, 900 + 500 * np.sin(2 * np.pi * 1.3 * t), 6) * sino(dur, 0.6)
        return x
    bau = em(vazio(1.6), rangido(251) * 0.9, 0)
    brilho = soundfont(harpa_glissando(0, [p(n) for n in ('C5', 'E5', 'G5', 'C6', 'E6', 'G6')], 0.04, 72) +
                       [(0.22, 1.0, p('C7'), 66, GLOCK, 0)], 1.6, 'bau')
    brilho = brilho / (np.abs(brilho).max() + 1e-9)
    bau = em(bau / (np.abs(bau).max() + 1e-9) * 0.7, brilho * 0.8, 0.32)
    bau = em(bau, baque(140, 80, 0.15, 0.04, 0.4, 0.3, 252), 0.38)
    R['BauAbre'] = (bau, -3)
    return R


if __name__ == '__main__':
    so = set(sys.argv[1:])
    for nome, (x, pico) in receitas().items():
        if not so or nome in so or nome.split('_')[0] in so:
            salvar(nome, x, pico)
