# Maré Baixa — conchas, sucata e bancos (v0.4)

> "A maré está subindo. Escolha bem a sua concha."

Roguelite 2D de plataforma da disciplina CC7140 (Unity 6.3). Esta versão cobre o GDD até a faixa
**Desejável**: a Vila da Praia (hub) e a campanha inteira, com **Recife Raso → Cidade Afundada (Ermitão
Colosso) → Fossa Abissal (Polvo Abissal)**, as 5 conchas, os 6 inimigos, as 4 cutscenes e música.

## Jogar

- **Mac:** `Entrega/MareBaixa.app` (gerado pelo build; não precisa do Unity).
- **Unity:** clique duas vezes em `Abrir projeto Unity.command` (ou, no Unity Hub, *Add project from disk* →
  pasta `MareBaixa`). Use o **Unity 6.3 LTS (6000.3.24f1)**. O projeto abre na cena `Menu`; aperte **Play**.
  Na primeira abertura o projeto se monta sozinho (menu **Maré Baixa → 1. Montar projeto**).

Conecte o controle por USB ou Bluetooth antes ou durante o jogo. O Input System reconhece
os layouts compatíveis de Xbox e PlayStation; teclado e mouse continuam disponíveis.

| Ação | Teclado | Gamepad (Xbox / PlayStation) |
|---|---|---|
| Mover | A / D ou setas | analógico esquerdo ou direcional |
| Pular (segurar = mais alto) · descer de plataforma: ↓ + pular | Espaço / W | A / × |
| Atacar com a arma equipada (ou pinças) | J | X / quadrado |
| Rolar dentro da concha (invulnerável) | K / Shift | B / círculo |
| Habilidade da concha (pulo duplo, cambalhota, investida ou clarão) | L | Y / triângulo |
| Pegar arma / vestir concha / descansar no banco / conversar / descer | E | RB / R1 |
| Comer alga (recupera 1 de resistência) | Q | LB / L1 |
| Pausa e mapa da run · segurar Esc pula cutscene | Esc / P | Start / Options (segure para pular cutscene) |

Nos menus e lojas, use o analógico/direcional para escolher, **A / ×** para confirmar e
**B / círculo** para voltar. **A / ×** também avança diálogos e cutscenes.

O layout HID USB `2dc8:6007` do 8BitDo Ultimate e o layout Switch Pro usam a disposição
Nintendo: **B** pula/confirma, **A** rola/volta, **Y** ataca e **X** usa a habilidade.
As legendas se ajustam ao controle reconhecido. No Ultimate Bluetooth (chave Bluetooth/2.4G),
a conexão USB pode ser enumerada sem enviar comandos no Mac; teste o pareamento Bluetooth
em modo Switch. O mapeamento USB foi validado com relatórios simulados, e a entrada física
USB ainda não foi confirmada. Outros dispositivos HID genéricos ainda
precisam de um layout específico; conectividade USB por si só não garante compatibilidade.
Para inspecionar os dispositivos reconhecidos no Unity: `sh ferramentas/compilar.sh devices`.
O aplicativo aceita `-inputDiagnostics` para registrar reconhecimento e comandos no log.

## O que já funciona

- **Vila da Praia:** Vovó Búzia conta a história na primeira visita (substitui a cutscene 1) e dá dicas;
  **Ostrácia** vende melhorias permanentes (+ar, aguentar 1 golpe sem concha, alga inicial); **Dona Lagosta**
  forja conchas, vende armas de sucata e define o equipamento inicial; Bolota faz piada. Progresso salvo entre sessões.
- **Run completa (GDD p. 4):** Recife Raso (entrada + 4 sorteadas de 8 + sala de concha) → bolha de ar
  (a água não sobe, fonte de cura) → Cidade Afundada (entrada + 5 sorteadas de 8 + arena do Ermitão
  Colosso) → bolha de ar → Fossa Abissal (entrada + 3 sorteadas de 6 + arena do Polvo Abissal). As
  sorteadas vêm em ordem de dificuldade (`@nivel`), e a maré é lenta, média e rápida em cada zona.
  Há **seis abrigos** ao todo: entre trechos do Recife e da Cidade, entre zonas e antes dos dois chefes.
