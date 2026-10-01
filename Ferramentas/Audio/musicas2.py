"""Musicas dos outros mundos, chefes, loja, menu, vitoria e fim de jogo."""
from motor import Musica, pad, baixo, arpejo, Acorde, p
from musicas import *  # noqa: constantes de instrumentos


def catacumbas():
    # 6/8 lento (3 batidas de seminima por compasso), Re dorico: harpa, flauta de pa, gotas.
    m = Musica('Catacumbas', bpm=100, batidas_por_compasso=3, compassos=40, semente=2)
    A = ['Dm', 'Dm', 'G/D', 'Dm', 'Bb', 'C', 'Dm', 'A']
    B = ['Gm', 'Dm', 'Bb', 'F', 'Gm', 'Dm', 'C', 'A']
    tudo = A + A + B + A + B

    harpa = m.trilha('harpa', HARPA, volume=100, pan=50, reverb=95)
    arpejo(harpa, tudo, [0, 2, 3, 4, 5, 4], 0.5, oitava=2, vel=64, legato=2.5, acentos=[12, 0, 4, 0, 6, 0])

    drone = m.trilha('drone', CORDAS_LENTAS, banco=BAIXOS, volume=84, reverb=80)
    baixo(drone, tudo, [(0, 3, 'b')], oitava=2, vel=60)

    nevoa = m.trilha('nevoa', PAD_ARCO, volume=70, pan=72, reverb=110)
    pad(nevoa, tudo, faixa=(50, 67), vozes=3, vel=54)

    gotas = m.trilha('gotas', KALIMBA, volume=64, pan=96, reverb=127)
    escala = [p(n) for n in ('D6', 'F6', 'A6', 'C7', 'E6', 'G6', 'A5')]
    for c in range(40):
        if m.rnd.random() < 0.7:
            t = c * 3 + m.rnd.choice([0.5, 1, 1.5, 2, 2.5])
            gotas.nota(t, 0.4, m.rnd.choice(escala), m.rnd.randint(34, 54))

    sinos = m.trilha('sinos', SINOS, volume=70, pan=40, reverb=120)
    for c in (0, 4, 32, 36):
        sinos.nota(c * 3, 3, Acorde(tudo[c]).raiz_em(4), 58)

    mel_a = ('D5:1.5 E5:.5 F5:1 | A5:1.5 G5:.5 F5:.5 E5:.5 | D5:1 B4:.5 D5:1.5 | A4:3 | '
             'F5:1.5 E5:.5 D5:1 | E5:1.5 G5:1.5 | F5:1 E5:.5 D5:1.5 | C#5:1.5 E5:1.5')
    mel_b = ('G5:1.5 Bb5:1.5 | A5:1 F5:.5 D5:1.5 | F5:1.5 D5:1.5 | A4:1 C5:.5 F5:1.5 | '
             'Bb5:1.5 A5:.5 G5:1 | F5:1.5 E5:.5 D5:1 | E5:1.5 G5:1.5 | E5:1 C#5:.5 A4:1.5')
    pa = m.trilha('flauta de pa', FLAUTA_PA, volume=100, pan=64, reverb=100)
    pa.melodia(8, mel_a, vel=84)
    flauta = m.trilha('flauta', FLAUTA, volume=92, pan=70, reverb=100)
    flauta.melodia(16, mel_b, vel=80)
    cello = m.trilha('cello', CORDAS_LENTAS, banco=CELLOS, volume=100, pan=56, reverb=95)
    cello.melodia(24, mel_a, vel=86, oitava=-1, legato=1.0)

    taiko = m.trilha('taiko', TAIKO, volume=82, reverb=90)
    for c in range(16, 32):
        taiko.nota(c * 3, 1, p('D2'), 70 if c % 2 == 0 else 52)
        if c % 4 == 3:
            taiko.nota(c * 3 + 2, 1, p('D2'), 50)
            taiko.nota(c * 3 + 2.5, 1, p('D2'), 58)
    return m


