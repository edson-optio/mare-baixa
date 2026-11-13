"""Desenha os novos itens com a paleta e as primitivas do gerador de arte do jogo.

python3 ferramentas/gerar_sucata.py [--forcar]
Não altera os sprites anteriores. Os desenhos são ampliados 2x, sem interpolação, para Sprites64.
"""
from pathlib import Path
import sys
from PIL import Image
from gerar_arte import Tela, retangulo, linha, elipse, hexa

DEST = Path(__file__).resolve().parent.parent / "MareBaixa/Assets/Art/Sprites64"
INK = hexa("101B2D")
CREAM = hexa("FFF0CE")
SAND = hexa("E6BD85")
TEAL = hexa("42B5AC")
LIGHT = hexa("9BE8DE")
RUST = hexa("B94724")


def save(t, name):
    path = DEST / (name + ".png")
    if path.exists() and "--forcar" not in sys.argv:
        return
    t.img.resize((t.w * 2, t.h * 2), Image.Resampling.NEAREST).save(path)
    print(path.relative_to(DEST.parent.parent.parent.parent))


def canudo():
    t = Tela(48, 16)
    t.parte(retangulo(3, 7, 36, 10) | retangulo(36, 4, 43, 7), CREAM)
    for x in range(13, 35, 6):
        t.pinta(linha(x, 7, x + 2, 10, 1) | linha(x + 1, 7, x + 3, 10), hexa("E85848"))
    t.pinta(retangulo(38, 4, 39, 7), hexa("E85848"))
    t.pinta(retangulo(4, 7, 11, 10), TEAL)
    for x in (5, 8, 11):
        t.pinta(linha(x, 7, x + 1, 10), hexa("2A7C7A"))
    t.parte(retangulo(12, 5, 13, 12), SAND)
    t.pinta(retangulo(15, 7, 34, 7), hexa("FFFFFF"))
    t.pinta(retangulo(43, 4, 43, 6), RUST)
    save(t, "arma-canudo")


def garfo():
    t = Tela(64, 18)
    t.parte(retangulo(3, 8, 44, 10) | retangulo(42, 5, 48, 13)
            | retangulo(48, 4, 60, 5) | retangulo(48, 8, 60, 9) | retangulo(48, 12, 60, 13), hexa("A6C5CD"))
    t.pinta(linha(15, 8, 43, 8), CREAM)
    t.pinta(linha(44, 6, 47, 6), hexa("E5F0F6"))
    for y in (4, 8, 12):
        t.pinta(linha(49, y, 59, y), hexa("F4ECFF"))
    t.pinta(retangulo(3, 8, 14, 10), RUST)
    for x in (5, 8, 11):
        t.pinta(linha(x, 8, x + 1, 10), SAND)
    t.pinta(retangulo(39, 9, 41, 10), hexa("6A8B91"))
    save(t, "arma-garfo")


def escova():
    t = Tela(44, 24)
    t.parte(retangulo(3, 11, 29, 14) | retangulo(26, 9, 39, 14), hexa("7050AD"))
    t.pinta(linha(5, 11, 27, 11), hexa("B8A1E0"))
    t.pinta(linha(7, 13, 20, 13), hexa("443267"))
    t.parte(retangulo(28, 4, 38, 9), LIGHT)
    for x in range(29, 39, 3):
        t.pinta(linha(x, 4, x, 8), CREAM)
        t.pinta(linha(x + 1, 6, x + 1, 9), TEAL)
    t.pinta(retangulo(31, 4, 32, 5), hexa("789B83"))
    t.pinta(retangulo(37, 5, 38, 6), hexa("789B83"))
    save(t, "arma-escova")


def banco():
    t = Tela(72, 38)
    # Estrutura de metal corroído, tábuas da superfície e uma conchinha luminosa no encosto.
    for x in (15, 53):
        t.parte(retangulo(x, 16, x + 3, 35), hexa("42656B"))
        t.pinta(retangulo(x, 29, x + 2, 31), RUST)
    for y in (13, 19):
        t.parte(retangulo(8, y, 62, y + 3), hexa("A46E4A"))
        t.pinta(linha(10, y, 61, y), SAND)
        t.pinta(linha(15, y + 2, 33, y + 2), hexa("714B3D"))
        t.pinta(linha(42, y + 2, 56, y + 2), hexa("714B3D"))
        for x in (16, 54):
            t.ponto(x, y + 1, CREAM)
    t.parte(retangulo(7, 25, 64, 28), hexa("BF8757"))
    t.pinta(linha(8, 25, 63, 25), CREAM)
    t.pinta(linha(12, 28, 59, 28), hexa("714B3D"))
    # Algas agarradas aos pés.
    for x, shift in ((11, 0), (59, 2)):
        t.parte(linha(x, 35, x - 1, 29 - shift) | linha(x - 1, 29 - shift, x + 1, 26 - shift), TEAL)
        t.pinta(linha(x, 34, x, 30 - shift), LIGHT)
    t.parte(elipse(35, 9, 5, 4), TEAL)
    t.pinta(elipse(35, 8, 3, 2), LIGHT)
    t.pinta(linha(35, 6, 35, 11), CREAM)
    t.ponto(34, 4, CREAM)
    t.ponto(41, 7, LIGHT)
    save(t, "banco-descanso")


if __name__ == "__main__":
    DEST.mkdir(parents=True, exist_ok=True)
    canudo()
    garfo()
    escova()
    banco()
