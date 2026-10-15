"""Monta as folhas de um inimigo visto de cima (1.22.0), feito no PixelLab no estilo do Aventureiro
(o Goblin da Clava e o Esqueleto Guerreiro).

Entrada: uma pasta com os quadros baixados do PixelLab, <animacao>/<direcao>/<n>.png (48 x 48), com
animacao em parado, andar, ataque, morte e direcao em east, south, north (o tamanho do quadro e o do andar); e, se tiver, rotacao/<direcao>.png
(o boneco parado, usado como parado de um quadro so quando falta a animacao daquele rumo). Os quadros sao
enfileirados em folhas do mesmo tamanho em Assets/Arte/Resources/Personagens/<Pasta>:

  Idle, Walk, Attack01, Death (de lado; o jogo espelha pra esquerda)
  Idle_Frente, Idle_Costas, Walk_Frente, Walk_Costas, Attack01_Frente, Attack01_Costas

Falta um rumo? Usa a rotacao (no parado) ou o de lado.

Com o prefab, tambem acerta ele: cria os .meta das folhas novas, liga as de frente e de costas
(paradoDeCima, andandoDeCima, ataqueDeCima), troca o tamanho do quadro, os pixels por unidade e o quadro
do golpe, e sobe ou desce o corpo pra o pe ficar no mesmo chao do desenho antigo.

Uso: python3 Ferramentas/Personagens/inimigo_de_cima.py <quadros> <Pasta> [<Prefab> <pixels> <golpe>]
     (ex.: ... GoblinDaClava GoblinDaClava 36 4)
"""

import os
import re
import sys
import uuid
from PIL import Image

RAIZ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PERSONAGENS = os.path.join(RAIZ, "Assets", "Arte", "Resources", "Personagens")
SAIDA = PERSONAGENS
QUADRO = 48

NOMES = {"parado": "Idle", "andar": "Walk", "ataque": "Attack01", "morte": "Death"}
RUMOS = {"east": "", "south": "_Frente", "north": "_Costas"}

