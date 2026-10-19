using UnityEngine;

namespace MareBaixa
{
    // Escuridão da Fossa e da fase 2 do Polvo (GDD p. 8): uma textura pequena (160×90) redesenhada
    // a cada quadro, presa à câmera, escura onde não há luz e com um furo de borda suave para cada
    // LightSource na tela. O filtro bilinear deixa as bordas macias. Fica acima da água (ordem 60)
    // e abaixo do HUD. Quem monta a sala escura cria com Create e destrói junto com a sala.
    [DefaultExecutionOrder(100)] // depois da câmera (CameraFollow) se mexer
    public class Escuridao : MonoBehaviour
    {
        const int Width = 160;
        const int Height = 90;
        const float Margin = 1.12f;     // cobre um pouco além da tela por causa do tremor da câmera
        const int SortingOrder = 60;
        const float InnerRadius = .45f; // fração do raio com luz plena
        const float Glow = .16f;        // brilho colorido no centro da luz

        static readonly Color DarkColor = new Color32(5, 7, 20, 255);
        static readonly Color WarmColor = new Color32(255, 176, 92, 255);
        static readonly Color ColdColor = new Color32(120, 190, 255, 255);

        public static Escuridao Current { get; private set; }

        // Opacidade pedida (0 = claro, 1 = breu), já com a transição de SetDarkness.
        public float Darkness { get; private set; }
        // O que aparece de fato neste quadro (com o clarão da Lâmpada).
        public float VisibleDarkness => Darkness * (1f - FlashAmount);

        float fromDarkness, toDarkness, changeStart, changeLength;
        float flashStart = -100f, flashLength;

        Texture2D texture;
        Sprite sprite;
        SpriteRenderer view;
        Color32[] pixels;
        float[] lit;
        Color[] tint;
        Vector2 bottomLeft, unit;

        public static Escuridao Create(float darkness)
        {
            var escuridao = new GameObject("Escuridão").AddComponent<Escuridao>();
            escuridao.Darkness = escuridao.fromDarkness = escuridao.toDarkness = Mathf.Clamp01(darkness);
            escuridao.LateUpdate();
            return escuridao;
        }

        void Awake()
        {
            Current = this;
            texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "Escuridão",
            };
            pixels = new Color32[Width * Height];
            lit = new float[Width * Height];
            tint = new Color[Width * Height];
            sprite = Sprite.Create(texture, new Rect(0f, 0f, Width, Height), new Vector2(.5f, .5f), 1f);
            view = gameObject.AddComponent<SpriteRenderer>();
            view.sprite = sprite;
            view.sortingOrder = SortingOrder;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            Destroy(sprite);
            Destroy(texture);
        }

        // Muda a escuridão aos poucos (o Polvo apaga a sala na fase 2).
        public void SetDarkness(float target, float seconds)
        {
            fromDarkness = Darkness;
            toDarkness = Mathf.Clamp01(target);
            changeStart = Time.time;
            changeLength = Mathf.Max(0f, seconds);
            if (changeLength <= 0f) Darkness = toDarkness;
        }

        // Clarão da Lâmpada: clareia tudo de uma vez e volta ao escuro durante "seconds".
        public void Flash(float seconds)
        {
            flashStart = Time.time;
            flashLength = Mathf.Max(.1f, seconds);
        }

        float FlashAmount
        {
            get
            {
                float t = Time.time - flashStart;
                if (t < 0f || t > flashLength) return 0f;
                const float Rise = .06f;
                if (t < Rise) return t / Rise;
                float hold = flashLength * .3f;
                if (t < hold) return 1f;
                return 1f - Mathf.SmoothStep(0f, 1f, (t - hold) / (flashLength - hold));
            }
        }

        // Quanto escuro está um ponto do mundo agora (0 a 1), pelo último quadro desenhado.
        public float AlphaAt(Vector2 world)
        {
            if (!view.enabled) return 0f;
            int x = Mathf.FloorToInt((world.x - bottomLeft.x) / unit.x);
            int y = Mathf.FloorToInt((world.y - bottomLeft.y) / unit.y);
            if (x < 0 || y < 0 || x >= Width || y >= Height) return VisibleDarkness;
            return pixels[y * Width + x].a / 255f;
        }

        void LateUpdate()
        {
            if (changeLength > 0f)
            {
                float t = Mathf.Clamp01((Time.time - changeStart) / changeLength);
                Darkness = Mathf.Lerp(fromDarkness, toDarkness, t);
                if (t >= 1f) changeLength = 0f;
            }

            var cam = Camera.main;
            if (cam == null) return;
            float viewHeight = cam.orthographicSize * 2f * Margin;
            float viewWidth = viewHeight * cam.aspect;
            Vector3 center = cam.transform.position;
            transform.position = new Vector3(center.x, center.y, 0f);
            transform.rotation = Quaternion.identity;
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            transform.localScale = new Vector3(viewWidth / Width / parentScale.x, viewHeight / Height / parentScale.y, 1f);
            bottomLeft = new Vector2(center.x - viewWidth * .5f, center.y - viewHeight * .5f);
            unit = new Vector2(viewWidth / Width, viewHeight / Height);

            float dark = VisibleDarkness;
            view.enabled = dark > .001f;
            if (!view.enabled) return;
            Redraw(dark);
        }

        void Redraw(float dark)
        {
            System.Array.Clear(lit, 0, lit.Length);

            foreach (var light in LightSource.All)
            {
                if (light == null) continue;
                float radius = light.CurrentRadius;
                if (radius <= 0f) continue;
                Vector2 position = light.Position;
                Color color = light.warm ? WarmColor : ColdColor;

                int x0 = Mathf.Max(0, Mathf.FloorToInt((position.x - radius - bottomLeft.x) / unit.x));
                int x1 = Mathf.Min(Width - 1, Mathf.CeilToInt((position.x + radius - bottomLeft.x) / unit.x));
                int y0 = Mathf.Max(0, Mathf.FloorToInt((position.y - radius - bottomLeft.y) / unit.y));
                int y1 = Mathf.Min(Height - 1, Mathf.CeilToInt((position.y + radius - bottomLeft.y) / unit.y));
                if (x0 > x1 || y0 > y1) continue;

                float inverse = 1f / radius;
                for (int y = y0; y <= y1; y++)
                {
                    float dy = (bottomLeft.y + (y + .5f) * unit.y - position.y) * inverse;
                    int row = y * Width;
                    for (int x = x0; x <= x1; x++)
                    {
                        float dx = (bottomLeft.x + (x + .5f) * unit.x - position.x) * inverse;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        if (distance >= 1f) continue;
                        float amount = 1f - Mathf.SmoothStep(0f, 1f, (distance - InnerRadius) / (1f - InnerRadius));
                        int i = row + x;
                        if (amount > lit[i])
                        {
                            lit[i] = amount;
                            tint[i] = color;
                        }
                    }
                }
            }

            for (int i = 0; i < pixels.Length; i++)
            {
                float light = lit[i];
                float shadow = dark * (1f - light);
                float glow = Glow * light * dark;
                float alpha = Mathf.Min(1f, shadow + glow);
                Color color = glow > 0f ? Color.Lerp(DarkColor, tint[i], glow / Mathf.Max(.0001f, alpha)) : DarkColor;
                pixels[i] = new Color32((byte)(color.r * 255f), (byte)(color.g * 255f), (byte)(color.b * 255f),
                    (byte)(alpha * 255f));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false);
        }
    }
}
