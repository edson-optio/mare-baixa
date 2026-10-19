using UnityEngine;

namespace MareBaixa
{
    // Bolota de tinta do Polvo Abissal (fase 2): voa devagar, cai um pouco e espirra ao bater no
    // chão ou em Casco, sumindo logo depois. Como o espinho, testa o chão por consulta.
    //
    // Prefab: SpriteRenderer (sorting 15, polvo-tinta) + Rigidbody2D Kinematic + CircleCollider2D
    // trigger (raio ~0.25) + JatoDeTinta.
    //
    // Números finais: gravidade 2.5, vida 4.5 s, espirro 0.3 s; tira 1 de concha (Casco.Hurt).
    [RequireComponent(typeof(Rigidbody2D))]
    public class JatoDeTinta : MonoBehaviour
    {
        public float gravity = 2.5f;
        public float lifetime = 4.5f;
        public float groundProbe = .12f;
        public float splashTime = .3f;

        Rigidbody2D body;
        SpriteRenderer spriteRenderer;
        Vector2 velocity;
        float dieAt;
        float splashAt = -1f;
        int groundMask;

        public bool Splashed => splashAt >= 0f;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            spriteRenderer = GetComponent<SpriteRenderer>();
            groundMask = LayerMask.GetMask("Chao");
            dieAt = Time.time + lifetime;
        }

        public void Launch(Vector2 newVelocity)
        {
            velocity = newVelocity;
            dieAt = Time.time + lifetime;
        }

        void FixedUpdate()
        {
            if (Splashed) return;
            velocity.y -= gravity * Time.fixedDeltaTime;
            Vector2 next = body.position + velocity * Time.fixedDeltaTime;
            body.MovePosition(next);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            if (Physics2D.OverlapCircle(next, groundProbe, groundMask) != null) Splash();
            else if (Time.time >= dieAt) Destroy(gameObject);
        }

        // Espirra: para, se espalha e desbota.
        void Update()
        {
            if (!Splashed) return;
            float t = (Time.time - splashAt) / splashTime;
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.8f, t);
            if (spriteRenderer != null)
            {
                var color = spriteRenderer.color;
                color.a = 1f - t;
                spriteRenderer.color = color;
            }
            if (t >= 1f) Destroy(gameObject);
        }

        void Splash()
        {
            if (Splashed) return;
            splashAt = Time.time;
            var collider = GetComponent<Collider2D>();
            if (collider != null) collider.enabled = false;
            Effects.Spark(transform.position);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (Splashed) return;
            var casco = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponent<Casco>()
                : other.GetComponent<Casco>();
            // Rolando (ou piscando depois de um golpe) a tinta passa direto.
            if (casco == null || casco.Dead || casco.Invulnerable) return;
            casco.Hurt(transform.position);
            Splash();
        }
    }
}
