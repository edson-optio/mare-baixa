using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MareBaixa
{
    // Funções pequenas para montar a interface (Canvas, textos, imagens e botões) pelo código.
    public static class UiKit
    {
        public static readonly Color ButtonColor = new Color32(24, 92, 98, 255);   // turquesa-escuro
        public static readonly Color PanelColor = new Color(GameRules.Ink.r, GameRules.Ink.g, GameRules.Ink.b, .9f);

        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopCenter = new Vector2(.5f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 Center = new Vector2(.5f, .5f);
        public static readonly Vector2 Left = new Vector2(0f, .5f);
        public static readonly Vector2 Right = new Vector2(1f, .5f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        public static readonly Vector2 BottomCenter = new Vector2(.5f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);

        static Font font;
        public static Font Font => font ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static RectTransform Canvas(string name)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;
            if (Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return (RectTransform)canvasObject.transform;
        }

        // Retângulo vazio; a âncora também é o pivô, então a posição conta a partir daquele canto.
        public static RectTransform Box(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        // Ocupa o pai inteiro (fundos, véus de tela cheia).
        public static RectTransform Stretch(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        // Véu de tela cheia atrás de painéis (pausa, loja, mensagens) e para fades.
        public static Image Veil(Transform parent, float alpha, Color? color = null)
        {
            var image = Stretch("Véu", parent).gameObject.AddComponent<Image>();
            Color tint = color ?? GameRules.Ink;
            image.color = new Color(tint.r, tint.g, tint.b, alpha);
            image.raycastTarget = false;
            return image;
        }

        public static Text Label(Transform parent, string text, int size, Vector2 anchor, Vector2 position, Vector2 boxSize,
            TextAnchor alignment = TextAnchor.MiddleCenter, Color? color = null)
        {
            var rect = Box("Texto", parent, anchor, position, boxSize);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color ?? GameRules.Cream;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = GameRules.Ink;
            outline.effectDistance = new Vector2(2f, -2f);
            return label;
        }

        public static Image Picture(Transform parent, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var image = Box("Imagem", parent, anchor, position, size).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public static Image Panel(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var image = Box("Painel", parent, anchor, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        // Painel com contorno escuro grosso, no estilo dos quadros do jogo.
        public static Image Frame(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color? color = null)
        {
            var image = Panel(parent, anchor, position, size, color ?? PanelColor);
            var border = image.gameObject.AddComponent<Outline>();
            border.effectColor = GameRules.Turquoise;
            border.effectDistance = new Vector2(3f, -3f);
            return image;
        }

        // Botão turquesa-escuro que fica laranja com o mouse em cima. Com hover = false quem
        // decide a cor é o painel dono do botão (seleção por teclado), via Highlight.
        public static Button Button(Transform parent, string text, Vector2 anchor, Vector2 position, UnityAction onClick,
            Vector2? size = null, int fontSize = 28, bool hover = true)
        {
            var rect = Box("Botão " + text, parent, anchor, position, size ?? new Vector2(340f, 60f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(onClick);
            // Tira o foco do EventSystem para Enter/Espaço não clicarem de novo no mesmo botão.
            button.onClick.AddListener(() =>
            {
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            });
            var border = rect.gameObject.AddComponent<Outline>();
            border.effectColor = GameRules.Ink;
            border.effectDistance = new Vector2(4f, -4f);
            Label(rect, text, fontSize, Center, Vector2.zero, rect.sizeDelta);

            if (hover)
            {
                OnPointer(button.gameObject, EventTriggerType.PointerEnter, () => Highlight(button, true));
                OnPointer(button.gameObject, EventTriggerType.PointerExit, () => Highlight(button, false));
            }
            return button;
        }

        public static void Highlight(Button button, bool on)
        {
            if (button != null) button.image.color = on ? GameRules.Orange : ButtonColor;
        }

        public static void SetButtonText(Button button, string text)
        {
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = text;
        }

        public static void OnPointer(GameObject target, EventTriggerType type, UnityAction action)
        {
            var trigger = target.GetComponent<EventTrigger>();
            if (trigger == null) trigger = target.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        // Pisca em tempo real (funciona com o jogo pausado).
        public static bool Blink(float period = .5f) => Mathf.Repeat(Time.unscaledTime, period) < period * .5f;

        // Confirmar em menus: E, Enter, Espaço ou botão A do controle.
        public static bool ConfirmPressed() =>
            Controls.Pressed(GameAction.Interact)
            || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0);

        // Navegação vertical com repetição controlada ao segurar (W/S, setas, analógico).
        public class NavRepeat
        {
            const float FirstDelay = .35f;
            const float RepeatDelay = .12f;
            int held;
            float nextAt;

            // -1 sobe, +1 desce, 0 nada.
            public int Read()
            {
                float vertical = Controls.Simulated ? 0f : Input.GetAxisRaw("Vertical");
                int direction = vertical > .5f ? -1 : vertical < -.5f ? 1 : 0;
                if (direction == 0)
                {
                    held = 0;
                    return 0;
                }
                if (direction != held)
                {
                    held = direction;
                    nextAt = Time.unscaledTime + FirstDelay;
                    return direction;
                }
                if (Time.unscaledTime < nextAt) return 0;
                nextAt = Time.unscaledTime + RepeatDelay;
                return direction;
            }
        }
    }
}
