# Tarefa A — Salas (dono: MB Level Designer)

Contexto do GDD: Casco desce pelo Recife Raso enquanto a maré sobe sem parar em cada sala; com a cabeça
debaixo d'água o ar (8 s) acaba, e bolhas (O) recarregam. O jogador decide entre explorar (pérolas, baús,
algas e conchas, normalmente nas partes baixas que alagam primeiro) e avançar até a saída. Inimigos do
Recife: Água-viva Errante (j: flutua em zigue-zague ~2 blocos para os lados e dá choque periódico) e Baiacu
Nervoso (b: patrulha ~2.5 blocos para os lados; quando apanha, incha e solta espinhos em volta).

Legenda das salas: topo de `MareBaixa/Assets/Scripts/Room.cs`. Física: `GameRules.cs` e `Casco.cs`.

## Arquivos
- `MareBaixa/Assets/Salas/vila.txt` — Vila da Praia (hub, SEM linha @mare). ~44 x 14. Chão de areia com
  relevo leve, 1–2 plataformas (=) de píer, casas de garrafa (h, ocupa ~2x2 a partir do pé), placas (p).
  Casco (C) perto da esquerda; moradores no chão, espaçados ≥5 blocos: V (Vovó Búzia, perto do início),
  Y (Ostrácia), L (Dona Lagosta), U (Bolota); a descida D no extremo direito (1x2).
  `@dica1 A / D para andar, Espaço para pular. Aperte E perto dos moradores para conversar.` perto do início
  e `@dica2 Aperte E na descida para mergulhar no Recife Raso.` perto da descida.
- `recife-entrada.txt` — primeira sala, sempre igual. `@nome Entrada do Recife`, `@mare 0.08`. Tutorial com
  dicas 1–5 em sequência: andar/pular (segurar = mais alto); J belisca; K rola dentro da concha
  (invulnerável); bolhas recarregam o ar quando a cabeça fica debaixo d'água; a saída (S) leva à próxima
  sala. Um Baiacu OU uma Água-viva, algumas pérolas, uma bolha.
- `recife-concha.txt` — sala de concha garantida. `@nome Gruta da Concha`, `@mare 0.14`. Concha sorteada (r)
  em destaque num nicho. `@dica1 Encoste numa concha e aperte E para trocar. A antiga fica para trás.` e outra
  dica sobre L (habilidade da concha) e Q (comer alga recupera 1 de resistência). Coloque uma alga (a).
- `recife-01.txt` … `recife-05.txt` — banco sorteado. Cada uma com `@nome` próprio e evocativo (ruínas de
  Coralina: ponto de ônibus afundado, praça dos corais, estacionamento submerso...), `@mare` entre 0.14 e
  0.2, dificuldade variada, 2–5 inimigos, pérolas (o), às vezes baú (T) e alga (a), bolhas (O), decoração
  (k K g p). Variem o layout: subida em zigue-zague, caverna larga com plataformas, poço etc.
- `ferramentas/validar_salas.py` — Python 3 sem dependências. Para cada .txt: exatamente um C; recife-* têm
  ao menos um S (vila tem D); sala fechada por # nas bordas; linhas com a mesma largura; C/S/D apoiados em
  # ou =; e ALCANÇABILIDADE de C até S/D por BFS sobre células onde Casco fica em pé (vazia com # ou =
  embaixo e espaço livre acima), movimentos: andar, cair, subir até 2 blocos com deslocamento horizontal
  até 3, saltar vãos de até 3 no mesmo nível, descer através de =. Relatório por sala; código 1 se falhar.

## Regras de layout
- 1 bloco = 32 px. Pulo máximo ≈ 2.5 blocos (use 2), alcance ≈ 4 (use 3). Deixe 2 blocos livres acima de
  onde Casco anda. A tela mostra 20 x 11 blocos. Recife: largura 30–50, altura 14–20.
- A água começa logo abaixo do chão mais baixo e sobe @mare blocos/s (0.16/s ⇒ 10 blocos em ~60 s): a SAÍDA
  fica em lugar médio/alto; tesouros nas partes baixas (decisão explorar x avançar). Bolhas (O) em pontos
  que vão alagar, para resgatar quem demorou.
- Dados (@...) no topo, antes do grid. Só caracteres da legenda.

Rode o validador até tudo passar. Relatório final (curto): arquivos, nome e tamanho de cada sala, saída do
validador.
