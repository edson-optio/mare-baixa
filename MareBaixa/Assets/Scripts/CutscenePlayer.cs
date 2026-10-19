using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MareBaixa
{
    // Toca uma cena de corte (Cutscenes.cs) por cima de tudo: quadro em tela cheia com zoom e pan
    // lentos, legenda em máquina de escrever e fades entre quadros. Roda em tempo não escalado e
    // congela o jogo enquanto dura. E/Espaço/Enter avança; segurar Esc (ou E) 0.6 s pula a cena.
    // Uso: yield return CutscenePlayer.Play(CutsceneId.PerolaRoubada);
    public class CutscenePlayer : MonoBehaviour
    {
        const int SortingOrder = 500;
        const float FadeTime = .35f;
        const float CharsPerSecond = 40f;
        const float ReadTime = 1.2f;   // mínimo depois que a legenda termina de aparecer
        const float Zoom = .08f;
        const float Pan = 24f;

        static CutscenePlayer current;

        public static bool Playing => current != null;
        public static CutsceneId? CurrentId => current != null ? current.id : (CutsceneId?)null;
        public static int FrameIndex => current != null ? current.index : -1;
        public static string CaptionText => current != null ? current.caption.text : "";

        CutsceneId id;
        Cutscene scene;
        int index = -1;
        float frameTime, frameLength, typed;
        bool skip, advance, done;
        int openedFrame;
        float previousTimeScale = 1f;

        RectTransform stage, picture;
        Image pictureImage, fallback, veil, blackBackdrop;
        Text fallbackTitle, caption, sceneTitle, arrow;
        AspectRatioFitter pictureFitter;
        UiKit.SkipHold skipHold;

        public static IEnumerator Play(CutsceneId id)
        {
            float timeScale = Time.timeScale;
            if (current != null)
            {
                timeScale = current.previousTimeScale;
                current.done = true;
                Destroy(current.gameObject);
            }
            var player = Create(id);
            player.previousTimeScale = timeScale;
            while (player != null && !player.done) yield return null;
        }

        public static void SkipCurrent()
        {
            if (current != null) current.skip = true;
        }

        static CutscenePlayer Create(CutsceneId id)
        {
            var root = UiKit.OverlayCanvas("Cutscene", SortingOrder);
            var player = root.gameObject.AddComponent<CutscenePlayer>();
            player.id = id;
            player.scene = Cutscenes.Get(id);
            player.Build(root);
            current = player;
            return player;
        }

        void Build(RectTransform root)
        {
            openedFrame = Time.frameCount;
            Time.timeScale = 0f;

            // Fundo preto (barras fora do 16:9); fica transparente até o primeiro fade fechar.
            blackBackdrop = UiKit.Veil(root, 0f, Color.black);
            blackBackdrop.raycastTarget = true;   // segura cliques para nada passar para o jogo

            stage = UiKit.Widescreen("Quadro", root);
            stage.gameObject.AddComponent<RectMask2D>();
            fallback = stage.gameObject.AddComponent<Image>();
            fallback.raycastTarget = false;
            fallbackTitle = UiKit.Label(stage, "", 54, UiKit.Center, new Vector2(0f, 60f), new Vector2(1000f, 200f),
                color: new Color(GameRules.Cream.r, GameRules.Cream.g, GameRules.Cream.b, .35f));

            picture = UiKit.Stretch("Imagem", stage);
            pictureImage = picture.gameObject.AddComponent<Image>();
            pictureImage.raycastTarget = false;
            pictureFitter = picture.gameObject.AddComponent<AspectRatioFitter>();
            pictureFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            pictureFitter.aspectRatio = GameRules.TargetAspect;

            // Faixa da legenda: escura e meio transparente, para ler em cima de qualquer arte.
            var band = UiKit.BottomBand("Legenda", stage, 150f);
            var bandImage = band.gameObject.AddComponent<Image>();
            bandImage.color = new Color(GameRules.Ink.r, GameRules.Ink.g, GameRules.Ink.b, .78f);
            bandImage.raycastTarget = false;
            caption = UiKit.Label(band, "", 30, UiKit.Center, new Vector2(0f, 4f), new Vector2(1060f, 120f));
            arrow = UiKit.Label(band, "E ▶", 22, UiKit.BottomRight, new Vector2(-24f, 12f), new Vector2(100f, 30f),
                TextAnchor.MiddleRight, GameRules.Turquoise);

            sceneTitle = UiKit.Label(stage, scene.title.ToUpperInvariant(), 26, UiKit.TopLeft, new Vector2(28f, -22f),
                new Vector2(600f, 40f), TextAnchor.MiddleLeft, GameRules.Orange);
            skipHold = new UiKit.SkipHold(stage);

            stage.gameObject.SetActive(false);
            veil = UiKit.Veil(root, 0f, Color.black);
        }

        void Start() => StartCoroutine(Run());

        IEnumerator Run()
        {
            // Escurece o jogo, monta o primeiro quadro e clareia sobre ele.
            yield return FadeVeil(0f, 1f);
            blackBackdrop.color = Color.black;
            stage.gameObject.SetActive(true);

            for (int i = 0; i < scene.frames.Length && !skip; i++)
            {
                ShowFrame(i);
                yield return FadeVeil(1f, 0f);
                while (!skip && !(advance && LineComplete) && frameTime < frameLength)
                {
                    if (advance && !LineComplete) typed = scene.frames[index].text.Length;
                    advance = false;
                    yield return null;
                }
                advance = false;
                yield return FadeVeil(veil.color.a, 1f);
            }

            stage.gameObject.SetActive(false);
            blackBackdrop.color = Color.clear;
            Time.timeScale = previousTimeScale;
            yield return FadeVeil(1f, 0f);
            done = true;
            Destroy(gameObject);
        }

        bool LineComplete => index >= 0 && typed >= scene.frames[index].text.Length;

        IEnumerator FadeVeil(float from, float to)
        {
            float time = 0f;
            float length = skip ? FadeTime * .5f : FadeTime;
            while (time < length)
            {
                time += Time.unscaledDeltaTime;
                SetVeil(Mathf.Lerp(from, to, time / length));
                yield return null;
            }
            SetVeil(to);
        }

        void SetVeil(float alpha)
        {
            veil.color = new Color(0f, 0f, 0f, alpha);
            veil.enabled = alpha > 0f;
        }

        void ShowFrame(int i)
        {
            index = i;
            var frame = scene.frames[i];
            frameTime = 0f;
            typed = 0f;
            frameLength = Mathf.Max(frame.seconds, frame.text.Length / CharsPerSecond + ReadTime);

            Sprite sprite = Kit.Current != null ? Kit.Current.CutscenePanel(frame.image) : null;
            pictureImage.sprite = sprite;
            pictureImage.enabled = sprite != null;
            if (sprite != null) pictureFitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);

            // Sem arte: quadro na cor da cena, mais escuro a cada quadro, com o título no meio.
            fallback.color = Color.Lerp(scene.tint, GameRules.Ink, i * .12f);
            fallbackTitle.text = sprite != null ? "" : scene.title + "\n<size=26>" + (i + 1) + " / " + scene.frames.Length + "</size>";
            sceneTitle.enabled = i == 0;
            caption.text = "";
            UpdateMotion();
        }

        void Update()
        {
            if (index < 0) return;
            if (skipHold.Tick()) skip = true;
            if (Time.frameCount != openedFrame && AdvancePressed()) advance = true;

            frameTime += Time.unscaledDeltaTime;
            string text = scene.frames[index].text;
            typed = Mathf.Min(text.Length, typed + CharsPerSecond * Time.unscaledDeltaTime);
            caption.text = text.Substring(0, Mathf.FloorToInt(typed));
            arrow.enabled = LineComplete && UiKit.Blink(.8f);
            UpdateMotion();
        }

        static bool AdvancePressed() =>
            Controls.Pressed(GameAction.Interact) || Controls.Pressed(GameAction.Jump)
            || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

        // Ken Burns de HQ: aproxima devagar e desliza para um lado (alterna a cada quadro).
        void UpdateMotion()
        {
            float t = frameLength > 0f ? Mathf.Clamp01(frameTime / frameLength) : 0f;
            float eased = t * t * (3f - 2f * t);
            picture.localScale = Vector3.one * (1f + Zoom * eased);
            Vector2 direction = index % 2 == 0 ? new Vector2(-1f, .4f) : new Vector2(1f, -.3f);
            picture.anchoredPosition = direction * Pan * eased;
            fallbackTitle.rectTransform.localScale = Vector3.one * (1f + Zoom * .5f * eased);
        }

        void OnDestroy()
        {
            if (current == this) current = null;
            // Cena trocada no meio da cutscene: não deixa o jogo congelado.
            if (!done && Time.timeScale == 0f) Time.timeScale = previousTimeScale;
            done = true;
        }
    }
}
