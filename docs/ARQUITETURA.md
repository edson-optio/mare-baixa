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

---

# v0.2 — Campanha completa (Recife → Cidade Afundada → Fossa Abissal)

Meta: o GDD até a faixa **Desejável** (p. 10). Extra (Maré Viva, Desafio Diário) fica fora.
Orquestrador: "Claude Code #3" (Maestri). Status do time na nota "Maré Baixa · Time".

## Regras de trabalho
- Cada frente só edita os **seus** arquivos (listados abaixo). Precisa de algo fora deles? Peça ao
  orquestrador com `maestri ask "Claude Code #3" "..."`; não edite o arquivo de outro.
- Compilar: `sh ferramentas/compilar.sh` (tem trava; nunca chame o Unity direto, ele não abre o projeto duas vezes).
  Só o orquestrador gera o app (`compilar.sh build`) e roda o `-selfTest`.
- Nada de commit: o orquestrador commita por marco.
- Arte que ainda não existe: use o nome combinado (tabela "Arte") mesmo assim. `ArtImporter` cria um
  placeholder colorido para qualquer PNG que faltar, e a arte do Gerador-Imagens substitui pelo mesmo nome.
- Prefabs: cada frente monta os prefabs dela num arquivo parcial do builder (`Editor/ProjectBuilder.<Frente>.cs`,
  `static partial class ProjectBuilder`), com os nomes de método fixos abaixo. Helpers disponíveis no builder:
  `S(nome)`, `ArtImporter.Frames(prefixo, n)`, `Sprite(nome, sprite, ordem)`, `Kinematic(go)`,
  `TriggerCircle(go, r)`, `TriggerBox(go, tamanho, offset)`, `Save<T>(go, nomeDoPrefab)`.

## Estrutura da run (dono: orquestrador, `RunManager`)
Uma cena só, `Run` (substitui `RecifeRaso`). O fundo troca por zona.

| Etapa | Salas |
|---|---|
| Recife Raso | `recife-entrada` + 4 sorteadas de `recife-0*` + `recife-concha` em posição aleatória |
| Bolha de ar | `bolha` (maré volta ao nível inicial, sem maré, fonte de cura usável 1×) |
| Cidade Afundada | `cidade-entrada` + 5 sorteadas de `cidade-0*` + `cidade-colosso` (arena) |
| Bolha de ar | `bolha` |
| Fossa Abissal | `fossa-entrada` + 3 sorteadas de `fossa-0*` + `fossa-polvo` (arena) |

As sorteadas são ordenadas por `@nivel` (1 fácil, 2 médio, 3 difícil; empates em ordem aleatória), para a
dificuldade subir dentro de cada zona (GDD p. 6). Vencer o Polvo ⇒ cutscene 4 + créditos ⇒ vila, com todas as
pérolas guardadas. Morrer em qualquer zona ⇒ vila com metade.

API nova: `public enum Zone { Vila, Recife, Cidade, Fossa, Bolha }`;
`RunManager.Plan` (`IReadOnlyList<RunStep>`, `RunStep { TextAsset room; Zone zone; string zoneName; }`),
`RunManager.RoomIndex`, `RunManager.CurrentZone`.

## Salas: legenda nova (dono do parser: orquestrador, `Room.cs`)
Continua tudo o que já existe. Novos caracteres:

| Char | O quê | Spawn em |
|---|---|---|
| `m` | Moreia de Cano (o cano fica no bloco; ela dá o bote para o lado livre) | pés |
| `s` | Siri Saqueador | pés |
| `f` | Peixe-pescador | centro |
| `w` | Tentáculo Sombrio (sai do chão; se o bloco de baixo não for `#` e houver `#` ao lado, sai da parede) | pés |
| `v` | Válvula (atacar baixa a água) | pés |
| `<` `>` | correnteza para a esquerda / direita (cada célula é uma área 1×1 que empurra) | centro |
| `H` | fonte de cura (só na bolha) | pés |
| `B` | chefe da arena: Ermitão Colosso (Cidade) ou Polvo Abissal (Fossa) | pés |
| `l` `i` | Lata de Refrigerante / Lâmpada Velha no chão | pés |
| `r` | concha nova sorteada: Tampinha/Espiral no Recife; Lata/Lâmpada na Cidade e na Fossa | pés |
| `x` `q` `n` `z` | decoração: carro enferrujado, semáforo apagado, coral bioluminescente (emite luz fraca), lixo (garrafa/pneu) | pés |

