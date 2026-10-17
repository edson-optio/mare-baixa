"""Desenha cada sala (Assets/Salas/*.txt) com os sprites do jogo, para revisar o layout sem abrir o Unity.

Uso: python3 ferramentas/render_salas.py [pasta_de_saida]   (padrão: design/salas-previa)
"""
import os
import sys
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.join(AQUI, "..")
SALAS = os.path.join(RAIZ, "MareBaixa", "Assets", "Salas")
ARTE = os.path.join(RAIZ, "MareBaixa", "Assets", "Art", "Gerados")
CENARIOS = os.path.join(RAIZ, "MareBaixa", "Assets", "Art", "Cenarios")
B = 32

SPRITES = {
    "C": "casco-parado-1", "S": "saida", "D": "saida", "j": "agua-viva-1", "b": "baiacu-1",
    "o": "perola-1", "T": "bau-fechado", "a": "alga", "O": "bolha-1", "c": "concha-comum",
    "t": "concha-tampinha", "e": "concha-espiral", "r": "concha-espiral", "k": "coral-1", "K": "coral-2",
    "g": "alga-decor-1", "p": "placa", "h": "casa-garrafa", "V": "vovo-buzia", "Y": "ostracia",
    "L": "dona-lagosta", "U": "bolota",
}
PES = set("CSDTctegrkKgphVYLU")


def carrega(nome):
    return Image.open(os.path.join(ARTE, nome + ".png")).convert("RGBA")


def render(arquivo, saida):
    linhas = [l.rstrip("\n") for l in open(arquivo, encoding="utf-8")]
    grid = [l for l in linhas if not l.startswith("@") and l.strip()]
    vila = os.path.basename(arquivo).startswith("vila")
    h, w = len(grid), max(len(l) for l in grid)
    img = Image.new("RGBA", (w * B, h * B), (18, 61, 73, 255))
    fundo = os.path.join(CENARIOS, ("fundo-vila" if vila else "fundo-recife") + ".png")
    if os.path.exists(fundo):
        f = Image.open(fundo).convert("RGBA").resize((w * B, h * B), Image.NEAREST)
        img.alpha_composite(f)
    zona = "vila" if vila else "recife"
    bloco = lambda n: Image.open(os.path.join(RAIZ, "MareBaixa", "Assets", "Art", "Blocos", n + ".png")).convert("RGBA")
    topo, meio, plat = bloco(zona + "-topo"), bloco(zona + "-meio"), bloco(zona + "-plataforma")
    at = lambda x, y: grid[y][x] if 0 <= y < h and x < len(grid[y]) else " "
    for y in range(h):
        for x in range(w):
            c = at(x, y)
            if c == "#":
                img.alpha_composite(topo if y > 0 and at(x, y - 1) != "#" else meio, (x * B, y * B))
            elif c == "=":
                img.alpha_composite(plat, (x * B, y * B))
    for y in range(h):
        for x in range(w):
            c = at(x, y)
            if c in SPRITES:
                s = carrega(SPRITES[c])
                if c in PES:
                    px, py = x * B + B // 2 - s.width // 2, (y + 1) * B - s.height
                else:
                    px, py = x * B + B // 2 - s.width // 2, y * B + B // 2 - s.height // 2
                img.alpha_composite(s, (px, py))
            elif c.isdigit():
                img.alpha_composite(Image.new("RGBA", (B, B), (255, 220, 80, 110)), (x * B, y * B))
    img.save(saida)


if __name__ == "__main__":
    destino = sys.argv[1] if len(sys.argv) > 1 else os.path.join(RAIZ, "design", "salas-previa")
    os.makedirs(destino, exist_ok=True)
    for nome in sorted(os.listdir(SALAS)):
        if nome.endswith(".txt"):
            render(os.path.join(SALAS, nome), os.path.join(destino, nome[:-4] + ".png"))
            print("ok", nome)