def cripta():
    # Valsa macabra em Mi menor: orgao, cravo, pizzicato, violino e xilofone de ossos.
    m = Musica('Cripta', bpm=138, batidas_por_compasso=3, compassos=48, semente=3)
    A = ['Em', 'Em', 'Am', 'B7', 'Em', 'C', 'Am6', 'B7']
    B = ['Em', 'B7', 'Em', 'B7', 'Am', 'Em', 'B7', 'Em']
    tudo = A + A + B + A + B + A

    orgao = m.trilha('orgao', ORGAO_IGREJA, volume=62, pan=60, reverb=110)
    pad(orgao, tudo, faixa=(52, 67), vozes=3, vel=60)

    pizz = m.trilha('pizz', 45, banco=BAIXOS, volume=100, pan=54, reverb=70)
    baixo(pizz, tudo, [(0, 1, 'b')], oitava=2, vel=92)

    cravo = m.trilha('cravo', CRAVO, volume=74, pan=82, reverb=80)
    for i, s in enumerate(tudo):
        a = Acorde(s)
        for bt in (1, 2):
            for g in range(3):
                cravo.nota(i * 3 + bt, 0.45, a.tom(g, 4), 58 if bt == 1 else 50)

    sinos = m.trilha('sinos', SINOS, volume=72, pan=40, reverb=120)
    for c in range(0, 48, 8):
        sinos.nota(c * 3, 3, p('E4'), 64)
        sinos.nota(c * 3 + 1.5, 3, p('B3'), 50)

    mel_a = ('B4:1 E5:1 G5:1 | F#5:1.5 E5:.5 D#5:1 | E5:1 A5:1 C6:1 | B5:2 A5:.5 F#5:.5 | '
             'G5:1 E5:1 B4:1 | C5:1 E5:1 G5:1 | F#5:1.5 E5:.5 C5:1 | B4:2 D#5:1')
    mel_b = ('E5:.5 r:.5 B4:.5 r:.5 G4:.5 B4:.5 | D#5:.5 r:.5 A4:.5 r:.5 F#4:.5 A4:.5 | '
             'E5:.5 G5:.5 B5:.5 G5:.5 E5:.5 B4:.5 | D#5:1 F#5:1 A5:1 | '
             'C6:.5 B5:.5 A5:.5 E5:.5 C5:.5 A4:.5 | B4:.5 C5:.5 B4:.5 G4:.5 E4:1 | '
             'F#4:.5 A4:.5 D#5:.5 F#5:.5 A5:1 | G5:1 E5:2')
    violino = m.trilha('violino', VIOLINO, volume=100, pan=64, reverb=95)
    violino.melodia(8, mel_a, vel=88, legato=1.0)
    violino.melodia(24, mel_a, vel=92, legato=1.0)
    xilo = m.trilha('xilofone', XILOFONE, volume=96, pan=74, reverb=85)
    xilo.melodia(16, mel_b, vel=86, legato=0.6)
    xilo.melodia(32, mel_b, vel=90, legato=0.6)
    celesta = m.trilha('celesta', CELESTA, volume=66, pan=90, reverb=110)
    celesta.melodia(32, mel_b, vel=60, oitava=1, legato=0.6)

    coro = m.trilha('coro', CORAL, volume=78, pan=70, reverb=110)
    pad(coro, tudo[24:32], compasso0=24, faixa=(57, 72), vozes=3, vel=60)
    pad(coro, tudo[40:48], compasso0=40, faixa=(57, 72), vozes=3, vel=58)
    orgao_mel = m.trilha('orgao melodia', ORGAO_IGREJA, volume=70, pan=50, reverb=110)
    orgao_mel.melodia(40, mel_a, vel=74, oitava=-1, legato=1.0)
    return m


