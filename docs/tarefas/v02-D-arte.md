# Tarefa v0.2 D — Arte da campanha (dono: Codex "Gerador-Imagens")

Projeto: `~/Desktop/code_local/Desenv. Jogos/mare-baixa`. Siga o mesmo processo e o mesmo padrão de qualidade
das entregas anteriores (`design/cenarios/LEIA-ME.txt`, `design/sprites/LEIA-ME.txt`, `design/tiles`,
`design/retratos`): pixel art, paleta limitada, `prompts.json`, originais guardados, redução só com NEAREST,
`preparar.py` reproduzível, `previa.png` e `validacao.json`. Referência visual dos personagens:
`design/personagens/01..04*.png` (a prancha 03 tem inimigos e chefes). Paleta por zona (GDD p. 10): turquesa no
recife, **verde e cinza na Cidade Afundada**, **azul-marinho e roxo bioluminescente na Fossa Abissal**.

Entregue cada PNG final **direto no caminho do Unity** abaixo (o nome é o contrato com o código; o jogo já usa
um placeholder com o mesmo nome até a sua arte chegar) e guarde o processo em `design/<pasta>`.
Sprites: fundo transparente, contorno `#101B2D`, sem antialiasing; personagens apoiados no chão com os pés
na última linha de pixels. Não edite cenas, scripts nem `.meta`.

## Prioridade 1 — sprites de jogo → `MareBaixa/Assets/Art/Gerados/` (processo em `design/sprites/`)
| Arquivo(s) | Tamanho | O quê |
|---|---|---|
| `concha-lata.png`, `concha-lampada.png` | 32×32 | conchas no mesmo enquadramento das outras (camada sobre o corpo de Casco) |
| `caco-lata.png`, `caco-lampada.png` | 8×8 | cacos quando a concha quebra |
| `moreia-escondida.png`, `moreia-bote.png`, `moreia-volta.png` | 64×32 | moreia saindo de um cano (o cano faz parte do sprite, à esquerda; ela olha para a direita) |
| `siri-1.png`, `siri-2.png`, `siri-garra.png`, `siri-fugindo.png` | 32×32 | siri saqueador (andando de lado, garras abertas, fugindo com uma concha nas costas) |
| `pescador-1.png`, `pescador-2.png`, `pescador-boca.png` | 48×32 | peixe-pescador abissal (corpo escuro, isca luminosa na frente) |
| `tentaculo-1.png`, `tentaculo-2.png` | 32×64 | tentáculo sombrio saindo do chão (base embaixo) |
| `tentaculo-aviso.png` | 32×8 | rachadura brilhando no chão |
| `valvula-1.png`, `valvula-2.png` | 32×32 | registro/válvula de cano enferrujado (girada) |
| `correnteza-1.png`, `correnteza-2.png` | 32×32 | riscos de água em movimento (meio transparentes) |
| `fonte-1.png`, `fonte-seca.png` | 32×32 | fonte de ar/cura da bolha (concha gigante com bolhas) |
| `saida-fechada.png` | 32×64 | a saída atual (`saida.png`) fechada por grade/lixo |
| `colosso-anda-1.png`, `colosso-anda-2.png`, `colosso-rola-1.png`, `colosso-rola-2.png`, `colosso-tonto.png`, `colosso-aviso.png` | 96×80 | Ermitão Colosso com pneu de carro como concha |
| `polvo-corpo.png` | 192×128 | Polvo Abissal (fundo da arena; sem os olhos) |
| `polvo-olho-aberto.png`, `polvo-olho-fechado.png` | 32×32 | olho do polvo (brilha) |
| `polvo-tinta.png` | 16×16 | gota de tinta |
| `carro.png` 64×32, `semaforo.png` 16×48, `coral-bio.png` 32×32, `lixo.png` 32×16 | — | decoração (Cidade e Fossa) |

## Prioridade 2 — blocos → `MareBaixa/Assets/Art/Blocos/` (processo em `design/tiles/`)
`cidade-topo.png`, `cidade-meio.png`, `cidade-plataforma.png`, `fossa-topo.png`, `fossa-meio.png`,
`fossa-plataforma.png` — 32×32, repetíveis como os do recife (asfalto/concreto rachado com musgo; rocha abissal
escura com veios roxos).

## Prioridade 3 — cenários → `MareBaixa/Assets/Art/Cenarios/` (processo em `design/cenarios/`)
640×360, mesmas regras dos existentes: `fundo-cidade.png`, `silhuetas-cidade.png` (prédios, postes, carros),
`fundo-fossa.png`, `silhuetas-fossa.png` (quase preto, poucas silhuetas), `fundo-bolha.png`,
`silhuetas-bolha.png` (bolsão de ar calmo, luz suave).

