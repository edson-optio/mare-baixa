#!/usr/bin/env python3
"""Valida os arquivos de sala de MareBaixa/Assets/Salas/*.txt (formato na legenda de Scripts/Room.cs
e na seção v0.2 de docs/ARQUITETURA.md).

Para cada sala confere: um único C; saída (S nas salas da run, D na vila); bordas fechadas por #;
largura das linhas; objetos apoiados; caracteres da legenda; dicas com texto; dados por tipo de sala
(@mare na faixa da zona, @nivel nas sorteadas, bolha sem maré e com H, arenas com um só B); e se dá
para ir andando e pulando de C até a saída (busca sobre as células onde Casco fica em pé).

Também mede a pressão da maré: estima quando Casco chega a cada célula do caminho mais rápido até a
saída e compara com quando a água cobre a cabeça dele ali mais os 8 s de ar. O índice de dificuldade
soma essa pressão, os inimigos, quanto a saída fica acima do C e o escuro. No fim, confere a curva de dificuldade: dentro de cada zona o índice sobe com o @nivel, e o nível 1 de uma zona fica entre o
nível 1 e o nível 3 da zona anterior.

Uso: python3 ferramentas/validar_salas.py [arquivos...]   (sem argumentos, valida todas as salas)
Sai com código 1 se alguma sala falhar.
"""
import heapq
import math
import re
import sys
from pathlib import Path

PASTA_SALAS = Path(__file__).resolve().parent.parent / "MareBaixa" / "Assets" / "Salas"

LEGENDA = set("#=CSDjboTaOcterkKgphVYLU123456789 msfwv<>HBlixqnz")
SOLIDO = "#"
PLATAFORMA = "="
INIMIGOS = "jbmsfw"
APOIADOS = "CSDTctelirBHvsmkKgphxqzVYLU"   # ficam em pé: precisam de # ou = embaixo (w tem regra própria)

# Física simplificada de Casco, em blocos (pulo real ≈ 2.5 de altura e ≈ 4 de alcance).
SUBIDA_MAX = 2      # altura que dá para subir num pulo
ALCANCE_SUBINDO = 3  # deslocamento horizontal ao subir
ALCANCE_NIVEL = 4    # vão de 3 blocos no mesmo nível (ou descendo)

# Tempo do percurso: velocidade de GameRules.MoveSpeed, gravidade 9.81 × 4, e uma margem para o
# jogador real (hesitar, desviar, bater em inimigo).
VELOCIDADE = 6.5
GRAVIDADE = 9.81 * 4
MARGEM_JOGADOR = 1.6
TEMPO_PULO = 0.45
ALTURA_CABECA = 0.9
MARE_ABAIXO_DO_CHAO = 0.5   # GameRules.TideStartBelowFloor
AR = 8.0                    # segundos de ar com a cabeça debaixo d'água (sem bolha)

FAIXA_MARE = {"recife": (0.12, 0.20), "cidade": (0.20, 0.30), "fossa": (0.30, 0.42)}
MARE_ARENA = {"cidade-colosso": 0.10, "fossa-polvo": 0.08}
TAMANHO = {  # (largura mín, máx, altura mín, máx)
    "recife": (30, 50, 14, 20), "cidade": (36, 56, 16, 24), "fossa": (28, 44, 18, 26),
    "bolha": (20, 30, 10, 14), "cidade-colosso": (38, 44, 13, 16), "fossa-polvo": (28, 36, 22, 26),
}
ESCURO_PADRAO = {"fossa": 0.9}   # o jogo aplica @escuro 0.9 na Fossa quando a sala não diz
ORDEM_ZONAS = ["recife", "cidade", "fossa"]


def tipo_da_sala(nome):
    if nome == "vila":
        return "vila"
    if nome == "bolha":
        return "bolha"
    if nome in MARE_ARENA:
        return "arena"
    if re.fullmatch(r"(recife|cidade|fossa)-\d\d", nome):
        return "sorteada"
    return "fixa"   # entradas e a sala da concha