def abismo():
    # Do menor frigio, 3+3+2: ostinato de cordas graves, metais, coro, taikos e sinos.
    m = Musica('Abismo', bpm=116, batidas_por_compasso=4, compassos=32, semente=4)
    A = ['Cm', 'Cm', 'Db', 'Cm', 'Ab', 'Bb', 'Db', 'G']
    B = ['Fm', 'Cm', 'Fm', 'G', 'Ab', 'Db', 'Bb', 'G']
    tudo = A[:4] + A + B + A + A[4:]

    acentos = [30, -8, -8, 26, -8, -8, 22, -4]
    celli = m.trilha('celli', CORDAS, banco=CELLOS, volume=96, pan=52, reverb=70)
    arpejo(celli, tudo, [0], 0.5, oitava=2, vel=72, legato=0.8, acentos=acentos)
    baixos = m.trilha('baixos', CORDAS, banco=BAIXOS, volume=96, pan=60, reverb=70)
    arpejo(baixos, tudo, [0], 0.5, oitava=1, vel=72, legato=0.8, acentos=acentos)

    coro = m.trilha('coro', CORAL, volume=80, pan=74, reverb=110)
    pad(coro, tudo, faixa=(55, 72), vozes=3, vel=58)

    mel_a = ('G4:3 Ab4:.5 G4:.5 | Eb4:2 C4:2 | F4:3 Eb4:.5 F4:.5 | G4:4 | '
             'Ab4:2 C5:2 | Bb4:2 D5:1 Bb4:1 | Ab4:1.5 G4:.5 F4:2 | B3:2 D4:2')
    mel_b = ('Ab4:2 G4:1 F4:1 | G4:2 Eb4:2 | C5:2 Bb4:1 Ab4:1 | B4:4 | '
             'C5:2 Eb5:2 | F5:2 Db5:2 | D5:2 F5:1 D5:1 | B4:2 D5:1 B4:1')
    metais = m.trilha('metais', METAIS, volume=100, pan=60, reverb=90)
    metais.melodia(4, mel_a, vel=92, legato=0.98)
    metais.melodia(20, mel_a, vel=100, oitava=1, legato=0.98)
    trombone = m.trilha('trombones', TROMBONE, volume=94, pan=44, reverb=90)
    trombone.melodia(4, mel_a, vel=88, oitava=-1, legato=0.98)
    trombone.melodia(20, mel_a, vel=94, legato=0.98)
    trompas = m.trilha('trompas', TROMPA, volume=100, pan=80, reverb=95)
    trompas.melodia(12, mel_b, vel=96, legato=0.98)

    sinos = m.trilha('sinos', SINOS, volume=70, pan=30, reverb=120)
    for c in range(12, 20):
        sinos.nota(c * 4, 4, Acorde(tudo[c]).raiz_em(4), 62)

    taiko = m.trilha('taiko', TAIKO, volume=96, reverb=85)
    timp = m.trilha('timpano', TIMPANO, volume=96, reverb=85)
    bat = m.bateria(kit=KIT_ORQUESTRA, volume=100, reverb=85)
    for c in range(32):
        t = c * 4
        taiko.nota(t, 1, p('C2'), 86)
        taiko.nota(t + 2.5, 1, p('C2'), 70)
        if c >= 4:
            bat.nota(t, 0.5, 36, 84)
            bat.nota(t + 1.5, 0.5, 36, 64)
            bat.nota(t + 3, 0.5, 36, 74)
            timp.nota(t, 1, Acorde(tudo[c]).baixo_em(2), 90)
        if 12 <= c < 20 or c >= 28:
            bat.nota(t + 1, 0.4, 38, 62)
            bat.nota(t + 3, 0.4, 38, 72)
        if c % 8 == 7:
            for k in range(8):
                bat.nota(t + 2 + k * 0.25, 0.25, 38, 40 + k * 8)
    for c in (4, 12, 20, 28):
        bat.nota(c * 4, 2, 57, 92)
    return m


