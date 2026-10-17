using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MareBaixa
{
    // Tela inicial: arte, título, um pouco da história, os controles e os botões para jogar
    // ou apagar o progresso. A cena Menu não tem Director; só a câmera.
    public class MenuScreen : MonoBehaviour
    {
        const float ConfirmWindow = 3f;
        const string ResetLabel = "Apagar progresso";

        public Sprite keyArt;
        public Sprite conceptArt;

        public Button PlayButton { get; private set; }
        public Button ResetButton { get; private set; }
        public string TitleText { get; private set; }
        public string StoryText { get; private set; }
        public string ControlsText { get; private set; }

        Text pearlsLabel;
        float resetArmedUntil = -1f;
        bool loading;

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

            PlayButton = UiKit.Button(root, "Jogar  (Enter)", UiKit.BottomLeft, new Vector2(40f, 28f), Play,
                new Vector2(340f, 64f), 30);
            ResetButton = UiKit.Button(root, ResetLabel, UiKit.BottomLeft, new Vector2(420f, 28f), ResetProgress,
                new Vector2(340f, 64f), 24);

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

        // Primeiro clique arma; o segundo, dentro de alguns segundos, apaga de verdade.
        void ResetProgress()
        {
            if (Time.unscaledTime > resetArmedUntil)
            {
                resetArmedUntil = Time.unscaledTime + ConfirmWindow;
                UiKit.SetButtonText(ResetButton, "Tem certeza? Clique de novo");
                return;
            }
            resetArmedUntil = -1f;
            Progress.Reset();
            UiKit.SetButtonText(ResetButton, "Progresso apagado");
            RefreshPearls();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Play();
            if (resetArmedUntil > 0f && Time.unscaledTime > resetArmedUntil)
            {
                resetArmedUntil = -1f;
                UiKit.SetButtonText(ResetButton, ResetLabel);
            }
        }
    }
}
