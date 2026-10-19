# Tarefa v0.2 E — Chefes: Ermitão Colosso e Polvo Abissal (dono: MB Boss Dev, terminal "Projeto Jogos")

Projeto: `~/Desktop/code_local/Desenv. Jogos/mare-baixa` (Unity 6000.3.24f1, montado por código).
Leia antes: `README.md`, `docs/ARQUITETURA.md` (inteiro, em especial a seção **v0.2**), o GDD p. 8
(`~/Downloads/GDD completo — Maré Baixa.docx`; leia com `textutil -convert txt -stdout`) e o código de
`Enemy`, `Baiacu`, `Spike`, `Casco`, `Mare`, `Director`, `Effects`, `GameAudio`, `Interactable` (IHittable),
`Editor/ProjectBuilder.cs`. Um MonoBehaviour por arquivo, comentários em português, identificadores em inglês.
O orquestrador mexe no núcleo ao mesmo tempo; o MB Gameplay Dev ("Ressaca") faz o `TentaculoSombrio`. Use a API
do contrato e compile com `sh ferramentas/compilar.sh` quando o orquestrador avisar que o núcleo está pronto.

## `Boss.cs` — base
`public abstract class Boss : MonoBehaviour, IHittable, IStunnable`
- `public static Boss Current` (o da sala; nulo sem chefe). `public string DisplayName`, `public int MaxHp`,
  `public int Hp`, `public int Phase` (1..n), `public bool Defeated`.
- Pisca ao apanhar, hit-stop, `Effects.Spark`. `protected abstract bool Vulnerable { get; }` (golpe fora da
  janela faz "tink": `Sfx.Error` + faísca, sem dano). `Stun` só vale onde o chefe disser.
- Intro: ao Casco entrar na arena, 1.2 s de apresentação (câmera treme, nome no HUD via barra de chefe) antes
  de atacar. Derrota: animação curta (pisca, encolhe, cacos/pérolas: 15 pérolas espalhadas),
  `Director.Instance.OnBossDefeated(this)`, `Defeated = true`.
- Contato machuca Casco (`casco.Hurt(pos)`) quando `virtual bool ContactHurts`.

## `ErmitaoColosso.cs` — mini-chefe da Cidade (GDD p. 8)
Caranguejo-ermitão gigante (~3×2.5 blocos) com um pneu de concha. maxHp 12, 2 fases (fase 2 abaixo de 50%).
- Ciclo: anda devagar até Casco ⇒ aviso (se encolhe no pneu, treme 0.7 s) ⇒ **rola** pela arena a 9 u/s
  (fase 2: 12 u/s, quica uma vez) até bater na parede (raycast na layer Chao) ⇒ impacto (shake forte, pedras
  caem do teto: 3 `Spike` para baixo em pontos aleatórios; fase 2: 5) ⇒ **tonto 2.5 s** (estrelinhas; único
  momento vulnerável e `Stun` da Lâmpada/Lata prolonga +1 s) ⇒ levanta.
- Fase 2 também: às vezes pula no lugar e cai fazendo onda de choque no chão (Casco precisa pular).
- Ao vencer: além das pérolas, solta uma concha rara: `ShellPickup.Spawn(ShellType.Lata ou Lampada, -1, ...)`
  (sorteada) e `Progress` marca `colossoDefeated` (o orquestrador cria o campo).
- Sprites: `colosso-anda-1/2`, `colosso-rola-1/2`, `colosso-tonto`, `colosso-aviso`.

## `PolvoAbissal.cs`, `PolvoOlho.cs`, `JatoDeTinta.cs` — chefe final (GDD p. 8)
O corpo fica no fundo da arena (sprite grande, sorting baixo, sem colisão); os dois **olhos** (`PolvoOlho`,
`IHittable`, com `LightSource` fraca) são os alvos, 3 blocos à esquerda e à direita do ponto B, 1 acima.
Cada olho tem 6 de vida; `Hp` do Boss = soma (12). Olhos fechados = invulneráveis.
- **Fase 1** (12→8): tentáculos saem do chão em sequência, com aviso:
  `TentaculoSombrio.Spawn(pes, Vector2.up, sala, true).Emerge(0.8f)` em 3–4 pontos seguidos perto de Casco.
  Entre as ondas, os olhos abrem 3 s.
- **Fase 2** (8→4): jorra tinta (`JatoDeTinta`: projéteis lentos que espirram e somem) e escurece a tela:
  `Escuridao.Current?.SetDarkness(.92f, 1.5f)` (se não houver escuridão na sala, crie com
  `Escuridao.Create(0f)`). Só os olhos e a luz de Casco aparecem. Tentáculos de parede às vezes.
- **Fase 3** (4→0): a água sobe rápido: `Mare.Current.Speed = 0.55f`; o jogador sobe as plataformas e ataca os
  olhos, que ficam abertos mais tempo. Ao vencer, a água recua (`Mare.Current.Speed = -2f` até o chão).
- Derrota ⇒ `OnBossDefeated` (o orquestrador toca a cutscene 4 e os créditos).
- Sprites: `polvo-corpo` (até 192×128 px), `polvo-olho-aberto`, `polvo-olho-fechado`, `polvo-tinta`.

## Prefabs — `Editor/ProjectBuilder.Chefes.cs` (`static partial class ProjectBuilder`)
`GameObject CreateColosso()` e `GameObject CreatePolvo()` (prefab já com os dois olhos filhos), guardados em
`kit.colosso` e `kit.polvo`. Helpers do builder: `S`, `ArtImporter.Frames`, `Sprite`, `Kinematic`,
`TriggerCircle`, `TriggerBox`, `Save<T>` (ver `ProjectBuilder.cs`). Sprites que faltarem viram placeholder.

## Verificação
Deixe `public` o que o teste automático precisar (ex.: `ErmitaoColosso.State`, `ForceDizzy()`,
`PolvoAbissal.ForcePhase(int)`, `PolvoOlho.Open`). Relatório final curto (arquivos, API de teste, valores,
pendências) com `maestri ask "Claude Code #3" "<relatório>"`.