def chefe():
    m = Musica('Chefe', bpm=150, batidas_por_compasso=4, compassos=32, semente=5)
    A = ['Dm', 'Dm', 'Bb', 'C', 'Dm', 'Dm', 'Gm', 'A']
    B = ['Gm', 'Dm', 'Bb', 'A', 'Gm', 'Dm', 'Eb', 'A']
    tudo = ['Dm', 'Dm', 'Dm', 'A'] + A + B + A + ['Bb', 'C', 'Bb', 'A']

    celli = m.trilha('celli', CORDAS, banco=CELLOS, volume=96, pan=50, reverb=65)
    arpejo(celli, tudo, [0, 0, 1, 0, 2, 0, 1, 2], 0.5, oitava=2, vel=78, legato=0.7, acentos=[20, -6, 0, -6, 10, -6, 0, -4])
    baixos = m.trilha('baixos', CORDAS, banco=BAIXOS, volume=90, reverb=65)
    arpejo(baixos, tudo, [0], 0.5, oitava=1, vel=76, legato=0.7, acentos=[20, -6, 0, -6, 10, -6, 0, -4])
    violinos = m.trilha('violinos', CORDAS, banco=VIOLINOS, volume=78, pan=70, reverb=75)
    arpejo(violinos, tudo, [3, 4, 5, 4], 0.25, oitava=3, vel=56, legato=0.8, acentos=[12, 0, 4, 0])

    mel_a = ('D4:1 A4:2 G4:.5 F4:.5 | E4:.5 F4:.5 D4:3 | F4:1 Bb4:2 A4:.5 G4:.5 | A4:1 G4:1 E4:2 | '
             'D4:1 A4:2 Bb4:.5 C5:.5 | D5:3 C5:.5 A4:.5 | Bb4:1.5 A4:.5 G4:1 Bb4:1 | A4:2 C#5:2')
    mel_b = ('G5:2 Bb5:2 | A5:2 F5:2 | F5:1 G5:1 A5:1 Bb5:1 | A5:2 E5:2 | '
             'D6:2 Bb5:1 G5:1 | A5:2 F5:1 D5:1 | G5:2 Bb5:1 Eb6:1 | C#6:2 A5:1 E5:1')
    metais = m.trilha('metais', METAIS, volume=100, pan=60, reverb=85)
    metais.melodia(4, mel_a, vel=98, legato=0.95)
    metais.melodia(20, mel_a, vel=104, legato=0.95)
    trombone = m.trilha('trombones', TROMBONE, volume=96, pan=44, reverb=85)
    trombone.melodia(4, mel_a, vel=90, oitava=-1, legato=0.95)
    trombone.melodia(20, mel_a, vel=96, oitava=-1, legato=0.95)
    trem = m.trilha('tremolo', TREMOLO, banco=VIOLINOS, volume=100, pan=70, reverb=90)
    trem.melodia(12, mel_b, vel=96, legato=1.0)
    lentos = m.trilha('violinos lentos', CORDAS_LENTAS, banco=VIOLINOS, volume=86, pan=76, reverb=95)
    lentos.melodia(20, mel_a, vel=82, oitava=1, legato=1.0)

    coro = m.trilha('coro', CORAL, volume=86, pan=66, reverb=105)
    pad(coro, tudo[12:28], compasso0=12, faixa=(55, 72), vozes=3, vel=66)

    timp = m.trilha('timpano', TIMPANO, volume=100, reverb=80)
    bat = m.bateria(kit=KIT_ORQUESTRA, volume=100, reverb=80)
    for c in range(32):
        t = c * 4
        timp.nota(t, 1, Acorde(tudo[c]).baixo_em(2), 96)
        if c < 4:
            timp.nota(t + 2, 1, Acorde(tudo[c]).baixo_em(2), 70)
            continue
        bat.nota(t, 0.5, 36, 90)
        bat.nota(t + 2, 0.5, 36, 80)
        bat.nota(t + 2.5, 0.5, 36, 62)
        bat.nota(t + 1, 0.4, 38, 74)
        bat.nota(t + 3, 0.4, 38, 80)
        if c % 8 == 3:
            for k in range(8):
                bat.nota(t + 2 + k * 0.25, 0.25, 38, 44 + k * 8)
    for c in (4, 12, 20, 28):
        bat.nota(c * 4, 2, 57, 96)
    return m


