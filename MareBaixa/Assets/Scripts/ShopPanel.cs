using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MareBaixa
{
    // Loja da vila (Ostrácia e Dona Lagosta): lista de itens com W/S ou setas, confirma com
    // E/Enter/Espaço ou clique, fecha com Esc ou na linha "Sair" que sempre vem no fim.
    // O Hud cria e monta (Build); o objeto fica desligado enquanto a loja está fechada.
    public class ShopPanel : MonoBehaviour
    {
        const float Width = 860f;
        const float RowHeight = 56f;
        const float RowStep = 64f;
        const float ListTop = 120f;
        const float FooterSpace = 84f;

        readonly List<ShopEntry> entries = new List<ShopEntry>();
        readonly List<Button> rows = new List<Button>();
        readonly List<Text> labels = new List<Text>();
        readonly List<Text> details = new List<Text>();
        readonly UiKit.NavRepeat nav = new UiKit.NavRepeat();

        RectTransform frame;
        Text titleLabel, balanceLabel, statusLabel;
        Image balanceIcon;
        int openedFrame = -1;
        System.Action onClose;

        public int Selected { get; private set; }
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
            UiKit.Label(frame, "W/S ou setas: escolher  ·  E, Enter ou Espaço: confirmar  ·  Esc: sair", 20,
                UiKit.BottomCenter, new Vector2(0f, 12f), new Vector2(Width - 40f, 28f), color: GameRules.Sand);
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
            frame.sizeDelta = new Vector2(Width, ListTop + Count * RowStep + FooterSpace);

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
                () => Choose(index), new Vector2(Width - 60f, RowHeight), 24, hover: false);
            UiKit.OnPointer(button.gameObject, EventTriggerType.PointerEnter, () => Select(index));
            labels.Add(UiKit.Label(button.transform, "", 24, UiKit.Left, new Vector2(18f, 0f), new Vector2(480f, RowHeight),
                TextAnchor.MiddleLeft));
            details.Add(UiKit.Label(button.transform, "", 22, UiKit.Right, new Vector2(-18f, 0f), new Vector2(290f, RowHeight),
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
            if (Controls.Pressed(GameAction.Pause)) Close();
            else if (UiKit.ConfirmPressed()) Confirm();
        }

        void Redraw()
        {
            int pearls = Director.Instance != null ? Director.Instance.PearlsShown : Progress.Data.pearls;
            balanceLabel.text = "Suas pérolas: " + pearls;
            balanceIcon.sprite = Kit.Current != null ? Kit.Current.pearlIcon : null;
            balanceIcon.enabled = balanceIcon.sprite != null;

            for (int i = 0; i < rows.Count; i++)
            {
                bool item = i < entries.Count;
                bool selected = i == Selected;
                labels[i].text = item ? entries[i].label : "Sair";
                details[i].text = item ? Detail(entries[i]) : "";
                details[i].color = selected ? GameRules.Cream : GameRules.Sand;
                UiKit.Highlight(rows[i], selected);
            }
        }

        static string Detail(ShopEntry entry) => entry.detail != null ? entry.detail() ?? "" : "";
    }
}
