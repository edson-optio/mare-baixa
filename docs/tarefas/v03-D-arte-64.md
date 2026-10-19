# Tarefa v0.3 D — Sprites em 64 px (dono: Codex "Gerador-Imagens")

Problema: os sprites de 32×32 perdem quase todo o detalhe das pranchas (`design/personagens/*.png`): o
siri ficou fragmentado e os olhos/texturas somem. Decisão do Edson: **dobrar a resolução dos sprites**.
O tamanho no mundo NÃO muda (o jogo importa estes com 64 pixels por bloco), só o detalhe dobra.
Na tela de 720p cada pixel do sprite vira exatamente 1 pixel da tela.

## Regras
- Pasta nova: `MareBaixa/Assets/Art/Sprites64/`, **mesmos nomes** de `Art/Gerados` (o jogo usa a versão 64
  quando existir e cai na de 32 enquanto não existir). Processo em `design/sprites64/`.
- Canvas = o dobro do atual: 32×32 → 64×64, 48×48 → 96×96, 64×32 → 128×64, 96×80 → 192×160,
  192×128 → 384×256 etc. Pés na última linha; mesmos enquadramentos/pivôs; Casco e conchas continuam em
  camadas no mesmo canvas, encaixe já posicionado (como no LEIA-ME de design/sprites).
- Fidelidade às pranchas `design/personagens/01..04`: silhueta, cores e personalidade iguais às da prancha.
  Paleta limitada (até ~24 cores por personagem), contorno `#101B2D`, sem antialiasing nem dithering
  ruidoso, olhos legíveis, leitura clara de silhueta em fundo escuro e claro.
- Animação: onde hoje há 2 quadros de andar/nadar, entregue 4 (`-1..-4`); Casco andando 6
  (`casco-anda-1..6`), ataque 3 (`casco-ataque-1..3`). Avise no LEIA-ME quais nomes ganharam quadros.

## Etapa 1 — AMOSTRA para aprovação (faça só isto e pare)
`casco-parado-1`, `casco-parado-2`, `casco-anda-1..6`, `concha-comum`, `concha-espiral`,
`siri-1..4`, `siri-garra`. Mais uma prévia `design/sprites64/amostra.png` com: cada sprite novo ampliado 4×
ao lado do antigo (32) ampliado 8× e do recorte correspondente da prancha, e Casco andando por cima de um
pedaço do fundo `fundo-recife` em escala real de jogo (2×). Responda com o caminho da prévia.

## Etapa 2 — APROVADA pelo Edson (amostra aprovada)
Todos os sprites de `Art/Gerados` (personagens, inimigos, chefes, NPCs, itens, decoração, cacos, brilhos,
bolhas, pérolas, água-superfície). Blocos e cenários ficam como estão.
- **Ajuste pedido na caminhada do Casco:** os 6 quadros da amostra quase não mudam entre si e na tela ele
  vai parecer deslizar. Refaça `casco-anda-1..6` com as pernas alternando de forma bem visível (passada
  clara: perna da frente avança enquanto a de trás recua, contato/passagem/contato), bob do corpo de 1–2 px
  nos quadros de passagem, e pinças balançando levemente. Confira num GIF a 12 fps antes de entregar.
- **Conjuntos completos de uma vez:** todos os quadros e todas as conchas de Casco (parado, anda, pulo,
  queda, ataque 1..3; concha comum, tampinha, espiral, lata, lâmpada) entram juntos na pasta; o jogo mistura
  resoluções se um conjunto chegar pela metade. Mesmo critério para cada inimigo e chefe.
- Ordem: Casco + conchas + cacos → inimigos do Recife → Cidade → Fossa → chefes → NPCs da vila → itens e
  decoração. Registre cada grupo em "Entregas".

## Entregas

- 2026-10-18 — Etapa 1 entregue para aprovação do Edson: 15 PNGs 64×64 (`casco-parado-1..2`, `casco-anda-1..6`, `concha-comum`, `concha-espiral`, `siri-1..4`, `siri-garra`) em `MareBaixa/Assets/Art/Sprites64/`. Prévia: `design/sprites64/amostra.png` (novos 4×, antigos 8×, recortes das pranchas e caminhada sobre Recife a 2×). Originais, prompts, preparação reproduzível, GIF e validação em `design/sprites64/`. Etapa 2 não iniciada; aguardando aprovação.
