# v0.5 — Vídeo da gameplay completa: modo `-demo` (piloto automático)

Pedido do Edson: um vídeo da gameplay COMPLETA. Decisão: o app ganha o modo `-demo`, que joga a campanha inteira
sozinho, como um jogador de verdade (sem teleporte), e grava um MP4 1080p 60 fps com áudio.

Comando final (o "Claude 1 " roda):
```
"Entrega/MareBaixa.app/Contents/MacOS/Maré Baixa" -demo -demoOutput "<pasta>" -screen-width 1920 -screen-height 1080 -screen-fullscreen 0
```

## Divisão
| Arquivo | Dono | O quê |
|---|---|---|
| `Scripts/DemoNavegador.cs` | Claude 1 | leva Casco até um ponto da sala (grafo do `validar_salas.py` em C# + controle de pulo) |
| `Scripts/DemoGravador.cs` | Claude 1 | `Time.captureFramerate = 60`, quadros + `AudioRenderer` → ffmpeg → MP4 |
| `Scripts/DemoPiloto.cs` (+ ganchos mínimos em `Casco.cs`/`Director`/etc. se precisar) | Projeto Jogos | o "jogador": fluxo inteiro, combate, chefes, decisões |

## API do navegador (Claude 1 entrega; já pode programar contra ela)
```csharp
public class DemoNavegador
{
    public DemoNavegador(Room room);                 // um por sala (recrie quando Director.Instance.Room mudar)
    // Chame 1× por Update. Escreve Controls.Simulated=true, SimulatedMove, SimulatedDown e Hold/Press(Jump)
    // para andar/pular até o alvo pelo caminho do grafo. Devolve true quando Casco está parado em pé na célula
    // alcançável mais próxima do alvo. Não aperta Attack/Dodge/Ability/Interact (isso é seu).
    public bool Ir(Casco casco, Vector2 alvoMundo);
    public bool Alcancavel(Vector2 alvoMundo);         // existe caminho da posição atual até perto do alvo
    public Vector2 Saida { get; }                       // posição do S (ou D na vila), em coordenadas de mundo
    public void Soltar();                               // zera movimento/pulo simulados (ex.: para atacar parado)
}
```
Coordenadas: célula (x, y) de `Room.At(x, y)` (y = 0 embaixo) ocupa [x, x+1] × [y, y+1] no mundo, somado a
`room.transform.position`.

## O que o DemoPiloto faz (Projeto Jogos)
1. Só existe com `-demo` (como o `GameVerification` só existe com `-selfTest`). **Não pode apagar o save real do
   Edson**: faça como o selfTest faz (save separado ou backup + restaura no fim).
2. Fluxo: Menu (Novo jogo) → Vila com a história da Vovó Búzia (avança diálogos com ritmo de leitura, ~1,5–2,5 s por
   fala, não instantâneo) → passa rápido pela loja/forja (mostra, não precisa comprar) → Descida → run inteira:
   Recife (pega a arma e uma concha nova), bolha (banco/fonte), Cidade, **Ermitão Colosso**, bolha, Fossa,
   **Polvo Abissal**, cutscene final e **créditos inteiros** (sem pular) → volta à vila → sai do app
   (`Application.Quit()`; o gravador fecha o arquivo no `OnApplicationQuit`).
   Cutscenes: deixe tocar inteiras (o vídeo é para apresentação), avançando com o ritmo natural.
3. Em cada sala: alvo padrão = `nav.Saida`. Desvios curtos para pérolas/baús/bolhas de ar alcançáveis perto do
   caminho deixam o vídeo vivo. Inimigo à frente a ~2 blocos: vire para ele e ataque (J); use a habilidade da concha
   de vez em quando (pulo duplo, clarão da Lâmpada no escuro). Coma alga se a concha estiver baixa.
4. Robustez acima de tudo: o vídeo precisa chegar ao fim. Pode ligar um "modo deus" discreto (ex.: `Casco.DemoGod`
   estático que ignora dano e ar **sem piscar**), mas prefira que Casco ainda reaja (knockback leve tudo bem).
   Se travar numa sala por > 25 s, plano B: teleporte curto para a próxima célula do caminho (melhor um corte do que
   travar). Registre no log `[DEMO]` cada sala, tempo e qualquer plano B usado.
5. Chefes: escreva a tática de verdade (Colosso: esquiva da rolada, ataca quando tonto na parede; Polvo: sobe nas
   plataformas e bate nos olhos nas 3 fases). Você conhece Boss/Colosso/Polvo melhor que ninguém.
6. Enquanto o DemoNavegador real não chega, use um stub seu local (não commite por cima do meu arquivo). Eu aviso
   quando `DemoNavegador.cs` e `DemoGravador.cs` estiverem no projeto.

## Regras
- Compilar só com `sh ferramentas/compilar.sh` (trava). Para testar o app, `compilar.sh build` está liberado nesta
  tarefa; rode com `-demo` **sem** `-demoOutput` para não gravar (o gravador só liga com `-demoOutput`).
- Sem commit. Não edite `DemoNavegador.cs`/`DemoGravador.cs`/salas; peça ao Claude 1.
