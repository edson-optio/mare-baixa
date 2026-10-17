using UnityEngine;

namespace MareBaixa
{
    // Bolha de ar presa no recife: encostar enche o ar de Casco. Ela estoura e volta a se formar
    // depois de um tempo, então dá para planejar o caminho debaixo d'água em volta dela.
    //
    // Prefab: SpriteRenderer (sorting 10, bolha-1) + Rigidbody2D Kinematic + CircleCollider2D
    // trigger (raio 0.4) + AirBubble. Preencher frames (bolha-1/2).
    [RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public class AirBubble : MonoBehaviour
    {
        public Sprite[] frames;
        public float framesPerSecond = 3f;
        public float respawnTime = 6f;
        public float bobHeight = .12f;
        public float bobPeriod = 1.8f;

        SpriteRenderer spriteRenderer;
        Collider2D trigger;
        Vector3 home;
        float phase;
        float respawnAt;
        float shownAt;
        bool popped;

        void Awake()
        {
            var body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            spriteRenderer = GetComponent<SpriteRenderer>();
            trigger = GetComponent<Collider2D>();
            home = transform.position;
            phase = Random.value * bobPeriod;
            shownAt = -10f;
        }

        void Start()
        {
            if (frames != null && frames.Length > 0)
            {
                var animator = GetComponent<SpriteAnimator>();
                if (animator == null) animator = gameObject.AddComponent<SpriteAnimator>();
                animator.Play(frames, framesPerSecond);
            }
        }

        void Update()
        {
            if (popped && Time.time >= respawnAt) Show();

            float wave = Mathf.Sin((Time.time + phase) / bobPeriod * Mathf.PI * 2f);
            transform.position = home + new Vector3(wave * bobHeight * .4f, wave * bobHeight, 0f);
            // Ao reaparecer, cresce do nada em vez de surgir de repente.
            float grow = Mathf.Clamp01((Time.time - shownAt) / .25f);
            transform.localScale = new Vector3(grow, grow, 1f);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (popped) return;
            var casco = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponent<Casco>()
                : other.GetComponent<Casco>();
            if (casco == null || casco.Dead) return;
            casco.RefillAir();   // já toca Sfx.Bubble
            Effects.Spark(transform.position);
            popped = true;
            respawnAt = Time.time + respawnTime;
            spriteRenderer.enabled = false;
            trigger.enabled = false;
        }

        void Show()
        {
            popped = false;
            shownAt = Time.time;
            spriteRenderer.enabled = true;
            trigger.enabled = true;
        }
    }
}
