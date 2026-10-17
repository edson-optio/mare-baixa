"""Gera a pixel art provisória do protótipo de Maré Baixa.

Tudo é desenhado em 32 x 32 px (NPCs grandes em 48 x 48), com a paleta proposta em
design/personagens/LEIA-ME.txt. Casco e as conchas saem em arquivos separados, com o mesmo
enquadramento, para a concha virar uma camada por cima do corpo (passo 2 do LEIA-ME).

Os fundos, a arte-chave do menu, os blocos de chão e os retratos ficam em Art/Cenarios, Art/Blocos e
Art/Retratos (gerados à parte, ver design/cenarios, design/tiles e design/retratos). Os sprites daqui são placeholders para validar o jogo. Para trocar um sprite, substitua o PNG em
MareBaixa/Assets/Art/Gerados mantendo o nome e use Maré Baixa > 1. Montar projeto no Unity.

Uso: python3 ferramentas/gerar_arte.py [--forcar]   (sem --forcar, não sobrescreve sprites existentes)
"""
import math
import os
import random
import sys
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.join(AQUI, "..")
SAIDA = os.path.join(RAIZ, "MareBaixa", "Assets", "Art", "Gerados")
CONCEITO = os.path.join(RAIZ, "MareBaixa", "Assets", "Art", "Conceito")


def hexa(cor, alfa=255):
    cor = cor.lstrip("#")
    return tuple(int(cor[i:i + 2], 16) for i in (0, 2, 4)) + (alfa,)


# Paleta proposta no LEIA-ME (mais alguns tons intermediários)
CONTORNO = hexa("101B2D")
SOMBRA_MAR = hexa("123D49")
LARANJA = hexa("F47724")
LARANJA_SOMBRA = hexa("B94724")
LARANJA_LUZ = hexa("FF9F52")
AREIA = hexa("E6BD85")
AREIA_SOMBRA = hexa("C2955E")
LUZ = hexa("FFF0CE")
TURQUESA = hexa("42B5AC")
TURQUESA_SOMBRA = hexa("2A7C7A")
TURQUESA_LUZ = hexa("9BE8DE")
VERDE_SUCATA = hexa("64834B")
VIOLETA = hexa("7050AD")
ABDOMEN = hexa("F59C78")
ABDOMEN_SOMBRA = hexa("D8735A")
BRANCO = hexa("FFFFFF")


class Tela:
    """Desenho em camadas: cada parte ganha contorno próprio antes do preenchimento."""

    def __init__(self, largura, altura=None):
        altura = altura or largura
        self.img = Image.new("RGBA", (largura, altura), (0, 0, 0, 0))
        self.w, self.h = largura, altura

    def dentro(self, p):
        return 0 <= p[0] < self.w and 0 <= p[1] < self.h

    def parte(self, pixels, cor, contorno=CONTORNO):
        pixels = {(int(x), int(y)) for x, y in pixels if self.dentro((x, y))}
        if contorno:
            for x, y in pixels:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    v = (x + dx, y + dy)
                    if v not in pixels and self.dentro(v):
                        self.img.putpixel(v, contorno)
        for p in pixels:
            self.img.putpixel(p, cor)

    def pinta(self, pixels, cor):
        self.parte(pixels, cor, contorno=None)

    def ponto(self, x, y, cor):
        if self.dentro((x, y)):
            self.img.putpixel((int(x), int(y)), cor)

    def salva(self, nome, pasta=SAIDA):
        # Os sprites finais (design/sprites) substituíram estes placeholders: só grava o que falta,
        # a não ser com --forcar.
        caminho = os.path.join(pasta, nome + ".png")
        if os.path.exists(caminho) and "--forcar" not in sys.argv:
            return
        self.img.save(caminho)


def elipse(cx, cy, rx, ry):
    return {(x, y) for x in range(int(cx - rx - 1), int(cx + rx + 2))
            for y in range(int(cy - ry - 1), int(cy + ry + 2))
            if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1}


