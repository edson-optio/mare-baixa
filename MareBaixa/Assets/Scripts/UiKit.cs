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

        // Fonte do jogo inteiro: Pixelify Sans (OFL, Assets/Fonts/Resources, licença ao lado). Um pixel do
        // desenho dela vale FontPixel pontos de tamanho (as maiúsculas têm 7 pixels de altura), então todo
        // tamanho é arredondado para múltiplo disso e o texto sai nítido. Sem o arquivo, volta à fonte do Unity.
        public const string FontName = "PixelifySans";
        public const int FontPixel = 11;

        static Font font;
        public static Font Font
        {
            get
            {
                if (font != null) return font;
                font = Resources.Load<Font>(FontName);
                if (font == null) return font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                SharpenFont(font);
                UnityEngine.Font.textureRebuilt += SharpenFont;
                return font;
            }
        }

        // A textura da fonte dinâmica é refeita quando ganha letras novas; mantém o filtro Point.
        static void SharpenFont(Font changed)
        {
            if (changed != font || changed.material == null || changed.material.mainTexture == null) return;
            changed.material.mainTexture.filterMode = FilterMode.Point;
        }

        // Tamanho mais próximo que cai na grade de pixels da fonte (mínimo 2 pixels = 22).
        public static int PixelSize(int size)
        {
            if (Font.name != FontName) return size;
            return Mathf.Max(2, Mathf.RoundToInt(size / (float)FontPixel)) * FontPixel;
        }

        public static RectTransform Canvas(string name)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 100;
            canvas.pixelPerfect = true;   // texto e pixel art alinhados à grade da tela
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;
            if (Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return (RectTransform)canvasObject.transform;
        }

        // Canvas por cima de tudo, até do HUD (cutscenes e créditos). Não depende da câmera.
        public static RectTransform OverlayCanvas(string name, int sortingOrder)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvas.pixelPerfect = true;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;
            return (RectTransform)canvasObject.transform;
        }

        // Área 16:9 centrada no pai (o resto fica com o fundo do pai, como barras de cinema).
        public static RectTransform Widescreen(string name, Transform parent)
        {
            var rect = Stretch(name, parent);
            var fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = GameRules.TargetAspect;
            return rect;
        }

        // Faixa presa à borda de baixo do pai, na largura toda.
        public static RectTransform BottomBand(string name, Transform parent, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = BottomCenter;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
            return rect;
        }

        // Anel branco gerado por código, para o indicador radial de "segure para pular".
        static Sprite ring;
        public static Sprite Ring
        {
            get
            {
                if (ring != null) return ring;
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(size * .5f, size * .5f));
                    float alpha = Mathf.Clamp01((6f - Mathf.Abs(distance - 25f)) / 2f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255f * alpha));
                }
                texture.SetPixels32(pixels);
                texture.Apply();
                ring = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(.5f, .5f), size);
                return ring;
            }
        }

        // Triângulo apontando para cima, em pixels duros (a fonte não tem ▲▼). Gire 180° para baixo.
        static Sprite triangle;
        public static Sprite Triangle
        {
            get
            {
                if (triangle != null) return triangle;
                const int width = 9, height = 5;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                var pixels = new Color32[width * height];
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = Mathf.Abs(x - width / 2) <= height - 1 - y
                        ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                texture.SetPixels32(pixels);
                texture.Apply();
                triangle = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(.5f, .5f), width);
                return triangle;
            }
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
            label.fontSize = PixelSize(size);
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

        // "Segure Esc (ou E) para pular": anel que enche no canto e avisa quando o tempo fecha.
        // Só conta um aperto que começou com o indicador já na tela (a tecla que abriu a cena não vale).
        public class SkipHold
        {
            public const float Seconds = .6f;
            readonly Image fill;
            readonly Text label;
            float held;
            bool armed;

            public float Amount => held / Seconds;

            public SkipHold(Transform parent)
            {
                var area = Box("Pular", parent, TopRight, new Vector2(-24f, -20f), new Vector2(330f, 48f));
                var back = Picture(area, Ring, Right, Vector2.zero, new Vector2(44f, 44f));
                back.color = new Color(1f, 1f, 1f, .25f);
                fill = Picture(area, Ring, Right, Vector2.zero, new Vector2(44f, 44f));
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Radial360;
                fill.fillOrigin = (int)Image.Origin360.Top;
                fill.fillClockwise = true;
                fill.fillAmount = 0f;
                fill.color = GameRules.Orange;
                label = Label(area, "segure Esc para pular", 20, Right, new Vector2(-54f, 0f), new Vector2(270f, 40f),
                    TextAnchor.MiddleRight, GameRules.Cream);
            }

            static bool KeyHeld() =>
                Input.GetKey(KeyCode.Escape) || Controls.Held(GameAction.Pause) || Controls.Held(GameAction.Interact);

            // Chamar a cada quadro; devolve true quando o jogador segurou o bastante.
            public bool Tick()
            {
                bool down = KeyHeld();
                if (!down) armed = true;
                held = armed && down ? held + Time.unscaledDeltaTime : 0f;
                fill.fillAmount = Mathf.Clamp01(Amount);
                label.color = new Color(GameRules.Cream.r, GameRules.Cream.g, GameRules.Cream.b, held > 0f ? 1f : .6f);
                return held >= Seconds;
            }
        }

        // Navegação vertical com repetição controlada ao segurar (W/S, setas, analógico).
        // Com horizontal = true, A/D e setas laterais também andam (direita = +1), para fileiras de botões.
        public class NavRepeat
        {
            const float FirstDelay = .35f;
            const float RepeatDelay = .12f;
            readonly bool horizontal;
            int held;
            float nextAt;

            public NavRepeat(bool horizontal = false) => this.horizontal = horizontal;

            // -1 sobe, +1 desce, 0 nada.
            public int Read()
            {
                float vertical = Controls.Simulated ? 0f : Input.GetAxisRaw("Vertical");
                int direction = vertical > .5f ? -1 : vertical < -.5f ? 1 : 0;
                if (direction == 0 && horizontal)
                {
                    float side = Controls.Simulated ? 0f : Input.GetAxisRaw("Horizontal");
                    direction = side > .5f ? 1 : side < -.5f ? -1 : 0;
                }
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
