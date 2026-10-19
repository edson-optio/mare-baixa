using UnityEngine;

namespace MareBaixa
{
    // Correnteza: uma célula 1 × 1 da sala que empurra Casco para um lado enquanto o centro dele
    // estiver na água. Várias células juntas formam o rio; o empurrão não soma entre elas.
    //
    // Prefab: SpriteRenderer (sorting 4, correnteza-1, meio transparente, arte apontando para a
    // direita) + Rigidbody2D Kinematic + BoxCollider2D trigger (1 × 1) + Correnteza. A sala
    // preenche direction (-1 esquerda, 1 direita). Preencher frames (correnteza-1/2).
    [RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public class Correnteza : MonoBehaviour
    {
        public float direction = 1f;
        public float strength = 3.5f;
        public Sprite[] frames;
        public float framesPerSecond = 6f;
        public float alpha = .5f;

        // Passo de física em que Casco já foi empurrado, para duas células não empurrarem em dobro.
        static float pushedAt = -1f;

        SpriteRenderer spriteRenderer;
        float phase;

        void Awake()
        {
            var body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            // Dormindo, deixaria de avisar Casco parado dentro dela.
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            spriteRenderer = GetComponent<SpriteRenderer>();
            phase = Random.value;
        }

        void Start()
        {
            spriteRenderer.flipX = direction < 0f;
            spriteRenderer.color = new Color(1f, 1f, 1f, alpha);
        }

        // Os riscos trocam de quadro, cada célula numa fase, para o rio não piscar inteiro junto.
        void Update()
        {
            if (frames != null && frames.Length > 0)
                spriteRenderer.sprite = frames[Mathf.FloorToInt((Time.time + phase) * framesPerSecond) % frames.Length];
        }

        void OnTriggerStay2D(Collider2D other)
        {
            var casco = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponent<Casco>()
                : other.GetComponent<Casco>();
            if (casco == null || casco.Dead) return;
            var mare = Mare.Current;
            if (mare == null || !mare.IsUnder(casco.Center.y)) return;
            if (Mathf.Approximately(pushedAt, Time.fixedTime)) return;
            pushedAt = Time.fixedTime;
            casco.Push(new Vector2(Mathf.Sign(direction) * strength, 0f));
        }
    }
}