def retangulo(x0, y0, x1, y1):
    return {(x, y) for x in range(x0, x1 + 1) for y in range(y0, y1 + 1)}


def linha(x0, y0, x1, y1, espessura=1):
    pontos = set()
    passos = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
    for i in range(passos + 1):
        t = i / passos
        x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
        for dx in range(espessura):
            for dy in range(espessura):
                pontos.add((int(round(x)) + dx, int(round(y)) + dy))
    return pontos


def triangulo(a, b, c):
    def lado(p, q, r):
        return (p[0] - r[0]) * (q[1] - r[1]) - (q[0] - r[0]) * (p[1] - r[1])
    xs, ys = [a[0], b[0], c[0]], [a[1], b[1], c[1]]
    pontos = set()
    for x in range(int(min(xs)), int(max(xs)) + 1):
        for y in range(int(min(ys)), int(max(ys)) + 1):
            p = (x + .5, y + .5)
            d1, d2, d3 = lado(p, a, b), lado(p, b, c), lado(p, c, a)
            neg = d1 < 0 or d2 < 0 or d3 < 0
            pos = d1 > 0 or d2 > 0 or d3 > 0
            if not (neg and pos):
                pontos.add((x, y))
    return pontos


def olho(t, cx, cy, r, olhar=(1, 0), aflito=False):
    t.parte(elipse(cx, cy, r, r + .3), LUZ)
    px, py = int(cx + olhar[0] * (r * .35)), int(cy + olhar[1] * (r * .35))
    t.pinta(retangulo(px - 1, py - 1, px, py), CONTORNO)
    t.ponto(px, py - 1, BRANCO)
    if aflito:
        t.pinta(linha(cx - r, cy - r - 1, cx + r - 1, cy - r), CONTORNO)


# ---------------------------------------------------------------- Casco (32 x 32, pés na linha 31)

def casco(nome, bob=0, fase=0, pose="normal"):
    t = Tela(32)
    y = -bob
    # pernas: quatro patas finas, alternando na caminhada
    ancoras = [11, 14, 18, 21]
    for i, ax in enumerate(ancoras):
        passo = (1 if (i + fase) % 2 else -1) if fase else 0
        if pose == "pulo":
            ponta = (ax - 2 + i, 28)
        elif pose == "queda":
            ponta = (ax + (i - 1.5) * 1.5, 31)
        else:
            ponta = (ax + passo + (i - 1.5) * .8, 31)
        t.parte(linha(ax, 24 + y, ponta[0], ponta[1], 1), LARANJA_SOMBRA)
    # abdômen mole enrolado (fica escondido quando há concha)
    t.parte(elipse(9.5, 19 + y, 5.5, 5), ABDOMEN)
    for k in range(3):
        t.pinta(linha(6 + k * 2, 15 + y, 5 + k * 2, 23 + y), ABDOMEN_SOMBRA)
    # corpo
    t.parte(elipse(17.5, 21 + y, 6.5, 4.5), LARANJA)
    t.pinta(elipse(17, 23 + y, 5, 2), LARANJA_SOMBRA)
    t.pinta(elipse(16, 19.5 + y, 3, 1.2), LARANJA_LUZ)
    # hastes e olhos
    olhar = (1, -.6) if pose == "queda" else (1, 0)
    t.parte(linha(19, 17 + y, 18, 11 + y, 1), LARANJA)
    t.parte(linha(23, 17 + y, 24, 10 + y, 1), LARANJA)
    olho(t, 18, 8.5 + y, 2.6, olhar)
    olho(t, 24.5, 8 + y, 2.8, olhar)
    # pinças: a de trás menor, a da frente desproporcional
    if pose.startswith("ataque"):
        estende = 3 if pose == "ataque2" else 1
        t.parte(linha(21, 22 + y, 24 + estende, 21 + y, 2), LARANJA_SOMBRA)
        t.parte(elipse(27 + estende, 20 + y, 4, 3), LARANJA)
        t.parte(triangulo((27 + estende, 16 + y), (32, 14 + y), (30 + estende, 19 + y)), LARANJA)
        t.pinta(elipse(26 + estende, 19 + y, 2, 1), LARANJA_LUZ)
    else:
        t.parte(elipse(23.5, 25 + y, 3, 2.3), LARANJA_SOMBRA)
        t.parte(elipse(26.5, 21.5 + y, 4, 3.4), LARANJA)
        t.parte(triangulo((25, 18 + y), (30, 16.5 + y), (29, 20 + y)), LARANJA)
        t.pinta(elipse(25.5, 20.5 + y, 2, 1.2), LARANJA_LUZ)
    t.salva(nome)


