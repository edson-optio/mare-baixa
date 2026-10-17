using UnityEngine;

namespace MareBaixa
{
    // A maré: camada de água semitransparente que sobe sem parar por cima da sala (GDD p. 7 e 10).
    public class Mare : MonoBehaviour
    {
        public static Mare Current { get; private set; }

        public float Level { get; private set; }
        public float Speed { get; set; }
        public float Bottom { get; private set; }
        public float Top { get; private set; }

        // Fração da sala já inundada, para o indicador lateral do HUD.
        public float Fill => Mathf.InverseLerp(Bottom, Top, Level);

        SpriteRenderer body;
        SpriteRenderer surface;
        Sprite[] surfaceFrames;
        float width;

        public static Mare Create(Transform parent, Kit kit, float roomWidth, float roomHeight, float floorY, float speed)
        {
            var go = new GameObject("Maré");
            go.transform.SetParent(parent, false);
            var mare = go.AddComponent<Mare>();
            mare.width = roomWidth + 4f;
            mare.Bottom = floorY - GameRules.TideStartBelowFloor;
            mare.Top = roomHeight;
            mare.Level = mare.Bottom;
            mare.Speed = speed;
            mare.surfaceFrames = kit.waterSurface;

            mare.body = Layer(go.transform, "Água", kit.white, GameRules.Water, 40);
            mare.surface = Layer(go.transform, "Superfície", kit.waterSurface[0], GameRules.WaterSurface, 41);
            mare.surface.drawMode = SpriteDrawMode.Tiled;
            mare.UpdateVisual();
            return mare;
        }

        static SpriteRenderer Layer(Transform parent, string name, Sprite sprite, Color color, int order)
        {
            var renderer = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        void OnEnable() => Current = this;
        void OnDisable() { if (Current == this) Current = null; }

        void Update()
        {
            Level = Mathf.Min(Level + Speed * Time.deltaTime, Top + 1f);
            UpdateVisual();
        }

        public void SetLevel(float level)
        {
            Level = level;
            UpdateVisual();
        }

        void UpdateVisual()
        {
            // Sprite branco de 32 px = 1 bloco; escala para cobrir do fundo da água até o nível,
            // sem tingir a rocha abaixo do chão mais baixo.
            float height = Mathf.Max(0f, Level - Bottom);
            body.transform.localPosition = new Vector3(width * .5f - 2f, Bottom + height * .5f, 0f);
            body.transform.localScale = new Vector3(width, height, 1f);
            int frame = Mathf.FloorToInt(Time.time * 3f) % surfaceFrames.Length;
            surface.sprite = surfaceFrames[frame];
            surface.size = new Vector2(width, .25f);
            surface.transform.localPosition = new Vector3(width * .5f - 2f, Level - .06f, 0f);
        }

        public bool IsUnder(float y) => y < Level;
    }
}
