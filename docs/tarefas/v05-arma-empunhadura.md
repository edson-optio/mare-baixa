# v0.5 — Empunhadura das armas (dono: "Projeto Jogos")

Pedido do Edson: "a animação do bicho segurando a arma, a espada está muito ruim, parece que ele não está
segurando direito". Vale para as 3 armas de sucata (Espada de Canudo, Lança de Garfo, Martelo de Escova).

## Onde está
- `MareBaixa/Assets/Scripts/Casco.cs` → `AnimateWeapon()` (fim do arquivo) e o `weaponRenderer` criado no `Awake`
  (filho de `visual`, sorting 23). Hoje: posição fixa (0.32, -0.12), ângulo -28° parado e Lerp 65°→-55° no golpe;
  o Garfo só estica 0.45. Não acompanha o bob do corpo (`WalkBob`/`IdleBob`, que só mexe na concha), nem os quadros
  `casco-ataque-1..3`, nem a pinça desenhada no sprite. Por isso parece solto.
- Sprites: `MareBaixa/Assets/Art/Sprites64/arma-canudo|arma-garfo|arma-escova.png` (96×32 a 64 PPU;
  pivô (0.1, 0.5) em `Editor/ArtImporter.cs:129`). Corpo: `casco-parado-1/2`, `casco-anda-1..6`, `casco-ataque-1..3`
  (64×64, pivô nos pés).
- Rastro do golpe: `Effects.Slash` (arco de 55° a -50°).

## O que fazer
1. A arma tem de sair da **pinça** de Casco (ache o pixel da pinça da frente em cada quadro e ancore o cabo ali,
   por quadro, inclusive com o bob de andar/parado e nos quadros de ataque).
2. Golpe com antecipação e follow-through: recua (wind-up curto) → corta rápido → segura um instante → volta.
   O arco do sprite deve bater com o rastro de `Effects.Slash`. Garfo = estocada (recua e estica); Escova = martelada
   de cima para baixo, mais pesada. Parado/andando: arma em posição de guarda, apontando para a frente/cima, sem flutuar.
3. Se precisar, ajuste o pivô do cabo em `ArtImporter.PivotOf` (para o pixel real do cabo em cada sprite) e/ou
   redesenhe os 3 PNGs (pode usar PIL, como `ferramentas/gerar_sucata.py`). Mantenha os nomes.
4. Confira visualmente: gere capturas/tira de quadros (a Run com cada arma, parado, andando e no meio do golpe)
   e olhe as imagens antes de dar por pronto.

## Regras
- Pode editar: `Casco.cs` (só a parte visual da arma; não mude dano/alcance/tempo de `Weapons.cs`),
  `Editor/ArtImporter.cs` (só `PivotOf`), `Art/Sprites64/arma-*.png`, `ferramentas/gerar_sucata.py`.
  Outros arquivos estão sendo editados agora pelo "Claude 1 " (salas, `ProjectBuilder.Mundo.cs`, `CreditsScreen.cs`).
- Compilar só com `sh ferramentas/compilar.sh` (tem trava). Não gere build (`compilar.sh build`) nem commit.
