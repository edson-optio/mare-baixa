using UnityEngine;

namespace MareBaixa
{
    // Baú do recife: um beliscão abre e ele solta pérolas pulando. Só abre uma vez.
    //
    // Prefab: SpriteRenderer (sorting 10, bau-fechado, pivô nos pés) + BoxCollider2D trigger
    // (0.9 × 0.7 blocos, offset 0, 0.35) + Chest. Sem Rigidbody2D: o golpe de Casco é uma consulta
    // de sobreposição. Preencher closedSprite (bau-fechado) e openSprite (bau-aberto).
    [RequireComponent(typeof(SpriteRenderer))]
    public class Chest : MonoBehaviour, IHittable
    {
        public Sprite closedSprite;
        public Sprite openSprite;
        public int pearls = 5;

        SpriteRenderer spriteRenderer;
        float openedAt = -10f;

        public bool Opened { get; private set; }

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (closedSprite != null) spriteRenderer.sprite = closedSprite;
        }

        public void Hit(int damage, Vector2 from)
        {
            if (Opened) return;
            Opened = true;
            openedAt = Time.time;
            if (openSprite != null) spriteRenderer.sprite = openSprite;
            Vector2 mouth = (Vector2)transform.position + new Vector2(0f, .6f);
            Pearl.Drop(mouth, pearls, transform.parent);
            Effects.Spark(mouth);
            Director.Instance?.Sound?.Play(Sfx.Door);
            Director.Instance?.CameraRig.Shake(.06f, .1f);
        }

        // Pulinho ao abrir: estica para cima e volta (o pivô nos pés mantém a base no chão).
        void Update()
        {
            float t = (Time.time - openedAt) / .25f;
            float stretch = t < 1f ? Mathf.Sin(t * Mathf.PI) * .15f : 0f;
            transform.localScale = new Vector3(1f - stretch * .5f, 1f + stretch, 1f);
        }
    }
}
