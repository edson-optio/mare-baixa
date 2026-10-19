# Tarefa v0.2 C — Cutscenes, escuridão e interface da campanha (dono: MB UI Dev, terminal "Lanterna")

Leia antes a seção **v0.2** de `docs/ARQUITETURA.md`, o GDD p. 6, 9 e 10 e o código atual de `Hud`, `UiKit`,
`DialogBox`, `MenuScreen`, `Director`, `RunManager`, `Casco`, `Kit`. O orquestrador mexe ao mesmo tempo no
núcleo (`RunManager`, `Kit`, `Casco`...): use a API do contrato e compile com `sh ferramentas/compilar.sh`
quando ele avisar que o núcleo está pronto.

## 1. Cutscenes (GDD p. 9) — `Cutscenes.cs`, `CutscenePlayer.cs`
- `public enum CutsceneId { PerolaRoubada, PrimeiraConcha, FundoDoMar, MareBaixa }`.
- `Cutscenes.cs`: dados estáticos. Cada cena tem 3–6 quadros `{ string image; string text; float seconds; }`
  com o roteiro do GDD p. 9 (escreva os textos curtos, tom leve, para 10+). Nomes das imagens:
  `cena1-1` … `cena1-5`, `cena2-1` … `cena2-3`, `cena3-1` … `cena3-3`, `cena4-1` … `cena4-5`.
- `CutscenePlayer.cs`: `public static IEnumerator Play(CutsceneId id)`. Cria o próprio Canvas por cima de
  tudo (sorting alto), roda em tempo não escalado, quadro em tela cheia com movimento leve (zoom/pan lento,
  estilo HQ), legenda embaixo com máquina de escrever, transição com fade. E/Espaço/Enter avança o quadro;
  **segurar Esc (ou E) por 0.6 s pula a cena toda** (mostre "segure Esc para pular" com um anel enchendo).
  Imagem: `Kit.Current.CutscenePanel(nome)` (o orquestrador cria isso; devolve null se faltar) ⇒ se nula,
  desenhe um quadro de cor da paleta com o texto, para funcionar antes da arte chegar.
  `public static bool Playing`. Para o teste: `public static void SkipCurrent()`.
- Quem chama (orquestrador): 1 ao começar o primeiro jogo (antes da Vovó Búzia), 2 na primeira concha nova
  vestida, 3 na primeira entrada na Fossa, 4 ao vencer o Polvo.

## 2. Créditos — `CreditsScreen.cs`
`public static IEnumerator Play()`: rolagem com título, equipe (CC7140 · `[Nome 1]`, `[Nome 2]`, `[Nome 3]` como
placeholders + "Programação: Edson Pedreira de Oliveira Junior"), agradecimentos e uma mensagem curta sobre o
lixo no mar (pilar "humor com mensagem"). Pulável como a cutscene.

## 3. Escuridão — `Escuridao.cs`, `LightSource.cs`
- Fossa e fase 2 do Polvo (GDD p. 8): a tela fica quase preta, só as luzes aparecem.
- `public static Escuridao Create(float darkness)` (chamado pelo orquestrador ao montar uma sala escura; ele
  destrói junto com a sala), `public static Escuridao Current`, `public float Darkness`,
  `public void SetDarkness(float alvo, float segundos)` (o Polvo usa), `public void Flash(float segundos)`
  (clarão da Lâmpada: clareia tudo e volta).
- `LightSource : MonoBehaviour { public float radius; public float flicker; }` registra-se numa lista estática.
- Técnica sugerida (pipeline 2D embutido, sem URP): uma textura de baixa resolução (ex.: 160×90) redesenhada
  por código a cada quadro, com o escuro e um furo de borda suave para cada `LightSource` na tela, num
  SpriteRenderer preso à câmera (sorting 60, acima da água). Outra técnica serve, desde que fique suave,
  funcione com várias luzes e custe pouco. O HUD continua por cima.
- Casco ganha um `LightSource` com `radius = casco.LightRadius` (o orquestrador adiciona). Deixe a luz de
  Casco num tom quente; a dos bichos, fria.

## 4. HUD e menus — `Hud.cs` e companhia
- **Barra de chefe** embaixo no centro: `Boss.Current` (`DisplayName`, `Hp`, `MaxHp`, `Phase`); entra com fade.
- **Título** já existe; o orquestrador manda "Zona · Sala · n/total".
- **Pausa com mapa** (GDD p. 4: "Pausa e mapa"): na pausa, uma faixa com as etapas de `RunManager.Plan`
  agrupadas por zona (Recife · Bolha · Cidade · Bolha · Fossa), a sala atual marcada (`RoomIndex`), as já
  feitas cheias e as futuras vazias, cor da zona (turquesa, verde-cinza, azul/roxo).
- **Ícones das conchas novas**: `Kit.Current.ShellSprite(ShellType.Lata/Lampada)` já vai existir. O nome da
  habilidade aparece embaixo do nome da concha ("L: investida", "L: clarão").
- **Loja da Dona Lagosta**: entradas para Lata e Lâmpada já vêm do `VilaManager` (orquestrador); confira só
  se o `ShopPanel` aguenta 6+ entradas sem estourar a tela.
- **Menu**: "Novo jogo" pergunta se quer apagar o progresso quando houver save; "Continuar" quando houver.

## Verificação
Rode a cutscene pelo `-selfTest` depois que o orquestrador ligar; deixe `public` o que o teste precisar e
liste no relatório. Relatório curto com `maestri ask "Claude Code #3" "<relatório>"`.
