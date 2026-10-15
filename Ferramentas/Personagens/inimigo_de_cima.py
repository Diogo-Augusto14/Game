"""Monta as folhas de um inimigo visto de cima (1.22.0), feito no PixelLab no estilo do Aventureiro
(o Goblin da Clava e o Esqueleto Guerreiro).

Entrada: uma pasta com os quadros baixados do PixelLab, <animacao>/<direcao>/<n>.png (48 x 48), com
animacao em parado, andar, ataque, morte e direcao em east, south, north; e, se tiver, rotacao/<direcao>.png
(o boneco parado, usado como parado de um quadro so quando falta a animacao daquele rumo). Os quadros sao
enfileirados em folhas de 48 x 48 em Assets/Arte/Resources/Personagens/<Pasta>:

  Idle, Walk, Attack01, Death (de lado; o jogo espelha pra esquerda)
  Idle_Frente, Idle_Costas, Walk_Frente, Walk_Costas, Attack01_Frente, Attack01_Costas

Falta um rumo? Usa a rotacao (no parado) ou o de lado.
Uso: python3 Ferramentas/Personagens/inimigo_de_cima.py <quadros> <Pasta>  (ex.: GoblinDaClava)
"""

import os
import sys
from PIL import Image

RAIZ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PERSONAGENS = os.path.join(RAIZ, "Assets", "Arte", "Resources", "Personagens")
SAIDA = PERSONAGENS
QUADRO = 48

NOMES = {"parado": "Idle", "andar": "Walk", "ataque": "Attack01", "morte": "Death"}
RUMOS = {"east": "", "south": "_Frente", "north": "_Costas"}

# Quadros escolhidos, por inimigo. Goblin: no parado de lado o PixelLab vira a cara pra camera no fim (vai
# e volta nos primeiros), e no ataque de frente um quadro saiu com um borrao branco.
ESCOLHAS = {
    "GoblinDaClava": {("parado", "east"): [0, 1, 2, 3, 2, 1], ("ataque", "south"): [0, 1, 2, 3, 4, 5, 7, 8]},
    # Esqueleto: no meio do golpe de lado ele vira pra camera (fica o comeco e a volta), e o parado de
    # lado vai abaixando a espada (vai e volta nos primeiros).
    "EsqueletoGuerreiro": {("ataque", "east"): [0, 1, 2, 3, 8], ("parado", "east"): [0, 1, 2, 1]},
}
ESCOLHA = {}


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


def rotacao(entrada, rumo):
    caminho = os.path.join(entrada, "rotacao", rumo + ".png")
    return [Image.open(caminho).convert("RGBA")] if os.path.exists(caminho) else []


def main():
    global SAIDA, ESCOLHA
    entrada, pasta = sys.argv[1], sys.argv[2]
    SAIDA = os.path.join(PERSONAGENS, pasta)
    ESCOLHA = ESCOLHAS.get(pasta, {})

    for animacao, nome in NOMES.items():
        de_lado = carregar(entrada, animacao, "east") or (rotacao(entrada, "east") if animacao == "parado" else [])

        if not de_lado:
            print("faltou", animacao)
            continue

        salvar(de_lado, nome)

        if animacao == "morte":
            continue

        for rumo, sufixo in RUMOS.items():
            if sufixo:
                parado = rotacao(entrada, rumo) if animacao == "parado" else []
                salvar(carregar(entrada, animacao, rumo) or parado or de_lado, nome + sufixo)


if __name__ == "__main__":
    main()