def chefe_final():
    m = Musica('ChefeFinal', bpm=160, batidas_por_compasso=4, compassos=32, semente=6)
    A = ['Bm', 'G', 'Em', 'F#', 'Bm', 'C', 'Em', 'F#7']
    B = ['G', 'A', 'Bm', 'Bm', 'G', 'A', 'F#', 'F#']
    tudo = A + B + A + B

    mel_a = ('B4:.5 C#5:.5 D5:.5 B4:.5 F#5:1 D5:1 | G5:1 F#5:.5 E5:.5 D5:1 B4:1 | '
             'E5:.5 F#5:.5 G5:.5 E5:.5 B5:1 G5:1 | A#5:1 F#5:1 C#5:1 A#4:1 | '
             'D5:.5 C#5:.5 B4:.5 C#5:.5 D5:1 F#5:1 | G5:1 E5:1 C5:2 | B4:1 E5:1 G5:1 B5:1 | A#5:1 C#6:1 E6:1 A#5:1')
    mel_b = ('D5:2 B4:2 | C#5:2 E5:2 | F#5:3 E5:.5 D5:.5 | B4:4 | '
             'G5:2 F#5:1 E5:1 | E5:2 A5:2 | F#5:2 E5:1 C#5:1 | A#4:2 C#5:2')

    orgao = m.trilha('orgao', ORGAO_IGREJA, volume=90, pan=56, reverb=110)
    orgao.melodia(0, mel_a, vel=90, legato=0.92)
    orgao.melodia(16, mel_a, vel=94, legato=0.92)
    acordes_orgao = m.trilha('orgao acordes', ORGAO_IGREJA, volume=70, pan=70, reverb=110)
    pad(acordes_orgao, A, faixa=(47, 62), vozes=3, vel=72)
    pad(acordes_orgao, A, compasso0=16, faixa=(47, 62), vozes=3, vel=72)

    metais = m.trilha('metais', METAIS, volume=100, pan=62, reverb=90)
    metais.melodia(8, mel_b, vel=100, legato=0.97)
    trombone = m.trilha('trombones', TROMBONE, volume=96, pan=40, reverb=90)
    trombone.melodia(8, mel_b, vel=92, oitava=-1, legato=0.97)
    baixo(trombone, A, [(0, 1.9, 'b'), (2, 1.9, 'b')], compasso0=16, oitava=2, vel=86)
    violinos = m.trilha('violinos', CORDAS_LENTAS, banco=VIOLINOS, volume=96, pan=72, reverb=95)
    violinos.melodia(24, mel_b, vel=94, oitava=1, legato=1.0)

    coro = m.trilha('coro', CORAL, volume=92, pan=66, reverb=110)
    pad(coro, B, compasso0=8, faixa=(55, 74), vozes=4, vel=74)
    pad(coro, B, compasso0=24, faixa=(55, 74), vozes=4, vel=78)

    acentos = [24, -6, 0, -6, 16, -6, 0, -4]
    celli = m.trilha('celli', CORDAS, banco=CELLOS, volume=92, pan=50, reverb=70)
    arpejo(celli, tudo, [0, 0, 0, 0, 0, 0, 2, 1], 0.5, oitava=2, vel=76, legato=0.7, acentos=acentos)
    baixos = m.trilha('baixos', CORDAS, banco=BAIXOS, volume=90, reverb=70)
    arpejo(baixos, tudo, [0], 0.5, oitava=1, vel=76, legato=0.7, acentos=acentos)

    sinos = m.trilha('sinos', SINOS, volume=74, pan=30, reverb=120)
    for c in list(range(0, 8, 2)) + list(range(16, 24, 2)):
        sinos.nota(c * 4, 4, Acorde(tudo[c]).raiz_em(4), 66)

    timp = m.trilha('timpano', TIMPANO, volume=100, reverb=80)
    bat = m.bateria(kit=KIT_ORQUESTRA, volume=100, reverb=80)
    for c in range(32):
        t = c * 4
        timp.nota(t, 1, Acorde(tudo[c]).baixo_em(2), 98)
        timp.nota(t + 2, 1, Acorde(tudo[c]).baixo_em(2), 80)
        for k in range(4):
            bat.nota(t + k, 0.5, 36, 86 if k % 2 == 0 else 66)
        bat.nota(t + 1, 0.4, 38, 78)
        bat.nota(t + 3, 0.4, 38, 84)
        if c % 8 == 7:
            for k in range(8):
                bat.nota(t + 2 + k * 0.25, 0.25, 38, 48 + k * 8)
    for c in (0, 8, 16, 24):
        bat.nota(c * 4, 2, 57, 100)
    return m


def menu():
    m = Musica('Menu', bpm=84, batidas_por_compasso=3, compassos=24, semente=7)
    tudo = ['Am', 'Am', 'F', 'F', 'Dm', 'Dm', 'E', 'E',
            'Am', 'Am', 'C', 'G', 'F', 'Dm', 'E', 'E',
            'Am', 'F', 'Dm', 'E', 'Am', 'F', 'Dm', 'E']
    harpa = m.trilha('harpa', HARPA, volume=96, pan=50, reverb=100)
    arpejo(harpa, tudo, [0, 2, 3, 4, 3, 2], 0.5, oitava=2, vel=56, legato=2.5, acentos=[10, 0, 2, 0, 2, 0])
    coro = m.trilha('coro', PAD_CORAL, volume=80, pan=70, reverb=110)
    pad(coro, tudo, faixa=(55, 70), vozes=3, vel=56)
    baixos = m.trilha('baixos', CORDAS_LENTAS, banco=BAIXOS, volume=72, reverb=80)
    baixo(baixos, tudo, [(0, 3, 'b')], oitava=1, vel=56)
    caixinha = m.trilha('caixinha', CAIXINHA, volume=96, pan=64, reverb=110)
    caixinha.melodia(0, 'E5:1 A5:1 C6:1 | B5:2 A5:1 | A5:1 C6:1 F6:1 | E6:2 C6:1 | D6:1 C6:1 A5:1 | F5:2 A5:1 | '
                        'G#5:1 B5:1 E6:1 | D6:2 B5:1 | C6:1 B5:1 A5:1 | E5:3 | G5:1 E5:1 C5:1 | D5:2 B4:1 | '
                        'A5:1 G5:1 F5:1 | D5:2 F5:1 | E5:1 G#5:1 B5:1 | E5:3', vel=78, legato=1.4)
    celesta = m.trilha('celesta', CELESTA, volume=74, pan=84, reverb=115)
    celesta.melodia(16, 'E5:3 | C5:3 | F5:3 | E5:1.5 G#5:1.5 | A5:3 | A5:3 | F5:2 D5:1 | B4:3', vel=60, legato=1.0)
    return m


