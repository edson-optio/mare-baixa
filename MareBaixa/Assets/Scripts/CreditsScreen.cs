using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MareBaixa
{
    // Créditos finais: rolagem por cima de tudo, em tempo não escalado, pulável como a cutscene
    // (segurar Esc ou E). Espaço/Enter acelera. Uso: yield return CreditsScreen.Play();
    public class CreditsScreen : MonoBehaviour
    {
        const int SortingOrder = 510;
        const float ScrollSpeed = 70f;
        const float FastFactor = 4f;
        const float FadeTime = .5f;
        const float HoldAtEnd = 2.5f;

        static CreditsScreen current;
        public static bool Playing => current != null;

        // Cada linha: texto e tamanho. Tamanho 0 = espaço vazio.
        static readonly (string text, int size, int kind)[] Lines =
        {
            ("MARÉ BAIXA", 80, 1),
            ("um jogo sobre conchas, maré e coragem", 26, 0),
            ("", 0, 0),
            ("EQUIPE", 30, 2),
            ("CC7140", 26, 0),
            ("[Nome 1]", 28, 0),
            ("[Nome 2]", 28, 0),
            ("[Nome 3]", 28, 0),
            ("", 0, 0),
            ("PROGRAMAÇÃO", 30, 2),
            ("Edson Pedreira de Oliveira Junior", 28, 0),
            ("", 0, 0),
            ("ELENCO", 30, 2),
            ("Casco · o ermitão pequeno demais", 24, 0),
            ("Vovó Búzia · a voz da experiência", 24, 0),
            ("Ostrácia · colecionadora de pérolas", 24, 0),
            ("Dona Lagosta · ferreira de conchas", 24, 0),
            ("Bolota · coragem em formato de baiacu", 24, 0),
            ("Polvo Abissal · quase dono da praia", 24, 0),
            ("", 0, 0),
            ("AGRADECIMENTOS", 30, 2),
            ("Kenney.nl, itch.io e OpenGameArt pelos assets gratuitos", 24, 0),
            ("Professores e colegas da disciplina", 24, 0),
            ("E você, por descer até o fundo", 24, 0),
            ("", 0, 0),
            ("", 0, 0),
            ("Coralina é inventada. O lixo, não.", 30, 3),
            ("Cada garrafa, pneu, tampinha e lata desta aventura existe aos montes", 24, 0),
            ("no mar de verdade. Casco até aproveita uma lata velha como concha;", 24, 0),
            ("a maioria dos bichos não tem essa sorte.", 24, 0),
            ("Jogue o lixo no lugar certo: a praia agradece.", 26, 3),
            ("", 0, 0),
            ("", 0, 0),
            ("Obrigado por jogar!", 44, 1),
        };

        RectTransform content;
        Image veil;
        UiKit.SkipHold skipHold;
        float contentHeight;
        bool skip, done;
        float previousTimeScale = 1f;

        public static IEnumerator Play()
        {
            float timeScale = Time.timeScale;
            if (current != null)
            {
                timeScale = current.previousTimeScale;
                current.done = true;
                Destroy(current.gameObject);
            }
            var root = UiKit.OverlayCanvas("Créditos", SortingOrder);
            var credits = root.gameObject.AddComponent<CreditsScreen>();
            credits.previousTimeScale = timeScale;
            credits.Build(root);
            current = credits;
            while (credits != null && !credits.done) yield return null;
        }

        public static void Skip()
        {
            if (current != null) current.skip = true;
        }

        void Build(RectTransform root)
        {
            Time.timeScale = 0f;
            var back = UiKit.Veil(root, 1f, GameRules.Ink);
            back.raycastTarget = true;

            var stage = UiKit.Widescreen("Rolagem", root);
            stage.gameObject.AddComponent<RectMask2D>();
            content = UiKit.Box("Linhas", stage, UiKit.BottomCenter, Vector2.zero, new Vector2(1200f, 10f));
            content.pivot = UiKit.TopCenter;

            float y = 0f;
            foreach (var (text, size, kind) in Lines)
            {
                float height = size == 0 ? 36f : size * 1.5f;
                if (size > 0)
                {
                    Color color = kind == 1 ? GameRules.Orange : kind == 2 ? GameRules.Turquoise
                        : kind == 3 ? GameRules.Sand : GameRules.Cream;
                    UiKit.Label(content, text, size, UiKit.TopCenter, new Vector2(0f, -y), new Vector2(1160f, height), color: color);
                }
                if (kind == 2) y += 6f;
                y += height;
            }
            contentHeight = y;
            content.sizeDelta = new Vector2(1200f, contentHeight);
            content.anchoredPosition = Vector2.zero;   // começa logo abaixo da borda de baixo

            skipHold = new UiKit.SkipHold(stage);
            veil = UiKit.Veil(root, 1f, Color.black);
        }

        void Start() => StartCoroutine(Run());

        IEnumerator Run()
        {
            yield return FadeVeil(1f, 0f);
            var stage = (RectTransform)content.parent;
            // Para quando a última linha chega ao meio da tela.
            float lastLine = Lines[Lines.Length - 1].size * 1.5f;
            float stop = contentHeight - lastLine * .5f + stage.rect.height * .5f;
            float held = 0f;
            while (!skip && held < HoldAtEnd)
            {
                float speed = ScrollSpeed * (FastPressed() ? FastFactor : 1f);
                float y = content.anchoredPosition.y + speed * Time.unscaledDeltaTime;
                if (y >= stop)
                {
                    y = stop;
                    held += Time.unscaledDeltaTime;
                }
                content.anchoredPosition = new Vector2(0f, y);
                yield return null;
            }
            yield return FadeVeil(veil.color.a, 1f);
            Time.timeScale = previousTimeScale;
            done = true;
            Destroy(gameObject);
        }

        static bool FastPressed() =>
            Controls.Held(GameAction.Jump) || Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter);

        void Update()
        {
            if (skipHold != null && skipHold.Tick()) skip = true;
        }

        IEnumerator FadeVeil(float from, float to)
        {
            float time = 0f;
            while (time < FadeTime)
            {
                time += Time.unscaledDeltaTime;
                veil.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, time / FadeTime));
                yield return null;
            }
            veil.color = new Color(0f, 0f, 0f, to);
        }

        void OnDestroy()
        {
            if (current == this) current = null;
            if (!done && Time.timeScale == 0f) Time.timeScale = previousTimeScale;
            done = true;
        }
    }
}
