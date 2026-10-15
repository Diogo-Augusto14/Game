"""Monta as folhas do Goblin da Clava visto de cima (1.22.0), feito no PixelLab no estilo do Aventureiro.

Entrada: uma pasta com os quadros baixados do PixelLab, <animacao>/<direcao>/<n>.png (48 x 48), com
animacao em parado, andar, ataque, morte e direcao em east, south, north. Os quadros sao enfileirados
em folhas de 48 x 48 em Assets/Arte/Resources/Personagens/GoblinDaClava:

  Idle, Walk, Attack01, Death (de lado; o jogo espelha pra esquerda)
  Idle_Frente, Idle_Costas, Walk_Frente, Walk_Costas, Attack01_Frente, Attack01_Costas

Todos os quadros vao pro mesmo chao: o pe do quadro parado de lado fica na mesma linha em todos.
Falta um rumo? Usa o de lado. Uso: python3 Ferramentas/Personagens/goblin_da_clava.py <pasta>
"""

import os
import sys
from PIL import Image

RAIZ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SAIDA = os.path.join(RAIZ, "Assets", "Arte", "Resources", "Personagens", "GoblinDaClava")
QUADRO = 48

NOMES = {"parado": "Idle", "andar": "Walk", "ataque": "Attack01", "morte": "Death"}
RUMOS = {"east": "", "south": "_Frente", "north": "_Costas"}

# Quadros escolhidos: no parado de lado o PixelLab vira a cara pra camera no fim (vai e volta nos
# primeiros), e no ataque de frente um quadro saiu com um borrao branco.
ESCOLHA = {("parado", "east"): [0, 1, 2, 3, 2, 1], ("ataque", "south"): [0, 1, 2, 3, 4, 5, 7, 8]}


def carregar(entrada, animacao, rumo):
    pasta = os.path.join(entrada, animacao, rumo)

    if not os.path.isdir(pasta):
        return []

    nomes = sorted((f for f in os.listdir(pasta) if f.endswith(".png")), key=lambda f: int(f[:-4]))
    quadros = [Image.open(os.path.join(pasta, f)).convert("RGBA") for f in nomes]
    escolha = ESCOLHA.get((animacao, rumo))
    return [quadros[i] for i in escolha] if escolha else quadros


def no_quadro(q):
    """O quadro no tamanho do jogo, centrado (o PixelLab as vezes devolve a tela maior)."""
    if q.size == (QUADRO, QUADRO):
        return q

    novo = Image.new("RGBA", (QUADRO, QUADRO), (0, 0, 0, 0))
    novo.alpha_composite(q, ((QUADRO - q.width) // 2, (QUADRO - q.height) // 2))
    return novo


def salvar(quadros, nome):
    folha = Image.new("RGBA", (QUADRO * len(quadros), QUADRO), (0, 0, 0, 0))

    for i, q in enumerate(quadros):
        folha.alpha_composite(no_quadro(q), (i * QUADRO, 0))

    folha.save(os.path.join(SAIDA, nome + ".png"))
    print(nome, len(quadros), "quadros")


def main():
    entrada = sys.argv[1]

    for animacao, nome in NOMES.items():
        de_lado = carregar(entrada, animacao, "east")

        if not de_lado:
            print("faltou", animacao)
            continue

        salvar(de_lado, nome)

        if animacao == "morte":
            continue

        for rumo, sufixo in RUMOS.items():
            if sufixo:
                salvar(carregar(entrada, animacao, rumo) or de_lado, nome + sufixo)


if __name__ == "__main__":
    main()
