# Maré Baixa — protótipo jogável (v0.1)

> "A maré está subindo. Escolha bem a sua concha."

Roguelite 2D de plataforma da disciplina CC7140 (Unity 6.3). Este protótipo cobre o **MVP do GDD**:
a Vila da Praia (hub) e uma run completa pelo **Recife Raso**, com conchas, maré, ar e pérolas.

## Jogar

- **Mac:** `Entrega/MareBaixa.app` (gerado pelo build; não precisa do Unity).
- **Unity:** clique duas vezes em `Abrir projeto Unity.command` (ou, no Unity Hub, *Add project from disk* →
  pasta `MareBaixa`). Use o **Unity 6.3 LTS (6000.3.24f1)**. O projeto abre na cena `Menu`; aperte **Play**.
  Na primeira abertura o projeto se monta sozinho (menu **Maré Baixa → 1. Montar projeto**).

| Ação | Teclado | Gamepad |
|---|---|---|
| Mover | A / D ou setas | analógico |
| Pular (segurar = mais alto) · descer de plataforma: ↓ + pular | Espaço / W | A |
| Pinças | J | X |
| Rolar dentro da concha (invulnerável) | K / Shift | B |
| Habilidade da concha | L | Y |
| Vestir concha / conversar / descer | E | RB |
| Comer alga (recupera 1 de resistência) | Q | LB |
| Pausa | Esc / P | Start |

## O que já funciona

- **Vila da Praia:** Vovó Búzia conta a história na primeira visita (substitui a cutscene 1) e dá dicas;
  **Ostrácia** vende melhorias permanentes (+ar, aguentar 1 golpe sem concha, alga inicial); **Dona Lagosta**
  forja a Tampinha e a Espiral e define a concha inicial; Bolota faz piada. Progresso salvo entre sessões.
- **Run no Recife Raso:** entrada fixa (tutorial) + 4 salas sorteadas de um banco de 5 + sala de concha
  garantida em posição aleatória = 6 salas, em ordem diferente a cada run.
- **Casco:** corrida, pulo variável com *coyote time* e *buffer*, pinças, rolamento com invulnerabilidade,
  plataformas atravessáveis, *hit-stop*, tremor de câmera e cacos quando a concha quebra.
- **Conchas (classe e vida):** Comum (3), Tampinha (2, rápida, pulo duplo) e Espiral (4, giro dos dois lados).
  Troca com E; a antiga fica no chão com a resistência que tinha. Sem concha, um golpe mata.
- **Maré e ar:** a água sobe sem parar em cada sala (indicador na lateral). Na água Casco fica 30% mais lento e
  o som fica abafado. Com a cabeça submersa o ar (8 s + melhorias) acaba, e as bolhas recarregam.
- **Inimigos:** Água-viva Errante (choque periódico: ataque entre um choque e outro) e Baiacu Nervoso (incha
  ao apanhar e explode em espinhos: bata e recue). Baús, pérolas com ímã e algas.
- **Morte:** volta à vila com metade das pérolas, em menos de 5 s. Concluir o Recife guarda todas.

Fica para as próximas etapas (GDD): Cidade Afundada e Ermitão Colosso, Fossa Abissal e Polvo, conchas Lata e
Lâmpada, válvula que baixa a água, cutscenes ilustradas e música.

## Organização

```
mare-baixa/
├── MareBaixa/                 projeto Unity
│   └── Assets/
│       ├── Art/               Gerados/ (sprites), Cenarios/ (fundos e menu), Blocos/ (tiles), Retratos/
│       ├── Salas/             vila.txt, recife-entrada.txt, recife-concha.txt, recife-01..05.txt
│       ├── Scripts/           código do jogo (um arquivo por responsabilidade)
│       └── Editor/            ProjectBuilder (monta cenas, prefabs e o Kit) e ArtImporter
├── design/                    personagens, cenários, tiles, retratos e prévias das salas
├── docs/                      ARQUITETURA.md (contrato entre as frentes), tarefas/ e revisoes/
├── ferramentas/               gerar_arte.py, validar_salas.py, render_salas.py
└── Verificacao/               relatório do teste automático e capturas de tela
```

### Criar ou mudar uma sala

As salas são texto: cada caractere é um bloco de 32 px (legenda no topo de `Scripts/Room.cs`). Depois de editar:

```sh
python3 ferramentas/validar_salas.py      # fechada, jogável e com caminho até a saída?
python3 ferramentas/render_salas.py       # gera design/salas-previa/*.png para conferir o visual
```

Toda sala `recife-0*.txt` entra sozinha no banco de sorteio ao montar o projeto.

### Arte

Os fundos, a arte do menu, os blocos de chão e os retratos foram gerados em pixel art a partir das pranchas de
`design/personagens` (prompts e originais em `design/cenarios`, `design/tiles` e `design/retratos`). Para trocar
um sprite, substitua o PNG mantendo o nome e use **Maré Baixa → 1. Montar projeto**. Sons são sintetizados no
código (`GameAudio.cs`).

## Verificação

O app aceita `-selfTest`: joga sozinho o loop inteiro e grava `Verificacao/resultado-testes.json` e as capturas
(menu, vila, lojas, combate, troca de concha, morte por falta de ar, cada sala da run e o fim do Recife).

```sh
UNITY=/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity
$UNITY -batchmode -quit -projectPath "$PWD/MareBaixa" \
  -executeMethod MareBaixa.Editor.ProjectBuilder.BuildAll -logFile Verificacao/unity-build.log
"Entrega/MareBaixa.app/Contents/MacOS/Maré Baixa" -selfTest -testOutput "$PWD/Verificacao" \
  -screen-width 1280 -screen-height 720 -screen-fullscreen 0
```
