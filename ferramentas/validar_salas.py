#!/usr/bin/env python3
"""Valida os arquivos de sala de MareBaixa/Assets/Salas/*.txt (formato na legenda de Scripts/Room.cs).

Para cada sala confere: um único C; saída (S nas salas do recife, D na vila); bordas fechadas por #;
largura das linhas; C, S e D apoiados em # ou =; caracteres da legenda; dicas com texto; e se dá para
ir andando e pulando de C até a saída (busca em largura sobre as células onde Casco fica em pé).

Uso: python3 ferramentas/validar_salas.py [arquivos...]   (sem argumentos, valida todas as salas)
Sai com código 1 se alguma sala falhar.
"""
import sys
from collections import deque
from pathlib import Path

PASTA_SALAS = Path(__file__).resolve().parent.parent / "MareBaixa" / "Assets" / "Salas"

LEGENDA = set("#=CSDjboTaOcterkKgphVYLU123456789 ")
SOLIDO = "#"
PLATAFORMA = "="

# Física simplificada de Casco, em blocos (pulo real ≈ 2.5 de altura e ≈ 4 de alcance).
SUBIDA_MAX = 2      # altura que dá para subir num pulo
ALCANCE_SUBINDO = 3  # deslocamento horizontal ao subir
ALCANCE_NIVEL = 4    # vão de 3 blocos no mesmo nível (ou descendo)


class Sala:
    def __init__(self, caminho):
        self.caminho = caminho
        self.nome = caminho.stem
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

    def livre(self, l, c):
        return self.at(l, c) not in (SOLIDO,)

    def vazio_para_ficar(self, l, c):
        return self.at(l, c) not in (SOLIDO, PLATAFORMA)

    def apoio(self, l, c):
        return self.at(l + 1, c) in (SOLIDO, PLATAFORMA)

    def em_pe(self, l, c):
        return self.vazio_para_ficar(l, c) and self.apoio(l, c)

    def procurar(self, ch):
        return [(l, c) for l, linha in enumerate(self.linhas) for c, x in enumerate(linha) if x == ch]

    # Cai em linha reta a partir de (l, c) até uma célula com apoio; None se não houver.
    def cair(self, l, c):
        while l < self.altura:
            if not self.vazio_para_ficar(l, c):
                return None
            if self.apoio(l, c):
                return (l, c)
            l += 1
        return None

    def vizinhos(self, l, c):
        # andar para o lado, ou sair da borda e cair
        for dc in (-1, 1):
            if self.vazio_para_ficar(l, c + dc):
                destino = self.cair(l, c + dc)
                if destino:
                    yield destino
        # descer atravessando a plataforma
        if self.at(l + 1, c) == PLATAFORMA:
            destino = self.cair(l + 2, c)
            if destino:
                yield destino
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
                    yield alvo

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

    def alcancavel(self, inicio):
        vistos = {inicio}
        fila = deque([inicio])
        while fila:
            atual = fila.popleft()
            for prox in self.vizinhos(*atual):
                if prox not in vistos:
                    vistos.add(prox)
                    fila.append(prox)
        return vistos


def validar(caminho):
    sala = Sala(caminho)
    erros, avisos, infos = [], [], []
    vila = sala.nome == "vila"

    if not sala.linhas:
        return [f"sala vazia"], avisos, infos

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
    if vila:
        if "mare" in sala.dados:
            erros.append("a vila não tem maré (tire a linha @mare)")
    else:
        if "mare" not in sala.dados:
            erros.append("falta a linha @mare")
        else:
            try:
                mare = float(sala.dados["mare"])
                if mare <= 0:
                    erros.append("@mare precisa ser positiva")
            except ValueError:
                erros.append(f"@mare inválida: {sala.dados['mare']!r}")
        if not 30 <= sala.largura <= 50 or not 14 <= sala.altura <= 20:
            avisos.append(f"tamanho {sala.largura}x{sala.altura} fora do previsto (30-50 x 14-20)")
    if "nome" not in sala.dados and not vila:
        avisos.append("falta a linha @nome")

    # início e saída
    inicios = sala.procurar("C")
    if len(inicios) != 1:
        erros.append(f"precisa de exatamente um C (achei {len(inicios)})")
    marca_saida = "D" if vila else "S"
    saidas = sala.procurar(marca_saida)
    if not saidas:
        erros.append(f"falta a {'descida D' if vila else 'saída S'}")
    for ch, pos in [("C", p) for p in inicios] + [(marca_saida, p) for p in saidas]:
        if not sala.apoio(*pos):
            erros.append(f"{ch} na linha {pos[0] + 1}, coluna {pos[1] + 1} não está apoiado em # ou =")
    for pos in sala.procurar("D"):
        if not sala.vazio_para_ficar(pos[0] - 1, pos[1]):
            erros.append("a descida D precisa de um bloco livre acima")

    # alcançabilidade
    if len(inicios) == 1 and saidas:
        vistos = sala.alcancavel(inicios[0])
        if not any(s in vistos for s in saidas):
            erros.append(f"não há caminho de C até {marca_saida}")
        # itens fora de alcance (só aviso: podem estar em lugares para nadar)
        for ch in "oTarcte":
            for (l, c) in sala.procurar(ch):
                perto = any((l + dl, c + dc) in vistos for dl in range(-1, 3) for dc in range(-1, 2))
                if not perto:
                    avisos.append(f"{ch} na linha {l + 1}, coluna {c + 1} parece fora de alcance")

    nome = sala.dados.get("nome", "Vila da Praia" if vila else "?")
    mare = sala.dados.get("mare", "-")
    infos.append(f"{nome!r}, {sala.largura}x{sala.altura}, maré {mare}, "
                 f"inimigos {sum(len(sala.procurar(x)) for x in 'jb')}, "
                 f"pérolas {len(sala.procurar('o'))}, bolhas {len(sala.procurar('O'))}")
    return erros, avisos, infos


def main(argumentos):
    arquivos = [Path(a) for a in argumentos] or sorted(PASTA_SALAS.glob("*.txt"))
    if not arquivos:
        print(f"nenhuma sala em {PASTA_SALAS}")
        return 1
    falhas = 0
    for caminho in arquivos:
        erros, avisos, infos = validar(caminho)
        situacao = "FALHOU" if erros else "ok"
        print(f"[{situacao}] {caminho.name}: {' '.join(infos)}")
        for e in erros:
            print(f"    erro: {e}")
        for a in avisos:
            print(f"    aviso: {a}")
        falhas += bool(erros)
    print(f"\n{len(arquivos) - falhas}/{len(arquivos)} salas passaram")
    return 1 if falhas else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
