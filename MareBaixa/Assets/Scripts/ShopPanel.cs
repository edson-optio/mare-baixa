using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MareBaixa
{
    // Loja da vila (Ostrácia e Dona Lagosta): lista de itens com W/S ou setas, confirma com
    // E/Enter/Espaço ou clique, fecha com Esc ou na linha "Sair" que sempre vem no fim.
    // O Hud cria e monta (Build); o objeto fica desligado enquanto a loja está fechada.
    // Com mais de MaxVisible linhas, a lista vira uma janela que rola com a seleção (setas ▲▼).
    public class ShopPanel : MonoBehaviour
    {
        const float Width = 900f;
        const float RowHeight = 52f;
        const float RowStep = 60f;
        const int MaxVisible = 8;
        const float RowWidth = Width - 60f;
        const float CellMargin = 18f;   // borda da linha até o texto, dos dois lados
        const float ColumnGap = 16f;    // folga mínima entre o nome e o detalhe
        const float MaxDetailWidth = 360f;
        const float ListTop = 120f;
        const float FooterSpace = 84f;

        readonly List<ShopEntry> entries = new List<ShopEntry>();
        readonly List<Button> rows = new List<Button>();
        readonly List<Text> labels = new List<Text>();
        readonly List<Text> details = new List<Text>();
        readonly UiKit.NavRepeat nav = new UiKit.NavRepeat();

        RectTransform frame;
        Text titleLabel, balanceLabel, statusLabel;
        Image upArrow, downArrow;
        int first;
        Image balanceIcon;
        int openedFrame = -1;
        System.Action onClose;

        public int Selected { get; private set; }
        public int FirstVisible => first;
        public int VisibleRows => Mathf.Min(Count, MaxVisible);
        // Itens mais a linha "Sair".
        public int Count => entries.Count + 1;
        public bool IsOpen => gameObject.activeSelf;
        public int ClosedFrame { get; private set; } = -1;
        public string Title => titleLabel.text;
        public string BalanceText => balanceLabel.text;
        public string StatusText => statusLabel.text;
        public string LabelAt(int index) => index >= 0 && index < labels.Count ? labels[index].text : "";
        public string DetailAt(int index) => index >= 0 && index < details.Count ? details[index].text : "";

        public void Build()
        {
            UiKit.Veil(transform, .45f);
            frame = UiKit.Frame(transform, UiKit.Center, Vector2.zero, new Vector2(Width, 300f)).rectTransform;
            titleLabel = UiKit.Label(frame, "", 34, UiKit.TopCenter, new Vector2(0f, -18f), new Vector2(Width - 40f, 44f),
                color: GameRules.Orange);
            balanceLabel = UiKit.Label(frame, "", 24, UiKit.TopRight, new Vector2(-68f, -72f), new Vector2(320f, 34f),
                TextAnchor.MiddleRight);
            balanceIcon = UiKit.Picture(frame, null, UiKit.TopRight, new Vector2(-26f, -72f), new Vector2(34f, 34f));
            statusLabel = UiKit.Label(frame, "", 22, UiKit.BottomCenter, new Vector2(0f, 46f), new Vector2(Width - 40f, 30f));
            UiKit.Label(frame, "W/S / direcional: escolher · Enter / {padJump}: confirmar · Esc / {padDodge}: sair", 20,
                UiKit.BottomCenter, new Vector2(0f, 12f), new Vector2(Width - 40f, 28f), color: GameRules.Sand);
            upArrow = UiKit.Picture(frame, UiKit.Triangle, UiKit.TopLeft, new Vector2(6f, -ListTop - 4f), new Vector2(18f, 10f));
            upArrow.color = GameRules.Turquoise;
            downArrow = UiKit.Picture(frame, UiKit.Triangle, UiKit.TopLeft, new Vector2(6f, 0f), new Vector2(18f, 10f));
            downArrow.color = GameRules.Turquoise;
            downArrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
            gameObject.SetActive(false);
        }

        public void Open(string title, List<ShopEntry> newEntries, System.Action closed)
        {
            foreach (var row in rows) Destroy(row.gameObject);
            rows.Clear();
            labels.Clear();
            details.Clear();
            entries.Clear();
            if (newEntries != null) entries.AddRange(newEntries);

            for (int i = 0; i < Count; i++) AddRow(i);
            frame.sizeDelta = new Vector2(Width, ListTop + VisibleRows * RowStep + FooterSpace);
            downArrow.rectTransform.anchoredPosition = new Vector2(6f, -ListTop - VisibleRows * RowStep + 16f);
            first = 0;

            titleLabel.text = title ?? "";
            statusLabel.text = "";
            onClose = closed;
            openedFrame = Time.frameCount;
            Selected = 0;
            gameObject.SetActive(true);
            Redraw();
        }

        void AddRow(int index)
        {
            var button = UiKit.Button(frame, "", UiKit.TopCenter, new Vector2(0f, -ListTop - index * RowStep),
                () => Choose(index), new Vector2(RowWidth, RowHeight), 24, hover: false);
            UiKit.OnPointer(button.gameObject, EventTriggerType.PointerEnter, () => Select(index));
            // Uma linha só: a fonte encolhe (best fit) em vez de quebrar o texto. A caixa tem a altura de
            // uma linha, então duas linhas nunca cabem.
            var label = UiKit.Label(button.transform, "", 22, UiKit.Left, new Vector2(CellMargin, 0f), new Vector2(500f, 30f),
                TextAnchor.MiddleLeft);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 16;
            label.resizeTextMaxSize = label.fontSize;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            labels.Add(label);
            details.Add(UiKit.Label(button.transform, "", 22, UiKit.Right, new Vector2(-CellMargin, 0f), new Vector2(320f, RowHeight),
                TextAnchor.MiddleRight, GameRules.Sand));
            rows.Add(button);
        }

        void Choose(int index)
        {
            Select(index);
            Confirm();
        }

        public void Select(int index)
        {
            if (!IsOpen) return;
            Selected = Mathf.Clamp(index, 0, Count - 1);
            Redraw();
        }

        // Compra o item escolhido (ou fecha, se for "Sair") e redesenha preços e saldo.
        public void Confirm()
        {
            if (!IsOpen) return;
            if (Selected >= entries.Count)
            {
                Close();
                return;
            }
            var entry = entries[Selected];
            bool ok = entry.onSelect != null && entry.onSelect();
            if (Director.Instance != null) Director.Instance.Sound.Play(ok ? Sfx.Buy : Sfx.Error);
            statusLabel.text = ok ? "Feito!" : "Não dá: faltam pérolas ou já está no máximo.";
            statusLabel.color = ok ? GameRules.Turquoise : GameRules.Danger;
            Redraw();
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

        void Update()
        {
            // O E que abriu a loja não pode comprar o primeiro item.
            if (Time.frameCount == openedFrame) return;
            int step = nav.Read();
            if (step != 0) Select((Selected + step + Count) % Count);
            if (Controls.CancelPressed) Close();
            else if (UiKit.ConfirmPressed()) Confirm();
        }

        void Redraw()
        {
            int pearls = Director.Instance != null ? Director.Instance.PearlsShown : Progress.Data.pearls;
            balanceLabel.text = "Suas pérolas: " + pearls;
            balanceIcon.sprite = Kit.Current != null ? Kit.Current.pearlIcon : null;
            balanceIcon.enabled = balanceIcon.sprite != null;

            // Janela de linhas visíveis seguindo a seleção.
            int visible = VisibleRows;
            first = Mathf.Clamp(Selected - visible / 2, 0, Mathf.Max(0, Count - visible));
            upArrow.enabled = first > 0;
            downArrow.enabled = first + visible < Count;

            for (int i = 0; i < rows.Count; i++)
            {
                bool shown = i >= first && i < first + visible;
                rows[i].gameObject.SetActive(shown);
                if (shown)
                    ((RectTransform)rows[i].transform).anchoredPosition = new Vector2(0f, -ListTop - (i - first) * RowStep);
                bool item = i < entries.Count;
                bool selected = i == Selected;
                labels[i].text = item ? entries[i].label : "Sair";
                details[i].text = item ? Detail(entries[i]) : "";
                FitColumns(labels[i], details[i]);
                details[i].color = selected ? GameRules.Cream : GameRules.Sand;
                UiKit.Highlight(rows[i], selected);
            }
        }

        // O detalhe ocupa só a largura do próprio texto (até MaxDetailWidth); o nome fica com o resto da
        // linha menos a folga, e o best fit encolhe o nome se ainda assim não couber.
        static void FitColumns(Text label, Text detail)
        {
            float detailWidth = Mathf.Min(Mathf.Ceil(detail.preferredWidth), MaxDetailWidth);
            detail.rectTransform.sizeDelta = new Vector2(detailWidth, detail.rectTransform.sizeDelta.y);
            float gap = detailWidth > 0f ? ColumnGap : 0f;
            float labelWidth = RowWidth - 2f * CellMargin - detailWidth - gap;
            label.rectTransform.sizeDelta = new Vector2(labelWidth, label.rectTransform.sizeDelta.y);
        }

        static string Detail(ShopEntry entry) => entry.detail != null ? entry.detail() ?? "" : "";
    }
}
