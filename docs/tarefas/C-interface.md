# Tarefa C — Interface (dono: MB UI Dev)

Leia antes `Casco.cs`, `Director.cs`, `Kit.cs`, `Controls.cs`, `Progress.cs`, `Shells.cs`, `Mare.cs`, `Hint.cs`,
`Interactable.cs`, `GameRules.cs`, `GameAudio.cs`, `RunManager.cs`, `VilaManager.cs` (estes dois já chamam a API
abaixo — ela precisa bater exatamente). Referência de UI por código: `../lab05/TrilhaDaBolota/Assets/Scripts/`
(`UiKit.cs`, `GameHUD.cs`, `MenuScreen.cs`): UnityEngine.UI legado, `LegacyRuntime.ttf`, Canvas ScreenSpaceCamera,
CanvasScaler 1280x720. Paleta: `GameRules.Ink/Cream/Orange/Turquoise/Sand/Pearl/Danger/DeepSea`.

- `UiKit.cs`: adaptar o do lab05 com as cores de Maré Baixa.
- `ShopEntry.cs`: `public class ShopEntry { public string label; public System.Func<string> detail; public System.Func<bool> onSelect; }`
- `DialogBox.cs`: caixa embaixo: retrato (Sprite, pode ser nulo), nome, texto com máquina de escrever (~60 car/s;
  apertar completa, depois avança), indicador "▼ E". Avança com `Controls.Pressed(GameAction.Interact)`,
  `Controls.Pressed(GameAction.Jump)` ou Enter. Ao terminar chama o callback (pode ser nulo).
- `ShopPanel.cs`: título, saldo (`Director.Instance.PearlsShown` + `Kit.Current.pearlIcon`), lista (label à
  esquerda, detail() à direita), W/S/setas (com repetição controlada) para escolher, E/Enter/Espaço confirma
  (toca Sfx.Buy se onSelect() der true, Sfx.Error se false, e redesenha), Esc/`GameAction.Pause` fecha, entrada
  "Sair" automática no fim, clicável com mouse. Para teste: `public int Selected`, `public void Select(int i)`,
  `public void Confirm()`.
- `Hud.cs`: `public class Hud : MonoBehaviour` criado por `AddComponent` em `Director.Awake` (Camera.main já
  existe); monta o Canvas no Awake e lê o estado a cada frame de `Director.Instance` e `.Casco` (podem ser nulos).
  * Sup. esquerdo: ícone da concha (`Kit.Current.ShellSprite`; sem concha ⇒ "SEM CONCHA" vermelho piscando),
    nome e pips de resistência (`ShellHp/ShellMaxHp`); abaixo algas (`algaIcon` × n) e pérolas
    (`pearlIcon` × `PearlsShown`).
  * Barra de ar (`bubbleIcon`) visível só com `Air < MaxAir`; vermelha piscando abaixo de 30%.
  * Topo central: `public void SetTitle(string)`.
  * Lateral direita: indicador vertical da maré (preenchimento turquesa = `Mare.Current.Fill`, marcador laranja
    na altura de Casco entre `Mare.Current.Bottom` e `.Top`); escondido se `Mare.Current == null`.
  * Base central: dica (`Hint.Near(casco.Center)`) e, acima, prompt (`casco.Focus?.Prompt`).
  * `public void Flash()`; `public void SetFade(float alpha)`;
    `public System.Collections.IEnumerator Fade(float from, float to, float seconds)` (tempo não escalado).
  * `public void ShowMessage(string title, string body, string footer)` / `public void HideMessage()`.
  * `public void ShowPause(System.Action onResume, System.Action onGiveUp)` / `public void HidePause()` —
    "Continuar", "Desistir (voltar à vila)" e resumo dos controles (funciona com timeScale = 0).
  * `public void ShowDialog(string speaker, Sprite portrait, string[] lines, System.Action onClose)`,
    `public void ShowShop(string title, System.Collections.Generic.List<ShopEntry> entries, System.Action onClose)`,
    `public bool PanelOpen`, `public ShopPanel Shop`, `public DialogBox Dialog`.
  * Leitura para teste: `public string TitleText, HintText, PromptText; public bool AirVisible, MessageVisible`.
- `MenuScreen.cs`: cena Menu (sem Director; Camera.main existe). `public Sprite keyArt; public Sprite conceptArt;`
  keyArt (arte 16:9, terço superior limpo) cobre a tela inteira atrás; se nulo, use conceptArt ao lado. Título
  "MARÉ BAIXA", subtítulo "A maré está subindo. Escolha bem a sua concha.", parágrafo curto de história (o Polvo
  Abissal roubou a Pérola Lunar, a água sobe, Casco desce por Coralina, a cidade afundada), quadro de controles
  (A/D mover, Espaço pular — segurar pula mais alto, J pinças, K rolar na concha, L habilidade da concha, E pegar
  concha/conversar, Q comer alga, Esc pausa), botões "Jogar" (`SceneManager.LoadScene(GameRules.VillageScene)`) e
  "Apagar progresso" (`Progress.Reset()`, pede confirmação com um 2º clique). Pérolas guardadas se > 0. Enter
  também começa. `public Button PlayButton; public string TitleText`.

Português do Brasil acentuado. Relatório final: arquivos, API pública (assinaturas) e pendências.