def concha_comum(nome="concha-comum"):
    t = Tela(32)
    t.parte(elipse(10, 16, 8.5, 8.3), AREIA)
    t.pinta(elipse(8, 19, 6.5, 4.5), AREIA_SOMBRA)
    t.pinta(elipse(9, 12, 5, 3), LUZ)
    for k in range(3):
        cx = 4 + k * 4
        t.pinta({(cx + int(math.sin(i / 3) * 1.5), 10 + i) for i in range(12)}, AREIA_SOMBRA)
    t.pinta(elipse(17, 21, 1.6, 2.6), CONTORNO)
    t.salva(nome)


def concha_tampinha(nome="concha-tampinha"):
    t = Tela(32)
    corpo = set()
    for yy in range(12, 24):
        meio = (yy - 12) / 11
        x0, x1 = int(6 - meio * 4), int(14 + meio * 4)
        corpo |= {(x, yy) for x in range(x0, x1 + 1)}
    t.parte(corpo, TURQUESA)
    t.pinta(retangulo(5, 13, 15, 14), TURQUESA_LUZ)
    t.pinta(retangulo(3, 20, 17, 22), TURQUESA_SOMBRA)
    # borda serrilhada de metal
    for x in range(2, 19, 2):
        t.parte({(x, 24), (x, 25)}, TURQUESA_SOMBRA)
    for x in range(3, 18, 2):
        t.ponto(x, 24, TURQUESA_LUZ)
    t.salva(nome)


def concha_espiral(nome="concha-espiral"):
    t = Tela(32)
    creme, creme_sombra = hexa("F3E3C3"), hexa("C9A97F")
    t.parte(triangulo((2, 3), (17, 14), (3, 22)) | elipse(10, 18, 7.5, 7), creme)
    t.pinta(elipse(9, 21, 6, 3.5), creme_sombra)
    # linha da espiral
    for i in range(70):
        ang = i / 7
        r = 1 + i / 9.5
        x = 9 + math.cos(ang) * r * .9 - i / 14
        yy = 16 + math.sin(ang) * r * .8 - i / 12
        t.ponto(int(x), int(yy), creme_sombra)
    t.pinta(elipse(16.5, 21, 1.6, 2.4), CONTORNO)
    t.salva(nome)


# ---------------------------------------------------------------- Inimigos

def agua_viva(nome, fase, carregada):
    t = Tela(32)
    sino = hexa("7FE3F0", 220) if not carregada else hexa("CFFAFF", 240)
    tent = hexa("4FC3D6", 230) if not carregada else hexa("FFE45C")
    for k, x0 in enumerate((10, 14, 18, 22)):
        pontos = set()
        for i in range(14):
            onda = math.sin((i + fase * 3 + k * 2) / 2.2) * 1.3
            pontos.add((int(x0 + onda), 15 + i))
        t.parte(pontos, tent)
    bell = {p for p in elipse(16, 13, 10, 8) if p[1] <= 15}
    t.parte(bell, sino)
    t.pinta({p for p in elipse(16, 11, 6, 4) if p[1] <= 12}, hexa("E8FDFF", 230))
    t.pinta({(x, 15) for x in range(7, 26) if x % 3 == fase % 3}, TURQUESA_SOMBRA)
    if carregada:
        random.seed(fase + 7)
        for _ in range(9):
            t.ponto(random.randint(3, 28), random.randint(2, 30), hexa("FFF27A"))
    t.salva(nome)