class Sala:
    def __init__(self, caminho):
        self.caminho = caminho
        self.nome = caminho.stem
        self.zona = self.nome.split("-")[0]
        self.tipo = tipo_da_sala(self.nome)
        self.dados = {}
        self.linhas = []
        for bruta in caminho.read_text(encoding="utf-8").replace("\r", "").split("\n"):
            if bruta.startswith("@"):
                espaco = bruta.find(" ")
                if espaco > 1:
                    self.dados[bruta[1:espaco].strip()] = bruta[espaco + 1:].strip()
                continue
            if bruta.strip() == "":
                continue
            self.linhas.append(bruta)
        self.altura = len(self.linhas)
        self.largura = max((len(l) for l in self.linhas), default=0)

    # (l, c) com l = 0 na linha de cima, como no arquivo.
    def at(self, l, c):
        if l < 0 or l >= self.altura or c < 0:
            return SOLIDO  # fora da sala conta como parede
        linha = self.linhas[l]
        return linha[c] if c < len(linha) else " "

    # Altura dos pés (em blocos, y = 0 embaixo, como no Unity) de quem está na linha l.
    def y(self, l):
        return self.altura - 1 - l

    def livre(self, l, c):
        return self.at(l, c) not in (SOLIDO,)

    def vazio_para_ficar(self, l, c):
        return self.at(l, c) not in (SOLIDO, PLATAFORMA)

    def apoio(self, l, c):
        return self.at(l + 1, c) in (SOLIDO, PLATAFORMA)

    def em_pe(self, l, c):
        return self.vazio_para_ficar(l, c) and self.apoio(l, c)

    def procurar(self, chs):
        return [(l, c) for l, linha in enumerate(self.linhas) for c, x in enumerate(linha) if x in chs]

    # Altura do chão mais baixo (Room.FloorY): topo do # exposto mais baixo.
    def chao_mais_baixo(self):
        alturas = [self.y(l) + 1 for l in range(1, self.altura) for c in range(self.largura)
                   if self.at(l, c) == SOLIDO and self.at(l - 1, c) != SOLIDO]
        return min(alturas, default=0)

    # Cai em linha reta a partir de (l, c) até uma célula com apoio; None se não houver.
    def cair(self, l, c):
        while l < self.altura:
            if not self.vazio_para_ficar(l, c):
                return None
            if self.apoio(l, c):
                return (l, c)
            l += 1
        return None

    def tempo_queda(self, blocos):
        return math.sqrt(2 * max(blocos, 0) / GRAVIDADE)

    # Vizinhos com o tempo aproximado do movimento.
    def vizinhos(self, l, c):
        # andar para o lado, ou sair da borda e cair
        for dc in (-1, 1):
            if self.vazio_para_ficar(l, c + dc):
                destino = self.cair(l, c + dc)
                if destino:
                    yield destino, 1 / VELOCIDADE + self.tempo_queda(destino[0] - l)
        # descer atravessando a plataforma
        if self.at(l + 1, c) == PLATAFORMA:
            destino = self.cair(l + 2, c)
            if destino:
                yield destino, 0.15 + self.tempo_queda(destino[0] - l)
        # pulos
        for subida in range(-self.altura, SUBIDA_MAX + 1):
            alcance = ALCANCE_SUBINDO if subida > 0 else ALCANCE_NIVEL
            for dc in range(-alcance, alcance + 1):
                if subida <= 0 and dc == 0:
                    continue
                alvo = (l - subida, c + dc)
                if not self.em_pe(*alvo):
                    continue
                if self.trajeto_livre(l, c, subida, dc):
                    yield alvo, max(TEMPO_PULO, abs(dc) / VELOCIDADE) + self.tempo_queda(-subida)

    # Trajeto do pulo: sobe na coluna de partida até a altura de travessia, anda na horizontal
    # nessa linha e desce na coluna de chegada. Na subida, = não bloqueia (atravessa por baixo).
    def trajeto_livre(self, l, c, subida, dc):
        if subida >= SUBIDA_MAX:
            topo = l - subida            # pulo no limite: passa raspando na altura do alvo
        elif subida > 0 or abs(dc) > 1:
            topo = l - max(subida, 0) - 1  # arco: precisa de um bloco livre acima
        else:
            topo = l - max(subida, 0)
        for linha in range(topo, l):
            if not self.livre(linha, c):
                return False
        passo = 1 if dc > 0 else -1
        for coluna in range(c, c + dc + passo, passo) if dc else [c]:
            if not self.livre(topo, coluna):
                return False
        alvo_l = l - subida
        for linha in range(topo, alvo_l + 1):
            celula = self.at(linha, c + dc)
            if celula == SOLIDO or (linha > topo and linha <= alvo_l and celula == PLATAFORMA):
                return False
        return True

    # Dijkstra pelo tempo: devolve {célula: (tempo, anterior)}.
    def caminhos(self, inicio):
        melhor = {inicio: (0.0, None)}
        fila = [(0.0, inicio)]
        while fila:
            t, atual = heapq.heappop(fila)
            if t > melhor[atual][0]:
                continue
            for prox, custo in self.vizinhos(*atual):
                nt = t + custo * MARGEM_JOGADOR
                if prox not in melhor or nt < melhor[prox][0]:
                    melhor[prox] = (nt, atual)
                    heapq.heappush(fila, (nt, prox))
        return melhor

    # Maior razão (chegada de Casco) / (água cobrindo a cabeça + ar) ao longo do caminho até a saída.
    def pressao(self, melhor, saida, mare):
        fundo = self.chao_mais_baixo() - MARE_ABAIXO_DO_CHAO
        pior, celula = 0.0, saida
        while celula is not None:
            t, anterior = melhor[celula]
            t_agua = (self.y(celula[0]) + ALTURA_CABECA - fundo) / mare + AR
            pior = max(pior, t / t_agua)
            celula = anterior
        return pior


