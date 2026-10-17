using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MareBaixa
{
    // Interface das cenas jogáveis, montada por código no Awake (o Director cria com AddComponent).
    // Lê o estado de Director.Instance e de Casco a cada quadro: concha e resistência, algas,
    // pérolas, ar, maré, dicas e prompts; e abre diálogo, loja, mensagens, pausa e fades.
    public class Hud : MonoBehaviour
    {
        const int MaxPips = 6;
        const float AirBarWidth = 200f;
        const float TideHeight = 320f;
        const float FlashTime = .18f;

        RectTransform root;
        GameObject status;
        Image shellIcon;
        Text shellName;
        readonly Image[] pips = new Image[MaxPips];
        readonly Image[] algae = new Image[GameRules.MaxAlgae];
        Image pearlIcon;
        Text pearlLabel;

        GameObject airGroup;
        Image airIcon, airFill;

        GameObject tideGroup;
        RectTransform tideFill, tideMarker;

        Text titleLabel, hintLabel, promptLabel;

        GameObject messagePanel;
        Text messageTitle, messageBody, messageFooter;

        GameObject pausePanel;
        Button resumeButton, giveUpButton;
        int pauseSelected;
        int pauseOpenedFrame = -1;
        System.Action onResume, onGiveUp;
        readonly UiKit.NavRepeat pauseNav = new UiKit.NavRepeat();

        Image flash, fade;
        float flashUntil;

        public DialogBox Dialog { get; private set; }
        public ShopPanel Shop { get; private set; }

        // Diálogo ou loja abertos (ou fechados neste mesmo quadro): Casco fica parado.
        public bool PanelOpen => Dialog.IsOpen || Shop.IsOpen
                                 || Dialog.ClosedFrame == Time.frameCount || Shop.ClosedFrame == Time.frameCount;

        public string TitleText => titleLabel.text;
        public string HintText { get; private set; } = "";
        public string PromptText { get; private set; } = "";
        public bool AirVisible => airGroup.activeSelf;
        public bool MessageVisible => messagePanel.activeSelf;
        public bool PauseVisible => pausePanel.activeSelf;
        public bool TideVisible => tideGroup.activeSelf;
        public string ShellText => shellName.text;
        public string PearlText => pearlLabel.text;
        public float FadeAlpha => fade.color.a;

        void Awake()
        {
            root = UiKit.Canvas("Interface");
            root.SetParent(transform, false);
            var kit = Kit.Current;

            BuildStatus(kit);
            BuildAir(kit);
            BuildTide();

            titleLabel = UiKit.Label(root, "", 24, UiKit.TopCenter, new Vector2(0f, -16f), new Vector2(580f, 36f));
            promptLabel = UiKit.Label(root, "", 26, UiKit.BottomCenter, new Vector2(0f, 78f), new Vector2(800f, 36f),
                color: GameRules.Sand);
            hintLabel = UiKit.Label(root, "", 22, UiKit.BottomCenter, new Vector2(0f, 22f), new Vector2(1000f, 50f));

            Dialog = UiKit.Stretch("Diálogo", root).gameObject.AddComponent<DialogBox>();
            Dialog.Build();
            Shop = UiKit.Stretch("Loja", root).gameObject.AddComponent<ShopPanel>();
            Shop.Build();

            BuildMessage();
            BuildPause();

            flash = UiKit.Veil(root, 0f, Color.white);
            flash.enabled = false;
            fade = UiKit.Veil(root, 0f);
            fade.enabled = false;
        }

        // ---------------------------------------------------------------- Montagem

        // Canto superior esquerdo: concha, pips de resistência, algas e pérolas.
        void BuildStatus(Kit kit)
        {
            status = UiKit.Box("Estado", root, UiKit.TopLeft, Vector2.zero, new Vector2(360f, 180f)).gameObject;
            var area = status.transform;

            UiKit.Frame(area, UiKit.TopLeft, new Vector2(16f, -16f), new Vector2(340f, 88f));
            shellIcon = UiKit.Picture(area, null, UiKit.TopLeft, new Vector2(24f, -24f), new Vector2(72f, 72f));
            shellName = UiKit.Label(area, "", 24, UiKit.TopLeft, new Vector2(108f, -22f), new Vector2(240f, 32f),
                TextAnchor.MiddleLeft);
            for (int i = 0; i < MaxPips; i++)
            {
                pips[i] = UiKit.Panel(area, UiKit.TopLeft, new Vector2(110f + i * 36f, -66f), new Vector2(28f, 16f),
                    GameRules.Turquoise);
                var border = pips[i].gameObject.AddComponent<Outline>();
                border.effectColor = GameRules.Ink;
                border.effectDistance = new Vector2(2f, -2f);
            }

            UiKit.Frame(area, UiKit.TopLeft, new Vector2(16f, -112f), new Vector2(340f, 56f));
            for (int i = 0; i < algae.Length; i++)
                algae[i] = UiKit.Picture(area, kit != null ? kit.algaIcon : null, UiKit.TopLeft,
                    new Vector2(26f + i * 46f, -120f), new Vector2(40f, 40f));
            float pearlX = 26f + algae.Length * 46f + 24f;
            pearlIcon = UiKit.Picture(area, kit != null ? kit.pearlIcon : null, UiKit.TopLeft,
                new Vector2(pearlX, -120f), new Vector2(40f, 40f));
            pearlLabel = UiKit.Label(area, "", 26, UiKit.TopLeft, new Vector2(pearlX + 48f, -120f), new Vector2(150f, 40f),
                TextAnchor.MiddleLeft, GameRules.Pearl);
        }

        // Barra de ar logo abaixo do estado; só aparece quando falta ar.
        void BuildAir(Kit kit)
        {
            airGroup = UiKit.Box("Ar", root, UiKit.TopLeft, new Vector2(16f, -176f), new Vector2(340f, 44f)).gameObject;
            var area = airGroup.transform;
            airIcon = UiKit.Picture(area, kit != null ? kit.bubbleIcon : null, UiKit.Left, new Vector2(4f, 0f),
                new Vector2(36f, 36f));
            var back = UiKit.Panel(area, UiKit.Left, new Vector2(48f, 0f), new Vector2(AirBarWidth + 8f, 22f),
                new Color(GameRules.Ink.r, GameRules.Ink.g, GameRules.Ink.b, .9f));
            var border = back.gameObject.AddComponent<Outline>();
            border.effectColor = GameRules.Turquoise;
            border.effectDistance = new Vector2(2f, -2f);
            airFill = UiKit.Panel(area, UiKit.Left, new Vector2(52f, 0f), new Vector2(AirBarWidth, 14f), GameRules.Turquoise);
            airGroup.SetActive(false);
        }

        // Lateral direita: régua da maré com o nível da água e a altura de Casco.
        void BuildTide()
        {
            tideGroup = UiKit.Box("Maré", root, UiKit.Right, new Vector2(-18f, 0f), new Vector2(64f, TideHeight + 40f)).gameObject;
            var area = tideGroup.transform;
            UiKit.Label(area, "MARÉ", 18, UiKit.TopCenter, Vector2.zero, new Vector2(80f, 26f), color: GameRules.Turquoise);
            var bar = UiKit.Frame(area, UiKit.BottomCenter, Vector2.zero, new Vector2(24f, TideHeight)).transform;
            tideFill = UiKit.Panel(bar, UiKit.BottomCenter, Vector2.zero, new Vector2(24f, 0f),
                new Color(GameRules.Turquoise.r, GameRules.Turquoise.g, GameRules.Turquoise.b, .85f)).rectTransform;
            tideMarker = UiKit.Panel(bar, UiKit.BottomCenter, Vector2.zero, new Vector2(40f, 8f), GameRules.Orange).rectTransform;
            var border = tideMarker.gameObject.AddComponent<Outline>();
            border.effectColor = GameRules.Ink;
            border.effectDistance = new Vector2(2f, -2f);
            tideGroup.SetActive(false);
        }

        void BuildMessage()
        {
            var veil = UiKit.Veil(root, .55f);
            messagePanel = veil.gameObject;
            messagePanel.name = "Mensagem";
            var frame = UiKit.Frame(veil.transform, UiKit.Center, Vector2.zero, new Vector2(840f, 380f)).transform;
            messageTitle = UiKit.Label(frame, "", 44, UiKit.TopCenter, new Vector2(0f, -24f), new Vector2(800f, 60f),
                color: GameRules.Orange);
            messageBody = UiKit.Label(frame, "", 26, UiKit.TopCenter, new Vector2(0f, -100f), new Vector2(760f, 200f),
                TextAnchor.UpperCenter);
            messageFooter = UiKit.Label(frame, "", 24, UiKit.BottomCenter, new Vector2(0f, 20f), new Vector2(760f, 36f),
                color: GameRules.Turquoise);
            messagePanel.SetActive(false);
        }

        void BuildPause()
        {
            var veil = UiKit.Veil(root, .7f);
            pausePanel = veil.gameObject;
            pausePanel.name = "Pausa";
            var frame = UiKit.Frame(veil.transform, UiKit.Center, Vector2.zero, new Vector2(820f, 540f)).transform;
            UiKit.Label(frame, "PAUSA", 60, UiKit.TopCenter, new Vector2(0f, -24f), new Vector2(600f, 76f), color: GameRules.Orange);
            resumeButton = PauseButton(frame, "Continuar", -124f, 0);
            giveUpButton = PauseButton(frame, "Desistir (voltar à vila)", -196f, 1);
            UiKit.Label(frame, "CONTROLES", 22, UiKit.TopCenter, new Vector2(0f, -278f), new Vector2(700f, 30f),
                color: GameRules.Turquoise);
            UiKit.Label(frame,
                "A/D ou setas: mover   ·   Espaço: pular (segure para ir mais alto)\n" +
                "J: pinças   ·   K: rolar na concha   ·   L: habilidade da concha\n" +
                "E: pegar concha / conversar   ·   Q: comer alga   ·   Esc: pausa",
                20, UiKit.TopCenter, new Vector2(0f, -314f), new Vector2(760f, 110f));
            UiKit.Label(frame, "W/S escolhe  ·  E ou Enter confirma  ·  Esc continua", 18, UiKit.BottomCenter,
                new Vector2(0f, 16f), new Vector2(760f, 28f), color: GameRules.Sand);
            pausePanel.SetActive(false);
        }

        Button PauseButton(Transform frame, string text, float y, int index)
        {
            var button = UiKit.Button(frame, text, UiKit.TopCenter, new Vector2(0f, y), () => ChoosePause(index),
                new Vector2(460f, 58f), 26, hover: false);
            UiKit.OnPointer(button.gameObject, EventTriggerType.PointerEnter, () => SelectPause(index));
            return button;
        }

        // ---------------------------------------------------------------- API das cenas

        public void SetTitle(string title) => titleLabel.text = title ?? "";

        // Clarão branco curto (concha quebrando).
        public void Flash() => flashUntil = Time.unscaledTime + FlashTime;

        public void SetFade(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            fade.color = new Color(GameRules.Ink.r, GameRules.Ink.g, GameRules.Ink.b, alpha);
            fade.enabled = alpha > 0f;
        }

        // Escurece ou clareia a tela em tempo real (vale com o jogo pausado).
        public IEnumerator Fade(float from, float to, float seconds)
        {
            SetFade(from);
            float time = 0f;
            while (time < seconds)
            {
                time += Time.unscaledDeltaTime;
                SetFade(Mathf.Lerp(from, to, time / seconds));
                yield return null;
            }
            SetFade(to);
        }

        public void ShowMessage(string title, string body, string footer)
        {
            messageTitle.text = title ?? "";
            messageBody.text = body ?? "";
            messageFooter.text = footer ?? "";
            messagePanel.SetActive(true);
        }

        public void HideMessage() => messagePanel.SetActive(false);

        public void ShowPause(System.Action resume, System.Action giveUp)
        {
            onResume = resume;
            onGiveUp = giveUp;
            pauseOpenedFrame = Time.frameCount;
            pausePanel.SetActive(true);
            SelectPause(0);
        }

        public void HidePause() => pausePanel.SetActive(false);

        public void ShowDialog(string speaker, Sprite portrait, string[] lines, System.Action onClose) =>
            Dialog.Open(speaker, portrait, lines, onClose);

        public void ShowShop(string title, List<ShopEntry> entries, System.Action onClose) =>
            Shop.Open(title, entries, onClose);

        void SelectPause(int index)
        {
            pauseSelected = index;
            UiKit.Highlight(resumeButton, index == 0);
            UiKit.Highlight(giveUpButton, index == 1);
        }

        void ChoosePause(int index)
        {
            if (!pausePanel.activeSelf) return;
            var action = index == 0 ? onResume : onGiveUp;
            action?.Invoke();
        }

        // ---------------------------------------------------------------- Atualização

        void Update()
        {
            var director = Director.Instance;
            var casco = director != null ? director.Casco : null;
            UpdateStatus(director, casco);
            UpdateAir(casco);
            UpdateTide(casco);
            UpdateTexts(casco);
            UpdatePause();
            UpdateFlash();
        }

        void UpdateStatus(Director director, Casco casco)
        {
            status.SetActive(casco != null);
            if (casco == null) return;
            var kit = Kit.Current;

            bool naked = casco.Shell == ShellType.Nenhuma;
            shellIcon.sprite = naked ? null : ShellSprite(kit, casco.Shell);
            shellIcon.enabled = shellIcon.sprite != null;
            shellName.text = naked ? "SEM CONCHA" : Shells.Get(casco.Shell).Name;
            shellName.color = naked ? GameRules.Danger : GameRules.Cream;
            shellName.enabled = !naked || UiKit.Blink(.6f);

            for (int i = 0; i < pips.Length; i++)
            {
                pips[i].gameObject.SetActive(i < casco.ShellMaxHp);
                pips[i].color = i < casco.ShellHp ? GameRules.Turquoise : GameRules.DeepSea;
            }

            for (int i = 0; i < algae.Length; i++)
                algae[i].color = i < casco.Algae ? Color.white : new Color(1f, 1f, 1f, .25f);
            pearlLabel.text = "× " + (director != null ? director.PearlsShown : 0);
        }

        static Sprite ShellSprite(Kit kit, ShellType type)
        {
            int index = (int)type;
            return kit != null && kit.shellSprites != null && index < kit.shellSprites.Length ? kit.shellSprites[index] : null;
        }

        void UpdateAir(Casco casco)
        {
            bool visible = casco != null && casco.MaxAir > 0f && casco.Air < casco.MaxAir - .01f;
            airGroup.SetActive(visible);
            if (!visible) return;
            float fraction = Mathf.Clamp01(casco.Air / casco.MaxAir);
            airFill.rectTransform.sizeDelta = new Vector2(AirBarWidth * fraction, 14f);
            bool low = fraction < .3f;
            airFill.color = !low ? GameRules.Turquoise : UiKit.Blink(.35f) ? GameRules.Danger : GameRules.Cream;
            airIcon.color = low && UiKit.Blink(.35f) ? GameRules.Danger : Color.white;
        }

        void UpdateTide(Casco casco)
        {
            var mare = Mare.Current;
            tideGroup.SetActive(mare != null);
            if (mare == null) return;
            tideFill.sizeDelta = new Vector2(24f, TideHeight * mare.Fill);
            tideMarker.gameObject.SetActive(casco != null);
            if (casco != null)
            {
                float height = Mathf.InverseLerp(mare.Bottom, mare.Top, casco.Center.y);
                tideMarker.anchoredPosition = new Vector2(0f, TideHeight * height - 4f);
            }
        }

        // Dica e prompt somem quando há painel, pausa ou mensagem por cima.
        void UpdateTexts(Casco casco)
        {
            bool busy = casco == null || casco.Dead || Dialog.IsOpen || Shop.IsOpen
                        || pausePanel.activeSelf || messagePanel.activeSelf;
            string hint = busy ? null : Hint.Near(casco.Center);
            string prompt = busy || casco.Focus == null ? null : casco.Focus.Prompt;
            HintText = hint ?? "";
            PromptText = prompt ?? "";
            hintLabel.text = HintText;
            promptLabel.text = PromptText;
        }

        void UpdatePause()
        {
            if (!pausePanel.activeSelf || Time.frameCount == pauseOpenedFrame) return;
            int step = pauseNav.Read();
            if (step != 0) SelectPause((pauseSelected + step + 2) % 2);
            if (UiKit.ConfirmPressed()) ChoosePause(pauseSelected);
        }

        void UpdateFlash()
        {
            float left = flashUntil - Time.unscaledTime;
            flash.enabled = left > 0f;
            if (left > 0f) flash.color = new Color(1f, 1f, 1f, .55f * left / FlashTime);
        }
    }
}
