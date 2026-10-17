# Revisão do núcleo nº 1 (level designer, frente A)

Revisão só de leitura de `MareBaixa/Assets/Scripts`: Casco, Room, Mare, RunManager, VilaManager, Director,
Controls, Progress, Effects e GameVerification. Também `MareBaixa/Assets/Editor/ProjectBuilder.cs` e
`ArtImporter.cs`.

Não encontrei nenhum erro de compilação no Unity 6000.3. O que achei são bugs de lógica, em ordem de prioridade.

## Alta

### 1. `Scripts/Casco.cs:278`: corte do pulo estraga o pulo com L e o empurrão de dano

O corte de altura do pulo (soltou o pulo na subida, o pulo fica curto) olha só se Espaço está apertado, e isso
estraga dois movimentos:

- **Pulo com L da Tampinha** (`UseAbility`, linha 331): como o L não é Espaço, o pulo nasce com 12,6 de
  velocidade e é cortado para 5,6 já no primeiro FixedUpdate. O pulo duplo pelo L vira um pulinho de uns
  0,4 bloco, e no chão o L faz um pulo baixo.
- **Empurrão de dano** (`Hurt`, linha 156): a subida de 7 é cortada para 5,6 se o jogador não estiver
  segurando Espaço.

**Correção sugerida:** guardar num campo `cutJump` se o pulo atual veio do Espaço. Ligar o campo em `Jump()`
só quando o aperto foi do Espaço, desligar ao pousar, e cortar só com `cutJump && !Held(Jump)`.

Hoje o teste do pulo duplo não pega isso, porque ele segura Espaço.

### 2. `Scripts/Casco.cs:300-302` e `:348-352`: descer pela plataforma falha debaixo d'água

O colisor volta a valer depois de 0,3 s fixos, mas na água a gravidade é 0,45×. Nesse tempo Casco cai uns
0,8 bloco, quando precisaria cair cerca de 1,6 para passar inteiro pela plataforma (`=`). Na volta da
colisão, a plataforma de mão única pode empurrar Casco para cima de novo.

**Correção sugerida:** só desligar o `IgnoreCollision` quando já tiver passado o tempo e
`Hitbox.Distance(platforms).isOverlapped` for falso.

As salas usam isso: o validador conta com "descer através de =", e os pisos baixos alagam.

## Média

### 3. `Scripts/Casco.cs:268-275`: o buffer de pulo gasta o pulo duplo da Tampinha

Com a Tampinha, apertar o pulo pouco antes de pousar (dentro do buffer de 0,12 s) gasta o pulo duplo no ar,
em vez de virar um pulo normal ao tocar o chão.

**Correção sugerida:** o pulo duplo só vale para um aperto que acontece no próprio quadro com Casco no ar.
Por exemplo, um flag `airJumpRequest` ligado no `ReadActions` quando `!Grounded` e fora do coyote time; o
buffer fica só para o pulo do chão.

### 4. `Scripts/Casco.cs:223-241` com `RunManager.cs:48-61`: o ar acaba durante a troca de sala

O ar continua diminuindo durante a troca de sala. Quem chega à saída com o ar quase zerado pode morrer durante
o escurecimento da tela. Nesse caso a corrotina `Death` roda junto com a `EnterRoom`, e o corpo de Casco é
teletransportado para a sala nova com o ar cheio.

**Correção sugerida:** não gastar ar com `Director.Instance.InputLocked`, ou chamar
`Casco.SetAir(MaxAir)` já no `OnExitReached`, antes do fade.

### 5. `Scripts/Room.cs:42-43`: sala do recife sem `@mare` fica sem maré

Uma sala do recife sem `@mare` fica sem maré nenhuma, sem aviso. A constante `GameRules.ReefTideSpeed` nunca
é usada.

**Correção sugerida:** usar `ReefTideSpeed` quando `zone == Zone.Recife && TideSpeed <= 0`.

Isso não afeta as salas atuais: o meu validador já exige o `@mare`.

## Baixa

### 6. `Scripts/Hint.cs:16-23`: `Near` não pega a dica mais próxima

`Near` devolve a primeira dica da lista, não a mais próxima. Na `recife-entrada`, a dica 4 encobre a 3 numa
faixa de cerca de 1 bloco (x de 22,5 a 23,5).

**Correção sugerida:** devolver a dica mais próxima em x. Se o orquestrador preferir não mexer no código, eu
afasto as dicas na minha sala.

### 7. `Scripts/Mare.cs:69-72`: a água tinge a rocha abaixo do chão

A água é desenhada desde `Bottom - 2` com ordem de desenho 40, por cima dos blocos. Desde o primeiro quadro
ela tinge a rocha abaixo do chão mais baixo. É só visual.

**Correção sugerida:** começar o desenho em `Bottom`, ou cortar no chão.

### 8. `Editor/ArtImporter.cs:68`: `agua-viva-*` importada como textura repetida

`path.Contains("agua")` também pega `agua-viva-*`, que passam a ser importadas como textura repetida
(Repeat). Não causa problema visível hoje.

**Correção sugerida:** testar `name.StartsWith("agua-superficie")`.

## Salas × código: tudo bate

- **Maré:** a água começa em `FloorY - 0,5`, e `FloorY` é o topo de `#` exposto mais baixo, que é o fundo
  baixo de cada sala. Na entrada, por exemplo, é o fundo do buraco.
- **Pivôs:** o C nasce nos pés, e o colisor de Casco vai de 0 a 0,62, então ele já começa apoiado. A saída
  (S, gatilho 1x2) e a descida (D) também ficam nos pés. A distância de interação de 1,1 alcança as duas e as
  conchas no chão.
- **Pulo:** pela física, o pulo alto sobe 2,5 blocos e o alcance horizontal é de cerca de 4,6 blocos no plano
  e 3,4 subindo 2. O validador usa 2 de subida, alcance 3 subindo e vão de até 3, que fica dentro do que a
  física permite (com Espaço segurado, como a dica 1 ensina).
- **Banco de salas:** `recife-0*.txt` tem 5 salas para as 4 que a run sorteia.

**Não é bug:** a morte deixar metade do total (pérolas guardadas mais as da run, `RunManager.cs:132`) segue o
GDD ao pé da letra: "metade das pérolas que não gastou".