def indice_dificuldade(pressao, inimigos, subida, escuro):
    return round(10 * pressao + 0.6 * inimigos + 0.2 * max(subida, 0) + 3 * escuro, 1)


def validar(caminho):
    sala = Sala(caminho)
    erros, avisos, infos = [], [], []
    medidas = {}
    vila = sala.tipo == "vila"

    if not sala.linhas:
        return ["sala vazia"], avisos, infos, medidas

    larguras = {len(l) for l in sala.linhas}
    if len(larguras) > 1:
        avisos.append(f"linhas com larguras diferentes: {sorted(larguras)}")

    for l, linha in enumerate(sala.linhas):
        for c, ch in enumerate(linha):
            if ch not in LEGENDA:
                erros.append(f"caractere fora da legenda {ch!r} na linha {l + 1}, coluna {c + 1}")
            if ch.isdigit() and f"dica{ch}" not in sala.dados:
                erros.append(f"dica {ch} sem linha @dica{ch}")

    # bordas
    if any(ch != SOLIDO for ch in sala.linhas[0].ljust(sala.largura)):
        erros.append("teto (primeira linha) não está todo fechado por #")
    if any(ch != SOLIDO for ch in sala.linhas[-1].ljust(sala.largura)):
        erros.append("chão (última linha) não está todo fechado por #")
    for l, linha in enumerate(sala.linhas):
        if linha[:1] != SOLIDO or linha.ljust(sala.largura)[-1] != SOLIDO:
            erros.append(f"lateral aberta na linha {l + 1}")
            break

    # dados
    mare = None
    if "mare" in sala.dados:
        try:
            mare = float(sala.dados["mare"])
            if mare <= 0:
                erros.append("@mare precisa ser positiva")
        except ValueError:
            erros.append(f"@mare inválida: {sala.dados['mare']!r}")
    if sala.tipo in ("vila", "bolha"):
        if "mare" in sala.dados:
            erros.append(f"{'a vila' if vila else 'a bolha'} não tem maré (tire a linha @mare)")
    elif "mare" not in sala.dados:
        erros.append("falta a linha @mare")
    elif mare is not None:
        if sala.tipo == "arena" and abs(mare - MARE_ARENA[sala.nome]) > 1e-6:
            avisos.append(f"@mare da arena deveria ser {MARE_ARENA[sala.nome]}")
        if sala.tipo == "sorteada" and sala.zona in FAIXA_MARE:
            baixo, alto = FAIXA_MARE[sala.zona]
            if not baixo - 1e-6 <= mare <= alto + 1e-6:
                erros.append(f"@mare {mare} fora da faixa da zona ({baixo}–{alto})")

    nivel = None
    if sala.tipo == "sorteada":
        if sala.dados.get("nivel") not in ("1", "2", "3"):
            erros.append("sala sorteada precisa de @nivel 1, 2 ou 3")
        else:
            nivel = int(sala.dados["nivel"])
    elif "nivel" in sala.dados:
        avisos.append("@nivel só vale nas salas sorteadas (zona-NN)")

    if "escuro" in sala.dados:
        try:
            if not 0 <= float(sala.dados["escuro"]) <= 1:
                erros.append("@escuro precisa estar entre 0 e 1")
        except ValueError:
            erros.append(f"@escuro inválido: {sala.dados['escuro']!r}")

    chave_tamanho = sala.nome if sala.nome in TAMANHO else sala.zona
    if chave_tamanho in TAMANHO:
        lmin, lmax, amin, amax = TAMANHO[chave_tamanho]
        if not lmin <= sala.largura <= lmax or not amin <= sala.altura <= amax:
            avisos.append(f"tamanho {sala.largura}x{sala.altura} fora do previsto "
                          f"({lmin}-{lmax} x {amin}-{amax})")
    if "nome" not in sala.dados and not vila:
        avisos.append("falta a linha @nome")

    # objetos especiais por tipo de sala
    chefes = sala.procurar("B")
    if sala.tipo == "arena":
        if len(chefes) != 1:
            erros.append(f"a arena precisa de exatamente um B (achei {len(chefes)})")
    elif chefes:
        erros.append("B (chefe) só pode aparecer nas arenas")
    fontes = sala.procurar("H")
    if sala.tipo == "bolha":
        if not fontes:
            erros.append("a bolha precisa de uma fonte de cura H")
    elif fontes:
        erros.append("H (fonte de cura) só pode aparecer na bolha")

    # apoio
    for ch in APOIADOS:
        for (l, c) in sala.procurar(ch):
            if not sala.apoio(l, c):
                erros.append(f"{ch} na linha {l + 1}, coluna {c + 1} não está apoiado em # ou =")
    for (l, c) in sala.procurar("w"):
        if not (sala.at(l + 1, c) == SOLIDO or SOLIDO in (sala.at(l, c - 1), sala.at(l, c + 1))):
            erros.append(f"w na linha {l + 1}, coluna {c + 1} não sai do chão nem da parede")
    for (l, c) in sala.procurar("m"):
        lados = [sala.at(l, c - 1), sala.at(l, c + 1)]
        if all(x == SOLIDO for x in lados):
            erros.append(f"m na linha {l + 1}, coluna {c + 1} não tem lado livre para o bote")
    for pos in sala.procurar("D"):
        if not sala.vazio_para_ficar(pos[0] - 1, pos[1]):
            erros.append("a descida D precisa de um bloco livre acima")

    # correntezas em faixas de 3 a 8 células
    for l, linha in enumerate(sala.linhas):
        for m in re.finditer(r"<+|>+", linha):
            if not 3 <= len(m.group()) <= 8:
                avisos.append(f"correnteza de {len(m.group())} células na linha {l + 1}, "
                              f"coluna {m.start() + 1} (o previsto é 3 a 8)")

    # início e saída
    inicios = sala.procurar("C")
    if len(inicios) != 1:
        erros.append(f"precisa de exatamente um C (achei {len(inicios)})")
    marca_saida = "D" if vila else "S"
    saidas = sala.procurar(marca_saida)
    if not saidas:
        erros.append(f"falta a {'descida D' if vila else 'saída S'}")

    inimigos = len(sala.procurar(INIMIGOS))
    texto_pressao = ""
    # alcançabilidade
    if len(inicios) == 1 and saidas:
        melhor = sala.caminhos(inicios[0])
        vistos = set(melhor)
        alcancadas = [s for s in saidas if s in vistos]
        if not alcancadas:
            erros.append(f"não há caminho de C até {marca_saida}")
        # itens fora de alcance (só aviso: podem estar em lugares para nadar)
        for ch in "oTarcteliHv":
            for (l, c) in sala.procurar(ch):
                perto = any((l + dl, c + dc) in vistos for dl in range(-1, 3) for dc in range(-1, 2))
                if not perto:
                    avisos.append(f"{ch} na linha {l + 1}, coluna {c + 1} parece fora de alcance")
        # Polvo: os olhos ficam 3 blocos ao lado do B e 1 acima; precisa dar para chegar a menos de 2.
        if sala.nome == "fossa-polvo" and len(chefes) == 1:
            bl, bc = chefes[0]
            for lado in (-3, 3):
                olho = (bl - 1, bc + lado)
                perto = [v for v in vistos if math.hypot(v[0] - 1 - olho[0], v[1] - olho[1]) < 2]
                if not perto:
                    erros.append(f"o olho {'esquerdo' if lado < 0 else 'direito'} do Polvo "
                                 f"(linha {olho[0] + 1}, coluna {olho[1] + 1}) não tem plataforma a menos de 2")
        # válvula longe da saída
        for (l, c) in sala.procurar("v"):
            if alcancadas and min(abs(c - s[1]) for s in alcancadas) < 12:
                avisos.append(f"a válvula na coluna {c + 1} está perto da saída (devia valer o risco)")
        if alcancadas and mare:
            saida = min(alcancadas, key=lambda s: melhor[s][0])
            pressao = sala.pressao(melhor, saida, mare)
            subida = inicios[0][0] - saida[0]
            escuro = float(sala.dados.get("escuro", ESCURO_PADRAO.get(sala.zona, 0)))
            medidas = {"pressao": pressao, "indice": indice_dificuldade(pressao, inimigos, subida, escuro),
                       "tempo": melhor[saida][0]}
            texto_pressao = (f", saída {subida:+d}, percurso {melhor[saida][0]:.0f}s, pressão {pressao:.2f}, "
                             f"índice {medidas['indice']}")

    medidas.update(zona=sala.zona, tipo=sala.tipo, nivel=nivel)
    nome = sala.dados.get("nome", "Vila da Praia" if vila else "?")
    infos.append(f"{nome!r}, {sala.largura}x{sala.altura}, maré {sala.dados.get('mare', '-')}"
                 f"{', nível ' + str(nivel) if nivel else ''}, inimigos {inimigos}, "
                 f"pérolas {len(sala.procurar('o'))}, bolhas {len(sala.procurar('O'))}{texto_pressao}")
    return erros, avisos, infos, medidas


