# Publicar no itch.io

## 1. Gerar a versão web

Pré-requisito (uma vez): **Unity Hub → Installs → 6000.3.24f1 → ⚙ → Add modules → Web Build Support**.

```sh
UNITY=/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity
$UNITY -batchmode -quit -projectPath "$PWD/MareBaixa" \
  -executeMethod MareBaixa.Editor.WebBuild.BuildAll -logFile Verificacao/unity-web.log
cd Entrega/WebGL && zip -r ../MareBaixa-web.zip . && cd -
```

No editor aberto, o mesmo build está em **Maré Baixa → 3. Gerar versão web (itch.io)**.
Para o download de Mac: `cd Entrega && zip -ry MareBaixa-mac.zip MareBaixa.app`.

## 2. Criar a página

itch.io → **Upload new project**:

| Campo | Valor |
|---|---|
| Title | Maré Baixa |
| Project URL | `mare-baixa` |
| Short description | A maré está subindo. Escolha bem a sua concha. |
| Classification | Games |
| Kind of project | **HTML** |
| Uploads | `MareBaixa-web.zip` → marcar **This file will be played in the browser**; `MareBaixa-mac.zip` → marcar **macOS** |
| Embed options | Viewport **1280 × 720**, marcar **Fullscreen button** e **Automatically start on page load** (opcional) |
| Genre | Platformer |
| Tags | roguelite, pixel-art, 2d, platformer, metroidvania, underwater |
| Input | Keyboard, Xbox controller, PlayStation controller |
| Language | Portuguese (Brazil) |
| Visibility | **Public** (ou *Restricted* com link para o professor, se preferir) |

Capa: 630 × 500. Use `docs/entrega/manual/img/thumb-1-menu.png` recortado, ou a arte da capa do manual.
Screenshots: as três thumbnails de `docs/entrega/manual/img/thumb-*.png` e `hud.png`.

### Descrição da página

```
Numa noite de lua cheia, o Polvo Abissal roubou a Pérola Lunar. Sem ela, a maré não para de subir e a Vila da
Praia está afundando. Casco, um caranguejo-ermitão pequeno o bastante para passar por qualquer fresta, vai descer
pelos recifes de Coralina até a cidade afundada para trazê-la de volta.

• A concha é a classe e a vida: Tampinha (pulo duplo), Espiral (cambalhota), Lata (investida) e Lâmpada (clarão).
  Sem concha, um golpe mata.
• A maré sobe sem parar em cada sala e o ar acaba debaixo d'água.
• Três zonas com salas sorteadas, dois chefes, armas feitas de lixo humano e bancos de descanso.
• Morreu? Volte à vila, gaste as pérolas em melhorias e tente de novo.

Controles: A/D mover · Espaço pular · J atacar · K rolar · L habilidade · E interagir · Q comer alga · Esc pausa.
Controles de Xbox e PlayStation funcionam.

Feito em Unity 6.3 para a disciplina CC7140 — Desenvolvimento de Jogos.
Equipe: Edson Pedreira de Oliveira Júnior, Henrique Bassan Rebechi e Matheo Campanelli de Aquino Esteves.
Código: https://github.com/edson-optio/mare-baixa
```

## 3. Conferir

Abra a página numa aba anônima, clique em **Run game**, jogue até a primeira sala do Recife e teste a tela cheia.
O save no navegador fica no IndexedDB do itch; limpar os dados do site apaga o progresso.
Copie o link para `docs/entrega/INFORMACOES.md`.