BAIACU = hexa("8C9A3E")
BAIACU_SOMBRA = hexa("5E6B2A")
BAIACU_BARRIGA = hexa("D9D59A")


def baiacu(nome, fase):
    t = Tela(32)
    t.parte(triangulo((2, 12 + fase), (8, 17), (2, 22 - fase)), BAIACU_SOMBRA)
    t.parte(elipse(16, 17, 9, 7), BAIACU)
    t.pinta(elipse(17, 21, 6, 2.5), BAIACU_BARRIGA)
    for (x, y) in ((10, 10), (15, 9), (20, 10), (24, 14), (9, 23), (21, 24)):
        t.ponto(x, y, CONTORNO)
    olho(t, 20, 14, 2.4, (1, 0))
    t.pinta(linha(17, 10, 22, 12), CONTORNO)  # sobrancelha brava
    t.parte({(24, 18 + fase), (25, 18 + fase)}, BAIACU_SOMBRA)
    t.salva(nome)


def baiacu_inflado(nome):
    t = Tela(32)
    for ang in range(0, 360, 30):
        a = math.radians(ang)
        t.parte(linha(16 + math.cos(a) * 10, 16 + math.sin(a) * 10, 16 + math.cos(a) * 15, 16 + math.sin(a) * 15), LUZ)
    t.parte(elipse(16, 16, 11, 11), BAIACU)
    t.pinta(elipse(16, 21, 8, 4), BAIACU_BARRIGA)
    olho(t, 21, 12, 2.8, (0, 0))
    t.pinta(linha(17, 7, 23, 9), CONTORNO)
    t.pinta(elipse(24, 18, 1.5, 1), CONTORNO)
    t.salva(nome)


def espinho(nome="espinho"):
    t = Tela(16)
    t.parte(triangulo((2, 6), (15, 8), (2, 10)), LUZ)
    t.salva(nome)


# ---------------------------------------------------------------- Moradores da vila

def vovo_buzia():
    t = Tela(48)
    concha, concha_sombra = hexa("D8A86A"), hexa("A87A45")
    t.parte(triangulo((20, 2), (34, 34), (6, 40)) | elipse(20, 34, 13, 9), concha)
    for i in range(5):
        t.pinta(linha(10 + i * 2, 38 - i * 7, 30 - i, 36 - i * 7), concha_sombra)
    corpo = hexa("C8642E")
    t.parte(linha(40, 25, 43, 47, 2), hexa("8A5A33"))  # bengala de madeira
    for ax in (26, 29, 32):
        t.parte(linha(ax, 42, ax + 1, 47), LARANJA_SOMBRA)
    t.parte(elipse(32, 40, 7, 4.5), corpo)
    t.parte(linha(33, 37, 33, 28), corpo)
    t.parte(linha(37, 37, 38, 27), corpo)
    olho(t, 33, 26, 2.6, (1, .4))
    olho(t, 38.5, 25, 2.6, (1, .4))
    t.pinta(linha(30, 23, 35, 23), corpo)  # pálpebras caídas
    t.pinta(linha(36, 22, 41, 22), corpo)
    t.parte(elipse(40, 39, 3, 2.5), corpo)
    t.salva("vovo-buzia")