Dados novos: `@nivel 1|2|3` (salas sorteadas), `@escuro 0..1` (opacidade da escuridão; a Fossa usa 0.9 por padrão).
Arenas: a saída `S` só abre quando o chefe for derrotado (`RoomExit` consulta `Director.Instance.ExitOpen`).

## Conchas (dono: orquestrador, `Shells.cs` e `Casco.cs`)
`public enum ShellType { Nenhuma, Comum, Tampinha, Espiral, Lata, Lampada }` (a ordem antiga fica igual por
causa do save).
- **Lata de Refrigerante**: resistência 6, velocidade 0.8, L = investida (0.35 s, 13 u/s, dano 2, derruba:
  chama `IStunnable.Stun(1.2f)` em quem acertar).
- **Lâmpada Velha**: resistência 3, velocidade 1, L = clarão (raio 4.5, `Stun(2f)` em todo `IStunnable`, e
  `Escuridao.Current?.Flash(1.5f)`). Casco com a Lâmpada tem raio de luz maior no escuro (`Casco.LightRadius`).

API nova de Casco (para inimigos e chefes):
- `bool casco.StealShell(out ShellType type, out int hp)`: tira a concha sem quebrar (o Siri leva). Falso se
  estiver sem concha, rolando ou invulnerável.
- `float casco.LightRadius` (2.5 normal, 4.5 com Lâmpada), `bool casco.Charging` (investida da Lata).
- `void casco.Push(Vector2 velocidade)`: empurrão externo somado ao movimento (correnteza, varrida de tentáculo).

## Combate e mundo: contratos compartilhados
- `public interface IStunnable { void Stun(float seconds); }` em `Scripts/Combat.cs` (orquestrador).
- `Mare.Current.Drain(float blocos, float segundos)`: baixa a água de uma vez e segura `segundos` antes de
  voltar a subir (válvula). `Mare.Current.Speed` pode ser trocado (fase 3 do Polvo).
- `Director.Instance.ExitOpen` (bool), `Director.Instance.OnBossDefeated(Boss boss)`,
  `Director.Instance.OnShellEquipped(ShellType type)` (dispara a cutscene 2 na primeira concha nova).
- Luz: `Escuridao.Current` (pode ser nulo fora da Fossa) e o componente `LightSource { public float radius; }`.
  Quem emite luz só adiciona `LightSource` (peixe-pescador, coral bioluminescente, olhos do Polvo).

## Frente A — Salas (MB Level Designer)
Arquivos: `MareBaixa/Assets/Salas/*.txt`, `ferramentas/validar_salas.py`, `ferramentas/render_salas.py`.

## Frente B — Mundo (MB Gameplay Dev)
`Enemy.cs` (passa a implementar `IStunnable` e o alerta), `Moreia.cs`, `Siri.cs`, `PeixePescador.cs`,
`TentaculoSombrio.cs`, `Valvula.cs`, `Correnteza.cs`, `FonteDeCura.cs`, `RoomExit.cs` (trava da arena), mais os arquivos que já eram da frente B,
e `Editor/ProjectBuilder.Mundo.cs`.

## Frente C — Interface (MB UI Dev)
`CutscenePlayer.cs`, `Cutscenes.cs`, `Escuridao.cs`, `LightSource.cs`, `CreditsScreen.cs`, mais `Hud.cs`,
`UiKit.cs`, `DialogBox.cs`, `ShopPanel.cs`, `ShopEntry.cs`, `MenuScreen.cs`.

## Frente E — Chefes (MB Boss Dev, terminal "Projeto Jogos")
`Boss.cs`, `ErmitaoColosso.cs`, `PolvoAbissal.cs`, `PolvoOlho.cs`, `JatoDeTinta.cs`, `Editor/ProjectBuilder.Chefes.cs`.

## Frente D — Arte (Codex "Gerador-Imagens")
`design/**` e os PNGs novos em `MareBaixa/Assets/Art/**` (lista exata na tarefa D).

## Núcleo (orquestrador)
`Room`, `RunManager`, `VilaManager`, `Director`, `Casco`, `Shells`, `GameRules`, `Progress`, `Kit`, `Mare`,
`Combat`, `GameAudio` (música por zona), `GameVerification`,
`Editor/ProjectBuilder.cs`, `Editor/ArtImporter.cs`.
