using UnityEngine;

namespace MareBaixa
{
    // Efeito curto, em tempo de jogo: não desaparece enquanto o impacto/pausa congela a ação.
    public class CombatTrail : MonoBehaviour
    {
        public float lifetime = .22f;
        float age;
        SpriteRenderer sprite;
        LineRenderer line;
        Color color;

        void Start()
        {
            sprite = GetComponent<SpriteRenderer>();
            line = GetComponent<LineRenderer>();
            color = sprite != null ? sprite.color : line.startColor;
        }

        void Update()
        {
            age += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - age / lifetime);
            var tint = new Color(color.r, color.g, color.b, color.a * alpha);
            if (sprite != null) sprite.color = tint;
            if (line != null) { line.startColor = tint; line.endColor = new Color(tint.r, tint.g, tint.b, 0f); }
            if (age >= lifetime) Destroy(gameObject);
        }
    }
}