def media(valores):
    return sum(valores) / len(valores) if valores else None


# Curva: por zona, índice médio de cada @nivel.
def conferir_curva(todas):
    erros, linhas = [], []
    curva = {}
    for zona in ORDEM_ZONAS:
        salas = [m for m in todas if m.get("zona") == zona and m.get("tipo") == "sorteada"]
        if not salas:
            continue
        niveis = {n: media([m["indice"] for m in salas if m.get("nivel") == n and "indice" in m])
                  for n in (1, 2, 3)}
        faltando = [n for n, v in niveis.items() if v is None]
        if faltando:
            erros.append(f"{zona}: o banco não tem sala de nível {', '.join(map(str, faltando))}")
            continue
        curva[zona] = niveis
        linhas.append(f"{zona}: nível 1 = {niveis[1]:.1f}, nível 2 = {niveis[2]:.1f}, nível 3 = {niveis[3]:.1f}")
        if not niveis[1] < niveis[2] < niveis[3]:
            erros.append(f"{zona}: o índice não sobe com o nível")
    for anterior, zona in zip(ORDEM_ZONAS, ORDEM_ZONAS[1:]):
        if anterior in curva and zona in curva:
            a, z = curva[anterior], curva[zona]
            if not a[1] < z[1] < a[3]:
                erros.append(f"{zona}: o nível 1 ({z[1]:.1f}) devia ficar entre o nível 1 ({a[1]:.1f}) "
                             f"e o nível 3 ({a[3]:.1f}) de {anterior}")
    return erros, linhas


def main(argumentos):
    arquivos = [Path(a) for a in argumentos] or sorted(PASTA_SALAS.glob("*.txt"))
    if not arquivos:
        print(f"nenhuma sala em {PASTA_SALAS}")
        return 1
    falhas = 0
    todas = []
    for caminho in arquivos:
        erros, avisos, infos, medidas = validar(caminho)
        todas.append(medidas)
        situacao = "FALHOU" if erros else "ok"
        print(f"[{situacao}] {caminho.name}: {' '.join(infos)}")
        for e in erros:
            print(f"    erro: {e}")
        for a in avisos:
            print(f"    aviso: {a}")
        falhas += bool(erros)
    print(f"\n{len(arquivos) - falhas}/{len(arquivos)} salas passaram")
    if not argumentos:
        erros_curva, linhas = conferir_curva(todas)
        print("\nCurva de dificuldade (índice = 10 × pressão + 0,6 × inimigos + 0,2 × subida da saída + 3 × escuro):")
        for linha in linhas:
            print(f"    {linha}")
        for e in erros_curva:
            print(f"    erro: {e}")
        if erros_curva:
            falhas += 1
    return 1 if falhas else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