def ostracia():
    t = Tela(48)
    casca, casca_sombra = hexa("A99BB8"), hexa("7C6E8E")
    manto = hexa("F2D6E0")
    t.parte({p for p in elipse(24, 21, 18, 12) if p[1] <= 24}, casca)
    for x in range(10, 40, 5):
        t.pinta(linha(24, 11, x, 23), casca_sombra)
    t.parte(elipse(24, 33, 15, 7), manto)
    olho(t, 19, 31, 2.3, (0, 0))
    olho(t, 29, 31, 2.3, (0, 0))
    t.pinta(linha(21, 36, 27, 36), CONTORNO)
    t.ponto(20, 35, CONTORNO)
    t.ponto(28, 35, CONTORNO)
    t.parte({p for p in elipse(24, 40, 19, 7) if p[1] >= 39}, casca)
    t.pinta(linha(7, 42, 41, 42), casca_sombra)
    for x in (8, 14, 34, 40):
        t.parte(elipse(x, 45, 2, 2), LUZ)
    t.salva("ostracia")


def dona_lagosta():
    t = Tela(48)
    verm, verm_sombra = hexa("D9483B"), hexa("9C2F2A")
    t.parte(linha(26, 8, 34, 1), verm_sombra)
    t.parte(linha(21, 8, 15, 0), verm_sombra)
    for i in range(4):
        t.parte(elipse(18 - i * 3, 40 + (i % 2), 4, 3.5), verm_sombra if i % 2 else verm)
    t.parte(elipse(24, 28, 7.5, 12), verm)
    t.parte(retangulo(19, 26, 29, 39), hexa("3B2F3F"))  # avental de trabalho
    t.pinta(retangulo(21, 28, 27, 30), hexa("5B4A5E"))
    for ax in (20, 24, 28):
        t.parte(linha(ax, 40, ax + 1, 47), verm_sombra)
    olho(t, 21, 14, 2.4, (1, 0))
    olho(t, 27, 14, 2.4, (1, 0))
    t.pinta(linha(18, 11, 23, 12), CONTORNO)  # cara emburrada
    t.pinta(linha(25, 12, 30, 11), CONTORNO)
    t.parte(elipse(10, 22, 5, 4) | triangulo((6, 18), (2, 12), (10, 18)), verm)
    t.parte(linha(37, 26, 41, 14, 2), hexa("7A5A3A"))  # cabo do martelo
    t.parte(retangulo(36, 9, 45, 14), hexa("8C97A3"))
    t.parte(elipse(38, 26, 4.5, 4), verm)
    t.salva("dona-lagosta")


def bolota(nome="bolota"):
    t = Tela(32)
    ouro, ouro_sombra = hexa("E7B648"), hexa("B88A2E")
    t.parte(triangulo((4, 15), (9, 19), (4, 23)), ouro_sombra)
    for (x, y) in ((11, 11), (16, 10), (21, 11), (25, 15)):
        t.parte({(x, y)}, ouro_sombra)
    t.parte(elipse(16, 20, 9, 8), ouro)
    t.pinta(elipse(17, 24, 6, 3), LUZ)
    olho(t, 15, 17, 2.8, (0, -1))
    olho(t, 21, 17, 2.8, (0, -1))
    t.pinta(linha(17, 23, 19, 22), CONTORNO)
    t.parte({(24, 21), (25, 22)}, ouro_sombra)
    t.salva(nome)


# ---------------------------------------------------------------- Itens e objetos

def perola(nome, brilho):
    t = Tela(16)
    t.parte(elipse(8, 8, 5, 5), hexa("F4ECFF"))
    t.pinta(elipse(9, 10, 3, 2), hexa("C9B8E8"))
    t.pinta(retangulo(5, 5, 6, 6), BRANCO)
    if brilho:
        for p in ((12, 3), (13, 2), (14, 3), (13, 4)):
            t.ponto(*p, BRANCO)
    t.salva(nome)


