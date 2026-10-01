import sys
import musicas, musicas2
MUSICAS = {'Porao': musicas.porao}
MUSICAS.update(musicas2.MUSICAS)
if __name__ == '__main__':
    nomes = sys.argv[1:] or list(MUSICAS)
    for n in nomes:
        m = MUSICAS[n]()
        sem_loop = getattr(m, 'sem_loop', False)
        wav, dur, pico = m.renderizar('saida', loop=not sem_loop, **getattr(m, 'mix', {}))
        print(f'{n}: {dur:.1f}s pico {pico:.1f} dB -> {wav}', flush=True)
