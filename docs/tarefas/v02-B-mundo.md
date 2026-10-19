# Tarefa v0.2 B — Inimigos e objetos da Cidade e da Fossa (dono: MB Gameplay Dev, terminal "Ressaca")

Leia antes a seção **v0.2** de `docs/ARQUITETURA.md`, o GDD p. 7–8 e o código atual de `Enemy`, `AguaViva`,
`Baiacu`, `Spike`, `Casco`, `Mare`, `Director`, `Interactable`, `Effects`, `GameAudio`, `Editor/ProjectBuilder.cs`.
Unity 6000.3, um MonoBehaviour por arquivo, comentários em português. O orquestrador está mexendo ao mesmo
tempo em `Casco`, `Mare`, `Director`, `Room`, `Kit`, `Shells`: use a API do contrato mesmo que ainda não compile
e confira com `sh ferramentas/compilar.sh` quando o orquestrador avisar que o núcleo está pronto.

## Contratos que você implementa
- `Enemy`: implementa `IStunnable` (`Stun(s)`: para o `Steer`, não machuca no contato, pisca branco/azul;
  dano normal enquanto atordoado). Alerta da válvula: `public static void Alert(Vector2 origem, float raio)`
  ⇒ cada inimigo vivo no raio chama `protected virtual void OnAlert(Vector2 origem)` (padrão: muda o `Home`
  em direção à origem por 4 s).
- `RoomExit`: só deixa sair se `Director.Instance.ExitOpen`; fechada, mostra o sprite `saida-fechada`.

## Inimigos (GDD p. 8). Valores são ponto de partida.
- `Moreia.cs` (`m`): escondida no cano (só um pedaço visível, invulnerável). Casco a menos de 3.5 blocos na
  frente ⇒ aviso 0.35 s (cabeça treme) ⇒ bote 0.25 s (estica 3 blocos, contato machuca) ⇒ volta 0.9 s
  (vulnerável, devagar) ⇒ esconde 1.2 s. maxHp 3, pérolas 2–3. Sprites `moreia-escondida`, `moreia-bote`,
  `moreia-volta`. Atordoada fica fora do cano.
- `Siri.cs` (`s`): anda de lado no chão (gravidade própria por raycast na layer Chao, sem cair de beiradas),
  persegue Casco a menos de 6 blocos. A 1.2 blocos: prepara 0.4 s (garras abertas) ⇒ agarrão. Pegou e Casco
  tem concha ⇒ `casco.StealShell(out t, out hp)` e foge para longe com a concha nas costas, mais rápido.
  Apanhar ou morrer enquanto carrega ⇒ larga a concha (`ShellPickup.Spawn(t, hp, pés, sala)`). Sem concha
  para roubar, o agarrão vira dano normal (`Hurt`). maxHp 3, pérolas 2–4. Sprites `siri-1`, `siri-2`,
  `siri-garra`, `siri-fugindo`.
- `PeixePescador.cs` (`f`): flutua devagar com a isca (`LightSource` num filho, raio 2.5, pisca suave) bem na
  frente da boca; no escuro só a isca aparece. Casco a menos de 4: vira para ele, abre a boca (0.5 s aviso) e
  dá uma mordida-avanço curta. Contato com a boca machuca; de lado/por trás é seguro. O clarão da Lâmpada
  atordoa 2 s (Stun). maxHp 3, pérolas 2–3. Sprites `pescador-1`, `pescador-2`, `pescador-boca`.
- `TentaculoSombrio.cs` (`w`): fica escondido; a cada 3–4.5 s (ou quando Casco passa perto) mostra um aviso no
  chão por 0.8 s (sprite `tentaculo-aviso`, partículas, `Sfx.Rumble` se existir, senão `Sfx.Shock`), sai e varre
  1.2 s (contato machuca e empurra com `casco.Push`), volta. Só é vulnerável enquanto está fora. maxHp 2.
  Sprites `tentaculo-1`, `tentaculo-2`, `tentaculo-aviso`. API para o Polvo usar:
  `public static TentaculoSombrio Spawn(Vector2 pes, Vector2 cima, Transform pai, bool umaVez)` e
  `public void Emerge(float aviso)` (com `umaVez = true` ele sai uma vez e se destrói).

## Objetos
- `Valvula.cs` (`v`, `IHittable`): um golpe gira a válvula (sprite `valvula-1/2`), `Mare.Current?.Drain(3f, 5f)`,
  `Enemy.Alert(pos, 12f)`, tremor de câmera leve, bolhas. Recarrega em 12 s (mostra que está pronta).
- `Correnteza.cs` (`<` `>`): trigger 1×1, `public float direction`; enquanto Casco estiver dentro e com o centro
  na água, `casco.Push(new Vector2(direction * 3.5f, 0))` por FixedUpdate; partículas/riscos animados
  (`correnteza-1/2`, sorting 4, meio transparente).
- `FonteDeCura.cs` (`H`, `: Interactable`): "E: beber da fonte" ⇒ concha volta à resistência máxima
  (`casco.SetShellHp(casco.ShellMaxHp)`) + algas completas; uma vez por bolha (depois "A fonte secou").
  Sem concha: dá a Concha Comum cheia (`ShellPickup.Spawn` no lugar). Sprites `fonte-1`, `fonte-seca`.

## Prefabs — `Editor/ProjectBuilder.Mundo.cs` (`static partial class ProjectBuilder`)
Métodos com estes nomes exatos, cada um devolvendo o prefab salvo:
`GameObject CreateMoray()`, `GameObject CreateCrab()`, `GameObject CreateAngler()`,
`TentaculoSombrio CreateTentacle()`, `GameObject CreateValve()`, `GameObject CreateCurrent()`,
`GameObject CreateHealFont()`. O orquestrador chama e guarda em `kit.moray`, `kit.crab`, `kit.angler`,
`kit.tentacle`, `kit.valve`, `kit.current`, `kit.healFont`. Atualize também o `CreateExit` se a saída
precisar do sprite fechado (diga ao orquestrador o que mudar, o `ProjectBuilder.cs` é dele).

## Verificação
Para cada inimigo, deixe métodos públicos simples para o teste automático (ex.: `Moreia.ForceStrike()`,
`Siri.Grabbing`, `TentaculoSombrio.Out`). Liste-os no relatório.
Relatório final curto (arquivos, API de teste, pendências) com `maestri ask "Claude Code #3" "<relatório>"`.