def bolha(nome, fase):
    t = Tela(32)
    r = 10 + fase * .6
    anel = elipse(16, 16, r, r) - elipse(16, 16, r - 1.5, r - 1.5)
    t.pinta(elipse(16, 16, r, r), hexa("BFF4FF", 70))
    t.pinta(anel, hexa("DFFBFF", 230))
    t.pinta(elipse(11, 11, 2.5, 2), BRANCO)
    t.pinta(elipse(20, 20, 3, 3) - elipse(20, 20, 2, 2), hexa("DFFBFF", 160))
    t.salva(nome)


def alga_item(nome="alga"):
    t = Tela(16)
    verde, verde_claro = hexa("3E9B4F"), hexa("7AD17C")
    t.parte(elipse(6, 8, 3, 6) | elipse(10, 9, 3, 5), verde)
    t.pinta(linha(6, 3, 6, 13), verde_claro)
    t.pinta(linha(10, 5, 10, 13), verde_claro)
    t.salva(nome)


def bau(nome, aberto):
    t = Tela(32)
    madeira, madeira_sombra, metal = hexa("8A5A33"), hexa("5E3B22"), hexa("C9B26B")
    t.parte(retangulo(5, 18, 26, 30), madeira)
    t.pinta(retangulo(5, 26, 26, 30), madeira_sombra)
    if aberto:
        t.parte(retangulo(5, 8, 26, 13), madeira_sombra)
        t.pinta(retangulo(7, 16, 24, 18), hexa("F4ECFF"))
    else:
        t.parte(elipse(15.5, 18, 11, 5) & retangulo(0, 12, 31, 18), madeira)
    t.parte(retangulo(14, 18, 17, 22), metal)
    t.salva(nome)


def saida(nome="saida"):
    t = Tela(32, 64)
    ferrugem, ferrugem_sombra = hexa("9C5A3C"), hexa("6B3A2A")
    t.parte(retangulo(2, 14, 29, 63), ferrugem)
    t.parte(retangulo(0, 10, 31, 17), ferrugem_sombra)
    t.pinta(retangulo(6, 22, 25, 63), hexa("0B1322"))
    for y in range(26, 60, 10):
        t.pinta(triangulo((11, y), (20, y), (15.5, y + 5)), TURQUESA_LUZ)
    t.salva(nome)


def placa(nome="placa"):
    t = Tela(32)
    t.parte(retangulo(14, 16, 17, 31), hexa("8A5A33"))
    t.parte(retangulo(3, 5, 28, 17), hexa("C49A6C"))
    for y in (9, 13):
        t.pinta(linha(6, y, 25, y), hexa("8A5A33"))
    t.salva(nome)