- **Chefes:** o Ermitão Colosso rola pela arena e fica tonto ao bater na parede; o Polvo Abissal tem 3 fases
  (tentáculos com aviso, tinta que escurece a tela, maré subindo enquanto se ataca os olhos). Vencer o
  Polvo toca a cutscene final e os créditos e guarda todas as pérolas.
- **Casco:** corrida, pulo variável com *coyote time* e *buffer*, pinças, rolamento com invulnerabilidade,
  plataformas atravessáveis, *hit-stop*, tremor de câmera e cacos quando a concha quebra.
- **Conchas (classe e vida):** Comum (3), Tampinha (2, rápida, pulo duplo), Espiral (4, cambalhota ofensiva),
  Lata (6, lenta, investida que atordoa) e Lâmpada (3, clarão que atordoa e ilumina o escuro).
  Troca com E; a antiga fica no chão com a resistência que tinha. Sem concha, um golpe mata.
- **Espiral:** a habilidade salta e avança girando, protege durante a cambalhota e dá **3 de dano**
  nos dois lados, uma vez por inimigo. O rastro turquesa identifica o movimento e o HUD mostra a recarga
  (1,65 s fora d'água). No ar avança sem ganhar outro impulso vertical. O rolamento comum continua em K.
- **Armas de lixo humano:** a **Espada de Canudo** corta rápido (2 de dano, 5 pérolas), a **Lança de Garfo**
  alcança mais longe (2 de dano, 9 pérolas) e o **Martelo de Escova** é mais lento e atordoa
  (3 de dano, 12 pérolas). Estão nas entradas do Recife, Cidade e Fossa, respectivamente.
  E / RB pega a arma; a anterior fica no chão. Comprar na Dona Lagosta libera permanentemente a escolha
  como arma inicial; reequipar na loja é grátis. Armas encontradas acompanham a jornada e ficam no save do banco.
- **Maré e ar:** a água sobe sem parar em cada sala (indicador na lateral). Na água Casco fica 30% mais lento e
  o som fica abafado. Com a cabeça submersa o ar (8 s + melhorias) acaba, e as bolhas recarregam.
- **Inimigos:** Água-viva Errante (choque periódico) e Baiacu Nervoso (incha e explode) no Recife; Moreia de
  Cano (bote; ataque na volta) e Siri Saqueador (rouba a concha; bata nele para recuperar) na Cidade;
  Peixe-pescador (isca luminosa) e Tentáculo Sombrio (aviso no chão) na Fossa escura. Válvulas baixam a
  água mas atraem inimigos; correntezas empurram. Baús, pérolas com ímã e algas.
- **Cutscenes:** A Pérola Roubada (início), A Primeira Concha, O Fundo do Mar e Maré Baixa (vitória), em
  quadros estilo HQ, puláveis. Música sintetizada por zona, abafada debaixo d'água.
- **Bancos / checkpoints:** nos abrigos, use **E / RB** junto ao banco. Casco deita, recupera concha,
  ar e algas e guarda as pérolas, o equipamento e o roteiro da jornada. Sem concha, recebe uma comum.
  Morrer devolve ao último banco com o equipamento salvo; perde apenas as pérolas obtidas depois do descanso.
  **Continuar do banco** no menu retoma o mesmo roteiro mesmo depois de fechar o aplicativo.
  Na pausa, **Menu** mantém o último banco; **Voltar à vila** encerra a jornada e apaga esse checkpoint.
  Sem banco salvo, a morte ainda volta à vila com metade das pérolas. A vitória guarda todas e encerra o checkpoint.

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

Novos símbolos: `R` = banco de descanso (em abrigos sem recompensas), `u` = canudo, `F` = garfo,
`E` = escova. Os sprites desses itens são reproduzíveis com `python3 ferramentas/gerar_sucata.py`.

### Compilar

`sh ferramentas/compilar.sh` monta o projeto em batchmode e mostra só os erros de C# (`build` também gera o
app). Tem trava, para vários agentes compilarem em paralelo sem abrir o Unity duas vezes.

Para testar o backend de controle com um gamepad virtual, execute
`sh ferramentas/compilar.sh gamepad`. O teste cobre botões, segurar/soltar, zona morta,
direcional, navegação e reconexão. Requer uma licença ativa do Unity; ainda é necessário
conferir o controle físico por USB/Bluetooth no sistema de destino.

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