## Prioridade 4 — cutscenes → `MareBaixa/Assets/Art/Cutscenes/` (processo em `design/cutscenes/`)
640×360, estilo quadrinho em pixel art (GDD p. 9), sem texto na imagem (a legenda é do jogo).
- `cena1-1..5` A Pérola Roubada: vila à noite sob a Pérola Lunar; tentáculo surge da água; a pérola sumindo
  no mar; de manhã a água cobre metade da praia; Vovó Búzia olhando para Casco.
- `cena2-1..3` A Primeira Concha: Casco experimenta uma concha grande demais; tropeça; fica orgulhoso.
- `cena3-1..3` O Fundo do Mar: tudo escuro; dois olhos enormes se abrem ao longe; close dos olhos.
- `cena4-1..5` Maré Baixa: Casco ergue a Pérola Lunar; a água recua e revela a cidade antiga; a vila
  comemora; Bolota oferece uma concha nova; Casco e Bolota no pôr do sol.

Ao terminar cada prioridade, escreva um resumo de uma linha no fim deste arquivo (seção "Entregas") com a
data e a lista dos arquivos. O orquestrador lê o retorno do seu terminal.

## Pedidos adicionais (depois da prioridade 1)
| Arquivo | Tamanho | O quê |
|---|---|---|
| `moreia-corpo.png` | 32×16 | pedaço do corpo da moreia, repetível na horizontal (liga a cabeça ao cano), base na última linha |
| `pescador-isca.png` | 16×16 | a isca luminosa do peixe-pescador, separada do corpo |

## Entregas

- 2026-10-18 — Prioridade 1 concluída: 38 PNGs em `MareBaixa/Assets/Art/Gerados/`: `concha-lata.png`, `concha-lampada.png`, `caco-lata.png`, `caco-lampada.png`, `moreia-escondida.png`, `moreia-bote.png`, `moreia-volta.png`, `siri-1.png`, `siri-2.png`, `siri-garra.png`, `siri-fugindo.png`, `pescador-1.png`, `pescador-2.png`, `pescador-boca.png`, `tentaculo-1.png`, `tentaculo-2.png`, `tentaculo-aviso.png`, `valvula-1.png`, `valvula-2.png`, `correnteza-1.png`, `correnteza-2.png`, `fonte-1.png`, `fonte-seca.png`, `saida-fechada.png`, `colosso-anda-1.png`, `colosso-anda-2.png`, `colosso-rola-1.png`, `colosso-rola-2.png`, `colosso-tonto.png`, `colosso-aviso.png`, `polvo-corpo.png`, `polvo-olho-aberto.png`, `polvo-olho-fechado.png`, `polvo-tinta.png`, `carro.png`, `semaforo.png`, `coral-bio.png`, `lixo.png`. Processo, originais, prompts, encaixes, prévia e validação em `design/sprites/v02/`; dimensões, alpha e apoio no chão validados.

- 2026-10-18 — Prioridade 2 concluída: seis PNGs 32×32 em `MareBaixa/Assets/Art/Blocos/`: `cidade-meio.png`, `cidade-topo.png`, `cidade-plataforma.png`, `fossa-meio.png`, `fossa-topo.png`, `fossa-plataforma.png`. Processo em `design/tiles/v02/`; mosaicos revisados, repetição horizontal/vertical e encaixe topo–miolo verificados.

- 2026-10-18 — Prioridade 3 concluída: seis PNGs 640×360 em `MareBaixa/Assets/Art/Cenarios/`: `fundo-cidade.png`, `silhuetas-cidade.png`, `fundo-fossa.png`, `silhuetas-fossa.png`, `fundo-bolha.png`, `silhuetas-bolha.png`. Processo em `design/cenarios/v02/`; fundos de 24 cores, silhuetas em três tons, alpha e repetição horizontal verificados; composições revisadas em `previa.png`.

- 2026-10-18 — Prioridade 4 concluída: 16 PNGs 640×360 em `MareBaixa/Assets/Art/Cutscenes/`: `cena1-1.png`, `cena1-2.png`, `cena1-3.png`, `cena1-4.png`, `cena1-5.png`, `cena2-1.png`, `cena2-2.png`, `cena2-3.png`, `cena3-1.png`, `cena3-2.png`, `cena3-3.png`, `cena4-1.png`, `cena4-2.png`, `cena4-3.png`, `cena4-4.png`, `cena4-5.png`. Processo completo em `design/cutscenes/`, com originais (inclusive versões descartadas), prompts, preparação NEAREST, prévias e validação; até 48 cores, sem legendas na arte, quatro sequências revisadas.
