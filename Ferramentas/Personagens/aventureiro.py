"""Monta as folhas do Aventureiro (heroi visto de cima, 1.22.0) a partir do pacote
"Adventurer 2D Top-Down" do Mattz Art (versao completa), descompactado em Pacotes/ (fora do repositorio).

O pacote tem quadros de 96 x 80, quatro direcoes (down, up, right, left) e parado, andar, correr, dois
ataques, esquiva, dano, cura e morte.
O jogo usa quadros de 100 x 100 com o pe 0,35 unidade abaixo do meio (com 32 pixels por unidade, a
linha 61). Sai assim, em Assets/Arte/Resources/Personagens/Herois/Aventureiro:

  Idle, Walk, Attack01, Dash, Hurt (de lado, olhando pra direita) e Death;
  Idle_Baixo, Idle_Cima, Idle_Esquerda e o mesmo pras outras (menos a morte).

Uso: python3 Ferramentas/Personagens/aventureiro.py
"""

import os
from PIL import Image

RAIZ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PACOTE = os.path.join(RAIZ, "Pacotes", "Adventurer V1", "FULL_Adventurer 2D Pixel Art", "Sprites")
SAIDA = os.path.join(RAIZ, "Assets", "Arte", "Resources", "Personagens", "Herois", "Aventureiro")

LARGURA, ALTURA = 96, 80
QUADRO = 100
# O pe do pacote fica na linha 58 do quadro dele; no jogo vai na 61. No meio, na horizontal.
DESLOCAMENTO = (2, 3)

ANIMACOES = {"Idle": "IDLE/idle", "Walk": "WALK/walk", "Attack01": "ATTACK 1/attack1", "Dash": "DASH/dash", "Hurt": "HURT/hurt"}
DIRECOES = {"": "right", "_Baixo": "down", "_Cima": "up", "_Esquerda": "left"}


def quadros(caminho):
    folha = Image.open(caminho).convert("RGBA")
    return [folha.crop((i * LARGURA, 0, (i + 1) * LARGURA, ALTURA)) for i in range(folha.width // LARGURA)]


def no_quadro(q):
    novo = Image.new("RGBA", (QUADRO, QUADRO), (0, 0, 0, 0))
    novo.alpha_composite(q, DESLOCAMENTO)
    return novo


def salvar(lista, nome):
    folha = Image.new("RGBA", (QUADRO * len(lista), QUADRO), (0, 0, 0, 0))

    for i, q in enumerate(lista):
        folha.alpha_composite(q, (i * QUADRO, 0))

    folha.save(os.path.join(SAIDA, nome + ".png"))


def main():
    os.makedirs(SAIDA, exist_ok=True)

    for nome, arquivo in ANIMACOES.items():
        for sufixo, direcao in DIRECOES.items():
            lista = [no_quadro(q) for q in quadros(os.path.join(PACOTE, f"{arquivo}_{direcao}.png"))]
            salvar(lista, nome + sufixo)

    salvar([no_quadro(q) for q in quadros(os.path.join(PACOTE, "DEATH/death_right.png"))], "Death")
    print("folhas em", SAIDA)


if __name__ == "__main__":
    main()
