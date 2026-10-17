# Tarefa B — Mundo: inimigos, itens e NPCs (dono: MB Gameplay Dev)

Um rascunho anterior já criou `Enemy.cs`, `AguaViva.cs`, `Baiacu.cs` e `Spike.cs`: revise, corrija e
complete. Leia antes `Casco.cs`, `Director.cs`, `Interactable.cs`, `Kit.cs`, `Room.cs`, `Effects.cs`,
`GameRules.cs`, `SpriteAnimator.cs`, `GameAudio.cs`. Unity 6 (`Rigidbody2D.linearVelocity`). Um MonoBehaviour
por arquivo, nome igual ao da classe. Os prefabs são montados pelo orquestrador no `Editor/ProjectBuilder.cs`:
deixe campos públicos para sprites/valores e documente, no comentário do topo, os componentes que o prefab
precisa (ex.: "Prefab: SpriteRenderer + Rigidbody2D Kinematic + CircleCollider2D trigger r=0.4").

- `Enemy.cs`: base abstrata `Enemy : MonoBehaviour, IHittable`. `maxHp`; pisca avermelhado ao apanhar;
  pequeno recuo; ao morrer solta pérolas (`pearlsMin/Max`, `Kit.Current.pearl` espalhadas com impulso) +
  `Effects.Spark` e se destrói. Dano por contato em `OnTriggerStay2D` com Casco ⇒ `casco.Hurt(transform.position)`
  quando `virtual bool ContactHurts`. Corpo Kinematic movido com `MovePosition` em FixedUpdate.
- `AguaViva.cs`: zigue-zague em volta da origem (~2 blocos horizontal, ~0.6 vertical). Ciclo: calma 1.8 s
  (calmFrames, contato inofensivo), aviso 0.4 s (pisca), choque 1.2 s (chargedFrames, ContactHurts, Sfx.Shock).
  Atacada durante o choque: não toma dano e dá choque em Casco. maxHp 2, pérolas 1–2.
- `Baiacu.cs`: patrulha lenta (~1.2 u/s) ±2.5 blocos, balança de leve, flipX na direção; contato sempre machuca.
  Ao apanhar: para, sprite `inflated`, cresce, pavio 1.0 s tremendo, EXPLODE em 8 `Spike` (`Kit.Current.spike`,
  ~6 u/s) com Sfx.Burst e shake leve, desincha 1.5 s antes de poder inchar de novo. maxHp 3, pérolas 2–3.
- `Spike.cs`: projétil trigger Kinematic, `Launch(Vector2 velocity)`, gira para a direção, vive 1.2 s, machuca
  Casco, some ao tocar a layer "Chao".
- `Pearl.cs`: coletável trigger; ímã a menos de 2 blocos; ao tocar ⇒ `Director.Instance.AddPearls(1)`, Spark,
  destrói. `Pop(Vector2 velocity)` para nascer pulando (desacelera e para em ~0.4 s).
- `AlgaPickup.cs`: ao tocar ⇒ `casco.AddAlga()`; só some se der certo. Balança devagar.
- `AirBubble.cs`: balança; ao tocar ⇒ `casco.RefillAir()`, estoura (some + Spark), reaparece em 6 s.
- `Chest.cs`: `IHittable`; um golpe abre (`closedSprite`→`openSprite`), solta 5 pérolas pulando; só uma vez.
- `ShellPickup.cs`: `: Interactable`, `public ShellType type; public int hp = -1;` (-1 = cheia);
  `public static ShellPickup Spawn(ShellType type, int hp, Vector2 feet, Transform parent)` instancia
  `Kit.Current.shellPickup`, define tipo/hp/sprite (`Kit.Current.ShellSprite(type)`). O sprite da concha tem
  pivô nos pés mas o desenho fica ~5 px acima da base: ajuste um filho visual para parecer apoiado no chão e
  balançar 1 px. `Prompt` = "E: vestir <nome> (<hp>/<max>)"; `Interact(casco)` ⇒ `casco.SwapShell(this)`.
- `RoomExit.cs`: trigger; Casco vivo toca ⇒ `Director.Instance.OnExitReached()` uma vez. Brilho pulsando opcional.
- `Descent.cs`: `: Interactable`, Prompt "E: mergulhar no Recife Raso", Interact ⇒ `Director.Instance.OnExitReached()`.
- `Npc.cs`: `public enum NpcKind { Buzia, Ostracia, Lagosta, Bolota }` + `Npc : Interactable` com `kind`,
  `displayName`, `radius` 1.6, Prompt "E: falar com <nome>", Interact ⇒ `Director.Instance.OnTalk(this)`.
  Respira (escala Y 1→1.03), vira para Casco quando perto; Bolota treme de medo de vez em quando.

Som: `Director.Instance?.Sound?.Play(Sfx.X)`. Shake: `Director.Instance.CameraRig.Shake(força, duração)`.
Sorting: itens 10, inimigos 15. Não crie prefabs, cenas nem .meta.

Relatório final: arquivos; para CADA componente a lista exata do prefab (Rigidbody2D, collider e tamanho em
blocos, isTrigger, campos de sprite, sorting order); pendências em outros arquivos.
