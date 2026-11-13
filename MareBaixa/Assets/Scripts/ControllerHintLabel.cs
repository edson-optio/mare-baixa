using UnityEngine;
using UnityEngine.UI;

namespace MareBaixa
{
    // As legendas acompanham a conexão/troca de controle, inclusive com o jogo pausado.
    public class ControllerHintLabel : MonoBehaviour
    {
        Text label;
        string template;
        bool? nintendo;

        public void Bind(Text target, string text)
        {
            label = target;
            template = text;
            Refresh();
        }

        void Update() => Refresh();

        void Refresh()
        {
            bool current = Controls.NintendoLayout;
            if (nintendo == current) return;
            nintendo = current;
            label.text = Controls.FormatHints(template);
        }
    }
}
