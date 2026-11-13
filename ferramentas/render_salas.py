"""Desenha cada sala (Assets/Salas/*.txt) com os sprites do jogo, para revisar o layout sem abrir o Unity.

Arte que ainda não existe (Cidade, Fossa, inimigos novos) vira um quadrado colorido com a letra da legenda.

Uso: python3 ferramentas/render_salas.py [pasta_de_saida]   (padrão: design/salas-previa)
"""
import os
import sys
from PIL import Image, ImageDraw

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.join(AQUI, "..")
SALAS = os.path.join(RAIZ, "MareBaixa", "Assets", "Salas")
ARTE = os.path.join(RAIZ, "MareBaixa", "Assets", "Art", "Gerados")
CENARIOS = os.path.join(RAIZ, "MareBaixa", "Assets", "Art", "Cenarios")
BLOCOS = os.path.join(RAIZ, "MareBaixa", "Assets", "Art", "Blocos")
B = 32

SPRITES = {
    "C": "casco-parado-1", "S": "saida", "D": "saida", "j": "agua-viva-1", "b": "baiacu-1",
    "o": "perola-1", "T": "bau-fechado", "a": "alga", "O": "bolha-1", "c": "concha-comum",
    "t": "concha-tampinha", "e": "concha-espiral", "r": "concha-espiral", "k": "coral-1", "K": "coral-2",
    "g": "alga-decor-1", "p": "placa", "h": "casa-garrafa", "V": "vovo-buzia", "Y": "ostracia",
    "L": "dona-lagosta", "U": "bolota",
    "R": "banco-descanso", "u": "arma-canudo", "F": "arma-garfo", "E": "arma-escova",
    "m": "moreia-escondida", "s": "siri-1", "f": "pescador-1", "w": "tentaculo-1", "v": "valvula-1",
    "<": "correnteza-1", ">": "correnteza-1", "H": "fonte-1", "B": "colosso-anda-1",
    "P": "polvo-corpo", "l": "concha-lata", "i": "concha-lampada", "x": "carro", "q": "semaforo", "n": "coral-bio", "z": "lixo",
}
# Cor e tamanho (em blocos) do quadrado provisório quando o PNG ainda não existe.
PROVISORIO = {
    "m": ((120, 150, 60), (2, 1)), "s": ((210, 80, 50), (1, 1)), "f": ((40, 40, 90), (1.5, 1)),
    "w": ((90, 40, 110), (1, 2)), "v": ((170, 90, 40), (1, 1)), "<": ((150, 220, 240), (1, 1)),
    ">": ((150, 220, 240), (1, 1)), "H": ((120, 220, 200), (1, 1)), "B": ((110, 70, 50), (3, 2.5)), "P": ((60, 30, 80), (6, 4)),
    "l": ((200, 40, 40), (1, 1)), "i": ((240, 230, 150), (1, 1)), "x": ((140, 70, 50), (2, 1)),
    "q": ((60, 60, 60), (0.5, 1.5)), "n": ((190, 90, 255), (1, 1)), "z": ((110, 120, 100), (1, 0.5)),
}
PES = set("CSDTctegrkKgphVYLUmswvHBPlixqz")
FUNDO_PADRAO = {"vila": (196, 150, 98), "recife": (18, 61, 73), "cidade": (40, 62, 56),
                "fossa": (10, 12, 34), "bolha": (40, 90, 110)}
BLOCO_PADRAO = {"cidade": ((95, 100, 92), (70, 74, 70), (130, 120, 90)),
                "fossa": ((50, 40, 90), (28, 24, 54), (80, 60, 120)),
                "bolha": ((120, 170, 160), (70, 110, 110), (150, 190, 170))}

_cache = {}


def carrega(c):
    if c not in _cache:
        caminho = os.path.join(ARTE, SPRITES[c] + ".png")
        if os.path.exists(caminho):
            _cache[c] = Image.open(caminho).convert("RGBA")
        else:
            cor, (lw, lh) = PROVISORIO.get(c, ((255, 0, 255), (1, 1)))
            img = Image.new("RGBA", (int(lw * B), int(lh * B)), cor + (230,))
            d = ImageDraw.Draw(img)
            d.rectangle([0, 0, img.width - 1, img.height - 1], outline=(255, 255, 255, 200))
            d.text((4, 2), c, fill=(255, 255, 255, 255))
            _cache[c] = img
    return _cache[c]


def bloco(zona, parte, indice):
    caminho = os.path.join(BLOCOS, f"{zona}-{parte}.png")
    if os.path.exists(caminho):
        return Image.open(caminho).convert("RGBA")
    if zona in BLOCO_PADRAO:
        cor = BLOCO_PADRAO[zona][indice]
        img = Image.new("RGBA", (B, B), cor + (255,))
        ImageDraw.Draw(img).rectangle([0, 0, B - 1, B - 1], outline=tuple(int(x * .75) for x in cor))
        if parte == "plataforma":
            img = Image.new("RGBA", (B, B), (0, 0, 0, 0))
            img.paste(Image.new("RGBA", (B, B // 3), cor + (255,)), (0, 0))
        return img
    return bloco("recife", parte, indice)


def render(arquivo, saida):
    linhas = [l.rstrip("\n") for l in open(arquivo, encoding="utf-8")]
    grid = [l for l in linhas if not l.startswith("@") and l.strip()]
    zona = os.path.basename(arquivo).split("-")[0].split(".")[0]
    if zona not in FUNDO_PADRAO:
        zona = "recife"
    h, w = len(grid), max(len(l) for l in grid)
    img = Image.new("RGBA", (w * B, h * B), FUNDO_PADRAO[zona] + (255,))
    fundo = os.path.join(CENARIOS, f"fundo-{zona}.png")
    if os.path.exists(fundo):
        f = Image.open(fundo).convert("RGBA").resize((w * B, h * B), Image.NEAREST)
        img.alpha_composite(f)
    topo, meio, plat = bloco(zona, "topo", 0), bloco(zona, "meio", 1), bloco(zona, "plataforma", 2)
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
                s = carrega("P" if c == "B" and zona == "fossa" else c)
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