def loja():
    m = Musica('Loja', bpm=112, batidas_por_compasso=4, compassos=24, semente=8)
    frase = ['Dm7', 'G7', 'Dm7', 'G7', 'Bb', 'A7', 'Dm', 'A7']
    tudo = frase * 3
    pizz = m.trilha('pizz', PIZZ, volume=100, pan=50, reverb=60)
    baixo(pizz, tudo, [(0, 0.9, 'b', 10), (1, 0.9, 2), (2, 0.9, '8'), (3, 0.9, 1)], oitava=2, vel=82)
    marimba = m.trilha('marimba', MARIMBA, volume=80, pan=80, reverb=70)
    for i, s in enumerate(tudo):
        a = Acorde(s)
        for bt in (1, 3):
            for g in range(3):
                marimba.nota(i * 4 + bt, 0.4, a.tom(g, 4), 60)
    fagote_mel = ('D4:.5 F4:.5 A4:.5 C5:.5 B4:1 A4:1 | G4:1 F4:.5 G4:.5 B4:1 r:1 | '
                  'A4:.5 G4:.5 F4:.5 E4:.5 D4:1 F4:1 | G4:2 r:2 | '
                  'F3:.5 Bb3:.5 D4:.5 F4:.5 D4:1 Bb3:1 | C#4:1 E4:1 G4:1 E4:1 | '
                  'F4:.5 E4:.5 D4:.5 C#4:.5 D4:1 A3:1 | E4:1 G4:1 A4:2')
    clarinete_mel = ('D5:.5 F5:.5 A5:.5 C6:.5 B5:1 A5:1 | G5:1 F5:.5 G5:.5 B5:1 r:1 | '
                     'A5:.5 G5:.5 F5:.5 E5:.5 D5:1 F5:1 | G5:2 r:2 | '
                     'F4:.5 Bb4:.5 D5:.5 F5:.5 D5:1 Bb4:1 | C#5:1 E5:1 G5:1 E5:1 | '
                     'F5:.5 E5:.5 D5:.5 C#5:.5 D5:1 A4:1 | E5:1 G5:1 A5:2')
    fagote = m.trilha('fagote', FAGOTE, volume=104, pan=58, reverb=70)
    fagote.swing = 0.33
    fagote.melodia(0, fagote_mel, vel=90, legato=0.85)
    fagote.melodia(16, fagote_mel, vel=86, legato=0.85)
    clarinete = m.trilha('clarinete', CLARINETE, volume=92, pan=72, reverb=75)
    clarinete.swing = 0.33
    clarinete.melodia(8, clarinete_mel, vel=86, legato=0.85)
    clarinete.melodia(16, clarinete_mel, vel=76, legato=0.85)
    bat = m.bateria(kit=KIT_PADRAO, volume=84, reverb=55)
    bat.swing = 0.33
    for c in range(24):
        t = c * 4
        bat.nota(t, 0.4, 36, 60)
        bat.nota(t + 2, 0.4, 36, 50)
        bat.nota(t + 1, 0.3, 37, 58)
        bat.nota(t + 3, 0.3, 37, 62)
        for k in range(8):
            bat.nota(t + k * 0.5, 0.2, 42, 40 if k % 2 else 52)
    return m


