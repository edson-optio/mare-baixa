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
        const float BossBarWidth = 520f;
        const float MapWidth = 800f;
        const float BannerWidth = 560f;   // de x 360 a 920: não encosta no painel de algas
        const float BannerHold = 2.2f;
        const float BannerSlide = .35f;
        const float BannerOffset = 140f;

        RectTransform root;
        GameObject status;
        Image shellIcon;
        Text shellName, shellAbility;
        Text weaponName, weaponDetail;
        Image weaponIcon;
        Text toastLabel, checkpointLabel;
        float toastUntil;
        readonly Image[] pips = new Image[MaxPips];
        readonly Image[] algae = new Image[GameRules.MaxAlgae];
        Image pearlIcon;
        Text pearlLabel;

        GameObject airGroup;
        Image airIcon, airFill;

        GameObject tideGroup;
        RectTransform tideFill, tideMarker;

        Text titleLabel, hintLabel, promptLabel;

        CanvasGroup bossGroup;
        Text bossName;
        RectTransform bossFill, bossChip;
        Boss shownBoss;
        float bossChipFraction;

        RectTransform mapArea;

        RectTransform banner;
        CanvasGroup bannerGroup;
        Image bannerTop, bannerBottom;
        Text bannerTitle, bannerSubtitle;
        Coroutine bannerRoutine;

        GameObject messagePanel;
        Text messageTitle, messageBody, messageFooter;

        GameObject pausePanel;
        Button resumeButton, giveUpButton, menuButton;
        int pauseSelected;
        int pauseOpenedFrame = -1;
        System.Action onResume, onGiveUp, onMenu;
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
        public string AbilityText => shellAbility.text;
        public string WeaponText => weaponName.text;
        public bool BossBarVisible => bossGroup.alpha > .01f;
        public string BossText => bossName.text;
        public int MapCells { get; private set; }
        public bool BannerVisible => banner.gameObject.activeSelf;
        public string BannerText => bannerTitle.text;
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
            BuildBossBar();
            BuildBanner();

            titleLabel = UiKit.Label(root, "", 24, UiKit.TopCenter, new Vector2(0f, -16f), new Vector2(580f, 36f));
            promptLabel = UiKit.Label(root, "", 26, UiKit.BottomCenter, new Vector2(0f, 78f), new Vector2(800f, 36f),
                color: GameRules.Sand);
            hintLabel = UiKit.Label(root, "", 22, UiKit.BottomCenter, new Vector2(0f, 22f), new Vector2(1000f, 50f));
            toastLabel = UiKit.Label(root, "", 23, UiKit.BottomCenter, new Vector2(0f, 132f), new Vector2(1080f, 42f),
                color: GameRules.Turquoise);

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
            status = UiKit.Box("Estado", root, UiKit.TopLeft, Vector2.zero, new Vector2(360f, 190f)).gameObject;
            var area = status.transform;

            UiKit.Frame(area, UiKit.TopLeft, new Vector2(16f, -16f), new Vector2(340f, 100f));
            shellIcon = UiKit.Picture(area, null, UiKit.TopLeft, new Vector2(24f, -30f), new Vector2(72f, 72f));
            shellName = UiKit.Label(area, "", 22, UiKit.TopLeft, new Vector2(104f, -20f), new Vector2(248f, 30f),
                TextAnchor.MiddleLeft);
            shellAbility = UiKit.Label(area, "", 22, UiKit.TopLeft, new Vector2(104f, -50f), new Vector2(248f, 26f),
                TextAnchor.MiddleLeft, GameRules.Sand);
            FitOneLine(shellName, 14);
            FitOneLine(shellAbility, 12);
            for (int i = 0; i < MaxPips; i++)
            {
                pips[i] = UiKit.Panel(area, UiKit.TopLeft, new Vector2(110f + i * 36f, -80f), new Vector2(28f, 16f),
                    GameRules.Turquoise);
                var border = pips[i].gameObject.AddComponent<Outline>();
                border.effectColor = GameRules.Ink;
                border.effectDistance = new Vector2(2f, -2f);
            }

            UiKit.Frame(area, UiKit.TopLeft, new Vector2(16f, -124f), new Vector2(340f, 56f));
            for (int i = 0; i < algae.Length; i++)
                algae[i] = UiKit.Picture(area, kit != null ? kit.algaIcon : null, UiKit.TopLeft,
                    new Vector2(26f + i * 46f, -132f), new Vector2(40f, 40f));
            float pearlX = 26f + algae.Length * 46f + 24f;
            pearlIcon = UiKit.Picture(area, kit != null ? kit.pearlIcon : null, UiKit.TopLeft,
                new Vector2(pearlX, -132f), new Vector2(40f, 40f));
            pearlLabel = UiKit.Label(area, "", 26, UiKit.TopLeft, new Vector2(pearlX + 48f, -132f), new Vector2(150f, 40f),
                TextAnchor.MiddleLeft, GameRules.Pearl);

            UiKit.Frame(area, UiKit.TopLeft, new Vector2(16f, -188f), new Vector2(340f, 66f));
            weaponIcon = UiKit.Picture(area, null, UiKit.TopLeft, new Vector2(24f, -199f), new Vector2(64f, 42f));
            weaponName = UiKit.Label(area, "", 21, UiKit.TopLeft, new Vector2(96f, -193f), new Vector2(250f, 28f),
                TextAnchor.MiddleLeft, GameRules.Cream);
            weaponDetail = UiKit.Label(area, "", 18, UiKit.TopLeft, new Vector2(96f, -222f), new Vector2(250f, 25f),
                TextAnchor.MiddleLeft, GameRules.Sand);
            FitOneLine(weaponName, 14);
            FitOneLine(weaponDetail, 12);
        }

        // Textos do quadro de estado mudam de tamanho (concha, arma, legenda do controle): em vez de quebrar a
        // linha e invadir o rótulo de baixo, a fonte encolhe até caber. O best fit só atua com Truncate.
        static void FitOneLine(Text label, int minSize)
        {
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMaxSize = label.fontSize;
            label.resizeTextMinSize = Mathf.Min(minSize, label.fontSize);
        }

        // Barra de ar logo abaixo do estado; só aparece quando falta ar.
        void BuildAir(Kit kit)
        {
            airGroup = UiKit.Box("Ar", root, UiKit.TopLeft, new Vector2(16f, -262f), new Vector2(340f, 44f)).gameObject;
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

        // No topo, logo abaixo do título (longe do chão onde a luta acontece): nome do chefe e a vida,
        // com um rastro claro que acompanha o dano. Cabe entre o painel da concha e a régua da maré.
        void BuildBossBar()
        {
            var area = UiKit.Box("Chefe", root, UiKit.TopCenter, new Vector2(0f, -56f), new Vector2(BossBarWidth + 40f, 66f));
            bossGroup = area.gameObject.AddComponent<CanvasGroup>();
            bossGroup.alpha = 0f;
            bossGroup.blocksRaycasts = false;
            bossName = UiKit.Label(area, "", 26, UiKit.TopCenter, Vector2.zero, new Vector2(BossBarWidth, 32f),
                color: GameRules.Orange);
            var bar = UiKit.Frame(area, UiKit.BottomCenter, Vector2.zero, new Vector2(BossBarWidth + 8f, 26f),
                new Color(GameRules.Ink.r, GameRules.Ink.g, GameRules.Ink.b, .92f)).transform;
            bossChip = UiKit.Panel(bar, UiKit.Left, new Vector2(4f, 0f), new Vector2(BossBarWidth, 18f), GameRules.Cream).rectTransform;
            bossFill = UiKit.Panel(bar, UiKit.Left, new Vector2(4f, 0f), new Vector2(BossBarWidth, 18f), GameRules.Danger).rectTransform;
        }

        // Faixa de entrada de zona no terço de cima, entre o estado (esquerda) e a régua da maré.
        void BuildBanner()
        {
            banner = UiKit.Box("Zona", root, UiKit.TopCenter, new Vector2(0f, -130f), new Vector2(BannerWidth, 124f));
            bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.blocksRaycasts = false;
            bannerGroup.interactable = false;
            var back = UiKit.Stretch("Fundo", banner).gameObject.AddComponent<Image>();
            back.color = new Color(GameRules.Ink.r, GameRules.Ink.g, GameRules.Ink.b, .78f);
            back.raycastTarget = false;
            bannerTop = UiKit.Panel(banner, UiKit.TopCenter, Vector2.zero, new Vector2(BannerWidth, 5f), GameRules.Turquoise);
            bannerBottom = UiKit.Panel(banner, UiKit.BottomCenter, Vector2.zero, new Vector2(BannerWidth, 5f), GameRules.Turquoise);
            bannerTitle = UiKit.Label(banner, "", 52, UiKit.TopCenter, new Vector2(0f, -12f), new Vector2(BannerWidth - 20f, 64f));
            bannerSubtitle = UiKit.Label(banner, "", 22, UiKit.BottomCenter, new Vector2(0f, 14f), new Vector2(BannerWidth - 20f, 30f),
                color: GameRules.Cream);
            banner.gameObject.SetActive(false);
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
            var frame = UiKit.Frame(veil.transform, UiKit.Center, Vector2.zero, new Vector2(880f, 640f)).transform;
            UiKit.Label(frame, "PAUSA", 60, UiKit.TopCenter, new Vector2(0f, -18f), new Vector2(600f, 72f), color: GameRules.Orange);
            mapArea = UiKit.Box("Mapa", frame, UiKit.TopCenter, new Vector2(0f, -96f), new Vector2(MapWidth + 40f, 112f));
            checkpointLabel = UiKit.Label(frame, "", 19, UiKit.TopCenter, new Vector2(0f, -188f), new Vector2(800f, 30f),
                color: GameRules.Sand);
            resumeButton = PauseButton(frame, "Continuar", -228f, 0);
            giveUpButton = PauseButton(frame, "Voltar à vila (apaga o banco)", -296f, 1);
            menuButton = PauseButton(frame, "Menu (mantém o último banco)", -364f, 2);
            UiKit.Label(frame, "CONTROLES", 22, UiKit.TopCenter, new Vector2(0f, -440f), new Vector2(700f, 30f),
                color: GameRules.Turquoise);
            UiKit.Label(frame,
                "A/D / analógico: mover   ·   Espaço / {padJump}: pular (segure)\n" +
                "J / {padAttack}: atacar · K / {padDodge}: rolar · L / {padAbility}: habilidade\n" +
                "E / RB (R1): interagir · Q / LB (L1): alga · Esc / Start: pausa",
                20, UiKit.TopCenter, new Vector2(0f, -473f), new Vector2(800f, 100f));
            UiKit.Label(frame, "W/S / direcional escolhe · Enter / {padJump} confirma · Esc / Start continua", 18, UiKit.BottomCenter,
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

        // Nome da zona chegando (primeira sala de cada zona e bolha de ar): entra deslizando da
        // esquerda, fica BannerHold s e sai para a direita. Só visual, em tempo normal do jogo.
        public void ShowZoneBanner(string zoneName, string subtitle, Color color)
        {
            if (bannerRoutine != null) StopCoroutine(bannerRoutine);
            bannerTitle.text = (zoneName ?? "").ToUpperInvariant();
            bannerTitle.color = color;
            bannerSubtitle.text = subtitle ?? "";
            bannerTop.color = bannerBottom.color = color;
            bannerRoutine = StartCoroutine(PlayBanner());
        }

        IEnumerator PlayBanner()
        {
            banner.gameObject.SetActive(true);
            yield return SlideBanner(-BannerOffset, 0f, 0f, 1f);
            float held = 0f;
            while (held < BannerHold)
            {
                held += Time.deltaTime;
                yield return null;
            }
            yield return SlideBanner(0f, BannerOffset, 1f, 0f);
            banner.gameObject.SetActive(false);
            bannerRoutine = null;
        }

        IEnumerator SlideBanner(float fromX, float toX, float fromAlpha, float toAlpha)
        {
            float time = 0f;
            while (true)
            {
                float t = Mathf.Clamp01(time / BannerSlide);
                float eased = t * t * (3f - 2f * t);
                banner.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, eased), -130f);
                bannerGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
                if (t >= 1f) yield break;
                time += Time.deltaTime;
                yield return null;
            }
        }

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

        public void Toast(string message, float seconds = 3f)
        {
            toastLabel.text = message;
            toastUntil = Time.time + seconds;
        }

        public void ShowPause(System.Action resume, System.Action giveUp, System.Action menu)
        {
            onResume = resume;
            onGiveUp = giveUp;
            onMenu = menu;
            checkpointLabel.text = Progress.HasCheckpoint
                ? "Banco salvo na etapa " + (Progress.Data.checkpoint.roomIndex + 1) + " · ao sair, volta ao último descanso"
                : "Nenhum banco salvo · descanse num abrigo para guardar a jornada";
            pauseOpenedFrame = Time.frameCount;
            BuildMap();
            pausePanel.SetActive(true);
            SelectPause(0);
        }

        public void HidePause() => pausePanel.SetActive(false);

        public void ShowDialog(string speaker, Sprite portrait, string[] lines, System.Action onClose) =>
            Dialog.Open(speaker, portrait, lines, onClose);

        public void ShowShop(string title, List<ShopEntry> entries, System.Action onClose) =>
            Shop.Open(title, entries, onClose);

        // Faixa da run na pausa (GDD p. 4, "Pausa e mapa"): as salas do plano agrupadas por zona,
        // as feitas cheias, a atual em laranja e as futuras só com o contorno na cor da zona.
        void BuildMap()
        {
            foreach (Transform child in mapArea) Destroy(child.gameObject);
            MapCells = 0;
            var run = Director.Instance as RunManager;
            var plan = run != null ? run.Plan : null;
            if (plan == null || plan.Count == 0)
            {
                UiKit.Label(mapArea, "Vila da Praia · aqui a maré não pega", 24, UiKit.Center, Vector2.zero,
                    new Vector2(MapWidth, 40f), color: GameRules.Sand);
                return;
            }

            var legend = new[] { Zone.Recife, Zone.Cidade, Zone.Fossa, Zone.Bolha };
            var names = new[] { "Recife", "Cidade", "Fossa", "Abrigos / bancos" };
            for (int i = 0; i < legend.Length; i++)
                UiKit.Label(mapArea, names[i], 18, UiKit.TopCenter, new Vector2(-300f + i * 200f, 0f),
                    new Vector2(190f, 26f), color: ZoneColor(legend[i]));
            float step = Mathf.Min(32f, MapWidth / plan.Count);
            float cell = step * .78f;
            float x = -(plan.Count * step) * .5f;
            int current = Mathf.Clamp(run.RoomIndex, 0, plan.Count - 1);
            for (int i = 0; i < plan.Count; i++)
            {
                Color color = ZoneColor(plan[i].zone);
                float cx = x + i * step + step * .5f;
                bool here = i == current;
                float size = here ? cell * 1.25f : cell;
                var square = UiKit.Panel(mapArea, UiKit.TopCenter, new Vector2(cx, -44f),
                    new Vector2(size, size), here ? GameRules.Orange : i < current ? color : GameRules.Ink);
                var border = square.gameObject.AddComponent<Outline>();
                border.effectColor = here ? GameRules.Cream : color;
                border.effectDistance = new Vector2(2f, -2f);
                if (Progress.HasCheckpoint && Progress.Data.checkpoint.roomIndex == i)
                    UiKit.Panel(mapArea, UiKit.TopCenter, new Vector2(cx, -74f), new Vector2(cell, 4f), GameRules.Cream);
                MapCells++;
            }

            UiKit.Label(mapArea, plan[current].zoneName + "  ·  sala " + (current + 1) + " de " + plan.Count, 20,
                UiKit.BottomCenter, new Vector2(0f, 0f), new Vector2(MapWidth, 28f), color: GameRules.Cream);
        }

        public static Color ZoneColor(Zone zone) => zone switch
        {
            Zone.Recife => GameRules.Turquoise,
            Zone.Cidade => new Color32(132, 158, 128, 255),   // verde-cinza
            Zone.Fossa => GameRules.Violet,
            Zone.Bolha => new Color32(176, 226, 240, 255),
            _ => GameRules.Sand,
        };

        void SelectPause(int index)
        {
            pauseSelected = index;
            UiKit.Highlight(resumeButton, index == 0);
            UiKit.Highlight(giveUpButton, index == 1);
            UiKit.Highlight(menuButton, index == 2);
        }

        void ChoosePause(int index)
        {
            if (!pausePanel.activeSelf) return;
            var action = index == 0 ? onResume : index == 1 ? onGiveUp : onMenu;
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
            UpdateBossBar();
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
            shellAbility.text = AbilityHint(casco.Shell);
            if (casco.AbilityCooldown > 0f) shellAbility.text += " · " + casco.AbilityCooldown.ToString("F1") + "s";
            var weapon = Weapons.Get(casco.Weapon);
            weaponName.text = weapon.Name;
            weaponDetail.text = Controls.FormatHints("J / {padAttack} · " + weapon.Damage + " dano");
            weaponIcon.sprite = Weapons.Sprite(casco.Weapon);
            weaponIcon.enabled = weaponIcon.sprite != null;

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

        // Linha curta embaixo do nome da concha: tecla e habilidade.
        public static string AbilityHint(ShellType type) => Controls.FormatHints(type switch
        {
            ShellType.Tampinha => "Espaço / {padJump} 2×: pulo duplo",
            ShellType.Espiral => "L / {padAbility}: cambalhota",
            ShellType.Lata => "L / {padAbility}: investida",
            ShellType.Lampada => "L / {padAbility}: clarão",
            _ => "",
        });

        void UpdateBossBar()
        {
            var boss = Boss.Current;
            bool show = boss != null && !boss.Defeated && boss.MaxHp > 0;
            if (boss != null && boss != shownBoss)
            {
                shownBoss = boss;
                bossChipFraction = 1f;
            }
            bossGroup.alpha = Mathf.MoveTowards(bossGroup.alpha, show ? 1f : 0f, Time.unscaledDeltaTime * 2.5f);
            if (shownBoss == null) return;

            float fraction = Mathf.Clamp01((float)shownBoss.Hp / Mathf.Max(1, shownBoss.MaxHp));
            if (shownBoss.Defeated) fraction = 0f;
            // O rastro claro desce devagar até a vida de verdade.
            bossChipFraction = bossChipFraction > fraction
                ? Mathf.MoveTowards(bossChipFraction, fraction, Time.unscaledDeltaTime * .35f)
                : fraction;
            bossFill.sizeDelta = new Vector2(BossBarWidth * fraction, 18f);
            bossChip.sizeDelta = new Vector2(BossBarWidth * bossChipFraction, 18f);
            string name = (shownBoss.DisplayName ?? "").ToUpperInvariant();
            bossName.text = shownBoss.Phase > 1 ? name + "  ·  fase " + shownBoss.Phase : name;
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
            toastLabel.enabled = Time.time < toastUntil && !busy;
        }

        void UpdatePause()
        {
            if (!pausePanel.activeSelf || Time.frameCount == pauseOpenedFrame) return;
            if (Controls.BackPressed) { onResume?.Invoke(); return; }
            int step = pauseNav.Read();
            if (step != 0) SelectPause((pauseSelected + step + 3) % 3);
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
