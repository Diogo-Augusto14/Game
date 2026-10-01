"""Motor de composicao: escreve MIDI, renderiza com fluidsynth e corta um loop sem emenda."""
import math
import os
import random
import re
import subprocess

import mido
import numpy as np
from scipy.io import wavfile

SF = '/usr/share/sounds/sf3/MuseScore_General_Lite.sf3'
TAXA = 44100

_NOTAS = {'C': 0, 'D': 2, 'E': 4, 'F': 5, 'G': 7, 'A': 9, 'B': 11}


def p(nome):
    """'A4' -> 69, 'C#5', 'Bb3'."""
    if isinstance(nome, int):
        return nome
    m = re.fullmatch(r'([A-G])([#b]*)(-?\d)', nome)
    if not m:
        raise ValueError(nome)
    n = _NOTAS[m.group(1)] + m.group(2).count('#') - m.group(2).count('b')
    return 12 * (int(m.group(3)) + 1) + n


_QUALIDADES = {
    '': [0, 4, 7], 'm': [0, 3, 7], 'dim': [0, 3, 6], 'aug': [0, 4, 8],
    '7': [0, 4, 7, 10], 'm7': [0, 3, 7, 10], 'maj7': [0, 4, 7, 11], 'm7b5': [0, 3, 6, 10],
    'dim7': [0, 3, 6, 9], 'sus4': [0, 5, 7], 'sus2': [0, 2, 7], '5': [0, 7], 'm6': [0, 3, 7, 9],
    'madd9': [0, 3, 7, 14], 'add9': [0, 4, 7, 14],
}


class Acorde:
    def __init__(self, simbolo):
        base = simbolo.split('/')[0]
        m = re.fullmatch(r'([A-G][#b]?)(.*)', base)
        self.raiz = p(m.group(1) + '0') % 12
        self.intervalos = _QUALIDADES[m.group(2)]
        self.baixo = p(simbolo.split('/')[1] + '0') % 12 if '/' in simbolo else self.raiz
        self.simbolo = simbolo

    def classes(self):
        return [(self.raiz + i) % 12 for i in self.intervalos]

    def tom(self, grau, oitava):
        """grau 0 = raiz, 1 = terca, 2 = quinta, 3 = setima...; grau acima do tamanho sobe oitava."""
        n = len(self.intervalos)
        o, g = divmod(grau, n)
        return 12 * (oitava + 1 + o) + self.raiz + self.intervalos[g]

    def raiz_em(self, oitava):
        return 12 * (oitava + 1) + self.raiz

    def baixo_em(self, oitava):
        return 12 * (oitava + 1) + self.baixo


class Trilha:
    def __init__(self, musica, nome, programa, banco=0, volume=100, pan=64, reverb=70, coro=0, bateria=False):
        self.musica = musica
        self.nome = nome
        self.programa = programa
        self.banco = banco
        self.volume = volume
        self.pan = pan
        self.reverb = reverb
        self.coro = coro
        self.bateria = bateria
        self.notas = []      # (inicio em batidas, duracao, nota, velocidade)
        self.controles = []  # (inicio, cc, valor)
        self.humano = 0.012  # desvio de tempo, em batidas
        self.swing = 0.0

    def nota(self, inicio, duracao, nota, vel=80):
        self.notas.append((inicio, duracao, p(nota), int(max(1, min(127, vel)))))

    def cc(self, inicio, cc, valor):
        self.controles.append((inicio, cc, int(valor)))

    def melodia(self, compasso, texto, vel=85, oitava=0, legato=0.95, acento=None):
        """texto: 'E5:1.5 D5:.5 C5:1 r:1' (duracao em batidas). Comeca no compasso dado (base 0)."""
        t = compasso * self.musica.batidas_por_compasso
        for k, barra in enumerate(texto.split('|')):
            soma = sum(float(x.split(':')[1]) for x in barra.split())
            if '|' in texto and abs(soma - self.musica.batidas_por_compasso) > 1e-6:
                raise ValueError(f'{self.musica.nome}/{self.nome}: compasso {compasso + k} soma {soma}')
        for i, tok in enumerate(t_ for t_ in texto.split() if t_ != '|'):
            nome, dur = tok.split(':')
            dur = float(dur)
            if nome != 'r':
                v = vel
                if acento and i in acento:
                    v += 12
                for n in nome.split('+'):
                    self.nota(t, dur * legato, p(n) + 12 * oitava, v)
            t += dur
        return t