def coral(nome, cor, galhos):
    t = Tela(32)
    random.seed(nome)
    for i in range(galhos):
        x0 = 8 + i * (16 // max(1, galhos - 1)) if galhos > 1 else 16
        topo = random.randint(6, 14)
        t.parte(linha(16, 31, x0, topo, 2), cor)
        t.parte(elipse(x0, topo, 2, 2), cor)
    t.salva(nome)


def alga_decor(nome, fase):
    t = Tela(32)
    for k, x0 in enumerate((10, 16, 22)):
        pontos = set()
        altura = 22 - k * 3 if k != 1 else 28
        for i in range(altura):
            onda = math.sin((i + fase * 4 + k * 3) / 3) * 1.6 * (i / altura)
            pontos |= {(int(x0 + onda), 31 - i), (int(x0 + onda) + 1, 31 - i)}
        t.parte(pontos, hexa("3E9B4F") if k != 1 else hexa("5BB565"))
    t.salva(nome)


def casa_garrafa(nome="casa-garrafa"):
    t = Tela(64)
    vidro, vidro_luz = hexa("6FBF8F", 235), hexa("BDF2D0")
    t.parte(retangulo(14, 22, 49, 63) | elipse(31.5, 22, 17.5, 10), vidro)
    t.parte(retangulo(26, 2, 37, 14), vidro)
    t.parte(retangulo(24, 0, 39, 4), hexa("C94C3A"))
    t.pinta(retangulo(18, 26, 21, 58), vidro_luz)
    t.parte(retangulo(28, 44, 37, 63), hexa("5E3B22"))
    t.parte(elipse(41, 36, 3.5, 3.5), hexa("FFD68A"))
    t.salva(nome)


def brilho(nome, fase):
    t = Tela(16)
    r = 2 + fase * 2
    for ang in range(0, 360, 45):
        a = math.radians(ang)
        t.ponto(int(8 + math.cos(a) * r), int(8 + math.sin(a) * r), LUZ if fase < 2 else hexa("FFF0CE", 140))
    if fase == 0:
        t.pinta(retangulo(7, 7, 8, 8), BRANCO)
    t.salva(nome)


def caco(nome, cor):
    t = Tela(8)
    t.parte(triangulo((1, 1), (6, 2), (2, 6)), cor)
    t.salva(nome)


# ---------------------------------------------------------------- Água

def agua(nome="agua"):
    t = Tela(32)
    t.pinta(retangulo(0, 0, 31, 31), BRANCO)
    t.salva(nome)


def agua_superficie(nome, fase):
    t = Tela(32, 8)
    for x in range(32):
        y = int(2 + math.sin((x + fase * 8) / 32 * math.pi * 2) * 1.5)
        t.pinta(retangulo(x, y, x, 7), BRANCO)
        t.ponto(x, y, hexa("FFFFFF"))
    t.salva(nome)


# ---------------------------------------------------------------- Arte conceitual para o menu

def conceito_menu():
    origem = os.path.join(RAIZ, "design", "personagens", "04-casco-concha-comum.png")
    img = Image.open(origem).convert("RGBA")
    alfa = img.getchannel("A").point(lambda v: 255 if v > 200 else 0)
    img.putalpha(alfa)
    img = img.crop(alfa.getbbox())
    img = img.resize((img.width // 3, img.height // 3), Image.NEAREST)
    img.save(os.path.join(CONCEITO, "casco-conceito.png"))


def main():
    os.makedirs(SAIDA, exist_ok=True)
    os.makedirs(CONCEITO, exist_ok=True)

    casco("casco-parado-1")
    casco("casco-parado-2", bob=1)
    for i in range(4):
        casco("casco-anda-%d" % (i + 1), bob=i % 2, fase=1 + i % 2)
    casco("casco-pulo", pose="pulo")
    casco("casco-queda", pose="queda")
    casco("casco-ataque-1", pose="ataque1")
    casco("casco-ataque-2", pose="ataque2")
    concha_comum()
    concha_tampinha()
    concha_espiral()

    for f in range(2):
        agua_viva("agua-viva-%d" % (f + 1), f, False)
        agua_viva("agua-viva-carregada-%d" % (f + 1), f, True)
        baiacu("baiacu-%d" % (f + 1), f)
    baiacu_inflado("baiacu-inflado")
    espinho()

    vovo_buzia()
    ostracia()
    dona_lagosta()
    bolota()

    perola("perola-1", False)
    perola("perola-2", True)
    bolha("bolha-1", 0)
    bolha("bolha-2", 1)
    alga_item()
    bau("bau-fechado", False)
    bau("bau-aberto", True)
    saida()
    placa()
    coral("coral-1", hexa("F07C9A"), 3)
    coral("coral-2", hexa("F4A04A"), 2)
    alga_decor("alga-decor-1", 0)
    alga_decor("alga-decor-2", 1)
    casa_garrafa()
    for f in range(3):
        brilho("brilho-%d" % (f + 1), f)
    caco("caco-areia", AREIA)
    caco("caco-turquesa", TURQUESA)
    caco("caco-creme", hexa("F3E3C3"))

    agua()
    agua_superficie("agua-superficie-1", 0)
    agua_superficie("agua-superficie-2", 1)

    conceito_menu()
    print("Arte gerada em", os.path.normpath(SAIDA))


if __name__ == "__main__":
    main()
