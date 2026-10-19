using UnityEngine;

namespace MareBaixa
{
    // Fundo fixo na câmera e silhuetas em parallax de cada zona. A run atravessa três zonas na mesma
    // cena, então o fundo troca junto com a sala (Director.BuildRoom).
    public class ZoneBackground : MonoBehaviour
    {
        public SpriteRenderer far;
        public SpriteRenderer silhouettes;

        static ZoneBackground current;

        void Awake() => current = this;
        void OnDestroy() { if (current == this) current = null; }

        public static void Show(Zone zone)
        {
            if (current == null || Kit.Current == null) return;
            ZoneArt art = Kit.Current.Art(zone);
            if (art.background != null) current.far.sprite = art.background;
            if (art.silhouettes != null)
            {
                current.silhouettes.sprite = art.silhouettes;
                current.silhouettes.size = new Vector2(art.silhouettes.bounds.size.x * 3f, art.silhouettes.bounds.size.y);
            }
            Camera.main.backgroundColor = art.backgroundColor;
        }
    }
}
