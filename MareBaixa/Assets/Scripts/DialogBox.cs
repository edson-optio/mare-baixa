using UnityEngine;
using UnityEngine.UI;

namespace MareBaixa
{
    // Caixa de diálogo embaixo da tela: retrato, nome e o texto aparecendo letra a letra.
    // E, Espaço ou Enter completa a fala; apertando de novo, passa para a próxima.
    // O Hud cria e monta (Build); o objeto fica desligado enquanto não há conversa.
    public class DialogBox : MonoBehaviour
    {
        const float CharsPerSecond = 60f;
        const float Width = 1120f;
        const float Height = 200f;
        const float PortraitSpace = 200f;

        Image portraitBack, portraitImage;
        Text nameLabel, bodyLabel, arrowLabel;
        string[] lines = new string[0];
        int lineIndex;
        float shown;
        int openedFrame = -1;
        System.Action onClose;

        public bool IsOpen => gameObject.activeSelf;
        // Quadro em que fechou: o Hud segura o controle de Casco até o fim dele, para o mesmo E
        // que fechou a conversa não abrir outra.
        public int ClosedFrame { get; private set; } = -1;
        public string Speaker => nameLabel.text;
        public int LineIndex => lineIndex;
        public string CurrentLine => lineIndex < lines.Length ? lines[lineIndex] : "";
        public bool LineComplete => shown >= CurrentLine.Length;
        public string VisibleText => bodyLabel.text;

        public void Build()
        {
            var frame = UiKit.Frame(transform, UiKit.BottomCenter, new Vector2(0f, 20f), new Vector2(Width, Height)).transform;
            portraitBack = UiKit.Panel(frame, UiKit.Left, new Vector2(20f, 0f), new Vector2(160f, 160f), GameRules.DeepSea);
            portraitImage = UiKit.Picture(portraitBack.transform, null, UiKit.Center, Vector2.zero, new Vector2(150f, 150f));
            nameLabel = UiKit.Label(frame, "", 28, UiKit.TopLeft, new Vector2(PortraitSpace, -14f),
                new Vector2(Width - PortraitSpace - 30f, 36f), TextAnchor.MiddleLeft, GameRules.Orange);
            bodyLabel = UiKit.Label(frame, "", 26, UiKit.TopLeft, new Vector2(PortraitSpace, -56f),
                new Vector2(Width - PortraitSpace - 30f, 110f), TextAnchor.UpperLeft);
            arrowLabel = UiKit.Label(frame, "▼ E", 22, UiKit.BottomRight, new Vector2(-18f, 10f), new Vector2(120f, 30f),
                TextAnchor.MiddleRight, GameRules.Turquoise);
            gameObject.SetActive(false);
        }

        public void Open(string speaker, Sprite portrait, string[] newLines, System.Action closed)
        {
            lines = newLines != null && newLines.Length > 0 ? newLines : new[] { "" };
            lineIndex = 0;
            shown = 0f;
            onClose = closed;
            openedFrame = Time.frameCount;

            bool hasPortrait = portrait != null;
            portraitBack.gameObject.SetActive(hasPortrait);
            portraitImage.sprite = portrait;
            float x = hasPortrait ? PortraitSpace : 28f;
            Place(nameLabel, x);
            Place(bodyLabel, x);
            nameLabel.text = speaker ?? "";

            gameObject.SetActive(true);
            Refresh();
        }

        static void Place(Text label, float x)
        {
            var rect = label.rectTransform;
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
            rect.sizeDelta = new Vector2(Width - x - 30f, rect.sizeDelta.y);
        }

        void Update()
        {
            if (shown < CurrentLine.Length)
                shown = Mathf.Min(CurrentLine.Length, shown + CharsPerSecond * Time.unscaledDeltaTime);
            // O mesmo aperto que abriu a conversa não pode pular a primeira fala.
            if (Time.frameCount != openedFrame && AdvancePressed()) Advance();
            Refresh();
        }

        static bool AdvancePressed() =>
            Controls.Pressed(GameAction.Interact) || Controls.Pressed(GameAction.Jump)
            || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

        // Primeiro completa a fala; com ela inteira, vai para a próxima ou fecha.
        public void Advance()
        {
            if (!IsOpen) return;
            if (!LineComplete)
            {
                shown = CurrentLine.Length;
                Refresh();
                return;
            }
            lineIndex++;
            shown = 0f;
            if (lineIndex >= lines.Length) Close();
            else Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            gameObject.SetActive(false);
            ClosedFrame = Time.frameCount;
            var callback = onClose;
            onClose = null;
            callback?.Invoke();
        }

        void Refresh()
        {
            string line = CurrentLine;
            bodyLabel.text = line.Substring(0, Mathf.Clamp(Mathf.FloorToInt(shown), 0, line.Length));
            arrowLabel.enabled = LineComplete && UiKit.Blink(.8f);
        }
    }
}