class Musica:
    def __init__(self, nome, bpm, batidas_por_compasso, compassos, semente=1):
        self.nome = nome
        self.bpm = bpm
        self.batidas_por_compasso = batidas_por_compasso
        self.compassos = compassos
        self.trilhas = []
        self.rnd = random.Random(semente)
        self.canais_livres = [c for c in range(16) if c != 9]

    @property
    def batidas(self):
        return self.compassos * self.batidas_por_compasso

    @property
    def segundos(self):
        return self.batidas * 60.0 / self.bpm

    def trilha(self, nome, programa, **kw):
        t = Trilha(self, nome, programa, **kw)
        self.trilhas.append(t)
        return t

    def bateria(self, nome='bateria', kit=0, **kw):
        t = Trilha(self, nome, kit, banco=128, bateria=True, **kw)
        t.humano = 0.006
        self.trilhas.append(t)
        return t

    # ------------------------------------------------------------ midi
    def _midi(self, repeticoes, cauda_batidas):
        tpb = 480
        mid = mido.MidiFile(ticks_per_beat=tpb)
        meta = mido.MidiTrack()
        mid.tracks.append(meta)
        meta.append(mido.MetaMessage('set_tempo', tempo=mido.bpm2tempo(self.bpm)))
        livres = list(self.canais_livres)
        rnd = random.Random(99)
        for tr in self.trilhas:
            canal = 9 if tr.bateria else livres.pop(0)
            eventos = []
            if not tr.bateria:
                eventos.append((0, 0, mido.Message('control_change', channel=canal, control=0, value=tr.banco)))
                eventos.append((0, 0, mido.Message('control_change', channel=canal, control=32, value=0)))
            eventos.append((0, 1, mido.Message('program_change', channel=canal, program=tr.programa)))
            for cc, v in ((7, tr.volume), (10, tr.pan), (91, tr.reverb), (93, tr.coro), (11, 127)):
                eventos.append((0, 2, mido.Message('control_change', channel=canal, control=cc, value=v)))
            for r in range(repeticoes):
                base = r * self.batidas
                for (ini, cc, v) in tr.controles:
                    eventos.append((int((base + ini) * tpb), 2, mido.Message('control_change', channel=canal, control=cc, value=v)))
                # O humano e sorteado igual a cada repeticao: o loop fica identico.
                rr = random.Random(hash(tr.nome) & 0xffff)
                for (ini, dur, n, vel) in tr.notas:
                    if tr.swing and abs((ini * 2) % 2 - 1) < 1e-6:
                        ini += tr.swing * 0.5
                    d = rr.uniform(-tr.humano, tr.humano)
                    a = max(0, int((base + ini + d) * tpb))
                    b = max(a + 1, int((base + ini + d + dur) * tpb))
                    vv = max(1, min(127, vel + rr.randint(-4, 4)))
                    eventos.append((a, 4, mido.Message('note_on', channel=canal, note=n, velocity=vv)))
                    eventos.append((b, 3, mido.Message('note_off', channel=canal, note=n, velocity=0)))
            eventos.sort(key=lambda e: (e[0], e[1]))
            mt = mido.MidiTrack()
            mid.tracks.append(mt)
            agora = 0
            for (t, _, msg) in eventos:
                msg.time = t - agora
                agora = t
                mt.append(msg)
            fim = int((repeticoes * self.batidas + cauda_batidas) * tpb)
            mt.append(mido.MetaMessage('end_of_track', time=max(0, fim - agora)))
        return mid

    # ------------------------------------------------------------ render
    def renderizar(self, pasta, loop=True, alvo_rms_db=-20.0, sala=0.75, nivel_reverb=0.8, ganho=0.35):
        os.makedirs(pasta, exist_ok=True)
        cauda = int(math.ceil(6.0 * self.bpm / 60.0))
        rep = 3 if loop else 1
        mid = self._midi(rep, cauda)
        caminho_mid = os.path.join(pasta, self.nome + '.mid')
        caminho_wav = os.path.join(pasta, self.nome + '.bruto.wav')
        mid.save(caminho_mid)
        subprocess.run([
            'fluidsynth', '-ni', '-q', '-O', 'float', '-T', 'wav', '-F', caminho_wav, '-r', str(TAXA), '-g', str(ganho),
            '-o', f'synth.reverb.room-size={sala}', '-o', 'synth.reverb.damp=0.35',
            '-o', 'synth.reverb.width=1.0', '-o', f'synth.reverb.level={nivel_reverb}',
            '-o', 'synth.chorus.active=1', '-o', 'synth.polyphony=512', SF, caminho_mid,
        ], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        taxa, a = wavfile.read(caminho_wav)
        a = a.astype(np.float64)
        n = int(round(self.segundos * TAXA))
        if loop:
            # Pega a 2a repeticao (ja com a cauda da 1a por baixo) e funde o comeco com o
            # que vem logo depois dela (comeco da 3a): a emenda do loop fica continua.
            k = 2048
            w = np.linspace(0.0, 1.0, k)[:, None]
            seg = a[n:2 * n].copy()
            seg[:k] = a[2 * n:2 * n + k] * (1 - w) + a[n:n + k] * w
            a = seg
        else:
            # sem loop: ate o fim da cauda, cortando o silencio do final
            vivo = np.where(np.abs(a).max(axis=1) > 1e-4)[0]
            a = a[: (vivo[-1] + 1) if len(vivo) else len(a)]
            fade = min(len(a), int(0.05 * TAXA))
            a[-fade:] *= np.linspace(1, 0, fade)[:, None]
        rms = np.sqrt(np.mean(a ** 2))
        a *= 10 ** (alvo_rms_db / 20) / max(rms, 1e-9)
        # limitador suave: segura picos acima de -1 dB
        teto = 10 ** (-1 / 20)
        pico = np.abs(a).max()
        if pico > teto:
            a = np.tanh(a / teto) * teto if pico < 3 * teto else a / pico * teto
        saida_wav = os.path.join(pasta, self.nome + '.wav')
        wavfile.write(saida_wav, TAXA, (np.clip(a, -1, 1) * 32767).astype(np.int16))
        os.remove(caminho_wav)
        return saida_wav, len(a) / TAXA, 20 * np.log10(np.abs(a).max() + 1e-12)


# ------------------------------------------------------------------ ajudantes de arranjo
def voz_mais_proxima(acorde, anterior, faixa=(55, 76), vozes=4):
    """Escolhe as notas do acorde (vozes) mais perto das anteriores (conducao de vozes)."""
    classes = acorde.classes()
    candidatos = [n for n in range(faixa[0], faixa[1] + 1) if n % 12 in classes]
    if anterior is None:
        centro = (faixa[0] + faixa[1]) // 2
        escolhidas = []
        for c in classes[:vozes]:
            opcoes = [n for n in candidatos if n % 12 == c]
            escolhidas.append(min(opcoes, key=lambda n: abs(n - centro)))
        while len(escolhidas) < vozes:
            escolhidas.append(escolhidas[0] + 12)
        return sorted(escolhidas)
    escolhidas = []
    usadas = set()
    for a in sorted(anterior):
        opcoes = sorted(candidatos, key=lambda n: (abs(n - a), n))
        for o in opcoes:
            if o not in escolhidas:
                escolhidas.append(o)
                usadas.add(o % 12)
                break
    # garante que todas as notas do acorde (ate 3) aparecem
    for c in classes[:3]:
        if c not in {n % 12 for n in escolhidas}:
            # troca a nota repetida mais proxima
            from collections import Counter
            cont = Counter(n % 12 for n in escolhidas)
            repetida = [n for n in escolhidas if cont[n % 12] > 1]
            if repetida:
                alvo = repetida[0]
                opcoes = [n for n in candidatos if n % 12 == c]
                novo = min(opcoes, key=lambda n: abs(n - alvo))
                escolhidas[escolhidas.index(alvo)] = novo
    return sorted(escolhidas)


def pad(trilha, acordes, compasso0=0, por_compasso=1, faixa=(55, 76), vozes=4, vel=60, legato=1.0):
    """Acordes sustentados com conducao de vozes. acordes: lista de simbolos, um por (compasso/por_compasso)."""
    m = trilha.musica
    dur = m.batidas_por_compasso / por_compasso
    anterior = None
    for i, s in enumerate(acordes):
        if s in ('-', None):
            continue
        a = Acorde(s)
        notas = voz_mais_proxima(a, anterior, faixa, vozes)
        anterior = notas
        ini = compasso0 * m.batidas_por_compasso + i * dur
        for n in notas:
            trilha.nota(ini, dur * legato, n, vel)


def baixo(trilha, acordes, padrao, compasso0=0, oitava=2, vel=80):
    """padrao: lista de (batida, duracao, grau) por compasso; grau 'b' = baixo do acorde, 0/1/2 = graus, '8' oitava."""
    m = trilha.musica
    for i, s in enumerate(acordes):
        if s in ('-', None):
            continue
        a = Acorde(s)
        ini = (compasso0 + i) * m.batidas_por_compasso
        for (bt, dur, g, *resto) in padrao:
            v = vel + (resto[0] if resto else 0)
            if g == 'b':
                n = a.baixo_em(oitava)
            elif g == '8':
                n = a.baixo_em(oitava + 1)
            else:
                n = a.tom(g, oitava)
            trilha.nota(ini + bt, dur, n, v)


def arpejo(trilha, acordes, graus, passo, compasso0=0, oitava=3, vel=70, legato=1.6, acentos=None):
    """Arpejo: graus do acorde (0 raiz, 1 terca, 2 quinta, 3 = raiz uma oitava acima...) a cada `passo` batidas."""
    m = trilha.musica
    por_compasso = int(round(m.batidas_por_compasso / passo))
    for i, s in enumerate(acordes):
        if s in ('-', None):
            continue
        a = Acorde(s)
        tri = [x for x in a.intervalos if x < 12][:3]
        ini = (compasso0 + i) * m.batidas_por_compasso
        for k in range(por_compasso):
            g = graus[k % len(graus)]
            o, r = divmod(g, len(tri))
            n = 12 * (oitava + 1 + o) + a.raiz + tri[r]
            v = vel + (acentos[k % len(acentos)] if acentos else 0)
            trilha.nota(ini + k * passo, passo * legato, n, v)
