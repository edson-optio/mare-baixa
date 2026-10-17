# Maré Baixa — Arquitetura do protótipo (contrato entre as frentes)

Unity **6000.3.24f1**, 2D, Input Manager clássico, namespace `MareBaixa`, código e comentários em
português (identificadores em inglês, como já está). **Um MonoBehaviour por arquivo, com o nome do
arquivo igual ao da classe.** Nada de pacotes extras (só os do `Packages/manifest.json`).
Unity 6: use `Rigidbody2D.linearVelocity`, `FindAnyObjectByType`, `FindObjectsByType(FindObjectsSortMode.None)`.

Referências: GDD em `~/Downloads/GDD completo — Maré Baixa.docx`; direção de arte em
`design/personagens/LEIA-ME.txt`; padrão de projeto herdado de `../lab05` (projeto montado por código).

## Cenas
`Menu` → `Vila` (hub, `VilaManager`) → `RecifeRaso` (run, `RunManager`). As cenas são geradas pelo
`Editor/ProjectBuilder.cs`. As salas são montadas **em tempo de execução** a partir de `Assets/Salas/*.txt`
por `Room.Build` (ver legenda no topo de `Scripts/Room.cs`).

## Escala e camadas
- 1 bloco = 1 unidade = 32 px (PPU 32). Câmera ortográfica 5.625 (tela = 20 × 11.25 blocos).
- Layer 6 `Chao`: chão e plataformas. Física: gravidade -9.81, Casco com gravityScale 4.
- Sorting order: tilemap 0–1, decoração 3, itens 10, inimigos 15, Casco 20/21, efeitos 44–45, água 40–41.
- Sprites de personagens/objetos que ficam no chão têm pivô nos pés; o resto, no centro.

## Já existe (dono: orquestrador)
`GameRules`, `Shells` (`ShellType { Nenhuma, Comum, Tampinha, Espiral }`), `Progress` (`SaveData`),
`Controls` (`Controls.Pressed(GameAction.X)`, `Controls.Held`, `Controls.Move`, `Controls.Down`),
`Kit` (ScriptableObject com prefabs/sprites/tiles; `Kit.Current`), `Director` (base de `VilaManager` e
`RunManager`; `Director.Instance`), `Casco`, `Room`, `Mare` (`Mare.Current.Level/Fill`), `Effects`
(`Effects.Spark(pos)`, `Effects.HitStop(s)`, `Effects.Shards`), `Shard`, `Hint`, `Interactable`,
`IHittable`, `CameraFollow`, `FixedViewport`, `Parallax`, `SpriteAnimator`, `GameAudio`
(`Director.Instance.Sound.Play(Sfx.X)`), e depois `RunManager`, `VilaManager`, `GameVerification`, `Editor/*`.

API de Casco usada pelas outras frentes:
`casco.Hurt(Vector2 origem)`, `casco.Die(string motivo)`, `casco.RefillAir()`, `bool casco.AddAlga()`,
`casco.SwapShell(ShellPickup)`, `casco.Center`, `casco.Shell`, `casco.ShellHp`, `casco.ShellMaxHp`,
`casco.Algae`, `casco.Air`, `casco.MaxAir`, `casco.HeadUnderwater`, `casco.Dead`, `casco.Focus` (Interactable
mais próximo, com `.Prompt`), `casco.Facing`.

API de Director usada pelas outras frentes:
`Director.Instance.AddPearls(int)`, `.OnExitReached()`, `.OnTalk(Npc)`, `.Sound`, `.Hud`, `.Casco`,
`.Room`, `.CameraRig.Shake(força, duração)`, `.PearlsShown`, `.Paused`.

## Frente A — Salas (dono: agente "salas")
Arquivos `MareBaixa/Assets/Salas/*.txt` + `ferramentas/validar_salas.py`.

## Frente B — Mundo: inimigos e itens (dono: agente "mundo")
`Enemy.cs` (base abstrata, `IHittable`), `AguaViva.cs`, `Baiacu.cs`, `Spike.cs`, `Pearl.cs`, `AlgaPickup.cs`,
`AirBubble.cs`, `Chest.cs`, `ShellPickup.cs`, `RoomExit.cs`, `Descent.cs`, `Npc.cs` (`NpcKind`).

## Frente C — Interface (dono: agente "interface")
`UiKit.cs`, `Hud.cs`, `DialogBox.cs`, `ShopPanel.cs`, `ShopEntry.cs`, `MenuScreen.cs`.

## Frente D — Arte de cenário (dono: Codex "Gerador-Imagens")
`design/cenarios/*` (fundos, parallax, arte-chave). O orquestrador importa para `Assets/Art/`.
