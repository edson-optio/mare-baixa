using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MareBaixa
{
    // Tela inicial: arte, título, um pouco da história, os controles e os botões. Com progresso
    // salvo aparecem "Continuar" e "Novo jogo" (que pergunta antes de apagar); sem, só "Novo jogo".
    // Teclado: A/D ou W/S escolhe, Enter/E/Espaço confirma. A cena Menu não tem Director; só a câmera.
    public class MenuScreen : MonoBehaviour
    {
        public Sprite keyArt;
        public Sprite conceptArt;

        // Botão principal (Continuar com save, Novo jogo sem); o teste clica nele.
        public Button PlayButton { get; private set; }
        public Button ContinueButton { get; private set; }
        public Button NewGameButton { get; private set; }
        public bool HasSave { get; private set; }
        public bool ConfirmVisible => confirmPanel != null && confirmPanel.activeSelf;
        public string TitleText { get; private set; }
        public string StoryText { get; private set; }
        public string ControlsText { get; private set; }

        Text pearlsLabel;
        bool loading;
        readonly List<Button> buttons = new List<Button>();
        readonly List<System.Action> actions = new List<System.Action>();
        readonly UiKit.NavRepeat nav = new UiKit.NavRepeat(horizontal: true);
        int selected;

        GameObject confirmPanel;
        Button confirmYes, confirmNo;
        int confirmSelected = 1;
        int confirmOpenedFrame = -1;
        readonly UiKit.NavRepeat confirmNav = new UiKit.NavRepeat(horizontal: true);

        // Tem algo a perder: já viu a história, já desceu ou guardou pérolas.
        static bool SaveExists()
        {
            var data = Progress.Data;
            return data.introSeen || data.runs > 0 || data.pearls > 0 || data.tampinhaUnlocked || data.espiralUnlocked;
        }

        void Start()
        {
            Time.timeScale = 1f;
            var root = UiKit.Canvas("Menu");
            BuildBackground(root);

            TitleText = "MARÉ BAIXA";
            UiKit.Label(root, TitleText, 88, UiKit.TopCenter, new Vector2(0f, -28f), new Vector2(1100f, 110f),
                color: GameRules.Orange);
            UiKit.Label(root, "A maré está subindo. Escolha bem a sua concha.", 28, UiKit.TopCenter,
                new Vector2(0f, -138f), new Vector2(1100f, 40f), color: GameRules.Cream);

            var panel = UiKit.Frame(root, UiKit.BottomLeft, new Vector2(40f, 112f), new Vector2(720f, 390f),
                new Color(GameRules.Ink.r, GameRules.Ink.g, GameRules.Ink.b, .82f)).transform;
            StoryText =
                "Numa noite de lua cheia, o Polvo Abissal roubou a Pérola Lunar. Sem ela, a maré não para de subir " +
                "e a Vila da Praia está afundando. Casco, um ermitão pequeno o bastante para passar por qualquer " +
                "fresta, vai descer pelos recifes de Coralina até a cidade afundada para trazê-la de volta.";
            UiKit.Label(panel, StoryText, 22, UiKit.TopLeft, new Vector2(24f, -20f), new Vector2(672f, 130f),
                TextAnchor.UpperLeft);

            UiKit.Label(panel, "CONTROLES", 24, UiKit.TopLeft, new Vector2(24f, -166f), new Vector2(672f, 32f),
                TextAnchor.MiddleLeft, GameRules.Turquoise);
            string left =
                "A / D: mover\n" +
                "Espaço: pular (segure para\npular mais alto)\n" +
                "J: pinças\n" +
                "K: rolar na concha";
            string right =
                "L: habilidade da concha\n" +
                "E: pegar concha / conversar\n" +
                "Q: comer alga\n" +
                "Esc: pausa";
            ControlsText = left + "\n" + right;
            UiKit.Label(panel, left, 21, UiKit.TopLeft, new Vector2(24f, -204f), new Vector2(330f, 170f), TextAnchor.UpperLeft);
            UiKit.Label(panel, right, 21, UiKit.TopLeft, new Vector2(370f, -204f), new Vector2(330f, 170f), TextAnchor.UpperLeft);

            HasSave = SaveExists();
            if (HasSave)
            {
                ContinueButton = AddButton(root, "Continuar", 40f, Play);
                NewGameButton = AddButton(root, "Novo jogo", 420f, AskNewGame);
                PlayButton = ContinueButton;
            }
            else
            {
                NewGameButton = AddButton(root, "Novo jogo", 40f, Play);
                PlayButton = NewGameButton;
            }
            Select(0);
            BuildConfirm(root);

            pearlsLabel = UiKit.Label(root, "", 24, UiKit.BottomRight, new Vector2(-40f, 40f), new Vector2(440f, 40f),
                TextAnchor.MiddleRight, GameRules.Pearl);
            RefreshPearls();
        }

        // A arte-chave cobre a tela toda (o terço de cima fica livre para o título). Sem ela,
        // fundo de mar profundo com a arte conceitual à direita.
        void BuildBackground(RectTransform root)
        {
            var background = UiKit.Stretch("Fundo", root).gameObject.AddComponent<Image>();
            background.raycastTarget = false;
            if (keyArt != null)
            {
                background.sprite = keyArt;
                var fitter = background.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = keyArt.rect.width / Mathf.Max(1f, keyArt.rect.height);
                return;
            }
            background.color = GameRules.DeepSea;
            if (conceptArt != null)
                UiKit.Picture(root, conceptArt, UiKit.Right, new Vector2(-30f, -30f), new Vector2(460f, 500f));
        }

        Button AddButton(RectTransform root, string text, float x, System.Action action)
        {
            int index = buttons.Count;
            var button = UiKit.Button(root, text, UiKit.BottomLeft, new Vector2(x, 28f), () => Choose(index),
                new Vector2(340f, 64f), 30, hover: false);
            UiKit.OnPointer(button.gameObject, EventTriggerType.PointerEnter, () => Select(index));
            buttons.Add(button);
            actions.Add(action);
            return button;
        }

        void Select(int index)
        {
            selected = index;
            for (int i = 0; i < buttons.Count; i++) UiKit.Highlight(buttons[i], i == index);
        }

        void Choose(int index)
        {
            if (ConfirmVisible || loading) return;
            Select(index);
            actions[index]();
        }

        // Janela "Começar do zero?" por cima do menu. "Voltar" vem escolhido para ninguém apagar sem querer.
        void BuildConfirm(RectTransform root)
        {
            var veil = UiKit.Veil(root, .7f);
            veil.raycastTarget = true;
            confirmPanel = veil.gameObject;
            confirmPanel.name = "Confirmar novo jogo";
            var frame = UiKit.Frame(veil.transform, UiKit.Center, Vector2.zero, new Vector2(760f, 320f)).transform;
            UiKit.Label(frame, "Começar do zero?", 44, UiKit.TopCenter, new Vector2(0f, -24f), new Vector2(700f, 56f),
                color: GameRules.Orange);
            int pearls = Progress.Data.pearls;
            UiKit.Label(frame,
                "Isso apaga o progresso salvo" + (pearls > 0 ? " (" + pearls + " pérolas guardadas)" : "") +
                ", as melhorias da vila e as conchas liberadas.",
                24, UiKit.TopCenter, new Vector2(0f, -96f), new Vector2(680f, 90f));
            confirmYes = UiKit.Button(frame, "Apagar e começar", UiKit.BottomCenter, new Vector2(-180f, 32f),
                () => ChooseConfirm(0), new Vector2(320f, 60f), 26, hover: false);
            confirmNo = UiKit.Button(frame, "Voltar", UiKit.BottomCenter, new Vector2(180f, 32f),
                () => ChooseConfirm(1), new Vector2(320f, 60f), 26, hover: false);
            UiKit.OnPointer(confirmYes.gameObject, EventTriggerType.PointerEnter, () => SelectConfirm(0));
            UiKit.OnPointer(confirmNo.gameObject, EventTriggerType.PointerEnter, () => SelectConfirm(1));
            confirmPanel.SetActive(false);
        }

        void AskNewGame()
        {
            confirmOpenedFrame = Time.frameCount;
            confirmPanel.SetActive(true);
            SelectConfirm(1);
        }

        void SelectConfirm(int index)
        {
            confirmSelected = index;
            UiKit.Highlight(confirmYes, index == 0);
            UiKit.Highlight(confirmNo, index == 1);
        }

        void ChooseConfirm(int index)
        {
            if (!ConfirmVisible) return;
            confirmPanel.SetActive(false);
            if (index == 0) NewGame();
        }

        // Novo jogo de verdade: apaga o save e vai para a vila (a Vovó Búzia e a cena 1 voltam).
        public void NewGame()
        {
            Progress.Reset();
            RefreshPearls();
            Play();
        }

        void RefreshPearls()
        {
            int pearls = Progress.Data.pearls;
            pearlsLabel.text = pearls > 0 ? "Pérolas guardadas: " + pearls : "";
        }

        void Play()
        {
            if (loading) return;
            loading = true;
            SceneManager.LoadScene(GameRules.VillageScene);
        }

        void Update()
        {
            if (loading) return;
            if (ConfirmVisible)
            {
                if (Time.frameCount == confirmOpenedFrame) return;
                int side = confirmNav.Read();
                if (side != 0) SelectConfirm(side < 0 ? 0 : 1);
                if (Controls.Pressed(GameAction.Pause)) ChooseConfirm(1);
                else if (UiKit.ConfirmPressed()) ChooseConfirm(confirmSelected);
                return;
            }
            int step = nav.Read();
            if (step != 0 && buttons.Count > 0) Select((selected + step + buttons.Count) % buttons.Count);
            if (UiKit.ConfirmPressed() && buttons.Count > 0) Choose(selected);
        }
    }
}