# Quadros escolhidos, por inimigo (o que o PixelLab estragou fica de fora).
ESCOLHAS = {
    # Goblin: no golpe de lado a clava some nos quadros 4 e 5.
    "GoblinDaClava": {("ataque", "east"): [0, 1, 2, 3, 3, 6]},
    # Esqueleto (refeito no plano pago, tudo no skeleton-v3): no golpe de lado a espada some em dois quadros
    # (ficam os que tem ela: levanta e desce, com o golpe no quadro 4 como nos outros rumos), e no parado
    # de frente o ultimo quadro abaixa a espada.
    "EsqueletoGuerreiro": {("ataque", "east"): [1, 1, 2, 2, 5, 6], ("parado", "south"): [0, 1, 2, 3, 4, 5, 6]},
    # Bruxo: no ataque de lado (fireball) dois quadros saem borrados e no ultimo ele some.
    "Bruxo": {("ataque", "east"): [0, 1, 1, 4, 4, 0]},
    # Orc: no golpe de lado o machado some no quadro 4.
    "Orc": {("ataque", "east"): [0, 1, 2, 3, 5, 6]},
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
    if q.width >= QUADRO and q.height >= QUADRO:
        return q.crop(((q.width - QUADRO) // 2, (q.height - QUADRO) // 2, (q.width + QUADRO) // 2, (q.height + QUADRO) // 2))

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


def pe(folha, quadro):
    """A linha logo abaixo do pe no primeiro quadro (o fundo do desenho)."""
    return Image.open(folha).convert("RGBA").crop((0, 0, quadro[0], quadro[1])).getbbox()[3]


def guid(png):
    """O guid do .meta da folha (cria o .meta, copiado do Idle, se nao tiver)."""
    meta = png + ".meta"

    if not os.path.exists(meta):
        modelo = open(os.path.join(os.path.dirname(png), "Idle.png.meta")).read()
        modelo = re.sub(r"^guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, modelo, flags=re.M)
        modelo = re.sub(r"spriteID: [0-9a-f]{32}", lambda _: "spriteID: " + uuid.uuid4().hex, modelo)
        open(meta, "w", newline="").write(modelo)

    return re.search(r"^guid: (\w+)", open(meta).read(), re.M).group(1)


class Prefab:
    """O prefab do inimigo: o texto, a folha parada, o quadro e o corpo (a posicao dele)."""

    def __init__(self, nome):
        self.caminho = os.path.join(RAIZ, "Assets", "Prefabs", nome + ".prefab")
        self.texto = open(self.caminho, encoding="utf-8", newline="").read()
        self.nl = "\r\n" if "\r\n" in self.texto else "\n"
        q = re.search(r"tamanhoDoQuadro: \{x: (\d+), y: (\d+)\}", self.texto)
        self.quadro = (int(q.group(1)), int(q.group(2)))
        self.pixels = float(re.search(r"  pixelsPorUnidade: ([\d.]+)", self.texto).group(1))
        corpo = re.search(r"  corpo: \{fileID: (\d+)\}", self.texto).group(1)
        dono = re.search(r"--- !u!212 &" + corpo + r".*?m_GameObject: \{fileID: (\d+)\}", self.texto, re.S).group(1)
        self.posicao = re.search(r"(--- !u!4 &\d+\s+Transform:(?:(?!---).)*?m_GameObject: \{fileID: " + dono
                                 + r"\}(?:(?!---).)*?m_LocalPosition: \{x: )([-\d.e]+)(, y: )([-\d.e]+)", self.texto, re.S)

    def pe_no_chao(self, folha):
        """Onde o pe fica, em unidades a partir da raiz do inimigo (negativo = abaixo)."""
        y = float(self.posicao.group(4))
        return y - (pe(folha, self.quadro) - self.quadro[1] / 2) / self.pixels

    def trocar(self, antes, depois):
        assert self.texto.count(antes) == 1, antes
        self.texto = self.texto.replace(antes, depois)

    def aplicar(self, chao, pixels, golpe, pasta):
        y = chao + (pe(os.path.join(pasta, "Idle.png"), (QUADRO, QUADRO)) - QUADRO / 2) / pixels
        p = self.posicao
        self.texto = self.texto[:p.start(4)] + str(round(y, 3)) + self.texto[p.end(4):]
        self.texto = re.sub(r"tamanhoDoQuadro: \{x: \d+, y: \d+\}", f"tamanhoDoQuadro: {{x: {QUADRO}, y: {QUADRO}}}", self.texto, count=1)
        self.texto = re.sub(r"(  pixelsPorUnidade: )[\d.]+", lambda m: m.group(1) + f"{pixels:g}", self.texto, count=1)
        self.texto = re.sub(r"(  quadroDoDisparo: )\d+", lambda m: m.group(1) + str(golpe), self.texto, count=1)
        # As listas de frente e costas: tira as velhas e poe as novas antes de velocidadeParaAndar.
        self.texto = re.sub(r"  (paradoDeCima|andandoDeCima|ataqueDeCima):(?:\r?\n  - \{[^}]*\})*\r?\n", "", self.texto)
        listas = ""

        for campo, nome in (("paradoDeCima", "Idle"), ("andandoDeCima", "Walk"), ("ataqueDeCima", "Attack01")):
            listas += f"  {campo}:" + self.nl
            for sufixo in ("_Frente", "_Costas"):
                listas += f"  - {{fileID: 2800000, guid: {guid(os.path.join(pasta, nome + sufixo + '.png'))}, type: 3}}" + self.nl

        self.texto = self.texto.replace("  velocidadeParaAndar:", listas + "  velocidadeParaAndar:", 1)
        open(self.caminho, "w", encoding="utf-8", newline="").write(self.texto)
        print("prefab: corpo em y =", round(y, 3))


def rotacao(entrada, rumo):
    caminho = os.path.join(entrada, "rotacao", rumo + ".png")
    return [Image.open(caminho).convert("RGBA")] if os.path.exists(caminho) else []


def main():
    global SAIDA, ESCOLHA, QUADRO
    entrada, pasta = sys.argv[1], sys.argv[2]
    # O tamanho do quadro e o das animacoes (o PixelLab aumenta a tela pra caber a arma: 64 vira 80).
    QUADRO = max(q.width for q in carregar(entrada, "andar", "east") or [Image.new("RGBA", (QUADRO, QUADRO))])
    SAIDA = os.path.join(PERSONAGENS, pasta)
    ESCOLHA = ESCOLHAS.get(pasta, {})
    prefab = Prefab(sys.argv[3]) if len(sys.argv) > 3 else None
    chao = prefab.pe_no_chao(os.path.join(SAIDA, "Idle.png")) if prefab else 0

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

    if prefab:
        prefab.aplicar(chao, float(sys.argv[4]), int(sys.argv[5]), SAIDA)


if __name__ == "__main__":
    main()
