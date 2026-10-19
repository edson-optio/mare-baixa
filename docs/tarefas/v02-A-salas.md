# Tarefa v0.2 A — Salas da campanha (dono: MB Level Designer, terminal "Claude 1")

Leia antes a seção **v0.2** de `docs/ARQUITETURA.md` (legenda nova, estrutura da run) e o GDD p. 4–8
(`~/Downloads/GDD completo — Maré Baixa.docx`; use `textutil -convert txt -stdout` para ler).
Mesmas regras de layout da tarefa A original (pulo ≈ 2 de altura, alcance 3, 2 livres acima, tela 20×11).

## 1. Curva de dificuldade (inclui o pedido do Edson: "ajusta a curva de dificuldade das salas")
- Toda sala sorteada ganha `@nivel 1|2|3`. O `RunManager` ordena as sorteadas por nível, então a zona sobe
  de dificuldade dentro da run. Dentro de cada banco, tenha salas dos três níveis.
- Ajuste também `@mare`, quantidade de inimigos e a altura da saída para que o nível 1 de uma zona seja
  mais fácil que o nível 3 da zona anterior, mas mais difícil que o nível 1 da anterior.
- Faixas de `@mare`: Recife 0.12–0.20, Cidade 0.20–0.30, Fossa 0.30–0.42. Arenas: Colosso 0.10; Polvo 0.08
  (na fase 3 o próprio chefe acelera a água).

## 2. Recife: completar o banco para 8 (GDD p. 5 pede 8 a 10)
`recife-06.txt` … `recife-08.txt` (os inimigos continuam j e b). Revise o nível das 01–05.

## 3. Bolha de ar — `bolha.txt`
`@nome Bolha de Ar`, SEM `@mare`. Sala pequena e segura (~24×12): Casco (C) à esquerda, uma fonte de cura `H`
no meio, algumas pérolas, decoração, saída `S`. `@dica1 Aqui a água não sobe. A fonte cura a concha uma vez.`

## 4. Cidade Afundada — `cidade-entrada.txt`, `cidade-01..08.txt`, `cidade-colosso.txt`
Visual (GDD p. 5): ruas, carros enferrujados (x), semáforos apagados (q), apartamentos inundados, lixo (z),
placas (p). Inimigos: Moreia de Cano (m, num bloco junto da parede ou de um cano; dá o bote ~3 blocos para o
lado livre), Siri Saqueador (s, anda no chão e tenta agarrar), mais j/b de vez em quando. Correntezas `<` `>`
(faixas de 3–8 células) em corredores alagados. Válvula `v` em ~1/3 das salas, longe da saída (vale o risco?).
Conchas `l`/`i`/`r` em ~2 salas. Largura 36–56, altura 16–24.
- `cidade-entrada`: `@nome Avenida Submersa`, tutorial com dicas sobre moreia ("espere o bote e ataque quando
  ela recolher"), siri ("ele rouba sua concha: bata nele para recuperar") e válvula ("bater na válvula baixa a
  água, mas o barulho atrai inimigos").
- `cidade-colosso`: `@nome Garagem do Colosso`, arena plana e larga (40×14), paredes `#` nos dois lados (o
  chefe rola até bater nelas), 2–3 plataformas `=` baixas para escapar por cima, chefe `B` à direita, Casco
  à esquerda, saída `S` alta à direita (abre ao vencer), 2 bolhas `O`. Sem outros inimigos.

## 5. Fossa Abissal — `fossa-entrada.txt`, `fossa-01..06.txt`, `fossa-polvo.txt`
Escuridão quase total (o jogo aplica `@escuro 0.9`; use 0.75 na entrada). Corais bioluminescentes `n`
servem de farol: ponha alguns no caminho certo. Inimigos: Peixe-pescador (f, flutua; a luz atrai), Tentáculo
Sombrio (w, no chão ou na parede). Salas mais apertadas e verticais, poços. Largura 28–44, altura 18–26.
- `fossa-entrada`: `@nome Beira do Abismo`, dicas sobre o escuro, o peixe-pescador ("a luz bonita é uma
  armadilha") e o tentáculo ("o chão treme antes: pule").
- `fossa-polvo`: `@nome Covil do Polvo`, ~32×24. Chefe `B` no centro-alto (o corpo do Polvo fica no fundo da
  arena, os dois olhos ficam 3 blocos à esquerda e à direita do B, 1 bloco acima). Plataformas `=` em escada
  dos dois lados até a altura dos olhos (o jogador precisa alcançá-los a menos de 2 blocos), chão largo
  embaixo para a fase 1 (tentáculos), 3 bolhas `O` espalhadas na subida (fase 3 inunda). Saída `S` no alto.

## 6. Validador e prévia
- `validar_salas.py`: legenda nova; `bolha` sem @mare e com H; arenas com exatamente um B; sorteadas com
  `@nivel`; `<`/`>` e `w`/`m` não contam como chão; `B`, `H`, `v`, `s`, `m`, `w`, `l`, `i` apoiados.
- `render_salas.py`: cores para os caracteres novos; gere as prévias de todas as salas.

Relatório final curto: tabela sala × tamanho × @nivel × @mare × inimigos, e a saída do validador.
Quando terminar, mande o relatório com `maestri ask "Claude Code #3" "<relatório>"`.
