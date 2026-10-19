# Maré Baixa — jogo completo (v0.2)

> "A maré está subindo. Escolha bem a sua concha."

Roguelite 2D de plataforma da disciplina CC7140 (Unity 6.3). Esta versão cobre o GDD até a faixa
**Desejável**: a Vila da Praia (hub) e a campanha inteira, com **Recife Raso → Cidade Afundada (Ermitão
Colosso) → Fossa Abissal (Polvo Abissal)**, as 5 conchas, os 6 inimigos, as 4 cutscenes e música.

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
| Habilidade da concha (pulo duplo, giro, investida ou clarão) | L | Y |
| Vestir concha / conversar / descer | E | RB |
| Comer alga (recupera 1 de resistência) | Q | LB |
| Pausa e mapa da run · segurar Esc pula cutscene | Esc / P | Start |

## O que já funciona

- **Vila da Praia:** Vovó Búzia conta a história na primeira visita (substitui a cutscene 1) e dá dicas;
  **Ostrácia** vende melhorias permanentes (+ar, aguentar 1 golpe sem concha, alga inicial); **Dona Lagosta**
  forja a Tampinha e a Espiral e define a concha inicial; Bolota faz piada. Progresso salvo entre sessões.
- **Run completa (GDD p. 4):** Recife Raso (entrada + 4 sorteadas de 8 + sala de concha) → bolha de ar
  (a água não sobe, fonte de cura) → Cidade Afundada (entrada + 5 sorteadas de 8 + arena do Ermitão
  Colosso) → bolha de ar → Fossa Abissal (entrada + 3 sorteadas de 6 + arena do Polvo Abissal). As
  sorteadas vêm em ordem de dificuldade (`@nivel`), e a maré é lenta, média e rápida em cada zona.
- **Chefes:** o Ermitão Colosso rola pela arena e fica tonto ao bater na parede; o Polvo Abissal tem 3 fases
  (tentáculos com aviso, tinta que escurece a tela, maré subindo enquanto se ataca os olhos). Vencer o
  Polvo toca a cutscene final e os créditos e guarda todas as pérolas.
- **Casco:** corrida, pulo variável com *coyote time* e *buffer*, pinças, rolamento com invulnerabilidade,
  plataformas atravessáveis, *hit-stop*, tremor de câmera e cacos quando a concha quebra.
- **Conchas (classe e vida):** Comum (3), Tampinha (2, rápida, pulo duplo), Espiral (4, giro dos dois lados),
  Lata (6, lenta, investida que atordoa) e Lâmpada (3, clarão que atordoa e ilumina o escuro).
  Troca com E; a antiga fica no chão com a resistência que tinha. Sem concha, um golpe mata.
- **Maré e ar:** a água sobe sem parar em cada sala (indicador na lateral). Na água Casco fica 30% mais lento e
  o som fica abafado. Com a cabeça submersa o ar (8 s + melhorias) acaba, e as bolhas recarregam.
- **Inimigos:** Água-viva Errante (choque periódico) e Baiacu Nervoso (incha e explode) no Recife; Moreia de
  Cano (bote; ataque na volta) e Siri Saqueador (rouba a concha; bata nele para recuperar) na Cidade;
  Peixe-pescador (isca luminosa) e Tentáculo Sombrio (aviso no chão) na Fossa escura. Válvulas baixam a
  água mas atraem inimigos; correntezas empurram. Baús, pérolas com ímã e algas.
- **Cutscenes:** A Pérola Roubada (início), A Primeira Concha, O Fundo do Mar e Maré Baixa (vitória), em
  quadros estilo HQ, puláveis. Música sintetizada por zona, abafada debaixo d'água.
- **Morte:** volta à vila com metade das pérolas, em menos de 5 s. Concluir o Recife guarda todas.

Fora do escopo (faixa Extra do GDD): modos Maré Viva e Desafio Diário.

## Organização

```
mare-baixa/
├── MareBaixa/                 projeto Unity
│   └── Assets/
│       ├── Art/               Gerados/ (sprites), Cenarios/ (fundos e menu), Blocos/ (tiles), Retratos/
│       ├── Salas/             vila, bolha, recife-*, cidade-*, fossa-* (.txt)
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

Toda sala `recife-0*.txt`, `cidade-0*.txt` ou `fossa-0*.txt` entra sozinha no banco da zona ao montar o
projeto; `@nivel 1..3` define a ordem dentro da run.

### Compilar

`sh ferramentas/compilar.sh` monta o projeto em batchmode e mostra só os erros de C# (`build` também gera o
app). Tem trava, para vários agentes compilarem em paralelo sem abrir o Unity duas vezes.

### Arte

Os fundos, a arte do menu, os blocos de chão e os retratos foram gerados em pixel art a partir das pranchas de
`design/personagens` (prompts e originais em `design/cenarios`, `design/tiles` e `design/retratos`). Para trocar
um sprite, substitua o PNG mantendo o nome e use **Maré Baixa → 1. Montar projeto**. Arte que ainda não
existe vira um placeholder colorido em `Art/Provisorios` (fora do git). Sons e música são sintetizados no
código (`GameAudio.cs`).

## Verificação

O app aceita `-selfTest`: joga sozinho o loop inteiro e grava `Verificacao/resultado-testes.json` e as capturas
(menu, vila, lojas, combate, as 5 conchas, morte por falta de ar, cada sala das três zonas, os dois chefes,
as cutscenes, os créditos e a vitória).

```sh
UNITY=/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity
$UNITY -batchmode -quit -projectPath "$PWD/MareBaixa" \
  -executeMethod MareBaixa.Editor.ProjectBuilder.BuildAll -logFile Verificacao/unity-build.log
"Entrega/MareBaixa.app/Contents/MacOS/Maré Baixa" -selfTest -testOutput "$PWD/Verificacao" \
  -screen-width 1280 -screen-height 720 -screen-fullscreen 0
```
