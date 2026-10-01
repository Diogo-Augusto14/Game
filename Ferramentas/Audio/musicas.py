"""As musicas do jogo, compostas nota a nota. Rodar: python3 musicas.py [nome...]"""
import sys

from motor import Musica, pad, baixo, arpejo, Acorde, p

# Programas GM (base 0) e bancos do MuseScore General.
PIANO, CELESTA, CAIXINHA, SINOS, XILOFONE, MARIMBA = 0, 8, 10, 14, 13, 12
CRAVO, ORGAO_IGREJA = 6, 19
CORAL, VOZES = 52, 53
CORDAS, CORDAS_LENTAS, TREMOLO, PIZZ, HARPA, TIMPANO = 48, 49, 44, 45, 46, 47
VIOLINO, VIOLONCELO, CONTRABAIXO = 40, 42, 43
TROMPETE, TROMBONE, TUBA, TROMPA, METAIS = 56, 57, 58, 60, 61
OBOE, FAGOTE, CLARINETE, FLAUTA, FLAUTA_PA = 68, 70, 71, 73, 75
PAD_QUENTE, PAD_CORAL, PAD_ARCO = 89, 91, 92
KALIMBA, TAIKO = 108, 116
# bancos de naipes do MuseScore General (programa 49 = lento, 48 = rapido, 44 = tremolo, 45 = pizzicato)
VIOLINOS, VIOLAS, CELLOS, BAIXOS = 20, 30, 40, 50
KIT_PADRAO, KIT_ORQUESTRA = 0, 48


def porao():
    m = Musica('Porao', bpm=92, batidas_por_compasso=4, compassos=32, semente=1)
    A = ['Am', 'F', 'C', 'G', 'Am', 'F', 'Dm', 'E']
    B = ['Am', 'G', 'F', 'E', 'Dm', 'Am', 'F', 'E']
    C = ['F', 'G', 'Em', 'Am', 'F', 'G', 'E', 'E7']
    tudo = A + A + B + C

    piano = m.trilha('piano', PIANO, volume=100, pan=58, reverb=80)
    arpejo(piano, tudo, [0, 2, 3, 4, 5, 4, 3, 2], 0.5, oitava=2, vel=52, legato=1.8, acentos=[10, 0, 4, 0, 6, 0, 4, 0])

    coro = m.trilha('coro', CORAL, volume=78, pan=70, reverb=100)
    pad(coro, tudo, faixa=(57, 72), vozes=3, vel=54)

    baixos = m.trilha('baixos', CORDAS_LENTAS, banco=BAIXOS, volume=80, reverb=70)
    baixo(baixos, tudo[:8], [(0, 4, 'b')], oitava=1, vel=58)
    baixo(baixos, tudo[8:], [(0, 4, 'b')], compasso0=8, oitava=1, vel=66)

    cordas = m.trilha('cordas', CORDAS_LENTAS, banco=VIOLINOS, volume=78, pan=50, reverb=95)
    pad(cordas, B + C, compasso0=16, faixa=(60, 79), vozes=3, vel=52)

    celesta = m.trilha('celesta', CELESTA, volume=74, pan=80, reverb=110)
    for i, s in enumerate(A):
        if i % 2 == 0:
            celesta.nota(i * 4, 3, Acorde(s).tom(2, 5), 62)
            celesta.nota(i * 4 + 1.5, 2, Acorde(s).tom(1, 6), 50)

    mel = m.trilha('melodia', PIANO, volume=110, pan=66, reverb=85)
    mel.melodia(8, 'E5:1.5 D5:.5 C5:1 E5:1 | A5:2 G5:1 F5:1 | E5:1.5 D5:.5 C5:1 G4:1 | B4:2 D5:2 | '
                   'E5:1.5 D5:.5 C5:1 E5:1 | A5:1.5 B5:.5 C6:1 A5:1 | F5:1 E5:1 D5:1 F5:1 | B4:1.5 C5:.5 B4:1 G#4:1', vel=82)
    mel.melodia(16, 'A5:1 G5:.5 A5:.5 E5:2 | G5:1 F5:.5 G5:.5 D5:2 | F5:1 E5:.5 F5:.5 C5:1 A4:1 | G#4:1 B4:1 E5:2 | '
                    'F5:1.5 E5:.5 D5:1 A4:1 | C5:1.5 B4:.5 A4:1 E5:1 | A5:1.5 G5:.5 F5:1 C5:1 | B4:2 G#4:1 B4:1', vel=80)
    eco = m.trilha('eco', CELESTA, volume=60, pan=96, reverb=120)
    eco.melodia(16, 'A5:1 G5:.5 A5:.5 E5:2 | G5:1 F5:.5 G5:.5 D5:2 | F5:1 E5:.5 F5:.5 C5:1 A4:1 | G#4:1 B4:1 E5:2 | '
                    'F5:1.5 E5:.5 D5:1 A4:1 | C5:1.5 B4:.5 A4:1 E5:1 | A5:1.5 G5:.5 F5:1 C5:1 | B4:2 G#4:1 B4:1', vel=55, oitava=1)

    violino = m.trilha('violino', CORDAS_LENTAS, banco=VIOLINOS, volume=96, pan=60, reverb=95)
    violino.melodia(24, 'C5:2 A4:2 | B4:2 D5:2 | E5:2 G5:1 E5:1 | A4:1 C5:1 E5:2 | '
                        'F5:2 E5:1 C5:1 | D5:2 B4:1 G4:1 | G#4:2 B4:2 | D5:2 B4:1 G#4:1', vel=88, legato=1.0)

    bat = m.bateria(kit=KIT_ORQUESTRA, volume=100, reverb=80)
    for c in range(8, 32):
        bat.nota(c * 4, 0.5, 36, 62)
        bat.nota(c * 4 + 0.5, 0.5, 36, 44)
        if c >= 24:
            bat.nota(c * 4 + 2, 0.5, 36, 48)
            bat.nota(c * 4 + 2.5, 0.5, 36, 36)
    return m
