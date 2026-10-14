"""As musicas do jogo, compostas nota a nota. Rodar: python3 musicas.py [nome...]"""
import sys

from motor import Musica, pad, baixo, arpejo, Acorde, p

# Programas GM (base 0) e bancos do MuseScore General.
PIANO, CELESTA, CAIXINHA, SINOS, XILOFONE, MARIMBA = 0, 8, 10, 14, 13, 12
CRAVO, ORGAO_IGREJA = 6, 19
CORAL, VOZES = 52, 53
CORDAS, CORDAS_LENTAS, TREMOLO, PIZZ, HARPA, TIMPANO = 48, 49, 44, 45, 46, 47
VIOLINO, VIOLONCELO, CONTRABAIXO = 40, 42, 43
VIOLAO = 24
TROMPETE, TROMBONE, TUBA, TROMPA, METAIS = 56, 57, 58, 60, 61
OBOE, FAGOTE, CLARINETE, FLAUTA, FLAUTA_PA = 68, 70, 71, 73, 75
PAD_QUENTE, PAD_CORAL, PAD_ARCO = 89, 91, 92
KALIMBA, TAIKO = 108, 116
# bancos de naipes do MuseScore General (programa 49 = lento, 48 = rapido, 44 = tremolo, 45 = pizzicato)
VIOLINOS, VIOLAS, CELLOS, BAIXOS = 20, 30, 40, 50
KIT_PADRAO, KIT_ORQUESTRA = 0, 48


def porao():
    # A minor sombrio (1.19.0): pizzicato grave rastejando, drone de contrabaixos, pad de arco, fagote e um
    # violino sozinho; um coracao de taiko. Nada de celesta nem piano de balada.
    m = Musica('Porao', bpm=80, batidas_por_compasso=4, compassos=32, semente=1)
    A = ['Am', 'Am', 'F', 'E', 'Am', 'Dm', 'Bb', 'E']
    B = ['Dm', 'Am', 'Bb', 'E', 'Dm', 'Am', 'F', 'E']
    tudo = A + A + B + A

    pizz = m.trilha('pizz', PIZZ, banco=CELLOS, volume=96, pan=54, reverb=80)
    arpejo(pizz, tudo, [0, 0, 2, 0, 1, 0, 2, 3], 0.5, oitava=2, vel=66, legato=0.5, acentos=[14, 0, 4, 0, 8, 0, 4, 0])

    baixos = m.trilha('baixos', CORDAS_LENTAS, banco=BAIXOS, volume=86, reverb=80)
    baixo(baixos, tudo, [(0, 4, 'b')], oitava=1, vel=58)

    nevoa = m.trilha('nevoa', PAD_ARCO, volume=72, pan=74, reverb=115)
    pad(nevoa, tudo[8:], compasso0=8, faixa=(52, 66), vozes=3, vel=44)

    vozes = m.trilha('vozes', VOZES, volume=66, pan=40, reverb=120)
    pad(vozes, tudo[16:], compasso0=16, faixa=(52, 64), vozes=3, vel=40)

    sinos = m.trilha('sinos', SINOS, volume=60, pan=30, reverb=127)
    for c in (0, 16):
        sinos.nota(c * 4, 4, p('A3'), 46)

    mel_a = ('A3:1.5 B3:.5 C4:1 E4:1 | D4:1 C4:1 B3:2 | A3:1.5 C4:.5 F4:1 E4:1 | E4:2 G#3:2 | '
             'A3:1 C4:1 E4:1 A4:1 | F4:2 D4:1 A3:1 | Bb3:1.5 A3:.5 G3:1 F3:1 | E3:2 G#3:1 B3:1')
    mel_b = ('F5:2 E5:1 D5:1 | C5:2 A4:2 | D5:1.5 C5:.5 Bb4:2 | G#4:4 | '
             'A4:1 D5:1 F5:1 A5:1 | G5:1 E5:1 C5:2 | A4:1.5 C5:.5 F5:1 E5:1 | E5:2 D5:1 B4:1')
    fagote = m.trilha('fagote', FAGOTE, volume=84, pan=60, reverb=95)
    fagote.melodia(8, mel_a, vel=72, legato=1.0)
    violino = m.trilha('violino', VIOLINO, volume=72, pan=70, reverb=110)
    violino.melodia(16, mel_b, vel=62, legato=1.0)
    cellos = m.trilha('cellos', CORDAS_LENTAS, banco=CELLOS, volume=96, pan=56, reverb=100)
    cellos.melodia(24, mel_a, vel=78, legato=1.0)

    taiko = m.trilha('taiko', TAIKO, volume=78, reverb=95)
    for c in range(8, 32):
        taiko.nota(c * 4, 1, p('A1'), 60)
        taiko.nota(c * 4 + 0.75, 1, p('A1'), 44)
        if c >= 24:
            taiko.nota(c * 4 + 2, 1, p('A1'), 50)
    return m