def vitoria():
    m = Musica('Vitoria', bpm=104, batidas_por_compasso=4, compassos=16, semente=9)
    tudo = ['Bb', 'F/A', 'Gm', 'Eb', 'Bb', 'F', 'Eb', 'F', 'Gm', 'Eb', 'Bb', 'F', 'Eb', 'F', 'Bb', 'Bb']
    trompete = m.trilha('trompete', TROMPETE, volume=104, pan=62, reverb=90)
    trompete.melodia(0, 'F4:.5 Bb4:.5 D5:1 F5:2 | Eb5:1 D5:.5 C5:.5 C5:2 | D5:1 Bb4:1 G4:1 Bb4:1 | C5:3 Bb4:.5 C5:.5 | '
                        'D5:1 F5:1 Bb5:2 | A5:1 G5:.5 F5:.5 C5:2 | G5:1.5 F5:.5 Eb5:1 G5:1 | F5:4 | '
                        'G5:1 F5:1 D5:2 | Eb5:1 D5:1 Bb4:2 | D5:1 F5:1 Bb5:2 | A5:2 C6:2 | '
                        'Bb5:1 G5:1 Eb5:1 G5:1 | F5:1 A5:1 C6:2 | Bb5:4 | r:2 D5:.5 Eb5:.5 F5:1', vel=96, legato=0.95)
    trompas = m.trilha('trompas', TROMPA, volume=90, pan=44, reverb=95)
    pad(trompas, tudo, faixa=(53, 67), vozes=3, vel=70)
    cordas = m.trilha('cordas', CORDAS, banco=VIOLINOS, volume=80, pan=76, reverb=90)
    arpejo(cordas, tudo, [0, 1, 2, 3, 2, 1, 2, 1], 0.5, oitava=4, vel=60, legato=1.0)
    baixos = m.trilha('baixos', CORDAS_LENTAS, banco=BAIXOS, volume=86, reverb=80)
    baixo(baixos, tudo, [(0, 2, 'b'), (2, 2, 'b')], oitava=1, vel=72)
    coro = m.trilha('coro', CORAL, volume=80, pan=70, reverb=105)
    pad(coro, tudo[8:], compasso0=8, faixa=(58, 74), vozes=3, vel=62)
    timp = m.trilha('timpano', TIMPANO, volume=96, reverb=85)
    bat = m.bateria(kit=KIT_ORQUESTRA, volume=96, reverb=85)
    for c in range(16):
        timp.nota(c * 4, 1, Acorde(tudo[c]).baixo_em(2), 88)
        bat.nota(c * 4 + 2, 0.4, 38, 56)
        bat.nota(c * 4 + 2.5, 0.4, 38, 46)
    for c in (0, 8):
        bat.nota(c * 4, 2, 57, 88)
    return m


def fim_de_jogo():
    m = Musica('FimDeJogo', bpm=66, batidas_por_compasso=4, compassos=8, semente=10)
    tudo = ['Dm', 'Bb', 'Gm', 'A', 'Dm', 'Gm', 'A', 'Dm']
    piano = m.trilha('piano', PIANO, volume=104, pan=60, reverb=95)
    piano.melodia(0, 'A4:2 F4:1 D4:1 | F4:2 D4:1 Bb3:1 | D4:1.5 E4:.5 F4:1 G4:1 | E4:2 C#4:2 | '
                     'F4:2 E4:1 D4:1 | Bb4:2 A4:1 G4:1 | E4:2 C#4:1 E4:1 | D4:4', vel=74, legato=1.2)
    arpejo(piano, tudo, [0, 2, 3, 2], 1, oitava=2, vel=48, legato=2.0)
    cordas = m.trilha('cordas', CORDAS_LENTAS, banco=VIOLINOS, volume=80, pan=74, reverb=110)
    pad(cordas, tudo, faixa=(57, 72), vozes=3, vel=52)
    cellos = m.trilha('cellos', CORDAS_LENTAS, banco=CELLOS, volume=86, pan=50, reverb=100)
    baixo(cellos, tudo, [(0, 4, 'b')], oitava=2, vel=62)
    coro = m.trilha('coro', VOZES, volume=70, pan=66, reverb=120)
    pad(coro, tudo, faixa=(60, 74), vozes=2, vel=48)
    return m


MUSICAS = {
    'Catacumbas': catacumbas,
    'Cripta': cripta,
    'Abismo': abismo,
    'Chefe': chefe,
    'ChefeFinal': chefe_final,
    'Menu': menu,
    'Loja': loja,
    'Vitoria': vitoria,
    'FimDeJogo': fim_de_jogo,
}
